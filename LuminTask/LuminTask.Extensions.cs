using System;
using System.Collections;
using System.Collections.Generic;
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
        #region Task 转换

        public static LuminTask<T> AsLuminTask<T>(this Task<T> task, CancellationToken cancellationToken = default)
        {
            if (task.IsCompleted)
            {
                if (task.IsCanceled)
                {
                    return LuminTask.FromCanceled<T>(cancellationToken);
                }
                else if (task.IsFaulted)
                {
                    return LuminTask.FromException<T>(task.Exception?.InnerException ?? task.Exception);
                }
                else
                {
                    return LuminTask.FromResult(task.Result);
                }
            }

            var core = LuminTaskSourceCore<T>.Create();

            task.ContinueWith(t =>
            {
                if (t.IsCanceled)
                {
                    LuminTaskSourceCore<T>.TrySetCanceled(core);
                }
                else if (t.IsFaulted)
                {
                    LuminTaskSourceCore<T>.TrySetException(core, t.Exception?.InnerException ?? t.Exception);
                }
                else
                {
                    LuminTaskSourceCore<T>.TrySetResult(core, t.Result);
                }
            }, TaskScheduler.Default);

            return new LuminTask<T>(LuminTaskSourceCore<T>.MethodTable, core, core->Id);
        }

        public static LuminTask AsLuminTask(this Task task, CancellationToken cancellationToken = default)
        {
            if (task.IsCompleted)
            {
                if (task.IsCanceled)
                {
                    return LuminTask.FromCanceled(cancellationToken);
                }
                else if (task.IsFaulted)
                {
                    return LuminTask.FromException(task.Exception?.InnerException ?? task.Exception);
                }
                else
                {
                    return LuminTask.CompletedTask();
                }
            }

            var core = LuminTaskSourceCore<AsyncUnit>.Create();

            task.ContinueWith(t =>
            {
                if (t.IsCanceled)
                {
                    LuminTaskSourceCore<AsyncUnit>.TrySetCanceled(core);
                }
                else if (t.IsFaulted)
                {
                    LuminTaskSourceCore<AsyncUnit>.TrySetException(core, t.Exception?.InnerException ?? t.Exception);
                }
                else
                {
                    LuminTaskSourceCore<AsyncUnit>.TrySetResult(core);
                }
            }, TaskScheduler.Default);

            return new LuminTask(LuminTaskSourceCore<AsyncUnit>.MethodTable, core, core->Id);
        }

        public static Task<T> AsTask<T>(this LuminTask<T> task)
        {
            var tcs = new TaskCompletionSource<T>();

            task.GetAwaiter().OnCompleted(() =>
            {
                try
                {
                    var result = task.GetAwaiter().GetResult();
                    tcs.TrySetResult(result);
                }
                catch (Exception ex)
                {
                    tcs.TrySetException(ex);
                }
            });

            return tcs.Task;
        }

        public static Task AsTask(this LuminTask task)
        {
            var tcs = new TaskCompletionSource<bool>();

            task.GetAwaiter().OnCompleted(() =>
            {
                try
                {
                    task.GetAwaiter().GetResult();
                    tcs.TrySetResult(true);
                }
                catch (Exception ex)
                {
                    tcs.TrySetException(ex);
                }
            });

            return tcs.Task;
        }

        #endregion

        #region 取消支持

        /// <summary>
        /// 附加外部取消令牌
        /// </summary>
        public static LuminTask AttachExternalCancellation(this LuminTask task, CancellationToken cancellationToken)
        {
            if (!cancellationToken.CanBeCanceled)
                return task;

            if (cancellationToken.IsCancellationRequested)
                return LuminTask.FromCanceled(cancellationToken);

            return AttachExternalCancellationPromise.Create(task, cancellationToken);
        }

        /// <summary>
        /// 附加外部取消令牌
        /// </summary>
        public static LuminTask<T> AttachExternalCancellation<T>(this LuminTask<T> task, CancellationToken cancellationToken)
        {
            if (!cancellationToken.CanBeCanceled)
                return task;

            if (cancellationToken.IsCancellationRequested)
                return LuminTask.FromCanceled<T>(cancellationToken);

            return AttachExternalCancellationPromise<T>.Create(task, cancellationToken);
        }

        private static class AttachExternalCancellationPromise
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static LuminTask Create(LuminTask task, CancellationToken cancellationToken)
            {
                var core = LuminTaskSourceCore<AsyncUnit>.Create();
                var state = new LuminTaskState(core, cancellationToken, task);

                PlayerLoopHelper.AddAction(PlayerLoopTiming.Update, state, &MoveNext);
                return new LuminTask(LuminTaskSourceCore<AsyncUnit>.MethodTable, core, core->Id);
            }

            public static bool MoveNext(in LuminTaskState state)
            {
                var innerTask = (LuminTask)state.State;

                if (state.CancellationToken.IsCancellationRequested)
                {
                    LuminTaskSourceCore<AsyncUnit>.TrySetCanceled(state.Source);
                    LuminTaskSourceCore<AsyncUnit>.Dispose(state.Source);
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
                        LuminTaskSourceCore<AsyncUnit>.TrySetException(state.Source, ex);
                    }
                    LuminTaskSourceCore<AsyncUnit>.Dispose(state.Source);
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

                PlayerLoopHelper.AddAction(PlayerLoopTiming.Update, state, &MoveNext);
                return new LuminTask<T>(LuminTaskSourceCore<T>.MethodTable, core, core->Id);
            }

            public static bool MoveNext(in LuminTaskState state)
            {
                var innerTask = (LuminTask<T>)state.State;

                if (state.CancellationToken.IsCancellationRequested)
                {
                    LuminTaskSourceCore<T>.TrySetCanceled(state.Source);
                    LuminTaskSourceCore<T>.Dispose(state.Source);
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
                        LuminTaskSourceCore<T>.TrySetException(state.Source, ex);
                    }
                    LuminTaskSourceCore<T>.Dispose(state.Source);
                    return false;
                }

                return true;
            }
        }

        #endregion

        #region 超时支持

        /// <summary>
        /// 添加超时
        /// </summary>
        public static LuminTask Timeout(this LuminTask task, TimeSpan timeout, CancellationToken cancellationToken = default)
        {
            return TimeoutPromise.Create(task, timeout, cancellationToken);
        }

        /// <summary>
        /// 添加超时
        /// </summary>
        public static LuminTask<T> Timeout<T>(this LuminTask<T> task, TimeSpan timeout, CancellationToken cancellationToken = default)
        {
            return TimeoutPromise<T>.Create(task, timeout, cancellationToken);
        }

        private static class TimeoutPromise
        {
            [StructLayout(LayoutKind.Sequential)]
            private struct TimeoutStateData
            {
                public long StartTicks;
                public long TimeoutTicks;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static LuminTask Create(LuminTask task, TimeSpan timeout, CancellationToken cancellationToken)
            {
                var core = LuminTaskSourceCore<AsyncUnit>.Create();
                
                var stateData = new TimeoutStateData
                {
                    StartTicks = DateTime.UtcNow.Ticks,
                    TimeoutTicks = timeout.Ticks
                };
                
                var stateTuple = StateTuple.Create(stateData);
                var state = new LuminTaskState(core, cancellationToken, task, stateTuple);

                PlayerLoopHelper.AddAction(PlayerLoopTiming.Update, state, &MoveNext);
                return new LuminTask(LuminTaskSourceCore<AsyncUnit>.MethodTable, core, core->Id);
            }

            public static bool MoveNext(in LuminTaskState state)
            {
                var innerTask = (LuminTask)state.State;
                var stateTuple = Unsafe.As<StateTuple<TimeoutStateData>>(state.StateTuple);
                if (stateTuple == null) return false;

                var data = stateTuple.Item1;

                if (state.CancellationToken.IsCancellationRequested)
                {
                    LuminTaskSourceCore<AsyncUnit>.TrySetCanceled(state.Source);
                    LuminTaskSourceCore<AsyncUnit>.Dispose(state.Source);
                    stateTuple.Dispose();
                    return false;
                }

                // 检查超时
                if (DateTime.UtcNow.Ticks - data.StartTicks > data.TimeoutTicks)
                {
                    LuminTaskSourceCore<AsyncUnit>.TrySetException(state.Source, new TimeoutException());
                    LuminTaskSourceCore<AsyncUnit>.Dispose(state.Source);
                    stateTuple.Dispose();
                    return false;
                }

                // 检查任务是否完成
                if (innerTask.Status != LuminTaskStatus.Pending)
                {
                    try
                    {
                        innerTask.GetAwaiter().GetResult();
                        LuminTaskSourceCore<AsyncUnit>.TrySetResult(state.Source);
                    }
                    catch (Exception ex)
                    {
                        LuminTaskSourceCore<AsyncUnit>.TrySetException(state.Source, ex);
                    }
                    LuminTaskSourceCore<AsyncUnit>.Dispose(state.Source);
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
                public long StartTicks;
                public long TimeoutTicks;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static LuminTask<T> Create(LuminTask<T> task, TimeSpan timeout, CancellationToken cancellationToken)
            {
                var core = LuminTaskSourceCore<T>.Create();
                
                var stateData = new TimeoutStateData
                {
                    StartTicks = DateTime.UtcNow.Ticks,
                    TimeoutTicks = timeout.Ticks
                };
                
                var stateTuple = StateTuple.Create(stateData);
                var state = new LuminTaskState(core, cancellationToken, task, stateTuple);

                PlayerLoopHelper.AddAction(PlayerLoopTiming.Update, state, &MoveNext);
                return new LuminTask<T>(LuminTaskSourceCore<T>.MethodTable, core, core->Id);
            }

            public static bool MoveNext(in LuminTaskState state)
            {
                var innerTask = (LuminTask<T>)state.State;
                var stateTuple = Unsafe.As<StateTuple<TimeoutStateData>>(state.StateTuple);
                if (stateTuple == null) return false;

                var data = stateTuple.Item1;

                if (state.CancellationToken.IsCancellationRequested)
                {
                    LuminTaskSourceCore<T>.TrySetCanceled(state.Source);
                    LuminTaskSourceCore<T>.Dispose(state.Source);
                    stateTuple.Dispose();
                    return false;
                }

                // 检查超时
                if (DateTime.UtcNow.Ticks - data.StartTicks > data.TimeoutTicks)
                {
                    LuminTaskSourceCore<T>.TrySetException(state.Source, new TimeoutException());
                    LuminTaskSourceCore<T>.Dispose(state.Source);
                    stateTuple.Dispose();
                    return false;
                }

                // 检查任务是否完成
                if (innerTask.Status != LuminTaskStatus.Pending)
                {
                    try
                    {
                        var result = innerTask.GetAwaiter().GetResult();
                        LuminTaskSourceCore<T>.TrySetResult(state.Source, result);
                    }
                    catch (Exception ex)
                    {
                        LuminTaskSourceCore<T>.TrySetException(state.Source, ex);
                    }
                    LuminTaskSourceCore<T>.Dispose(state.Source);
                    stateTuple.Dispose();
                    return false;
                }

                return true;
            }
        }

        #endregion

        #region Forget (忽略结果)

        /// <summary>
        /// 忽略任务结果，仅记录异常
        /// </summary>
        public static void Forget(this LuminTask task)
        {
            var awaiter = task.GetAwaiter();
            
            if (awaiter.IsCompleted)
            {
                try
                {
                    awaiter.GetResult();
                }
                catch (Exception)
                {
                    // 忽略异常
                }
            }
            else
            {
                awaiter.OnCompleted(() =>
                {
                    try
                    {
                        awaiter.GetResult();
                    }
                    catch (Exception)
                    {
                        // 忽略异常
                    }
                });
            }
        }

        /// <summary>
        /// 忽略任务结果，使用自定义异常处理
        /// </summary>
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
                try
                {
                    awaiter.GetResult();
                }
                catch (Exception ex)
                {
                    exceptionHandler(ex);
                }
            }
            else
            {
                awaiter.OnCompleted(() =>
                {
                    try
                    {
                        awaiter.GetResult();
                    }
                    catch (Exception ex)
                    {
                        exceptionHandler(ex);
                    }
                });
            }
        }

        /// <summary>
        /// 忽略任务结果，仅记录异常
        /// </summary>
        public static void Forget<T>(this LuminTask<T> task)
        {
            var awaiter = task.GetAwaiter();
            
            if (awaiter.IsCompleted)
            {
                try
                {
                    awaiter.GetResult();
                }
                catch (Exception)
                {
                    // 忽略异常
                }
            }
            else
            {
                awaiter.OnCompleted(() =>
                {
                    try
                    {
                        awaiter.GetResult();
                    }
                    catch (Exception)
                    {
                        // 忽略异常
                    }
                });
            }
        }

        /// <summary>
        /// 忽略任务结果，使用自定义异常处理
        /// </summary>
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
                try
                {
                    awaiter.GetResult();
                }
                catch (Exception ex)
                {
                    exceptionHandler(ex);
                }
            }
            else
            {
                awaiter.OnCompleted(() =>
                {
                    try
                    {
                        awaiter.GetResult();
                    }
                    catch (Exception ex)
                    {
                        exceptionHandler(ex);
                    }
                });
            }
        }

        #endregion

        #region ContinueWith (链式调用)

        // LuminTask<T> -> Action<T> -> LuminTask
        public static LuminTask ContinueWith<T>(this LuminTask<T> task, Action<T> continuation, PlayerLoopTiming loopTiming = PlayerLoopTiming.DotNet)
        {
            return ContinueWithActionPromise<T>.Create(task, continuation, loopTiming);
        }

        // LuminTask<T> -> Func<T, TR> -> LuminTask<TR>
        public static LuminTask<TR> ContinueWith<T, TR>(this LuminTask<T> task, Func<T, TR> continuation, PlayerLoopTiming loopTiming = PlayerLoopTiming.DotNet)
        {
            return ContinueWithFuncPromise<T, TR>.Create(task, continuation, loopTiming);
        }

        // LuminTask<T> -> Func<T, LuminTask> -> LuminTask
        public static LuminTask ContinueWith<T>(this LuminTask<T> task, Func<T, LuminTask> continuation, PlayerLoopTiming loopTiming = PlayerLoopTiming.DotNet)
        {
            return ContinueWithAsyncActionPromise<T>.Create(task, continuation, loopTiming);
        }

        // LuminTask<T> -> Func<T, LuminTask<TR>> -> LuminTask<TR>
        public static LuminTask<TR> ContinueWith<T, TR>(this LuminTask<T> task, Func<T, LuminTask<TR>> continuation, PlayerLoopTiming loopTiming = PlayerLoopTiming.DotNet)
        {
            return ContinueWithAsyncFuncPromise<T, TR>.Create(task, continuation, loopTiming);
        }

        // LuminTask -> Action -> LuminTask
        public static LuminTask ContinueWith(this LuminTask task, Action continuation, PlayerLoopTiming loopTiming = PlayerLoopTiming.DotNet)
        {
            return ContinueWithActionPromise.Create(task, continuation, loopTiming);
        }

        // LuminTask -> Func<T> -> LuminTask<T>
        public static LuminTask<T> ContinueWith<T>(this LuminTask task, Func<T> continuation, PlayerLoopTiming loopTiming = PlayerLoopTiming.DotNet)
        {
            return ContinueWithFuncPromise<T>.Create(task, continuation, loopTiming);
        }

        // LuminTask -> Func<LuminTask> -> LuminTask
        public static LuminTask ContinueWith(this LuminTask task, Func<LuminTask> continuation, PlayerLoopTiming loopTiming = PlayerLoopTiming.DotNet)
        {
            return ContinueWithAsyncActionPromise.Create(task, continuation, loopTiming);
        }

        // LuminTask -> Func<LuminTask<T>> -> LuminTask<T>
        public static LuminTask<T> ContinueWith<T>(this LuminTask task, Func<LuminTask<T>> continuation, PlayerLoopTiming loopTiming = PlayerLoopTiming.DotNet)
        {
            return ContinueWithAsyncFuncPromise<T>.Create(task, continuation, loopTiming);
        }

        // LuminTask<T> -> Action<T> -> LuminTask
        private static class ContinueWithActionPromise<T>
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static LuminTask Create(LuminTask<T> task, Action<T> continuation, PlayerLoopTiming loopTiming = PlayerLoopTiming.DotNet)
            {
                var core = LuminTaskSourceCore<AsyncUnit>.Create();
                var stateTuple = StateTuple.Create(continuation);
                var state = new LuminTaskState(core, CancellationToken.None, task, stateTuple);

                PlayerLoopHelper.AddAction(loopTiming, state, &MoveNext);
                return new LuminTask(LuminTaskSourceCore<AsyncUnit>.MethodTable, core, core->Id);
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
                    continuation.Item1(result);
                    LuminTaskSourceCore<AsyncUnit>.TrySetResult(state.Source);
                }
                catch (Exception ex)
                {
                    LuminTaskSourceCore<AsyncUnit>.TrySetException(state.Source, ex);
                }

                LuminTaskSourceCore<AsyncUnit>.Dispose(state.Source);
                continuation.Dispose();
                return false;
            }
        }

        // LuminTask<T> -> Func<T, TR> -> LuminTask<TR>
        private static class ContinueWithFuncPromise<T, TR>
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static LuminTask<TR> Create(LuminTask<T> task, Func<T, TR> continuation, PlayerLoopTiming loopTiming = PlayerLoopTiming.DotNet)
            {
                var core = LuminTaskSourceCore<TR>.Create();
                var stateTuple = StateTuple.Create(continuation);
                var state = new LuminTaskState(core, CancellationToken.None, task, stateTuple);

                PlayerLoopHelper.AddAction(loopTiming, state, &MoveNext);
                return new LuminTask<TR>(LuminTaskSourceCore<TR>.MethodTable, core, core->Id);
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
                    var continuationResult = continuation.Item1(result);
                    LuminTaskSourceCore<TR>.TrySetResult(state.Source, continuationResult);
                }
                catch (Exception ex)
                {
                    LuminTaskSourceCore<TR>.TrySetException(state.Source, ex);
                }

                LuminTaskSourceCore<TR>.Dispose(state.Source);
                continuation.Dispose();
                return false;
            }
        }

        // LuminTask<T> -> Func<T, LuminTask> -> LuminTask
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

                PlayerLoopHelper.AddAction(loopTiming, state, &MoveNext);
                return new LuminTask(LuminTaskSourceCore<AsyncUnit>.MethodTable, core, core->Id);
            }

            public static bool MoveNext(in LuminTaskState state)
            {
                var innerTask = (LuminTask<T>)state.State;
                var stateTuple = Unsafe.As<StateTuple<StateData>>(state.StateTuple);
                ref var data = ref stateTuple.Item1;

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
                            LuminTaskSourceCore<AsyncUnit>.Dispose(state.Source);
                            stateTuple.Dispose();
                            return false;
                        }
                    }
                    catch (Exception ex)
                    {
                        LuminTaskSourceCore<AsyncUnit>.TrySetException(state.Source, ex);
                        LuminTaskSourceCore<AsyncUnit>.Dispose(state.Source);
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
                        LuminTaskSourceCore<AsyncUnit>.TrySetException(state.Source, ex);
                    }

                    LuminTaskSourceCore<AsyncUnit>.Dispose(state.Source);
                    stateTuple.Dispose();
                    return false;
                }

                return true;
            }
        }

        // LuminTask<T> -> Func<T, LuminTask<TR>> -> LuminTask<TR>
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

                PlayerLoopHelper.AddAction(loopTiming, state, &MoveNext);
                return new LuminTask<TR>(LuminTaskSourceCore<TR>.MethodTable, core, core->Id);
            }

            public static bool MoveNext(in LuminTaskState state)
            {
                var innerTask = (LuminTask<T>)state.State;
                var stateTuple = Unsafe.As<StateTuple<StateData>>(state.StateTuple);
                ref var data = ref stateTuple.Item1;

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
                            LuminTaskSourceCore<TR>.Dispose(state.Source);
                            stateTuple.Dispose();
                            return false;
                        }
                    }
                    catch (Exception ex)
                    {
                        LuminTaskSourceCore<TR>.TrySetException(state.Source, ex);
                        LuminTaskSourceCore<TR>.Dispose(state.Source);
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
                        LuminTaskSourceCore<TR>.TrySetException(state.Source, ex);
                    }

                    LuminTaskSourceCore<TR>.Dispose(state.Source);
                    stateTuple.Dispose();
                    return false;
                }

                return true;
            }
        }

        // LuminTask -> Action -> LuminTask
        private static class ContinueWithActionPromise
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static LuminTask Create(LuminTask task, Action continuation, PlayerLoopTiming loopTiming = PlayerLoopTiming.DotNet)
            {
                var core = LuminTaskSourceCore<AsyncUnit>.Create();
                var stateTuple = StateTuple.Create(continuation);
                var state = new LuminTaskState(core, CancellationToken.None, task, stateTuple);

                PlayerLoopHelper.AddAction(loopTiming, state, &MoveNext);
                return new LuminTask(LuminTaskSourceCore<AsyncUnit>.MethodTable, core, core->Id);
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
                    continuation.Item1();
                    LuminTaskSourceCore<AsyncUnit>.TrySetResult(state.Source);
                }
                catch (Exception ex)
                {
                    LuminTaskSourceCore<AsyncUnit>.TrySetException(state.Source, ex);
                }

                LuminTaskSourceCore<AsyncUnit>.Dispose(state.Source);
                continuation.Dispose();
                return false;
            }
        }

        // LuminTask -> Func<T> -> LuminTask<T>
        private static class ContinueWithFuncPromise<T>
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static LuminTask<T> Create(LuminTask task, Func<T> continuation, PlayerLoopTiming loopTiming = PlayerLoopTiming.DotNet)
            {
                var core = LuminTaskSourceCore<T>.Create();
                var stateTuple = StateTuple.Create(continuation);
                var state = new LuminTaskState(core, CancellationToken.None, task, stateTuple);

                PlayerLoopHelper.AddAction(loopTiming, state, &MoveNext);
                return new LuminTask<T>(LuminTaskSourceCore<T>.MethodTable, core, core->Id);
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
                    var result = continuation.Item1();
                    LuminTaskSourceCore<T>.TrySetResult(state.Source, result);
                }
                catch (Exception ex)
                {
                    LuminTaskSourceCore<T>.TrySetException(state.Source, ex);
                }

                LuminTaskSourceCore<T>.Dispose(state.Source);
                continuation.Dispose();
                return false;
            }
        }

        // LuminTask -> Func<LuminTask> -> LuminTask
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

                PlayerLoopHelper.AddAction(loopTiming, state, &MoveNext);
                return new LuminTask(LuminTaskSourceCore<AsyncUnit>.MethodTable, core, core->Id);
            }

            public static bool MoveNext(in LuminTaskState state)
            {
                var innerTask = (LuminTask)state.State;
                var stateTuple = Unsafe.As<StateTuple<StateData>>(state.StateTuple);
                ref var data = ref stateTuple.Item1;

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
                            LuminTaskSourceCore<AsyncUnit>.Dispose(state.Source);
                            stateTuple.Dispose();
                            return false;
                        }
                    }
                    catch (Exception ex)
                    {
                        LuminTaskSourceCore<AsyncUnit>.TrySetException(state.Source, ex);
                        LuminTaskSourceCore<AsyncUnit>.Dispose(state.Source);
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
                        LuminTaskSourceCore<AsyncUnit>.TrySetException(state.Source, ex);
                    }

                    LuminTaskSourceCore<AsyncUnit>.Dispose(state.Source);
                    stateTuple.Dispose();
                    return false;
                }

                return true;
            }
        }

        // LuminTask -> Func<LuminTask<T>> -> LuminTask<T>
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

                PlayerLoopHelper.AddAction(loopTiming, state, &MoveNext);
                return new LuminTask<T>(LuminTaskSourceCore<T>.MethodTable, core, core->Id);
            }

            public static bool MoveNext(in LuminTaskState state)
            {
                var innerTask = (LuminTask)state.State;
                var stateTuple = Unsafe.As<StateTuple<StateData>>(state.StateTuple);
                ref var data = ref stateTuple.Item1;

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
                            LuminTaskSourceCore<T>.Dispose(state.Source);
                            stateTuple.Dispose();
                            return false;
                        }
                    }
                    catch (Exception ex)
                    {
                        LuminTaskSourceCore<T>.TrySetException(state.Source, ex);
                        LuminTaskSourceCore<T>.Dispose(state.Source);
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
                        LuminTaskSourceCore<T>.TrySetException(state.Source, ex);
                    }

                    LuminTaskSourceCore<T>.Dispose(state.Source);
                    stateTuple.Dispose();
                    return false;
                }

                return true;
            }
        }

        #endregion

        #region 转换为协程

        /// <summary>
        /// 将LuminTask转换为Unity协程
        /// </summary>
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

        /// <summary>
        /// 将LuminTask转换为Unity协程
        /// </summary>
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

        /// <summary>
        /// 抑制取消异常，返回是否取消和结果
        /// </summary>
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

        /// <summary>
        /// 抑制取消异常，返回是否取消
        /// </summary>
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
        
        public static LuminTaskAwaiter GetAwaiter(this LuminTask[] tasks)
        {
            return LuminTask.WhenAll(tasks).GetAwaiter();
        }

        public static LuminTaskAwaiter GetAwaiter(this IEnumerable<LuminTask> tasks)
        {
            return LuminTask.WhenAll(tasks).GetAwaiter();
        }

        public static LuminTaskAwaiter<T[]> GetAwaiter<T>(this LuminTask<T>[] tasks)
        {
            return LuminTask.WhenAll(tasks).GetAwaiter();
        }

        public static LuminTaskAwaiter<T[]> GetAwaiter<T>(this IEnumerable<LuminTask<T>> tasks)
        {
            return LuminTask.WhenAll(tasks).GetAwaiter();
        }

        public static LuminTaskAwaiter<(T1, T2)> GetAwaiter<T1, T2>(this (LuminTask<T1> task1, LuminTask<T2> task2) tasks)
        {
            return LuminTask.WhenAll(tasks.task1, tasks.task2).GetAwaiter();
        }

        public static LuminTaskAwaiter<(T1, T2, T3)> GetAwaiter<T1, T2, T3>(this (LuminTask<T1> task1, LuminTask<T2> task2, LuminTask<T3> task3) tasks)
        {
            return LuminTask.WhenAll(tasks.task1, tasks.task2, tasks.task3).GetAwaiter();
        }

        public static LuminTaskAwaiter<(T1, T2, T3, T4)> GetAwaiter<T1, T2, T3, T4>(this (LuminTask<T1> task1, LuminTask<T2> task2, LuminTask<T3> task3, LuminTask<T4> task4) tasks)
        {
            return LuminTask.WhenAll(tasks.task1, tasks.task2, tasks.task3, tasks.task4).GetAwaiter();
        }

        public static LuminTaskAwaiter<(T1, T2, T3, T4, T5)> GetAwaiter<T1, T2, T3, T4, T5>(this (LuminTask<T1> task1, LuminTask<T2> task2, LuminTask<T3> task3, LuminTask<T4> task4, LuminTask<T5> task5) tasks)
        {
            return LuminTask.WhenAll(tasks.task1, tasks.task2, tasks.task3, tasks.task4, tasks.task5).GetAwaiter();
        }

        public static LuminTaskAwaiter<(T1, T2, T3, T4, T5, T6)> GetAwaiter<T1, T2, T3, T4, T5, T6>(this (LuminTask<T1> task1, LuminTask<T2> task2, LuminTask<T3> task3, LuminTask<T4> task4, LuminTask<T5> task5, LuminTask<T6> task6) tasks)
        {
            return LuminTask.WhenAll(tasks.task1, tasks.task2, tasks.task3, tasks.task4, tasks.task5, tasks.task6).GetAwaiter();
        }

        public static LuminTaskAwaiter<(T1, T2, T3, T4, T5, T6, T7)> GetAwaiter<T1, T2, T3, T4, T5, T6, T7>(this (LuminTask<T1> task1, LuminTask<T2> task2, LuminTask<T3> task3, LuminTask<T4> task4, LuminTask<T5> task5, LuminTask<T6> task6, LuminTask<T7> task7) tasks)
        {
            return LuminTask.WhenAll(tasks.task1, tasks.task2, tasks.task3, tasks.task4, tasks.task5, tasks.task6, tasks.task7).GetAwaiter();
        }

        public static LuminTaskAwaiter<(T1, T2, T3, T4, T5, T6, T7, T8)> GetAwaiter<T1, T2, T3, T4, T5, T6, T7, T8>(this (LuminTask<T1> task1, LuminTask<T2> task2, LuminTask<T3> task3, LuminTask<T4> task4, LuminTask<T5> task5, LuminTask<T6> task6, LuminTask<T7> task7, LuminTask<T8> task8) tasks)
        {
            return LuminTask.WhenAll(tasks.task1, tasks.task2, tasks.task3, tasks.task4, tasks.task5, tasks.task6, tasks.task7, tasks.task8).GetAwaiter();
        }

        public static LuminTaskAwaiter<(T1, T2, T3, T4, T5, T6, T7, T8, T9)> GetAwaiter<T1, T2, T3, T4, T5, T6, T7, T8, T9>(this (LuminTask<T1> task1, LuminTask<T2> task2, LuminTask<T3> task3, LuminTask<T4> task4, LuminTask<T5> task5, LuminTask<T6> task6, LuminTask<T7> task7, LuminTask<T8> task8, LuminTask<T9> task9) tasks)
        {
            return LuminTask.WhenAll(tasks.task1, tasks.task2, tasks.task3, tasks.task4, tasks.task5, tasks.task6, tasks.task7, tasks.task8, tasks.task9).GetAwaiter();
        }

        public static LuminTaskAwaiter<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10)> GetAwaiter<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(this (LuminTask<T1> task1, LuminTask<T2> task2, LuminTask<T3> task3, LuminTask<T4> task4, LuminTask<T5> task5, LuminTask<T6> task6, LuminTask<T7> task7, LuminTask<T8> task8, LuminTask<T9> task9, LuminTask<T10> task10) tasks)
        {
            return LuminTask.WhenAll(tasks.task1, tasks.task2, tasks.task3, tasks.task4, tasks.task5, tasks.task6, tasks.task7, tasks.task8, tasks.task9, tasks.task10).GetAwaiter();
        }

        public static LuminTaskAwaiter<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11)> GetAwaiter<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(this (LuminTask<T1> task1, LuminTask<T2> task2, LuminTask<T3> task3, LuminTask<T4> task4, LuminTask<T5> task5, LuminTask<T6> task6, LuminTask<T7> task7, LuminTask<T8> task8, LuminTask<T9> task9, LuminTask<T10> task10, LuminTask<T11> task11) tasks)
        {
            return LuminTask.WhenAll(tasks.task1, tasks.task2, tasks.task3, tasks.task4, tasks.task5, tasks.task6, tasks.task7, tasks.task8, tasks.task9, tasks.task10, tasks.task11).GetAwaiter();
        }

        public static LuminTaskAwaiter<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12)> GetAwaiter<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(this (LuminTask<T1> task1, LuminTask<T2> task2, LuminTask<T3> task3, LuminTask<T4> task4, LuminTask<T5> task5, LuminTask<T6> task6, LuminTask<T7> task7, LuminTask<T8> task8, LuminTask<T9> task9, LuminTask<T10> task10, LuminTask<T11> task11, LuminTask<T12> task12) tasks)
        {
            return LuminTask.WhenAll(tasks.task1, tasks.task2, tasks.task3, tasks.task4, tasks.task5, tasks.task6, tasks.task7, tasks.task8, tasks.task9, tasks.task10, tasks.task11, tasks.task12).GetAwaiter();
        }

        public static LuminTaskAwaiter<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13)> GetAwaiter<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(this (LuminTask<T1> task1, LuminTask<T2> task2, LuminTask<T3> task3, LuminTask<T4> task4, LuminTask<T5> task5, LuminTask<T6> task6, LuminTask<T7> task7, LuminTask<T8> task8, LuminTask<T9> task9, LuminTask<T10> task10, LuminTask<T11> task11, LuminTask<T12> task12, LuminTask<T13> task13) tasks)
        {
            return LuminTask.WhenAll(tasks.task1, tasks.task2, tasks.task3, tasks.task4, tasks.task5, tasks.task6, tasks.task7, tasks.task8, tasks.task9, tasks.task10, tasks.task11, tasks.task12, tasks.task13).GetAwaiter();
        }

        public static LuminTaskAwaiter<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14)> GetAwaiter<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(this (LuminTask<T1> task1, LuminTask<T2> task2, LuminTask<T3> task3, LuminTask<T4> task4, LuminTask<T5> task5, LuminTask<T6> task6, LuminTask<T7> task7, LuminTask<T8> task8, LuminTask<T9> task9, LuminTask<T10> task10, LuminTask<T11> task11, LuminTask<T12> task12, LuminTask<T13> task13, LuminTask<T14> task14) tasks)
        {
            return LuminTask.WhenAll(tasks.task1, tasks.task2, tasks.task3, tasks.task4, tasks.task5, tasks.task6, tasks.task7, tasks.task8, tasks.task9, tasks.task10, tasks.task11, tasks.task12, tasks.task13, tasks.task14).GetAwaiter();
        }

        public static LuminTaskAwaiter<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15)> GetAwaiter<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(this (LuminTask<T1> task1, LuminTask<T2> task2, LuminTask<T3> task3, LuminTask<T4> task4, LuminTask<T5> task5, LuminTask<T6> task6, LuminTask<T7> task7, LuminTask<T8> task8, LuminTask<T9> task9, LuminTask<T10> task10, LuminTask<T11> task11, LuminTask<T12> task12, LuminTask<T13> task13, LuminTask<T14> task14, LuminTask<T15> task15) tasks)
        {
            return LuminTask.WhenAll(tasks.task1, tasks.task2, tasks.task3, tasks.task4, tasks.task5, tasks.task6, tasks.task7, tasks.task8, tasks.task9, tasks.task10, tasks.task11, tasks.task12, tasks.task13, tasks.task14, tasks.task15).GetAwaiter();
        }

        // 非泛型元组的扩展方法
        public static LuminTaskAwaiter GetAwaiter(this (LuminTask task1, LuminTask task2) tasks)
        {
            return LuminTask.WhenAll(tasks.task1, tasks.task2).GetAwaiter();
        }

        public static LuminTaskAwaiter GetAwaiter(this (LuminTask task1, LuminTask task2, LuminTask task3) tasks)
        {
            return LuminTask.WhenAll(tasks.task1, tasks.task2, tasks.task3).GetAwaiter();
        }

        public static LuminTaskAwaiter GetAwaiter(this (LuminTask task1, LuminTask task2, LuminTask task3, LuminTask task4) tasks)
        {
            return LuminTask.WhenAll(tasks.task1, tasks.task2, tasks.task3, tasks.task4).GetAwaiter();
        }

        public static LuminTaskAwaiter GetAwaiter(this (LuminTask task1, LuminTask task2, LuminTask task3, LuminTask task4, LuminTask task5) tasks)
        {
            return LuminTask.WhenAll(tasks.task1, tasks.task2, tasks.task3, tasks.task4, tasks.task5).GetAwaiter();
        }

        public static LuminTaskAwaiter GetAwaiter(this (LuminTask task1, LuminTask task2, LuminTask task3, LuminTask task4, LuminTask task5, LuminTask task6) tasks)
        {
            return LuminTask.WhenAll(tasks.task1, tasks.task2, tasks.task3, tasks.task4, tasks.task5, tasks.task6).GetAwaiter();
        }

        public static LuminTaskAwaiter GetAwaiter(this (LuminTask task1, LuminTask task2, LuminTask task3, LuminTask task4, LuminTask task5, LuminTask task6, LuminTask task7) tasks)
        {
            return LuminTask.WhenAll(tasks.task1, tasks.task2, tasks.task3, tasks.task4, tasks.task5, tasks.task6, tasks.task7).GetAwaiter();
        }

        public static LuminTaskAwaiter GetAwaiter(this (LuminTask task1, LuminTask task2, LuminTask task3, LuminTask task4, LuminTask task5, LuminTask task6, LuminTask task7, LuminTask task8) tasks)
        {
            return LuminTask.WhenAll(tasks.task1, tasks.task2, tasks.task3, tasks.task4, tasks.task5, tasks.task6, tasks.task7, tasks.task8).GetAwaiter();
        }

        public static LuminTaskAwaiter GetAwaiter(this (LuminTask task1, LuminTask task2, LuminTask task3, LuminTask task4, LuminTask task5, LuminTask task6, LuminTask task7, LuminTask task8, LuminTask task9) tasks)
        {
            return LuminTask.WhenAll(tasks.task1, tasks.task2, tasks.task3, tasks.task4, tasks.task5, tasks.task6, tasks.task7, tasks.task8, tasks.task9).GetAwaiter();
        }

        public static LuminTaskAwaiter GetAwaiter(this (LuminTask task1, LuminTask task2, LuminTask task3, LuminTask task4, LuminTask task5, LuminTask task6, LuminTask task7, LuminTask task8, LuminTask task9, LuminTask task10) tasks)
        {
            return LuminTask.WhenAll(tasks.task1, tasks.task2, tasks.task3, tasks.task4, tasks.task5, tasks.task6, tasks.task7, tasks.task8, tasks.task9, tasks.task10).GetAwaiter();
        }

        public static LuminTaskAwaiter GetAwaiter(this (LuminTask task1, LuminTask task2, LuminTask task3, LuminTask task4, LuminTask task5, LuminTask task6, LuminTask task7, LuminTask task8, LuminTask task9, LuminTask task10, LuminTask task11) tasks)
        {
            return LuminTask.WhenAll(tasks.task1, tasks.task2, tasks.task3, tasks.task4, tasks.task5, tasks.task6, tasks.task7, tasks.task8, tasks.task9, tasks.task10, tasks.task11).GetAwaiter();
        }

        public static LuminTaskAwaiter GetAwaiter(this (LuminTask task1, LuminTask task2, LuminTask task3, LuminTask task4, LuminTask task5, LuminTask task6, LuminTask task7, LuminTask task8, LuminTask task9, LuminTask task10, LuminTask task11, LuminTask task12) tasks)
        {
            return LuminTask.WhenAll(tasks.task1, tasks.task2, tasks.task3, tasks.task4, tasks.task5, tasks.task6, tasks.task7, tasks.task8, tasks.task9, tasks.task10, tasks.task11, tasks.task12).GetAwaiter();
        }

        public static LuminTaskAwaiter GetAwaiter(this (LuminTask task1, LuminTask task2, LuminTask task3, LuminTask task4, LuminTask task5, LuminTask task6, LuminTask task7, LuminTask task8, LuminTask task9, LuminTask task10, LuminTask task11, LuminTask task12, LuminTask task13) tasks)
        {
            return LuminTask.WhenAll(tasks.task1, tasks.task2, tasks.task3, tasks.task4, tasks.task5, tasks.task6, tasks.task7, tasks.task8, tasks.task9, tasks.task10, tasks.task11, tasks.task12, tasks.task13).GetAwaiter();
        }

        public static LuminTaskAwaiter GetAwaiter(this (LuminTask task1, LuminTask task2, LuminTask task3, LuminTask task4, LuminTask task5, LuminTask task6, LuminTask task7, LuminTask task8, LuminTask task9, LuminTask task10, LuminTask task11, LuminTask task12, LuminTask task13, LuminTask task14) tasks)
        {
            return LuminTask.WhenAll(tasks.task1, tasks.task2, tasks.task3, tasks.task4, tasks.task5, tasks.task6, tasks.task7, tasks.task8, tasks.task9, tasks.task10, tasks.task11, tasks.task12, tasks.task13, tasks.task14).GetAwaiter();
        }

        public static LuminTaskAwaiter GetAwaiter(this (LuminTask task1, LuminTask task2, LuminTask task3, LuminTask task4, LuminTask task5, LuminTask task6, LuminTask task7, LuminTask task8, LuminTask task9, LuminTask task10, LuminTask task11, LuminTask task12, LuminTask task13, LuminTask task14, LuminTask task15) tasks)
        {
            return LuminTask.WhenAll(tasks.task1, tasks.task2, tasks.task3, tasks.task4, tasks.task5, tasks.task6, tasks.task7, tasks.task8, tasks.task9, tasks.task10, tasks.task11, tasks.task12, tasks.task13, tasks.task14, tasks.task15).GetAwaiter();
        }
        
    }
    
    
    
}