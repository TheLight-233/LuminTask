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

internal sealed class WhenEachPromise<T> : ILuminTaskAsyncEnumerable<WhenEachResult<T>>
{
    private readonly LuminTask<T>[] _tasks;
    private readonly int _taskCount;

    public WhenEachPromise(LuminTask<T>[] tasks)
    {
        _taskCount = tasks.Length;
        _tasks = tasks;
    }
    
    public WhenEachPromise(IEnumerable<LuminTask<T>> tasks)
    {
        _tasks = tasks.ToArray();
        _taskCount = _tasks.Length;
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
        return new Enumerator(_tasks, _taskCount, cancellationToken);
    }
    
    private sealed class Enumerator : ILuminTaskAsyncEnumerator<WhenEachResult<T>>
    {
        private readonly LuminTask<T>[] _tasks;
        private readonly int _taskCount;
        private readonly CancellationToken _cancellationToken;
        private readonly LuminChannel<WhenEachResult<T>> _channel;
        private int _remainingTasks;
        private bool _disposed;
        private WhenEachResult<T> _current;
        private bool _allTasksCompleted;

        public WhenEachResult<T> Current => _current;

         public Enumerator(
             LuminTask<T>[] tasks, 
            int taskCount,
            CancellationToken cancellationToken)
        {
            _tasks = tasks;
            _taskCount = taskCount;
            _remainingTasks = taskCount;
            _cancellationToken = cancellationToken;
            
            _channel = LuminChannel.CreateSingleConsumerUnbounded<WhenEachResult<T>>();
            
            StartAllTasks();
        }

        private void StartAllTasks()
        {
            for (int i = 0; i < _taskCount; i++)
            {
                ProcessTaskAsync(i);
            }
        }

        private async void ProcessTaskAsync(int index)
        {
            try
            {
                var result = await _tasks[index];
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
            
            _channel.Writer.TryWrite(result);
            
            var remaining = Interlocked.Decrement(ref _remainingTasks);
            if (remaining <= 0)
            {
                OnAllTasksCompleted();
            }
        }

        private void OnAllTasksCompleted()
        {
            _allTasksCompleted = true;
            _channel.Writer.TryComplete();
        }

        public async LuminTask<bool> MoveNextAsync()
        {
            if (_disposed)
                return false;

            _cancellationToken.ThrowIfCancellationRequested();
            
            if (_allTasksCompleted)
            {
                if (_channel.Reader.TryRead(out var result))
                {
                    _current = result;
                    return true;
                }
                return false;
            }
            
            if (await _channel.Reader.WaitToReadAsync(_cancellationToken))
            {
                if (_channel.Reader.TryRead(out var result))
                {
                    _current = result;
                    return true;
                }
            }

            return false;
        }

        public LuminTask DisposeAsync()
        {
            if (_disposed) 
                return LuminTask.CompletedTask();
    
            _disposed = true;
            
            _channel.Writer.TryComplete();
            
            return LuminTask.CompletedTask();
        }
    }
}