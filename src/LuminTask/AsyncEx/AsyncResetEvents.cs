using System;
using System.Runtime.CompilerServices;
using System.Threading;

namespace LuminThread.AsyncEx
{
    public sealed class AsyncManualResetEvent : IDisposable
    {
        private readonly object _mutex = new object();
        private readonly IAsyncWaitQueue<object> _queue;
        private volatile bool _isSet;
        private volatile bool _isDisposed;

        public AsyncManualResetEvent(bool initialState = false)
        {
            _queue = new DefaultAsyncWaitQueue<object>();
            _isSet = initialState;
        }

        ~AsyncManualResetEvent() => Dispose(false);

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

                return _queue.Enqueue(cancellationToken);
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
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                _isDisposed = true;
                if (disposing)
                    _queue.CancelAll(default);
            }
        }
    }

    public sealed class AsyncManualResetEvent<T> : IDisposable
    {
        private readonly object _mutex = new object();
        private readonly IAsyncWaitQueue<T> _queue;
        private volatile bool _isSet;
        private T _result;
        private volatile bool _isDisposed;

        public AsyncManualResetEvent()
        {
            _queue = new DefaultAsyncWaitQueue<T>();
        }

        ~AsyncManualResetEvent() => Dispose(false);

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

                return _queue.Enqueue(cancellationToken);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                _isDisposed = true;
                if (disposing)
                    _queue.CancelAll(default);
            }
        }
    }

    public sealed class AsyncAutoResetEvent : IDisposable
    {
        private readonly object _mutex = new object();
        private readonly IAsyncWaitQueue<object> _queue;
        private volatile bool _isSet;
        private volatile bool _isDisposed;

        public AsyncAutoResetEvent(bool initialState = false)
        {
            _queue = new DefaultAsyncWaitQueue<object>();
            _isSet = initialState;
        }

        ~AsyncAutoResetEvent() => Dispose(false);

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

                return _queue.Enqueue(cancellationToken);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Set()
        {
            lock (_mutex)
            {
                if (_queue.IsEmpty)
                    _isSet = true;
                else
                    _queue.Dequeue(null);
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
                if (disposing)
                    _queue.CancelAll(default);
            }
        }
    }

    public sealed class AsyncAutoResetEvent<T> : IDisposable
    {
        private readonly object _mutex = new object();
        private readonly IAsyncWaitQueue<T> _queue;
        private volatile bool _isSet;
        private T _result;
        private volatile bool _isDisposed;

        public AsyncAutoResetEvent()
        {
            _queue = new DefaultAsyncWaitQueue<T>();
        }

        ~AsyncAutoResetEvent() => Dispose(false);

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

                return _queue.Enqueue(cancellationToken);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Set(T result)
        {
            lock (_mutex)
            {
                if (_queue.IsEmpty)
                {
                    _isSet = true;
                    _result = result;
                }
                else
                {
                    _queue.Dequeue(result);
                }
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
                if (disposing)
                    _queue.CancelAll(default);
            }
        }
    }

    public static class AsyncResetEventExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static LuminTask WaitAsync(this AsyncManualResetEvent evt, TimeSpan timeout)
        {
            using var cts = new CancellationTokenSource(timeout);
            return evt.WaitAsync(cts.Token);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static LuminTask<T> WaitAsync<T>(this AsyncManualResetEvent<T> evt, TimeSpan timeout)
        {
            using var cts = new CancellationTokenSource(timeout);
            return evt.WaitAsync(cts.Token);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static LuminTask WaitAsync(this AsyncAutoResetEvent evt, TimeSpan timeout)
        {
            using var cts = new CancellationTokenSource(timeout);
            return evt.WaitAsync(cts.Token);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static LuminTask<T> WaitAsync<T>(this AsyncAutoResetEvent<T> evt, TimeSpan timeout)
        {
            using var cts = new CancellationTokenSource(timeout);
            return evt.WaitAsync(cts.Token);
        }
    }
}
