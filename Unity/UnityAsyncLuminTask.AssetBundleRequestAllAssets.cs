#if UNITY_2018_4 || UNITY_2019_4_OR_NEWER

using System;
using System.Runtime.CompilerServices;
using System.Threading;
using UnityEngine;
using LuminThread.TaskSource;
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

        // Same shape as the AssetBundleRequest adapter, only the result extraction differs, so this entry
        // reuses the shared control block, its pool and its ownership rules instead of keeping a second
        // copy of the old StateData pattern - which also carried the old defect where the completed
        // callback and the player loop both released the same state tuple.
        public static LuminTask<UnityEngine.Object[]> AwaitForAllAssets(this AssetBundleRequest asyncOperation, IProgress<float> progress = null, PlayerLoopTiming timing = PlayerLoopTiming.Update, CancellationToken cancellationToken = default, bool cancelImmediately = false)
        {
            if (asyncOperation == null)
                throw new ArgumentNullException(nameof(asyncOperation));
            if (cancellationToken.IsCancellationRequested)
                return LuminTask.FromCanceled<UnityEngine.Object[]>(cancellationToken);
            if (asyncOperation.isDone)
                return LuminTask.FromResult(asyncOperation.allAssets);
            var handle = AsyncOperationPromise.Create(asyncOperation, timing, progress, cancellationToken, cancelImmediately,
                AssetBundleAllAssetsWriter.Write, PlainCanceledWriter.Write);
            return LuminTaskSource.ToTask<UnityEngine.Object[]>(handle);
        }

        public struct AssetBundleRequestAllAssetsLuminTaskAwaiter : ICriticalNotifyCompletion
        {
            private AssetBundleRequest _asyncOperation;

            public AssetBundleRequestAllAssetsLuminTaskAwaiter(AssetBundleRequest asyncOperation)
            {
                _asyncOperation = asyncOperation;
            }

            public AssetBundleRequestAllAssetsLuminTaskAwaiter GetAwaiter()
            {
                return this;
            }

            public bool IsCompleted => _asyncOperation.isDone;

            public UnityEngine.Object[] GetResult() => _asyncOperation.allAssets;

            public void OnCompleted(Action continuation)
            {
                UnsafeOnCompleted(continuation);
            }

            public void UnsafeOnCompleted(Action continuation)
            {
                // The handler removes itself when Unity raises it. A field written here would not be
                // visible to the resumed GetResult: this awaiter is a struct, and the builder copies the
                // state machine - and with it the awaiter - into its box before OnCompleted runs.
                var operation = _asyncOperation;
                Action<AsyncOperation> handler = null;
                handler = _ => { operation.completed -= handler; continuation(); };
                operation.completed += handler;
            }
        }
    }
}

#endif