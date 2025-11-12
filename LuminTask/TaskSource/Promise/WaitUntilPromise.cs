using System;
using System.Runtime.CompilerServices;
using System.Threading;
using LuminThread.Interface;

namespace LuminThread.TaskSource.Promise;

public unsafe struct WaitUntilPromise<T>
{
    Func<bool> _predicate;
    CancellationToken _cancellationToken;
    CancellationTokenRegistration _cancellationTokenRegistration;
    bool _cancelImmediately;

    LuminTaskSourceCore<T>* _core;
    
    internal LuminTaskSourceCore<T>* Source => _core;

    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static WaitUntilPromise<T> Create(Func<bool> predicate, PlayerLoopTiming timing, CancellationToken cancellationToken, bool cancelImmediately, out short token)
    {
        var core = LuminTaskSourceCore<T>.Create();

        var waitPromise = new WaitUntilPromise<T>();

        waitPromise._predicate = predicate;
        waitPromise._cancellationToken = cancellationToken;
        waitPromise._cancelImmediately = cancelImmediately;
        waitPromise._core = core;
        
        token = core->Id;
        
        PlayerLoopHelper.AddAction(timing, new LuminTaskState(core, cancellationToken, predicate), MoveNext);
        
        return waitPromise;
    }
    
    public void GetResult(short token)
    {
        try
        {
            LuminTaskSourceCore<T>.GetResult(_core, token);
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
        return LuminTaskSourceCore<T>.GetStatus(_core, token);
    }

    public LuminTaskStatus UnsafeGetStatus()
    {
        return LuminTaskSourceCore<T>.UnsafeGetStatus(_core);
    }

    public void OnCompleted(Action<object> continuation, object state, short token)
    {
        LuminTaskSourceCore<T>.OnCompleted(_core, continuation, state, token);
    }

    private static bool MoveNext(in LuminTaskState state)
    {
        if (state.CancellationToken.IsCancellationRequested)
        {
            LuminTaskSourceCore<T>.TrySetCanceled(state.Source);
            
            return false;
        }

        try
        {
            if (!((Func<bool>)state.State)())
            {
                return true;
            }
        }
        catch (Exception ex)
        {
            LuminTaskSourceCore<T>.TrySetException(state.Source, ex);
            return false;
        }

        LuminTaskSourceCore<T>.TrySetResult(state.Source);
        return false;
    }

    void Dispose()
    {
        _predicate = default;
        _cancellationToken = default;
        _cancellationTokenRegistration.Dispose();
        _cancelImmediately = default;

        if (_core != null)
        {
            LuminTaskSourceCore<T>.Dispose(_core);
        }
    }
}

public unsafe struct WaitUntilPromise<TState, TResult>
{
    TState _state;
    Func<TState, bool> _predicate;
    CancellationToken _cancellationToken;
    CancellationTokenRegistration _cancellationTokenRegistration;
    bool _cancelImmediately;

    LuminTaskSourceCore<TResult>* _core;
    
    internal LuminTaskSourceCore<TResult>* Source => _core;
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static WaitUntilPromise<TState, TResult> Create(TState state, Func<TState, bool> predicate, PlayerLoopTiming timing, CancellationToken cancellationToken, bool cancelImmediately, out short token)
    {
        var core = LuminTaskSourceCore<TResult>.Create();

        var waitPromise = new WaitUntilPromise<TState, TResult>();
        waitPromise._core = core;
        waitPromise._state = state;
        waitPromise._predicate = predicate;
        waitPromise._cancellationToken = cancellationToken;
        waitPromise._cancelImmediately = cancelImmediately;

        token = core->Id;
        
        PlayerLoopHelper.AddAction(timing, new LuminTaskState(core, cancellationToken, predicate), waitPromise.MoveNext);
        
        return waitPromise;
    }
    
    public void GetResult(short token)
    {
        try
        {
            LuminTaskSourceCore<TResult>.GetResult(_core, token);
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
        return LuminTaskSourceCore<TResult>.GetStatus(_core, token);
    }

    public LuminTaskStatus UnsafeGetStatus()
    {
        return LuminTaskSourceCore<TResult>.UnsafeGetStatus(_core);
    }

    public void OnCompleted(Action<object> continuation, object state, short token)
    {
        LuminTaskSourceCore<TResult>.OnCompleted(_core, continuation, state, token);
    }

    private bool MoveNext(in LuminTaskState state)
    {
        if (state.CancellationToken.IsCancellationRequested)
        {
            LuminTaskSourceCore<TResult>.TrySetCanceled(state.Source);
            LuminTaskSourceCore<TResult>.Dispose(state.Source);
            return false;
        }

        try
        {
            if (!_predicate(_state))
            {
                return true;
            }
        }
        catch (Exception ex)
        {
            LuminTaskSourceCore<TResult>.TrySetException(state.Source, ex);
            LuminTaskSourceCore<TResult>.Dispose(state.Source);
            return false;
        }

        LuminTaskSourceCore<TResult>.TrySetResult(state.Source);
        LuminTaskSourceCore<TResult>.Dispose(state.Source);
        return false;
    }

    void Dispose()
    {
        _state = default;
        _predicate = default;
        _cancellationToken = default;
        _cancellationTokenRegistration.Dispose();
        _cancelImmediately = default;

        if (_core != null)
        {
            LuminTaskSourceCore<TResult>.Dispose(_core);
        }
    }
}