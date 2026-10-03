using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace LuminThread.Utility;

public static class LuminTaskExceptionHelper
{
    [DoesNotReturn]
    public static void ThrowInvalidOperation(string message) =>
        throw new InvalidOperationException(message);

    [DoesNotReturn]
    public static void ThrowOperationCancel(string message) =>
        throw new OperationCanceledException(message);

    [DoesNotReturn]
    public static void ThrowTaskItemExhausted() =>
        throw new InvalidOperationException($"TaskItem exhausted: simultaneously live task count reached the limit of 32767 (current live count: {LuminTaskBag.GetLiveCount()}).");

    public static void ThrowArgumentNullException<T>(T value, string paramName)
        where T : class
    {
        if (value == null)
            throw new ArgumentNullException(paramName);
    }

    [DoesNotReturn]
    public static void ThrowInvalidError() =>
        throw new InvalidOperationException("Critical: invalid error type");

    // The handle carries a slot generation, so a recycled slot can never be aliased by an old
    // handle. These three are genuine contract violations rather than diagnostics, which is why
    // they always throw.
    [DoesNotReturn]
    public static void ThrowStaleHandle() =>
        throw new InvalidOperationException("LuminTask handle is stale: the slot was recycled (generation mismatch). A task may only be consumed once.");

    [DoesNotReturn]
    public static void ThrowInvalidHandle() =>
        throw new InvalidOperationException("LuminTask handle is invalid: the slot was never committed or the handle is corrupt.");

    [DoesNotReturn]
    public static void ThrowNullHandle() =>
        throw new InvalidOperationException("LuminTask handle is null: this operation needs a live slot, but the task completed synchronously.");

    [DoesNotReturn]
    public static void ThrowNotCompleted() =>
        throw new InvalidOperationException("LuminTask is not completed yet; await it before reading the result.");

    [DoesNotReturn]
    public static void ThrowAlreadyAwaited() =>
        throw new InvalidOperationException("LuminTask may only be awaited once; a continuation is already registered.");

    [DoesNotReturn]
    public static void ThrowSentinelInvoked() =>
        throw new InvalidOperationException("LuminTask completion sentinel must never be invoked.");
}
