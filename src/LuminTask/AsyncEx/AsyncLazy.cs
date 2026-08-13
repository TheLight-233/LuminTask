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
        public LuminTaskAwaiter<T> GetAwaiter() => Task.GetAwaiter();

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

        // 工厂至多执行一次；结果停在 Task<T> 上供任意次数 await（LuminTask 只能消费一次）。
        private Task<T>? _resolution;
        private bool _isStarted;
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

                return AwaitResolution(resolution);
            }
        }

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
