using System;
using System.Runtime.CompilerServices;
using System.Threading;

namespace LuminThread.AsyncEx
{
    /// <summary>
    /// 高性能异步条件变量
    /// </summary>
    public sealed class AsyncConditionVariable
    {
        private readonly AsyncLock _asyncLock;
        private readonly IAsyncWaitQueue<AsyncLock.ReleaseScope> _waitQueue;

        public AsyncConditionVariable(AsyncLock asyncLock)
        {
            _asyncLock = asyncLock ?? throw new ArgumentNullException(nameof(asyncLock));
            _waitQueue = new DefaultAsyncWaitQueue<AsyncLock.ReleaseScope>();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public LuminTask<AsyncLock.ReleaseScope> WaitAsync(CancellationToken cancellationToken = default)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return LuminTask.FromCanceled<AsyncLock.ReleaseScope>(cancellationToken);
            }

            // 使用扩展方法，自动处理取消
            return _waitQueue.Enqueue(cancellationToken);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Notify()
        {
            if (!_waitQueue.IsEmpty)
            {
                _waitQueue.Dequeue(new AsyncLock.ReleaseScope(_asyncLock));
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void NotifyAll()
        {
            if (!_waitQueue.IsEmpty)
            {
                _waitQueue.DequeueAll(new AsyncLock.ReleaseScope(_asyncLock));
            }
        }
    }

    public static class AsyncConditionVariableExtensions
    {
        /// <summary>
        /// 释放当前锁，等待条件变量信号，然后重新获取锁
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static LuminTask<AsyncLock.ReleaseScope> WaitWithLockAsync(
            this AsyncConditionVariable conditionVariable,
            AsyncLock.ReleaseScope currentLock,
            CancellationToken cancellationToken = default)
        {
            if (conditionVariable == null)
                throw new ArgumentNullException(nameof(conditionVariable));

            // 释放当前锁
            currentLock.Dispose();

            // 等待条件变量信号（会返回新的锁）
            return conditionVariable.WaitAsync(cancellationToken);
        }
    }

    /// <summary>
    /// 高性能异步监视器
    /// </summary>
    public sealed class AsyncMonitor
    {
        private readonly AsyncLock _asyncLock;
        private readonly AsyncConditionVariable _conditionVariable;

        public AsyncMonitor()
        {
            _asyncLock = new AsyncLock();
            _conditionVariable = new AsyncConditionVariable(_asyncLock);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public LuminTask<MonitorScope> EnterAsync(CancellationToken cancellationToken = default)
        {
            return EnterInternalAsync(cancellationToken);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private async LuminTask<MonitorScope> EnterInternalAsync(CancellationToken cancellationToken)
        {
            var lockScope = await _asyncLock.LockAsync(cancellationToken);
            return new MonitorScope(this, lockScope);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal LuminTask<AsyncLock.ReleaseScope> WaitInternalAsync(
            AsyncLock.ReleaseScope lockScope,
            CancellationToken cancellationToken)
        {
            return _conditionVariable.WaitWithLockAsync(lockScope, cancellationToken);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void PulseInternal()
        {
            _conditionVariable.Notify();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void PulseAllInternal()
        {
            _conditionVariable.NotifyAll();
        }

        /// <summary>
        /// 监视器作用域
        /// </summary>
        public struct MonitorScope : IDisposable
        {
            private readonly AsyncMonitor _monitor;
            private AsyncLock.ReleaseScope _lockScope;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal MonitorScope(AsyncMonitor monitor, AsyncLock.ReleaseScope lockScope)
            {
                _monitor = monitor;
                _lockScope = lockScope;
            }

            /// <summary>
            /// 等待信号
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public LuminTask<MonitorScope> WaitAsync(CancellationToken cancellationToken = default)
            {
                return WaitInternalAsync(cancellationToken);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private async LuminTask<MonitorScope> WaitInternalAsync(CancellationToken cancellationToken)
            {
                var newLockScope = await _monitor.WaitInternalAsync(_lockScope, cancellationToken);
                return new MonitorScope(_monitor, newLockScope);
            }

            /// <summary>
            /// 唤醒一个等待者
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Pulse()
            {
                _monitor?.PulseInternal();
            }

            /// <summary>
            /// 唤醒所有等待者
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void PulseAll()
            {
                _monitor?.PulseAllInternal();
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Dispose()
            {
                _lockScope.Dispose();
            }
        }
    }

    public static class AsyncMonitorExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static LuminTask<AsyncMonitor.MonitorScope> EnterAsync(
            this AsyncMonitor monitor,
            CancellationToken cancellationToken = default)
        {
            return monitor.EnterAsync(cancellationToken);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static LuminTask<AsyncMonitor.MonitorScope> EnterAsync(
            this AsyncMonitor monitor,
            TimeSpan timeout)
        {
            using var cts = new CancellationTokenSource(timeout);
            return monitor.EnterAsync(cts.Token);
        }
    }
}
