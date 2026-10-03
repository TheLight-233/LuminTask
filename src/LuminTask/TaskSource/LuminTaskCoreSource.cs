using System;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Threading;
using LuminThread.Interface;
using LuminThread.Utility;

namespace LuminThread.TaskSource;

// The one completion protocol in the library. Every task kind - factories, completion sources,
// async methods, loop-driven sources, combinators - runs through this single non-generic static
// class: there are no per-kind / per-T vtables and no source blocks any more. The only generic
// part is the result payload, and that lives in LuminTaskResultBridge.
//
// Completion is decided by the CONTINUATION SLOT, not by the status word:
//
//   null      - no awaiter has registered yet
//   sentinel  - the producer already drained (terminal status and payload are published)
//   anything  - a registered continuation
//
// This ordering is what makes the protocol race-free: a producer publishes the payload and the
// status first and then exchanges the continuation slot as its very last touch. A consumer that
// observes the sentinel is therefore guaranteed to see the payload, and a consumer that observes
// a status word alone never concludes anything - so the window where a consumer could release the
// slot while the producer is still about to exchange it cannot open.
//
// Single releaser: the producer holds nothing but a 64-bit handle (no pointer to misuse), and only
// the consumer's GetResult / Forget returns the slot.
//
// Resolve hands back three things at once - the hot item ref, the chunk's cold side and the
// chunk-local index - because the hot fields (continuation/state/context) and the cold ones
// (exception/token/registration/result) live in different arrays by design.
internal static class LuminTaskCoreSource
{
    private static readonly Action<object> CompletedSentinelAction = SentinelInvoked;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void SentinelInvoked(object state) => LuminTaskExceptionHelper.ThrowSentinelInvoked();

    internal static Action<object> CompletedSentinel => CompletedSentinelAction;

    private const long ClaimFlag = 1;
    private const int StatusShift = 8;
    private const int GenerationShift = 16;

    // ---------------------------------------------------------------- handle -> slot

    // Any failed check throws: a consumer (GetResult / Forget) arriving with a stale handle is a
    // contract violation.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static ref LuminTaskItem Resolve(LuminTaskHandle handle, out LuminTaskCold cold, out int index)
    {
        uint raw = handle.SlotRaw();

        if (raw == 0)
            LuminTaskExceptionHelper.ThrowNullHandle();

        int id = (int)raw - 1;

        if (id >= LuminTaskBag.Capacity)
            LuminTaskExceptionHelper.ThrowInvalidHandle();

        ref var item = ref LuminTaskBag.GetItem(id);

        if (item.Generation != handle.Generation())
            LuminTaskExceptionHelper.ThrowStaleHandle();

        cold = LuminTaskBag.GetCold(id);
        index = id & LuminTaskBag.CHUNK_MASK;

        return ref item;
    }

    // Producer side (loop-driven sources, cancellation callbacks) may arrive after the consumer:
    // once the handle is stale the operation gives up quietly instead of throwing.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static ref LuminTaskItem TryResolve(LuminTaskHandle handle, out LuminTaskCold cold, out int index)
    {
        uint raw = handle.SlotRaw();

        if (raw == 0)
        {
            cold = null!;
            index = 0;
            return ref Unsafe.NullRef<LuminTaskItem>();
        }

        int id = (int)raw - 1;

        if (id >= LuminTaskBag.Capacity)
        {
            cold = null!;
            index = 0;
            return ref Unsafe.NullRef<LuminTaskItem>();
        }

        ref var item = ref LuminTaskBag.GetItem(id);

        if (item.Generation != handle.Generation())
        {
            cold = null!;
            index = 0;
            return ref Unsafe.NullRef<LuminTaskItem>();
        }

        cold = LuminTaskBag.GetCold(id);
        index = id & LuminTaskBag.CHUNK_MASK;

        return ref item;
    }

    // ---------------------------------------------------------------- read side

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool IsCompleted(LuminTaskHandle handle)
    {
        ref var item = ref Resolve(handle, out _, out _);

        return ReferenceEquals(Volatile.Read(ref item.Continuation), CompletedSentinelAction);
    }

    internal static LuminTaskStatus GetStatus(LuminTaskHandle handle)
    {
        ref var item = ref Resolve(handle, out _, out _);

        return ReferenceEquals(Volatile.Read(ref item.Continuation), CompletedSentinelAction)
            ? item.Status
            : LuminTaskStatus.Pending;
    }

    // ---------------------------------------------------------------- write side

    // Claims the completion right. The generation is part of this CAS's expected value, so only a
    // single producer can move the slot out of the open state, and a slot that was already recycled
    // to another task fails here instead of being claimed by mistake.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool TryBeginCompletion(ref LuminTaskItem item, uint generation)
    {
        long observed = Volatile.Read(ref item.SyncState);

        while (true)
        {
            if ((uint)((ulong)observed >> GenerationShift) != generation)
                return false;

            if ((observed & ClaimFlag) != 0)
                return false;

            if (Interlocked.CompareExchange(ref item.SyncState, observed | ClaimFlag, observed) == observed)
                return true;

            observed = Volatile.Read(ref item.SyncState);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void PublishStatus(ref LuminTaskItem item, LuminTaskStatus status)
        => item.Status = status;

    // Fires the completion. After this returns the producer must not touch the slot again: the
    // continuation below may consume the task (GetResult -> Return), which recycles the slot and
    // can hand it to a different task.
    internal static void Drain(ref LuminTaskItem item)
    {
        var continuation = Interlocked.Exchange(ref item.Continuation, CompletedSentinelAction);

        if (continuation is null || ReferenceEquals(continuation, CompletedSentinelAction))
            return;

        object? state = item.State;
        var capturedContext = item.CapturedContext;
        item.State = null;
        item.CapturedContext = null;

        FireContinuation(capturedContext, (Action<object>)continuation, state!);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool TrySetResult(LuminTaskHandle handle)
    {
        ref var item = ref TryResolve(handle, out _, out _);

        if (Unsafe.IsNullRef(ref item))
            return false;

        if (!TryBeginCompletion(ref item, handle.Generation()))
            return false;

        PublishStatus(ref item, LuminTaskStatus.Succeeded);
        Drain(ref item);

        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool TrySetException(LuminTaskHandle handle, Exception exception)
    {
        LuminTaskExceptionHelper.ThrowArgumentNullException(exception, nameof(exception));

        if (exception is OperationCanceledException canceled)
            return TrySetCanceled(handle, canceled.CancellationToken);

        ref var item = ref TryResolve(handle, out var cold, out int index);

        if (Unsafe.IsNullRef(ref item))
            return false;

        if (!TryBeginCompletion(ref item, handle.Generation()))
            return false;

        cold.SetException(index, ExceptionDispatchInfo.Capture(exception));
        PublishStatus(ref item, LuminTaskStatus.Faulted);
        Drain(ref item);

        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool TrySetCanceled(LuminTaskHandle handle, CancellationToken cancellationToken = default)
    {
        ref var item = ref TryResolve(handle, out var cold, out int index);

        if (Unsafe.IsNullRef(ref item))
            return false;

        if (!TryBeginCompletion(ref item, handle.Generation()))
            return false;

        cold.SetCancellationToken(index, cancellationToken);
        PublishStatus(ref item, LuminTaskStatus.Canceled);
        Drain(ref item);

        return true;
    }

    // ---------------------------------------------------------------- awaiter side

    // ConfigureAwait(false/true) sets the capture policy before the continuation is registered. A
    // zero handle means the task completed synchronously, so there is no policy to set.
    internal static void SetContinueOnCapturedContext(LuminTaskHandle handle, bool value)
    {
        if (handle.IsNull())
            return;

        ref var item = ref TryResolve(handle, out _, out _);

        if (!Unsafe.IsNullRef(ref item))
            item.ContinueOnCapturedContext = value;
    }

    internal static void OnCompleted(LuminTaskHandle handle, Action<object> continuation, object? state)
    {
        ref var item = ref Resolve(handle, out _, out _);

        var capturedContext = item.ContinueOnCapturedContext ? ExecutionContext.Capture() : null;

        // Publish the state before the continuation pointer: once the producer sees the
        // continuation it is guaranteed to observe the matching state.
        item.State = state;
        item.CapturedContext = capturedContext;

        object? previous = Interlocked.CompareExchange(ref item.Continuation, continuation, null);

        if (previous is null)
            return;

        if (ReferenceEquals(previous, CompletedSentinelAction))
        {
            // The producer already drained and found no awaiter, so nothing will invoke us.
            item.State = null;
            item.CapturedContext = null;
            FireContinuation(capturedContext, continuation, state!);
            return;
        }

        LuminTaskExceptionHelper.ThrowAlreadyAwaited();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void FireContinuation(ExecutionContext? capturedContext, Action<object> continuation, object state)
    {
        if (capturedContext is null)
        {
            continuation(state);
            return;
        }

        // Action<object> and ContextCallback have the same delegate shape, so this reinterprets the
        // reference instead of boxing it.
        ExecutionContext.Run(capturedContext, Unsafe.As<ContextCallback>(continuation), state);
    }

    // ---------------------------------------------------------------- consume and release

    // Consumes a void slot: throws for a failed task, releases on success.
    internal static void ConsumeVoid(LuminTaskHandle handle)
    {
        ref var item = ref Resolve(handle, out var cold, out int index);

        switch (item.Status)
        {
            case LuminTaskStatus.Succeeded:
                break;

            case LuminTaskStatus.Faulted:
            {
                var exception = (ExceptionDispatchInfo?)cold.ExceptionAt(index);
                Return(handle);
                exception?.Throw();
                LuminTaskExceptionHelper.ThrowInvalidError();
                break;
            }

            case LuminTaskStatus.Canceled:
            {
                CancellationToken cancellationToken = cold.CancellationTokenAt(index);
                Return(handle);
                throw new OperationCanceledException(cancellationToken);
            }

            default:
                LuminTaskExceptionHelper.ThrowNotCompleted();
                break;
        }

        Return(handle);
    }

    // Releases the slot without observing the outcome. Exceptions are dropped by design - this is
    // the fire-and-forget exit and the only legal way to skip awaiting a task.
    internal static void Forget(LuminTaskHandle handle) => Return(handle);

    // Returns the slot to the bag. The caller must already have established that it is the single
    // consumer of this slot.
    internal static void Return(LuminTaskHandle handle)
    {
        ref var item = ref Resolve(handle, out var cold, out int index);

        // 1-3) Release everything the cold side holds for this slot: the pending cancellation
        //    registration (Release is non-blocking and safe even when the caller is the cancellation
        //    callback itself), the unmanaged block carrying an unmanaged result or a loop-driven
        //    source's scratch, a pooled result container, and the recorded exception/token.
        //    LuminTaskCold.ReleaseSlot never allocates: a field whose block was never faulted in has
        //    nothing to release.
        cold.ReleaseSlot(index);

        // 4) Drop the hot roots, then bump the generation and recycle. A slot that ran out of
        //    generations is retired instead of wrapping, which is what keeps stale handles unable to
        //    alias a new task.
        item.Continuation = null;
        item.State = null;
        item.CapturedContext = null;

        uint generation = item.Generation;

        if (generation < uint.MaxValue)
        {
            item.Generation = generation + 1;
            LuminTaskBag.ResetId(item.Id);
        }

        LuminTaskLeakTracker.OnReturn(handle); // DEBUG-only; compiled out in Release
    }
}