
// AsyncInstantiateOperation was added since Unity 2022.3.20 / 2023.3.0b7
#if UNITY_2022_3 && !(UNITY_2022_3_0 || UNITY_2022_3_1 || UNITY_2022_3_2 || UNITY_2022_3_3 || UNITY_2022_3_4 || UNITY_2022_3_5 || UNITY_2022_3_6 || UNITY_2022_3_7 || UNITY_2022_3_8 || UNITY_2022_3_9 || UNITY_2022_3_10 || UNITY_2022_3_11 || UNITY_2022_3_12 || UNITY_2022_3_13 || UNITY_2022_3_14 || UNITY_2022_3_15 || UNITY_2022_3_16 || UNITY_2022_3_17 || UNITY_2022_3_18 || UNITY_2022_3_19)
#define UNITY_2022_SUPPORT
#endif

#if UNITY_2022_SUPPORT || UNITY_2023_3_OR_NEWER

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using UnityEngine;
using LuminThread;
using LuminThread.Interface;
using LuminThread.TaskSource;
using LuminThread.Utility;

namespace LuminThread.Unity
{
    public static class AsyncInstantiateOperationExtensions
    {
        // AsyncInstantiateOperation<T> has GetAwaiter so no need to impl
        // public static LuminTaskAwaiter<T[]> GetAwaiter<T>(this AsyncInstantiateOperation<T> operation) where T : Object

        public static LuminTask<UnityEngine.Object[]> WithCancellation(this AsyncInstantiateOperation asyncOperation, CancellationToken cancellationToken)
        {
            return ToLuminTask(asyncOperation, cancellationToken: cancellationToken);
        }

        public static LuminTask<UnityEngine.Object[]> WithCancellation(this AsyncInstantiateOperation asyncOperation, CancellationToken cancellationToken, bool cancelImmediately)
        {
            return ToLuminTask(asyncOperation, cancellationToken: cancellationToken, cancelImmediately: cancelImmediately);
        }

        public static LuminTask<UnityEngine.Object[]> ToLuminTask(this AsyncInstantiateOperation asyncOperation, IProgress<float> progress = null, PlayerLoopTiming timing = PlayerLoopTiming.Update, CancellationToken cancellationToken = default, bool cancelImmediately = false)
        {
            if (asyncOperation == null)
                throw new ArgumentNullException(nameof(asyncOperation));
            if (cancellationToken.IsCancellationRequested) 
                return LuminTask.FromCanceled<UnityEngine.Object[]>(cancellationToken);
            if (asyncOperation.isDone) 
                return LuminTask.FromResult(asyncOperation.Result);
            return AsyncInstantiateOperationPromise.Create(asyncOperation, timing, progress, cancellationToken, cancelImmediately);
        }

        public static LuminTask<T[]> WithCancellation<T>(this AsyncInstantiateOperation<T> asyncOperation, CancellationToken cancellationToken)
            where T : UnityEngine.Object
        {
            return ToLuminTask(asyncOperation, cancellationToken: cancellationToken);
        }

        public static LuminTask<T[]> WithCancellation<T>(this AsyncInstantiateOperation<T> asyncOperation, CancellationToken cancellationToken, bool cancelImmediately)
            where T : UnityEngine.Object
        {
            return ToLuminTask(asyncOperation, cancellationToken: cancellationToken, cancelImmediately: cancelImmediately);
        }

        public static LuminTask<T[]> ToLuminTask<T>(this AsyncInstantiateOperation<T> asyncOperation, IProgress<float> progress = null, PlayerLoopTiming timing = PlayerLoopTiming.Update, CancellationToken cancellationToken = default, bool cancelImmediately = false)
            where T : UnityEngine.Object
        {
            if (asyncOperation == null)
                throw new ArgumentNullException(nameof(asyncOperation));
            if (cancellationToken.IsCancellationRequested) 
                return LuminTask.FromCanceled<T[]>(cancellationToken);
            if (asyncOperation.isDone) 
                return LuminTask.FromResult(asyncOperation.Result);
            return AsyncInstantiateOperationPromise<T>.Create(asyncOperation, timing, progress, cancellationToken, cancelImmediately);
        }

        private static class AsyncInstantiateOperationPromise
        {
            [StructLayout(LayoutKind.Sequential)]
            private struct StateData
            {
                public bool Completed;
                public bool IsCancellationRequested;
            }

            public static unsafe LuminTask<UnityEngine.Object[]> Create(AsyncInstantiateOperation asyncOperation, PlayerLoopTiming timing, IProgress<float> progress, CancellationToken cancellationToken, bool cancelImmediately)
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
                        LuminTaskSourceCore<UnityEngine.Object[]>.TrySetResult(core, asyncOperation.Result);
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
                var asyncOperation = state.State as AsyncInstantiateOperation;
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
                    LuminTaskSourceCore<UnityEngine.Object[]>.TrySetResult(state.Source, asyncOperation.Result);
                    stateTuple.Dispose();
                    return false;
                }

                return true;
            }
        }

        private static class AsyncInstantiateOperationPromise<T> where T : UnityEngine.Object
        {
            [StructLayout(LayoutKind.Sequential)]
            private struct StateData
            {
                public bool Completed;
                public bool IsCancellationRequested;
            }

            public static unsafe LuminTask<T[]> Create(AsyncInstantiateOperation<T> asyncOperation, PlayerLoopTiming timing, IProgress<float> progress, CancellationToken cancellationToken, bool cancelImmediately)
            {
                var core = LuminTaskSourceCore<T[]>.Create();
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
                        LuminTaskSourceCore<T[]>.TrySetCanceled(core);
                    }
                    else
                    {
                        LuminTaskSourceCore<T[]>.TrySetResult(core, asyncOperation.Result);
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
                        LuminTaskSourceCore<T[]>.TrySetCanceled(core);
                    });
                }

                PlayerLoopHelper.AddAction(timing, state, &MoveNext);

                return new LuminTask<T[]>(LuminTaskSourceCore<T[]>.MethodTable, core, core->Id);
            }

            public static unsafe bool MoveNext(in LuminTaskState state)
            {
                var asyncOperation = state.State as AsyncInstantiateOperation<T>;
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
                    LuminTaskSourceCore<T[]>.TrySetCanceled(state.Source);
                    stateTuple.Dispose();
                    return false;
                }

                var progress = stateTuple.Item2;
                progress?.Report(asyncOperation.progress);

                if (asyncOperation.isDone)
                {
                    stateTuple.Item1.Completed = true;
                    LuminTaskSourceCore<T[]>.TrySetResult(state.Source, asyncOperation.Result);
                    stateTuple.Dispose();
                    return false;
                }

                return true;
            }
        }
    }
}

#endif
