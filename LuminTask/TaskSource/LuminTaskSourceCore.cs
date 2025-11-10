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
    static readonly bool IsContainsReference = RuntimeHelpers.IsReferenceOrContainsReferences<T>();

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

    public short Id;

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
        
        item.ContinueOnCapturedContext = continueOnCapturedContext;
        
        return ptr;
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
        
        ref var item = ref LuminTaskMarshal.GetTaskItem(source.Id);

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
        
        ref var item = ref LuminTaskMarshal.GetTaskItem(source.Id);

        switch (item.Status)
        {
            case LuminTaskStatus.Succeeded:
                return IsContainsReference 
                    ? (T)item.ResultRef! 
                    : Unsafe.ReadUnaligned<T>(Unsafe.AsPointer(ref item.ResultValue[0]));
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
        
        ref var item = ref LuminTaskMarshal.GetTaskItem(source.Id);

        if (item.Status != LuminTaskStatus.Pending)
        {
            ExecuteContinuation(ref item);
            return;
        }

        item.Continuation = continuation;
        item.State = state;

        if (item.ContinueOnCapturedContext)
        {
            item.CapturedContext = ExecutionContext.Capture();
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
        if (IsContainsReference)
        {
            item.ResultRef = result;
        }
        else
        {
            Unsafe.WriteUnaligned(ref item.ResultValue[0], result);
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
        if (item.Status != LuminTaskStatus.Pending)
        {
            if (item.Continuation != null)
            {
                if (item.CapturedContext != null)
                {
                    ExecutionContext.Run(item.CapturedContext, static s =>
                    {
                        var (cont, st) = (Tuple<Action<object>, object?>)s!;
                        cont(st!);
                    }, Tuple.Create(item.Continuation, item.State));
                }
                else
                {
                    item.Continuation(item.State!);
                }
                item.Continuation = null;
                item.State = null;
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Dispose(void* ptr)
    {
        
        ref var source = ref Unsafe.AsRef<LuminTaskSourceCore<T>>(ptr);
        ref var item = ref LuminTaskMarshal.GetTaskItem(source.Id);
        
        LuminTaskBag.ResetId(item.Id);
        
        item.Reset();
        
        MemoryHelper.Free(ptr);
    }
    
}