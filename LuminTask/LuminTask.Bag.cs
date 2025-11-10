using System;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Threading;
using LuminThread.Interface;

namespace LuminThread;

[Preserve]
public static class LuminTaskBag
{
    public static readonly LuminTaskItem[] TaskBag = new LuminTaskItem[MaxBagCount];
    public const int MaxBagCount = 255;
    public static byte Next = 0;
    
    static LuminTaskBag()
    {
        for (short i = 0; i < MaxBagCount; i++)
        {
            TaskBag[i] = new LuminTaskItem(i);
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

    internal LuminTaskItem(short id)
    {
        Id = id;
    }
    
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