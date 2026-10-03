namespace LuminThread;

// Task handle bit layout, plus the bit accessors.
//
// The handle is a bare 64-bit value on purpose - see LuminTask.Global.cs, where
// `global using LuminTaskHandle = ulong;` gives that value a name without introducing a wrapper
// type. Keeping it an alias means LuminTask stays an 8-byte struct and every call site passes a
// plain value, while the code still reads as "handle" rather than "ulong".
//
//   bits  0..15   slot index + 1      (0 is reserved for a synchronously-completed task)
//   bits 16..47   slot generation     (bumped on every recycle, so a stale handle fails fast)
//
// There is deliberately no source-kind field. Every source kind runs the same lifecycle through
// LuminTaskCoreSource - status query, continuation registration, completion and release are all
// kind-independent - and the two things that used to need per-kind code now live in the slot
// itself: the cancellation registration and the per-frame scratch of loop-driven sources. The only
// remaining per-source piece is the per-frame step function, and that travels with the player-loop
// item, not with the task.
//
// Two properties make the handle worth its bit juggling:
//
//   * Recycling safety. A slot can be reused by a different task, but an old handle carries the old
//     generation, so every operation rejects it before touching the slot. Pointer-based handles
//     could dereference freed memory; this cannot.
//
//   * Single releaser. A producer that fires a completion holds nothing but this 64-bit value, so
//     once the consumer releases the slot inside the inlined continuation there is no pointer left
//     for the producer to misuse.
//
// The bit accessors live on a separate type because LuminTaskHandle is a using alias for ulong
// (see LuminTask.Global.cs) and a type cannot share its name with that alias.
internal static class LuminTaskHandleBits
{
    internal const int SlotBits = 16;
    internal const int GenerationShift = SlotBits;

    internal const ulong SlotMask = (1UL << SlotBits) - 1;

    internal static LuminTaskHandle Create(int id, uint generation)
    {
        // The slot field is offset by one so that a zero handle means "no slot".
        ulong slotBits = (uint)id + 1UL;

        return ((ulong)generation << GenerationShift) | slotBits;
    }

    internal static bool IsNull(this LuminTaskHandle handle) => handle == 0;

    internal static uint SlotRaw(this LuminTaskHandle handle) => (uint)(handle & SlotMask);

    internal static uint Generation(this LuminTaskHandle handle) => (uint)(handle >> GenerationShift);
}