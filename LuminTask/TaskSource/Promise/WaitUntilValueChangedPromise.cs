using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using LuminThread.Interface;

namespace LuminThread.TaskSource.Promise;

public unsafe struct WaitUntilValueChangedPromise<TTarget, TValue>
{
    TTarget _target;
    Func<TTarget, TValue> _monitorFunction;
    TValue _currentValue;
    IEqualityComparer<TValue> _equalityComparer;
    CancellationToken _cancellationToken;
    CancellationTokenRegistration _cancellationTokenRegistration;
    bool _cancelImmediately;

    LuminTaskSourceCore<TValue>* _core;
    
    internal LuminTaskSourceCore<TValue>* Source => _core;
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static WaitUntilValueChangedPromise<TTarget, TValue> Create(
        TTarget target, 
        Func<TTarget, TValue> monitorFunction, 
        IEqualityComparer<TValue> equalityComparer,
        PlayerLoopTiming timing, 
        CancellationToken cancellationToken, 
        bool cancelImmediately, 
        out short token)
    {
        var core = LuminTaskSourceCore<TValue>.Create();

        var waitPromise = new WaitUntilValueChangedPromise<TTarget, TValue>();
        waitPromise._core = core;
        waitPromise._target = target;
        waitPromise._monitorFunction = monitorFunction;
        waitPromise._currentValue = monitorFunction(target);
        waitPromise._equalityComparer = equalityComparer ?? EqualityComparer<TValue>.Default;
        waitPromise._cancellationToken = cancellationToken;
        waitPromise._cancelImmediately = cancelImmediately;

        token = core->Id;
        
        PlayerLoopHelper.AddAction(timing, new LuminTaskState(core), waitPromise.MoveNext);
        
        return waitPromise;
    }
    
    public TValue GetResult(short token)
    {
        try
        {
            return LuminTaskSourceCore<TValue>.GetResultValue(_core, token);
        }
        finally
        {
            if (!(_cancelImmediately && _cancellationToken.IsCancellationRequested))
            {
                Dispose();
            }
        }
    }

    public LuminTaskStatus GetStatus(short token)
    {
        return LuminTaskSourceCore<TValue>.GetStatus(_core, token);
    }

    public LuminTaskStatus UnsafeGetStatus()
    {
        return LuminTaskSourceCore<TValue>.UnsafeGetStatus(_core);
    }

    public void OnCompleted(Action<object> continuation, object state, short token)
    {
        LuminTaskSourceCore<TValue>.OnCompleted(_core, continuation, state, token);
    }

    public bool MoveNext(in LuminTaskState state)
    {
        if (_cancellationToken.IsCancellationRequested)
        {
            LuminTaskSourceCore<TValue>.TrySetCanceled(state.Source);
            return false;
        }

        try
        {
            var nextValue = _monitorFunction(_target);
            if (_equalityComparer.Equals(_currentValue, nextValue))
            {
                return true;
            }
            
            LuminTaskSourceCore<TValue>.TrySetResult(state.Source, nextValue);
            return false;
        }
        catch (Exception ex)
        {
            LuminTaskSourceCore<TValue>.TrySetException(state.Source, ex);
            return false;
        }
    }

    void Dispose()
    {
        _target = default;
        _monitorFunction = default;
        _currentValue = default;
        _equalityComparer = default;
        _cancellationToken = default;
        _cancellationTokenRegistration.Dispose();
        _cancelImmediately = default;

        if (_core != null)
        {
            LuminTaskSourceCore<TValue>.Dispose(_core);
        }
    }
}