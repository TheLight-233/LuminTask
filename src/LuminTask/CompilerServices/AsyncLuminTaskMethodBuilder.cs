using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security;
using LuminThread.TaskSource;
using LuminThread.Utility;

namespace LuminThread.CompilerServices
{
    // Builder for a non-generic async LuminTask. No source object exists up front: the slot is
    // rented on the first suspension, so an async method that completes synchronously returns the
    // zero handle and allocates nothing at all. What the builder carries between suspensions is a
    // 64-bit handle, not a pointer into unmanaged memory, so a recycled slot fails the generation
    // check instead of being read as stale memory.
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct AsyncLuminTaskMethodBuilder
    {
        private LuminTaskHandle _handle;
        private IStateMachineBox? _box;
        private Exception? _exception;

        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static AsyncLuminTaskMethodBuilder Create()
            => default;

        public LuminTask Task
        {
            [DebuggerHidden]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                if (_handle != 0)
                    return new(_handle);
                if (_exception != null)
                    return LuminTask.FromException(_exception);
                return LuminTask.CompletedTask();
            }
        }

        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetResult()
        {
            // _box != null  <=>  _handle != 0 (both are created together on first suspension).
            if (_box != null)
            {
                LuminTaskHandle handle = _handle;
                var box = _box;
                box.Return();   // zeroes the state machine (== `this`); read nothing off `this` after.
                LuminTaskCoreSource.TrySetResult(handle);
            }
        }

        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetException(Exception exception)
        {
            if (_box != null)
            {
                LuminTaskHandle handle = _handle;
                var box = _box;
                box.Return();
                LuminTaskCoreSource.TrySetException(handle, exception);
            }
            else
            {
                _exception = exception;
            }
        }

        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AwaitOnCompleted<TAwaiter, TStateMachine>(
            ref TAwaiter awaiter,
            ref TStateMachine stateMachine)
            where TAwaiter : INotifyCompletion
            where TStateMachine : IAsyncStateMachine
        {
            if (_box == null)
            {
                if (_handle == 0)
                    _handle = LuminTask.RentSlot();
                var box = StateMachineBox<TStateMachine>.Rent();
                _box = box;                       // set this._box BEFORE copying the state machine
                box.StateMachine = stateMachine;  // the copy now carries _handle / _box
            }
            awaiter.OnCompleted(_box.MoveNextAction);
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
            if (_box == null)
            {
                if (_handle == 0)
                    _handle = LuminTask.RentSlot();
                var box = StateMachineBox<TStateMachine>.Rent();
                _box = box;
                box.StateMachine = stateMachine;
            }
            awaiter.UnsafeOnCompleted(_box.MoveNextAction);
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
    }

    // Same layout rule as the non-generic builder, plus the synchronous result. A method that
    // completes without suspending carries its value in _result and never rents a slot - that is
    // what makes the non-suspended path allocation free.
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct AsyncLuminTaskMethodBuilder<T>
    {
        private LuminTaskHandle _handle;
        private IStateMachineBox? _box;
        private Exception? _exception;
        private T _result;

        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static AsyncLuminTaskMethodBuilder<T> Create()
            => default;

        public LuminTask<T> Task
        {
            [DebuggerHidden]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                if (_handle != 0)
                    return new(_handle);
                if (_exception != null)
                    return LuminTask.FromException<T>(_exception);
                return new LuminTask<T>(_result);
            }
        }

        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetResult(T result)
        {
            if (_box != null)
            {
                LuminTaskHandle handle = _handle;
                var box = _box;
                box.Return();
                LuminTaskResultBridge.TrySetResult(handle, in result);
            }
            else
            {
                _result = result;
            }
        }

        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetException(Exception exception)
        {
            if (_box != null)
            {
                LuminTaskHandle handle = _handle;
                var box = _box;
                box.Return();
                LuminTaskCoreSource.TrySetException(handle, exception);
            }
            else
            {
                _exception = exception;
            }
        }

        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AwaitOnCompleted<TAwaiter, TStateMachine>(
            ref TAwaiter awaiter,
            ref TStateMachine stateMachine)
            where TAwaiter : INotifyCompletion
            where TStateMachine : IAsyncStateMachine
        {
            if (_box == null)
            {
                if (_handle == 0)
                    _handle = LuminTask.RentSlot();
                var box = StateMachineBox<TStateMachine>.Rent();
                _box = box;
                box.StateMachine = stateMachine;
            }
            awaiter.OnCompleted(_box.MoveNextAction);
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
            if (_box == null)
            {
                if (_handle == 0)
                    _handle = LuminTask.RentSlot();
                var box = StateMachineBox<TStateMachine>.Rent();
                _box = box;
                box.StateMachine = stateMachine;
            }
            awaiter.UnsafeOnCompleted(_box.MoveNextAction);
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
    }
}