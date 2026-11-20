using System;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Threading;
using LuminThread.CompilerServices;
using LuminThread.Interface;
using LuminThread.Utility;

namespace LuminThread;

public static partial class LuminTaskSource
{
    internal const long Active = 4;
    private const long Busy = 8;
    private const long Registered = 16;
    private const long Capture = 32;
    internal const int KindShift = 8;
    internal const long KindMask = 31L << KindShift;
    internal const long Reference = 17L << KindShift;
    internal const long Cell = 18L << KindShift;
    private const long StatusMask = 3;
    private const long IdentityMask = ~65535L;

    public static void Trim()
    {
        SlotStore.Trim();
        LuminTaskIdleCache.Trim();
        PlayerLoopHelper.TrimBuffers();
    }

    internal static long ReadControl(ulong handle)
    {
        var page = SlotStore.Find(handle, out int offset);
        if (page == null) LuminTaskExceptionHelper.ThrowStaleHandle();
        long control = Volatile.Read(ref page.Slots[offset].Control);
        if (!Matches(control, handle)) LuminTaskExceptionHelper.ThrowStaleHandle();
        return control;
    }

    private static bool Matches(long control, ulong handle) =>
        (control & Active) != 0 && ((control ^ (long)handle) & IdentityMask) == 0;

    // Never hold the slot gate over a pool/allocator lock or a user callback.
    internal static SlotStore.Page? Enter(ulong handle, out int offset, out long control, bool pendingOnly = false)
    {
        var page = SlotStore.Find(handle, out offset);
        control = 0;
        if (page == null) return null;
        ref long word = ref page.Slots[offset].Control;
        var spin = new SpinWait();
        do
        {
            control = Volatile.Read(ref word);
            if (!Matches(control, handle) || (pendingOnly && (control & StatusMask) != 0)) return null;
            if ((control & Busy) == 0 && Interlocked.CompareExchange(ref word, control | Busy, control) == control)
                return page;
            spin.SpinOnce();
        } while (true);
    }

    internal static LuminTaskStatus GetStatus(ulong handle) => (LuminTaskStatus)(ReadControl(handle) & StatusMask);
    internal static bool IsCompleted(ulong handle) => GetStatus(handle) != LuminTaskStatus.Pending;

    internal static void SetContinueOnCapturedContext(ulong handle, bool capture)
    {
        if (handle == 0) return;
        var page = Enter(handle, out int i, out long control);
        if (page == null) LuminTaskExceptionHelper.ThrowStaleHandle();
        bool registered = (control & Registered) != 0;
        if (!registered) control = capture ? control | Capture : control & ~Capture;
        Volatile.Write(ref page.Slots[i].Control, control);
        if (registered) LuminTaskExceptionHelper.ThrowAlreadyAwaited();
    }

    internal static void OnCompleted(ulong handle, Action continuation)
    {
        if (continuation == null) throw new ArgumentNullException(nameof(continuation));
        if (handle == 0) { continuation(); return; }
        OnCompleted(handle, static state => ((Action)state)(), continuation);
    }

    internal static void OnCompleted(ulong handle, Action<object> continuation, object? state)
    {
        if (continuation == null) throw new ArgumentNullException(nameof(continuation));
        var page = Enter(handle, out int i, out long control);
        if (page == null) LuminTaskExceptionHelper.ThrowStaleHandle();
        if ((control & Capture) != 0)
        {
            Volatile.Write(ref page.Slots[i].Control, control);
            RegisterCaptured(handle, continuation, state);
            return;
        }
        RegisterLocked(ref page.Slots[i], control, continuation, state);
    }

    private static void RegisterCaptured(ulong handle, Action<object> continuation, object? state)
    {
        var context = ExecutionContext.Capture();
        StateTuple<(ExecutionContext, Action<object>, object?)>? wrapper = null;
        if (context != null)
        {
            wrapper = StateTuple.Create((context, continuation, state));
            state = wrapper;
            continuation = RunCaptured;
        }

        var page = Enter(handle, out int i, out long control);
        if (page == null || (control & Registered) != 0)
        {
            if (page != null) Volatile.Write(ref page.Slots[i].Control, control);
            wrapper?.Dispose();
            if (page == null) LuminTaskExceptionHelper.ThrowStaleHandle();
            LuminTaskExceptionHelper.ThrowAlreadyAwaited();
        }

        RegisterLocked(ref page.Slots[i], control, continuation, state);
    }

    private static void RegisterLocked(ref Slot slot, long control, Action<object> continuation, object? state)
    {
        if ((control & Registered) != 0)
        {
            Volatile.Write(ref slot.Control, control);
            LuminTaskExceptionHelper.ThrowAlreadyAwaited();
        }
        bool ready = (control & StatusMask) != 0;
        if (!ready)
        {
            slot.Continuation = continuation;
            slot.ContinuationState = state;
        }
        Volatile.Write(ref slot.Control, control | Registered);
        if (ready) Invoke(continuation, state);
    }

    private static void Invoke(Action<object> callback, object? state)
    {
        try { callback(state!); }
        catch (Exception ex) { LuminTaskScheduler.PublishUnobservedTaskException(ex); }
    }

    private static void RunCaptured(object state)
    {
        var wrapper = (StateTuple<(ExecutionContext Context, Action<object> Callback, object? State)>)state;
        try
        {
            ExecutionContext.Run(wrapper.Item.Context, static s =>
            {
                var call = ((StateTuple<(ExecutionContext, Action<object>, object?)>)s!).Item;
                call.Item2(call.Item3!);
            }, wrapper);
        }
        finally { wrapper.Dispose(); }
    }

    internal static bool TrySetResult(ulong handle) => Complete(handle, LuminTaskStatus.Succeeded, null);

    internal static bool TrySetException(ulong handle, Exception error)
    {
        if (error == null) throw new ArgumentNullException(nameof(error));
        return error is OperationCanceledException canceled
            ? TrySetCanceled(handle, canceled.CancellationToken)
            : Complete(handle, LuminTaskStatus.Faulted, ExceptionDispatchInfo.Capture(error));
    }

    internal static bool TrySetCanceled(ulong handle, CancellationToken token = default) =>
        Complete(handle, LuminTaskStatus.Canceled, token);

    private static bool Complete(ulong handle, LuminTaskStatus status, object? outcome)
    {
        var page = Enter(handle, out int i, out long control, pendingOnly: true);
        if (page == null) return false;
        ref var slot = ref page.Slots[i];
        var discarded = (control & KindMask) == Cell ? (IPooledResult?)slot.Payload : null;
        slot.Payload = outcome;
        Publish(ref slot, control & ~KindMask, status, discarded);
        return true;
    }

    internal static void Publish(ref Slot slot, long control, LuminTaskStatus status, IPooledResult? discarded = null)
    {
        var continuation = slot.Continuation;
        var state = slot.ContinuationState;
        var producer = slot.ProducerState;
        slot.Continuation = null;
        slot.ContinuationState = null;
        slot.ProducerState = null;
        Volatile.Write(ref slot.Control, control | (long)status);
        // A consumer may already have recycled the slot.
        discarded?.ReturnToPool();
        if (producer is StateMachineBox runner) runner.Exit();
        else if (producer is CancellationCallbackState binding) binding.Registration.Release();
        if (continuation != null) Invoke(continuation, state);
    }

    // A claimed terminal slot remains in its page until this consumer returns it.
    internal static SlotStore.Page ClaimResult(ulong handle, out int i, out long control)
    {
        var page = Enter(handle, out i, out control);
        if (page == null) LuminTaskExceptionHelper.ThrowStaleHandle();
        ref var slot = ref page.Slots[i];
        long status = control & StatusMask;
        if (status == 0)
        {
            Volatile.Write(ref slot.Control, control);
            LuminTaskExceptionHelper.ThrowNotCompleted();
        }
        if (status != (long)LuminTaskStatus.Succeeded)
        {
            var error = slot.Payload;
            ReleaseResult(page, i, handle);
            if (status == (long)LuminTaskStatus.Faulted) ((ExceptionDispatchInfo)error!).Throw();
            throw new OperationCanceledException((CancellationToken)error!);
        }
        return page;
    }

    internal static void ReleaseResult(SlotStore.Page page, int i, ulong handle)
    {
        ref var slot = ref page.Slots[i];
        slot.Payload = null;
        Volatile.Write(ref slot.Control, 0);
        SlotStore.Return(page, i, handle);
    }

    internal static void ConsumeVoid(ulong handle)
    {
        var page = ClaimResult(handle, out int i, out long control);
        var cell = (control & KindMask) == Cell ? (IPooledResult?)page.Slots[i].Payload : null;
        ReleaseResult(page, i, handle);
        cell?.ReturnToPool();
    }

    // Called by combinators only from a terminal child's continuation.
    internal static void Forget(ulong handle)
    {
        try { ConsumeVoid(handle); } catch (Exception) { }
    }

    internal static void AttachRegistration(ulong handle, CancellationCallbackState binding)
    {
        var page = Enter(handle, out int i, out long control, pendingOnly: true);
        if (page == null) { binding.Registration.Release(); return; }
        page.Slots[i].ProducerState = binding;
        Volatile.Write(ref page.Slots[i].Control, control);
    }

    internal static void AbandonUnpublished(ulong handle)
    {
        TrySetResult(handle);
        Forget(handle);
    }
}
