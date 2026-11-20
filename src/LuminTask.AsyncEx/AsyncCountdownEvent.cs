using LuminThread.Utility;
using System;
using System.Runtime.CompilerServices;
using System.Threading;

namespace LuminThread.AsyncEx
{
    /// <summary>异步倒计数事件。值类型句柄，共享池化内部状态。</summary>
    public readonly struct AsyncCountdownEvent : IDisposable
    {
        private readonly CountdownEventState? _state;
        private readonly uint _generation;

        public AsyncCountdownEvent(int initialCount)
        {
            if (initialCount < 0)
                throw new ArgumentOutOfRangeException(nameof(initialCount));

            var state = EventStatePool<CountdownEventState>.Rent();
            state.Initialize(initialCount);
            _state = state;
            _generation = state.Generation;
        }

        public int CurrentCount => GetState().CurrentCount;

        public bool IsSet => GetState().IsSet;

        public bool IsDisposed => GetState().IsDisposed;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public LuminTask WaitAsync(CancellationToken cancellationToken = default)
            => GetState().WaitAsync(cancellationToken);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Signal() => GetState().Signal();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Signal(int signalCount) => GetState().Signal(signalCount);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddCount() => GetState().AddCount();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddCount(int count) => GetState().AddCount(count);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryAddCount() => GetState().TryAddCount();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryAddCount(int count) => GetState().TryAddCount(count);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Reset() => GetState().Reset();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Reset(int count) => GetState().Reset(count);

        public void Dispose()
        {
            var state = _state;
            if (state == null || state.Generation != _generation) return;
            state.Dispose();
        }

        private CountdownEventState GetState()
            => _state is { } state && state.Generation == _generation
                ? state
                : throw new InvalidOperationException("AsyncCountdownEvent 未初始化（default 值）、已释放或句柄过期");
    }

    internal sealed class CountdownEventState : IEventPoolableState
    {
        public ILuminCacheNode? CacheNext { get; set; }
        public int Charge => 192;

        private readonly object _mutex = new();
        private readonly IAsyncWaitQueue<object> _queue;
        private volatile int _currentCount;
        private volatile bool _isDisposed;

        internal uint Generation { get; private set; }

        public CountdownEventState()
        {
            _queue = new DefaultAsyncWaitQueue<object>();
        }

        internal void Initialize(int initialCount) => _currentCount = initialCount;

        void IEventPoolableState.ResetForReuse()
        {
            _currentCount = 0;
            _isDisposed = false;
        }

        void IEventPoolableState.BumpGeneration() => Generation++;

        public int CurrentCount => _currentCount;

        public bool IsSet => _currentCount == 0;

        public bool IsDisposed => _isDisposed;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public LuminTask WaitAsync(CancellationToken cancellationToken = default)
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(AsyncCountdownEvent));

            if (_currentCount == 0)
                return LuminTask.CompletedTask();

            if (cancellationToken.IsCancellationRequested)
                return LuminTask.FromCanceled(cancellationToken);

            lock (_mutex)
            {
                if (_currentCount == 0)
                    return LuminTask.CompletedTask();

                return _queue.Enqueue(cancellationToken);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Signal()
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(AsyncCountdownEvent));

            lock (_mutex)
            {
                if (_currentCount <= 0)
                    throw new InvalidOperationException("Cannot signal more than current count");

                _currentCount--;

                if (_currentCount == 0)
                    _queue.DequeueAll(null);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Signal(int signalCount)
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(AsyncCountdownEvent));

            if (signalCount <= 0)
                throw new ArgumentOutOfRangeException(nameof(signalCount));

            lock (_mutex)
            {
                if (_currentCount < signalCount)
                    throw new InvalidOperationException("Cannot signal more than current count");

                _currentCount -= signalCount;

                if (_currentCount == 0)
                    _queue.DequeueAll(null);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddCount()
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(AsyncCountdownEvent));

            lock (_mutex)
            {
                _currentCount++;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddCount(int count)
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(AsyncCountdownEvent));

            if (count <= 0)
                throw new ArgumentOutOfRangeException(nameof(count));

            lock (_mutex)
            {
                _currentCount += count;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryAddCount()
        {
            if (_isDisposed)
                return false;

            lock (_mutex)
            {
                _currentCount++;
                return true;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryAddCount(int count)
        {
            if (_isDisposed || count <= 0)
                return false;

            lock (_mutex)
            {
                _currentCount += count;
                return true;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Reset()
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(AsyncCountdownEvent));

            lock (_mutex)
            {
                _currentCount = 1;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Reset(int count)
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(AsyncCountdownEvent));

            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count));

            lock (_mutex)
            {
                _currentCount = count;
            }
        }

        public void Dispose()
        {
            lock (_mutex)
            {
                if (_isDisposed) return;
                _isDisposed = true;
                _queue.CancelAll(default);
            }

            EventStatePool<CountdownEventState>.Return(this);
        }
    }

    public static class AsyncCountdownEventExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static LuminTask WaitAsync(this AsyncCountdownEvent evt, TimeSpan timeout)
        {
            using var cts = new CancellationTokenSource(timeout);
            return evt.WaitAsync(cts.Token);
        }
    }
}
