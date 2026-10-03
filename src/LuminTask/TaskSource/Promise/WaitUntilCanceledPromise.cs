using LuminThread.Interface;

namespace LuminThread.TaskSource.Promise;

// Completes successfully once the token is canceled (note: for this source cancellation is
// success, not failure).
internal static class WaitUntilCanceledPromise
{
    internal static readonly MoveNext MoveNextAction = MoveNext;

    private static bool MoveNext(in LuminTaskState state)
    {
        if (state.CancellationToken.IsCancellationRequested)
        {
            LuminTaskCoreSource.TrySetResult(state.Handle);
            return false;
        }

        return true;
    }
}