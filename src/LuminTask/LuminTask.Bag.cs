using System.Runtime.CompilerServices;
using System;
using System.Threading;
using LuminThread.Utility;

namespace LuminThread;

public static class LuminTaskBag
{
    public static int Capacity => SlotStore.Capacity;
    public static int GetLiveCount() => SlotStore.Live;
    public static int GetAvailableCount() => SlotStore.Available;
    public static void PrintAllTasksStatus() =>
        Console.WriteLine($"Capacity: {Capacity}, live: {GetLiveCount()}, available: {GetAvailableCount()}");
}

internal struct Inline16
{
    internal long First, Second;
}

internal struct Slot
{
    internal long Control;
    internal Inline16 Data;
    internal Action<object>? Continuation;
    internal object? ContinuationState, Payload, ProducerState;
}

internal static class SlotStore
{
    internal const int PageSize = 64;
    private const int MaxSlots = short.MaxValue;
    private const int MaxPages = (MaxSlots + PageSize - 1) / PageSize;
    private const long LastGeneration = (1L << 48) - 1;

    internal sealed class Page
    {
        internal readonly Slot[] Slots;
        internal readonly int Number;
        internal int NextVirgin, Used, Free = -1;
        internal Page? Next;

        internal Page(int number)
        {
            Number = number;
            Slots = new Slot[Math.Min(PageSize, MaxSlots - number * PageSize)];
        }
    }

    // The generation survives detaching a completely empty page.
    private struct Entry
    {
        internal Page? Page;
        internal long Generation;
    }

    private static readonly StorageGate Gate = new();
    private static Entry[] _directory = Array.Empty<Entry>();
    private static Page? _available;
    private static int _capacity, _live, _unavailable, _peakLive;
    internal static int Capacity => Volatile.Read(ref _capacity);
    internal static int Live => Volatile.Read(ref _live);
    internal static int Available { get { using (Gate.Enter()) return _capacity - _live - _unavailable; } }

    internal static void Snapshot(out int live, out int capacity, out int peak, out int directoryLength)
    {
        using (Gate.Enter())
        {
            live = _live;
            capacity = _capacity;
            peak = _peakLive;
            directoryLength = _directory.Length;
        }
    }

    internal static ulong Rent(long flags, object? payload)
    {
        ulong handle;
        using (Gate.Enter())
        {
            var page = _available ?? AddPage();
            int offset = page.Free;
            if (offset < 0) offset = page.NextVirgin++;
            else page.Free = (int)page.Slots[offset].Data.First;
            ref var slot = ref page.Slots[offset];
            ref var entry = ref _directory[page.Number];
            long generation = ++entry.Generation;
            page.Used++;
            if (++_live > _peakLive) _peakLive = _live;
            if (page.Used == page.Slots.Length || generation == LastGeneration)
            {
                _available = page.Next;
                page.Next = null;
                if (generation == LastGeneration) _unavailable += page.Slots.Length - page.Used;
            }
            slot.Data = default;
            slot.Payload = payload;
            handle = ((ulong)generation << 16) | (uint)(page.Number * PageSize + offset + 1);
            Volatile.Write(ref slot.Control, (generation << 16) | LuminTaskSource.Active | flags);
        }
        LuminTaskLeakTracker.OnRent(handle);
        return handle;
    }

    private static Page AddPage()
    {
        int number = 0;
        while (number < _directory.Length && (_directory[number].Page != null ||
               _directory[number].Generation == LastGeneration)) number++;
        if (number == MaxPages) LuminTaskExceptionHelper.ThrowTaskItemExhausted();
        if (number == _directory.Length)
        {
            var larger = new Entry[Math.Min(MaxPages, Math.Max(1, number * 2))];
            Array.Copy(_directory, larger, number);
            Volatile.Write(ref _directory, larger);
        }
        var page = new Page(number);
        Volatile.Write(ref _directory[number].Page, page);
        _capacity += page.Slots.Length;
        return _available = page;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static Page? Find(ulong handle, out int offset)
    {
        int index = (ushort)handle - 1;
        offset = index & (PageSize - 1);
        var directory = Volatile.Read(ref _directory);
        int number = index >> 6;
        if ((uint)number >= (uint)directory.Length || index >= MaxSlots) return null;
        return Volatile.Read(ref directory[number].Page);
    }

    internal static void Return(Page page, int offset, ulong handle)
    {
        LuminTaskLeakTracker.OnReturn(handle);
        using (Gate.Enter())
        {
            if (_directory[page.Number].Generation == LastGeneration) _unavailable++;
            else if (page.Used == page.Slots.Length)
            {
                page.Next = _available;
                _available = page;
            }
            page.Slots[offset].Data.First = page.Free;
            page.Free = offset;
            page.Used--;
            _live--;
        }
    }

    internal static void Trim()
    {
        using (Gate.Enter())
        {
            _available = null;
            for (int i = _directory.Length - 1; i >= 0; i--)
            {
                ref var entry = ref _directory[i];
                var page = entry.Page;
                if (page == null) continue;
                page.Next = null;
                if (page.Used == 0 && (i != 0 || entry.Generation == LastGeneration))
                {
                    Volatile.Write(ref entry.Page, null);
                    _capacity -= page.Slots.Length;
                    if (entry.Generation == LastGeneration) _unavailable -= page.Slots.Length;
                }
                else if (page.Used < page.Slots.Length && entry.Generation != LastGeneration)
                {
                    page.Next = _available;
                    _available = page;
                }
            }
        }
    }
}

[Preserve]
public struct LuminTaskState
{
    // Everything a loop-driven source carries in the player-loop queue: one handle, the token to
    // check each frame, and the predicate/helper object. Per-frame scratch does NOT live here - it
    // lives in the slot's inline fields.
    public LuminTaskHandle Handle;
    public CancellationToken CancellationToken;
    public object? State;
    internal Action<object>? OnShutdown;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public LuminTaskState(LuminTaskHandle handle, CancellationToken cancellationToken)
    {
        Handle = handle;
        CancellationToken = cancellationToken;
        State = null;
        OnShutdown = null;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public LuminTaskState(LuminTaskHandle handle, CancellationToken cancellationToken, object? state)
    {
        Handle = handle;
        CancellationToken = cancellationToken;
        State = state;
        OnShutdown = null;
    }
}
