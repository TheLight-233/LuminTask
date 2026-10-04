using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using LuminThread.Interface;
using LuminThread.Utility;

namespace LuminThread
{
    public enum PlayerLoopTiming : byte
    {
        Initialization = 0,
        LastInitialization = 1,

        EarlyUpdate = 2,
        LastEarlyUpdate = 3,

        FixedUpdate = 4,
        LastFixedUpdate = 5,

        PreUpdate = 6,
        LastPreUpdate = 7,

        Update = 8,
        LastUpdate = 9,

        PreLateUpdate = 10,
        LastPreLateUpdate = 11,

        PostLateUpdate = 12,
        LastPostLateUpdate = 13,

        TimeUpdate = 14,
        LastTimeUpdate = 15,

        DotNet = 16
    }

    public static partial class PlayerLoopHelper
    {
        public const int LoopTimingCount = 17;

        public static int MainThreadId => _mainThreadId;
        public static bool IsMainThread => Thread.CurrentThread.ManagedThreadId == _mainThreadId;
        public static SynchronizationContext? MainThreadSynchronizationContext { get; private set; }

        private static IPlayLoopStrategy?[] _strategies;
        private static int _mainThreadId;

        static PlayerLoopHelper()
        {
            _strategies = new IPlayLoopStrategy[LoopTimingCount];
            _mainThreadId = Thread.CurrentThread.ManagedThreadId;
            Initialize();
        }

        public static void Initialize()
        {
            _strategies[(byte)PlayerLoopTiming.DotNet] = new StandardPlayLoopStrategy();
        }

        public static void RegisterMainThread(SynchronizationContext synchronizationContext)
        {
            if (synchronizationContext == null) throw new ArgumentNullException(nameof(synchronizationContext));
            MainThreadSynchronizationContext = synchronizationContext;
            _mainThreadId = Thread.CurrentThread.ManagedThreadId;
        }

        public static void UnregisterMainThread() => MainThreadSynchronizationContext = null;

        public static void AddAction(PlayerLoopTiming timing, LuminTaskState state, MoveNext moveNext)
        {
            var strategy = _strategies[(byte)timing];
            if (strategy is null)
                ThrowInvalidLoopTiming(timing);
            strategy!.AddAction(state, moveNext);
        }

        public static void AddContinuation(PlayerLoopTiming timing, Action continuation)
        {
            var strategy = _strategies[(byte)timing];
            if (strategy is null)
                ThrowInvalidLoopTiming(timing);
            strategy!.AddContinuation(continuation);
        }

        public static void SetStrategy(PlayerLoopTiming timing, IPlayLoopStrategy strategy)
        {
            _strategies[(byte)timing]?.Dispose();
            _strategies[(byte)timing] = strategy;
        }

        static void ThrowInvalidLoopTiming(PlayerLoopTiming playerLoopTiming)
        {
            throw new InvalidOperationException("Target playerLoopTiming is not injected. Please check PlayerLoopHelper.Initialize. PlayerLoopTiming:" + playerLoopTiming);
        }

        public static void Dispose()
        {
            foreach (var strategy in _strategies)
            {
                strategy?.Dispose();
            }
        }

        private sealed class StandardPlayLoopStrategy : IPlayLoopStrategy
        {
            private const int MaxTasksPerFrame = 1000;
            private const int ActivePollIntervalMilliseconds = 1;

            // 排空后保留的小额常驻容量（低于它不收缩，避免每帧重复扩容）。
            private const int IdleActiveTasksCapacity = 64;

            // 首次实际入队才创建下面的队列/活动表/CTS/定时器：只初始化了 PlayerLoopHelper 却从
            // 不使用 DotNet 时机的进程（Unity 最常见）不再为它付出常驻内存。_timer 同时是发布
            // 标记，用 Volatile 读写做双检锁，保证跨线程首次入队时只创建一次。
            private readonly object _initLock = new();
            private ConcurrentQueue<(LuminTaskState, MoveNext)>? _incomingTasks;
            private List<(LuminTaskState, MoveNext)>? _activeTasks;
            private ConcurrentQueue<Action>? _continuations;
            private CancellationTokenSource? _cts;
            private System.Threading.Timer? _timer;
            private int _processing;
            private int _disposed;

            public StandardPlayLoopStrategy()
            {
            }

            // 首次入队路径上的最坏代价是一次锁；之后只是 _timer 的 volatile 读与分支。
            private void EnsureInfrastructure()
            {
                if (Volatile.Read(ref _timer) is not null) return;

                lock (_initLock)
                {
                    if (_timer is not null) return;

                    // Dispose takes the same lock, so a late AddAction cannot resurrect the
                    // strategy after its infrastructure has already been torn down.
                    if (Volatile.Read(ref _disposed) != 0)
                        throw new ObjectDisposedException(nameof(StandardPlayLoopStrategy));

                    _incomingTasks = new ConcurrentQueue<(LuminTaskState, MoveNext)>();
                    _continuations = new ConcurrentQueue<Action>();
                    _activeTasks = new List<(LuminTaskState, MoveNext)>();
                    _cts = new CancellationTokenSource();

                    // Timer-driven scheduling: no dedicated background thread. The timer is armed
                    // whenever work is queued and parked once the active set drains, so a fully idle
                    // strategy causes no periodic wake-ups at all.
                    Volatile.Write(ref _timer, new Timer(
                        static state => ((StandardPlayLoopStrategy)state!).OnTick(), this,
                        Timeout.Infinite, Timeout.Infinite));
                }
            }

            void IPlayLoopStrategy.AddAction(LuminTaskState state, MoveNext moveNext)
            {
                ThrowIfDisposed();
                EnsureInfrastructure();
                _incomingTasks!.Enqueue((state, moveNext));
                ArmTimer();
            }

            void IPlayLoopStrategy.AddContinuation(Action continuation)
            {
                ThrowIfDisposed();
                EnsureInfrastructure();
                _continuations!.Enqueue(continuation);
                ArmTimer();
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private void ThrowIfDisposed()
            {
                if (Volatile.Read(ref _disposed) != 0)
                    throw new ObjectDisposedException(nameof(StandardPlayLoopStrategy));
            }

            private void ArmTimer()
            {
                try
                {
                    _timer!.Change(0, ActivePollIntervalMilliseconds);
                }
                catch (ObjectDisposedException)
                {
                }
            }

            public void RunCore()
            {
                var cts = _cts;
                if (cts is null || cts.IsCancellationRequested)
                    return;

                ProcessIncomingTasks();
                ProcessActiveTasks();
                ProcessContinuations();
            }

            private void OnTick()
            {
                if (Volatile.Read(ref _disposed) != 0)
                    return;
                // Only one processing pass at a time; concurrent ticks are dropped.
                if (Interlocked.CompareExchange(ref _processing, 1, 0) != 0)
                    return;

                try
                {
                    RunCore();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"PlayerLoop fatal error: {ex}");
                }
                finally
                {
                    Volatile.Write(ref _processing, 0);

                    // Drain the active set, then park the timer. Producers always re-arm after
                    // enqueueing, and the driver double-checks the queues, so no wake is lost.
                    if (!HasActiveTasks() && !HasImmediateWork())
                    {
                        try { _timer!.Change(Timeout.Infinite, Timeout.Infinite); }
                        catch (ObjectDisposedException) { }
                        if (HasActiveTasks() || HasImmediateWork())
                            ArmTimer();
                    }
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private bool HasActiveTasks()
                => _activeTasks is { Count: > 0 };

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private bool HasImmediateWork()
                => _incomingTasks is { IsEmpty: false } || _continuations is { IsEmpty: false };

            private void ProcessIncomingTasks()
            {
                var incoming = _incomingTasks!;
                var active = _activeTasks!;

                for (int i = 0; i < MaxTasksPerFrame && incoming.TryDequeue(out var item); i++)
                {
                    active.Add(item);
                }
            }

            private void ProcessActiveTasks()
            {
                var active = _activeTasks!;
                if (active.Count == 0) return;

                int writeIndex = 0;
                for (int readIndex = 0; readIndex < active.Count; readIndex++)
                {
                    if (_cts!.IsCancellationRequested) break;

                    var (state, moveNext) = active[readIndex];
                    bool shouldContinue = false;

                    try
                    {
                        shouldContinue = moveNext(state);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Task execution error: {ex}");
                        // The promise escaped before completing its task. Drop the item and release
                        // its helper state; a fault cannot be delivered generically because the
                        // scheduler has no knowledge of the result type T.
                        (state.State as IDisposable)?.Dispose();
                        shouldContinue = false;
                    }

                    if (shouldContinue)
                    {
                        active[writeIndex++] = (state, moveNext);
                    }
                }

                active.RemoveRange(writeIndex, active.Count - writeIndex);

                // 峰值（例如一帧涌入上万任务）过后列表会长期持有整块容量。排空时把超出
                // 小额下限的容量还给 GC；下限本身避免“每帧归零 → 立刻重新扩容”的抖动。
                if (active.Count == 0 && active.Capacity > IdleActiveTasksCapacity)
                {
                    active.Capacity = IdleActiveTasksCapacity;
                }
            }

            private void ProcessContinuations()
            {
                var continuations = _continuations!;

                for (var i = 0;
                     i < MaxTasksPerFrame && continuations.TryDequeue(out var continuation);
                     i++)
                {
                    try
                    {
                        continuation();
                    }
                    catch (Exception exception)
                    {
                        // One faulty callback must not terminate the shared scheduler thread.
                        LuminTaskScheduler.PublishUnobservedTaskException(exception);
                    }
                }
            }

            private void Cleanup()
            {
                if (_incomingTasks is { } incoming)
                {
                    while (incoming.TryDequeue(out _)) { }
                }

                _activeTasks?.Clear();
                _continuations?.Clear();
            }

            public void Dispose()
            {
                if (Interlocked.Exchange(ref _disposed, 1) != 0) return;

                lock (_initLock)
                {
                    _cts?.Cancel();
                    _timer?.Dispose();
                    Cleanup();
                    _cts?.Dispose();
                }
            }
        }
    }
}
