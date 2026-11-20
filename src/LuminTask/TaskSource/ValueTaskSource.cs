using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks.Sources;

namespace LuminThread.TaskSource;

// Thin IValueTaskSource wrapper: it holds nothing but a 64-bit handle and forwards every operation
// to the global LuminTaskSource. The token is part of the ValueTask protocol shape; the
// handle's own generation already carries the validation duty, so it is ignored here.
//
// The generic operator LuminTask<T> -> ValueTask<T> was removed on purpose: IL2CPP materialises
// every method of every closed generic type, so that implicit operator alone instantiated
// ValueTaskSource<T> once per LuminTask<T> (152 closures in the bench build) and dragged
// IValueTaskSource<T> along with it. The ToValueTask capability stays reachable through the
// explicit AsValueTask<T>() extension, which only pays the cost where it is actually called.
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
        => (ValueTaskSourceStatus)(int)LuminTaskSource.GetStatus(_handle);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void GetResult(short token) => LuminTaskSource.ConsumeVoid(_handle);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void OnCompleted(Action<object> continuation, object state, short token, ValueTaskSourceOnCompletedFlags flags)
        => LuminTaskSource.OnCompleted(_handle, continuation, state);
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
        => (ValueTaskSourceStatus)(int)LuminTaskSource.GetStatus(_handle);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T GetResult(short token) => LuminTaskResultBridge.Get<T>(_handle);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void OnCompleted(Action<object> continuation, object state, short token, ValueTaskSourceOnCompletedFlags flags)
        => LuminTaskSource.OnCompleted(_handle, continuation, state);
}