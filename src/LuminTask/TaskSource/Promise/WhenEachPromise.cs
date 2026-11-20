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
        if (!cancellationToken.CanBeCanceled && tasks.Length != 0 && tasks[0]._handle == 0)
        {
            int i = 1;
            while (i < tasks.Length && tasks[i]._handle == 0) i++;
            if (i == tasks.Length) return new InlineEnumerator(tasks);
        }
        return new Enumerator(tasks, cancellationToken);
    }

    // Inline successes are already in delivery order. Snapshot values at GetAsyncEnumerator
    // so the caller can immediately reuse its task array, even before the first MoveNext.
    private sealed class InlineEnumerator : ILuminTaskAsyncEnumerator<WhenEachResult<T>>
    {
        private readonly object _gate = new();
        private T[]? _values;
        private int _index;
        public WhenEachResult<T> Current { get; private set; }

        internal InlineEnumerator(LuminTask<T>[] tasks)
        {
            _values = new T[tasks.Length];
            for (int i = 0; i < tasks.Length; i++) _values[i] = tasks[i]._result!;
        }

        public LuminTask<bool> MoveNextAsync()
        {
            lock (_gate)
            {
                if (_values == null) return CompletedTasks.False();
                Current = new WhenEachResult<T>(_values[_index]);
                _values[_index++] = default!;
                if (_index == _values.Length) _values = null;
                return CompletedTasks.True();
            }
        }

        public LuminTask DisposeAsync()
        {
            lock (_gate) { _values = null; Current = default; }
            return default;
        }
    }

    // The consumer keeps its own identity after disposal. Only the unexposed observer can be reused.
    private sealed class Enumerator : ILuminTaskAsyncEnumerator<WhenEachResult<T>>
    {
        private Session? _session;
        public WhenEachResult<T> Current { get; internal set; }

        internal Enumerator(LuminTask<T>[] tasks, CancellationToken token)
        {
            var session = Session.Rent(token);
            _session = session;
            try { session.Start(this, tasks, token); }
            catch { DisposeAsync().GetResult(); throw; }
        }

        public LuminTask<bool> MoveNextAsync() =>
            Volatile.Read(ref _session)?.MoveNext(this) ?? CompletedTasks.False();

        public LuminTask DisposeAsync()
        {
            Interlocked.Exchange(ref _session, null)?.Close(this);
            return default;
        }
    }

    private sealed class Session : SlotObserver, ILuminCacheNode
    {
        public ILuminCacheNode? CacheNext { get; set; }
        private Enumerator? _owner;
        private Queue<WhenEachResult<T>>? _ready;
        private int _remaining;
        private ulong _waiter;
        private CancellationToken _token;
        private CancellationTokenRegistration _registration;
        private bool _poolable;

        internal static Session Rent(CancellationToken token) =>
            token.CanBeCanceled ? new Session() : (Session?)LuminTaskIdleCache.Take(typeof(Session)) ?? new Session();

        internal void Start(Enumerator owner, LuminTask<T>[] tasks, CancellationToken token)
        {
            _owner = owner;
            _remaining = tasks.Length;
            _poolable = !token.CanBeCanceled;
            _token = token;
            if (token.CanBeCanceled && tasks.Length != 0)
                _registration = token.RegisterWithoutCaptureExecutionContext(static state => ((Session)state).Cancel(), this);
            foreach (var task in tasks) Watch(task);
        }

        private void Watch(LuminTask<T> task)
        {
            if (task._handle != 0 && LuminTaskSource.TryRegisterObserver(task._handle, this, 0)) return;
            if (task.IsCompleted) { Observe(task); return; }
            var observation = StateTuple.Create((this, task._handle));
            try
            {
                task.SourceOnCompleted(static state =>
                {
                    var box = (StateTuple<(Session Owner, ulong Handle)>)state;
                    var child = box.Item;
                    box.Dispose();
                    child.Owner.Observe(LuminTaskSource.ToTask<T>(child.Handle));
                }, observation);
            }
            catch { observation.Dispose(); throw; }
        }

        internal override void Completed(ulong handle, int index) => Observe(LuminTaskSource.ToTask<T>(handle));

        private void Observe(LuminTask<T> task)
        {
            WhenEachResult<T> item = default;
            if (Volatile.Read(ref _owner) != null)
            {
                try { item = new WhenEachResult<T>(task.GetResult()); }
                catch (Exception ex) { item = new WhenEachResult<T>(ex); }
            }
            else if (task._handle != 0)
            {
                LuminTaskSource.TryForget(task._handle);
            }

            bool recycle;
            ulong waiter = 0;
            CancellationToken? cancellation = null;
            CancellationTokenRegistration release = default;
            lock (this)
            {
                recycle = --_remaining == 0 && _owner == null && _poolable;
                if (_remaining == 0)
                {
                    release = _registration;
                    _registration = default;
                }
                if (_owner != null && !_token.IsCancellationRequested)
                {
                    waiter = _waiter;
                    _waiter = 0;
                    if (waiter == 0) (_ready ??= new Queue<WhenEachResult<T>>()).Enqueue(item);
                    else _owner.Current = item;
                }
                else if (_remaining == 0 && _waiter != 0)
                {
                    // Cancellation may already be requested before its callback enters.
                    waiter = _waiter;
                    _waiter = 0;
                    cancellation = _token;
                }
            }
            // Only captured locals may be used after publishing: a continuation can dispose and rent again.
            release.Release();
            if (recycle) LuminTaskIdleCache.Return(this, 128);
            if (waiter != 0)
            {
                if (cancellation.HasValue) LuminTaskSource.TrySetCanceled(waiter, cancellation.Value);
                else LuminTaskResultBridge.TrySetResult(waiter, true);
            }
        }

        internal LuminTask<bool> MoveNext(Enumerator owner)
        {
            lock (this)
            {
                if (!ReferenceEquals(_owner, owner)) return CompletedTasks.False();
                if (_token.IsCancellationRequested) return LuminTask.FromCanceled<bool>(_token);
                if (_waiter != 0) throw new InvalidOperationException("A MoveNextAsync call is already pending.");
                if (_ready is { Count: > 0 })
                {
                    owner.Current = _ready.Dequeue();
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
            lock (this)
            {
                waiter = _waiter;
                _waiter = 0;
                token = _token;
                _ready = null;
            }
            if (waiter != 0) LuminTaskSource.TrySetCanceled(waiter, token);
        }

        internal void Close(Enumerator owner)
        {
            ulong waiter;
            bool reusable;
            CancellationTokenRegistration release;
            lock (this)
            {
                // Either Close or the last completing child returns the session. Token callbacks can outlive both.
                reusable = _remaining == 0 && _poolable;
                Volatile.Write(ref _owner, null);
                owner.Current = default;
                _ready = null;
                waiter = _waiter;
                _waiter = 0;
                release = _registration;
                _registration = default;
                _token = default;
            }
            release.Release();
            if (waiter != 0) LuminTaskResultBridge.TrySetResult(waiter, false);
            if (reusable) LuminTaskIdleCache.Return(this, 128);
        }
    }
}
