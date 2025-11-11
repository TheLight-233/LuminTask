using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using LuminThread.Interface;

namespace LuminThread;

public readonly unsafe struct LuminTaskAwaiter : ICriticalNotifyCompletion
{
    private readonly VTable _source;
    private readonly void* _taskSource;
    private readonly short _id;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal LuminTaskAwaiter(VTable source, void* taskSource, short id)
    {
        _source = source;
        _taskSource = taskSource;
        _id = id;
    }

    public bool IsCompleted
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            if (_source.GetStatus == null)
                return true;
                
            return _source.GetStatus(_taskSource, _id).IsCompleted();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void GetResult()
    {
        _source.GetResult(_taskSource, _id);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void OnCompleted(Action continuation)
    {
        if (_source.OnCompleted == null)
        {
            continuation();
        }
        else
        {
            _source.OnCompleted(_taskSource, static s => ((Action)s)(), continuation, _id);
        }
    }
        
    [DebuggerHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SourceOnCompleted(Action<object> continuation, object state)
    {
        if (_source.OnCompleted == null)
        {
            continuation(state);
        }
        else
        {
            _source.OnCompleted(_taskSource, continuation, state, _id);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void UnsafeOnCompleted(Action continuation) => OnCompleted(continuation);
}

public readonly unsafe struct LuminTaskAwaiter<T> : ICriticalNotifyCompletion
{
    private readonly VTable _source;
    private readonly void* _taskSource;
    private readonly short _id;
    private readonly T? _result;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal LuminTaskAwaiter(VTable source, void* taskSource, short id, T? result = default)
    {
        _source = source;
        _taskSource = taskSource;
        _id = id;
        _result = result;
    }

    public bool IsCompleted
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            if (_source.GetStatus == null)
                return true;
                
            return _source.GetStatus(_taskSource, _id).IsCompleted();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T GetResult()
    {
        if (_source.GetResultValue == null)
            return _result!;

        var result = ((delegate*<void*, short, T>)_source.GetResultValue)(_taskSource, _id);
            
        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void OnCompleted(Action continuation)
    {
        if (_source.OnCompleted == null)
        {
            continuation();
        }
        else
        {
            _source.OnCompleted(_taskSource, static s => ((Action)s)(), continuation, _id);
        }
    }
        
    [DebuggerHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SourceOnCompleted(Action<object> continuation, object state)
    {
        if (_source.OnCompleted == null)
        {
            continuation(state);
        }
        else
        {
            _source.OnCompleted(_taskSource, continuation, state, _id);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void UnsafeOnCompleted(Action continuation) => OnCompleted(continuation);
}