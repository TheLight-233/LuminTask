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
        var source = DelayPromise<AsyncUnit>.Create(millisecondsDelay, loopTiming, cancellationToken, cancelImmediately);

        return new LuminTask(LuminTaskSourceCore<AsyncUnit>.MethodTable, source.Source, source.Source->Id);
    }

    [DebuggerHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static LuminTask Delay(TimeSpan delay, PlayerLoopTiming loopTiming = PlayerLoopTiming.DotNet, CancellationToken cancellationToken = default, bool cancelImmediately = false)
    {
        return Delay((int)delay.TotalMilliseconds, loopTiming, cancellationToken, cancelImmediately);
    }

        
}