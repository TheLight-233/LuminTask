using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace LuminThread.AsyncEx
{
    /// <summary>异步互斥锁。值类型句柄，共享内部状态。</summary>
    public readonly struct AsyncLock : IDisposable
    {
        private readonly LockState? _state;

        public AsyncLock() : this(new DefaultAsyncWaitQueue<ReleaseScope>())
        {
        }

        public AsyncLock(IAsyncWaitQueue<ReleaseScope> waitQueue)
        {
            _state = new LockState(waitQueue);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public LuminTask<ReleaseScope> LockAsync(CancellationToken cancellationToken = default)
            => GetState().LockAsync(cancellationToken);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void ReleaseLock() => GetState().Release();

        public void Dispose() => GetState().Dispose();

        private LockState GetState()
            => _state ?? throw new InvalidOperationException("AsyncLock 未初始化（default 值）或已释放");

        /// <summary>锁作用域句柄。Dispose 即释放。结构体，获取锁无分配。</summary>
        public readonly struct ReleaseScope : IDisposable, IAsyncDisposable
        {
            private readonly LockState? _parent;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal ReleaseScope(LockState parent) => _parent = parent;

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

    internal sealed class LockState : IDisposable
    {
        private readonly IAsyncWaitQueue<AsyncLock.ReleaseScope> _waitQueue;
        private readonly object _mutex = new object();
        private bool _locked;
        private volatile bool _isDisposed;

        public LockState(IAsyncWaitQueue<AsyncLock.ReleaseScope> waitQueue)
        {
            _waitQueue = waitQueue ?? throw new ArgumentNullException(nameof(waitQueue));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public LuminTask<AsyncLock.ReleaseScope> LockAsync(CancellationToken cancellationToken = default)
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(AsyncLock));

            if (cancellationToken.IsCancellationRequested)
                return LuminTask.FromCanceled<AsyncLock.ReleaseScope>(cancellationToken);

            lock (_mutex)
            {
                if (!_locked)
                {
                    _locked = true;
                    return LuminTask.FromResult(new AsyncLock.ReleaseScope(this));
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

                if (!_waitQueue.TryDequeue(new AsyncLock.ReleaseScope(this)))
                    _locked = false;
            }
        }

        public void Dispose()
        {
            if (_isDisposed) return;
            _isDisposed = true;
            _waitQueue.CancelAll(default);
        }
    }
}
