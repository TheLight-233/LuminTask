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
public sealed class LuminTaskCold
{
    // Scratch size a loop-driven source may reserve (currently two timestamps for Delay).
    internal const int LoopStateSize = 16;

    // Entries per cold block. This is the allocation granularity of every cold field.
    internal const int BlockBits = 6;
    internal const int BlockSize = 1 << BlockBits;
    internal const int BlockMask = BlockSize - 1;
    private const int BlockCount = LuminTaskBag.CHUNK_SIZE >> BlockBits;

    // Reference results, and the pooled container of a reference-containing value result.
    private object?[]?[]? _ref;

    // Unmanaged results and loop scratch: a MemoryHelper block whose size is decided by the writer.
    // A slot never uses both, so one pointer per slot covers both roles.
    private nint[]?[]? _payload;

    private object?[]?[]? _exception;
    private CancellationToken[]?[]? _cancellationTokens;
    private CancellationTokenRegistration[]?[]? _registrations;

    // ---------------------------------------------------------------- writers (create on demand)

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void SetRef(int index, object? value)
        => RefBlock(index)[index & BlockMask] = value;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void SetPayload(int index, nint value)
        => PayloadBlock(index)[index & BlockMask] = value;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void SetException(int index, object? value)
        => ExceptionBlock(index)[index & BlockMask] = value;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void SetCancellationToken(int index, in CancellationToken value)
        => CancellationTokenBlock(index)[index & BlockMask] = value;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void SetRegistration(int index, in CancellationTokenRegistration value)
        => RegistrationBlock(index)[index & BlockMask] = value;

    // ---------------------------------------------------------------- non-allocating readers

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal object? RefAt(int index)
    {
        var block = RefBlockOrNull(index);
        return block is null ? null : block[index & BlockMask];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal nint PayloadAt(int index)
    {
        var block = PayloadBlockOrNull(index);
        return block is null ? 0 : block[index & BlockMask];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal object? ExceptionAt(int index)
    {
        var block = ExceptionBlockOrNull(index);
        return block is null ? null : block[index & BlockMask];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal CancellationToken CancellationTokenAt(int index)
    {
        var block = CancellationTokenBlockOrNull(index);
        return block is null ? default : block[index & BlockMask];
    }

    // ---------------------------------------------------------------- release

    /// <summary>
    /// Drops everything the slot at <paramref name="index"/> owns: the pending cancellation
    /// registration, the unmanaged result/scratch block, a pooled result container, the recorded
    /// exception and token. Never allocates — a field whose block was never faulted has nothing to
    /// release.
    /// </summary>
    internal unsafe void ReleaseSlot(int index)
    {
        int sub = index & BlockMask;

        // Pending registration first: Release is non-blocking and safe even when the caller is the
        // cancellation callback itself.
        if (RegistrationBlockOrNull(index) is { } registrations)
        {
            registrations[sub].Release();
            registrations[sub] = default;
        }

        if (PayloadBlockOrNull(index) is { } payload && payload[sub] != 0)
        {
            MemoryHelper.Free((void*)payload[sub]);
            payload[sub] = 0;
        }

        // The pooled case is detected through IPooledResult and never through IDisposable: a user's
        // result type may itself implement IDisposable and must not be disposed by the task machinery.
        if (RefBlockOrNull(index) is { } refs)
        {
            (refs[sub] as IPooledResult)?.ReturnToPool();
            refs[sub] = null;
        }

        if (ExceptionBlockOrNull(index) is { } exceptions)
            exceptions[sub] = null;

        if (CancellationTokenBlockOrNull(index) is { } tokens)
            tokens[sub] = default;
    }

    // ---------------------------------------------------------------- block plumbing

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private object?[] RefBlock(int index)
    {
        var blocks = _ref;
        if (blocks is null)
            Volatile.Write(ref _ref, blocks = new object?[]?[BlockCount]);

        int slot = index >> BlockBits;
        var block = Volatile.Read(ref blocks[slot]);
        if (block is null)
            Volatile.Write(ref blocks[slot], block = new object?[BlockSize]);
        return block;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private object?[]? RefBlockOrNull(int index)
        => _ref is null ? null : Volatile.Read(ref _ref[index >> BlockBits]);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private nint[] PayloadBlock(int index)
    {
        var blocks = _payload;
        if (blocks is null)
            Volatile.Write(ref _payload, blocks = new nint[]?[BlockCount]);

        int slot = index >> BlockBits;
        var block = Volatile.Read(ref blocks[slot]);
        if (block is null)
            Volatile.Write(ref blocks[slot], block = new nint[BlockSize]);
        return block;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private nint[]? PayloadBlockOrNull(int index)
        => _payload is null ? null : Volatile.Read(ref _payload[index >> BlockBits]);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private object?[] ExceptionBlock(int index)
    {
        var blocks = _exception;
        if (blocks is null)
            Volatile.Write(ref _exception, blocks = new object?[]?[BlockCount]);

        int slot = index >> BlockBits;
        var block = Volatile.Read(ref blocks[slot]);
        if (block is null)
            Volatile.Write(ref blocks[slot], block = new object?[BlockSize]);
        return block;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private object?[]? ExceptionBlockOrNull(int index)
        => _exception is null ? null : Volatile.Read(ref _exception[index >> BlockBits]);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private CancellationToken[] CancellationTokenBlock(int index)
    {
        var blocks = _cancellationTokens;
        if (blocks is null)
            Volatile.Write(ref _cancellationTokens, blocks = new CancellationToken[]?[BlockCount]);

        int slot = index >> BlockBits;
        var block = Volatile.Read(ref blocks[slot]);
        if (block is null)
            Volatile.Write(ref blocks[slot], block = new CancellationToken[BlockSize]);
        return block;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private CancellationToken[]? CancellationTokenBlockOrNull(int index)
        => _cancellationTokens is null ? null : Volatile.Read(ref _cancellationTokens[index >> BlockBits]);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private CancellationTokenRegistration[] RegistrationBlock(int index)
    {
        var blocks = _registrations;
        if (blocks is null)
            Volatile.Write(ref _registrations, blocks = new CancellationTokenRegistration[]?[BlockCount]);

        int slot = index >> BlockBits;
        var block = Volatile.Read(ref blocks[slot]);
        if (block is null)
            Volatile.Write(ref blocks[slot], block = new CancellationTokenRegistration[BlockSize]);
        return block;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private CancellationTokenRegistration[]? RegistrationBlockOrNull(int index)
        => _registrations is null ? null : Volatile.Read(ref _registrations[index >> BlockBits]);
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