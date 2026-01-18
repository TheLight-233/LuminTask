
#if UNITY_2018_4 || UNITY_2019_4_OR_NEWER

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using UnityEngine;
using LuminThread.TaskSource;
using LuminThread.Utility;

namespace LuminThread.Unity
{
    public static partial class UnityAsyncExtensions
    {
        public static AssetBundleRequestAllAssetsAwaiter AwaitForAllAssets(this AssetBundleRequest asyncOperation)
        {
            if (asyncOperation == null)
                throw new ArgumentNullException(nameof(asyncOperation));
            return new AssetBundleRequestAllAssetsAwaiter(asyncOperation);
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

        public struct AssetBundleRequestAllAssetsAwaiter : ICriticalNotifyCompletion
        {
            private AssetBundleRequest _asyncOperation;
            private Action<AsyncOperation> _continuationAction;

            public AssetBundleRequestAllAssetsAwaiter(AssetBundleRequest asyncOperation)
            {
                _asyncOperation = asyncOperation;
                _continuationAction = null;
            }

            public AssetBundleRequestAllAssetsAwaiter GetAwaiter()
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
            }

            public static unsafe LuminTask<UnityEngine.Object[]> Create(AssetBundleRequest asyncOperation, PlayerLoopTiming timing, IProgress<float> progress, CancellationToken cancellationToken, bool cancelImmediately)
            {
                var core = LuminTaskSourceCore<UnityEngine.Object[]>.Create();
                var stateData = new StateData
                {
                    Completed = false,
                    IsCancellationRequested = false
                };

                var stateTuple = StateTuple.Create(stateData, progress);
                var state = new LuminTaskState(core, cancellationToken, asyncOperation, stateTuple);
                
                Action<AsyncOperation> continuation = null;
                continuation = _ =>
                {
                    if (stateData.Completed)
                        return;
                        
                    stateData.Completed = true;
                    stateData.IsCancellationRequested = cancellationToken.IsCancellationRequested;
                    
                    if (cancellationToken.IsCancellationRequested)
                    {
                        LuminTaskSourceCore<UnityEngine.Object[]>.TrySetCanceled(core);
                    }
                    else
                    {
                        LuminTaskSourceCore<UnityEngine.Object[]>.TrySetResult(core, asyncOperation.allAssets);
                    }
                    
                    if (continuation != null)
                    {
                        asyncOperation.completed -= continuation;
                        continuation = null;
                    }
                };

                asyncOperation.completed += continuation;

                if (cancelImmediately && cancellationToken.CanBeCanceled)
                {
                    var registration = cancellationToken.Register(() =>
                    {
                        stateData.Completed = true;
                        stateData.IsCancellationRequested = true;
                        LuminTaskSourceCore<UnityEngine.Object[]>.TrySetCanceled(core);
                    });
                }

                PlayerLoopHelper.AddAction(timing, state, &MoveNext);

                return new LuminTask<UnityEngine.Object[]>(LuminTaskSourceCore<UnityEngine.Object[]>.MethodTable, core, core->Id);
            }

            public static unsafe bool MoveNext(in LuminTaskState state)
            {
                var asyncOperation = state.State as AssetBundleRequest;
                if (asyncOperation == null)
                    return false;

                var stateTuple = state.StateTuple as StateTuple<StateData, IProgress<float>>;
                if (stateTuple == null)
                    return false;

                if (stateTuple.Item1.Completed)
                    return false;

                if (state.CancellationToken.IsCancellationRequested)
                {
                    stateTuple.Item1.Completed = true;
                    stateTuple.Item1.IsCancellationRequested = true;
                    LuminTaskSourceCore<UnityEngine.Object[]>.TrySetCanceled(state.Source);
                    stateTuple.Dispose();
                    return false;
                }

                var progress = stateTuple.Item2;
                progress?.Report(asyncOperation.progress);

                if (asyncOperation.isDone)
                {
                    stateTuple.Item1.Completed = true;
                    LuminTaskSourceCore<UnityEngine.Object[]>.TrySetResult(state.Source, asyncOperation.allAssets);
                    stateTuple.Dispose();
                    return false;
                }

                return true;
            }
        }
    }
}

#endif
