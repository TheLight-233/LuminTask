using System;
using System.Runtime.CompilerServices;
using LuminThread.Utility;

namespace LuminThread.TaskSource.Promise;

// The public heterogeneous overloads have three tuple shapes. Only their typed
// field reads and output construction differ; CombinerNode owns the lifecycle.

internal sealed class TupleJoin<T1, T2> : CombinerNode
{
    private T1 _value1 = default!;
    private T2 _value2 = default!;
    protected override int Charge => 96 + Unsafe.SizeOf<T1>() + Unsafe.SizeOf<T2>();

    internal static ulong Create(bool any, in LuminTask<T1> task1, in LuminTask<T2> task2)
    {
        var join = (TupleJoin<T1, T2>?)LuminTaskIdleCache.Take(typeof(TupleJoin<T1, T2>)) ?? new TupleJoin<T1, T2>();
        ulong handle = any ? LuminTaskResultBridge.Rent<(int, T1, T2)>() : LuminTaskResultBridge.Rent<(T1, T2)>();
        join.Begin(handle, any, 2);
        try
        {
            join._value1 = task1._result!;
            join._value2 = task2._result!;
            join.Watch(task1._handle, 0);
            join.Watch(task2._handle, 1);
            return handle;
        }
        catch { join.Reject(); throw; }
        finally { join.Exit(); }
    }

    protected override void Read(ulong handle, int index)
    {
        switch (index)
        {
            case 0: _value1 = LuminTaskResultBridge.Get<T1>(handle); break;
            case 1: _value2 = LuminTaskResultBridge.Get<T2>(handle); break;
        }
    }

    protected override void Publish(int index)
    {
        if (Any)
        {
            var result = (index, index == 0 ? _value1 : default!, index == 1 ? _value2 : default!);
            Clear();
            LuminTaskResultBridge.TrySetResult(Handle, in result);
        }
        else
        {
            var result = (_value1, _value2);
            Clear();
            LuminTaskResultBridge.TrySetResult(Handle, in result);
        }
    }

    protected override void Clear()
    {
        _value1 = default!;
        _value2 = default!;
    }
}

internal sealed class TupleJoin<T1, T2, T3> : CombinerNode
{
    private T1 _value1 = default!;
    private T2 _value2 = default!;
    private T3 _value3 = default!;
    protected override int Charge => 96 + Unsafe.SizeOf<T1>() + Unsafe.SizeOf<T2>() + Unsafe.SizeOf<T3>();

    internal static ulong Create(bool any, in LuminTask<T1> task1, in LuminTask<T2> task2, in LuminTask<T3> task3)
    {
        var join = (TupleJoin<T1, T2, T3>?)LuminTaskIdleCache.Take(typeof(TupleJoin<T1, T2, T3>)) ?? new TupleJoin<T1, T2, T3>();
        ulong handle = any ? LuminTaskResultBridge.Rent<(int, T1, T2, T3)>() : LuminTaskResultBridge.Rent<(T1, T2, T3)>();
        join.Begin(handle, any, 3);
        try
        {
            join._value1 = task1._result!;
            join._value2 = task2._result!;
            join._value3 = task3._result!;
            join.Watch(task1._handle, 0);
            join.Watch(task2._handle, 1);
            join.Watch(task3._handle, 2);
            return handle;
        }
        catch { join.Reject(); throw; }
        finally { join.Exit(); }
    }

    protected override void Read(ulong handle, int index)
    {
        switch (index)
        {
            case 0: _value1 = LuminTaskResultBridge.Get<T1>(handle); break;
            case 1: _value2 = LuminTaskResultBridge.Get<T2>(handle); break;
            case 2: _value3 = LuminTaskResultBridge.Get<T3>(handle); break;
        }
    }

    protected override void Publish(int index)
    {
        if (Any)
        {
            var result = (index, index == 0 ? _value1 : default!, index == 1 ? _value2 : default!, index == 2 ? _value3 : default!);
            Clear();
            LuminTaskResultBridge.TrySetResult(Handle, in result);
        }
        else
        {
            var result = (_value1, _value2, _value3);
            Clear();
            LuminTaskResultBridge.TrySetResult(Handle, in result);
        }
    }

    protected override void Clear()
    {
        _value1 = default!;
        _value2 = default!;
        _value3 = default!;
    }
}

internal sealed class TupleJoin<T1, T2, T3, T4> : CombinerNode
{
    private T1 _value1 = default!;
    private T2 _value2 = default!;
    private T3 _value3 = default!;
    private T4 _value4 = default!;
    protected override int Charge => 96 + Unsafe.SizeOf<T1>() + Unsafe.SizeOf<T2>() + Unsafe.SizeOf<T3>() + Unsafe.SizeOf<T4>();

    internal static ulong Create(bool any, in LuminTask<T1> task1, in LuminTask<T2> task2, in LuminTask<T3> task3, in LuminTask<T4> task4)
    {
        var join = (TupleJoin<T1, T2, T3, T4>?)LuminTaskIdleCache.Take(typeof(TupleJoin<T1, T2, T3, T4>)) ?? new TupleJoin<T1, T2, T3, T4>();
        ulong handle = any ? LuminTaskResultBridge.Rent<(int, T1, T2, T3, T4)>() : LuminTaskResultBridge.Rent<(T1, T2, T3, T4)>();
        join.Begin(handle, any, 4);
        try
        {
            join._value1 = task1._result!;
            join._value2 = task2._result!;
            join._value3 = task3._result!;
            join._value4 = task4._result!;
            join.Watch(task1._handle, 0);
            join.Watch(task2._handle, 1);
            join.Watch(task3._handle, 2);
            join.Watch(task4._handle, 3);
            return handle;
        }
        catch { join.Reject(); throw; }
        finally { join.Exit(); }
    }

    protected override void Read(ulong handle, int index)
    {
        switch (index)
        {
            case 0: _value1 = LuminTaskResultBridge.Get<T1>(handle); break;
            case 1: _value2 = LuminTaskResultBridge.Get<T2>(handle); break;
            case 2: _value3 = LuminTaskResultBridge.Get<T3>(handle); break;
            case 3: _value4 = LuminTaskResultBridge.Get<T4>(handle); break;
        }
    }

    protected override void Publish(int index)
    {
        if (Any)
        {
            var result = (index, index == 0 ? _value1 : default!, index == 1 ? _value2 : default!, index == 2 ? _value3 : default!, index == 3 ? _value4 : default!);
            Clear();
            LuminTaskResultBridge.TrySetResult(Handle, in result);
        }
        else
        {
            var result = (_value1, _value2, _value3, _value4);
            Clear();
            LuminTaskResultBridge.TrySetResult(Handle, in result);
        }
    }

    protected override void Clear()
    {
        _value1 = default!;
        _value2 = default!;
        _value3 = default!;
        _value4 = default!;
    }
}
