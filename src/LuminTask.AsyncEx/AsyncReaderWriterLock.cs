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

        private ReaderWriterLockState GetState()
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
        private readonly IAsyncWaitQueue<ReaderKey> _readerQueue;
        private readonly IAsyncWaitQueue<WriterKey> _writerQueue;

        private int _locksHeld;
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
                if (_locksHeld >= 0 && _writerQueue.IsEmpty)
                {
                    _locksHeld++;
                    return LuminTask.FromResult(new ReaderKey(this));
                }

                return _readerQueue.Enqueue(cancellationToken);
            }
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
                if (_locksHeld == 0)
                {
                    _locksHeld = -1;
                    return LuminTask.FromResult(new WriterKey(this));
                }

                return _writerQueue.Enqueue(cancellationToken);
            }
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
            if (!_writerQueue.IsEmpty && _locksHeld == 0)
            {
                _locksHeld = -1;
                _writerQueue.Dequeue(new WriterKey(this));
            }
            else if (!_readerQueue.IsEmpty && _locksHeld >= 0)
            {
                while (!_readerQueue.IsEmpty)
                {
                    _readerQueue.Dequeue(new ReaderKey(this));
                    _locksHeld++;
                }
            }
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
            using var cts = new CancellationTokenSource(timeout);
            return rwLock.ReaderLockAsync(cts.Token);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static LuminTask<WriterKey> WriterLockAsync(this AsyncReaderWriterLock rwLock, TimeSpan timeout)
        {
            using var cts = new CancellationTokenSource(timeout);
            return rwLock.WriterLockAsync(cts.Token);
        }
    }
}
