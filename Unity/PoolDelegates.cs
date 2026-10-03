using System;
using System.Runtime.CompilerServices;

namespace LuminThread.Unity
{
    /// <summary>
    /// Pooled delegate for async operations to reduce GC allocations: the instance itself is the
    /// delegate target, and it is recycled per thread after firing, so a steady-state call costs
    /// nothing.
    /// </summary>
    /// <typeparam name="T">The parameter type for the delegate</typeparam>
    public sealed class PooledDelegate<T>
    {
        // Bounded like every other pool in the library: past this the instance is simply dropped,
        // which costs one GC once instead of letting the free list track the peak forever.
        private const int MaxRetainedPerThread = 16;

        [ThreadStatic] private static PooledDelegate<T> _freeList;
        [ThreadStatic] private static int _freeCount;

        private PooledDelegate<T> _next;
        private Action _continuation;
        private readonly Action<T> _runDelegate;

        private PooledDelegate()
        {
            _runDelegate = Run;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Action<T> Create(Action continuation)
        {
            var item = _freeList;
            if (item != null)
            {
                _freeList = item._next;
                item._next = null;
                _freeCount--;
            }
            else
            {
                item = new PooledDelegate<T>();
            }

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
            if (_freeCount < MaxRetainedPerThread)
            {
                _next = _freeList;
                _freeList = this;
                _freeCount++;
            }
            else
            {
                _next = null;
            }

            call?.Invoke();
        }
    }
}