using System;
using System.Runtime.CompilerServices;
using System.Threading;

namespace LuminThread.AsyncEx
{
    /// <summary>异步读写锁。值类型句柄，共享内部状态。</summary>
    public readonly struct AsyncReaderWriterLock : IDisposable
    {
        private readonly ReaderWriterLockState? _state;

        public AsyncReaderWriterLock()
        {
            _state = new ReaderWriterLockState();
        }

        public int Id => GetState().Id;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public LuminTask<ReaderKey> ReaderLockAsync(CancellationToken cancellationToken = default)
            => GetState().ReaderLockAsync(cancellationToken);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public LuminTask<WriterKey> WriterLockAsync(CancellationToken cancellationToken = default)
            => GetState().WriterLockAsync(cancellationToken);

        public void Dispose() => GetState().Dispose();

        internal ReaderWriterLockState GetState()
            => _state ?? throw new InvalidOperationException("AsyncReaderWriterLock 未初始化（default 值）或已释放");
    }

    /// <summary>读锁句柄。Dispose 即释放。结构体，获取读锁无分配。</summary>
    public readonly struct ReaderKey : IDisposable
    {
        private readonly ReaderWriterLockState? _state;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal ReaderKey(ReaderWriterLockState state) => _state = state;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Dispose() => _state?.ReleaseReaderLock();
    }

    /// <summary>写锁句柄。Dispose 即释放。结构体，获取写锁无分配。</summary>
    public readonly struct WriterKey : IDisposable
    {
        private readonly ReaderWriterLockState? _state;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal WriterKey(ReaderWriterLockState state) => _state = state;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Dispose() => _state?.ReleaseWriterLock();
    }

    internal sealed class ReaderWriterLockState : IDisposable
    {
        private static int s_nextId = 1;

        private readonly object _mutex = new object();
        private readonly DefaultAsyncWaitQueue<ReaderKey> _readerQueue;
        private readonly DefaultAsyncWaitQueue<WriterKey> _writerQueue;

        private int _locksHeld;
        private bool _dispatching;
        private int _id;
        private volatile bool _isDisposed;

        public ReaderWriterLockState()
        {
            _writerQueue = new DefaultAsyncWaitQueue<WriterKey>();
            _readerQueue = new DefaultAsyncWaitQueue<ReaderKey>();
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public LuminTask<ReaderKey> ReaderLockAsync(CancellationToken cancellationToken = default)
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(AsyncReaderWriterLock));

            if (cancellationToken.IsCancellationRequested)
                return LuminTask.FromCanceled<ReaderKey>(cancellationToken);

            lock (_mutex)
            {
                if (TryAcquireReaderLocked())
                {
                    return LuminTask.FromResult(new ReaderKey(this));
                }

                return _readerQueue.EnqueueWithCancellation(cancellationToken);
            }
        }

        private bool TryAcquireReaderLocked()
        {
            if (_locksHeld < 0 || !_writerQueue.IsEmpty) return false;
            _locksHeld++;
            return true;
        }

        internal bool TryAcquireReader()
        {
            if (!Monitor.TryEnter(_mutex)) return false;
            try { return !_isDisposed && TryAcquireReaderLocked(); }
            finally { Monitor.Exit(_mutex); }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public LuminTask<WriterKey> WriterLockAsync(CancellationToken cancellationToken = default)
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(AsyncReaderWriterLock));

            if (cancellationToken.IsCancellationRequested)
                return LuminTask.FromCanceled<WriterKey>(cancellationToken);

            lock (_mutex)
            {
                if (TryAcquireWriterLocked())
                {
                    return LuminTask.FromResult(new WriterKey(this));
                }

                return _writerQueue.EnqueueWithCancellation(cancellationToken);
            }
        }

        private bool TryAcquireWriterLocked()
        {
            if (_locksHeld != 0) return false;
            _locksHeld = -1;
            return true;
        }

        internal bool TryAcquireWriter()
        {
            if (!Monitor.TryEnter(_mutex)) return false;
            try { return !_isDisposed && TryAcquireWriterLocked(); }
            finally { Monitor.Exit(_mutex); }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void ReleaseReaderLock()
        {
            lock (_mutex)
            {
                if (_locksHeld > 0)
                {
                    _locksHeld--;
                    ReleaseWaitersNoLock();
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void ReleaseWriterLock()
        {
            lock (_mutex)
            {
                if (_locksHeld == -1)
                {
                    _locksHeld = 0;
                    ReleaseWaitersNoLock();
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ReleaseWaitersNoLock()
        {
            // Grant before publishing. A continuation may immediately release its key and
            // queue more work on the same thread. The outer dispatcher drains that work.
            if (_dispatching) return;
            _dispatching = true;
            try
            {
                while (_locksHeld >= 0)
                {
                    if (!_writerQueue.IsEmpty)
                    {
                        if (_locksHeld != 0) break;
                        _locksHeld = -1;
                        if (!_writerQueue.TryDequeue(new WriterKey(this))) _locksHeld = 0;
                    }
                    else
                    {
                        if (_readerQueue.IsEmpty) break;
                        _locksHeld++;
                        if (!_readerQueue.TryDequeue(new ReaderKey(this))) _locksHeld--;
                    }
                }
            }
            finally { _dispatching = false; }
        }

        public void Dispose()
        {
            if (!_isDisposed)
            {
                _isDisposed = true;
                _writerQueue.CancelAll(default);
                _readerQueue.CancelAll(default);
            }
        }
    }

    public static class AsyncReaderWriterLockExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static LuminTask<ReaderKey> ReaderLockAsync(this AsyncReaderWriterLock rwLock, TimeSpan timeout)
        {
            if (timeout == Timeout.InfiniteTimeSpan) return rwLock.ReaderLockAsync();
            if (WaitTimeout.CanAcquireImmediately(timeout) && rwLock.GetState().TryAcquireReader())
                return LuminTask.FromResult(new ReaderKey(rwLock.GetState()));

            var timer = new CancellationTokenSource(timeout);
            try { return WaitTimeout.Finish(rwLock.ReaderLockAsync(timer.Token), timer); }
            catch { timer.Dispose(); throw; }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static LuminTask<WriterKey> WriterLockAsync(this AsyncReaderWriterLock rwLock, TimeSpan timeout)
        {
            if (timeout == Timeout.InfiniteTimeSpan) return rwLock.WriterLockAsync();
            if (WaitTimeout.CanAcquireImmediately(timeout) && rwLock.GetState().TryAcquireWriter())
                return LuminTask.FromResult(new WriterKey(rwLock.GetState()));

            var timer = new CancellationTokenSource(timeout);
            try { return WaitTimeout.Finish(rwLock.WriterLockAsync(timer.Token), timer); }
            catch { timer.Dispose(); throw; }
        }
    }
}
