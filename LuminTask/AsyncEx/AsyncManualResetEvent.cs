using System;
using System.Runtime.CompilerServices;
using System.Threading;

namespace LuminThread.AsyncEx
{
    public sealed class AsyncManualResetEvent
    {
        private readonly AsyncLock _asyncLock;
        private volatile bool _isSet;

        public AsyncManualResetEvent(bool initialState = false)
        {
            _asyncLock = new AsyncLock();
            _isSet = initialState;
        }

        public bool IsSet => _isSet;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public LuminTask<bool> WaitAsync(CancellationToken cancellationToken = default)
        {
            if (_isSet)
            {
                return new LuminTask<bool>(true);
            }

            return WaitInternalAsync(cancellationToken);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private async LuminTask<bool> WaitInternalAsync(CancellationToken cancellationToken)
        {
            using (await _asyncLock.LockAsync(cancellationToken))
            {
                while (!_isSet)
                {
                    var condition = new AsyncConditionVariable(_asyncLock);
                    await condition.WaitAsync(cancellationToken);
                }
                return true;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Set()
        {
            if (!_isSet)
            {
                _isSet = true;
                using (_asyncLock.LockAsync().Result)
                {
                    // 锁内设置确保可见性，实际通知由等待循环处理
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Reset()
        {
            _isSet = false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public LuminTask<bool> WaitAsync(TimeSpan timeout)
        {
            using var cts = new CancellationTokenSource(timeout);
            return WaitAsync(cts.Token);
        }
    }

    public sealed class AsyncManualResetEvent<T>
    {
        private readonly AsyncLock _asyncLock;
        private volatile bool _isSet;
        private T _result;

        public AsyncManualResetEvent()
        {
            _asyncLock = new AsyncLock();
        }

        public bool IsSet => _isSet;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public LuminTask<T> WaitAsync(CancellationToken cancellationToken = default)
        {
            if (_isSet)
            {
                return new LuminTask<T>(_result);
            }

            return WaitInternalAsync(cancellationToken);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private async LuminTask<T> WaitInternalAsync(CancellationToken cancellationToken)
        {
            using (await _asyncLock.LockAsync(cancellationToken))
            {
                while (!_isSet)
                {
                    var condition = new AsyncConditionVariable(_asyncLock);
                    await condition.WaitAsync(cancellationToken);
                }
                return _result;
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
        public void Reset()
        {
            using (_asyncLock.LockAsync().Result)
            {
                _isSet = false;
                _result = default;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public LuminTask<T> WaitAsync(TimeSpan timeout)
        {
            using var cts = new CancellationTokenSource(timeout);
            return WaitAsync(cts.Token);
        }
    }
}