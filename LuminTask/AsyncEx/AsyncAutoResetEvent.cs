using System;
using System.Runtime.CompilerServices;
using System.Threading;

namespace LuminThread.AsyncEx;

public sealed class AsyncAutoResetEvent
{
    private readonly AsyncLock _asyncLock;
    private readonly AsyncConditionVariable _conditionVariable;
    private volatile int _isSet; // 0 = false, 1 = true
    private volatile int _hasWaitingTask; // 0 = false, 1 = true

    public AsyncAutoResetEvent(bool initialState = false)
    {
        _asyncLock = new AsyncLock();
        _conditionVariable = new AsyncConditionVariable(_asyncLock);
        _isSet = initialState ? 1 : 0;
        _hasWaitingTask = 0;
    }

    public bool IsSet => _isSet == 1;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public LuminTask<bool> WaitAsync(CancellationToken cancellationToken = default)
    {
        // 快速路径：如果已设置且没有等待任务，则直接返回
        if (_isSet == 1 && Interlocked.CompareExchange(ref _hasWaitingTask, 1, 0) == 0)
        {
            Interlocked.Exchange(ref _isSet, 0);
            return new LuminTask<bool>(true);
        }

        return WaitInternalAsync(cancellationToken);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private async LuminTask<bool> WaitInternalAsync(CancellationToken cancellationToken)
    {
        using (await _asyncLock.LockAsync(cancellationToken))
        {
            Interlocked.Exchange(ref _hasWaitingTask, 1);
                
            while (_isSet == 0)
            {
                await _conditionVariable.WaitAsync(cancellationToken);
            }

            Interlocked.Exchange(ref _isSet, 0);
            Interlocked.Exchange(ref _hasWaitingTask, 0);
            return true;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Set()
    {
        if (Interlocked.CompareExchange(ref _isSet, 1, 0) == 0)
        {
            _conditionVariable.Notify();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public LuminTask<bool> WaitAsync(TimeSpan timeout)
    {
        using var cts = new CancellationTokenSource(timeout);
        return WaitAsync(cts.Token);
    }
}

public sealed class AsyncAutoResetEvent<T>
{
    private readonly AsyncLock _asyncLock;
    private readonly AsyncConditionVariable _conditionVariable;
    private volatile int _isSet; // 0 = false, 1 = true
    private volatile int _hasWaitingTask; // 0 = false, 1 = true
    private T _result;

    public AsyncAutoResetEvent()
    {
        _asyncLock = new AsyncLock();
        _conditionVariable = new AsyncConditionVariable(_asyncLock);
        _isSet = 0;
        _hasWaitingTask = 0;
    }

    public bool IsSet => _isSet == 1;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public LuminTask<T> WaitAsync(CancellationToken cancellationToken = default)
    {
        // 快速路径：如果已设置且没有等待任务，则直接返回
        if (_isSet == 1 && Interlocked.CompareExchange(ref _hasWaitingTask, 1, 0) == 0)
        {
            var result = _result;
            Interlocked.Exchange(ref _isSet, 0);
            _result = default!;
            return new LuminTask<T>(result);
        }

        return WaitInternalAsync(cancellationToken);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private async LuminTask<T> WaitInternalAsync(CancellationToken cancellationToken)
    {
        using (await _asyncLock.LockAsync(cancellationToken))
        {
            Interlocked.Exchange(ref _hasWaitingTask, 1);
                
            while (_isSet == 0)
            {
                await _conditionVariable.WaitAsync(cancellationToken);
            }

            var result = _result;
            Interlocked.Exchange(ref _isSet, 0);
            _result = default!;
            Interlocked.Exchange(ref _hasWaitingTask, 0);
            return result;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Set(T result)
    {
        using (_asyncLock.LockAsync().Result)
        {
            if (_isSet == 0)
            {
                Interlocked.Exchange(ref _isSet, 1);
                _result = result;
                _conditionVariable.Notify();
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public LuminTask<T> WaitAsync(TimeSpan timeout)
    {
        using var cts = new CancellationTokenSource(timeout);
        return WaitAsync(cts.Token);
    }
}