using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using LuminThread.TaskSource;
using LuminThread.Utility;

namespace LuminThread;

public partial struct LuminTask
{
    internal static ulong RentSlot() => SlotStore.Rent(0, null);

    [DebuggerHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static LuminTask CompletedTask() => new ();

    [DebuggerHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static LuminTask CanceledTask() => FromCanceled(CancellationToken.None);

    [DebuggerHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static LuminTask FromResult() => new();

    [DebuggerHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static LuminTask<T> FromResult<T>(T value) => new (value);

    [DebuggerHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static LuminTask FromException(Exception ex)
    {
        if (ex is OperationCanceledException oce)
        {
            return FromCanceled(oce.CancellationToken);
        }

        var handle = RentSlot();
        LuminTaskSource.TrySetException(handle, ex);

        return new LuminTask(handle);
    }

    [DebuggerHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static LuminTask<T> FromException<T>(Exception ex)
    {
        if (ex is OperationCanceledException oce)
        {
            return FromCanceled<T>(oce.CancellationToken);
        }

        var handle = RentSlot();
        LuminTaskSource.TrySetException(handle, ex);

        return new LuminTask<T>(handle);
    }

    [DebuggerHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static LuminTask FromCanceled(CancellationToken cancellationToken = default)
    {
        var handle = RentSlot();
        LuminTaskSource.TrySetCanceled(handle, cancellationToken);

        return new LuminTask(handle);
    }

    [DebuggerHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static LuminTask<T> FromCanceled<T>(CancellationToken cancellationToken = default)
    {
        var handle = RentSlot();
        LuminTaskSource.TrySetCanceled(handle, cancellationToken);

        return new LuminTask<T>(handle);
    }

    private static void AttachCancellation(LuminTaskHandle handle, CancellationToken cancellationToken)
        => AttachCancellation(handle, cancellationToken, CancellationCallbackState.Callback);

    private static void AttachCancellation(LuminTaskHandle handle, CancellationToken cancellationToken, Action<object> callback)
    {
        var binding = new CancellationCallbackState(handle, cancellationToken);
        try
        {
            binding.Registration = cancellationToken.RegisterWithoutCaptureExecutionContext(callback, binding);
            LuminTaskSource.AttachRegistration(handle, binding);
        }
        catch
        {
            LuminTaskSource.AbandonUnpublished(handle);
            throw;
        }
    }

    [DebuggerHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static LuminTask Never(CancellationToken cancellationToken)
    {
        var handle = RentSlot();

        if (cancellationToken.IsCancellationRequested)
        {
            LuminTaskSource.TrySetCanceled(handle, cancellationToken);
        }
        else if (cancellationToken.CanBeCanceled)
        {
            // A never-completing task can only learn about cancellation from the outside, and the callback
            // needs to find its slot. Never is not a hot path, so the single boxed state is accepted
            // instead of adding a pooled per-slot state object.
            AttachCancellation(handle, cancellationToken);
        }

        return new LuminTask(handle);
    }

    [DebuggerHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static LuminTask<T> Never<T>(CancellationToken cancellationToken)
    {
        var handle = RentSlot();

        if (cancellationToken.IsCancellationRequested)
        {
            LuminTaskSource.TrySetCanceled(handle, cancellationToken);
        }
        else if (cancellationToken.CanBeCanceled)
        {
            AttachCancellation(handle, cancellationToken);
        }

        return new LuminTask<T>(handle);
    }

    [DebuggerHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static LuminTask Create(Func<LuminTask> factory)
    {
        return factory();
    }

    [DebuggerHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static LuminTask Create(Func<CancellationToken, LuminTask> factory, CancellationToken cancellationToken)
    {
        return factory(cancellationToken);
    }

    [DebuggerHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static LuminTask Create<T>(T state, Func<T, LuminTask> factory)
    {
        return factory(state);
    }

    [DebuggerHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static LuminTask<T> Create<T>(Func<LuminTask<T>> factory)
    {
        return factory();
    }
}

public partial struct LuminTask<T>
{
    [DebuggerHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static LuminTask<T> FromResult(T result) => new(result);
}

// Carries a handle into a CancellationToken callback. Only the Never factories and the
// cancelImmediately options of the loop-driven sources need it, so the allocation is confined to
// a cold path. The same box serves both outcomes: cancel the task, or complete it successfully
// (WaitUntilCanceled).
internal sealed class CancellationCallbackState
{
    internal static readonly Action<object> Callback = static state =>
    {
        var box = (CancellationCallbackState)state;
        LuminTaskSource.TrySetCanceled(box.Handle, box.CancellationToken);
    };

    internal static readonly Action<object> CompleteCallback = static state =>
    {
        var box = (CancellationCallbackState)state;
        LuminTaskSource.TrySetResult(box.Handle);
    };

    internal CancellationTokenRegistration Registration;
    internal readonly LuminTaskHandle Handle;
    internal readonly CancellationToken CancellationToken;

    internal CancellationCallbackState(LuminTaskHandle handle, CancellationToken cancellationToken)
    {
        Handle = handle;
        CancellationToken = cancellationToken;
    }
}

internal static class CompletedTasks
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static LuminTask<AsyncUnit> AsyncUnit() => LuminTask<AsyncUnit>.FromResult(Utility.AsyncUnit.Default);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static LuminTask<bool> True() => LuminTask<bool>.FromResult(true);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static LuminTask<bool> False() => LuminTask<bool>.FromResult(false);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static LuminTask<int> Zero() => LuminTask<int>.FromResult(0);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static LuminTask<int> MinusOne() => LuminTask<int>.FromResult(-1);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static LuminTask<int> One() => LuminTask<int>.FromResult(1);
}
