using System;
using System.Runtime.CompilerServices;
using System.Threading;
using LuminThread.TaskSource;
using LuminThread.Utility;

namespace LuminThread
{
    // Producer-side handle for a LuminTask whose completion is driven manually, the LuminTask
    // analogue of TaskCompletionSource. The backing slot lives in LuminTaskBag and is released
    // when the single consumer awaits Task (GetResult), so the usual contract applies:
    //
    //   * Complete the source at most once (TrySet* returns false on a second, still-pending call).
    //   * Task must be awaited (or Forget()-ed) exactly once.
    //   * Do NOT call TrySet* after the task may already have been consumed: the handle carries a
    //     slot generation, so a late completion is rejected instead of touching a recycled slot.
    //     In practice, complete the source before/at the single await and then stop using it.
    //
    // A source that is created but never completed-and-consumed leaks its slot; the DEBUG leak
    // tracker (LuminTaskLeakTracker) reports it with the creation stack.
    public sealed class LuminTaskCompletionSource<T>
    {
        private readonly LuminTaskHandle _handle;

        public LuminTaskCompletionSource()
        {
            _handle = LuminTaskResultBridge.Rent<T>();
        }

        public LuminTask<T> Task
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new LuminTask<T>(_handle);
        }

        // The raw handle is what an extension assembly (see the Unity integrations) needs in order
        // to drive the source from a player-loop callback: the loop item carries the handle, the
        // callback completes it through this instance.
        public LuminTaskHandle Handle
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _handle;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TrySetResult(T result)
            => LuminTaskResultBridge.TrySetResult(_handle, in result);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TrySetException(Exception exception)
            => LuminTaskSource.TrySetException(_handle, exception);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TrySetCanceled(CancellationToken cancellationToken = default)
            => LuminTaskSource.TrySetCanceled(_handle, cancellationToken);
    }

    public sealed class LuminTaskCompletionSource
    {
        private readonly LuminTaskHandle _handle;

        public LuminTaskCompletionSource()
        {
            _handle = LuminTask.RentSlot();
        }

        public LuminTask Task
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new LuminTask(_handle);
        }

        // See LuminTaskCompletionSource<T>.Handle.
        public LuminTaskHandle Handle
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _handle;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TrySetResult()
            => LuminTaskSource.TrySetResult(_handle);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TrySetException(Exception exception)
            => LuminTaskSource.TrySetException(_handle, exception);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TrySetCanceled(CancellationToken cancellationToken = default)
            => LuminTaskSource.TrySetCanceled(_handle, cancellationToken);
    }
}