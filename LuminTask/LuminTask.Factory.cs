using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Threading;
using Lumin.Threading.Core;
using Lumin.Threading.Interface;
using Lumin.Threading.Source;
using Lumin.Threading.Tasks.Utility;

namespace Lumin.Threading.Tasks;

public delegate LuminTask LuminTaskFunction();
public delegate LuminTask<T> LuminTaskFunction<T>();
public delegate LuminTask LuminTaskFunctionWithToken(CancellationToken token);
public delegate LuminTask LuminTaskFunctionWithState<in T>(T value);

public readonly ref partial struct LuminTask
{
    public static LuminTask CompletedTask() => new ();
    
    public static LuminTask CanceledTask() => new (new CanceledResultSource(CancellationToken.None), 0);
    
    [DebuggerHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static LuminTask FromResult() => new(null, 0);

    public static LuminTask<T> FromResult<T>(T value) => new (value);
    
    public static LuminTask FromException(Exception ex)
    {
        if (ex is OperationCanceledException oce)
        {
            return FromCanceled(oce.CancellationToken);
        }

        return new LuminTask(new ExceptionResultSource(ex), 0);
    }
    
    public static LuminTask<T> FromException<T>(Exception ex)
    {
        if (ex is OperationCanceledException oce)
        {
            return FromCanceled<T>(oce.CancellationToken);
        }

        return new LuminTask<T>(new ExceptionResultSource<T>(ex), 0);
    }

    public static LuminTask FromCanceled(CancellationToken cancellationToken = default)
    {
        if (cancellationToken == CancellationToken.None)
        {
            return CanceledTask();
        }
        else
        {
            return new LuminTask(new CanceledResultSource(cancellationToken), 0);
        }
    }

    public static LuminTask<T> FromCanceled<T>(CancellationToken cancellationToken = default)
    {
        return new LuminTask<T>(new CanceledResultSource<T>(cancellationToken), 0);
    }

#if NET9_0_OR_GREATER
    public static LuminTask Create(Func<LuminTask> factory)
    {
        return factory();
    }

    public static LuminTask Create(Func<CancellationToken, LuminTask> factory, CancellationToken cancellationToken)
    {
        return factory(cancellationToken);
    }

    public static LuminTask Create<T>(T state, Func<T, LuminTask> factory)
    {
        return factory(state);
    }

    public static LuminTask<T> Create<T>(Func<LuminTask<T>> factory)
    {
        return factory();
    }
#endif
    public static LuminTask Create(LuminTaskFunction factory)
    {
        return factory();
    }

    public static LuminTask Create(LuminTaskFunctionWithToken factory, CancellationToken cancellationToken)
    {
        return factory(cancellationToken);
    }

    public static LuminTask Create<T>(T state, LuminTaskFunctionWithState<T> factory)
    {
        return factory(state);
    }

    public static LuminTask<T> Create<T>(LuminTaskFunction<T> factory)
    {
        return factory();
    }
    
    /// <summary>
    /// Never complete.
    /// </summary>
    public static LuminTask Never(CancellationToken cancellationToken)
    {
        return new LuminTask<AsyncUnit>(new NeverPromise<AsyncUnit>(cancellationToken), 0);
    }

    /// <summary>
    /// Never complete.
    /// </summary>
    public static LuminTask<T> Never<T>(CancellationToken cancellationToken)
    {
        return new LuminTask<T>(new NeverPromise<T>(cancellationToken), 0);
    }
    
    sealed class ExceptionResultSource : ILuminTaskSource
    {
        readonly ExceptionDispatchInfo _exception;
        bool _calledGet;

        public ExceptionResultSource(Exception exception)
        {
            this._exception = ExceptionDispatchInfo.Capture(exception);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void GetResult(short token)
        {
            if (!_calledGet)
            {
                _calledGet = true;
                GC.SuppressFinalize(this);
            }
            _exception.Throw();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public LuminTaskStatus GetStatus(short token)
        {
            return LuminTaskStatus.Faulted;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public LuminTaskStatus UnsafeGetStatus()
        {
            return LuminTaskStatus.Faulted;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void OnCompleted(Action<object> continuation, object state, short token)
        {
            continuation(state);
        }

        ~ExceptionResultSource()
        {
            if (!_calledGet)
            {
                LuminTaskScheduler.PublishUnobservedTaskException(_exception.SourceException);
            }
        }
    }

    sealed class ExceptionResultSource<T> : ILuminTaskSource<T>
    {
        readonly ExceptionDispatchInfo _exception;
        bool _calledGet;

        public ExceptionResultSource(Exception exception)
        {
            this._exception = ExceptionDispatchInfo.Capture(exception);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T GetResult(short token)
        {
            if (!_calledGet)
            {
                _calledGet = true;
                GC.SuppressFinalize(this);
            }
            _exception.Throw();
            return default;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void ILuminTaskSource.GetResult(short token)
        {
            if (!_calledGet)
            {
                _calledGet = true;
                GC.SuppressFinalize(this);
            }
            _exception.Throw();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public LuminTaskStatus GetStatus(short token)
        {
            return LuminTaskStatus.Faulted;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public LuminTaskStatus UnsafeGetStatus()
        {
            return LuminTaskStatus.Faulted;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void OnCompleted(Action<object> continuation, object state, short token)
        {
            continuation(state);
        }

        ~ExceptionResultSource()
        {
            if (!_calledGet)
            {
                LuminTaskScheduler.PublishUnobservedTaskException(_exception.SourceException);
            }
        }
    }

    sealed class CanceledResultSource : ILuminTaskSource
    {
        readonly CancellationToken _cancellationToken;

        public CanceledResultSource(CancellationToken cancellationToken)
        {
            this._cancellationToken = cancellationToken;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void GetResult(short token)
        {
            throw new OperationCanceledException(_cancellationToken);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public LuminTaskStatus GetStatus(short token)
        {
            return LuminTaskStatus.Canceled;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public LuminTaskStatus UnsafeGetStatus()
        {
            return LuminTaskStatus.Canceled;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void OnCompleted(Action<object> continuation, object state, short token)
        {
            continuation(state);
        }
    }

    sealed class CanceledResultSource<T> : ILuminTaskSource<T>
    {
        readonly CancellationToken _cancellationToken;

        public CanceledResultSource(CancellationToken cancellationToken)
        {
            this._cancellationToken = cancellationToken;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T GetResult(short token)
        {
            throw new OperationCanceledException(_cancellationToken);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void ILuminTaskSource.GetResult(short token)
        {
            throw new OperationCanceledException(_cancellationToken);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public LuminTaskStatus GetStatus(short token)
        {
            return LuminTaskStatus.Canceled;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public LuminTaskStatus UnsafeGetStatus()
        {
            return LuminTaskStatus.Canceled;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void OnCompleted(Action<object> continuation, object state, short token)
        {
            continuation(state);
        }
    }

    sealed class NeverPromise<T> : ILuminTaskSource<T>
    {
        static readonly Action<object> _cancellationCallback = CancellationCallback;

        CancellationToken _cancellationToken;
        LuminTaskCompletionSourceCore<T> _core;
        
        public NeverPromise(CancellationToken cancellationToken)
        {
            this._cancellationToken = cancellationToken;
            if (this._cancellationToken.CanBeCanceled)
            {
                this._cancellationToken.RegisterWithoutCaptureExecutionContext(_cancellationCallback, this);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void CancellationCallback(object state)
        {
            var self = (NeverPromise<T>)state;
            self._core.TrySetCanceled(self._cancellationToken);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T GetResult(short token)
        {
            return _core.GetResult(token);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public LuminTaskStatus GetStatus(short token)
        {
            return _core.GetStatus(token);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public LuminTaskStatus UnsafeGetStatus()
        {
            return _core.UnsafeGetStatus();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void OnCompleted(Action<object> continuation, object state, short token)
        {
            _core.OnCompleted(continuation, state, token);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void ILuminTaskSource.GetResult(short token)
        {
            _core.GetResult(token);
        }
    }
    
} 

public readonly ref partial struct LuminTask<T>
{
    [DebuggerHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static LuminTask<T> FromResult(T result) => new(result);
}


internal static class CompletedTasks
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static LuminTask<AsyncUnit> AsyncUnit() => LuminTask<AsyncUnit>.FromResult(Utility.AsyncUnit.Default);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static LuminTask<bool> True() => LuminTask<bool>.FromResult(true);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static LuminTask<bool> False() => LuminTask<bool>.FromResult(false);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static LuminTask<int> Zero() => LuminTask<int>.FromResult(0);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static LuminTask<int> MinusOne() => LuminTask<int>.FromResult(-1);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static LuminTask<int> One() => LuminTask<int>.FromResult(1);
}