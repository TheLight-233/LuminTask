using System;
using System.Runtime.CompilerServices;

namespace LuminThread.Utility;

internal static class StateTuple
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static StateTuple<T1> Create<T1>(in T1 item1)
    {
        return StatePool<T1>.Create(item1);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static StateTuple<T1, T2> Create<T1, T2>(in T1 item1, in T2 item2)
    {
        return StatePool<T1, T2>.Create(item1, item2);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static StateTuple<T1, T2, T3> Create<T1, T2, T3>(in T1 item1, in T2 item2, in T3 item3)
    {
        return StatePool<T1, T2, T3>.Create(item1, item2, item3);
    }
}

internal class StateTuple<T1> : IDisposable
#if NET8_0_OR_GREATER
    , IPooledObjectPolicy<StateTuple<T1>>
#endif
{
    public T1 Item1;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Deconstruct(out T1 item1)
    {
        item1 = this.Item1;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Dispose()
    {
        StatePool<T1>.Return(this);
    }

#if NET8_0_OR_GREATER
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static StateTuple<T1> IPooledObjectPolicy<StateTuple<T1>>.Create() => new();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static bool IPooledObjectPolicy<StateTuple<T1>>.Return(StateTuple<T1> obj) => true;
#endif
}

internal static class StatePool<T1>
{
#if NET8_0_OR_GREATER
    private static readonly ObjectPool<StateTuple<T1>> _pool = new ();
#else
    private static readonly ObjectPool<StateTuple<T1>> _pool = new (new StateTuplePoolPolicy());
#endif

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static StateTuple<T1> Create(in T1 item1)
    {
        var obj = _pool.Rent();
        obj.Item1 = item1;
        return obj;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Return(StateTuple<T1> tuple)
    {
        tuple.Item1 = default;
        _pool.Return(tuple);
    }

#if NETSTANDARD2_1
    private sealed class StateTuplePoolPolicy : IPooledObjectPolicy<StateTuple<T1>>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        StateTuple<T1> IPooledObjectPolicy<StateTuple<T1>>.Create() => new();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        bool IPooledObjectPolicy<StateTuple<T1>>.Return(StateTuple<T1> obj) => true;
    }
#endif
}

internal class StateTuple<T1, T2> : IDisposable
#if NET8_0_OR_GREATER
    , IPooledObjectPolicy<StateTuple<T1, T2>>
#endif
{
    public T1 Item1;
    public T2 Item2;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Deconstruct(out T1 item1, out T2 item2)
    {
        item1 = this.Item1;
        item2 = this.Item2;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Dispose()
    {
        StatePool<T1, T2>.Return(this);
    }
    
#if NET8_0_OR_GREATER
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static StateTuple<T1, T2> IPooledObjectPolicy<StateTuple<T1, T2>>.Create() => new();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static bool IPooledObjectPolicy<StateTuple<T1, T2>>.Return(StateTuple<T1, T2> obj) => true;
#endif
}

internal static class StatePool<T1, T2>
{
#if NET8_0_OR_GREATER
    private static readonly ObjectPool<StateTuple<T1, T2>> _pool = new ();
#else
    private static readonly ObjectPool<StateTuple<T1, T2>> _pool = new (new StateTuplePoolPolicy());
#endif

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static StateTuple<T1, T2> Create(in T1 item1, in T2 item2)
    {
        var obj = _pool.Rent();
        obj.Item1 = item1;
        obj.Item2 = item2;

        return obj;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Return(StateTuple<T1, T2> tuple)
    {
        tuple.Item1 = default;
        tuple.Item2 = default;
        _pool.Return(tuple);
    }
    
#if NETSTANDARD2_1
    private sealed class StateTuplePoolPolicy : IPooledObjectPolicy<StateTuple<T1, T2>>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        StateTuple<T1, T2> IPooledObjectPolicy<StateTuple<T1, T2>>.Create() => new();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        bool IPooledObjectPolicy<StateTuple<T1, T2>>.Return(StateTuple<T1, T2> obj) => true;
    }
#endif
}

internal class StateTuple<T1, T2, T3> : IDisposable
#if NET8_0_OR_GREATER
    , IPooledObjectPolicy<StateTuple<T1, T2, T3>>
#endif
{
    public T1 Item1;
    public T2 Item2;
    public T3 Item3;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Deconstruct(out T1 item1, out T2 item2, out T3 item3)
    {
        item1 = this.Item1;
        item2 = this.Item2;
        item3 = this.Item3;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Dispose()
    {
        StatePool<T1, T2, T3>.Return(this);
    }
    
#if NET8_0_OR_GREATER
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static StateTuple<T1, T2, T3> IPooledObjectPolicy<StateTuple<T1, T2, T3>>.Create() => new();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static bool IPooledObjectPolicy<StateTuple<T1, T2, T3>>.Return(StateTuple<T1, T2, T3> obj) => true;
#endif
}

internal static class StatePool<T1, T2, T3>
{
#if NET8_0_OR_GREATER
    private static readonly ObjectPool<StateTuple<T1, T2, T3>> _pool = new ();
#else
    private static readonly ObjectPool<StateTuple<T1, T2, T3>> _pool = new (new StateTuplePoolPolicy());
#endif

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static StateTuple<T1, T2, T3> Create(in T1 item1, in T2 item2, in T3 item3)
    {
        var obj = _pool.Rent();
        obj.Item1 = item1;
        obj.Item2 = item2;
        obj.Item3 = item3;

        return new StateTuple<T1, T2, T3> { Item1 = item1, Item2 = item2, Item3 = item3 };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Return(StateTuple<T1, T2, T3> tuple)
    {
        tuple.Item1 = default;
        tuple.Item2 = default;
        tuple.Item3 = default;
        _pool.Return(tuple);
    }
        
#if NETSTANDARD2_1
    private sealed class StateTuplePoolPolicy : IPooledObjectPolicy<StateTuple<T1, T2, T3>>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        StateTuple<T1, T2, T3> IPooledObjectPolicy<StateTuple<T1, T2, T3>>.Create() => new();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        bool IPooledObjectPolicy<StateTuple<T1, T2, T3>>.Return(StateTuple<T1, T2, T3> obj) => true;
    }
#endif
}