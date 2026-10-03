using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
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
[AsyncMethodBuilder(typeof(AsyncLuminTaskMethodBuilder))]
[StructLayout(LayoutKind.Sequential)]
public readonly unsafe partial struct LuminTask : ICriticalNotifyCompletion
{
    internal readonly LuminTaskHandle _handle;

    public short Id => (short)(_handle.SlotRaw() - 1);

    [DebuggerHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal LuminTask(LuminTaskHandle handle)
    {
        _handle = handle;
    }

    [DebuggerHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public LuminTask GetAwaiter() => this;

    public bool IsCompleted
    {
        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _handle.IsNull() || LuminTaskCoreSource.IsCompleted(_handle);
    }

    [DebuggerHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void GetResult()
    {
        if (_handle.IsNull())
            return;

        LuminTaskCoreSource.ConsumeVoid(_handle);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void OnCompleted(Action continuation)
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
    public void SourceOnCompleted(Action<object> continuation, object state)
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
    public void UnsafeOnCompleted(Action continuation) => OnCompleted(continuation);

    public LuminTaskStatus Status
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
    public bool Equals(LuminTask other)
    {
        return other._handle == _handle;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override string? ToString()
    {
        return _handle.IsNull() ? "()" : "(" + LuminTaskCoreSource.GetStatus(_handle) + ")";
    }
}

[AsyncMethodBuilder(typeof(AsyncLuminTaskMethodBuilder<>))]
[StructLayout(LayoutKind.Sequential)]
public readonly unsafe partial struct LuminTask<T> : ICriticalNotifyCompletion
{
    internal readonly LuminTaskHandle _handle;

    // Only meaningful on the synchronous completion path, where no slot exists at all.
    internal readonly T? _result;

    internal static readonly bool IsValueType = typeof(T).IsValueType;

    public short Id => (short)(_handle.SlotRaw() - 1);

    public T? Result => _handle.IsNull()
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
    public LuminTask<T> GetAwaiter() => this;

    public bool IsCompleted
    {
        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _handle.IsNull() || LuminTaskCoreSource.IsCompleted(_handle);
    }

    [DebuggerHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T GetResult()
    {
        if (_handle.IsNull())
            return _result!;

        return LuminTaskResultBridge.Get<T>(_handle);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void OnCompleted(Action continuation)
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
    public void SourceOnCompleted(Action<object> continuation, object state)
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
    public void UnsafeOnCompleted(Action continuation) => OnCompleted(continuation);

    public LuminTaskStatus Status
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
    public static implicit operator ValueTask<T>(LuminTask<T> self)
    {
        if (self._handle.IsNull())
        {
            return new ValueTask<T>(self._result!);
        }

        return new ValueTask<T>(new ValueTaskSource<T>(self._handle), 0);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Equals(LuminTask<T> other)
    {
        return other._handle == _handle;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Equals(LuminTask other)
    {
        return other._handle == _handle;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override string? ToString()
    {
        return _handle.IsNull() ? _result?.ToString()
            : "(" + LuminTaskCoreSource.GetStatus(_handle) + ")";
    }
}