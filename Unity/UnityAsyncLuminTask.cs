
using LuminThread.Utility;
using UnityEngine.Networking;
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using UnityEngine;
using LuminThread.TaskSource;

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
            }

            public static unsafe LuminTask Create(AsyncOperation asyncOperation, PlayerLoopTiming timing, IProgress<float> progress, CancellationToken cancellationToken, bool cancelImmediately)
            {
                var core = LuminTaskSourceCore<AsyncUnit>.Create();
                var stateData = new StateData
                {
                    Completed = false,
                    IsCancellationRequested = false
                };

                var stateTuple = StateTuple.Create(stateData);
                var state = new LuminTaskState(core, cancellationToken, asyncOperation, stateTuple);
                
                Action<AsyncOperation> continuation = null;
                continuation = _ =>
                {
                    if (stateData.Completed)
                        return;
                        
                    stateData.Completed = true;
                    stateData.IsCancellationRequested = cancellationToken.IsCancellationRequested;
                    stateTuple.Item1.Completed = true;
                    stateTuple.Item1.IsCancellationRequested = stateData.IsCancellationRequested;
                    
                    if (cancellationToken.IsCancellationRequested)
                    {
                        LuminTaskSourceCore<AsyncUnit>.TrySetCanceled(core);
                    }
                    else
                    {
                        LuminTaskSourceCore<AsyncUnit>.TrySetResult(core);
                    }
                    
                    if (continuation != null)
                    {
                        asyncOperation.completed -= continuation;
                        continuation = null;
                    }

                    stateTuple.Item1.Registration.Dispose();
                    stateTuple.Dispose();
                };

                asyncOperation.completed += continuation;

                if (cancelImmediately && cancellationToken.CanBeCanceled)
                {
                    stateTuple.Item1.Registration = cancellationToken.Register(() =>
                    {
                        stateData.Completed = true;
                        stateData.IsCancellationRequested = true;
                        LuminTaskSourceCore<AsyncUnit>.TrySetCanceled(core);
                    });
                }

                PlayerLoopHelper.AddAction(timing, state, &MoveNext);

                return new LuminTask(LuminTaskSourceCore<AsyncUnit>.MethodTablePtr, core, core->Id);
            }

            public static unsafe bool MoveNext(in LuminTaskState state)
            {
                var asyncOperation = state.State as AsyncOperation;
                if (asyncOperation == null)
                    return false;

                var stateTuple = state.StateTuple as StateTuple<StateData>;
                if (stateTuple == null)
                    return false;

                if (stateTuple.Item1.Completed)
                    return false;

                if (state.CancellationToken.IsCancellationRequested)
                {
                    stateTuple.Item1.Completed = true;
                    stateTuple.Item1.IsCancellationRequested = true;
                    LuminTaskSourceCore<AsyncUnit>.TrySetCanceled(state.Source);
                    stateTuple.Item1.Registration.Dispose();
                    stateTuple.Dispose();
                    return false;
                }

                var progress = (state.StateTuple as StateTuple<StateData, IProgress<float>>)?.Item2;
                progress?.Report(asyncOperation.progress);

                if (asyncOperation.isDone)
                {
                    stateTuple.Item1.Completed = true;
                    LuminTaskSourceCore<AsyncUnit>.TrySetResult(state.Source);
                    stateTuple.Item1.Registration.Dispose();
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
            }

            public static unsafe LuminTask<UnityEngine.Object> Create(ResourceRequest asyncOperation, PlayerLoopTiming timing, IProgress<float> progress, CancellationToken cancellationToken, bool cancelImmediately)
            {
                var core = LuminTaskSourceCore<UnityEngine.Object>.Create();
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
                    stateTuple.Item1.Completed = true;
                    stateTuple.Item1.IsCancellationRequested = stateData.IsCancellationRequested;
                    
                    if (cancellationToken.IsCancellationRequested)
                    {
                        LuminTaskSourceCore<UnityEngine.Object>.TrySetCanceled(core);
                    }
                    else
                    {
                        LuminTaskSourceCore<UnityEngine.Object>.TrySetResult(core, asyncOperation.asset);
                    }
                    
                    if (continuation != null)
                    {
                        asyncOperation.completed -= continuation;
                        continuation = null;
                    }

                    stateTuple.Item1.Registration.Dispose();
                    stateTuple.Dispose();
                };

                asyncOperation.completed += continuation;

                if (cancelImmediately && cancellationToken.CanBeCanceled)
                {
                    stateTuple.Item1.Registration = cancellationToken.Register(() =>
                    {
                        stateData.Completed = true;
                        stateData.IsCancellationRequested = true;
                        LuminTaskSourceCore<UnityEngine.Object>.TrySetCanceled(core);
                    });
                }

                PlayerLoopHelper.AddAction(timing, state, &MoveNext);

                return new LuminTask<UnityEngine.Object>(LuminTaskSourceCore<UnityEngine.Object>.MethodTablePtr, core, core->Id);
            }

            public static unsafe bool MoveNext(in LuminTaskState state)
            {
                var asyncOperation = state.State as ResourceRequest;
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
                    LuminTaskSourceCore<UnityEngine.Object>.TrySetCanceled(state.Source);
                    stateTuple.Item1.Registration.Dispose();
                    stateTuple.Dispose();
                    return false;
                }

                var progress = stateTuple.Item2;
                progress?.Report(asyncOperation.progress);

                if (asyncOperation.isDone)
                {
                    stateTuple.Item1.Completed = true;
                    LuminTaskSourceCore<UnityEngine.Object>.TrySetResult(state.Source, asyncOperation.asset);
                    stateTuple.Item1.Registration.Dispose();
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
            }

            public static unsafe LuminTask<UnityEngine.Object> Create(AssetBundleRequest asyncOperation, PlayerLoopTiming timing, IProgress<float> progress, CancellationToken cancellationToken, bool cancelImmediately)
            {
                var core = LuminTaskSourceCore<UnityEngine.Object>.Create();
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
                    stateTuple.Item1.Completed = true;
                    stateTuple.Item1.IsCancellationRequested = stateData.IsCancellationRequested;
                    
                    if (cancellationToken.IsCancellationRequested)
                    {
                        LuminTaskSourceCore<UnityEngine.Object>.TrySetCanceled(core);
                    }
                    else
                    {
                        LuminTaskSourceCore<UnityEngine.Object>.TrySetResult(core, asyncOperation.asset);
                    }
                    
                    if (continuation != null)
                    {
                        asyncOperation.completed -= continuation;
                        continuation = null;
                    }

                    stateTuple.Item1.Registration.Dispose();
                    stateTuple.Dispose();
                };

                asyncOperation.completed += continuation;

                if (cancelImmediately && cancellationToken.CanBeCanceled)
                {
                    stateTuple.Item1.Registration = cancellationToken.Register(() =>
                    {
                        stateData.Completed = true;
                        stateData.IsCancellationRequested = true;
                        LuminTaskSourceCore<UnityEngine.Object>.TrySetCanceled(core);
                    });
                }

                PlayerLoopHelper.AddAction(timing, state, &MoveNext);

                return new LuminTask<UnityEngine.Object>(LuminTaskSourceCore<UnityEngine.Object>.MethodTablePtr, core, core->Id);
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
                    LuminTaskSourceCore<UnityEngine.Object>.TrySetCanceled(state.Source);
                    stateTuple.Item1.Registration.Dispose();
                    stateTuple.Dispose();
                    return false;
                }

                var progress = stateTuple.Item2;
                progress?.Report(asyncOperation.progress);

                if (asyncOperation.isDone)
                {
                    stateTuple.Item1.Completed = true;
                    LuminTaskSourceCore<UnityEngine.Object>.TrySetResult(state.Source, asyncOperation.asset);
                    stateTuple.Item1.Registration.Dispose();
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
            }

            public static unsafe LuminTask<AssetBundle> Create(AssetBundleCreateRequest asyncOperation, PlayerLoopTiming timing, IProgress<float> progress, CancellationToken cancellationToken, bool cancelImmediately)
            {
                var core = LuminTaskSourceCore<AssetBundle>.Create();
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
                    stateTuple.Item1.Completed = true;
                    stateTuple.Item1.IsCancellationRequested = stateData.IsCancellationRequested;
                    
                    if (cancellationToken.IsCancellationRequested)
                    {
                        LuminTaskSourceCore<AssetBundle>.TrySetCanceled(core);
                    }
                    else
                    {
                        LuminTaskSourceCore<AssetBundle>.TrySetResult(core, asyncOperation.assetBundle);
                    }
                    
                    if (continuation != null)
                    {
                        asyncOperation.completed -= continuation;
                        continuation = null;
                    }

                    stateTuple.Item1.Registration.Dispose();
                    stateTuple.Dispose();
                };

                asyncOperation.completed += continuation;

                if (cancelImmediately && cancellationToken.CanBeCanceled)
                {
                    stateTuple.Item1.Registration = cancellationToken.Register(() =>
                    {
                        stateData.Completed = true;
                        stateData.IsCancellationRequested = true;
                        LuminTaskSourceCore<AssetBundle>.TrySetCanceled(core);
                    });
                }

                PlayerLoopHelper.AddAction(timing, state, &MoveNext);

                return new LuminTask<AssetBundle>(LuminTaskSourceCore<AssetBundle>.MethodTablePtr, core, core->Id);
            }

            public static unsafe bool MoveNext(in LuminTaskState state)
            {
                var asyncOperation = state.State as AssetBundleCreateRequest;
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
                    LuminTaskSourceCore<AssetBundle>.TrySetCanceled(state.Source);
                    stateTuple.Item1.Registration.Dispose();
                    stateTuple.Dispose();
                    return false;
                }

                var progress = stateTuple.Item2;
                progress?.Report(asyncOperation.progress);

                if (asyncOperation.isDone)
                {
                    stateTuple.Item1.Completed = true;
                    LuminTaskSourceCore<AssetBundle>.TrySetResult(state.Source, asyncOperation.assetBundle);
                    stateTuple.Item1.Registration.Dispose();
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
            }

            public static unsafe LuminTask<UnityWebRequest> Create(UnityWebRequestAsyncOperation asyncOperation, PlayerLoopTiming timing, IProgress<float> progress, CancellationToken cancellationToken, bool cancelImmediately)
            {
                var core = LuminTaskSourceCore<UnityWebRequest>.Create();
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
                    stateTuple.Item1.Completed = true;
                    stateTuple.Item1.IsCancellationRequested = stateData.IsCancellationRequested;
                    
                    if (cancellationToken.IsCancellationRequested)
                    {
                        LuminTaskSourceCore<UnityWebRequest>.TrySetCanceled(core);
                    }
                    else if (IsError(asyncOperation.webRequest))
                    {
                        LuminTaskSourceCore<UnityWebRequest>.TrySetException(core, new UnityWebRequestLuminTaskException(asyncOperation.webRequest));
                    }
                    else
                    {
                        LuminTaskSourceCore<UnityWebRequest>.TrySetResult(core, asyncOperation.webRequest);
                    }
                    
                    if (continuation != null)
                    {
                        asyncOperation.completed -= continuation;
                        continuation = null;
                    }

                    stateTuple.Item1.Registration.Dispose();
                    stateTuple.Dispose();
                };

                asyncOperation.completed += continuation;

                if (cancelImmediately && cancellationToken.CanBeCanceled)
                {
                    stateTuple.Item1.Registration = cancellationToken.Register(() =>
                    {
                        stateData.Completed = true;
                        stateData.IsCancellationRequested = true;
                        asyncOperation.webRequest.Abort();
                        LuminTaskSourceCore<UnityWebRequest>.TrySetCanceled(core);
                    });
                }

                PlayerLoopHelper.AddAction(timing, state, &MoveNext);

                return new LuminTask<UnityWebRequest>(LuminTaskSourceCore<UnityWebRequest>.MethodTablePtr, core, core->Id);
            }

            public static unsafe bool MoveNext(in LuminTaskState state)
            {
                var asyncOperation = state.State as UnityWebRequestAsyncOperation;
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
                    asyncOperation.webRequest.Abort();
                    LuminTaskSourceCore<UnityWebRequest>.TrySetCanceled(state.Source);
                    stateTuple.Item1.Registration.Dispose();
                    stateTuple.Dispose();
                    return false;
                }

                var progress = stateTuple.Item2;
                progress?.Report(asyncOperation.progress);

                if (asyncOperation.isDone)
                {
                    stateTuple.Item1.Completed = true;
                    
                    if (IsError(asyncOperation.webRequest))
                    {
                        LuminTaskSourceCore<UnityWebRequest>.TrySetException(state.Source, new UnityWebRequestLuminTaskException(asyncOperation.webRequest));
                    }
                    else
                    {
                        LuminTaskSourceCore<UnityWebRequest>.TrySetResult(state.Source, asyncOperation.webRequest);
                    }
                    
                    stateTuple.Item1.Registration.Dispose();
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
