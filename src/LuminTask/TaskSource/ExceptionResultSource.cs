using LuminThread.Interface;
using LuminThread.Utility;
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;

namespace LuminThread.TaskSource;

public unsafe struct ExceptionResultSource<T>
{
    public static readonly VTable MethodTable = new VTable
    {
        GetResult = &GetResult,
        GetResultValue = (delegate*<void*, short, T>)&GetResultValue,
        GetStatus = &GetStatus,
        UnsafeGetStatus = &UnsafeGetStatus,
        OnCompleted = &OnCompleted,
    };
    public static readonly unsafe VTable* MethodTablePtr = AllocMethodTablePtr();
    private static unsafe VTable* AllocMethodTablePtr()
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
    private bool _calledGet;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ExceptionResultSource(short id, Exception exception)
    {
        Id = id;
        LuminTaskMarshal.GetTaskItem(id).Exception = ExceptionDispatchInfo.Capture(exception);
        _calledGet = false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ExceptionResultSource<T>* Create(Exception exception)
    {
        ExceptionResultSource<T>* ptr = (ExceptionResultSource<T>*)MemoryHelper.Alloc((nuint)Unsafe.SizeOf<LuminTaskSourceCore<T>>());

        ptr->Id = LuminTaskBag.GetId();

        ref var item = ref LuminTaskMarshal.GetTaskItem(ptr->Id);
        
        item.Reset();
        
        item.Exception = ExceptionDispatchInfo.Capture(exception);

        return ptr;
    }

    [DebuggerHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void GetResult(void* ptr, short token)
    {
        ref var source = ref Unsafe.AsRef<ExceptionResultSource<T>>(ptr);

        if (token != source.Id) LuminTaskExceptionHelper.ThrowTokenMismatch();

        ref var item = ref LuminTaskMarshal.GetTaskItem(source.Id);

        if (!source._calledGet)
        {
            source._calledGet = true;
        }
        
        var ex = item.Exception;
        Dispose(ptr);
        ex?.Throw();
    }

    [DebuggerHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T GetResultValue(void* ptr, short token)
    {
        ref var source = ref Unsafe.AsRef<ExceptionResultSource<T>>(ptr);

        if (token != source.Id) LuminTaskExceptionHelper.ThrowTokenMismatch();

        ref var item = ref LuminTaskMarshal.GetTaskItem(source.Id);

        if (!source._calledGet)
        {
            source._calledGet = true;
        }

        var ex = item.Exception;
        Dispose(ptr);
        ex?.Throw();
        return default;
    }

    [DebuggerHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static LuminTaskStatus GetStatus(void* ptr, short token)
    {
        return LuminTaskStatus.Faulted;
    }

    [DebuggerHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static LuminTaskStatus UnsafeGetStatus(void* ptr)
    {
        return LuminTaskStatus.Faulted;
    }

    [DebuggerHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void OnCompleted(void* ptr, Action<object> continuation, object state, short token)
    {
        continuation(state);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Dispose(void* ptr)
    {
        ref var source = ref Unsafe.AsRef<ExceptionResultSource<T>>(ptr);
        ref var item = ref LuminTaskMarshal.GetTaskItem(source.Id);

        LuminTaskBag.ResetId(item.Id);

        if (!source._calledGet)
        {
            LuminTaskScheduler.PublishUnobservedTaskException(item.Exception?.SourceException);
        }

        MemoryHelper.Free(ptr);
    }
}
