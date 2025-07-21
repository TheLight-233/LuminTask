#if UNITY_2019_3_OR_NEWER
#define UNITY_PLAYERLOOP_V2
#endif

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using UnityEngine;
using Lumin.Threading.Interface;
using Lumin.Threading.Unity;

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

namespace Lumin.Threading.Unity
{
    /// <summary>
    /// Unity主线程策略实现，在Unity的PlayerLoop中执行任务
    /// </summary>
    internal class UnityMainThreadPlayLoopStrategy : IPlayLoopStrategy
    {
        private readonly ConcurrentQueue<IPlayLoopItem> _actionQueue = new ConcurrentQueue<IPlayLoopItem>();
        private readonly ConcurrentQueue<Action> _continuationQueue = new ConcurrentQueue<Action>();
        private readonly List<IPlayLoopItem> _activeTasks = new List<IPlayLoopItem>();

        public void AddAction(IPlayLoopItem item)
        {
            _actionQueue.Enqueue(item);
        }

        public void AddContinuation(Action continuation)
        {
            _continuationQueue.Enqueue(continuation);
        }

        /// <summary>
        /// 执行当前帧的所有任务和继续
        /// </summary>
        public void RunCore()
        {
            ProcessContinuations();
            ProcessIncomingTasks();
            ProcessActiveTasks();
        }

        private void ProcessContinuations()
        {
            while (_continuationQueue.TryDequeue(out var continuation))
            {
                try
                {
                    continuation.Invoke();
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                }
            }
        }

        private void ProcessIncomingTasks()
        {
            while (_actionQueue.TryDequeue(out var item))
            {
                _activeTasks.Add(item);
            }
        }

        private void ProcessActiveTasks()
        {
            for (int i = _activeTasks.Count - 1; i >= 0; i--)
            {
                var task = _activeTasks[i];
                bool shouldContinue = false;

                try
                {
                    shouldContinue = task.MoveNext();
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                }

                if (!shouldContinue)
                {
                    _activeTasks.RemoveAt(i);
                    (task as IDisposable)?.Dispose();
                }
            }
        }

        public void Dispose()
        {
            _actionQueue.Clear();
            _continuationQueue.Clear();

            foreach (var task in _activeTasks)
            {
                (task as IDisposable)?.Dispose();
            }
            _activeTasks.Clear();
        }
        
    }

    /// <summary>
    /// Unity PlayerLoop注入器
    /// </summary>
    internal static class UnityPlayerLoopInjector
    {
        private static readonly Dictionary<PlayerLoopTiming, UnityMainThreadPlayLoopStrategy> _strategies = 
            new Dictionary<PlayerLoopTiming, UnityMainThreadPlayLoopStrategy>();

        // Unity PlayerLoop阶段映射
        private static readonly Dictionary<PlayerLoopTiming, Type> _unityLoopTypeMap = new Dictionary<PlayerLoopTiming, Type>
        {
            { PlayerLoopTiming.Initialization, typeof(PlayerLoopType.Initialization) },
            { PlayerLoopTiming.LastInitialization, typeof(PlayerLoopType.Initialization) },
            { PlayerLoopTiming.EarlyUpdate, typeof(PlayerLoopType.EarlyUpdate) },
            { PlayerLoopTiming.LastEarlyUpdate, typeof(PlayerLoopType.EarlyUpdate) },
            { PlayerLoopTiming.FixedUpdate, typeof(PlayerLoopType.FixedUpdate) },
            { PlayerLoopTiming.LastFixedUpdate, typeof(PlayerLoopType.FixedUpdate) },
            { PlayerLoopTiming.PreUpdate, typeof(PlayerLoopType.PreUpdate) },
            { PlayerLoopTiming.LastPreUpdate, typeof(PlayerLoopType.PreUpdate) },
            { PlayerLoopTiming.Update, typeof(PlayerLoopType.Update) },
            { PlayerLoopTiming.LastUpdate, typeof(PlayerLoopType.Update) },
            { PlayerLoopTiming.PreLateUpdate, typeof(PlayerLoopType.PreLateUpdate) },
            { PlayerLoopTiming.LastPreLateUpdate, typeof(PlayerLoopType.PreLateUpdate) },
            { PlayerLoopTiming.PostLateUpdate, typeof(PlayerLoopType.PostLateUpdate) },
            { PlayerLoopTiming.LastPostLateUpdate, typeof(PlayerLoopType.PostLateUpdate) },
#if UNITY_2020_2_OR_NEWER
            { PlayerLoopTiming.TimeUpdate, typeof(PlayerLoopType.TimeUpdate) },
            { PlayerLoopTiming.LastTimeUpdate, typeof(PlayerLoopType.TimeUpdate) },
#endif
        };

#if UNITY_2020_1_OR_NEWER
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
#else
		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
#endif
        private static void Initialize()
        {
            // 为每个Unity相关阶段创建策略
            foreach (var timing in _unityLoopTypeMap.Keys)
            {
                var strategy = new UnityMainThreadPlayLoopStrategy();
                PlayerLoopHelper.SetStrategy(timing, strategy);
                _strategies[timing] = strategy;
            }

            // 修改PlayerLoop系统
            var playerLoop = PlayerLoop.GetCurrentPlayerLoop();
            InjectPlayerLoopSystems(ref playerLoop);
            PlayerLoop.SetPlayerLoop(playerLoop);

#if UNITY_EDITOR
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
#endif
        }

        private static void InjectPlayerLoopSystems(ref PlayerLoopSystem loopSystem)
        {
            var newSubSystems = new List<PlayerLoopSystem>(loopSystem.subSystemList);

            // 遍历所有顶层系统
            for (int i = 0; i < newSubSystems.Count; i++)
            {
                var system = newSubSystems[i];
                var subSystems = system.subSystemList != null 
                    ? new List<PlayerLoopSystem>(system.subSystemList) 
                    : new List<PlayerLoopSystem>();

                // 查找所有需要注入到这个系统的timing
                foreach (var kv in _unityLoopTypeMap)
                {
                    if (kv.Value != system.type) continue;

                    var timing = kv.Key;
                    if (!_strategies.TryGetValue(timing, out var strategy)) continue;

                    var playerLoopSystem = new PlayerLoopSystem
                    {
                        type = typeof(UnityPlayerLoopInjector),
                        updateDelegate = strategy.RunCore
                    };

                    // 根据是否是"Last"决定插入位置
                    if (timing.ToString().StartsWith("Last"))
                    {
                        subSystems.Add(playerLoopSystem);
                    }
                    else
                    {
                        subSystems.Insert(0, playerLoopSystem);
                    }
                }

                // 更新子系统列表
                if (subSystems.Count > 0)
                {
                    system.subSystemList = subSystems.ToArray();
                    newSubSystems[i] = system;
                }
            }

            loopSystem.subSystemList = newSubSystems.ToArray();
        }

#if UNITY_EDITOR
        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingPlayMode)
            {
                // 清理所有策略
                foreach (var strategy in _strategies.Values)
                {
                    strategy.Dispose();
                }
                _strategies.Clear();
            }
        }
#endif
    }
}