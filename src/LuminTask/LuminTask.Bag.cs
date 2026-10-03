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
    // The result bytes and the per-frame scratch live in two byte arenas (LuminTaskCold.ResultArena
    // / LoopArena) rather than inside the item: reserving 64 + 16 bytes in every slot made the item
    // about four times larger than the fields that are actually on the fast path.
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
    //  _freeStack : packed Treiber-stack head = ((long)version << 32) | (uint)(topId + 1).
    //               The low half is 0 when the stack is empty; the high half is an ABA version tag.
    //               A `long` field cannot be declared `volatile` in C#, so it is only ever touched
    //               through Interlocked / Volatile.Read.
    //  _freeNextChunks : the free-list link of each slot, held as one int[CHUNK_SIZE] per chunk and
    //               allocated together with the chunk. A flat int[MAX_SLOTS] would reserve ~128 KB
    //               up front for a pool that normally never leaves its first chunk (256 slots), so
    //               the cost now tracks the chunks that actually exist.
    //  _highWater : the next id that has never been allocated; advanced only when the stack is empty.
    //  _liveCount : number of currently-rented ids (diagnostics only).
    private static readonly int[]?[] _freeNextChunks = new int[]?[MAX_CHUNKS];
    private static long _freeStack;
    private static int _highWater;
    private static int _liveCount;

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
    /// Returns the cold side (lazily allocated per-field arrays and the two byte arenas) of the
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
            int topPlus1 = (int)(uint)head;
            if (topPlus1 == 0)
            {
                id = -1;
                return false;
            }

            int topId = topPlus1 - 1;
            int next = _freeNextChunks[topId >> CHUNK_BITS]![topId & CHUNK_MASK]; // link below the top
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
            _freeNextChunks[id >> CHUNK_BITS]![id & CHUNK_MASK] = topPlus1 - 1; // link above the top
            uint version = (uint)(head >> 32);
            long desired = ((long)(version + 1) << 32) | (uint)(id + 1);

            long observed = Interlocked.CompareExchange(ref _freeStack, desired, head);
            if (observed == head)
            {
                if (Interlocked.Decrement(ref _liveCount) == 0)
                {
                    Volatile.Write(ref _idleSinceTimestamp, Environment.TickCount);
                    ArmColdTrim();
                }
                return;
            }
            head = observed;                             // contended: retry with the observed head
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
        if (Volatile.Read(ref _liveCount) != 0) return;

        int idleFor = unchecked(Environment.TickCount - Volatile.Read(ref _idleSinceTimestamp));
        if (idleFor < IDLE_COLD_TRIM_MILLISECONDS)
        {
            // 定时器早于“当前静默窗口”到期（可能来自上一轮挂载）：补挂剩余时间，
            // 否则这个静默窗口既不会回收也不会再挂，块就永远留着了。
            ArmColdTrim(Math.Max(1, IDLE_COLD_TRIM_MILLISECONDS - idleFor));
            return;
        }

        int chunkCount = _capacity >> CHUNK_BITS;
        for (int chunk = 0; chunk < chunkCount; chunk++)
            Volatile.Read(ref _cold[chunk])?.TrimEmptyBlocks();
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
        Volatile.Write(ref _freeNextChunks[chunkIndex], new int[CHUNK_SIZE]);
        Volatile.Write(ref _cold[chunkIndex], new LuminTaskCold());
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
// There is deliberately no result byte arena. Bytes reserved per slot are exactly the "always
// resident" memory this design avoids: an unmanaged result is allocated with MemoryHelper at the
// size the value actually needs and freed when the slot is consumed, so a slot costs nothing until
// it produces something.
//
// RECLAMATION: an empty block can be dropped (table entry -> null) without a lock, because every
// non-default write BOOKS an entry on the block's counter first. The reclaimer only wins the CAS
// 0 -> Retired; a writer that loses that race sees the retired marker and re-publishes a fresh
// block, so no value can ever land in a discarded array. Blocks are trimmed lazily by
// LuminTaskBag only after the whole bag has been idle for a while - see TrimEmptyBlocks.
public sealed class LuminTaskCold
{
    // Scratch size a loop-driven source may reserve (currently two timestamps for Delay).
    internal const int LoopStateSize = 16;

    // Entries per cold block. This is the allocation granularity of every cold field.
    internal const int BlockBits = 6;
    internal const int BlockSize = 1 << BlockBits;
    internal const int BlockMask = BlockSize - 1;
    private const int BlockCount = LuminTaskBag.CHUNK_SIZE >> BlockBits;

    // 冷字段种类：只用于“惰性建块”时选择数组类型，不参与索引。
    private enum ColdField : byte
    {
        Ref,
        Payload,
        Exception,
        CancellationToken,
        Registration,
        Box,
    }

    // 一个冷字段的 64 项块。数组与它的计数打包在同一个引用里发布，任何读者/写者看到的
    // (数组, 计数) 都成对。
    //
    // Count 语义：>= 0 为该块中的非默认条目数；Retired(-1) 表示该块正在被回收。
    // 写者写非默认值前必须先用 CAS 预订一个条目位；回收者只有 CAS 0 -> Retired 成功才允许
    // 摘除表项。竞争细节见 TrimEmptyBlocks / Book。
    private sealed class ColdBlock
    {
        internal const int Retired = -1;

        internal readonly Array Values;
        internal int Count;

        internal ColdBlock(Array values) => Values = values;
    }

    // 是否已经创建过任何冷块（从未触及冷侧的负载不必挂回收定时器）。
    internal static volatile bool AnyBlockCreated;

    // Reference results, and the pooled container of a reference-containing value result.
    private ColdBlock?[]? _ref;

    // Unmanaged results and loop scratch: a MemoryHelper block whose size is decided by the writer.
    // A slot never uses both, so one pointer per slot covers both roles.
    private ColdBlock?[]? _payload;

    private ColdBlock?[]? _exception;
    private ColdBlock?[]? _cancellationTokens;
    private ColdBlock?[]? _registrations;

    // State machine box of a suspended async method. It lives here rather than in the builder struct
    // because a LuminTask is only a handle: the builder reaches the box back through the slot to
    // re-register its continuation and to return the box to its pool on completion. The reference is
    // dropped - never returned - on slot release, because the state machine inside the box may still
    // be executing when the consumer releases the slot.
    private ColdBlock?[]? _box;

    // ---------------------------------------------------------------- writers (create on demand)
    //
    // 写者只在“条目由默认变非默认”时预订计数位（见 ColdBlock/Book）。同一个条目只由它所属
    // slot 的属主串行写入（完成会合提供了跨线程先后关系），所以读到的前值可信。

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void SetRef(int index, object? value)
    {
        int slot = index >> BlockBits, sub = index & BlockMask;
        var table = Table(ref _ref);
        var block = Volatile.Read(ref table[slot]);
        var values = block is null ? null : (object?[])block.Values;
        bool had = values is not null && values[sub] is not null;
        bool has = value is not null;

        if (!has && !had) return;
        if (has && !had) block = Book(table, slot, ColdField.Ref);
        else if (had && !has) Interlocked.Decrement(ref block!.Count);

        ((object?[])block!.Values)[sub] = value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void SetBox(int index, object box)
    {
        int slot = index >> BlockBits, sub = index & BlockMask;
        var table = Table(ref _box);
        var block = Volatile.Read(ref table[slot]);
        bool had = block is not null && ((object?[])block.Values)[sub] is not null;

        if (!had) block = Book(table, slot, ColdField.Box);

        ((object?[])block!.Values)[sub] = box;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void SetPayload(int index, nint value)
    {
        int slot = index >> BlockBits, sub = index & BlockMask;
        var table = Table(ref _payload);
        var block = Volatile.Read(ref table[slot]);
        bool had = block is not null && ((nint[])block.Values)[sub] != 0;
        bool has = value != 0;

        if (!has && !had) return;
        if (has && !had) block = Book(table, slot, ColdField.Payload);
        else if (had && !has) Interlocked.Decrement(ref block!.Count);

        ((nint[])block!.Values)[sub] = value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void SetException(int index, object? value)
    {
        int slot = index >> BlockBits, sub = index & BlockMask;
        var table = Table(ref _exception);
        var block = Volatile.Read(ref table[slot]);
        var values = block is null ? null : (object?[])block.Values;
        bool had = values is not null && values[sub] is not null;
        bool has = value is not null;

        if (!has && !had) return;
        if (has && !had) block = Book(table, slot, ColdField.Exception);
        else if (had && !has) Interlocked.Decrement(ref block!.Count);

        ((object?[])block!.Values)[sub] = value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void SetCancellationToken(int index, in CancellationToken value)
    {
        int slot = index >> BlockBits, sub = index & BlockMask;
        var table = Table(ref _cancellationTokens);
        var block = Volatile.Read(ref table[slot]);
        bool had = block is not null && !((CancellationToken[])block.Values)[sub].Equals(default(CancellationToken));
        bool has = !value.Equals(default(CancellationToken));

        if (!has && !had) return;
        if (has && !had) block = Book(table, slot, ColdField.CancellationToken);
        else if (had && !has) Interlocked.Decrement(ref block!.Count);

        ((CancellationToken[])block!.Values)[sub] = value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void SetRegistration(int index, in CancellationTokenRegistration value)
    {
        int slot = index >> BlockBits, sub = index & BlockMask;
        var table = Table(ref _registrations);
        var block = Volatile.Read(ref table[slot]);
        bool had = block is not null
                   && !((CancellationTokenRegistration[])block.Values)[sub].Equals(default(CancellationTokenRegistration));
        bool has = !value.Equals(default(CancellationTokenRegistration));

        if (!has && !had) return;
        if (has && !had) block = Book(table, slot, ColdField.Registration);
        else if (had && !has) Interlocked.Decrement(ref block!.Count);

        ((CancellationTokenRegistration[])block!.Values)[sub] = value;
    }

    // ---------------------------------------------------------------- non-allocating readers

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal object? RefAt(int index)
    {
        var block = BlockOrNull(_ref, index);
        return block is null ? null : ((object?[])block.Values)[index & BlockMask];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal object? BoxAt(int index)
    {
        var block = BlockOrNull(_box, index);
        return block is null ? null : ((object?[])block.Values)[index & BlockMask];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal nint PayloadAt(int index)
    {
        var block = BlockOrNull(_payload, index);
        return block is null ? 0 : ((nint[])block.Values)[index & BlockMask];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal object? ExceptionAt(int index)
    {
        var block = BlockOrNull(_exception, index);
        return block is null ? null : ((object?[])block.Values)[index & BlockMask];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal CancellationToken CancellationTokenAt(int index)
    {
        var block = BlockOrNull(_cancellationTokens, index);
        return block is null ? default : ((CancellationToken[])block.Values)[index & BlockMask];
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
        int sub = index & BlockMask;

        // Pending registration first: Release is non-blocking and safe even when the caller is the
        // cancellation callback itself.
        if (BlockOrNull(_registrations, index) is { } registrations)
        {
            var values = (CancellationTokenRegistration[])registrations.Values;
            bool had = !values[sub].Equals(default(CancellationTokenRegistration));
            values[sub].Release();
            values[sub] = default;
            if (had) Interlocked.Decrement(ref registrations.Count);
        }

        ClearObjectEntry(_box, index);

        if (BlockOrNull(_payload, index) is { } payload)
        {
            var values = (nint[])payload.Values;
            if (values[sub] != 0)
            {
                MemoryHelper.Free((void*)values[sub]);
                values[sub] = 0;
                Interlocked.Decrement(ref payload.Count);
            }
        }

        // The pooled case is detected through IPooledResult and never through IDisposable: a user's
        // result type may itself implement IDisposable and must not be disposed by the task machinery.
        if (BlockOrNull(_ref, index) is { } refs)
        {
            var values = (object?[])refs.Values;
            if (values[sub] is not null)
            {
                (values[sub] as IPooledResult)?.ReturnToPool();
                values[sub] = null;
                Interlocked.Decrement(ref refs.Count);
            }
        }

        ClearObjectEntry(_exception, index);

        if (BlockOrNull(_cancellationTokens, index) is { } tokens)
        {
            var values = (CancellationToken[])tokens.Values;
            if (!values[sub].Equals(default(CancellationToken)))
            {
                values[sub] = default;
                Interlocked.Decrement(ref tokens.Count);
            }
        }
    }

    // ---------------------------------------------------------------- block plumbing

    // 惰性创建字段的块表。CAS 发布：两个写者同时创建时不会互相覆盖（旧实现是普通写，
    // 落败者的值会写进被覆盖掉的数组里）。
    private static ColdBlock?[] Table(ref ColdBlock?[]? table)
    {
        var blocks = table;
        if (blocks is not null) return blocks;

        var fresh = new ColdBlock?[BlockCount];
        return Interlocked.CompareExchange(ref table, fresh, null) ?? fresh;
    }

    // 取块并在需要时预订一个非默认条目位。若现有块已在回收中，改成把表项换上新块；
    // 任何情况下返回的块都是“已预订、可安全写入”的。
    private static ColdBlock Book(ColdBlock?[] table, int slot, ColdField field)
    {
        for (;;)
        {
            var block = Volatile.Read(ref table[slot]) ?? PublishBlock(table, slot, field);

            if (TryBook(block)) return block;

            // 该块已被回收者标记退役：换新块。CAS 失败说明表项已被别人动过，重读重试。
            var replacement = NewBlock(field);
            Volatile.Write(ref replacement.Count, 1);   // 预订位随发布一起生效
            if (Interlocked.CompareExchange(ref table[slot], replacement, block) is null) return replacement;
        }
    }

    private static ColdBlock PublishBlock(ColdBlock?[] table, int slot, ColdField field)
    {
        var fresh = NewBlock(field);
        return Interlocked.CompareExchange(ref table[slot], fresh, null) ?? fresh;
    }

    // 预订一个非默认条目位。CAS 循环与回收者的 0 -> Retired 竞争：赢的一方决定该块是继续
    // 使用还是被摘除。
    private static bool TryBook(ColdBlock block)
    {
        int count = Volatile.Read(ref block.Count);
        while (count >= 0)
        {
            if (Interlocked.CompareExchange(ref block.Count, count + 1, count) == count) return true;
            count = Volatile.Read(ref block.Count);
        }
        return false;
    }

    private static ColdBlock NewBlock(ColdField field)
    {
        AnyBlockCreated = true;
        return field switch
        {
            ColdField.Payload => new ColdBlock(new nint[BlockSize]),
            ColdField.CancellationToken => new ColdBlock(new CancellationToken[BlockSize]),
            ColdField.Registration => new ColdBlock(new CancellationTokenRegistration[BlockSize]),
            _ => new ColdBlock(new object?[BlockSize]),
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ColdBlock? BlockOrNull(ColdBlock?[]? table, int index)
        => table is null ? null : Volatile.Read(ref table[index >> BlockBits]);

    // 清掉一个对象型条目（box / exception）：只在原来确实有值时递减计数。
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void ClearObjectEntry(ColdBlock?[]? table, int index)
    {
        if (BlockOrNull(table, index) is not { } block) return;

        int sub = index & BlockMask;
        var values = (object?[])block.Values;
        if (values[sub] is null) return;

        values[sub] = null;
        Interlocked.Decrement(ref block.Count);
    }

    // ---------------------------------------------------------------- reclamation

    /// <summary>
    /// 回收所有当前为空的块。只由 LuminTaskBag 的静默回收调用：计数保证安全——并发写者要么
    /// 已经预订了条目（回收者的 CAS 失败），要么会在发现退役标记后换成新块（见 Book）。
    /// </summary>
    internal void TrimEmptyBlocks()
    {
        TrimTable(_ref);
        TrimTable(_payload);
        TrimTable(_exception);
        TrimTable(_cancellationTokens);
        TrimTable(_registrations);
        TrimTable(_box);
    }

    private static void TrimTable(ColdBlock?[]? table)
    {
        if (table is null) return;

        for (int slot = 0; slot < BlockCount; slot++)
        {
            var block = Volatile.Read(ref table[slot]);
            if (block is null) continue;
            if (Interlocked.CompareExchange(ref block.Count, ColdBlock.Retired, 0) != 0) continue;

            Interlocked.CompareExchange(ref table[slot], null, block);
        }
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