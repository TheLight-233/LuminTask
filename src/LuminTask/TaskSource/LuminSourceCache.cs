using System;
using System.Runtime.CompilerServices;
using LuminThread.Utility;

namespace LuminThread.TaskSource;

// Non-generic, per-thread single-slot cache for the native task-source blocks. A core block's
// size varies by T, so the slot stores (pointer, size); on rent the block is reused only when it
// is at least as large as the request, otherwise it is freed and a fresh block is allocated.
// Because the slot is thread-local and shared across ALL closed T, resident memory is O(threads)
// instead of O(threads × T) — a task source block only lingers for the last type used on each
// thread. Renting from and returning to the slot remain allocation-free.
internal static unsafe class LuminSourceCache
{
    [ThreadStatic] private static void* s_block;
    [ThreadStatic] private static nuint s_blockSize;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void* Rent(nuint size)
    {
        var block = s_block;
        if (block != null)
        {
            if (s_blockSize >= size)
            {
                s_block = null;
                s_blockSize = 0;
                return block;
            }

            MemoryHelper.Free(block);
            s_block = null;
            s_blockSize = 0;
        }

        return MemoryHelper.Alloc(size);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void Return(void* block, nuint size)
    {
        if (s_block == null)
        {
            s_block = block;
            s_blockSize = size;
        }
        else
        {
            MemoryHelper.Free(block);
        }
    }
}