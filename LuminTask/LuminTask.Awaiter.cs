using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using LuminThread.Interface;

namespace LuminThread;

public readonly unsafe struct LuminTaskAwaiter : ICriticalNotifyCompletion
{
    private readonly LuminTask _task;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal LuminTaskAwaiter(in LuminTask task)
    {
        _task = task;
    }

    public bool IsCompleted
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            if (_task._vTable.GetStatus == null)
                return true;
                
            return _task._vTable.GetStatus(_task._taskSource, _task._id).IsCompleted();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void GetResult()
    {
        if (_task._vTable.GetResult == null)
            return;
        
        _task._vTable.GetResult(_task._taskSource, _task._id);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void OnCompleted(Action continuation)
    {
        if (_task._vTable.OnCompleted == null)
        {
            continuation();
        }
        else
        {
            _task._vTable.OnCompleted(_task._taskSource, static s => ((Action)s)(), continuation, _task._id);
        }
    }
        
    [DebuggerHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SourceOnCompleted(Action<object> continuation, object state)
    {
        if (_task._vTable.OnCompleted == null)
        {
            continuation(state);
        }
        else
        {
            _task._vTable.OnCompleted(_task._taskSource, continuation, state, _task._id);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void UnsafeOnCompleted(Action continuation) => OnCompleted(continuation);
}

public readonly unsafe struct LuminTaskAwaiter<T> : ICriticalNotifyCompletion
{
    private readonly LuminTask<T> _task;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal LuminTaskAwaiter(in LuminTask<T> task)
    {
        _task = task;
    }

    public bool IsCompleted
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            if (_task._vTable.GetStatus == null)
                return true;
           
            return _task._vTable.GetStatus(_task._taskSource, _task._id).IsCompleted();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T GetResult()
    {
        if (_task._vTable.GetResultValue == null)
            return _task._result!;
        
        var result = ((delegate*<void*, short, T>)_task._vTable.GetResultValue)(_task._taskSource, _task._id);
            
        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void OnCompleted(Action continuation)
    {
        if (_task._vTable.OnCompleted == null)
        {
            continuation();
        }
        else
        {
            _task._vTable.OnCompleted(_task._taskSource, static s => ((Action)s)(), continuation, _task._id);
        }
    }
        
    [DebuggerHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SourceOnCompleted(Action<object> continuation, object state)
    {
        if (_task._vTable.OnCompleted == null)
        {
            continuation(state);
        }
        else
        {
            _task._vTable.OnCompleted(_task._taskSource, continuation, state, _task._id);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void UnsafeOnCompleted(Action continuation) => OnCompleted(continuation);
}