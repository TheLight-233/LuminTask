using System.Collections.Concurrent;
using System.Threading.Tasks;
using LuminThread.TaskSource;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading;

namespace LuminThread.AsyncEx
{
    public sealed class AsyncLock
    {
        private volatile int _state;
        private volatile WaiterNode _waiters;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public LuminTask<ReleaseScope> LockAsync(CancellationToken token = default)
        {
            if (token.IsCancellationRequested)
            {
                return new LuminTask<ReleaseScope>(new ReleaseScope(null));
            }

            if (Interlocked.CompareExchange(ref _state, 1, 0) == 0)
            {
                return new LuminTask<ReleaseScope>(new ReleaseScope(this));
            }

            var node = WaiterNode.Rent(this, token);
            node.PushInto(ref _waiters);
            return node.Task;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Release()
        {
            _state = 0;

            var node = WaiterNode.PopFrom(ref _waiters);
            if (node != null && node.IsValid)
            {
                if (Interlocked.CompareExchange(ref _state, 1, 0) == 0)
                {
                    if (node.TrySetResult(new ReleaseScope(this)))
                    {
                        return;
                    }
                }
                
                if (node.IsValid)
                {
                    node.PushInto(ref _waiters);
                }
            }
        }

        public readonly struct ReleaseScope : IDisposable, IAsyncDisposable
        {
            private readonly AsyncLock _parent;
            
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public ReleaseScope(AsyncLock parent) => _parent = parent;
            
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Dispose()
            {
                _parent?.Release();
            }
            
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public ValueTask DisposeAsync()
            {
                _parent?.Release();
                return default;
            }

            public bool Equals(in ReleaseScope obj)
            {
                return ReferenceEquals(_parent, obj._parent);
            }
        }

        private sealed unsafe class WaiterNode
        {
            private static readonly ConcurrentStack<WaiterNode> Pool = new();
            private static readonly object PoolLock = new object();

            private LuminTaskSourceCore<ReleaseScope>* _core;
            private CancellationTokenRegistration _ctr;
            private WaiterNode _next;
            private AsyncLock _parent;
            private volatile bool _isCompleted;
            private volatile bool _isValid = true;

            public bool IsValid => _isValid && _core != null && !_isCompleted;
            
            public LuminTask<ReleaseScope> Task => 
                new LuminTask<ReleaseScope>(LuminTaskSourceCore<ReleaseScope>.MethodTable, _core, _core != null ? _core->Id : (short)0);

            private WaiterNode() { }

            public static WaiterNode Rent(AsyncLock parent, CancellationToken token)
            {
                WaiterNode node;
                
                lock (PoolLock)
                {
                    if (!Pool.TryPop(out node))
                    {
                        node = new WaiterNode();
                    }
                }

                if (node._core == null)
                {
                    node._core = LuminTaskSourceCore<ReleaseScope>.Create();
                }

                node._parent = parent;
                node._isCompleted = false;
                node._isValid = true;
                node._next = null;

                if (token.CanBeCanceled)
                {
                    node._ctr = token.Register(static n =>
                    {
                        var node = (WaiterNode)n;
                        if (node.IsValid)
                        {
                            node.TrySetCanceled();
                        }
                    }, node);
                }

                return node;
            }

            private void Return()
            {
                if (!_isValid) return;
                
                _isValid = false;
                _isCompleted = true;
                _parent = null;
                _next = null;
                _ctr.Dispose();

                if (_core != null)
                {
                    lock (PoolLock)
                    {
                        Pool.Push(this);
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

            public bool TrySetResult(ReleaseScope scope)
            {
                if (!IsValid || _core == null) return false;
                
                if (LuminTaskSourceCore<ReleaseScope>.TrySetResult(_core, scope))
                {
                    _isCompleted = true;
                    Return();
                    return true;
                }
                return false;
            }

            public bool TrySetCanceled()
            {
                if (!IsValid || _core == null) return false;
                
                if (LuminTaskSourceCore<ReleaseScope>.TrySetCanceled(_core))
                {
                    _isCompleted = true;
                    Return();
                    return true;
                }
                return false;
            }
        }
    }
}