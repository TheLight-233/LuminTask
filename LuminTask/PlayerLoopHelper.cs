using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using LuminThread.Interface;

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
        
        // Unity 2020.2 added TimeUpdate https://docs.unity3d.com/2020.2/Documentation/ScriptReference/PlayerLoop.TimeUpdate.html
        TimeUpdate = 14,
        LastTimeUpdate = 15,

        DotNet = 16
    }
    
    public static partial class PlayerLoopHelper
    {
        public const int LoopTimingCount = 17;

        public static int MainThreadId => _mainThreadId;
        public static bool IsMainThread => Thread.CurrentThread.ManagedThreadId == _mainThreadId;
        
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
            private const int SleepWhenIdle = 1;
    
            private readonly ConcurrentQueue<IPlayLoopItem> _incomingQueue1 = new();
            private readonly ConcurrentQueue<(LuminTaskState, MoveNext)> _incomingQueue2 = new();
            private readonly ConcurrentQueue<(LuminTaskState, IntPtr)> _incomingQueue3 = new();
            private readonly List<IPlayLoopItem> _activeTasks1 = new();
            private readonly List<(LuminTaskState, MoveNext)> _activeTasks2 = new();
            private readonly List<(LuminTaskState, IntPtr)> _activeTasks3 = new();
            private readonly ConcurrentQueue<Action> _continuations = new();
            private readonly CancellationTokenSource _cts = new();
            private Thread _workerThread;
            private volatile bool _disposed;
            private volatile bool _running;
            private readonly ManualResetEventSlim _workAvailable = new();

            public StandardPlayLoopStrategy()
            {
                _workerThread = new Thread(RunCore)
                {
                    IsBackground = true,
                    Name = "PlayerLoopWorker",
                    Priority = ThreadPriority.AboveNormal
                };
            }

            void IPlayLoopStrategy.AddAction(IPlayLoopItem item)
            {
                if (_disposed) 
                    throw new ObjectDisposedException(nameof(StandardPlayLoopStrategy));
                StartIfNeeded();
                _incomingQueue1.Enqueue(item);
                _workAvailable.Set();
            }
    
            void IPlayLoopStrategy.AddAction(LuminTaskState state, MoveNext item)
            {
                if (_disposed) 
                    throw new ObjectDisposedException(nameof(StandardPlayLoopStrategy));
                StartIfNeeded();
                _incomingQueue2.Enqueue((state, item));
                _workAvailable.Set();
            }
    
            unsafe void IPlayLoopStrategy.AddAction(LuminTaskState state, delegate*<in LuminTaskState, bool> item)
            {
                if (_disposed) 
                    throw new ObjectDisposedException(nameof(StandardPlayLoopStrategy));
                StartIfNeeded();
                _incomingQueue3.Enqueue((state, new IntPtr(item)));
                _workAvailable.Set();
            }

            void IPlayLoopStrategy.AddContinuation(Action continuation)
            {
                if (_disposed) 
                    throw new ObjectDisposedException(nameof(StandardPlayLoopStrategy));
                StartIfNeeded();
                _continuations.Enqueue(continuation);
                _workAvailable.Set();
            }

            private void StartIfNeeded()
            {
                if (!_running)
                {
                    _running = true;
                    _workerThread.Start();
                }
            }

            public void RunCore()
            {
                try
                {
                    while (!_cts.IsCancellationRequested)
                    {
                        ProcessIncomingTasks();
                        ProcessActiveTasks();
                        ProcessContinuations();
                
                        if (_activeTasks1.Count == 0 && _activeTasks2.Count == 0 && _activeTasks3.Count == 0 && 
                            _incomingQueue1.IsEmpty && _incomingQueue2.IsEmpty && _incomingQueue3.IsEmpty)
                        {
                            _workAvailable.Wait(SleepWhenIdle, _cts.Token);
                            _workAvailable.Reset();
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"PlayerLoop fatal error: {ex}");
                }
                finally
                {
                    Cleanup();
                }
            }

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
                while (_continuations.TryDequeue(out var continuation))
                {
                    continuation();
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
                if (_disposed) return;
        
                _disposed = true;
                _cts.Cancel();
                _workAvailable.Set();
        
                if (_workerThread != null)
                {
                    if (!_workerThread.Join(TimeSpan.FromSeconds(2)))
                    {
                        _workerThread.Interrupt();
                    }
                    _workerThread = null;
                }
        
                _cts.Dispose();
                _workAvailable.Dispose();
                Cleanup();
            }
        }
    }
}