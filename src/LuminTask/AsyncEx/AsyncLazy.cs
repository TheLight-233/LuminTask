using System;
using System.Runtime.CompilerServices;
using System.Collections.Generic;
using System.Threading;

namespace LuminThread.AsyncEx
{
    [Flags]
    public enum AsyncLazyFlags
    {
        None = 0x0,
        ExecuteOnCallingThread = 0x1,
        RetryOnFailure = 0x2,
    }

    /// <summary>延迟异步初始化。值类型句柄，共享内部状态。</summary>
    public readonly struct AsyncLazy<T> : IDisposable
    {
        private readonly AsyncLazyState<T>? _state;

        public AsyncLazy(Func<T> factory, AsyncLazyFlags flags = AsyncLazyFlags.None)
        {
            _state = new AsyncLazyState<T>(WrapFactory(factory), flags);
        }

        public AsyncLazy(Func<LuminTask<T>> factory, AsyncLazyFlags flags = AsyncLazyFlags.None)
        {
            _state = new AsyncLazyState<T>(factory, flags);
        }

        public bool IsStarted => GetState().IsStarted;

        public LuminTask<T> Task => GetState().Task;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public LuminTask<T> GetAwaiter() => Task.GetAwaiter();

        public void Dispose() => GetState().Dispose();

        private AsyncLazyState<T> GetState()
            => _state ?? throw new InvalidOperationException("AsyncLazy<T> 未初始化（default 值）或已释放");

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Func<LuminTask<T>> WrapFactory(Func<T> factory)
            => () => LuminTask.FromResult(factory());
    }

    internal sealed class AsyncLazyState<T> : IDisposable
    {
        private readonly object _mutex = new object();
        private readonly Func<LuminTask<T>> _factory;
        private readonly bool _retryOnFailure;

        private readonly List<LuminTaskCompletionSource<T>> _waiters = new();
        private bool _isStarted;
        private bool _isCompleted;
        private T? _result;
        private Exception? _error;
        private volatile bool _isDisposed;

        public AsyncLazyState(Func<LuminTask<T>> factory, AsyncLazyFlags flags)
        {
            if (factory == null)
                throw new ArgumentNullException(nameof(factory));

            _retryOnFailure = (flags & AsyncLazyFlags.RetryOnFailure) == AsyncLazyFlags.RetryOnFailure;

            if ((flags & AsyncLazyFlags.ExecuteOnCallingThread) == AsyncLazyFlags.ExecuteOnCallingThread)
                _factory = factory;
            else
                _factory = () => LuminTask.Run(factory);
        }

        public bool IsStarted
        {
            get
            {
                lock (_mutex)
                    return _isStarted;
            }
        }

        public LuminTask<T> Task
        {
            get
            {
                if (_isDisposed)
                    throw new ObjectDisposedException(nameof(AsyncLazy<T>));

                LuminTask<T> result;
                var start = false;
                lock (_mutex)
                {
                    if (_isCompleted)
                    {
                        if (_error is null)
                            return LuminTask.FromResult(_result!);
                        if (_error is OperationCanceledException canceled)
                            return LuminTask.FromCanceled<T>(canceled.CancellationToken);
                        return LuminTask.FromException<T>(_error);
                    }

                    if (!_isStarted)
                    {
                        _isStarted = true;
                        start = true;
                    }

                    var waiter = new LuminTaskCompletionSource<T>();
                    _waiters.Add(waiter);
                    result = waiter.Task;
                }

                if (start)
                    ResolveAsync().Forget();
                return result;
            }
        }

        private async LuminTask ResolveAsync()
        {
            try
            {
                var result = await _factory().ConfigureAwait(false);
                List<LuminTaskCompletionSource<T>> waiters;
                lock (_mutex)
                {
                    _result = result;
                    _error = null;
                    _isCompleted = true;
                    waiters = new List<LuminTaskCompletionSource<T>>(_waiters);
                    _waiters.Clear();
                }
                foreach (var waiter in waiters)
                    waiter.TrySetResult(result);
            }
            catch (Exception exception)
            {
                List<LuminTaskCompletionSource<T>> waiters;
                lock (_mutex)
                {
                    _error = exception;
                    _isCompleted = !_retryOnFailure;
                    _isStarted = _retryOnFailure ? false : true;
                    waiters = new List<LuminTaskCompletionSource<T>>(_waiters);
                    _waiters.Clear();
                }
                foreach (var waiter in waiters)
                {
                    if (exception is OperationCanceledException canceled)
                        waiter.TrySetCanceled(canceled.CancellationToken);
                    else
                        waiter.TrySetException(exception);
                }
            }
        }

        public void Dispose()
        {
            _isDisposed = true;
        }
    }

    public static class AsyncLazyExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static AsyncLazy<T> Create<T>(Func<T> factory, AsyncLazyFlags flags = AsyncLazyFlags.None)
        {
            return new AsyncLazy<T>(factory, flags);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static AsyncLazy<T> Create<T>(Func<LuminTask<T>> factory, AsyncLazyFlags flags = AsyncLazyFlags.None)
        {
            return new AsyncLazy<T>(factory, flags);
        }
    }
}
