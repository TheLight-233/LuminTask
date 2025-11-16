using System;
using System.Runtime.CompilerServices;
using System.Threading;

namespace LuminThread.AsyncEx;

public sealed class AsyncAutoResetEvent
{
    private readonly AsyncLock _asyncLock;
    private volatile bool _isSet;
    private volatile bool _hasWaitingTask;

    public AsyncAutoResetEvent(bool initialState = false)
    {
        _asyncLock = new AsyncLock();
        _isSet = initialState;
        _hasWaitingTask = false;
    }

    public bool IsSet => _isSet;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public LuminTask<bool> WaitAsync(CancellationToken cancellationToken = default)
    {
        if (_isSet && !_hasWaitingTask)
        {
            _isSet = false;
            return new LuminTask<bool>(true);
        }

        return WaitInternalAsync(cancellationToken);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private async LuminTask<bool> WaitInternalAsync(CancellationToken cancellationToken)
    {
        using (await _asyncLock.LockAsync(cancellationToken))
        {
            _hasWaitingTask = true;
                
            while (!_isSet)
            {
                var condition = new AsyncConditionVariable(_asyncLock);
                await condition.WaitAsync(cancellationToken);
            }

            _isSet = false;
            _hasWaitingTask = false;
            return true;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Set()
    {
        using (_asyncLock.LockAsync().Result)
        {
            if (!_isSet)
            {
                _isSet = true;
            }
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
    private volatile bool _isSet;
    private volatile bool _hasWaitingTask;
    private T _result;

    public AsyncAutoResetEvent()
    {
        _asyncLock = new AsyncLock();
        _isSet = false;
        _hasWaitingTask = false;
    }

    public bool IsSet => _isSet;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public LuminTask<T> WaitAsync(CancellationToken cancellationToken = default)
    {
        if (_isSet && !_hasWaitingTask)
        {
            var result = _result;
            _isSet = false;
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
            _hasWaitingTask = true;
                
            while (!_isSet)
            {
                var condition = new AsyncConditionVariable(_asyncLock);
                await condition.WaitAsync(cancellationToken);
            }

            var result = _result;
            _isSet = false;
            _result = default!;
            _hasWaitingTask = false;
            return result;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Set(T result)
    {
        using (_asyncLock.LockAsync().Result)
        {
            if (!_isSet)
            {
                _isSet = true;
                _result = result;
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