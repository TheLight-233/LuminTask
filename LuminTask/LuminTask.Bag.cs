using System;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Threading;
using LuminThread.Interface;
using LuminThread.Utility;

namespace LuminThread;

[Preserve]
public static class LuminTaskBag
{
    public static readonly LuminTaskItem[] TaskBag = new LuminTaskItem[MaxBagCount];
    public static readonly int[] IndexList = new int[MaxBagCount];
    private static volatile int _headWithTag;
    public const int MaxBagCount = 255;
    public const short End = 0;
    
    static LuminTaskBag()
    {
        for (short i = 0; i < MaxBagCount; i++)
        {
            TaskBag[i] = new LuminTaskItem(i);
        }

        for (short i = 1; i < MaxBagCount; i++)
        {
            IndexList[i] = i + 1;
        }
        IndexList[0] = End;
        IndexList[MaxBagCount - 1] = End;

        // 初始 head：索引 1，tag = 1
        _headWithTag = (1 & 0xFF) | (1 << 8);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static short GetId()
    {
        while (true)
        {
            int head = _headWithTag;
            int idx = head & 0xFF;

            if (idx == End) 
                LuminTaskExceptionHelper.ThrowTaskItemExhausted();
            
            ref int next = ref Unsafe.Add(ref LuminTaskMarshal.GetArrayDataReference(IndexList), (nint)(uint)idx);

            int tag = head >> 8;
            int newTag = unchecked(tag + 1);

            int newHead = (next & 0xFF) | (newTag << 8);
            
            if (Interlocked.CompareExchange(ref _headWithTag, newHead, head) == head)
                return (short)idx;
        }
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ResetId(short idx)
    {
        int i = idx;

        while (true)
        {
            int head = _headWithTag;
            int headIndex = head & 0xFF;
            int tag = head >> 8;
            
            Unsafe.Add(ref LuminTaskMarshal.GetArrayDataReference(IndexList), i) = headIndex;

            int newTag = unchecked(tag + 1);
            int newHead = (i & 0xFF) | (newTag << 8);
            
            if (Interlocked.CompareExchange(ref _headWithTag, newHead, head) == head)
                return;
        }
    }
}

[Preserve]
[StructLayout(LayoutKind.Explicit)]
public unsafe struct LuminTaskItem
{
    [FieldOffset(0)]
    public readonly short Id;
    [FieldOffset(2)]
    public LuminTaskStatus Status = LuminTaskStatus.Pending;
    [FieldOffset(3)]
    public bool ContinueOnCapturedContext = true;
    [FieldOffset(8)]
    public Action<object>? Continuation;
    [FieldOffset(16)]
    public object? State;
    [FieldOffset(24)]
    public ExecutionContext? CapturedContext;
    [FieldOffset(32)]
    public ExceptionDispatchInfo? Exception;
    [FieldOffset(40)]
    public object? Error;
    [FieldOffset(48)]
    public CancellationToken CancellationToken;
    [FieldOffset(56)] public object? ResultRef;
    [FieldOffset(64)] public fixed byte ResultValue[64];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal LuminTaskItem(short id)
    {
        Id = id;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Reset()
    {
        Status = LuminTaskStatus.Pending;
        ContinueOnCapturedContext = false;
        Continuation = null;
        State = null;
        CapturedContext = null;
        Exception = null;
        Error = null;
        ResultRef = null;
    }
}

[Preserve]
[StructLayout(LayoutKind.Explicit)]
public unsafe struct LuminTaskState
{
    [FieldOffset(0)]
    public readonly void* Source;
    [FieldOffset(8)]
    public readonly CancellationToken CancellationToken;
    [FieldOffset(16)]
    public object State;
    [FieldOffset(24)]
    public object StateTuple;
    [FieldOffset(32)]
    public void* ValueState;
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public LuminTaskState(void* source)
    {
        Source = source;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public LuminTaskState(void* source, CancellationToken token)
    {
        Source = source;
        CancellationToken = token;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public LuminTaskState(void* source, CancellationToken token, object state)
    {
        Source = source;
        CancellationToken = token;
        State = state;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public LuminTaskState(void* source, CancellationToken token, object state, object stateTuple)
    {
        Source = source;
        CancellationToken = token;
        State = state;
        StateTuple = stateTuple;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public LuminTaskState(void* source, CancellationToken token, object state, void* valueState)
    {
        Source = source;
        CancellationToken = token;
        State = state;
        ValueState = valueState;
    }
    
    
}