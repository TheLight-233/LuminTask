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
struct PromiseState
            {
                public bool Completed;
                public bool AutoReleaseWhenCanceled;
                public CancellationTokenRegistration Registration;
                public ulong Handle;
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

                var task = LuminTaskSource.Create(out ulong taskHandle);

                var promiseState = new PromiseState
                {
                    Completed = false,
                    AutoReleaseWhenCanceled = autoReleaseWhenCanceled,
                    Handle = taskHandle
                };

                var stateTuple = StateTuple.Create(promiseState, handle, progress);
                var state = new LuminTaskState(taskHandle, cancellationToken, stateTuple);

                // 添加取消注册
                if (cancellationToken.CanBeCanceled)
                {
                    stateTuple.Item.Item1.Registration = cancellationToken.Register(static s =>
                    {
                        var taskState = (LuminTaskState)s;
                        var tuple = (StateTuple<(PromiseState, HandleBase, IProgress<float>)>)taskState.State!;

                        if (tuple.Item.Item1.AutoReleaseWhenCanceled && tuple.Item.Item2.IsValid)
                        {
                            tuple.Item.Item2.Release();
                        }
                        LuminTaskSource.SetCanceled(tuple.Item.Item1.Handle, taskState.CancellationToken);
                        
                        // 回调自身在途：仅清引用，避免 Dispose 自己造成死锁；释放交给 MoveNext 完成路径。
                        tuple.Item.Item1.Registration = default;
                        tuple.Dispose();
                    }, state);
                }

                PlayerLoopHelper.AddAction(timing, state, MoveNextAction);

                return task;
            }

            private static readonly MoveNext MoveNextAction = MoveNext;

            public static bool MoveNext(in LuminTaskState state)
            {
                var tuple = (StateTuple<(PromiseState, HandleBase, IProgress<float>)>)state.State!;

                if (tuple.Item.Item1.Completed)
                {
                    tuple.Dispose();
                    return false;
                }

                if (state.CancellationToken.IsCancellationRequested)
                {
                    tuple.Item.Item1.Completed = true;
                    if (tuple.Item.Item1.AutoReleaseWhenCanceled && tuple.Item.Item2.IsValid)
                    {
                        tuple.Item.Item2.Release();
                    }
                    LuminTaskSource.SetCanceled(tuple.Item.Item1.Handle, state.CancellationToken);
                    tuple.Item.Item1.Registration.Dispose();
                    tuple.Dispose();
                    return false;
                }

                // 检查是否完成
                if (tuple.Item.Item2.IsDone)
                {
                    tuple.Item.Item1.Completed = true;
                    
                    if (tuple.Item.Item2.Status == EOperationStatus.Failed)
                    {
                        LuminTaskSource.SetException(tuple.Item.Item1.Handle, new Exception(tuple.Item.Item2.LastError));
                    }
                    else
                    {
                        LuminTaskSource.SetResult(tuple.Item.Item1.Handle);
                    }
                    
                    tuple.Item.Item1.Registration.Dispose();
                    tuple.Dispose();
                    return false;
                }

                // 报告进度
                if (tuple.Item.Item3 != null && tuple.Item.Item2.IsValid)
                {
                    tuple.Item.Item3.Report(tuple.Item.Item2.Progress);
                }

                return true;
            }
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
            struct PromiseState
            {
                public bool Completed;
                public bool AutoReleaseWhenCanceled;
                public CancellationTokenRegistration Registration;
                public ulong Handle;
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

                var task = LuminTaskSource.Create<AssetHandle>(out ulong taskHandle);

                var promiseState = new PromiseState
                {
                    Completed = false,
                    AutoReleaseWhenCanceled = autoReleaseWhenCanceled,
                    Handle = taskHandle
                };

                var stateTuple = StateTuple.Create(promiseState, handle, progress);
                var state = new LuminTaskState(taskHandle, cancellationToken, stateTuple);

                // 添加取消注册
                if (cancellationToken.CanBeCanceled)
                {
                    stateTuple.Item.Item1.Registration = cancellationToken.Register(static s =>
                    {
                        var taskState = (LuminTaskState)s;
                        var tuple = (StateTuple<(PromiseState, AssetHandle, IProgress<float>)>)taskState.State!;

                        if (tuple.Item.Item1.AutoReleaseWhenCanceled && tuple.Item.Item2.IsValid)
                        {
                            tuple.Item.Item2.Release();
                        }
                        LuminTaskSource.SetCanceled(tuple.Item.Item1.Handle, taskState.CancellationToken);
                        
                        // 回调自身在途：仅清引用，避免 Dispose 自己造成死锁；释放交给 MoveNext 完成路径。
                        tuple.Item.Item1.Registration = default;
                        tuple.Dispose();
                    }, state);
                }

                PlayerLoopHelper.AddAction(timing, state, MoveNextAction);

                return task;
            }

            private static readonly MoveNext MoveNextAction = MoveNext;

            public static bool MoveNext(in LuminTaskState state)
            {
                var tuple = (StateTuple<(PromiseState, AssetHandle, IProgress<float>)>)state.State!;

                if (tuple.Item.Item1.Completed)
                {
                    tuple.Dispose();
                    return false;
                }

                if (state.CancellationToken.IsCancellationRequested)
                {
                    tuple.Item.Item1.Completed = true;
                    if (tuple.Item.Item1.AutoReleaseWhenCanceled && tuple.Item.Item2.IsValid)
                    {
                        tuple.Item.Item2.Release();
                    }
                    LuminTaskSource.SetCanceled(tuple.Item.Item1.Handle, state.CancellationToken);
                    tuple.Item.Item1.Registration.Dispose();
                    tuple.Dispose();
                    return false;
                }

                // 检查是否完成
                if (tuple.Item.Item2.IsDone)
                {
                    tuple.Item.Item1.Completed = true;
                    
                    if (tuple.Item.Item2.Status == EOperationStatus.Failed)
                    {
                        LuminTaskSource.SetException(tuple.Item.Item1.Handle, new Exception(tuple.Item.Item2.LastError));
                    }
                    else
                    {
                        LuminTaskSource.SetResult(tuple.Item.Item1.Handle, tuple.Item.Item2);
                    }
                    
                    tuple.Item.Item1.Registration.Dispose();
                    tuple.Dispose();
                    return false;
                }

                // 报告进度
                if (tuple.Item.Item3 != null && tuple.Item.Item2.IsValid)
                {
                    tuple.Item.Item3.Report(tuple.Item.Item2.Progress);
                }

                return true;
            }
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
            struct PromiseState
            {
                public bool Completed;
                public bool AutoReleaseWhenCanceled;
                public CancellationTokenRegistration Registration;
                public ulong Handle;
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

                var task = LuminTaskSource.Create<SceneHandle>(out ulong taskHandle);

                var promiseState = new PromiseState
                {
                    Completed = false,
                    AutoReleaseWhenCanceled = autoReleaseWhenCanceled,
                    Handle = taskHandle
                };

                var stateTuple = StateTuple.Create(promiseState, handle, progress);
                var state = new LuminTaskState(taskHandle, cancellationToken, stateTuple);

                // 添加取消注册
                if (cancellationToken.CanBeCanceled)
                {
                    stateTuple.Item.Item1.Registration = cancellationToken.Register(static s =>
                    {
                        var taskState = (LuminTaskState)s;
                        var tuple = (StateTuple<(PromiseState, SceneHandle, IProgress<float>)>)taskState.State!;

                        if (tuple.Item.Item1.AutoReleaseWhenCanceled && tuple.Item.Item2.IsValid)
                        {
                            tuple.Item.Item2.Release();
                        }
                        LuminTaskSource.SetCanceled(tuple.Item.Item1.Handle, taskState.CancellationToken);
                        
                        // 回调自身在途：仅清引用，避免 Dispose 自己造成死锁；释放交给 MoveNext 完成路径。
                        tuple.Item.Item1.Registration = default;
                        tuple.Dispose();
                    }, state);
                }

                PlayerLoopHelper.AddAction(timing, state, MoveNextAction);

                return task;
            }

            private static readonly MoveNext MoveNextAction = MoveNext;

            public static bool MoveNext(in LuminTaskState state)
            {
                var tuple = (StateTuple<(PromiseState, SceneHandle, IProgress<float>)>)state.State!;

                if (tuple.Item.Item1.Completed)
                {
                    tuple.Dispose();
                    return false;
                }

                if (state.CancellationToken.IsCancellationRequested)
                {
                    tuple.Item.Item1.Completed = true;
                    if (tuple.Item.Item1.AutoReleaseWhenCanceled && tuple.Item.Item2.IsValid)
                    {
                        tuple.Item.Item2.Release();
                    }
                    LuminTaskSource.SetCanceled(tuple.Item.Item1.Handle, state.CancellationToken);
                    tuple.Item.Item1.Registration.Dispose();
                    tuple.Dispose();
                    return false;
                }

                // 检查是否完成
                if (tuple.Item.Item2.IsDone)
                {
                    tuple.Item.Item1.Completed = true;
                    
                    if (tuple.Item.Item2.Status == EOperationStatus.Failed)
                    {
                        LuminTaskSource.SetException(tuple.Item.Item1.Handle, new Exception(tuple.Item.Item2.LastError));
                    }
                    else
                    {
                        LuminTaskSource.SetResult(tuple.Item.Item1.Handle, tuple.Item.Item2);
                    }
                    
                    tuple.Item.Item1.Registration.Dispose();
                    tuple.Dispose();
                    return false;
                }

                // 报告进度
                if (tuple.Item.Item3 != null && tuple.Item.Item2.IsValid)
                {
                    tuple.Item.Item3.Report(tuple.Item.Item2.Progress);
                }

                return true;
            }
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
            struct PromiseState
            {
                public bool Completed;
                public bool AutoReleaseWhenCanceled;
                public CancellationTokenRegistration Registration;
                public ulong Handle;
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

                var task = LuminTaskSource.Create<RawFileHandle>(out ulong taskHandle);

                var promiseState = new PromiseState
                {
                    Completed = false,
                    AutoReleaseWhenCanceled = autoReleaseWhenCanceled,
                    Handle = taskHandle
                };

                var stateTuple = StateTuple.Create(promiseState, handle, progress);
                var state = new LuminTaskState(taskHandle, cancellationToken, stateTuple);

                // 添加取消注册
                if (cancellationToken.CanBeCanceled)
                {
                    stateTuple.Item.Item1.Registration = cancellationToken.Register(static s =>
                    {
                        var taskState = (LuminTaskState)s;
                        var tuple = (StateTuple<(PromiseState, RawFileHandle, IProgress<float>)>)taskState.State!;

                        if (tuple.Item.Item1.AutoReleaseWhenCanceled && tuple.Item.Item2.IsValid)
                        {
                            tuple.Item.Item2.Release();
                        }
                        LuminTaskSource.SetCanceled(tuple.Item.Item1.Handle, taskState.CancellationToken);
                        
                        // 回调自身在途：仅清引用，避免 Dispose 自己造成死锁；释放交给 MoveNext 完成路径。
                        tuple.Item.Item1.Registration = default;
                        tuple.Dispose();
                    }, state);
                }

                PlayerLoopHelper.AddAction(timing, state, MoveNextAction);

                return task;
            }

            private static readonly MoveNext MoveNextAction = MoveNext;

            public static bool MoveNext(in LuminTaskState state)
            {
                var tuple = (StateTuple<(PromiseState, RawFileHandle, IProgress<float>)>)state.State!;

                if (tuple.Item.Item1.Completed)
                {
                    tuple.Dispose();
                    return false;
                }

                if (state.CancellationToken.IsCancellationRequested)
                {
                    tuple.Item.Item1.Completed = true;
                    if (tuple.Item.Item1.AutoReleaseWhenCanceled && tuple.Item.Item2.IsValid)
                    {
                        tuple.Item.Item2.Release();
                    }
                    LuminTaskSource.SetCanceled(tuple.Item.Item1.Handle, state.CancellationToken);
                    tuple.Item.Item1.Registration.Dispose();
                    tuple.Dispose();
                    return false;
                }

                // 检查是否完成
                if (tuple.Item.Item2.IsDone)
                {
                    tuple.Item.Item1.Completed = true;
                    
                    if (tuple.Item.Item2.Status == EOperationStatus.Failed)
                    {
                        LuminTaskSource.SetException(tuple.Item.Item1.Handle, new Exception(tuple.Item.Item2.LastError));
                    }
                    else
                    {
                        LuminTaskSource.SetResult(tuple.Item.Item1.Handle, tuple.Item.Item2);
                    }
                    
                    tuple.Item.Item1.Registration.Dispose();
                    tuple.Dispose();
                    return false;
                }

                // 报告进度
                if (tuple.Item.Item3 != null && tuple.Item.Item2.IsValid)
                {
                    tuple.Item.Item3.Report(tuple.Item.Item2.Progress);
                }

                return true;
            }
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
            struct PromiseState
            {
                public bool Completed;
                public bool AutoReleaseWhenCanceled;
                public CancellationTokenRegistration Registration;
                public ulong Handle;
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

                var task = LuminTaskSource.Create<SubAssetsHandle>(out ulong taskHandle);

                var promiseState = new PromiseState
                {
                    Completed = false,
                    AutoReleaseWhenCanceled = autoReleaseWhenCanceled,
                    Handle = taskHandle
                };

                var stateTuple = StateTuple.Create(promiseState, handle, progress);
                var state = new LuminTaskState(taskHandle, cancellationToken, stateTuple);

                // 添加取消注册
                if (cancellationToken.CanBeCanceled)
                {
                    stateTuple.Item.Item1.Registration = cancellationToken.Register(static s =>
                    {
                        var taskState = (LuminTaskState)s;
                        var tuple = (StateTuple<(PromiseState, SubAssetsHandle, IProgress<float>)>)taskState.State!;

                        if (tuple.Item.Item1.AutoReleaseWhenCanceled && tuple.Item.Item2.IsValid)
                        {
                            tuple.Item.Item2.Release();
                        }
                        LuminTaskSource.SetCanceled(tuple.Item.Item1.Handle, taskState.CancellationToken);
                        
                        // 回调自身在途：仅清引用，避免 Dispose 自己造成死锁；释放交给 MoveNext 完成路径。
                        tuple.Item.Item1.Registration = default;
                        tuple.Dispose();
                    }, state);
                }

                PlayerLoopHelper.AddAction(timing, state, MoveNextAction);

                return task;
            }

            private static readonly MoveNext MoveNextAction = MoveNext;

            public static bool MoveNext(in LuminTaskState state)
            {
                var tuple = (StateTuple<(PromiseState, SubAssetsHandle, IProgress<float>)>)state.State!;

                if (tuple.Item.Item1.Completed)
                {
                    tuple.Dispose();
                    return false;
                }

                if (state.CancellationToken.IsCancellationRequested)
                {
                    tuple.Item.Item1.Completed = true;
                    if (tuple.Item.Item1.AutoReleaseWhenCanceled && tuple.Item.Item2.IsValid)
                    {
                        tuple.Item.Item2.Release();
                    }
                    LuminTaskSource.SetCanceled(tuple.Item.Item1.Handle, state.CancellationToken);
                    tuple.Item.Item1.Registration.Dispose();
                    tuple.Dispose();
                    return false;
                }

                // 检查是否完成
                if (tuple.Item.Item2.IsDone)
                {
                    tuple.Item.Item1.Completed = true;
                    
                    if (tuple.Item.Item2.Status == EOperationStatus.Failed)
                    {
                        LuminTaskSource.SetException(tuple.Item.Item1.Handle, new Exception(tuple.Item.Item2.LastError));
                    }
                    else
                    {
                        LuminTaskSource.SetResult(tuple.Item.Item1.Handle, tuple.Item.Item2);
                    }
                    
                    tuple.Item.Item1.Registration.Dispose();
                    tuple.Dispose();
                    return false;
                }

                // 报告进度
                if (tuple.Item.Item3 != null && tuple.Item.Item2.IsValid)
                {
                    tuple.Item.Item3.Report(tuple.Item.Item2.Progress);
                }

                return true;
            }
        }

        #endregion
    }
}
