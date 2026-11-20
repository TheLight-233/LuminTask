using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Threading;
using LuminThread.Interface;
using LuminThread.Utility;

namespace LuminThread.TaskSource.Promise;

public readonly struct WhenEachResult<T>
{
    public T Result { get; }
    public Exception Exception { get; }

    public bool IsCompletedSuccessfully => Exception == null;

    public bool IsFaulted => Exception != null;

    public WhenEachResult(T result)
    {
        this.Result = result;
        this.Exception = null!;
    }

    public WhenEachResult(Exception exception)
    {
        if (exception == null) throw new ArgumentNullException(nameof(exception));
        this.Result = default!;
        this.Exception = exception;
    }

    public void TryThrow()
    {
        if (IsFaulted)
        {
            ExceptionDispatchInfo.Capture(Exception).Throw();
        }
    }

    public T GetResult()
    {
        if (IsFaulted)
        {
            ExceptionDispatchInfo.Capture(Exception).Throw();
        }
        return Result;
    }

    public override string ToString()
    {
        if (IsCompletedSuccessfully)
        {
            return Result?.ToString() ?? "";
        }
        else
        {
            return $"Exception{{{Exception.Message}}}";
        }
    }
}

internal sealed class WhenEachPromise<T> : ILuminTaskAsyncEnumerable<WhenEachResult<T>>
{
    private LuminTask<T>[]? _tasks;

    public WhenEachPromise(LuminTask<T>[] tasks)
    {
        _tasks = tasks;
    }

    public WhenEachPromise(IEnumerable<LuminTask<T>> tasks)
    {
        _tasks = tasks.ToArray();
    }

    public WhenEachPromise(LuminTask<T> task1) : this([task1]) { }
    public WhenEachPromise(LuminTask<T> task1, LuminTask<T> task2) : this([task1, task2]) { }
    public WhenEachPromise(LuminTask<T> task1, LuminTask<T> task2, LuminTask<T> task3) : this([task1, task2, task3]) { }
    public WhenEachPromise(LuminTask<T> task1, LuminTask<T> task2, LuminTask<T> task3, LuminTask<T> task4) : this([task1, task2, task3, task4]) { }
    public WhenEachPromise(LuminTask<T> task1, LuminTask<T> task2, LuminTask<T> task3, LuminTask<T> task4, LuminTask<T> task5) : this([task1, task2, task3, task4, task5]) { }
    public WhenEachPromise(LuminTask<T> task1, LuminTask<T> task2, LuminTask<T> task3, LuminTask<T> task4, LuminTask<T> task5, LuminTask<T> task6) : this([task1, task2, task3, task4, task5, task6]) { }
    public WhenEachPromise(LuminTask<T> task1, LuminTask<T> task2, LuminTask<T> task3, LuminTask<T> task4, LuminTask<T> task5, LuminTask<T> task6, LuminTask<T> task7) : this([task1, task2, task3, task4, task5, task6, task7]) { }
    public WhenEachPromise(LuminTask<T> task1, LuminTask<T> task2, LuminTask<T> task3, LuminTask<T> task4, LuminTask<T> task5, LuminTask<T> task6, LuminTask<T> task7, LuminTask<T> task8) : this([task1, task2, task3, task4, task5, task6, task7, task8]) { }
    public WhenEachPromise(LuminTask<T> task1, LuminTask<T> task2, LuminTask<T> task3, LuminTask<T> task4, LuminTask<T> task5, LuminTask<T> task6, LuminTask<T> task7, LuminTask<T> task8, LuminTask<T> task9) : this([task1, task2, task3, task4, task5, task6, task7, task8, task9]) { }
    public WhenEachPromise(LuminTask<T> task1, LuminTask<T> task2, LuminTask<T> task3, LuminTask<T> task4, LuminTask<T> task5, LuminTask<T> task6, LuminTask<T> task7, LuminTask<T> task8, LuminTask<T> task9, LuminTask<T> task10) : this([task1, task2, task3, task4, task5, task6, task7, task8, task9, task10]) { }
    public WhenEachPromise(LuminTask<T> task1, LuminTask<T> task2, LuminTask<T> task3, LuminTask<T> task4, LuminTask<T> task5, LuminTask<T> task6, LuminTask<T> task7, LuminTask<T> task8, LuminTask<T> task9, LuminTask<T> task10, LuminTask<T> task11) : this([task1, task2, task3, task4, task5, task6, task7, task8, task9, task10, task11]) { }
    public WhenEachPromise(LuminTask<T> task1, LuminTask<T> task2, LuminTask<T> task3, LuminTask<T> task4, LuminTask<T> task5, LuminTask<T> task6, LuminTask<T> task7, LuminTask<T> task8, LuminTask<T> task9, LuminTask<T> task10, LuminTask<T> task11, LuminTask<T> task12) : this([task1, task2, task3, task4, task5, task6, task7, task8, task9, task10, task11, task12]) { }
    public WhenEachPromise(LuminTask<T> task1, LuminTask<T> task2, LuminTask<T> task3, LuminTask<T> task4, LuminTask<T> task5, LuminTask<T> task6, LuminTask<T> task7, LuminTask<T> task8, LuminTask<T> task9, LuminTask<T> task10, LuminTask<T> task11, LuminTask<T> task12, LuminTask<T> task13) : this([task1, task2, task3, task4, task5, task6, task7, task8, task9, task10, task11, task12, task13]) { }
    public WhenEachPromise(LuminTask<T> task1, LuminTask<T> task2, LuminTask<T> task3, LuminTask<T> task4, LuminTask<T> task5, LuminTask<T> task6, LuminTask<T> task7, LuminTask<T> task8, LuminTask<T> task9, LuminTask<T> task10, LuminTask<T> task11, LuminTask<T> task12, LuminTask<T> task13, LuminTask<T> task14) : this([task1, task2, task3, task4, task5, task6, task7, task8, task9, task10, task11, task12, task13, task14]) { }
    public WhenEachPromise(LuminTask<T> task1, LuminTask<T> task2, LuminTask<T> task3, LuminTask<T> task4, LuminTask<T> task5, LuminTask<T> task6, LuminTask<T> task7, LuminTask<T> task8, LuminTask<T> task9, LuminTask<T> task10, LuminTask<T> task11, LuminTask<T> task12, LuminTask<T> task13, LuminTask<T> task14, LuminTask<T> task15) : this([task1, task2, task3, task4, task5, task6, task7, task8, task9, task10, task11, task12, task13, task14, task15]) { }

    public ILuminTaskAsyncEnumerator<WhenEachResult<T>> GetAsyncEnumerator(
        CancellationToken cancellationToken = default)
    {
        var tasks = Interlocked.Exchange(ref _tasks, null)
            ?? throw new InvalidOperationException("WhenEach inputs have already been consumed.");
        return new Enumerator(tasks, cancellationToken);
    }

    private sealed class Enumerator : ILuminTaskAsyncEnumerator<WhenEachResult<T>>
    {
        private readonly object _gate = new();
        private Queue<WhenEachResult<T>>? _ready;
        private int _remaining;
        private ulong _waiter;
        private CancellationToken _token;
        private CancellationTokenRegistration _registration;
        private bool _disposed;

        public WhenEachResult<T> Current { get; private set; }

        internal Enumerator(LuminTask<T>[] tasks, CancellationToken token)
        {
            _remaining = tasks.Length;
            _token = token;
            if (token.CanBeCanceled && tasks.Length != 0)
                _registration = token.RegisterWithoutCaptureExecutionContext(static state => ((Enumerator)state).Cancel(), this);

            try
            {
                foreach (var task in tasks) Watch(task);
            }
            catch
            {
                DisposeAsync().GetResult();
                throw;
            }
        }

        private void Watch(LuminTask<T> task)
        {
            if (task.IsCompleted) { Observe(task); return; }
            var observation = StateTuple.Create((this, task._handle));
            try
            {
                task.SourceOnCompleted(static state =>
                {
                    var box = (StateTuple<(Enumerator Owner, ulong Handle)>)state;
                    var child = box.Item;
                    box.Dispose();
                    child.Owner.Observe(LuminTaskSource.ToTask<T>(child.Handle));
                }, observation);
            }
            catch { observation.Dispose(); throw; }
        }

        private void Observe(LuminTask<T> task)
        {
            WhenEachResult<T> item;
            try { item = new WhenEachResult<T>(task.GetResult()); }
            catch (Exception ex) { item = new WhenEachResult<T>(ex); }

            ulong waiter = 0;
            CancellationToken? cancellation = null;
            CancellationTokenRegistration release = default;
            lock (_gate)
            {
                if (--_remaining == 0)
                {
                    release = _registration;
                    _registration = default;
                }
                if (!_disposed && !_token.IsCancellationRequested)
                {
                    waiter = _waiter;
                    _waiter = 0;
                    if (waiter == 0) (_ready ??= new Queue<WhenEachResult<T>>()).Enqueue(item);
                    else Current = item;
                }
                else if (_remaining == 0 && _waiter != 0)
                {
                    // Cancellation may already be requested before its callback enters.
                    waiter = _waiter;
                    _waiter = 0;
                    cancellation = _token;
                }
            }
            release.Release();
            if (waiter != 0)
            {
                if (cancellation.HasValue) LuminTaskSource.TrySetCanceled(waiter, cancellation.Value);
                else LuminTaskResultBridge.TrySetResult(waiter, true);
            }
        }

        public LuminTask<bool> MoveNextAsync()
        {
            lock (_gate)
            {
                if (_disposed) return CompletedTasks.False();
                if (_token.IsCancellationRequested) return LuminTask.FromCanceled<bool>(_token);
                if (_waiter != 0) throw new InvalidOperationException("A MoveNextAsync call is already pending.");
                if (_ready is { Count: > 0 })
                {
                    Current = _ready.Dequeue();
                    return CompletedTasks.True();
                }
                if (_remaining == 0) return CompletedTasks.False();
                return LuminTaskSource.Create<bool>(out _waiter);
            }
        }

        private void Cancel()
        {
            ulong waiter;
            CancellationToken token;
            lock (_gate)
            {
                waiter = _waiter;
                _waiter = 0;
                token = _token;
                _ready = null;
            }
            if (waiter != 0) LuminTaskSource.TrySetCanceled(waiter, token);
        }

        public LuminTask DisposeAsync()
        {
            ulong waiter;
            CancellationTokenRegistration release;
            lock (_gate)
            {
                _disposed = true;
                Current = default;
                _ready = null;
                waiter = _waiter;
                _waiter = 0;
                release = _registration;
                _registration = default;
                _token = default;
            }
            release.Release();
            if (waiter != 0) LuminTaskResultBridge.TrySetResult(waiter, false);
            return default;
        }
    }
}
