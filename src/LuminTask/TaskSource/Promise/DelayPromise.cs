using System;
using System.Diagnostics;
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

        if (!LuminTaskResultBridge.TryReadState(state.Handle, out Inline16 timing)) return false;
        if (Stopwatch.GetTimestamp() - timing.First < timing.Second)
            return true;

        LuminTaskSource.TrySetResult(state.Handle);
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static long MillisecondsToStopwatchTicks(int milliseconds)
        => milliseconds == System.Threading.Timeout.Infinite
            ? long.MaxValue
            : (long)(milliseconds * ((double)Stopwatch.Frequency / 1000D));
}