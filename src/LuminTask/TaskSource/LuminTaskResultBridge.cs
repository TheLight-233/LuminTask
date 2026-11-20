using System;
using System.Runtime.CompilerServices;
using System.Threading;
using LuminThread.Interface;
using LuminThread.Utility;

namespace LuminThread.TaskSource;

internal static class LuminTaskResultBridge
{
    // IL2CPP may walk struct fields to classify T. Cache the immutable answer once per result type.
    private static class Layout<T>
    {
        internal static readonly long Kind = RuntimeHelpers.IsReferenceOrContainsReferences<T>()
            ? (typeof(T).IsValueType ? LuminTaskSource.Cell : LuminTaskSource.Reference)
            : InlineKind(Unsafe.SizeOf<T>());
    }

    private static long InlineKind(int size) =>
        size <= 16 ? (long)size << LuminTaskSource.KindShift : LuminTaskSource.Cell;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static ulong Rent<T>()
    {
        long kind = Layout<T>.Kind;
        if (kind != LuminTaskSource.Cell) return SlotStore.Rent(kind, null);
        var cell = StatePool<T>.Create(default!);
        try { return SlotStore.Rent(kind, cell); }
        catch { cell.Dispose(); throw; }
    }

    internal static bool TrySetResult<T>(ulong handle, in T value)
    {
        long kind = Layout<T>.Kind;
        if (kind < LuminTaskSource.Reference)
            return TrySetInline(handle, kind, ref Unsafe.As<T, byte>(ref Unsafe.AsRef(in value)));
        return TrySetManaged(handle, in value);
    }

    // Keep IL2CPP's fully shared generics out of the slot protocol for common game values.
    private static bool TrySetInline(ulong handle, long kind, ref byte value)
    {
        var page = LuminTaskSource.Enter(handle, out int i, out long control, pendingOnly: true);
        if (page == null) return false;
        ref var slot = ref page.Slots[i];
        long prepared = control & LuminTaskSource.KindMask;
        if (prepared != 0 && prepared != kind)
        {
            Volatile.Write(ref slot.Control, control);
            throw new InvalidOperationException("Result storage does not match T.");
        }
        Unsafe.CopyBlockUnaligned(ref Unsafe.As<Inline16, byte>(ref slot.Data), ref value,
            (uint)(kind >> LuminTaskSource.KindShift));
        LuminTaskSource.Publish(ref slot, control | kind, LuminTaskStatus.Succeeded);
        return true;
    }

    private static bool TrySetManaged<T>(ulong handle, in T value)
    {
        var page = LuminTaskSource.Enter(handle, out int i, out long control, pendingOnly: true);
        if (page == null) return false;
        ref var slot = ref page.Slots[i];
        try { Store(ref slot, ref control, in value); }
        catch { Volatile.Write(ref slot.Control, control); throw; }
        LuminTaskSource.Publish(ref slot, control, LuminTaskStatus.Succeeded);
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void Store<T>(ref Slot slot, ref long control, in T value)
    {
        long kind = Layout<T>.Kind;
        long prepared = control & LuminTaskSource.KindMask;
        if (prepared != kind && (prepared != 0 || kind == LuminTaskSource.Cell))
            throw new InvalidOperationException("Result storage does not match T. Rent the source with Rent<T>.");
        if (kind == LuminTaskSource.Reference) slot.Payload = value;
        else if (kind == LuminTaskSource.Cell) ((StateTuple<T>)slot.Payload!).Item = value;
        else Unsafe.CopyBlockUnaligned(ref Unsafe.As<Inline16, byte>(ref slot.Data),
            ref Unsafe.As<T, byte>(ref Unsafe.AsRef(in value)), (uint)(kind >> LuminTaskSource.KindShift));
        control |= kind;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static T Read<T>(ref Slot slot)
    {
        long kind = Layout<T>.Kind;
        if ((slot.Control & LuminTaskSource.KindMask) != kind)
            throw new InvalidOperationException("Result storage does not match the requested type.");
        if (kind == LuminTaskSource.Reference) return (T)slot.Payload!;
        if (kind == LuminTaskSource.Cell) return ((StateTuple<T>)slot.Payload!).Item;
        T value = default!;
        Unsafe.CopyBlockUnaligned(ref Unsafe.As<T, byte>(ref value),
            ref Unsafe.As<Inline16, byte>(ref slot.Data), (uint)(kind >> LuminTaskSource.KindShift));
        return value;
    }

    internal static T Get<T>(ulong handle)
    {
        long kind = Layout<T>.Kind;
        if (kind >= LuminTaskSource.Reference) return GetManaged<T>(handle);
        T value = default!;
        GetInline(handle, kind, ref Unsafe.As<T, byte>(ref value));
        return value;
    }

    private static void GetInline(ulong handle, long kind, ref byte destination)
    {
        var page = LuminTaskSource.ClaimResult(handle, out int i, out long control);
        ref var slot = ref page.Slots[i];
        if ((control & LuminTaskSource.KindMask) != kind)
        {
            var cell = (control & LuminTaskSource.KindMask) == LuminTaskSource.Cell ? (IPooledResult?)slot.Payload : null;
            LuminTaskSource.ReleaseResult(page, i, handle);
            cell?.ReturnToPool();
            throw new InvalidOperationException("Result storage does not match the requested type.");
        }
        Unsafe.CopyBlockUnaligned(ref destination, ref Unsafe.As<Inline16, byte>(ref slot.Data),
            (uint)(kind >> LuminTaskSource.KindShift));
        LuminTaskSource.ReleaseResult(page, i, handle);
    }

    private static T GetManaged<T>(ulong handle)
    {
        var page = LuminTaskSource.ClaimResult(handle, out int i, out long control);
        ref var slot = ref page.Slots[i];
        var cell = (control & LuminTaskSource.KindMask) == LuminTaskSource.Cell ? (IPooledResult?)slot.Payload : null;
        try { return Read<T>(ref slot); }
        finally
        {
            LuminTaskSource.ReleaseResult(page, i, handle);
            cell?.ReturnToPool();
        }
    }

    internal static ulong RentState<T>(in T value)
    {
        ulong handle = Rent<T>();
        var page = SlotStore.Find(handle, out int i)!;
        ref var slot = ref page.Slots[i];
        long control = slot.Control;
        Store(ref slot, ref control, in value);
        return handle;
    }

    internal static bool TryReadState<T>(ulong handle, out T value)
    {
        var page = LuminTaskSource.Enter(handle, out int i, out long control, pendingOnly: true);
        if (page == null) { value = default!; return false; }
        ref var slot = ref page.Slots[i];
        try { value = Read<T>(ref slot); return true; }
        finally { Volatile.Write(ref slot.Control, control); }
    }
}
