using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using LuminThread.TaskSource;
using LuminThread.Utility;

namespace LuminThread;

public unsafe partial struct LuminTask
{
    // Rents a slot from the bag and clears it. Factories, completion sources and suspended async
    // methods all share this single lifecycle, which is why there is no per-source kind any more.
    internal static LuminTaskHandle RentSlot()
    {
        short id = LuminTaskBag.GetId();
        ref var item = ref LuminTaskBag.GetItem(id);

        LuminTaskHandle handle = LuminTaskHandleBits.Create(id, item.Generation);

        LuminTaskLeakTracker.OnRent(handle); // DEBUG-only; compiled out in Release

        return handle;
    }

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
        LuminTaskCoreSource.TrySetException(handle, ex);

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
        LuminTaskCoreSource.TrySetException(handle, ex);

        return new LuminTask<T>(handle);
    }

    [DebuggerHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static LuminTask FromCanceled(CancellationToken cancellationToken = default)
    {
        var handle = RentSlot();
        LuminTaskCoreSource.TrySetCanceled(handle, cancellationToken);

        return new LuminTask(handle);
    }

    [DebuggerHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static LuminTask<T> FromCanceled<T>(CancellationToken cancellationToken = default)
    {
        var handle = RentSlot();
        LuminTaskCoreSource.TrySetCanceled(handle, cancellationToken);

        return new LuminTask<T>(handle);
    }

    // The handle was just rented, so it is valid by construction and does not need the checking
    // resolve path.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ref LuminTaskItem GetRentedItem(LuminTaskHandle handle)
        => ref LuminTaskBag.GetItem((int)handle.SlotRaw() - 1);

    // Same as GetRentedItem, but also hands back the cold side and the chunk-local index, which is
    // what the loop-driven sources need for their scratch and their registration.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ref LuminTaskItem GetRentedSlot(LuminTaskHandle handle, out LuminTaskCold cold, out int index)
    {
        int id = (int)handle.SlotRaw() - 1;
        cold = LuminTaskBag.GetCold(id);
        index = id & LuminTaskBag.CHUNK_MASK;
        return ref LuminTaskBag.GetItem(id);
    }

    // Attaches an outside-in cancellation to a slot (Never, and the cancelImmediately option of the
    // loop-driven sources). The slot owns the registration and the core releases it on Return, so
    // nothing leaks if the task is consumed after cancellation.
    private static void AttachCancellation(LuminTaskHandle handle, CancellationToken cancellationToken)
        => AttachCancellation(handle, cancellationToken, CancellationCallbackState.Callback);

    private static void AttachCancellation(LuminTaskHandle handle, CancellationToken cancellationToken, Action<object> callback)
    {
        ref var item = ref GetRentedSlot(handle, out var cold, out int index);

        if (Unsafe.IsNullRef(ref item))
            return;

        var box = new CancellationCallbackState(handle, cancellationToken);

        cold.SetRegistration(index, cancellationToken.RegisterWithoutCaptureExecutionContext(callback, box));
    }

    [DebuggerHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static LuminTask Never(CancellationToken cancellationToken)
    {
        var handle = RentSlot();

        if (cancellationToken.IsCancellationRequested)
        {
            LuminTaskCoreSource.TrySetCanceled(handle, cancellationToken);
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
            LuminTaskCoreSource.TrySetCanceled(handle, cancellationToken);
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
        LuminTaskCoreSource.TrySetCanceled(box.Handle, box.CancellationToken);
    };

    internal static readonly Action<object> CompleteCallback = static state =>
    {
        var box = (CancellationCallbackState)state;
        LuminTaskCoreSource.TrySetResult(box.Handle);
    };

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
