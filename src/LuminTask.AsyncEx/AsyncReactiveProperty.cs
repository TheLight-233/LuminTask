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

internal sealed class ReactivePropertyState<T> : IDisposable
{
    private T _value;
    private readonly IEqualityComparer<T> _equalityComparer;
    // The oldest, noncancelable waiter only needs its global source handle.
    private ulong _firstWaiter;
    private DefaultAsyncWaitQueue<T>? _waiters;
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

        ulong first;
        WaitNode? others;
        lock (this)
        {
            first = _firstWaiter;
            _firstWaiter = 0;
            others = _waiters?.DetachAllNodes();
        }

        // New subscriptions made by continuations belong to the next change.
        DefaultAsyncWaitQueue<T>.CompleteBatch(others, value);
        if (first != 0) LuminTaskResultBridge.TrySetResult(first, in value);
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

        lock (this)
        {
            if (IsDisposed) return LuminTask.FromCanceled<T>(cancellationToken);
            if (!cancellationToken.CanBeCanceled && _firstWaiter == 0 && (_waiters == null || _waiters.IsEmpty))
                return LuminTaskSource.Create<T>(out _firstWaiter);
            return (_waiters ??= new DefaultAsyncWaitQueue<T>(newestFirst: true)).EnqueueWithCancellation(cancellationToken);
        }
    }

    public void Dispose()
    {
        ulong first;
        WaitNode? others;
        lock (this)
        {
            if (IsDisposed) return;
            IsDisposed = true;
            first = _firstWaiter;
            _firstWaiter = 0;
            others = _waiters?.DetachAllNodes();
            _waiters = null;
        }
        AsyncWaitQueueCore.CancelBatch(others, default);
        if (first != 0) LuminTaskSource.TrySetCanceled(first);
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
            var change = _state.WaitAsync(_ct);
            if (await new LuminTaskSafeExtensions.CancellationCheck(change)) return false;
            _current = change.GetResult();
            return true;
        }

        public LuminTask DisposeAsync() => LuminTask.CompletedTask();
    }
}
