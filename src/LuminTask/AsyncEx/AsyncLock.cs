using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace LuminThread.AsyncEx
{
    public sealed class AsyncLock : IDisposable
    {
        private readonly IAsyncWaitQueue<ReleaseScope> _waitQueue;
        private readonly object _mutex = new object();
        private bool _locked;
        private volatile bool _isDisposed;

        public AsyncLock() : this(new DefaultAsyncWaitQueue<ReleaseScope>()) { }

        public AsyncLock(IAsyncWaitQueue<ReleaseScope> waitQueue)
        {
            _waitQueue = waitQueue ?? throw new ArgumentNullException(nameof(waitQueue));
        }

        ~AsyncLock() => Dispose(false);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public LuminTask<ReleaseScope> LockAsync(CancellationToken cancellationToken = default)
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(AsyncLock));

            if (cancellationToken.IsCancellationRequested)
                return LuminTask.FromCanceled<ReleaseScope>(cancellationToken);

            lock (_mutex)
            {
                if (!_locked)
                {
                    _locked = true;
                    return LuminTask.FromResult(new ReleaseScope(this));
                }

                return _waitQueue.Enqueue(cancellationToken);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void Release()
        {
            lock (_mutex)
            {
                if (_isDisposed) return;

                // Hand the lock directly to the next waiter; if none remain (e.g. all were
                // cancelled), mark it free. No window where the lock is free yet a waiter waits.
                if (!_waitQueue.TryDequeue(new ReleaseScope(this)))
                    _locked = false;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (_isDisposed) return;
            _isDisposed = true;

            // Only from an explicit Dispose: completing queued tasks from the finalizer could
            // run continuations on the finalizer thread / touch already-finalized objects.
            if (disposing)
                _waitQueue.CancelAll(default);
        }

        public readonly struct ReleaseScope : IDisposable, IAsyncDisposable
        {
            private readonly AsyncLock _parent;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public ReleaseScope(AsyncLock parent) => _parent = parent;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Dispose() => _parent?.Release();

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public ValueTask DisposeAsync()
            {
                _parent?.Release();
                return default;
            }

            public override bool Equals(object obj) => obj is ReleaseScope other && Equals(other);

            public bool Equals(ReleaseScope other) => ReferenceEquals(_parent, other._parent);

            public override int GetHashCode() => _parent?.GetHashCode() ?? 0;

            public static bool operator ==(ReleaseScope left, ReleaseScope right) => left.Equals(right);

            public static bool operator !=(ReleaseScope left, ReleaseScope right) => !left.Equals(right);
        }
    }
}
