using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Threading;
using Lumin.Threading.Interface;
using Lumin.Threading.Tasks.Utility;
using Lumin.Threading.Tasks;

namespace Lumin.Threading.Source
{
    // 基础接口定义
    public interface IResolvePromise
    {
        bool TrySetResult();
    }

    public interface IResolvePromise<T>
    {
        bool TrySetResult(T value);
    }

    public interface IRejectPromise
    {
        bool TrySetException(Exception exception);
    }

    public interface ICancelPromise
    {
        bool TrySetCanceled(CancellationToken cancellationToken = default);
    }

    public interface IPromise<T> : IResolvePromise<T>, IRejectPromise, ICancelPromise { }
    public interface IPromise : IResolvePromise, IRejectPromise, ICancelPromise { }
    
    internal static class LuminTaskCompletionSourceCoreShared
    {
        public static readonly Action<object> Sentinel = CompletionSentinel;

        private static void CompletionSentinel(object _)
        {
            throw new InvalidOperationException("Sentinel delegate should never be invoked");
        }
    }

    internal class ExceptionHolder
    {
        private readonly ExceptionDispatchInfo _exception;
        private bool _calledGet;

        public ExceptionHolder(ExceptionDispatchInfo exception)
        {
            _exception = exception;
        }

        public ExceptionDispatchInfo GetException()
        {
            if (!_calledGet)
            {
                _calledGet = true;
                GC.SuppressFinalize(this);
            }
            return _exception;
        }

        ~ExceptionHolder()
        {
            if (!_calledGet)
            {
                LuminTaskScheduler.PublishUnobservedTaskException(_exception.SourceException);
            }
        }
    }
    
    [StructLayout(LayoutKind.Auto)]
    public struct LuminTaskCompletionSourceCore<TResult>
    {
        private TResult _result;
        private object _error; // ExceptionDispatchInfo or OperationCanceledException
        private short _version;
        private int _completedCount; // 0: not completed, 1: completed
        private Action<object> _continuation;
        private object _continuationState;
        private bool _hasUnhandledError;

        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Reset()
        {
            ReportUnhandledError();
            
            unchecked
            {
                _version++;
            }
            
            _completedCount = 0;
            _result = default;
            _error = null;
            _hasUnhandledError = false;
            _continuation = null;
            _continuationState = null;
        }

        private void ReportUnhandledError()
        {
            if (_hasUnhandledError)
            {
                try
                {
                    if (_error is OperationCanceledException oc)
                    {
                        LuminTaskScheduler.PublishUnobservedTaskException(oc);
                    }
                    else if (_error is ExceptionHolder eh)
                    {
                        LuminTaskScheduler.PublishUnobservedTaskException(eh.GetException().SourceException);
                    }
                }
                catch
                {
                    // Ignore
                }
            }
        }

        internal void MarkHandled()
        {
            _hasUnhandledError = false;
        }

        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TrySetResult(TResult result)
        {
            if (Interlocked.CompareExchange(ref _completedCount, 1, 0) == 0)
            {
                _result = result;
                InvokeContinuation();
                return true;
            }
            return false;
        }

        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TrySetException(Exception exception)
        {
            if (Interlocked.CompareExchange(ref _completedCount, 1, 0) == 0)
            {
                _hasUnhandledError = true;
                if (exception is OperationCanceledException)
                {
                    _error = exception;
                }
                else
                {
                    _error = new ExceptionHolder(ExceptionDispatchInfo.Capture(exception));
                }
                InvokeContinuation();
                return true;
            }
            return false;
        }

        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TrySetCanceled(CancellationToken cancellationToken = default)
        {
            if (Interlocked.CompareExchange(ref _completedCount, 1, 0) == 0)
            {
                _hasUnhandledError = true;
                _error = new OperationCanceledException(cancellationToken);
                InvokeContinuation();
                return true;
            }
            return false;
        }

        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void InvokeContinuation()
        {
            var cont = _continuation;
            var state = _continuationState;
            _continuation = null;
            _continuationState = null;
            
            if (cont != null || Interlocked.CompareExchange(ref _continuation, LuminTaskCompletionSourceCoreShared.Sentinel, null) != null)
            {
                cont?.Invoke(state);
            }
        }

        [DebuggerHidden]
        public short Version => _version;

        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public LuminTaskStatus GetStatus(short token)
        {
            ValidateToken(token);
            return (_continuation == null || _completedCount == 0) ? LuminTaskStatus.Pending
                 : (_error == null) ? LuminTaskStatus.Succeeded
                 : (_error is OperationCanceledException) ? LuminTaskStatus.Canceled
                 : LuminTaskStatus.Faulted;
        }

        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public LuminTaskStatus UnsafeGetStatus()
        {
            return (_continuation == null || _completedCount == 0) ? LuminTaskStatus.Pending
                 : (_error == null) ? LuminTaskStatus.Succeeded
                 : (_error is OperationCanceledException) ? LuminTaskStatus.Canceled
                 : LuminTaskStatus.Faulted;
        }

        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TResult GetResult(short token)
        {
            ValidateToken(token);
            if (_completedCount == 0)
            {
                ThrowNotCompleted();
            }

            if (_error != null)
            {
                _hasUnhandledError = false;
                if (_error is OperationCanceledException oce)
                {
                    throw oce;
                }
                else if (_error is ExceptionHolder eh)
                {
                    eh.GetException().Throw();
                }
                ThrowInvalidError();
            }

            return _result;
        }

        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void OnCompleted(Action<object> continuation, object state, short token)
        {
            if (continuation == null) ThrowArgumentNull(nameof(continuation));
            ValidateToken(token);

            // 检查是否已经设置过continuation
            object oldContinuation = _continuation;
            if (oldContinuation == null)
            {
                _continuationState = state;
                oldContinuation = Interlocked.CompareExchange(ref _continuation, continuation, null);
            }

            if (oldContinuation != null)
            {
                if (!ReferenceEquals(oldContinuation, LuminTaskCompletionSourceCoreShared.Sentinel))
                {
                    ThrowMultipleContinuations();
                }
                continuation(state);
            }
        }

        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private void ValidateToken(short token)
        {
            if (token != _version)
            {
                ThrowTokenMismatch();
            }
        }

        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowTokenMismatch() => 
            throw new InvalidOperationException("Token version mismatch");

        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowNotCompleted() => 
            throw new InvalidOperationException("Operation not completed");

        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowInvalidError() => 
            throw new InvalidOperationException("Critical: invalid error type");

        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowArgumentNull(string param) => 
            throw new ArgumentNullException(param);

        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowMultipleContinuations() => 
            throw new InvalidOperationException("Multiple continuations registered");
    }
    
    // 自动重置的任务源 (非泛型)
    public sealed class AutoResetLuminTaskCompletionSource : 
        ILuminTaskSource, ITaskPoolNode<AutoResetLuminTaskCompletionSource>, IPromise
    {
        private static readonly TaskPool<AutoResetLuminTaskCompletionSource> Pool = 
            new TaskPool<AutoResetLuminTaskCompletionSource>(() => new AutoResetLuminTaskCompletionSource());
        
        private AutoResetLuminTaskCompletionSource _nextNode;
        public ref AutoResetLuminTaskCompletionSource NextNode => ref _nextNode;
        
        private LuminTaskStatus _status = LuminTaskStatus.Pending;
        private ExceptionDispatchInfo _exception;
        private Action<object> _continuation;
        private object _state;
        private short _token;
        private bool _continueOnCapturedContext = true;
        
        public short Token => _token;

        private AutoResetLuminTaskCompletionSource() { }

        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static AutoResetLuminTaskCompletionSource Create()
        {
            if (!Pool.TryPop(out var result))
            {
                result = new AutoResetLuminTaskCompletionSource();
            }
            result._token++;
            return result;
        }

        [DebuggerHidden]
        public static AutoResetLuminTaskCompletionSource CreateFromCanceled(
            CancellationToken cancellationToken, out short token)
        {
            var source = Create();
            source.TrySetCanceled(cancellationToken);
            token = source._token;
            return source;
        }

        [DebuggerHidden]
        public static AutoResetLuminTaskCompletionSource CreateFromException(
            Exception exception, out short token)
        {
            var source = Create();
            source.TrySetException(exception);
            token = source._token;
            return source;
        }

        [DebuggerHidden]
        public static AutoResetLuminTaskCompletionSource CreateCompleted(out short token)
        {
            var source = Create();
            source.TrySetResult();
            token = source._token;
            return source;
        }

        public Tasks.LuminTask Task
        {
            [DebuggerHidden]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new (this, _token);
        }

        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TrySetResult()
        {
            if (_status != LuminTaskStatus.Pending) return false;
            
            _status = LuminTaskStatus.Succeeded;
            ExecuteContinuation();
            return true;
        }

        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TrySetCanceled(CancellationToken cancellationToken = default)
        {
            if (_status != LuminTaskStatus.Pending) return false;
            
            _status = LuminTaskStatus.Canceled;
            _exception = ExceptionDispatchInfo.Capture(new OperationCanceledException(cancellationToken));
            ExecuteContinuation();
            return true;
        }

        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TrySetException(Exception exception)
        {
            if (_status != LuminTaskStatus.Pending) return false;
            if (exception is OperationCanceledException oce)
            {
                return TrySetCanceled(oce.CancellationToken);
            }
            
            _status = LuminTaskStatus.Faulted;
            _exception = ExceptionDispatchInfo.Capture(exception);
            ExecuteContinuation();
            return true;
        }

        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ExecuteContinuation()
        {
            var cont = _continuation;
            var state = _state;
            _continuation = null;
            _state = null;
            
            if (cont != null)
            {
                if (_continueOnCapturedContext)
                {
                    var context = SynchronizationContext.Current;
                    if (context != null)
                    {
                        context.Post(s => cont(s), state);
                        return;
                    }
                }
                cont(state);
            }
        }

        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void GetResult(short token)
        {
            if (token != _token) ThrowTokenMismatch();
            
            switch (_status)
            {
                case LuminTaskStatus.Succeeded:
                    break;
                case LuminTaskStatus.Faulted:
                    _exception?.Throw();
                    _exception = null;
                    ThrowInvalidOperation("Task faulted");
                    break;
                case LuminTaskStatus.Canceled:
                    ThrowOperationCancel("Task canceled");
                    break;
                default:
                    ThrowInvalidOperation("Task not completed");
                    break;
            }
        }

        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public LuminTaskStatus GetStatus(short token)
        {
            return token == _token ? _status : LuminTaskStatus.Faulted;
        }

        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public LuminTaskStatus UnsafeGetStatus() => _status;

        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void OnCompleted(Action<object> continuation, object state, short token)
        {
            if (token != _token)
            {
                continuation(state);
                return;
            }
            
            if (_status != LuminTaskStatus.Pending)
            {
                continuation(state);
            }
            else
            {
                _continuation = continuation;
                _state = state;
            }
        }

        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryReturn()
        {
            _status = LuminTaskStatus.Pending;
            _exception = null;
            _continuation = null;
            _state = null;
            return Pool.TryPush(this);
        }

        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowTokenMismatch() => 
            throw new InvalidOperationException("Token mismatch");

        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowInvalidOperation(string message) => 
            throw new InvalidOperationException(message);
        
        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowOperationCancel(string message) => 
            throw new OperationCanceledException(message);
    }

    // 自动重置的任务源 (泛型)
    public sealed class AutoResetLuminTaskCompletionSource<T> : 
        ILuminTaskSource<T>, ITaskPoolNode<AutoResetLuminTaskCompletionSource<T>>, IPromise<T>
    {
        private static readonly TaskPool<AutoResetLuminTaskCompletionSource<T>> Pool = 
            new TaskPool<AutoResetLuminTaskCompletionSource<T>>(() => new AutoResetLuminTaskCompletionSource<T>());
        
        private AutoResetLuminTaskCompletionSource<T> _nextNode;
        public ref AutoResetLuminTaskCompletionSource<T> NextNode => ref _nextNode;
        
        private LuminTaskStatus _status = LuminTaskStatus.Pending;
        private ExceptionDispatchInfo _exception;
        private T _result;
        private Action<object> _continuation;
        private object _state;
        private short _token;
        private bool _continueOnCapturedContext = true;

        public short Token => _token;
        
        private AutoResetLuminTaskCompletionSource() { }

        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static AutoResetLuminTaskCompletionSource<T> Create()
        {
            if (!Pool.TryPop(out var result))
            {
                result = new AutoResetLuminTaskCompletionSource<T>();
            }
            result._token++;
            return result;
        }

        [DebuggerHidden]
        public static AutoResetLuminTaskCompletionSource<T> CreateFromCanceled(
            CancellationToken cancellationToken, out short token)
        {
            var source = Create();
            source.TrySetCanceled(cancellationToken);
            token = source._token;
            return source;
        }

        [DebuggerHidden]
        public static AutoResetLuminTaskCompletionSource<T> CreateFromException(
            Exception exception, out short token)
        {
            var source = Create();
            source.TrySetException(exception);
            token = source._token;
            return source;
        }

        [DebuggerHidden]
        public static AutoResetLuminTaskCompletionSource<T> CreateFromResult(
            T result, out short token)
        {
            var source = Create();
            source.TrySetResult(result);
            token = source._token;
            return source;
        }

        public LuminTask<T> Task
        {
            [DebuggerHidden]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new LuminTask<T>(this, _token);
        }

        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TrySetResult(T result)
        {
            if (_status != LuminTaskStatus.Pending) return false;
            
            _status = LuminTaskStatus.Succeeded;
            _result = result;
            ExecuteContinuation();
            return true;
        }

        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TrySetCanceled(CancellationToken cancellationToken = default)
        {
            if (_status != LuminTaskStatus.Pending) return false;
            
            _status = LuminTaskStatus.Canceled;
            _exception = ExceptionDispatchInfo.Capture(new OperationCanceledException(cancellationToken));
            ExecuteContinuation();
            return true;
        }

        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TrySetException(Exception exception)
        {
            if (_status != LuminTaskStatus.Pending) return false;
            if (exception is OperationCanceledException oce)
            {
                return TrySetCanceled(oce.CancellationToken);
            }
            
            _status = LuminTaskStatus.Faulted;
            _exception = ExceptionDispatchInfo.Capture(exception);
            ExecuteContinuation();
            return true;
        }

        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ExecuteContinuation()
        {
            var cont = _continuation;
            var state = _state;
            _continuation = null;
            _state = null;
            
            if (cont != null)
            {
                if (_continueOnCapturedContext)
                {
                    var context = SynchronizationContext.Current;
                    if (context != null)
                    {
                        context.Post(s => cont(s), state);
                        return;
                    }
                }
                cont(state);
            }
        }

        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void ILuminTaskSource.GetResult(short token)
        {
            GetResult(token);
        }

        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T GetResult(short token)
        {
            if (token != _token) ThrowTokenMismatch();
            
            switch (_status)
            {
                case LuminTaskStatus.Succeeded:
                    return _result;
                case LuminTaskStatus.Faulted:
                    _exception?.Throw();
                    _exception = null;
                    ThrowInvalidOperation("Task faulted");
                    break;
                case LuminTaskStatus.Canceled:
                    ThrowOperationCancel("Task canceled");
                    break;
                default:
                    ThrowInvalidOperation("Task not completed");
                    break;
            }
            return default;
        }

        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public LuminTaskStatus GetStatus(short token)
        {
            return token == _token ? _status : LuminTaskStatus.Faulted;
        }

        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public LuminTaskStatus UnsafeGetStatus() => _status;

        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void OnCompleted(Action<object> continuation, object state, short token)
        {
            if (token != _token)
            {
                continuation(state);
                return;
            }
            
            if (_status != LuminTaskStatus.Pending)
            {
                continuation(state);
            }
            else
            {
                _continuation = continuation;
                _state = state;
            }
        }

        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryReturn()
        {
            _status = LuminTaskStatus.Pending;
            _exception = null;
            _result = default;
            _continuation = null;
            _state = null;
            return Pool.TryPush(this);
        }

        private void Reset()
        {
            _status = LuminTaskStatus.Pending;
            _exception = null;
            _result = default;
            _continuation = null;
            _state = null;
        }

        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowTokenMismatch() => 
            throw new InvalidOperationException("Token mismatch");

        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowInvalidOperation(string message) => 
            throw new InvalidOperationException(message);
        
        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowOperationCancel(string message) => 
            throw new OperationCanceledException(message);
    }

    // 基础任务源 (非泛型)
    public sealed class LuminTaskCompletionSource : ILuminTaskSource, IPromise
    {
        private readonly object _lock = new object();
        private LuminTaskStatus _status = LuminTaskStatus.Pending;
        private ExceptionDispatchInfo _exception;
        private Action<object> _singleContinuation;
        private object _singleState;
        private List<(Action<object>, object)> _secondaryContinuations;
        private short _token;

        public LuminTaskCompletionSource()
        {
            _token = (short)Environment.TickCount;
        }

        public Tasks.LuminTask Task
        {
            [DebuggerHidden]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new (this, _token);
        }

        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TrySetResult()
        {
            return TrySignalCompletion(LuminTaskStatus.Succeeded);
        }

        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TrySetCanceled(CancellationToken cancellationToken = default)
        {
            if (UnsafeGetStatus() != LuminTaskStatus.Pending) return false;
            
            _exception = ExceptionDispatchInfo.Capture(new OperationCanceledException(cancellationToken));
            return TrySignalCompletion(LuminTaskStatus.Canceled);
        }

        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TrySetException(Exception exception)
        {
            if (UnsafeGetStatus() != LuminTaskStatus.Pending) return false;
            
            if (exception is OperationCanceledException oce)
            {
                return TrySetCanceled(oce.CancellationToken);
            }
            
            _exception = ExceptionDispatchInfo.Capture(exception);
            return TrySignalCompletion(LuminTaskStatus.Faulted);
        }

        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void GetResult(short token)
        {
            if (token != _token) ThrowTokenMismatch();
            
            switch (_status)
            {
                case LuminTaskStatus.Succeeded:
                    return;
                case LuminTaskStatus.Faulted:
                    _exception?.Throw();
                    break;
                case LuminTaskStatus.Canceled:
                    ThrowOperationCancel("Task canceled");
                    break;
                default:
                    ThrowInvalidOperation("Task not completed");
                    break;
            }
        }

        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public LuminTaskStatus GetStatus(short token)
        {
            return token == _token ? _status : LuminTaskStatus.Faulted;
        }

        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public LuminTaskStatus UnsafeGetStatus() => _status;

        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void OnCompleted(Action<object> continuation, object state, short token)
        {
            lock (_lock)
            {
                if (_status != LuminTaskStatus.Pending)
                {
                    continuation(state);
                    return;
                }

                if (_singleContinuation == null)
                {
                    _singleContinuation = continuation;
                    _singleState = state;
                }
                else
                {
                    _secondaryContinuations ??= new List<(Action<object>, object)>();
                    _secondaryContinuations.Add((continuation, state));
                }
            }
        }

        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool TrySignalCompletion(LuminTaskStatus status)
        {
            lock (_lock)
            {
                if (_status != LuminTaskStatus.Pending) return false;
                
                _status = status;
                
                if (_singleContinuation != null)
                {
                    try
                    {
                        _singleContinuation(_singleState);
                    }
                    catch (Exception ex)
                    {
                        // 记录异常但不传播
                        Debug.WriteLine($"Continuation exception: {ex}");
                    }
                }

                if (_secondaryContinuations != null)
                {
                    foreach (var (cont, state) in _secondaryContinuations)
                    {
                        try
                        {
                            cont(state);
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"Continuation exception: {ex}");
                        }
                    }
                }

                _singleContinuation = null;
                _singleState = null;
                _secondaryContinuations = null;
                
                return true;
            }
        }

        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowTokenMismatch() => 
            throw new InvalidOperationException("Token mismatch");

        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowInvalidOperation(string message) => 
            throw new InvalidOperationException(message);
        
        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowOperationCancel(string message) => 
            throw new OperationCanceledException(message);
    }

    // 基础任务源 (泛型)
    public sealed class LuminTaskCompletionSource<T> : ILuminTaskSource<T>, IPromise<T>
    {
        private readonly object _lock = new object();
        private LuminTaskStatus _status = LuminTaskStatus.Pending;
        private ExceptionDispatchInfo _exception;
        private T _result;
        private Action<object> _singleContinuation;
        private object _singleState;
        private List<(Action<object>, object)> _secondaryContinuations;
        private short _token;

        public LuminTaskCompletionSource()
        {
            _token = (short)Environment.TickCount;
        }

        public LuminTask<T> Task
        {
            [DebuggerHidden]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new LuminTask<T>(this, _token);
        }

        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TrySetResult(T result)
        {
            lock (_lock)
            {
                if (_status != LuminTaskStatus.Pending) return false;
                
                _status = LuminTaskStatus.Succeeded;
                _result = result;
                
                SignalCompletion();
                return true;
            }
        }

        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TrySetCanceled(CancellationToken cancellationToken = default)
        {
            lock (_lock)
            {
                if (_status != LuminTaskStatus.Pending) return false;
                
                _status = LuminTaskStatus.Canceled;
                _exception = ExceptionDispatchInfo.Capture(new OperationCanceledException(cancellationToken));
                
                SignalCompletion();
                return true;
            }
        }

        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TrySetException(Exception exception)
        {
            lock (_lock)
            {
                if (_status != LuminTaskStatus.Pending) return false;
                
                if (exception is OperationCanceledException oce)
                {
                    return TrySetCanceled(oce.CancellationToken);
                }
                
                _status = LuminTaskStatus.Faulted;
                _exception = ExceptionDispatchInfo.Capture(exception);
                
                SignalCompletion();
                return true;
            }
        }

        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void ILuminTaskSource.GetResult(short token)
        {
            GetResult(token);
        }
        
        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T GetResult(short token)
        {
            if (token != _token) ThrowTokenMismatch();
            
            switch (_status)
            {
                case LuminTaskStatus.Succeeded:
                    return _result;
                case LuminTaskStatus.Faulted:
                    _exception?.Throw();
                    break;
                case LuminTaskStatus.Canceled:
                    ThrowOperationCancel("Task canceled");
                    break;
                default:
                    ThrowInvalidOperation("Task not completed");
                    break;
            }
            return default!;
        }

        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public LuminTaskStatus GetStatus(short token)
        {
            return token == _token ? _status : LuminTaskStatus.Faulted;
        }

        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public LuminTaskStatus UnsafeGetStatus() => _status;

        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void OnCompleted(Action<object> continuation, object state, short token)
        {
            lock (_lock)
            {
                if (_status != LuminTaskStatus.Pending)
                {
                    continuation(state);
                    return;
                }

                if (_singleContinuation == null)
                {
                    _singleContinuation = continuation;
                    _singleState = state;
                }
                else
                {
                    _secondaryContinuations ??= new List<(Action<object>, object)>();
                    _secondaryContinuations.Add((continuation, state));
                }
            }
        }

        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SignalCompletion()
        {
            if (_singleContinuation != null)
            {
                try
                {
                    _singleContinuation(_singleState);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Continuation exception: {ex}");
                }
            }

            if (_secondaryContinuations != null)
            {
                foreach (var (cont, state) in _secondaryContinuations)
                {
                    try
                    {
                        cont(state);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Continuation exception: {ex}");
                    }
                }
            }

            _singleContinuation = null;
            _singleState = null;
            _secondaryContinuations = null;
        }

        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowTokenMismatch() => 
            throw new InvalidOperationException("Token mismatch");

        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowInvalidOperation(string message) => 
            throw new InvalidOperationException(message);
        
        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowOperationCancel(string message) => 
            throw new OperationCanceledException(message);
    }

    // 对象池实现
    internal class TaskPool<T> where T : class, ITaskPoolNode<T>
    {
        private readonly Func<T> _factory;
        private readonly Stack<T> _stack = new Stack<T>();
        private int _size;

        public TaskPool(Func<T> factory)
        {
            _factory = factory;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryPop(out T result)
        {
            lock (_stack)
            {
                if (_stack.Count > 0)
                {
                    _size--;
                    result = _stack.Pop();
                    return true;
                }
            }
            
            result = _factory();
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryPush(T item)
        {
            lock (_stack)
            {
                if (_size < int.MaxValue)
                {
                    _size++;
                    _stack.Push(item);
                    return true;
                }
            }
            return false;
        }

        public int Size => _size;
    }

    internal interface ITaskPoolNode<T>
    {
        ref T NextNode { get; }
    }
}