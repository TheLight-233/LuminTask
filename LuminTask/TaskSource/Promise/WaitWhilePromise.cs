using System;
using System.Runtime.CompilerServices;
using System.Threading;
using LuminThread.Interface;

namespace LuminThread.TaskSource.Promise;

public unsafe struct WaitWhilePromise<T>
{
    Func<bool> _predicate;
    CancellationToken _cancellationToken;
    CancellationTokenRegistration _cancellationTokenRegistration;
    bool _cancelImmediately;

    LuminTaskSourceCore<T>* _core;
    
    internal LuminTaskSourceCore<T>* Source => _core;
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static WaitWhilePromise<T> Create(Func<bool> predicate, PlayerLoopTiming timing, CancellationToken cancellationToken, bool cancelImmediately, out short token)
    {
        var core = LuminTaskSourceCore<T>.Create();

        var waitPromise = new WaitWhilePromise<T>();
        waitPromise._core = core;
        waitPromise._predicate = predicate;
        waitPromise._cancellationToken = cancellationToken;
        waitPromise._cancelImmediately = cancelImmediately;

        token = core->Id;
        
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

    public bool MoveNext()
    {
        if (_cancellationToken.IsCancellationRequested)
        {
            LuminTaskSourceCore<T>.TrySetCanceled(_core);
            return false;
        }

        try
        {
            if (_predicate())
            {
                return true;
            }
        }
        catch (Exception ex)
        {
            LuminTaskSourceCore<T>.TrySetException(_core, ex);
            return false;
        }

        LuminTaskSourceCore<T>.TrySetResult(_core);
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

public unsafe struct WaitWhilePromise<TState, TResult>
{
    TState _state;
    Func<TState, bool> _predicate;
    CancellationToken _cancellationToken;
    CancellationTokenRegistration _cancellationTokenRegistration;
    bool _cancelImmediately;

    LuminTaskSourceCore<TResult>* _core;
    
    internal LuminTaskSourceCore<TResult>* Source => _core;
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static WaitWhilePromise<TState, TResult> Create(TState state, Func<TState, bool> predicate, PlayerLoopTiming timing, CancellationToken cancellationToken, bool cancelImmediately, out short token)
    {
        var core = LuminTaskSourceCore<TResult>.Create();

        var waitPromise = new WaitWhilePromise<TState, TResult>();
        waitPromise._core = core;
        waitPromise._state = state;
        waitPromise._predicate = predicate;
        waitPromise._cancellationToken = cancellationToken;
        waitPromise._cancelImmediately = cancelImmediately;

        token = core->Id;
        
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

    public bool MoveNext()
    {
        if (_cancellationToken.IsCancellationRequested)
        {
            LuminTaskSourceCore<TResult>.TrySetCanceled(_core);
            return false;
        }

        try
        {
            if (_predicate(_state))
            {
                return true;
            }
        }
        catch (Exception ex)
        {
            LuminTaskSourceCore<TResult>.TrySetException(_core, ex);
            return false;
        }

        LuminTaskSourceCore<TResult>.TrySetResult(_core);
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