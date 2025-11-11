using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using Lumin.Threading.Interface;

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
        
        public static void AddAction(PlayerLoopTiming timing, IntPtr source, MoveNext moveNext)
        {
            var strategy = _strategies[(byte)timing];
            if (strategy is null) 
                ThrowInvalidLoopTiming(timing);
            strategy!.AddAction(source, moveNext);
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
            private const int SleepWhenIdle = 10; // ms
            
            private readonly ConcurrentStack<IPlayLoopItem> _incomingQueue = new();
            private readonly ConcurrentStack<(IntPtr, MoveNext)> _incomingQueue2 = new();
            private readonly ConcurrentBag<Action> _continuations = new();
            private readonly List<IPlayLoopItem> _activeTasks = new();
            private readonly List<(IntPtr, MoveNext)> _activeTasks2 = new();
            private readonly CancellationTokenSource _cts = new();
            private Thread _workerThread;
            private volatile bool _disposed;
            private volatile bool _running;

            public StandardPlayLoopStrategy()
            {
                _workerThread = new Thread(((IPlayLoopStrategy)this).RunCore)
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
                if (!_running) 
                {
                    _running = true;
                    _workerThread.Start();
                }
                
                _incomingQueue.Push(item);
            }
            
            void IPlayLoopStrategy.AddAction(IntPtr ptr, MoveNext item)
            {
                if (_disposed) 
                    throw new ObjectDisposedException(nameof(StandardPlayLoopStrategy));
                if (!_running) 
                {
                    _running = true;
                    _workerThread.Start();
                }
                
                _incomingQueue2.Push((ptr, item));
            }

            void IPlayLoopStrategy.AddContinuation(Action continuation)
            {
                if (_disposed) 
                    throw new ObjectDisposedException(nameof(StandardPlayLoopStrategy));
                if (!_running) 
                {
                    _running = true;
                    _workerThread.Start();
                }
                
                _continuations.Add(continuation);
            }

            void IPlayLoopStrategy.RunCore()
            {
                try
                {
                    while (!_cts.IsCancellationRequested)
                    {
                        ProcessIncomingTasks();
                        ProcessActiveTasks();
                        ProcessActiveTasks2();
                        ProcessContinuations();
                        
                        // 没有任务时休眠
                        if (_activeTasks.Count is 0 && _incomingQueue.IsEmpty)
                        {
                            Thread.Sleep(SleepWhenIdle);
                        }
                    }
                }
                catch (ThreadAbortException)
                {
                    // 正常退出
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
                // 将新任务从队列转移到活动列表
                for (int i = 0; i < MaxTasksPerFrame && _incomingQueue.TryPop(out var item); i++)
                {
                    _activeTasks.Add(item);
                }
                
                for (int i = 0; i < MaxTasksPerFrame && _incomingQueue2.TryPop(out var item); i++)
                {
                    _activeTasks2.Add(item);
                }
            }

            private void ProcessActiveTasks()
            {
                for (int i = _activeTasks.Count - 1; i >= 0; i--)
                {
                    if (_cts.IsCancellationRequested) return;
                    
                    try
                    {
                        var task = _activeTasks[i];
                        bool shouldContinue = task.MoveNext();
                        
                        if (!shouldContinue)
                        {
                            _activeTasks.RemoveAt(i);
                            
                            if (task is IDisposable disposable)
                                disposable.Dispose();
                        }
                    }
                    catch (Exception ex)
                    {
                        _activeTasks.RemoveAt(i);
                        Console.WriteLine($"Task execution error: {ex}");
                    }
                }
                
            }
            
            private unsafe void ProcessActiveTasks2()
            {
                for (int i = _activeTasks2.Count - 1; i >= 0; i--)
                {
                    if (_cts.IsCancellationRequested) return;
                    
                    try
                    {
                        var task = _activeTasks2[i];
                        bool shouldContinue = task.Item2(task.Item1.ToPointer());
                        
                        if (!shouldContinue)
                        {
                            _activeTasks2.RemoveAt(i);
                        }
                    }
                    catch (Exception ex)
                    {
                        _activeTasks2.RemoveAt(i);
                        Console.WriteLine($"Task execution error: {ex}");
                    }
                }
                
            }

            private void ProcessContinuations()
            {
                foreach (var continuation in _continuations)
                {
                    continuation();
                }
                
                _continuations.Clear();
            }

            private void Cleanup()
            {
                // 清理所有剩余任务
                while (_incomingQueue.TryPop(out var item))
                {
                    (item as IDisposable)?.Dispose();
                }
                
                foreach (var task in _activeTasks)
                {
                    (task as IDisposable)?.Dispose();
                }
                
                _activeTasks.Clear();
                _activeTasks2.Clear();
            }

            public void Dispose()
            {
                if (_disposed) return;
                
                _disposed = true;
                _cts.Cancel();
                
                if (_workerThread != null)
                {
                    if (!_workerThread.Join(TimeSpan.FromSeconds(2)))
                    {
                        _workerThread.Interrupt();
                    }
                    _workerThread = null;
                }
                
                _cts.Dispose();
                Cleanup();
            }
        }
    }
}