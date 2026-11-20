using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using LuminThread.TaskSource;

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
        // Built directly instead of `return task;`: the generic implicit operator was removed on
        // purpose, because IL2CPP materialises every method of every closed generic type and that
        // operator alone instantiated ValueTaskSource<T> once per LuminTask<T> (152 closures in the
        // bench build). Keeping the conversion explicit pays that cost only at the call sites that
        // actually use it, while the ToValueTask capability stays available.
        if (task._handle.IsNull())
        {
            return new ValueTask<T>(task._result!);
        }

        return new ValueTask<T>(new ValueTaskSource<T>(task._handle), 0);
    }

    public static LuminTask AsLuminTask(this ValueTask task)
    {
        if (!task.IsCompleted)
            return AwaitPending(task);

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
            return AwaitPending(task);

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

    // INotifyCompletion keeps the registration ExecutionContext, as the Task bridge did.
    // ConfigureAwait(false) avoids dispatching back to the caller's scheduler.
    private readonly struct ConversionAwaiter : INotifyCompletion
    {
        private readonly ValueTask _value;
        internal ConversionAwaiter(ValueTask value) => _value = value;
        public ConversionAwaiter GetAwaiter() => this;
        public bool IsCompleted => _value.IsCompleted;
        public void OnCompleted(Action continuation) => _value.ConfigureAwait(false).GetAwaiter().OnCompleted(continuation);
        public void GetResult() => _value.GetAwaiter().GetResult();
    }

    private readonly struct ConversionAwaiter<T> : INotifyCompletion
    {
        private readonly ValueTask<T> _value;
        internal ConversionAwaiter(ValueTask<T> value) => _value = value;
        public ConversionAwaiter<T> GetAwaiter() => this;
        public bool IsCompleted => _value.IsCompleted;
        public void OnCompleted(Action continuation) => _value.ConfigureAwait(false).GetAwaiter().OnCompleted(continuation);
        public T GetResult() => _value.GetAwaiter().GetResult();
    }

    internal static async LuminTask AwaitPending(ValueTask task, CancellationToken fallbackToken = default)
    {
        try { await new ConversionAwaiter(task); }
        catch (OperationCanceledException error) when (!error.CancellationToken.CanBeCanceled && fallbackToken.CanBeCanceled)
        { throw new OperationCanceledException(fallbackToken); }
    }

    internal static async LuminTask<T> AwaitPending<T>(ValueTask<T> task, CancellationToken fallbackToken = default)
    {
        try { return await new ConversionAwaiter<T>(task); }
        catch (OperationCanceledException error) when (!error.CancellationToken.CanBeCanceled && fallbackToken.CanBeCanceled)
        { throw new OperationCanceledException(fallbackToken); }
    }
}
