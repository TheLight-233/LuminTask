using System;
using System.Threading;
using System.Runtime.CompilerServices;
using LuminThread.Interface;
using LuminThread.Utility;

namespace LuminThread;

/// <summary>Single-consumer pump used by both the Unity timings and the .NET timer.</summary>
public class ManagedPlayerLoop : IPlayLoopStrategy
{
    // Only the delegate and configuration live on an unused Unity timing.
    private Buffers? _buffers;
    private Timer? _timer;
    private readonly bool _timerDriven;
    private bool _running, _closed;
    private int _accepted;

    public ManagedPlayerLoop(bool timerDriven = false) => _timerDriven = timerDriven;

    private sealed class Buffers
    {
        internal Ring<(LuminTaskState State, MoveNext Step)> Incoming, Active;
        internal Ring<Action> Continuations;
    }

    private struct Ring<T>
    {
        private T[]? _items;
        private int _head;
        internal int Count { get; private set; }
        internal long Bytes => (long)(_items?.Length ?? 0) * Unsafe.SizeOf<T>();

        internal void Reserve(int count)
        {
            if (count > (_items?.Length ?? 0)) Resize(CapacityFor(count));
        }

        internal void Trim(int reserved)
        {
            int needed = Math.Max(Count, reserved);
            if (needed == 0) { this = default; return; }
            int size = CapacityFor(needed);
            if (_items != null && size < _items.Length) Resize(size);
        }

        private static int CapacityFor(int count)
        {
            int size = 8;
            while (size < count) size = checked(size * 2);
            return size;
        }

        private void Resize(int size)
        {
            var items = new T[size];
            if (Count != 0)
            {
                int first = Math.Min(Count, _items!.Length - _head);
                Array.Copy(_items, _head, items, 0, first);
                Array.Copy(_items, 0, items, first, Count - first);
            }
            _head = 0;
            _items = items;
        }

        internal void Put(T item)
        {
            _items![(_head + Count) & (_items.Length - 1)] = item;
            Count++;
        }

        internal T Take()
        {
            T item = _items![_head];
            _items[_head] = default!;
            _head = (_head + 1) & (_items.Length - 1);
            Count--;
            return item;
        }
    }

    public void AddAction(LuminTaskState state, MoveNext moveNext)
    {
        if (moveNext == null) throw new ArgumentNullException(nameof(moveNext));
        lock (this)
        {
            var buffers = Start();
            buffers.Incoming.Reserve(buffers.Incoming.Count + 1);
            buffers.Active.Reserve(_accepted + 1);
            buffers.Incoming.Put((state, moveNext));
            _accepted++;
        }
    }

    public void AddContinuation(Action action)
    {
        if (action == null) throw new ArgumentNullException(nameof(action));
        lock (this)
        {
            var buffers = Start();
            buffers.Continuations.Reserve(buffers.Continuations.Count + 1);
            buffers.Continuations.Put(action);
        }
    }

    private Buffers Start()
    {
        if (_closed) throw new ObjectDisposedException(nameof(ManagedPlayerLoop));
        var buffers = _buffers ??= new Buffers();
        if (_timerDriven && !_running && _accepted == 0 && buffers.Continuations.Count == 0)
        {
            _timer ??= new Timer(static s => ((ManagedPlayerLoop)s!).RunCore(), this, Timeout.Infinite, Timeout.Infinite);
            _timer.Change(0, 1);
        }
        return buffers;
    }

    public void RunCore()
    {
        var buffers = Volatile.Read(ref _buffers);
        if (buffers == null) return;
        lock (this)
        {
            if (_running || _closed || !ReferenceEquals(buffers, _buffers)) return;
            _running = true;
        }
        try
        {
            if (!_timerDriven) RunContinuations(buffers);
            int count;
            lock (this)
            {
                int incoming = _timerDriven ? Math.Min(1000, buffers.Incoming.Count) : buffers.Incoming.Count;
                for (int i = 0; i < incoming; i++) buffers.Active.Put(buffers.Incoming.Take());
                count = buffers.Active.Count;
            }
            for (int i = 0; i < count; i++)
            {
                (LuminTaskState State, MoveNext Step) work;
                lock (this)
                {
                    if (_closed) break;
                    work = buffers.Active.Take();
                }

                bool keep = false;
                try { keep = work.Step(work.State); }
                catch (Exception ex)
                {
                    if (!LuminTaskSource.TrySetException(work.State.Handle, ex))
                        LuminTaskScheduler.PublishUnobservedTaskException(ex);
                }

                lock (this)
                {
                    if (keep && !_closed) { buffers.Active.Put(work); continue; }
                    _accepted--;
                }
                if (keep) Cancel(work.State);
            }
            if (_timerDriven) RunContinuations(buffers);
        }
        finally
        {
            lock (this)
            {
                _running = false;
                if (!_closed && _accepted == 0 && buffers.Continuations.Count == 0)
                    _timer?.Change(Timeout.Infinite, Timeout.Infinite);
            }
        }
    }

    private void RunContinuations(Buffers buffers)
    {
        int count;
        lock (this) count = buffers.Continuations.Count;
        if (_timerDriven) count = Math.Min(count, 1000);
        for (int i = 0; i < count; i++)
        {
            Action action;
            lock (this)
            {
                if (_closed) return;
                action = buffers.Continuations.Take();
            }
            Invoke(action);
        }
    }

    private static void Invoke(Action action)
    {
        try { action(); }
        catch (Exception ex) { LuminTaskScheduler.PublishUnobservedTaskException(ex); }
    }

    internal long BufferBytes
    {
        get
        {
            lock (this)
                return _buffers == null ? 0 : _buffers.Incoming.Bytes + _buffers.Active.Bytes + _buffers.Continuations.Bytes;
        }
    }

    internal void TrimBuffers()
    {
        lock (this)
        {
            if (_buffers == null) return;
            if (!_running && _accepted == 0 && _buffers.Continuations.Count == 0)
            {
                _buffers = null;
                _timer?.Dispose();
                _timer = null;
                return;
            }
            _buffers.Incoming.Trim(0);
            _buffers.Active.Trim(_accepted);
            _buffers.Continuations.Trim(0);
        }
    }

    private static void Cancel(LuminTaskState state)
    {
        try
        {
            try { state.OnShutdown?.Invoke(state.State!); }
            finally { LuminTaskSource.TrySetCanceled(state.Handle, state.CancellationToken); }
        }
        catch (Exception ex) { LuminTaskScheduler.PublishUnobservedTaskException(ex); }
    }

    public void Dispose()
    {
        Buffers? buffers;
        lock (this)
        {
            if (_closed) return;
            _closed = true;
            _timer?.Dispose();
            buffers = _buffers;
            _buffers = null;
        }
        if (buffers == null) return;
        while (true)
        {
            LuminTaskState state = default;
            Action? continuation = null;
            lock (this)
            {
                if (buffers.Incoming.Count > 0) state = buffers.Incoming.Take().State;
                else if (buffers.Active.Count > 0) state = buffers.Active.Take().State;
                else if (buffers.Continuations.Count > 0) continuation = buffers.Continuations.Take();
                else return;
                if (continuation == null) _accepted--;
            }
            if (continuation != null) Invoke(continuation);
            else Cancel(state);
        }
    }
}
