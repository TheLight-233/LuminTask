
#if UNITY_2018_4 || UNITY_2019_4_OR_NEWER

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using UnityEngine;
using LuminThread.TaskSource;
using LuminThread.Utility;
using LuminThread.Interface;

namespace LuminThread.Unity
{
    public static partial class UnityAsyncLuminTask
    {
        public static AssetBundleRequestAllAssetsLuminTaskAwaiter AwaitForAllAssets(this AssetBundleRequest asyncOperation)
        {
            if (asyncOperation == null)
                throw new ArgumentNullException(nameof(asyncOperation));
            return new AssetBundleRequestAllAssetsLuminTaskAwaiter(asyncOperation);
        }

        public static LuminTask<UnityEngine.Object[]> AwaitForAllAssets(this AssetBundleRequest asyncOperation, CancellationToken cancellationToken)
        {
            return AwaitForAllAssets(asyncOperation, null, PlayerLoopTiming.Update, cancellationToken: cancellationToken);
        }

        public static LuminTask<UnityEngine.Object[]> AwaitForAllAssets(this AssetBundleRequest asyncOperation, CancellationToken cancellationToken, bool cancelImmediately)
        {
            return AwaitForAllAssets(asyncOperation, progress: null, cancellationToken: cancellationToken, cancelImmediately: cancelImmediately);
        }

        public static LuminTask<UnityEngine.Object[]> AwaitForAllAssets(this AssetBundleRequest asyncOperation, IProgress<float> progress = null, PlayerLoopTiming timing = PlayerLoopTiming.Update, CancellationToken cancellationToken = default, bool cancelImmediately = false)
        {
            if (asyncOperation == null)
                throw new ArgumentNullException(nameof(asyncOperation));
            if (cancellationToken.IsCancellationRequested)
                return LuminTask.FromCanceled<UnityEngine.Object[]>(cancellationToken);
            if (asyncOperation.isDone)
                return LuminTask.FromResult(asyncOperation.allAssets);
            return AssetBundleRequestAllAssetsPromise.Create(asyncOperation, timing, progress, cancellationToken, cancelImmediately);
        }

        public struct AssetBundleRequestAllAssetsLuminTaskAwaiter : ICriticalNotifyCompletion
        {
            private AssetBundleRequest _asyncOperation;
            private Action<AsyncOperation> _continuationAction;

            public AssetBundleRequestAllAssetsLuminTaskAwaiter(AssetBundleRequest asyncOperation)
            {
                _asyncOperation = asyncOperation;
                _continuationAction = null;
            }

            public AssetBundleRequestAllAssetsLuminTaskAwaiter GetAwaiter()
            {
                return this;
            }

            public bool IsCompleted => _asyncOperation.isDone;

            public UnityEngine.Object[] GetResult()
            {
                if (_continuationAction != null)
                {
                    _asyncOperation.completed -= _continuationAction;
                    _continuationAction = null;
                }
                return _asyncOperation.allAssets;
            }

            public void OnCompleted(Action continuation)
            {
                UnsafeOnCompleted(continuation);
            }

            public void UnsafeOnCompleted(Action continuation)
            {
                if (_continuationAction != null)
                    throw new InvalidOperationException("Continuation is already registered");
                
                _continuationAction = Continuation;
                _asyncOperation.completed += _continuationAction;
                
                void Continuation(AsyncOperation _) => continuation();
            }
        }

        private static class AssetBundleRequestAllAssetsPromise
        {
            [StructLayout(LayoutKind.Sequential)]
            private struct StateData
            {
                public bool Completed;
                public bool IsCancellationRequested;
                public CancellationTokenRegistration Registration;
                public ulong Handle;
            }

            public static unsafe LuminTask<UnityEngine.Object[]> Create(AssetBundleRequest asyncOperation, PlayerLoopTiming timing, IProgress<float> progress, CancellationToken cancellationToken, bool cancelImmediately)
            {
                var task = LuminTaskSource.Create<UnityEngine.Object[]>(out ulong handle);
                var stateData = new StateData
                {
                    Completed = false,
                    IsCancellationRequested = false,
                    Handle = handle
                };

                var stateTuple = StateTuple.Create(stateData, progress, asyncOperation);
                var state = new LuminTaskState(handle, cancellationToken, stateTuple);
                
                Action<AsyncOperation> continuation = null;
                continuation = _ =>
                {
                    if (stateData.Completed)
                        return;
                        
                    stateData.Completed = true;
                    stateData.IsCancellationRequested = cancellationToken.IsCancellationRequested;
                    stateTuple.Item.Item1.Completed = true;
                    stateTuple.Item.Item1.IsCancellationRequested = stateData.IsCancellationRequested;
                    
                    if (cancellationToken.IsCancellationRequested)
                    {
                        LuminTaskSource.SetCanceled(handle, cancellationToken);
                    }
                    else
                    {
                        LuminTaskSource.SetResult(handle, asyncOperation.allAssets);
                    }
                    
                    if (continuation != null)
                    {
                        asyncOperation.completed -= continuation;
                        continuation = null;
                    }

                    stateTuple.Item.Item1.Registration.Dispose();
                    stateTuple.Dispose();
                };

                asyncOperation.completed += continuation;

                if (cancelImmediately && cancellationToken.CanBeCanceled)
                {
                    stateTuple.Item.Item1.Registration = cancellationToken.Register(() =>
                    {
                        stateData.Completed = true;
                        stateData.IsCancellationRequested = true;
                        LuminTaskSource.SetCanceled(handle, cancellationToken);
                    });
                }

                PlayerLoopHelper.AddAction(timing, state, MoveNextAction);

                return task;
            }

            private static readonly MoveNext MoveNextAction = MoveNext;

            public static bool MoveNext(in LuminTaskState state)
            {
                var stateTuple = state.State as StateTuple<(StateData, IProgress<float>, AssetBundleRequest)>;
                if (stateTuple == null)
                    return false;

                var asyncOperation = stateTuple.Item.Item3;

                if (stateTuple.Item.Item1.Completed)
                    return false;

                if (state.CancellationToken.IsCancellationRequested)
                {
                    stateTuple.Item.Item1.Completed = true;
                    stateTuple.Item.Item1.IsCancellationRequested = true;
                    LuminTaskSource.SetCanceled(stateTuple.Item.Item1.Handle, state.CancellationToken);
                    stateTuple.Item.Item1.Registration.Dispose();
                    stateTuple.Dispose();
                    return false;
                }

                var progress = stateTuple.Item.Item2;
                progress?.Report(asyncOperation.progress);

                if (asyncOperation.isDone)
                {
                    stateTuple.Item.Item1.Completed = true;
                    LuminTaskSource.SetResult(stateTuple.Item.Item1.Handle, asyncOperation.allAssets);
                    stateTuple.Item.Item1.Registration.Dispose();
                    stateTuple.Dispose();
                    return false;
                }

                return true;
            }
        }
    }
}

#endif
