using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace LuminThread.Utility;

public static unsafe class MemoryHelper
{
    static delegate*<nuint, void*> AllocFunc;
    
    static delegate*<void*, void> FreeFunc;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Set(delegate*<nuint, void*> allocFunc, delegate*<void*, void> freeFunc)
    {
        AllocFunc = allocFunc;
        FreeFunc = freeFunc;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void* Alloc(nuint size)
    {
        if (AllocFunc is not null)
            return AllocFunc(size);
        
#if NET5_0_OR_GREATER
        return NativeMemory.Alloc(size);
#else
        return Marshal.AllocHGlobal((nint)size).ToPointer();
#endif
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Free(void* ptr)
    {
        if (FreeFunc is not null)
            FreeFunc(ptr);
        
#if NET5_0_OR_GREATER
        NativeMemory.Free(ptr);
#else   
        Marshal.FreeHGlobal(new IntPtr(ptr));
#endif
    }
}