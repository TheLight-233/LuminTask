using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using LuminThread.Interface;
using LuminThread.TaskSource;

namespace LuminThread.Utility;

public static class LuminChannel
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static LuminChannel<T> CreateSingleConsumerUnbounded<T>()
    {
        return new SingleConsumerUnboundedLuminChannel<T>();
    }
}

public abstract class LuminChannel<TWrite, TRead>
{
    public LuminChannelReader<TRead> Reader { get; protected set; }
    public LuminChannelWriter<TWrite> Writer { get; protected set; }

    public static implicit operator LuminChannelReader<TRead>(LuminChannel<TWrite, TRead> channel) => channel.Reader;
    public static implicit operator LuminChannelWriter<TWrite>(LuminChannel<TWrite, TRead> channel) => channel.Writer;
}

public abstract class LuminChannel<T> : LuminChannel<T, T>
{
}

public abstract class LuminChannelReader<T>
{
    public abstract bool TryRead(out T item);
    public abstract LuminTask<bool> WaitToReadAsync(CancellationToken cancellationToken = default(CancellationToken));

    public abstract LuminTask Completion { get; }

    public LuminTask<T> ReadAsync(CancellationToken cancellationToken = default(CancellationToken))
    {
        if (this.TryRead(out var item))
        {
            return LuminTask<T>.FromResult(item);
        }

        return ReadAsyncCore(cancellationToken);
    }

    async LuminTask<T> ReadAsyncCore(CancellationToken cancellationToken = default(CancellationToken))
    {
        if (await WaitToReadAsync(cancellationToken))
        {
            if (TryRead(out var item))
            {
                return item;
            }
        }

        throw new LuminChannelClosedException();
    }

    public abstract ILuminTaskAsyncEnumerable<T> ReadAllAsync(CancellationToken cancellationToken = default(CancellationToken));
}

public abstract class LuminChannelWriter<T>
{
    public abstract bool TryWrite(T item);
    public abstract bool TryComplete(Exception error = null);

    public void Complete(Exception error = null)
    {
        if (!TryComplete(error))
        {
            throw new LuminChannelClosedException();
        }
    }
}

public sealed class LuminChannelClosedException : InvalidOperationException
{
    public LuminChannelClosedException() :
        base("Channel is already closed.")
    { }

    public LuminChannelClosedException(string message) : base(message) { }

    public LuminChannelClosedException(Exception innerException) :
        base("Channel is already closed", innerException)
    { }

    public LuminChannelClosedException(string message, Exception innerException) : base(message, innerException) { }
}

internal unsafe class SingleConsumerUnboundedLuminChannel<T> : LuminChannel<T>, IDisposable
{
    readonly Queue<T> items;
    readonly SingleConsumerUnboundedLuminChannelReader readerSource;
    LuminTaskHandle completedTaskSource;

    Exception completionError;
    bool closed;
    bool _isDisposed;

    public SingleConsumerUnboundedLuminChannel()
    {
        items = new Queue<T>();
        Writer = new SingleConsumerUnboundedLuminChannelWriter(this);
        readerSource = new SingleConsumerUnboundedLuminChannelReader(this);
        Reader = readerSource;
    }

    ~SingleConsumerUnboundedLuminChannel() => Dispose(false);

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
        if (_isDisposed) return;
        _isDisposed = true;

        LuminTaskHandle completionSource;
        lock (items)
        {

            if (disposing)
            {
                while (items.Count > 0)
                {
                    var item = items.Dequeue();
                    (item as IDisposable)?.Dispose();
                }
            }
            else
            {
                items.Clear();
            }

            completionSource = completedTaskSource;
            completedTaskSource = 0;
            closed = true;
        }

        if (completionSource != 0)
            LuminTaskCoreSource.TrySetCanceled(completionSource);

        readerSource.CancelPendingWaiter();
    }

    sealed class SingleConsumerUnboundedLuminChannelWriter : LuminChannelWriter<T>
    {
        readonly SingleConsumerUnboundedLuminChannel<T> parent;

        public SingleConsumerUnboundedLuminChannelWriter(SingleConsumerUnboundedLuminChannel<T> parent)
        {
            this.parent = parent;
        }

        public override bool TryWrite(T item)
        {
            bool waiting;
            lock (parent.items)
            {
                if (parent.closed) return false;

                parent.items.Enqueue(item);
                waiting = parent.readerSource.isWaiting;
            }

            if (waiting)
            {
                parent.readerSource.SignalContinuation();
            }

            return true;
        }

        public override bool TryComplete(Exception error = null)
        {
            bool waiting;
            bool completeNow;
            LuminTaskHandle completionSource = 0;
            lock (parent.items)
            {
                if (parent.closed) return false;
                parent.closed = true;
                waiting = parent.readerSource.isWaiting;
                completeNow = parent.items.Count == 0;

                if (completeNow)
                {
                    if (error == null)
                    {
                        if (parent.completedTaskSource != 0)
                        {
                            completionSource = parent.completedTaskSource;
                            parent.completedTaskSource = 0;
                        }
                    }
                    else
                    {
                        if (parent.completedTaskSource != 0)
                        {
                            completionSource = parent.completedTaskSource;
                            parent.completedTaskSource = 0;
                        }
                    }

                }

                parent.completionError = error;
            }

            if (completionSource != 0)
            {
                if (error == null)
                    LuminTaskCoreSource.TrySetResult(completionSource);
                else
                    LuminTaskCoreSource.TrySetException(completionSource, error);
            }

            if (waiting && completeNow)
                parent.readerSource.SignalCompleted(error);

            return true;
        }
    }

    sealed class SingleConsumerUnboundedLuminChannelReader : LuminChannelReader<T>
    {
        readonly Action<object> CancellationCallbackDelegate = CancellationCallback;
        readonly SingleConsumerUnboundedLuminChannel<T> parent;

        CancellationToken cancellationToken;
        CancellationTokenRegistration cancellationTokenRegistration;
        LuminTaskHandle core;
        internal bool isWaiting;

        public SingleConsumerUnboundedLuminChannelReader(SingleConsumerUnboundedLuminChannel<T> parent)
        {
            this.parent = parent;

        }

        ~SingleConsumerUnboundedLuminChannelReader()
        {
            CancelPendingWaiter();
        }

        public override LuminTask Completion
        {
            get
            {
                lock (parent.items)
                {
                    if (parent.completedTaskSource != 0)
                        return new LuminTask(parent.completedTaskSource);

                    if (parent.closed)
                    {
                        if (parent._isDisposed)
                            return LuminTask.FromCanceled(new CancellationToken(canceled: true));
                        return parent.completionError == null
                            ? LuminTask.CompletedTask()
                            : LuminTask.FromException(parent.completionError);
                    }

                    parent.completedTaskSource = LuminTask.RentSlot();
                    return new LuminTask(parent.completedTaskSource);
                }
            }
        }

        public override bool TryRead(out T item)
        {
            LuminTaskHandle completionSource = 0;
            lock (parent.items)
            {
                if (parent.items.Count != 0)
                {
                    item = parent.items.Dequeue();

                    if (parent.closed && parent.items.Count == 0)
                    {
                        if (parent.completionError != null)
                        {
                            if (parent.completedTaskSource != 0)
                            {
                                completionSource = parent.completedTaskSource;
                                parent.completedTaskSource = 0;
                            }
                        }
                        else
                        {
                            if (parent.completedTaskSource != 0)
                            {
                                completionSource = parent.completedTaskSource;
                                parent.completedTaskSource = 0;
                            }
                        }
                    }
                }
                else
                {
                    item = default;
                    return false;
                }
            }


            if (completionSource != 0)
            {
                if (parent.completionError == null)
                    LuminTaskCoreSource.TrySetResult(completionSource);
                else
                    LuminTaskCoreSource.TrySetException(completionSource, parent.completionError);
            }

            return true;
        }

        public override LuminTask<bool> WaitToReadAsync(CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return LuminTask.FromCanceled<bool>(cancellationToken);
            }

            lock (parent.items)
            {
                if (parent.items.Count != 0)
                {
                    return CompletedTasks.True();
                }

                if (parent.closed)
                {
                    if (parent.completionError == null)
                    {
                        return CompletedTasks.False();
                    }
                    else
                    {
                        return LuminTask.FromException<bool>(parent.completionError);
                    }
                }

                if (core != 0)
                    throw new InvalidOperationException("Only one pending WaitToReadAsync operation is allowed.");

                core = LuminTask.RentSlot();
                var task = new LuminTask<bool>(core);

                isWaiting = true;

                this.cancellationToken = cancellationToken;
                if (this.cancellationToken.CanBeCanceled)
                {
                    var registration = this.cancellationToken.RegisterWithoutCaptureExecutionContext(CancellationCallbackDelegate, this);
                    if (isWaiting && core != 0)
                        cancellationTokenRegistration = registration;
                    else
                        registration.Dispose();
                }

                return task;
            }
        }

        public void SignalContinuation()
        {
            CompletePendingWaiter(true, null);
        }

        public void SignalCancellation()
        {
            CompletePendingWaiter(false, new OperationCanceledException(cancellationToken));
        }

        public void SignalCompleted(Exception error)
        {
            CompletePendingWaiter(false, error);
        }

        internal void CancelPendingWaiter()
            => CompletePendingWaiter(false, new OperationCanceledException(cancellationToken));

        private void CompletePendingWaiter(bool result, Exception error)
        {
            LuminTaskHandle source;
            CancellationToken token;
            lock (parent.items)
            {
                if (!isWaiting || core == 0)
                    return;

                source = core;
                core = 0;
                isWaiting = false;
                token = cancellationToken;
                cancellationToken = default;
            }

            cancellationTokenRegistration.Release();

            if (error is OperationCanceledException)
                LuminTaskCoreSource.TrySetCanceled(source, token);
            else if (error != null)
                LuminTaskCoreSource.TrySetException(source, error);
            else
                LuminTaskResultBridge.TrySetResult(source, in result);
        }

        public override ILuminTaskAsyncEnumerable<T> ReadAllAsync(CancellationToken cancellationToken = default)
        {
            return new ReadAllAsyncEnumerable(this, cancellationToken);
        }

        static void CancellationCallback(object state)
        {
            var self = (SingleConsumerUnboundedLuminChannelReader)state;
            self.SignalCancellation();
        }

        sealed class ReadAllAsyncEnumerable : ILuminTaskAsyncEnumerable<T>, ILuminTaskAsyncEnumerator<T>
        {
            readonly Action<object> CancellationCallback1Delegate = CancellationCallback1;
            readonly Action<object> CancellationCallback2Delegate = CancellationCallback2;

            readonly SingleConsumerUnboundedLuminChannelReader parent;
            CancellationToken cancellationToken1;
            CancellationToken cancellationToken2;
            CancellationTokenRegistration cancellationTokenRegistration1;
            CancellationTokenRegistration cancellationTokenRegistration2;

            T current;
            bool cacheValue;
            bool running;

            public ReadAllAsyncEnumerable(SingleConsumerUnboundedLuminChannelReader parent, CancellationToken cancellationToken)
            {
                this.parent = parent;
                this.cancellationToken1 = cancellationToken;
            }

            public ILuminTaskAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
            {
                if (running)
                {
                    throw new InvalidOperationException("Enumerator is already running, does not allow call GetAsyncEnumerator twice.");
                }

                if (this.cancellationToken1 != cancellationToken)
                {
                    this.cancellationToken2 = cancellationToken;
                }

                if (this.cancellationToken1.CanBeCanceled)
                {
                    this.cancellationTokenRegistration1 =  this.cancellationToken1.RegisterWithoutCaptureExecutionContext(CancellationCallback1Delegate, this);
                }

                if (this.cancellationToken2.CanBeCanceled)
                {
                    this.cancellationTokenRegistration2 = this.cancellationToken2.RegisterWithoutCaptureExecutionContext(CancellationCallback2Delegate, this);
                }

                running = true;
                return this;
            }

            public T Current
            {
                get
                {
                    if (cacheValue)
                    {
                        return current;
                    }
                    parent.TryRead(out current);
                    return current;
                }
            }

            public LuminTask<bool> MoveNextAsync()
            {
                cacheValue = false;
                return parent.WaitToReadAsync(CancellationToken.None);
            }

            public LuminTask DisposeAsync()
            {
                cancellationTokenRegistration1.Dispose();
                cancellationTokenRegistration2.Dispose();
                return default;
            }

            static void CancellationCallback1(object state)
            {
                var self = (ReadAllAsyncEnumerable)state;
                self.parent.SignalCancellation();
            }

            static void CancellationCallback2(object state)
            {
                var self = (ReadAllAsyncEnumerable)state;
                self.parent.SignalCancellation();
            }
        }
    }
}
