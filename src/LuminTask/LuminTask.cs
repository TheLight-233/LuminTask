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

// A task is nothing but a 64-bit slot handle (see LuminTask.Handle.cs); a zero handle means the
// task completed synchronously and holds no slot at all. Compared with the previous
// {VTable*, void* source, short id} triple this drops the struct from 24 to 8 bytes, removes every
// unmanaged source block and every per-T vtable, and - most importantly - leaves no raw pointer
// alive: all operations validate the slot generation before touching the slot, so a stale handle
// fails fast instead of reading recycled memory.
//
// The task is also its own async method builder. The two pieces of builder state that used to live
// in AsyncLuminTaskMethodBuilder[<T>] are parked in the slot instead of in the struct: the state
// machine box (LuminTaskCoreSource.SetBox / BoxAt, applied by LuminTaskBuilder) and a synchronously
// raised exception (recorded by renting a slot and completing it faulted). What remains in the
// struct is the handle - and for LuminTask<T> the inline result of the synchronous path - so the
// size is unchanged: 8 bytes, and 8 + sizeof(T).
//
// `readonly` is applied member by member rather than to the struct, because the builder half has to
// write the handle. Awaiter-side members are all readonly, so awaiting through a readonly field or
// an `in` parameter still avoids a defensive copy.
[AsyncMethodBuilder(typeof(LuminTask))]
[StructLayout(LayoutKind.Sequential)]
public unsafe partial struct LuminTask : ICriticalNotifyCompletion
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
        get => _handle.IsNull() || LuminTaskCoreSource.IsCompleted(_handle);
    }

    [DebuggerHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly void GetResult()
    {
        if (_handle.IsNull())
            return;

        LuminTaskCoreSource.ConsumeVoid(_handle);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly void OnCompleted(Action continuation)
    {
        if (_handle.IsNull())
        {
            continuation();
        }
        else
        {
            LuminTaskCoreSource.OnCompleted(_handle, static s => ((Action)s)(), continuation);
        }
    }

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
            LuminTaskCoreSource.OnCompleted(_handle, continuation, state);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly void UnsafeOnCompleted(Action continuation) => OnCompleted(continuation);

    public readonly LuminTaskStatus Status
    {
        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _handle.IsNull() ? LuminTaskStatus.Succeeded : LuminTaskCoreSource.GetStatus(_handle);
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
        return _handle.IsNull() ? "()" : "(" + LuminTaskCoreSource.GetStatus(_handle) + ")";
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
        get => new LuminTask(_handle);
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

        // Read everything needed first: returning the box zeroes the state machine that hosts this
        // builder, so nothing may be read off `this` afterwards.
        LuminTaskBuilder.ReturnBox(handle);
        LuminTaskCoreSource.TrySetResult(handle);
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

        LuminTaskBuilder.ReturnBox(handle);
        LuminTaskCoreSource.TrySetException(handle, exception);
    }

    [DebuggerHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AwaitOnCompleted<TAwaiter, TStateMachine>(
        ref TAwaiter awaiter,
        ref TStateMachine stateMachine)
        where TAwaiter : INotifyCompletion
        where TStateMachine : IAsyncStateMachine
    {
        awaiter.OnCompleted(LuminTaskBuilder.GetOrCreateBox(ref _handle, ref stateMachine).MoveNextAction);
    }

    [DebuggerHidden]
    [SecuritySafeCritical]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(
        ref TAwaiter awaiter,
        ref TStateMachine stateMachine)
        where TAwaiter : ICriticalNotifyCompletion
        where TStateMachine : IAsyncStateMachine
    {
        awaiter.UnsafeOnCompleted(LuminTaskBuilder.GetOrCreateBox(ref _handle, ref stateMachine).MoveNextAction);
    }
}

[AsyncMethodBuilder(typeof(LuminTask<>))]
[StructLayout(LayoutKind.Sequential)]
public unsafe partial struct LuminTask<T> : ICriticalNotifyCompletion
{
    internal LuminTaskHandle _handle;

    // Only meaningful on the synchronous completion path, where no slot exists at all; the builder
    // writes it with SetResult(T) so a method that never suspends carries its value inline.
    internal T? _result;

    internal static readonly bool IsValueType = typeof(T).IsValueType;

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
        get => _handle.IsNull() || LuminTaskCoreSource.IsCompleted(_handle);
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
    {
        if (_handle.IsNull())
        {
            continuation();
        }
        else
        {
            LuminTaskCoreSource.OnCompleted(_handle, static s => ((Action)s)(), continuation);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly void UnsafeOnCompleted(Action continuation) => OnCompleted(continuation);

    public readonly LuminTaskStatus Status
    {
        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _handle.IsNull() ? LuminTaskStatus.Succeeded : LuminTaskCoreSource.GetStatus(_handle);
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
            : "(" + LuminTaskCoreSource.GetStatus(_handle) + ")";
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
        get => _handle.IsNull() ? new LuminTask<T>(_result!) : new LuminTask<T>(_handle);
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

        // `result` is a parameter, so it survives returning the box (which zeroes the state machine
        // that hosts this builder); the handle is captured before that as well.
        LuminTaskBuilder.ReturnBox(handle);
        LuminTaskResultBridge.TrySetResult(handle, in result);
    }

    [DebuggerHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetException(Exception exception)
    {
        LuminTaskHandle handle = _handle;

        if (handle.IsNull())
            handle = _handle = LuminTask.RentSlot();

        LuminTaskBuilder.ReturnBox(handle);
        LuminTaskCoreSource.TrySetException(handle, exception);
    }

    [DebuggerHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AwaitOnCompleted<TAwaiter, TStateMachine>(
        ref TAwaiter awaiter,
        ref TStateMachine stateMachine)
        where TAwaiter : INotifyCompletion
        where TStateMachine : IAsyncStateMachine
    {
        awaiter.OnCompleted(LuminTaskBuilder.GetOrCreateBox(ref _handle, ref stateMachine).MoveNextAction);
    }

    [DebuggerHidden]
    [SecuritySafeCritical]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(
        ref TAwaiter awaiter,
        ref TStateMachine stateMachine)
        where TAwaiter : ICriticalNotifyCompletion
        where TStateMachine : IAsyncStateMachine
    {
        awaiter.UnsafeOnCompleted(LuminTaskBuilder.GetOrCreateBox(ref _handle, ref stateMachine).MoveNextAction);
    }
}