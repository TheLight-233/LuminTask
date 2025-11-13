using System;
using System.Runtime.CompilerServices;
using System.Threading;
using LuminThread.Interface;
using LuminThread.Utility;

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
        
        PlayerLoopHelper.AddAction(timing, new LuminTaskState(core, cancellationToken, predicate), &MoveNext);
        
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
        Func<bool> predicate = Unsafe.As<Func<bool>>(state.State);
        
        if (state.CancellationToken.IsCancellationRequested)
        {
            LuminTaskSourceCore<T>.TrySetCanceled(state.Source);
            LuminTaskSourceCore<T>.Dispose(state.Source);
            
            return false;
        }

        try
        {
            if (!predicate())
            {
                return true;
            }
        }
        catch (Exception ex)
        {
            LuminTaskSourceCore<T>.TrySetException(state.Source, ex);
            LuminTaskSourceCore<T>.Dispose(state.Source);
            return false;
        }

        LuminTaskSourceCore<T>.TrySetResult(state.Source);
        LuminTaskSourceCore<T>.Dispose(state.Source);
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

        if (TypeMeta<TState>.IsValueType)
        {
#if NET8_0_OR_GREATER
            if (LuminTask.Model is LuminTaskModel.Unsafe || !TypeMeta<TState>.IsReferenceOrContainsReferences)
            {
                var ptr = MemoryHelper.Alloc(TypeMeta<TState>.Size);
                Unsafe.WriteUnaligned(ptr, state);
                PlayerLoopHelper.AddAction(timing, new LuminTaskState(core, cancellationToken, predicate, ptr), &MoveNextValue);
            }
            else
            {
                PlayerLoopHelper.AddAction(timing, new LuminTaskState(core, cancellationToken, predicate, StateTuple.Create(state)), &MoveNextValueContainsReference);
            }
#else 
            var ptr = MemoryHelper.Alloc(TypeMeta<TState>.Size);
            Unsafe.WriteUnaligned(ptr, state);
            PlayerLoopHelper.AddAction(timing, new LuminTaskState(core, cancellationToken, predicate, ptr), &MoveNextValue);
#endif
        }
        else
        {
            PlayerLoopHelper.AddAction(timing, new LuminTaskState(core, cancellationToken, predicate, state!), &MoveNextReference);
        }
        
        
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

    private static bool MoveNextReference(in LuminTaskState state)
    {
        Func<TState, bool> predicate = Unsafe.As<Func<TState, bool>>(state.State);
        TState tState = (TState)state.StateTuple;
        
        if (state.CancellationToken.IsCancellationRequested)
        {
            LuminTaskSourceCore<TResult>.TrySetCanceled(state.Source);
            LuminTaskSourceCore<TResult>.Dispose(state.Source);
            return false;
        }

        try
        {
            if (!predicate(tState))
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
    
    private static bool MoveNextValue(in LuminTaskState state)
    {
        Func<TState, bool> predicate = Unsafe.As<Func<TState, bool>>(state.State);
        TState tState = Unsafe.ReadUnaligned<TState>(state.ValueState);
        
        if (state.CancellationToken.IsCancellationRequested)
        {
            LuminTaskSourceCore<TResult>.TrySetCanceled(state.Source);
            LuminTaskSourceCore<TResult>.Dispose(state.Source);
            MemoryHelper.Free(state.ValueState);
            return false;
        }

        try
        {
            if (!predicate(tState))
            {
                return true;
            }
        }
        catch (Exception ex)
        {
            LuminTaskSourceCore<TResult>.TrySetException(state.Source, ex);
            LuminTaskSourceCore<TResult>.Dispose(state.Source);
            MemoryHelper.Free(state.ValueState);
            return false;
        }

        LuminTaskSourceCore<TResult>.TrySetResult(state.Source);
        LuminTaskSourceCore<TResult>.Dispose(state.Source);
        MemoryHelper.Free(state.ValueState);
        
        return false;
    }
    
    private static bool MoveNextValueContainsReference(in LuminTaskState state)
    {
        Func<TState, bool> predicate = Unsafe.As<Func<TState, bool>>(state.State);
        StateTuple<TState> stateTuple = Unsafe.As<StateTuple<TState>>(state.StateTuple);
        
        if (state.CancellationToken.IsCancellationRequested)
        {
            LuminTaskSourceCore<TResult>.TrySetCanceled(state.Source);
            LuminTaskSourceCore<TResult>.Dispose(state.Source);
            stateTuple.Dispose();
            return false;
        }

        try
        {
            if (!predicate(stateTuple.Item1))
            {
                return true;
            }
        }
        catch (Exception ex)
        {
            LuminTaskSourceCore<TResult>.TrySetException(state.Source, ex);
            LuminTaskSourceCore<TResult>.Dispose(state.Source);
            stateTuple.Dispose();
            return false;
        }

        LuminTaskSourceCore<TResult>.TrySetResult(state.Source);
        LuminTaskSourceCore<TResult>.Dispose(state.Source);
        stateTuple.Dispose();
        
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