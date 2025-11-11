using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace LuminThread.AsyncEx
{
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
        public LuminTask<AsyncLock.ReleaseScope> EnterAsync(CancellationToken cancellationToken = default)
        {
            return _asyncLock.LockAsync(cancellationToken);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public LuminTask<AsyncLock.ReleaseScope> WaitAsync(AsyncLock.ReleaseScope lockScope, CancellationToken cancellationToken = default)
        {
            if (lockScope.Equals(default))
                throw new ArgumentException("Lock scope is not valid", nameof(lockScope));

            return _conditionVariable.WaitWithLockAsync(lockScope, cancellationToken);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public LuminTask<AsyncLock.ReleaseScope> WaitAsync(AsyncLock.ReleaseScope lockScope, TimeSpan timeout)
        {
            using var cts = new CancellationTokenSource(timeout);
            return WaitAsync(lockScope, cts.Token);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Pulse()
        {
            _conditionVariable.Notify();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void PulseAll()
        {
            _conditionVariable.NotifyAll();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Exit(AsyncLock.ReleaseScope lockScope)
        {
            lockScope.Dispose();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ValueTask ExitAsync(AsyncLock.ReleaseScope lockScope)
        {
            return lockScope.DisposeAsync();
        }

        public readonly struct MonitorScope : IDisposable, IAsyncDisposable
        {
            private readonly AsyncLock.ReleaseScope _lockScope;
            private readonly AsyncMonitor _monitor;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public MonitorScope(AsyncMonitor monitor, AsyncLock.ReleaseScope lockScope)
            {
                _monitor = monitor;
                _lockScope = lockScope;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public LuminTask<MonitorScope> WaitAsync(CancellationToken cancellationToken = default)
            {
                return WaitAndRetakeLock(_monitor.WaitAsync(_lockScope, cancellationToken), _monitor);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public LuminTask<MonitorScope> WaitAsync(TimeSpan timeout)
            {
                return WaitAndRetakeLock(_monitor.WaitAsync(_lockScope, timeout), _monitor);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Pulse() => _monitor.Pulse();

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void PulseAll() => _monitor.PulseAll();

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Dispose()
            {
                _monitor.Exit(_lockScope);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public ValueTask DisposeAsync()
            {
                return _monitor.ExitAsync(_lockScope);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static async LuminTask<MonitorScope> WaitAndRetakeLock(
                LuminTask<AsyncLock.ReleaseScope> waitTask, AsyncMonitor monitor)
            {
                var newLockScope = await waitTask;
                return new MonitorScope(monitor, newLockScope);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static LuminTask<MonitorScope> EnterScopeAsync(AsyncMonitor monitor, CancellationToken cancellationToken = default)
        {
            return EnterScopeInternal(monitor, cancellationToken);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static async LuminTask<MonitorScope> EnterScopeInternal(AsyncMonitor monitor, CancellationToken cancellationToken)
        {
            var lockScope = await monitor.EnterAsync(cancellationToken);
            return new MonitorScope(monitor, lockScope);
        }
    }

    public static class AsyncMonitorExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static LuminTask<AsyncMonitor.MonitorScope> EnterScopeAsync(this AsyncMonitor monitor, CancellationToken cancellationToken = default)
        {
            return AsyncMonitor.EnterScopeAsync(monitor, cancellationToken);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static LuminTask<AsyncLock.ReleaseScope> WaitWithLockAsync(
            this AsyncConditionVariable conditionVariable,
            AsyncLock.ReleaseScope currentLock,
            CancellationToken cancellationToken = default)
        {
            if (conditionVariable == null) throw new ArgumentNullException(nameof(conditionVariable));

            currentLock.Dispose();

            var waitTask = conditionVariable.WaitAsync(cancellationToken);
            
            return WaitAndRetakeLock(waitTask, conditionVariable._asyncLock);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static async LuminTask<AsyncLock.ReleaseScope> WaitAndRetakeLock(
            LuminTask<AsyncLock.ReleaseScope> waitTask, AsyncLock asyncLock)
        {
            await waitTask;
            return await asyncLock.LockAsync();
        }
    }
}