using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace LuminThread.Utility;

public static class LuminTaskMarshal
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ref LuminTaskItem GetTaskItem(int index)
    {
#if NET5_0_OR_GREATER
        return ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(LuminTaskBag.TaskBag), (nint)(uint)index);
#else
        return ref Unsafe.Add(ref LuminTaskBag.TaskBag.AsSpan().GetPinnableReference(), (nint)(uint)index);
#endif
    }

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