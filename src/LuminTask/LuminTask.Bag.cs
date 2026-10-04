using System;
using System.Runtime.CompilerServices;
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
    // Storage is split in two, following the ECS idea that a scan should only touch the array it
    // needs:
    //
    //   * LuminTaskItem   - the hot fields every operation touches (sync word, continuation slot,
    //                       continuation state, captured context). Kept as tight as possible so a
    //                       page of items stays cache friendly.
    //   * LuminTaskCold   - the cold side, one lazily allocated array PER FIELD, indexed by the
    //                       slot's position inside its chunk. A page only pays for the arrays it
    //                       actually used, and no scan of the hot side ever pulls them in.
    //
    // Payload blocks hold pointer-sized inline results or pointers to larger results and loop
    // scratch. They are allocated on demand instead of reserving a full result buffer per item.
    //
    // ID ALLOCATION (perf-critical): a freed id is recycled through a lock-free Treiber stack, so
    // both GetId and ResetId are O(1) regardless of how many slots are live. The stack reuses the
    // most-recently-freed slot, which is also more cache friendly. `_highWater` only advances when
    // the stack is empty, so it settles at the peak number of simultaneously-live tasks.
    //
    // Correctness: each id flows alloc -> free -> alloc, pushed exactly once per free and popped
    // exactly once per alloc, both via Interlocked CAS (a full fence), so no id is ever handed to
    // two callers and the freeing thread's writes are visible to the next taker. The 32-bit version
    // tag in the stack head defeats ABA.
    // ------------------------------------------------------------------------------------

    internal const int CHUNK_BITS = 8;
    internal const int CHUNK_SIZE = 1 << CHUNK_BITS;   // 256 items per chunk
    internal const int CHUNK_MASK = CHUNK_SIZE - 1;
    private const int MAX_SLOTS  = short.MaxValue;    // 32767 (handles keep the id in 16 bits)
    private const int MAX_CHUNKS = (MAX_SLOTS + CHUNK_SIZE - 1) / CHUNK_SIZE;
    private const int INITIAL_CHUNKS = 1;

    // 冷侧块回收阈值：整包持续静默这么久之后才回收空块。“排空即回收”会让单任务流式负载
    // 每次释放都把块还掉、下个任务又重建（每任务一次数组分配），这个阈值把它变成
    // “只在真正的静默窗口回收”。
    private const int IDLE_COLD_TRIM_MILLISECONDS = 1000;

    // Outer arrays are pre-sized to MAX_CHUNKS and never reallocated; only their slots get filled in
    // as the pool grows. An individual chunk reference, once published, never changes.
    private static readonly LuminTaskItem[][] _chunks = new LuminTaskItem[MAX_CHUNKS][];
    private static readonly LuminTaskCold?[] _cold = new LuminTaskCold[MAX_CHUNKS];
    private static readonly object _expandLock = new object();

    // Lock-free id allocator state.
    //  _freeStack : version(32) | live(16) | (topId + 1)(16).
    //               The low 16 bits are 0 when empty. live=65535 exclusively owns idle trimming.
    //               Pop/push and lifetime accounting share one CAS; the ABA tag stays 32 bits.
    //               A `long` field cannot be declared `volatile` in C#, so it is only ever touched
    //               through Interlocked / Volatile.Read.
    //  _freeNextChunks : next id + 1 (0 means end), two bytes per backed item.
    //  _highWater : the next id that has never been allocated; advanced only when the stack is empty.
    private const long FreeIdMask = 0xFFFFL;
    private const long LiveUnit = 1L << 16;
    private const long LiveMask = 0xFFFF0000L;
    private static readonly ushort[]?[] _freeNextChunks = new ushort[]?[MAX_CHUNKS];
    private static long _freeStack;
    private static int _highWater;
    private static int _retiredCount;

    // Number of backed slots (always a multiple of CHUNK_SIZE). Volatile: published after the
    // backing chunk so a reader that sees an id < _capacity also sees its chunk.
    private static volatile int _capacity;

    // 冷侧回收：整包转入静默（live 归零）时记下时刻并挂一个一次性定时器；到点仍然静默才回收
    // 空块（见 LuminTaskCold.TrimEmptyBlocks）。定时器惰性创建：从没触及冷侧块的负载不付费。
    private static Timer? _coldTrimTimer;
    private static int _coldTrimArmed;

    // Environment.TickCount 记下的静默起点；差值用 unchecked 计算，回绕也正确。
    private static int _idleSinceTimestamp;

    static LuminTaskBag()
    {
        for (int i = 0; i < INITIAL_CHUNKS; i++)
            AddChunkLocked();
    }

    public static int Capacity => _capacity;

    /// <summary>
    /// Returns a stable ref to the hot item backing the given slot id. The caller is responsible
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

    /// <summary>
    /// Returns the cold side (lazily allocated per-field arrays) of the
    /// chunk that owns the given slot id.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static LuminTaskCold GetCold(int id)
        => Volatile.Read(ref _cold[id >> CHUNK_BITS])!;

    // Pops the most-recently-freed id off the lock-free stack. Returns false when the stack is
    // empty (the caller then advances the high-water mark).
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool TryPopFreeId(out short id)
    {
        long head = Volatile.Read(ref _freeStack);
        while (true)
        {
            if ((head & LiveMask) == LiveMask)
            {
                Thread.SpinWait(1);
                head = Volatile.Read(ref _freeStack);
                continue;
            }
            int topPlus1 = (int)(head & FreeIdMask);
            if (topPlus1 == 0)
            {
                id = -1;
                return false;
            }

            int topId = topPlus1 - 1;
            int next = _freeNextChunks[topId >> CHUNK_BITS]![topId & CHUNK_MASK]; // link below the top
            uint version = (uint)(head >> 32);
            long desired = ((long)(version + 1) << 32) | ((head & LiveMask) + LiveUnit) | (uint)next;

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
        if (TryGetId(out short id)) return id;
        LuminTaskExceptionHelper.ThrowTaskItemExhausted();
        return -1;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryGetId(out short id)
    {
        if (TryPopFreeId(out id))
        {
            return true;
        }

        return TryGetNewId(out id);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool TryGetNewId(out short id)
    {
        int next = Volatile.Read(ref _highWater);
        while (next < MAX_SLOTS)
        {
            int observed = Interlocked.CompareExchange(ref _highWater, next + 1, next);
            if (observed == next)
            {
                if (next >= _capacity) EnsureCapacity(next);
                Enter();
                id = (short)next;
                return true;
            }
            next = observed;
        }
        id = -1;
        return false;
    }

    // First-use ids have no free-list entry. Join their lifetime before touching cold fields;
    // recycled ids already did so in the pop CAS.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void Enter()
    {
        long head = Volatile.Read(ref _freeStack);
        while (true)
        {
            if ((head & LiveMask) != LiveMask)
            {
                uint version = (uint)(head >> 32);
                long desired = ((long)(version + 1) << 32) | ((head & LiveMask) + LiveUnit) | (head & FreeIdMask);
                long observed = Interlocked.CompareExchange(ref _freeStack, desired, head);
                if (observed == head) return;
                head = observed;
            }
            else
            {
                Thread.SpinWait(1);
                head = Volatile.Read(ref _freeStack);
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ResetId(short idx)
    {
        int id = idx;
        long head = Volatile.Read(ref _freeStack);
        while (true)
        {
            int topPlus1 = (int)(head & FreeIdMask);
            _freeNextChunks[id >> CHUNK_BITS]![id & CHUNK_MASK] = (ushort)topPlus1;
            uint version = (uint)(head >> 32);
            long desired = ((long)(version + 1) << 32) | ((head & LiveMask) - LiveUnit) | (uint)(id + 1);

            long observed = Interlocked.CompareExchange(ref _freeStack, desired, head);
            if (observed == head)
            {
                if ((desired & LiveMask) == 0 && LuminTaskCold.AnyBlockCreated)
                {
                    Volatile.Write(ref _idleSinceTimestamp, Environment.TickCount);
                    ArmColdTrim();
                }
                return;
            }
            head = observed;                             // contended: retry with the observed head
        }
    }

    // Generation exhaustion permanently removes an item, but must still leave its live lifetime.
    internal static void RetireId()
    {
        Interlocked.Increment(ref _retiredCount);
        long head = Volatile.Read(ref _freeStack);
        while (true)
        {
            uint version = (uint)(head >> 32);
            long desired = ((long)(version + 1) << 32) | ((head & LiveMask) - LiveUnit) | (head & FreeIdMask);
            long observed = Interlocked.CompareExchange(ref _freeStack, desired, head);
            if (observed == head)
            {
                if ((desired & LiveMask) == 0 && LuminTaskCold.AnyBlockCreated)
                {
                    Volatile.Write(ref _idleSinceTimestamp, Environment.TickCount);
                    ArmColdTrim();
                }
                return;
            }
            head = observed;
        }
    }

    // 挂一次性回收定时器。已挂则不再重复挂：忙碌负载下最多每秒重挂一次。
    private static void ArmColdTrim() => ArmColdTrim(IDLE_COLD_TRIM_MILLISECONDS);

    private static void ArmColdTrim(int dueMilliseconds)
    {
        if (!LuminTaskCold.AnyBlockCreated) return;
        if (Interlocked.CompareExchange(ref _coldTrimArmed, 1, 0) != 0) return;

        var timer = _coldTrimTimer;
        if (timer is null)
        {
            var fresh = new Timer(static _ => TrimColdBlocks(), null, Timeout.Infinite, Timeout.Infinite);
            timer = Interlocked.CompareExchange(ref _coldTrimTimer, fresh, null) ?? fresh;
        }

        timer.Change(dueMilliseconds, Timeout.Infinite);
    }

    private static void TrimColdBlocks()
    {
        Volatile.Write(ref _coldTrimArmed, 0);

        // 忙起来了：留给下一个静默窗口（每次静默转换都会重新挂）。
        long head = Volatile.Read(ref _freeStack);
        if ((head & LiveMask) != 0) return;

        int idleFor = unchecked(Environment.TickCount - Volatile.Read(ref _idleSinceTimestamp));
        if (idleFor < IDLE_COLD_TRIM_MILLISECONDS)
        {
            // 定时器早于“当前静默窗口”到期（可能来自上一轮挂载）：补挂剩余时间，
            // 否则这个静默窗口既不会回收也不会再挂，块就永远留着了。
            ArmColdTrim(Math.Max(1, IDLE_COLD_TRIM_MILLISECONDS - idleFor));
            return;
        }

        uint version = (uint)(head >> 32);
        long trimming = ((long)(version + 1) << 32) | LiveMask | (head & FreeIdMask);
        if (Interlocked.CompareExchange(ref _freeStack, trimming, head) != head) return;
        try
        {
            int chunkCount = _capacity >> CHUNK_BITS;
            for (int chunk = 0; chunk < chunkCount; chunk++)
                Volatile.Read(ref _cold[chunk])?.TrimEmptyBlocks();
            LuminTaskCold.AnyBlockCreated = false;
        }
        finally
        {
            Volatile.Write(ref _freeStack, ((long)(version + 2) << 32) | (head & FreeIdMask));
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
        Volatile.Write(ref _freeNextChunks[chunkIndex], new ushort[CHUNK_SIZE]);
        Volatile.Write(ref _cold[chunkIndex], new LuminTaskCold());
        Volatile.Write(ref _chunks[chunkIndex], chunk);
        _capacity += CHUNK_SIZE;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetAvailableCount() => Math.Min(_capacity, MAX_SLOTS) - GetLiveCount() - Volatile.Read(ref _retiredCount);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetLiveCount()
    {
        int count = (int)((Volatile.Read(ref _freeStack) & LiveMask) >> 16);
        return count == ushort.MaxValue ? 0 : count;
    }

    public static void PrintAllTasksStatus()
    {
        int cap = _capacity;
        Console.WriteLine($"[LuminTaskBag Debug] Capacity: {cap}, Live: {GetLiveCount()}, Available: {GetAvailableCount()}");
        Console.WriteLine("Id  | Status               | Generation | HasContinuation | HasException | HasResult");
        Console.WriteLine("----|----------------------|------------|-----------------|--------------|----------");

        for (int i = 0; i < cap; i++)
        {
            ref readonly var item = ref GetItem(i);
            var cold = GetCold(i);
            int index = i & CHUNK_MASK;
            string status       = item.Status.ToString().PadRight(20);
            string hasCont      = (item.Continuation != null).ToString();
            string hasEx        = (cold.ExceptionAt(index) != null).ToString();
            string hasResult    = (cold.RefAt(index) != null || cold.PayloadAt(index) != 0).ToString();
            Console.WriteLine($"{item.Id,3} | {status} | {item.Generation,10} | {hasCont,15} | {hasEx,12} | {hasResult,9}");
        }
        Console.WriteLine();
    }
}

// The hot half of a slot: only what the hot paths (await rendezvous, completion, frame scan)
// actually touch. Explicit layout is used so the offsets are part of the source rather than a
// compiler decision, and so the layout cannot silently drift between target frameworks.
//
// Every offset is spelled out; no managed reference overlaps another field, which is what keeps the
// runtime's reference tracking valid in Explicit layout.
//
//   0   long   SyncState                    claim(8) | status(8) | generation(32)
//   8   object Continuation                 null / sentinel / registered continuation
//   16  object State                        continuation state
//   24  ExecutionContext CapturedContext    captured at registration time
//   32  short  Id                           slot index, stable across recycles
//   34  bool   ContinueOnCapturedContext
[Preserve]
[StructLayout(LayoutKind.Explicit, Size = 40)]
public struct LuminTaskItem
{
    [FieldOffset(0)]  public long SyncState;
    [FieldOffset(8)]  public Action<object>? Continuation;
    [FieldOffset(16)] public object? State;
    [FieldOffset(24)] public ExecutionContext? CapturedContext;
    [FieldOffset(32)] public short Id;
    [FieldOffset(34)] public bool ContinueOnCapturedContext;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal LuminTaskItem(short id)
    {
        Continuation = null;
        State = null;
        CapturedContext = null;
        SyncState = 0;
        Id = id;
        ContinueOnCapturedContext = false;
    }

    public LuminTaskStatus Status
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => (LuminTaskStatus)((Volatile.Read(ref SyncState) >> 8) & 0xFF);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set => Volatile.Write(ref SyncState, (SyncState & ~0xFF00L) | ((long)value << 8));
    }

    public uint Generation
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => (uint)((ulong)Volatile.Read(ref SyncState) >> 16);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal set => Volatile.Write(ref SyncState, (long)(((ulong)value << 16) | ((ulong)SyncState & 0xFFFF)));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Reset()
    {
        // Keep Id and generation; clear only the claim flag and the previous completion state. The
        // cold side is cleared by LuminTaskCoreSource.Return.
        Volatile.Write(ref SyncState, SyncState & ~0xFFFFL);
        ContinueOnCapturedContext = false;
        Continuation = null;
        State = null;
        CapturedContext = null;
    }
}

// The cold half of a chunk: structure-of-arrays, stored as 64-slot BLOCKS that are created on
// demand. Sub-blocking keeps the "only pay for what you touch" property at a useful granularity: a
// chunk that only ever uses a few slots with cancellation faults in one 64-entry
// CancellationTokenRegistration block (~2 KB) instead of reserving all 256 entries (~8 KB). The
// block table of a field is itself created on the first write, so a chunk that never uses a field
// pays nothing for it.
//
// Indexing is two level: `index >> BlockBits` selects the block, `index & BlockMask` the entry
// inside it. Indexes are the slot's position INSIDE its chunk (id & CHUNK_MASK), not the global id.
//
// Small unmanaged results reuse the pointer-sized payload entry as inline bytes; up to 16 bytes
// use a separate lazy wide block. Larger results and loop scratch use MemoryHelper pointers.
//
// RECLAMATION: the bag exclusively claims a fully idle lifetime count before dropping blocks.
// Renting joins that count before using cold fields, and returning leaves it after cleanup.
// Consequently each field needs only its array, with no wrapper object or per-write counter.
public sealed class LuminTaskCold
{
    // Scratch size a loop-driven source may reserve (currently two timestamps for Delay).
    internal const int LoopStateSize = 16;

    // Entries per cold block. This is the allocation granularity of every cold field.
    internal const int BlockBits = 6;
    internal const int BlockSize = 1 << BlockBits;
    internal const int BlockMask = BlockSize - 1;
    private const int BlockCount = LuminTaskBag.CHUNK_SIZE >> BlockBits;

    [Flags]
    private enum ColdField : byte
    {
        Ref = 1,
        Payload = 2,
        Exception = 4,
        CancellationToken = 8,
        Registration = 16,
        Box = 32,
        InlinePayload = 64,
        WidePayload = 128,
    }

    // One byte per item, written by its owner before completion is published. This also
    // distinguishes inline payload bits (including zero) from an owned native pointer.
    // Registrations are excluded: installing one can race its cancellation callback.
    private readonly ColdField[] _fields = new ColdField[LuminTaskBag.CHUNK_SIZE];

    // 是否已经创建过任何冷块（从未触及冷侧的负载不必挂回收定时器）。
    internal static volatile bool AnyBlockCreated;

    // Reference results, and the pooled container of a reference-containing value result.
    private Array?[]? _ref;

    // Inline result bits, or a MemoryHelper pointer for large results and loop scratch.
    private Array?[]? _payload;
    private Array?[]? _widePayload;

    private Array?[]? _exception;
    private Array?[]? _cancellationTokens;
    private Array?[]? _registrations;

    // State machine box of a suspended async method. It lives here rather than in the builder struct
    // because a LuminTask is only a handle: the builder reaches the box back through the slot to
    // re-register its continuation and to return the box to its pool on completion. The reference is
    // dropped - never returned - on slot release, because the state machine inside the box may still
    // be executing when the consumer releases the slot.
    private Array?[]? _box;

    // ---------------------------------------------------------------- writers (create on demand)
    //
    // Entries are owned by one item. The completion rendezvous publishes them to its consumer.

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void SetRef(int index, object? value)
        => SetObject(ref _ref, index, value, ColdField.Ref);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void SetBox(int index, object box)
        => SetObject(ref _box, index, box, ColdField.Box);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void SetPayload(int index, nint value)
    {
        if (value == 0 && (_fields[index] & ColdField.Payload) == 0) return;
        ((nint[])Block(ref _payload, index, ColdField.Payload))[index & BlockMask] = value;
        _fields[index] &= ~ColdField.InlinePayload;
        if (value != 0) _fields[index] |= ColdField.Payload;
        else _fields[index] &= ~ColdField.Payload;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void SetInlinePayload(int index, nint value)
    {
        ((nint[])Block(ref _payload, index, ColdField.Payload))[index & BlockMask] = value;
        _fields[index] |= ColdField.Payload | ColdField.InlinePayload;
    }

    // Vector3/Quaternion-sized values get a separate lazy block; int-only pages do not pay for it.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal ref byte WidePayloadForWrite(int index)
    {
        var values = (ulong[])Block(ref _widePayload, index, ColdField.WidePayload);
        _fields[index] |= ColdField.WidePayload;
        return ref Unsafe.As<ulong, byte>(ref values[(index & BlockMask) * 2]);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal ref byte WidePayloadAt(int index)
    {
        var values = (ulong[])BlockOrNull(_widePayload, index)!;
        return ref Unsafe.As<ulong, byte>(ref values[(index & BlockMask) * 2]);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void SetException(int index, object? value)
        => SetObject(ref _exception, index, value, ColdField.Exception);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void SetObject(ref Array?[]? table, int index, object? value, ColdField field)
    {
        if (value is null && (_fields[index] & field) == 0) return;
        ((object?[])Block(ref table, index, field))[index & BlockMask] = value;
        if (value is not null) _fields[index] |= field;
        else _fields[index] &= ~field;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void SetCancellationToken(int index, in CancellationToken value)
    {
        bool has = !value.Equals(default(CancellationToken));
        if (!has && (_fields[index] & ColdField.CancellationToken) == 0) return;
        ((CancellationToken[])Block(ref _cancellationTokens, index, ColdField.CancellationToken))[index & BlockMask] = value;
        if (has) _fields[index] |= ColdField.CancellationToken;
        else _fields[index] &= ~ColdField.CancellationToken;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void SetRegistration(int index, in CancellationTokenRegistration value)
    {
        if (value.Equals(default(CancellationTokenRegistration)) && BlockOrNull(_registrations, index) is null) return;
        ((CancellationTokenRegistration[])Block(ref _registrations, index, ColdField.Registration))[index & BlockMask] = value;
    }

    // ---------------------------------------------------------------- non-allocating readers

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal object? RefAt(int index)
    {
        var block = BlockOrNull(_ref, index);
        return block is null ? null : ((object?[])block)[index & BlockMask];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal object? BoxAt(int index)
    {
        var block = BlockOrNull(_box, index);
        return block is null ? null : ((object?[])block)[index & BlockMask];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal nint PayloadAt(int index)
    {
        var block = BlockOrNull(_payload, index);
        return block is null ? 0 : ((nint[])block)[index & BlockMask];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal object? ExceptionAt(int index)
    {
        var block = BlockOrNull(_exception, index);
        return block is null ? null : ((object?[])block)[index & BlockMask];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal CancellationToken CancellationTokenAt(int index)
    {
        var block = BlockOrNull(_cancellationTokens, index);
        return block is null ? default : ((CancellationToken[])block)[index & BlockMask];
    }

    // ---------------------------------------------------------------- release

    /// <summary>
    /// Drops everything the slot at <paramref name="index"/> owns: the pending cancellation
    /// registration, the unmanaged result/scratch block, a pooled result container, the recorded
    /// exception and token, and the reference to the suspended state machine box. Never allocates —
    /// a field whose block was never faulted has nothing to release.
    /// </summary>
    internal unsafe void ReleaseSlot(int index)
    {
        var fields = _fields[index];
        int sub = index & BlockMask;

        // Pending registration first: Release is non-blocking and safe even when the caller is the
        // cancellation callback itself.
        if (BlockOrNull(_registrations, index) is { } registrations)
        {
            var values = (CancellationTokenRegistration[])registrations;
            values[sub].Release();
            values[sub] = default;
        }

        if (fields == 0) return;

        if ((fields & ColdField.Box) != 0) ClearObjectEntry(_box, index);

        if ((fields & ColdField.Payload) != 0 && BlockOrNull(_payload, index) is { } payload)
        {
            var values = (nint[])payload;
            if ((fields & ColdField.InlinePayload) == 0)
                MemoryHelper.Free((void*)values[sub]);
            values[sub] = 0;
        }

        if ((fields & ColdField.WidePayload) != 0 && BlockOrNull(_widePayload, index) is { } wide)
        {
            var values = (ulong[])wide;
            values[sub * 2] = 0;
            values[sub * 2 + 1] = 0;
        }

        // The pooled case is detected through IPooledResult and never through IDisposable: a user's
        // result type may itself implement IDisposable and must not be disposed by the task machinery.
        if ((fields & ColdField.Ref) != 0 && BlockOrNull(_ref, index) is { } refs)
        {
            var values = (object?[])refs;
            (values[sub] as IPooledResult)?.ReturnToPool();
            values[sub] = null;
        }

        if ((fields & ColdField.Exception) != 0) ClearObjectEntry(_exception, index);

        if ((fields & ColdField.CancellationToken) != 0 && BlockOrNull(_cancellationTokens, index) is { } tokens)
        {
            ((CancellationToken[])tokens)[sub] = default;
        }
        _fields[index] = 0;
    }

    // ---------------------------------------------------------------- block plumbing

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Array Block(ref Array?[]? table, int index, ColdField field)
    {
        var blocks = table;
        return (blocks is null ? null : Volatile.Read(ref blocks[index >> BlockBits]))
            ?? CreateBlock(ref table, index >> BlockBits, field);
    }

    // Only first touch allocates. The bag's lifetime gate prevents reclamation until all owners
    // finish; CAS here only arbitrates concurrent first writers to different entries in a block.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Array CreateBlock(ref Array?[]? table, int slot, ColdField field)
    {
        var blocks = table;
        if (blocks is null)
        {
            var freshTable = new Array?[BlockCount];
            blocks = Interlocked.CompareExchange(ref table, freshTable, null) ?? freshTable;
        }
        var existing = Volatile.Read(ref blocks[slot]);
        if (existing is not null) return existing;
        AnyBlockCreated = true;
        Array fresh = field switch
        {
            ColdField.Payload => new nint[BlockSize],
            ColdField.WidePayload => new ulong[BlockSize * 2],
            ColdField.CancellationToken => new CancellationToken[BlockSize],
            ColdField.Registration => new CancellationTokenRegistration[BlockSize],
            _ => new object?[BlockSize],
        };
        return Interlocked.CompareExchange(ref blocks[slot], fresh, null) ?? fresh;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Array? BlockOrNull(Array?[]? table, int index)
        => table is null ? null : Volatile.Read(ref table[index >> BlockBits]);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void ClearObjectEntry(Array?[]? table, int index)
    {
        if (BlockOrNull(table, index) is not { } block) return;

        ((object?[])block)[index & BlockMask] = null;
    }

    // ---------------------------------------------------------------- reclamation

    // Only called while LuminTaskBag owns its exclusive idle gate. Every entry was already
    // cleared by its consumer, so tables and blocks can all be dropped without a scan.
    internal void TrimEmptyBlocks()
    {
        _ref = null;
        _payload = null;
        _widePayload = null;
        _exception = null;
        _cancellationTokens = null;
        _registrations = null;
        _box = null;
    }
}

[Preserve]
public struct LuminTaskState
{
    // Everything a loop-driven source carries in the player-loop queue: one handle, the token to
    // check each frame, and the predicate/helper object. Per-frame scratch does NOT live here - it
    // goes into the slot's loop arena.
    public LuminTaskHandle Handle;
    public CancellationToken CancellationToken;
    public object? State;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public LuminTaskState(LuminTaskHandle handle, CancellationToken cancellationToken)
    {
        Handle = handle;
        CancellationToken = cancellationToken;
        State = null;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public LuminTaskState(LuminTaskHandle handle, CancellationToken cancellationToken, object? state)
    {
        Handle = handle;
        CancellationToken = cancellationToken;
        State = state;
    }
}
