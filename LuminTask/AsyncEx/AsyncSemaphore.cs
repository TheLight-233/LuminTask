using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;

namespace LuminThread.AsyncEx
{
    [DebuggerDisplay("Id = {Id}, CurrentCount = {_count}")]
    public sealed class AsyncSemaphore : IDisposable
    {
        private readonly object _mutex = new object();
        private readonly IAsyncWaitQueue<object> _queue;
        private long _count;
        private int _id;
        private volatile bool _isDisposed;

        public AsyncSemaphore(long initialCount)
        {
            if (initialCount < 0)
                throw new ArgumentOutOfRangeException(nameof(initialCount));

            _queue = new DefaultAsyncWaitQueue<object>();
            _count = initialCount;
        }

        ~AsyncSemaphore() => Dispose(false);

        public int Id
        {
            get
            {
                if (_id == 0)
                    Interlocked.CompareExchange(ref _id, GetNextId(), 0);
                return _id;
            }
        }

        private static int _nextId = 1;
        private static int GetNextId() => Interlocked.Increment(ref _nextId);

        public long CurrentCount
        {
            get { lock (_mutex) return _count; }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public LuminTask WaitAsync(CancellationToken cancellationToken = default)
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(AsyncSemaphore));

            if (cancellationToken.IsCancellationRequested)
                return LuminTask.FromCanceled(cancellationToken);

            lock (_mutex)
            {
                if (_count > 0)
                {
                    _count--;
                    return LuminTask.CompletedTask();
                }

                return _queue.Enqueue(cancellationToken);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Release(long releaseCount = 1)
        {
            if (releaseCount < 0)
                throw new ArgumentOutOfRangeException(nameof(releaseCount));

            if (releaseCount == 0)
                return;

            lock (_mutex)
            {
                checked { var test = _count + releaseCount; }

                while (releaseCount > 0 && !_queue.IsEmpty)
                {
                    _queue.Dequeue(null);
                    releaseCount--;
                }

                _count += releaseCount;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public LuminTask<IDisposable> LockAsync(CancellationToken cancellationToken = default)
        {
            return LockInternalAsync(cancellationToken);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private async LuminTask<IDisposable> LockInternalAsync(CancellationToken cancellationToken)
        {
            await WaitAsync(cancellationToken);
            return new SemaphoreLock(this);
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
                _isDisposed = true;
                _queue.CancelAll(default);
            }
        }

        private sealed class SemaphoreLock : IDisposable
        {
            private AsyncSemaphore _semaphore;

            public SemaphoreLock(AsyncSemaphore semaphore)
            {
                _semaphore = semaphore;
            }

            public void Dispose()
            {
                var semaphore = Interlocked.Exchange(ref _semaphore, null);
                semaphore?.Release();
            }
        }
    }

    public static class AsyncSemaphoreExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static LuminTask WaitAsync(this AsyncSemaphore semaphore, TimeSpan timeout)
        {
            using var cts = new CancellationTokenSource(timeout);
            return semaphore.WaitAsync(cts.Token);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static LuminTask<IDisposable> LockAsync(this AsyncSemaphore semaphore, TimeSpan timeout)
        {
            using var cts = new CancellationTokenSource(timeout);
            return semaphore.LockAsync(cts.Token);
        }
    }
}
