using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using LuminThread.TaskSource;
using LuminThread.TaskSource.Promise;
using LuminThread.Utility;

namespace LuminThread;

public readonly partial struct LuminTask
{
    [DebuggerHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe LuminTask WaitUntil(Func<bool> condition, PlayerLoopTiming timing = PlayerLoopTiming.DotNet,
        CancellationToken cancellationToken = default, bool cancelImmediately = false)
    {
        var promise = WaitUntilPromise<AsyncUnit>.Create(condition, timing, cancellationToken, cancelImmediately, out var id);
        
        return new LuminTask(LuminTaskSourceCore<AsyncUnit>.MethodTable, promise.Source, id);
    }

    [DebuggerHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe LuminTask WaitUntil<T>(T state, Func<T, bool> predicate,
        PlayerLoopTiming timing = PlayerLoopTiming.DotNet,
        CancellationToken cancellationToken = default(CancellationToken), bool cancelImmediately = false)
    {
        var promise = WaitUntilPromise<T, AsyncUnit>.Create(state, predicate, timing, cancellationToken, cancelImmediately, out var id);
        
        return new LuminTask(LuminTaskSourceCore<AsyncUnit>.MethodTable, promise.Source, id);
    }

    [DebuggerHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe LuminTask WaitWhile(Func<bool> predicate, PlayerLoopTiming timing = PlayerLoopTiming.DotNet,
        CancellationToken cancellationToken = default(CancellationToken), bool cancelImmediately = false)
    {
        var promise = WaitWhilePromise<AsyncUnit>.Create(predicate, timing, cancellationToken, cancelImmediately, out var id);
        
        return new LuminTask(LuminTaskSourceCore<AsyncUnit>.MethodTable, promise.Source, id);
    }

    [DebuggerHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe LuminTask WaitWhile<T>(T state, Func<T, bool> predicate,
        PlayerLoopTiming timing = PlayerLoopTiming.DotNet,
        CancellationToken cancellationToken = default(CancellationToken), bool cancelImmediately = false)
    {
        var promise = WaitWhilePromise<T, AsyncUnit>.Create(state, predicate, timing, cancellationToken, cancelImmediately, out var id);
        
        return new LuminTask(LuminTaskSourceCore<AsyncUnit>.MethodTable, promise.Source, id);
    }

    [DebuggerHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe LuminTask WaitUntilCanceled(CancellationToken cancellationToken,
        PlayerLoopTiming timing = PlayerLoopTiming.DotNet, bool completeImmediately = false)
    {
        var promise = WaitUntilCanceledPromise<AsyncUnit>.Create(cancellationToken, timing, completeImmediately, out var id);
        
        return new LuminTask(LuminTaskSourceCore<AsyncUnit>.MethodTable, promise.Source, id);
    }

    [DebuggerHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe LuminTask<U> WaitUntilValueChanged<T, U>(
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
        
        var promise = WaitUntilValueChangedPromise<T, U>.Create(
            target, monitorFunction, equalityComparer, monitorTiming,
            cancellationToken, cancelImmediately, out var id);
        
        return new LuminTask<U>(LuminTaskSourceCore<U>.MethodTable, promise.Source, id);
    }
}