using System;
using System.Runtime.CompilerServices;
using System.Threading;

namespace LuminThread.AsyncEx;

public sealed class AsyncCountDownEvent
{
    private readonly AsyncLock _asyncLock;
    private readonly AsyncConditionVariable _conditionVariable;
    private volatile int _currentCount;
    private volatile int _isDisposed; // 0 = false, 1 = true

    public AsyncCountDownEvent(int initialCount)
    {
        if (initialCount < 0)
            throw new ArgumentOutOfRangeException(nameof(initialCount), "Initial count must be non-negative");

        _asyncLock = new AsyncLock();
        _conditionVariable = new AsyncConditionVariable(_asyncLock);
        _currentCount = initialCount;
        _isDisposed = 0;
    }

    public int CurrentCount => _currentCount;
    public bool IsSet => _currentCount == 0;
    public bool IsDisposed => _isDisposed == 1;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public LuminTask<bool> WaitAsync(CancellationToken cancellationToken = default)
    {
        if (_isDisposed == 1)
            throw new ObjectDisposedException(nameof(AsyncCountDownEvent));

        // 快速路径：如果已经归零，直接返回
        if (_currentCount == 0)
            return new LuminTask<bool>(true);

        return WaitInternalAsync(cancellationToken);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private async LuminTask<bool> WaitInternalAsync(CancellationToken cancellationToken)
    {
        // 添加空检查
        if (_asyncLock == null || _conditionVariable == null)
            return false;

        var lockScope = await _asyncLock.LockAsync(cancellationToken);
        if (lockScope.Equals(default(AsyncLock.ReleaseScope)))
        {
            return false;
        }

        try
        {
            using (lockScope)
            {
                while (_currentCount > 0 && _isDisposed == 0)
                {
                    var waitTask = _conditionVariable.WaitAsync(cancellationToken);
                    if (waitTask.Equals(default(LuminTask<AsyncLock.ReleaseScope>)))
                    {
                        break;
                    }
                    
                    var newLockScope = await waitTask;
                    if (newLockScope.Equals(default(AsyncLock.ReleaseScope)))
                    {
                        break;
                    }
                    
                    // 释放旧的锁范围，使用新的
                    lockScope.Dispose();
                    lockScope = newLockScope;
                }
                
                return _currentCount == 0;
            }
        }
        catch
        {
            lockScope.Dispose();
            return false;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Signal()
    {
        if (_isDisposed == 1)
            throw new ObjectDisposedException(nameof(AsyncCountDownEvent));

        if (_currentCount <= 0)
            throw new InvalidOperationException("Cannot signal when count is already zero");

        var newCount = Interlocked.Decrement(ref _currentCount);
        
        if (newCount == 0)
        {
            _conditionVariable?.NotifyAll();
            return true;
        }

        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Signal(int signalCount)
    {
        if (_isDisposed == 1)
            throw new ObjectDisposedException(nameof(AsyncCountDownEvent));

        if (signalCount <= 0)
            throw new ArgumentOutOfRangeException(nameof(signalCount), "Signal count must be positive");

        if (_currentCount < signalCount)
            throw new InvalidOperationException("Cannot signal more than the current count");

        var newCount = Interlocked.Add(ref _currentCount, -signalCount);
        
        if (newCount == 0)
        {
            _conditionVariable?.NotifyAll();
            return true;
        }

        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AddCount()
    {
        AddCount(1);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AddCount(int signalCount)
    {
        if (_isDisposed == 1)
            throw new ObjectDisposedException(nameof(AsyncCountDownEvent));

        if (signalCount <= 0)
            throw new ArgumentOutOfRangeException(nameof(signalCount), "Signal count must be positive");

        Interlocked.Add(ref _currentCount, signalCount);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryAddCount()
    {
        return TryAddCount(1);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryAddCount(int signalCount)
    {
        if (_isDisposed == 1 || signalCount <= 0)
            return false;

        Interlocked.Add(ref _currentCount, signalCount);
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Reset()
    {
        Reset(1);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Reset(int count)
    {
        if (_isDisposed == 1)
            throw new ObjectDisposedException(nameof(AsyncCountDownEvent));

        if (count < 0)
            throw new ArgumentOutOfRangeException(nameof(count), "Count must be non-negative");

        Interlocked.Exchange(ref _currentCount, count);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public LuminTask<bool> WaitAsync(TimeSpan timeout)
    {
        using var cts = new CancellationTokenSource(timeout);
        return WaitAsync(cts.Token);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Dispose()
    {
        if (Interlocked.CompareExchange(ref _isDisposed, 1, 0) == 0)
        {
            // 通知所有等待者
            _conditionVariable?.NotifyAll();
        }
    }
}