using LuminThread.Interface;
using LuminThread.TaskSource;
using LuminThread.TaskSource.Promise;

namespace LuminThread;

using System;
using System.Runtime.CompilerServices;
using System.Threading;

public partial struct LuminTask
{
    // Yield is a real task, not a bare continuation registration. Renting a slot makes each yield
    // slightly more expensive, but it deletes YieldAwaitable/.Awaiter - two types, and one more
    // awaiter dimension for every state machine that yields.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static LuminTask Yield()
    {
        return Yield(PlayerLoopTiming.DotNet, CancellationToken.None);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static LuminTask Yield(PlayerLoopTiming timing)
    {
        return Yield(timing, CancellationToken.None);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static LuminTask Yield(CancellationToken cancellationToken, bool cancelImmediately = false)
    {
        return Yield(PlayerLoopTiming.Update, cancellationToken, cancelImmediately);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static LuminTask Yield(PlayerLoopTiming timing, CancellationToken cancellationToken, bool cancelImmediately = false)
    {
        var handle = RentSlot();

        if (cancellationToken.IsCancellationRequested)
        {
            LuminTaskCoreSource.TrySetCanceled(handle, cancellationToken);
            return new LuminTask(handle);
        }

        if (cancelImmediately && cancellationToken.CanBeCanceled)
            AttachCancellation(handle, cancellationToken);

        PlayerLoopHelper.AddAction(timing, new LuminTaskState(handle, cancellationToken), YieldPromise.MoveNextAction);

        return new LuminTask(handle);
    }
}