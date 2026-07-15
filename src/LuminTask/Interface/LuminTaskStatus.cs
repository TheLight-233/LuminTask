using System.Runtime.CompilerServices;

namespace LuminThread.Interface;

public enum LuminTaskStatus : byte
{

    Pending = 0,

    Succeeded = 1,

    Faulted = 2,

    Canceled = 3
}

public static class LuminTaskStatusExtensions
{

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsCompleted(this LuminTaskStatus status)
    {
        return status != LuminTaskStatus.Pending;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsCompletedSuccessfully(this LuminTaskStatus status)
    {
        return status == LuminTaskStatus.Succeeded;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsCanceled(this LuminTaskStatus status)
    {
        return status == LuminTaskStatus.Canceled;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsFaulted(this LuminTaskStatus status)
    {
        return status == LuminTaskStatus.Faulted;
    }
}
