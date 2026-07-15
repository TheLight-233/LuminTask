using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;

namespace LuminThread.AsyncEx
{
    [DebuggerDisplay("Id = {Id}, State = {GetStateForDebugger}, ReaderCount = {GetReaderCountForDebugger}")]
    public sealed class AsyncReaderWriterLock : IDisposable
    {
        private readonly object _mutex = new object();
        private readonly IAsyncWaitQueue<IDisposable> _writerQueue;
        private readonly IAsyncWaitQueue<IDisposable> _readerQueue;

        private int _locksHeld;
        private int _id;
        private volatile bool _isDisposed;

        public AsyncReaderWriterLock()
        {
            _writerQueue = new DefaultAsyncWaitQueue<IDisposable>();
            _readerQueue = new DefaultAsyncWaitQueue<IDisposable>();
        }

        ~AsyncReaderWriterLock() => Dispose(false);

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

        internal enum State { Unlocked, ReadLocked, WriteLocked }

        [DebuggerNonUserCode]
        internal State GetStateForDebugger
        {
            get
            {
                if (_locksHeld == 0) return State.Unlocked;
                if (_locksHeld == -1) return State.WriteLocked;
                return State.ReadLocked;
            }
        }

        [DebuggerNonUserCode]
        internal int GetReaderCountForDebugger => Math.Max(0, _locksHeld);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public LuminTask<IDisposable> ReaderLockAsync(CancellationToken cancellationToken = default)
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(AsyncReaderWriterLock));

            if (cancellationToken.IsCancellationRequested)
                return LuminTask.FromCanceled<IDisposable>(cancellationToken);

            lock (_mutex)
            {
                if (_locksHeld >= 0 && _writerQueue.IsEmpty)
                {
                    _locksHeld++;
                    return LuminTask.FromResult<IDisposable>(new ReaderKey(this));
                }

                return _readerQueue.Enqueue(cancellationToken);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public LuminTask<IDisposable> WriterLockAsync(CancellationToken cancellationToken = default)
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(AsyncReaderWriterLock));

            if (cancellationToken.IsCancellationRequested)
                return LuminTask.FromCanceled<IDisposable>(cancellationToken);

            lock (_mutex)
            {
                if (_locksHeld == 0)
                {
                    _locksHeld = -1;
                    return LuminTask.FromResult<IDisposable>(new WriterKey(this));
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
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                _isDisposed = true;
                if (disposing)
                {
                    _writerQueue.CancelAll(default);
                    _readerQueue.CancelAll(default);
                }
            }
        }

        private sealed class ReaderKey : IDisposable
        {
            private AsyncReaderWriterLock _asyncReaderWriterLock;

            public ReaderKey(AsyncReaderWriterLock asyncReaderWriterLock)
            {
                _asyncReaderWriterLock = asyncReaderWriterLock;
            }

            public void Dispose()
            {
                var rwLock = Interlocked.Exchange(ref _asyncReaderWriterLock, null);
                rwLock?.ReleaseReaderLock();
            }
        }

        private sealed class WriterKey : IDisposable
        {
            private AsyncReaderWriterLock _asyncReaderWriterLock;

            public WriterKey(AsyncReaderWriterLock asyncReaderWriterLock)
            {
                _asyncReaderWriterLock = asyncReaderWriterLock;
            }

            public void Dispose()
            {
                var rwLock = Interlocked.Exchange(ref _asyncReaderWriterLock, null);
                rwLock?.ReleaseWriterLock();
            }
        }
    }

    public static class AsyncReaderWriterLockExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static LuminTask<IDisposable> ReaderLockAsync(this AsyncReaderWriterLock rwLock, TimeSpan timeout)
        {
            using var cts = new CancellationTokenSource(timeout);
            return rwLock.ReaderLockAsync(cts.Token);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static LuminTask<IDisposable> WriterLockAsync(this AsyncReaderWriterLock rwLock, TimeSpan timeout)
        {
            using var cts = new CancellationTokenSource(timeout);
            return rwLock.WriterLockAsync(cts.Token);
        }
    }
}
