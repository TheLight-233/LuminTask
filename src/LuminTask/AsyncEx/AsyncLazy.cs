using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

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

        // The factory runs at most once; its outcome is parked in a System Task<T>.
        // Unlike LuminTask (which, like ValueTask, may only be awaited once), a Task<T>
        // is safe to await any number of times, so every caller can observe the result
        // without re-awaiting a single LuminTask source (which would double-free it).
        private Task<T>? _resolution;
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

                Task<T> resolution;
                lock (_mutex)
                {
                    if (!_isStarted)
                    {
                        _isStarted = true;
                        _resolution = ResolveAsync();
                    }
                    resolution = _resolution!;
                }

                // Fresh single-await LuminTask per access, wrapping the shared resolution.
                return AwaitResolution(resolution);
            }
        }

        // Invokes the factory exactly once and stores its outcome in the returned Task<T>.
        private async Task<T> ResolveAsync()
        {
            try
            {
                return await _factory();
            }
            catch when (_retryOnFailure)
            {
                lock (_mutex)
                {
                    _isStarted = false;
                    _resolution = null;
                }
                throw;
            }
        }

        private static async LuminTask<T> AwaitResolution(Task<T> resolution)
            => await resolution;

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
