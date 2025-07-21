using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Threading;
using Lumin.Threading.Interface;
using Lumin.Threading.Tasks;
using Lumin.Threading.Utility;

namespace Lumin.Threading.Source;

internal sealed class LuminTaskSource : IDisposable, ILuminTaskSource
#if NET8_0_OR_GREATER
    , IPooledObjectPolicy<LuminTaskSource>
#endif
{
#if NET8_0_OR_GREATER
    private static readonly ObjectPool<LuminTaskSource> Pool = new();
#else
    private static readonly ObjectPool<LuminTaskSource> Pool = new(new LuminTaskPoolPolicy());
#endif

    private Action<object>? _continuation;
    private object? _state;
    private ExecutionContext? _capturedContext;
    private LuminTaskStatus _status = LuminTaskStatus.Pending;
    private short _token;
    private ExceptionDispatchInfo? _exception;
    private bool _continueOnCapturedContext = true;

    public short Token => _token;

    [DebuggerHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public LuminTaskStatus UnsafeGetStatus() => _status;

    [DebuggerHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public LuminTaskStatus GetStatus(short token)
    {
        if (token != _token) return LuminTaskStatus.Faulted;
        return _status;
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
            case LuminTaskStatus.Pending:
            default:
                ThrowInvalidOperation("Task not completed");
                break;
        }
    }

    [DebuggerHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void OnCompleted(Action<object> continuation, object state, short token)
    {
        if (token != _token) ThrowTokenMismatch();

        if (_status != LuminTaskStatus.Pending)
        {
            ExecuteContinuation(continuation, state);
            return;
        }
        
        _continuation = continuation;
        _state = state;

        if (_continueOnCapturedContext)
        {
            _capturedContext = ExecutionContext.Capture();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TrySetResult()
    {
        if (_status != LuminTaskStatus.Pending) return false;
        _status = LuminTaskStatus.Succeeded;
        ExecuteContinuation();
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TrySetException(Exception exception)
    {
        if (_status != LuminTaskStatus.Pending) return false;
        _status = LuminTaskStatus.Faulted;
        _exception = ExceptionDispatchInfo.Capture(exception);
        ExecuteContinuation();
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TrySetCanceled()
    {
        if (_status != LuminTaskStatus.Pending) return false;
        _status = LuminTaskStatus.Canceled;
        ExecuteContinuation();
        return true;
    }

    [DebuggerHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ExecuteContinuation()
    {
        if (_continuation != null)
        {
            ExecuteContinuation(_continuation, _state);
            _continuation = null;
            _state = null;
        }
    }

    [DebuggerHidden]
    [MethodImpl(MethodImplOptions.NoInlining)]
    private void ExecuteContinuation(Action<object> continuation, object? state)
    {
        if (_capturedContext != null)
        {
            ExecutionContext.Run(_capturedContext, static s =>
            {
                var (cont, st) = (ValueTuple<Action<object>, object?>)s!;
                cont(st!);
            }, (continuation, state));
            
        }
        else
        {
            continuation(state!);
        }
    }

    [DebuggerHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Reset()
    {
        _token++;
        _status = LuminTaskStatus.Pending;
        _continuation = null;
        _state = null;
        _capturedContext = null;
        _exception = null;
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static LuminTaskSource Rent(bool continueOnCapturedContext = true)
    {
        var src = Pool.Rent();
        src._continueOnCapturedContext = continueOnCapturedContext;
        return src;
    }

#if NET8_0_OR_GREATER
    [DebuggerHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static LuminTaskSource IPooledObjectPolicy<LuminTaskSource>.Create() => new();

    [DebuggerHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static bool IPooledObjectPolicy<LuminTaskSource>.Return(LuminTaskSource obj) => true;
#else
    private sealed class LuminTaskPoolPolicy : IPooledObjectPolicy<LuminTaskSource>
    {
        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public LuminTaskSource Create() => new();

        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Return(LuminTaskSource obj)
        {
            return true;
        }
    }
#endif

    [DebuggerHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Return(LuminTaskSource source)
    {
        source.Dispose();
        Pool.Return(source);
    }

    [DebuggerHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Dispose()
    {
        Reset();
    }
}

internal sealed class LuminTaskSource<T> : IDisposable, ILuminTaskSource<T>
#if NET8_0_OR_GREATER
    , IPooledObjectPolicy<LuminTaskSource<T>>
#endif
{
#if NET8_0_OR_GREATER
    private static readonly ObjectPool<LuminTaskSource<T>> Pool = new();
#else
    private static readonly ObjectPool<LuminTaskSource<T>> Pool = new(new LuminTaskPoolPolicy());
#endif

    private Action<object>? _continuation;
    private object? _state;
    private ExecutionContext? _capturedContext;
    private LuminTaskStatus _status = LuminTaskStatus.Pending;
    private short _token;
    private ExceptionDispatchInfo? _exception;
    private bool _continueOnCapturedContext = true;
    private T? _result;
    
    public short Token => _token;

    [DebuggerHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public LuminTaskStatus UnsafeGetStatus() => _status;

    [DebuggerHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public LuminTaskStatus GetStatus(short token)
    {
        if (token != _token) return LuminTaskStatus.Faulted;
        return _status;
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
                return _result!;
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
        throw new InvalidOperationException("Unreachable code");
    }

    [DebuggerHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void OnCompleted(Action<object> continuation, object state, short token)
    {
        if (token != _token) ThrowTokenMismatch();

        if (_status != LuminTaskStatus.Pending)
        {
            ExecuteContinuation(continuation, state);
            return;
        }

        _continuation = continuation;
        _state = state;

        if (_continueOnCapturedContext)
        {
            _capturedContext = ExecutionContext.Capture();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TrySetResult(T result)
    {
        if (_status != LuminTaskStatus.Pending) return false;
        _result = result;
        _status = LuminTaskStatus.Succeeded;
        ExecuteContinuation();
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TrySetException(Exception exception)
    {
        if (_status != LuminTaskStatus.Pending) return false;
        _status = LuminTaskStatus.Faulted;
        _exception = ExceptionDispatchInfo.Capture(exception);
        ExecuteContinuation();
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TrySetCanceled()
    {
        if (_status != LuminTaskStatus.Pending) return false;
        _status = LuminTaskStatus.Canceled;
        ExecuteContinuation();
        return true;
    }

    [DebuggerHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ExecuteContinuation()
    {
        if (_continuation != null)
        {
            ExecuteContinuation(_continuation, _state);
            _continuation = null;
            _state = null;
        }
    }

    [DebuggerHidden]
    [MethodImpl(MethodImplOptions.NoInlining)]
    private void ExecuteContinuation(Action<object> continuation, object? state)
    {
        if (_capturedContext != null)
        {
            ExecutionContext.Run(_capturedContext, static s =>
            {
                var (cont, st) = (ValueTuple<Action<object>, object?>)s!;
                cont(st!);
            }, (continuation, state));
        }
        else
        {
            continuation(state!);
        }
    }

    [DebuggerHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Reset()
    {
        _token++;
        _status = LuminTaskStatus.Pending;
        _continuation = null;
        _state = null;
        _capturedContext = null;
        _exception = null;
        _result = default;
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static LuminTask<T> Create(bool continueOnCapturedContext = true)
    {
        var source = Pool.Rent();
        source._continueOnCapturedContext = continueOnCapturedContext;
        return new LuminTask<T>(source, source._token);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static LuminTaskSource<T> Rent(bool continueOnCapturedContext = true)
    {
        var src = Pool.Rent();
        src._continueOnCapturedContext = continueOnCapturedContext;
        return src;
    }

#if NET8_0_OR_GREATER
    [DebuggerHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static LuminTaskSource<T> IPooledObjectPolicy<LuminTaskSource<T>>.Create() => new();

    [DebuggerHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static bool IPooledObjectPolicy<LuminTaskSource<T>>.Return(LuminTaskSource<T> obj) => true;
#else
    private sealed class LuminTaskPoolPolicy : IPooledObjectPolicy<LuminTaskSource<T>>
    {
        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public LuminTaskSource<T> Create() => new();

        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Return(LuminTaskSource<T> obj)
        {
            obj.Dispose();
            return true;
        }
    }
#endif

    [DebuggerHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Return(LuminTaskSource<T> source)
    {
        source.Dispose();
        Pool.Return(source);
    }

    [DebuggerHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Dispose()
    {
        Reset();
    }
}