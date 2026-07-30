
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using LuminThread.Interface;
using LuminThread.Utility;

namespace LuminThread.TaskSource.Promise;

public unsafe struct DelayPromise<T>
{
    private long _startTimestamp;
    private long _delayTimestampTicks;
    private LuminTaskSourceCore<T>* _core;

    internal LuminTaskSourceCore<T>* Source => _core;

    public LuminTask Task => new LuminTask(LuminTaskSourceCore<T>.MethodTableAutoDisposePtr, _core, _core->Id);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DelayPromise<T> Create(
        int millisecondsDelay,
        PlayerLoopTiming timing,
        CancellationToken cancellationToken,
        bool cancelImmediately = false)
    {
        var core = LuminTaskSourceCore<T>.Create();

        var delayPromise = new DelayPromise<T>
        {
            _startTimestamp = Stopwatch.GetTimestamp(),
            _delayTimestampTicks = MillisecondsToStopwatchTicks(millisecondsDelay),
            _core      = core,
        };

        var promisePtr = MemoryHelper.Alloc((nuint)Unsafe.SizeOf<DelayPromise<T>>());
        Unsafe.WriteUnaligned(promisePtr, delayPromise);

        PlayerLoopHelper.AddAction(
            timing,
            new LuminTaskState(core, cancellationToken, null, valueState: promisePtr),
            &MoveNext);

        return delayPromise;
    }

    public LuminTaskStatus GetStatus(short token) => LuminTaskSourceCore<T>.GetStatus(_core, token);
    public LuminTaskStatus UnsafeGetStatus()      => LuminTaskSourceCore<T>.UnsafeGetStatus(_core);

    public void OnCompleted(Action<object> continuation, object state, short token)
        => LuminTaskSourceCore<T>.OnCompleted(_core, continuation, state, token);

    private static bool MoveNext(in LuminTaskState state)
    {
        var promisePtr = state.ValueState;
        if (promisePtr == null) return false;

        ref var promise = ref Unsafe.AsRef<DelayPromise<T>>(promisePtr);

        if (state.CancellationToken.IsCancellationRequested)
        {
            LuminTaskSourceCore<T>.TrySetCanceled(state.Source, state.CancellationToken);
            MemoryHelper.Free(promisePtr);
            return false;
        }

        long elapsed = Stopwatch.GetTimestamp() - promise._startTimestamp;

        if (elapsed >= promise._delayTimestampTicks)
        {
            LuminTaskSourceCore<T>.TrySetResult(state.Source);
            MemoryHelper.Free(promisePtr);
            return false;
        }

        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static long MillisecondsToStopwatchTicks(int milliseconds)
        => milliseconds == System.Threading.Timeout.Infinite
            ? long.MaxValue
            : (long)(milliseconds * ((double)Stopwatch.Frequency / 1000D));
}
