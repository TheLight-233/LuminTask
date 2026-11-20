using LuminThread.Utility;
using System;
using System.Runtime.CompilerServices;

namespace LuminThread.Unity
{
    /// <summary>
    /// Pooled delegate for async operations to reduce GC allocations: the instance itself is the
    /// delegate target, and it is recycled through the shared cache after firing, so a steady-state call costs
    /// nothing.
    /// </summary>
    /// <typeparam name="T">The parameter type for the delegate</typeparam>
    public sealed class PooledDelegate<T> : ILuminCacheNode
    {
        public ILuminCacheNode CacheNext { get; set; }
        private Action _continuation;
        private readonly Action<T> _runDelegate;

        private PooledDelegate()
        {
            _runDelegate = Run;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Action<T> Create(Action continuation)
        {
            var item = (PooledDelegate<T>)LuminTaskIdleCache.Take(typeof(PooledDelegate<T>)) ?? new PooledDelegate<T>();
            item._continuation = continuation;
            return item._runDelegate;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Run(T _)
        {
            var call = _continuation;
            _continuation = null;

            // Return before invoking: the continuation may create another pooled delegate of this
            // shape, and it must find this slot available rather than allocating a second one.
            LuminTaskIdleCache.Return(this, 144);
            call?.Invoke();
        }
    }
}