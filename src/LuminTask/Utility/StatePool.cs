using System;
using System.Runtime.CompilerServices;
using System.Threading;

namespace LuminThread.Utility;

public static class StateTuple
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static StateTuple<T> Create<T>(in T item)
    {
        return StatePool<T>.Create(item);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static StateTuple<(T1, T2)> Create<T1, T2>(in T1 item1, in T2 item2)
    {
        return StatePool<(T1, T2)>.Create((item1, item2));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static StateTuple<(T1, T2, T3)> Create<T1, T2, T3>(in T1 item1, in T2 item2, in T3 item3)
    {
        return StatePool<(T1, T2, T3)>.Create((item1, item2, item3));
    }
}

// Marks a pooled container so the non-generic release path can recycle it without knowing T.
//
// It is deliberately NOT inferred from IDisposable: a user's result type may itself implement
// IDisposable, and the task machinery must never dispose a value it merely stored.
internal interface IPooledResult
{
    void ReturnToPool();
}

public class StateTuple<T> : IDisposable, IPooledResult
{
    public T Item;
    private int _disposed; // 0 = 可用, 1 = 已归还/Dispose 中
    internal StateTuple<T>? Next;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Deconstruct(out T item)
    {
        item = Item;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
            return;

        StatePool<T>.Return(this);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void IPooledResult.ReturnToPool() => Dispose();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void Reset() => _disposed = 0;
}

public static class StatePool<T>
{
    // Cap on how many containers a single closed T keeps warm. Without it the overflow chain grows to
    // whatever peak the process reached and never shrinks, which is exactly the resident memory the
    // pool is supposed to bound. One fast slot plus this many overflow entries is the retained set.
    private const int MaxRetained = 32;

    private static StateTuple<T>? _fastItem;
    private static StateTuple<T>? _overflow;
    private static int _overflowCount;
    private static SpinLock _overflowLock = new(enableThreadOwnerTracking: false);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static StateTuple<T> Create(in T item)
    {
        var tuple = Interlocked.Exchange(ref _fastItem, null);
        if (tuple is null)
        {
            var lockTaken = false;
            try
            {
                _overflowLock.Enter(ref lockTaken);
                tuple = _overflow;
                if (tuple is not null)
                {
                    _overflow = tuple.Next;
                    _overflowCount--;
                    tuple.Next = null;
                }
            }
            finally
            {
                if (lockTaken)
                    _overflowLock.Exit(useMemoryBarrier: false);
            }
        }

        tuple ??= new StateTuple<T>();
        tuple.Reset();
        tuple.Item = item;
        return tuple;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Return(StateTuple<T> tuple)
    {
        tuple.Item = default;
        tuple.Next = null;

        if (Interlocked.CompareExchange(ref _fastItem, tuple, null) is null)
            return;

        var lockTaken = false;
        try
        {
            _overflowLock.Enter(ref lockTaken);

            // Past the cap the container is simply dropped: it costs one GC, once, and keeps the
            // pool's retained size bounded instead of tracking the high-water mark forever.
            if (_overflowCount >= MaxRetained)
                return;

            tuple.Next = _overflow;
            _overflow = tuple;
            _overflowCount++;
        }
        finally
        {
            if (lockTaken)
                _overflowLock.Exit(useMemoryBarrier: false);
        }
    }
}
