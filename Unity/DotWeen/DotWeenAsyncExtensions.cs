
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
    public enum TweenCancelBehaviour
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

    public static class DOTweenAsyncExtensions
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

        public static TweenAwaiter GetAwaiter(this Tween tween)
        {
            return new TweenAwaiter(tween);
        }

        public static LuminTask WithCancellation(this Tween tween, CancellationToken cancellationToken)
        {
            if (tween == null)
                throw new ArgumentNullException(nameof(tween));

            if (!tween.IsActive()) return LuminTask.CompletedTask();
            return TweenConfiguredSource.Create(tween, TweenCancelBehaviour.Kill, cancellationToken, CallbackType.Kill);
        }

        public static LuminTask ToLuminTask(this Tween tween, TweenCancelBehaviour tweenCancelBehaviour = TweenCancelBehaviour.Kill, CancellationToken cancellationToken = default)
        {
            if (tween == null)
                throw new ArgumentNullException(nameof(tween));

            if (!tween.IsActive()) return LuminTask.CompletedTask();
            return TweenConfiguredSource.Create(tween, tweenCancelBehaviour, cancellationToken, CallbackType.Kill);
        }

        public static LuminTask AwaitForComplete(this Tween tween, TweenCancelBehaviour tweenCancelBehaviour = TweenCancelBehaviour.Kill, CancellationToken cancellationToken = default)
        {
            if (tween == null)
                throw new ArgumentNullException(nameof(tween));

            if (!tween.IsActive()) return LuminTask.CompletedTask();
            return TweenConfiguredSource.Create(tween, tweenCancelBehaviour, cancellationToken, CallbackType.Complete);
        }

        public static LuminTask AwaitForPause(this Tween tween, TweenCancelBehaviour tweenCancelBehaviour = TweenCancelBehaviour.Kill, CancellationToken cancellationToken = default)
        {
            if (tween == null)
                throw new ArgumentNullException(nameof(tween));

            if (!tween.IsActive()) return LuminTask.CompletedTask();
            return TweenConfiguredSource.Create(tween, tweenCancelBehaviour, cancellationToken, CallbackType.Pause);
        }

        public static LuminTask AwaitForPlay(this Tween tween, TweenCancelBehaviour tweenCancelBehaviour = TweenCancelBehaviour.Kill, CancellationToken cancellationToken = default)
        {
            if (tween == null)
                throw new ArgumentNullException(nameof(tween));

            if (!tween.IsActive()) return LuminTask.CompletedTask();
            return TweenConfiguredSource.Create(tween, tweenCancelBehaviour, cancellationToken, CallbackType.Play);
        }

        public static LuminTask AwaitForRewind(this Tween tween, TweenCancelBehaviour tweenCancelBehaviour = TweenCancelBehaviour.Kill, CancellationToken cancellationToken = default)
        {
            if (tween == null)
                throw new ArgumentNullException(nameof(tween));

            if (!tween.IsActive()) return LuminTask.CompletedTask();
            return TweenConfiguredSource.Create(tween, tweenCancelBehaviour, cancellationToken, CallbackType.Rewind);
        }

        public static LuminTask AwaitForStepComplete(this Tween tween, TweenCancelBehaviour tweenCancelBehaviour = TweenCancelBehaviour.Kill, CancellationToken cancellationToken = default)
        {
            if (tween == null)
                throw new ArgumentNullException(nameof(tween));

            if (!tween.IsActive()) return LuminTask.CompletedTask();
            return TweenConfiguredSource.Create(tween, tweenCancelBehaviour, cancellationToken, CallbackType.StepComplete);
        }

        public struct TweenAwaiter : ICriticalNotifyCompletion
        {
            readonly Tween tween;

            // killed(non active) as completed.
            public bool IsCompleted => !tween.IsActive();

            public TweenAwaiter(Tween tween)
            {
                this.tween = tween;
            }

            public TweenAwaiter GetAwaiter()
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

        public unsafe struct TweenConfiguredSource
        {
            struct PromiseState
            {
                public bool Canceled;
            }

            public static LuminTask Create(Tween tween, TweenCancelBehaviour cancelBehaviour, CancellationToken cancellationToken, CallbackType callbackType)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    DoCancelBeforeCreate(tween, cancelBehaviour);
                    return LuminTask.FromCanceled(cancellationToken);
                }

                var core = LuminTaskSourceCore<AsyncUnit>.Create();
                
                var promiseState = new PromiseState
                {
                    Canceled = false
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
                var state = new LuminTaskState(core, cancellationToken, stateTuple);

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
                    cancellationToken.Register(static s =>
                    {
                        var taskState = (LuminTaskState)s;
                        var tuple = Unsafe.As<StateTuple<PromiseState, (Tween, TweenCancelBehaviour, CallbackType, TweenCallback)>>(taskState.StateTuple);
                        
                        var (tween, cancelBehaviour, callbackType, originalCallback) = tuple.Item2;
                        
                        switch (cancelBehaviour)
                        {
                            case TweenCancelBehaviour.Kill:
                            default:
                                tween.Kill(false);
                                break;
                            case TweenCancelBehaviour.KillAndCancelAwait:
                                tuple.Item1.Canceled = true;
                                tween.Kill(false);
                                break;
                            case TweenCancelBehaviour.KillWithCompleteCallback:
                                tween.Kill(true);
                                break;
                            case TweenCancelBehaviour.KillWithCompleteCallbackAndCancelAwait:
                                tuple.Item1.Canceled = true;
                                tween.Kill(true);
                                break;
                            case TweenCancelBehaviour.Complete:
                                tween.Complete(false);
                                break;
                            case TweenCancelBehaviour.CompleteAndCancelAwait:
                                tuple.Item1.Canceled = true;
                                tween.Complete(false);
                                break;
                            case TweenCancelBehaviour.CompleteWithSequenceCallback:
                                tween.Complete(true);
                                break;
                            case TweenCancelBehaviour.CompleteWithSequenceCallbackAndCancelAwait:
                                tuple.Item1.Canceled = true;
                                tween.Complete(true);
                                break;
                            case TweenCancelBehaviour.CancelAwait:
                                RestoreOriginalCallback(tween, callbackType, originalCallback);
                                LuminTaskSourceCore<AsyncUnit>.TrySetCanceled(taskState.Source);
                                break;
                        }
                    }, state);
                }

                return new LuminTask(LuminTaskSourceCore<AsyncUnit>.MethodTable, core, core->Id);
            }

            static void OnCompleted(LuminTaskState state)
            {
                var tuple = Unsafe.As<StateTuple<PromiseState, (Tween, TweenCancelBehaviour, CallbackType, TweenCallback)>>(state.StateTuple);
                
                var (tween, cancelBehaviour, callbackType, originalCallback) = tuple.Item2;
                
                if (state.CancellationToken.IsCancellationRequested)
                {
                    if (cancelBehaviour == TweenCancelBehaviour.KillAndCancelAwait
                        || cancelBehaviour == TweenCancelBehaviour.KillWithCompleteCallbackAndCancelAwait
                        || cancelBehaviour == TweenCancelBehaviour.CompleteAndCancelAwait
                        || cancelBehaviour == TweenCancelBehaviour.CompleteWithSequenceCallbackAndCancelAwait
                        || cancelBehaviour == TweenCancelBehaviour.CancelAwait)
                    {
                        tuple.Item1.Canceled = true;
                    }
                }
                
                if (tuple.Item1.Canceled)
                {
                    LuminTaskSourceCore<AsyncUnit>.TrySetCanceled(state.Source);
                }
                else
                {
                    originalCallback?.Invoke();
                    LuminTaskSourceCore<AsyncUnit>.TrySetResult(state.Source);
                }
                
                RestoreOriginalCallback(tween, callbackType, originalCallback);
                tuple.Dispose();
                LuminTaskSourceCore<AsyncUnit>.Dispose(state.Source);
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

            static void DoCancelBeforeCreate(Tween tween, TweenCancelBehaviour tweenCancelBehaviour)
            {
                switch (tweenCancelBehaviour)
                {
                    case TweenCancelBehaviour.Kill:
                    default:
                        tween.Kill(false);
                        break;
                    case TweenCancelBehaviour.KillAndCancelAwait:
                        tween.Kill(false);
                        break;
                    case TweenCancelBehaviour.KillWithCompleteCallback:
                        tween.Kill(true);
                        break;
                    case TweenCancelBehaviour.KillWithCompleteCallbackAndCancelAwait:
                        tween.Kill(true);
                        break;
                    case TweenCancelBehaviour.Complete:
                        tween.Complete(false);
                        break;
                    case TweenCancelBehaviour.CompleteAndCancelAwait:
                        tween.Complete(false);
                        break;
                    case TweenCancelBehaviour.CompleteWithSequenceCallback:
                        tween.Complete(true);
                        break;
                    case TweenCancelBehaviour.CompleteWithSequenceCallbackAndCancelAwait:
                        tween.Complete(true);
                        break;
                    case TweenCancelBehaviour.CancelAwait:
                        break;
                }
            }
        }
    }

    sealed class PooledTweenCallback
    {
        static readonly ConcurrentQueue<PooledTweenCallback> pool = new ConcurrentQueue<PooledTweenCallback>();

        readonly TweenCallback runDelegate;

        Action continuation;

        PooledTweenCallback()
        {
            runDelegate = Run;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TweenCallback Create(Action continuation)
        {
            if (!pool.TryDequeue(out var item))
            {
                item = new PooledTweenCallback();
            }

            item.continuation = continuation;
            return item.runDelegate;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void Run()
        {
            var call = continuation;
            continuation = null;
            if (call != null)
            {
                pool.Enqueue(this);
                call.Invoke();
            }
        }
    }
}
