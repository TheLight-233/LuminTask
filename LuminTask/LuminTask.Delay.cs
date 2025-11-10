using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using LuminThread.TaskSource;
using LuminThread.Utility;

namespace LuminThread;

public readonly unsafe partial struct LuminTask
{
    [DebuggerHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static LuminTask Delay(int millisecondsDelay, CancellationToken cancellationToken = default)
    {
        var source = LuminTaskSourceCore<AsyncUnit>.Create();
        Timer? timer = null;
        timer = new Timer(static s =>
        {
            var source = ((IntPtr, Timer?))s!;
            try
            {
                LuminTaskSourceCore<AsyncUnit>.TrySetResult(source.Item1.ToPointer());
            }
            finally
            {
                LuminTaskSourceCore<AsyncUnit>.Dispose(source.Item1.ToPointer());
                source.Item2?.Dispose();
            }
        }, (new IntPtr(source), timer), millisecondsDelay, Timeout.Infinite);

        if (cancellationToken.CanBeCanceled)
        {
            cancellationToken.Register(() =>
            {
                timer.Dispose();
                LuminTaskSourceCore<AsyncUnit>.TrySetCanceled(source);
                LuminTaskSourceCore<AsyncUnit>.Dispose(source);
            });
        }

        return new LuminTask(LuminTaskSourceCore<AsyncUnit>.MethodTable, source, source->Id);
    }

    [DebuggerHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static LuminTask Delay(TimeSpan delay, CancellationToken cancellationToken = default)
    {
        return Delay((int)delay.TotalMilliseconds, cancellationToken);
    }

        
}