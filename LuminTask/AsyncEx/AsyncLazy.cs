using System;
using System.Runtime.CompilerServices;
using System.Threading;

namespace LuminThread.AsyncEx
{
    /// <summary>
    /// AsyncLazy 行为标志
    /// </summary>
    [Flags]
    public enum AsyncLazyFlags
    {
        /// <summary>
        /// 无特殊标志。工厂方法在线程池线程上执行，失败时不重试（缓存失败）
        /// </summary>
        None = 0x0,

        /// <summary>
        /// 在调用线程上执行工厂方法
        /// </summary>
        ExecuteOnCallingThread = 0x1,

        /// <summary>
        /// 如果工厂方法失败，下次重新运行工厂方法而不是缓存失败的任务
        /// </summary>
        RetryOnFailure = 0x2,
    }

    /// <summary>
    /// 高性能异步延迟初始化
    /// </summary>
    public sealed class AsyncLazy<T>
    {
        private readonly object _mutex = new object();
        private readonly Func<LuminTask<T>> _factory;
        private readonly bool _retryOnFailure;
        private LuminTask<T> _instance;
        private bool _isStarted;

        public AsyncLazy(Func<T> factory, AsyncLazyFlags flags = AsyncLazyFlags.None)
            : this(WrapFactory(factory), flags)
        {
        }

        public AsyncLazy(Func<LuminTask<T>> factory, AsyncLazyFlags flags = AsyncLazyFlags.None)
        {
            if (factory == null)
                throw new ArgumentNullException(nameof(factory));

            _retryOnFailure = (flags & AsyncLazyFlags.RetryOnFailure) == AsyncLazyFlags.RetryOnFailure;

            // 根据标志包装工厂方法
            if ((flags & AsyncLazyFlags.ExecuteOnCallingThread) == AsyncLazyFlags.ExecuteOnCallingThread)
            {
                _factory = factory;
            }
            else
            {
                _factory = () => LuminTask.Run(factory);
            }
        }

        public bool IsStarted
        {
            get
            {
                lock (_mutex)
                {
                    return _isStarted;
                }
            }
        }

        public LuminTask<T> Task
        {
            get
            {
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
            if (_retryOnFailure)
            {
                return CreateRetryableTask();
            }
            else
            {
                return _factory();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private LuminTask<T> CreateRetryableTask()
        {
            return CreateRetryableTaskAsync();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private async LuminTask<T> CreateRetryableTaskAsync()
        {
            try
            {
                return await _factory();
            }
            catch
            {
                // 失败时重置状态，允许重试
                lock (_mutex)
                {
                    _isStarted = false;
                }
                throw;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public LuminTaskAwaiter<T> GetAwaiter()
        {
            return Task.GetAwaiter();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Func<LuminTask<T>> WrapFactory(Func<T> factory)
        {
            return () => LuminTask.FromResult(factory());
        }
    }

    /// <summary>
    /// AsyncLazy 扩展方法
    /// </summary>
    public static class AsyncLazyExtensions
    {
        /// <summary>
        /// 创建从同步工厂的 AsyncLazy
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static AsyncLazy<T> Create<T>(Func<T> factory, AsyncLazyFlags flags = AsyncLazyFlags.None)
        {
            return new AsyncLazy<T>(factory, flags);
        }

        /// <summary>
        /// 创建从异步工厂的 AsyncLazy
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static AsyncLazy<T> Create<T>(Func<LuminTask<T>> factory, AsyncLazyFlags flags = AsyncLazyFlags.None)
        {
            return new AsyncLazy<T>(factory, flags);
        }
    }
}
