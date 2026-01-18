using System;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

namespace LuminThread.Unity
{
    /// <summary>
    /// Pooled delegate for async operations to reduce GC allocations
    /// </summary>
    /// <typeparam name="T">The parameter type for the delegate</typeparam>
    public sealed class PooledDelegate<T>
    {
        private static readonly ConcurrentQueue<PooledDelegate<T>> pool = new ConcurrentQueue<PooledDelegate<T>>();

        private readonly Action<T> runDelegate;
        private Action continuation;

        private PooledDelegate()
        {
            runDelegate = Run;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Action<T> Create(Action continuation)
        {
            if (!pool.TryDequeue(out var item))
            {
                item = new PooledDelegate<T>();
            }

            item.continuation = continuation;
            return item.runDelegate;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Run(T _)
        {
            var call = continuation;
            continuation = null;
            if (call != null)
            {
                pool.Enqueue(this);
                call.Invoke();
            }
        }
    }
}