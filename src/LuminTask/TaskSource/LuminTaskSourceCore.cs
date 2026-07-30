using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Threading;
using LuminThread.Interface;
using LuminThread.Utility;

namespace LuminThread.TaskSource;

public unsafe struct LuminTaskSourceCore<T>
{
    public static readonly VTable MethodTable = new VTable
    {
        GetResult = &GetResultAndDispose,
        GetResultValue = (delegate*<void*, short, T>)&GetResultValueAndDispose,
        GetStatus = &GetStatus,
        UnsafeGetStatus = &UnsafeGetStatus,
        OnCompleted = &OnCompleted,
        TrySetResult = &TrySetResult,
        TrySetCanceled = &TrySetCanceled,
        TrySetException = &TrySetException,
    };
    public static readonly VTable* MethodTablePtr = AllocMethodTablePtr();
    private static VTable* AllocMethodTablePtr()
    {
#if NET5_0_OR_GREATER
        var p = (VTable*)System.Runtime.InteropServices.NativeMemory.Alloc((nuint)System.Runtime.CompilerServices.Unsafe.SizeOf<VTable>());
#else
        var p = (VTable*)System.Runtime.InteropServices.Marshal.AllocHGlobal(System.Runtime.CompilerServices.Unsafe.SizeOf<VTable>()).ToPointer();
#endif
        *p = MethodTable;
        return p;
    }

    // Kept as aliases for source compatibility. All core-backed tasks now follow the same
    // consume-then-release lifecycle, so every closed T needs only one native vtable.
    public static readonly VTable MethodTableAutoDispose = MethodTable;
    public static readonly VTable* MethodTableAutoDisposePtr = MethodTablePtr;

    // Parked in LuminTaskItem.Continuation once the source completes, so the producer
    // (ExecuteContinuation) and the consumer (OnCompleted) can rendezvous with a single
    // interlocked operation instead of a racy two-field (Status + Continuation) handshake.
    private static readonly Action<object> s_completedSentinel = SentinelInvoked;
    private static void SentinelInvoked(object _)
        => LuminTaskExceptionHelper.ThrowInvalidOperation("LuminTask completion sentinel must never be invoked.");

    private static readonly ContextCallback s_executionContextCallback = RunWithExecutionContext;
    private static IntPtr s_cachedSource;

    public short Id;
    private static readonly bool ShouldClearResult = TypeMeta<T>.Size > 64;

    public LuminTaskSourceCore(short id)
    {
        Id = id;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static LuminTaskSourceCore<T>* Create(bool continueOnCapturedContext = false)
    {
        var cached = Interlocked.Exchange(ref s_cachedSource, IntPtr.Zero);
        var ptr = cached != IntPtr.Zero
            ? (LuminTaskSourceCore<T>*)cached.ToPointer()
            : (LuminTaskSourceCore<T>*)MemoryHelper.Alloc(
                (nuint)Math.Max(Unsafe.SizeOf<LuminTaskSourceCore<T>>(), IntPtr.Size));

        ptr->Id = LuminTaskBag.GetId();
        ref var item = ref LuminTaskMarshal.GetTaskItem(ptr->Id);

        item.Reset();

        item.ContinueOnCapturedContext = continueOnCapturedContext;

        LuminTaskLeakTracker.OnRent(ptr); // DEBUG-only; compiled out in Release

        return ptr;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static LuminTaskSourceCore<T> CreateSafe(bool continueOnCapturedContext = false)
    {
        LuminTaskSourceCore<T> core = new LuminTaskSourceCore<T>();

        core.Id = LuminTaskBag.GetId();
        ref var item = ref LuminTaskMarshal.GetTaskItem(core.Id);

        item.Reset();

        item.ContinueOnCapturedContext = continueOnCapturedContext;

        return core;
    }

    [DebuggerHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static LuminTaskStatus UnsafeGetStatus(void* ptr) =>
        ReadStatus(ref LuminTaskMarshal.GetTaskItem(Unsafe.AsRef<LuminTaskSourceCore<T>>(ptr).Id));

    [DebuggerHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static LuminTaskStatus GetStatus(void* ptr, short token)
    {
        ref var source = ref Unsafe.AsRef<LuminTaskSourceCore<T>>(ptr);

        if (token != source.Id) return LuminTaskStatus.Faulted;

        return ReadStatus(ref LuminTaskMarshal.GetTaskItem(source.Id));
    }

    [DebuggerHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void GetResult(void* ptr, short token)
    {
        ref var source = ref Unsafe.AsRef<LuminTaskSourceCore<T>>(ptr);

        if (token != source.Id) LuminTaskExceptionHelper.ThrowTokenMismatch();

        ref var item = ref LuminTaskMarshal.GetTaskItem(token);

        switch (ReadStatus(ref item))
        {
            case LuminTaskStatus.Succeeded:
                break;
            case LuminTaskStatus.Faulted:
                item.Exception?.Throw();
                item.Exception = null;
                LuminTaskExceptionHelper.ThrowInvalidOperation("Task faulted");
                break;
            case LuminTaskStatus.Canceled:
                throw new OperationCanceledException(item.CancellationToken);
            case LuminTaskStatus.Pending:
            default:
                LuminTaskExceptionHelper.ThrowInvalidOperation("Task not completed");
                break;
        }
    }

    [DebuggerHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void GetResultAndDispose(void* ptr, short token)
    {
        ref var source = ref Unsafe.AsRef<LuminTaskSourceCore<T>>(ptr);

        if (token != source.Id) LuminTaskExceptionHelper.ThrowTokenMismatch();

        ref var item = ref LuminTaskMarshal.GetTaskItem(token);

        switch (ReadStatus(ref item))
        {
            case LuminTaskStatus.Succeeded:
                Release(ptr);
                break;
            case LuminTaskStatus.Faulted:
            {
                var edi = item.Exception;
                item.Exception = null;
                Release(ptr);
                edi?.Throw();
                LuminTaskExceptionHelper.ThrowInvalidOperation("Task faulted");
                break;
            }
            case LuminTaskStatus.Canceled:
            {
                var cancellationToken = item.CancellationToken;
                Release(ptr);
                throw new OperationCanceledException(cancellationToken);
            }
            case LuminTaskStatus.Pending:
            default:
                LuminTaskExceptionHelper.ThrowInvalidOperation("Task not completed");
                break;
        }
    }

    [DebuggerHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T GetResultValue(void* ptr, short token)
    {
        ref var source = ref Unsafe.AsRef<LuminTaskSourceCore<T>>(ptr);

        if (token != source.Id) LuminTaskExceptionHelper.ThrowTokenMismatch();

        ref var item = ref LuminTaskMarshal.GetTaskItem(token);

        switch (ReadStatus(ref item))
        {
            case LuminTaskStatus.Succeeded:
                if (TypeMeta<T>.IsValueType)
                {
#if NET8_0_OR_GREATER
                    if (LuminTask.Model is LuminTaskModel.Unsafe || !TypeMeta<T>.IsReferenceOrContainsReferences)
                    {
                        if (TypeMeta<T>.Size > 64)
                        {
                            var resultPtr = (void*)Unsafe.ReadUnaligned<nint>(ref item.ResultValue[0]);
                            var result = Unsafe.ReadUnaligned<T>(resultPtr);

                            return result;
                        }

                        return Unsafe.ReadUnaligned<T>(ref item.ResultValue[0]);
                    }
                    else
                    {
                        if (item.ResultRef is StateTuple<T> stateTuple)
                        {
                            var result = stateTuple.Item1;
                            return result;
                        }
                        return default!;
                    }
#else
                    if (TypeMeta<T>.Size > 64)
                    {
                        var resultPtr = (void*)Unsafe.ReadUnaligned<nint>(ref item.ResultValue[0]);
                        var result = Unsafe.ReadUnaligned<T>(resultPtr);

                        return result;
                    }
                    else
                    {
                        return Unsafe.ReadUnaligned<T>(ref item.ResultValue[0]);
                    }
#endif
                }
                return (T)item.ResultRef!;
            case LuminTaskStatus.Faulted:
                item.Exception?.Throw();
                item.Exception = null;
                LuminTaskExceptionHelper.ThrowInvalidOperation("Task faulted");
                break;
            case LuminTaskStatus.Canceled:
                throw new OperationCanceledException(item.CancellationToken);
            case LuminTaskStatus.Pending:
            default:
                LuminTaskExceptionHelper.ThrowInvalidOperation("Task not completed");
                break;
        }

        throw new InvalidOperationException("Unreachable code");
    }

    [DebuggerHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T GetResultValueAndDispose(void* ptr, short token)
    {
        ref var source = ref Unsafe.AsRef<LuminTaskSourceCore<T>>(ptr);

        if (token != source.Id) LuminTaskExceptionHelper.ThrowTokenMismatch();

        ref var item = ref LuminTaskMarshal.GetTaskItem(token);

        switch (ReadStatus(ref item))
        {
            case LuminTaskStatus.Succeeded:
            {
                T result;
                if (TypeMeta<T>.IsValueType)
                {
#if NET8_0_OR_GREATER
                    if (LuminTask.Model is LuminTaskModel.Unsafe || !TypeMeta<T>.IsReferenceOrContainsReferences)
                    {
                        if (TypeMeta<T>.Size > 64)
                        {
                            var resultPtr = (void*)Unsafe.ReadUnaligned<nint>(ref item.ResultValue[0]);
                            result = Unsafe.ReadUnaligned<T>(resultPtr);
                        }
                        else
                        {
                            result = Unsafe.ReadUnaligned<T>(ref item.ResultValue[0]);
                        }
                    }
                    else
                    {
                        result = item.ResultRef is StateTuple<T> stateTuple ? stateTuple.Item1 : default!;
                    }
#else
                    if (TypeMeta<T>.Size > 64)
                    {
                        var resultPtr = (void*)Unsafe.ReadUnaligned<nint>(ref item.ResultValue[0]);
                        result = Unsafe.ReadUnaligned<T>(resultPtr);
                    }
                    else
                    {
                        result = Unsafe.ReadUnaligned<T>(ref item.ResultValue[0]);
                    }
#endif
                }
                else
                {
                    result = (T)item.ResultRef!;
                }

                // Result has been copied out; safe to release the source now.
                Release(ptr);
                return result;
            }
            case LuminTaskStatus.Faulted:
            {
                var edi = item.Exception;
                item.Exception = null;
                Release(ptr);
                edi?.Throw();
                LuminTaskExceptionHelper.ThrowInvalidOperation("Task faulted");
                break;
            }
            case LuminTaskStatus.Canceled:
            {
                var cancellationToken = item.CancellationToken;
                Release(ptr);
                throw new OperationCanceledException(cancellationToken);
            }
            case LuminTaskStatus.Pending:
            default:
                LuminTaskExceptionHelper.ThrowInvalidOperation("Task not completed");
                break;
        }

        throw new InvalidOperationException("Unreachable code");
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void OnCompleted(void* ptr, Action<object> continuation, object state, short token)
    {
        ref var source = ref Unsafe.AsRef<LuminTaskSourceCore<T>>(ptr);

        if (token != source.Id) LuminTaskExceptionHelper.ThrowTokenMismatch();

        ref var item = ref LuminTaskMarshal.GetTaskItem(token);

        var capturedContext = item.ContinueOnCapturedContext ? ExecutionContext.Capture() : null;

        // Publish state + captured context BEFORE the continuation pointer becomes visible,
        // so the producer is guaranteed to observe them once it sees the continuation.
        item.State = state;
        item.CapturedContext = capturedContext;

        // Single full-fence rendezvous on the continuation slot.
        var previous = Interlocked.CompareExchange(ref item.Continuation, continuation, null);

        if (previous is null)
        {
            // Registered before completion; the producer will invoke it.
            return;
        }

        if (ReferenceEquals(previous, s_completedSentinel))
        {
            // Producer already completed; run inline now using the local state/context.
            item.State = null;
            FireContinuation(capturedContext, continuation, state);
            return;
        }

        // The slot already held another continuation: the task is being awaited twice.
        LuminTaskExceptionHelper.ThrowInvalidOperation(
            "LuminTask may only be awaited once; a continuation is already registered.");
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void FireContinuation(ExecutionContext? capturedContext, Action<object> continuation, object state)
    {
        if (capturedContext is null)
        {
            continuation(state);
            return;
        }

        var box = StateTuple.Create(continuation, state);
        ExecutionContext.Run(capturedContext, s_executionContextCallback, box);
    }

    private static void RunWithExecutionContext(object? boxed)
    {
        var box = (StateTuple<Action<object>, object>)boxed!;
        var continuation = box.Item1;
        var state = box.Item2;
        box.Dispose();
        continuation(state);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TrySetResult(void* ptr)
    {
        ref var source = ref Unsafe.AsRef<LuminTaskSourceCore<T>>(ptr);
        ref var item = ref LuminTaskMarshal.GetTaskItem(source.Id);

        if (!TryBeginCompletion(ref item)) return false;
        PublishStatus(ref item, LuminTaskStatus.Succeeded);

        ExecuteContinuation(ref item);

        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TrySetResult(void* ptr, T result)
    {
        ref var source = ref Unsafe.AsRef<LuminTaskSourceCore<T>>(ptr);
        ref var item = ref LuminTaskMarshal.GetTaskItem(source.Id);

        if (!TryBeginCompletion(ref item)) return false;

        if (TypeMeta<T>.IsValueType)
        {
#if NET8_0_OR_GREATER
            if (LuminTask.Model is LuminTaskModel.Unsafe || !TypeMeta<T>.IsReferenceOrContainsReferences)
            {
                if (TypeMeta<T>.Size > 64)
                {
                    var resultPtr = MemoryHelper.Alloc(TypeMeta<T>.Size);
                    Unsafe.WriteUnaligned(resultPtr, result);
                    Unsafe.WriteUnaligned(ref item.ResultValue[0], (nint)resultPtr);
                }
                else
                {
                    Unsafe.WriteUnaligned(ref item.ResultValue[0], result);
                }
            }
            else
            {
                var stateTuple = StateTuple.Create(result);
                item.ResultRef = stateTuple;
            }
#else
            if (TypeMeta<T>.Size > 64)
            {
                var resultPtr = MemoryHelper.Alloc(TypeMeta<T>.Size);
                Unsafe.WriteUnaligned(resultPtr, result);
                Unsafe.WriteUnaligned(ref item.ResultValue[0], (nint)resultPtr);
            }
            else
            {
                Unsafe.WriteUnaligned(ref item.ResultValue[0], result);
            }
#endif
        }
        else
        {
            item.ResultRef = result;
        }

        PublishStatus(ref item, LuminTaskStatus.Succeeded);

        ExecuteContinuation(ref item);

        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TrySetException(void* ptr, Exception exception)
    {
        if (exception is null) throw new ArgumentNullException(nameof(exception));
        if (exception is OperationCanceledException canceled)
            return TrySetCanceled(ptr, canceled.CancellationToken);

        ref var source = ref Unsafe.AsRef<LuminTaskSourceCore<T>>(ptr);
        ref var item = ref LuminTaskMarshal.GetTaskItem(source.Id);

        if (!TryBeginCompletion(ref item)) return false;
        item.Exception = ExceptionDispatchInfo.Capture(exception);
        PublishStatus(ref item, LuminTaskStatus.Faulted);

        ExecuteContinuation(ref item);

        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TrySetCanceled(void* ptr)
        => TrySetCanceled(ptr, default);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TrySetCanceled(void* ptr, CancellationToken cancellationToken)
    {
        ref var source = ref Unsafe.AsRef<LuminTaskSourceCore<T>>(ptr);
        ref var item = ref LuminTaskMarshal.GetTaskItem(source.Id);

        if (!TryBeginCompletion(ref item)) return false;
        item.CancellationToken = cancellationToken;
        PublishStatus(ref item, LuminTaskStatus.Canceled);

        ExecuteContinuation(ref item);

        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool TryBeginCompletion(ref LuminTaskItem item)
        => Interlocked.CompareExchange(ref item.CompletionState, 1, 0) == 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static LuminTaskStatus ReadStatus(ref LuminTaskItem item)
        => (LuminTaskStatus)Volatile.Read(ref Unsafe.As<LuminTaskStatus, byte>(ref item.Status));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void PublishStatus(ref LuminTaskItem item, LuminTaskStatus status)
        => Volatile.Write(ref Unsafe.As<LuminTaskStatus, byte>(ref item.Status), (byte)status);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ExecuteContinuation(ref LuminTaskItem item)
    {
        // Mark completed and atomically take any registered continuation (full fence).
        // The producer status write above happens-before this Exchange, so a consumer that
        // later observes the sentinel via its CompareExchange also observes the result/status.
        var continuation = Interlocked.Exchange(ref item.Continuation, s_completedSentinel);

        if (continuation is null || ReferenceEquals(continuation, s_completedSentinel))
        {
            // No awaiter yet (OnCompleted will run it inline when it sees the sentinel),
            // or completion already happened — nothing to do here.
            return;
        }

        var capturedContext = item.CapturedContext;
        var state = item.State;
        item.State = null;
        FireContinuation(capturedContext, continuation, state!);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Dispose(void* ptr)
    {
        // Legacy producer-side call retained for source compatibility. A producer cannot
        // safely release a single-consumer source: TrySet* may invoke the consumer inline,
        // and that consumer can release the native pointer before control returns here.
        // Actual release is owned exclusively by GetResultAndDispose.
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void Release(void* ptr)
    {
        ref var source = ref Unsafe.AsRef<LuminTaskSourceCore<T>>(ptr);
        ref var item = ref LuminTaskMarshal.GetTaskItem(source.Id);

#if NET8_0_OR_GREATER
        if (LuminTask.Model != LuminTaskModel.Unsafe &&
            TypeMeta<T>.IsValueType && TypeMeta<T>.IsReferenceOrContainsReferences &&
            item.ResultRef is StateTuple<T> stateTuple)
        {
            item.ResultRef = null;
            stateTuple.Dispose();
        }
#endif

        if (ShouldClearResult)
        {
            var resultPtr = (void*)Unsafe.ReadUnaligned<nint>(ref item.ResultValue[0]);
            if (resultPtr != null)
            {
                MemoryHelper.Free(resultPtr);
                Unsafe.WriteUnaligned(ref item.ResultValue[0], (nint)0);
            }
        }

        // Drop managed roots at consumption time instead of retaining them until this slot is
        // rented again. This mirrors DotNext's AfterConsumed cleanup and materially lowers
        // retention when the task pool is mostly idle after a burst.
        item.ResultRef = null;
        item.ResultRefDispose = null;
        item.Exception = null;
        item.Error = null;
        item.State = null;
        item.CapturedContext = null;
        item.Continuation = null;
        item.CancellationToken = default;

        LuminTaskBag.ResetId(item.Id);

        LuminTaskLeakTracker.OnReturn(ptr); // DEBUG-only; compiled out in Release

        // A single lock-free native slot eliminates alloc/free traffic for the overwhelmingly
        // common sequential case without allowing an unbounded per-T cache. Contended excess
        // sources still return directly to the configured allocator.
        if (Interlocked.CompareExchange(ref s_cachedSource, new IntPtr(ptr), IntPtr.Zero) != IntPtr.Zero)
            MemoryHelper.Free(ptr);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void DisposeSafe(LuminTaskSourceCore<T> source)
    {
        ref var item = ref LuminTaskMarshal.GetTaskItem(source.Id);

#if NET8_0_OR_GREATER
        if (LuminTask.Model != LuminTaskModel.Unsafe &&
            TypeMeta<T>.IsValueType && TypeMeta<T>.IsReferenceOrContainsReferences &&
            item.ResultRef is StateTuple<T> stateTuple)
        {
            item.ResultRef = null;
            stateTuple.Dispose();
        }
#endif

        if (ShouldClearResult)
        {
            var resultPtr = (void*)Unsafe.ReadUnaligned<nint>(ref item.ResultValue[0]);
            if (resultPtr != null)
            {
                MemoryHelper.Free(resultPtr);
                Unsafe.WriteUnaligned(ref item.ResultValue[0], (nint)0);
            }
        }

        item.ResultRef = null;
        item.ResultRefDispose = null;
        item.Exception = null;
        item.Error = null;
        item.State = null;
        item.CapturedContext = null;
        item.Continuation = null;
        item.CancellationToken = default;

        LuminTaskBag.ResetId(item.Id);
    }

}
