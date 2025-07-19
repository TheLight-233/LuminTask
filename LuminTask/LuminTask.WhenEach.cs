using System;
using System.Runtime.ExceptionServices;
using System.Threading;
using Lumin.Threading.Core;
using Lumin.Threading.Interface;
using Lumin.Threading.Source;
using Lumin.Threading.Tasks.Utility;
using Lumin.Threading.Utility;

namespace Lumin.Threading.Tasks;


public partial struct LuminTask
{
    public static ILuminTaskAsyncEnumerable<WhenEachResult<T>> WhenEach<T>(LuminTask<T> task1)
    {
        return new WhenEachPromise<T>(task1);
    }

    public static ILuminTaskAsyncEnumerable<WhenEachResult<T>> WhenEach<T>(LuminTask<T> task1, LuminTask<T> task2)
    {
        return new WhenEachPromise<T>(task1, task2);
    }
    
    public static ILuminTaskAsyncEnumerable<WhenEachResult<T>> WhenEach<T>(LuminTask<T> task1, LuminTask<T> task2, LuminTask<T> task3)
    {
        return new WhenEachPromise<T>(task1, task2, task3);
    }

    public static ILuminTaskAsyncEnumerable<WhenEachResult<T>> WhenEach<T>(LuminTask<T> task1, LuminTask<T> task2, LuminTask<T> task3, LuminTask<T> task4)
    {
        return new WhenEachPromise<T>(task1, task2, task3, task4);
    }

    public static ILuminTaskAsyncEnumerable<WhenEachResult<T>> WhenEach<T>(LuminTask<T> task1, LuminTask<T> task2, LuminTask<T> task3, LuminTask<T> task4, LuminTask<T> task5)
    {
        return new WhenEachPromise<T>(task1, task2, task3, task4, task5);
    }

    public static ILuminTaskAsyncEnumerable<WhenEachResult<T>> WhenEach<T>(LuminTask<T> task1, LuminTask<T> task2, LuminTask<T> task3, LuminTask<T> task4, LuminTask<T> task5, LuminTask<T> task6)
    {
        return new WhenEachPromise<T>(task1, task2, task3, task4, task5, task6);
    }

    public static ILuminTaskAsyncEnumerable<WhenEachResult<T>> WhenEach<T>(LuminTask<T> task1, LuminTask<T> task2, LuminTask<T> task3, LuminTask<T> task4, LuminTask<T> task5, LuminTask<T> task6, LuminTask<T> task7)
    {
        return new WhenEachPromise<T>(task1, task2, task3, task4, task5, task6, task7);
    }

    public static ILuminTaskAsyncEnumerable<WhenEachResult<T>> WhenEach<T>(LuminTask<T> task1, LuminTask<T> task2, LuminTask<T> task3, LuminTask<T> task4, LuminTask<T> task5, LuminTask<T> task6, LuminTask<T> task7, LuminTask<T> task8)
    {
        return new WhenEachPromise<T>(task1, task2, task3, task4, task5, task6, task7, task8);
    }

    public static ILuminTaskAsyncEnumerable<WhenEachResult<T>> WhenEach<T>(LuminTask<T> task1, LuminTask<T> task2, LuminTask<T> task3, LuminTask<T> task4, LuminTask<T> task5, LuminTask<T> task6, LuminTask<T> task7, LuminTask<T> task8, LuminTask<T> task9)
    {
        return new WhenEachPromise<T>(task1, task2, task3, task4, task5, task6, task7, task8, task9);
    }

    public static ILuminTaskAsyncEnumerable<WhenEachResult<T>> WhenEach<T>(LuminTask<T> task1, LuminTask<T> task2, LuminTask<T> task3, LuminTask<T> task4, LuminTask<T> task5, LuminTask<T> task6, LuminTask<T> task7, LuminTask<T> task8, LuminTask<T> task9, LuminTask<T> task10)
    {
        return new WhenEachPromise<T>(task1, task2, task3, task4, task5, task6, task7, task8, task9, task10);
    }

    public static ILuminTaskAsyncEnumerable<WhenEachResult<T>> WhenEach<T>(LuminTask<T> task1, LuminTask<T> task2, LuminTask<T> task3, LuminTask<T> task4, LuminTask<T> task5, LuminTask<T> task6, LuminTask<T> task7, LuminTask<T> task8, LuminTask<T> task9, LuminTask<T> task10, LuminTask<T> task11)
    {
        return new WhenEachPromise<T>(task1, task2, task3, task4, task5, task6, task7, task8, task9, task10, task11);
    }

    public static ILuminTaskAsyncEnumerable<WhenEachResult<T>> WhenEach<T>(LuminTask<T> task1, LuminTask<T> task2, LuminTask<T> task3, LuminTask<T> task4, LuminTask<T> task5, LuminTask<T> task6, LuminTask<T> task7, LuminTask<T> task8, LuminTask<T> task9, LuminTask<T> task10, LuminTask<T> task11, LuminTask<T> task12)
    {
        return new WhenEachPromise<T>(task1, task2, task3, task4, task5, task6, task7, task8, task9, task10, task11, task12);
    }

    public static ILuminTaskAsyncEnumerable<WhenEachResult<T>> WhenEach<T>(LuminTask<T> task1, LuminTask<T> task2, LuminTask<T> task3, LuminTask<T> task4, LuminTask<T> task5, LuminTask<T> task6, LuminTask<T> task7, LuminTask<T> task8, LuminTask<T> task9, LuminTask<T> task10, LuminTask<T> task11, LuminTask<T> task12, LuminTask<T> task13)
    {
        return new WhenEachPromise<T>(task1, task2, task3, task4, task5, task6, task7, task8, task9, task10, task11, task12, task13);
    }

    public static ILuminTaskAsyncEnumerable<WhenEachResult<T>> WhenEach<T>(LuminTask<T> task1, LuminTask<T> task2, LuminTask<T> task3, LuminTask<T> task4, LuminTask<T> task5, LuminTask<T> task6, LuminTask<T> task7, LuminTask<T> task8, LuminTask<T> task9, LuminTask<T> task10, LuminTask<T> task11, LuminTask<T> task12, LuminTask<T> task13, LuminTask<T> task14)
    {
        return new WhenEachPromise<T>(task1, task2, task3, task4, task5, task6, task7, task8, task9, task10, task11, task12, task13, task14);
    }

    public static ILuminTaskAsyncEnumerable<WhenEachResult<T>> WhenEach<T>(LuminTask<T> task1, LuminTask<T> task2, LuminTask<T> task3, LuminTask<T> task4, LuminTask<T> task5, LuminTask<T> task6, LuminTask<T> task7, LuminTask<T> task8, LuminTask<T> task9, LuminTask<T> task10, LuminTask<T> task11, LuminTask<T> task12, LuminTask<T> task13, LuminTask<T> task14, LuminTask<T> task15)
    {
        return new WhenEachPromise<T>(task1, task2, task3, task4, task5, task6, task7, task8, task9, task10, task11, task12, task13, task14, task15);
    }
}

public readonly struct WhenEachResult<T>
{
    public T Result { get; }
    public Exception Exception { get; }
    
    public bool IsCompletedSuccessfully => Exception == null;
    
    public bool IsFaulted => Exception != null;

    public WhenEachResult(T result)
    {
        this.Result = result;
        this.Exception = null;
    }

    public WhenEachResult(Exception exception)
    {
        if (exception == null) throw new ArgumentNullException(nameof(exception));
        this.Result = default;
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

#region Promise Implementations

internal sealed class WhenEachPromise<T> : ILuminTaskAsyncEnumerable<WhenEachResult<T>>
{
    private readonly Awaiter<T>[] _awaiters;
    private readonly int _taskCount;
    
    public WhenEachPromise(LuminTask<T> task1)
    {
        _taskCount = 1;
        _awaiters = new Awaiter<T>[_taskCount];
        SetAwaiter(task1, 0);
    }
    
    public WhenEachPromise(LuminTask<T> task1, LuminTask<T> task2)
    {
        _taskCount = 2;
        _awaiters = new Awaiter<T>[_taskCount];
        SetAwaiter(task1, 0);
        SetAwaiter(task2, 1);
    }
    
    public WhenEachPromise(LuminTask<T> task1, LuminTask<T> task2, LuminTask<T> task3)
    {
        _taskCount = 3;
        _awaiters = new Awaiter<T>[_taskCount];
        SetAwaiter(task1, 0);
        SetAwaiter(task2, 1);
        SetAwaiter(task3, 2);
    }
    
    public WhenEachPromise(LuminTask<T> task1, LuminTask<T> task2, LuminTask<T> task3, LuminTask<T> task4)
    {
        _taskCount = 4;
        _awaiters = new Awaiter<T>[_taskCount];
        SetAwaiter(task1, 0);
        SetAwaiter(task2, 1);
        SetAwaiter(task3, 2);
        SetAwaiter(task4, 3);
    }
    
    public WhenEachPromise(LuminTask<T> task1, LuminTask<T> task2, LuminTask<T> task3, LuminTask<T> task4, LuminTask<T> task5)
    {
        _taskCount = 5;
        _awaiters = new Awaiter<T>[_taskCount];
        SetAwaiter(task1, 0);
        SetAwaiter(task2, 1);
        SetAwaiter(task3, 2);
        SetAwaiter(task4, 3);
        SetAwaiter(task5, 4);
    }
    
    public WhenEachPromise(LuminTask<T> task1, LuminTask<T> task2, LuminTask<T> task3, LuminTask<T> task4, LuminTask<T> task5, LuminTask<T> task6)
    {
        _taskCount = 6;
        _awaiters = new Awaiter<T>[_taskCount];
        SetAwaiter(task1, 0);
        SetAwaiter(task2, 1);
        SetAwaiter(task3, 2);
        SetAwaiter(task4, 3);
        SetAwaiter(task5, 4);
        SetAwaiter(task6, 5);
    }
    
    public WhenEachPromise(LuminTask<T> task1, LuminTask<T> task2, LuminTask<T> task3, LuminTask<T> task4, LuminTask<T> task5, LuminTask<T> task6, LuminTask<T> task7)
    {
        _taskCount = 7;
        _awaiters = new Awaiter<T>[_taskCount];
        SetAwaiter(task1, 0);
        SetAwaiter(task2, 1);
        SetAwaiter(task3, 2);
        SetAwaiter(task4, 3);
        SetAwaiter(task5, 4);
        SetAwaiter(task6, 5);
        SetAwaiter(task7, 6);
    }
    
    public WhenEachPromise(LuminTask<T> task1, LuminTask<T> task2, LuminTask<T> task3, LuminTask<T> task4, LuminTask<T> task5, LuminTask<T> task6, LuminTask<T> task7, LuminTask<T> task8)
    {
        _taskCount = 8;
        _awaiters = new Awaiter<T>[_taskCount];
        SetAwaiter(task1, 0);
        SetAwaiter(task2, 1);
        SetAwaiter(task3, 2);
        SetAwaiter(task4, 3);
        SetAwaiter(task5, 4);
        SetAwaiter(task6, 5);
        SetAwaiter(task7, 6);
        SetAwaiter(task8, 7);
    }
    
    public WhenEachPromise(LuminTask<T> task1, LuminTask<T> task2, LuminTask<T> task3, LuminTask<T> task4, LuminTask<T> task5, LuminTask<T> task6, LuminTask<T> task7, LuminTask<T> task8, LuminTask<T> task9)
    {
        _taskCount = 9;
        _awaiters = new Awaiter<T>[_taskCount];
        SetAwaiter(task1, 0);
        SetAwaiter(task2, 1);
        SetAwaiter(task3, 2);
        SetAwaiter(task4, 3);
        SetAwaiter(task5, 4);
        SetAwaiter(task6, 5);
        SetAwaiter(task7, 6);
        SetAwaiter(task8, 7);
        SetAwaiter(task9, 8);
    }
    
    public WhenEachPromise(LuminTask<T> task1, LuminTask<T> task2, LuminTask<T> task3, LuminTask<T> task4, LuminTask<T> task5, LuminTask<T> task6, LuminTask<T> task7, LuminTask<T> task8, LuminTask<T> task9, LuminTask<T> task10)
    {
        _taskCount = 10;
        _awaiters = new Awaiter<T>[_taskCount];
        SetAwaiter(task1, 0);
        SetAwaiter(task2, 1);
        SetAwaiter(task3, 2);
        SetAwaiter(task4, 3);
        SetAwaiter(task5, 4);
        SetAwaiter(task6, 5);
        SetAwaiter(task7, 6);
        SetAwaiter(task8, 7);
        SetAwaiter(task9, 8);
        SetAwaiter(task10, 9);
    }
    
    public WhenEachPromise(LuminTask<T> task1, LuminTask<T> task2, LuminTask<T> task3, LuminTask<T> task4, LuminTask<T> task5, LuminTask<T> task6, LuminTask<T> task7, LuminTask<T> task8, LuminTask<T> task9, LuminTask<T> task10, LuminTask<T> task11)
    {
        _taskCount = 11;
        _awaiters = new Awaiter<T>[_taskCount];
        SetAwaiter(task1, 0);
        SetAwaiter(task2, 1);
        SetAwaiter(task3, 2);
        SetAwaiter(task4, 3);
        SetAwaiter(task5, 4);
        SetAwaiter(task6, 5);
        SetAwaiter(task7, 6);
        SetAwaiter(task8, 7);
        SetAwaiter(task9, 8);
        SetAwaiter(task10, 9);
        SetAwaiter(task11, 10);
    }
    
    public WhenEachPromise(LuminTask<T> task1, LuminTask<T> task2, LuminTask<T> task3, LuminTask<T> task4, LuminTask<T> task5, LuminTask<T> task6, LuminTask<T> task7, LuminTask<T> task8, LuminTask<T> task9, LuminTask<T> task10, LuminTask<T> task11, LuminTask<T> task12)
    {
        _taskCount = 12;
        _awaiters = new Awaiter<T>[_taskCount];
        SetAwaiter(task1, 0);
        SetAwaiter(task2, 1);
        SetAwaiter(task3, 2);
        SetAwaiter(task4, 3);
        SetAwaiter(task5, 4);
        SetAwaiter(task6, 5);
        SetAwaiter(task7, 6);
        SetAwaiter(task8, 7);
        SetAwaiter(task9, 8);
        SetAwaiter(task10, 9);
        SetAwaiter(task11, 10);
        SetAwaiter(task12, 11);
    }
    
    public WhenEachPromise(LuminTask<T> task1, LuminTask<T> task2, LuminTask<T> task3, LuminTask<T> task4, LuminTask<T> task5, LuminTask<T> task6, LuminTask<T> task7, LuminTask<T> task8, LuminTask<T> task9, LuminTask<T> task10, LuminTask<T> task11, LuminTask<T> task12, LuminTask<T> task13)
    {
        _taskCount = 13;
        _awaiters = new Awaiter<T>[_taskCount];
        SetAwaiter(task1, 0);
        SetAwaiter(task2, 1);
        SetAwaiter(task3, 2);
        SetAwaiter(task4, 3);
        SetAwaiter(task5, 4);
        SetAwaiter(task6, 5);
        SetAwaiter(task7, 6);
        SetAwaiter(task8, 7);
        SetAwaiter(task9, 8);
        SetAwaiter(task10, 9);
        SetAwaiter(task11, 10);
        SetAwaiter(task12, 11);
        SetAwaiter(task13, 12);
    }
    
    public WhenEachPromise(LuminTask<T> task1, LuminTask<T> task2, LuminTask<T> task3, LuminTask<T> task4, LuminTask<T> task5, LuminTask<T> task6, LuminTask<T> task7, LuminTask<T> task8, LuminTask<T> task9, LuminTask<T> task10, LuminTask<T> task11, LuminTask<T> task12, LuminTask<T> task13, LuminTask<T> task14)
    {
        _taskCount = 14;
        _awaiters = new Awaiter<T>[_taskCount];
        SetAwaiter(task1, 0);
        SetAwaiter(task2, 1);
        SetAwaiter(task3, 2);
        SetAwaiter(task4, 3);
        SetAwaiter(task5, 4);
        SetAwaiter(task6, 5);
        SetAwaiter(task7, 6);
        SetAwaiter(task8, 7);
        SetAwaiter(task9, 8);
        SetAwaiter(task10, 9);
        SetAwaiter(task11, 10);
        SetAwaiter(task12, 11);
        SetAwaiter(task13, 12);
        SetAwaiter(task14, 13);
    }
    
    public WhenEachPromise(LuminTask<T> task1, LuminTask<T> task2, LuminTask<T> task3, LuminTask<T> task4, LuminTask<T> task5, LuminTask<T> task6, LuminTask<T> task7, LuminTask<T> task8, LuminTask<T> task9, LuminTask<T> task10, LuminTask<T> task11, LuminTask<T> task12, LuminTask<T> task13, LuminTask<T> task14, LuminTask<T> task15)
    {
        _taskCount = 15;
        _awaiters = new Awaiter<T>[_taskCount];
        SetAwaiter(task1, 0);
        SetAwaiter(task2, 1);
        SetAwaiter(task3, 2);
        SetAwaiter(task4, 3);
        SetAwaiter(task5, 4);
        SetAwaiter(task6, 5);
        SetAwaiter(task7, 6);
        SetAwaiter(task8, 7);
        SetAwaiter(task9, 8);
        SetAwaiter(task10, 9);
        SetAwaiter(task11, 10);
        SetAwaiter(task12, 11);
        SetAwaiter(task13, 12);
        SetAwaiter(task14, 13);
        SetAwaiter(task15, 14);
    }
    
    private void SetAwaiter(LuminTask<T> task, int index)
    {
        try
        {
            _awaiters[index] = new Awaiter<T>(task.GetAwaiter());
        }
        catch (Exception ex)
        {
            _awaiters[index] = new Awaiter<T>(ex);
        }
    }

    public ILuminTaskAsyncEnumerator<WhenEachResult<T>> GetAsyncEnumerator(
        CancellationToken cancellationToken = default)
    {
        return new Enumerator(_awaiters, _taskCount, cancellationToken);
    }
    
    private readonly struct Awaiter<TResult>
    {
        internal readonly LuminTaskAwaiter<TResult> _awaiter;
        internal readonly Exception _exception;

        public bool IsCompleted => true;

        public Awaiter(LuminTaskAwaiter<TResult> awaiter)
        {
            _awaiter = awaiter;
            _exception = null;
        }
        
        public Awaiter(Exception exception)
        {
            if (exception is null) throw new ArgumentNullException(nameof(exception));
            _awaiter = default;
            _exception = exception;
        }

        public TResult GetResult()
        {
            if (_exception != null)
            {
                ExceptionDispatchInfo.Capture(_exception).Throw();
            }
            return _awaiter.GetResult();
        }

        public void OnCompleted(Action continuation) => continuation?.Invoke();
        public void UnsafeOnCompleted(Action continuation) => continuation?.Invoke();
    }

    private sealed class Enumerator : ILuminTaskAsyncEnumerator<WhenEachResult<T>>
    {
        private readonly Awaiter<T>[] _awaiters;
        private readonly int _taskCount;
        private readonly CancellationToken _cancellationToken;
        private readonly CancellationTokenSource _linkedCts;
        private readonly object _lock = new();
        private AutoResetLuminTaskCompletionSource<bool> _waitSource;
        private int _remainingTasks;
        private bool _allTasksCompleted;
        private bool _waiting;
        private bool _disposed;
        private WhenEachResult<T> _current;

        public WhenEachResult<T> Current => _current;

        public Enumerator(
            Awaiter<T>[] awaiters, 
            int taskCount,
            CancellationToken cancellationToken)
        {
            _awaiters = awaiters;
            _taskCount = taskCount;
            _remainingTasks = taskCount;
            _cancellationToken = cancellationToken;
            _linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            _waitSource = AutoResetLuminTaskCompletionSource<bool>.Create();
            _waiting = false;
        
            StartAllTasks();
        }

        private void StartAllTasks()
        {
            for (int i = 0; i < _taskCount; i++)
            {
                var index = i;
                var awaiter = _awaiters[i];
            
                if (awaiter._exception != null)
                {
                    // 处理立即异常
                    OnTaskCompleted(new WhenEachResult<T>(awaiter._exception), index);
                }
                else if (awaiter._awaiter.IsCompleted)
                {
                    // 同步处理已完成任务
                    ProcessCompletedAwaiter(awaiter._awaiter, index);
                }
                else
                {
                    // 异步等待任务完成
                    awaiter._awaiter.SourceOnCompleted(static state => 
                    {
                        var tuple = (StateTuple<Enumerator, int>)state;
                        tuple.Item1.ProcessAwaiter(tuple.Item2);
                    }, StateTuple.Create(this, index));
                }
            }
        }

        private void ProcessAwaiter(int index)
        {
            if (_disposed) return;
        
            var awaiter = _awaiters[index]._awaiter;
            ProcessCompletedAwaiter(awaiter, index);
        }

        private void ProcessCompletedAwaiter(in LuminTaskAwaiter<T> awaiter, int index)
        {
            try
            {
                var result = awaiter.GetResult();
                OnTaskCompleted(new WhenEachResult<T>(result), index);
            }
            catch (Exception ex)
            {
                OnTaskCompleted(new WhenEachResult<T>(ex), index);
            }
        }

        private void OnTaskCompleted(WhenEachResult<T> result, int index)
        {
            if (_disposed) return;
            
            // 使用锁确保线程安全
            lock (_lock)
            {
                _current = result;
                if (_waiting)
                {
                    _waiting = false;
                    // 重置等待源
                    var oldSource = _waitSource;
                    _waitSource = AutoResetLuminTaskCompletionSource<bool>.Create();
                    oldSource.TrySetResult(true);
                    oldSource.TryReturn();
                    
                }
            }
        
            var remaining = Interlocked.Decrement(ref _remainingTasks);
            if (remaining <= 0)
            {
                OnAllTasksCompleted();
            }
        }

        private void OnAllTasksCompleted()
        {
            // 使用锁确保线程安全
            lock (_lock)
            {
                _allTasksCompleted = true;
                if (_waiting)
                {
                    _waiting = false;
                    var oldSource = _waitSource;
                    _waitSource = AutoResetLuminTaskCompletionSource<bool>.Create();
                    oldSource.TrySetResult(false);
                    oldSource.TryReturn();
                }
            }
        }

        public LuminTask<bool> MoveNextAsync()
        {
            if (_disposed) 
                return LuminTask.FromResult(false);
    
            _cancellationToken.ThrowIfCancellationRequested();

            // 使用锁确保线程安全
            lock (_lock)
            {
            
                // 检查是否所有任务已完成
                if (_allTasksCompleted || _remainingTasks == 0)
                {
                    return LuminTask.FromResult(false);
                }

                // 设置等待状态
                _waiting = true;
            
                // 返回当前等待源的任务
                return _waitSource.Task;
            }
        }

        public LuminTask DisposeAsync()
        {
            if (_disposed) 
                return LuminTask.CompletedTask();
    
            _disposed = true;
    
            // 使用锁确保线程安全
            AutoResetLuminTaskCompletionSource<bool> oldSource = null;
            lock (_lock)
            {
                if (_waiting)
                {
                    _waiting = false;
                    oldSource = _waitSource;
                    oldSource.TrySetException(new OperationCanceledException(_cancellationToken));
                }
            }
        
            // 取消所有任务
            _linkedCts.Cancel();
            _linkedCts.Dispose();
        
            // 返回等待源到对象池
            if (oldSource != null)
            {
                oldSource.TryReturn();
            }
            _waitSource?.TryReturn();
        
            return LuminTask.CompletedTask();
        }
    }
}

#endregion