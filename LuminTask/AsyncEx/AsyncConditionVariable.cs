using System;
using System.Runtime.CompilerServices;
using System.Threading;

namespace LuminThread.AsyncEx
{
    public sealed class AsyncConditionVariable : IDisposable
    {
        private readonly AsyncLock _asyncLock;
        private readonly IAsyncWaitQueue<AsyncLock.ReleaseScope> _waitQueue;
        private volatile bool _isDisposed;

        public AsyncConditionVariable(AsyncLock asyncLock)
        {
            _asyncLock = asyncLock ?? throw new ArgumentNullException(nameof(asyncLock));
            _waitQueue = new DefaultAsyncWaitQueue<AsyncLock.ReleaseScope>();
        }

        ~AsyncConditionVariable() => Dispose(false);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public LuminTask<AsyncLock.ReleaseScope> WaitAsync(CancellationToken cancellationToken = default)
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(AsyncConditionVariable));

            if (cancellationToken.IsCancellationRequested)
                return LuminTask.FromCanceled<AsyncLock.ReleaseScope>(cancellationToken);

            return _waitQueue.Enqueue(cancellationToken);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Notify()
        {
            if (!_waitQueue.IsEmpty)
                _waitQueue.Dequeue(new AsyncLock.ReleaseScope(_asyncLock));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void NotifyAll()
        {
            if (!_waitQueue.IsEmpty)
                _waitQueue.DequeueAll(new AsyncLock.ReleaseScope(_asyncLock));
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
            if (conditionVariable == null)
                throw new ArgumentNullException(nameof(conditionVariable));

            currentLock.Dispose();
            return conditionVariable.WaitAsync(cancellationToken);
        }
    }

    public sealed class AsyncMonitor : IDisposable
    {
        private readonly AsyncLock _asyncLock;
        private readonly AsyncConditionVariable _conditionVariable;
        private volatile bool _isDisposed;

        public AsyncMonitor()
        {
            _asyncLock = new AsyncLock();
            _conditionVariable = new AsyncConditionVariable(_asyncLock);
        }

        ~AsyncMonitor() => Dispose(false);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public LuminTask<MonitorScope> EnterAsync(CancellationToken cancellationToken = default)
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(AsyncMonitor));

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
        internal void PulseInternal() => _conditionVariable.Notify();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void PulseAllInternal() => _conditionVariable.NotifyAll();

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
                    _conditionVariable.Dispose();
                    _asyncLock.Dispose();
                }
            }
        }

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

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Pulse() => _monitor?.PulseInternal();

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void PulseAll() => _monitor?.PulseAllInternal();

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Dispose() => _lockScope.Dispose();
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
