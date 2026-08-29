using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace LuminThread.Utility;

public static class LuminTaskExceptionHelper
{
    public static void ThrowTokenMismatch()
    {
        if (LuminTask.EnableRuntimeValidation)
            throw new InvalidOperationException("Token mismatch: the task was already consumed or its version is invalid.");
    }

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
}
