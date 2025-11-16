using System;
using System.Runtime.CompilerServices;
using LuminThread.Interface;

namespace LuminThread.TaskSource;

public sealed unsafe class ValueTaskSource : ILuminTaskSource
{
    private readonly VTable _method;
    private readonly void* _taskSource;

    public ValueTaskSource(in VTable table, void* taskSource)
    {
        _method = table;
        _taskSource = taskSource;
    }

    ~ValueTaskSource()
    {
        _method.Dispose(_taskSource);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public LuminTaskStatus GetStatus(short token)
    {
        return _method.GetStatus(_taskSource, token);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public LuminTaskStatus UnsafeGetStatus()
    {
        return _method.UnsafeGetStatus(_taskSource);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void GetResult(short token)
    { 
        _method.GetResult(_taskSource, token);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void OnCompleted(Action<object> continuation, object state, short token)
    {
        _method.OnCompleted(_taskSource, continuation, state, token);
    }
}

public sealed unsafe class ValueTaskSource<T> : ILuminTaskSource<T>
{
    private readonly VTable _method;
    private readonly void* _taskSource;

    public ValueTaskSource(in VTable table, void* taskSource)
    {
        _method = table;
        _taskSource = taskSource;
    }
    
    ~ValueTaskSource()
    {
        _method.Dispose(_taskSource);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public LuminTaskStatus GetStatus(short token)
    {
        return _method.GetStatus(_taskSource, token);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public LuminTaskStatus UnsafeGetStatus()
    {
        return _method.UnsafeGetStatus(_taskSource);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void ILuminTaskSource.GetResult(short token)
    { 
        _method.GetResult(_taskSource, token);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T GetResult(short token)
    {
        return ((delegate*<void*, short, T>)_method.GetResultValue)(_taskSource, token);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void OnCompleted(Action<object> continuation, object state, short token)
    {
        _method.OnCompleted(_taskSource, continuation, state, token);
    }
}