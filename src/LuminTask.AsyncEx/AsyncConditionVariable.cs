using System;
using System.Runtime.CompilerServices;
using System.Threading;

namespace LuminThread.AsyncEx
{
    /// <summary>异步条件变量。值类型句柄，共享内部状态。</summary>
    public readonly struct AsyncConditionVariable : IDisposable
    {
        private readonly ConditionVariableState? _state;

        public AsyncConditionVariable(AsyncLock asyncLock)
        {
            _state = new ConditionVariableState(asyncLock);
        }

        public async LuminTask<AsyncLock.ReleaseScope> WaitAsync(
            CancellationToken cancellationToken = default)
            => await GetState().WaitAsync(cancellationToken);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Notify() => GetState().Notify();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void NotifyAll() => GetState().NotifyAll();

        public void Dispose() => GetState().Dispose();

        private ConditionVariableState GetState()
            => _state ?? throw new InvalidOperationException("AsyncConditionVariable 未初始化（default 值）或已释放");
    }

    internal sealed class ConditionVariableState : IDisposable
    {
        private readonly AsyncLock _asyncLock;
        private readonly DefaultAsyncWaitQueue<bool> _waitQueue;
        private volatile bool _isDisposed;

        public ConditionVariableState(AsyncLock asyncLock)
        {
            _asyncLock = asyncLock;
            _waitQueue = new DefaultAsyncWaitQueue<bool>();
        }

        public async LuminTask<AsyncLock.ReleaseScope> WaitAsync(
            CancellationToken cancellationToken = default)
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(AsyncConditionVariable));

            cancellationToken.ThrowIfCancellationRequested();

            var waitTask = _waitQueue.EnqueueWithCancellation(cancellationToken);

            _asyncLock.ReleaseLock();

            await waitTask;

            return await _asyncLock.LockAsync(cancellationToken);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Notify()
        {
            if (!_waitQueue.IsEmpty)
                _waitQueue.Dequeue(true);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void NotifyAll()
        {
            if (!_waitQueue.IsEmpty)
                _waitQueue.DequeueAll(true);
        }

        public void Dispose()
        {
            if (!_isDisposed)
            {
                _isDisposed = true;
                _waitQueue.CancelAll(default);
            }
        }
    }

    public static class AsyncConditionVariableExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static LuminTask<AsyncLock.ReleaseScope> WaitWithLockAsync(
            this AsyncConditionVariable conditionVariable,
            AsyncLock.ReleaseScope currentLock,
            CancellationToken cancellationToken = default)
        {
            return conditionVariable.WaitAsync(cancellationToken);
        }
    }

    /// <summary>异步监视器（互斥锁 + 条件变量）。值类型句柄，共享内部状态。</summary>
    public readonly struct AsyncMonitor : IDisposable
    {
        private readonly MonitorState? _state;

        public AsyncMonitor()
        {
            _state = new MonitorState();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public LuminTask<MonitorScope> EnterAsync(CancellationToken cancellationToken = default)
            => GetState().EnterAsync(cancellationToken);

        public void Dispose() => GetState().Dispose();

        internal MonitorState GetState()
            => _state ?? throw new InvalidOperationException("AsyncMonitor 未初始化（default 值）或已释放");

        /// <summary>监视器临界区句柄。Dispose 即退出。结构体，无分配。</summary>
        public struct MonitorScope : IDisposable
        {
            private readonly MonitorState? _monitor;
            private AsyncLock.ReleaseScope _lockScope;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal MonitorScope(MonitorState monitor, AsyncLock.ReleaseScope lockScope)
            {
                _monitor = monitor;
                _lockScope = lockScope;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public LuminTask<MonitorScope> WaitAsync(CancellationToken cancellationToken = default)
            {
                return WaitInternalAsync(cancellationToken);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private async LuminTask<MonitorScope> WaitInternalAsync(CancellationToken cancellationToken)
            {
                var newLockScope = await _monitor!.WaitInternalAsync(_lockScope, cancellationToken);
                return new MonitorScope(_monitor, newLockScope);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Pulse() => _monitor?.PulseInternal();

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void PulseAll() => _monitor?.PulseAllInternal();

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Dispose() => _lockScope.Dispose();
        }
    }

    internal sealed class MonitorState : IDisposable
    {
        private readonly AsyncLock _asyncLock;
        private readonly AsyncConditionVariable _conditionVariable;
        private volatile bool _isDisposed;

        public MonitorState()
        {
            _asyncLock = new AsyncLock();
            _conditionVariable = new AsyncConditionVariable(_asyncLock);
        }

        internal bool TryEnter(out AsyncMonitor.MonitorScope scope)
        {
            scope = default;
            if (_isDisposed || !_asyncLock.GetState().TryAcquire(out var key)) return false;
            scope = new AsyncMonitor.MonitorScope(this, key);
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public LuminTask<AsyncMonitor.MonitorScope> EnterAsync(CancellationToken cancellationToken = default)
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(AsyncMonitor));

            return EnterInternalAsync(cancellationToken);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private async LuminTask<AsyncMonitor.MonitorScope> EnterInternalAsync(CancellationToken cancellationToken)
        {
            var lockScope = await _asyncLock.LockAsync(cancellationToken);
            return new AsyncMonitor.MonitorScope(this, lockScope);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal LuminTask<AsyncLock.ReleaseScope> WaitInternalAsync(
            AsyncLock.ReleaseScope lockScope,
            CancellationToken cancellationToken)
        {
            return _conditionVariable.WaitWithLockAsync(lockScope, cancellationToken);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void PulseInternal() => _conditionVariable.Notify();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void PulseAllInternal() => _conditionVariable.NotifyAll();

        public void Dispose()
        {
            if (!_isDisposed)
            {
                _isDisposed = true;
                _conditionVariable.Dispose();
                _asyncLock.Dispose();
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
            if (timeout == Timeout.InfiniteTimeSpan) return monitor.EnterAsync();
            if (WaitTimeout.CanAcquireImmediately(timeout) && monitor.GetState().TryEnter(out var scope))
                return LuminTask.FromResult(scope);

            var timer = new CancellationTokenSource(timeout);
            try { return WaitTimeout.Finish(monitor.EnterAsync(timer.Token), timer); }
            catch { timer.Dispose(); throw; }
        }
    }
}
