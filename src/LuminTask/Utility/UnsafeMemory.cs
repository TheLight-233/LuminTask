using System;
using System.Runtime.CompilerServices;

namespace LuminThread.Utility;

// Alignment-free reads and writes of unmanaged values.
//
// Unsafe.ReadUnaligned / WriteUnaligned are only usable where the backend supports a wide unaligned
// load. IL2CPP rejects them when T is wider than a machine word, so a 32-bit player (ARMv7, x86)
// would throw the moment a result is, say, a long or a Vector3. On those targets the copy goes
// through Unsafe.CopyBlock, which is a plain memcpy and has no width restriction.
//
// Both methods are only valid for unmanaged T: the 32-bit path copies raw bytes, so a value type
// that contains references must never reach here (LuminTaskResultBridge routes those through the
// pooled container path instead).
internal static unsafe class UnsafeMemory
{
    // Evaluated once per process. On 64-bit targets the intrinsic path is taken; there is no JIT
    // flag to fold this away, but the branch is perfectly predicted and only sits on the result path.
    private static readonly bool UseUnalignedIntrinsics = IntPtr.Size >= 8;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static T Read<T>(void* source)
    {
        if (UseUnalignedIntrinsics)
            return Unsafe.ReadUnaligned<T>(source);

        T value = default!;
        Unsafe.CopyBlock(Unsafe.AsPointer(ref value), source, (uint)Unsafe.SizeOf<T>());
        return value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void Write<T>(void* destination, in T value)
    {
        if (UseUnalignedIntrinsics)
        {
            Unsafe.WriteUnaligned(destination, value);
            return;
        }

        Unsafe.CopyBlock(destination, Unsafe.AsPointer(ref Unsafe.AsRef(in value)), (uint)Unsafe.SizeOf<T>());
    }
}