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
    // ------------------------------------------------------------------------------------
    // Non-moving, segmented storage + a lock-free id allocator.
    //
    // Items live in fixed-size CHUNK_SIZE blocks that, once allocated, are NEVER relocated, so a
    // `ref LuminTaskItem` stays valid for the slot's lifetime even while the pool grows on another
    // thread. The outer chunk array is pre-sized to MAX_CHUNKS and never swapped.
    //
    // ID ALLOCATION (perf-critical): a freed id is recycled through a lock-free Treiber stack, so
    // both GetId and ResetId are O(1) regardless of how many slots are live. The previous design
    // claimed the lowest free bit in a bitmap, which degraded to an O(capacity/word) scan once the
    // pool was saturated (e.g. a WhenAll over ~10k tasks grows the pool past 10k slots and every
    // subsequent claim had to scan ~150+ words). The stack reuses the most-recently-freed slot,
    // which is also more cache-friendly. `_highWater` only advances when the stack is empty, so it
    // settles at the peak number of simultaneously-live tasks.
    //
    // Correctness: each id flows alloc -> free -> alloc, pushed exactly once per free and popped
    // exactly once per alloc, both via Interlocked CAS (a full fence), so no id is ever handed to
    // two callers and the freeing thread's writes (slot Reset, etc.) are visible to the next taker.
    // The 32-bit version tag in the stack head defeats ABA.
    // ------------------------------------------------------------------------------------

    private const int CHUNK_BITS = 8;
    private const int CHUNK_SIZE = 1 << CHUNK_BITS;   // 256 items per chunk
    private const int CHUNK_MASK = CHUNK_SIZE - 1;
    private const int MAX_SLOTS  = short.MaxValue;    // 32767 (ids must fit in a short)
    private const int MAX_CHUNKS = (MAX_SLOTS + CHUNK_SIZE - 1) / CHUNK_SIZE;
    private const int INITIAL_CHUNKS = 1;

    // Outer array is pre-sized to MAX_CHUNKS and never reallocated; only its slots get filled in
    // as the pool grows. An individual chunk reference, once published, never changes.
    private static readonly LuminTaskItem[]?[] _chunks = new LuminTaskItem[MAX_CHUNKS][];
    private static readonly object _expandLock = new object();

    // Lock-free id allocator state.
    //  _freeStack : packed Treiber-stack head = ((long)version << 32) | (uint)(topId + 1).
    //               The low half is 0 when the stack is empty; the high half is an ABA version tag.
    //               A `long` field cannot be declared `volatile` in C#, so it is only ever touched
    //               through Interlocked / Volatile.Read.
    //  _freeNext  : _freeNext[id] is the id directly below `id` on the stack (-1 at the bottom).
    //  _highWater : the next id that has never been allocated; advanced only when the stack is empty.
    //  _liveCount : number of currently-rented ids (diagnostics only).
    private static readonly int[] _freeNext = new int[MAX_SLOTS];
    private static long _freeStack;
    private static int _highWater;
    private static int _liveCount;

    // Number of backed slots (always a multiple of CHUNK_SIZE). Volatile: published after the
    // backing chunk so a reader that sees an id < _capacity also sees its chunk.
    private static volatile int _capacity;

    static LuminTaskBag()
    {
        for (int i = 0; i < INITIAL_CHUNKS; i++)
            AddChunkLocked();
    }

    public static int Capacity => _capacity;

    /// <summary>
    /// Returns a stable ref to the item backing the given slot id. The caller is responsible
    /// for passing an id that has been handed out by <see cref="GetId"/> (hence is &lt; _capacity);
    /// an out-of-range id throws (IndexOutOfRange / NullReference) rather than corrupting memory.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ref LuminTaskItem GetItem(int id)
    {
        // Acquire-read pairs with the Volatile.Write in AddChunkLocked, so a chunk allocated
        // during growth on another thread is always observed as non-null here.
        var chunk = Volatile.Read(ref _chunks[id >> CHUNK_BITS])!;
        return ref chunk[id & CHUNK_MASK];
    }

    // Pops the most-recently-freed id off the lock-free stack. Returns false when the stack is
    // empty (the caller then advances the high-water mark).
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool TryPopFreeId(out short id)
    {
        long head = Volatile.Read(ref _freeStack);
        while (true)
        {
            int topPlus1 = (int)(uint)head;
            if (topPlus1 == 0)
            {
                id = -1;
                return false;
            }

            int topId = topPlus1 - 1;
            int next = _freeNext[topId];                 // id below the top (-1 at the bottom)
            uint version = (uint)(head >> 32);
            long desired = ((long)(version + 1) << 32) | (uint)(next + 1);

            long observed = Interlocked.CompareExchange(ref _freeStack, desired, head);
            if (observed == head)
            {
                id = (short)topId;
                return true;
            }
            head = observed;                             // contended: retry with the observed head
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static short GetId()
    {
        if (TryPopFreeId(out var recycled))
        {
            Interlocked.Increment(ref _liveCount);
            return recycled;
        }

        // Stack empty: hand out a never-used id from the monotonic high-water mark.
        int id = Interlocked.Increment(ref _highWater) - 1;
        if (id >= MAX_SLOTS)
            LuminTaskExceptionHelper.ThrowTaskItemExhausted();

        if (id >= _capacity)
            EnsureCapacity(id);

        Interlocked.Increment(ref _liveCount);
        return (short)id;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryGetId(out short id)
    {
        if (TryPopFreeId(out id))
        {
            Interlocked.Increment(ref _liveCount);
            return true;
        }

        int abs = Interlocked.Increment(ref _highWater) - 1;
        if (abs >= MAX_SLOTS)
        {
            id = -1;
            return false;
        }

        if (abs >= _capacity)
            EnsureCapacity(abs);

        Interlocked.Increment(ref _liveCount);
        id = (short)abs;
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ResetId(short idx)
    {
        int id = idx;
        long head = Volatile.Read(ref _freeStack);
        while (true)
        {
            int topPlus1 = (int)(uint)head;
            _freeNext[id] = topPlus1 - 1;                // link this node above the current top
            uint version = (uint)(head >> 32);
            long desired = ((long)(version + 1) << 32) | (uint)(id + 1);

            long observed = Interlocked.CompareExchange(ref _freeStack, desired, head);
            if (observed == head)
            {
                Interlocked.Decrement(ref _liveCount);
                return;
            }
            head = observed;                             // contended: retry with the observed head
        }
    }

    private static void EnsureCapacity(int id)
    {
        lock (_expandLock)
        {
            while (id >= _capacity)
                AddChunkLocked();
        }
    }

    // Must be called under _expandLock (or from the static constructor, which is single-threaded).
    private static void AddChunkLocked()
    {
        int chunkIndex = _capacity >> CHUNK_BITS;
        if (chunkIndex >= MAX_CHUNKS)
            LuminTaskExceptionHelper.ThrowTaskItemExhausted();

        var chunk = new LuminTaskItem[CHUNK_SIZE];
        int baseId = chunkIndex << CHUNK_BITS;
        for (int i = 0; i < CHUNK_SIZE; i++)
            chunk[i] = new LuminTaskItem((short)(baseId + i));

        // Publish the chunk before bumping capacity: any thread that later observes an id within
        // [old, new) capacity (always after the interlocked completion rendezvous, which provides
        // the happens-before edge) is guaranteed to observe this chunk reference as non-null.
        Volatile.Write(ref _chunks[chunkIndex], chunk);
        _capacity += CHUNK_SIZE;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetAvailableCount() => _capacity - Volatile.Read(ref _liveCount);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetLiveCount() => Volatile.Read(ref _liveCount);

    public static void PrintAllTasksStatus()
    {
        int cap = _capacity;
        Console.WriteLine($"[LuminTaskBag Debug] Capacity: {cap}, Live: {GetLiveCount()}, Available: {GetAvailableCount()}");
        Console.WriteLine("Id  | Status               | HasContinuation | HasException | HasError | CapturedContext");
        Console.WriteLine("----|----------------------|-----------------|--------------|----------|----------------");

        for (int i = 0; i < cap; i++)
        {
            ref readonly var task = ref GetItem(i);
            string status        = task.Status.ToString().PadRight(20);
            string hasCont       = (task.Continuation != null).ToString();
            string hasEx         = (task.Exception     != null).ToString();
            string hasErr        = (task.Error         != null).ToString();
            string hasCtx        = (task.CapturedContext != null).ToString();
            Console.WriteLine($"{task.Id,3} | {status} | {hasCont,15} | {hasEx,12} | {hasErr,8} | {hasCtx}");
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
