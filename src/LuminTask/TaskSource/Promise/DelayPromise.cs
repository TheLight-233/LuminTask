using System;
using System.Diagnostics;
using System.Threading;
using System.Runtime.CompilerServices;
using LuminThread.Interface;
using LuminThread.Utility;

namespace LuminThread.TaskSource.Promise;

// Start and duration share the slot's 16-byte managed inline field.
internal static class DelayPromise
{
    internal static readonly MoveNext MoveNextAction = MoveNext;

    private static bool MoveNext(in LuminTaskState state)
    {
        if (state.CancellationToken.IsCancellationRequested)
        {
            LuminTaskSource.TrySetCanceled(state.Handle, state.CancellationToken);
            return false;
        }

        var page = LuminTaskSource.Enter(state.Handle, out int index, out long control, pendingOnly: true);
        if (page == null) return false;
        ref var slot = ref page.Slots[index];
        if (Stopwatch.GetTimestamp() - slot.Data.First < slot.Data.Second)
        {
            Volatile.Write(ref slot.Control, control);
            return true;
        }
        LuminTaskSource.Publish(ref slot, state.Handle, control, LuminTaskStatus.Succeeded);
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static long MillisecondsToStopwatchTicks(int milliseconds)
        => milliseconds == System.Threading.Timeout.Infinite
            ? long.MaxValue
            : (long)(milliseconds * ((double)Stopwatch.Frequency / 1000D));
}