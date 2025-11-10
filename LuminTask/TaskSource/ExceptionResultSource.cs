using LuminThread.Interface;
using LuminThread.Utility;
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using Lumin.Threading.Tasks.Utility;

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
        
        ptr->Id = 0;
        
        LuminTaskMarshal.GetTaskItem(ptr->Id).Exception = ExceptionDispatchInfo.Capture(exception);
        
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
        
        item.Exception?.Throw();
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
        
        item.Exception?.Throw();
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
        
        item.Reset();
        
        if (!source._calledGet)
        {
            LuminTaskScheduler.PublishUnobservedTaskException(item.Exception?.SourceException);
        }
        
        MemoryHelper.Free(ptr);
    }
}