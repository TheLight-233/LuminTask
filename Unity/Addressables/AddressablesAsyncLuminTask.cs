
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
    public static class AddressablesAsyncLuminTask
    {
        #region AsyncOperationHandle

        public static LuminTask GetAwaiter(this AsyncOperationHandle handle)
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

            return AsyncOperationHandleLuminTaskConfiguredSource.Create(handle, timing, progress, cancellationToken, cancelImmediately, autoReleaseWhenCanceled);
        }

        public struct AsyncOperationHandleLuminTaskAwaiter : ICriticalNotifyCompletion
        {
            AsyncOperationHandle handle;
            Action<AsyncOperationHandle> continuationAction;

            public AsyncOperationHandleLuminTaskAwaiter(AsyncOperationHandle handle)
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

        public struct AsyncOperationHandleLuminTaskConfiguredSource
        {
struct PromiseState
            {
                public bool Completed;
                public bool CancelImmediately;
                public bool AutoReleaseWhenCanceled;
                public CancellationTokenRegistration Registration;
                public ulong Handle;
            }

            public static LuminTask Create(AsyncOperationHandle handle, PlayerLoopTiming timing, IProgress<float> progress, CancellationToken cancellationToken, bool cancelImmediately, bool autoReleaseWhenCanceled)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return LuminTask.FromCanceled(cancellationToken);
                }

                var task = LuminTaskSource.Create(out ulong sourceHandle);

                var promiseState = new PromiseState
                {
                    Completed = false,
                    CancelImmediately = cancelImmediately,
                    AutoReleaseWhenCanceled = autoReleaseWhenCanceled,
                    Handle = sourceHandle
                };

                var stateTuple = StateTuple.Create(promiseState, handle, progress);
                var state = new LuminTaskState(sourceHandle, cancellationToken, stateTuple);

                // Register cancellation if needed
                if (cancelImmediately && cancellationToken.CanBeCanceled)
                {
                    stateTuple.Item.Item1.Registration = cancellationToken.Register(static s =>
                    {
                        var taskState = (LuminTaskState)s;
                        var tuple = (StateTuple<(PromiseState, AsyncOperationHandle, IProgress<float>)>)taskState.State!;
                        
                        if (tuple.Item.Item1.AutoReleaseWhenCanceled && tuple.Item.Item2.IsValid())
                        {
                            Addressables.Release(tuple.Item.Item2);
                        }
                        LuminTaskSource.SetCanceled(tuple.Item.Item1.Handle, taskState.CancellationToken);
                        // 回调自身在途：仅清引用，释放交给 OnCompleted / MoveNext。
                        tuple.Item.Item1.Registration = default;
                    }, state);
                }

                // Setup completion callback
                Action<AsyncOperationHandle> completedCallback = _ => OnCompleted(state);
                handle.Completed += completedCallback;

                // Add to player loop for progress tracking
                PlayerLoopHelper.AddAction(timing, state, MoveNextAction);

                return task;
            }

            private static readonly MoveNext MoveNextAction = MoveNext;

            static bool MoveNext(in LuminTaskState state)
            {
                var tuple = (StateTuple<(PromiseState, AsyncOperationHandle, IProgress<float>)>)state.State!;
                
                if (tuple.Item.Item1.Completed)
                {
                    tuple.Dispose();
                    return false;
                }

                if (state.CancellationToken.IsCancellationRequested)
                {
                    tuple.Item.Item1.Completed = true;
                    if (tuple.Item.Item1.AutoReleaseWhenCanceled && tuple.Item.Item2.IsValid())
                    {
                        Addressables.Release(tuple.Item.Item2);
                    }
                    LuminTaskSource.SetCanceled(tuple.Item.Item1.Handle, state.CancellationToken);
                    tuple.Item.Item1.Registration.Dispose();
                    tuple.Dispose();
                    return false;
                }

                // Report progress
                if (tuple.Item.Item3 != null && tuple.Item.Item2.IsValid())
                {
                    tuple.Item.Item3.Report(tuple.Item.Item2.GetDownloadStatus().Percent);
                }

                return true;
            }

            static void OnCompleted(LuminTaskState state)
            {
                var tuple = (StateTuple<(PromiseState, AsyncOperationHandle, IProgress<float>)>)state.State!;
                
                if (tuple.Item.Item1.Completed)
                {
                    return;
                }

                tuple.Item.Item1.Completed = true;

                if (state.CancellationToken.IsCancellationRequested)
                {
                    if (tuple.Item.Item1.AutoReleaseWhenCanceled && tuple.Item.Item2.IsValid())
                    {
                        Addressables.Release(tuple.Item.Item2);
                    }
                    LuminTaskSource.SetCanceled(tuple.Item.Item1.Handle, state.CancellationToken);
                }
                else if (tuple.Item.Item2.Status == AsyncOperationStatus.Failed)
                {
                    LuminTaskSource.SetException(tuple.Item.Item1.Handle, tuple.Item.Item2.OperationException);
                }
                else
                {
                    LuminTaskSource.SetResult(tuple.Item.Item1.Handle);
                }
                
                tuple.Item.Item1.Registration.Dispose();
                tuple.Dispose();
            }
        }

        #endregion

        #region AsyncOperationHandle<T>

        public static LuminTask<T> GetAwaiter<T>(this AsyncOperationHandle<T> handle)
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

            return AsyncOperationHandleLuminTaskConfiguredSource<T>.Create(handle, timing, progress, cancellationToken, cancelImmediately, autoReleaseWhenCanceled);
        }

public struct AsyncOperationHandleLuminTaskConfiguredSource<T>
        {
            struct PromiseState
            {
                public bool Completed;
                public bool CancelImmediately;
                public bool AutoReleaseWhenCanceled;
                public CancellationTokenRegistration Registration;
                public ulong Handle;
            }

            public static LuminTask<T> Create(AsyncOperationHandle<T> handle, PlayerLoopTiming timing, IProgress<float> progress, CancellationToken cancellationToken, bool cancelImmediately, bool autoReleaseWhenCanceled)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return LuminTask.FromCanceled<T>(cancellationToken);
                }

                var task = LuminTaskSource.Create<T>(out ulong sourceHandle);

                var promiseState = new PromiseState
                {
                    Completed = false,
                    CancelImmediately = cancelImmediately,
                    AutoReleaseWhenCanceled = autoReleaseWhenCanceled,
                    Handle = sourceHandle
                };

                var stateTuple = StateTuple.Create(promiseState, handle, progress);
                var state = new LuminTaskState(sourceHandle, cancellationToken, stateTuple);

                // Register cancellation if needed
                if (cancelImmediately && cancellationToken.CanBeCanceled)
                {
                    stateTuple.Item.Item1.Registration = cancellationToken.Register(static s =>
                    {
                        var taskState = (LuminTaskState)s;
                        var tuple = (StateTuple<(PromiseState, AsyncOperationHandle<T>, IProgress<float>)>)taskState.State!;
                        
                        if (tuple.Item.Item1.AutoReleaseWhenCanceled && tuple.Item.Item2.IsValid())
                        {
                            Addressables.Release(tuple.Item.Item2);
                        }
                        LuminTaskSource.SetCanceled(tuple.Item.Item1.Handle, taskState.CancellationToken);
                        // 回调自身在途：仅清引用，释放交给 OnCompleted / MoveNext。
                        tuple.Item.Item1.Registration = default;
                    }, state);
                }

                // Setup completion callback
                Action<AsyncOperationHandle<T>> completedCallback = _ => OnCompleted(state);
                handle.Completed += completedCallback;

                // Add to player loop for progress tracking
                PlayerLoopHelper.AddAction(timing, state, MoveNextAction);

                return task;
            }

            private static readonly MoveNext MoveNextAction = MoveNext;

            static bool MoveNext(in LuminTaskState state)
            {
                var tuple = (StateTuple<(PromiseState, AsyncOperationHandle<T>, IProgress<float>)>)state.State!;
                
                if (tuple.Item.Item1.Completed)
                {
                    tuple.Dispose();
                    return false;
                }

                if (state.CancellationToken.IsCancellationRequested)
                {
                    tuple.Item.Item1.Completed = true;
                    if (tuple.Item.Item1.AutoReleaseWhenCanceled && tuple.Item.Item2.IsValid())
                    {
                        Addressables.Release(tuple.Item.Item2);
                    }
                    LuminTaskSource.SetCanceled(tuple.Item.Item1.Handle, state.CancellationToken);
                    tuple.Item.Item1.Registration.Dispose();
                    tuple.Dispose();
                    return false;
                }

                // Report progress
                if (tuple.Item.Item3 != null && tuple.Item.Item2.IsValid())
                {
                    tuple.Item.Item3.Report(tuple.Item.Item2.GetDownloadStatus().Percent);
                }

                return true;
            }

            static void OnCompleted(LuminTaskState state)
            {
                var tuple = (StateTuple<(PromiseState, AsyncOperationHandle<T>, IProgress<float>)>)state.State!;
                
                if (tuple.Item.Item1.Completed)
                {
                    return;
                }

                tuple.Item.Item1.Completed = true;

                if (state.CancellationToken.IsCancellationRequested)
                {
                    if (tuple.Item.Item1.AutoReleaseWhenCanceled && tuple.Item.Item2.IsValid())
                    {
                        Addressables.Release(tuple.Item.Item2);
                    }
                    LuminTaskSource.SetCanceled(tuple.Item.Item1.Handle, state.CancellationToken);
                }
                else if (tuple.Item.Item2.Status == AsyncOperationStatus.Failed)
                {
                    LuminTaskSource.SetException(tuple.Item.Item1.Handle, tuple.Item.Item2.OperationException);
                }
                else
                {
                    LuminTaskSource.SetResult(tuple.Item.Item1.Handle, tuple.Item.Item2.Result);
                }
                
                tuple.Item.Item1.Registration.Dispose();
                tuple.Dispose();
            }
        }

        #endregion
    }
}
