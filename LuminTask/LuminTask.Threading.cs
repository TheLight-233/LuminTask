using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using LuminThread.Utility;

namespace LuminThread;

public readonly partial struct LuminTask
{
    
    /// <summary>
    /// If running on mainthread, do nothing. Otherwise, same as LuminTask.Yield(PlayerLoopTiming.DotNet).
    /// </summary>
    [DebuggerHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static SwitchToMainThreadAwaitable SwitchToMainThread(CancellationToken cancellationToken = default)
    {
#if NET8_0_OR_GREATER
        throw new ArgumentException("DotNet don't have main thread", nameof(PlayerLoopTiming.DotNet));
#endif
        return new SwitchToMainThreadAwaitable(PlayerLoopTiming.Update, cancellationToken);
    }

    /// <summary>
    /// If running on mainthread, do nothing. Otherwise, same as LuminTask.Yield(timing).
    /// </summary>
    [DebuggerHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static SwitchToMainThreadAwaitable SwitchToMainThread(PlayerLoopTiming timing, CancellationToken cancellationToken = default)
    {
        if (timing is PlayerLoopTiming.DotNet)
            throw new ArgumentException("DotNet don't have main thread", nameof(timing));
        return new SwitchToMainThreadAwaitable(timing, cancellationToken);
    }

    /// <summary>
    /// Return to mainthread(same as await SwitchToMainThread) after using scope is closed.
    /// </summary>
    [DebuggerHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ReturnToMainThread ReturnToMainThread(CancellationToken cancellationToken = default)
    {
        return new ReturnToMainThread(PlayerLoopTiming.DotNet, cancellationToken);
    }

    /// <summary>
    /// Return to mainthread(same as await SwitchToMainThread) after using scope is closed.
    /// </summary>
    [DebuggerHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ReturnToMainThread ReturnToMainThread(PlayerLoopTiming timing, CancellationToken cancellationToken = default)
    {
        return new ReturnToMainThread(timing, cancellationToken);
    }

    /// <summary>
    /// Queue the action to PlayerLoop.
    /// </summary>
    [DebuggerHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Post(Action action, PlayerLoopTiming timing = PlayerLoopTiming.DotNet)
    {
        PlayerLoopHelper.AddContinuation(timing, action);
    }

    [DebuggerHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static SwitchToThreadPoolAwaitable SwitchToThreadPool()
    {
        return new SwitchToThreadPoolAwaitable();
    }

    /// <summary>
    /// Note: use SwitchToThreadPool is recommended.
    /// </summary>
    [DebuggerHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static SwitchToTaskPoolAwaitable SwitchToTaskPool()
    {
        return new SwitchToTaskPoolAwaitable();
    }

    [DebuggerHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static SwitchToSynchronizationContextAwaitable SwitchToSynchronizationContext(SynchronizationContext synchronizationContext, CancellationToken cancellationToken = default)
    {
    
        LuminTaskExceptionHelper.ThrowArgumentNullException(synchronizationContext, nameof(synchronizationContext));
        return new SwitchToSynchronizationContextAwaitable(synchronizationContext, cancellationToken);
    }

    [DebuggerHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ReturnToSynchronizationContext ReturnToSynchronizationContext(SynchronizationContext synchronizationContext, CancellationToken cancellationToken = default)
    {
        return new ReturnToSynchronizationContext(synchronizationContext, false, cancellationToken);
    }

    [DebuggerHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ReturnToSynchronizationContext ReturnToCurrentSynchronizationContext(bool dontPostWhenSameContext = true, CancellationToken cancellationToken = default)
    {
        return new ReturnToSynchronizationContext(SynchronizationContext.Current, dontPostWhenSameContext, cancellationToken);
    }
}


public readonly struct SwitchToMainThreadAwaitable
{
    readonly PlayerLoopTiming playerLoopTiming;
    readonly CancellationToken cancellationToken;

    public SwitchToMainThreadAwaitable(PlayerLoopTiming playerLoopTiming, CancellationToken cancellationToken)
    {
        this.playerLoopTiming = playerLoopTiming;
        this.cancellationToken = cancellationToken;
    }

    public Awaiter GetAwaiter() => new Awaiter(playerLoopTiming, cancellationToken);

    public readonly struct Awaiter : ICriticalNotifyCompletion
    {
        readonly PlayerLoopTiming playerLoopTiming;
        readonly CancellationToken cancellationToken;

        public Awaiter(PlayerLoopTiming playerLoopTiming, CancellationToken cancellationToken)
        {
            this.playerLoopTiming = playerLoopTiming;
            this.cancellationToken = cancellationToken;
        }

        public bool IsCompleted
        {
            get
            {
                var currentThreadId = System.Threading.Thread.CurrentThread.ManagedThreadId;
                if (PlayerLoopHelper.MainThreadId == currentThreadId)
                {
                    return true; // run immediate.
                }
                else
                {
                    return false; // register continuation.
                }
            }
        }

        public void GetResult() { cancellationToken.ThrowIfCancellationRequested(); }

        public void OnCompleted(Action continuation)
        {
            PlayerLoopHelper.AddContinuation(playerLoopTiming, continuation);
        }

        public void UnsafeOnCompleted(Action continuation)
        {
            PlayerLoopHelper.AddContinuation(playerLoopTiming, continuation);
        }
    }
}

public readonly struct ReturnToMainThread
{
    readonly PlayerLoopTiming playerLoopTiming;
    readonly CancellationToken cancellationToken;

    public ReturnToMainThread(PlayerLoopTiming playerLoopTiming, CancellationToken cancellationToken)
    {
        this.playerLoopTiming = playerLoopTiming;
        this.cancellationToken = cancellationToken;
    }

    public Awaiter DisposeAsync()
    {
        return new Awaiter(playerLoopTiming, cancellationToken); // run immediate.
    }

    public readonly struct Awaiter : ICriticalNotifyCompletion
    {
        readonly PlayerLoopTiming timing;
        readonly CancellationToken cancellationToken;

        public Awaiter(PlayerLoopTiming timing, CancellationToken cancellationToken)
        {
            this.timing = timing;
            this.cancellationToken = cancellationToken;
        }

        public Awaiter GetAwaiter() => this;

        public bool IsCompleted => PlayerLoopHelper.MainThreadId == System.Threading.Thread.CurrentThread.ManagedThreadId;

        public void GetResult() { cancellationToken.ThrowIfCancellationRequested(); }

        public void OnCompleted(Action continuation)
        {
            PlayerLoopHelper.AddContinuation(timing, continuation);
        }

        public void UnsafeOnCompleted(Action continuation)
        {
            PlayerLoopHelper.AddContinuation(timing, continuation);
        }
    }
}

public readonly struct SwitchToThreadPoolAwaitable
{
    public Awaiter GetAwaiter() => new Awaiter();

    public readonly struct Awaiter : ICriticalNotifyCompletion
    {
        static readonly WaitCallback switchToCallback = Callback;

        public bool IsCompleted => false;
        public void GetResult() { }

        public void OnCompleted(Action continuation)
        {
            ThreadPool.QueueUserWorkItem(switchToCallback, continuation);
        }

        public void UnsafeOnCompleted(Action continuation)
        {
#if NETCOREAPP3_1
                ThreadPool.UnsafeQueueUserWorkItem(ThreadPoolWorkItem.Create(continuation), false);
#else
            ThreadPool.UnsafeQueueUserWorkItem(switchToCallback, continuation);
#endif
        }

        static void Callback(object state)
        {
            var continuation = (Action)state;
            continuation();
        }
    }

#if NETCOREAPP3_1

        sealed class ThreadPoolWorkItem : IThreadPoolWorkItem, ITaskPoolNode<ThreadPoolWorkItem>
        {
            static TaskPool<ThreadPoolWorkItem> pool;
            ThreadPoolWorkItem nextNode;
            public ref ThreadPoolWorkItem NextNode => ref nextNode;

            static ThreadPoolWorkItem()
            {
                TaskPool.RegisterSizeGetter(typeof(ThreadPoolWorkItem), () => pool.Size);
            }

            Action continuation;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static ThreadPoolWorkItem Create(Action continuation)
            {
                if (!pool.TryPop(out var item))
                {
                    item = new ThreadPoolWorkItem();
                }

                item.continuation = continuation;
                return item;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Execute()
            {
                var call = continuation;
                continuation = null;
                if (call != null)
                {
                    pool.TryPush(this);
                    call.Invoke();
                }
            }
        }

#endif
}

public readonly struct SwitchToTaskPoolAwaitable
{
    public Awaiter GetAwaiter() => new Awaiter();

    public struct Awaiter : ICriticalNotifyCompletion
    {
        static readonly Action<object> switchToCallback = Callback;

        public bool IsCompleted => false;
        public void GetResult() { }

        public void OnCompleted(Action continuation)
        {
            Task.Factory.StartNew(switchToCallback, continuation, CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
        }

        public void UnsafeOnCompleted(Action continuation)
        {
            Task.Factory.StartNew(switchToCallback, continuation, CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
        }

        static void Callback(object state)
        {
            var continuation = (Action)state;
            continuation();
        }
    }
}

public readonly struct SwitchToSynchronizationContextAwaitable
{
    readonly SynchronizationContext synchronizationContext;
    readonly CancellationToken cancellationToken;

    public SwitchToSynchronizationContextAwaitable(SynchronizationContext synchronizationContext, CancellationToken cancellationToken)
    {
        this.synchronizationContext = synchronizationContext;
        this.cancellationToken = cancellationToken;
    }

    public Awaiter GetAwaiter() => new Awaiter(synchronizationContext, cancellationToken);

    public struct Awaiter : ICriticalNotifyCompletion
    {
        static readonly SendOrPostCallback switchToCallback = Callback;
        readonly SynchronizationContext synchronizationContext;
        readonly CancellationToken cancellationToken;

        public Awaiter(SynchronizationContext synchronizationContext, CancellationToken cancellationToken)
        {
            this.synchronizationContext = synchronizationContext;
            this.cancellationToken = cancellationToken;
        }

        public bool IsCompleted => false;
        public void GetResult() { cancellationToken.ThrowIfCancellationRequested(); }

        public void OnCompleted(Action continuation)
        {
            synchronizationContext.Post(switchToCallback, continuation);
        }

        public void UnsafeOnCompleted(Action continuation)
        {
            synchronizationContext.Post(switchToCallback, continuation);
        }

        static void Callback(object state)
        {
            var continuation = (Action)state;
            continuation();
        }
    }
}

public readonly struct ReturnToSynchronizationContext
{
    readonly SynchronizationContext syncContext;
    readonly bool dontPostWhenSameContext;
    readonly CancellationToken cancellationToken;

    public ReturnToSynchronizationContext(SynchronizationContext syncContext, bool dontPostWhenSameContext, CancellationToken cancellationToken)
    {
        this.syncContext = syncContext;
        this.dontPostWhenSameContext = dontPostWhenSameContext;
        this.cancellationToken = cancellationToken;
    }

    public Awaiter DisposeAsync()
    {
        return new Awaiter(syncContext, dontPostWhenSameContext, cancellationToken);
    }

    public struct Awaiter : ICriticalNotifyCompletion
    {
        static readonly SendOrPostCallback switchToCallback = Callback;

        readonly SynchronizationContext synchronizationContext;
        readonly bool dontPostWhenSameContext;
        readonly CancellationToken cancellationToken;

        public Awaiter(SynchronizationContext synchronizationContext, bool dontPostWhenSameContext, CancellationToken cancellationToken)
        {
            this.synchronizationContext = synchronizationContext;
            this.dontPostWhenSameContext = dontPostWhenSameContext;
            this.cancellationToken = cancellationToken;
        }

        public Awaiter GetAwaiter() => this;

        public bool IsCompleted
        {
            get
            {
                if (!dontPostWhenSameContext) return false;

                var current = SynchronizationContext.Current;
                if (current == synchronizationContext)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        public void GetResult() { cancellationToken.ThrowIfCancellationRequested(); }

        public void OnCompleted(Action continuation)
        {
            synchronizationContext.Post(switchToCallback, continuation);
        }

        public void UnsafeOnCompleted(Action continuation)
        {
            synchronizationContext.Post(switchToCallback, continuation);
        }

        static void Callback(object state)
        {
            var continuation = (Action)state;
            continuation();
        }
    }
}