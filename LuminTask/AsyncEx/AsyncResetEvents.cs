using System;
using System.Runtime.CompilerServices;
using System.Threading;

namespace LuminThread.AsyncEx
{
    /// <summary>
    /// 高性能异步手动重置事件
    /// </summary>
    public sealed class AsyncManualResetEvent
    {
        private readonly object _mutex = new object();
        private readonly IAsyncWaitQueue<object> _queue;
        private volatile bool _isSet;

        public AsyncManualResetEvent(bool initialState = false)
        {
            _queue = new DefaultAsyncWaitQueue<object>();
            _isSet = initialState;
        }

        public bool IsSet => _isSet;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public LuminTask WaitAsync(CancellationToken cancellationToken = default)
        {
            // 快速路径：已设置
            if (_isSet)
            {
                return LuminTask.CompletedTask();
            }

            if (cancellationToken.IsCancellationRequested)
            {
                return LuminTask.FromCanceled(cancellationToken);
            }

            lock (_mutex)
            {
                // 双重检查
                if (_isSet)
                {
                    return LuminTask.CompletedTask();
                }

                // 使用扩展方法，自动处理取消
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
                    // 唤醒所有等待者
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
    }

    /// <summary>
    /// 高性能异步手动重置事件（带返回值）
    /// </summary>
    public sealed class AsyncManualResetEvent<T>
    {
        private readonly object _mutex = new object();
        private readonly IAsyncWaitQueue<T> _queue;
        private volatile bool _isSet;
        private T _result;

        public AsyncManualResetEvent()
        {
            _queue = new DefaultAsyncWaitQueue<T>();
        }

        public bool IsSet => _isSet;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public LuminTask<T> WaitAsync(CancellationToken cancellationToken = default)
        {
            // 快速路径：已设置
            if (_isSet)
            {
                return LuminTask.FromResult(_result);
            }

            if (cancellationToken.IsCancellationRequested)
            {
                return LuminTask.FromCanceled<T>(cancellationToken);
            }

            lock (_mutex)
            {
                // 双重检查
                if (_isSet)
                {
                    return LuminTask.FromResult(_result);
                }

                // 使用扩展方法，自动处理取消
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
                    // 唤醒所有等待者
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
    }

    /// <summary>
    /// 高性能异步自动重置事件
    /// </summary>
    public sealed class AsyncAutoResetEvent
    {
        private readonly object _mutex = new object();
        private readonly IAsyncWaitQueue<object> _queue;
        private volatile bool _isSet;

        public AsyncAutoResetEvent(bool initialState = false)
        {
            _queue = new DefaultAsyncWaitQueue<object>();
            _isSet = initialState;
        }

        public bool IsSet => _isSet;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public LuminTask WaitAsync(CancellationToken cancellationToken = default)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return LuminTask.FromCanceled(cancellationToken);
            }

            lock (_mutex)
            {
                // 快速路径：已设置且无等待者
                if (_isSet && _queue.IsEmpty)
                {
                    _isSet = false;
                    return LuminTask.CompletedTask();
                }

                // 使用扩展方法，自动处理取消
                return _queue.Enqueue(cancellationToken);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Set()
        {
            lock (_mutex)
            {
                if (_queue.IsEmpty)
                {
                    // 无等待者，设置状态
                    _isSet = true;
                }
                else
                {
                    // 有等待者，唤醒一个
                    _queue.Dequeue(null);
                }
            }
        }
    }

    /// <summary>
    /// 高性能异步自动重置事件（带返回值）
    /// </summary>
    public sealed class AsyncAutoResetEvent<T>
    {
        private readonly object _mutex = new object();
        private readonly IAsyncWaitQueue<T> _queue;
        private volatile bool _isSet;
        private T _result;

        public AsyncAutoResetEvent()
        {
            _queue = new DefaultAsyncWaitQueue<T>();
        }

        public bool IsSet => _isSet;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public LuminTask<T> WaitAsync(CancellationToken cancellationToken = default)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return LuminTask.FromCanceled<T>(cancellationToken);
            }

            lock (_mutex)
            {
                // 快速路径：已设置且无等待者
                if (_isSet && _queue.IsEmpty)
                {
                    var result = _result;
                    _isSet = false;
                    _result = default;
                    return LuminTask.FromResult(result);
                }

                // 使用扩展方法，自动处理取消
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
                    // 无等待者，设置状态
                    _isSet = true;
                    _result = result;
                }
                else
                {
                    // 有等待者，唤醒一个
                    _queue.Dequeue(result);
                }
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
