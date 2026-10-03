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

            private readonly ConcurrentQueue<(LuminTaskState, MoveNext)> _incomingTasks = new();
            private readonly List<(LuminTaskState, MoveNext)> _activeTasks = new();
            private readonly ConcurrentQueue<Action> _continuations = new();
            private readonly CancellationTokenSource _cts = new();
            private readonly System.Threading.Timer _timer;
            private int _processing;
            private int _disposed;

            // Timer-driven scheduling: no dedicated background thread. The timer is armed whenever
            // work is queued and parked once the active set drains, so a fully idle strategy causes
            // no periodic wake-ups at all.
            public StandardPlayLoopStrategy()
            {
                _timer = new Timer(static state => ((StandardPlayLoopStrategy)state!).OnTick(), this,
                    Timeout.Infinite, Timeout.Infinite);
            }

            void IPlayLoopStrategy.AddAction(LuminTaskState state, MoveNext moveNext)
            {
                ThrowIfDisposed();
                _incomingTasks.Enqueue((state, moveNext));
                ArmTimer();
            }

            void IPlayLoopStrategy.AddContinuation(Action continuation)
            {
                ThrowIfDisposed();
                _continuations.Enqueue(continuation);
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
                    _timer.Change(0, ActivePollIntervalMilliseconds);
                }
                catch (ObjectDisposedException)
                {
                }
            }

            public void RunCore()
            {
                if (_cts.IsCancellationRequested)
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
                        try { _timer.Change(Timeout.Infinite, Timeout.Infinite); }
                        catch (ObjectDisposedException) { }
                        if (HasActiveTasks() || HasImmediateWork())
                            ArmTimer();
                    }
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private bool HasActiveTasks()
                => _activeTasks.Count != 0;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private bool HasImmediateWork()
                => !_incomingTasks.IsEmpty || !_continuations.IsEmpty;

            private void ProcessIncomingTasks()
            {
                for (int i = 0; i < MaxTasksPerFrame && _incomingTasks.TryDequeue(out var item); i++)
                {
                    _activeTasks.Add(item);
                }
            }

            private void ProcessActiveTasks()
            {
                if (_activeTasks.Count == 0) return;

                int writeIndex = 0;
                for (int readIndex = 0; readIndex < _activeTasks.Count; readIndex++)
                {
                    if (_cts.IsCancellationRequested) break;

                    var (state, moveNext) = _activeTasks[readIndex];
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
                        _activeTasks[writeIndex++] = (state, moveNext);
                    }
                }

                _activeTasks.RemoveRange(writeIndex, _activeTasks.Count - writeIndex);
            }

            private void ProcessContinuations()
            {
                for (var i = 0;
                     i < MaxTasksPerFrame && _continuations.TryDequeue(out var continuation);
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
                while (_incomingTasks.TryDequeue(out _)) { }
                _activeTasks.Clear();
            }

            public void Dispose()
            {
                if (Interlocked.Exchange(ref _disposed, 1) != 0) return;

                _cts.Cancel();
                _timer.Dispose();
                Cleanup();
                _cts.Dispose();
            }
        }
    }
}
