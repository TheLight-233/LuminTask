using System;
using System.Runtime.CompilerServices;
using System.Threading;
using LuminThread.Interface;
using LuminThread.TaskSource;
using LuminThread.Utility;

namespace LuminThread;

public partial struct LuminTask
{
    /// <summary>Yields to the selected player-loop timing for the requested number of frames.</summary>
    public static LuminTask DelayFrame(int frameCount, PlayerLoopTiming timing = PlayerLoopTiming.Update, CancellationToken cancellationToken = default)
    {
        if (frameCount < 0) throw new ArgumentOutOfRangeException(nameof(frameCount));
        if (cancellationToken.IsCancellationRequested) return FromCanceled(cancellationToken);
        if (frameCount == 0) return CompletedTask();

        long remaining = frameCount;
        var handle = LuminTaskResultBridge.RentState(in remaining);

        PlayerLoopHelper.Schedule(timing, new LuminTaskState(handle, cancellationToken), DelayFrameStep);

        return new LuminTask(handle);
    }

    private static bool DelayFrameStep(in LuminTaskState state)
    {
        if (state.CancellationToken.IsCancellationRequested)
        {
            LuminTaskSource.TrySetCanceled(state.Handle, state.CancellationToken);
            return false;
        }

        var page = LuminTaskSource.Enter(state.Handle, out int i, out long control, pendingOnly: true);
        if (page == null) return false;
        ref var slot = ref page.Slots[i];
        bool waiting = --slot.Data.First > 0;
        Volatile.Write(ref slot.Control, control);
        if (waiting) return true;

        LuminTaskSource.TrySetResult(state.Handle);
        return false;
    }
}