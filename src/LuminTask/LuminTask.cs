using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security;
using System.Threading;
using System.Threading.Tasks;
using LuminThread.CompilerServices;
using LuminThread.Interface;
using LuminThread.TaskSource;
using LuminThread.Utility;

namespace LuminThread;

// A synchronous success uses the zero handle and stores a typed result in the task itself.
// The builder rents a generation-checked slot on first suspension or fault. The slot owns
// the runner while pending. Readonly awaiter members avoid defensive copies of the task.
[AsyncMethodBuilder(typeof(LuminTask))]
[StructLayout(LayoutKind.Sequential)]
public partial struct LuminTask : ICriticalNotifyCompletion
{
    internal LuminTaskHandle _handle;

    public readonly short Id => (short)(_handle.SlotRaw() - 1);

    [DebuggerHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal LuminTask(LuminTaskHandle handle)
    {
        _handle = handle;
    }

    [DebuggerHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly LuminTask GetAwaiter() => this;

    public readonly bool IsCompleted
    {
        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _handle.IsNull() || LuminTaskSource.IsCompleted(_handle);
    }

    [DebuggerHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly void GetResult()
    {
        if (_handle.IsNull())
            return;

        LuminTaskSource.ConsumeVoid(_handle);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly void OnCompleted(Action continuation)
        => LuminTaskSource.OnCompleted(_handle, continuation);

    [DebuggerHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly void SourceOnCompleted(Action<object> continuation, object state)
    {
        if (_handle.IsNull())
        {
            continuation(state);
        }
        else
        {
            LuminTaskSource.OnCompleted(_handle, continuation, state);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly void UnsafeOnCompleted(Action continuation) => OnCompleted(continuation);

    public readonly LuminTaskStatus Status
    {
        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _handle.IsNull() ? LuminTaskStatus.Succeeded : LuminTaskSource.GetStatus(_handle);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator ValueTask(in LuminTask self)
    {
        if (self._handle.IsNull())
        {
            return default;
        }

        return new ValueTask(new ValueTaskSource(self._handle), 0);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool Equals(LuminTask other)
    {
        return other._handle == _handle;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly override string? ToString()
    {
        return _handle.IsNull() ? "()" : "(" + LuminTaskSource.GetStatus(_handle) + ")";
    }

    // ----------------------------------------------------------------------------------
    // Async method builder. The compiler asks for exactly these members because
    // [AsyncMethodBuilder(typeof(LuminTask))] points at this type; none of them is readonly,
    // they are what writes _handle.
    // ----------------------------------------------------------------------------------

    [DebuggerHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static LuminTask Create() => default;

    public LuminTask Task
    {
        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => this;
    }

    [DebuggerHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Start<TStateMachine>(ref TStateMachine stateMachine)
        where TStateMachine : IAsyncStateMachine
    {
        stateMachine.MoveNext();
    }

    [DebuggerHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetStateMachine(IAsyncStateMachine stateMachine) { }

    [DebuggerHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetResult()
    {
        LuminTaskHandle handle = _handle;

        // A zero handle means the method never suspended: Task already hands out a completed task.
        if (handle.IsNull())
            return;

        LuminTaskSource.TrySetResult(handle);
    }

    [DebuggerHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetException(Exception exception)
    {
        LuminTaskHandle handle = _handle;

        // Faulted before the first suspension: there is no slot yet, so the exception gets a slot of
        // its own instead of an Exception field in the struct.
        if (handle.IsNull())
            handle = _handle = RentSlot();

        LuminTaskSource.TrySetException(handle, exception);
    }

    [DebuggerHidden]
    public void AwaitOnCompleted<TAwaiter, TStateMachine>(
        ref TAwaiter awaiter,
        ref TStateMachine stateMachine)
        where TAwaiter : INotifyCompletion
        where TStateMachine : IAsyncStateMachine
    {
        bool first = _handle == 0;
        if (first) _handle = RentSlot();
        var runner = StateMachineBox<TStateMachine>.Bind(_handle, ref stateMachine, first);
        try { awaiter.OnCompleted(runner.MoveNextAction); }
        finally { if (first) runner.Exit(); }
    }

    [DebuggerHidden]
    [SecuritySafeCritical]
    public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(
        ref TAwaiter awaiter,
        ref TStateMachine stateMachine)
        where TAwaiter : ICriticalNotifyCompletion
        where TStateMachine : IAsyncStateMachine
    {
        bool first = _handle == 0;
        if (first) _handle = RentSlot();
        var runner = StateMachineBox<TStateMachine>.Bind(_handle, ref stateMachine, first);
        try { awaiter.UnsafeOnCompleted(runner.MoveNextAction); }
        finally { if (first) runner.Exit(); }
    }
}

[AsyncMethodBuilder(typeof(LuminTask<>))]
[StructLayout(LayoutKind.Sequential)]
public partial struct LuminTask<T> : ICriticalNotifyCompletion
{
    internal LuminTaskHandle _handle;

    // Only meaningful on the synchronous completion path, where no slot exists at all; the builder
    // writes it with SetResult(T) so a method that never suspends carries its value inline.
    internal T? _result;

    public readonly T? Result => _handle.IsNull()
        ? _result
        : throw new InvalidOperationException(
            "LuminTask<T>.Result is only valid for a synchronously-completed task (e.g. FromResult); " +
            "await the task or use GetAwaiter().GetResult() instead.");

    [DebuggerHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal LuminTask(LuminTaskHandle handle)
    {
        _handle = handle;
    }

    [DebuggerHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public LuminTask(T result)
    {
        _handle = 0;
        _result = result;
    }

    [DebuggerHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly LuminTask<T> GetAwaiter() => this;

    public readonly bool IsCompleted
    {
        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _handle.IsNull() || LuminTaskSource.IsCompleted(_handle);
    }

    [DebuggerHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly T GetResult()
    {
        if (_handle.IsNull())
            return _result!;

        return LuminTaskResultBridge.Get<T>(_handle);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly void OnCompleted(Action continuation)
        => LuminTaskSource.OnCompleted(_handle, continuation);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly void UnsafeOnCompleted(Action continuation) => OnCompleted(continuation);

    public readonly LuminTaskStatus Status
    {
        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _handle.IsNull() ? LuminTaskStatus.Succeeded : LuminTaskSource.GetStatus(_handle);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator LuminTask(LuminTask<T> self)
    {
        if (self._handle.IsNull())
        {
            return LuminTask.CompletedTask();
        }

        return new LuminTask(self._handle);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool Equals(LuminTask<T> other)
    {
        return other._handle == _handle;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool Equals(LuminTask other)
    {
        return other._handle == _handle;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly override string? ToString()
    {
        return _handle.IsNull() ? _result?.ToString()
            : "(" + LuminTaskSource.GetStatus(_handle) + ")";
    }

    // ----------------------------------------------------------------------------------
    // Async method builder - same shape as the non-generic one, plus the synchronous result.
    // ----------------------------------------------------------------------------------

    [DebuggerHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static LuminTask<T> Create() => default;

    public LuminTask<T> Task
    {
        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => this;
    }

    [DebuggerHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Start<TStateMachine>(ref TStateMachine stateMachine)
        where TStateMachine : IAsyncStateMachine
    {
        stateMachine.MoveNext();
    }

    [DebuggerHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetStateMachine(IAsyncStateMachine stateMachine) { }

    [DebuggerHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetResult(T result)
    {
        LuminTaskHandle handle = _handle;

        // Synchronous completion: the result stays inline in the struct, no slot at all.
        if (handle.IsNull())
        {
            _result = result;
            return;
        }

        LuminTaskResultBridge.TrySetResult(handle, in result);
    }

    [DebuggerHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetException(Exception exception)
    {
        LuminTaskHandle handle = _handle;

        if (handle.IsNull())
            handle = _handle = LuminTask.RentSlot();

        LuminTaskSource.TrySetException(handle, exception);
    }

    [DebuggerHidden]
    public void AwaitOnCompleted<TAwaiter, TStateMachine>(
        ref TAwaiter awaiter,
        ref TStateMachine stateMachine)
        where TAwaiter : INotifyCompletion
        where TStateMachine : IAsyncStateMachine
    {
        bool first = _handle == 0;
        if (first) _handle = LuminTaskResultBridge.Rent<T>();
        var runner = StateMachineBox<TStateMachine>.Bind(_handle, ref stateMachine, first);
        try { awaiter.OnCompleted(runner.MoveNextAction); }
        finally { if (first) runner.Exit(); }
    }

    [DebuggerHidden]
    [SecuritySafeCritical]
    public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(
        ref TAwaiter awaiter,
        ref TStateMachine stateMachine)
        where TAwaiter : ICriticalNotifyCompletion
        where TStateMachine : IAsyncStateMachine
    {
        bool first = _handle == 0;
        if (first) _handle = LuminTaskResultBridge.Rent<T>();
        var runner = StateMachineBox<TStateMachine>.Bind(_handle, ref stateMachine, first);
        try { awaiter.UnsafeOnCompleted(runner.MoveNextAction); }
        finally { if (first) runner.Exit(); }
    }
}
