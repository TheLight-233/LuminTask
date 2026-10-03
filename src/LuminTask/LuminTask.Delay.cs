using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using LuminThread.TaskSource;
using LuminThread.TaskSource.Promise;
using LuminThread.Utility;

namespace LuminThread;

public readonly unsafe partial struct LuminTask
{
    [DebuggerHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static LuminTask Delay(int millisecondsDelay, PlayerLoopTiming loopTiming = PlayerLoopTiming.DotNet, CancellationToken cancellationToken = default, bool cancelImmediately = false)
    {
        if (millisecondsDelay < System.Threading.Timeout.Infinite)
            throw new ArgumentOutOfRangeException(nameof(millisecondsDelay));
        if (cancellationToken.IsCancellationRequested)
            return FromCanceled(cancellationToken);
        if (millisecondsDelay == 0)
            return CompletedTask();

        var handle = RentSlot();
        ref var item = ref GetRentedSlot(handle, out var cold, out int index);

        // The pending state gets its own unmanaged block, sized to what Delay actually needs (two
        // timestamps). The slot holds only the pointer, and Return frees it.
        unsafe
        {
            void* scratch = MemoryHelper.Alloc(LuminTaskCold.LoopStateSize);
            UnsafeMemory.Write(scratch, Stopwatch.GetTimestamp());
            UnsafeMemory.Write((byte*)scratch + sizeof(long),
                DelayPromise.MillisecondsToStopwatchTicks(millisecondsDelay));
            cold.SetPayload(index, (nint)scratch);
        }

        if (cancelImmediately && cancellationToken.CanBeCanceled)
            AttachCancellation(handle, cancellationToken);

        PlayerLoopHelper.AddAction(loopTiming, new LuminTaskState(handle, cancellationToken), DelayPromise.MoveNextAction);

        return new LuminTask(handle);
    }

    [DebuggerHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static LuminTask Delay(TimeSpan delay, PlayerLoopTiming loopTiming = PlayerLoopTiming.DotNet, CancellationToken cancellationToken = default, bool cancelImmediately = false)
    {
        if (delay == System.Threading.Timeout.InfiniteTimeSpan)
            return Delay(System.Threading.Timeout.Infinite, loopTiming, cancellationToken, cancelImmediately);
        var totalMilliseconds = delay.TotalMilliseconds;
        if (totalMilliseconds < 0D || totalMilliseconds > int.MaxValue)
            throw new ArgumentOutOfRangeException(nameof(delay));
        return Delay((int)totalMilliseconds, loopTiming, cancellationToken, cancelImmediately);
    }

}