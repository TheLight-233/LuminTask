using System;
using System.Runtime.CompilerServices;
using System.Threading;
using LuminThread.TaskSource;

namespace LuminThread.AsyncEx
{
    public sealed class AsyncConditionVariable
    {
        internal readonly AsyncLock _asyncLock;
        private volatile WaiterNode _waiters;

        public AsyncConditionVariable(AsyncLock asyncLock)
        {
            _asyncLock = asyncLock ?? throw new ArgumentNullException(nameof(asyncLock));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public LuminTask<AsyncLock.ReleaseScope> WaitAsync(CancellationToken cancellationToken = default)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return new LuminTask<AsyncLock.ReleaseScope>(default);
            }

            var node = WaiterNode.Rent(this, cancellationToken);
            node.PushInto(ref _waiters);
            return node.Task;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Notify()
        {
            var node = WaiterNode.PopFrom(ref _waiters);
            if (node != null && node.IsValid)
            {
                node.TrySetResult();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void NotifyAll()
        {
            WaiterNode node;
            while ((node = WaiterNode.PopFrom(ref _waiters)) != null)
            {
                if (node.IsValid)
                {
                    node.TrySetResult();
                }
            }
        }

        private sealed unsafe class WaiterNode
        {
            private static readonly object PoolLock = new object();
            private static WaiterNode _pool;

            private LuminTaskSourceCore<AsyncLock.ReleaseScope>* _core;
            private CancellationTokenRegistration _ctr;
            private WaiterNode _next;
            private AsyncConditionVariable _parent;
            private volatile bool _isCompleted;
            private volatile bool _isValid = true;

            public bool IsValid => _isValid && _core != null && !_isCompleted;

            public LuminTask<AsyncLock.ReleaseScope> Task
            {
                get
                {
                    if (_core == null) return default;
                    
                    return new LuminTask<AsyncLock.ReleaseScope>(
                        LuminTaskSourceCore<AsyncLock.ReleaseScope>.MethodTable, 
                        _core, 
                        _core->Id);
                }
            }

            private WaiterNode() { }

            public static WaiterNode Rent(AsyncConditionVariable parent, CancellationToken token)
            {
                WaiterNode node;

                lock (PoolLock)
                {
                    node = _pool;
                    if (node != null)
                    {
                        _pool = node._next;
                        node._next = null;
                    }
                }

                if (node == null)
                {
                    node = new WaiterNode();
                }

                if (node._core == null)
                {
                    node._core = LuminTaskSourceCore<AsyncLock.ReleaseScope>.Create();
                }

                node._parent = parent;
                node._isCompleted = false;
                node._isValid = true;

                if (token.CanBeCanceled)
                {
                    node._ctr = token.Register(static n =>
                    {
                        var waiterNode = (WaiterNode)n;
                        if (waiterNode.IsValid)
                        {
                            waiterNode.TrySetCanceled();
                        }
                    }, node);
                }

                return node;
            }

            private void Return()
            {
                if (!_isValid) return;

                lock (PoolLock)
                {
                    _isValid = false;
                    _isCompleted = true;
                    _parent = null;
                    _ctr.Dispose();
                    _next = _pool;
                    _pool = this;
                    
                    if (_core != null)
                    {
                        LuminTaskSourceCore<AsyncLock.ReleaseScope>.Dispose(_core);
                    }
                }
            }

            public void PushInto(ref WaiterNode head)
            {
                if (!IsValid) return;

                WaiterNode current;
                do
                {
                    current = head;
                    _next = current;
                }
                while (Interlocked.CompareExchange(ref head, this, current) != current);
            }

            public static WaiterNode PopFrom(ref WaiterNode head)
            {
                WaiterNode current, next;
                do
                {
                    current = head;
                    if (current == null) return null;
                    next = current._next;
                }
                while (Interlocked.CompareExchange(ref head, next, current) != current);

                current._next = null;
                return current;
            }

            public bool TrySetResult()
            {
                if (!IsValid || _core == null) return false;

                if (LuminTaskSourceCore<AsyncLock.ReleaseScope>.TrySetResult(_core))
                {
                    CompleteWait();
                    
                    return true;
                }
                return false;
            }

            public bool TrySetCanceled()
            {
                if (!IsValid || _core == null) return false;

                if (LuminTaskSourceCore<AsyncLock.ReleaseScope>.TrySetCanceled(_core))
                {
                    CompleteWait();
                    return true;
                }
                return false;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private void CompleteWait()
            {
                _isCompleted = true;
                Return();
            }
        }
    }

    public static class AsyncConditionVariableExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static LuminTask<AsyncLock.ReleaseScope> WaitWithLockAsync(
            this AsyncConditionVariable conditionVariable,
            AsyncLock.ReleaseScope currentLock)
        {
            if (conditionVariable == null) throw new ArgumentNullException(nameof(conditionVariable));

            currentLock.Dispose();

            var waitTask = conditionVariable.WaitAsync();
            
            return WaitAndRetakeLock(waitTask, conditionVariable._asyncLock);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static async LuminTask<AsyncLock.ReleaseScope> WaitAndRetakeLock(
            LuminTask<AsyncLock.ReleaseScope> waitTask, AsyncLock asyncLock)
        {
            await waitTask;
            return await asyncLock.LockAsync();
        }
    }
}