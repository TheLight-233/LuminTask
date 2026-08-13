using System;
using System.Runtime.CompilerServices;
using System.Threading;

namespace LuminThread.Utility;

public static class CancellationTokenExtensions
{

    static readonly Action<object> disposeCallback = DisposeCallback;

    public static CancellationTokenAwaitable WaitUntilCanceled(this CancellationToken cancellationToken)
    {
        return new CancellationTokenAwaitable(cancellationToken);
    }

    /// <summary>
    /// Returns a LuminTask that completes when the token is canceled.
    /// </summary>
    /// <param name="cancellationToken">The cancelable token to observe.</param>
    /// <param name="completeAsCanceled">
    /// When true, the returned task enters the canceled state; otherwise it completes successfully.
    /// </param>
    public static LuminTask WaitAsync(this CancellationToken cancellationToken, bool completeAsCanceled = false)
    {
        if (!cancellationToken.CanBeCanceled)
            return LuminTask.FromException(new ArgumentException("The token must support cancellation.", nameof(cancellationToken)));

        if (cancellationToken.IsCancellationRequested)
            return completeAsCanceled
                ? LuminTask.FromCanceled(cancellationToken)
                : LuminTask.CompletedTask();

        return WaitAsyncCore(cancellationToken, completeAsCanceled);
    }

    private static async LuminTask WaitAsyncCore(CancellationToken cancellationToken, bool completeAsCanceled)
    {
        await cancellationToken.WaitUntilCanceled();
        if (completeAsCanceled)
            cancellationToken.ThrowIfCancellationRequested();
    }

    public static CancellationTokenRegistration RegisterWithoutCaptureExecutionContext(this CancellationToken cancellationToken, Action callback)
    {
        var restoreFlow = false;
        if (!ExecutionContext.IsFlowSuppressed())
        {
            ExecutionContext.SuppressFlow();
            restoreFlow = true;
        }

        try
        {
            return cancellationToken.Register(callback, false);
        }
        finally
        {
            if (restoreFlow)
            {
                ExecutionContext.RestoreFlow();
            }
        }
    }

    public static CancellationTokenRegistration RegisterWithoutCaptureExecutionContext(this CancellationToken cancellationToken, Action<object> callback, object state)
    {
        var restoreFlow = false;
        if (!ExecutionContext.IsFlowSuppressed())
        {
            ExecutionContext.SuppressFlow();
            restoreFlow = true;
        }

        try
        {
            return cancellationToken.Register(callback, state, false);
        }
        finally
        {
            if (restoreFlow)
            {
                ExecutionContext.RestoreFlow();
            }
        }
    }

    public static CancellationTokenRegistration AddTo(this IDisposable disposable, CancellationToken cancellationToken)
    {
        return cancellationToken.RegisterWithoutCaptureExecutionContext(disposeCallback, disposable);
    }

    static void DisposeCallback(object state)
    {
        var d = (IDisposable)state;
        d.Dispose();
    }
}

public readonly struct CancellationTokenAwaitable
{
    readonly CancellationToken cancellationToken;

    public CancellationTokenAwaitable(CancellationToken cancellationToken)
    {
        this.cancellationToken = cancellationToken;
    }

    public Awaiter GetAwaiter()
    {
        return new Awaiter(cancellationToken);
    }

    public struct Awaiter : ICriticalNotifyCompletion
    {
        CancellationToken cancellationToken;

        public Awaiter(CancellationToken cancellationToken)
        {
            this.cancellationToken = cancellationToken;
        }

        public bool IsCompleted => !cancellationToken.CanBeCanceled || cancellationToken.IsCancellationRequested;

        public void GetResult()
        {
        }

        public void OnCompleted(Action continuation)
        {
            UnsafeOnCompleted(continuation);
        }

        public void UnsafeOnCompleted(Action continuation)
        {
            cancellationToken.RegisterWithoutCaptureExecutionContext(continuation);
        }
    }
}

internal static class CancellationTokenRegistrationReleaseExtensions
{
    /// <summary>释放注册：优先非阻塞 Unregister；仅回调已在途时回退到阻塞的 Dispose 保证安全。</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Release(this ref CancellationTokenRegistration registration)
    {
        if (!registration.Token.CanBeCanceled)
        {
            registration = default;
            return;
        }

#if NET5_0_OR_GREATER
        if (!registration.Unregister())
            registration.Dispose();
#else
        registration.Dispose();
#endif

        registration = default;
    }
}
