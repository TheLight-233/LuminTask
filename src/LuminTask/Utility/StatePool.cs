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

public class StateTuple<T> : IDisposable, IPooledResult, ILuminCacheNode
{
    public T Item = default!;
    private int _disposed; // 0 = 可用, 1 = 已归还/Dispose 中
    ILuminCacheNode? ILuminCacheNode.CacheNext { get; set; }

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

        StatePool<T>.ReturnCleared(this);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void IPooledResult.ReturnToPool() => Dispose();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void Reset() => _disposed = 0;
}

public static class StatePool<T>
{
    private static readonly Type PoolType = typeof(StateTuple<T>);

    public static StateTuple<T> Create(in T item)
    {
        var tuple = (StateTuple<T>?)LuminTaskIdleCache.Take(PoolType) ?? new StateTuple<T>();
        tuple.Reset();
        tuple.Item = item;
        return tuple;
    }

    public static void Return(StateTuple<T> tuple) => tuple.Dispose();

    internal static void ReturnCleared(StateTuple<T> tuple)
    {
        tuple.Item = default!;
        LuminTaskIdleCache.Return(tuple, (Unsafe.SizeOf<T>() + 6 * IntPtr.Size + 7) & ~7);
    }
}
