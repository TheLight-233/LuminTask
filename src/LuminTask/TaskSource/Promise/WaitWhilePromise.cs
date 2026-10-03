using System;
using System.Runtime.CompilerServices;
using LuminThread.Interface;

namespace LuminThread.TaskSource.Promise;

// Waits while the condition holds. Stateless version: the predicate lives in LuminTaskState.State.
internal static class WaitWhilePromise
{
    internal static readonly MoveNext MoveNextAction = MoveNext;

    private static bool MoveNext(in LuminTaskState state)
    {
        var predicate = (Func<bool>)state.State!;

        if (state.CancellationToken.IsCancellationRequested)
        {
            LuminTaskCoreSource.TrySetCanceled(state.Handle, state.CancellationToken);
            return false;
        }

        try
        {
            if (predicate())
                return true;
        }
        catch (Exception ex)
        {
            LuminTaskCoreSource.TrySetException(state.Handle, ex);
            return false;
        }

        LuminTaskCoreSource.TrySetResult(state.Handle);
        return false;
    }
}

// Stateful version: the predicate lives in LuminTaskState.State and TState borrows the item's
// result storage while pending.
internal static class WaitWhilePromise<TState>
{
    internal static readonly MoveNext MoveNextAction = MoveNext;

    private static bool MoveNext(in LuminTaskState state)
    {
        var predicate = (Func<TState, bool>)state.State!;

        ref var item = ref LuminTaskCoreSource.TryResolve(state.Handle, out var cold, out int index);

        if (Unsafe.IsNullRef(ref item))
            return false;

        if (state.CancellationToken.IsCancellationRequested)
        {
            LuminTaskCoreSource.TrySetCanceled(state.Handle, state.CancellationToken);
            return false;
        }

        TState current = LuminTaskResultBridge.GetState<TState>(cold, index);

        try
        {
            if (predicate(current))
                return true;
        }
        catch (Exception ex)
        {
            LuminTaskCoreSource.TrySetException(state.Handle, ex);
            return false;
        }

        LuminTaskCoreSource.TrySetResult(state.Handle);
        return false;
    }
}