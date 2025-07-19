#pragma warning disable CS1591
#pragma warning disable CS0108


using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks.Sources;


namespace Lumin.Threading.Interface;

public enum LuminTaskStatus
{
    /// <summary>The operation has not yet completed.</summary>
    Pending = 0,
    /// <summary>The operation completed successfully.</summary>
    Succeeded = 1,
    /// <summary>The operation completed with an error.</summary>
    Faulted = 2,
    /// <summary>The operation completed due to cancellation.</summary>
    Canceled = 3
}

// similar as IValueTaskSource
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

public static class LuminTaskStatusExtensions
{
    /// <summary>status != Pending.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsCompleted(this LuminTaskStatus status)
    {
        return status != LuminTaskStatus.Pending;
    }

    /// <summary>status == Succeeded.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsCompletedSuccessfully(this LuminTaskStatus status)
    {
        return status == LuminTaskStatus.Succeeded;
    }

    /// <summary>status == Canceled.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsCanceled(this LuminTaskStatus status)
    {
        return status == LuminTaskStatus.Canceled;
    }

    /// <summary>status == Faulted.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsFaulted(this LuminTaskStatus status)
    {
        return status == LuminTaskStatus.Faulted;
    }
}