
using DG.Tweening;
using System;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Threading;
using LuminThread.TaskSource;
using LuminThread.Utility;
using LuminThread.Interface;

namespace LuminThread.Unity
{
    public enum TweenLuminTaskCancelBehaviour
    {
        Kill,
        KillWithCompleteCallback,
        Complete,
        CompleteWithSequenceCallback,
        CancelAwait,

        // AndCancelAwait
        KillAndCancelAwait,
        KillWithCompleteCallbackAndCancelAwait,
        CompleteAndCancelAwait,
        CompleteWithSequenceCallbackAndCancelAwait
    }

    public static class DotWeenLuminTask
    {
        public enum CallbackType
        {
            Kill,
            Complete,
            Pause,
            Play,
            Rewind,
            StepComplete
        }

        public static TweenLuminTaskAwaiter GetAwaiter(this Tween tween)
        {
            return new TweenLuminTaskAwaiter(tween);
        }

        public static LuminTask WithCancellation(this Tween tween, CancellationToken cancellationToken)
        {
            if (tween == null)
                throw new ArgumentNullException(nameof(tween));

            if (!tween.IsActive()) return LuminTask.CompletedTask();
            return TweenLuminTaskConfiguredSource.Create(tween, TweenLuminTaskCancelBehaviour.Kill, cancellationToken, CallbackType.Kill);
        }

        public static LuminTask ToLuminTask(this Tween tween, TweenLuminTaskCancelBehaviour tweenCancelBehaviour = TweenLuminTaskCancelBehaviour.Kill, CancellationToken cancellationToken = default)
        {
            if (tween == null)
                throw new ArgumentNullException(nameof(tween));

            if (!tween.IsActive()) return LuminTask.CompletedTask();
            return TweenLuminTaskConfiguredSource.Create(tween, tweenCancelBehaviour, cancellationToken, CallbackType.Kill);
        }

        public static LuminTask AwaitForComplete(this Tween tween, TweenLuminTaskCancelBehaviour tweenCancelBehaviour = TweenLuminTaskCancelBehaviour.Kill, CancellationToken cancellationToken = default)
        {
            if (tween == null)
                throw new ArgumentNullException(nameof(tween));

            if (!tween.IsActive()) return LuminTask.CompletedTask();
            return TweenLuminTaskConfiguredSource.Create(tween, tweenCancelBehaviour, cancellationToken, CallbackType.Complete);
        }

        public static LuminTask AwaitForPause(this Tween tween, TweenLuminTaskCancelBehaviour tweenCancelBehaviour = TweenLuminTaskCancelBehaviour.Kill, CancellationToken cancellationToken = default)
        {
            if (tween == null)
                throw new ArgumentNullException(nameof(tween));

            if (!tween.IsActive()) return LuminTask.CompletedTask();
            return TweenLuminTaskConfiguredSource.Create(tween, tweenCancelBehaviour, cancellationToken, CallbackType.Pause);
        }

        public static LuminTask AwaitForPlay(this Tween tween, TweenLuminTaskCancelBehaviour tweenCancelBehaviour = TweenLuminTaskCancelBehaviour.Kill, CancellationToken cancellationToken = default)
        {
            if (tween == null)
                throw new ArgumentNullException(nameof(tween));

            if (!tween.IsActive()) return LuminTask.CompletedTask();
            return TweenLuminTaskConfiguredSource.Create(tween, tweenCancelBehaviour, cancellationToken, CallbackType.Play);
        }

        public static LuminTask AwaitForRewind(this Tween tween, TweenLuminTaskCancelBehaviour tweenCancelBehaviour = TweenLuminTaskCancelBehaviour.Kill, CancellationToken cancellationToken = default)
        {
            if (tween == null)
                throw new ArgumentNullException(nameof(tween));

            if (!tween.IsActive()) return LuminTask.CompletedTask();
            return TweenLuminTaskConfiguredSource.Create(tween, tweenCancelBehaviour, cancellationToken, CallbackType.Rewind);
        }

        public static LuminTask AwaitForStepComplete(this Tween tween, TweenLuminTaskCancelBehaviour tweenCancelBehaviour = TweenLuminTaskCancelBehaviour.Kill, CancellationToken cancellationToken = default)
        {
            if (tween == null)
                throw new ArgumentNullException(nameof(tween));

            if (!tween.IsActive()) return LuminTask.CompletedTask();
            return TweenLuminTaskConfiguredSource.Create(tween, tweenCancelBehaviour, cancellationToken, CallbackType.StepComplete);
        }

        public struct TweenLuminTaskAwaiter : ICriticalNotifyCompletion
        {
            readonly Tween tween;

            // killed(non active) as completed.
            public bool IsCompleted => !tween.IsActive();

            public TweenLuminTaskAwaiter(Tween tween)
            {
                this.tween = tween;
            }

            public TweenLuminTaskAwaiter GetAwaiter()
            {
                return this;
            }

            public void GetResult()
            {
            }

            public void OnCompleted(Action continuation)
            {
                UnsafeOnCompleted(continuation);
            }

            public void UnsafeOnCompleted(Action continuation)
            {
                // onKill is called after OnCompleted, both Complete(false/true) and Kill(false/true).
                tween.onKill = PooledTweenCallback.Create(continuation);
            }
        }

        public struct TweenLuminTaskConfiguredSource
        {
            struct PromiseState
            {
                public bool Canceled;
                public CancellationTokenRegistration Registration;
                public ulong Handle;
            }

            public static LuminTask Create(Tween tween, TweenLuminTaskCancelBehaviour cancelBehaviour, CancellationToken cancellationToken, CallbackType callbackType)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    DoCancelBeforeCreate(tween, cancelBehaviour);
                    return LuminTask.FromCanceled(cancellationToken);
                }

                var task = LuminTaskSource.Create(out ulong handle);
                
                var promiseState = new PromiseState
                {
                    Canceled = false,
                    Handle = handle
                };

                // Save original callback
                TweenCallback originalCallback = callbackType switch
                {
                    CallbackType.Kill => tween.onKill,
                    CallbackType.Complete => tween.onComplete,
                    CallbackType.Pause => tween.onPause,
                    CallbackType.Play => tween.onPlay,
                    CallbackType.Rewind => tween.onRewind,
                    CallbackType.StepComplete => tween.onStepComplete,
                    _ => null
                };

                // Use tuple for more than 3 parameters
                var stateTuple = StateTuple.Create(
                    promiseState, 
                    (tween, cancelBehaviour, callbackType, originalCallback)
                );
                var state = new LuminTaskState(handle, cancellationToken, stateTuple);

                // Setup completion callback
                Action onCompleteCallback = () => OnCompleted(state);
                TweenCallback tweenCallback = new TweenCallback(onCompleteCallback);

                switch (callbackType)
                {
                    case CallbackType.Kill:
                        tween.onKill = tweenCallback;
                        break;
                    case CallbackType.Complete:
                        tween.onComplete = tweenCallback;
                        break;
                    case CallbackType.Pause:
                        tween.onPause = tweenCallback;
                        break;
                    case CallbackType.Play:
                        tween.onPlay = tweenCallback;
                        break;
                    case CallbackType.Rewind:
                        tween.onRewind = tweenCallback;
                        break;
                    case CallbackType.StepComplete:
                        tween.onStepComplete = tweenCallback;
                        break;
                }

                // Register cancellation
                if (cancellationToken.CanBeCanceled)
                {
                    stateTuple.Item.Item1.Registration = cancellationToken.Register(static s =>
                    {
                        var taskState = (LuminTaskState)s;
                        var tuple = (StateTuple<(PromiseState, (Tween, TweenLuminTaskCancelBehaviour, CallbackType, TweenCallback))>)taskState.State!;
                        
                        var (tween, cancelBehaviour, callbackType, originalCallback) = tuple.Item.Item2;
                        
                        switch (cancelBehaviour)
                        {
                            case TweenLuminTaskCancelBehaviour.Kill:
                            default:
                                tween.Kill(false);
                                break;
                            case TweenLuminTaskCancelBehaviour.KillAndCancelAwait:
                                tuple.Item.Item1.Canceled = true;
                                tween.Kill(false);
                                break;
                            case TweenLuminTaskCancelBehaviour.KillWithCompleteCallback:
                                tween.Kill(true);
                                break;
                            case TweenLuminTaskCancelBehaviour.KillWithCompleteCallbackAndCancelAwait:
                                tuple.Item.Item1.Canceled = true;
                                tween.Kill(true);
                                break;
                            case TweenLuminTaskCancelBehaviour.Complete:
                                tween.Complete(false);
                                break;
                            case TweenLuminTaskCancelBehaviour.CompleteAndCancelAwait:
                                tuple.Item.Item1.Canceled = true;
                                tween.Complete(false);
                                break;
                            case TweenLuminTaskCancelBehaviour.CompleteWithSequenceCallback:
                                tween.Complete(true);
                                break;
                            case TweenLuminTaskCancelBehaviour.CompleteWithSequenceCallbackAndCancelAwait:
                                tuple.Item.Item1.Canceled = true;
                                tween.Complete(true);
                                break;
                            case TweenLuminTaskCancelBehaviour.CancelAwait:
                                RestoreOriginalCallback(tween, callbackType, originalCallback);
                                LuminTaskSource.SetCanceled(tuple.Item.Item1.Handle, taskState.CancellationToken);
                                break;
                        }

                        // 回调自身在途：仅清引用，释放交给 OnCompleted。
                        tuple.Item.Item1.Registration = default;
                    }, state);
                }

                return task;
            }

            static void OnCompleted(LuminTaskState state)
            {
                var tuple = (StateTuple<(PromiseState, (Tween, TweenLuminTaskCancelBehaviour, CallbackType, TweenCallback))>)state.State!;
                
                var (tween, cancelBehaviour, callbackType, originalCallback) = tuple.Item.Item2;
                
                if (state.CancellationToken.IsCancellationRequested)
                {
                    if (cancelBehaviour == TweenLuminTaskCancelBehaviour.KillAndCancelAwait
                        || cancelBehaviour == TweenLuminTaskCancelBehaviour.KillWithCompleteCallbackAndCancelAwait
                        || cancelBehaviour == TweenLuminTaskCancelBehaviour.CompleteAndCancelAwait
                        || cancelBehaviour == TweenLuminTaskCancelBehaviour.CompleteWithSequenceCallbackAndCancelAwait
                        || cancelBehaviour == TweenLuminTaskCancelBehaviour.CancelAwait)
                    {
                        tuple.Item.Item1.Canceled = true;
                    }
                }
                
                if (tuple.Item.Item1.Canceled)
                {
                    LuminTaskSource.SetCanceled(tuple.Item.Item1.Handle, state.CancellationToken);
                }
                else
                {
                    originalCallback?.Invoke();
                    LuminTaskSource.SetResult(tuple.Item.Item1.Handle);
                }
                
                RestoreOriginalCallback(tween, callbackType, originalCallback);
                tuple.Item.Item1.Registration.Dispose();
                tuple.Dispose();
            }

            static void RestoreOriginalCallback(Tween tween, CallbackType callbackType, TweenCallback originalCallback)
            {
                switch (callbackType)
                {
                    case CallbackType.Kill:
                        tween.onKill = originalCallback;
                        break;
                    case CallbackType.Complete:
                        tween.onComplete = originalCallback;
                        break;
                    case CallbackType.Pause:
                        tween.onPause = originalCallback;
                        break;
                    case CallbackType.Play:
                        tween.onPlay = originalCallback;
                        break;
                    case CallbackType.Rewind:
                        tween.onRewind = originalCallback;
                        break;
                    case CallbackType.StepComplete:
                        tween.onStepComplete = originalCallback;
                        break;
                }
            }

            static void DoCancelBeforeCreate(Tween tween, TweenLuminTaskCancelBehaviour tweenCancelBehaviour)
            {
                switch (tweenCancelBehaviour)
                {
                    case TweenLuminTaskCancelBehaviour.Kill:
                    default:
                        tween.Kill(false);
                        break;
                    case TweenLuminTaskCancelBehaviour.KillAndCancelAwait:
                        tween.Kill(false);
                        break;
                    case TweenLuminTaskCancelBehaviour.KillWithCompleteCallback:
                        tween.Kill(true);
                        break;
                    case TweenLuminTaskCancelBehaviour.KillWithCompleteCallbackAndCancelAwait:
                        tween.Kill(true);
                        break;
                    case TweenLuminTaskCancelBehaviour.Complete:
                        tween.Complete(false);
                        break;
                    case TweenLuminTaskCancelBehaviour.CompleteAndCancelAwait:
                        tween.Complete(false);
                        break;
                    case TweenLuminTaskCancelBehaviour.CompleteWithSequenceCallback:
                        tween.Complete(true);
                        break;
                    case TweenLuminTaskCancelBehaviour.CompleteWithSequenceCallbackAndCancelAwait:
                        tween.Complete(true);
                        break;
                    case TweenLuminTaskCancelBehaviour.CancelAwait:
                        break;
                }
            }
        }
    }

    sealed class PooledTweenCallback : ILuminCacheNode
    {
        public ILuminCacheNode CacheNext { get; set; }
        private Action continuation;
        readonly TweenCallback runDelegate;

        PooledTweenCallback()
        {
            runDelegate = Run;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TweenCallback Create(Action continuation)
        {
            var item = (PooledTweenCallback)LuminTaskIdleCache.Take(typeof(PooledTweenCallback)) ?? new PooledTweenCallback();
            item.continuation = continuation;
            return item.runDelegate;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void Run()
        {
            var call = continuation;
            continuation = null;

            LuminTaskIdleCache.Return(this, 144);
            call?.Invoke();
        }
    }
}
