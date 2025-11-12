using System;
using System.Runtime.CompilerServices;
using System.Threading;
using LuminThread.TaskSource;

namespace LuminThread.AsyncEx
{
    public sealed class AsyncConditionVariable
    {
        internal readonly AsyncLock _asyncLock;
        private readonly AsyncWaitQueue<AsyncLock.ReleaseScope> _waitQueue;

        public AsyncConditionVariable(AsyncLock asyncLock)
        {
            _asyncLock = asyncLock ?? throw new ArgumentNullException(nameof(asyncLock));
            _waitQueue = new AsyncWaitQueue<AsyncLock.ReleaseScope>();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public LuminTask<AsyncLock.ReleaseScope> WaitAsync(CancellationToken cancellationToken = default)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return LuminTask.FromCanceled<AsyncLock.ReleaseScope>(cancellationToken);
            }

            return _waitQueue.Enqueue(cancellationToken);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Notify()
        {
            if (!_waitQueue.IsEmpty)
            {
                _waitQueue.Dequeue(new AsyncLock.ReleaseScope(_asyncLock));
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void NotifyAll()
        {
            if (!_waitQueue.IsEmpty)
            {
                _waitQueue.DequeueAll(new AsyncLock.ReleaseScope(_asyncLock));
            }
        }
        
        private sealed class AsyncWaitQueue<T> : IAsyncWaitQueue<T>
        {
            private readonly LuminDeque<Waiter> _waiters = new();
            private readonly object _syncLock = new();

            public bool IsEmpty
            {
                get { lock (_syncLock) return _waiters.Count == 0; }
            }

            public LuminTask<T> Enqueue()
            {
                return Enqueue(CancellationToken.None);
            }

            public LuminTask<T> Enqueue(CancellationToken cancellationToken)
            {
                lock (_syncLock)
                {
                    var waiter = new Waiter(cancellationToken);
                    _waiters.PushBack(waiter);
                    return waiter.Task;
                }
            }

            public void Dequeue(T? result = default)
            {
                Waiter waiter;
                lock (_syncLock)
                {
                    if (_waiters.Count == 0) return;
                    waiter = _waiters.PopFront();
                }

                waiter.TrySetResult(result);
            }

            public void DequeueAll(T? result = default)
            {
                lock (_syncLock)
                {
                    foreach (var waiter in _waiters)
                    {
                        waiter.TrySetResult(result!);
                    }
                }
                
            }

            public bool TryCancel(LuminTask task, CancellationToken cancellationToken)
            {
                lock (_syncLock)
                {
                    for (int i = 0; i < _waiters.Count; i++)
                    {
                        if (_waiters[i].Task.Equals(task))
                        {
                            _waiters[i].TrySetCanceled(cancellationToken);
                            _waiters.RemoveAt(i);
                            return true;
                        }
                    }
                }
                return false;
            }

            public void CancelAll(CancellationToken cancellationToken)
            {
                lock (_syncLock)
                {
                    foreach (var waiter in _waiters)
                    {
                        waiter.TrySetCanceled(cancellationToken);
                    }
                }
            }

            private sealed unsafe class Waiter
            {
                private readonly LuminTaskSourceCore<T>* _core;
                private readonly CancellationTokenRegistration _cancellationRegistration;
                private volatile bool _isCompleted;

                public LuminTask<T> Task => new LuminTask<T>(
                    LuminTaskSourceCore<T>.MethodTable, 
                    _core, 
                    _core->Id);

                public Waiter(CancellationToken cancellationToken)
                {
                    _core = LuminTaskSourceCore<T>.Create();

                    if (cancellationToken.CanBeCanceled)
                    {
                        _cancellationRegistration = cancellationToken.Register(() =>
                        {
                            if (!_isCompleted)
                            {
                                TrySetCanceled(cancellationToken);
                            }
                        });
                    }
                }

                public bool TrySetResult(T result)
                {
                    if (_isCompleted) return false;
                    
                    if (LuminTaskSourceCore<T>.TrySetResult(_core, result))
                    {
                        Complete();
                        return true;
                    }
                    return false;
                }

                public bool TrySetCanceled(CancellationToken cancellationToken = default)
                {
                    if (_isCompleted) return false;

                    if (LuminTaskSourceCore<T>.TrySetCanceled(_core))
                    {
                        Complete();
                        return true;
                    }
                    return false;
                }

                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                private void Complete()
                {
                    _isCompleted = true;
                    _cancellationRegistration.Dispose();

                    if (_core != null)
                    {
                        LuminTaskSourceCore<T>.Dispose(_core);
                    }
                }
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