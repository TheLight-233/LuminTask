using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using LuminThread.Utility;

namespace LuminThread;

public readonly partial struct LuminTask
{
    /// <summary>Registers the synchronization context that represents the application's main thread.</summary>
    public static void RegisterMainThread(SynchronizationContext synchronizationContext)
        => PlayerLoopHelper.RegisterMainThread(synchronizationContext);

    /// <summary>Clears the registered main-thread synchronization context.</summary>
    public static void UnregisterMainThread()
        => PlayerLoopHelper.UnregisterMainThread();

    [DebuggerHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static SwitchToMainThreadAwaitable SwitchToMainThread(CancellationToken cancellationToken = default)
    {

        var context = PlayerLoopHelper.MainThreadSynchronizationContext;
        if (context != null)
            return new SwitchToMainThreadAwaitable(context, cancellationToken);
#if NET8_0_OR_GREATER
        throw new ArgumentException("DotNet has no registered main thread. Call PlayerLoopHelper.RegisterMainThread first.", nameof(PlayerLoopTiming.DotNet));
#else
        return new SwitchToMainThreadAwaitable(PlayerLoopTiming.Update, cancellationToken);
#endif
    }

    [DebuggerHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static SwitchToMainThreadAwaitable SwitchToMainThread(PlayerLoopTiming timing, CancellationToken cancellationToken = default)
    {
        if (timing is PlayerLoopTiming.DotNet)
            throw new ArgumentException("DotNet don't have main thread", nameof(timing));
        return new SwitchToMainThreadAwaitable(timing, cancellationToken);
    }

    [DebuggerHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ReturnToMainThread ReturnToMainThread(CancellationToken cancellationToken = default)
    {
        return new ReturnToMainThread(PlayerLoopTiming.DotNet, cancellationToken);
    }

    [DebuggerHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ReturnToMainThread ReturnToMainThread(PlayerLoopTiming timing, CancellationToken cancellationToken = default)
    {
        return new ReturnToMainThread(timing, cancellationToken);
    }

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
    readonly SynchronizationContext? synchronizationContext;

    public SwitchToMainThreadAwaitable(PlayerLoopTiming playerLoopTiming, CancellationToken cancellationToken)
    {
        this.playerLoopTiming = playerLoopTiming;
        this.cancellationToken = cancellationToken;
        synchronizationContext = null;
    }

    internal SwitchToMainThreadAwaitable(SynchronizationContext synchronizationContext, CancellationToken cancellationToken)
    {
        playerLoopTiming = PlayerLoopTiming.DotNet;
        this.cancellationToken = cancellationToken;
        this.synchronizationContext = synchronizationContext;
    }

    public Awaiter GetAwaiter() => new Awaiter(playerLoopTiming, synchronizationContext, cancellationToken);

    public readonly struct Awaiter : ICriticalNotifyCompletion
    {
        readonly PlayerLoopTiming playerLoopTiming;
        readonly SynchronizationContext? synchronizationContext;
        readonly CancellationToken cancellationToken;

        public Awaiter(PlayerLoopTiming playerLoopTiming, CancellationToken cancellationToken)
            : this(playerLoopTiming, null, cancellationToken) { }

        internal Awaiter(PlayerLoopTiming playerLoopTiming, SynchronizationContext? synchronizationContext, CancellationToken cancellationToken)
        {
            this.playerLoopTiming = playerLoopTiming;
            this.synchronizationContext = synchronizationContext;
            this.cancellationToken = cancellationToken;
        }

        public bool IsCompleted
        {
            get
            {
                var currentThreadId = System.Threading.Thread.CurrentThread.ManagedThreadId;
                if (PlayerLoopHelper.MainThreadId == currentThreadId ||
                    (synchronizationContext != null && SynchronizationContext.Current == synchronizationContext))
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
            if (synchronizationContext != null)
                synchronizationContext.Post(static state => ((Action)state!).Invoke(), continuation);
            else
                PlayerLoopHelper.AddContinuation(playerLoopTiming, continuation);
        }

        public void UnsafeOnCompleted(Action continuation)
        {
            OnCompleted(continuation);
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
        return new Awaiter(playerLoopTiming, cancellationToken);
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
#if NET5_0_OR_GREATER
            ThreadPool.UnsafeQueueUserWorkItem(ThreadPoolWorkItem.Create(continuation), preferLocal: false);
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

#if NET5_0_OR_GREATER

    // IThreadPoolWorkItem lets the runtime enqueue this instance directly. The WaitCallback overload
    // allocates an internal work item on every call, while this path allocates nothing once the pool
    // has warmed up - which is what makes a steady-state SwitchToThreadPool GC-free.
    //
    // Only .NET 5+ exposes the interface; on netstandard2.1 (Unity) the WaitCallback path above is
    // kept as-is.
    sealed class ThreadPoolWorkItem : IThreadPoolWorkItem
    {
        // Bounded: one lock-free fast slot plus a small locked overflow list.
        const int MaxRetained = 32;

        static ThreadPoolWorkItem? fastItem;
        static ThreadPoolWorkItem? overflow;
        static int overflowCount;
        static SpinLock overflowLock = new(enableThreadOwnerTracking: false);

        ThreadPoolWorkItem? next;
        Action? continuation;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static ThreadPoolWorkItem Create(Action continuation)
        {
            var item = Interlocked.Exchange(ref fastItem, null);
            if (item == null)
            {
                var lockTaken = false;
                try
                {
                    overflowLock.Enter(ref lockTaken);
                    item = overflow;
                    if (item != null)
                    {
                        overflow = item.next;
                        overflowCount--;
                        item.next = null;
                    }
                }
                finally
                {
                    if (lockTaken)
                        overflowLock.Exit(useMemoryBarrier: false);
                }
            }

            item ??= new ThreadPoolWorkItem();
            item.continuation = continuation;
            return item;
        }

        void IThreadPoolWorkItem.Execute()
        {
            var call = continuation;
            continuation = null;

            // Return before invoking: the continuation may queue more work of this shape.
            if (Interlocked.CompareExchange(ref fastItem, this, null) != null)
            {
                var lockTaken = false;
                try
                {
                    overflowLock.Enter(ref lockTaken);
                    if (overflowCount < MaxRetained)
                    {
                        next = overflow;
                        overflow = this;
                        overflowCount++;
                    }
                    else
                    {
                        next = null;
                    }
                }
                finally
                {
                    if (lockTaken)
                        overflowLock.Exit(useMemoryBarrier: false);
                }
            }

            call?.Invoke();
        }
    }

#endif
}

public readonly struct SwitchToTaskPoolAwaitable
{
    public Awaiter GetAwaiter() => new Awaiter();

    public struct Awaiter : ICriticalNotifyCompletion
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
            ThreadPool.UnsafeQueueUserWorkItem(switchToCallback, continuation);
        }

        static void Callback(object? state)
        {
            var continuation = (Action)state!;
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
