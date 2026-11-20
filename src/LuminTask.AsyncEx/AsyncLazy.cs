using System;
using System.Runtime.CompilerServices;
using System.Collections.Generic;
using LuminThread.TaskSource;
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
        private Func<LuminTask<T>>? _factory;
        private readonly bool _executeOnCallingThread;
        private readonly bool _retryOnFailure;

        private ulong _firstWaiter;
        private ulong _secondWaiter;
        private List<ulong>? _additionalWaiters;
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

            _executeOnCallingThread = (flags & AsyncLazyFlags.ExecuteOnCallingThread) != 0;
            _factory = factory;
        }

        public bool IsStarted
        {
            get
            {
                lock (this)
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
                lock (this)
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

                    result = LuminTaskSource.Create<T>(out var waiter);
                    if (_firstWaiter == 0) _firstWaiter = waiter;
                    else if (_secondWaiter == 0) _secondWaiter = waiter;
                    else (_additionalWaiters ??= new List<ulong>()).Add(waiter);
                }

                if (start)
                    ResolveAsync().Forget();
                return result;
            }
        }

        private async LuminTask ResolveAsync()
        {
            T result = default!;
            Exception? error = null;
            try
            {
                var factory = _factory!;
                result = await (_executeOnCallingThread ? factory() : LuminTask.Run(factory)).ConfigureAwait(false);
            }
            catch (Exception ex) { error = ex; }

            ulong first, second;
            List<ulong>? additional;
            lock (this)
            {
                _result = result;
                _error = error;
                _isCompleted = error == null || !_retryOnFailure;
                _isStarted = _isCompleted;
                if (_isCompleted) _factory = null;
                first = _firstWaiter;
                second = _secondWaiter;
                additional = _additionalWaiters;
                _firstWaiter = 0;
                _secondWaiter = 0;
                _additionalWaiters = null;
            }

            Complete(first, in result, error);
            if (second != 0) Complete(second, in result, error);
            if (additional != null)
                foreach (var handle in additional)
                    Complete(handle, in result, error);
        }

        private static void Complete(ulong handle, in T result, Exception? error)
        {
            if (error == null) LuminTaskResultBridge.TrySetResult(handle, in result);
            else LuminTaskSource.TrySetException(handle, error);
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
