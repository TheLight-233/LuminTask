#if ENABLE_MANAGED_JOBS
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

using System;
using System.Threading;
using Unity.Jobs;
using UnityEngine;
using LuminThread.TaskSource;
using LuminThread.Utility;
using System.Runtime.CompilerServices;

namespace LuminThread.Unity
{
    public static partial class UnityAsyncExtensions
    {
        public static LuminTaskAwaiter GetAwaiter(this JobHandle jobHandle)
        {
            return JobHandlePromise.Create(jobHandle, PlayerLoopTiming.Update, CancellationToken.None).GetAwaiter();
        }

        public static LuminTask ToLuminTask(this JobHandle jobHandle, PlayerLoopTiming timing = PlayerLoopTiming.Update, CancellationToken cancellationToken = default)
        {
            return JobHandlePromise.Create(jobHandle, timing, cancellationToken);
        }

        private static class JobHandlePromise
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static unsafe LuminTask Create(JobHandle jobHandle, PlayerLoopTiming timing, CancellationToken cancellationToken)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return LuminTask.FromCanceled(cancellationToken);
                }

                if (jobHandle.IsCompleted)
                {
                    jobHandle.Complete();
                    return LuminTask.CompletedTask();
                }

                var core = LuminTaskSourceCore<AsyncUnit>.Create();
                var state = new LuminTaskState(core, cancellationToken, jobHandle);

                PlayerLoopHelper.AddAction(timing, state, &MoveNext);

                return new LuminTask(LuminTaskSourceCore<AsyncUnit>.MethodTable, core, core->Id);
            }

            public static unsafe bool MoveNext(in LuminTaskState state)
            {
                var jobHandle = (JobHandle)state.State;

                if (state.CancellationToken.IsCancellationRequested)
                {
                    jobHandle.Complete();
                    LuminTaskSourceCore<AsyncUnit>.TrySetCanceled(state.Source);
                    Dispose(state.Source);
                    return false;
                }

                if (jobHandle.IsCompleted)
                {
                    jobHandle.Complete();
                    LuminTaskSourceCore<AsyncUnit>.TrySetResult(state.Source);
                    Dispose(state.Source);
                    return false;
                }

                return true;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static unsafe void Dispose(void* source)
            {
                if (source != null)
                {
                    LuminTaskSourceCore<AsyncUnit>.Dispose(source);
                }
            }
        }

        [Obsolete("Use ToLuminTask instead. CancellationToken is not supported for JobHandle.")]
        public static async LuminTask WaitAsync(this JobHandle jobHandle, PlayerLoopTiming waitTiming, CancellationToken cancellationToken = default)
        {
            await LuminTask.Yield(waitTiming);
            jobHandle.Complete();
            cancellationToken.ThrowIfCancellationRequested();
        }
    }
}

#endif