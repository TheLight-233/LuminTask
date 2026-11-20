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
            LuminTaskSource.TrySetCanceled(state.Handle, state.CancellationToken);
            return false;
        }

        LuminTaskSource.TrySetResult(state.Handle);
        return false;
    }
}