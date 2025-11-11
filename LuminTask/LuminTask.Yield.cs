using LuminThread.Interface;
using LuminThread.TaskSource.Promise;
using LuminThread.Utility;

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
    public static unsafe LuminTask Yield(CancellationToken cancellationToken, bool cancelImmediately = false)
    {
        var ptr = YieldPromise.Create(PlayerLoopTiming.Update, cancellationToken, cancelImmediately, out var id);
        return new LuminTask(YieldPromise.MethodTable, ptr, id);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe LuminTask Yield(PlayerLoopTiming timing, CancellationToken cancellationToken, bool cancelImmediately = false)
    {
        var ptr = YieldPromise.Create(timing, cancellationToken, cancelImmediately, out var id);
        return new LuminTask(YieldPromise.MethodTable, ptr, id);
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