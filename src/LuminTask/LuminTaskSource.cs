using System;
using System.Runtime.CompilerServices;
using System.Threading;
using LuminThread.TaskSource;

namespace LuminThread;

// Handle-based source entry point, for code that lives outside this assembly and therefore cannot
// reach the internal core (notably the Unity integrations, which compile into the user's
// Assembly-CSharp).
//
// The point is what it avoids: no LuminTaskCompletionSource object is created, so driving a source
// from a player-loop callback costs no allocation per operation. The loop item carries the raw
// handle, and the callback completes it through the static setters below.
//
// The contract is the same as for every other source in the library: exactly one completion. The
// setters are Try* because the first completion wins - a late or duplicated call is ignored rather
// than throwing, which is what makes them safe to call from a cancellation callback racing the
// player loop.
public static class LuminTaskSource
{
    /// <summary>
    /// Rents a source and returns the task that observes it. The caller owns exactly one completion
    /// of <paramref name="handle"/>; a handle that is never completed keeps its slot rented.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static LuminTask Create(out LuminTaskHandle handle)
    {
        handle = LuminTask.RentSlot();

        return new LuminTask(handle);
    }

    /// <summary>
    /// Typed counterpart of <see cref="Create(out LuminTaskHandle)"/>.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static LuminTask<T> Create<T>(out LuminTaskHandle handle)
    {
        handle = LuminTask.RentSlot();

        return new LuminTask<T>(handle);
    }

    /// <summary>
    /// Rents a source without materialising the task. Use this when the caller only keeps the handle
    /// and rebuilds the task later with <see cref="ToTask"/> - the shape an event handler with an
    /// idempotent ToLuminTask needs. Discarding a task to get here would be rejected by the analyzer
    /// (LUM1005), which is why this entry point exists separately from Create.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Rent(out LuminTaskHandle handle) => handle = LuminTask.RentSlot();

    /// <summary>Typed counterpart of <see cref="Rent(out LuminTaskHandle)"/>.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Rent<T>(out LuminTaskHandle handle) => handle = LuminTask.RentSlot();

    /// <summary>
    /// Rebuilds the task that observes <paramref name="handle"/>. A zero handle means "no source"
    /// and yields a completed task, which is what lets a caller that only kept the handle (instead
    /// of the task struct) hand the same task out again - for example an event handler whose
    /// ToLuminTask must stay idempotent across repeated calls.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static LuminTask ToTask(LuminTaskHandle handle) => new (handle);

    /// <summary>Typed counterpart of <see cref="ToTask"/>.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static LuminTask<T> ToTask<T>(LuminTaskHandle handle) => new (handle);

    /// <summary>Completes the source successfully. Returns false if it was already completed.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool SetResult(LuminTaskHandle handle) => LuminTaskCoreSource.TrySetResult(handle);

    /// <summary>Completes the source with a result. Returns false if it was already completed.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool SetResult<T>(LuminTaskHandle handle, T result)
        => LuminTaskResultBridge.TrySetResult(handle, in result);

    /// <summary>Completes the source with a fault. Returns false if it was already completed.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool SetException(LuminTaskHandle handle, Exception exception)
        => LuminTaskCoreSource.TrySetException(handle, exception);

    /// <summary>Completes the source as canceled. Returns false if it was already completed.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool SetCanceled(LuminTaskHandle handle, CancellationToken cancellationToken = default)
        => LuminTaskCoreSource.TrySetCanceled(handle, cancellationToken);
}