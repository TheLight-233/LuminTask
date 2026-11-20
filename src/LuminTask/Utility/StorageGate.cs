using System;
using System.Runtime.CompilerServices;
using System.Threading;

namespace LuminThread.Utility;

// Short non-recursive critical sections in the slot allocator and idle cache.
// Keep the contended yielding path out of the single-threaded player loop.
internal sealed class StorageGate : IDisposable
{
    private int _held;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal StorageGate Enter()
    {
        if (Interlocked.CompareExchange(ref _held, 1, 0) != 0) Wait();
        return this;
    }

    private void Wait()
    {
        var spin = new SpinWait();
        do { spin.SpinOnce(); }
        while (Volatile.Read(ref _held) != 0 || Interlocked.CompareExchange(ref _held, 1, 0) != 0);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Dispose() => Volatile.Write(ref _held, 0);
}
