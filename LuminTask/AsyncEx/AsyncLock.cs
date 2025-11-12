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
        private readonly IAsyncWaitQueue<ReleaseScope> _waitQueue;
        private volatile int _state; // 0 = 空闲, 1 = 已占用

        public AsyncLock() : this(new DefaultAsyncWaitQueue<ReleaseScope>()) { }

        public AsyncLock(IAsyncWaitQueue<ReleaseScope> waitQueue)
        {
            _waitQueue = waitQueue ?? throw new ArgumentNullException(nameof(waitQueue));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe LuminTask<ReleaseScope> LockAsync(CancellationToken token = default)
        {
            if (token.IsCancellationRequested)
            {
                var completedCore = LuminTaskSourceCore<ReleaseScope>.Create();
                LuminTaskSourceCore<ReleaseScope>.TrySetCanceled(completedCore);
                return new LuminTask<ReleaseScope>(LuminTaskSourceCore<ReleaseScope>.MethodTable, completedCore, completedCore->Id);
            }
            
            if (Interlocked.CompareExchange(ref _state, 1, 0) == 0)
            {
                return LuminTask.FromResult(new ReleaseScope(this));
            }
            
            var task = _waitQueue.Enqueue();
            
            if (Interlocked.CompareExchange(ref _state, 1, 0) == 0)
            {
                if (_waitQueue.TryCancel(task, token))
                {
                    return LuminTask.FromResult(new ReleaseScope(this));
                }
            }

            return task;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Release()
        {
            Interlocked.Exchange(ref _state, 0);
            
            if (!_waitQueue.IsEmpty)
            {
                if (Interlocked.CompareExchange(ref _state, 1, 0) == 0)
                {
                    _waitQueue.Dequeue(new ReleaseScope(this));
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

            public override bool Equals(object obj)
            {
                return obj is ReleaseScope other && Equals(other);
            }

            public bool Equals(ReleaseScope other)
            {
                return ReferenceEquals(_parent, other._parent);
            }

            public override int GetHashCode()
            {
                return _parent?.GetHashCode() ?? 0;
            }
        }
    }
}