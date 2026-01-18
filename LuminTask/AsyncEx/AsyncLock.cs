using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace LuminThread.AsyncEx
{
    /// <summary>
    /// 高性能异步互斥锁
    /// </summary>
    public sealed class AsyncLock
    {
        private readonly IAsyncWaitQueue<ReleaseScope> _waitQueue;
        private volatile int _state; // 0 = 空闲, 1 = 已占用

        public AsyncLock() : this(new DefaultAsyncWaitQueue<ReleaseScope>()) { }

        public AsyncLock(IAsyncWaitQueue<ReleaseScope> waitQueue)
        {
            _waitQueue = waitQueue ?? throw new ArgumentNullException(nameof(waitQueue));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public LuminTask<ReleaseScope> LockAsync(CancellationToken cancellationToken = default)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return LuminTask.FromCanceled<ReleaseScope>(cancellationToken);
            }

            // 快速路径：尝试立即获取锁
            if (Interlocked.CompareExchange(ref _state, 1, 0) == 0)
            {
                return LuminTask.FromResult(new ReleaseScope(this));
            }

            // 慢速路径：加入等待队列，使用扩展方法自动处理取消
            var task = _waitQueue.Enqueue(cancellationToken);

            // 双重检查：可能在入队期间锁被释放
            if (Interlocked.CompareExchange(ref _state, 1, 0) == 0)
            {
                if (_waitQueue.TryCancel(task, cancellationToken))
                {
                    return LuminTask.FromResult(new ReleaseScope(this));
                }
            }

            return task;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void Release()
        {
            // 释放锁
            Interlocked.Exchange(ref _state, 0);

            // 唤醒下一个等待者
            if (!_waitQueue.IsEmpty)
            {
                if (Interlocked.CompareExchange(ref _state, 1, 0) == 0)
                {
                    _waitQueue.Dequeue(new ReleaseScope(this));
                }
            }
        }

        /// <summary>
        /// 锁释放器
        /// </summary>
        public readonly struct ReleaseScope : IDisposable, IAsyncDisposable
        {
            private readonly AsyncLock _parent;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public ReleaseScope(AsyncLock parent) => _parent = parent;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Dispose()
            {
                _parent?.Release();
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public ValueTask DisposeAsync()
            {
                _parent?.Release();
                return default;
            }

            public override bool Equals(object obj)
            {
                return obj is ReleaseScope other && Equals(other);
            }

            public bool Equals(ReleaseScope other)
            {
                return ReferenceEquals(_parent, other._parent);
            }

            public override int GetHashCode()
            {
                return _parent?.GetHashCode() ?? 0;
            }

            public static bool operator ==(ReleaseScope left, ReleaseScope right)
            {
                return left.Equals(right);
            }

            public static bool operator !=(ReleaseScope left, ReleaseScope right)
            {
                return !left.Equals(right);
            }
        }
    }
}
