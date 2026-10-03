using LuminThread.Interface;
using LuminThread.TaskSource;
using LuminThread.TaskSource.Promise;

namespace LuminThread;

using System;
using System.Runtime.CompilerServices;
using System.Threading;

public readonly partial struct LuminTask
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static YieldAwaitable Yield()
    {
        return new YieldAwaitable(PlayerLoopTiming.DotNet);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static YieldAwaitable Yield(PlayerLoopTiming timing)
    {
        return new YieldAwaitable(timing);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static LuminTask Yield(CancellationToken cancellationToken, bool cancelImmediately = false)
    {
        return Yield(PlayerLoopTiming.Update, cancellationToken, cancelImmediately);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static LuminTask Yield(PlayerLoopTiming timing, CancellationToken cancellationToken, bool cancelImmediately = false)
    {
        var handle = RentSlot();

        if (cancellationToken.IsCancellationRequested)
        {
            LuminTaskCoreSource.TrySetCanceled(handle, cancellationToken);
            return new LuminTask(handle);
        }

        if (cancelImmediately && cancellationToken.CanBeCanceled)
            AttachCancellation(handle, cancellationToken);

        PlayerLoopHelper.AddAction(timing, new LuminTaskState(handle, cancellationToken), YieldPromise.MoveNextAction);

        return new LuminTask(handle);
    }

    public readonly struct YieldAwaitable
    {
        readonly PlayerLoopTiming timing;

        public YieldAwaitable(PlayerLoopTiming timing)
        {
            this.timing = timing;
        }

        public Awaiter GetAwaiter()
        {
            return new Awaiter(timing);
        }

        public LuminTask ToLuminTask()
        {
            return LuminTask.Yield(timing, CancellationToken.None);
        }

        public readonly struct Awaiter : ICriticalNotifyCompletion
        {
            readonly PlayerLoopTiming timing;

            public Awaiter(PlayerLoopTiming timing)
            {
                this.timing = timing;
            }

            public bool IsCompleted => false;

            public void GetResult() { }

            public void OnCompleted(Action continuation)
            {
                PlayerLoopHelper.AddContinuation(timing, continuation);
            }

            public void UnsafeOnCompleted(Action continuation)
            {
                PlayerLoopHelper.AddContinuation(timing, continuation);
            }
        }
    }

}