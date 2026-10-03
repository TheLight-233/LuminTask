using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using LuminThread.Interface;

namespace LuminThread.TaskSource.Promise;

// Watches a target value until it changes. The helper (target, monitor function, comparer and the
// current value) hangs off LuminTaskState.State and only lives while the source is pending: it is
// dropped together with the player-loop item.
internal sealed class WaitUntilValueChangedState<TTarget, TValue>
{
    internal readonly TTarget Target;
    internal readonly Func<TTarget, TValue> MonitorFunction;
    internal readonly IEqualityComparer<TValue> EqualityComparer;
    internal TValue CurrentValue;

    internal WaitUntilValueChangedState(
        TTarget target,
        Func<TTarget, TValue> monitorFunction,
        IEqualityComparer<TValue> equalityComparer,
        TValue currentValue)
    {
        Target = target;
        MonitorFunction = monitorFunction;
        EqualityComparer = equalityComparer;
        CurrentValue = currentValue;
    }
}

internal static class WaitUntilValueChangedPromise<TTarget, TValue>
{
    internal static readonly MoveNext MoveNextAction = MoveNext;

    private static bool MoveNext(in LuminTaskState state)
    {
        var helper = (WaitUntilValueChangedState<TTarget, TValue>)state.State!;

        if (state.CancellationToken.IsCancellationRequested)
        {
            LuminTaskCoreSource.TrySetCanceled(state.Handle, state.CancellationToken);
            return false;
        }

        try
        {
            TValue next = helper.MonitorFunction(helper.Target);

            if (helper.EqualityComparer.Equals(helper.CurrentValue, next))
                return true;

            LuminTaskResultBridge.TrySetResult(state.Handle, in next);
            return false;
        }
        catch (Exception ex)
        {
            LuminTaskCoreSource.TrySetException(state.Handle, ex);
            return false;
        }
    }
}