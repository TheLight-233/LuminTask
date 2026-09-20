using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using LuminThread.Interface;
using LuminThread.TaskSource;
using LuminThread.Utility;

namespace LuminThread.AsyncEx;

/// <summary>异步响应式属性。值类型句柄，共享内部状态。</summary>
public readonly struct AsyncReactiveProperty<T> : IDisposable
{
    internal readonly ReactivePropertyState<T>? State;

    public AsyncReactiveProperty(T initialValue, IEqualityComparer<T>? equalityComparer = null)
    {
        State = new ReactivePropertyState<T>(initialValue, equalityComparer);
    }

    internal AsyncReactiveProperty(ReactivePropertyState<T> state)
    {
        State = state;
    }

    public T Value
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => GetState().Value;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set => GetState().Value = value;
    }

    public event Action<T>? OnValueChanged
    {
        add => GetState().OnValueChanged += value;
        remove => GetState().OnValueChanged -= value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IDisposable Subscribe(Action<T> onValueChanged) => GetState().Subscribe(onValueChanged);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public LuminTask<T> WaitAsync(CancellationToken cancellationToken = default)
        => GetState().WaitAsync(cancellationToken);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ILuminTaskAsyncEnumerable<T> WithoutCurrent()
        => new AsyncReactivePropertyEnumerable<T>(GetState(), emitCurrent: false);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ILuminTaskAsyncEnumerable<T> ToAsyncEnumerable()
        => new AsyncReactivePropertyEnumerable<T>(GetState(), emitCurrent: true);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadonlyAsyncReactiveProperty<T> ToReadOnly() => new(GetState());

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Dispose() => GetState().Dispose();

    private ReactivePropertyState<T> GetState()
        => State ?? throw new InvalidOperationException("AsyncReactiveProperty<T> 未初始化（default 值）或已释放");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator T(AsyncReactiveProperty<T> property) => property.GetState().Value;
}

/// <summary>只读异步响应式属性。值类型句柄，共享内部状态。</summary>
public readonly struct ReadonlyAsyncReactiveProperty<T> : IDisposable
{
    private readonly ReactivePropertyState<T>? _state;
    private readonly bool _ownsSource;

    public ReadonlyAsyncReactiveProperty(T initialValue, IEqualityComparer<T>? equalityComparer = null)
    {
        _state = new ReactivePropertyState<T>(initialValue, equalityComparer);
        _ownsSource = true;
    }

    public ReadonlyAsyncReactiveProperty(AsyncReactiveProperty<T> source)
    {
        _state = source.State ?? throw new InvalidOperationException("AsyncReactiveProperty<T> 未初始化（default 值）或已释放");
        _ownsSource = false;
    }

    internal ReadonlyAsyncReactiveProperty(ReactivePropertyState<T> source)
    {
        _state = source;
        _ownsSource = false;
    }

    public T Value => GetState().Value;

    public event Action<T>? OnValueChanged
    {
        add => GetState().OnValueChanged += value;
        remove => GetState().OnValueChanged -= value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IDisposable Subscribe(Action<T> onValueChanged) => GetState().Subscribe(onValueChanged);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public LuminTask<T> WaitAsync(CancellationToken cancellationToken = default)
        => GetState().WaitAsync(cancellationToken);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ILuminTaskAsyncEnumerable<T> WithoutCurrent() => GetState().WithoutCurrent();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ILuminTaskAsyncEnumerable<T> ToAsyncEnumerable() => GetState().ToAsyncEnumerable();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Dispose()
    {
        if (_ownsSource) GetState().Dispose();
    }

    private ReactivePropertyState<T> GetState()
        => _state ?? throw new InvalidOperationException("ReadonlyAsyncReactiveProperty<T> 未初始化（default 值）或已释放");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator T(ReadonlyAsyncReactiveProperty<T> property) => property.Value;
}

internal sealed unsafe class ReactivePropertyState<T> : IDisposable
{
    private T _value;
    private readonly IEqualityComparer<T> _equalityComparer;
    private readonly object _lock = new();
    private AsyncReactivePropertyAwaiter<T>? _awaiterHead;
    internal volatile bool IsDisposed;

    public event Action<T>? OnValueChanged;

    public ReactivePropertyState(T initialValue, IEqualityComparer<T>? equalityComparer)
    {
        _value = initialValue;
        _equalityComparer = equalityComparer ?? EqualityComparer<T>.Default;
    }

    public T Value
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _value;
        set
        {
            if (_equalityComparer.Equals(_value, value)) return;
            _value = value;
            RaiseAll(value);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IDisposable Subscribe(Action<T> onValueChanged)
    {
        OnValueChanged += onValueChanged;
        return new Subscription(this, onValueChanged);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ILuminTaskAsyncEnumerable<T> WithoutCurrent()
        => new AsyncReactivePropertyEnumerable<T>(this, emitCurrent: false);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ILuminTaskAsyncEnumerable<T> ToAsyncEnumerable()
        => new AsyncReactivePropertyEnumerable<T>(this, emitCurrent: true);

    private void RaiseAll(T value)
    {
        OnValueChanged?.Invoke(value);

        AsyncReactivePropertyAwaiter<T>? head;
        lock (_lock)
        {
            head = _awaiterHead;
            _awaiterHead = null;
        }

        var node = head;
        while (node != null)
        {
            var next = node.Next;
            node.Next = null;
            node.TrySetResult(value);
            node = next;
        }
    }

    private sealed class Subscription : IDisposable
    {
        private ReactivePropertyState<T>? _parent;
        private Action<T> _handler;

        public Subscription(ReactivePropertyState<T> parent, Action<T> handler)
        {
            _parent = parent;
            _handler = handler;
        }

        public void Dispose()
        {
            if (_parent is null) return;
            _parent.OnValueChanged -= _handler;
            _parent = null;
        }
    }

    public LuminTask<T> WaitAsync(CancellationToken cancellationToken = default)
    {
        if (IsDisposed) return LuminTask.FromCanceled<T>(cancellationToken);
        if (cancellationToken.IsCancellationRequested) return LuminTask.FromCanceled<T>(cancellationToken);

        var core = LuminTaskSourceCore<T>.Create();
        var awaiter = new AsyncReactivePropertyAwaiter<T>(core);
        var task = new LuminTask<T>(LuminTaskSourceCore<T>.MethodTablePtr, core, core->Id);

        lock (_lock)
        {
            if (IsDisposed)
            {
                LuminTaskSourceCore<T>.TrySetCanceled(core);
                return task;
            }
            awaiter.Next = _awaiterHead;
            _awaiterHead = awaiter;
        }

        if (cancellationToken.CanBeCanceled)
            awaiter.RegisterCancellation(cancellationToken, this);

        return task;
    }

    internal void TryRemoveAwaiter(AsyncReactivePropertyAwaiter<T> awaiter)
    {
        lock (_lock)
        {
            if (_awaiterHead == awaiter)
            {
                _awaiterHead = awaiter.Next;
                return;
            }
            var prev = _awaiterHead;
            while (prev?.Next != null)
            {
                if (prev.Next == awaiter)
                {
                    prev.Next = awaiter.Next;
                    return;
                }
                prev = prev.Next;
            }
        }
    }

    public void Dispose()
    {
        if (IsDisposed) return;
        IsDisposed = true;

        AsyncReactivePropertyAwaiter<T>? head;
        lock (_lock)
        {
            head = _awaiterHead;
            _awaiterHead = null;
        }

        var node = head;
        while (node != null)
        {
            var next = node.Next;
            node.Next = null;
            node.TrySetCanceled();
            node = next;
        }
    }
}

internal sealed unsafe class AsyncReactivePropertyAwaiter<T>
{
    public AsyncReactivePropertyAwaiter<T>? Next;
    private IntPtr _corePtr;
    private CancellationTokenRegistration _registration;

    public AsyncReactivePropertyAwaiter(LuminTaskSourceCore<T>* core)
    {
        _corePtr = (IntPtr)core;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void TrySetResult(T value)
    {
        var ptr = Interlocked.Exchange(ref _corePtr, IntPtr.Zero);
        if (ptr == IntPtr.Zero) return;

        var core = (LuminTaskSourceCore<T>*)ptr;
        LuminTaskSourceCore<T>.TrySetResult(core, value);
        LuminTaskSourceCore<T>.Dispose(core);
        _registration.Release();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void TrySetCanceled()
    {
        var ptr = Interlocked.Exchange(ref _corePtr, IntPtr.Zero);
        if (ptr == IntPtr.Zero) return;

        var core = (LuminTaskSourceCore<T>*)ptr;
        LuminTaskSourceCore<T>.TrySetCanceled(core);
        LuminTaskSourceCore<T>.Dispose(core);
        _registration.Release();
    }

    public void RegisterCancellation(CancellationToken cancellationToken, ReactivePropertyState<T> parent)
    {
        var state = StateTuple.Create(this, parent);
        _registration = cancellationToken.Register(
            static state =>
            {
                var tuple = (StateTuple<(AsyncReactivePropertyAwaiter<T>, ReactivePropertyState<T>)>)state!;
                tuple.Item.Item2.TryRemoveAwaiter(tuple.Item.Item1);
                tuple.Item.Item1.TrySetCanceled();
                tuple.Dispose();
            },
            state);
    }
}

internal sealed class AsyncReactivePropertyEnumerable<T> : ILuminTaskAsyncEnumerable<T>
{
    private readonly ReactivePropertyState<T> _state;
    private readonly bool _emitCurrent;

    public AsyncReactivePropertyEnumerable(ReactivePropertyState<T> state, bool emitCurrent)
    {
        _state = state;
        _emitCurrent = emitCurrent;
    }

    public ILuminTaskAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        => new Enumerator(_state, _emitCurrent, cancellationToken);

    private sealed class Enumerator : ILuminTaskAsyncEnumerator<T>
    {
        private readonly ReactivePropertyState<T> _state;
        private readonly CancellationToken _ct;
        private bool _firstMove;
        private readonly bool _emitCurrent;
        private T _current;

        public T Current => _current;

        public Enumerator(ReactivePropertyState<T> state, bool emitCurrent, CancellationToken ct)
        {
            _state = state;
            _emitCurrent = emitCurrent;
            _firstMove = true;
            _current = default!;
            _ct = ct;
        }

        public LuminTask<bool> MoveNextAsync()
        {
            if (_ct.IsCancellationRequested || _state.IsDisposed)
                return LuminTask.FromResult(false);

            if (_firstMove && _emitCurrent)
            {
                _firstMove = false;
                _current = _state.Value;
                return LuminTask.FromResult(true);
            }
            _firstMove = false;

            return Core();
        }

        private async LuminTask<bool> Core()
        {
            try
            {
                _current = await _state.WaitAsync(_ct);
                return true;
            }
            catch (OperationCanceledException)
            {
                return false;
            }
        }

        public LuminTask DisposeAsync() => LuminTask.CompletedTask();
    }
}
