using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace LuminThread.Utility;

public static class LuminTaskExceptionHelper
{
    [DoesNotReturn]
    [Conditional("DEBUG")]
    public static void ThrowTokenMismatch() =>
        throw new InvalidOperationException("Token mismatch");

    [DoesNotReturn]
    public static void ThrowInvalidOperation(string message) =>
        throw new InvalidOperationException(message);
    
    [DoesNotReturn]
    public static void ThrowOperationCancel(string message) =>
        throw new OperationCanceledException(message);
    
    [DoesNotReturn]
    public static void ThrowTaskItemExhausted() => 
        throw new InvalidOperationException("TaskItem exhausted");

    [DoesNotReturn]
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