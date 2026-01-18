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
    public static class YooAssetAsyncExtensions
    {
        #region HandleBase (基类，无返回值)

        public static LuminTaskAwaiter GetAwaiter(this HandleBase handle)
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

            return HandleBaseConfiguredSource.Create(handle, timing, progress, cancellationToken, autoReleaseWhenCanceled);
        }

        public unsafe struct HandleBaseConfiguredSource
        {
            struct PromiseState
            {
                public bool Completed;
                public bool AutoReleaseWhenCanceled;
            }

            public static LuminTask Create(
                HandleBase handle, 
                PlayerLoopTiming timing, 
                IProgress<float> progress, 
                CancellationToken cancellationToken, 
                bool autoReleaseWhenCanceled)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return LuminTask.FromCanceled(cancellationToken);
                }

                var core = LuminTaskSourceCore<AsyncUnit>.Create();

                var promiseState = new PromiseState
                {
                    Completed = false,
                    AutoReleaseWhenCanceled = autoReleaseWhenCanceled
                };

                var stateTuple = StateTuple.Create(promiseState, handle, progress);
                var state = new LuminTaskState(core, cancellationToken, stateTuple);

                // 添加取消注册
                if (cancellationToken.CanBeCanceled)
                {
                    cancellationToken.Register(static s =>
                    {
                        var taskState = (LuminTaskState)s;
                        var tuple = Unsafe.As<StateTuple<PromiseState, HandleBase, IProgress<float>>>(taskState.StateTuple);

                        if (tuple.Item1.AutoReleaseWhenCanceled && tuple.Item2.IsValid)
                        {
                            tuple.Item2.Release();
                        }
                        LuminTaskSourceCore<AsyncUnit>.TrySetCanceled(taskState.Source);
                        
                        tuple.Dispose();
                        LuminTaskSourceCore<AsyncUnit>.Dispose(taskState.Source);
                    }, state);
                }

                PlayerLoopHelper.AddAction(timing, state, &MoveNext);

                return new LuminTask(LuminTaskSourceCore<AsyncUnit>.MethodTable, core, core->Id);
            }

            public static bool MoveNext(in LuminTaskState state)
            {
                var tuple = Unsafe.As<StateTuple<PromiseState, HandleBase, IProgress<float>>>(state.StateTuple);

                if (tuple.Item1.Completed)
                {
                    tuple.Dispose();
                    return false;
                }

                if (state.CancellationToken.IsCancellationRequested)
                {
                    tuple.Item1.Completed = true;
                    if (tuple.Item1.AutoReleaseWhenCanceled && tuple.Item2.IsValid)
                    {
                        tuple.Item2.Release();
                    }
                    LuminTaskSourceCore<AsyncUnit>.TrySetCanceled(state.Source);
                    tuple.Dispose();
                    LuminTaskSourceCore<AsyncUnit>.Dispose(state.Source);
                    return false;
                }

                // 检查是否完成
                if (tuple.Item2.IsDone)
                {
                    tuple.Item1.Completed = true;
                    
                    if (tuple.Item2.Status == EOperationStatus.Failed)
                    {
                        LuminTaskSourceCore<AsyncUnit>.TrySetException(state.Source, new Exception(tuple.Item2.LastError));
                    }
                    else
                    {
                        LuminTaskSourceCore<AsyncUnit>.TrySetResult(state.Source);
                    }
                    
                    tuple.Dispose();
                    LuminTaskSourceCore<AsyncUnit>.Dispose(state.Source);
                    return false;
                }

                // 报告进度
                if (tuple.Item3 != null && tuple.Item2.IsValid)
                {
                    tuple.Item3.Report(tuple.Item2.Progress);
                }

                return true;
            }
        }

        #endregion

        #region AssetHandle (资源加载)

        public static LuminTaskAwaiter<AssetHandle> GetAwaiter(this AssetHandle handle)
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

            return AssetHandleConfiguredSource.Create(handle, timing, progress, cancellationToken, autoReleaseWhenCanceled);
        }

        public unsafe struct AssetHandleConfiguredSource
        {
            struct PromiseState
            {
                public bool Completed;
                public bool AutoReleaseWhenCanceled;
            }

            public static LuminTask<AssetHandle> Create(
                AssetHandle handle, 
                PlayerLoopTiming timing, 
                IProgress<float> progress, 
                CancellationToken cancellationToken, 
                bool autoReleaseWhenCanceled)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return LuminTask.FromCanceled<AssetHandle>(cancellationToken);
                }

                var core = LuminTaskSourceCore<AssetHandle>.Create();

                var promiseState = new PromiseState
                {
                    Completed = false,
                    AutoReleaseWhenCanceled = autoReleaseWhenCanceled
                };

                var stateTuple = StateTuple.Create(promiseState, handle, progress);
                var state = new LuminTaskState(core, cancellationToken, stateTuple);

                // 添加取消注册
                if (cancellationToken.CanBeCanceled)
                {
                    cancellationToken.Register(static s =>
                    {
                        var taskState = (LuminTaskState)s;
                        var tuple = Unsafe.As<StateTuple<PromiseState, AssetHandle, IProgress<float>>>(taskState.StateTuple);

                        if (tuple.Item1.AutoReleaseWhenCanceled && tuple.Item2.IsValid)
                        {
                            tuple.Item2.Release();
                        }
                        LuminTaskSourceCore<AssetHandle>.TrySetCanceled(taskState.Source);
                        
                        tuple.Dispose();
                        LuminTaskSourceCore<AssetHandle>.Dispose(taskState.Source);
                    }, state);
                }

                PlayerLoopHelper.AddAction(timing, state, &MoveNext);

                return new LuminTask<AssetHandle>(LuminTaskSourceCore<AssetHandle>.MethodTable, core, core->Id);
            }

            public static bool MoveNext(in LuminTaskState state)
            {
                var tuple = Unsafe.As<StateTuple<PromiseState, AssetHandle, IProgress<float>>>(state.StateTuple);

                if (tuple.Item1.Completed)
                {
                    tuple.Dispose();
                    return false;
                }

                if (state.CancellationToken.IsCancellationRequested)
                {
                    tuple.Item1.Completed = true;
                    if (tuple.Item1.AutoReleaseWhenCanceled && tuple.Item2.IsValid)
                    {
                        tuple.Item2.Release();
                    }
                    LuminTaskSourceCore<AssetHandle>.TrySetCanceled(state.Source);
                    tuple.Dispose();
                    LuminTaskSourceCore<AssetHandle>.Dispose(state.Source);
                    return false;
                }

                // 检查是否完成
                if (tuple.Item2.IsDone)
                {
                    tuple.Item1.Completed = true;
                    
                    if (tuple.Item2.Status == EOperationStatus.Failed)
                    {
                        LuminTaskSourceCore<AssetHandle>.TrySetException(state.Source, new Exception(tuple.Item2.LastError));
                    }
                    else
                    {
                        LuminTaskSourceCore<AssetHandle>.TrySetResult(state.Source, tuple.Item2);
                    }
                    
                    tuple.Dispose();
                    LuminTaskSourceCore<AssetHandle>.Dispose(state.Source);
                    return false;
                }

                // 报告进度
                if (tuple.Item3 != null && tuple.Item2.IsValid)
                {
                    tuple.Item3.Report(tuple.Item2.Progress);
                }

                return true;
            }
        }

        #endregion

        #region SceneHandle (场景加载)

        public static LuminTaskAwaiter<SceneHandle> GetAwaiter(this SceneHandle handle)
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

            return SceneHandleConfiguredSource.Create(handle, timing, progress, cancellationToken, autoReleaseWhenCanceled);
        }

        public unsafe struct SceneHandleConfiguredSource
        {
            struct PromiseState
            {
                public bool Completed;
                public bool AutoReleaseWhenCanceled;
            }

            public static LuminTask<SceneHandle> Create(
                SceneHandle handle, 
                PlayerLoopTiming timing, 
                IProgress<float> progress, 
                CancellationToken cancellationToken, 
                bool autoReleaseWhenCanceled = false)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return LuminTask.FromCanceled<SceneHandle>(cancellationToken);
                }

                var core = LuminTaskSourceCore<SceneHandle>.Create();

                var promiseState = new PromiseState
                {
                    Completed = false,
                    AutoReleaseWhenCanceled = autoReleaseWhenCanceled
                };

                var stateTuple = StateTuple.Create(promiseState, handle, progress);
                var state = new LuminTaskState(core, cancellationToken, stateTuple);

                // 添加取消注册
                if (cancellationToken.CanBeCanceled)
                {
                    cancellationToken.Register(static s =>
                    {
                        var taskState = (LuminTaskState)s;
                        var tuple = Unsafe.As<StateTuple<PromiseState, SceneHandle, IProgress<float>>>(taskState.StateTuple);

                        if (tuple.Item1.AutoReleaseWhenCanceled && tuple.Item2.IsValid)
                        {
                            tuple.Item2.Release();
                        }
                        LuminTaskSourceCore<SceneHandle>.TrySetCanceled(taskState.Source);
                        
                        tuple.Dispose();
                        LuminTaskSourceCore<SceneHandle>.Dispose(taskState.Source);
                    }, state);
                }

                PlayerLoopHelper.AddAction(timing, state, &MoveNext);

                return new LuminTask<SceneHandle>(LuminTaskSourceCore<SceneHandle>.MethodTable, core, core->Id);
            }

            public static bool MoveNext(in LuminTaskState state)
            {
                var tuple = Unsafe.As<StateTuple<PromiseState, SceneHandle, IProgress<float>>>(state.StateTuple);

                if (tuple.Item1.Completed)
                {
                    tuple.Dispose();
                    return false;
                }

                if (state.CancellationToken.IsCancellationRequested)
                {
                    tuple.Item1.Completed = true;
                    if (tuple.Item1.AutoReleaseWhenCanceled && tuple.Item2.IsValid)
                    {
                        tuple.Item2.Release();
                    }
                    LuminTaskSourceCore<SceneHandle>.TrySetCanceled(state.Source);
                    tuple.Dispose();
                    LuminTaskSourceCore<SceneHandle>.Dispose(state.Source);
                    return false;
                }

                // 检查是否完成
                if (tuple.Item2.IsDone)
                {
                    tuple.Item1.Completed = true;
                    
                    if (tuple.Item2.Status == EOperationStatus.Failed)
                    {
                        LuminTaskSourceCore<SceneHandle>.TrySetException(state.Source, new Exception(tuple.Item2.LastError));
                    }
                    else
                    {
                        LuminTaskSourceCore<SceneHandle>.TrySetResult(state.Source, tuple.Item2);
                    }
                    
                    tuple.Dispose();
                    LuminTaskSourceCore<SceneHandle>.Dispose(state.Source);
                    return false;
                }

                // 报告进度
                if (tuple.Item3 != null && tuple.Item2.IsValid)
                {
                    tuple.Item3.Report(tuple.Item2.Progress);
                }

                return true;
            }
        }

        #endregion

        #region RawFileHandle (原生文件)

        public static LuminTaskAwaiter<RawFileHandle> GetAwaiter(this RawFileHandle handle)
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

            return RawFileHandleConfiguredSource.Create(handle, timing, progress, cancellationToken, autoReleaseWhenCanceled);
        }

        public unsafe struct RawFileHandleConfiguredSource
        {
            struct PromiseState
            {
                public bool Completed;
                public bool AutoReleaseWhenCanceled;
            }

            public static LuminTask<RawFileHandle> Create(
                RawFileHandle handle, 
                PlayerLoopTiming timing, 
                IProgress<float> progress, 
                CancellationToken cancellationToken, 
                bool autoReleaseWhenCanceled)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return LuminTask.FromCanceled<RawFileHandle>(cancellationToken);
                }

                var core = LuminTaskSourceCore<RawFileHandle>.Create();

                var promiseState = new PromiseState
                {
                    Completed = false,
                    AutoReleaseWhenCanceled = autoReleaseWhenCanceled
                };

                var stateTuple = StateTuple.Create(promiseState, handle, progress);
                var state = new LuminTaskState(core, cancellationToken, stateTuple);

                // 添加取消注册
                if (cancellationToken.CanBeCanceled)
                {
                    cancellationToken.Register(static s =>
                    {
                        var taskState = (LuminTaskState)s;
                        var tuple = Unsafe.As<StateTuple<PromiseState, RawFileHandle, IProgress<float>>>(taskState.StateTuple);

                        if (tuple.Item1.AutoReleaseWhenCanceled && tuple.Item2.IsValid)
                        {
                            tuple.Item2.Release();
                        }
                        LuminTaskSourceCore<RawFileHandle>.TrySetCanceled(taskState.Source);
                        
                        tuple.Dispose();
                        LuminTaskSourceCore<RawFileHandle>.Dispose(taskState.Source);
                    }, state);
                }

                PlayerLoopHelper.AddAction(timing, state, &MoveNext);

                return new LuminTask<RawFileHandle>(LuminTaskSourceCore<RawFileHandle>.MethodTable, core, core->Id);
            }

            public static bool MoveNext(in LuminTaskState state)
            {
                var tuple = Unsafe.As<StateTuple<PromiseState, RawFileHandle, IProgress<float>>>(state.StateTuple);

                if (tuple.Item1.Completed)
                {
                    tuple.Dispose();
                    return false;
                }

                if (state.CancellationToken.IsCancellationRequested)
                {
                    tuple.Item1.Completed = true;
                    if (tuple.Item1.AutoReleaseWhenCanceled && tuple.Item2.IsValid)
                    {
                        tuple.Item2.Release();
                    }
                    LuminTaskSourceCore<RawFileHandle>.TrySetCanceled(state.Source);
                    tuple.Dispose();
                    LuminTaskSourceCore<RawFileHandle>.Dispose(state.Source);
                    return false;
                }

                // 检查是否完成
                if (tuple.Item2.IsDone)
                {
                    tuple.Item1.Completed = true;
                    
                    if (tuple.Item2.Status == EOperationStatus.Failed)
                    {
                        LuminTaskSourceCore<RawFileHandle>.TrySetException(state.Source, new Exception(tuple.Item2.LastError));
                    }
                    else
                    {
                        LuminTaskSourceCore<RawFileHandle>.TrySetResult(state.Source, tuple.Item2);
                    }
                    
                    tuple.Dispose();
                    LuminTaskSourceCore<RawFileHandle>.Dispose(state.Source);
                    return false;
                }

                // 报告进度
                if (tuple.Item3 != null && tuple.Item2.IsValid)
                {
                    tuple.Item3.Report(tuple.Item2.Progress);
                }

                return true;
            }
        }

        #endregion

        #region SubAssetsHandle (子资源加载)

        public static LuminTaskAwaiter<SubAssetsHandle> GetAwaiter(this SubAssetsHandle handle)
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

            return SubAssetsHandleConfiguredSource.Create(handle, timing, progress, cancellationToken, autoReleaseWhenCanceled);
        }

        public unsafe struct SubAssetsHandleConfiguredSource
        {
            struct PromiseState
            {
                public bool Completed;
                public bool AutoReleaseWhenCanceled;
            }

            public static LuminTask<SubAssetsHandle> Create(
                SubAssetsHandle handle, 
                PlayerLoopTiming timing, 
                IProgress<float> progress, 
                CancellationToken cancellationToken, 
                bool autoReleaseWhenCanceled)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return LuminTask.FromCanceled<SubAssetsHandle>(cancellationToken);
                }

                var core = LuminTaskSourceCore<SubAssetsHandle>.Create();

                var promiseState = new PromiseState
                {
                    Completed = false,
                    AutoReleaseWhenCanceled = autoReleaseWhenCanceled
                };

                var stateTuple = StateTuple.Create(promiseState, handle, progress);
                var state = new LuminTaskState(core, cancellationToken, stateTuple);

                // 添加取消注册
                if (cancellationToken.CanBeCanceled)
                {
                    cancellationToken.Register(static s =>
                    {
                        var taskState = (LuminTaskState)s;
                        var tuple = Unsafe.As<StateTuple<PromiseState, SubAssetsHandle, IProgress<float>>>(taskState.StateTuple);

                        if (tuple.Item1.AutoReleaseWhenCanceled && tuple.Item2.IsValid)
                        {
                            tuple.Item2.Release();
                        }
                        LuminTaskSourceCore<SubAssetsHandle>.TrySetCanceled(taskState.Source);
                        
                        tuple.Dispose();
                        LuminTaskSourceCore<SubAssetsHandle>.Dispose(taskState.Source);
                    }, state);
                }

                PlayerLoopHelper.AddAction(timing, state, &MoveNext);

                return new LuminTask<SubAssetsHandle>(LuminTaskSourceCore<SubAssetsHandle>.MethodTable, core, core->Id);
            }

            public static bool MoveNext(in LuminTaskState state)
            {
                var tuple = Unsafe.As<StateTuple<PromiseState, SubAssetsHandle, IProgress<float>>>(state.StateTuple);

                if (tuple.Item1.Completed)
                {
                    tuple.Dispose();
                    return false;
                }

                if (state.CancellationToken.IsCancellationRequested)
                {
                    tuple.Item1.Completed = true;
                    if (tuple.Item1.AutoReleaseWhenCanceled && tuple.Item2.IsValid)
                    {
                        tuple.Item2.Release();
                    }
                    LuminTaskSourceCore<SubAssetsHandle>.TrySetCanceled(state.Source);
                    tuple.Dispose();
                    LuminTaskSourceCore<SubAssetsHandle>.Dispose(state.Source);
                    return false;
                }

                // 检查是否完成
                if (tuple.Item2.IsDone)
                {
                    tuple.Item1.Completed = true;
                    
                    if (tuple.Item2.Status == EOperationStatus.Failed)
                    {
                        LuminTaskSourceCore<SubAssetsHandle>.TrySetException(state.Source, new Exception(tuple.Item2.LastError));
                    }
                    else
                    {
                        LuminTaskSourceCore<SubAssetsHandle>.TrySetResult(state.Source, tuple.Item2);
                    }
                    
                    tuple.Dispose();
                    LuminTaskSourceCore<SubAssetsHandle>.Dispose(state.Source);
                    return false;
                }

                // 报告进度
                if (tuple.Item3 != null && tuple.Item2.IsValid)
                {
                    tuple.Item3.Report(tuple.Item2.Progress);
                }

                return true;
            }
        }

        #endregion
    }
}