using System;
using System.Threading;
using LuminThread.TaskSource;
using LuminThread.Utility;

namespace LuminThread;

public readonly partial struct LuminTask
{
    /// <summary>Yields to the selected player-loop timing for the requested number of frames.</summary>
    public static unsafe LuminTask DelayFrame(int frameCount, PlayerLoopTiming timing = PlayerLoopTiming.Update, CancellationToken cancellationToken = default)
    {
        if (frameCount < 0) throw new ArgumentOutOfRangeException(nameof(frameCount));
        if (cancellationToken.IsCancellationRequested) return FromCanceled(cancellationToken);
        if (frameCount == 0) return CompletedTask();

        var core = LuminTaskSourceCore<AsyncUnit>.Create();
        var stateTuple = StateTuple.Create(frameCount);
        PlayerLoopHelper.AddAction(timing, new LuminTaskState(core, cancellationToken, null, stateTuple), &MoveNext);
        return new LuminTask(LuminTaskSourceCore<AsyncUnit>.MethodTablePtr, core, core->Id);
    }

    private static unsafe bool MoveNext(in LuminTaskState state)
    {
        var tuple = state.StateTuple as StateTuple<int>;
        if (tuple == null) return false;

        if (state.CancellationToken.IsCancellationRequested)
        {
            LuminTaskSourceCore<AsyncUnit>.TrySetCanceled(state.Source, state.CancellationToken);
            tuple.Dispose();
            return false;
        }

        if (--tuple.Item1 <= 0)
        {
            LuminTaskSourceCore<AsyncUnit>.TrySetResult(state.Source);
            tuple.Dispose();
            return false;
        }

        return true;
    }
}
