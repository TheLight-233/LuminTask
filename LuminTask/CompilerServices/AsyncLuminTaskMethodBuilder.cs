
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security;
using Lumin.Threading.Source;
using Lumin.Threading.Core;

namespace Lumin.Threading.Tasks.CompilerServices
{
    public struct AsyncLuminTaskMethodBuilder
    {
        private LuminTaskSource? _source;
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
                if (_isCompleted && _source == null)
                    return LuminTask.FromResult();

                if (_exception != null && _source == null)
                {
                    var src = LuminTaskSource.Rent(_continueOnCapturedContext);
                    src.TrySetException(_exception);
                    return new LuminTask(src, src.Token);
                }

                // 延迟创建 source
                _source ??= LuminTaskSource.Rent(_continueOnCapturedContext);
        
                // 若已设置状态，同步到新 source
                if (_isCompleted)
                    _source.TrySetResult();
                else if (_exception != null)
                    _source.TrySetException(_exception);

                return new LuminTask(_source, _source.Token);
            }
        }

        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetResult()
        {
            if (_source != null)
            {
                _source.TrySetResult();
                LuminTaskSource.Return(_source);
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
                _source.TrySetException(exception);
                LuminTaskSource.Return(_source);
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
            var src = _source ??= LuminTaskSource.Rent(_continueOnCapturedContext);

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
            var src = _source ??= LuminTaskSource.Rent(_continueOnCapturedContext);

            awaiter.UnsafeOnCompleted(stateMachine.MoveNext);
        }

        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Start<TStateMachine>(ref TStateMachine stateMachine)
            where TStateMachine : IAsyncStateMachine
            => stateMachine.MoveNext();

        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetStateMachine(IAsyncStateMachine stateMachine) { }

        private static class MoveNextRunner<TStateMachine>
            where TStateMachine : IAsyncStateMachine
        {
            [DebuggerHidden]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static void Schedule(ref TStateMachine stateMachine, LuminTaskSource source)
            {
                source.OnCompleted(
                    static s => ((TStateMachine)s).MoveNext(),
                    stateMachine,
                    source.Token);
            }
        }
    }

    [StructLayout(LayoutKind.Auto)]
    public struct AsyncLuminTaskMethodBuilder<T>
    {
        private LuminTaskSource<T>? _source;
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
                if (_haveResult && _source == null && _exception == null)
                {
                    return LuminTask<T>.FromResult(_result);
                }

                if (_source == null)
                {
                    _source = LuminTaskSource<T>.Rent(_continueOnCapturedContext);

                    if (_haveResult)
                    {
                        _source.TrySetResult(_result);
                        _haveResult = false;
                    }
                    else if (_exception != null)
                    {
                        _source.TrySetException(_exception);
                        _exception = null;
                    }
                }

                return new LuminTask<T>(_source, _source.Token);
            }
        }

        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetResult(T result)
        {
            if (_source != null)
            {
                _source.TrySetResult(result);
                LuminTaskSource<T>.Return(_source);
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
                _source.TrySetException(exception);
                LuminTaskSource<T>.Return(_source);
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
            var src = _source ??= LuminTaskSource<T>.Rent(_continueOnCapturedContext);

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
            var src = _source ??= LuminTaskSource<T>.Rent(_continueOnCapturedContext);

            awaiter.UnsafeOnCompleted(stateMachine.MoveNext);
        }

        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Start<TStateMachine>(ref TStateMachine stateMachine)
            where TStateMachine : IAsyncStateMachine
            => stateMachine.MoveNext();

        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetStateMachine(IAsyncStateMachine stateMachine) { }

        private static class MoveNextRunner<TStateMachine>
            where TStateMachine : IAsyncStateMachine
        {
            [DebuggerHidden]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static void Schedule(ref TStateMachine stateMachine, LuminTaskSource<T> source)
            {
                source.OnCompleted(
                    static s => ((TStateMachine)s).MoveNext(),
                    stateMachine,
                    source.Token);
            }
        }
    }
}