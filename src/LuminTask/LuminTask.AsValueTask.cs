using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace LuminThread;

public static class LuminTaskValueTaskExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ValueTask AsValueTask(this in LuminTask task)
    {
        return task;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ValueTask<T> AsValueTask<T>(this in LuminTask<T> task)
    {
        return task;
    }

    public static LuminTask AsLuminTask(this ValueTask task)
    {
        if (!task.IsCompleted)
            return task.AsTask().AsLuminTask();

        try
        {
            task.GetAwaiter().GetResult();
            return LuminTask.CompletedTask();
        }
        catch (OperationCanceledException canceled)
        {
            return LuminTask.FromCanceled(canceled.CancellationToken);
        }
        catch (Exception exception)
        {
            return LuminTask.FromException(exception);
        }
    }

    public static LuminTask<T> AsLuminTask<T>(this ValueTask<T> task)
    {
        if (!task.IsCompleted)
            return task.AsTask().AsLuminTask();

        try
        {
            return LuminTask.FromResult(task.GetAwaiter().GetResult());
        }
        catch (OperationCanceledException canceled)
        {
            return LuminTask.FromCanceled<T>(canceled.CancellationToken);
        }
        catch (Exception exception)
        {
            return LuminTask.FromException<T>(exception);
        }
    }

}
