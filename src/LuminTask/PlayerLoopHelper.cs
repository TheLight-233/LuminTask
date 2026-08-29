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

        public static void AddAction(PlayerLoopTiming timing, IPlayLoopItem item)
        {
            var strategy = _strategies[(byte)timing];
            if (strategy is null)
                ThrowInvalidLoopTiming(timing);
            strategy!.AddAction(item);
        }

        public static void AddAction(PlayerLoopTiming timing, LuminTaskState state, MoveNext moveNext)
        {
            var strategy = _strategies[(byte)timing];
            if (strategy is null)
                ThrowInvalidLoopTiming(timing);
            strategy!.AddAction(state, moveNext);
        }

        public static unsafe void AddAction(PlayerLoopTiming timing, LuminTaskState state, delegate*<in LuminTaskState, bool> moveNext)
        {
            var strategy = _strategies[(byte)timing];
            if (strategy is null)
                ThrowInvalidLoopTiming(timing);
            if (strategy is not IFunctionPointerPlayLoopStrategy pointerStrategy)
                throw new PlatformNotSupportedException("The selected PlayerLoop strategy does not support function-pointer dispatch.");
            pointerStrategy.AddAction(state, new IntPtr(moveNext));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static unsafe void AddAction(
            PlayerLoopTiming timing,
            LuminTaskState state,
            MoveNext managedMoveNext,
            delegate*<in LuminTaskState, bool> functionPointer)
        {
            var strategy = _strategies[(byte)timing];
            if (strategy is null)
                ThrowInvalidLoopTiming(timing);

            if (strategy is IFunctionPointerPlayLoopStrategy pointerStrategy)
                pointerStrategy.AddAction(state, new IntPtr(functionPointer));
            else
                strategy!.AddAction(state, managedMoveNext);
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

        private sealed class StandardPlayLoopStrategy : IPlayLoopStrategy, IFunctionPointerPlayLoopStrategy
        {
            private const int MaxTasksPerFrame = 1000;
            private const int ActivePollIntervalMilliseconds = 1;

            private readonly ConcurrentQueue<IPlayLoopItem> _incomingQueue1 = new();
            private readonly ConcurrentQueue<(LuminTaskState, MoveNext)> _incomingQueue2 = new();
            private readonly ConcurrentQueue<(LuminTaskState, IntPtr)> _incomingQueue3 = new();
            private readonly List<IPlayLoopItem> _activeTasks1 = new();
            private readonly List<(LuminTaskState, MoveNext)> _activeTasks2 = new();
            private readonly List<(LuminTaskState, IntPtr)> _activeTasks3 = new();
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

            void IPlayLoopStrategy.AddAction(IPlayLoopItem item)
            {
                ThrowIfDisposed();
                _incomingQueue1.Enqueue(item);
                ArmTimer();
            }

            void IPlayLoopStrategy.AddAction(LuminTaskState state, MoveNext item)
            {
                ThrowIfDisposed();
                _incomingQueue2.Enqueue((state, item));
                ArmTimer();
            }

            void IFunctionPointerPlayLoopStrategy.AddAction(LuminTaskState state, IntPtr item)
            {
                ThrowIfDisposed();
                _incomingQueue3.Enqueue((state, item));
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
                => _activeTasks1.Count != 0 || _activeTasks2.Count != 0 || _activeTasks3.Count != 0;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private bool HasImmediateWork()
                => !_incomingQueue1.IsEmpty || !_incomingQueue2.IsEmpty || !_incomingQueue3.IsEmpty ||
                   !_continuations.IsEmpty;

            private void ProcessIncomingTasks()
            {
                for (int i = 0; i < MaxTasksPerFrame && _incomingQueue1.TryDequeue(out var item); i++)
                {
                    _activeTasks1.Add(item);
                }

                for (int i = 0; i < MaxTasksPerFrame && _incomingQueue2.TryDequeue(out var item); i++)
                {
                    _activeTasks2.Add(item);
                }

                for (int i = 0; i < MaxTasksPerFrame && _incomingQueue3.TryDequeue(out var item); i++)
                {
                    _activeTasks3.Add(item);
                }
            }

            private void ProcessActiveTasks()
            {
                ProcessActiveTasks1();
                ProcessActiveTasks2();
                ProcessActiveTasks3();
            }

            private void ProcessActiveTasks1()
            {
                if (_activeTasks1.Count == 0) return;

                int writeIndex = 0;
                for (int readIndex = 0; readIndex < _activeTasks1.Count; readIndex++)
                {
                    if (_cts.IsCancellationRequested) break;

                    var task = _activeTasks1[readIndex];
                    bool shouldContinue = false;

                    try
                    {
                        shouldContinue = task.MoveNext();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Task execution error: {ex}");
                        shouldContinue = false;
                    }

                    if (shouldContinue)
                    {
                        _activeTasks1[writeIndex++] = task;
                    }
                    else
                    {
                        if (task is IDisposable disposable)
                            disposable.Dispose();
                    }
                }

                _activeTasks1.RemoveRange(writeIndex, _activeTasks1.Count - writeIndex);
            }

            private void ProcessActiveTasks2()
            {
                if (_activeTasks2.Count == 0) return;

                int writeIndex = 0;
                for (int readIndex = 0; readIndex < _activeTasks2.Count; readIndex++)
                {
                    if (_cts.IsCancellationRequested) break;

                    var (state, moveNext) = _activeTasks2[readIndex];
                    bool shouldContinue = false;

                    try
                    {
                        shouldContinue = moveNext(state);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Task execution error: {ex}");
                        // The promise escaped before releasing its pooled state. Return it now so
                        // the dropped item doesn't leak a StateTuple; a fault cannot be delivered
                        // generically because the scheduler has no knowledge of the result type T.
                        (state.StateTuple as IDisposable)?.Dispose();
                        (state.State as IDisposable)?.Dispose();
                        shouldContinue = false;
                    }

                    if (shouldContinue)
                    {
                        _activeTasks2[writeIndex++] = (state, moveNext);
                    }
                }

                _activeTasks2.RemoveRange(writeIndex, _activeTasks2.Count - writeIndex);
            }

            private unsafe void ProcessActiveTasks3()
            {
                if (_activeTasks3.Count == 0) return;

                int writeIndex = 0;
                for (int readIndex = 0; readIndex < _activeTasks3.Count; readIndex++)
                {
                    if (_cts.IsCancellationRequested) break;

                    var (state, ptr) = _activeTasks3[readIndex];
                    bool shouldContinue = false;

                    try
                    {
                        shouldContinue = ((delegate*<in LuminTaskState, bool>)ptr)(state);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Task execution error: {ex}");
                        // Same last-resort release as queue 2: return pooled state of a dropped item.
                        (state.StateTuple as IDisposable)?.Dispose();
                        (state.State as IDisposable)?.Dispose();
                        shouldContinue = false;
                    }

                    if (shouldContinue)
                    {
                        _activeTasks3[writeIndex++] = (state, ptr);
                    }
                }

                _activeTasks3.RemoveRange(writeIndex, _activeTasks3.Count - writeIndex);
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
                while (_incomingQueue1.TryDequeue(out var item))
                {
                    (item as IDisposable)?.Dispose();
                }

                while (_incomingQueue2.TryDequeue(out _)) { }
                while (_incomingQueue3.TryDequeue(out _)) { }

                foreach (var task in _activeTasks1)
                {
                    (task as IDisposable)?.Dispose();
                }

                _activeTasks1.Clear();
                _activeTasks2.Clear();
                _activeTasks3.Clear();
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
