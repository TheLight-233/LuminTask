using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using LuminThread.Interface;
using LuminThread.Utility;

namespace LuminThread.TaskSource.Promise;

// Timed wait. The two timestamps that span the frames borrow the item's own LoopState scratch
// instead of a separate native block.
internal static class DelayPromise
{
    internal static readonly MoveNext MoveNextAction = MoveNext;

    private static unsafe bool MoveNext(in LuminTaskState state)
    {
        ref var item = ref LuminTaskCoreSource.TryResolve(state.Handle, out var cold, out int index);

        // The slot was already consumed and recycled: drop this item.
        if (Unsafe.IsNullRef(ref item))
            return false;

        if (state.CancellationToken.IsCancellationRequested)
        {
            LuminTaskCoreSource.TrySetCanceled(state.Handle, state.CancellationToken);
            return false;
        }

        nint scratch = cold.PayloadAt(index);

        // Released already (the slot was consumed): drop the item.
        if (scratch == 0)
            return false;

        long startTimestamp = UnsafeMemory.Read<long>((void*)scratch);
        long delayTicks = UnsafeMemory.Read<long>((byte*)scratch + sizeof(long));

        if (Stopwatch.GetTimestamp() - startTimestamp < delayTicks)
            return true;

        LuminTaskCoreSource.TrySetResult(state.Handle);
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static long MillisecondsToStopwatchTicks(int milliseconds)
        => milliseconds == System.Threading.Timeout.Infinite
            ? long.MaxValue
            : (long)(milliseconds * ((double)Stopwatch.Frequency / 1000D));
}