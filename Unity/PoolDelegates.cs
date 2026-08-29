using System;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

namespace LuminThread.Unity
{
    /// <summary>
    /// Shared pooling core for pooled delegates to reduce GC allocations.
    /// Holds the continuation and the pool; both the <see cref="PooledDelegate{T}"/> (
    /// <c>Action&lt;T&gt;</c>) and the <c>Action</c> / <c>TweenCallback</c> variants reuse it.
    /// </summary>
    internal static class PooledDelegateCore
    {
        private static readonly ConcurrentQueue<PooledDelegateBody> pool = new ConcurrentQueue<PooledDelegateBody>();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static PooledDelegateBody Rent(Action continuation)
        {
            if (!pool.TryDequeue(out var body))
                body = new PooledDelegateBody();

            body.Continuation = continuation;
            return body;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Release(PooledDelegateBody body)
        {
            pool.Enqueue(body);
        }
    }

    /// <summary>
    /// Pool entry storing the wrapped continuation.
    /// </summary>
    internal sealed class PooledDelegateBody
    {
        public Action Continuation;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Invoke()
        {
            var call = Continuation;
            Continuation = null;
            if (call != null)
            {
                PooledDelegateCore.Release(this);
                call.Invoke();
            }
        }
    }

    /// <summary>
    /// Pooled delegate for async operations to reduce GC allocations.
    /// </summary>
    /// <typeparam name="T">The parameter type for the delegate</typeparam>
    public sealed class PooledDelegate<T>
    {
        private readonly PooledDelegateBody body;
        private readonly Action<T> runDelegate;

        private PooledDelegate(PooledDelegateBody body)
        {
            this.body = body;
            runDelegate = Run;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Action<T> Create(Action continuation)
        {
            var body = PooledDelegateCore.Rent(continuation);
            var item = new PooledDelegate<T>(body);
            return item.runDelegate;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Run(T _)
        {
            body.Invoke();
        }
    }
}