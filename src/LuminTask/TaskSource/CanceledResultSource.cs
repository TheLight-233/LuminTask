using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using LuminThread.Interface;
using LuminThread.Utility;

namespace LuminThread.TaskSource;

public unsafe struct CanceledResultSource<T>
{
    private static IntPtr s_cachedSource;

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

    public CanceledResultSource(short id, CancellationToken cancellationToken = default)
    {
        Id = id;
        LuminTaskMarshal.GetTaskItem(id).CancellationToken = cancellationToken;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static CanceledResultSource<T>* Create(CancellationToken cancellationToken = default)
    {
        var cached = Interlocked.Exchange(ref s_cachedSource, IntPtr.Zero);
        var ptr = cached != IntPtr.Zero
            ? (CanceledResultSource<T>*)cached.ToPointer()
            : (CanceledResultSource<T>*)MemoryHelper.Alloc(
                (nuint)Math.Max(Unsafe.SizeOf<CanceledResultSource<T>>(), IntPtr.Size));

        ptr->Id = LuminTaskBag.GetId();

        ref var item = ref LuminTaskMarshal.GetTaskItem(ptr->Id);
        
        item.Reset();
        
        item.CancellationToken = cancellationToken;
        
        return ptr;
    }

    [DebuggerHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void GetResult(void* ptr, short token)
    {
        ref var source = ref Unsafe.AsRef<CanceledResultSource<T>>(ptr);

        if (token != source.Id) LuminTaskExceptionHelper.ThrowTokenMismatch();

        var ct = LuminTaskMarshal.GetTaskItem(source.Id).CancellationToken;
        Dispose(ptr);
        throw new OperationCanceledException(ct);
    }

    [DebuggerHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T GetResultValue(void* ptr, short token)
    {
        ref var source = ref Unsafe.AsRef<CanceledResultSource<T>>(ptr);

        if (token != source.Id) LuminTaskExceptionHelper.ThrowTokenMismatch();

        var ct = LuminTaskMarshal.GetTaskItem(source.Id).CancellationToken;
        Dispose(ptr);
        throw new OperationCanceledException(ct);
    }

    [DebuggerHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static LuminTaskStatus GetStatus(void* ptr, short token)
    {
        return LuminTaskStatus.Canceled;
    }

    [DebuggerHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static LuminTaskStatus UnsafeGetStatus(void* ptr)
    {
        return LuminTaskStatus.Canceled;
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
        ref var source = ref Unsafe.AsRef<CanceledResultSource<T>>(ptr);
        ref var item = ref LuminTaskMarshal.GetTaskItem(source.Id);
        item.CancellationToken = default;

        LuminTaskBag.ResetId(item.Id);

        if (Interlocked.CompareExchange(ref s_cachedSource, new IntPtr(ptr), IntPtr.Zero) != IntPtr.Zero)
            MemoryHelper.Free(ptr);
    }
}
