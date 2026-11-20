using System;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Threading;
using YooAsset;
using LuminThread.TaskSource;
using LuminThread.Utility;
using LuminThread.Interface;

namespace LuminThread.Unity
{
    public static class YooAssetAsyncLuminTask
    {
        private struct WaitState
        {
            public HandleBase Resource;
            public IProgress<float> Progress;
            public bool ReleaseOnCancel;
            public CancellationTokenRegistration Registration;
        }

        private static readonly Action<object> ReleaseState = state =>
        {
            var box = (StateTuple<WaitState>)state;
            box.Item.Registration.Dispose();
            box.Dispose();
        };

        private static readonly MoveNext Poll = MoveNext;

        private static ulong Schedule(HandleBase resource, PlayerLoopTiming timing, IProgress<float> progress,
            CancellationToken token, bool releaseOnCancel)
        {
            LuminTaskSource.Rent(out var source);
            var box = StateTuple.Create(new WaitState { Resource = resource, Progress = progress, ReleaseOnCancel = releaseOnCancel });
            if (token.CanBeCanceled)
                box.Item.Registration = UnityLoopWork.RegisterCancellation(source, token);
            UnityLoopWork.Add(timing, new LuminTaskState(source, token, box), Poll, ReleaseState);
            return source;
        }

        private static bool MoveNext(in LuminTaskState state)
        {
            var box = (StateTuple<WaitState>)state.State;
            var resource = box.Item.Resource;
            bool keep = false;
            try
            {
                if (state.CancellationToken.IsCancellationRequested)
                {
                    if (box.Item.ReleaseOnCancel && resource.IsValid) resource.Release();
                    LuminTaskSource.SetCanceled(state.Handle, state.CancellationToken);
                }
                else if (resource.IsDone)
                {
                    if (resource.Status == EOperationStatus.Failed)
                        LuminTaskSource.SetException(state.Handle, new Exception(resource.LastError));
                    else
                        LuminTaskSource.SetResult(state.Handle, resource);
                }
                else
                {
                    if (resource.IsValid) box.Item.Progress?.Report(resource.Progress);
                    keep = true;
                }
                return keep;
            }
            finally { if (!keep) ReleaseState(box); }
        }

        // YooAsset 2.1 exposes release on each concrete handle. SDKs with HandleBase.Release
        // bind the instance method directly.
        private static void Release(this HandleBase resource)
        {
            if (resource is IDisposable disposable) disposable.Dispose();
            else if (resource is SceneHandle scene) scene.UnloadAsync();
        }

        #region HandleBase (基类，无返回值)

        public static LuminTask GetAwaiter(this HandleBase handle)
        {
            return ToLuminTask(handle).GetAwaiter();
        }

        public static LuminTask WithCancellation(this HandleBase handle, CancellationToken cancellationToken, bool autoReleaseWhenCanceled = false)
        {
            return ToLuminTask(handle, cancellationToken: cancellationToken, autoReleaseWhenCanceled: autoReleaseWhenCanceled);
        }

        public static LuminTask ToLuminTask(
            this HandleBase handle, 
            IProgress<float> progress = null, 
            PlayerLoopTiming timing = PlayerLoopTiming.Update, 
            CancellationToken cancellationToken = default, 
            bool autoReleaseWhenCanceled = false)
        {
            if (cancellationToken.IsCancellationRequested) 
                return LuminTask.FromCanceled(cancellationToken);

            if (handle == null || !handle.IsValid)
            {
                return LuminTask.CompletedTask();
            }

            if (handle.IsDone)
            {
                if (handle.Status == EOperationStatus.Failed)
                {
                    return LuminTask.FromException(new Exception(handle.LastError));
                }
                return LuminTask.CompletedTask();
            }

            return HandleBaseLuminTaskConfiguredSource.Create(handle, timing, progress, cancellationToken, autoReleaseWhenCanceled);
        }

        public struct HandleBaseLuminTaskConfiguredSource
        {
            public static LuminTask Create(HandleBase handle, PlayerLoopTiming timing, IProgress<float> progress,
                CancellationToken cancellationToken, bool autoReleaseWhenCanceled)
            {
                if (cancellationToken.IsCancellationRequested)
                    return LuminTask.FromCanceled(cancellationToken);
                var source = Schedule(handle, timing, progress, cancellationToken, autoReleaseWhenCanceled);
                return LuminTaskSource.ToTask(source);
            }

            public static bool MoveNext(in LuminTaskState state) => YooAssetAsyncLuminTask.MoveNext(state);
        }

        #endregion

        #region AssetHandle (资源加载)

        public static LuminTask<AssetHandle> GetAwaiter(this AssetHandle handle)
        {
            return ToLuminTask(handle).GetAwaiter();
        }

        public static LuminTask<AssetHandle> WithCancellation(this AssetHandle handle, CancellationToken cancellationToken, bool autoReleaseWhenCanceled = false)
        {
            return ToLuminTask(handle, cancellationToken: cancellationToken, autoReleaseWhenCanceled: autoReleaseWhenCanceled);
        }

        public static LuminTask<AssetHandle> ToLuminTask(
            this AssetHandle handle, 
            IProgress<float> progress = null, 
            PlayerLoopTiming timing = PlayerLoopTiming.Update, 
            CancellationToken cancellationToken = default, 
            bool autoReleaseWhenCanceled = false)
        {
            if (cancellationToken.IsCancellationRequested) 
                return LuminTask.FromCanceled<AssetHandle>(cancellationToken);

            if (handle == null || !handle.IsValid)
            {
                throw new Exception("Attempting to use an invalid asset handle");
            }

            if (handle.IsDone)
            {
                if (handle.Status == EOperationStatus.Failed)
                {
                    return LuminTask.FromException<AssetHandle>(new Exception(handle.LastError));
                }
                return LuminTask.FromResult(handle);
            }

            return AssetHandleLuminTaskConfiguredSource.Create(handle, timing, progress, cancellationToken, autoReleaseWhenCanceled);
        }

        public struct AssetHandleLuminTaskConfiguredSource
        {
            public static LuminTask<AssetHandle> Create(AssetHandle handle, PlayerLoopTiming timing, IProgress<float> progress,
                CancellationToken cancellationToken, bool autoReleaseWhenCanceled)
            {
                if (cancellationToken.IsCancellationRequested)
                    return LuminTask.FromCanceled<AssetHandle>(cancellationToken);
                var source = Schedule(handle, timing, progress, cancellationToken, autoReleaseWhenCanceled);
                return LuminTaskSource.ToTask<AssetHandle>(source);
            }

            public static bool MoveNext(in LuminTaskState state) => YooAssetAsyncLuminTask.MoveNext(state);
        }

        #endregion

        #region SceneHandle (场景加载)

        public static LuminTask<SceneHandle> GetAwaiter(this SceneHandle handle)
        {
            return ToLuminTask(handle).GetAwaiter();
        }

        public static LuminTask<SceneHandle> WithCancellation(this SceneHandle handle, CancellationToken cancellationToken, bool autoReleaseWhenCanceled = false)
        {
            return ToLuminTask(handle, cancellationToken: cancellationToken, autoReleaseWhenCanceled: autoReleaseWhenCanceled);
        }

        public static LuminTask<SceneHandle> ToLuminTask(
            this SceneHandle handle, 
            IProgress<float> progress = null, 
            PlayerLoopTiming timing = PlayerLoopTiming.Update, 
            CancellationToken cancellationToken = default, 
            bool autoReleaseWhenCanceled = false)
        {
            if (cancellationToken.IsCancellationRequested) 
                return LuminTask.FromCanceled<SceneHandle>(cancellationToken);

            if (handle == null || !handle.IsValid)
            {
                throw new Exception("Attempting to use an invalid scene handle");
            }

            if (handle.IsDone)
            {
                if (handle.Status == EOperationStatus.Failed)
                {
                    return LuminTask.FromException<SceneHandle>(new Exception(handle.LastError));
                }
                return LuminTask.FromResult(handle);
            }

            return SceneHandleLuminTaskConfiguredSource.Create(handle, timing, progress, cancellationToken, autoReleaseWhenCanceled);
        }

        public struct SceneHandleLuminTaskConfiguredSource
        {
            public static LuminTask<SceneHandle> Create(SceneHandle handle, PlayerLoopTiming timing, IProgress<float> progress,
                CancellationToken cancellationToken, bool autoReleaseWhenCanceled = false)
            {
                if (cancellationToken.IsCancellationRequested)
                    return LuminTask.FromCanceled<SceneHandle>(cancellationToken);
                var source = Schedule(handle, timing, progress, cancellationToken, autoReleaseWhenCanceled);
                return LuminTaskSource.ToTask<SceneHandle>(source);
            }

            public static bool MoveNext(in LuminTaskState state) => YooAssetAsyncLuminTask.MoveNext(state);
        }

        #endregion

        #region RawFileHandle (原生文件)

        public static LuminTask<RawFileHandle> GetAwaiter(this RawFileHandle handle)
        {
            return ToLuminTask(handle).GetAwaiter();
        }

        public static LuminTask<RawFileHandle> WithCancellation(this RawFileHandle handle, CancellationToken cancellationToken, bool autoReleaseWhenCanceled = false)
        {
            return ToLuminTask(handle, cancellationToken: cancellationToken, autoReleaseWhenCanceled: autoReleaseWhenCanceled);
        }

        public static LuminTask<RawFileHandle> ToLuminTask(
            this RawFileHandle handle, 
            IProgress<float> progress = null, 
            PlayerLoopTiming timing = PlayerLoopTiming.Update, 
            CancellationToken cancellationToken = default, 
            bool autoReleaseWhenCanceled = false)
        {
            if (cancellationToken.IsCancellationRequested) 
                return LuminTask.FromCanceled<RawFileHandle>(cancellationToken);

            if (handle == null || !handle.IsValid)
            {
                throw new Exception("Attempting to use an invalid raw file handle");
            }

            if (handle.IsDone)
            {
                if (handle.Status == EOperationStatus.Failed)
                {
                    return LuminTask.FromException<RawFileHandle>(new Exception(handle.LastError));
                }
                return LuminTask.FromResult(handle);
            }

            return RawFileHandleLuminTaskConfiguredSource.Create(handle, timing, progress, cancellationToken, autoReleaseWhenCanceled);
        }

        public struct RawFileHandleLuminTaskConfiguredSource
        {
            public static LuminTask<RawFileHandle> Create(RawFileHandle handle, PlayerLoopTiming timing, IProgress<float> progress,
                CancellationToken cancellationToken, bool autoReleaseWhenCanceled)
            {
                if (cancellationToken.IsCancellationRequested)
                    return LuminTask.FromCanceled<RawFileHandle>(cancellationToken);
                var source = Schedule(handle, timing, progress, cancellationToken, autoReleaseWhenCanceled);
                return LuminTaskSource.ToTask<RawFileHandle>(source);
            }

            public static bool MoveNext(in LuminTaskState state) => YooAssetAsyncLuminTask.MoveNext(state);
        }

        #endregion

        #region SubAssetsHandle (子资源加载)

        public static LuminTask<SubAssetsHandle> GetAwaiter(this SubAssetsHandle handle)
        {
            return ToLuminTask(handle).GetAwaiter();
        }

        public static LuminTask<SubAssetsHandle> WithCancellation(this SubAssetsHandle handle, CancellationToken cancellationToken, bool autoReleaseWhenCanceled = false)
        {
            return ToLuminTask(handle, cancellationToken: cancellationToken, autoReleaseWhenCanceled: autoReleaseWhenCanceled);
        }

        public static LuminTask<SubAssetsHandle> ToLuminTask(
            this SubAssetsHandle handle, 
            IProgress<float> progress = null, 
            PlayerLoopTiming timing = PlayerLoopTiming.Update, 
            CancellationToken cancellationToken = default, 
            bool autoReleaseWhenCanceled = false)
        {
            if (cancellationToken.IsCancellationRequested) 
                return LuminTask.FromCanceled<SubAssetsHandle>(cancellationToken);

            if (handle == null || !handle.IsValid)
            {
                throw new Exception("Attempting to use an invalid sub assets handle");
            }

            if (handle.IsDone)
            {
                if (handle.Status == EOperationStatus.Failed)
                {
                    return LuminTask.FromException<SubAssetsHandle>(new Exception(handle.LastError));
                }
                return LuminTask.FromResult(handle);
            }

            return SubAssetsHandleLuminTaskConfiguredSource.Create(handle, timing, progress, cancellationToken, autoReleaseWhenCanceled);
        }

        public struct SubAssetsHandleLuminTaskConfiguredSource
        {
            public static LuminTask<SubAssetsHandle> Create(SubAssetsHandle handle, PlayerLoopTiming timing, IProgress<float> progress,
                CancellationToken cancellationToken, bool autoReleaseWhenCanceled)
            {
                if (cancellationToken.IsCancellationRequested)
                    return LuminTask.FromCanceled<SubAssetsHandle>(cancellationToken);
                var source = Schedule(handle, timing, progress, cancellationToken, autoReleaseWhenCanceled);
                return LuminTaskSource.ToTask<SubAssetsHandle>(source);
            }

            public static bool MoveNext(in LuminTaskState state) => YooAssetAsyncLuminTask.MoveNext(state);
        }

        #endregion
    }
}
