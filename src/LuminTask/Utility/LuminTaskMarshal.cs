using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace LuminThread.Utility;

public static class LuminTaskMarshal
{
    // Delegates to the segmented, non-moving store. The returned ref is stable for the slot's
    // lifetime even if the pool grows on another thread (chunks are never relocated), which
    // removes the dangling-ref-across-grow hazard the old single-array + Unsafe.Add had.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ref LuminTaskItem GetTaskItem(int index)
        => ref LuminTaskBag.GetItem(index);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ref T GetArrayDataReference<T>(T[] array)
    {
#if NET5_0_OR_GREATER
        return ref MemoryMarshal.GetArrayDataReference(array);
#else
        return ref MemoryMarshal.GetReference(array.AsSpan());
#endif
    }
}
