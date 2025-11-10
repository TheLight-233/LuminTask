using System.Runtime.CompilerServices;

namespace LuminThread.Interface;

public enum LuminTaskStatus : byte
{
    /// <summary>The operation has not yet completed.</summary>
    Pending = 0,
    /// <summary>The operation completed successfully.</summary>
    Succeeded = 1,
    /// <summary>The operation completed with an error.</summary>
    Faulted = 2,
    /// <summary>The operation completed due to cancellation.</summary>
    Canceled = 3
}

public static class LuminTaskStatusExtensions
{
    /// <summary>status != Pending.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsCompleted(this LuminTaskStatus status)
    {
        return status != LuminTaskStatus.Pending;
    }

    /// <summary>status == Succeeded.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsCompletedSuccessfully(this LuminTaskStatus status)
    {
        return status == LuminTaskStatus.Succeeded;
    }

    /// <summary>status == Canceled.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsCanceled(this LuminTaskStatus status)
    {
        return status == LuminTaskStatus.Canceled;
    }

    /// <summary>status == Faulted.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsFaulted(this LuminTaskStatus status)
    {
        return status == LuminTaskStatus.Faulted;
    }
}