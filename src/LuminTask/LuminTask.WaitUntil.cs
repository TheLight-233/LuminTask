using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using LuminThread.TaskSource;
using LuminThread.TaskSource.Promise;
using LuminThread.Utility;

namespace LuminThread;

public partial struct LuminTask
{
    [DebuggerHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static LuminTask WaitUntil(Func<bool> condition, PlayerLoopTiming timing = PlayerLoopTiming.DotNet,
        CancellationToken cancellationToken = default, bool cancelImmediately = false)
    {
        var handle = RentSlot();

        if (cancelImmediately && cancellationToken.CanBeCanceled)
            AttachCancellation(handle, cancellationToken);

        PlayerLoopHelper.AddAction(timing, new LuminTaskState(handle, cancellationToken, condition), WaitUntilPromise.MoveNextAction);

        return new LuminTask(handle);
    }

    [DebuggerHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static LuminTask WaitUntil<T>(T state, Func<T, bool> predicate,
        PlayerLoopTiming timing = PlayerLoopTiming.DotNet,
        CancellationToken cancellationToken = default(CancellationToken), bool cancelImmediately = false)
    {
        var handle = RentSlot();
        ref var item = ref GetRentedSlot(handle, out var cold, out int index);

        LuminTaskResultBridge.SetState(cold, index, in state);

        if (cancelImmediately && cancellationToken.CanBeCanceled)
            AttachCancellation(handle, cancellationToken);

        PlayerLoopHelper.AddAction(timing, new LuminTaskState(handle, cancellationToken, predicate), WaitUntilPromise<T>.MoveNextAction);

        return new LuminTask(handle);
    }

    [DebuggerHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static LuminTask WaitWhile(Func<bool> predicate, PlayerLoopTiming timing = PlayerLoopTiming.DotNet,
        CancellationToken cancellationToken = default(CancellationToken), bool cancelImmediately = false)
    {
        var handle = RentSlot();

        if (cancelImmediately && cancellationToken.CanBeCanceled)
            AttachCancellation(handle, cancellationToken);

        PlayerLoopHelper.AddAction(timing, new LuminTaskState(handle, cancellationToken, predicate), WaitWhilePromise.MoveNextAction);

        return new LuminTask(handle);
    }

    [DebuggerHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static LuminTask WaitWhile<T>(T state, Func<T, bool> predicate,
        PlayerLoopTiming timing = PlayerLoopTiming.DotNet,
        CancellationToken cancellationToken = default(CancellationToken), bool cancelImmediately = false)
    {
        var handle = RentSlot();
        ref var item = ref GetRentedSlot(handle, out var cold, out int index);

        LuminTaskResultBridge.SetState(cold, index, in state);

        if (cancelImmediately && cancellationToken.CanBeCanceled)
            AttachCancellation(handle, cancellationToken);

        PlayerLoopHelper.AddAction(timing, new LuminTaskState(handle, cancellationToken, predicate), WaitWhilePromise<T>.MoveNextAction);

        return new LuminTask(handle);
    }

    [DebuggerHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static LuminTask WaitUntilCanceled(CancellationToken cancellationToken,
        PlayerLoopTiming timing = PlayerLoopTiming.DotNet, bool completeImmediately = false)
    {
        var handle = RentSlot();

        if (cancellationToken.IsCancellationRequested)
        {
            // Cancellation here means success, not failure.
            LuminTaskCoreSource.TrySetResult(handle);
            return new LuminTask(handle);
        }

        if (completeImmediately && cancellationToken.CanBeCanceled)
            AttachCancellation(handle, cancellationToken, CancellationCallbackState.CompleteCallback);

        PlayerLoopHelper.AddAction(timing, new LuminTaskState(handle, cancellationToken), WaitUntilCanceledPromise.MoveNextAction);

        return new LuminTask(handle);
    }

    [DebuggerHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static LuminTask<U> WaitUntilValueChanged<T, U>(
        T target,
        Func<T, U> monitorFunction,
        PlayerLoopTiming monitorTiming = PlayerLoopTiming.DotNet,
        IEqualityComparer<U>? equalityComparer = null,
        CancellationToken cancellationToken = default,
        bool cancelImmediately = false)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return LuminTask.FromCanceled<U>(cancellationToken);
        }

        equalityComparer ??= EqualityComparer<U>.Default;

        var handle = RentSlot();
        var helper = new WaitUntilValueChangedState<T, U>(
            target, monitorFunction, equalityComparer, monitorFunction(target));

        if (cancelImmediately && cancellationToken.CanBeCanceled)
            AttachCancellation(handle, cancellationToken);

        PlayerLoopHelper.AddAction(monitorTiming, new LuminTaskState(handle, cancellationToken, helper), WaitUntilValueChangedPromise<T, U>.MoveNextAction);

        return new LuminTask<U>(handle);
    }
}