#if UNITY_2019_3_OR_NEWER
#define UNITY_PLAYERLOOP_V2
#endif

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using LuminThread;
using LuminThread.Interface;
using UnityEngine;
using UnityEngine.Scripting;

#if UNITY_PLAYERLOOP_V2
using UnityEngine.LowLevel;
using PlayerLoopType = UnityEngine.PlayerLoop;
#else
using UnityEngine.Experimental.LowLevel;
using PlayerLoopType = UnityEngine.Experimental.PlayerLoop;
#endif

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace LuminThread.Unity
{
    /// <summary>
    /// Unity main-thread strategy. It consumes the same single AddAction ABI as every other
    /// strategy - a LuminTaskState plus a cached managed MoveNext - so this assembly contains no
    /// calli site for UnityLinker to analyze.
    /// </summary>
    internal sealed class UnityMainThreadPlayLoopStrategy : IPlayLoopStrategy
    {
        // 排空后保留的小额常驻容量（低于它不收缩，避免每帧重复扩容）。
        private const int IdleActiveTasksCapacity = 64;

        private readonly struct ScheduledAction
        {
            internal readonly LuminTaskState State;
            internal readonly MoveNext MoveNext;

            internal ScheduledAction(in LuminTaskState state, MoveNext moveNext)
            {
                State = state;
                MoveNext = moveNext;
            }
        }

        // 首次实际入队才创建下面的队列/活动表：启动时为 16 个 timing 各建一套是固定成本，
        // 绝大多数项目只用其中一两个。_activeTasks 同时是发布标记，用 Volatile 读写做双检锁，
        // 保证跨线程首次入队只创建一次，且创建后时机语义与之前完全一致。
        private readonly object _initLock = new object();
        private ConcurrentQueue<ScheduledAction> _incomingTasks;
        private ConcurrentQueue<Action> _continuations;
        private List<ScheduledAction> _activeTasks;
        private int _disposed;

        internal readonly PlayerLoopSystem.UpdateFunction UpdateDelegate;

        internal UnityMainThreadPlayLoopStrategy()
        {
            UpdateDelegate = RunCore;
        }

        // 首次入队路径上的最坏代价是一次锁；之后只是 _activeTasks 的 volatile 读与分支。
        private void EnsureInfrastructure()
        {
            if (Volatile.Read(ref _activeTasks) != null)
                return;

            lock (_initLock)
            {
                if (_activeTasks != null)
                    return;

                // Dispose 取同一把锁，晚到的 AddAction 不会在拆除之后再复活策略。
                if (Volatile.Read(ref _disposed) != 0)
                    throw new ObjectDisposedException(nameof(UnityMainThreadPlayLoopStrategy));

                _incomingTasks = new ConcurrentQueue<ScheduledAction>();
                _continuations = new ConcurrentQueue<Action>();
                // 发布标记放最后：看到它非空就一定能看到上面两个队列。
                Volatile.Write(ref _activeTasks, new List<ScheduledAction>());
            }
        }

        public void AddAction(LuminTaskState state, MoveNext moveNext)
        {
            if (moveNext == null) throw new ArgumentNullException(nameof(moveNext));
            ThrowIfDisposed();
            EnsureInfrastructure();
            _incomingTasks.Enqueue(new ScheduledAction(state, moveNext));
        }

        public void AddContinuation(Action continuation)
        {
            if (continuation == null) throw new ArgumentNullException(nameof(continuation));
            ThrowIfDisposed();
            EnsureInfrastructure();
            _continuations.Enqueue(continuation);
        }

        public void RunCore()
        {
            if (Volatile.Read(ref _disposed) != 0)
                return;

            // 该 timing 从未被入队过，没有需要排空的基础设施。
            var active = Volatile.Read(ref _activeTasks);
            if (active == null)
                return;

            ProcessContinuations();
            DrainIncoming(active);
            ProcessActiveTasks(active);
        }

        private void ProcessContinuations()
        {
            var continuations = _continuations;
            if (continuations == null)
                return;

            while (continuations.TryDequeue(out var continuation))
            {
                try
                {
                    continuation();
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception);
                }
            }
        }

        private void DrainIncoming(List<ScheduledAction> active)
        {
            var incoming = _incomingTasks;
            if (incoming == null)
                return;

            while (incoming.TryDequeue(out var action))
                active.Add(action);
        }

        private void ProcessActiveTasks(List<ScheduledAction> active)
        {
            var count = active.Count;
            if (count == 0)
                return;

            var writeIndex = 0;
            for (var readIndex = 0; readIndex < count; readIndex++)
            {
                var action = active[readIndex];
                var keep = false;
                try
                {
                    keep = action.MoveNext(action.State);
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception);
                }

                if (keep)
                    active[writeIndex++] = action;
            }

            if (writeIndex < count)
                active.RemoveRange(writeIndex, count - writeIndex);

            // 与核心 StandardPlayLoopStrategy 一致：峰值排空后释放多余容量，只用小额下限
            // 兜住常规负载，避免每帧归零后反复扩容。
            if (active.Count == 0 && active.Capacity > IdleActiveTasksCapacity)
            {
                active.Capacity = IdleActiveTasksCapacity;
            }
        }

        private void ThrowIfDisposed()
        {
            if (Volatile.Read(ref _disposed) != 0)
                throw new ObjectDisposedException(nameof(UnityMainThreadPlayLoopStrategy));
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) != 0)
                return;

            lock (_initLock)
            {
                if (_incomingTasks != null)
                {
                    while (_incomingTasks.TryDequeue(out _)) { }
                }

                if (_continuations != null)
                {
                    while (_continuations.TryDequeue(out _)) { }
                }

                _activeTasks?.Clear();
            }
        }
    }

    internal static class UnityPlayerLoopInjector
    {
        private const int UnityTimingCount = (int)PlayerLoopTiming.LastTimeUpdate + 1;

        private readonly struct InjectionPoint
        {
            internal readonly PlayerLoopTiming Timing;
            internal readonly Type LoopType;
            internal readonly bool IsLast;

            internal InjectionPoint(PlayerLoopTiming timing, Type loopType, bool isLast)
            {
                Timing = timing;
                LoopType = loopType;
                IsLast = isLast;
            }
        }

        private sealed class LuminTaskPlayerLoopRunner { }

        private static readonly UnityMainThreadPlayLoopStrategy[] s_strategies =
            new UnityMainThreadPlayLoopStrategy[UnityTimingCount];

        private static readonly InjectionPoint[] s_injectionPoints =
        {
            new InjectionPoint(PlayerLoopTiming.Initialization, typeof(PlayerLoopType.Initialization), false),
            new InjectionPoint(PlayerLoopTiming.LastInitialization, typeof(PlayerLoopType.Initialization), true),
            new InjectionPoint(PlayerLoopTiming.EarlyUpdate, typeof(PlayerLoopType.EarlyUpdate), false),
            new InjectionPoint(PlayerLoopTiming.LastEarlyUpdate, typeof(PlayerLoopType.EarlyUpdate), true),
            new InjectionPoint(PlayerLoopTiming.FixedUpdate, typeof(PlayerLoopType.FixedUpdate), false),
            new InjectionPoint(PlayerLoopTiming.LastFixedUpdate, typeof(PlayerLoopType.FixedUpdate), true),
            new InjectionPoint(PlayerLoopTiming.PreUpdate, typeof(PlayerLoopType.PreUpdate), false),
            new InjectionPoint(PlayerLoopTiming.LastPreUpdate, typeof(PlayerLoopType.PreUpdate), true),
            new InjectionPoint(PlayerLoopTiming.Update, typeof(PlayerLoopType.Update), false),
            new InjectionPoint(PlayerLoopTiming.LastUpdate, typeof(PlayerLoopType.Update), true),
            new InjectionPoint(PlayerLoopTiming.PreLateUpdate, typeof(PlayerLoopType.PreLateUpdate), false),
            new InjectionPoint(PlayerLoopTiming.LastPreLateUpdate, typeof(PlayerLoopType.PreLateUpdate), true),
            new InjectionPoint(PlayerLoopTiming.PostLateUpdate, typeof(PlayerLoopType.PostLateUpdate), false),
            new InjectionPoint(PlayerLoopTiming.LastPostLateUpdate, typeof(PlayerLoopType.PostLateUpdate), true),
#if UNITY_2020_2_OR_NEWER
            new InjectionPoint(PlayerLoopTiming.TimeUpdate, typeof(PlayerLoopType.TimeUpdate), false),
            new InjectionPoint(PlayerLoopTiming.LastTimeUpdate, typeof(PlayerLoopType.TimeUpdate), true),
#endif
        };

        private static int s_initialized;
#if UNITY_EDITOR
        [InitializeOnLoadMethod]
        private static void RegisterEditorCallbacks()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }
#endif


#if UNITY_2020_1_OR_NEWER
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
#else
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
#endif
        private static void Initialize()
        {
            if (Interlocked.Exchange(ref s_initialized, 1) != 0)
                return;

            try
            {
                for (var i = 0; i < s_injectionPoints.Length; i++)
                {
                    var timing = s_injectionPoints[i].Timing;
                    var strategy = new UnityMainThreadPlayLoopStrategy();
                    s_strategies[(int)timing] = strategy;
                    PlayerLoopHelper.SetStrategy(timing, strategy);
                }

                var playerLoop = PlayerLoop.GetCurrentPlayerLoop();
                InjectPlayerLoopSystems(ref playerLoop);
                PlayerLoop.SetPlayerLoop(playerLoop);

#if UNITY_EDITOR
                EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
                EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
#endif
            }
            catch
            {
                ShutdownStrategies();
                Volatile.Write(ref s_initialized, 0);
                throw;
            }
        }

        private static void InjectPlayerLoopSystems(ref PlayerLoopSystem playerLoop)
        {
            var systems = playerLoop.subSystemList;
            if (systems == null || systems.Length == 0)
                return;

            systems = (PlayerLoopSystem[])systems.Clone();
            for (var systemIndex = 0; systemIndex < systems.Length; systemIndex++)
            {
                var system = systems[systemIndex];
                UnityMainThreadPlayLoopStrategy first = null;
                UnityMainThreadPlayLoopStrategy last = null;

                for (var pointIndex = 0; pointIndex < s_injectionPoints.Length; pointIndex++)
                {
                    var point = s_injectionPoints[pointIndex];
                    if (point.LoopType != system.type)
                        continue;

                    var strategy = s_strategies[(int)point.Timing];
                    if (point.IsLast)
                        last = strategy;
                    else
                        first = strategy;
                }

                if (first == null && last == null)
                    continue;

                var oldSystems = system.subSystemList;
                var retainedCount = 0;
                if (oldSystems != null)
                {
                    for (var i = 0; i < oldSystems.Length; i++)
                    {
                        if (oldSystems[i].type != typeof(LuminTaskPlayerLoopRunner))
                            retainedCount++;
                    }
                }

                var newSystems = new PlayerLoopSystem[
                    retainedCount + (first == null ? 0 : 1) + (last == null ? 0 : 1)];

                var writeIndex = 0;
                if (first != null)
                    newSystems[writeIndex++] = CreateRunner(first);

                if (oldSystems != null)
                {
                    for (var i = 0; i < oldSystems.Length; i++)
                    {
                        if (oldSystems[i].type != typeof(LuminTaskPlayerLoopRunner))
                            newSystems[writeIndex++] = oldSystems[i];
                    }
                }

                if (last != null)
                    newSystems[writeIndex] = CreateRunner(last);

                system.subSystemList = newSystems;
                systems[systemIndex] = system;
            }

            playerLoop.subSystemList = systems;
        }

        private static PlayerLoopSystem CreateRunner(UnityMainThreadPlayLoopStrategy strategy)
        {
            return new PlayerLoopSystem
            {
                type = typeof(LuminTaskPlayerLoopRunner),
                updateDelegate = strategy.UpdateDelegate
            };
        }

#if UNITY_EDITOR
        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                Initialize();
                return;
            }

            if (state != PlayModeStateChange.ExitingPlayMode)
                return;

            var playerLoop = PlayerLoop.GetCurrentPlayerLoop();
            RemovePlayerLoopSystems(ref playerLoop);
            PlayerLoop.SetPlayerLoop(playerLoop);

            ShutdownStrategies();
            Volatile.Write(ref s_initialized, 0);
        }

        private static void RemovePlayerLoopSystems(ref PlayerLoopSystem playerLoop)
        {
            var systems = playerLoop.subSystemList;
            if (systems == null)
                return;

            systems = (PlayerLoopSystem[])systems.Clone();
            for (var systemIndex = 0; systemIndex < systems.Length; systemIndex++)
            {
                var system = systems[systemIndex];
                var oldSystems = system.subSystemList;
                if (oldSystems == null)
                    continue;

                var retainedCount = 0;
                for (var i = 0; i < oldSystems.Length; i++)
                {
                    if (oldSystems[i].type != typeof(LuminTaskPlayerLoopRunner))
                        retainedCount++;
                }

                if (retainedCount == oldSystems.Length)
                    continue;

                var newSystems = new PlayerLoopSystem[retainedCount];
                var writeIndex = 0;
                for (var i = 0; i < oldSystems.Length; i++)
                {
                    if (oldSystems[i].type != typeof(LuminTaskPlayerLoopRunner))
                        newSystems[writeIndex++] = oldSystems[i];
                }

                system.subSystemList = newSystems;
                systems[systemIndex] = system;
            }

            playerLoop.subSystemList = systems;
        }
#endif

        private static void ShutdownStrategies()
        {
            for (var i = 0; i < s_strategies.Length; i++)
            {
                s_strategies[i]?.Dispose();
                s_strategies[i] = null;
            }
        }
    }
}
