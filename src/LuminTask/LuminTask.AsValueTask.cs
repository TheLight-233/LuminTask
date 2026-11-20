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
        => task.AsLuminTask(flowExecutionContext: true);

    /// <param name="task">The value task to consume once.</param>
    /// <param name="flowExecutionContext">Whether to flow the registration context (including AsyncLocal values)
    /// to the bridge continuation if the task suspends.</param>
    public static LuminTask AsLuminTask(this ValueTask task, bool flowExecutionContext)
    {
        if (!task.IsCompleted)
            return AwaitPending(task, flowExecutionContext: flowExecutionContext);

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
        => task.AsLuminTask(flowExecutionContext: true);

    /// <inheritdoc cref="AsLuminTask(ValueTask, bool)"/>
    public static LuminTask<T> AsLuminTask<T>(this ValueTask<T> task, bool flowExecutionContext)
    {
        if (!task.IsCompleted)
            return AwaitPending(task, flowExecutionContext: flowExecutionContext);

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

    // Scheduling and ExecutionContext flow are independent. Both paths avoid scheduler capture.
    private readonly struct ConversionAwaiter : INotifyCompletion
    {
        private readonly ValueTask _value;
        private readonly bool _flow;
        internal ConversionAwaiter(ValueTask value, bool flow) => (_value, _flow) = (value, flow);
        public ConversionAwaiter GetAwaiter() => this;
        public bool IsCompleted => _value.IsCompleted;
        public void OnCompleted(Action continuation)
        {
            var awaiter = _value.ConfigureAwait(false).GetAwaiter();
            if (_flow) awaiter.OnCompleted(continuation);
            else awaiter.UnsafeOnCompleted(continuation);
        }
        public void GetResult() => _value.GetAwaiter().GetResult();
    }

    private readonly struct ConversionAwaiter<T> : INotifyCompletion
    {
        private readonly ValueTask<T> _value;
        private readonly bool _flow;
        internal ConversionAwaiter(ValueTask<T> value, bool flow) => (_value, _flow) = (value, flow);
        public ConversionAwaiter<T> GetAwaiter() => this;
        public bool IsCompleted => _value.IsCompleted;
        public void OnCompleted(Action continuation)
        {
            var awaiter = _value.ConfigureAwait(false).GetAwaiter();
            if (_flow) awaiter.OnCompleted(continuation);
            else awaiter.UnsafeOnCompleted(continuation);
        }
        public T GetResult() => _value.GetAwaiter().GetResult();
    }

    internal static async LuminTask AwaitPending(ValueTask task, CancellationToken fallbackToken = default,
        bool flowExecutionContext = true)
    {
        try { await new ConversionAwaiter(task, flowExecutionContext); }
        catch (OperationCanceledException error) when (!error.CancellationToken.CanBeCanceled && fallbackToken.CanBeCanceled)
        { throw new OperationCanceledException(fallbackToken); }
    }

    internal static async LuminTask<T> AwaitPending<T>(ValueTask<T> task, CancellationToken fallbackToken = default,
        bool flowExecutionContext = true)
    {
        try { return await new ConversionAwaiter<T>(task, flowExecutionContext); }
        catch (OperationCanceledException error) when (!error.CancellationToken.CanBeCanceled && fallbackToken.CanBeCanceled)
        { throw new OperationCanceledException(fallbackToken); }
    }
}
