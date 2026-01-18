using System;
using System.Runtime.CompilerServices;
using System.Threading;
using UnityEngine;
using UnityEngine.Video;
using LuminThread.TaskSource;
using LuminThread.Utility;
using LuminThread.Interface;

namespace LuminThread.Unity
{
    /// <summary>
    /// Unity 内置组件的异步扩展
    /// </summary>
    public static class UnityComponentAsyncExtensions
    {
        #region AudioSource (音频播放)

        /// <summary>
        /// 播放音频并等待完成
        /// </summary>
        public static LuminTask PlayAsync(
            this AudioSource audioSource,
            CancellationToken cancellationToken = default,
            PlayerLoopTiming timing = PlayerLoopTiming.Update)
        {
            if (audioSource == null)
                throw new ArgumentNullException(nameof(audioSource));

            if (cancellationToken.IsCancellationRequested)
                return LuminTask.FromCanceled(cancellationToken);

            if (!audioSource.isPlaying)
            {
                audioSource.Play();
            }

            return AudioSourceConfiguredSource.Create(audioSource, timing, cancellationToken);
        }

        /// <summary>
        /// 播放指定的音频片段并等待完成
        /// </summary>
        public static LuminTask PlayAsync(
            this AudioSource audioSource,
            AudioClip clip,
            CancellationToken cancellationToken = default,
            PlayerLoopTiming timing = PlayerLoopTiming.Update)
        {
            if (audioSource == null)
                throw new ArgumentNullException(nameof(audioSource));
            if (clip == null)
                throw new ArgumentNullException(nameof(clip));

            if (cancellationToken.IsCancellationRequested)
                return LuminTask.FromCanceled(cancellationToken);

            audioSource.clip = clip;
            audioSource.Play();

            return AudioSourceConfiguredSource.Create(audioSource, timing, cancellationToken);
        }

        public unsafe struct AudioSourceConfiguredSource
        {
            struct PromiseState
            {
                public bool Completed;
            }

            public static LuminTask Create(
                AudioSource audioSource,
                PlayerLoopTiming timing,
                CancellationToken cancellationToken)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return LuminTask.FromCanceled(cancellationToken);
                }

                var core = LuminTaskSourceCore<AsyncUnit>.Create();

                var promiseState = new PromiseState { Completed = false };
                var stateTuple = StateTuple.Create(promiseState, audioSource);
                var state = new LuminTaskState(core, cancellationToken, stateTuple);

                // Register cancellation
                if (cancellationToken.CanBeCanceled)
                {
                    cancellationToken.Register(static s =>
                    {
                        var taskState = (LuminTaskState)s;
                        var tuple = Unsafe.As<StateTuple<PromiseState, AudioSource>>(taskState.StateTuple);
                        
                        // Stop audio on cancel
                        if (tuple.Item2 != null)
                        {
                            tuple.Item2.Stop();
                        }
                        
                        LuminTaskSourceCore<AsyncUnit>.TrySetCanceled(taskState.Source);
                    }, state);
                }

                // Add to player loop
                PlayerLoopHelper.AddAction(timing, state, &MoveNext);

                return new LuminTask(LuminTaskSourceCore<AsyncUnit>.MethodTable, core, core->Id);
            }

            static bool MoveNext(in LuminTaskState state)
            {
                var tuple = Unsafe.As<StateTuple<PromiseState, AudioSource>>(state.StateTuple);

                if (tuple.Item1.Completed)
                {
                    tuple.Dispose();
                    return false;
                }

                if (state.CancellationToken.IsCancellationRequested)
                {
                    tuple.Item1.Completed = true;
                    if (tuple.Item2 != null)
                    {
                        tuple.Item2.Stop();
                    }
                    LuminTaskSourceCore<AsyncUnit>.TrySetCanceled(state.Source);
                    tuple.Dispose();
                    return false;
                }

                // Check if audio is still playing
                if (tuple.Item2 == null || !tuple.Item2.isPlaying)
                {
                    tuple.Item1.Completed = true;
                    LuminTaskSourceCore<AsyncUnit>.TrySetResult(state.Source);
                    tuple.Dispose();
                    LuminTaskSourceCore<AsyncUnit>.Dispose(state.Source);
                    return false;
                }

                return true;
            }
        }

        #endregion

        #region Animator (动画控制)

        /// <summary>
        /// 等待当前动画状态播放完成
        /// </summary>
        public static LuminTask WaitForAnimationAsync(
            this Animator animator,
            int layer = 0,
            CancellationToken cancellationToken = default,
            PlayerLoopTiming timing = PlayerLoopTiming.Update)
        {
            if (animator == null)
                throw new ArgumentNullException(nameof(animator));

            if (cancellationToken.IsCancellationRequested)
                return LuminTask.FromCanceled(cancellationToken);

            return AnimatorConfiguredSource.Create(animator, layer, null, timing, cancellationToken);
        }

        /// <summary>
        /// 播放动画并等待完成
        /// </summary>
        public static LuminTask PlayAsync(
            this Animator animator,
            string stateName,
            int layer = 0,
            CancellationToken cancellationToken = default,
            PlayerLoopTiming timing = PlayerLoopTiming.Update)
        {
            if (animator == null)
                throw new ArgumentNullException(nameof(animator));
            if (string.IsNullOrEmpty(stateName))
                throw new ArgumentNullException(nameof(stateName));

            if (cancellationToken.IsCancellationRequested)
                return LuminTask.FromCanceled(cancellationToken);

            animator.Play(stateName, layer);

            return AnimatorConfiguredSource.Create(animator, layer, stateName, timing, cancellationToken);
        }

        public unsafe struct AnimatorConfiguredSource
        {
            struct PromiseState
            {
                public bool Completed;
                public int StateHash;
                public bool WaitingForState;
            }

            public static LuminTask Create(
                Animator animator,
                int layer,
                string stateName,
                PlayerLoopTiming timing,
                CancellationToken cancellationToken)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return LuminTask.FromCanceled(cancellationToken);
                }

                var core = LuminTaskSourceCore<AsyncUnit>.Create();

                var stateHash = string.IsNullOrEmpty(stateName) ? 0 : Animator.StringToHash(stateName);
                var promiseState = new PromiseState
                {
                    Completed = false,
                    StateHash = stateHash,
                    WaitingForState = !string.IsNullOrEmpty(stateName)
                };

                var stateTuple = StateTuple.Create(promiseState, animator, layer);
                var state = new LuminTaskState(core, cancellationToken, stateTuple);

                // Register cancellation
                if (cancellationToken.CanBeCanceled)
                {
                    cancellationToken.Register(static s =>
                    {
                        var taskState = (LuminTaskState)s;
                        LuminTaskSourceCore<AsyncUnit>.TrySetCanceled(taskState.Source);
                    }, state);
                }

                // Add to player loop
                PlayerLoopHelper.AddAction(timing, state, &MoveNext);

                return new LuminTask(LuminTaskSourceCore<AsyncUnit>.MethodTable, core, core->Id);
            }

            static bool MoveNext(in LuminTaskState state)
            {
                var tuple = Unsafe.As<StateTuple<PromiseState, Animator, int>>(state.StateTuple);

                if (tuple.Item1.Completed)
                {
                    tuple.Dispose();
                    return false;
                }

                if (state.CancellationToken.IsCancellationRequested)
                {
                    tuple.Item1.Completed = true;
                    LuminTaskSourceCore<AsyncUnit>.TrySetCanceled(state.Source);
                    tuple.Dispose();
                    return false;
                }

                if (tuple.Item2 == null)
                {
                    tuple.Item1.Completed = true;
                    LuminTaskSourceCore<AsyncUnit>.TrySetException(state.Source, new Exception("Animator is null"));
                    tuple.Dispose();
                    LuminTaskSourceCore<AsyncUnit>.Dispose(state.Source);
                    return false;
                }

                var animatorStateInfo = tuple.Item2.GetCurrentAnimatorStateInfo(tuple.Item3);

                // If waiting for specific state, check if we're in it
                if (tuple.Item1.WaitingForState)
                {
                    if (animatorStateInfo.shortNameHash == tuple.Item1.StateHash)
                    {
                        tuple.Item1.WaitingForState = false; // Now wait for it to complete
                    }
                    else
                    {
                        return true; // Keep waiting for state
                    }
                }

                // Check if animation is complete (normalized time >= 1)
                if (animatorStateInfo.normalizedTime >= 1.0f && !tuple.Item2.IsInTransition(tuple.Item3))
                {
                    tuple.Item1.Completed = true;
                    LuminTaskSourceCore<AsyncUnit>.TrySetResult(state.Source);
                    tuple.Dispose();
                    LuminTaskSourceCore<AsyncUnit>.Dispose(state.Source);
                    return false;
                }

                return true;
            }
        }

        #endregion

        #region ParticleSystem (粒子系统)

        /// <summary>
        /// 播放粒子系统并等待完成
        /// </summary>
        public static LuminTask PlayAsync(
            this ParticleSystem particleSystem,
            bool withChildren = true,
            CancellationToken cancellationToken = default,
            PlayerLoopTiming timing = PlayerLoopTiming.Update)
        {
            if (particleSystem == null)
                throw new ArgumentNullException(nameof(particleSystem));

            if (cancellationToken.IsCancellationRequested)
                return LuminTask.FromCanceled(cancellationToken);

            particleSystem.Play(withChildren);

            return ParticleSystemConfiguredSource.Create(particleSystem, withChildren, timing, cancellationToken);
        }

        public unsafe struct ParticleSystemConfiguredSource
        {
            struct PromiseState
            {
                public bool Completed;
                public bool WithChildren;
            }

            public static LuminTask Create(
                ParticleSystem particleSystem,
                bool withChildren,
                PlayerLoopTiming timing,
                CancellationToken cancellationToken)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return LuminTask.FromCanceled(cancellationToken);
                }

                var core = LuminTaskSourceCore<AsyncUnit>.Create();

                var promiseState = new PromiseState
                {
                    Completed = false,
                    WithChildren = withChildren
                };

                var stateTuple = StateTuple.Create(promiseState, particleSystem);
                var state = new LuminTaskState(core, cancellationToken, stateTuple);

                // Register cancellation
                if (cancellationToken.CanBeCanceled)
                {
                    cancellationToken.Register(static s =>
                    {
                        var taskState = (LuminTaskState)s;
                        var tuple = Unsafe.As<StateTuple<PromiseState, ParticleSystem>>(taskState.StateTuple);
                        
                        if (tuple.Item2 != null)
                        {
                            tuple.Item2.Stop(tuple.Item1.WithChildren);
                        }
                        
                        LuminTaskSourceCore<AsyncUnit>.TrySetCanceled(taskState.Source);
                    }, state);
                }

                // Add to player loop
                PlayerLoopHelper.AddAction(timing, state, &MoveNext);

                return new LuminTask(LuminTaskSourceCore<AsyncUnit>.MethodTable, core, core->Id);
            }

            static bool MoveNext(in LuminTaskState state)
            {
                var tuple = Unsafe.As<StateTuple<PromiseState, ParticleSystem>>(state.StateTuple);

                if (tuple.Item1.Completed)
                {
                    tuple.Dispose();
                    return false;
                }

                if (state.CancellationToken.IsCancellationRequested)
                {
                    tuple.Item1.Completed = true;
                    if (tuple.Item2 != null)
                    {
                        tuple.Item2.Stop(tuple.Item1.WithChildren);
                    }
                    LuminTaskSourceCore<AsyncUnit>.TrySetCanceled(state.Source);
                    tuple.Dispose();
                    return false;
                }

                if (tuple.Item2 == null)
                {
                    tuple.Item1.Completed = true;
                    LuminTaskSourceCore<AsyncUnit>.TrySetException(state.Source, new Exception("ParticleSystem is null"));
                    tuple.Dispose();
                    LuminTaskSourceCore<AsyncUnit>.Dispose(state.Source);
                    return false;
                }

                // Check if particle system has stopped
                if (!tuple.Item2.IsAlive(tuple.Item1.WithChildren))
                {
                    tuple.Item1.Completed = true;
                    LuminTaskSourceCore<AsyncUnit>.TrySetResult(state.Source);
                    tuple.Dispose();
                    LuminTaskSourceCore<AsyncUnit>.Dispose(state.Source);
                    return false;
                }

                return true;
            }
        }

        #endregion

        #region VideoPlayer (视频播放)

        /// <summary>
        /// 播放视频并等待完成
        /// </summary>
        public static LuminTask PlayAsync(
            this VideoPlayer videoPlayer,
            CancellationToken cancellationToken = default,
            PlayerLoopTiming timing = PlayerLoopTiming.Update)
        {
            if (videoPlayer == null)
                throw new ArgumentNullException(nameof(videoPlayer));

            if (cancellationToken.IsCancellationRequested)
                return LuminTask.FromCanceled(cancellationToken);

            if (!videoPlayer.isPlaying)
            {
                videoPlayer.Play();
            }

            return VideoPlayerConfiguredSource.Create(videoPlayer, timing, cancellationToken);
        }

        public unsafe struct VideoPlayerConfiguredSource
        {
            struct PromiseState
            {
                public bool Completed;
            }

            public static LuminTask Create(
                VideoPlayer videoPlayer,
                PlayerLoopTiming timing,
                CancellationToken cancellationToken)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return LuminTask.FromCanceled(cancellationToken);
                }

                var core = LuminTaskSourceCore<AsyncUnit>.Create();

                var promiseState = new PromiseState { Completed = false };
                var stateTuple = StateTuple.Create(promiseState, videoPlayer);
                var state = new LuminTaskState(core, cancellationToken, stateTuple);

                // Register cancellation
                if (cancellationToken.CanBeCanceled)
                {
                    cancellationToken.Register(static s =>
                    {
                        var taskState = (LuminTaskState)s;
                        var tuple = Unsafe.As<StateTuple<PromiseState, VideoPlayer>>(taskState.StateTuple);
                        
                        if (tuple.Item2 != null)
                        {
                            tuple.Item2.Stop();
                        }
                        
                        LuminTaskSourceCore<AsyncUnit>.TrySetCanceled(taskState.Source);
                    }, state);
                }

                // Setup completion callback
                VideoPlayer.EventHandler loopPointReached = _ => OnCompleted(state);
                videoPlayer.loopPointReached += loopPointReached;

                // Add to player loop for error checking
                PlayerLoopHelper.AddAction(timing, state, &MoveNext);

                return new LuminTask(LuminTaskSourceCore<AsyncUnit>.MethodTable, core, core->Id);
            }

            static bool MoveNext(in LuminTaskState state)
            {
                var tuple = Unsafe.As<StateTuple<PromiseState, VideoPlayer>>(state.StateTuple);

                if (tuple.Item1.Completed)
                {
                    tuple.Dispose();
                    return false;
                }

                if (state.CancellationToken.IsCancellationRequested)
                {
                    tuple.Item1.Completed = true;
                    if (tuple.Item2 != null)
                    {
                        tuple.Item2.Stop();
                    }
                    LuminTaskSourceCore<AsyncUnit>.TrySetCanceled(state.Source);
                    tuple.Dispose();
                    return false;
                }

                // Check for errors
                if (tuple.Item2 == null)
                {
                    tuple.Item1.Completed = true;
                    LuminTaskSourceCore<AsyncUnit>.TrySetException(state.Source, new Exception("VideoPlayer is null"));
                    tuple.Dispose();
                    LuminTaskSourceCore<AsyncUnit>.Dispose(state.Source);
                    return false;
                }

                return true;
            }

            static void OnCompleted(LuminTaskState state)
            {
                var tuple = Unsafe.As<StateTuple<PromiseState, VideoPlayer>>(state.StateTuple);

                if (tuple.Item1.Completed)
                {
                    return;
                }
                tuple.Item1.Completed = true;

                if (state.CancellationToken.IsCancellationRequested)
                {
                    LuminTaskSourceCore<AsyncUnit>.TrySetCanceled(state.Source);
                }
                else
                {
                    LuminTaskSourceCore<AsyncUnit>.TrySetResult(state.Source);
                }

                tuple.Dispose();
                LuminTaskSourceCore<AsyncUnit>.Dispose(state.Source);
            }
        }

        #endregion
        
    }
}