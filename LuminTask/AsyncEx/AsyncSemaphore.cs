using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using LuminThread.TaskSource;

namespace LuminThread.AsyncEx
{
    /// <summary>
    /// 异步信号量
    /// </summary>
    [DebuggerDisplay("Id = {Id}, CurrentCount = {_count}")]
    public sealed class AsyncSemaphore
    {
        private readonly AsyncLock _mutex;
        private readonly IAsyncWaitQueue<object> _queue;
        private long _count;
        private int _id;

        /// <summary>
        /// 创建新的异步信号量
        /// </summary>
        /// <param name="initialCount">初始计数，必须大于等于0</param>
        public AsyncSemaphore(long initialCount)
        {
            if (initialCount < 0)
                throw new ArgumentOutOfRangeException(nameof(initialCount));
            
            _mutex = new AsyncLock();
            _queue = new DefaultAsyncWaitQueue<object>();
            _count = initialCount;
        }

        /// <summary>
        /// 获取实例的唯一标识符
        /// </summary>
        public int Id => IdManager<AsyncSemaphore>.GetId(ref _id);

        /// <summary>
        /// 获取当前可用的信号量计数
        /// </summary>
        public long CurrentCount
        {
            get
            {
                using (_mutex.LockAsync().Result)
                {
                    return _count;
                }
            }
        }

        /// <summary>
        /// 异步等待信号量
        /// </summary>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>表示等待完成的任务</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public LuminTask WaitAsync(CancellationToken cancellationToken = default)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return LuminTask.FromCanceled(cancellationToken);
            }

            return WaitInternalAsync(cancellationToken);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private async LuminTask WaitInternalAsync(CancellationToken cancellationToken)
        {
            using (await _mutex.LockAsync(cancellationToken))
            {
                // 如果信号量可用，立即获取
                if (_count > 0)
                {
                    _count--;
                }
                else
                {
                    // 等待信号量可用或取消
                    await _queue.Enqueue();
                }
            }
        }

        /// <summary>
        /// 异步等待信号量
        /// </summary>
        /// <returns>表示等待完成的任务</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public LuminTask WaitAsync()
        {
            return WaitAsync(CancellationToken.None);
        }

        /// <summary>
        /// 同步等待信号量
        /// </summary>
        /// <param name="cancellationToken">取消令牌</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Wait(CancellationToken cancellationToken = default)
        {
            WaitAsync(cancellationToken).GetAwaiter().GetResult();
        }

        /// <summary>
        /// 同步等待信号量
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Wait()
        {
            Wait(CancellationToken.None);
        }

        /// <summary>
        /// 释放信号量
        /// </summary>
        /// <param name="releaseCount">释放数量</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Release(long releaseCount = 1)
        {
            if (releaseCount < 0)
                throw new ArgumentOutOfRangeException(nameof(releaseCount));

            if (releaseCount == 0)
                return;

            using (_mutex.LockAsync().Result)
            {
                // 检查是否会溢出
                checked
                {
                    var test = _count + releaseCount;
                }

                // 先释放等待的任务
                while (releaseCount > 0 && !_queue.IsEmpty)
                {
                    _queue.Dequeue(null);
                    releaseCount--;
                }

                // 剩余的增加到计数中
                _count += releaseCount;
            }
        }

        /// <summary>
        /// 异步获取信号量锁，返回可释放的对象
        /// </summary>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>释放信号量的 disposable 对象</returns>
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

        /// <summary>
        /// 异步获取信号量锁，返回可释放的对象
        /// </summary>
        /// <returns>释放信号量的 disposable 对象</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public LuminTask<IDisposable> LockAsync()
        {
            return LockAsync(CancellationToken.None);
        }

        /// <summary>
        /// 同步获取信号量锁，返回可释放的对象
        /// </summary>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>释放信号量的 disposable 对象</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IDisposable Lock(CancellationToken cancellationToken = default)
        {
            Wait(cancellationToken);
            return new SemaphoreLock(this);
        }

        /// <summary>
        /// 同步获取信号量锁，返回可释放的对象
        /// </summary>
        /// <returns>释放信号量的 disposable 对象</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IDisposable Lock()
        {
            return Lock(CancellationToken.None);
        }

        /// <summary>
        /// 信号量锁释放器
        /// </summary>
        private sealed class SemaphoreLock : IDisposable
        {
            private readonly AsyncSemaphore _semaphore;
            private volatile bool _isDisposed;

            public SemaphoreLock(AsyncSemaphore semaphore)
            {
                _semaphore = semaphore;
                _isDisposed = false;
            }

            public void Dispose()
            {
                if (!_isDisposed)
                {
                    _semaphore.Release();
                    _isDisposed = true;
                }
            }
        }

        // 调试视图
        [DebuggerNonUserCode]
        private sealed class DebugView
        {
            private readonly AsyncSemaphore _semaphore;

            public DebugView(AsyncSemaphore semaphore)
            {
                _semaphore = semaphore;
            }

            public int Id => _semaphore.Id;
            public long CurrentCount => _semaphore._count;
            public bool HasWaitingTasks => !_semaphore._queue.IsEmpty;
        }
    }

    /// <summary>
    /// 扩展方法
    /// </summary>
    public static class AsyncSemaphoreExtensions
    {
        /// <summary>
        /// 带超时的等待
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static LuminTask WaitAsync(this AsyncSemaphore semaphore, TimeSpan timeout)
        {
            using var cts = new CancellationTokenSource(timeout);
            return semaphore.WaitAsync(cts.Token);
        }

        /// <summary>
        /// 带超时的锁获取
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static LuminTask<IDisposable> LockAsync(this AsyncSemaphore semaphore, TimeSpan timeout)
        {
            using var cts = new CancellationTokenSource(timeout);
            return semaphore.LockAsync(cts.Token);
        }
    }
}