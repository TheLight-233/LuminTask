using System;
using System.Threading;
using Lumin.Threading.Interface;
using Lumin.Threading.Source;
using Lumin.Threading.Tasks.Utility;
using Lumin.Threading.Utility;

namespace Lumin.Threading.Tasks.Reactive;

public interface IReadOnlyAsyncReactiveProperty<T> : ILuminTaskAsyncEnumerable<T>
{
    T Value { get; }
    ILuminTaskAsyncEnumerable<T> WithoutCurrent();
    LuminTask<T> WaitAsync(CancellationToken cancellationToken = default);
}

public interface IAsyncReactiveProperty<T> : IReadOnlyAsyncReactiveProperty<T>
{
    new T Value { get; set; }
}

[Serializable]
public class AsyncReactiveProperty<T> : IAsyncReactiveProperty<T>, IDisposable
{
    TriggerEvent<T> triggerEvent;

#if UNITY_2018_3_OR_NEWER
    [UnityEngine.SerializeField]
#endif
    T latestValue;

    public T Value
    {
        get
        {
            return latestValue;
        }
        set
        {
            this.latestValue = value;
            triggerEvent.SetResult(value);
        }
    }

    public AsyncReactiveProperty(T value)
    {
        this.latestValue = value;
        this.triggerEvent = default;
    }

    public ILuminTaskAsyncEnumerable<T> WithoutCurrent()
    {
        return new WithoutCurrentEnumerable(this);
    }

    public ILuminTaskAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken)
    {
        return new Enumerator(this, cancellationToken, true);
    }

    public void Dispose()
    {
        triggerEvent.SetCompleted();
    }

    public static implicit operator T(AsyncReactiveProperty<T> value)
    {
        return value.Value;
    }

    public override string ToString()
    {
        if (isValueType) return latestValue.ToString();
        return latestValue?.ToString();
    }

    public LuminTask<T> WaitAsync(CancellationToken cancellationToken = default)
    {
        return new LuminTask<T>(WaitAsyncSource.Create(this, cancellationToken, out var token), token);
    }

    static bool isValueType;

    static AsyncReactiveProperty()
    {
        isValueType = typeof(T).IsValueType;
    }

    sealed class WaitAsyncSource : ILuminTaskSource<T>, ITriggerHandler<T>, Source.ITaskPoolNode<WaitAsyncSource>
    {
        static Action<object> cancellationCallback = CancellationCallback;

        static Source.TaskPool<WaitAsyncSource> pool;
        WaitAsyncSource nextNode;
        ref WaitAsyncSource Source.ITaskPoolNode<WaitAsyncSource>.NextNode => ref nextNode;

        static WaitAsyncSource()
        {
            TaskPool.RegisterSizeGetter(typeof(WaitAsyncSource), () => pool.Size);
        }

        AsyncReactiveProperty<T> parent;
        CancellationToken cancellationToken;
        CancellationTokenRegistration cancellationTokenRegistration;
        LuminTaskCompletionSourceCore<T> core;

        WaitAsyncSource()
        {
        }

        public static ILuminTaskSource<T> Create(AsyncReactiveProperty<T> parent, CancellationToken cancellationToken, out short token)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return AutoResetLuminTaskCompletionSource<T>.CreateFromCanceled(cancellationToken, out token);
            }

            if (!pool.TryPop(out var result))
            {
                result = new WaitAsyncSource();
            }

            result.parent = parent;
            result.cancellationToken = cancellationToken;

            if (cancellationToken.CanBeCanceled)
            {
                result.cancellationTokenRegistration = cancellationToken.RegisterWithoutCaptureExecutionContext(cancellationCallback, result);
            }

            result.parent.triggerEvent.Add(result);

            TaskTracker.TrackActiveTask(result, 3);

            token = result.core.Version;
            return result;
        }

        bool TryReturn()
        {
            TaskTracker.RemoveTracking(this);
            core.Reset();
            cancellationTokenRegistration.Dispose();
            cancellationTokenRegistration = default;
            parent.triggerEvent.Remove(this);
            parent = null;
            cancellationToken = default;
            return pool.TryPush(this);
        }

        static void CancellationCallback(object state)
        {
            var self = (WaitAsyncSource)state;
            self.OnCanceled(self.cancellationToken);
        }

        public T GetResult(short token)
        {
            try
            {
                return core.GetResult(token);
            }
            finally
            {
                TryReturn();
            }
        }

        void ILuminTaskSource.GetResult(short token)
        {
            GetResult(token);
        }

        public void OnCompleted(Action<object> continuation, object state, short token)
        {
            core.OnCompleted(continuation, state, token);
        }

        public LuminTaskStatus GetStatus(short token)
        {
            return core.GetStatus(token);
        }

        public LuminTaskStatus UnsafeGetStatus()
        {
            return core.UnsafeGetStatus();
        }

        // ITriggerHandler

        ITriggerHandler<T> ITriggerHandler<T>.Prev { get; set; }
        ITriggerHandler<T> ITriggerHandler<T>.Next { get; set; }

        public void OnCanceled(CancellationToken cancellationToken)
        {
            core.TrySetCanceled(cancellationToken);
        }

        public void OnCompleted()
        {
            // Complete as Cancel.
            core.TrySetCanceled(CancellationToken.None);
        }

        public void OnError(Exception ex)
        {
            core.TrySetException(ex);
        }

        public void OnNext(T value)
        {
            core.TrySetResult(value);
        }
    }

    sealed class WithoutCurrentEnumerable : ILuminTaskAsyncEnumerable<T>
    {
        readonly AsyncReactiveProperty<T> parent;

        public WithoutCurrentEnumerable(AsyncReactiveProperty<T> parent)
        {
            this.parent = parent;
        }

        public ILuminTaskAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        {
            return new Enumerator(parent, cancellationToken, false);
        }
    }

    sealed class Enumerator : MoveNextSource, ILuminTaskAsyncEnumerator<T>, ITriggerHandler<T>
    {
        static Action<object> cancellationCallback = CancellationCallback;

        readonly AsyncReactiveProperty<T> parent;
        readonly CancellationToken cancellationToken;
        readonly CancellationTokenRegistration cancellationTokenRegistration;
        T value;
        bool isDisposed;
        bool firstCall;

        public Enumerator(AsyncReactiveProperty<T> parent, CancellationToken cancellationToken, bool publishCurrentValue)
        {
            this.parent = parent;
            this.cancellationToken = cancellationToken;
            this.firstCall = publishCurrentValue;

            parent.triggerEvent.Add(this);
            TaskTracker.TrackActiveTask(this, 3);

            if (cancellationToken.CanBeCanceled)
            {
                cancellationTokenRegistration = cancellationToken.RegisterWithoutCaptureExecutionContext(cancellationCallback, this);
            }
        }

        public T Current => value;

        ITriggerHandler<T> ITriggerHandler<T>.Prev { get; set; }
        ITriggerHandler<T> ITriggerHandler<T>.Next { get; set; }

        public LuminTask<bool> MoveNextAsync()
        {
            // raise latest value on first call.
            if (firstCall)
            {
                firstCall = false;
                value = parent.Value;
                return CompletedTasks.True();
            }

            completionSource.Reset();
            return new LuminTask<bool>(this, completionSource.Version);
        }

        public LuminTask DisposeAsync()
        {
            if (!isDisposed)
            {
                isDisposed = true;
                TaskTracker.RemoveTracking(this);
                completionSource.TrySetCanceled(cancellationToken);
                parent.triggerEvent.Remove(this);
            }
            return default;
        }

        public void OnNext(T value)
        {
            this.value = value;
            completionSource.TrySetResult(true);
        }

        public void OnCanceled(CancellationToken cancellationToken)
        {
            DisposeAsync();
        }

        public void OnCompleted()
        {
            completionSource.TrySetResult(false);
        }

        public void OnError(Exception ex)
        {
            completionSource.TrySetException(ex);
        }

        static void CancellationCallback(object state)
        {
            var self = (Enumerator)state;
            self.DisposeAsync();
        }
    }
}

public class ReadOnlyAsyncReactiveProperty<T> : IReadOnlyAsyncReactiveProperty<T>, IDisposable
{
    TriggerEvent<T> triggerEvent;

    T latestValue;
    ILuminTaskAsyncEnumerator<T> enumerator;

    public T Value
    {
        get
        {
            return latestValue;
        }
    }

    public ReadOnlyAsyncReactiveProperty(T initialValue, ILuminTaskAsyncEnumerable<T> source, CancellationToken cancellationToken)
    {
        latestValue = initialValue;
        ConsumeEnumerator(source, cancellationToken);
    }

    public ReadOnlyAsyncReactiveProperty(ILuminTaskAsyncEnumerable<T> source, CancellationToken cancellationToken)
    {
        ConsumeEnumerator(source, cancellationToken);
    }

    async void ConsumeEnumerator(ILuminTaskAsyncEnumerable<T> source, CancellationToken cancellationToken)
    {
        enumerator = source.GetAsyncEnumerator(cancellationToken);
        try
        {
            while (await enumerator.MoveNextAsync())
            {
                var value = enumerator.Current;
                this.latestValue = value;
                triggerEvent.SetResult(value);
            }
        }
        finally
        {
            await enumerator.DisposeAsync();
            enumerator = null;
        }
    }

    public ILuminTaskAsyncEnumerable<T> WithoutCurrent()
    {
        return new WithoutCurrentEnumerable(this);
    }

    public ILuminTaskAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken)
    {
        return new Enumerator(this, cancellationToken, true);
    }

    public void Dispose()
    {
        if (enumerator != null)
        {
            enumerator.DisposeAsync();
        }

        triggerEvent.SetCompleted();
    }

    public static implicit operator T(ReadOnlyAsyncReactiveProperty<T> value)
    {
        return value.Value;
    }

    public override string ToString()
    {
        if (isValueType) return latestValue.ToString();
        return latestValue?.ToString();
    }

    public LuminTask<T> WaitAsync(CancellationToken cancellationToken = default)
    {
        return new LuminTask<T>(WaitAsyncSource.Create(this, cancellationToken, out var token), token);
    }

    static bool isValueType;

    static ReadOnlyAsyncReactiveProperty()
    {
        isValueType = typeof(T).IsValueType;
    }

    sealed class WaitAsyncSource : ILuminTaskSource<T>, ITriggerHandler<T>, Source.ITaskPoolNode<WaitAsyncSource>
    {
        static Action<object> cancellationCallback = CancellationCallback;

        static Source.TaskPool<WaitAsyncSource> pool;
        WaitAsyncSource nextNode;
        ref WaitAsyncSource Source.ITaskPoolNode<WaitAsyncSource>.NextNode => ref nextNode;

        static WaitAsyncSource()
        {
            TaskPool.RegisterSizeGetter(typeof(WaitAsyncSource), () => pool.Size);
        }

        ReadOnlyAsyncReactiveProperty<T> parent;
        CancellationToken cancellationToken;
        CancellationTokenRegistration cancellationTokenRegistration;
        LuminTaskCompletionSourceCore<T> core;

        WaitAsyncSource()
        {
        }

        public static ILuminTaskSource<T> Create(ReadOnlyAsyncReactiveProperty<T> parent, CancellationToken cancellationToken, out short token)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return AutoResetLuminTaskCompletionSource<T>.CreateFromCanceled(cancellationToken, out token);
            }

            if (!pool.TryPop(out var result))
            {
                result = new WaitAsyncSource();
            }

            result.parent = parent;
            result.cancellationToken = cancellationToken;

            if (cancellationToken.CanBeCanceled)
            {
                result.cancellationTokenRegistration = cancellationToken.RegisterWithoutCaptureExecutionContext(cancellationCallback, result);
            }

            result.parent.triggerEvent.Add(result);

            TaskTracker.TrackActiveTask(result, 3);

            token = result.core.Version;
            return result;
        }

        bool TryReturn()
        {
            TaskTracker.RemoveTracking(this);
            core.Reset();
            cancellationTokenRegistration.Dispose();
            cancellationTokenRegistration = default;
            parent.triggerEvent.Remove(this);
            parent = null;
            cancellationToken = default;
            return pool.TryPush(this);
        }

        static void CancellationCallback(object state)
        {
            var self = (WaitAsyncSource)state;
            self.OnCanceled(self.cancellationToken);
        }
        

        public T GetResult(short token)
        {
            try
            {
                return core.GetResult(token);
            }
            finally
            {
                TryReturn();
            }
        }

        void ILuminTaskSource.GetResult(short token)
        {
            GetResult(token);
        }

        public void OnCompleted(Action<object> continuation, object state, short token)
        {
            core.OnCompleted(continuation, state, token);
        }

        public LuminTaskStatus GetStatus(short token)
        {
            return core.GetStatus(token);
        }

        public LuminTaskStatus UnsafeGetStatus()
        {
            return core.UnsafeGetStatus();
        }

        // ITriggerHandler

        ITriggerHandler<T> ITriggerHandler<T>.Prev { get; set; }
        ITriggerHandler<T> ITriggerHandler<T>.Next { get; set; }

        public void OnCanceled(CancellationToken cancellationToken)
        {
            core.TrySetCanceled(cancellationToken);
        }

        public void OnCompleted()
        {
            // Complete as Cancel.
            core.TrySetCanceled(CancellationToken.None);
        }

        public void OnError(Exception ex)
        {
            core.TrySetException(ex);
        }

        public void OnNext(T value)
        {
            core.TrySetResult(value);
        }
    }

    sealed class WithoutCurrentEnumerable : ILuminTaskAsyncEnumerable<T>
    {
        readonly ReadOnlyAsyncReactiveProperty<T> parent;

        public WithoutCurrentEnumerable(ReadOnlyAsyncReactiveProperty<T> parent)
        {
            this.parent = parent;
        }

        public ILuminTaskAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        {
            return new Enumerator(parent, cancellationToken, false);
        }
    }

    sealed class Enumerator : MoveNextSource, ILuminTaskAsyncEnumerator<T>, ITriggerHandler<T>
    {
        static Action<object> cancellationCallback = CancellationCallback;

        readonly ReadOnlyAsyncReactiveProperty<T> parent;
        readonly CancellationToken cancellationToken;
        readonly CancellationTokenRegistration cancellationTokenRegistration;
        T value;
        bool isDisposed;
        bool firstCall;

        public Enumerator(ReadOnlyAsyncReactiveProperty<T> parent, CancellationToken cancellationToken, bool publishCurrentValue)
        {
            this.parent = parent;
            this.cancellationToken = cancellationToken;
            this.firstCall = publishCurrentValue;

            parent.triggerEvent.Add(this);
            TaskTracker.TrackActiveTask(this, 3);

            if (cancellationToken.CanBeCanceled)
            {
                cancellationTokenRegistration = cancellationToken.RegisterWithoutCaptureExecutionContext(cancellationCallback, this);
            }
        }

        public T Current => value;
        ITriggerHandler<T> ITriggerHandler<T>.Prev { get; set; }
        ITriggerHandler<T> ITriggerHandler<T>.Next { get; set; }

        public LuminTask<bool> MoveNextAsync()
        {
            // raise latest value on first call.
            if (firstCall)
            {
                firstCall = false;
                value = parent.Value;
                return CompletedTasks.True();
            }

            completionSource.Reset();
            return new LuminTask<bool>(this, completionSource.Version);
        }

        public LuminTask DisposeAsync()
        {
            if (!isDisposed)
            {
                isDisposed = true;
                TaskTracker.RemoveTracking(this);
                completionSource.TrySetCanceled(cancellationToken);
                parent.triggerEvent.Remove(this);
            }
            return default;
        }

        public void OnNext(T value)
        {
            this.value = value;
            completionSource.TrySetResult(true);
        }

        public void OnCanceled(CancellationToken cancellationToken)
        {
            DisposeAsync();
        }

        public void OnCompleted()
        {
            completionSource.TrySetResult(false);
        }

        public void OnError(Exception ex)
        {
            completionSource.TrySetException(ex);
        }

        static void CancellationCallback(object state)
        {
            var self = (Enumerator)state;
            self.DisposeAsync();
        }
    }
}

public static class StateExtensions
{
    public static ReadOnlyAsyncReactiveProperty<T> ToReadOnlyAsyncReactiveProperty<T>(this ILuminTaskAsyncEnumerable<T> source, CancellationToken cancellationToken)
    {
        return new ReadOnlyAsyncReactiveProperty<T>(source, cancellationToken);
    }

    public static ReadOnlyAsyncReactiveProperty<T> ToReadOnlyAsyncReactiveProperty<T>(this ILuminTaskAsyncEnumerable<T> source, T initialValue, CancellationToken cancellationToken)
    {
        return new ReadOnlyAsyncReactiveProperty<T>(initialValue, source, cancellationToken);
    }
}