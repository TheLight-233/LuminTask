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
        GetResult = &GetResult,
        GetResultValue = (delegate*<void*, short, T>)&GetResultValue,
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

    public short Id;
    public static bool ShouldClearResult = TypeMeta<T>.Size > 64;

    public LuminTaskSourceCore(short id)
    {
        Id = id;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static LuminTaskSourceCore<T>* Create(bool continueOnCapturedContext = true)
    {
        LuminTaskSourceCore<T>* ptr = (LuminTaskSourceCore<T>*)MemoryHelper.Alloc((nuint)Unsafe.SizeOf<LuminTaskSourceCore<T>>());

        ptr->Id = LuminTaskBag.GetId();
        ref var item = ref LuminTaskMarshal.GetTaskItem(ptr->Id);

        item.Reset();

        item.ContinueOnCapturedContext = continueOnCapturedContext;

        return ptr;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static LuminTaskSourceCore<T> CreateSafe(bool continueOnCapturedContext = true)
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
        LuminTaskMarshal.GetTaskItem(Unsafe.AsRef<LuminTaskSourceCore<T>>(ptr).Id).Status;

    [DebuggerHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static LuminTaskStatus GetStatus(void* ptr, short token)
    {
        ref var source = ref Unsafe.AsRef<LuminTaskSourceCore<T>>(ptr);

        if (token != source.Id) return LuminTaskStatus.Faulted;

        return LuminTaskMarshal.GetTaskItem(source.Id).Status;
    }

    [DebuggerHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void GetResult(void* ptr, short token)
    {
        ref var source = ref Unsafe.AsRef<LuminTaskSourceCore<T>>(ptr);

        if (token != source.Id) LuminTaskExceptionHelper.ThrowTokenMismatch();

        ref var item = ref LuminTaskMarshal.GetTaskItem(token);

        switch (item.Status)
        {
            case LuminTaskStatus.Succeeded:
                break;
            case LuminTaskStatus.Faulted:
                item.Exception?.Throw();
                item.Exception = null;
                LuminTaskExceptionHelper.ThrowInvalidOperation("Task faulted");
                break;
            case LuminTaskStatus.Canceled:
                LuminTaskExceptionHelper.ThrowOperationCancel("Task canceled");
                break;
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

        switch (item.Status)
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
                LuminTaskExceptionHelper.ThrowOperationCancel("Task canceled");
                break;
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

        if (item.ContinueOnCapturedContext)
            item.CapturedContext = ExecutionContext.Capture();

        if (item.Status != LuminTaskStatus.Pending)
        {
            FireContinuation(ref item, continuation, state);
            return;
        }

        item.State = state;
        Volatile.Write(ref item.Continuation, continuation);

        if (item.Status != LuminTaskStatus.Pending)
        {
            if (System.Threading.Interlocked.CompareExchange(ref item.Continuation, null, continuation) == continuation)
            {
                item.State = null;
                FireContinuation(ref item, continuation, state);
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void FireContinuation(ref LuminTaskItem item, Action<object> continuation, object state)
    {
        if (item.CapturedContext != null)
        {
            item.Continuation = continuation;
            item.State = state;
            ExecutionContext.Run(item.CapturedContext, static s =>
            {
                var id = (short)(int)s!;
                ref var it = ref LuminTaskBag.TaskBag[id];
                var cont = it.Continuation!;
                var st   = it.State;
                it.Continuation = null;
                it.State = null;
                cont(st!);
            }, LuminTaskBag.BoxedIds[item.Id]);
        }
        else
        {
            continuation(state);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TrySetResult(void* ptr)
    {
        ref var source = ref Unsafe.AsRef<LuminTaskSourceCore<T>>(ptr);
        ref var item = ref LuminTaskMarshal.GetTaskItem(source.Id);

        if (item.Status != LuminTaskStatus.Pending) return false;
        item.Status = LuminTaskStatus.Succeeded;

        ExecuteContinuation(ref item);

        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TrySetResult(void* ptr, T result)
    {
        ref var source = ref Unsafe.AsRef<LuminTaskSourceCore<T>>(ptr);
        ref var item = ref LuminTaskMarshal.GetTaskItem(source.Id);

        if (item.Status != LuminTaskStatus.Pending) return false;

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

        item.Status = LuminTaskStatus.Succeeded;

        ExecuteContinuation(ref item);

        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TrySetException(void* ptr, Exception exception)
    {
        ref var source = ref Unsafe.AsRef<LuminTaskSourceCore<T>>(ptr);
        ref var item = ref LuminTaskMarshal.GetTaskItem(source.Id);

        if (item.Status != LuminTaskStatus.Pending) return false;
        item.Status = LuminTaskStatus.Faulted;
        item.Exception = ExceptionDispatchInfo.Capture(exception);

        ExecuteContinuation(ref item);

        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TrySetCanceled(void* ptr)
    {
        ref var source = ref Unsafe.AsRef<LuminTaskSourceCore<T>>(ptr);
        ref var item = ref LuminTaskMarshal.GetTaskItem(source.Id);

        if (item.Status != LuminTaskStatus.Pending) return false;
        item.Status = LuminTaskStatus.Canceled;

        ExecuteContinuation(ref item);

        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ExecuteContinuation(ref LuminTaskItem item)
    {
        var cont = System.Threading.Volatile.Read(ref item.Continuation);
        if (cont == null) return;
        var st = item.State;
        item.State = null;
        FireContinuation(ref item, cont, st!);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Dispose(void* ptr)
    {

        ref var source = ref Unsafe.AsRef<LuminTaskSourceCore<T>>(ptr);
        ref var item = ref LuminTaskMarshal.GetTaskItem(source.Id);

#if NET8_0_OR_GREATER
        if (LuminTask.Model != LuminTaskModel.Unsafe &&
            TypeMeta<T>.IsValueType && TypeMeta<T>.IsReferenceOrContainsReferences &&
            item.ResultRef is StateTuple<T> stateTuple)
        {
            item.ResultRefDispose = stateTuple;
        }
#endif

        if (ShouldClearResult)
        {
            item.ShouldClearResult = true;
        }

        LuminTaskBag.ResetId(item.Id);

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
            item.ResultRefDispose = stateTuple;
        }
#endif

        if (ShouldClearResult)
        {
            item.ShouldClearResult = true;
        }

        LuminTaskBag.ResetId(item.Id);
    }

}
