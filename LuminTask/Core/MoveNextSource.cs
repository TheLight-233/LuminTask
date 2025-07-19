using System;
using Lumin.Threading.Core;
using Lumin.Threading.Interface;
using Lumin.Threading.Source;

namespace Lumin.Threading.Source;

public abstract class MoveNextSource : ILuminTaskSource<bool>
{
    protected LuminTaskCompletionSourceCore<bool> completionSource;

    public bool GetResult(short token)
    {
        return completionSource.GetResult(token);
    }

    public LuminTaskStatus GetStatus(short token)
    {
        return completionSource.GetStatus(token);
    }

    public void OnCompleted(Action<object> continuation, object state, short token)
    {
        completionSource.OnCompleted(continuation, state, token);
    }

    public LuminTaskStatus UnsafeGetStatus()
    {
        return completionSource.UnsafeGetStatus();
    }

    void ILuminTaskSource.GetResult(short token)
    {
        completionSource.GetResult(token);
    }

    protected bool TryGetResult<T>(LuminTaskAwaiter<T> awaiter, out T result)
    {
        try
        {
            result = awaiter.GetResult();
            return true;
        }
        catch (Exception ex)
        {
            completionSource.TrySetException(ex);
            result = default;
            return false;
        }
    }

    protected bool TryGetResult(LuminTaskAwaiter awaiter)
    {
        try
        {
            awaiter.GetResult();
            return true;
        }
        catch (Exception ex)
        {
            completionSource.TrySetException(ex);
            return false;
        }
    }
}