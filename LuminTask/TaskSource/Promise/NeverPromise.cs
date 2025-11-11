using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using LuminThread.Interface;
using LuminThread.Utility;

namespace LuminThread.TaskSource.Promise;


public unsafe struct NeverPromise<T>
{
    public static readonly VTable MethodTable = new VTable
    {
        GetResult    = &GetResult,
        GetResultValue = (delegate*<void*, short, T>)&GetResultValue,
        GetStatus    = &GetStatus,
        UnsafeGetStatus = &UnsafeGetStatus,
        OnCompleted  = &OnCompleted,
        Dispose = &Dispose,
    };

    static readonly Action<object> _cancellationCallback = CancellationCallback;
    
    public short Id;
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public NeverPromise(short id) => Id = id;
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static NeverPromise<T>* Create(CancellationToken cancellationToken = default)
    {
        NeverPromise<T>* ptr = (NeverPromise<T>*)MemoryHelper.Alloc((nuint)sizeof(NeverPromise<T>));
        ptr->Id = 0;

        LuminTaskMarshal.GetTaskItem(ptr->Id).CancellationToken = cancellationToken;

        if (cancellationToken.CanBeCanceled)
        {
            cancellationToken.RegisterWithoutCaptureExecutionContext(_cancellationCallback, new IntPtr(ptr));
        }
        

        return ptr;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static void CancellationCallback(object state)
    {
        var ptr = ((IntPtr)state).ToPointer();
        ref var self = ref Unsafe.AsRef<NeverPromise<T>>(ptr);
        ref var item = ref LuminTaskMarshal.GetTaskItem(self.Id);
        item.Error = new OperationCanceledException(item.CancellationToken);
        
        item.Continuation?.Invoke(item.State!);
        
        Dispose(ptr);
    }
    
    
    [DebuggerHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void GetResult(void* ptr, short token)
    {
        ref var self = ref Unsafe.AsRef<NeverPromise<T>>(ptr);
        if (token != self.Id) LuminTaskExceptionHelper.ThrowTokenMismatch();

        ref var item = ref LuminTaskMarshal.GetTaskItem(self.Id);
        
        if (item.Error != null)
        {
            if (item.Error is OperationCanceledException oce)
            {
                throw oce;
            }
            LuminTaskExceptionHelper.ThrowInvalidError();
        }
    }

    [DebuggerHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T GetResultValue(void* ptr, short token)
    {
        GetResult(ptr, token);
        return default!;
    }

    [DebuggerHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static LuminTaskStatus GetStatus(void* ptr, short token)
    {
        ref var self = ref Unsafe.AsRef<NeverPromise<T>>(ptr);
        if (token != self.Id) LuminTaskExceptionHelper.ThrowTokenMismatch();

        ref var item = ref LuminTaskMarshal.GetTaskItem(self.Id);
        return (item.Continuation == null) ? LuminTaskStatus.Pending
            : (item.Error == null) ? LuminTaskStatus.Succeeded
            : (item.Error is OperationCanceledException) ? LuminTaskStatus.Canceled
            : LuminTaskStatus.Faulted;
    }

    [DebuggerHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static LuminTaskStatus UnsafeGetStatus(void* ptr)
    {
        ref var item = ref LuminTaskMarshal.GetTaskItem(Unsafe.AsRef<NeverPromise<T>>(ptr).Id);
        return (item.Continuation == null) ? LuminTaskStatus.Pending
            : (item.Error == null) ? LuminTaskStatus.Succeeded
            : (item.Error is OperationCanceledException) ? LuminTaskStatus.Canceled
            : LuminTaskStatus.Faulted;
    }

    [DebuggerHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void OnCompleted(void* ptr, Action<object> continuation, object state, short token)
    {
        ref var item = ref LuminTaskMarshal.GetTaskItem(Unsafe.AsRef<NeverPromise<T>>(ptr).Id);

        object? oldContinuation = item.Continuation;
        
        if (oldContinuation is null)
        {
            item.State = state;
            oldContinuation = Interlocked.CompareExchange(ref item.Continuation, continuation, null);
        }
        
        if (oldContinuation != null)
        {
            continuation(state);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Dispose(void* ptr)
    {
        ref var source = ref Unsafe.AsRef<ExceptionResultSource<T>>(ptr);
        ref var item = ref LuminTaskMarshal.GetTaskItem(source.Id);
        
        LuminTaskBag.ResetId(item.Id);
        
        item.Reset();
  
        MemoryHelper.Free(ptr);
    }
}