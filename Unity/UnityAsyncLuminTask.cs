
using LuminThread.Utility;
using UnityEngine.Networking;
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using UnityEngine;
using LuminThread.TaskSource;
using LuminThread.Interface;

namespace LuminThread.Unity
{
    public static partial class UnityAsyncLuminTask
    {
        #region AsyncOperation

        public static AsyncOperationLuminTaskAwaiter GetAwaiter(this AsyncOperation asyncOperation)
        {
            if (asyncOperation == null)
                throw new ArgumentNullException(nameof(asyncOperation));
            return new AsyncOperationLuminTaskAwaiter(asyncOperation);
        }

        public static LuminTask WithCancellation(this AsyncOperation asyncOperation, CancellationToken cancellationToken)
        {
            return ToLuminTask(asyncOperation, cancellationToken: cancellationToken);
        }

        public static LuminTask WithCancellation(this AsyncOperation asyncOperation, CancellationToken cancellationToken, bool cancelImmediately)
        {
            return ToLuminTask(asyncOperation, cancellationToken: cancellationToken, cancelImmediately: cancelImmediately);
        }

        public static LuminTask ToLuminTask(this AsyncOperation asyncOperation, IProgress<float> progress = null, PlayerLoopTiming timing = PlayerLoopTiming.Update, CancellationToken cancellationToken = default, bool cancelImmediately = false)
        {
            if (asyncOperation == null)
                throw new ArgumentNullException(nameof(asyncOperation));
            if (cancellationToken.IsCancellationRequested)
                return LuminTask.FromCanceled(cancellationToken);
            if (asyncOperation.isDone)
                return LuminTask.CompletedTask();
            return AsyncOperationPromise.Create(asyncOperation, timing, progress, cancellationToken, cancelImmediately);
        }

        public struct AsyncOperationLuminTaskAwaiter : ICriticalNotifyCompletion
        {
            private AsyncOperation _asyncOperation;
            private Action<AsyncOperation> _continuationAction;

            public AsyncOperationLuminTaskAwaiter(AsyncOperation asyncOperation)
            {
                _asyncOperation = asyncOperation;
                _continuationAction = null;
            }

            public bool IsCompleted => _asyncOperation.isDone;

            public void GetResult()
            {
                if (_continuationAction != null)
                {
                    _asyncOperation.completed -= _continuationAction;
                    _continuationAction = null;
                }
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

        private static class AsyncOperationPromise
        {
            [StructLayout(LayoutKind.Sequential)]
            private struct StateData
            {
                public bool Completed;
                public bool IsCancellationRequested;
                public CancellationTokenRegistration Registration;
                public ulong Handle;
            }

            public static unsafe LuminTask Create(AsyncOperation asyncOperation, PlayerLoopTiming timing, IProgress<float> progress, CancellationToken cancellationToken, bool cancelImmediately)
            {
                var task = LuminTaskSource.Create(out ulong handle);
                var stateData = new StateData
                {
                    Completed = false,
                    IsCancellationRequested = false,
                    Handle = handle
                };

                var stateTuple = StateTuple.Create(stateData, asyncOperation);
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
                        LuminTaskSource.SetResult(handle);
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
                var stateTuple = state.State as StateTuple<(StateData, AsyncOperation)>;
                if (stateTuple == null)
                    return false;

                var asyncOperation = stateTuple.Item.Item2;

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

                var progress = (state.State as StateTuple<(StateData, IProgress<float>)>)?.Item.Item2;
                progress?.Report(asyncOperation.progress);

                if (asyncOperation.isDone)
                {
                    stateTuple.Item.Item1.Completed = true;
                    LuminTaskSource.SetResult(stateTuple.Item.Item1.Handle);
                    stateTuple.Item.Item1.Registration.Dispose();
                    stateTuple.Dispose();
                    return false;
                }

                return true;
            }
        }

        #endregion

        #region ResourceRequest

        public static ResourceRequestLuminTaskAwaiter GetAwaiter(this ResourceRequest asyncOperation)
        {
            if (asyncOperation == null)
                throw new ArgumentNullException(nameof(asyncOperation));
            return new ResourceRequestLuminTaskAwaiter(asyncOperation);
        }

        public static LuminTask<UnityEngine.Object> WithCancellation(this ResourceRequest asyncOperation, CancellationToken cancellationToken)
        {
            return ToLuminTask(asyncOperation, cancellationToken: cancellationToken);
        }

        public static LuminTask<UnityEngine.Object> WithCancellation(this ResourceRequest asyncOperation, CancellationToken cancellationToken, bool cancelImmediately)
        {
            return ToLuminTask(asyncOperation, cancellationToken: cancellationToken, cancelImmediately: cancelImmediately);
        }

        public static LuminTask<UnityEngine.Object> ToLuminTask(this ResourceRequest asyncOperation, IProgress<float> progress = null, PlayerLoopTiming timing = PlayerLoopTiming.Update, CancellationToken cancellationToken = default, bool cancelImmediately = false)
        {
            if (asyncOperation == null)
                throw new ArgumentNullException(nameof(asyncOperation));
            if (cancellationToken.IsCancellationRequested)
                return LuminTask.FromCanceled<UnityEngine.Object>(cancellationToken);
            if (asyncOperation.isDone)
                return LuminTask.FromResult(asyncOperation.asset);
            return ResourceRequestPromise.Create(asyncOperation, timing, progress, cancellationToken, cancelImmediately);
        }

        public struct ResourceRequestLuminTaskAwaiter : ICriticalNotifyCompletion
        {
            private ResourceRequest _asyncOperation;
            private Action<AsyncOperation> _continuationAction;

            public ResourceRequestLuminTaskAwaiter(ResourceRequest asyncOperation)
            {
                _asyncOperation = asyncOperation;
                _continuationAction = null;
            }

            public bool IsCompleted => _asyncOperation.isDone;

            public UnityEngine.Object GetResult()
            {
                if (_continuationAction != null)
                {
                    _asyncOperation.completed -= _continuationAction;
                    _continuationAction = null;
                }
                return _asyncOperation.asset;
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

        private static class ResourceRequestPromise
        {
            [StructLayout(LayoutKind.Sequential)]
            private struct StateData
            {
                public bool Completed;
                public bool IsCancellationRequested;
                public CancellationTokenRegistration Registration;
                public ulong Handle;
            }

            public static unsafe LuminTask<UnityEngine.Object> Create(ResourceRequest asyncOperation, PlayerLoopTiming timing, IProgress<float> progress, CancellationToken cancellationToken, bool cancelImmediately)
            {
                var task = LuminTaskSource.Create<UnityEngine.Object>(out ulong handle);
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
                        LuminTaskSource.SetResult(handle, asyncOperation.asset);
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
                var stateTuple = state.State as StateTuple<(StateData, IProgress<float>, ResourceRequest)>;
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
                    LuminTaskSource.SetResult(stateTuple.Item.Item1.Handle, asyncOperation.asset);
                    stateTuple.Item.Item1.Registration.Dispose();
                    stateTuple.Dispose();
                    return false;
                }

                return true;
            }
        }

        #endregion

        #region AssetBundleRequest

        public static AssetBundleRequestLuminTaskAwaiter GetAwaiter(this AssetBundleRequest asyncOperation)
        {
            if (asyncOperation == null)
                throw new ArgumentNullException(nameof(asyncOperation));
            return new AssetBundleRequestLuminTaskAwaiter(asyncOperation);
        }

        public static LuminTask<UnityEngine.Object> WithCancellation(this AssetBundleRequest asyncOperation, CancellationToken cancellationToken)
        {
            return ToLuminTask(asyncOperation, cancellationToken: cancellationToken);
        }

        public static LuminTask<UnityEngine.Object> WithCancellation(this AssetBundleRequest asyncOperation, CancellationToken cancellationToken, bool cancelImmediately)
        {
            return ToLuminTask(asyncOperation, cancellationToken: cancellationToken, cancelImmediately: cancelImmediately);
        }

        public static LuminTask<UnityEngine.Object> ToLuminTask(this AssetBundleRequest asyncOperation, IProgress<float> progress = null, PlayerLoopTiming timing = PlayerLoopTiming.Update, CancellationToken cancellationToken = default, bool cancelImmediately = false)
        {
            if (asyncOperation == null)
                throw new ArgumentNullException(nameof(asyncOperation));
            if (cancellationToken.IsCancellationRequested)
                return LuminTask.FromCanceled<UnityEngine.Object>(cancellationToken);
            if (asyncOperation.isDone)
                return LuminTask.FromResult(asyncOperation.asset);
            return AssetBundleRequestPromise.Create(asyncOperation, timing, progress, cancellationToken, cancelImmediately);
        }

        public struct AssetBundleRequestLuminTaskAwaiter : ICriticalNotifyCompletion
        {
            private AssetBundleRequest _asyncOperation;
            private Action<AsyncOperation> _continuationAction;

            public AssetBundleRequestLuminTaskAwaiter(AssetBundleRequest asyncOperation)
            {
                _asyncOperation = asyncOperation;
                _continuationAction = null;
            }

            public bool IsCompleted => _asyncOperation.isDone;

            public UnityEngine.Object GetResult()
            {
                if (_continuationAction != null)
                {
                    _asyncOperation.completed -= _continuationAction;
                    _continuationAction = null;
                }
                return _asyncOperation.asset;
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

        private static class AssetBundleRequestPromise
        {
            [StructLayout(LayoutKind.Sequential)]
            private struct StateData
            {
                public bool Completed;
                public bool IsCancellationRequested;
                public CancellationTokenRegistration Registration;
                public ulong Handle;
            }

            public static unsafe LuminTask<UnityEngine.Object> Create(AssetBundleRequest asyncOperation, PlayerLoopTiming timing, IProgress<float> progress, CancellationToken cancellationToken, bool cancelImmediately)
            {
                var task = LuminTaskSource.Create<UnityEngine.Object>(out ulong handle);
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
                        LuminTaskSource.SetResult(handle, asyncOperation.asset);
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
                    LuminTaskSource.SetResult(stateTuple.Item.Item1.Handle, asyncOperation.asset);
                    stateTuple.Item.Item1.Registration.Dispose();
                    stateTuple.Dispose();
                    return false;
                }

                return true;
            }
        }

        #endregion
        
        #region AssetBundleCreateRequest

        public static AssetBundleCreateRequestLuminTaskAwaiter GetAwaiter(this AssetBundleCreateRequest asyncOperation)
        {
            if (asyncOperation == null)
                throw new ArgumentNullException(nameof(asyncOperation));
            return new AssetBundleCreateRequestLuminTaskAwaiter(asyncOperation);
        }

        public static LuminTask<AssetBundle> WithCancellation(this AssetBundleCreateRequest asyncOperation, CancellationToken cancellationToken)
        {
            return ToLuminTask(asyncOperation, cancellationToken: cancellationToken);
        }

        public static LuminTask<AssetBundle> WithCancellation(this AssetBundleCreateRequest asyncOperation, CancellationToken cancellationToken, bool cancelImmediately)
        {
            return ToLuminTask(asyncOperation, cancellationToken: cancellationToken, cancelImmediately: cancelImmediately);
        }

        public static LuminTask<AssetBundle> ToLuminTask(this AssetBundleCreateRequest asyncOperation, IProgress<float> progress = null, PlayerLoopTiming timing = PlayerLoopTiming.Update, CancellationToken cancellationToken = default, bool cancelImmediately = false)
        {
            if (asyncOperation == null)
                throw new ArgumentNullException(nameof(asyncOperation));
            if (cancellationToken.IsCancellationRequested)
                return LuminTask.FromCanceled<AssetBundle>(cancellationToken);
            if (asyncOperation.isDone)
                return LuminTask.FromResult(asyncOperation.assetBundle);
            return AssetBundleCreateRequestPromise.Create(asyncOperation, timing, progress, cancellationToken, cancelImmediately);
        }

        public struct AssetBundleCreateRequestLuminTaskAwaiter : ICriticalNotifyCompletion
        {
            private AssetBundleCreateRequest _asyncOperation;
            private Action<AsyncOperation> _continuationAction;

            public AssetBundleCreateRequestLuminTaskAwaiter(AssetBundleCreateRequest asyncOperation)
            {
                _asyncOperation = asyncOperation;
                _continuationAction = null;
            }

            public bool IsCompleted => _asyncOperation.isDone;

            public AssetBundle GetResult()
            {
                if (_continuationAction != null)
                {
                    _asyncOperation.completed -= _continuationAction;
                    _continuationAction = null;
                }
                return _asyncOperation.assetBundle;
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

        private static class AssetBundleCreateRequestPromise
        {
            [StructLayout(LayoutKind.Sequential)]
            private struct StateData
            {
                public bool Completed;
                public bool IsCancellationRequested;
                public CancellationTokenRegistration Registration;
                public ulong Handle;
            }

            public static unsafe LuminTask<AssetBundle> Create(AssetBundleCreateRequest asyncOperation, PlayerLoopTiming timing, IProgress<float> progress, CancellationToken cancellationToken, bool cancelImmediately)
            {
                var task = LuminTaskSource.Create<AssetBundle>(out ulong handle);
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
                        LuminTaskSource.SetResult(handle, asyncOperation.assetBundle);
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
                var stateTuple = state.State as StateTuple<(StateData, IProgress<float>, AssetBundleCreateRequest)>;
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
                    LuminTaskSource.SetResult(stateTuple.Item.Item1.Handle, asyncOperation.assetBundle);
                    stateTuple.Item.Item1.Registration.Dispose();
                    stateTuple.Dispose();
                    return false;
                }

                return true;
            }
        }

        #endregion
        
        #region UnityWebRequestAsyncOperation

        public static UnityWebRequestOperationLuminTaskAwaiter GetAwaiter(this UnityWebRequestAsyncOperation asyncOperation)
        {
            if (asyncOperation == null)
                throw new ArgumentNullException(nameof(asyncOperation));
            return new UnityWebRequestOperationLuminTaskAwaiter(asyncOperation);
        }

        public static LuminTask<UnityWebRequest> WithCancellation(this UnityWebRequestAsyncOperation asyncOperation, CancellationToken cancellationToken)
        {
            return ToLuminTask(asyncOperation, cancellationToken: cancellationToken);
        }

        public static LuminTask<UnityWebRequest> WithCancellation(this UnityWebRequestAsyncOperation asyncOperation, CancellationToken cancellationToken, bool cancelImmediately)
        {
            return ToLuminTask(asyncOperation, cancellationToken: cancellationToken, cancelImmediately: cancelImmediately);
        }

        public static LuminTask<UnityWebRequest> ToLuminTask(this UnityWebRequestAsyncOperation asyncOperation, IProgress<float> progress = null, PlayerLoopTiming timing = PlayerLoopTiming.Update, CancellationToken cancellationToken = default, bool cancelImmediately = false)
        {
            if (asyncOperation == null)
                throw new ArgumentNullException(nameof(asyncOperation));
            if (cancellationToken.IsCancellationRequested)
                return LuminTask.FromCanceled<UnityWebRequest>(cancellationToken);
            if (asyncOperation.isDone)
            {
                if (IsError(asyncOperation.webRequest))
                {
                    return LuminTask.FromException<UnityWebRequest>(new UnityWebRequestLuminTaskException(asyncOperation.webRequest));
                }
                return LuminTask.FromResult(asyncOperation.webRequest);
            }
            return UnityWebRequestAsyncOperationPromise.Create(asyncOperation, timing, progress, cancellationToken, cancelImmediately);
        }

        public struct UnityWebRequestOperationLuminTaskAwaiter : ICriticalNotifyCompletion
        {
            private UnityWebRequestAsyncOperation _asyncOperation;
            private Action<AsyncOperation> _continuationAction;

            public UnityWebRequestOperationLuminTaskAwaiter(UnityWebRequestAsyncOperation asyncOperation)
            {
                _asyncOperation = asyncOperation;
                _continuationAction = null;
            }

            public bool IsCompleted => _asyncOperation.isDone;

            public UnityWebRequest GetResult()
            {
                if (_continuationAction != null)
                {
                    _asyncOperation.completed -= _continuationAction;
                    _continuationAction = null;
                }
                
                if (IsError(_asyncOperation.webRequest))
                {
                    throw new UnityWebRequestLuminTaskException(_asyncOperation.webRequest);
                }
                return _asyncOperation.webRequest;
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

        private static class UnityWebRequestAsyncOperationPromise
        {
            [StructLayout(LayoutKind.Sequential)]
            private struct StateData
            {
                public bool Completed;
                public bool IsCancellationRequested;
                public CancellationTokenRegistration Registration;
                public ulong Handle;
            }

            public static unsafe LuminTask<UnityWebRequest> Create(UnityWebRequestAsyncOperation asyncOperation, PlayerLoopTiming timing, IProgress<float> progress, CancellationToken cancellationToken, bool cancelImmediately)
            {
                var task = LuminTaskSource.Create<UnityWebRequest>(out ulong handle);
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
                    else if (IsError(asyncOperation.webRequest))
                    {
                        LuminTaskSource.SetException(handle, new UnityWebRequestLuminTaskException(asyncOperation.webRequest));
                    }
                    else
                    {
                        LuminTaskSource.SetResult(handle, asyncOperation.webRequest);
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
                        asyncOperation.webRequest.Abort();
                        LuminTaskSource.SetCanceled(handle, cancellationToken);
                    });
                }

                PlayerLoopHelper.AddAction(timing, state, MoveNextAction);

                return task;
            }

            private static readonly MoveNext MoveNextAction = MoveNext;

            public static bool MoveNext(in LuminTaskState state)
            {
                var stateTuple = state.State as StateTuple<(StateData, IProgress<float>, UnityWebRequestAsyncOperation)>;
                if (stateTuple == null)
                    return false;

                var asyncOperation = stateTuple.Item.Item3;

                if (stateTuple.Item.Item1.Completed)
                    return false;

                if (state.CancellationToken.IsCancellationRequested)
                {
                    stateTuple.Item.Item1.Completed = true;
                    stateTuple.Item.Item1.IsCancellationRequested = true;
                    asyncOperation.webRequest.Abort();
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
                    
                    if (IsError(asyncOperation.webRequest))
                    {
                        LuminTaskSource.SetException(stateTuple.Item.Item1.Handle, new UnityWebRequestLuminTaskException(asyncOperation.webRequest));
                    }
                    else
                    {
                        LuminTaskSource.SetResult(stateTuple.Item.Item1.Handle, asyncOperation.webRequest);
                    }
                    
                    stateTuple.Item.Item1.Registration.Dispose();
                    stateTuple.Dispose();
                    return false;
                }

                return true;
            }
        }

        #endregion

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsError(UnityWebRequest request)
        {
#if UNITY_2020_2_OR_NEWER
            return request.result == UnityWebRequest.Result.ConnectionError ||
                   request.result == UnityWebRequest.Result.ProtocolError ||
                   request.result == UnityWebRequest.Result.DataProcessingError;
#else
            return request.isNetworkError || request.isHttpError;
#endif
        }

    }
}
