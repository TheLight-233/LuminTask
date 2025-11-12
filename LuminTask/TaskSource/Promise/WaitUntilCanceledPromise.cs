using System;
using System.Runtime.CompilerServices;
using System.Threading;
using LuminThread.Interface;

namespace LuminThread.TaskSource.Promise;

public unsafe struct WaitUntilCanceledPromise<T>
{
    CancellationToken _cancellationToken;
    CancellationTokenRegistration _cancellationTokenRegistration;
    bool _cancelImmediately;

    LuminTaskSourceCore<T>* _core;
    
    internal LuminTaskSourceCore<T>* Source => _core;
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static WaitUntilCanceledPromise<T> Create(CancellationToken cancellationToken, PlayerLoopTiming timing, bool cancelImmediately, out short token)
    {
        var core = LuminTaskSourceCore<T>.Create();

        var waitPromise = new WaitUntilCanceledPromise<T>();
        waitPromise._core = core;
        waitPromise._cancellationToken = cancellationToken;
        waitPromise._cancelImmediately = cancelImmediately;

        token = core->Id;
        
        return waitPromise;
    }
    
    public void GetResult(short token)
    {
        try
        {
            LuminTaskSourceCore<T>.GetResult(_core, token);
        }
        finally
        {
            if (!(_cancelImmediately && _cancellationToken.IsCancellationRequested))
            {
                Dispose();
            }
        }
    }

    public LuminTaskStatus GetStatus(short token)
    {
        return LuminTaskSourceCore<T>.GetStatus(_core, token);
    }

    public LuminTaskStatus UnsafeGetStatus()
    {
        return LuminTaskSourceCore<T>.UnsafeGetStatus(_core);
    }

    public void OnCompleted(Action<object> continuation, object state, short token)
    {
        LuminTaskSourceCore<T>.OnCompleted(_core, continuation, state, token);
    }

    public bool MoveNext()
    {
        if (_cancellationToken.IsCancellationRequested)
        {
            LuminTaskSourceCore<T>.TrySetResult(_core);
            return false;
        }

        return true;
    }

    void Dispose()
    {
        _cancellationToken = default;
        _cancellationTokenRegistration.Dispose();
        _cancelImmediately = default;

        if (_core != null)
        {
            LuminTaskSourceCore<T>.Dispose(_core);
        }
    }
}
