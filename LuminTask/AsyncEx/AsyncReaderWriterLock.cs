using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;

namespace LuminThread.AsyncEx
{
    /// <summary>
    /// 高性能异步读写锁
    /// </summary>
    [DebuggerDisplay("Id = {Id}, State = {GetStateForDebugger}, ReaderCount = {GetReaderCountForDebugger}")]
    public sealed class AsyncReaderWriterLock
    {
        private readonly object _mutex = new object();
        private readonly IAsyncWaitQueue<IDisposable> _writerQueue;
        private readonly IAsyncWaitQueue<IDisposable> _readerQueue;

        private int _locksHeld; // 0 = 无锁, >0 = 读者数量, -1 = 写者持有
        private int _id;

        public AsyncReaderWriterLock()
        {
            _writerQueue = new DefaultAsyncWaitQueue<IDisposable>();
            _readerQueue = new DefaultAsyncWaitQueue<IDisposable>();
        }

        public int Id
        {
            get
            {
                if (_id == 0)
                {
                    Interlocked.CompareExchange(ref _id, GetNextId(), 0);
                }
                return _id;
            }
        }

        private static int _nextId = 1;
        private static int GetNextId() => Interlocked.Increment(ref _nextId);

        internal enum State
        {
            Unlocked,
            ReadLocked,
            WriteLocked,
        }

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
            if (cancellationToken.IsCancellationRequested)
            {
                return LuminTask.FromCanceled<IDisposable>(cancellationToken);
            }

            lock (_mutex)
            {
                // 快速路径：无锁或读锁且无等待的写者
                if (_locksHeld >= 0 && _writerQueue.IsEmpty)
                {
                    _locksHeld++;
                    return LuminTask.FromResult<IDisposable>(new ReaderKey(this));
                }

                // 使用扩展方法，自动处理取消
                return _readerQueue.Enqueue(cancellationToken);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public LuminTask<IDisposable> WriterLockAsync(CancellationToken cancellationToken = default)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return LuminTask.FromCanceled<IDisposable>(cancellationToken);
            }

            lock (_mutex)
            {
                // 快速路径：无锁
                if (_locksHeld == 0)
                {
                    _locksHeld = -1;
                    return LuminTask.FromResult<IDisposable>(new WriterKey(this));
                }

                // 使用扩展方法，自动处理取消
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
            // 优先写者
            if (!_writerQueue.IsEmpty && _locksHeld == 0)
            {
                _locksHeld = -1;
                _writerQueue.Dequeue(new WriterKey(this));
            }
            // 然后是所有读者
            else if (!_readerQueue.IsEmpty && _locksHeld >= 0)
            {
                while (!_readerQueue.IsEmpty)
                {
                    _readerQueue.Dequeue(new ReaderKey(this));
                    _locksHeld++;
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
