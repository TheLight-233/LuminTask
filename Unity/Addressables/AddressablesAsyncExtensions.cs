
using System;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Threading;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using LuminThread.TaskSource;
using LuminThread.Utility;
using LuminThread.Interface;

namespace LuminThread.Unity
{
    public static class AddressablesAsyncExtensions
    {
        #region AsyncOperationHandle

        public static LuminTaskAwaiter GetAwaiter(this AsyncOperationHandle handle)
        {
            return ToLuminTask(handle).GetAwaiter();
        }

        public static LuminTask WithCancellation(this AsyncOperationHandle handle, CancellationToken cancellationToken, bool cancelImmediately = false, bool autoReleaseWhenCanceled = false)
        {
            return ToLuminTask(handle, cancellationToken: cancellationToken, cancelImmediately: cancelImmediately, autoReleaseWhenCanceled: autoReleaseWhenCanceled);
        }

        public static LuminTask ToLuminTask(this AsyncOperationHandle handle, IProgress<float> progress = null, PlayerLoopTiming timing = PlayerLoopTiming.Update, CancellationToken cancellationToken = default, bool cancelImmediately = false, bool autoReleaseWhenCanceled = false)
        {
            if (cancellationToken.IsCancellationRequested) return LuminTask.FromCanceled(cancellationToken);

            if (!handle.IsValid())
            {
                // autoReleaseHandle:true handle is invalid(immediately internal handle == null) so return completed.
                return LuminTask.CompletedTask();
            }

            if (handle.IsDone)
            {
                if (handle.Status == AsyncOperationStatus.Failed)
                {
                    return LuminTask.FromException(handle.OperationException);
                }
                return LuminTask.CompletedTask();
            }

            return AsyncOperationHandleConfiguredSource.Create(handle, timing, progress, cancellationToken, cancelImmediately, autoReleaseWhenCanceled);
        }

        public struct AsyncOperationHandleAwaiter : ICriticalNotifyCompletion
        {
            AsyncOperationHandle handle;
            Action<AsyncOperationHandle> continuationAction;

            public AsyncOperationHandleAwaiter(AsyncOperationHandle handle)
            {
                this.handle = handle;
                this.continuationAction = null;
            }

            public bool IsCompleted => handle.IsDone;

            public void GetResult()
            {
                if (continuationAction != null)
                {
                    handle.Completed -= continuationAction;
                    continuationAction = null;
                }

                if (handle.Status == AsyncOperationStatus.Failed)
                {
                    var e = handle.OperationException;
                    handle = default;
                    ExceptionDispatchInfo.Capture(e).Throw();
                }

                var result = handle.Result;
                handle = default;
            }

            public void OnCompleted(Action continuation)
            {
                UnsafeOnCompleted(continuation);
            }

            public void UnsafeOnCompleted(Action continuation)
            {
                if (continuationAction != null)
                    throw new InvalidOperationException("Continuation is already registered");
                    
                continuationAction = PooledDelegate<AsyncOperationHandle>.Create(continuation);
                handle.Completed += continuationAction;
            }
        }

        public unsafe struct AsyncOperationHandleConfiguredSource
        {
            struct PromiseState
            {
                public bool Completed;
                public bool CancelImmediately;
                public bool AutoReleaseWhenCanceled;
            }

            public static LuminTask Create(AsyncOperationHandle handle, PlayerLoopTiming timing, IProgress<float> progress, CancellationToken cancellationToken, bool cancelImmediately, bool autoReleaseWhenCanceled)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return LuminTask.FromCanceled(cancellationToken);
                }

                var core = LuminTaskSourceCore<AsyncUnit>.Create();
                
                var promiseState = new PromiseState
                {
                    Completed = false,
                    CancelImmediately = cancelImmediately,
                    AutoReleaseWhenCanceled = autoReleaseWhenCanceled
                };

                var stateTuple = StateTuple.Create(promiseState, handle, progress);
                var state = new LuminTaskState(core, cancellationToken, stateTuple);

                // Register cancellation if needed
                if (cancelImmediately && cancellationToken.CanBeCanceled)
                {
                    cancellationToken.Register(static s =>
                    {
                        var taskState = (LuminTaskState)s;
                        var tuple = Unsafe.As<StateTuple<PromiseState, AsyncOperationHandle, IProgress<float>>>(taskState.StateTuple);
                        
                        if (tuple.Item1.AutoReleaseWhenCanceled && tuple.Item2.IsValid())
                        {
                            Addressables.Release(tuple.Item2);
                        }
                        LuminTaskSourceCore<AsyncUnit>.TrySetCanceled(taskState.Source);
                    }, state);
                }

                // Setup completion callback
                Action<AsyncOperationHandle> completedCallback = _ => OnCompleted(state);
                handle.Completed += completedCallback;

                // Add to player loop for progress tracking
                PlayerLoopHelper.AddAction(timing, state, &MoveNext);

                return new LuminTask(LuminTaskSourceCore<AsyncUnit>.MethodTable, core, core->Id);
            }

            static bool MoveNext(in LuminTaskState state)
            {
                var tuple = Unsafe.As<StateTuple<PromiseState, AsyncOperationHandle, IProgress<float>>>(state.StateTuple);
                
                if (tuple.Item1.Completed)
                {
                    tuple.Dispose();
                    return false;
                }

                if (state.CancellationToken.IsCancellationRequested)
                {
                    tuple.Item1.Completed = true;
                    if (tuple.Item1.AutoReleaseWhenCanceled && tuple.Item2.IsValid())
                    {
                        Addressables.Release(tuple.Item2);
                    }
                    LuminTaskSourceCore<AsyncUnit>.TrySetCanceled(state.Source);
                    tuple.Dispose();
                    return false;
                }

                // Report progress
                if (tuple.Item3 != null && tuple.Item2.IsValid())
                {
                    tuple.Item3.Report(tuple.Item2.GetDownloadStatus().Percent);
                }

                return true;
            }

            static void OnCompleted(LuminTaskState state)
            {
                var tuple = Unsafe.As<StateTuple<PromiseState, AsyncOperationHandle, IProgress<float>>>(state.StateTuple);
                
                if (tuple.Item1.Completed)
                {
                    return;
                }

                tuple.Item1.Completed = true;

                if (state.CancellationToken.IsCancellationRequested)
                {
                    if (tuple.Item1.AutoReleaseWhenCanceled && tuple.Item2.IsValid())
                    {
                        Addressables.Release(tuple.Item2);
                    }
                    LuminTaskSourceCore<AsyncUnit>.TrySetCanceled(state.Source);
                }
                else if (tuple.Item2.Status == AsyncOperationStatus.Failed)
                {
                    LuminTaskSourceCore<AsyncUnit>.TrySetException(state.Source, tuple.Item2.OperationException);
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

        #region AsyncOperationHandle<T>

        public static LuminTaskAwaiter<T> GetAwaiter<T>(this AsyncOperationHandle<T> handle)
        {
            return ToLuminTask(handle).GetAwaiter();
        }

        public static LuminTask<T> WithCancellation<T>(this AsyncOperationHandle<T> handle, CancellationToken cancellationToken, bool cancelImmediately = false, bool autoReleaseWhenCanceled = false)
        {
            return ToLuminTask(handle, cancellationToken: cancellationToken, cancelImmediately: cancelImmediately, autoReleaseWhenCanceled: autoReleaseWhenCanceled);
        }

        public static LuminTask<T> ToLuminTask<T>(this AsyncOperationHandle<T> handle, IProgress<float> progress = null, PlayerLoopTiming timing = PlayerLoopTiming.Update, CancellationToken cancellationToken = default, bool cancelImmediately = false, bool autoReleaseWhenCanceled = false)
        {
            if (cancellationToken.IsCancellationRequested) return LuminTask.FromCanceled<T>(cancellationToken);

            if (!handle.IsValid())
            {
                throw new Exception("Attempting to use an invalid operation handle");
            }

            if (handle.IsDone)
            {
                if (handle.Status == AsyncOperationStatus.Failed)
                {
                    return LuminTask.FromException<T>(handle.OperationException);
                }
                return LuminTask.FromResult(handle.Result);
            }

            return AsyncOperationHandleConfiguredSource<T>.Create(handle, timing, progress, cancellationToken, cancelImmediately, autoReleaseWhenCanceled);
        }

        public unsafe struct AsyncOperationHandleConfiguredSource<T>
        {
            struct PromiseState
            {
                public bool Completed;
                public bool CancelImmediately;
                public bool AutoReleaseWhenCanceled;
            }

            public static LuminTask<T> Create(AsyncOperationHandle<T> handle, PlayerLoopTiming timing, IProgress<float> progress, CancellationToken cancellationToken, bool cancelImmediately, bool autoReleaseWhenCanceled)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return LuminTask.FromCanceled<T>(cancellationToken);
                }

                var core = LuminTaskSourceCore<T>.Create();
                
                var promiseState = new PromiseState
                {
                    Completed = false,
                    CancelImmediately = cancelImmediately,
                    AutoReleaseWhenCanceled = autoReleaseWhenCanceled
                };

                var stateTuple = StateTuple.Create(promiseState, handle, progress);
                var state = new LuminTaskState(core, cancellationToken, stateTuple);

                // Register cancellation if needed
                if (cancelImmediately && cancellationToken.CanBeCanceled)
                {
                    cancellationToken.Register(static s =>
                    {
                        var taskState = (LuminTaskState)s;
                        var tuple = Unsafe.As<StateTuple<PromiseState, AsyncOperationHandle<T>, IProgress<float>>>(taskState.StateTuple);
                        
                        if (tuple.Item1.AutoReleaseWhenCanceled && tuple.Item2.IsValid())
                        {
                            Addressables.Release(tuple.Item2);
                        }
                        LuminTaskSourceCore<T>.TrySetCanceled(taskState.Source);
                    }, state);
                }

                // Setup completion callback
                Action<AsyncOperationHandle<T>> completedCallback = _ => OnCompleted(state);
                handle.Completed += completedCallback;

                // Add to player loop for progress tracking
                PlayerLoopHelper.AddAction(timing, state, &MoveNext);

                return new LuminTask<T>(LuminTaskSourceCore<T>.MethodTable, core, core->Id);
            }

            static bool MoveNext(in LuminTaskState state)
            {
                var tuple = Unsafe.As<StateTuple<PromiseState, AsyncOperationHandle<T>, IProgress<float>>>(state.StateTuple);
                
                if (tuple.Item1.Completed)
                {
                    tuple.Dispose();
                    return false;
                }

                if (state.CancellationToken.IsCancellationRequested)
                {
                    tuple.Item1.Completed = true;
                    if (tuple.Item1.AutoReleaseWhenCanceled && tuple.Item2.IsValid())
                    {
                        Addressables.Release(tuple.Item2);
                    }
                    LuminTaskSourceCore<T>.TrySetCanceled(state.Source);
                    tuple.Dispose();
                    return false;
                }

                // Report progress
                if (tuple.Item3 != null && tuple.Item2.IsValid())
                {
                    tuple.Item3.Report(tuple.Item2.GetDownloadStatus().Percent);
                }

                return true;
            }

            static void OnCompleted(LuminTaskState state)
            {
                var tuple = Unsafe.As<StateTuple<PromiseState, AsyncOperationHandle<T>, IProgress<float>>>(state.StateTuple);
                
                if (tuple.Item1.Completed)
                {
                    return;
                }

                tuple.Item1.Completed = true;

                if (state.CancellationToken.IsCancellationRequested)
                {
                    if (tuple.Item1.AutoReleaseWhenCanceled && tuple.Item2.IsValid())
                    {
                        Addressables.Release(tuple.Item2);
                    }
                    LuminTaskSourceCore<T>.TrySetCanceled(state.Source);
                }
                else if (tuple.Item2.Status == AsyncOperationStatus.Failed)
                {
                    LuminTaskSourceCore<T>.TrySetException(state.Source, tuple.Item2.OperationException);
                }
                else
                {
                    LuminTaskSourceCore<T>.TrySetResult(state.Source, tuple.Item2.Result);
                }
                
                tuple.Dispose();
                LuminTaskSourceCore<T>.Dispose(state.Source);
            }
        }

        #endregion
    }
}
