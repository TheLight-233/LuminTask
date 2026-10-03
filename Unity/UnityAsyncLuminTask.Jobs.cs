#if ENABLE_MANAGED_JOBS
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

using System;
using System.Threading;
using Unity.Jobs;
using UnityEngine;
using LuminThread.TaskSource;
using LuminThread.Utility;
using System.Runtime.CompilerServices;
using LuminThread.Interface;

namespace LuminThread.Unity
{
    public static partial class UnityAsyncLuminTask
    {
        public static LuminTask GetAwaiter(this JobHandle jobHandle)
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

                var task = LuminTaskSource.Create(out ulong handle);
                var state = new LuminTaskState(handle, cancellationToken, StateTuple.Create(handle, jobHandle));

                PlayerLoopHelper.AddAction(timing, state, MoveNextAction);

                return task;
            }

            private static readonly MoveNext MoveNextAction = MoveNext;

            public static bool MoveNext(in LuminTaskState state)
            {
                var tuple = (StateTuple<(ulong, JobHandle)>)state.State!;
                var jobHandle = tuple.Item.Item2;

                if (state.CancellationToken.IsCancellationRequested)
                {
                    jobHandle.Complete();
                    LuminTaskSource.SetCanceled(tuple.Item.Item1, state.CancellationToken);
                    return false;
                }

                if (jobHandle.IsCompleted)
                {
                    jobHandle.Complete();
                    LuminTaskSource.SetResult(tuple.Item.Item1);
                    return false;
                }

                return true;
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