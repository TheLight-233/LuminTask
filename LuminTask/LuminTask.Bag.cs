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
    private static volatile int _head;
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
            IndexList[i] = (short)(i + 1);
        }
        IndexList[0] = End;
        _head = 1; //第一个索引
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static short GetId()
    {
        int current, idx;
        do
        {
            current = _head;          // 低 8 位 = 索引
            idx = current & 0xFF;
            if (idx == End) LuminTaskExceptionHelper.ThrowTaskItemExhausted();
        } while (Interlocked.CompareExchange(ref _head,
                     IndexList[idx],  
                     current) != current);
        return (short)idx;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ResetId(short idx)
    {
        int current;
        do
        {
            current = _head;
            IndexList[idx] = current;
        } while (Interlocked.CompareExchange(ref _head, idx, current) != current);
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

public readonly unsafe struct LuminTaskState
{
    public readonly void* Source;
    public readonly CancellationToken CancellationToken;
    public readonly object State;
    
    public LuminTaskState(void* source)
    {
        Source = source;
    }
    
    public LuminTaskState(void* source, CancellationToken token)
    {
        Source = source;
        CancellationToken = token;
    }

    public LuminTaskState(void* source, CancellationToken token, object state)
    {
        Source = source;
        CancellationToken = token;
        State = state;
    }
}