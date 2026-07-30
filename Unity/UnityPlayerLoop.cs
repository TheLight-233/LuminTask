#if UNITY_2019_3_OR_NEWER
#define UNITY_PLAYERLOOP_V2
#endif

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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
    /// Unity main-thread strategy. Function-pointer dispatch intentionally stays in the
    /// LuminTask core assembly; Unity uses the managed MoveNext fallback so UnityLinker
    /// never has to analyze a calli site in this extension assembly.
    /// </summary>
    [Preserve]
    internal sealed class UnityMainThreadPlayLoopStrategy : IPlayLoopStrategy
    {
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

        private readonly ConcurrentQueue<IPlayLoopItem> _incomingItems =
            new ConcurrentQueue<IPlayLoopItem>();
        private readonly ConcurrentQueue<ScheduledAction> _incomingActions =
            new ConcurrentQueue<ScheduledAction>();
        private readonly ConcurrentQueue<Action> _continuations =
            new ConcurrentQueue<Action>();

        private readonly List<IPlayLoopItem> _activeItems = new List<IPlayLoopItem>();
        private readonly List<ScheduledAction> _activeActions = new List<ScheduledAction>();
        private int _disposed;

        internal readonly PlayerLoopSystem.UpdateFunction UpdateDelegate;

        internal UnityMainThreadPlayLoopStrategy()
        {
            UpdateDelegate = RunCore;
        }

        public void AddAction(IPlayLoopItem item)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));
            ThrowIfDisposed();
            _incomingItems.Enqueue(item);
        }

        public void AddAction(LuminTaskState state, MoveNext moveNext)
        {
            if (moveNext == null) throw new ArgumentNullException(nameof(moveNext));
            ThrowIfDisposed();
            _incomingActions.Enqueue(new ScheduledAction(state, moveNext));
        }

        public void AddContinuation(Action continuation)
        {
            if (continuation == null) throw new ArgumentNullException(nameof(continuation));
            ThrowIfDisposed();
            _continuations.Enqueue(continuation);
        }

        public void RunCore()
        {
            if (Volatile.Read(ref _disposed) != 0)
                return;

            ProcessContinuations();
            DrainIncoming();
            ProcessActiveItems();
            ProcessActiveActions();
        }

        private void ProcessContinuations()
        {
            while (_continuations.TryDequeue(out var continuation))
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

        private void DrainIncoming()
        {
            while (_incomingItems.TryDequeue(out var item))
                _activeItems.Add(item);

            while (_incomingActions.TryDequeue(out var action))
                _activeActions.Add(action);
        }

        private void ProcessActiveItems()
        {
            var count = _activeItems.Count;
            if (count == 0)
                return;

            var writeIndex = 0;
            for (var readIndex = 0; readIndex < count; readIndex++)
            {
                var item = _activeItems[readIndex];
                var keep = false;
                try
                {
                    keep = item.MoveNext();
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception);
                }

                if (keep)
                    _activeItems[writeIndex++] = item;
                else
                    (item as IDisposable)?.Dispose();
            }

            if (writeIndex < count)
                _activeItems.RemoveRange(writeIndex, count - writeIndex);
        }

        private void ProcessActiveActions()
        {
            var count = _activeActions.Count;
            if (count == 0)
                return;

            var writeIndex = 0;
            for (var readIndex = 0; readIndex < count; readIndex++)
            {
                var action = _activeActions[readIndex];
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
                    _activeActions[writeIndex++] = action;
            }

            if (writeIndex < count)
                _activeActions.RemoveRange(writeIndex, count - writeIndex);
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

            while (_incomingItems.TryDequeue(out var item))
                (item as IDisposable)?.Dispose();
            while (_incomingActions.TryDequeue(out _)) { }
            while (_continuations.TryDequeue(out _)) { }

            for (var i = 0; i < _activeItems.Count; i++)
                (_activeItems[i] as IDisposable)?.Dispose();

            _activeItems.Clear();
            _activeActions.Clear();
        }
    }

    [Preserve]
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
