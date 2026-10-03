using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks.Sources;

namespace LuminThread.TaskSource;

// Thin IValueTaskSource wrapper: it holds nothing but a 64-bit handle and forwards every operation
// to the global LuminTaskCoreSource. The token is part of the ValueTask protocol shape; the
// handle's own generation already carries the validation duty, so it is ignored here.
//
// LuminTaskStatus matches ValueTaskSourceStatus value for value
// (Pending / Succeeded / Faulted / Canceled = 0 / 1 / 2 / 3), so the projection is a plain cast.
public sealed class ValueTaskSource : IValueTaskSource
{
    private readonly LuminTaskHandle _handle;

    public ValueTaskSource(LuminTaskHandle handle)
    {
        _handle = handle;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ValueTaskSourceStatus GetStatus(short token)
        => (ValueTaskSourceStatus)(int)LuminTaskCoreSource.GetStatus(_handle);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void GetResult(short token) => LuminTaskCoreSource.ConsumeVoid(_handle);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void OnCompleted(Action<object> continuation, object state, short token, ValueTaskSourceOnCompletedFlags flags)
        => LuminTaskCoreSource.OnCompleted(_handle, continuation, state);
}

public sealed class ValueTaskSource<T> : IValueTaskSource<T>
{
    private readonly LuminTaskHandle _handle;

    public ValueTaskSource(LuminTaskHandle handle)
    {
        _handle = handle;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ValueTaskSourceStatus GetStatus(short token)
        => (ValueTaskSourceStatus)(int)LuminTaskCoreSource.GetStatus(_handle);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T GetResult(short token) => LuminTaskResultBridge.Get<T>(_handle);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void OnCompleted(Action<object> continuation, object state, short token, ValueTaskSourceOnCompletedFlags flags)
        => LuminTaskCoreSource.OnCompleted(_handle, continuation, state);
}