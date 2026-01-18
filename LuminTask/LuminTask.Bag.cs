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
    private const int MAX_BAG_COUNT = 256;
    private const int BAG_MASK = MAX_BAG_COUNT - 1;
    
    public static readonly LuminTaskItem[] TaskBag = new LuminTaskItem[MAX_BAG_COUNT];
    private static readonly LuminBitMap _idBitMap = new LuminBitMap(MAX_BAG_COUNT);
    private static int _nextScanField = 0;
    
    private static object _sync = new ();

    static LuminTaskBag()
    {
        for (short i = 0; i < MAX_BAG_COUNT; i++)
        {
            TaskBag[i] = new LuminTaskItem(i);
        }
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static short GetId()
    {
        int startFieldIdx = _nextScanField;
        int remainingFields = _idBitMap.FieldCount;
        
        do
        {
            if (_idBitMap.TryFindAndClaimFrom(startFieldIdx, 1, out var bitmapIndex))
            {
                int id = bitmapIndex.ToAbsoluteIndex();
                _nextScanField = (id + 1) >> 3;
                //Console.WriteLine("[LuminTaskBag] Allocated Id: " + id);
                return (short)id;
            }
            
            startFieldIdx = 0;
        } while (Interlocked.Decrement(ref remainingFields) > 0);
        
        LuminTaskExceptionHelper.ThrowTaskItemExhausted();
        return -1;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryGetId(out short id)
    {
        int startFieldIdx = _nextScanField;
        int remainingFields = _idBitMap.FieldCount;
        
        do
        {
            if (_idBitMap.TryFindAndClaimFrom(startFieldIdx, 1, out var bitmapIndex))
            {
                int absoluteId = bitmapIndex.ToAbsoluteIndex();
                _nextScanField = (absoluteId + 1) >> 3;
                id = (short)absoluteId;
                return true;
            }
            
            startFieldIdx = 0;
        } while (Interlocked.Decrement(ref remainingFields) > 0);
        
        id = -1;
        return false;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ResetId(short idx)
    {
        //Console.WriteLine("[LuminTaskBag] Releasing Id: " + idx);
        var bitmapIndex = LuminBitMap.BitmapIndex.FromAbsoluteIndex(idx);
        _idBitMap.Unclaim(bitmapIndex, 1);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetAvailableCount()
    {
        return _idBitMap.CountClearBits();
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsIdAvailable(short idx)
    {
        var bitmapIndex = LuminBitMap.BitmapIndex.FromAbsoluteIndex(idx);
        return !_idBitMap.IsAnyClaimed(bitmapIndex, 1);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ClearAllIds()
    {
        _idBitMap.ClearAll();
        _nextScanField = 0;
    }
    
    public static void PrintAllTasksStatus()
    {
        Console.WriteLine($"[LuminTaskBag Debug] Total tasks: {MAX_BAG_COUNT}, Available: {GetAvailableCount()}");
        Console.WriteLine("Id  | Status               | Bitmap | HasContinuation | HasException | HasError | CapturedContext");
        Console.WriteLine("----|----------------------|--------|-----------------|--------------|----------|----------------");

        for (int i = 0; i < MAX_BAG_COUNT; i++)
        {
            ref readonly var task = ref TaskBag[i];
            string status = task.Status.ToString().PadRight(20);
            bool isOccupied = !IsIdAvailable((short)i);  // 检查位图是否标记为占用
            string bitmapStatus = isOccupied ? "Y" : "N";
            string hasContinuation = (task.Continuation != null).ToString();
            string hasException = (task.Exception != null).ToString();
            string hasError = (task.Error != null).ToString();
            string hasCapturedContext = (task.CapturedContext != null).ToString();

            Console.WriteLine($"{task.Id,3} | {status} | {bitmapStatus,6} | {hasContinuation,15} | {hasException,12} | {hasError,8} | {hasCapturedContext}");
        }
        Console.WriteLine();
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
