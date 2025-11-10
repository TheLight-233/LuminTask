using System;
using System.Threading.Tasks.Sources;

namespace LuminThread.Interface;

public interface ILuminTaskSource : IValueTaskSource
{
    LuminTaskStatus GetStatus(short token);
    void OnCompleted(Action<object> continuation, object state, short token);
    void GetResult(short token);

    LuminTaskStatus UnsafeGetStatus(); // only for debug use.
        

    ValueTaskSourceStatus IValueTaskSource.GetStatus(short token)
    {
        return (ValueTaskSourceStatus)(int)GetStatus(token);
    }

    void IValueTaskSource.GetResult(short token)
    {
        GetResult(token);
    }

    void IValueTaskSource.OnCompleted(Action<object> continuation, object state, short token, System.Threading.Tasks.Sources.ValueTaskSourceOnCompletedFlags flags)
    {
        // ignore flags, always none.
        OnCompleted(continuation, state, token);
    }
        
}

public interface ILuminTaskSource<out T> : ILuminTaskSource, IValueTaskSource<T>
{
    new T GetResult(short token);
        

    public new  LuminTaskStatus GetStatus(short token)
    {
        return ((ILuminTaskSource)this).GetStatus(token);
    }

    public new void OnCompleted(Action<object> continuation, object state, short token)
    {
        ((ILuminTaskSource)this).OnCompleted(continuation, state, token);
    }

    ValueTaskSourceStatus IValueTaskSource<T>.GetStatus(short token)
    {
        return (ValueTaskSourceStatus)(int)((ILuminTaskSource)this).GetStatus(token);
    }

    T IValueTaskSource<T>.GetResult(short token)
    {
        return GetResult(token);
    }

    void IValueTaskSource<T>.OnCompleted(Action<object> continuation, object state, short token, ValueTaskSourceOnCompletedFlags flags)
    {
        // ignore flags, always none.
        ((ILuminTaskSource)this).OnCompleted(continuation, state, token);
    }
        
}