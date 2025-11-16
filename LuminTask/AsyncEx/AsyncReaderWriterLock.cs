using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using LuminThread.TaskSource;

namespace LuminThread.AsyncEx
{
    /// <summary>
    /// 异步读写锁，支持读者-写者同步模式
    /// </summary>
    [DebuggerDisplay("Id = {Id}, State = {GetStateForDebugger}, ReaderCount = {GetReaderCountForDebugger}")]
    public sealed class AsyncReaderWriterLock
    {
        private readonly AsyncLock _mutex;
        private readonly IAsyncWaitQueue<IDisposable> _writerQueue;
        private readonly IAsyncWaitQueue<IDisposable> _readerQueue;
        
        private int _id;
        private int _locksHeld; // 0 = 无锁, >0 = 读者数量, -1 = 写者持有

        /// <summary>
        /// 创建新的异步读写锁
        /// </summary>
        public AsyncReaderWriterLock()
        {
            _mutex = new AsyncLock();
            _writerQueue = new DefaultAsyncWaitQueue<IDisposable>();
            _readerQueue = new DefaultAsyncWaitQueue<IDisposable>();
            _locksHeld = 0;
        }

        /// <summary>
        /// 获取实例的唯一标识符
        /// </summary>
        public int Id => IdManager<AsyncReaderWriterLock>.GetId(ref _id);

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
                if (_locksHeld == 0)
                    return State.Unlocked;
                if (_locksHeld == -1)
                    return State.WriteLocked;
                return State.ReadLocked;
            }
        }

        [DebuggerNonUserCode]
        internal int GetReaderCountForDebugger => (_locksHeld > 0 ? _locksHeld : 0);

        /// <summary>
        /// 异步获取读锁
        /// </summary>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>释放读锁的 disposable 对象</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public LuminTask<IDisposable> ReaderLockAsync(CancellationToken cancellationToken = default)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return LuminTask.FromCanceled<IDisposable>(cancellationToken);
            }

            return ReaderLockInternalAsync(cancellationToken);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private async LuminTask<IDisposable> ReaderLockInternalAsync(CancellationToken cancellationToken)
        {
            using (await _mutex.LockAsync(cancellationToken))
            {
                // 如果锁可用或者在读模式且没有等待的写者，立即获取
                if (_locksHeld >= 0 && _writerQueue.IsEmpty)
                {
                    _locksHeld++;
                    return new ReaderKey(this);
                }
                else
                {
                    // 等待锁可用或取消
                    return await _readerQueue.Enqueue();
                }
            }
        }

        /// <summary>
        /// 异步获取写锁
        /// </summary>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>释放写锁的 disposable 对象</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public LuminTask<IDisposable> WriterLockAsync(CancellationToken cancellationToken = default)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return LuminTask.FromCanceled<IDisposable>(cancellationToken);
            }

            return WriterLockInternalAsync(cancellationToken);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private async LuminTask<IDisposable> WriterLockInternalAsync(CancellationToken cancellationToken)
        {
            using (await _mutex.LockAsync(cancellationToken))
            {
                // 如果锁可用，立即获取
                if (_locksHeld == 0)
                {
                    _locksHeld = -1;
                    return new WriterKey(this);
                }
                else
                {
                    // 等待锁可用或取消
                    return await _writerQueue.Enqueue();
                }
            }
        }

        /// <summary>
        /// 同步获取读锁
        /// </summary>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>释放读锁的 disposable 对象</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IDisposable ReaderLock(CancellationToken cancellationToken = default)
        {
            return ReaderLockAsync(cancellationToken).GetAwaiter().GetResult();
        }

        /// <summary>
        /// 同步获取读锁
        /// </summary>
        /// <returns>释放读锁的 disposable 对象</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IDisposable ReaderLock()
        {
            return ReaderLock(CancellationToken.None);
        }

        /// <summary>
        /// 同步获取写锁
        /// </summary>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>释放写锁的 disposable 对象</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IDisposable WriterLock(CancellationToken cancellationToken = default)
        {
            return WriterLockAsync(cancellationToken).GetAwaiter().GetResult();
        }

        /// <summary>
        /// 同步获取写锁
        /// </summary>
        /// <returns>释放写锁的 disposable 对象</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IDisposable WriterLock()
        {
            return WriterLock(CancellationToken.None);
        }

        /// <summary>
        /// 释放等待的任务
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ReleaseWaiters()
        {
            if (_locksHeld == -1)
                return;

            // 优先给写者，然后是读者
            if (!_writerQueue.IsEmpty)
            {
                if (_locksHeld == 0)
                {
                    _locksHeld = -1;
                    // 使用新的 WriterKey 实例，避免 token 重用问题
                    _writerQueue.Dequeue(new WriterKey(this));
                    return;
                }
            }
            else
            {
                // 通知所有等待的读者
                while (!_readerQueue.IsEmpty)
                {
                    // 使用新的 ReaderKey 实例，避免 token 重用问题
                    _readerQueue.Dequeue(new ReaderKey(this));
                    _locksHeld++;
                }
            }
        }

        /// <summary>
        /// 释放读锁
        /// </summary>
        internal void ReleaseReaderLock()
        {
            using (_mutex.LockAsync().Result)
            {
                if (_locksHeld > 0)
                {
                    _locksHeld--;
                    ReleaseWaiters();
                }
            }
        }

        /// <summary>
        /// 释放写锁
        /// </summary>
        internal void ReleaseWriterLock()
        {
            using (_mutex.LockAsync().Result)
            {
                if (_locksHeld == -1)
                {
                    _locksHeld = 0;
                    ReleaseWaiters();
                }
            }
        }

        /// <summary>
        /// 读锁释放器
        /// </summary>
        private sealed class ReaderKey : IDisposable
        {
            private readonly AsyncReaderWriterLock _asyncReaderWriterLock;
            private volatile bool _isDisposed;

            public ReaderKey(AsyncReaderWriterLock asyncReaderWriterLock)
            {
                _asyncReaderWriterLock = asyncReaderWriterLock;
                _isDisposed = false;
            }

            public void Dispose()
            {
                if (!_isDisposed)
                {
                    _asyncReaderWriterLock.ReleaseReaderLock();
                    _isDisposed = true;
                }
            }
        }

        /// <summary>
        /// 写锁释放器
        /// </summary>
        private sealed class WriterKey : IDisposable
        {
            private readonly AsyncReaderWriterLock _asyncReaderWriterLock;
            private volatile bool _isDisposed;

            public WriterKey(AsyncReaderWriterLock asyncReaderWriterLock)
            {
                _asyncReaderWriterLock = asyncReaderWriterLock;
                _isDisposed = false;
            }

            public void Dispose()
            {
                if (!_isDisposed)
                {
                    _asyncReaderWriterLock.ReleaseWriterLock();
                    _isDisposed = true;
                }
            }
        }

        // 调试视图
        [DebuggerNonUserCode]
        private sealed class DebugView
        {
            private readonly AsyncReaderWriterLock _rwl;

            public DebugView(AsyncReaderWriterLock rwl)
            {
                _rwl = rwl;
            }

            public int Id => _rwl.Id;
            public State State => _rwl.GetStateForDebugger;
            public int ReaderCount => _rwl.GetReaderCountForDebugger;
            public bool HasWaitingWriters => !_rwl._writerQueue.IsEmpty;
            public bool HasWaitingReaders => !_rwl._readerQueue.IsEmpty;
        }
    }

    /// <summary>
    /// ID 管理器
    /// </summary>
    internal static class IdManager<T>
    {
        private static int _nextId;
        
        public static int GetId(ref int id)
        {
            if (id == 0)
            {
                Interlocked.CompareExchange(ref id, Interlocked.Increment(ref _nextId), 0);
            }
            return id;
        }
    }

    /// <summary>
    /// 扩展方法
    /// </summary>
    public static class AsyncReaderWriterLockExtensions
    {
        /// <summary>
        /// 带超时的读锁获取
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static LuminTask<IDisposable> ReaderLockAsync(this AsyncReaderWriterLock rwLock, TimeSpan timeout)
        {
            using var cts = new CancellationTokenSource(timeout);
            return rwLock.ReaderLockAsync(cts.Token);
        }

        /// <summary>
        /// 带超时的写锁获取
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static LuminTask<IDisposable> WriterLockAsync(this AsyncReaderWriterLock rwLock, TimeSpan timeout)
        {
            using var cts = new CancellationTokenSource(timeout);
            return rwLock.WriterLockAsync(cts.Token);
        }
    }
}