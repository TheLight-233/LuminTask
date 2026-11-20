using System.Runtime.CompilerServices;
using LuminThread.TaskSource;

namespace LuminThread;

// The capture policy lives in the slot itself (LuminTaskSource.SetContinueOnCapturedContext), so
// ConfigureAwait only has to write that flag and hand the task back: the awaiter is LuminTask's own
// GetAwaiter/OnCompleted pair, no wrapper type involved.
//
// Keeping it a single type is deliberate. A dedicated awaitable + awaiter pair per T (the previous
// ConfiguredLuminTaskAwaitable[<T>] / ConfiguredLuminTaskAwaiter[<T>]) added four closed generic
// types and one extra (TAwaiter, TStateMachine) instantiation of AwaitOnCompleted /
// AwaitUnsafeOnCompleted at every await site that used it - pure metadata and code-size cost for a
// boolean that the slot already stores.
public partial struct LuminTask
{
    /// <summary>Controls whether the await continuation captures the current execution context.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly LuminTask ConfigureAwait(bool continueOnCapturedContext)
    {
        LuminTaskSource.SetContinueOnCapturedContext(_handle, continueOnCapturedContext);
        return this;
    }
}

public partial struct LuminTask<T>
{
    /// <summary>Controls whether the await continuation captures the current execution context.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly LuminTask<T> ConfigureAwait(bool continueOnCapturedContext)
    {
        LuminTaskSource.SetContinueOnCapturedContext(_handle, continueOnCapturedContext);
        return this;
    }
}