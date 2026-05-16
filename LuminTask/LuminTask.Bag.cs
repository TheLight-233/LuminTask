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
    private const int INITIAL_CAPACITY = 256;
    private const int EXPAND_STEP      = 32;

    private static volatile LuminTaskItem[] _taskBag;
    private static volatile LuminBitMap     _idBitMap;
    private static volatile object[]        _boxedIds;
    
    private static volatile short[] _queue;
    private static volatile int _queueHead;
    private static volatile int _queueTail;
    private static volatile int _queueCount;

    private static volatile int _nextScanField = 0;
    private static readonly object _expandLock = new object();

    public static LuminTaskItem[] TaskBag => _taskBag;
    internal static object[] BoxedIds => _boxedIds;

    static LuminTaskBag()
    {
        _taskBag  = Allocate(INITIAL_CAPACITY);
        _idBitMap = new LuminBitMap(INITIAL_CAPACITY);
        _boxedIds = MakeBoxedIds(INITIAL_CAPACITY);
        
        _queue = new short[INITIAL_CAPACITY];
        for (short i = 0; i < INITIAL_CAPACITY; i++)
            _queue[i] = i;
        _queueHead = 0;
        _queueTail = 0;
        _queueCount = INITIAL_CAPACITY;
    }

    private static LuminTaskItem[] Allocate(int count)
    {
        var arr = new LuminTaskItem[count];
        for (short i = 0; i < count; i++)
            arr[i] = new LuminTaskItem(i);
        return arr;
    }

    private static object[] MakeBoxedIds(int count)
    {
        var arr = new object[count];
        for (int i = 0; i < count; i++)
            arr[i] = i;
        return arr;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static short GetId()
    {
        while (true)
        {
            int count = Interlocked.Decrement(ref _queueCount);
            if (count >= 0)
            {
                int head = Interlocked.Increment(ref _queueHead) - 1;
                int capacity = _queue.Length;
                short id = _queue[head % capacity];

                var bitmapIndex = LuminBitMap.BitmapIndex.FromAbsoluteIndex(id);
                _idBitMap.TryClaim(bitmapIndex, 1);
                
                return id;
            }
            
            Interlocked.Increment(ref _queueCount);
            
            var bitmap = _idBitMap;
            int startIdx = _nextScanField;
            int remaining = bitmap.FieldCount;

            do
            {
                if (bitmap.TryFindAndClaimFrom(startIdx, 1, out var bitmapIndex))
                {
                    int id = bitmapIndex.ToAbsoluteIndex();
                    _nextScanField = (id + 1) >> 3;
                    return (short)id;
                }
                startIdx = 0;
            } while (Interlocked.Decrement(ref remaining) > 0);

            Expand();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryGetId(out short id)
    {
        int count = Interlocked.Decrement(ref _queueCount);
        if (count >= 0)
        {
            int head = Interlocked.Increment(ref _queueHead) - 1;
            int capacity = _queue.Length;
            id = _queue[head % capacity];
            
            var bitmapIndex = LuminBitMap.BitmapIndex.FromAbsoluteIndex(id);
            _idBitMap.TryClaim(bitmapIndex, 1);
            
            return true;
        }
        
        Interlocked.Increment(ref _queueCount);
        
        var bitmap = _idBitMap;
        int startIdx = _nextScanField;
        int remaining = bitmap.FieldCount;

        do
        {
            if (bitmap.TryFindAndClaimFrom(startIdx, 1, out var bitmapIndex))
            {
                int absoluteId = bitmapIndex.ToAbsoluteIndex();
                _nextScanField = (absoluteId + 1) >> 3;
                id = (short)absoluteId;
                return true;
            }
            startIdx = 0;
        } while (Interlocked.Decrement(ref remaining) > 0);

        id = -1;
        return false;
    }

    private static void Expand()
    {
        lock (_expandLock)
        {
            int oldCap = _taskBag.Length;
            int newCap = oldCap + EXPAND_STEP;

            if (newCap > short.MaxValue)
                LuminTaskExceptionHelper.ThrowTaskItemExhausted();

            var oldBag    = _taskBag;
            var oldBitMap = _idBitMap;
            var oldBoxed  = _boxedIds;
            var oldQueue  = _queue;

            var newBag    = new LuminTaskItem[newCap];
            var newBitMap = new LuminBitMap(newCap);
            var newBoxed  = new object[newCap];
            var newQueue  = new short[newCap];
            
            for (int i = 0; i < oldCap; i++)
            {
                newBag[i]   = oldBag[i];
                newBoxed[i] = oldBoxed[i];
                if (oldBitMap.IsAnyClaimed(LuminBitMap.BitmapIndex.FromAbsoluteIndex(i), 1))
                    newBitMap.TryClaim(LuminBitMap.BitmapIndex.FromAbsoluteIndex(i), 1);
            }
            
            for (short i = (short)oldCap; i < newCap; i++)
            {
                newBag[i]   = new LuminTaskItem(i);
                newBoxed[i] = (int)i;
            }
            
            int newHead = 0;
            int newTail = 0;
            int newCount = 0;
            
            int oldQueueCount = _queueCount;
            if (oldQueueCount > 0)
            {
                int oldHead = _queueHead;
                for (int i = 0; i < oldQueueCount; i++)
                {
                    short qid = oldQueue[(oldHead + i) % oldCap];
                    if (qid < newCap && !newBitMap.IsAnyClaimed(LuminBitMap.BitmapIndex.FromAbsoluteIndex(qid), 1))
                    {
                        newQueue[newTail++] = qid;
                        newCount++;
                    }
                }
            }

            for (short i = (short)oldCap; i < newCap; i++)
            {
                newQueue[newTail++] = i;
                newCount++;
            }

            _taskBag  = newBag;
            _boxedIds = newBoxed;
            _queue    = newQueue;
            _queueHead = 0;
            _queueTail = newCount;
            _queueCount = newCount;
            
            Thread.MemoryBarrier();
            _idBitMap = newBitMap;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ResetId(short idx)
    {
        var bitmapIndex = LuminBitMap.BitmapIndex.FromAbsoluteIndex(idx);
        _idBitMap.Unclaim(bitmapIndex, 1);
        
        int count = Interlocked.Increment(ref _queueCount);
        int tail = Interlocked.Increment(ref _queueTail) - 1;
        int capacity = _queue.Length;
        _queue[tail % capacity] = idx;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetAvailableCount() => _queueCount;

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
        
        int cap = _taskBag.Length;
        _queue = new short[cap];
        for (short i = 0; i < cap; i++)
            _queue[i] = i;
        _queueHead = 0;
        _queueTail = 0;
        _queueCount = cap;
    }

    public static void PrintAllTasksStatus()
    {
        var bag = _taskBag;
        int cap = bag.Length;
        Console.WriteLine($"[LuminTaskBag Debug] Capacity: {cap}, Available: {GetAvailableCount()}");
        Console.WriteLine("Id  | Status               | Bitmap | HasContinuation | HasException | HasError | CapturedContext");
        Console.WriteLine("----|----------------------|--------|-----------------|--------------|----------|----------------");

        for (int i = 0; i < cap; i++)
        {
            ref readonly var task = ref bag[i];
            string status        = task.Status.ToString().PadRight(20);
            bool isOccupied      = !IsIdAvailable((short)i);
            string bitmapStatus  = isOccupied ? "Y" : "N";
            string hasCont       = (task.Continuation != null).ToString();
            string hasEx         = (task.Exception     != null).ToString();
            string hasErr        = (task.Error         != null).ToString();
            string hasCtx        = (task.CapturedContext != null).ToString();
            Console.WriteLine($"{task.Id,3} | {status} | {bitmapStatus,6} | {hasCont,15} | {hasEx,12} | {hasErr,8} | {hasCtx}");
        }
        Console.WriteLine();
    }
}

[Preserve]
[StructLayout(LayoutKind.Explicit)]
public unsafe struct LuminTaskItem
{
    [FieldOffset(0)]  public readonly short Id;
    [FieldOffset(2)]  public LuminTaskStatus Status = LuminTaskStatus.Pending;
    [FieldOffset(3)]  public bool ContinueOnCapturedContext = true;
    [FieldOffset(4)]  public bool ShouldClearResult;
    [FieldOffset(8)]  public IDisposable? ResultRefDispose;
    [FieldOffset(16)]  public Action<object>? Continuation;
    [FieldOffset(24)] public object? State;
    [FieldOffset(32)] public ExecutionContext? CapturedContext;
    [FieldOffset(40)] public ExceptionDispatchInfo? Exception;
    [FieldOffset(48)] public object? Error;
    [FieldOffset(56)] public CancellationToken CancellationToken;
    [FieldOffset(64)] public object? ResultRef;
    [FieldOffset(72)] public fixed byte ResultValue[64];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal LuminTaskItem(short id) { Id = id; }

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
        CancellationToken = default;
        unsafe { fixed (byte* p = ResultValue) Unsafe.InitBlock(p, 0, 64); }

        if (ResultRefDispose != null)
        {
            ResultRefDispose.Dispose();
            ResultRefDispose = null;
        }
        
        if (ShouldClearResult)
        {
            var resultPtr = (void*)Unsafe.ReadUnaligned<nint>(ref ResultValue[0]);
            if (resultPtr != null)
            {
                MemoryHelper.Free(resultPtr);
            }
        }
    }
}

[Preserve]
[StructLayout(LayoutKind.Explicit)]
public unsafe struct LuminTaskState
{
    [FieldOffset(0)]  public readonly void* Source;
    [FieldOffset(8)]  public readonly CancellationToken CancellationToken;
    [FieldOffset(16)] public object State;
    [FieldOffset(24)] public object StateTuple;
    [FieldOffset(32)] public void* ValueState;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public LuminTaskState(void* source) { Source = source; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public LuminTaskState(void* source, CancellationToken token)
    { Source = source; CancellationToken = token; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public LuminTaskState(void* source, CancellationToken token, object state)
    { Source = source; CancellationToken = token; State = state; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public LuminTaskState(void* source, CancellationToken token, object state, object stateTuple)
    { Source = source; CancellationToken = token; State = state; StateTuple = stateTuple; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public LuminTaskState(void* source, CancellationToken token, object state, void* valueState)
    { Source = source; CancellationToken = token; State = state; ValueState = valueState; }
}
