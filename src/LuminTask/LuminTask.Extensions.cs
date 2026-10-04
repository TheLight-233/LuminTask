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
    public static class LuminTaskExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ValueTask AsValueTask(this LuminTask task) => task;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ValueTask<T> AsValueTask<T>(this LuminTask<T> task)
        {
            // Same wiring as the `in`-ref overload: kept explicit so IL2CPP does not instantiate
            // ValueTaskSource<T> for every closed LuminTask<T>.
            if (task._handle.IsNull())
            {
                return new ValueTask<T>(task._result!);
            }

            return new ValueTask<T>(new ValueTaskSource<T>(task._handle), 0);
        }

        // ----------------------------------------------------------------------------------
        // LuminTask<T> member-lowering. `Id` and `SourceOnCompleted` touch nothing but the handle,
        // so they used to live on the closed type and were therefore materialised for every
        // LuminTask<T> closure (a few hundred of them). As generic extension methods they are
        // instantiated per T only at the call sites that actually use them instead. `Status` stays
        // an instance member: it is called from dozens of places, so lowering it would only move
        // the instantiation from the type table to the generic method table.
        // ----------------------------------------------------------------------------------

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static short GetId<T>(this in LuminTask<T> task) => (short)(task._handle.SlotRaw() - 1);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SourceOnCompleted<T>(this in LuminTask<T> task, Action<object> continuation, object state)
        {
            if (task._handle.IsNull())
            {
                continuation(state);
            }
            else
            {
                LuminTaskCoreSource.OnCompleted(task._handle, continuation, state);
            }
        }

        #region Task conversion

        public static LuminTask<T> AsLuminTask<T>(this Task<T> task, CancellationToken cancellationToken = default)
        {
            if (task is null) throw new ArgumentNullException(nameof(task));
            if (task.IsCompleted) return ConvertCompletedTask(task, cancellationToken);

            var handle = LuminTask.RentSlot();
            var state = StateTuple.Create((handle, cancellationToken));
            task.ContinueWith(
                static (completed, boxedState) => CompleteTaskConversion(completed, boxedState),
                state,
                CancellationToken.None,
                TaskContinuationOptions.ExecuteSynchronously,
                TaskScheduler.Default);

            return new LuminTask<T>(handle);
        }

        public static LuminTask AsLuminTask(this Task task, CancellationToken cancellationToken = default)
        {
            if (task is null) throw new ArgumentNullException(nameof(task));
            if (task.IsCompleted) return ConvertCompletedTask(task, cancellationToken);

            var handle = LuminTask.RentSlot();
            var state = StateTuple.Create((handle, cancellationToken));
            task.ContinueWith(
                static (completed, boxedState) => CompleteTaskConversion(completed, boxedState),
                state,
                CancellationToken.None,
                TaskContinuationOptions.ExecuteSynchronously,
                TaskScheduler.Default);

            return new LuminTask(handle);
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
            var state = (StateTuple<(LuminTaskHandle, CancellationToken)>)boxedState!;
            var handle = state.Item.Item1;
            var fallbackToken = state.Item.Item2;
            state.Dispose();

            try
            {
                var result = task.GetAwaiter().GetResult();
                LuminTaskResultBridge.TrySetResult(handle, in result);
            }
            catch (OperationCanceledException canceled)
            {
                LuminTaskCoreSource.TrySetCanceled(handle, SelectCancellationToken(canceled, fallbackToken));
            }
            catch (Exception exception)
            {
                LuminTaskCoreSource.TrySetException(handle, exception);
            }
        }

        private static void CompleteTaskConversion(Task task, object? boxedState)
        {
            var state = (StateTuple<(LuminTaskHandle, CancellationToken)>)boxedState!;
            var handle = state.Item.Item1;
            var fallbackToken = state.Item.Item2;
            state.Dispose();

            try
            {
                task.GetAwaiter().GetResult();
                LuminTaskCoreSource.TrySetResult(handle);
            }
            catch (OperationCanceledException canceled)
            {
                LuminTaskCoreSource.TrySetCanceled(handle, SelectCancellationToken(canceled, fallbackToken));
            }
            catch (Exception exception)
            {
                LuminTaskCoreSource.TrySetException(handle, exception);
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
        private static void TrySetExceptionOrCanceled(LuminTaskHandle handle, Exception exception)
        {
            if (exception is OperationCanceledException canceled)
                LuminTaskCoreSource.TrySetCanceled(handle, canceled.CancellationToken);
            else
                LuminTaskCoreSource.TrySetException(handle, exception);
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

        // 句柄版排空：进入 Timeout 的内层任务已确认 Pending，只保留句柄即可在放弃等待后
        // 仍然消费掉它，让它的槽位（与池化结果容器）在完成时被释放。
        private static void Drain(LuminTaskHandle handle) => Drain(LuminTaskSource.ToTask(handle));

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

        #region Cancellation support

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
            private static readonly MoveNext MoveNextAction = MoveNext;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static LuminTask Create(LuminTask task, CancellationToken cancellationToken)
            {
                var handle = LuminTask.RentSlot();
                var state = new LuminTaskState(handle, cancellationToken, task);

                PlayerLoopHelper.AddAction(PlayerLoopTiming.DotNet, state, MoveNextAction);
                return new LuminTask(handle);
            }

            public static bool MoveNext(in LuminTaskState state)
            {
                var innerTask = (LuminTask)state.State!;

                if (state.CancellationToken.IsCancellationRequested)
                {
                    Drain(innerTask);
                    LuminTaskCoreSource.TrySetCanceled(state.Handle, state.CancellationToken);
                    return false;
                }

                if (innerTask.Status != LuminTaskStatus.Pending)
                {
                    try
                    {
                        innerTask.GetAwaiter().GetResult();
                        LuminTaskCoreSource.TrySetResult(state.Handle);
                    }
                    catch (Exception ex)
                    {
                        TrySetExceptionOrCanceled(state.Handle, ex);
                    }
                    return false;
                }

                return true;
            }
        }

        private static class AttachExternalCancellationPromise<T>
        {
            private static readonly MoveNext MoveNextAction = MoveNext;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static LuminTask<T> Create(LuminTask<T> task, CancellationToken cancellationToken)
            {
                var handle = LuminTask.RentSlot();
                var state = new LuminTaskState(handle, cancellationToken, task);

                PlayerLoopHelper.AddAction(PlayerLoopTiming.DotNet, state, MoveNextAction);
                return new LuminTask<T>(handle);
            }

            public static bool MoveNext(in LuminTaskState state)
            {
                var innerTask = (LuminTask<T>)state.State!;

                if (state.CancellationToken.IsCancellationRequested)
                {
                    Drain(innerTask);
                    LuminTaskCoreSource.TrySetCanceled(state.Handle, state.CancellationToken);
                    return false;
                }

                if (innerTask.Status != LuminTaskStatus.Pending)
                {
                    try
                    {
                        var result = innerTask.GetAwaiter().GetResult();
                        LuminTaskResultBridge.TrySetResult(state.Handle, in result);
                    }
                    catch (Exception ex)
                    {
                        TrySetExceptionOrCanceled(state.Handle, ex);
                    }
                    return false;
                }

                return true;
            }
        }

        #endregion

        #region Timeout support

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

        // Timing state shared by both Timeout promises. Declared once at this scope so the generic
        // promise does not carry a second, per-T copy of the same two fields.
        [StructLayout(LayoutKind.Sequential)]
        private struct TimeoutStateData
        {
            public long StartTimestamp;
            public long TimeoutTimestampTicks;
        }

        private static class TimeoutPromise
        {
            private static readonly MoveNext MoveNextAction = MoveNext;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static LuminTask Create(LuminTask task, TimeSpan timeout, CancellationToken cancellationToken)
            {
                var handle = LuminTask.RentSlot();

                var stateData = new TimeoutStateData
                {
                    StartTimestamp = Stopwatch.GetTimestamp(),
                    TimeoutTimestampTicks = ToStopwatchTicks(timeout)
                };

                var stateTuple = StateTuple.Create((task._handle, stateData));
                var state = new LuminTaskState(handle, cancellationToken, stateTuple);

                PlayerLoopHelper.AddAction(PlayerLoopTiming.DotNet, state, MoveNextAction);
                return new LuminTask(handle);
            }

            public static bool MoveNext(in LuminTaskState state)
            {
                var stateTuple = Unsafe.As<StateTuple<(LuminTaskHandle, TimeoutStateData)>>(state.State!);
                if (stateTuple == null) return false;

                var innerHandle = stateTuple.Item.Item1;
                var data = stateTuple.Item.Item2;

                if (state.CancellationToken.IsCancellationRequested)
                {
                    Drain(innerHandle);
                    LuminTaskCoreSource.TrySetCanceled(state.Handle, state.CancellationToken);
                    stateTuple.Dispose();
                    return false;
                }

                if (Stopwatch.GetTimestamp() - data.StartTimestamp >= data.TimeoutTimestampTicks)
                {
                    Drain(innerHandle);
                    LuminTaskCoreSource.TrySetException(state.Handle, new TimeoutException());
                    stateTuple.Dispose();
                    return false;
                }

                if (LuminTaskCoreSource.GetStatus(innerHandle) != LuminTaskStatus.Pending)
                {
                    try
                    {
                        LuminTaskSource.ToTask(innerHandle).GetAwaiter().GetResult();
                        LuminTaskCoreSource.TrySetResult(state.Handle);
                    }
                    catch (Exception ex)
                    {
                        TrySetExceptionOrCanceled(state.Handle, ex);
                    }
                    stateTuple.Dispose();
                    return false;
                }

                return true;
            }
        }

        private static class TimeoutPromise<T>
        {
            private static readonly MoveNext MoveNextAction = MoveNext;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static LuminTask<T> Create(LuminTask<T> task, TimeSpan timeout, CancellationToken cancellationToken)
            {
                var handle = LuminTask.RentSlot();

                var stateData = new TimeoutStateData
                {
                    StartTimestamp = Stopwatch.GetTimestamp(),
                    TimeoutTimestampTicks = ToStopwatchTicks(timeout)
                };

                var stateTuple = StateTuple.Create((task._handle, stateData));
                var state = new LuminTaskState(handle, cancellationToken, stateTuple);

                PlayerLoopHelper.AddAction(PlayerLoopTiming.DotNet, state, MoveNextAction);
                return new LuminTask<T>(handle);
            }

            public static bool MoveNext(in LuminTaskState state)
            {
                var stateTuple = Unsafe.As<StateTuple<(LuminTaskHandle, TimeoutStateData)>>(state.State!);
                if (stateTuple == null) return false;

                var innerHandle = stateTuple.Item.Item1;
                var data = stateTuple.Item.Item2;

                if (state.CancellationToken.IsCancellationRequested)
                {
                    Drain(innerHandle);
                    LuminTaskCoreSource.TrySetCanceled(state.Handle, state.CancellationToken);
                    stateTuple.Dispose();
                    return false;
                }

                if (Stopwatch.GetTimestamp() - data.StartTimestamp >= data.TimeoutTimestampTicks)
                {
                    Drain(innerHandle);
                    LuminTaskCoreSource.TrySetException(state.Handle, new TimeoutException());
                    stateTuple.Dispose();
                    return false;
                }

                if (LuminTaskCoreSource.GetStatus(innerHandle) != LuminTaskStatus.Pending)
                {
                    try
                    {
                        // 结果仍由泛型方法从槽位读出，句柄状态里不保存 T，也不装箱。
                        var result = LuminTaskSource.ToTask<T>(innerHandle).GetAwaiter().GetResult();
                        LuminTaskResultBridge.TrySetResult(state.Handle, in result);
                    }
                    catch (Exception ex)
                    {
                        TrySetExceptionOrCanceled(state.Handle, ex);
                    }
                    stateTuple.Dispose();
                    return false;
                }

                return true;
            }
        }

        #endregion

        #region Forget (ignore result)

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
                // Forget discards the result, so the non-generic block (which exits through
                // LuminTaskCoreSource.ConsumeVoid and returns the pooled result container on the way)
                // covers LuminTask<T> as well. Sharing it drops the per-T ForgetBlock<T> generic
                // instantiation entirely.
                ForgetBlock.Schedule(awaiter, null);
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
                // See the note on the parameterless overload: Forget never reads the result, so the
                // non-generic block serves LuminTask<T> too.
                ForgetBlock.Schedule(awaiter, exceptionHandler);
            }
        }

        #endregion

        #region ContinueWith (chaining)

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
            private static readonly MoveNext MoveNextAction = MoveNext;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static LuminTask Create(LuminTask<T> task, Action<T> continuation, PlayerLoopTiming loopTiming = PlayerLoopTiming.DotNet)
            {
                var handle = LuminTask.RentSlot();
                var stateTuple = StateTuple.Create((task, continuation));
                var state = new LuminTaskState(handle, CancellationToken.None, stateTuple);

                PlayerLoopHelper.AddAction(loopTiming, state, MoveNextAction);
                return new LuminTask(handle);
            }

            public static bool MoveNext(in LuminTaskState state)
            {
                var stateTuple = Unsafe.As<StateTuple<(LuminTask<T>, Action<T>)>>(state.State!);
                var innerTask = stateTuple.Item.Item1;
                var continuation = stateTuple.Item.Item2;

                if (innerTask.Status == LuminTaskStatus.Pending)
                    return true;

                try
                {
                    var result = innerTask.GetAwaiter().GetResult();
                    continuation(result);
                    LuminTaskCoreSource.TrySetResult(state.Handle);
                }
                catch (Exception ex)
                {
                    TrySetExceptionOrCanceled(state.Handle, ex);
                }

                stateTuple.Dispose();
                return false;
            }
        }

        private static class ContinueWithFuncPromise<T, TR>
        {
            private static readonly MoveNext MoveNextAction = MoveNext;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static LuminTask<TR> Create(LuminTask<T> task, Func<T, TR> continuation, PlayerLoopTiming loopTiming = PlayerLoopTiming.DotNet)
            {
                var handle = LuminTask.RentSlot();
                var stateTuple = StateTuple.Create((task, continuation));
                var state = new LuminTaskState(handle, CancellationToken.None, stateTuple);

                PlayerLoopHelper.AddAction(loopTiming, state, MoveNextAction);
                return new LuminTask<TR>(handle);
            }

            public static bool MoveNext(in LuminTaskState state)
            {
                var stateTuple = Unsafe.As<StateTuple<(LuminTask<T>, Func<T, TR>)>>(state.State!);
                var innerTask = stateTuple.Item.Item1;
                var continuation = stateTuple.Item.Item2;

                if (innerTask.Status == LuminTaskStatus.Pending)
                    return true;

                try
                {
                    var result = innerTask.GetAwaiter().GetResult();
                    var continuationResult = continuation(result);
                    LuminTaskResultBridge.TrySetResult(state.Handle, in continuationResult);
                }
                catch (Exception ex)
                {
                    TrySetExceptionOrCanceled(state.Handle, ex);
                }

                stateTuple.Dispose();
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

            private static readonly MoveNext MoveNextAction = MoveNext;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static LuminTask Create(LuminTask<T> task, Func<T, LuminTask> continuation, PlayerLoopTiming loopTiming = PlayerLoopTiming.DotNet)
            {
                var handle = LuminTask.RentSlot();
                var stateData = new StateData
                {
                    Continuation = continuation,
                    WaitingForContinuation = false
                };
                var stateTuple = StateTuple.Create((task, stateData));
                var state = new LuminTaskState(handle, CancellationToken.None, stateTuple);

                PlayerLoopHelper.AddAction(loopTiming, state, MoveNextAction);
                return new LuminTask(handle);
            }

            public static bool MoveNext(in LuminTaskState state)
            {
                var stateTuple = Unsafe.As<StateTuple<(LuminTask<T>, StateData)>>(state.State!);
                var innerTask = stateTuple.Item.Item1;
                ref var data = ref stateTuple.Item.Item2;

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
                            LuminTaskCoreSource.TrySetResult(state.Handle);
                            stateTuple.Dispose();
                            return false;
                        }
                    }
                    catch (Exception ex)
                    {
                        TrySetExceptionOrCanceled(state.Handle, ex);
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
                        LuminTaskCoreSource.TrySetResult(state.Handle);
                    }
                    catch (Exception ex)
                    {
                        TrySetExceptionOrCanceled(state.Handle, ex);
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

            private static readonly MoveNext MoveNextAction = MoveNext;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static LuminTask<TR> Create(LuminTask<T> task, Func<T, LuminTask<TR>> continuation, PlayerLoopTiming loopTiming = PlayerLoopTiming.DotNet)
            {
                var handle = LuminTask.RentSlot();
                var stateData = new StateData
                {
                    Continuation = continuation,
                    WaitingForContinuation = false
                };
                var stateTuple = StateTuple.Create((task, stateData));
                var state = new LuminTaskState(handle, CancellationToken.None, stateTuple);

                PlayerLoopHelper.AddAction(loopTiming, state, MoveNextAction);
                return new LuminTask<TR>(handle);
            }

            public static bool MoveNext(in LuminTaskState state)
            {
                var stateTuple = Unsafe.As<StateTuple<(LuminTask<T>, StateData)>>(state.State!);
                var innerTask = stateTuple.Item.Item1;
                ref var data = ref stateTuple.Item.Item2;

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
                            LuminTaskResultBridge.TrySetResult(state.Handle, in finalResult);
                            stateTuple.Dispose();
                            return false;
                        }
                    }
                    catch (Exception ex)
                    {
                        TrySetExceptionOrCanceled(state.Handle, ex);
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
                        LuminTaskResultBridge.TrySetResult(state.Handle, in finalResult);
                    }
                    catch (Exception ex)
                    {
                        TrySetExceptionOrCanceled(state.Handle, ex);
                    }

                    stateTuple.Dispose();
                    return false;
                }

                return true;
            }
        }

        private static class ContinueWithActionPromise
        {
            private static readonly MoveNext MoveNextAction = MoveNext;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static LuminTask Create(LuminTask task, Action continuation, PlayerLoopTiming loopTiming = PlayerLoopTiming.DotNet)
            {
                var handle = LuminTask.RentSlot();
                var stateTuple = StateTuple.Create((task, continuation));
                var state = new LuminTaskState(handle, CancellationToken.None, stateTuple);

                PlayerLoopHelper.AddAction(loopTiming, state, MoveNextAction);
                return new LuminTask(handle);
            }

            public static bool MoveNext(in LuminTaskState state)
            {
                var stateTuple = Unsafe.As<StateTuple<(LuminTask, Action)>>(state.State!);
                var innerTask = stateTuple.Item.Item1;
                var continuation = stateTuple.Item.Item2;

                if (innerTask.Status == LuminTaskStatus.Pending)
                    return true;

                try
                {
                    innerTask.GetAwaiter().GetResult();
                    continuation();
                    LuminTaskCoreSource.TrySetResult(state.Handle);
                }
                catch (Exception ex)
                {
                    TrySetExceptionOrCanceled(state.Handle, ex);
                }

                stateTuple.Dispose();
                return false;
            }
        }

        private static class ContinueWithFuncPromise<T>
        {
            private static readonly MoveNext MoveNextAction = MoveNext;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static LuminTask<T> Create(LuminTask task, Func<T> continuation, PlayerLoopTiming loopTiming = PlayerLoopTiming.DotNet)
            {
                var handle = LuminTask.RentSlot();
                var stateTuple = StateTuple.Create((task, continuation));
                var state = new LuminTaskState(handle, CancellationToken.None, stateTuple);

                PlayerLoopHelper.AddAction(loopTiming, state, MoveNextAction);
                return new LuminTask<T>(handle);
            }

            public static bool MoveNext(in LuminTaskState state)
            {
                var stateTuple = Unsafe.As<StateTuple<(LuminTask, Func<T>)>>(state.State!);
                var innerTask = stateTuple.Item.Item1;
                var continuation = stateTuple.Item.Item2;

                if (innerTask.Status == LuminTaskStatus.Pending)
                    return true;

                try
                {
                    innerTask.GetAwaiter().GetResult();
                    var result = continuation();
                    LuminTaskResultBridge.TrySetResult(state.Handle, in result);
                }
                catch (Exception ex)
                {
                    TrySetExceptionOrCanceled(state.Handle, ex);
                }

                stateTuple.Dispose();
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

            private static readonly MoveNext MoveNextAction = MoveNext;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static LuminTask Create(LuminTask task, Func<LuminTask> continuation, PlayerLoopTiming loopTiming = PlayerLoopTiming.DotNet)
            {
                var handle = LuminTask.RentSlot();
                var stateData = new StateData
                {
                    Continuation = continuation,
                    WaitingForContinuation = false
                };
                var stateTuple = StateTuple.Create((task, stateData));
                var state = new LuminTaskState(handle, CancellationToken.None, stateTuple);

                PlayerLoopHelper.AddAction(loopTiming, state, MoveNextAction);
                return new LuminTask(handle);
            }

            public static bool MoveNext(in LuminTaskState state)
            {
                var stateTuple = Unsafe.As<StateTuple<(LuminTask, StateData)>>(state.State!);
                var innerTask = stateTuple.Item.Item1;
                ref var data = ref stateTuple.Item.Item2;

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
                            LuminTaskCoreSource.TrySetResult(state.Handle);
                            stateTuple.Dispose();
                            return false;
                        }
                    }
                    catch (Exception ex)
                    {
                        TrySetExceptionOrCanceled(state.Handle, ex);
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
                        LuminTaskCoreSource.TrySetResult(state.Handle);
                    }
                    catch (Exception ex)
                    {
                        TrySetExceptionOrCanceled(state.Handle, ex);
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

            private static readonly MoveNext MoveNextAction = MoveNext;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static LuminTask<T> Create(LuminTask task, Func<LuminTask<T>> continuation, PlayerLoopTiming loopTiming = PlayerLoopTiming.DotNet)
            {
                var handle = LuminTask.RentSlot();
                var stateData = new StateData
                {
                    Continuation = continuation,
                    WaitingForContinuation = false
                };
                var stateTuple = StateTuple.Create((task, stateData));
                var state = new LuminTaskState(handle, CancellationToken.None, stateTuple);

                PlayerLoopHelper.AddAction(loopTiming, state, MoveNextAction);
                return new LuminTask<T>(handle);
            }

            public static bool MoveNext(in LuminTaskState state)
            {
                var stateTuple = Unsafe.As<StateTuple<(LuminTask, StateData)>>(state.State!);
                var innerTask = stateTuple.Item.Item1;
                ref var data = ref stateTuple.Item.Item2;

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
                            LuminTaskResultBridge.TrySetResult(state.Handle, in result);
                            stateTuple.Dispose();
                            return false;
                        }
                    }
                    catch (Exception ex)
                    {
                        TrySetExceptionOrCanceled(state.Handle, ex);
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
                        LuminTaskResultBridge.TrySetResult(state.Handle, in result);
                    }
                    catch (Exception ex)
                    {
                        TrySetExceptionOrCanceled(state.Handle, ex);
                    }

                    stateTuple.Dispose();
                    return false;
                }

                return true;
            }
        }

        #endregion

        #region Convert to coroutine

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
        #region Helpers

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

    }

}
