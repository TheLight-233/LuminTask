
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using LuminThread.Interface;
using LuminThread.Utility;

namespace LuminThread.TaskSource.Promise;

public unsafe struct DelayPromise<T>
{
    private long _startTime;  // 使用 long 避免回绕问题
    private int _delayTime;
    private CancellationToken _cancellationToken;
    private CancellationTokenRegistration _cancellationTokenRegistration;
    private bool _cancelImmediately;

    private LuminTaskSourceCore<T>* _core;
    
    internal LuminTaskSourceCore<T>* Source => _core;
    
    public LuminTask Task => new LuminTask(LuminTaskSourceCore<T>.MethodTable, _core, _core->Id);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DelayPromise<T> Create(int millisecondsDelay, PlayerLoopTiming timing, CancellationToken cancellationToken, bool cancelImmediately = false)
    {
        var core = LuminTaskSourceCore<T>.Create();
        var delayPromise = new DelayPromise<T>();

        delayPromise._startTime = GetTimestamp();
        delayPromise._delayTime = millisecondsDelay;
        delayPromise._cancellationToken = cancellationToken;
        delayPromise._cancelImmediately = cancelImmediately;
        delayPromise._core = core;
        
        var promisePtr = MemoryHelper.Alloc((nuint)Unsafe.SizeOf<DelayPromise<T>>());
        Unsafe.WriteUnaligned(promisePtr, delayPromise);

        if (cancellationToken.CanBeCanceled)
        {
            delayPromise._cancellationTokenRegistration = cancellationToken.Register(() =>
            {
                LuminTaskSourceCore<T>.TrySetCanceled(delayPromise._core);
                LuminTaskSourceCore<T>.Dispose(delayPromise._core);
                MemoryHelper.Free(promisePtr);
            });
        }

        PlayerLoopHelper.AddAction(timing, new LuminTaskState(core, cancellationToken, null, valueState: promisePtr), &MoveNext);
        
        return delayPromise;
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
        var promisePtr = state.ValueState;
        if (promisePtr == null) return false;
        
        ref var promise = ref Unsafe.AsRef<DelayPromise<T>>(state.ValueState);
        
        if (state.CancellationToken.IsCancellationRequested)
        {
            LuminTaskSourceCore<T>.TrySetCanceled(state.Source);
            LuminTaskSourceCore<T>.Dispose(state.Source);
            MemoryHelper.Free(promisePtr);
            return false;
        }

        long currentTime = GetTimestamp();
        long elapsed = currentTime - promise._startTime;
        
        if (elapsed >= promise._delayTime)
        {
            LuminTaskSourceCore<T>.TrySetResult(state.Source);
            LuminTaskSourceCore<T>.Dispose(state.Source);
            MemoryHelper.Free(promisePtr);
            return false;
        }

        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static long GetTimestamp()
    {
        return Stopwatch.GetTimestamp() * 1000 / Stopwatch.Frequency;
    }

    private void Dispose()
    {
        _cancellationTokenRegistration.Dispose();
        
        if (_core != null)
        {
            LuminTaskSourceCore<T>.Dispose(_core);
            _core = null;
        }
    }
}
