
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
                public CancellationTokenRegistration Registration;
                public ulong Handle;
                public TweenCallback Callback;
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
                    Handle = handle
                };

                // Save original callback
                TweenCallback originalCallback = GetCallback(tween, callbackType);

                // Use tuple for more than 3 parameters
                var stateTuple = StateTuple.Create(
                    promiseState, 
                    (tween, cancelBehaviour, callbackType, originalCallback)
                );
                var state = new LuminTaskState(handle, cancellationToken, stateTuple);

                // Setup completion callback
                TweenCallback tweenCallback = () =>
                {
                    // An overlapping await may still hold this handler as its predecessor.
                    if (!Finish(state, true)) originalCallback?.Invoke();
                };
                stateTuple.Item.Item1.Callback = tweenCallback;

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
                    var registration = cancellationToken.Register(static boxed =>
                    {
                        var work = (LuminTaskState)boxed;
                        if (PlayerLoopHelper.IsMainThread) Cancel(work);
                        else DispatchCancel(work);
                    }, state);
                    if (stateTuple.Item.Item1.Handle == handle)
                        stateTuple.Item.Item1.Registration = registration;
                    else
                        registration.Dispose();
                }

                return task;
            }

            private static void DispatchCancel(LuminTaskState state) =>
                PlayerLoopHelper.AddContinuation(PlayerLoopTiming.Update, () => Cancel(state));

            private static void Cancel(LuminTaskState state)
            {
                var box = (StateTuple<(PromiseState, (Tween, TweenLuminTaskCancelBehaviour, CallbackType, TweenCallback))>)state.State!;
                if (box.Item.Item1.Handle != state.Handle) return;
                var (tween, behaviour, _, _) = box.Item.Item2;
                DoCancelBeforeCreate(tween, behaviour);
                // Kill/Complete may have delivered the selected callback synchronously.
                // Otherwise detach it and settle the waiter without synthesizing a tween event.
                Finish(state, false);
            }

            private static bool Finish(LuminTaskState state, bool raised)
            {
                var box = (StateTuple<(PromiseState, (Tween, TweenLuminTaskCancelBehaviour, CallbackType, TweenCallback))>)state.State!;
                if (box.Item.Item1.Handle != state.Handle) return false;
                var (tween, behaviour, callbackType, original) = box.Item.Item2;
                var canceled = state.CancellationToken.IsCancellationRequested &&
                    (behaviour == TweenLuminTaskCancelBehaviour.KillAndCancelAwait ||
                     behaviour == TweenLuminTaskCancelBehaviour.KillWithCompleteCallbackAndCancelAwait ||
                     behaviour == TweenLuminTaskCancelBehaviour.CompleteAndCancelAwait ||
                     behaviour == TweenLuminTaskCancelBehaviour.CompleteWithSequenceCallbackAndCancelAwait ||
                     behaviour == TweenLuminTaskCancelBehaviour.CancelAwait);
                box.Item.Item1.Handle = 0;
                if (GetCallback(tween, callbackType) == box.Item.Item1.Callback)
                    RestoreOriginalCallback(tween, callbackType, original);
                box.Item.Item1.Registration.Dispose();
                box.Dispose();
                if (canceled) LuminTaskSource.SetCanceled(state.Handle, state.CancellationToken);
                else
                {
                    try { if (raised) original?.Invoke(); }
                    catch (Exception error) { LuminTaskSource.SetException(state.Handle, error); throw; }
                    LuminTaskSource.SetResult(state.Handle);
                }
                return true;
            }

            private static TweenCallback GetCallback(Tween tween, CallbackType callbackType) => callbackType switch
                {
                    CallbackType.Kill => tween.onKill,
                    CallbackType.Complete => tween.onComplete,
                    CallbackType.Pause => tween.onPause,
                    CallbackType.Play => tween.onPlay,
                    CallbackType.Rewind => tween.onRewind,
                    CallbackType.StepComplete => tween.onStepComplete,
                    _ => null
                };

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
