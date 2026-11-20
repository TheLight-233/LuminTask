
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
    public static partial class UnityAsyncLuminTask
    {
        #region AsyncGPUReadbackRequest

        public static LuminTask<AsyncGPUReadbackRequest> GetAwaiter(this AsyncGPUReadbackRequest asyncOperation)
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
                public CancellationTokenRegistration Registration;
                public ulong Handle;
            }

            public static LuminTask<AsyncGPUReadbackRequest> Create(AsyncGPUReadbackRequest asyncOperation, PlayerLoopTiming timing, CancellationToken cancellationToken, bool cancelImmediately)
            {
                var task = LuminTaskSource.Create<AsyncGPUReadbackRequest>(out ulong handle);
                var stateData = new StateData
                {
                    Completed = false,
                    IsCancellationRequested = false,
                    HasError = false,
                    Handle = handle
                };

                var stateTuple = StateTuple.Create(stateData, asyncOperation);
                var state = new LuminTaskState(handle, cancellationToken, stateTuple);

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
                var stateTuple = (StateTuple<(StateData, AsyncGPUReadbackRequest)>)state.State!;
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

                if (asyncOperation.hasError)
                {
                    stateTuple.Item.Item1.Completed = true;
                    stateTuple.Item.Item1.HasError = true;
                    LuminTaskSource.SetException(stateTuple.Item.Item1.Handle, new Exception("AsyncGPUReadbackRequest.hasError = true"));
                    stateTuple.Item.Item1.Registration.Dispose();
                    stateTuple.Dispose();
                    return false;
                }

                if (asyncOperation.done)
                {
                    stateTuple.Item.Item1.Completed = true;
                    LuminTaskSource.SetResult(stateTuple.Item.Item1.Handle, asyncOperation);
                    stateTuple.Item.Item1.Registration.Dispose();
                    stateTuple.Dispose();
                    return false;
                }

                return true;
            }
        }

        #endregion
    }
}
