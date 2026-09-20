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

public class StateTuple<T> : IDisposable
#if NET8_0_OR_GREATER
    , IPooledObjectPolicy<StateTuple<T>>
#endif
{
    public T Item;
    private int _disposed; // 0 = 可用, 1 = 已归还/Dispose 中

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
    internal void Reset() => _disposed = 0;

#if NET8_0_OR_GREATER
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static StateTuple<T> IPooledObjectPolicy<StateTuple<T>>.Create() => new();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static bool IPooledObjectPolicy<StateTuple<T>>.Return(StateTuple<T> obj) => true;
#endif
}

public static class StatePool<T>
{
#if NET8_0_OR_GREATER
    private static readonly ObjectPool<StateTuple<T>> _pool = new();
#else
    private static readonly ObjectPool<StateTuple<T>> _pool = new(new StateTuplePoolPolicy());
#endif

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static StateTuple<T> Create(in T item)
    {
        var tuple = _pool.Rent();
        tuple.Reset();
        tuple.Item = item;
        return tuple;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Return(StateTuple<T> tuple)
    {
        tuple.Item = default;
        _pool.Return(tuple);
    }

#if NETSTANDARD2_1
    private sealed class StateTuplePoolPolicy : IPooledObjectPolicy<StateTuple<T>>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        StateTuple<T> IPooledObjectPolicy<StateTuple<T>>.Create() => new();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        bool IPooledObjectPolicy<StateTuple<T>>.Return(StateTuple<T> obj) => true;
    }
#endif
}