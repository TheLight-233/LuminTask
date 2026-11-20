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
    public static class UnityComponentAsyncLuminTask
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

            return AudioSourceLuminTaskConfiguredSource.Create(audioSource, timing, cancellationToken);
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

            return AudioSourceLuminTaskConfiguredSource.Create(audioSource, timing, cancellationToken);
        }

        public struct AudioSourceLuminTaskConfiguredSource
        {
            struct PromiseState
            {
                public bool Completed;
                public CancellationTokenRegistration Registration;
                public ulong Handle;
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

                var task = LuminTaskSource.Create(out ulong handle);

                var promiseState = new PromiseState { Completed = false, Handle = handle };
                var stateTuple = StateTuple.Create(promiseState, audioSource);
                var state = new LuminTaskState(handle, cancellationToken, stateTuple);

                // Register cancellation
                if (cancellationToken.CanBeCanceled)
                {
                    stateTuple.Item.Item1.Registration = UnityLoopWork.RegisterCancellation(state.Handle, cancellationToken);
                }

                // Add to player loop
                UnityLoopWork.Add(timing, state, MoveNextAction, Release);

                return task;
            }

            private static readonly Action<object> Release = state =>
            {
                var tuple = (StateTuple<(PromiseState, AudioSource)>)state;
                tuple.Item.Item1.Registration.Dispose();
                tuple.Dispose();
            };

            private static readonly MoveNext MoveNextAction = MoveNext;

            static bool MoveNext(in LuminTaskState state)
            {
                var tuple = (StateTuple<(PromiseState, AudioSource)>)state.State!;
                bool keep = false;
                try
                {
                    if (tuple.Item.Item1.Completed) return false;

                    if (state.CancellationToken.IsCancellationRequested)
                    {
                        tuple.Item.Item1.Completed = true;
                        if (tuple.Item.Item2 != null)
                        {
                            tuple.Item.Item2.Stop();
                        }
                        LuminTaskSource.SetCanceled(tuple.Item.Item1.Handle, state.CancellationToken);
                        return false;
                    }

                    // Check if audio is still playing
                    if (tuple.Item.Item2 == null || !tuple.Item.Item2.isPlaying)
                    {
                        tuple.Item.Item1.Completed = true;
                        LuminTaskSource.SetResult(tuple.Item.Item1.Handle);
                        return false;
                    }

                    keep = true;
                    return true;
                }
                finally { if (!keep) Release(tuple); }
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

            return AnimatorLuminTaskConfiguredSource.Create(animator, layer, null, timing, cancellationToken);
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

            return AnimatorLuminTaskConfiguredSource.Create(animator, layer, stateName, timing, cancellationToken);
        }

        public struct AnimatorLuminTaskConfiguredSource
        {
            struct PromiseState
            {
                public bool Completed;
                public int StateHash;
                public bool WaitingForState;
                public CancellationTokenRegistration Registration;
                public ulong Handle;
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

                var task = LuminTaskSource.Create(out ulong handle);

                var stateHash = string.IsNullOrEmpty(stateName) ? 0 : Animator.StringToHash(stateName);
                var promiseState = new PromiseState
                {
                    Completed = false,
                    StateHash = stateHash,
                    WaitingForState = !string.IsNullOrEmpty(stateName),
                    Handle = handle
                };

                var stateTuple = StateTuple.Create(promiseState, animator, layer);
                var state = new LuminTaskState(handle, cancellationToken, stateTuple);

                // Register cancellation
                if (cancellationToken.CanBeCanceled)
                {
                    stateTuple.Item.Item1.Registration = UnityLoopWork.RegisterCancellation(state.Handle, cancellationToken);
                }

                // Add to player loop
                UnityLoopWork.Add(timing, state, MoveNextAction, Release);

                return task;
            }

            private static readonly Action<object> Release = state =>
            {
                var tuple = (StateTuple<(PromiseState, Animator, int)>)state;
                tuple.Item.Item1.Registration.Dispose();
                tuple.Dispose();
            };

            private static readonly MoveNext MoveNextAction = MoveNext;

            static bool MoveNext(in LuminTaskState state)
            {
                var tuple = (StateTuple<(PromiseState, Animator, int)>)state.State!;
                bool keep = false;
                try
                {
                    if (tuple.Item.Item1.Completed) return false;

                    if (state.CancellationToken.IsCancellationRequested)
                    {
                        tuple.Item.Item1.Completed = true;
                        LuminTaskSource.SetCanceled(tuple.Item.Item1.Handle, state.CancellationToken);
                        return false;
                    }

                    if (tuple.Item.Item2 == null)
                    {
                        tuple.Item.Item1.Completed = true;
                        LuminTaskSource.SetException(tuple.Item.Item1.Handle, new Exception("Animator is null"));
                        return false;
                    }

                    var animatorStateInfo = tuple.Item.Item2.GetCurrentAnimatorStateInfo(tuple.Item.Item3);

                    // If waiting for specific state, check if we're in it
                    if (tuple.Item.Item1.WaitingForState)
                    {
                        if (animatorStateInfo.shortNameHash == tuple.Item.Item1.StateHash)
                        {
                            tuple.Item.Item1.WaitingForState = false; // Now wait for it to complete
                        }
                        else
                        {
                            keep = true;
                            return true; // Keep waiting for state
                        }
                    }

                    // Check if animation is complete (normalized time >= 1)
                    if (animatorStateInfo.normalizedTime >= 1.0f && !tuple.Item.Item2.IsInTransition(tuple.Item.Item3))
                    {
                        tuple.Item.Item1.Completed = true;
                        LuminTaskSource.SetResult(tuple.Item.Item1.Handle);
                        return false;
                    }

                    keep = true;
                    return true;
                }
                finally { if (!keep) Release(tuple); }
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

            return ParticleSystemLuminTaskConfiguredSource.Create(particleSystem, withChildren, timing, cancellationToken);
        }

        public struct ParticleSystemLuminTaskConfiguredSource
        {
            struct PromiseState
            {
                public bool Completed;
                public bool WithChildren;
                public CancellationTokenRegistration Registration;
                public ulong Handle;
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

                var task = LuminTaskSource.Create(out ulong handle);

                var promiseState = new PromiseState
                {
                    Completed = false,
                    WithChildren = withChildren,
                    Handle = handle
                };

                var stateTuple = StateTuple.Create(promiseState, particleSystem);
                var state = new LuminTaskState(handle, cancellationToken, stateTuple);

                // Register cancellation
                if (cancellationToken.CanBeCanceled)
                {
                    stateTuple.Item.Item1.Registration = UnityLoopWork.RegisterCancellation(state.Handle, cancellationToken);
                }

                // Add to player loop
                UnityLoopWork.Add(timing, state, MoveNextAction, Release);

                return task;
            }

            private static readonly Action<object> Release = state =>
            {
                var tuple = (StateTuple<(PromiseState, ParticleSystem)>)state;
                tuple.Item.Item1.Registration.Dispose();
                tuple.Dispose();
            };

            private static readonly MoveNext MoveNextAction = MoveNext;

            static bool MoveNext(in LuminTaskState state)
            {
                var tuple = (StateTuple<(PromiseState, ParticleSystem)>)state.State!;
                bool keep = false;
                try
                {
                    if (tuple.Item.Item1.Completed) return false;

                    if (state.CancellationToken.IsCancellationRequested)
                    {
                        tuple.Item.Item1.Completed = true;
                        if (tuple.Item.Item2 != null)
                        {
                            tuple.Item.Item2.Stop(tuple.Item.Item1.WithChildren);
                        }
                        LuminTaskSource.SetCanceled(tuple.Item.Item1.Handle, state.CancellationToken);
                        return false;
                    }

                    if (tuple.Item.Item2 == null)
                    {
                        tuple.Item.Item1.Completed = true;
                        LuminTaskSource.SetException(tuple.Item.Item1.Handle, new Exception("ParticleSystem is null"));
                        return false;
                    }

                    // Check if particle system has stopped
                    if (!tuple.Item.Item2.IsAlive(tuple.Item.Item1.WithChildren))
                    {
                        tuple.Item.Item1.Completed = true;
                        LuminTaskSource.SetResult(tuple.Item.Item1.Handle);
                        return false;
                    }

                    keep = true;
                    return true;
                }
                finally { if (!keep) Release(tuple); }
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

            return VideoPlayerLuminTaskConfiguredSource.Create(videoPlayer, timing, cancellationToken);
        }

        public struct VideoPlayerLuminTaskConfiguredSource
        {
            struct PromiseState
            {
                public bool Completed;
                public VideoPlayer.EventHandler Callback;
                public CancellationTokenRegistration Registration;
                public ulong Handle;
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

                var task = LuminTaskSource.Create(out ulong handle);

                var promiseState = new PromiseState { Completed = false, Handle = handle };
                var stateTuple = StateTuple.Create(promiseState, videoPlayer);
                var state = new LuminTaskState(handle, cancellationToken, stateTuple);

                // Register cancellation
                if (cancellationToken.CanBeCanceled)
                {
                    stateTuple.Item.Item1.Registration = UnityLoopWork.RegisterCancellation(state.Handle, cancellationToken);
                }

                // Setup completion callback
                stateTuple.Item.Item1.Callback = _ => OnCompleted(state);
                videoPlayer.loopPointReached += stateTuple.Item.Item1.Callback;

                // Add to player loop for error checking
                UnityLoopWork.Add(timing, state, MoveNextAction, Release);

                return task;
            }

            private static readonly Action<object> Release = state =>
            {
                var tuple = (StateTuple<(PromiseState, VideoPlayer)>)state;
                if (tuple.Item.Item2 != null) tuple.Item.Item2.loopPointReached -= tuple.Item.Item1.Callback;
                tuple.Item.Item1.Registration.Dispose();
                tuple.Dispose();
            };

            private static readonly MoveNext MoveNextAction = MoveNext;

            static bool MoveNext(in LuminTaskState state)
            {
                var tuple = (StateTuple<(PromiseState, VideoPlayer)>)state.State!;
                bool keep = false;
                try
                {
                    if (tuple.Item.Item1.Completed) return false;

                    if (state.CancellationToken.IsCancellationRequested)
                    {
                        tuple.Item.Item1.Completed = true;
                        if (tuple.Item.Item2 != null)
                        {
                            tuple.Item.Item2.Stop();
                        }
                        LuminTaskSource.SetCanceled(tuple.Item.Item1.Handle, state.CancellationToken);
                        return false;
                    }

                    // Check for errors
                    if (tuple.Item.Item2 == null)
                    {
                        tuple.Item.Item1.Completed = true;
                        LuminTaskSource.SetException(tuple.Item.Item1.Handle, new Exception("VideoPlayer is null"));
                        return false;
                    }

                    keep = true;
                    return true;
                }
                finally { if (!keep) Release(tuple); }
            }

            static void OnCompleted(LuminTaskState state)
            {
                var tuple = (StateTuple<(PromiseState, VideoPlayer)>)state.State!;

                if (tuple.Item.Item1.Handle != state.Handle || tuple.Item.Item1.Completed)
                {
                    return;
                }
                tuple.Item.Item1.Completed = true;

                if (state.CancellationToken.IsCancellationRequested)
                {
                    LuminTaskSource.SetCanceled(tuple.Item.Item1.Handle, state.CancellationToken);
                }
                else
                {
                    LuminTaskSource.SetResult(tuple.Item.Item1.Handle);
                }

            }
        }

        #endregion
        
    }
}