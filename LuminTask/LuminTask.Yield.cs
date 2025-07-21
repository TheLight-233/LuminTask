using System;
using System.Runtime.CompilerServices;
using System.Threading;
using Lumin.Threading.Interface;
using Lumin.Threading.Source;
using Lumin.Threading.Tasks.Utility;
using Lumin.Threading.Unity;
using Lumin.Threading.Utility;

namespace Lumin.Threading.Tasks;

public readonly ref partial struct LuminTask
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static YieldAwaitable Yield()
    {
        // optimized for single continuation
        return new YieldAwaitable(PlayerLoopTiming.DotNet);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static YieldAwaitable Yield(PlayerLoopTiming timing)
    {
        // optimized for single continuation
        return new YieldAwaitable(timing);
    }
    
    public static LuminTask Yield(CancellationToken cancellationToken, bool cancelImmediately = false)
    {
        return new LuminTask(YieldPromise.Create(PlayerLoopTiming.Update, cancellationToken, cancelImmediately, out var token), token);
    }

    public static LuminTask Yield(PlayerLoopTiming timing, CancellationToken cancellationToken, bool cancelImmediately = false)
    {
        return new LuminTask(YieldPromise.Create(timing, cancellationToken, cancelImmediately, out var token), token);
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
    sealed class YieldPromise : ILuminTaskSource, IPlayLoopItem, Threading.Utility.ITaskPoolNode<YieldPromise>
    {
        static Threading.Utility.TaskPool<YieldPromise> pool;
        YieldPromise nextNode;
        public ref YieldPromise NextNode => ref nextNode;

        static YieldPromise()
        {
            TaskPool.RegisterSizeGetter(typeof(YieldPromise), () => pool.Size);
        }

        CancellationToken cancellationToken;
        CancellationTokenRegistration cancellationTokenRegistration;
        bool cancelImmediately;
        LuminTaskCompletionSourceCore<object> core;

        private YieldPromise() { }

        public static ILuminTaskSource Create(PlayerLoopTiming timing, CancellationToken cancellationToken, bool cancelImmediately, out short token)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return AutoResetLuminTaskCompletionSource.CreateFromCanceled(cancellationToken, out token);
            }

            if (!pool.TryPop(out var result))
            {
                result = new YieldPromise();
            }

            result.cancellationToken = cancellationToken;
            result.cancelImmediately = cancelImmediately;
                
            if (cancelImmediately && cancellationToken.CanBeCanceled)
            {
                result.cancellationTokenRegistration = cancellationToken.RegisterWithoutCaptureExecutionContext(state =>
                {
                    var promise = (YieldPromise)state;
                    promise.core.TrySetCanceled(promise.cancellationToken);
                }, result);
            }

            TaskTracker.TrackActiveTask(result, 3);

            PlayerLoopHelper.AddAction(timing, result);

            token = result.core.Version;
            return result;
        }

        public void GetResult(short token)
        {
            try
            {
                core.GetResult(token);
            }
            finally
            {
                if (!(cancelImmediately && cancellationToken.IsCancellationRequested))
                {
                    TryReturn();
                }
                else
                {
                    TaskTracker.RemoveTracking(this);
                }
            }
        }

        public LuminTaskStatus GetStatus(short token)
        {
            return core.GetStatus(token);
        }

        public LuminTaskStatus UnsafeGetStatus()
        {
            return core.UnsafeGetStatus();
        }

        public void OnCompleted(Action<object> continuation, object state, short token)
        {
            core.OnCompleted(continuation, state, token);
        }

        public bool MoveNext()
        {
            if (cancellationToken.IsCancellationRequested)
            {
                core.TrySetCanceled(cancellationToken);
                return false;
            }

            core.TrySetResult(null);
            return false;
        }

        bool TryReturn()
        {
            TaskTracker.RemoveTracking(this);
            core.Reset();
            cancellationToken = default;
            cancellationTokenRegistration.Dispose();
            cancelImmediately = default;
            return pool.TryPush(this);
        }
    }
}