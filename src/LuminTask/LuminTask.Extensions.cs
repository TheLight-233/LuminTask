using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using LuminThread.TaskSource;
using LuminThread.Utility;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using LuminThread.Interface;

namespace LuminThread
{
    public static unsafe class LuminTaskExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ValueTask AsValueTask(this LuminTask task) => task;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ValueTask<T> AsValueTask<T>(this LuminTask<T> task) => task;

        #region Task 转换

        public static LuminTask<T> AsLuminTask<T>(this Task<T> task, CancellationToken cancellationToken = default)
        {
            if (task is null) throw new ArgumentNullException(nameof(task));
            if (task.IsCompleted) return ConvertCompletedTask(task, cancellationToken);

            var core = LuminTaskSourceCore<T>.Create();
            var state = StateTuple.Create(new IntPtr(core), cancellationToken);
            task.ContinueWith(
                static (completed, boxedState) => CompleteTaskConversion(completed, boxedState),
                state,
                CancellationToken.None,
                TaskContinuationOptions.ExecuteSynchronously,
                TaskScheduler.Default);

            return new LuminTask<T>(LuminTaskSourceCore<T>.MethodTableAutoDisposePtr, core, core->Id);
        }

        public static LuminTask AsLuminTask(this Task task, CancellationToken cancellationToken = default)
        {
            if (task is null) throw new ArgumentNullException(nameof(task));
            if (task.IsCompleted) return ConvertCompletedTask(task, cancellationToken);

            var core = LuminTaskSourceCore<AsyncUnit>.Create();
            var state = StateTuple.Create(new IntPtr(core), cancellationToken);
            task.ContinueWith(
                static (completed, boxedState) => CompleteTaskConversion(completed, boxedState),
                state,
                CancellationToken.None,
                TaskContinuationOptions.ExecuteSynchronously,
                TaskScheduler.Default);

            return new LuminTask(LuminTaskSourceCore<AsyncUnit>.MethodTableAutoDisposePtr, core, core->Id);
        }

        private static LuminTask<T> ConvertCompletedTask<T>(Task<T> task, CancellationToken fallbackToken)
        {
            try
            {
                return LuminTask.FromResult(task.GetAwaiter().GetResult());
            }
            catch (OperationCanceledException canceled)
            {
                return LuminTask.FromCanceled<T>(SelectCancellationToken(canceled, fallbackToken));
            }
            catch (Exception exception)
            {
                return LuminTask.FromException<T>(exception);
            }
        }

        private static LuminTask ConvertCompletedTask(Task task, CancellationToken fallbackToken)
        {
            try
            {
                task.GetAwaiter().GetResult();
                return LuminTask.CompletedTask();
            }
            catch (OperationCanceledException canceled)
            {
                return LuminTask.FromCanceled(SelectCancellationToken(canceled, fallbackToken));
            }
            catch (Exception exception)
            {
                return LuminTask.FromException(exception);
            }
        }

        private static void CompleteTaskConversion<T>(Task<T> task, object? boxedState)
        {
            var state = (StateTuple<(IntPtr, CancellationToken)>)boxedState!;
            var core = (LuminTaskSourceCore<T>*)state.Item.Item1.ToPointer();
            var fallbackToken = state.Item.Item2;
            state.Dispose();

            try
            {
                LuminTaskSourceCore<T>.TrySetResult(core, task.GetAwaiter().GetResult());
            }
            catch (OperationCanceledException canceled)
            {
                LuminTaskSourceCore<T>.TrySetCanceled(core, SelectCancellationToken(canceled, fallbackToken));
            }
            catch (Exception exception)
            {
                LuminTaskSourceCore<T>.TrySetException(core, exception);
            }
        }

        private static void CompleteTaskConversion(Task task, object? boxedState)
        {
            var state = (StateTuple<(IntPtr, CancellationToken)>)boxedState!;
            var core = (LuminTaskSourceCore<AsyncUnit>*)state.Item.Item1.ToPointer();
            var fallbackToken = state.Item.Item2;
            state.Dispose();

            try
            {
                task.GetAwaiter().GetResult();
                LuminTaskSourceCore<AsyncUnit>.TrySetResult(core);
            }
            catch (OperationCanceledException canceled)
            {
                LuminTaskSourceCore<AsyncUnit>.TrySetCanceled(core, SelectCancellationToken(canceled, fallbackToken));
            }
            catch (Exception exception)
            {
                LuminTaskSourceCore<AsyncUnit>.TrySetException(core, exception);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static CancellationToken SelectCancellationToken(
            OperationCanceledException exception,
            CancellationToken fallbackToken)
            => exception.CancellationToken.CanBeCanceled ? exception.CancellationToken : fallbackToken;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static CancellationToken NormalizeCanceledToken(CancellationToken token)
            => token.IsCancellationRequested ? token : new CancellationToken(canceled: true);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void TrySetExceptionOrCanceled<TResult>(void* source, Exception exception)
        {
            if (exception is OperationCanceledException canceled)
                LuminTaskSourceCore<TResult>.TrySetCanceled(source, canceled.CancellationToken);
            else
                LuminTaskSourceCore<TResult>.TrySetException(source, exception);
        }

        // Timeout/cancellation only stops waiting; it does not transfer ownership of the
        // single-consumer source back to its producer. Keep a lightweight continuation attached
        // so the abandoned inner task is consumed and can release its native source when it
        // eventually completes.
        private static void Drain(LuminTask task)
        {
            var awaiter = task.GetAwaiter();
            if (awaiter.IsCompleted)
            {
                try { awaiter.GetResult(); }
                catch (Exception) { }
                return;
            }

            awaiter.SourceOnCompleted(static state =>
            {
                var pendingAwaiter = (LuminTask)state;
                try { pendingAwaiter.GetResult(); }
                catch (Exception) { }
            }, awaiter);
        }

        private static void Drain<T>(LuminTask<T> task)
        {
            var awaiter = task.GetAwaiter();
            if (awaiter.IsCompleted)
            {
                try { awaiter.GetResult(); }
                catch (Exception) { }
                return;
            }

            awaiter.SourceOnCompleted(static state =>
            {
                var pendingAwaiter = (LuminTask<T>)state;
                try { pendingAwaiter.GetResult(); }
                catch (Exception) { }
            }, awaiter);
        }

        public static Task<T> AsTask<T>(this LuminTask<T> task)
        {
            var awaiter = task.GetAwaiter();
            if (awaiter.IsCompleted)
            {
                try
                {
                    return Task.FromResult(awaiter.GetResult());
                }
                catch (OperationCanceledException canceled)
                {
                    return Task.FromCanceled<T>(NormalizeCanceledToken(canceled.CancellationToken));
                }
                catch (Exception exception)
                {
                    return Task.FromException<T>(exception);
                }
            }

            return new LuminTaskAsTaskSource<T>(task).Task;
        }

        public static Task AsTask(this LuminTask task)
        {
            var awaiter = task.GetAwaiter();
            if (awaiter.IsCompleted)
            {
                try
                {
                    awaiter.GetResult();
                    return Task.CompletedTask;
                }
                catch (OperationCanceledException canceled)
                {
                    return Task.FromCanceled(NormalizeCanceledToken(canceled.CancellationToken));
                }
                catch (Exception exception)
                {
                    return Task.FromException(exception);
                }
            }

            return new LuminTaskAsTaskSource(task).Task;
        }

        private sealed class LuminTaskAsTaskSource : TaskCompletionSource<bool>
        {
            private LuminTask _task;

            internal LuminTaskAsTaskSource(in LuminTask task)
                : base(TaskCreationOptions.RunContinuationsAsynchronously)
            {
                _task = task;
                task.GetAwaiter().SourceOnCompleted(
                    static state => ((LuminTaskAsTaskSource)state).Complete(),
                    this);
            }

            private void Complete()
            {
                var task = _task;
                _task = default;
                try
                {
                    task.GetAwaiter().GetResult();
                    TrySetResult(true);
                }
                catch (OperationCanceledException canceled)
                {
                    TrySetCanceled(NormalizeCanceledToken(canceled.CancellationToken));
                }
                catch (Exception exception)
                {
                    TrySetException(exception);
                }
            }
        }

        private sealed class LuminTaskAsTaskSource<T> : TaskCompletionSource<T>
        {
            private LuminTask<T> _task;

            internal LuminTaskAsTaskSource(in LuminTask<T> task)
                : base(TaskCreationOptions.RunContinuationsAsynchronously)
            {
                _task = task;
                task.GetAwaiter().SourceOnCompleted(
                    static state => ((LuminTaskAsTaskSource<T>)state).Complete(),
                    this);
            }

            private void Complete()
            {
                var task = _task;
                _task = default;
                try
                {
                    TrySetResult(task.GetAwaiter().GetResult());
                }
                catch (OperationCanceledException canceled)
                {
                    TrySetCanceled(NormalizeCanceledToken(canceled.CancellationToken));
                }
                catch (Exception exception)
                {
                    TrySetException(exception);
                }
            }
        }

        #endregion

        #region 取消支持

        public static LuminTask AttachExternalCancellation(this LuminTask task, CancellationToken cancellationToken)
        {
            if (task.Status != LuminTaskStatus.Pending)
                return task;

            if (!cancellationToken.CanBeCanceled)
                return task;

            if (cancellationToken.IsCancellationRequested)
            {
                Drain(task);
                return LuminTask.FromCanceled(cancellationToken);
            }

            return AttachExternalCancellationPromise.Create(task, cancellationToken);
        }

        public static LuminTask<T> AttachExternalCancellation<T>(this LuminTask<T> task, CancellationToken cancellationToken)
        {
            if (task.Status != LuminTaskStatus.Pending)
                return task;

            if (!cancellationToken.CanBeCanceled)
                return task;

            if (cancellationToken.IsCancellationRequested)
            {
                Drain(task);
                return LuminTask.FromCanceled<T>(cancellationToken);
            }

            return AttachExternalCancellationPromise<T>.Create(task, cancellationToken);
        }

        /// <summary>Waits for this task while allowing the wait to be canceled.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static LuminTask WaitAsync(this LuminTask task, CancellationToken cancellationToken)
            => task.AttachExternalCancellation(cancellationToken);

        /// <summary>Waits for this task while allowing the wait to be canceled.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static LuminTask<T> WaitAsync<T>(this LuminTask<T> task, CancellationToken cancellationToken)
            => task.AttachExternalCancellation(cancellationToken);

        private static class AttachExternalCancellationPromise
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static LuminTask Create(LuminTask task, CancellationToken cancellationToken)
            {
                var core = LuminTaskSourceCore<AsyncUnit>.Create();
                var state = new LuminTaskState(core, cancellationToken, task);

                PlayerLoopHelper.AddAction(PlayerLoopTiming.DotNet, state, MoveNext, &MoveNext);
                return new LuminTask(LuminTaskSourceCore<AsyncUnit>.MethodTableAutoDisposePtr, core, core->Id);
            }

            public static bool MoveNext(in LuminTaskState state)
            {
                var innerTask = (LuminTask)state.State;

                if (state.CancellationToken.IsCancellationRequested)
                {
                    Drain(innerTask);
                    LuminTaskSourceCore<AsyncUnit>.TrySetCanceled(state.Source, state.CancellationToken);
                    return false;
                }

                if (innerTask.Status != LuminTaskStatus.Pending)
                {
                    try
                    {
                        innerTask.GetAwaiter().GetResult();
                        LuminTaskSourceCore<AsyncUnit>.TrySetResult(state.Source);
                    }
                    catch (Exception ex)
                    {
                        TrySetExceptionOrCanceled<AsyncUnit>(state.Source, ex);
                    }
                    return false;
                }

                return true;
            }
        }

        private static class AttachExternalCancellationPromise<T>
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static LuminTask<T> Create(LuminTask<T> task, CancellationToken cancellationToken)
            {
                var core = LuminTaskSourceCore<T>.Create();
                var state = new LuminTaskState(core, cancellationToken, task);

                PlayerLoopHelper.AddAction(PlayerLoopTiming.DotNet, state, MoveNext, &MoveNext);
                return new LuminTask<T>(LuminTaskSourceCore<T>.MethodTableAutoDisposePtr, core, core->Id);
            }

            public static bool MoveNext(in LuminTaskState state)
            {
                var innerTask = (LuminTask<T>)state.State;

                if (state.CancellationToken.IsCancellationRequested)
                {
                    Drain(innerTask);
                    LuminTaskSourceCore<T>.TrySetCanceled(state.Source, state.CancellationToken);
                    return false;
                }

                if (innerTask.Status != LuminTaskStatus.Pending)
                {
                    try
                    {
                        var result = innerTask.GetAwaiter().GetResult();
                        LuminTaskSourceCore<T>.TrySetResult(state.Source, result);
                    }
                    catch (Exception ex)
                    {
                        TrySetExceptionOrCanceled<T>(state.Source, ex);
                    }
                    return false;
                }

                return true;
            }
        }

        #endregion

        #region 超时支持

        public static LuminTask Timeout(this LuminTask task, TimeSpan timeout, CancellationToken cancellationToken = default)
        {
            ValidateTimeout(timeout);
            if (task.Status != LuminTaskStatus.Pending)
                return task;
            if (cancellationToken.IsCancellationRequested)
            {
                Drain(task);
                return LuminTask.FromCanceled(cancellationToken);
            }
            if (timeout == System.Threading.Timeout.InfiniteTimeSpan)
                return cancellationToken.CanBeCanceled ? task.AttachExternalCancellation(cancellationToken) : task;
            if (timeout == TimeSpan.Zero)
            {
                Drain(task);
                return LuminTask.FromException(new TimeoutException());
            }
            return TimeoutPromise.Create(task, timeout, cancellationToken);
        }

        public static LuminTask<T> Timeout<T>(this LuminTask<T> task, TimeSpan timeout, CancellationToken cancellationToken = default)
        {
            ValidateTimeout(timeout);
            if (task.Status != LuminTaskStatus.Pending)
                return task;
            if (cancellationToken.IsCancellationRequested)
            {
                Drain(task);
                return LuminTask.FromCanceled<T>(cancellationToken);
            }
            if (timeout == System.Threading.Timeout.InfiniteTimeSpan)
                return cancellationToken.CanBeCanceled ? task.AttachExternalCancellation(cancellationToken) : task;
            if (timeout == TimeSpan.Zero)
            {
                Drain(task);
                return LuminTask.FromException<T>(new TimeoutException());
            }
            return TimeoutPromise<T>.Create(task, timeout, cancellationToken);
        }

        /// <summary>Waits for this task with a timeout and optional cancellation.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static LuminTask WaitAsync(
            this LuminTask task,
            TimeSpan timeout,
            CancellationToken cancellationToken = default)
            => task.Timeout(timeout, cancellationToken);

        /// <summary>Waits for this task with a timeout and optional cancellation.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static LuminTask<T> WaitAsync<T>(
            this LuminTask<T> task,
            TimeSpan timeout,
            CancellationToken cancellationToken = default)
            => task.Timeout(timeout, cancellationToken);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void ValidateTimeout(TimeSpan timeout)
        {
            if (timeout < TimeSpan.Zero && timeout != System.Threading.Timeout.InfiniteTimeSpan)
                throw new ArgumentOutOfRangeException(nameof(timeout));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static long ToStopwatchTicks(TimeSpan timeout)
        {
            var ticks = timeout.Ticks * ((double)Stopwatch.Frequency / TimeSpan.TicksPerSecond);
            return ticks >= long.MaxValue ? long.MaxValue : (long)ticks;
        }

        private static class TimeoutPromise
        {
            [StructLayout(LayoutKind.Sequential)]
            private struct TimeoutStateData
            {
                public long StartTimestamp;
                public long TimeoutTimestampTicks;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static LuminTask Create(LuminTask task, TimeSpan timeout, CancellationToken cancellationToken)
            {
                var core = LuminTaskSourceCore<AsyncUnit>.Create();

                var stateData = new TimeoutStateData
                {
                    StartTimestamp = Stopwatch.GetTimestamp(),
                    TimeoutTimestampTicks = ToStopwatchTicks(timeout)
                };

                var stateTuple = StateTuple.Create(stateData);
                var state = new LuminTaskState(core, cancellationToken, task, stateTuple);

                PlayerLoopHelper.AddAction(PlayerLoopTiming.DotNet, state, MoveNext, &MoveNext);
                return new LuminTask(LuminTaskSourceCore<AsyncUnit>.MethodTableAutoDisposePtr, core, core->Id);
            }

            public static bool MoveNext(in LuminTaskState state)
            {
                var innerTask = (LuminTask)state.State;
                var stateTuple = Unsafe.As<StateTuple<TimeoutStateData>>(state.StateTuple);
                if (stateTuple == null) return false;

                var data = stateTuple.Item;

                if (state.CancellationToken.IsCancellationRequested)
                {
                    Drain(innerTask);
                    LuminTaskSourceCore<AsyncUnit>.TrySetCanceled(state.Source, state.CancellationToken);
                    stateTuple.Dispose();
                    return false;
                }

                if (Stopwatch.GetTimestamp() - data.StartTimestamp >= data.TimeoutTimestampTicks)
                {
                    Drain(innerTask);
                    LuminTaskSourceCore<AsyncUnit>.TrySetException(state.Source, new TimeoutException());
                    stateTuple.Dispose();
                    return false;
                }

                if (innerTask.Status != LuminTaskStatus.Pending)
                {
                    try
                    {
                        innerTask.GetAwaiter().GetResult();
                        LuminTaskSourceCore<AsyncUnit>.TrySetResult(state.Source);
                    }
                    catch (Exception ex)
                    {
                        TrySetExceptionOrCanceled<AsyncUnit>(state.Source, ex);
                    }
                    stateTuple.Dispose();
                    return false;
                }

                return true;
            }
        }

        private static class TimeoutPromise<T>
        {
            [StructLayout(LayoutKind.Sequential)]
            private struct TimeoutStateData
            {
                public long StartTimestamp;
                public long TimeoutTimestampTicks;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static LuminTask<T> Create(LuminTask<T> task, TimeSpan timeout, CancellationToken cancellationToken)
            {
                var core = LuminTaskSourceCore<T>.Create();

                var stateData = new TimeoutStateData
                {
                    StartTimestamp = Stopwatch.GetTimestamp(),
                    TimeoutTimestampTicks = ToStopwatchTicks(timeout)
                };

                var stateTuple = StateTuple.Create(stateData);
                var state = new LuminTaskState(core, cancellationToken, task, stateTuple);

                PlayerLoopHelper.AddAction(PlayerLoopTiming.DotNet, state, MoveNext, &MoveNext);
                return new LuminTask<T>(LuminTaskSourceCore<T>.MethodTableAutoDisposePtr, core, core->Id);
            }

            public static bool MoveNext(in LuminTaskState state)
            {
                var innerTask = (LuminTask<T>)state.State;
                var stateTuple = Unsafe.As<StateTuple<TimeoutStateData>>(state.StateTuple);
                if (stateTuple == null) return false;

                var data = stateTuple.Item;

                if (state.CancellationToken.IsCancellationRequested)
                {
                    Drain(innerTask);
                    LuminTaskSourceCore<T>.TrySetCanceled(state.Source, state.CancellationToken);
                    stateTuple.Dispose();
                    return false;
                }

                if (Stopwatch.GetTimestamp() - data.StartTimestamp >= data.TimeoutTimestampTicks)
                {
                    Drain(innerTask);
                    LuminTaskSourceCore<T>.TrySetException(state.Source, new TimeoutException());
                    stateTuple.Dispose();
                    return false;
                }

                if (innerTask.Status != LuminTaskStatus.Pending)
                {
                    try
                    {
                        var result = innerTask.GetAwaiter().GetResult();
                        LuminTaskSourceCore<T>.TrySetResult(state.Source, result);
                    }
                    catch (Exception ex)
                    {
                        TrySetExceptionOrCanceled<T>(state.Source, ex);
                    }
                    stateTuple.Dispose();
                    return false;
                }

                return true;
            }
        }

        #endregion

        #region Forget (忽略结果)

        public static void Forget(this LuminTask task)
        {
            var awaiter = task.GetAwaiter();

            if (awaiter.IsCompleted)
            {
                try { awaiter.GetResult(); }
                catch (Exception) { }
            }
            else
            {
                ForgetBlock.Schedule(awaiter, null);
            }
        }

        public static void Forget(this LuminTask task, Action<Exception> exceptionHandler)
        {
            if (exceptionHandler == null)
            {
                Forget(task);
                return;
            }

            var awaiter = task.GetAwaiter();

            if (awaiter.IsCompleted)
            {
                try { awaiter.GetResult(); }
                catch (Exception ex) { exceptionHandler(ex); }
            }
            else
            {
                ForgetBlock.Schedule(awaiter, exceptionHandler);
            }
        }

        public static void Forget<T>(this LuminTask<T> task)
        {
            var awaiter = task.GetAwaiter();

            if (awaiter.IsCompleted)
            {
                try { awaiter.GetResult(); }
                catch (Exception) { }
            }
            else
            {
                ForgetBlock<T>.Schedule(awaiter, null);
            }
        }

        public static void Forget<T>(this LuminTask<T> task, Action<Exception> exceptionHandler)
        {
            if (exceptionHandler == null)
            {
                Forget(task);
                return;
            }

            var awaiter = task.GetAwaiter();

            if (awaiter.IsCompleted)
            {
                try { awaiter.GetResult(); }
                catch (Exception ex) { exceptionHandler(ex); }
            }
            else
            {
                ForgetBlock<T>.Schedule(awaiter, exceptionHandler);
            }
        }

        #endregion

        #region ContinueWith (链式调用)

        public static LuminTask ContinueWith<T>(this LuminTask<T> task, Action<T> continuation, PlayerLoopTiming loopTiming = PlayerLoopTiming.DotNet)
        {
            return ContinueWithActionPromise<T>.Create(task, continuation, loopTiming);
        }

        public static LuminTask<TR> ContinueWith<T, TR>(this LuminTask<T> task, Func<T, TR> continuation, PlayerLoopTiming loopTiming = PlayerLoopTiming.DotNet)
        {
            return ContinueWithFuncPromise<T, TR>.Create(task, continuation, loopTiming);
        }

        public static LuminTask ContinueWith<T>(this LuminTask<T> task, Func<T, LuminTask> continuation, PlayerLoopTiming loopTiming = PlayerLoopTiming.DotNet)
        {
            return ContinueWithAsyncActionPromise<T>.Create(task, continuation, loopTiming);
        }

        public static LuminTask<TR> ContinueWith<T, TR>(this LuminTask<T> task, Func<T, LuminTask<TR>> continuation, PlayerLoopTiming loopTiming = PlayerLoopTiming.DotNet)
        {
            return ContinueWithAsyncFuncPromise<T, TR>.Create(task, continuation, loopTiming);
        }

        public static LuminTask ContinueWith(this LuminTask task, Action continuation, PlayerLoopTiming loopTiming = PlayerLoopTiming.DotNet)
        {
            return ContinueWithActionPromise.Create(task, continuation, loopTiming);
        }

        public static LuminTask<T> ContinueWith<T>(this LuminTask task, Func<T> continuation, PlayerLoopTiming loopTiming = PlayerLoopTiming.DotNet)
        {
            return ContinueWithFuncPromise<T>.Create(task, continuation, loopTiming);
        }

        public static LuminTask ContinueWith(this LuminTask task, Func<LuminTask> continuation, PlayerLoopTiming loopTiming = PlayerLoopTiming.DotNet)
        {
            return ContinueWithAsyncActionPromise.Create(task, continuation, loopTiming);
        }

        public static LuminTask<T> ContinueWith<T>(this LuminTask task, Func<LuminTask<T>> continuation, PlayerLoopTiming loopTiming = PlayerLoopTiming.DotNet)
        {
            return ContinueWithAsyncFuncPromise<T>.Create(task, continuation, loopTiming);
        }

        private static class ContinueWithActionPromise<T>
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static LuminTask Create(LuminTask<T> task, Action<T> continuation, PlayerLoopTiming loopTiming = PlayerLoopTiming.DotNet)
            {
                var core = LuminTaskSourceCore<AsyncUnit>.Create();
                var stateTuple = StateTuple.Create(continuation);
                var state = new LuminTaskState(core, CancellationToken.None, task, stateTuple);

                PlayerLoopHelper.AddAction(loopTiming, state, MoveNext, &MoveNext);
                return new LuminTask(LuminTaskSourceCore<AsyncUnit>.MethodTableAutoDisposePtr, core, core->Id);
            }

            public static bool MoveNext(in LuminTaskState state)
            {
                var innerTask = (LuminTask<T>)state.State;
                var continuation = Unsafe.As<StateTuple<Action<T>>>(state.StateTuple);

                if (innerTask.Status == LuminTaskStatus.Pending)
                    return true;

                try
                {
                    var result = innerTask.GetAwaiter().GetResult();
                    continuation.Item(result);
                    LuminTaskSourceCore<AsyncUnit>.TrySetResult(state.Source);
                }
                catch (Exception ex)
                {
                    TrySetExceptionOrCanceled<AsyncUnit>(state.Source, ex);
                }

                continuation.Dispose();
                return false;
            }
        }

        private static class ContinueWithFuncPromise<T, TR>
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static LuminTask<TR> Create(LuminTask<T> task, Func<T, TR> continuation, PlayerLoopTiming loopTiming = PlayerLoopTiming.DotNet)
            {
                var core = LuminTaskSourceCore<TR>.Create();
                var stateTuple = StateTuple.Create(continuation);
                var state = new LuminTaskState(core, CancellationToken.None, task, stateTuple);

                PlayerLoopHelper.AddAction(loopTiming, state, MoveNext, &MoveNext);
                return new LuminTask<TR>(LuminTaskSourceCore<TR>.MethodTableAutoDisposePtr, core, core->Id);
            }

            public static bool MoveNext(in LuminTaskState state)
            {
                var innerTask = (LuminTask<T>)state.State;
                var continuation = Unsafe.As<StateTuple<Func<T, TR>>>(state.StateTuple);

                if (innerTask.Status == LuminTaskStatus.Pending)
                    return true;

                try
                {
                    var result = innerTask.GetAwaiter().GetResult();
                    var continuationResult = continuation.Item(result);
                    LuminTaskSourceCore<TR>.TrySetResult(state.Source, continuationResult);
                }
                catch (Exception ex)
                {
                    TrySetExceptionOrCanceled<TR>(state.Source, ex);
                }

                continuation.Dispose();
                return false;
            }
        }

        private static class ContinueWithAsyncActionPromise<T>
        {
            [StructLayout(LayoutKind.Sequential)]
            private struct StateData
            {
                public Func<T, LuminTask> Continuation;
                public LuminTask ContinuationTask;
                public bool WaitingForContinuation;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static LuminTask Create(LuminTask<T> task, Func<T, LuminTask> continuation, PlayerLoopTiming loopTiming = PlayerLoopTiming.DotNet)
            {
                var core = LuminTaskSourceCore<AsyncUnit>.Create();
                var stateData = new StateData
                {
                    Continuation = continuation,
                    WaitingForContinuation = false
                };
                var stateTuple = StateTuple.Create(stateData);
                var state = new LuminTaskState(core, CancellationToken.None, task, stateTuple);

                PlayerLoopHelper.AddAction(loopTiming, state, MoveNext, &MoveNext);
                return new LuminTask(LuminTaskSourceCore<AsyncUnit>.MethodTableAutoDisposePtr, core, core->Id);
            }

            public static bool MoveNext(in LuminTaskState state)
            {
                var innerTask = (LuminTask<T>)state.State;
                var stateTuple = Unsafe.As<StateTuple<StateData>>(state.StateTuple);
                ref var data = ref stateTuple.Item;

                if (!data.WaitingForContinuation)
                {
                    if (innerTask.Status == LuminTaskStatus.Pending)
                        return true;

                    try
                    {
                        var result = innerTask.GetAwaiter().GetResult();
                        data.ContinuationTask = data.Continuation(result);
                        data.WaitingForContinuation = true;

                        if (data.ContinuationTask.Status != LuminTaskStatus.Pending)
                        {
                            data.ContinuationTask.GetAwaiter().GetResult();
                            LuminTaskSourceCore<AsyncUnit>.TrySetResult(state.Source);
                            stateTuple.Dispose();
                            return false;
                        }
                    }
                    catch (Exception ex)
                    {
                        TrySetExceptionOrCanceled<AsyncUnit>(state.Source, ex);
                        stateTuple.Dispose();
                        return false;
                    }
                }
                else
                {
                    if (data.ContinuationTask.Status == LuminTaskStatus.Pending)
                        return true;

                    try
                    {
                        data.ContinuationTask.GetAwaiter().GetResult();
                        LuminTaskSourceCore<AsyncUnit>.TrySetResult(state.Source);
                    }
                    catch (Exception ex)
                    {
                        TrySetExceptionOrCanceled<AsyncUnit>(state.Source, ex);
                    }

                    stateTuple.Dispose();
                    return false;
                }

                return true;
            }
        }

        private static class ContinueWithAsyncFuncPromise<T, TR>
        {
            [StructLayout(LayoutKind.Sequential)]
            private struct StateData
            {
                public Func<T, LuminTask<TR>> Continuation;
                public LuminTask<TR> ContinuationTask;
                public bool WaitingForContinuation;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static LuminTask<TR> Create(LuminTask<T> task, Func<T, LuminTask<TR>> continuation, PlayerLoopTiming loopTiming = PlayerLoopTiming.DotNet)
            {
                var core = LuminTaskSourceCore<TR>.Create();
                var stateData = new StateData
                {
                    Continuation = continuation,
                    WaitingForContinuation = false
                };
                var stateTuple = StateTuple.Create(stateData);
                var state = new LuminTaskState(core, CancellationToken.None, task, stateTuple);

                PlayerLoopHelper.AddAction(loopTiming, state, MoveNext, &MoveNext);
                return new LuminTask<TR>(LuminTaskSourceCore<TR>.MethodTableAutoDisposePtr, core, core->Id);
            }

            public static bool MoveNext(in LuminTaskState state)
            {
                var innerTask = (LuminTask<T>)state.State;
                var stateTuple = Unsafe.As<StateTuple<StateData>>(state.StateTuple);
                ref var data = ref stateTuple.Item;

                if (!data.WaitingForContinuation)
                {
                    if (innerTask.Status == LuminTaskStatus.Pending)
                        return true;

                    try
                    {
                        var result = innerTask.GetAwaiter().GetResult();
                        data.ContinuationTask = data.Continuation(result);
                        data.WaitingForContinuation = true;

                        if (data.ContinuationTask.Status != LuminTaskStatus.Pending)
                        {
                            var finalResult = data.ContinuationTask.GetAwaiter().GetResult();
                            LuminTaskSourceCore<TR>.TrySetResult(state.Source, finalResult);
                            stateTuple.Dispose();
                            return false;
                        }
                    }
                    catch (Exception ex)
                    {
                        TrySetExceptionOrCanceled<TR>(state.Source, ex);
                        stateTuple.Dispose();
                        return false;
                    }
                }
                else
                {
                    if (data.ContinuationTask.Status == LuminTaskStatus.Pending)
                        return true;

                    try
                    {
                        var finalResult = data.ContinuationTask.GetAwaiter().GetResult();
                        LuminTaskSourceCore<TR>.TrySetResult(state.Source, finalResult);
                    }
                    catch (Exception ex)
                    {
                        TrySetExceptionOrCanceled<TR>(state.Source, ex);
                    }

                    stateTuple.Dispose();
                    return false;
                }

                return true;
            }
        }

        private static class ContinueWithActionPromise
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static LuminTask Create(LuminTask task, Action continuation, PlayerLoopTiming loopTiming = PlayerLoopTiming.DotNet)
            {
                var core = LuminTaskSourceCore<AsyncUnit>.Create();
                var stateTuple = StateTuple.Create(continuation);
                var state = new LuminTaskState(core, CancellationToken.None, task, stateTuple);

                PlayerLoopHelper.AddAction(loopTiming, state, MoveNext, &MoveNext);
                return new LuminTask(LuminTaskSourceCore<AsyncUnit>.MethodTableAutoDisposePtr, core, core->Id);
            }

            public static bool MoveNext(in LuminTaskState state)
            {
                var innerTask = (LuminTask)state.State;
                var continuation = Unsafe.As<StateTuple<Action>>(state.StateTuple);

                if (innerTask.Status == LuminTaskStatus.Pending)
                    return true;

                try
                {
                    innerTask.GetAwaiter().GetResult();
                    continuation.Item();
                    LuminTaskSourceCore<AsyncUnit>.TrySetResult(state.Source);
                }
                catch (Exception ex)
                {
                    TrySetExceptionOrCanceled<AsyncUnit>(state.Source, ex);
                }

                continuation.Dispose();
                return false;
            }
        }

        private static class ContinueWithFuncPromise<T>
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static LuminTask<T> Create(LuminTask task, Func<T> continuation, PlayerLoopTiming loopTiming = PlayerLoopTiming.DotNet)
            {
                var core = LuminTaskSourceCore<T>.Create();
                var stateTuple = StateTuple.Create(continuation);
                var state = new LuminTaskState(core, CancellationToken.None, task, stateTuple);

                PlayerLoopHelper.AddAction(loopTiming, state, MoveNext, &MoveNext);
                return new LuminTask<T>(LuminTaskSourceCore<T>.MethodTableAutoDisposePtr, core, core->Id);
            }

            public static bool MoveNext(in LuminTaskState state)
            {
                var innerTask = (LuminTask)state.State;
                var continuation = Unsafe.As<StateTuple<Func<T>>>(state.StateTuple);

                if (innerTask.Status == LuminTaskStatus.Pending)
                    return true;

                try
                {
                    innerTask.GetAwaiter().GetResult();
                    var result = continuation.Item();
                    LuminTaskSourceCore<T>.TrySetResult(state.Source, result);
                }
                catch (Exception ex)
                {
                    TrySetExceptionOrCanceled<T>(state.Source, ex);
                }

                continuation.Dispose();
                return false;
            }
        }

        private static class ContinueWithAsyncActionPromise
        {
            [StructLayout(LayoutKind.Sequential)]
            private struct StateData
            {
                public Func<LuminTask> Continuation;
                public LuminTask ContinuationTask;
                public bool WaitingForContinuation;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static LuminTask Create(LuminTask task, Func<LuminTask> continuation, PlayerLoopTiming loopTiming = PlayerLoopTiming.DotNet)
            {
                var core = LuminTaskSourceCore<AsyncUnit>.Create();
                var stateData = new StateData
                {
                    Continuation = continuation,
                    WaitingForContinuation = false
                };
                var stateTuple = StateTuple.Create(stateData);
                var state = new LuminTaskState(core, CancellationToken.None, task, stateTuple);

                PlayerLoopHelper.AddAction(loopTiming, state, MoveNext, &MoveNext);
                return new LuminTask(LuminTaskSourceCore<AsyncUnit>.MethodTableAutoDisposePtr, core, core->Id);
            }

            public static bool MoveNext(in LuminTaskState state)
            {
                var innerTask = (LuminTask)state.State;
                var stateTuple = Unsafe.As<StateTuple<StateData>>(state.StateTuple);
                ref var data = ref stateTuple.Item;

                if (!data.WaitingForContinuation)
                {
                    if (innerTask.Status == LuminTaskStatus.Pending)
                        return true;

                    try
                    {
                        innerTask.GetAwaiter().GetResult();
                        data.ContinuationTask = data.Continuation();
                        data.WaitingForContinuation = true;

                        if (data.ContinuationTask.Status != LuminTaskStatus.Pending)
                        {
                            data.ContinuationTask.GetAwaiter().GetResult();
                            LuminTaskSourceCore<AsyncUnit>.TrySetResult(state.Source);
                            stateTuple.Dispose();
                            return false;
                        }
                    }
                    catch (Exception ex)
                    {
                        TrySetExceptionOrCanceled<AsyncUnit>(state.Source, ex);
                        stateTuple.Dispose();
                        return false;
                    }
                }
                else
                {
                    if (data.ContinuationTask.Status == LuminTaskStatus.Pending)
                        return true;

                    try
                    {
                        data.ContinuationTask.GetAwaiter().GetResult();
                        LuminTaskSourceCore<AsyncUnit>.TrySetResult(state.Source);
                    }
                    catch (Exception ex)
                    {
                        TrySetExceptionOrCanceled<AsyncUnit>(state.Source, ex);
                    }

                    stateTuple.Dispose();
                    return false;
                }

                return true;
            }
        }

        private static class ContinueWithAsyncFuncPromise<T>
        {
            [StructLayout(LayoutKind.Sequential)]
            private struct StateData
            {
                public Func<LuminTask<T>> Continuation;
                public LuminTask<T> ContinuationTask;
                public bool WaitingForContinuation;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static LuminTask<T> Create(LuminTask task, Func<LuminTask<T>> continuation, PlayerLoopTiming loopTiming = PlayerLoopTiming.DotNet)
            {
                var core = LuminTaskSourceCore<T>.Create();
                var stateData = new StateData
                {
                    Continuation = continuation,
                    WaitingForContinuation = false
                };
                var stateTuple = StateTuple.Create(stateData);
                var state = new LuminTaskState(core, CancellationToken.None, task, stateTuple);

                PlayerLoopHelper.AddAction(loopTiming, state, MoveNext, &MoveNext);
                return new LuminTask<T>(LuminTaskSourceCore<T>.MethodTableAutoDisposePtr, core, core->Id);
            }

            public static bool MoveNext(in LuminTaskState state)
            {
                var innerTask = (LuminTask)state.State;
                var stateTuple = Unsafe.As<StateTuple<StateData>>(state.StateTuple);
                ref var data = ref stateTuple.Item;

                if (!data.WaitingForContinuation)
                {
                    if (innerTask.Status == LuminTaskStatus.Pending)
                        return true;

                    try
                    {
                        innerTask.GetAwaiter().GetResult();
                        data.ContinuationTask = data.Continuation();
                        data.WaitingForContinuation = true;

                        if (data.ContinuationTask.Status != LuminTaskStatus.Pending)
                        {
                            var result = data.ContinuationTask.GetAwaiter().GetResult();
                            LuminTaskSourceCore<T>.TrySetResult(state.Source, result);
                            stateTuple.Dispose();
                            return false;
                        }
                    }
                    catch (Exception ex)
                    {
                        TrySetExceptionOrCanceled<T>(state.Source, ex);
                        stateTuple.Dispose();
                        return false;
                    }
                }
                else
                {
                    if (data.ContinuationTask.Status == LuminTaskStatus.Pending)
                        return true;

                    try
                    {
                        var result = data.ContinuationTask.GetAwaiter().GetResult();
                        LuminTaskSourceCore<T>.TrySetResult(state.Source, result);
                    }
                    catch (Exception ex)
                    {
                        TrySetExceptionOrCanceled<T>(state.Source, ex);
                    }

                    stateTuple.Dispose();
                    return false;
                }

                return true;
            }
        }

        #endregion

        #region 转换为协程

        public static IEnumerator ToCoroutine<T>(this LuminTask<T> task, Action<T> onCompleted = null, Action<Exception> onError = null)
        {
            while (task.Status == LuminTaskStatus.Pending)
            {
                yield return null;
            }

            if (task.Status == LuminTaskStatus.Succeeded)
            {
                try
                {
                    var result = task.GetAwaiter().GetResult();
                    onCompleted?.Invoke(result);
                }
                catch (Exception ex)
                {
                    onError?.Invoke(ex);
                }
            }
            else if (task.Status == LuminTaskStatus.Faulted)
            {
                try
                {
                    task.GetAwaiter().GetResult();
                }
                catch (Exception ex)
                {
                    onError?.Invoke(ex);
                }
            }
            else if (task.Status == LuminTaskStatus.Canceled)
            {
                onError?.Invoke(new OperationCanceledException());
            }
        }

        public static IEnumerator ToCoroutine(this LuminTask task, Action onCompleted = null, Action<Exception> onError = null)
        {
            while (task.Status == LuminTaskStatus.Pending)
            {
                yield return null;
            }

            if (task.Status == LuminTaskStatus.Succeeded)
            {
                try
                {
                    task.GetAwaiter().GetResult();
                    onCompleted?.Invoke();
                }
                catch (Exception ex)
                {
                    onError?.Invoke(ex);
                }
            }
            else if (task.Status == LuminTaskStatus.Faulted)
            {
                try
                {
                    task.GetAwaiter().GetResult();
                }
                catch (Exception ex)
                {
                    onError?.Invoke(ex);
                }
            }
            else if (task.Status == LuminTaskStatus.Canceled)
            {
                onError?.Invoke(new OperationCanceledException());
            }
        }

        #endregion

    }

    public static class LuminTaskSafeExtensions
    {
        #region 辅助方法

        public static async LuminTask<(bool IsCanceled, T Result)> SuppressCancellationThrow<T>(this LuminTask<T> task)
        {
            try
            {
                var result = await task;
                return (false, result);
            }
            catch (OperationCanceledException)
            {
                return (true, default);
            }
        }

        public static async LuminTask<bool> SuppressCancellationThrow(this LuminTask task)
        {
            try
            {
                await task;
                return false;
            }
            catch (OperationCanceledException)
            {
                return true;
            }
        }

        #endregion

        public static LuminTask GetAwaiter(this LuminTask[] tasks)
        {
            return LuminTask.WhenAll(tasks).GetAwaiter();
        }

        public static LuminTask GetAwaiter(this IEnumerable<LuminTask> tasks)
        {
            return LuminTask.WhenAll(tasks).GetAwaiter();
        }

        public static LuminTask<T[]> GetAwaiter<T>(this LuminTask<T>[] tasks)
        {
            return LuminTask.WhenAll(tasks).GetAwaiter();
        }

        public static LuminTask<T[]> GetAwaiter<T>(this IEnumerable<LuminTask<T>> tasks)
        {
            return LuminTask.WhenAll(tasks).GetAwaiter();
        }

        public static LuminTask<(T1, T2)> GetAwaiter<T1, T2>(this (LuminTask<T1> task1, LuminTask<T2> task2) tasks)
        {
            return LuminTask.WhenAll(tasks.task1, tasks.task2).GetAwaiter();
        }

        public static LuminTask<(T1, T2, T3)> GetAwaiter<T1, T2, T3>(this (LuminTask<T1> task1, LuminTask<T2> task2, LuminTask<T3> task3) tasks)
        {
            return LuminTask.WhenAll(tasks.task1, tasks.task2, tasks.task3).GetAwaiter();
        }

        public static LuminTask<(T1, T2, T3, T4)> GetAwaiter<T1, T2, T3, T4>(this (LuminTask<T1> task1, LuminTask<T2> task2, LuminTask<T3> task3, LuminTask<T4> task4) tasks)
        {
            return LuminTask.WhenAll(tasks.task1, tasks.task2, tasks.task3, tasks.task4).GetAwaiter();
        }

        public static LuminTask<(T1, T2, T3, T4, T5)> GetAwaiter<T1, T2, T3, T4, T5>(this (LuminTask<T1> task1, LuminTask<T2> task2, LuminTask<T3> task3, LuminTask<T4> task4, LuminTask<T5> task5) tasks)
        {
            return LuminTask.WhenAll(tasks.task1, tasks.task2, tasks.task3, tasks.task4, tasks.task5).GetAwaiter();
        }

        public static LuminTask<(T1, T2, T3, T4, T5, T6)> GetAwaiter<T1, T2, T3, T4, T5, T6>(this (LuminTask<T1> task1, LuminTask<T2> task2, LuminTask<T3> task3, LuminTask<T4> task4, LuminTask<T5> task5, LuminTask<T6> task6) tasks)
        {
            return LuminTask.WhenAll(tasks.task1, tasks.task2, tasks.task3, tasks.task4, tasks.task5, tasks.task6).GetAwaiter();
        }

        public static LuminTask<(T1, T2, T3, T4, T5, T6, T7)> GetAwaiter<T1, T2, T3, T4, T5, T6, T7>(this (LuminTask<T1> task1, LuminTask<T2> task2, LuminTask<T3> task3, LuminTask<T4> task4, LuminTask<T5> task5, LuminTask<T6> task6, LuminTask<T7> task7) tasks)
        {
            return LuminTask.WhenAll(tasks.task1, tasks.task2, tasks.task3, tasks.task4, tasks.task5, tasks.task6, tasks.task7).GetAwaiter();
        }

        public static LuminTask<(T1, T2, T3, T4, T5, T6, T7, T8)> GetAwaiter<T1, T2, T3, T4, T5, T6, T7, T8>(this (LuminTask<T1> task1, LuminTask<T2> task2, LuminTask<T3> task3, LuminTask<T4> task4, LuminTask<T5> task5, LuminTask<T6> task6, LuminTask<T7> task7, LuminTask<T8> task8) tasks)
        {
            return LuminTask.WhenAll(tasks.task1, tasks.task2, tasks.task3, tasks.task4, tasks.task5, tasks.task6, tasks.task7, tasks.task8).GetAwaiter();
        }

        public static LuminTask<(T1, T2, T3, T4, T5, T6, T7, T8, T9)> GetAwaiter<T1, T2, T3, T4, T5, T6, T7, T8, T9>(this (LuminTask<T1> task1, LuminTask<T2> task2, LuminTask<T3> task3, LuminTask<T4> task4, LuminTask<T5> task5, LuminTask<T6> task6, LuminTask<T7> task7, LuminTask<T8> task8, LuminTask<T9> task9) tasks)
        {
            return LuminTask.WhenAll(tasks.task1, tasks.task2, tasks.task3, tasks.task4, tasks.task5, tasks.task6, tasks.task7, tasks.task8, tasks.task9).GetAwaiter();
        }

        public static LuminTask<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10)> GetAwaiter<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(this (LuminTask<T1> task1, LuminTask<T2> task2, LuminTask<T3> task3, LuminTask<T4> task4, LuminTask<T5> task5, LuminTask<T6> task6, LuminTask<T7> task7, LuminTask<T8> task8, LuminTask<T9> task9, LuminTask<T10> task10) tasks)
        {
            return LuminTask.WhenAll(tasks.task1, tasks.task2, tasks.task3, tasks.task4, tasks.task5, tasks.task6, tasks.task7, tasks.task8, tasks.task9, tasks.task10).GetAwaiter();
        }

        public static LuminTask<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11)> GetAwaiter<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(this (LuminTask<T1> task1, LuminTask<T2> task2, LuminTask<T3> task3, LuminTask<T4> task4, LuminTask<T5> task5, LuminTask<T6> task6, LuminTask<T7> task7, LuminTask<T8> task8, LuminTask<T9> task9, LuminTask<T10> task10, LuminTask<T11> task11) tasks)
        {
            return LuminTask.WhenAll(tasks.task1, tasks.task2, tasks.task3, tasks.task4, tasks.task5, tasks.task6, tasks.task7, tasks.task8, tasks.task9, tasks.task10, tasks.task11).GetAwaiter();
        }

        public static LuminTask<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12)> GetAwaiter<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(this (LuminTask<T1> task1, LuminTask<T2> task2, LuminTask<T3> task3, LuminTask<T4> task4, LuminTask<T5> task5, LuminTask<T6> task6, LuminTask<T7> task7, LuminTask<T8> task8, LuminTask<T9> task9, LuminTask<T10> task10, LuminTask<T11> task11, LuminTask<T12> task12) tasks)
        {
            return LuminTask.WhenAll(tasks.task1, tasks.task2, tasks.task3, tasks.task4, tasks.task5, tasks.task6, tasks.task7, tasks.task8, tasks.task9, tasks.task10, tasks.task11, tasks.task12).GetAwaiter();
        }

        public static LuminTask<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13)> GetAwaiter<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(this (LuminTask<T1> task1, LuminTask<T2> task2, LuminTask<T3> task3, LuminTask<T4> task4, LuminTask<T5> task5, LuminTask<T6> task6, LuminTask<T7> task7, LuminTask<T8> task8, LuminTask<T9> task9, LuminTask<T10> task10, LuminTask<T11> task11, LuminTask<T12> task12, LuminTask<T13> task13) tasks)
        {
            return LuminTask.WhenAll(tasks.task1, tasks.task2, tasks.task3, tasks.task4, tasks.task5, tasks.task6, tasks.task7, tasks.task8, tasks.task9, tasks.task10, tasks.task11, tasks.task12, tasks.task13).GetAwaiter();
        }

        public static LuminTask<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14)> GetAwaiter<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(this (LuminTask<T1> task1, LuminTask<T2> task2, LuminTask<T3> task3, LuminTask<T4> task4, LuminTask<T5> task5, LuminTask<T6> task6, LuminTask<T7> task7, LuminTask<T8> task8, LuminTask<T9> task9, LuminTask<T10> task10, LuminTask<T11> task11, LuminTask<T12> task12, LuminTask<T13> task13, LuminTask<T14> task14) tasks)
        {
            return LuminTask.WhenAll(tasks.task1, tasks.task2, tasks.task3, tasks.task4, tasks.task5, tasks.task6, tasks.task7, tasks.task8, tasks.task9, tasks.task10, tasks.task11, tasks.task12, tasks.task13, tasks.task14).GetAwaiter();
        }

        public static LuminTask<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15)> GetAwaiter<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(this (LuminTask<T1> task1, LuminTask<T2> task2, LuminTask<T3> task3, LuminTask<T4> task4, LuminTask<T5> task5, LuminTask<T6> task6, LuminTask<T7> task7, LuminTask<T8> task8, LuminTask<T9> task9, LuminTask<T10> task10, LuminTask<T11> task11, LuminTask<T12> task12, LuminTask<T13> task13, LuminTask<T14> task14, LuminTask<T15> task15) tasks)
        {
            return LuminTask.WhenAll(tasks.task1, tasks.task2, tasks.task3, tasks.task4, tasks.task5, tasks.task6, tasks.task7, tasks.task8, tasks.task9, tasks.task10, tasks.task11, tasks.task12, tasks.task13, tasks.task14, tasks.task15).GetAwaiter();
        }

        public static LuminTask GetAwaiter(this (LuminTask task1, LuminTask task2) tasks)
        {
            return LuminTask.WhenAll(tasks.task1, tasks.task2).GetAwaiter();
        }

        public static LuminTask GetAwaiter(this (LuminTask task1, LuminTask task2, LuminTask task3) tasks)
        {
            return LuminTask.WhenAll(tasks.task1, tasks.task2, tasks.task3).GetAwaiter();
        }

        public static LuminTask GetAwaiter(this (LuminTask task1, LuminTask task2, LuminTask task3, LuminTask task4) tasks)
        {
            return LuminTask.WhenAll(tasks.task1, tasks.task2, tasks.task3, tasks.task4).GetAwaiter();
        }

        public static LuminTask GetAwaiter(this (LuminTask task1, LuminTask task2, LuminTask task3, LuminTask task4, LuminTask task5) tasks)
        {
            return LuminTask.WhenAll(tasks.task1, tasks.task2, tasks.task3, tasks.task4, tasks.task5).GetAwaiter();
        }

        public static LuminTask GetAwaiter(this (LuminTask task1, LuminTask task2, LuminTask task3, LuminTask task4, LuminTask task5, LuminTask task6) tasks)
        {
            return LuminTask.WhenAll(tasks.task1, tasks.task2, tasks.task3, tasks.task4, tasks.task5, tasks.task6).GetAwaiter();
        }

        public static LuminTask GetAwaiter(this (LuminTask task1, LuminTask task2, LuminTask task3, LuminTask task4, LuminTask task5, LuminTask task6, LuminTask task7) tasks)
        {
            return LuminTask.WhenAll(tasks.task1, tasks.task2, tasks.task3, tasks.task4, tasks.task5, tasks.task6, tasks.task7).GetAwaiter();
        }

        public static LuminTask GetAwaiter(this (LuminTask task1, LuminTask task2, LuminTask task3, LuminTask task4, LuminTask task5, LuminTask task6, LuminTask task7, LuminTask task8) tasks)
        {
            return LuminTask.WhenAll(tasks.task1, tasks.task2, tasks.task3, tasks.task4, tasks.task5, tasks.task6, tasks.task7, tasks.task8).GetAwaiter();
        }

        public static LuminTask GetAwaiter(this (LuminTask task1, LuminTask task2, LuminTask task3, LuminTask task4, LuminTask task5, LuminTask task6, LuminTask task7, LuminTask task8, LuminTask task9) tasks)
        {
            return LuminTask.WhenAll(tasks.task1, tasks.task2, tasks.task3, tasks.task4, tasks.task5, tasks.task6, tasks.task7, tasks.task8, tasks.task9).GetAwaiter();
        }

        public static LuminTask GetAwaiter(this (LuminTask task1, LuminTask task2, LuminTask task3, LuminTask task4, LuminTask task5, LuminTask task6, LuminTask task7, LuminTask task8, LuminTask task9, LuminTask task10) tasks)
        {
            return LuminTask.WhenAll(tasks.task1, tasks.task2, tasks.task3, tasks.task4, tasks.task5, tasks.task6, tasks.task7, tasks.task8, tasks.task9, tasks.task10).GetAwaiter();
        }

        public static LuminTask GetAwaiter(this (LuminTask task1, LuminTask task2, LuminTask task3, LuminTask task4, LuminTask task5, LuminTask task6, LuminTask task7, LuminTask task8, LuminTask task9, LuminTask task10, LuminTask task11) tasks)
        {
            return LuminTask.WhenAll(tasks.task1, tasks.task2, tasks.task3, tasks.task4, tasks.task5, tasks.task6, tasks.task7, tasks.task8, tasks.task9, tasks.task10, tasks.task11).GetAwaiter();
        }

        public static LuminTask GetAwaiter(this (LuminTask task1, LuminTask task2, LuminTask task3, LuminTask task4, LuminTask task5, LuminTask task6, LuminTask task7, LuminTask task8, LuminTask task9, LuminTask task10, LuminTask task11, LuminTask task12) tasks)
        {
            return LuminTask.WhenAll(tasks.task1, tasks.task2, tasks.task3, tasks.task4, tasks.task5, tasks.task6, tasks.task7, tasks.task8, tasks.task9, tasks.task10, tasks.task11, tasks.task12).GetAwaiter();
        }

        public static LuminTask GetAwaiter(this (LuminTask task1, LuminTask task2, LuminTask task3, LuminTask task4, LuminTask task5, LuminTask task6, LuminTask task7, LuminTask task8, LuminTask task9, LuminTask task10, LuminTask task11, LuminTask task12, LuminTask task13) tasks)
        {
            return LuminTask.WhenAll(tasks.task1, tasks.task2, tasks.task3, tasks.task4, tasks.task5, tasks.task6, tasks.task7, tasks.task8, tasks.task9, tasks.task10, tasks.task11, tasks.task12, tasks.task13).GetAwaiter();
        }

        public static LuminTask GetAwaiter(this (LuminTask task1, LuminTask task2, LuminTask task3, LuminTask task4, LuminTask task5, LuminTask task6, LuminTask task7, LuminTask task8, LuminTask task9, LuminTask task10, LuminTask task11, LuminTask task12, LuminTask task13, LuminTask task14) tasks)
        {
            return LuminTask.WhenAll(tasks.task1, tasks.task2, tasks.task3, tasks.task4, tasks.task5, tasks.task6, tasks.task7, tasks.task8, tasks.task9, tasks.task10, tasks.task11, tasks.task12, tasks.task13, tasks.task14).GetAwaiter();
        }

        public static LuminTask GetAwaiter(this (LuminTask task1, LuminTask task2, LuminTask task3, LuminTask task4, LuminTask task5, LuminTask task6, LuminTask task7, LuminTask task8, LuminTask task9, LuminTask task10, LuminTask task11, LuminTask task12, LuminTask task13, LuminTask task14, LuminTask task15) tasks)
        {
            return LuminTask.WhenAll(tasks.task1, tasks.task2, tasks.task3, tasks.task4, tasks.task5, tasks.task6, tasks.task7, tasks.task8, tasks.task9, tasks.task10, tasks.task11, tasks.task12, tasks.task13, tasks.task14, tasks.task15).GetAwaiter();
        }

    }

}
