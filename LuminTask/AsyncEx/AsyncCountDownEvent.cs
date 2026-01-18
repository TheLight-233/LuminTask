using System;
using System.Runtime.CompilerServices;
using System.Threading;

namespace LuminThread.AsyncEx
{
    /// <summary>
    /// 高性能异步倒计时事件
    /// </summary>
    public sealed class AsyncCountdownEvent
    {
        private readonly object _mutex = new object();
        private readonly IAsyncWaitQueue<object> _queue;
        private volatile int _currentCount;
        private volatile bool _isDisposed;

        public AsyncCountdownEvent(int initialCount)
        {
            if (initialCount < 0)
                throw new ArgumentOutOfRangeException(nameof(initialCount));

            _queue = new DefaultAsyncWaitQueue<object>();
            _currentCount = initialCount;
        }

        public int CurrentCount => _currentCount;
        public bool IsSet => _currentCount == 0;
        public bool IsDisposed => _isDisposed;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public LuminTask WaitAsync(CancellationToken cancellationToken = default)
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(AsyncCountdownEvent));

            // 快速路径：已归零
            if (_currentCount == 0)
            {
                return LuminTask.CompletedTask();
            }

            if (cancellationToken.IsCancellationRequested)
            {
                return LuminTask.FromCanceled(cancellationToken);
            }

            lock (_mutex)
            {
                // 双重检查
                if (_currentCount == 0)
                {
                    return LuminTask.CompletedTask();
                }

                // 使用扩展方法，自动处理取消
                return _queue.Enqueue(cancellationToken);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Signal()
        {
            Signal(1);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Signal(int signalCount)
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(AsyncCountdownEvent));

            if (signalCount <= 0)
                throw new ArgumentOutOfRangeException(nameof(signalCount));

            lock (_mutex)
            {
                if (_currentCount < signalCount)
                    throw new InvalidOperationException("Cannot signal more than current count");

                _currentCount -= signalCount;

                if (_currentCount == 0)
                {
                    // 唤醒所有等待者
                    _queue.DequeueAll(null);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddCount()
        {
            AddCount(1);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddCount(int count)
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(AsyncCountdownEvent));

            if (count <= 0)
                throw new ArgumentOutOfRangeException(nameof(count));

            lock (_mutex)
            {
                _currentCount += count;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryAddCount()
        {
            return TryAddCount(1);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryAddCount(int count)
        {
            if (_isDisposed || count <= 0)
                return false;

            lock (_mutex)
            {
                _currentCount += count;
                return true;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Reset()
        {
            Reset(1);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Reset(int count)
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(AsyncCountdownEvent));

            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count));

            lock (_mutex)
            {
                _currentCount = count;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Dispose()
        {
            if (!_isDisposed)
            {
                lock (_mutex)
                {
                    _isDisposed = true;
                    _queue.DequeueAll(null);
                }
            }
        }
    }

    public static class AsyncCountdownEventExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static LuminTask WaitAsync(this AsyncCountdownEvent evt, TimeSpan timeout)
        {
            using var cts = new CancellationTokenSource(timeout);
            return evt.WaitAsync(cts.Token);
        }
    }
}
