using System;
using System.Runtime.CompilerServices;
using System.Threading;
using LuminThread.Interface;
using LuminThread.Utility;

namespace LuminThread.TaskSource.Promise;

public unsafe struct YieldPromise
{
    public static readonly VTable MethodTable = new VTable
    {
        GetResult    = &GetResult,
        GetStatus    = &GetStatus,
        UnsafeGetStatus = &UnsafeGetStatus,
        OnCompleted  = &OnCompleted,
        Dispose = &Dispose,
    };
    
    bool cancelImmediately;
    short Id;

    public static YieldPromise* Create(PlayerLoopTiming timing, CancellationToken cancellationToken, bool cancelImmediately, out short token)
    {
        YieldPromise* ptr = (YieldPromise*)MemoryHelper.Alloc((nuint)sizeof(YieldPromise));

        ptr->Id = LuminTaskBag.GetId();

        ref var item = ref LuminTaskMarshal.GetTaskItem(ptr->Id);
            
        if (cancellationToken.IsCancellationRequested)
        {
            item.Status = LuminTaskStatus.Canceled;
        }

        item.CancellationToken = cancellationToken;
        ptr->cancelImmediately = cancelImmediately;
                
        if (cancelImmediately && cancellationToken.CanBeCanceled)
        {
            cancellationToken.RegisterWithoutCaptureExecutionContext(static state =>
            {
                ref var promise = ref Unsafe.AsRef<YieldPromise>(((IntPtr)state).ToPointer());
                ref var item = ref LuminTaskMarshal.GetTaskItem(promise.Id);
                item.Error = new OperationCanceledException(item.CancellationToken);
        
                item.Continuation?.Invoke(item.State!);
            }, new IntPtr(ptr));
        }

        PlayerLoopHelper.AddAction(timing, new LuminTaskState(ptr), MoveNext);
            
        token = ptr->Id;
        
        return ptr;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void GetResult(void* ptr, short token)
    {
        ref var source = ref Unsafe.AsRef<YieldPromise>(ptr);
        ref var item = ref LuminTaskMarshal.GetTaskItem(source.Id);
        
        if (token != source.Id) LuminTaskExceptionHelper.ThrowTokenMismatch();
        
        try
        {
            if (item.Error != null)
            {
                if (item.Error is OperationCanceledException oce)
                {
                    throw oce;
                }
                    
                LuminTaskExceptionHelper.ThrowInvalidError();
            }
        }
        finally
        {
            if (!(source.cancelImmediately && item.CancellationToken.IsCancellationRequested))
            {
                Dispose(ptr);
            }
        }
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static LuminTaskStatus GetStatus(void* ptr, short token)
    {
        ref var source = ref Unsafe.AsRef<YieldPromise>(ptr);
        
        if (token != source.Id) return LuminTaskStatus.Faulted;
        
        return LuminTaskMarshal.GetTaskItem(source.Id).Status;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static LuminTaskStatus UnsafeGetStatus(void* ptr) => 
        LuminTaskMarshal.GetTaskItem(Unsafe.AsRef<YieldPromise>(ptr).Id).Status;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void OnCompleted(void* ptr, Action<object> continuation, object state, short token)
    {
        ref var source = ref Unsafe.AsRef<YieldPromise>(ptr);
        ref var item = ref LuminTaskMarshal.GetTaskItem(source.Id);

        if (token != source.Id) LuminTaskExceptionHelper.ThrowTokenMismatch();
        
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
    public static bool MoveNext(in LuminTaskState state)
    {
        ref var source = ref Unsafe.AsRef<YieldPromise>(state.Source);
        ref var item = ref LuminTaskMarshal.GetTaskItem(source.Id);
        
        if (item.CancellationToken.IsCancellationRequested)
        {
            item.Error = new OperationCanceledException(item.CancellationToken);
        }
        
        item.Continuation?.Invoke(item.State!);
        
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static void Dispose(void* ptr)
    {
        ref var source = ref Unsafe.AsRef<YieldPromise>(ptr);
        ref var item = ref LuminTaskMarshal.GetTaskItem(source.Id);
            
        LuminTaskBag.ResetId(item.Id);
            
        item.Reset();
        MemoryHelper.Free(ptr);
    }
}