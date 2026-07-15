using System;
using System.Runtime.CompilerServices;
using System.Threading;

namespace LuminThread.AsyncEx
{
    public sealed class AsyncCountdownEvent : IDisposable
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

        ~AsyncCountdownEvent() => Dispose(false);

        public int CurrentCount => _currentCount;
        public bool IsSet => _currentCount == 0;
        public bool IsDisposed => _isDisposed;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public LuminTask WaitAsync(CancellationToken cancellationToken = default)
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(AsyncCountdownEvent));

            if (_currentCount == 0)
                return LuminTask.CompletedTask();

            if (cancellationToken.IsCancellationRequested)
                return LuminTask.FromCanceled(cancellationToken);

            lock (_mutex)
            {
                if (_currentCount == 0)
                    return LuminTask.CompletedTask();

                return _queue.Enqueue(cancellationToken);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Signal() => Signal(1);

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
                    _queue.DequeueAll(null);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddCount() => AddCount(1);

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
        public bool TryAddCount() => TryAddCount(1);

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
        public void Reset() => Reset(1);

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

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                lock (_mutex)
                {
                    if (!_isDisposed)
                    {
                        _isDisposed = true;
                        if (disposing)
                            _queue.CancelAll(default);
                    }
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
