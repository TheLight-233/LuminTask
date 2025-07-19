using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using Lumin.Threading.Core;
using Lumin.Threading.Interface;
using Lumin.Threading.Tasks.CompilerServices;
using Lumin.Threading.Tasks.Utility;

namespace Lumin.Threading.Tasks;

[AsyncMethodBuilder(typeof(AsyncLuminTaskMethodBuilder))]
[StructLayout(LayoutKind.Auto)]
public readonly ref partial struct LuminTask
{
    internal readonly ILuminTaskSource? _source;
    internal readonly short _token;
    
    public short CurrentId => _token;

    [DebuggerHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal LuminTask(ILuminTaskSource? source, short token)
    {
        _source = source;
        _token = token;
    }

    [DebuggerHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public LuminTaskAwaiter GetAwaiter() => new(_source, _token);
    
    public LuminTaskStatus Status
    {
        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _source?.GetStatus(_token) ?? LuminTaskStatus.Succeeded;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator System.Threading.Tasks.ValueTask(in LuminTask self)
    {
        if (self._source is null)
        {
            return default;
        }
        
        return new System.Threading.Tasks.ValueTask(self._source, self._token);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override string ToString()
    {
        if (_source is null) return "()";
        return "(" + _source.UnsafeGetStatus() + ")";
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public LuminTask<bool> SuppressCancellationThrow()
    {
        var status = Status;
        if (status == LuminTaskStatus.Succeeded) return CompletedTasks.False();
        if (status == LuminTaskStatus.Canceled) return CompletedTasks.True();
        return new LuminTask<bool>(new IsCanceledSource(_source!), _token);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public LuminTask Preserve()
    {
        if (_source is null)
        {
            return this;
        }
        else
        {
            return new LuminTask(new MemoizeSource(_source), _token);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public LuminTask<AsyncUnit> AsAsyncUnitLuminTask()
    {
        if (this._source is null) return CompletedTasks.AsyncUnit();

        var status = this._source.GetStatus(this._token);
        if (status.IsCompletedSuccessfully())
        {
            this._source.GetResult(this._token);
            return CompletedTasks.AsyncUnit();
        }
        else if (this._source is ILuminTaskSource<AsyncUnit> asyncUnitSource)
        {
            return new LuminTask<AsyncUnit>(asyncUnitSource, this._token);
        }

        return new LuminTask<AsyncUnit>(new AsyncUnitSource(this._source), this._token);
    }

    sealed class AsyncUnitSource : ILuminTaskSource<AsyncUnit>
    {
        readonly ILuminTaskSource _source;

        public AsyncUnitSource(ILuminTaskSource source)
        {
            this._source = source;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public AsyncUnit GetResult(short token)
        {
            _source.GetResult(token);
            return AsyncUnit.Default;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public LuminTaskStatus GetStatus(short token)
        {
            return _source.GetStatus(token);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void OnCompleted(Action<object> continuation, object state, short token)
        {
            _source.OnCompleted(continuation, state, token);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public LuminTaskStatus UnsafeGetStatus()
        {
            return _source.UnsafeGetStatus();
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void ILuminTaskSource.GetResult(short token)
        {
            GetResult(token);
        }
    }

    sealed class IsCanceledSource : ILuminTaskSource<bool>
    {
        readonly ILuminTaskSource _source;

        public IsCanceledSource(ILuminTaskSource source)
        {
            this._source = source;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool GetResult(short token)
        {
            if (_source.GetStatus(token) == LuminTaskStatus.Canceled)
            {
                return true;
            }

            _source.GetResult(token);
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void ILuminTaskSource.GetResult(short token)
        {
            GetResult(token);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public LuminTaskStatus GetStatus(short token)
        {
            return _source.GetStatus(token);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public LuminTaskStatus UnsafeGetStatus()
        {
            return _source.UnsafeGetStatus();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void OnCompleted(Action<object> continuation, object state, short token)
        {
            _source.OnCompleted(continuation, state, token);
        }
    }

    sealed class MemoizeSource : ILuminTaskSource
    {
        ILuminTaskSource? _source;
        ExceptionDispatchInfo? _exception;
        LuminTaskStatus _status;

        public MemoizeSource(ILuminTaskSource source)
        {
            this._source = source;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void GetResult(short token)
        {
            if (_source is null)
            {
                if (_exception is not null)
                {
                    _exception.Throw();
                }
            }
            else
            {
                try
                {
                    _source.GetResult(token);
                    _status = LuminTaskStatus.Succeeded;
                }
                catch (Exception ex)
                {
                    _exception = ExceptionDispatchInfo.Capture(ex);
                    if (ex is OperationCanceledException)
                    {
                        _status = LuminTaskStatus.Canceled;
                    }
                    else
                    {
                        _status = LuminTaskStatus.Faulted;
                    }
                    throw;
                }
                finally
                {
                    _source = null;
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public LuminTaskStatus GetStatus(short token)
        {
            if (_source is null)
            {
                return _status;
            }

            return _source.GetStatus(token);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void OnCompleted(Action<object> continuation, object state, short token)
        {
            if (_source is null)
            {
                continuation(state);
            }
            else
            {
                _source.OnCompleted(continuation, state, token);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public LuminTaskStatus UnsafeGetStatus()
        {
            if (_source is null)
            {
                return _status;
            }

            return _source.UnsafeGetStatus();
        }
    }
}

[AsyncMethodBuilder(typeof(AsyncLuminTaskMethodBuilder<>))]
[StructLayout(LayoutKind.Auto)]
public readonly ref partial struct LuminTask<T>
{
    internal readonly ILuminTaskSource<T>? _source;
    internal readonly short _token;
    internal readonly T? _result;
    
    public short CurrentId => _token;
    public T? Result => _result;

    [DebuggerHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal LuminTask(ILuminTaskSource<T>? source, short token)
    {
        _source = source;
        _token = token;
        _result = default;
    }

    [DebuggerHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal LuminTask(T result)
    {
        _source = null;
        _token = 0;
        _result = result;
    }

    [DebuggerHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public LuminTaskAwaiter<T> GetAwaiter() => new(_source, _token, _result);
    
    public LuminTaskStatus Status
    {
        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _source?.GetStatus(_token) ?? LuminTaskStatus.Succeeded;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public LuminTask<T> Preserve()
    {
        if (_source == null)
        {
            return this;
        }
        else
        {
            return new LuminTask<T>(new MemoizeSource(_source), _token);
        }
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public LuminTask AsUniTask()
    {
        if (this._source == null) return LuminTask.CompletedTask();

        var status = this._source.GetStatus(this._token);
        if (status.IsCompletedSuccessfully())
        {
            this._source.GetResult(this._token);
            return LuminTask.CompletedTask();
        }
        
        return new LuminTask(this._source, this._token);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator LuminTask(LuminTask<T> self)
    {
        return self.AsUniTask();
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator System.Threading.Tasks.ValueTask<T>(in LuminTask<T> self)
    {
        if (self._source == null)
        {
            return new System.Threading.Tasks.ValueTask<T>(self._result!);
        }
        
        return new System.Threading.Tasks.ValueTask<T>(self._source, self._token);

    }



    /// <summary>
    /// returns (bool IsCanceled, T Result) instead of throws OperationCanceledException.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public LuminTask<(bool IsCanceled, T Result)> SuppressCancellationThrow()
    {
        if (_source == null)
        {
            return new LuminTask<(bool IsCanceled, T Result)>((false, _result)!);
        }

        return new LuminTask<(bool, T)>(new IsCanceledSource(_source), _token);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override string? ToString()
    {
        return (_source is null) ? _result?.ToString()
            : "(" + _source.UnsafeGetStatus() + ")";
    }

    sealed class IsCanceledSource : ILuminTaskSource<(bool, T)>
    {
        readonly ILuminTaskSource<T> _source;

        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IsCanceledSource(ILuminTaskSource<T> source)
        {
            this._source = source;
        }

        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public (bool, T) GetResult(short token)
        {
            if (_source.GetStatus(token) == LuminTaskStatus.Canceled)
            {
                return (true, default)!;
            }

            var result = _source.GetResult(token);
            return (false, result);
        }

        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void ILuminTaskSource.GetResult(short token)
        {
            GetResult(token);
        }

        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public LuminTaskStatus GetStatus(short token)
        {
            return _source.GetStatus(token);
        }

        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public LuminTaskStatus UnsafeGetStatus()
        {
            return _source.UnsafeGetStatus();
        }

        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void OnCompleted(Action<object> continuation, object state, short token)
        {
            _source.OnCompleted(continuation, state, token);
        }
    }

    sealed class MemoizeSource : ILuminTaskSource<T>
    {
        ILuminTaskSource<T>? _source;
        T? _result;
        ExceptionDispatchInfo? _exception;
        LuminTaskStatus _status;

        public MemoizeSource(ILuminTaskSource<T> source)
        {
            this._source = source;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T GetResult(short token)
        {
            if (_source == null)
            {
                if (_exception != null)
                {
                    _exception.Throw();
                }
                return _result!;
            }
            else
            {
                try
                {
                    _result = _source.GetResult(token);
                    _status = LuminTaskStatus.Succeeded;
                    return _result;
                }
                catch (Exception ex)
                {
                    _exception = ExceptionDispatchInfo.Capture(ex);
                    if (ex is OperationCanceledException)
                    {
                        _status = LuminTaskStatus.Canceled;
                    }
                    else
                    {
                        _status = LuminTaskStatus.Faulted;
                    }
                    throw;
                }
                finally
                {
                    _source = null;
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void ILuminTaskSource.GetResult(short token)
        {
            GetResult(token);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public LuminTaskStatus GetStatus(short token)
        {
            if (_source == null)
            {
                return _status;
            }

            return _source.GetStatus(token);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void OnCompleted(Action<object> continuation, object state, short token)
        {
            if (_source == null)
            {
                continuation(state);
            }
            else
            {
                _source.OnCompleted(continuation, state, token);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public LuminTaskStatus UnsafeGetStatus()
        {
            if (_source == null)
            {
                return _status;
            }

            return _source.UnsafeGetStatus();
        }
    }
}