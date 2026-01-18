
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using UnityEngine.Rendering;
using LuminThread;
using LuminThread.Interface;
using LuminThread.TaskSource;
using LuminThread.Utility;

namespace LuminThread.Unity
{
    public static partial class UnityAsyncExtensions
    {
        #region AsyncGPUReadbackRequest

        public static LuminTaskAwaiter<AsyncGPUReadbackRequest> GetAwaiter(this AsyncGPUReadbackRequest asyncOperation)
        {
            return ToLuminTask(asyncOperation).GetAwaiter();
        }

        public static LuminTask<AsyncGPUReadbackRequest> WithCancellation(this AsyncGPUReadbackRequest asyncOperation, CancellationToken cancellationToken)
        {
            return ToLuminTask(asyncOperation, cancellationToken: cancellationToken);
        }

        public static LuminTask<AsyncGPUReadbackRequest> WithCancellation(this AsyncGPUReadbackRequest asyncOperation, CancellationToken cancellationToken, bool cancelImmediately)
        {
            return ToLuminTask(asyncOperation, cancellationToken: cancellationToken, cancelImmediately: cancelImmediately);
        }

        public static LuminTask<AsyncGPUReadbackRequest> ToLuminTask(this AsyncGPUReadbackRequest asyncOperation, PlayerLoopTiming timing = PlayerLoopTiming.Update, CancellationToken cancellationToken = default, bool cancelImmediately = false)
        {
            if (asyncOperation.done) 
                return LuminTask.FromResult(asyncOperation);
            return AsyncGPUReadbackRequestPromise.Create(asyncOperation, timing, cancellationToken, cancelImmediately);
        }
        
        private static class AsyncGPUReadbackRequestPromise
        {
            [StructLayout(LayoutKind.Sequential)]
            private struct StateData
            {
                public bool Completed;
                public bool IsCancellationRequested;
                public bool HasError;
            }

            public static unsafe LuminTask<AsyncGPUReadbackRequest> Create(AsyncGPUReadbackRequest asyncOperation, PlayerLoopTiming timing, CancellationToken cancellationToken, bool cancelImmediately)
            {
                var core = LuminTaskSourceCore<AsyncGPUReadbackRequest>.Create();
                var stateData = new StateData
                {
                    Completed = false,
                    IsCancellationRequested = false,
                    HasError = false
                };

                var stateTuple = StateTuple.Create(stateData);
                var state = new LuminTaskState(core, cancellationToken, asyncOperation, stateTuple);

                if (cancelImmediately && cancellationToken.CanBeCanceled)
                {
                    var registration = cancellationToken.Register(() =>
                    {
                        stateData.Completed = true;
                        stateData.IsCancellationRequested = true;
                        LuminTaskSourceCore<AsyncGPUReadbackRequest>.TrySetCanceled(core);
                    });
                }

                PlayerLoopHelper.AddAction(timing, state, &MoveNext);

                return new LuminTask<AsyncGPUReadbackRequest>(LuminTaskSourceCore<AsyncGPUReadbackRequest>.MethodTable, core, core->Id);
            }

            public static unsafe bool MoveNext(in LuminTaskState state)
            {
                var asyncOperation = state.State as AsyncGPUReadbackRequest?;
                if (!asyncOperation.HasValue)
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
                    LuminTaskSourceCore<AsyncGPUReadbackRequest>.TrySetCanceled(state.Source);
                    stateTuple.Dispose();
                    return false;
                }

                if (asyncOperation.Value.hasError)
                {
                    stateTuple.Item1.Completed = true;
                    stateTuple.Item1.HasError = true;
                    LuminTaskSourceCore<AsyncGPUReadbackRequest>.TrySetException(state.Source, new Exception("AsyncGPUReadbackRequest.hasError = true"));
                    stateTuple.Dispose();
                    return false;
                }

                if (asyncOperation.Value.done)
                {
                    stateTuple.Item1.Completed = true;
                    LuminTaskSourceCore<AsyncGPUReadbackRequest>.TrySetResult(state.Source, asyncOperation.Value);
                    stateTuple.Dispose();
                    return false;
                }

                return true;
            }
        }

        #endregion
    }
}
