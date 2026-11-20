using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using LuminThread.Utility;

namespace LuminThread;

public partial struct LuminTask
{
    /// <summary>Registers the synchronization context that represents the application's main thread.</summary>
    public static void RegisterMainThread(SynchronizationContext synchronizationContext)
        => PlayerLoopHelper.RegisterMainThread(synchronizationContext);

    /// <summary>Clears the registered main-thread synchronization context.</summary>
    public static void UnregisterMainThread()
        => PlayerLoopHelper.UnregisterMainThread();

    [DebuggerHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static SwitchToAwaitable SwitchToMainThread(CancellationToken cancellationToken = default)
    {

        var context = PlayerLoopHelper.MainThreadSynchronizationContext;
        if (context != null)
            return new SwitchToAwaitable(PlayerLoopTiming.DotNet, context, cancellationToken, SwitchToAwaitable.TargetMainThread);
#if NET8_0_OR_GREATER
        throw new ArgumentException("DotNet has no registered main thread. Call PlayerLoopHelper.RegisterMainThread first.", nameof(PlayerLoopTiming.DotNet));
#else
        return new SwitchToAwaitable(PlayerLoopTiming.Update, cancellationToken);
#endif
    }

    [DebuggerHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static SwitchToAwaitable SwitchToMainThread(PlayerLoopTiming timing, CancellationToken cancellationToken = default)
    {
        if (timing is PlayerLoopTiming.DotNet)
            throw new ArgumentException("DotNet don't have main thread", nameof(timing));
        return new SwitchToAwaitable(timing, cancellationToken);
    }

    [DebuggerHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ReturnToAwaitable ReturnToMainThread(CancellationToken cancellationToken = default)
    {
        return new ReturnToAwaitable(PlayerLoopTiming.DotNet, cancellationToken);
    }

    [DebuggerHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ReturnToAwaitable ReturnToMainThread(PlayerLoopTiming timing, CancellationToken cancellationToken = default)
    {
        return new ReturnToAwaitable(timing, cancellationToken);
    }

    [DebuggerHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Post(Action action, PlayerLoopTiming timing = PlayerLoopTiming.DotNet)
    {
        PlayerLoopHelper.AddContinuation(timing, action);
    }

    [DebuggerHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static SwitchToAwaitable SwitchToThreadPool()
    {
        return new SwitchToAwaitable(PlayerLoopTiming.DotNet, null, CancellationToken.None, SwitchToAwaitable.TargetThreadPool);
    }

    [DebuggerHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static SwitchToAwaitable SwitchToTaskPool()
    {
        return new SwitchToAwaitable(PlayerLoopTiming.DotNet, null, CancellationToken.None, SwitchToAwaitable.TargetThreadPool);
    }

    [DebuggerHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static SwitchToAwaitable SwitchToSynchronizationContext(SynchronizationContext synchronizationContext, CancellationToken cancellationToken = default)
    {
        LuminTaskExceptionHelper.ThrowArgumentNullException(synchronizationContext, nameof(synchronizationContext));
        return new SwitchToAwaitable(PlayerLoopTiming.DotNet, synchronizationContext, cancellationToken, SwitchToAwaitable.TargetSynchronizationContext);
    }

    [DebuggerHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ReturnToAwaitable ReturnToSynchronizationContext(SynchronizationContext synchronizationContext, CancellationToken cancellationToken = default)
    {
        return new ReturnToAwaitable(PlayerLoopTiming.DotNet, synchronizationContext, false, cancellationToken);
    }

    [DebuggerHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ReturnToAwaitable ReturnToCurrentSynchronizationContext(bool dontPostWhenSameContext = true, CancellationToken cancellationToken = default)
    {
        return new ReturnToAwaitable(PlayerLoopTiming.DotNet, SynchronizationContext.Current, dontPostWhenSameContext, cancellationToken);
    }
}

// The one awaiter for every "switch to" target: a player-loop timing / main thread, a specific
// synchronization context, or the thread pool. The four originals (main thread, thread pool, task
// pool, synchronization context) each carried a nested awaiter, so merging them removes ten types
// and - the part that matters for metadata - collapses four awaiter dimensions into one for every
// state machine that switches threads.
//
// The type is its own awaiter (`GetAwaiter` returns `this`), which is what makes the merge possible
// without a second type per target.
public readonly struct SwitchToAwaitable : ICriticalNotifyCompletion
{
    internal const byte TargetMainThread = 0;

    // SwitchToSynchronizationContext always posts, even when it is already on that context - that is
    // the behaviour of the original awaiter, and SwitchToMainThread keeps its "already there"
    // short-circuit. A byte discriminator is enough to tell the two apart.
    internal const byte TargetSynchronizationContext = 1;
    internal const byte TargetThreadPool = 2;

    static readonly SendOrPostCallback ContextCallback = PostToContext;
    static readonly WaitCallback PoolCallback = PostToPool;

    readonly PlayerLoopTiming timing;
    readonly SynchronizationContext? synchronizationContext;
    readonly CancellationToken cancellationToken;
    readonly byte target;

    [DebuggerHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public SwitchToAwaitable(PlayerLoopTiming timing, CancellationToken cancellationToken)
    {
        this.timing = timing;
        synchronizationContext = null;
        this.cancellationToken = cancellationToken;
        target = TargetMainThread;
    }

    [DebuggerHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal SwitchToAwaitable(PlayerLoopTiming timing, SynchronizationContext? synchronizationContext, CancellationToken cancellationToken, byte target)
    {
        this.timing = timing;
        this.synchronizationContext = synchronizationContext;
        this.cancellationToken = cancellationToken;
        this.target = target;
    }

    [DebuggerHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public SwitchToAwaitable GetAwaiter() => this;

    public bool IsCompleted
    {
        get
        {
            if (target == TargetThreadPool || target == TargetSynchronizationContext)
                return false;

            // Main-thread target: already there when the thread or the registered context matches.
            if (PlayerLoopHelper.MainThreadId == Thread.CurrentThread.ManagedThreadId)
                return true;

            return synchronizationContext != null && ReferenceEquals(SynchronizationContext.Current, synchronizationContext);
        }
    }

    [DebuggerHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void GetResult() => cancellationToken.ThrowIfCancellationRequested();

    public void OnCompleted(Action continuation)
    {
        if (target == TargetThreadPool)
        {
            ThreadPool.QueueUserWorkItem(PoolCallback, continuation);
            return;
        }

        var context = synchronizationContext;
        if (context != null)
        {
            context.Post(ContextCallback, continuation);
            return;
        }

        PlayerLoopHelper.AddContinuation(timing, continuation);
    }

    public void UnsafeOnCompleted(Action continuation)
    {
        if (target == TargetThreadPool)
        {
#if NET5_0_OR_GREATER
            ThreadPool.UnsafeQueueUserWorkItem(ThreadPoolWorkItem.Create(continuation), preferLocal: false);
#else
            ThreadPool.UnsafeQueueUserWorkItem(PoolCallback, continuation);
#endif
            return;
        }

        OnCompleted(continuation);
    }

    static void PostToContext(object? state) => ((Action)state!).Invoke();

    static void PostToPool(object? state) => ((Action)state!).Invoke();

#if NET5_0_OR_GREATER

    // IThreadPoolWorkItem lets the runtime enqueue this instance directly. The WaitCallback overload
    // allocates an internal work item on every call, while this path allocates nothing once the pool
    // has warmed up - which is what makes a steady-state SwitchToThreadPool GC-free.
    //
    // Only .NET 5+ exposes the interface; on netstandard2.1 (Unity) the WaitCallback path above is
    // kept as-is.
    sealed class ThreadPoolWorkItem : IThreadPoolWorkItem, ILuminCacheNode
    {
        public ILuminCacheNode? CacheNext { get; set; }
        private Action? _continuation;

        internal static ThreadPoolWorkItem Create(Action continuation)
        {
            var item = (ThreadPoolWorkItem?)LuminTaskIdleCache.Take(typeof(ThreadPoolWorkItem)) ?? new ThreadPoolWorkItem();
            item._continuation = continuation;
            return item;
        }

        void IThreadPoolWorkItem.Execute()
        {
            var call = _continuation;
            _continuation = null;
            LuminTaskIdleCache.Return(this, 48);
            call!();
        }
    }

#endif
}

// The one awaiter for returning to a context: either the main thread (posted through the player
// loop timing) or a specific synchronization context. `DisposeAsync` keeps the `await using`
// shape of the originals, and the type is its own awaiter so the two originals collapse into a
// single type and a single awaiter dimension.
public readonly struct ReturnToAwaitable : ICriticalNotifyCompletion
{
    static readonly SendOrPostCallback PostCallback = Post;

    readonly PlayerLoopTiming timing;
    readonly SynchronizationContext? synchronizationContext;
    readonly bool dontPostWhenSameContext;
    readonly CancellationToken cancellationToken;

    [DebuggerHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReturnToAwaitable(PlayerLoopTiming timing, CancellationToken cancellationToken)
    {
        this.timing = timing;
        synchronizationContext = null;
        dontPostWhenSameContext = false;
        this.cancellationToken = cancellationToken;
    }

    [DebuggerHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal ReturnToAwaitable(PlayerLoopTiming timing, SynchronizationContext? synchronizationContext, bool dontPostWhenSameContext, CancellationToken cancellationToken)
    {
        this.timing = timing;
        this.synchronizationContext = synchronizationContext;
        this.dontPostWhenSameContext = dontPostWhenSameContext;
        this.cancellationToken = cancellationToken;
    }

    [DebuggerHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReturnToAwaitable GetAwaiter() => this;

    [DebuggerHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReturnToAwaitable DisposeAsync() => this;

    public bool IsCompleted
    {
        get
        {
            if (synchronizationContext == null)
                return PlayerLoopHelper.MainThreadId == Thread.CurrentThread.ManagedThreadId;

            return dontPostWhenSameContext && ReferenceEquals(SynchronizationContext.Current, synchronizationContext);
        }
    }

    [DebuggerHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void GetResult() => cancellationToken.ThrowIfCancellationRequested();

    public void OnCompleted(Action continuation)
    {
        var context = synchronizationContext;
        if (context != null)
        {
            context.Post(PostCallback, continuation);
            return;
        }

        PlayerLoopHelper.AddContinuation(timing, continuation);
    }

    public void UnsafeOnCompleted(Action continuation) => OnCompleted(continuation);

    static void Post(object? state) => ((Action)state!).Invoke();
}