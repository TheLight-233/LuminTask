using System;
using System.Runtime.CompilerServices;
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

    public sealed class AsyncLazy<T> : IDisposable
    {
        private readonly object _mutex = new object();
        private readonly Func<LuminTask<T>> _factory;
        private readonly bool _retryOnFailure;
        private LuminTask<T> _instance;
        private bool _isStarted;
        private volatile bool _isDisposed;

        public AsyncLazy(Func<T> factory, AsyncLazyFlags flags = AsyncLazyFlags.None)
            : this(WrapFactory(factory), flags)
        {
        }

        public AsyncLazy(Func<LuminTask<T>> factory, AsyncLazyFlags flags = AsyncLazyFlags.None)
        {
            if (factory == null)
                throw new ArgumentNullException(nameof(factory));

            _retryOnFailure = (flags & AsyncLazyFlags.RetryOnFailure) == AsyncLazyFlags.RetryOnFailure;

            if ((flags & AsyncLazyFlags.ExecuteOnCallingThread) == AsyncLazyFlags.ExecuteOnCallingThread)
                _factory = factory;
            else
                _factory = () => LuminTask.Run(factory);
        }

        ~AsyncLazy() => Dispose(false);

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

                lock (_mutex)
                {
                    if (!_isStarted)
                    {
                        _isStarted = true;
                        _instance = CreateTask();
                    }
                    return _instance;
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private LuminTask<T> CreateTask()
        {
            return _retryOnFailure ? CreateRetryableTask() : _factory();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private LuminTask<T> CreateRetryableTask() => CreateRetryableTaskAsync();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private async LuminTask<T> CreateRetryableTaskAsync()
        {
            try
            {
                return await _factory();
            }
            catch
            {
                lock (_mutex)
                {
                    _isStarted = false;
                }
                throw;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public LuminTaskAwaiter<T> GetAwaiter() => Task.GetAwaiter();

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
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Func<LuminTask<T>> WrapFactory(Func<T> factory)
        {
            return () => LuminTask.FromResult(factory());
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
