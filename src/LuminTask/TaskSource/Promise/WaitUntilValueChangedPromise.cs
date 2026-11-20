using System;
using System.Collections.Generic;
using LuminThread.Interface;
using LuminThread.Utility;

namespace LuminThread.TaskSource.Promise;

internal static class WaitUntilValueChangedPromise<TTarget, TValue>
{
    internal static readonly MoveNext MoveNextAction = MoveNext;

    private static bool MoveNext(in LuminTaskState state)
    {
        var helper = (StateTuple<(TTarget Target, Func<TTarget, TValue> Monitor,
            IEqualityComparer<TValue> Comparer, TValue Original)>)state.State!;
        if (state.CancellationToken.IsCancellationRequested)
        {
            helper.Dispose();
            LuminTaskSource.TrySetCanceled(state.Handle, state.CancellationToken);
            return false;
        }

        TValue next;
        try
        {
            ref var options = ref helper.Item;
            next = options.Monitor(options.Target);
            if (options.Comparer.Equals(options.Original, next)) return true;
        }
        catch (Exception ex)
        {
            helper.Dispose();
            LuminTaskSource.TrySetException(state.Handle, ex);
            return false;
        }
        helper.Dispose();
        LuminTaskResultBridge.TrySetResult(state.Handle, in next);
        return false;
    }
}
