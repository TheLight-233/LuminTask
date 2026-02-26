using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace LuminThread.AsyncEx
{
    public sealed class AsyncLock : IDisposable
    {
        private readonly IAsyncWaitQueue<ReleaseScope> _waitQueue;
        private volatile int _state;
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

            if (Interlocked.CompareExchange(ref _state, 1, 0) == 0)
                return LuminTask.FromResult(new ReleaseScope(this));

            var task = _waitQueue.Enqueue(cancellationToken);

            if (Interlocked.CompareExchange(ref _state, 1, 0) == 0)
            {
                if (_waitQueue.TryCancel(task, cancellationToken))
                    return LuminTask.FromResult(new ReleaseScope(this));
            }

            return task;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void Release()
        {
            if (_isDisposed) return;

            Interlocked.Exchange(ref _state, 0);

            if (!_waitQueue.IsEmpty)
            {
                if (Interlocked.CompareExchange(ref _state, 1, 0) == 0)
                    _waitQueue.Dequeue(new ReleaseScope(this));
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
                _waitQueue.CancelAll(default);
            }
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
