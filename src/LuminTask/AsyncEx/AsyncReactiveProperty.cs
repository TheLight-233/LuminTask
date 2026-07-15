using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using LuminThread.Interface;
using LuminThread.TaskSource;

namespace LuminThread.AsyncEx;

public sealed unsafe class AsyncReactiveProperty<T> : IDisposable
{
    private T _value;
    private readonly IEqualityComparer<T> _equalityComparer;
    private readonly object _lock = new();
    private AsyncReactivePropertyAwaiter<T>? _awaiterHead;
    internal volatile bool IsDisposed;

    public event Action<T>? OnValueChanged;

    public AsyncReactiveProperty(T initialValue, IEqualityComparer<T>? equalityComparer = null)
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

    public IDisposable Subscribe(Action<T> onValueChanged)
    {
        OnValueChanged += onValueChanged;
        return new Subscription(this, onValueChanged);
    }

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
        private AsyncReactiveProperty<T>? _parent;
        private Action<T> _handler;

        public Subscription(AsyncReactiveProperty<T> parent, Action<T> handler)
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

        lock (_lock)
        {
            if (IsDisposed)
            {
                LuminTaskSourceCore<T>.TrySetCanceled(core);
                LuminTaskSourceCore<T>.Dispose(core);
                return LuminTask.FromCanceled<T>(cancellationToken);
            }
            awaiter.Next = _awaiterHead;
            _awaiterHead = awaiter;
        }

        if (cancellationToken.CanBeCanceled)
            awaiter.RegisterCancellation(cancellationToken, this);

        return new LuminTask<T>(LuminTaskSourceCore<T>.MethodTablePtr, core, core->Id);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ILuminTaskAsyncEnumerable<T> WithoutCurrent()
        => new AsyncReactivePropertyEnumerable<T>(this, emitCurrent: false);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ILuminTaskAsyncEnumerable<T> ToAsyncEnumerable()
        => new AsyncReactivePropertyEnumerable<T>(this, emitCurrent: true);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadonlyAsyncReactiveProperty<T> ToReadOnly() => new(this);

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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator T(AsyncReactiveProperty<T> property) => property._value;
}

public sealed class ReadonlyAsyncReactiveProperty<T> : IDisposable
{
    private readonly AsyncReactiveProperty<T> _source;
    private readonly bool _ownsSource;

    public ReadonlyAsyncReactiveProperty(T initialValue, IEqualityComparer<T>? equalityComparer = null)
    {
        _source = new AsyncReactiveProperty<T>(initialValue, equalityComparer);
        _ownsSource = true;
    }

    public ReadonlyAsyncReactiveProperty(AsyncReactiveProperty<T> source)
    {
        _source = source ?? throw new ArgumentNullException(nameof(source));
        _ownsSource = false;
    }

    public T Value => _source.Value;

    public event Action<T>? OnValueChanged
    {
        add    => _source.OnValueChanged += value;
        remove => _source.OnValueChanged -= value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IDisposable Subscribe(Action<T> onValueChanged) => _source.Subscribe(onValueChanged);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public LuminTask<T> WaitAsync(CancellationToken cancellationToken = default)
        => _source.WaitAsync(cancellationToken);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ILuminTaskAsyncEnumerable<T> WithoutCurrent() => _source.WithoutCurrent();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ILuminTaskAsyncEnumerable<T> ToAsyncEnumerable() => _source.ToAsyncEnumerable();

    public void Dispose()
    {
        if (_ownsSource) _source.Dispose();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator T(ReadonlyAsyncReactiveProperty<T> property) => property.Value;
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
        _registration.Dispose();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void TrySetCanceled()
    {
        var ptr = Interlocked.Exchange(ref _corePtr, IntPtr.Zero);
        if (ptr == IntPtr.Zero) return;

        var core = (LuminTaskSourceCore<T>*)ptr;
        LuminTaskSourceCore<T>.TrySetCanceled(core);
        LuminTaskSourceCore<T>.Dispose(core);
    }

    public void RegisterCancellation(CancellationToken cancellationToken, AsyncReactiveProperty<T> parent)
    {
        _registration = cancellationToken.Register(
            static state =>
            {
                var (awaiter, p) = ((AsyncReactivePropertyAwaiter<T>, AsyncReactiveProperty<T>))state!;
                p.TryRemoveAwaiter(awaiter);
                awaiter.TrySetCanceled();
            },
            (this, parent));
    }
}

internal sealed class AsyncReactivePropertyEnumerable<T> : ILuminTaskAsyncEnumerable<T>
{
    private readonly AsyncReactiveProperty<T> _property;
    private readonly bool _emitCurrent;

    public AsyncReactivePropertyEnumerable(AsyncReactiveProperty<T> property, bool emitCurrent)
    {
        _property = property;
        _emitCurrent = emitCurrent;
    }

    public ILuminTaskAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        => new Enumerator(_property, _emitCurrent, cancellationToken);

    private sealed class Enumerator : ILuminTaskAsyncEnumerator<T>
    {
        private readonly AsyncReactiveProperty<T> _property;
        private readonly CancellationToken _ct;
        private bool _firstMove;
        private readonly bool _emitCurrent;
        private T _current;

        public T Current => _current;

        public Enumerator(AsyncReactiveProperty<T> property, bool emitCurrent, CancellationToken ct)
        {
            _property = property;
            _emitCurrent = emitCurrent;
            _firstMove = true;
            _current = default!;
            _ct = ct;
        }

        public LuminTask<bool> MoveNextAsync()
        {
            if (_ct.IsCancellationRequested || _property.IsDisposed)
                return LuminTask.FromResult(false);

            if (_firstMove && _emitCurrent)
            {
                _firstMove = false;
                _current = _property.Value;
                return LuminTask.FromResult(true);
            }
            _firstMove = false;

            return Core();
        }

        private async LuminTask<bool> Core()
        {
            try
            {
                _current = await _property.WaitAsync(_ct);
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