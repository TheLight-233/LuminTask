using LuminThread.Utility;
using System;
using System.Runtime.CompilerServices;
using System.Threading;

namespace LuminThread.AsyncEx
{
    /// <summary>异步手动复位事件。值类型句柄，共享池化内部状态。</summary>
    public readonly struct AsyncManualResetEvent : IDisposable
    {
        private readonly ManualResetEventState? _state;
        private readonly uint _generation;

        public AsyncManualResetEvent() : this(false)
        {
        }

        public AsyncManualResetEvent(bool initialState)
        {
            var state = EventStatePool<ManualResetEventState>.Rent();
            state.Initialize(initialState);
            _state = state;
            _generation = state.Generation;
        }

        public bool IsSet => GetState().IsSet;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public LuminTask WaitAsync(CancellationToken cancellationToken = default)
            => GetState().WaitAsync(cancellationToken);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Set() => GetState().Set();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Reset() => GetState().Reset();

        public void Dispose()
        {
            var state = _state;
            if (state == null || state.Generation != _generation) return;
            state.Dispose();
        }

        private ManualResetEventState GetState()
            => _state is { } state && state.Generation == _generation
                ? state
                : throw new InvalidOperationException("AsyncManualResetEvent 未初始化（default 值）、已释放或句柄过期");
    }

    /// <summary>带结果的异步手动复位事件。值类型句柄，共享池化内部状态。</summary>
    public readonly struct AsyncManualResetEvent<T> : IDisposable
    {
        private readonly ManualResetEventState<T>? _state;
        private readonly uint _generation;

        public AsyncManualResetEvent()
        {
            var state = EventStatePool<ManualResetEventState<T>>.Rent();
            state.Initialize();
            _state = state;
            _generation = state.Generation;
        }

        public bool IsSet => GetState().IsSet;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public LuminTask<T> WaitAsync(CancellationToken cancellationToken = default)
            => GetState().WaitAsync(cancellationToken);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Set(T result) => GetState().Set(result);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Reset() => GetState().Reset();

        public void Dispose()
        {
            var state = _state;
            if (state == null || state.Generation != _generation) return;
            state.Dispose();
        }

        internal ManualResetEventState<T> GetState()
            => _state is { } state && state.Generation == _generation
                ? state
                : throw new InvalidOperationException("AsyncManualResetEvent<T> 未初始化（default 值）、已释放或句柄过期");
    }

    /// <summary>异步自动复位事件。值类型句柄，共享池化内部状态。</summary>
    public readonly struct AsyncAutoResetEvent : IDisposable
    {
        private readonly AutoResetEventState? _state;
        private readonly uint _generation;

        public AsyncAutoResetEvent() : this(false)
        {
        }

        public AsyncAutoResetEvent(bool initialState)
        {
            var state = EventStatePool<AutoResetEventState>.Rent();
            state.Initialize(initialState);
            _state = state;
            _generation = state.Generation;
        }

        public bool IsSet => GetState().IsSet;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public LuminTask WaitAsync(CancellationToken cancellationToken = default)
            => GetState().WaitAsync(cancellationToken);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Set() => GetState().Set();

        public void Dispose()
        {
            var state = _state;
            if (state == null || state.Generation != _generation) return;
            state.Dispose();
        }

        internal AutoResetEventState GetState()
            => _state is { } state && state.Generation == _generation
                ? state
                : throw new InvalidOperationException("AsyncAutoResetEvent 未初始化（default 值）、已释放或句柄过期");
    }

    /// <summary>带结果的异步自动复位事件。值类型句柄，共享池化内部状态。</summary>
    public readonly struct AsyncAutoResetEvent<T> : IDisposable
    {
        private readonly AutoResetEventState<T>? _state;
        private readonly uint _generation;

        public AsyncAutoResetEvent()
        {
            var state = EventStatePool<AutoResetEventState<T>>.Rent();
            state.Initialize();
            _state = state;
            _generation = state.Generation;
        }

        public bool IsSet => GetState().IsSet;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public LuminTask<T> WaitAsync(CancellationToken cancellationToken = default)
            => GetState().WaitAsync(cancellationToken);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Set(T result) => GetState().Set(result);

        public void Dispose()
        {
            var state = _state;
            if (state == null || state.Generation != _generation) return;
            state.Dispose();
        }

        internal AutoResetEventState<T> GetState()
            => _state is { } state && state.Generation == _generation
                ? state
                : throw new InvalidOperationException("AsyncAutoResetEvent<T> 未初始化（default 值）、已释放或句柄过期");
    }

    internal sealed class ManualResetEventState : IEventPoolableState
    {
        public ILuminCacheNode? CacheNext { get; set; }
        public int Charge => 192;

        private readonly object _mutex = new();
        private readonly DefaultAsyncWaitQueue<object> _queue;
        private volatile bool _isSet;
        private volatile bool _isDisposed;

        internal uint Generation { get; private set; }

        public ManualResetEventState()
        {
            _queue = new DefaultAsyncWaitQueue<object>();
        }

        internal void Initialize(bool initialState) => _isSet = initialState;

        void IEventPoolableState.ResetForReuse()
        {
            _isSet = false;
            _isDisposed = false;
        }

        void IEventPoolableState.BumpGeneration() => Generation++;

        public bool IsSet => _isSet;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public LuminTask WaitAsync(CancellationToken cancellationToken = default)
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(AsyncManualResetEvent));

            if (_isSet)
                return LuminTask.CompletedTask();

            if (cancellationToken.IsCancellationRequested)
                return LuminTask.FromCanceled(cancellationToken);

            lock (_mutex)
            {
                if (_isSet)
                    return LuminTask.CompletedTask();

                return _queue.EnqueueWithCancellation(cancellationToken);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Set()
        {
            lock (_mutex)
            {
                if (!_isSet)
                {
                    _isSet = true;
                    _queue.DequeueAll(null);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Reset()
        {
            lock (_mutex)
            {
                _isSet = false;
            }
        }

        public void Dispose()
        {
            if (_isDisposed) return;
            _isDisposed = true;
            _queue.CancelAll(default);
            EventStatePool<ManualResetEventState>.Return(this);
        }
    }

    internal sealed class ManualResetEventState<T> : IEventPoolableState
    {
        public ILuminCacheNode? CacheNext { get; set; }
        public int Charge => 192 + Unsafe.SizeOf<T>();

        private readonly object _mutex = new();
        private readonly DefaultAsyncWaitQueue<T> _queue;
        private volatile bool _isSet;
        private T _result;
        private volatile bool _isDisposed;

        internal uint Generation { get; private set; }

        public ManualResetEventState()
        {
            _queue = new DefaultAsyncWaitQueue<T>();
        }

        internal void Initialize() { }

        void IEventPoolableState.ResetForReuse()
        {
            _isSet = false;
            _result = default;
            _isDisposed = false;
        }

        void IEventPoolableState.BumpGeneration() => Generation++;

        public bool IsSet => _isSet;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public LuminTask<T> WaitAsync(CancellationToken cancellationToken = default)
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(AsyncManualResetEvent<T>));

            if (_isSet)
                return LuminTask.FromResult(_result);

            if (cancellationToken.IsCancellationRequested)
                return LuminTask.FromCanceled<T>(cancellationToken);

            lock (_mutex)
            {
                if (_isSet)
                    return LuminTask.FromResult(_result);

                return _queue.EnqueueWithCancellation(cancellationToken);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal bool TryWait(out T result)
        {
            result = default!;
            if (!Monitor.TryEnter(_mutex)) return false;
            try
            {
                if (_isDisposed || !_isSet) return false;
                result = _result;
                return true;
            }
            finally { Monitor.Exit(_mutex); }
        }

        public void Set(T result)
        {
            lock (_mutex)
            {
                if (!_isSet)
                {
                    _isSet = true;
                    _result = result;
                    _queue.DequeueAll(result);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Reset()
        {
            lock (_mutex)
            {
                _isSet = false;
                _result = default;
            }
        }

        public void Dispose()
        {
            if (_isDisposed) return;
            _isDisposed = true;
            _queue.CancelAll(default);
            EventStatePool<ManualResetEventState<T>>.Return(this);
        }
    }

    internal sealed class AutoResetEventState : IEventPoolableState
    {
        public ILuminCacheNode? CacheNext { get; set; }
        public int Charge => 192;

        private readonly object _mutex = new();
        private readonly DefaultAsyncWaitQueue<object> _queue;
        private volatile bool _isSet;
        private volatile bool _isDisposed;

        internal uint Generation { get; private set; }

        public AutoResetEventState()
        {
            _queue = new DefaultAsyncWaitQueue<object>();
        }

        internal void Initialize(bool initialState) => _isSet = initialState;

        void IEventPoolableState.ResetForReuse()
        {
            _isSet = false;
            _isDisposed = false;
        }

        void IEventPoolableState.BumpGeneration() => Generation++;

        public bool IsSet => _isSet;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public LuminTask WaitAsync(CancellationToken cancellationToken = default)
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(AsyncAutoResetEvent));

            if (cancellationToken.IsCancellationRequested)
                return LuminTask.FromCanceled(cancellationToken);

            lock (_mutex)
            {
                if (_isSet && _queue.IsEmpty)
                {
                    _isSet = false;
                    return LuminTask.CompletedTask();
                }

                return _queue.EnqueueWithCancellation(cancellationToken);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal bool TryWait()
        {
            if (!Monitor.TryEnter(_mutex)) return false;
            try
            {
                if (_isDisposed || !_isSet) return false;
                _isSet = false;
                return true;
            }
            finally { Monitor.Exit(_mutex); }
        }

        public void Set()
        {
            lock (_mutex)
            {
                if (!_queue.TryDequeue(null))
                    _isSet = true;
            }
        }

        public void Dispose()
        {
            if (_isDisposed) return;
            _isDisposed = true;
            _queue.CancelAll(default);
            EventStatePool<AutoResetEventState>.Return(this);
        }
    }

    internal sealed class AutoResetEventState<T> : IEventPoolableState
    {
        public ILuminCacheNode? CacheNext { get; set; }
        public int Charge => 192 + Unsafe.SizeOf<T>();

        private readonly object _mutex = new();
        private readonly DefaultAsyncWaitQueue<T> _queue;
        private volatile bool _isSet;
        private T _result;
        private volatile bool _isDisposed;

        internal uint Generation { get; private set; }

        public AutoResetEventState()
        {
            _queue = new DefaultAsyncWaitQueue<T>();
        }

        internal void Initialize() { }

        void IEventPoolableState.ResetForReuse()
        {
            _isSet = false;
            _result = default;
            _isDisposed = false;
        }

        void IEventPoolableState.BumpGeneration() => Generation++;

        public bool IsSet => _isSet;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public LuminTask<T> WaitAsync(CancellationToken cancellationToken = default)
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(AsyncAutoResetEvent<T>));

            if (cancellationToken.IsCancellationRequested)
                return LuminTask.FromCanceled<T>(cancellationToken);

            lock (_mutex)
            {
                if (_isSet && _queue.IsEmpty)
                {
                    var result = _result;
                    _isSet = false;
                    _result = default;
                    return LuminTask.FromResult(result);
                }

                return _queue.EnqueueWithCancellation(cancellationToken);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal bool TryWait(out T result)
        {
            result = default!;
            if (!Monitor.TryEnter(_mutex)) return false;
            try
            {
                if (_isDisposed || !_isSet) return false;
                result = _result;
                _isSet = false;
                _result = default!;
                return true;
            }
            finally { Monitor.Exit(_mutex); }
        }

        public void Set(T result)
        {
            lock (_mutex)
            {
                if (!_queue.TryDequeue(result))
                {
                    _isSet = true;
                    _result = result;
                }
            }
        }

        public void Dispose()
        {
            if (_isDisposed) return;
            _isDisposed = true;
            _queue.CancelAll(default);
            EventStatePool<AutoResetEventState<T>>.Return(this);
        }
    }

    public static class AsyncResetEventExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static LuminTask WaitAsync(this AsyncManualResetEvent evt, TimeSpan timeout)
            => WaitManualAsync(evt, timeout);

        private static async LuminTask WaitManualAsync(AsyncManualResetEvent evt, TimeSpan timeout)
        {
            if (timeout == Timeout.InfiniteTimeSpan) { await evt.WaitAsync(); return; }
            if (WaitTimeout.CanAcquireImmediately(timeout) && evt.IsSet) return;
            using var cts = new CancellationTokenSource(timeout);
            await evt.WaitAsync(cts.Token);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static LuminTask<T> WaitAsync<T>(this AsyncManualResetEvent<T> evt, TimeSpan timeout)
            => WaitManualAsync(evt, timeout);

        private static async LuminTask<T> WaitManualAsync<T>(AsyncManualResetEvent<T> evt, TimeSpan timeout)
        {
            if (timeout == Timeout.InfiniteTimeSpan) return await evt.WaitAsync();
            if (WaitTimeout.CanAcquireImmediately(timeout) && evt.GetState().TryWait(out var result)) return result;
            using var cts = new CancellationTokenSource(timeout);
            return await evt.WaitAsync(cts.Token);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static LuminTask WaitAsync(this AsyncAutoResetEvent evt, TimeSpan timeout)
            => WaitAutoAsync(evt, timeout);

        private static async LuminTask WaitAutoAsync(AsyncAutoResetEvent evt, TimeSpan timeout)
        {
            if (timeout == Timeout.InfiniteTimeSpan) { await evt.WaitAsync(); return; }
            if (WaitTimeout.CanAcquireImmediately(timeout) && evt.GetState().TryWait()) return;
            using var cts = new CancellationTokenSource(timeout);
            await evt.WaitAsync(cts.Token);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static LuminTask<T> WaitAsync<T>(this AsyncAutoResetEvent<T> evt, TimeSpan timeout)
            => WaitAutoAsync(evt, timeout);

        private static async LuminTask<T> WaitAutoAsync<T>(AsyncAutoResetEvent<T> evt, TimeSpan timeout)
        {
            if (timeout == Timeout.InfiniteTimeSpan) return await evt.WaitAsync();
            if (WaitTimeout.CanAcquireImmediately(timeout) && evt.GetState().TryWait(out var result)) return result;
            using var cts = new CancellationTokenSource(timeout);
            return await evt.WaitAsync(cts.Token);
        }
    }
}
