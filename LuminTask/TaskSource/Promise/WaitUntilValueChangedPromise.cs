using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using LuminThread.Interface;
using LuminThread.Utility;

namespace LuminThread.TaskSource.Promise;

public unsafe struct WaitUntilValueChangedPromise<TTarget, TValue>
{
    CancellationToken _cancellationToken;
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
        waitPromise._cancellationToken = cancellationToken;
        waitPromise._cancelImmediately = cancelImmediately;

        token = core->Id;
        
        var currentValue = monitorFunction(target);
        var comparer = equalityComparer;
        
        var stateTuple = StateTuple.Create(target, currentValue, comparer);
        
        PlayerLoopHelper.AddAction(timing, new LuminTaskState(core, cancellationToken, monitorFunction, stateTuple), &MoveNext);
        
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

    private static bool MoveNext(in LuminTaskState state)
    {
        Func<TTarget, TValue> func = Unsafe.As<Func<TTarget, TValue>>(state.State);
        StateTuple<TTarget, TValue, IEqualityComparer<TValue>> stateTuple = 
            Unsafe.As<StateTuple<TTarget, TValue, IEqualityComparer<TValue>>>(state.StateTuple);
        
        if (state.CancellationToken.IsCancellationRequested)
        {
            LuminTaskSourceCore<TValue>.TrySetCanceled(state.Source);
            LuminTaskSourceCore<TValue>.Dispose(state.Source);
            stateTuple.Dispose();
            return false;
        }

        try
        {
            var nextValue = func(stateTuple.Item1);
            if (stateTuple.Item3.Equals(stateTuple.Item2, nextValue))
            {
                return true;
            }
            
            LuminTaskSourceCore<TValue>.TrySetResult(state.Source, nextValue);
            LuminTaskSourceCore<TValue>.Dispose(state.Source);
            stateTuple.Dispose();
            return false;
        }
        catch (Exception ex)
        {
            LuminTaskSourceCore<TValue>.TrySetException(state.Source, ex);
            LuminTaskSourceCore<TValue>.Dispose(state.Source);
            stateTuple.Dispose();
            return false;
        }
    }

    void Dispose()
    {
        _cancellationToken = default;
        _cancelImmediately = default;

        if (_core != null)
        {
            LuminTaskSourceCore<TValue>.Dispose(_core);
        }
    }
}