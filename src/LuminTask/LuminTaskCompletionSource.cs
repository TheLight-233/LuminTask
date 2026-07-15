using System;
using System.Runtime.CompilerServices;
using System.Threading;
using LuminThread.TaskSource;
using LuminThread.Utility;

namespace LuminThread
{
    // Producer-side handle for a LuminTask whose completion is driven manually, the LuminTask
    // analogue of TaskCompletionSource. The backing source lives in unmanaged memory and is
    // released when the single consumer awaits Task (GetResult), so the usual contract applies:
    //
    //   * Complete the source at most once (TrySet* returns false on a second, still-pending call).
    //   * Task must be awaited (or Forget()-ed) exactly once.
    //   * Do NOT call TrySet* after the task may already have been consumed: unlike
    //     TaskCompletionSource there is no GC keep-alive, so a late completion would touch freed
    //     memory. In practice, complete the source before/at the single await and then stop using it.
    //
    // A source that is created but never completed-and-consumed leaks its slot; the DEBUG leak
    // tracker (LuminTaskLeakTracker) reports it with the creation stack.
    public sealed unsafe class LuminTaskCompletionSource<T>
    {
        private readonly LuminTaskSourceCore<T>* _source;
        private readonly short _id;

        public LuminTaskCompletionSource()
        {
            _source = LuminTaskSourceCore<T>.Create(false);
            _id = _source->Id;
        }

        public LuminTask<T> Task
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new LuminTask<T>(LuminTaskSourceCore<T>.MethodTableAutoDisposePtr, _source, _id);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TrySetResult(T result)
            => LuminTaskSourceCore<T>.TrySetResult(_source, result);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TrySetException(Exception exception)
            => LuminTaskSourceCore<T>.TrySetException(_source, exception);

        // The token is accepted for API familiarity; the thrown OperationCanceledException does
        // not currently carry it (the core's cancel path is token-less).
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TrySetCanceled(CancellationToken cancellationToken = default)
            => LuminTaskSourceCore<T>.TrySetCanceled(_source);
    }

    public sealed unsafe class LuminTaskCompletionSource
    {
        private readonly LuminTaskSourceCore<AsyncUnit>* _source;
        private readonly short _id;

        public LuminTaskCompletionSource()
        {
            _source = LuminTaskSourceCore<AsyncUnit>.Create(false);
            _id = _source->Id;
        }

        public LuminTask Task
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new LuminTask(LuminTaskSourceCore<AsyncUnit>.MethodTableAutoDisposePtr, _source, _id);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TrySetResult()
            => LuminTaskSourceCore<AsyncUnit>.TrySetResult(_source);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TrySetException(Exception exception)
            => LuminTaskSourceCore<AsyncUnit>.TrySetException(_source, exception);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TrySetCanceled(CancellationToken cancellationToken = default)
            => LuminTaskSourceCore<AsyncUnit>.TrySetCanceled(_source);
    }
}
