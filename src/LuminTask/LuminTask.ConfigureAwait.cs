using System;
using System.Runtime.CompilerServices;
using System.Threading;
using LuminThread.Utility;

namespace LuminThread;

public readonly unsafe partial struct LuminTask
{
    /// <summary>Controls whether the await continuation captures the current execution context.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ConfiguredLuminTaskAwaitable ConfigureAwait(bool continueOnCapturedContext)
        => new(this, continueOnCapturedContext);
}

public readonly unsafe partial struct LuminTask<T>
{
    /// <summary>Controls whether the await continuation captures the current execution context.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ConfiguredLuminTaskAwaitable<T> ConfigureAwait(bool continueOnCapturedContext)
        => new(this, continueOnCapturedContext);
}

public readonly unsafe struct ConfiguredLuminTaskAwaitable
{
    private readonly LuminTask _task;
    private readonly bool _continueOnCapturedContext;

    internal ConfiguredLuminTaskAwaitable(LuminTask task, bool continueOnCapturedContext)
    {
        _task = task;
        _continueOnCapturedContext = continueOnCapturedContext;
    }

    public ConfiguredLuminTaskAwaiter GetAwaiter()
        => new(_task, _continueOnCapturedContext);
}

public readonly unsafe struct ConfiguredLuminTaskAwaitable<T>
{
    private readonly LuminTask<T> _task;
    private readonly bool _continueOnCapturedContext;

    internal ConfiguredLuminTaskAwaitable(LuminTask<T> task, bool continueOnCapturedContext)
    {
        _task = task;
        _continueOnCapturedContext = continueOnCapturedContext;
    }

    public ConfiguredLuminTaskAwaiter<T> GetAwaiter()
        => new(_task, _continueOnCapturedContext);
}

public readonly unsafe struct ConfiguredLuminTaskAwaiter : ICriticalNotifyCompletion
{
    private readonly LuminTask _task;
    private readonly bool _continueOnCapturedContext;

    internal ConfiguredLuminTaskAwaiter(LuminTask task, bool continueOnCapturedContext)
    {
        _task = task;
        _continueOnCapturedContext = continueOnCapturedContext;
    }

    public bool IsCompleted => _task.GetAwaiter().IsCompleted;
    public void GetResult() => _task.GetAwaiter().GetResult();

    public void OnCompleted(Action continuation)
    {
        SetCapturePolicy();
        _task.GetAwaiter().OnCompleted(continuation);
    }

    public void UnsafeOnCompleted(Action continuation)
    {
        SetCapturePolicy();
        _task.GetAwaiter().UnsafeOnCompleted(continuation);
    }

    private void SetCapturePolicy()
    {
        if (_task._taskSource != null)
            LuminTaskMarshal.GetTaskItem(_task._id).ContinueOnCapturedContext = _continueOnCapturedContext;
    }
}

public readonly unsafe struct ConfiguredLuminTaskAwaiter<T> : ICriticalNotifyCompletion
{
    private readonly LuminTask<T> _task;
    private readonly bool _continueOnCapturedContext;

    internal ConfiguredLuminTaskAwaiter(LuminTask<T> task, bool continueOnCapturedContext)
    {
        _task = task;
        _continueOnCapturedContext = continueOnCapturedContext;
    }

    public bool IsCompleted => _task.GetAwaiter().IsCompleted;
    public T GetResult() => _task.GetAwaiter().GetResult();

    public void OnCompleted(Action continuation)
    {
        SetCapturePolicy();
        _task.GetAwaiter().OnCompleted(continuation);
    }

    public void UnsafeOnCompleted(Action continuation)
    {
        SetCapturePolicy();
        _task.GetAwaiter().UnsafeOnCompleted(continuation);
    }

    private void SetCapturePolicy()
    {
        if (_task._taskSource != null)
            LuminTaskMarshal.GetTaskItem(_task._id).ContinueOnCapturedContext = _continueOnCapturedContext;
    }
}
