using System;
using LuminThread.Interface;

namespace LuminThread.TaskSource.Promise;

// Waits until the condition holds. Stateless version: the predicate lives in LuminTaskState.State.
internal static class WaitUntilPromise
{
    internal static readonly MoveNext Until = static (in LuminTaskState state) => Poll(in state, false);
    internal static readonly MoveNext While = static (in LuminTaskState state) => Poll(in state, true);

    private static bool Poll(in LuminTaskState state, bool waitWhile)
    {
        var predicate = (Func<bool>)state.State!;

        if (state.CancellationToken.IsCancellationRequested)
        {
            LuminTaskSource.TrySetCanceled(state.Handle, state.CancellationToken);
            return false;
        }

        try
        {
            if (predicate() == waitWhile)
                return true;
        }
        catch (Exception ex)
        {
            LuminTaskSource.TrySetException(state.Handle, ex);
            return false;
        }

        LuminTaskSource.TrySetResult(state.Handle);
        return false;
    }
}

// The predicate is queued with the work. Its typed argument uses the pending slot's
// normal result layout and stays visible to the GC across frames.
internal static class WaitUntilPromise<TState>
{
    internal static readonly MoveNext Until = static (in LuminTaskState state) => Poll(in state, false);
    internal static readonly MoveNext While = static (in LuminTaskState state) => Poll(in state, true);

    private static bool Poll(in LuminTaskState state, bool waitWhile)
    {
        var predicate = (Func<TState, bool>)state.State!;

        if (state.CancellationToken.IsCancellationRequested)
        {
            LuminTaskSource.TrySetCanceled(state.Handle, state.CancellationToken);
            return false;
        }

        if (!LuminTaskResultBridge.TryReadState(state.Handle, out TState current)) return false;

        try
        {
            if (predicate(current) == waitWhile)
                return true;
        }
        catch (Exception ex)
        {
            LuminTaskSource.TrySetException(state.Handle, ex);
            return false;
        }

        LuminTaskSource.TrySetResult(state.Handle);
        return false;
    }
}