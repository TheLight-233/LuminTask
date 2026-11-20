using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using LuminThread.Utility;

namespace LuminThread.TaskSource.Promise;

// One installing owner and one owner per installed callback. A slow loser retains
// its observation and this cleared node, never the other inputs or the output array.
internal abstract class CombinerNode : SlotObserver, ILuminCacheNode
{
    private readonly struct Observation
    {
        internal readonly CombinerNode Join;
        internal readonly ulong Handle;
        internal readonly int Index;
        internal Observation(CombinerNode join, ulong handle, int index) =>
            (Join, Handle, Index) = (join, handle, index);
    }

    private int _owners, _remaining;
    protected bool Any, Closed;
    protected ulong Handle;
    protected abstract int Charge { get; }
    public ILuminCacheNode? CacheNext { get; set; }

    protected void Begin(ulong handle, bool any, int count)
    {
        Handle = handle;
        Any = any;
        Closed = false;
        _remaining = count;
        _owners = 1;
    }

    protected void Watch(ulong handle, int index)
    {
        if (handle == 0 || LuminTaskSource.IsCompleted(handle))
        {
            Observe(handle, index);
            return;
        }
        Interlocked.Increment(ref _owners);
        try
        {
            if ((uint)index >= LuminTaskSource.ObserverIndexCount || !LuminTaskSource.TryRegisterObserver(handle, this, index))
                RegisterWithState(handle, index);
        }
        catch { Exit(); throw; }
    }

    private void RegisterWithState(ulong handle, int index)
    {
        var observation = StateTuple.Create(new Observation(this, handle, index));
        try { LuminTaskSource.OnCompleted(handle, OnReady, observation); }
        catch { observation.Dispose(); throw; }
    }

    private static void OnReady(object state)
    {
        var box = (StateTuple<Observation>)state;
        var child = box.Item;
        box.Dispose();
        child.Join.Completed(child.Handle, child.Index);
    }

    internal sealed override void Completed(ulong handle, int index)
    {
        try { Observe(handle, index); }
        catch (Exception ex) { LuminTaskScheduler.PublishUnobservedTaskException(ex); }
        finally { Exit(); }
    }

    // With this gate held, only the winner can transition to Closed.
    protected bool SucceedLocked() => Closed = Any || --_remaining == 0;

    private void Observe(ulong handle, int index)
    {
        Exception? error = null;
        bool drain;
        lock (this)
        {
            drain = Closed;
            if (!drain)
            {
                try
                {
                    if (handle != 0) Read(handle, index);
                    if (!SucceedLocked()) return;
                }
                catch (Exception ex) { error = ex; Closed = true; }
            }
        }
        if (drain)
        {
            if (handle != 0) LuminTaskSource.TryForget(handle);
        }
        else if (error != null)
        {
            Clear();
            LuminTaskSource.TrySetException(Handle, error);
        }
        else Publish(index);
    }

    protected abstract void Read(ulong handle, int index);
    protected abstract void Publish(int index);
    protected virtual void Clear() { }

    protected void Reject()
    {
        lock (this) Closed = true;
        // A callback may have won publication and still be outside the lock.
        // Consume through the normal waiting path in either case.
        LuminTaskSource.TrySetResult(Handle);
        LuminTaskSource.ToTask(Handle).Forget();
    }

    protected void Exit()
    {
        if (Interlocked.Decrement(ref _owners) != 0) return;
        Clear();
        Handle = 0;
        LuminTaskIdleCache.Return(this, Charge);
    }
}

internal sealed class VoidJoin : CombinerNode
{
    protected override int Charge => 96;
    protected override void Read(ulong handle, int index) => LuminTaskSource.ConsumeVoid(handle);
    protected override void Publish(int index)
    {
        if (Any) LuminTaskResultBridge.TrySetResult(Handle, index);
        else LuminTaskSource.TrySetResult(Handle);
    }

    internal static ulong Create(bool any, int count, LuminTask[]? tasks,
        LuminTask a = default, LuminTask b = default, LuminTask c = default, LuminTask d = default,
        List<LuminTask>? list = null)
    {
        var join = (VoidJoin?)LuminTaskIdleCache.Take(typeof(VoidJoin)) ?? new VoidJoin();
        ulong handle = LuminTask.RentSlot();
        join.Begin(handle, any, count);
        try
        {
            if (tasks != null)
                for (int i = 0; i < count; i++) join.Watch(tasks[i]._handle, any ? i : 0);
            else if (list != null)
                for (int i = 0; i < count; i++) join.Watch(list[i]._handle, any ? i : 0);
            else
            {
                join.Watch(a._handle, 0);
                join.Watch(b._handle, 1);
                if (count > 2) join.Watch(c._handle, 2);
                if (count > 3) join.Watch(d._handle, 3);
            }
            return handle;
        }
        catch { join.Reject(); throw; }
        finally { join.Exit(); }
    }
}

internal sealed class ArrayJoin<T> : CombinerNode
{
    private T[]? _results;
    private T _winner = default!;
    protected override int Charge => 104 + Unsafe.SizeOf<T>();
    protected override void Clear() { _winner = default!; _results = null; }

    internal static ulong Create(bool any, int count, LuminTask<T>[]? tasks, List<LuminTask<T>>? list = null)
    {
        var output = any ? null : new T[count];
        var join = (ArrayJoin<T>?)LuminTaskIdleCache.Take(typeof(ArrayJoin<T>)) ?? new ArrayJoin<T>();
        ulong handle = any ? LuminTaskResultBridge.Rent<(int, T)>() : LuminTaskResultBridge.Rent<T[]>();
        join.Begin(handle, any, count);
        join._results = output;
        try
        {
            for (int i = 0; i < count; i++)
            {
                var task = tasks != null ? tasks[i] : list![i];
                if (task._handle != 0) join.Watch(task._handle, i);
                else join.Inline(task._result!, i);
            }
            return handle;
        }
        catch { join.Reject(); throw; }
        finally { join.Exit(); }
    }

    private void Inline(T value, int index)
    {
        lock (this)
        {
            if (Closed) return;
            Store(value, index);
            if (!SucceedLocked()) return;
        }
        Publish(index);
    }

    private void Store(T value, int index)
    {
        if (Any) _winner = value;
        else _results![index] = value;
    }

    protected override void Read(ulong handle, int index) => Store(LuminTaskResultBridge.Get<T>(handle), index);

    protected override void Publish(int index)
    {
        var winner = _winner;
        var output = _results;
        Clear();
        if (Any) LuminTaskResultBridge.TrySetResult(Handle, (index, winner));
        else LuminTaskResultBridge.TrySetResult(Handle, output);
    }
}
