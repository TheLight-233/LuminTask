using LuminThread.Interface;

namespace LuminThread.TaskSource.Promise;

// Yields once: carries no per-frame state, checks the token each frame and lets the core publish
// the completion.
internal static class YieldPromise
{
    internal static readonly MoveNext MoveNextAction = MoveNext;

    private static bool MoveNext(in LuminTaskState state)
    {
        if (state.CancellationToken.IsCancellationRequested)
        {
            LuminTaskCoreSource.TrySetCanceled(state.Handle, state.CancellationToken);
            return false;
        }

        LuminTaskCoreSource.TrySetResult(state.Handle);
        return false;
    }
}