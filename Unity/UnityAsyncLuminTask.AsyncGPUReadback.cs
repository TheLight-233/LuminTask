
using System;
using System.Threading;
using UnityEngine.Rendering;
using LuminThread;
using LuminThread.Interface;
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
            private struct StateData
            {
                public AsyncGPUReadbackRequest Request;
                public CancellationToken Token;
                public CancellationTokenRegistration Registration;
                public ulong Handle;
            }

            private static readonly MoveNext MoveNextAction = MoveNext;
            private static readonly Action<object> CancelAction = Cancel;
            private static readonly Action<object> ReleaseAction = Release;

            public static LuminTask<AsyncGPUReadbackRequest> Create(AsyncGPUReadbackRequest request, PlayerLoopTiming timing,
                CancellationToken cancellationToken, bool cancelImmediately)
            {
                var task = LuminTaskSource.Create<AsyncGPUReadbackRequest>(out var handle);
                var box = StateTuple.Create(new StateData { Request = request, Token = cancellationToken, Handle = handle });
                try
                {
                    if (cancelImmediately && cancellationToken.CanBeCanceled)
                        box.Item.Registration = cancellationToken.Register(CancelAction, box);
                    PlayerLoopHelper.AddAction(timing, new LuminTaskState(handle, cancellationToken, box, ReleaseAction), MoveNextAction);
                }
                catch
                {
                    Release(box);
                    LuminTaskSource.SetCanceled(handle, cancellationToken);
                    task.Forget();
                    throw;
                }
                return task;
            }

            private static void Cancel(object state)
            {
                var box = (StateTuple<StateData>)state;
                LuminTaskSource.SetCanceled(box.Item.Handle, box.Item.Token);
            }

            private static void Release(object state)
            {
                var box = (StateTuple<StateData>)state;
                box.Item.Registration.Dispose();
                box.Dispose();
            }

            private static bool MoveNext(in LuminTaskState state)
            {
                var box = (StateTuple<StateData>)state.State;
                try
                {
                    if (state.CancellationToken.IsCancellationRequested)
                        LuminTaskSource.SetCanceled(state.Handle, state.CancellationToken);
                    else if (box.Item.Request.hasError)
                        LuminTaskSource.SetException(state.Handle, new Exception("AsyncGPUReadbackRequest.hasError = true"));
                    else if (box.Item.Request.done)
                        LuminTaskSource.SetResult(state.Handle, box.Item.Request);
                    else
                        return true;
                }
                catch
                {
                    Release(box);
                    throw;
                }
                Release(box);
                return false;
            }
        }

        #endregion
    }
}
