using System;
using System.Runtime.CompilerServices;
using System.Threading;

namespace LuminThread.AsyncEx
{
    /// <summary>异步信号量。值类型句柄，共享内部状态。</summary>
    public readonly struct AsyncSemaphore : IDisposable
    {
        private readonly SemaphoreState? _state;

        public AsyncSemaphore(long initialCount)
        {
            if (initialCount < 0)
                throw new ArgumentOutOfRangeException(nameof(initialCount));

            _state = new SemaphoreState(initialCount);
        }

        public int Id => GetState().Id;

        public long CurrentCount => GetState().CurrentCount;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public LuminTask WaitAsync(CancellationToken cancellationToken = default)
            => GetState().WaitAsync(cancellationToken);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Release(long releaseCount = 1) => GetState().Release(releaseCount);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public LuminTask<SemaphoreReleaser> LockAsync(CancellationToken cancellationToken = default)
            => GetState().LockAsync(cancellationToken);

        public void Dispose() => GetState().Dispose();

        private SemaphoreState GetState()
            => _state ?? throw new InvalidOperationException("AsyncSemaphore 未初始化（default 值）或已释放");
    }

    /// <summary>信号量锁句柄。Dispose 即释放。结构体，获取锁无分配。</summary>
    public readonly struct SemaphoreReleaser : IDisposable
    {
        private readonly SemaphoreState? _state;

        internal SemaphoreReleaser(SemaphoreState state)
        {
            _state = state;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Dispose() => _state?.Release();
    }

    internal sealed class SemaphoreState : IDisposable
    {
        private static int s_nextId = 1;

        private readonly object _mutex = new object();
        private readonly IAsyncWaitQueue<object> _queue;
        private long _count;
        private int _id;
        private volatile bool _isDisposed;

        public SemaphoreState(long initialCount)
        {
            _queue = new DefaultAsyncWaitQueue<object>();
            _count = initialCount;
        }

        public int Id
        {
            get
            {
                if (_id == 0)
                    Interlocked.CompareExchange(ref _id, Interlocked.Increment(ref s_nextId), 0);
                return _id;
            }
        }

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
        public LuminTask<SemaphoreReleaser> LockAsync(CancellationToken cancellationToken = default)
            => LockInternalAsync(cancellationToken);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private async LuminTask<SemaphoreReleaser> LockInternalAsync(CancellationToken cancellationToken)
        {
            await WaitAsync(cancellationToken);
            return new SemaphoreReleaser(this);
        }

        public void Dispose()
        {
            if (!_isDisposed)
            {
                _isDisposed = true;
                _queue.CancelAll(default);
            }
        }
    }

    public static class AsyncSemaphoreExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static LuminTask WaitAsync(this AsyncSemaphore semaphore, TimeSpan timeout)
            => WaitWithTimeoutAsync(semaphore, timeout);

        private static async LuminTask WaitWithTimeoutAsync(AsyncSemaphore semaphore, TimeSpan timeout)
        {
            using var cts = new CancellationTokenSource(timeout);
            await semaphore.WaitAsync(cts.Token);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static LuminTask<SemaphoreReleaser> LockAsync(this AsyncSemaphore semaphore, TimeSpan timeout)
            => LockWithTimeoutAsync(semaphore, timeout);

        private static async LuminTask<SemaphoreReleaser> LockWithTimeoutAsync(AsyncSemaphore semaphore, TimeSpan timeout)
        {
            using var cts = new CancellationTokenSource(timeout);
            return await semaphore.LockAsync(cts.Token);
        }
    }
}
