
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
            get => new (LuminTaskSourceCore<AsyncUnit>.MethodTable, _source, _source->Id);
        }

        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetResult()
        {
            if (_source != null)
            {
                LuminTaskSourceCore<AsyncUnit>.TrySetResult(_source);
                LuminTaskSourceCore<AsyncUnit>.Dispose(_source);
                _source = null;
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
            if (_source != null)
            {
                LuminTaskSourceCore<AsyncUnit>.TrySetException(_source, exception);
                LuminTaskSourceCore<AsyncUnit>.Dispose(_source);
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
            awaiter.OnCompleted(stateMachine.MoveNext);
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
            awaiter.UnsafeOnCompleted(stateMachine.MoveNext);
        }

        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Start<TStateMachine>(ref TStateMachine stateMachine)
            where TStateMachine : IAsyncStateMachine
        {
            if (_source is null)
                _source = LuminTaskSourceCore<AsyncUnit>.Create();
            
            stateMachine.MoveNext();
        }

        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetStateMachine(IAsyncStateMachine stateMachine) { }

        private static class MoveNextRunner<TStateMachine>
            where TStateMachine : IAsyncStateMachine
        {
            [DebuggerHidden]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static void Schedule(ref TStateMachine stateMachine, LuminTaskSourceCore<AsyncUnit>* source)
            {
                LuminTaskSourceCore<AsyncUnit>.MethodTable.OnCompleted(
                    source,
                    static s => ((TStateMachine)s).MoveNext(),
                    stateMachine,
                    source->Id);
            }
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct AsyncLuminTaskMethodBuilder<T>
    {
        private LuminTaskSourceCore<T>* _source;
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
            get => new (LuminTaskSourceCore<T>.MethodTable, _source, _source->Id);
        }

        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetResult(T result)
        {
            if (_source != null)
            {
                LuminTaskSourceCore<T>.TrySetResult(_source, result);
                LuminTaskSourceCore<T>.Dispose(_source);
                _source = null;
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
            if (_source != null)
            {
                LuminTaskSourceCore<T>.TrySetException(_source, exception);
                LuminTaskSourceCore<T>.Dispose(_source);
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
            awaiter.OnCompleted(stateMachine.MoveNext);
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
            awaiter.UnsafeOnCompleted(stateMachine.MoveNext);
        }

        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Start<TStateMachine>(ref TStateMachine stateMachine)
            where TStateMachine : IAsyncStateMachine
        {
            if (_source is null)
                _source = LuminTaskSourceCore<T>.Create(_continueOnCapturedContext);
            stateMachine.MoveNext();
        }

        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetStateMachine(IAsyncStateMachine stateMachine) { }

        private static class MoveNextRunner<TStateMachine>
            where TStateMachine : IAsyncStateMachine
        {
            [DebuggerHidden]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static void Schedule(ref TStateMachine stateMachine, LuminTaskSourceCore<T>* source)
            {
                LuminTaskSourceCore<T>.OnCompleted(
                    source,
                    static s => ((TStateMachine)s).MoveNext(),
                    stateMachine,
                    source->Id);
            }
        }
    }
}