// 优化后的 LuminTaskAwaiter.cs
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Lumin.Threading.Interface;
using Lumin.Threading.Source;

namespace Lumin.Threading.Core
{
    public readonly struct LuminTaskAwaiter : ICriticalNotifyCompletion
    {
        private readonly ILuminTaskSource? _source;
        private readonly short _token;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal LuminTaskAwaiter(ILuminTaskSource? source, short token)
        {
            _source = source;
            _token = token;
        }

        public bool IsCompleted
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _source?.GetStatus(_token).IsCompleted() ?? true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void GetResult()
        {
            _source?.GetResult(_token);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void OnCompleted(Action continuation)
        {
            if (_source == null)
            {
                continuation();
            }
            else
            {
                _source.OnCompleted(static s => ((Action)s)(), continuation, _token);
                
            }
        }
        
        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SourceOnCompleted(Action<object> continuation, object state)
        {
            if (_source == null)
            {
                continuation(state);
            }
            else
            {
                _source.OnCompleted(continuation, state, _token);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void UnsafeOnCompleted(Action continuation) => OnCompleted(continuation);
    }

    public readonly struct LuminTaskAwaiter<T> : ICriticalNotifyCompletion
    {
        private readonly ILuminTaskSource<T>? _source;
        private readonly short _token;
        private readonly T? _result;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal LuminTaskAwaiter(ILuminTaskSource<T>? source, short token, T? result = default)
        {
            _source = source;
            _token = token;
            _result = result;
        }

        public bool IsCompleted
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _source?.GetStatus(_token).IsCompleted() ?? true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T GetResult()
        {
            if (_source == null)
                return _result!;

            var result = _source.GetResult(_token);
            
            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void OnCompleted(Action continuation)
        {
            if (_source == null)
            {
                continuation();
            }
            else
            {
                _source.OnCompleted(static s => ((Action)s)(), continuation, _token);
            }
        }
        
        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SourceOnCompleted(Action<object> continuation, object state)
        {
            if (_source == null)
            {
                continuation(state);
            }
            else
            {
                _source.OnCompleted(continuation, state, _token);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void UnsafeOnCompleted(Action continuation) => OnCompleted(continuation);
    }
}
