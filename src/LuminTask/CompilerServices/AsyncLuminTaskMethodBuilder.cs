using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security;
using LuminThread.TaskSource;
using LuminThread.Utility;

namespace LuminThread.CompilerServices
{
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct AsyncLuminTaskMethodBuilder
    {
        private LuminTaskSourceCore<AsyncUnit>* _source;
        private IStateMachineBox? _box;
        private bool _continueOnCapturedContext;
        private Exception? _exception;
        private bool _isCompleted;

        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static AsyncLuminTaskMethodBuilder Create()
            => new() { _continueOnCapturedContext = false };

        public LuminTask Task
        {
            [DebuggerHidden]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                if (_source != null)
                    return new(LuminTaskSourceCore<AsyncUnit>.MethodTableAutoDisposePtr, _source, _source->Id);
                if (_exception != null)
                {
                    var es = ExceptionResultSource<AsyncUnit>.Create(_exception);
                    return new(ExceptionResultSource<AsyncUnit>.MethodTablePtr, es, es->Id);
                }
                return LuminTask.CompletedTask();
            }
        }

        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetResult()
        {
            // _box != null  <=>  _source != null (both created together on first suspension).
            if (_box != null)
            {
                var source = _source;
                var box = _box;
                box.Return();   // zeroes the state machine (== `this`); read nothing off `this` after.
                LuminTaskSourceCore<AsyncUnit>.TrySetResult(source);
            }
            else
            {
                _isCompleted = true;
            }
        }

        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetException(Exception exception)
        {
            if (_box != null)
            {
                var source = _source;
                var box = _box;
                box.Return();
                LuminTaskSourceCore<AsyncUnit>.TrySetException(source, exception);
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
                if (_source == null)
                    _source = LuminTaskSourceCore<AsyncUnit>.Create(_continueOnCapturedContext);
                var box = StateMachineBox<TStateMachine>.Rent();
                _box = box;                       // set this._box BEFORE copying the state machine
                box.StateMachine = stateMachine;  // the copy now carries _box / _source
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
                if (_source == null)
                    _source = LuminTaskSourceCore<AsyncUnit>.Create(_continueOnCapturedContext);
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

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct AsyncLuminTaskMethodBuilder<T>
    {
        private LuminTaskSourceCore<T>* _source;
        private IStateMachineBox? _box;
        private bool _continueOnCapturedContext;
        private Exception? _exception;
        private T _result;
        private bool _haveResult;

        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static AsyncLuminTaskMethodBuilder<T> Create()
            => new() { _continueOnCapturedContext = false };

        public LuminTask<T> Task
        {
            [DebuggerHidden]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                if (_source != null)
                    return new(LuminTaskSourceCore<T>.MethodTableAutoDisposePtr, _source, _source->Id);
                if (_exception != null)
                {
                    var es = ExceptionResultSource<T>.Create(_exception);
                    return new(ExceptionResultSource<T>.MethodTablePtr, es, es->Id);
                }
                return new LuminTask<T>(_result);
            }
        }

        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetResult(T result)
        {
            if (_box != null)
            {
                var source = _source;
                var box = _box;
                box.Return();
                LuminTaskSourceCore<T>.TrySetResult(source, result);
            }
            else
            {
                _result = result;
                _haveResult = true;
            }
        }

        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetException(Exception exception)
        {
            if (_box != null)
            {
                var source = _source;
                var box = _box;
                box.Return();
                LuminTaskSourceCore<T>.TrySetException(source, exception);
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
                if (_source == null)
                    _source = LuminTaskSourceCore<T>.Create(_continueOnCapturedContext);
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
                if (_source == null)
                    _source = LuminTaskSourceCore<T>.Create(_continueOnCapturedContext);
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
