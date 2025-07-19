
using System;
using System.Threading;
using Lumin.Threading.Core;
using Lumin.Threading.Interface;
using Lumin.Threading.Source;
using Lumin.Threading.Tasks.Utility;
using Lumin.Threading.Utility;

namespace Lumin.Threading.Tasks;

public readonly ref partial struct LuminTask
{
    public static LuminTask<(int winArgumentIndex, T1 result1, T2 result2)> WhenAny<T1, T2>(LuminTask<T1> task1, LuminTask<T2> task2)
    {
        return new LuminTask<(int winArgumentIndex, T1 result1, T2 result2)>(new WhenAnyPromise<T1, T2>(task1, task2), 0);
    }

    sealed class WhenAnyPromise<T1, T2> : ILuminTaskSource<(int, T1 result1, T2 result2)>
    {
        int completedCount;
        LuminTaskCompletionSourceCore<(int, T1 result1, T2 result2)> core;

        public WhenAnyPromise(LuminTask<T1> task1, LuminTask<T2> task2)
        {
            TaskTracker.TrackActiveTask(this, 3);

            this.completedCount = 0;
            {
                var awaiter = task1.GetAwaiter();

                if (awaiter.IsCompleted)
                {
                    TryInvokeContinuationT1(this, awaiter);
                }
                else
                {
                    awaiter.SourceOnCompleted(static state =>
                    {
                        using (var t = (StateTuple<WhenAnyPromise<T1, T2>, LuminTaskAwaiter<T1>>)state)
                        {
                            TryInvokeContinuationT1(t.Item1, t.Item2);
                        }
                    }, StateTuple.Create(this, awaiter));
                }
            }
            {
                var awaiter = task2.GetAwaiter();

                if (awaiter.IsCompleted)
                {
                    TryInvokeContinuationT2(this, awaiter);
                }
                else
                {
                    awaiter.SourceOnCompleted(static state =>
                    {
                        using (var t = (StateTuple<WhenAnyPromise<T1, T2>, LuminTaskAwaiter<T2>>)state)
                        {
                            TryInvokeContinuationT2(t.Item1, t.Item2);
                        }
                    }, StateTuple.Create(this, awaiter));
                }
            }
        }

        static void TryInvokeContinuationT1(WhenAnyPromise<T1, T2> self, in LuminTaskAwaiter<T1> awaiter)
        {
            T1 result;
            try
            {
                result = awaiter.GetResult();
            }
            catch (Exception ex)
            {
                self.core.TrySetException(ex);
                return;
            }

            if (Interlocked.Increment(ref self.completedCount) == 1)
            {
                self.core.TrySetResult((0, result, default));
            }
        }

        static void TryInvokeContinuationT2(WhenAnyPromise<T1, T2> self, in LuminTaskAwaiter<T2> awaiter)
        {
            T2 result;
            try
            {
                result = awaiter.GetResult();
            }
            catch (Exception ex)
            {
                self.core.TrySetException(ex);
                return;
            }

            if (Interlocked.Increment(ref self.completedCount) == 1)
            {
                self.core.TrySetResult((1, default, result));
            }
        }


        public (int, T1 result1, T2 result2) GetResult(short token)
        {
            TaskTracker.RemoveTracking(this);
            GC.SuppressFinalize(this);
            return core.GetResult(token);
        }

        public LuminTaskStatus GetStatus(short token)
        {
            return core.GetStatus(token);
        }

        public void OnCompleted(Action<object> continuation, object state, short token)
        {
            core.OnCompleted(continuation, state, token);
        }

        public LuminTaskStatus UnsafeGetStatus()
        {
            return core.UnsafeGetStatus();
        }

        void ILuminTaskSource.GetResult(short token)
        {
            GetResult(token);
        }
    }

    public static LuminTask<(int winArgumentIndex, T1 result1, T2 result2, T3 result3)> WhenAny<T1, T2, T3>(LuminTask<T1> task1, LuminTask<T2> task2, LuminTask<T3> task3)
    {
        return new LuminTask<(int winArgumentIndex, T1 result1, T2 result2, T3 result3)>(new WhenAnyPromise<T1, T2, T3>(task1, task2, task3), 0);
    }

    sealed class WhenAnyPromise<T1, T2, T3> : ILuminTaskSource<(int, T1 result1, T2 result2, T3 result3)>
    {
        int completedCount;
        LuminTaskCompletionSourceCore<(int, T1 result1, T2 result2, T3 result3)> core;

        public WhenAnyPromise(LuminTask<T1> task1, LuminTask<T2> task2, LuminTask<T3> task3)
        {
            TaskTracker.TrackActiveTask(this, 3);

            this.completedCount = 0;
            {
                var awaiter = task1.GetAwaiter();

                if (awaiter.IsCompleted)
                {
                    TryInvokeContinuationT1(this, awaiter);
                }
                else
                {
                    awaiter.SourceOnCompleted(static state =>
                    {
                        using (var t = (StateTuple<WhenAnyPromise<T1, T2, T3>, LuminTaskAwaiter<T1>>)state)
                        {
                            TryInvokeContinuationT1(t.Item1, t.Item2);
                        }
                    }, StateTuple.Create(this, awaiter));
                }
            }
            {
                var awaiter = task2.GetAwaiter();

                if (awaiter.IsCompleted)
                {
                    TryInvokeContinuationT2(this, awaiter);
                }
                else
                {
                    awaiter.SourceOnCompleted(static state =>
                    {
                        using (var t = (StateTuple<WhenAnyPromise<T1, T2, T3>, LuminTaskAwaiter<T2>>)state)
                        {
                            TryInvokeContinuationT2(t.Item1, t.Item2);
                        }
                    }, StateTuple.Create(this, awaiter));
                }
            }
            {
                var awaiter = task3.GetAwaiter();

                if (awaiter.IsCompleted)
                {
                    TryInvokeContinuationT3(this, awaiter);
                }
                else
                {
                    awaiter.SourceOnCompleted(static state =>
                    {
                        using (var t = (StateTuple<WhenAnyPromise<T1, T2, T3>, LuminTaskAwaiter<T3>>)state)
                        {
                            TryInvokeContinuationT3(t.Item1, t.Item2);
                        }
                    }, StateTuple.Create(this, awaiter));
                }
            }
        }

        static void TryInvokeContinuationT1(WhenAnyPromise<T1, T2, T3> self, in LuminTaskAwaiter<T1> awaiter)
        {
            T1 result;
            try
            {
                result = awaiter.GetResult();
            }
            catch (Exception ex)
            {
                self.core.TrySetException(ex);
                return;
            }

            if (Interlocked.Increment(ref self.completedCount) == 1)
            {
                self.core.TrySetResult((0, result, default, default));
            }
        }

        static void TryInvokeContinuationT2(WhenAnyPromise<T1, T2, T3> self, in LuminTaskAwaiter<T2> awaiter)
        {
            T2 result;
            try
            {
                result = awaiter.GetResult();
            }
            catch (Exception ex)
            {
                self.core.TrySetException(ex);
                return;
            }

            if (Interlocked.Increment(ref self.completedCount) == 1)
            {
                self.core.TrySetResult((1, default, result, default));
            }
        }

        static void TryInvokeContinuationT3(WhenAnyPromise<T1, T2, T3> self, in LuminTaskAwaiter<T3> awaiter)
        {
            T3 result;
            try
            {
                result = awaiter.GetResult();
            }
            catch (Exception ex)
            {
                self.core.TrySetException(ex);
                return;
            }

            if (Interlocked.Increment(ref self.completedCount) == 1)
            {
                self.core.TrySetResult((2, default, default, result));
            }
        }


        public (int, T1 result1, T2 result2, T3 result3) GetResult(short token)
        {
            TaskTracker.RemoveTracking(this);
            GC.SuppressFinalize(this);
            return core.GetResult(token);
        }

        public LuminTaskStatus GetStatus(short token)
        {
            return core.GetStatus(token);
        }

        public void OnCompleted(Action<object> continuation, object state, short token)
        {
            core.OnCompleted(continuation, state, token);
        }

        public LuminTaskStatus UnsafeGetStatus()
        {
            return core.UnsafeGetStatus();
        }

        void ILuminTaskSource.GetResult(short token)
        {
            GetResult(token);
        }
    }

    public static LuminTask<(int winArgumentIndex, T1 result1, T2 result2, T3 result3, T4 result4)> WhenAny<T1, T2, T3, T4>(LuminTask<T1> task1, LuminTask<T2> task2, LuminTask<T3> task3, LuminTask<T4> task4)
    {
        return new LuminTask<(int winArgumentIndex, T1 result1, T2 result2, T3 result3, T4 result4)>(new WhenAnyPromise<T1, T2, T3, T4>(task1, task2, task3, task4), 0);
    }

    sealed class WhenAnyPromise<T1, T2, T3, T4> : ILuminTaskSource<(int, T1 result1, T2 result2, T3 result3, T4 result4)>
    {
        int completedCount;
        LuminTaskCompletionSourceCore<(int, T1 result1, T2 result2, T3 result3, T4 result4)> core;

        public WhenAnyPromise(LuminTask<T1> task1, LuminTask<T2> task2, LuminTask<T3> task3, LuminTask<T4> task4)
        {
            TaskTracker.TrackActiveTask(this, 3);

            this.completedCount = 0;
            {
                var awaiter = task1.GetAwaiter();

                if (awaiter.IsCompleted)
                {
                    TryInvokeContinuationT1(this, awaiter);
                }
                else
                {
                    awaiter.SourceOnCompleted(static state =>
                    {
                        using (var t = (StateTuple<WhenAnyPromise<T1, T2, T3, T4>, LuminTaskAwaiter<T1>>)state)
                        {
                            TryInvokeContinuationT1(t.Item1, t.Item2);
                        }
                    }, StateTuple.Create(this, awaiter));
                }
            }
            {
                var awaiter = task2.GetAwaiter();

                if (awaiter.IsCompleted)
                {
                    TryInvokeContinuationT2(this, awaiter);
                }
                else
                {
                    awaiter.SourceOnCompleted(static state =>
                    {
                        using (var t = (StateTuple<WhenAnyPromise<T1, T2, T3, T4>, LuminTaskAwaiter<T2>>)state)
                        {
                            TryInvokeContinuationT2(t.Item1, t.Item2);
                        }
                    }, StateTuple.Create(this, awaiter));
                }
            }
            {
                var awaiter = task3.GetAwaiter();

                if (awaiter.IsCompleted)
                {
                    TryInvokeContinuationT3(this, awaiter);
                }
                else
                {
                    awaiter.SourceOnCompleted(static state =>
                    {
                        using (var t = (StateTuple<WhenAnyPromise<T1, T2, T3, T4>, LuminTaskAwaiter<T3>>)state)
                        {
                            TryInvokeContinuationT3(t.Item1, t.Item2);
                        }
                    }, StateTuple.Create(this, awaiter));
                }
            }
            {
                var awaiter = task4.GetAwaiter();

                if (awaiter.IsCompleted)
                {
                    TryInvokeContinuationT4(this, awaiter);
                }
                else
                {
                    awaiter.SourceOnCompleted(static state =>
                    {
                        using (var t = (StateTuple<WhenAnyPromise<T1, T2, T3, T4>, LuminTaskAwaiter<T4>>)state)
                        {
                            TryInvokeContinuationT4(t.Item1, t.Item2);
                        }
                    }, StateTuple.Create(this, awaiter));
                }
            }
        }

        static void TryInvokeContinuationT1(WhenAnyPromise<T1, T2, T3, T4> self, in LuminTaskAwaiter<T1> awaiter)
        {
            T1 result;
            try
            {
                result = awaiter.GetResult();
            }
            catch (Exception ex)
            {
                self.core.TrySetException(ex);
                return;
            }

            if (Interlocked.Increment(ref self.completedCount) == 1)
            {
                self.core.TrySetResult((0, result, default, default, default));
            }
        }

        static void TryInvokeContinuationT2(WhenAnyPromise<T1, T2, T3, T4> self, in LuminTaskAwaiter<T2> awaiter)
        {
            T2 result;
            try
            {
                result = awaiter.GetResult();
            }
            catch (Exception ex)
            {
                self.core.TrySetException(ex);
                return;
            }

            if (Interlocked.Increment(ref self.completedCount) == 1)
            {
                self.core.TrySetResult((1, default, result, default, default));
            }
        }

        static void TryInvokeContinuationT3(WhenAnyPromise<T1, T2, T3, T4> self, in LuminTaskAwaiter<T3> awaiter)
        {
            T3 result;
            try
            {
                result = awaiter.GetResult();
            }
            catch (Exception ex)
            {
                self.core.TrySetException(ex);
                return;
            }

            if (Interlocked.Increment(ref self.completedCount) == 1)
            {
                self.core.TrySetResult((2, default, default, result, default));
            }
        }

        static void TryInvokeContinuationT4(WhenAnyPromise<T1, T2, T3, T4> self, in LuminTaskAwaiter<T4> awaiter)
        {
            T4 result;
            try
            {
                result = awaiter.GetResult();
            }
            catch (Exception ex)
            {
                self.core.TrySetException(ex);
                return;
            }

            if (Interlocked.Increment(ref self.completedCount) == 1)
            {
                self.core.TrySetResult((3, default, default, default, result));
            }
        }


        public (int, T1 result1, T2 result2, T3 result3, T4 result4) GetResult(short token)
        {
            TaskTracker.RemoveTracking(this);
            GC.SuppressFinalize(this);
            return core.GetResult(token);
        }

        public LuminTaskStatus GetStatus(short token)
        {
            return core.GetStatus(token);
        }

        public void OnCompleted(Action<object> continuation, object state, short token)
        {
            core.OnCompleted(continuation, state, token);
        }

        public LuminTaskStatus UnsafeGetStatus()
        {
            return core.UnsafeGetStatus();
        }

        void ILuminTaskSource.GetResult(short token)
        {
            GetResult(token);
        }
    }

    public static LuminTask<(int winArgumentIndex, T1 result1, T2 result2, T3 result3, T4 result4, T5 result5)> WhenAny<T1, T2, T3, T4, T5>(LuminTask<T1> task1, LuminTask<T2> task2, LuminTask<T3> task3, LuminTask<T4> task4, LuminTask<T5> task5)
    {
        return new LuminTask<(int winArgumentIndex, T1 result1, T2 result2, T3 result3, T4 result4, T5 result5)>(new WhenAnyPromise<T1, T2, T3, T4, T5>(task1, task2, task3, task4, task5), 0);
    }

    sealed class WhenAnyPromise<T1, T2, T3, T4, T5> : ILuminTaskSource<(int, T1 result1, T2 result2, T3 result3, T4 result4, T5 result5)>
    {
        int completedCount;
        LuminTaskCompletionSourceCore<(int, T1 result1, T2 result2, T3 result3, T4 result4, T5 result5)> core;

        public WhenAnyPromise(LuminTask<T1> task1, LuminTask<T2> task2, LuminTask<T3> task3, LuminTask<T4> task4, LuminTask<T5> task5)
        {
            TaskTracker.TrackActiveTask(this, 3);

            this.completedCount = 0;
            {
                var awaiter = task1.GetAwaiter();

                if (awaiter.IsCompleted)
                {
                    TryInvokeContinuationT1(this, awaiter);
                }
                else
                {
                    awaiter.SourceOnCompleted(static state =>
                    {
                        using (var t = (StateTuple<WhenAnyPromise<T1, T2, T3, T4, T5>, LuminTaskAwaiter<T1>>)state)
                        {
                            TryInvokeContinuationT1(t.Item1, t.Item2);
                        }
                    }, StateTuple.Create(this, awaiter));
                }
            }
            {
                var awaiter = task2.GetAwaiter();

                if (awaiter.IsCompleted)
                {
                    TryInvokeContinuationT2(this, awaiter);
                }
                else
                {
                    awaiter.SourceOnCompleted(static state =>
                    {
                        using (var t = (StateTuple<WhenAnyPromise<T1, T2, T3, T4, T5>, LuminTaskAwaiter<T2>>)state)
                        {
                            TryInvokeContinuationT2(t.Item1, t.Item2);
                        }
                    }, StateTuple.Create(this, awaiter));
                }
            }
            {
                var awaiter = task3.GetAwaiter();

                if (awaiter.IsCompleted)
                {
                    TryInvokeContinuationT3(this, awaiter);
                }
                else
                {
                    awaiter.SourceOnCompleted(static state =>
                    {
                        using (var t = (StateTuple<WhenAnyPromise<T1, T2, T3, T4, T5>, LuminTaskAwaiter<T3>>)state)
                        {
                            TryInvokeContinuationT3(t.Item1, t.Item2);
                        }
                    }, StateTuple.Create(this, awaiter));
                }
            }
            {
                var awaiter = task4.GetAwaiter();

                if (awaiter.IsCompleted)
                {
                    TryInvokeContinuationT4(this, awaiter);
                }
                else
                {
                    awaiter.SourceOnCompleted(static state =>
                    {
                        using (var t = (StateTuple<WhenAnyPromise<T1, T2, T3, T4, T5>, LuminTaskAwaiter<T4>>)state)
                        {
                            TryInvokeContinuationT4(t.Item1, t.Item2);
                        }
                    }, StateTuple.Create(this, awaiter));
                }
            }
            {
                var awaiter = task5.GetAwaiter();

                if (awaiter.IsCompleted)
                {
                    TryInvokeContinuationT5(this, awaiter);
                }
                else
                {
                    awaiter.SourceOnCompleted(static state =>
                    {
                        using (var t = (StateTuple<WhenAnyPromise<T1, T2, T3, T4, T5>, LuminTaskAwaiter<T5>>)state)
                        {
                            TryInvokeContinuationT5(t.Item1, t.Item2);
                        }
                    }, StateTuple.Create(this, awaiter));
                }
            }
        }

        static void TryInvokeContinuationT1(WhenAnyPromise<T1, T2, T3, T4, T5> self, in LuminTaskAwaiter<T1> awaiter)
        {
            T1 result;
            try
            {
                result = awaiter.GetResult();
            }
            catch (Exception ex)
            {
                self.core.TrySetException(ex);
                return;
            }

            if (Interlocked.Increment(ref self.completedCount) == 1)
            {
                self.core.TrySetResult((0, result, default, default, default, default));
            }
        }

        static void TryInvokeContinuationT2(WhenAnyPromise<T1, T2, T3, T4, T5> self, in LuminTaskAwaiter<T2> awaiter)
        {
            T2 result;
            try
            {
                result = awaiter.GetResult();
            }
            catch (Exception ex)
            {
                self.core.TrySetException(ex);
                return;
            }

            if (Interlocked.Increment(ref self.completedCount) == 1)
            {
                self.core.TrySetResult((1, default, result, default, default, default));
            }
        }

        static void TryInvokeContinuationT3(WhenAnyPromise<T1, T2, T3, T4, T5> self, in LuminTaskAwaiter<T3> awaiter)
        {
            T3 result;
            try
            {
                result = awaiter.GetResult();
            }
            catch (Exception ex)
            {
                self.core.TrySetException(ex);
                return;
            }

            if (Interlocked.Increment(ref self.completedCount) == 1)
            {
                self.core.TrySetResult((2, default, default, result, default, default));
            }
        }

        static void TryInvokeContinuationT4(WhenAnyPromise<T1, T2, T3, T4, T5> self, in LuminTaskAwaiter<T4> awaiter)
        {
            T4 result;
            try
            {
                result = awaiter.GetResult();
            }
            catch (Exception ex)
            {
                self.core.TrySetException(ex);
                return;
            }

            if (Interlocked.Increment(ref self.completedCount) == 1)
            {
                self.core.TrySetResult((3, default, default, default, result, default));
            }
        }

        static void TryInvokeContinuationT5(WhenAnyPromise<T1, T2, T3, T4, T5> self, in LuminTaskAwaiter<T5> awaiter)
        {
            T5 result;
            try
            {
                result = awaiter.GetResult();
            }
            catch (Exception ex)
            {
                self.core.TrySetException(ex);
                return;
            }

            if (Interlocked.Increment(ref self.completedCount) == 1)
            {
                self.core.TrySetResult((4, default, default, default, default, result));
            }
        }


        public (int, T1 result1, T2 result2, T3 result3, T4 result4, T5 result5) GetResult(short token)
        {
            TaskTracker.RemoveTracking(this);
            GC.SuppressFinalize(this);
            return core.GetResult(token);
        }

        public LuminTaskStatus GetStatus(short token)
        {
            return core.GetStatus(token);
        }

        public void OnCompleted(Action<object> continuation, object state, short token)
        {
            core.OnCompleted(continuation, state, token);
        }

        public LuminTaskStatus UnsafeGetStatus()
        {
            return core.UnsafeGetStatus();
        }

        void ILuminTaskSource.GetResult(short token)
        {
            GetResult(token);
        }
    }

    public static LuminTask<(int winArgumentIndex, T1 result1, T2 result2, T3 result3, T4 result4, T5 result5, T6 result6)> WhenAny<T1, T2, T3, T4, T5, T6>(LuminTask<T1> task1, LuminTask<T2> task2, LuminTask<T3> task3, LuminTask<T4> task4, LuminTask<T5> task5, LuminTask<T6> task6)
    {
        return new LuminTask<(int winArgumentIndex, T1 result1, T2 result2, T3 result3, T4 result4, T5 result5, T6 result6)>(new WhenAnyPromise<T1, T2, T3, T4, T5, T6>(task1, task2, task3, task4, task5, task6), 0);
    }

    sealed class WhenAnyPromise<T1, T2, T3, T4, T5, T6> : ILuminTaskSource<(int, T1 result1, T2 result2, T3 result3, T4 result4, T5 result5, T6 result6)>
    {
        int completedCount;
        LuminTaskCompletionSourceCore<(int, T1 result1, T2 result2, T3 result3, T4 result4, T5 result5, T6 result6)> core;

        public WhenAnyPromise(LuminTask<T1> task1, LuminTask<T2> task2, LuminTask<T3> task3, LuminTask<T4> task4, LuminTask<T5> task5, LuminTask<T6> task6)
        {
            TaskTracker.TrackActiveTask(this, 3);

            this.completedCount = 0;
            {
                var awaiter = task1.GetAwaiter();

                if (awaiter.IsCompleted)
                {
                    TryInvokeContinuationT1(this, awaiter);
                }
                else
                {
                    awaiter.SourceOnCompleted(static state =>
                    {
                        using (var t = (StateTuple<WhenAnyPromise<T1, T2, T3, T4, T5, T6>, LuminTaskAwaiter<T1>>)state)
                        {
                            TryInvokeContinuationT1(t.Item1, t.Item2);
                        }
                    }, StateTuple.Create(this, awaiter));
                }
            }
            {
                var awaiter = task2.GetAwaiter();

                if (awaiter.IsCompleted)
                {
                    TryInvokeContinuationT2(this, awaiter);
                }
                else
                {
                    awaiter.SourceOnCompleted(static state =>
                    {
                        using (var t = (StateTuple<WhenAnyPromise<T1, T2, T3, T4, T5, T6>, LuminTaskAwaiter<T2>>)state)
                        {
                            TryInvokeContinuationT2(t.Item1, t.Item2);
                        }
                    }, StateTuple.Create(this, awaiter));
                }
            }
            {
                var awaiter = task3.GetAwaiter();

                if (awaiter.IsCompleted)
                {
                    TryInvokeContinuationT3(this, awaiter);
                }
                else
                {
                    awaiter.SourceOnCompleted(static state =>
                    {
                        using (var t = (StateTuple<WhenAnyPromise<T1, T2, T3, T4, T5, T6>, LuminTaskAwaiter<T3>>)state)
                        {
                            TryInvokeContinuationT3(t.Item1, t.Item2);
                        }
                    }, StateTuple.Create(this, awaiter));
                }
            }
            {
                var awaiter = task4.GetAwaiter();

                if (awaiter.IsCompleted)
                {
                    TryInvokeContinuationT4(this, awaiter);
                }
                else
                {
                    awaiter.SourceOnCompleted(static state =>
                    {
                        using (var t = (StateTuple<WhenAnyPromise<T1, T2, T3, T4, T5, T6>, LuminTaskAwaiter<T4>>)state)
                        {
                            TryInvokeContinuationT4(t.Item1, t.Item2);
                        }
                    }, StateTuple.Create(this, awaiter));
                }
            }
            {
                var awaiter = task5.GetAwaiter();

                if (awaiter.IsCompleted)
                {
                    TryInvokeContinuationT5(this, awaiter);
                }
                else
                {
                    awaiter.SourceOnCompleted(static state =>
                    {
                        using (var t = (StateTuple<WhenAnyPromise<T1, T2, T3, T4, T5, T6>, LuminTaskAwaiter<T5>>)state)
                        {
                            TryInvokeContinuationT5(t.Item1, t.Item2);
                        }
                    }, StateTuple.Create(this, awaiter));
                }
            }
            {
                var awaiter = task6.GetAwaiter();

                if (awaiter.IsCompleted)
                {
                    TryInvokeContinuationT6(this, awaiter);
                }
                else
                {
                    awaiter.SourceOnCompleted(static state =>
                    {
                        using (var t = (StateTuple<WhenAnyPromise<T1, T2, T3, T4, T5, T6>, LuminTaskAwaiter<T6>>)state)
                        {
                            TryInvokeContinuationT6(t.Item1, t.Item2);
                        }
                    }, StateTuple.Create(this, awaiter));
                }
            }
        }

        static void TryInvokeContinuationT1(WhenAnyPromise<T1, T2, T3, T4, T5, T6> self, in LuminTaskAwaiter<T1> awaiter)
        {
            T1 result;
            try
            {
                result = awaiter.GetResult();
            }
            catch (Exception ex)
            {
                self.core.TrySetException(ex);
                return;
            }

            if (Interlocked.Increment(ref self.completedCount) == 1)
            {
                self.core.TrySetResult((0, result, default, default, default, default, default));
            }
        }

        static void TryInvokeContinuationT2(WhenAnyPromise<T1, T2, T3, T4, T5, T6> self, in LuminTaskAwaiter<T2> awaiter)
        {
            T2 result;
            try
            {
                result = awaiter.GetResult();
            }
            catch (Exception ex)
            {
                self.core.TrySetException(ex);
                return;
            }

            if (Interlocked.Increment(ref self.completedCount) == 1)
            {
                self.core.TrySetResult((1, default, result, default, default, default, default));
            }
        }

        static void TryInvokeContinuationT3(WhenAnyPromise<T1, T2, T3, T4, T5, T6> self, in LuminTaskAwaiter<T3> awaiter)
        {
            T3 result;
            try
            {
                result = awaiter.GetResult();
            }
            catch (Exception ex)
            {
                self.core.TrySetException(ex);
                return;
            }

            if (Interlocked.Increment(ref self.completedCount) == 1)
            {
                self.core.TrySetResult((2, default, default, result, default, default, default));
            }
        }

        static void TryInvokeContinuationT4(WhenAnyPromise<T1, T2, T3, T4, T5, T6> self, in LuminTaskAwaiter<T4> awaiter)
        {
            T4 result;
            try
            {
                result = awaiter.GetResult();
            }
            catch (Exception ex)
            {
                self.core.TrySetException(ex);
                return;
            }

            if (Interlocked.Increment(ref self.completedCount) == 1)
            {
                self.core.TrySetResult((3, default, default, default, result, default, default));
            }
        }

        static void TryInvokeContinuationT5(WhenAnyPromise<T1, T2, T3, T4, T5, T6> self, in LuminTaskAwaiter<T5> awaiter)
        {
            T5 result;
            try
            {
                result = awaiter.GetResult();
            }
            catch (Exception ex)
            {
                self.core.TrySetException(ex);
                return;
            }

            if (Interlocked.Increment(ref self.completedCount) == 1)
            {
                self.core.TrySetResult((4, default, default, default, default, result, default));
            }
        }

        static void TryInvokeContinuationT6(WhenAnyPromise<T1, T2, T3, T4, T5, T6> self, in LuminTaskAwaiter<T6> awaiter)
        {
            T6 result;
            try
            {
                result = awaiter.GetResult();
            }
            catch (Exception ex)
            {
                self.core.TrySetException(ex);
                return;
            }

            if (Interlocked.Increment(ref self.completedCount) == 1)
            {
                self.core.TrySetResult((5, default, default, default, default, default, result));
            }
        }


        public (int, T1 result1, T2 result2, T3 result3, T4 result4, T5 result5, T6 result6) GetResult(short token)
        {
            TaskTracker.RemoveTracking(this);
            GC.SuppressFinalize(this);
            return core.GetResult(token);
        }

        public LuminTaskStatus GetStatus(short token)
        {
            return core.GetStatus(token);
        }

        public void OnCompleted(Action<object> continuation, object state, short token)
        {
            core.OnCompleted(continuation, state, token);
        }

        public LuminTaskStatus UnsafeGetStatus()
        {
            return core.UnsafeGetStatus();
        }

        void ILuminTaskSource.GetResult(short token)
        {
            GetResult(token);
        }
    }

    public static LuminTask<(int winArgumentIndex, T1 result1, T2 result2, T3 result3, T4 result4, T5 result5, T6 result6, T7 result7)> WhenAny<T1, T2, T3, T4, T5, T6, T7>(LuminTask<T1> task1, LuminTask<T2> task2, LuminTask<T3> task3, LuminTask<T4> task4, LuminTask<T5> task5, LuminTask<T6> task6, LuminTask<T7> task7)
    {
        return new LuminTask<(int winArgumentIndex, T1 result1, T2 result2, T3 result3, T4 result4, T5 result5, T6 result6, T7 result7)>(new WhenAnyPromise<T1, T2, T3, T4, T5, T6, T7>(task1, task2, task3, task4, task5, task6, task7), 0);
    }

    sealed class WhenAnyPromise<T1, T2, T3, T4, T5, T6, T7> : ILuminTaskSource<(int, T1 result1, T2 result2, T3 result3, T4 result4, T5 result5, T6 result6, T7 result7)>
    {
        int completedCount;
        LuminTaskCompletionSourceCore<(int, T1 result1, T2 result2, T3 result3, T4 result4, T5 result5, T6 result6, T7 result7)> core;

        public WhenAnyPromise(LuminTask<T1> task1, LuminTask<T2> task2, LuminTask<T3> task3, LuminTask<T4> task4, LuminTask<T5> task5, LuminTask<T6> task6, LuminTask<T7> task7)
        {
            TaskTracker.TrackActiveTask(this, 3);

            this.completedCount = 0;
            {
                var awaiter = task1.GetAwaiter();

                if (awaiter.IsCompleted)
                {
                    TryInvokeContinuationT1(this, awaiter);
                }
                else
                {
                    awaiter.SourceOnCompleted(static state =>
                    {
                        using (var t = (StateTuple<WhenAnyPromise<T1, T2, T3, T4, T5, T6, T7>, LuminTaskAwaiter<T1>>)state)
                        {
                            TryInvokeContinuationT1(t.Item1, t.Item2);
                        }
                    }, StateTuple.Create(this, awaiter));
                }
            }
            {
                var awaiter = task2.GetAwaiter();

                if (awaiter.IsCompleted)
                {
                    TryInvokeContinuationT2(this, awaiter);
                }
                else
                {
                    awaiter.SourceOnCompleted(static state =>
                    {
                        using (var t = (StateTuple<WhenAnyPromise<T1, T2, T3, T4, T5, T6, T7>, LuminTaskAwaiter<T2>>)state)
                        {
                            TryInvokeContinuationT2(t.Item1, t.Item2);
                        }
                    }, StateTuple.Create(this, awaiter));
                }
            }
            {
                var awaiter = task3.GetAwaiter();

                if (awaiter.IsCompleted)
                {
                    TryInvokeContinuationT3(this, awaiter);
                }
                else
                {
                    awaiter.SourceOnCompleted(static state =>
                    {
                        using (var t = (StateTuple<WhenAnyPromise<T1, T2, T3, T4, T5, T6, T7>, LuminTaskAwaiter<T3>>)state)
                        {
                            TryInvokeContinuationT3(t.Item1, t.Item2);
                        }
                    }, StateTuple.Create(this, awaiter));
                }
            }
            {
                var awaiter = task4.GetAwaiter();

                if (awaiter.IsCompleted)
                {
                    TryInvokeContinuationT4(this, awaiter);
                }
                else
                {
                    awaiter.SourceOnCompleted(static state =>
                    {
                        using (var t = (StateTuple<WhenAnyPromise<T1, T2, T3, T4, T5, T6, T7>, LuminTaskAwaiter<T4>>)state)
                        {
                            TryInvokeContinuationT4(t.Item1, t.Item2);
                        }
                    }, StateTuple.Create(this, awaiter));
                }
            }
            {
                var awaiter = task5.GetAwaiter();

                if (awaiter.IsCompleted)
                {
                    TryInvokeContinuationT5(this, awaiter);
                }
                else
                {
                    awaiter.SourceOnCompleted(static state =>
                    {
                        using (var t = (StateTuple<WhenAnyPromise<T1, T2, T3, T4, T5, T6, T7>, LuminTaskAwaiter<T5>>)state)
                        {
                            TryInvokeContinuationT5(t.Item1, t.Item2);
                        }
                    }, StateTuple.Create(this, awaiter));
                }
            }
            {
                var awaiter = task6.GetAwaiter();

                if (awaiter.IsCompleted)
                {
                    TryInvokeContinuationT6(this, awaiter);
                }
                else
                {
                    awaiter.SourceOnCompleted(static state =>
                    {
                        using (var t = (StateTuple<WhenAnyPromise<T1, T2, T3, T4, T5, T6, T7>, LuminTaskAwaiter<T6>>)state)
                        {
                            TryInvokeContinuationT6(t.Item1, t.Item2);
                        }
                    }, StateTuple.Create(this, awaiter));
                }
            }
            {
                var awaiter = task7.GetAwaiter();

                if (awaiter.IsCompleted)
                {
                    TryInvokeContinuationT7(this, awaiter);
                }
                else
                {
                    awaiter.SourceOnCompleted(static state =>
                    {
                        using (var t = (StateTuple<WhenAnyPromise<T1, T2, T3, T4, T5, T6, T7>, LuminTaskAwaiter<T7>>)state)
                        {
                            TryInvokeContinuationT7(t.Item1, t.Item2);
                        }
                    }, StateTuple.Create(this, awaiter));
                }
            }
        }

        static void TryInvokeContinuationT1(WhenAnyPromise<T1, T2, T3, T4, T5, T6, T7> self, in LuminTaskAwaiter<T1> awaiter)
        {
            T1 result;
            try
            {
                result = awaiter.GetResult();
            }
            catch (Exception ex)
            {
                self.core.TrySetException(ex);
                return;
            }

            if (Interlocked.Increment(ref self.completedCount) == 1)
            {
                self.core.TrySetResult((0, result, default, default, default, default, default, default));
            }
        }

        static void TryInvokeContinuationT2(WhenAnyPromise<T1, T2, T3, T4, T5, T6, T7> self, in LuminTaskAwaiter<T2> awaiter)
        {
            T2 result;
            try
            {
                result = awaiter.GetResult();
            }
            catch (Exception ex)
            {
                self.core.TrySetException(ex);
                return;
            }

            if (Interlocked.Increment(ref self.completedCount) == 1)
            {
                self.core.TrySetResult((1, default, result, default, default, default, default, default));
            }
        }

        static void TryInvokeContinuationT3(WhenAnyPromise<T1, T2, T3, T4, T5, T6, T7> self, in LuminTaskAwaiter<T3> awaiter)
        {
            T3 result;
            try
            {
                result = awaiter.GetResult();
            }
            catch (Exception ex)
            {
                self.core.TrySetException(ex);
                return;
            }

            if (Interlocked.Increment(ref self.completedCount) == 1)
            {
                self.core.TrySetResult((2, default, default, result, default, default, default, default));
            }
        }

        static void TryInvokeContinuationT4(WhenAnyPromise<T1, T2, T3, T4, T5, T6, T7> self, in LuminTaskAwaiter<T4> awaiter)
        {
            T4 result;
            try
            {
                result = awaiter.GetResult();
            }
            catch (Exception ex)
            {
                self.core.TrySetException(ex);
                return;
            }

            if (Interlocked.Increment(ref self.completedCount) == 1)
            {
                self.core.TrySetResult((3, default, default, default, result, default, default, default));
            }
        }

        static void TryInvokeContinuationT5(WhenAnyPromise<T1, T2, T3, T4, T5, T6, T7> self, in LuminTaskAwaiter<T5> awaiter)
        {
            T5 result;
            try
            {
                result = awaiter.GetResult();
            }
            catch (Exception ex)
            {
                self.core.TrySetException(ex);
                return;
            }

            if (Interlocked.Increment(ref self.completedCount) == 1)
            {
                self.core.TrySetResult((4, default, default, default, default, result, default, default));
            }
        }

        static void TryInvokeContinuationT6(WhenAnyPromise<T1, T2, T3, T4, T5, T6, T7> self, in LuminTaskAwaiter<T6> awaiter)
        {
            T6 result;
            try
            {
                result = awaiter.GetResult();
            }
            catch (Exception ex)
            {
                self.core.TrySetException(ex);
                return;
            }

            if (Interlocked.Increment(ref self.completedCount) == 1)
            {
                self.core.TrySetResult((5, default, default, default, default, default, result, default));
            }
        }

        static void TryInvokeContinuationT7(WhenAnyPromise<T1, T2, T3, T4, T5, T6, T7> self, in LuminTaskAwaiter<T7> awaiter)
        {
            T7 result;
            try
            {
                result = awaiter.GetResult();
            }
            catch (Exception ex)
            {
                self.core.TrySetException(ex);
                return;
            }

            if (Interlocked.Increment(ref self.completedCount) == 1)
            {
                self.core.TrySetResult((6, default, default, default, default, default, default, result));
            }
        }


        public (int, T1 result1, T2 result2, T3 result3, T4 result4, T5 result5, T6 result6, T7 result7) GetResult(short token)
        {
            TaskTracker.RemoveTracking(this);
            GC.SuppressFinalize(this);
            return core.GetResult(token);
        }

        public LuminTaskStatus GetStatus(short token)
        {
            return core.GetStatus(token);
        }

        public void OnCompleted(Action<object> continuation, object state, short token)
        {
            core.OnCompleted(continuation, state, token);
        }

        public LuminTaskStatus UnsafeGetStatus()
        {
            return core.UnsafeGetStatus();
        }

        void ILuminTaskSource.GetResult(short token)
        {
            GetResult(token);
        }
    }

    public static LuminTask<(int winArgumentIndex, T1 result1, T2 result2, T3 result3, T4 result4, T5 result5, T6 result6, T7 result7, T8 result8)> WhenAny<T1, T2, T3, T4, T5, T6, T7, T8>(LuminTask<T1> task1, LuminTask<T2> task2, LuminTask<T3> task3, LuminTask<T4> task4, LuminTask<T5> task5, LuminTask<T6> task6, LuminTask<T7> task7, LuminTask<T8> task8)
    {
        return new LuminTask<(int winArgumentIndex, T1 result1, T2 result2, T3 result3, T4 result4, T5 result5, T6 result6, T7 result7, T8 result8)>(new WhenAnyPromise<T1, T2, T3, T4, T5, T6, T7, T8>(task1, task2, task3, task4, task5, task6, task7, task8), 0);
    }

    sealed class WhenAnyPromise<T1, T2, T3, T4, T5, T6, T7, T8> : ILuminTaskSource<(int, T1 result1, T2 result2, T3 result3, T4 result4, T5 result5, T6 result6, T7 result7, T8 result8)>
    {
        int completedCount;
        LuminTaskCompletionSourceCore<(int, T1 result1, T2 result2, T3 result3, T4 result4, T5 result5, T6 result6, T7 result7, T8 result8)> core;

        public WhenAnyPromise(LuminTask<T1> task1, LuminTask<T2> task2, LuminTask<T3> task3, LuminTask<T4> task4, LuminTask<T5> task5, LuminTask<T6> task6, LuminTask<T7> task7, LuminTask<T8> task8)
        {
            TaskTracker.TrackActiveTask(this, 3);

            this.completedCount = 0;
            {
                var awaiter = task1.GetAwaiter();

                if (awaiter.IsCompleted)
                {
                    TryInvokeContinuationT1(this, awaiter);
                }
                else
                {
                    awaiter.SourceOnCompleted(static state =>
                    {
                        using (var t = (StateTuple<WhenAnyPromise<T1, T2, T3, T4, T5, T6, T7, T8>, LuminTaskAwaiter<T1>>)state)
                        {
                            TryInvokeContinuationT1(t.Item1, t.Item2);
                        }
                    }, StateTuple.Create(this, awaiter));
                }
            }
            {
                var awaiter = task2.GetAwaiter();

                if (awaiter.IsCompleted)
                {
                    TryInvokeContinuationT2(this, awaiter);
                }
                else
                {
                    awaiter.SourceOnCompleted(static state =>
                    {
                        using (var t = (StateTuple<WhenAnyPromise<T1, T2, T3, T4, T5, T6, T7, T8>, LuminTaskAwaiter<T2>>)state)
                        {
                            TryInvokeContinuationT2(t.Item1, t.Item2);
                        }
                    }, StateTuple.Create(this, awaiter));
                }
            }
            {
                var awaiter = task3.GetAwaiter();

                if (awaiter.IsCompleted)
                {
                    TryInvokeContinuationT3(this, awaiter);
                }
                else
                {
                    awaiter.SourceOnCompleted(static state =>
                    {
                        using (var t = (StateTuple<WhenAnyPromise<T1, T2, T3, T4, T5, T6, T7, T8>, LuminTaskAwaiter<T3>>)state)
                        {
                            TryInvokeContinuationT3(t.Item1, t.Item2);
                        }
                    }, StateTuple.Create(this, awaiter));
                }
            }
            {
                var awaiter = task4.GetAwaiter();

                if (awaiter.IsCompleted)
                {
                    TryInvokeContinuationT4(this, awaiter);
                }
                else
                {
                    awaiter.SourceOnCompleted(static state =>
                    {
                        using (var t = (StateTuple<WhenAnyPromise<T1, T2, T3, T4, T5, T6, T7, T8>, LuminTaskAwaiter<T4>>)state)
                        {
                            TryInvokeContinuationT4(t.Item1, t.Item2);
                        }
                    }, StateTuple.Create(this, awaiter));
                }
            }
            {
                var awaiter = task5.GetAwaiter();

                if (awaiter.IsCompleted)
                {
                    TryInvokeContinuationT5(this, awaiter);
                }
                else
                {
                    awaiter.SourceOnCompleted(static state =>
                    {
                        using (var t = (StateTuple<WhenAnyPromise<T1, T2, T3, T4, T5, T6, T7, T8>, LuminTaskAwaiter<T5>>)state)
                        {
                            TryInvokeContinuationT5(t.Item1, t.Item2);
                        }
                    }, StateTuple.Create(this, awaiter));
                }
            }
            {
                var awaiter = task6.GetAwaiter();

                if (awaiter.IsCompleted)
                {
                    TryInvokeContinuationT6(this, awaiter);
                }
                else
                {
                    awaiter.SourceOnCompleted(static state =>
                    {
                        using (var t = (StateTuple<WhenAnyPromise<T1, T2, T3, T4, T5, T6, T7, T8>, LuminTaskAwaiter<T6>>)state)
                        {
                            TryInvokeContinuationT6(t.Item1, t.Item2);
                        }
                    }, StateTuple.Create(this, awaiter));
                }
            }
            {
                var awaiter = task7.GetAwaiter();

                if (awaiter.IsCompleted)
                {
                    TryInvokeContinuationT7(this, awaiter);
                }
                else
                {
                    awaiter.SourceOnCompleted(static state =>
                    {
                        using (var t = (StateTuple<WhenAnyPromise<T1, T2, T3, T4, T5, T6, T7, T8>, LuminTaskAwaiter<T7>>)state)
                        {
                            TryInvokeContinuationT7(t.Item1, t.Item2);
                        }
                    }, StateTuple.Create(this, awaiter));
                }
            }
            {
                var awaiter = task8.GetAwaiter();

                if (awaiter.IsCompleted)
                {
                    TryInvokeContinuationT8(this, awaiter);
                }
                else
                {
                    awaiter.SourceOnCompleted(static state =>
                    {
                        using (var t = (StateTuple<WhenAnyPromise<T1, T2, T3, T4, T5, T6, T7, T8>, LuminTaskAwaiter<T8>>)state)
                        {
                            TryInvokeContinuationT8(t.Item1, t.Item2);
                        }
                    }, StateTuple.Create(this, awaiter));
                }
            }
        }

        static void TryInvokeContinuationT1(WhenAnyPromise<T1, T2, T3, T4, T5, T6, T7, T8> self, in LuminTaskAwaiter<T1> awaiter)
        {
            T1 result;
            try
            {
                result = awaiter.GetResult();
            }
            catch (Exception ex)
            {
                self.core.TrySetException(ex);
                return;
            }

            if (Interlocked.Increment(ref self.completedCount) == 1)
            {
                self.core.TrySetResult((0, result, default, default, default, default, default, default, default));
            }
        }

        static void TryInvokeContinuationT2(WhenAnyPromise<T1, T2, T3, T4, T5, T6, T7, T8> self, in LuminTaskAwaiter<T2> awaiter)
        {
            T2 result;
            try
            {
                result = awaiter.GetResult();
            }
            catch (Exception ex)
            {
                self.core.TrySetException(ex);
                return;
            }

            if (Interlocked.Increment(ref self.completedCount) == 1)
            {
                self.core.TrySetResult((1, default, result, default, default, default, default, default, default));
            }
        }

        static void TryInvokeContinuationT3(WhenAnyPromise<T1, T2, T3, T4, T5, T6, T7, T8> self, in LuminTaskAwaiter<T3> awaiter)
        {
            T3 result;
            try
            {
                result = awaiter.GetResult();
            }
            catch (Exception ex)
            {
                self.core.TrySetException(ex);
                return;
            }

            if (Interlocked.Increment(ref self.completedCount) == 1)
            {
                self.core.TrySetResult((2, default, default, result, default, default, default, default, default));
            }
        }

        static void TryInvokeContinuationT4(WhenAnyPromise<T1, T2, T3, T4, T5, T6, T7, T8> self, in LuminTaskAwaiter<T4> awaiter)
        {
            T4 result;
            try
            {
                result = awaiter.GetResult();
            }
            catch (Exception ex)
            {
                self.core.TrySetException(ex);
                return;
            }

            if (Interlocked.Increment(ref self.completedCount) == 1)
            {
                self.core.TrySetResult((3, default, default, default, result, default, default, default, default));
            }
        }

        static void TryInvokeContinuationT5(WhenAnyPromise<T1, T2, T3, T4, T5, T6, T7, T8> self, in LuminTaskAwaiter<T5> awaiter)
        {
            T5 result;
            try
            {
                result = awaiter.GetResult();
            }
            catch (Exception ex)
            {
                self.core.TrySetException(ex);
                return;
            }

            if (Interlocked.Increment(ref self.completedCount) == 1)
            {
                self.core.TrySetResult((4, default, default, default, default, result, default, default, default));
            }
        }

        static void TryInvokeContinuationT6(WhenAnyPromise<T1, T2, T3, T4, T5, T6, T7, T8> self, in LuminTaskAwaiter<T6> awaiter)
        {
            T6 result;
            try
            {
                result = awaiter.GetResult();
            }
            catch (Exception ex)
            {
                self.core.TrySetException(ex);
                return;
            }

            if (Interlocked.Increment(ref self.completedCount) == 1)
            {
                self.core.TrySetResult((5, default, default, default, default, default, result, default, default));
            }
        }

        static void TryInvokeContinuationT7(WhenAnyPromise<T1, T2, T3, T4, T5, T6, T7, T8> self, in LuminTaskAwaiter<T7> awaiter)
        {
            T7 result;
            try
            {
                result = awaiter.GetResult();
            }
            catch (Exception ex)
            {
                self.core.TrySetException(ex);
                return;
            }

            if (Interlocked.Increment(ref self.completedCount) == 1)
            {
                self.core.TrySetResult((6, default, default, default, default, default, default, result, default));
            }
        }

        static void TryInvokeContinuationT8(WhenAnyPromise<T1, T2, T3, T4, T5, T6, T7, T8> self, in LuminTaskAwaiter<T8> awaiter)
        {
            T8 result;
            try
            {
                result = awaiter.GetResult();
            }
            catch (Exception ex)
            {
                self.core.TrySetException(ex);
                return;
            }

            if (Interlocked.Increment(ref self.completedCount) == 1)
            {
                self.core.TrySetResult((7, default, default, default, default, default, default, default, result));
            }
        }


        public (int, T1 result1, T2 result2, T3 result3, T4 result4, T5 result5, T6 result6, T7 result7, T8 result8) GetResult(short token)
        {
            TaskTracker.RemoveTracking(this);
            GC.SuppressFinalize(this);
            return core.GetResult(token);
        }

        public LuminTaskStatus GetStatus(short token)
        {
            return core.GetStatus(token);
        }

        public void OnCompleted(Action<object> continuation, object state, short token)
        {
            core.OnCompleted(continuation, state, token);
        }

        public LuminTaskStatus UnsafeGetStatus()
        {
            return core.UnsafeGetStatus();
        }

        void ILuminTaskSource.GetResult(short token)
        {
            GetResult(token);
        }
    }

    public static LuminTask<(int winArgumentIndex, T1 result1, T2 result2, T3 result3, T4 result4, T5 result5, T6 result6, T7 result7, T8 result8, T9 result9)> WhenAny<T1, T2, T3, T4, T5, T6, T7, T8, T9>(LuminTask<T1> task1, LuminTask<T2> task2, LuminTask<T3> task3, LuminTask<T4> task4, LuminTask<T5> task5, LuminTask<T6> task6, LuminTask<T7> task7, LuminTask<T8> task8, LuminTask<T9> task9)
    {
        return new LuminTask<(int winArgumentIndex, T1 result1, T2 result2, T3 result3, T4 result4, T5 result5, T6 result6, T7 result7, T8 result8, T9 result9)>(new WhenAnyPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9>(task1, task2, task3, task4, task5, task6, task7, task8, task9), 0);
    }

    sealed class WhenAnyPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9> : ILuminTaskSource<(int, T1 result1, T2 result2, T3 result3, T4 result4, T5 result5, T6 result6, T7 result7, T8 result8, T9 result9)>
    {
        int completedCount;
        LuminTaskCompletionSourceCore<(int, T1 result1, T2 result2, T3 result3, T4 result4, T5 result5, T6 result6, T7 result7, T8 result8, T9 result9)> core;

        public WhenAnyPromise(LuminTask<T1> task1, LuminTask<T2> task2, LuminTask<T3> task3, LuminTask<T4> task4, LuminTask<T5> task5, LuminTask<T6> task6, LuminTask<T7> task7, LuminTask<T8> task8, LuminTask<T9> task9)
        {
            TaskTracker.TrackActiveTask(this, 3);

            this.completedCount = 0;
            {
                var awaiter = task1.GetAwaiter();

                if (awaiter.IsCompleted)
                {
                    TryInvokeContinuationT1(this, awaiter);
                }
                else
                {
                    awaiter.SourceOnCompleted(static state =>
                    {
                        using (var t = (StateTuple<WhenAnyPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9>, LuminTaskAwaiter<T1>>)state)
                        {
                            TryInvokeContinuationT1(t.Item1, t.Item2);
                        }
                    }, StateTuple.Create(this, awaiter));
                }
            }
            {
                var awaiter = task2.GetAwaiter();

                if (awaiter.IsCompleted)
                {
                    TryInvokeContinuationT2(this, awaiter);
                }
                else
                {
                    awaiter.SourceOnCompleted(static state =>
                    {
                        using (var t = (StateTuple<WhenAnyPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9>, LuminTaskAwaiter<T2>>)state)
                        {
                            TryInvokeContinuationT2(t.Item1, t.Item2);
                        }
                    }, StateTuple.Create(this, awaiter));
                }
            }
            {
                var awaiter = task3.GetAwaiter();

                if (awaiter.IsCompleted)
                {
                    TryInvokeContinuationT3(this, awaiter);
                }
                else
                {
                    awaiter.SourceOnCompleted(static state =>
                    {
                        using (var t = (StateTuple<WhenAnyPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9>, LuminTaskAwaiter<T3>>)state)
                        {
                            TryInvokeContinuationT3(t.Item1, t.Item2);
                        }
                    }, StateTuple.Create(this, awaiter));
                }
            }
            {
                var awaiter = task4.GetAwaiter();

                if (awaiter.IsCompleted)
                {
                    TryInvokeContinuationT4(this, awaiter);
                }
                else
                {
                    awaiter.SourceOnCompleted(static state =>
                    {
                        using (var t = (StateTuple<WhenAnyPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9>, LuminTaskAwaiter<T4>>)state)
                        {
                            TryInvokeContinuationT4(t.Item1, t.Item2);
                        }
                    }, StateTuple.Create(this, awaiter));
                }
            }
            {
                var awaiter = task5.GetAwaiter();

                if (awaiter.IsCompleted)
                {
                    TryInvokeContinuationT5(this, awaiter);
                }
                else
                {
                    awaiter.SourceOnCompleted(static state =>
                    {
                        using (var t = (StateTuple<WhenAnyPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9>, LuminTaskAwaiter<T5>>)state)
                        {
                            TryInvokeContinuationT5(t.Item1, t.Item2);
                        }
                    }, StateTuple.Create(this, awaiter));
                }
            }
            {
                var awaiter = task6.GetAwaiter();

                if (awaiter.IsCompleted)
                {
                    TryInvokeContinuationT6(this, awaiter);
                }
                else
                {
                    awaiter.SourceOnCompleted(static state =>
                    {
                        using (var t = (StateTuple<WhenAnyPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9>, LuminTaskAwaiter<T6>>)state)
                        {
                            TryInvokeContinuationT6(t.Item1, t.Item2);
                        }
                    }, StateTuple.Create(this, awaiter));
                }
            }
            {
                var awaiter = task7.GetAwaiter();

                if (awaiter.IsCompleted)
                {
                    TryInvokeContinuationT7(this, awaiter);
                }
                else
                {
                    awaiter.SourceOnCompleted(static state =>
                    {
                        using (var t = (StateTuple<WhenAnyPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9>, LuminTaskAwaiter<T7>>)state)
                        {
                            TryInvokeContinuationT7(t.Item1, t.Item2);
                        }
                    }, StateTuple.Create(this, awaiter));
                }
            }
            {
                var awaiter = task8.GetAwaiter();

                if (awaiter.IsCompleted)
                {
                    TryInvokeContinuationT8(this, awaiter);
                }
                else
                {
                    awaiter.SourceOnCompleted(static state =>
                    {
                        using (var t = (StateTuple<WhenAnyPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9>, LuminTaskAwaiter<T8>>)state)
                        {
                            TryInvokeContinuationT8(t.Item1, t.Item2);
                        }
                    }, StateTuple.Create(this, awaiter));
                }
            }
            {
                var awaiter = task9.GetAwaiter();

                if (awaiter.IsCompleted)
                {
                    TryInvokeContinuationT9(this, awaiter);
                }
                else
                {
                    awaiter.SourceOnCompleted(static state =>
                    {
                        using (var t = (StateTuple<WhenAnyPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9>, LuminTaskAwaiter<T9>>)state)
                        {
                            TryInvokeContinuationT9(t.Item1, t.Item2);
                        }
                    }, StateTuple.Create(this, awaiter));
                }
            }
        }

        static void TryInvokeContinuationT1(WhenAnyPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9> self, in LuminTaskAwaiter<T1> awaiter)
        {
            T1 result;
            try
            {
                result = awaiter.GetResult();
            }
            catch (Exception ex)
            {
                self.core.TrySetException(ex);
                return;
            }

            if (Interlocked.Increment(ref self.completedCount) == 1)
            {
                self.core.TrySetResult((0, result, default, default, default, default, default, default, default, default));
            }
        }

        static void TryInvokeContinuationT2(WhenAnyPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9> self, in LuminTaskAwaiter<T2> awaiter)
        {
            T2 result;
            try
            {
                result = awaiter.GetResult();
            }
            catch (Exception ex)
            {
                self.core.TrySetException(ex);
                return;
            }

            if (Interlocked.Increment(ref self.completedCount) == 1)
            {
                self.core.TrySetResult((1, default, result, default, default, default, default, default, default, default));
            }
        }

        static void TryInvokeContinuationT3(WhenAnyPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9> self, in LuminTaskAwaiter<T3> awaiter)
        {
            T3 result;
            try
            {
                result = awaiter.GetResult();
            }
            catch (Exception ex)
            {
                self.core.TrySetException(ex);
                return;
            }

            if (Interlocked.Increment(ref self.completedCount) == 1)
            {
                self.core.TrySetResult((2, default, default, result, default, default, default, default, default, default));
            }
        }

        static void TryInvokeContinuationT4(WhenAnyPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9> self, in LuminTaskAwaiter<T4> awaiter)
        {
            T4 result;
            try
            {
                result = awaiter.GetResult();
            }
            catch (Exception ex)
            {
                self.core.TrySetException(ex);
                return;
            }

            if (Interlocked.Increment(ref self.completedCount) == 1)
            {
                self.core.TrySetResult((3, default, default, default, result, default, default, default, default, default));
            }
        }

        static void TryInvokeContinuationT5(WhenAnyPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9> self, in LuminTaskAwaiter<T5> awaiter)
        {
            T5 result;
            try
            {
                result = awaiter.GetResult();
            }
            catch (Exception ex)
            {
                self.core.TrySetException(ex);
                return;
            }

            if (Interlocked.Increment(ref self.completedCount) == 1)
            {
                self.core.TrySetResult((4, default, default, default, default, result, default, default, default, default));
            }
        }

        static void TryInvokeContinuationT6(WhenAnyPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9> self, in LuminTaskAwaiter<T6> awaiter)
        {
            T6 result;
            try
            {
                result = awaiter.GetResult();
            }
            catch (Exception ex)
            {
                self.core.TrySetException(ex);
                return;
            }

            if (Interlocked.Increment(ref self.completedCount) == 1)
            {
                self.core.TrySetResult((5, default, default, default, default, default, result, default, default, default));
            }
        }

        static void TryInvokeContinuationT7(WhenAnyPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9> self, in LuminTaskAwaiter<T7> awaiter)
        {
            T7 result;
            try
            {
                result = awaiter.GetResult();
            }
            catch (Exception ex)
            {
                self.core.TrySetException(ex);
                return;
            }

            if (Interlocked.Increment(ref self.completedCount) == 1)
            {
                self.core.TrySetResult((6, default, default, default, default, default, default, result, default, default));
            }
        }

        static void TryInvokeContinuationT8(WhenAnyPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9> self, in LuminTaskAwaiter<T8> awaiter)
        {
            T8 result;
            try
            {
                result = awaiter.GetResult();
            }
            catch (Exception ex)
            {
                self.core.TrySetException(ex);
                return;
            }

            if (Interlocked.Increment(ref self.completedCount) == 1)
            {
                self.core.TrySetResult((7, default, default, default, default, default, default, default, result, default));
            }
        }

        static void TryInvokeContinuationT9(WhenAnyPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9> self, in LuminTaskAwaiter<T9> awaiter)
        {
            T9 result;
            try
            {
                result = awaiter.GetResult();
            }
            catch (Exception ex)
            {
                self.core.TrySetException(ex);
                return;
            }

            if (Interlocked.Increment(ref self.completedCount) == 1)
            {
                self.core.TrySetResult((8, default, default, default, default, default, default, default, default, result));
            }
        }


        public (int, T1 result1, T2 result2, T3 result3, T4 result4, T5 result5, T6 result6, T7 result7, T8 result8, T9 result9) GetResult(short token)
        {
            TaskTracker.RemoveTracking(this);
            GC.SuppressFinalize(this);
            return core.GetResult(token);
        }

        public LuminTaskStatus GetStatus(short token)
        {
            return core.GetStatus(token);
        }

        public void OnCompleted(Action<object> continuation, object state, short token)
        {
            core.OnCompleted(continuation, state, token);
        }

        public LuminTaskStatus UnsafeGetStatus()
        {
            return core.UnsafeGetStatus();
        }

        void ILuminTaskSource.GetResult(short token)
        {
            GetResult(token);
        }
    }

    public static LuminTask<(int winArgumentIndex, T1 result1, T2 result2, T3 result3, T4 result4, T5 result5, T6 result6, T7 result7, T8 result8, T9 result9, T10 result10)> WhenAny<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(LuminTask<T1> task1, LuminTask<T2> task2, LuminTask<T3> task3, LuminTask<T4> task4, LuminTask<T5> task5, LuminTask<T6> task6, LuminTask<T7> task7, LuminTask<T8> task8, LuminTask<T9> task9, LuminTask<T10> task10)
    {
        return new LuminTask<(int winArgumentIndex, T1 result1, T2 result2, T3 result3, T4 result4, T5 result5, T6 result6, T7 result7, T8 result8, T9 result9, T10 result10)>(new WhenAnyPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(task1, task2, task3, task4, task5, task6, task7, task8, task9, task10), 0);
    }

    sealed class WhenAnyPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> : ILuminTaskSource<(int, T1 result1, T2 result2, T3 result3, T4 result4, T5 result5, T6 result6, T7 result7, T8 result8, T9 result9, T10 result10)>
    {
        int completedCount;
        LuminTaskCompletionSourceCore<(int, T1 result1, T2 result2, T3 result3, T4 result4, T5 result5, T6 result6, T7 result7, T8 result8, T9 result9, T10 result10)> core;

        public WhenAnyPromise(LuminTask<T1> task1, LuminTask<T2> task2, LuminTask<T3> task3, LuminTask<T4> task4, LuminTask<T5> task5, LuminTask<T6> task6, LuminTask<T7> task7, LuminTask<T8> task8, LuminTask<T9> task9, LuminTask<T10> task10)
        {
            TaskTracker.TrackActiveTask(this, 3);

            this.completedCount = 0;
            {
                var awaiter = task1.GetAwaiter();

                if (awaiter.IsCompleted)
                {
                    TryInvokeContinuationT1(this, awaiter);
                }
                else
                {
                    awaiter.SourceOnCompleted(static state =>
                    {
                        using (var t = (StateTuple<WhenAnyPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>, LuminTaskAwaiter<T1>>)state)
                        {
                            TryInvokeContinuationT1(t.Item1, t.Item2);
                        }
                    }, StateTuple.Create(this, awaiter));
                }
            }
            {
                var awaiter = task2.GetAwaiter();

                if (awaiter.IsCompleted)
                {
                    TryInvokeContinuationT2(this, awaiter);
                }
                else
                {
                    awaiter.SourceOnCompleted(static state =>
                    {
                        using (var t = (StateTuple<WhenAnyPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>, LuminTaskAwaiter<T2>>)state)
                        {
                            TryInvokeContinuationT2(t.Item1, t.Item2);
                        }
                    }, StateTuple.Create(this, awaiter));
                }
            }
            {
                var awaiter = task3.GetAwaiter();

                if (awaiter.IsCompleted)
                {
                    TryInvokeContinuationT3(this, awaiter);
                }
                else
                {
                    awaiter.SourceOnCompleted(static state =>
                    {
                        using (var t = (StateTuple<WhenAnyPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>, LuminTaskAwaiter<T3>>)state)
                        {
                            TryInvokeContinuationT3(t.Item1, t.Item2);
                        }
                    }, StateTuple.Create(this, awaiter));
                }
            }
            {
                var awaiter = task4.GetAwaiter();

                if (awaiter.IsCompleted)
                {
                    TryInvokeContinuationT4(this, awaiter);
                }
                else
                {
                    awaiter.SourceOnCompleted(static state =>
                    {
                        using (var t = (StateTuple<WhenAnyPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>, LuminTaskAwaiter<T4>>)state)
                        {
                            TryInvokeContinuationT4(t.Item1, t.Item2);
                        }
                    }, StateTuple.Create(this, awaiter));
                }
            }
            {
                var awaiter = task5.GetAwaiter();

                if (awaiter.IsCompleted)
                {
                    TryInvokeContinuationT5(this, awaiter);
                }
                else
                {
                    awaiter.SourceOnCompleted(static state =>
                    {
                        using (var t = (StateTuple<WhenAnyPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>, LuminTaskAwaiter<T5>>)state)
                        {
                            TryInvokeContinuationT5(t.Item1, t.Item2);
                        }
                    }, StateTuple.Create(this, awaiter));
                }
            }
            {
                var awaiter = task6.GetAwaiter();

                if (awaiter.IsCompleted)
                {
                    TryInvokeContinuationT6(this, awaiter);
                }
                else
                {
                    awaiter.SourceOnCompleted(static state =>
                    {
                        using (var t = (StateTuple<WhenAnyPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>, LuminTaskAwaiter<T6>>)state)
                        {
                            TryInvokeContinuationT6(t.Item1, t.Item2);
                        }
                    }, StateTuple.Create(this, awaiter));
                }
            }
            {
                var awaiter = task7.GetAwaiter();

                if (awaiter.IsCompleted)
                {
                    TryInvokeContinuationT7(this, awaiter);
                }
                else
                {
                    awaiter.SourceOnCompleted(static state =>
                    {
                        using (var t = (StateTuple<WhenAnyPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>, LuminTaskAwaiter<T7>>)state)
                        {
                            TryInvokeContinuationT7(t.Item1, t.Item2);
                        }
                    }, StateTuple.Create(this, awaiter));
                }
            }
            {
                var awaiter = task8.GetAwaiter();

                if (awaiter.IsCompleted)
                {
                    TryInvokeContinuationT8(this, awaiter);
                }
                else
                {
                    awaiter.SourceOnCompleted(static state =>
                    {
                        using (var t = (StateTuple<WhenAnyPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>, LuminTaskAwaiter<T8>>)state)
                        {
                            TryInvokeContinuationT8(t.Item1, t.Item2);
                        }
                    }, StateTuple.Create(this, awaiter));
                }
            }
            {
                var awaiter = task9.GetAwaiter();

                if (awaiter.IsCompleted)
                {
                    TryInvokeContinuationT9(this, awaiter);
                }
                else
                {
                    awaiter.SourceOnCompleted(static state =>
                    {
                        using (var t = (StateTuple<WhenAnyPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>, LuminTaskAwaiter<T9>>)state)
                        {
                            TryInvokeContinuationT9(t.Item1, t.Item2);
                        }
                    }, StateTuple.Create(this, awaiter));
                }
            }
            {
                var awaiter = task10.GetAwaiter();

                if (awaiter.IsCompleted)
                {
                    TryInvokeContinuationT10(this, awaiter);
                }
                else
                {
                    awaiter.SourceOnCompleted(static state =>
                    {
                        using (var t = (StateTuple<WhenAnyPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>, LuminTaskAwaiter<T10>>)state)
                        {
                            TryInvokeContinuationT10(t.Item1, t.Item2);
                        }
                    }, StateTuple.Create(this, awaiter));
                }
            }
        }

        static void TryInvokeContinuationT1(WhenAnyPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> self, in LuminTaskAwaiter<T1> awaiter)
        {
            T1 result;
            try
            {
                result = awaiter.GetResult();
            }
            catch (Exception ex)
            {
                self.core.TrySetException(ex);
                return;
            }

            if (Interlocked.Increment(ref self.completedCount) == 1)
            {
                self.core.TrySetResult((0, result, default, default, default, default, default, default, default, default, default));
            }
        }

        static void TryInvokeContinuationT2(WhenAnyPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> self, in LuminTaskAwaiter<T2> awaiter)
        {
            T2 result;
            try
            {
                result = awaiter.GetResult();
            }
            catch (Exception ex)
            {
                self.core.TrySetException(ex);
                return;
            }

            if (Interlocked.Increment(ref self.completedCount) == 1)
            {
                self.core.TrySetResult((1, default, result, default, default, default, default, default, default, default, default));
            }
        }

        static void TryInvokeContinuationT3(WhenAnyPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> self, in LuminTaskAwaiter<T3> awaiter)
        {
            T3 result;
            try
            {
                result = awaiter.GetResult();
            }
            catch (Exception ex)
            {
                self.core.TrySetException(ex);
                return;
            }

            if (Interlocked.Increment(ref self.completedCount) == 1)
            {
                self.core.TrySetResult((2, default, default, result, default, default, default, default, default, default, default));
            }
        }

        static void TryInvokeContinuationT4(WhenAnyPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> self, in LuminTaskAwaiter<T4> awaiter)
        {
            T4 result;
            try
            {
                result = awaiter.GetResult();
            }
            catch (Exception ex)
            {
                self.core.TrySetException(ex);
                return;
            }

            if (Interlocked.Increment(ref self.completedCount) == 1)
            {
                self.core.TrySetResult((3, default, default, default, result, default, default, default, default, default, default));
            }
        }

        static void TryInvokeContinuationT5(WhenAnyPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> self, in LuminTaskAwaiter<T5> awaiter)
        {
            T5 result;
            try
            {
                result = awaiter.GetResult();
            }
            catch (Exception ex)
            {
                self.core.TrySetException(ex);
                return;
            }

            if (Interlocked.Increment(ref self.completedCount) == 1)
            {
                self.core.TrySetResult((4, default, default, default, default, result, default, default, default, default, default));
            }
        }

        static void TryInvokeContinuationT6(WhenAnyPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> self, in LuminTaskAwaiter<T6> awaiter)
        {
            T6 result;
            try
            {
                result = awaiter.GetResult();
            }
            catch (Exception ex)
            {
                self.core.TrySetException(ex);
                return;
            }

            if (Interlocked.Increment(ref self.completedCount) == 1)
            {
                self.core.TrySetResult((5, default, default, default, default, default, result, default, default, default, default));
            }
        }

        static void TryInvokeContinuationT7(WhenAnyPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> self, in LuminTaskAwaiter<T7> awaiter)
        {
            T7 result;
            try
            {
                result = awaiter.GetResult();
            }
            catch (Exception ex)
            {
                self.core.TrySetException(ex);
                return;
            }

            if (Interlocked.Increment(ref self.completedCount) == 1)
            {
                self.core.TrySetResult((6, default, default, default, default, default, default, result, default, default, default));
            }
        }

        static void TryInvokeContinuationT8(WhenAnyPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> self, in LuminTaskAwaiter<T8> awaiter)
        {
            T8 result;
            try
            {
                result = awaiter.GetResult();
            }
            catch (Exception ex)
            {
                self.core.TrySetException(ex);
                return;
            }

            if (Interlocked.Increment(ref self.completedCount) == 1)
            {
                self.core.TrySetResult((7, default, default, default, default, default, default, default, result, default, default));
            }
        }

        static void TryInvokeContinuationT9(WhenAnyPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> self, in LuminTaskAwaiter<T9> awaiter)
        {
            T9 result;
            try
            {
                result = awaiter.GetResult();
            }
            catch (Exception ex)
            {
                self.core.TrySetException(ex);
                return;
            }

            if (Interlocked.Increment(ref self.completedCount) == 1)
            {
                self.core.TrySetResult((8, default, default, default, default, default, default, default, default, result, default));
            }
        }

        static void TryInvokeContinuationT10(WhenAnyPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> self, in LuminTaskAwaiter<T10> awaiter)
        {
            T10 result;
            try
            {
                result = awaiter.GetResult();
            }
            catch (Exception ex)
            {
                self.core.TrySetException(ex);
                return;
            }

            if (Interlocked.Increment(ref self.completedCount) == 1)
            {
                self.core.TrySetResult((9, default, default, default, default, default, default, default, default, default, result));
            }
        }


        public (int, T1 result1, T2 result2, T3 result3, T4 result4, T5 result5, T6 result6, T7 result7, T8 result8, T9 result9, T10 result10) GetResult(short token)
        {
            TaskTracker.RemoveTracking(this);
            GC.SuppressFinalize(this);
            return core.GetResult(token);
        }

        public LuminTaskStatus GetStatus(short token)
        {
            return core.GetStatus(token);
        }

        public void OnCompleted(Action<object> continuation, object state, short token)
        {
            core.OnCompleted(continuation, state, token);
        }

        public LuminTaskStatus UnsafeGetStatus()
        {
            return core.UnsafeGetStatus();
        }

        void ILuminTaskSource.GetResult(short token)
        {
            GetResult(token);
        }
    }

    public static LuminTask<(int winArgumentIndex, T1 result1, T2 result2, T3 result3, T4 result4, T5 result5, T6 result6, T7 result7, T8 result8, T9 result9, T10 result10, T11 result11)> WhenAny<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(LuminTask<T1> task1, LuminTask<T2> task2, LuminTask<T3> task3, LuminTask<T4> task4, LuminTask<T5> task5, LuminTask<T6> task6, LuminTask<T7> task7, LuminTask<T8> task8, LuminTask<T9> task9, LuminTask<T10> task10, LuminTask<T11> task11)
    {
        return new LuminTask<(int winArgumentIndex, T1 result1, T2 result2, T3 result3, T4 result4, T5 result5, T6 result6, T7 result7, T8 result8, T9 result9, T10 result10, T11 result11)>(new WhenAnyPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(task1, task2, task3, task4, task5, task6, task7, task8, task9, task10, task11), 0);
    }

    sealed class WhenAnyPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> : ILuminTaskSource<(int, T1 result1, T2 result2, T3 result3, T4 result4, T5 result5, T6 result6, T7 result7, T8 result8, T9 result9, T10 result10, T11 result11)>
    {
        int completedCount;
        LuminTaskCompletionSourceCore<(int, T1 result1, T2 result2, T3 result3, T4 result4, T5 result5, T6 result6, T7 result7, T8 result8, T9 result9, T10 result10, T11 result11)> core;

        public WhenAnyPromise(LuminTask<T1> task1, LuminTask<T2> task2, LuminTask<T3> task3, LuminTask<T4> task4, LuminTask<T5> task5, LuminTask<T6> task6, LuminTask<T7> task7, LuminTask<T8> task8, LuminTask<T9> task9, LuminTask<T10> task10, LuminTask<T11> task11)
        {
            TaskTracker.TrackActiveTask(this, 3);

            this.completedCount = 0;
            {
                var awaiter = task1.GetAwaiter();

                if (awaiter.IsCompleted)
                {
                    TryInvokeContinuationT1(this, awaiter);
                }
                else
                {
                    awaiter.SourceOnCompleted(static state =>
                    {
                        using (var t = (StateTuple<WhenAnyPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>, LuminTaskAwaiter<T1>>)state)
                        {
                            TryInvokeContinuationT1(t.Item1, t.Item2);
                        }
                    }, StateTuple.Create(this, awaiter));
                }
            }
            {
                var awaiter = task2.GetAwaiter();

                if (awaiter.IsCompleted)
                {
                    TryInvokeContinuationT2(this, awaiter);
                }
                else
                {
                    awaiter.SourceOnCompleted(static state =>
                    {
                        using (var t = (StateTuple<WhenAnyPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>, LuminTaskAwaiter<T2>>)state)
                        {
                            TryInvokeContinuationT2(t.Item1, t.Item2);
                        }
                    }, StateTuple.Create(this, awaiter));
                }
            }
            {
                var awaiter = task3.GetAwaiter();

                if (awaiter.IsCompleted)
                {
                    TryInvokeContinuationT3(this, awaiter);
                }
                else
                {
                    awaiter.SourceOnCompleted(static state =>
                    {
                        using (var t = (StateTuple<WhenAnyPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>, LuminTaskAwaiter<T3>>)state)
                        {
                            TryInvokeContinuationT3(t.Item1, t.Item2);
                        }
                    }, StateTuple.Create(this, awaiter));
                }
            }
            {
                var awaiter = task4.GetAwaiter();

                if (awaiter.IsCompleted)
                {
                    TryInvokeContinuationT4(this, awaiter);
                }
                else
                {
                    awaiter.SourceOnCompleted(static state =>
                    {
                        using (var t = (StateTuple<WhenAnyPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>, LuminTaskAwaiter<T4>>)state)
                        {
                            TryInvokeContinuationT4(t.Item1, t.Item2);
                        }
                    }, StateTuple.Create(this, awaiter));
                }
            }
            {
                var awaiter = task5.GetAwaiter();

                if (awaiter.IsCompleted)
                {
                    TryInvokeContinuationT5(this, awaiter);
                }
                else
                {
                    awaiter.SourceOnCompleted(static state =>
                    {
                        using (var t = (StateTuple<WhenAnyPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>, LuminTaskAwaiter<T5>>)state)
                        {
                            TryInvokeContinuationT5(t.Item1, t.Item2);
                        }
                    }, StateTuple.Create(this, awaiter));
                }
            }
            {
                var awaiter = task6.GetAwaiter();

                if (awaiter.IsCompleted)
                {
                    TryInvokeContinuationT6(this, awaiter);
                }
                else
                {
                    awaiter.SourceOnCompleted(static state =>
                    {
                        using (var t = (StateTuple<WhenAnyPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>, LuminTaskAwaiter<T6>>)state)
                        {
                            TryInvokeContinuationT6(t.Item1, t.Item2);
                        }
                    }, StateTuple.Create(this, awaiter));
                }
            }
            {
                var awaiter = task7.GetAwaiter();

                if (awaiter.IsCompleted)
                {
                    TryInvokeContinuationT7(this, awaiter);
                }
                else
                {
                    awaiter.SourceOnCompleted(static state =>
                    {
                        using (var t = (StateTuple<WhenAnyPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>, LuminTaskAwaiter<T7>>)state)
                        {
                            TryInvokeContinuationT7(t.Item1, t.Item2);
                        }
                    }, StateTuple.Create(this, awaiter));
                }
            }
            {
                var awaiter = task8.GetAwaiter();

                if (awaiter.IsCompleted)
                {
                    TryInvokeContinuationT8(this, awaiter);
                }
                else
                {
                    awaiter.SourceOnCompleted(static state =>
                    {
                        using (var t = (StateTuple<WhenAnyPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>, LuminTaskAwaiter<T8>>)state)
                        {
                            TryInvokeContinuationT8(t.Item1, t.Item2);
                        }
                    }, StateTuple.Create(this, awaiter));
                }
            }
            {
                var awaiter = task9.GetAwaiter();

                if (awaiter.IsCompleted)
                {
                    TryInvokeContinuationT9(this, awaiter);
                }
                else
                {
                    awaiter.SourceOnCompleted(static state =>
                    {
                        using (var t = (StateTuple<WhenAnyPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>, LuminTaskAwaiter<T9>>)state)
                        {
                            TryInvokeContinuationT9(t.Item1, t.Item2);
                        }
                    }, StateTuple.Create(this, awaiter));
                }
            }
            {
                var awaiter = task10.GetAwaiter();

                if (awaiter.IsCompleted)
                {
                    TryInvokeContinuationT10(this, awaiter);
                }
                else
                {
                    awaiter.SourceOnCompleted(static state =>
                    {
                        using (var t = (StateTuple<WhenAnyPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>, LuminTaskAwaiter<T10>>)state)
                        {
                            TryInvokeContinuationT10(t.Item1, t.Item2);
                        }
                    }, StateTuple.Create(this, awaiter));
                }
            }
            {
                var awaiter = task11.GetAwaiter();

                if (awaiter.IsCompleted)
                {
                    TryInvokeContinuationT11(this, awaiter);
                }
                else
                {
                    awaiter.SourceOnCompleted(static state =>
                    {
                        using (var t = (StateTuple<WhenAnyPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>, LuminTaskAwaiter<T11>>)state)
                        {
                            TryInvokeContinuationT11(t.Item1, t.Item2);
                        }
                    }, StateTuple.Create(this, awaiter));
                }
            }
        }

        static void TryInvokeContinuationT1(WhenAnyPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> self, in LuminTaskAwaiter<T1> awaiter)
        {
            T1 result;
            try
            {
                result = awaiter.GetResult();
            }
            catch (Exception ex)
            {
                self.core.TrySetException(ex);
                return;
            }

            if (Interlocked.Increment(ref self.completedCount) == 1)
            {
                self.core.TrySetResult((0, result, default, default, default, default, default, default, default, default, default, default));
            }
        }

        static void TryInvokeContinuationT2(WhenAnyPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> self, in LuminTaskAwaiter<T2> awaiter)
        {
            T2 result;
            try
            {
                result = awaiter.GetResult();
            }
            catch (Exception ex)
            {
                self.core.TrySetException(ex);
                return;
            }

            if (Interlocked.Increment(ref self.completedCount) == 1)
            {
                self.core.TrySetResult((1, default, result, default, default, default, default, default, default, default, default, default));
            }
        }

        static void TryInvokeContinuationT3(WhenAnyPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> self, in LuminTaskAwaiter<T3> awaiter)
        {
            T3 result;
            try
            {
                result = awaiter.GetResult();
            }
            catch (Exception ex)
            {
                self.core.TrySetException(ex);
                return;
            }

            if (Interlocked.Increment(ref self.completedCount) == 1)
            {
                self.core.TrySetResult((2, default, default, result, default, default, default, default, default, default, default, default));
            }
        }

        static void TryInvokeContinuationT4(WhenAnyPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> self, in LuminTaskAwaiter<T4> awaiter)
        {
            T4 result;
            try
            {
                result = awaiter.GetResult();
            }
            catch (Exception ex)
            {
                self.core.TrySetException(ex);
                return;
            }

            if (Interlocked.Increment(ref self.completedCount) == 1)
            {
                self.core.TrySetResult((3, default, default, default, result, default, default, default, default, default, default, default));
            }
        }

        static void TryInvokeContinuationT5(WhenAnyPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> self, in LuminTaskAwaiter<T5> awaiter)
        {
            T5 result;
            try
            {
                result = awaiter.GetResult();
            }
            catch (Exception ex)
            {
                self.core.TrySetException(ex);
                return;
            }

            if (Interlocked.Increment(ref self.completedCount) == 1)
            {
                self.core.TrySetResult((4, default, default, default, default, result, default, default, default, default, default, default));
            }
        }

        static void TryInvokeContinuationT6(WhenAnyPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> self, in LuminTaskAwaiter<T6> awaiter)
        {
            T6 result;
            try
            {
                result = awaiter.GetResult();
            }
            catch (Exception ex)
            {
                self.core.TrySetException(ex);
                return;
            }

            if (Interlocked.Increment(ref self.completedCount) == 1)
            {
                self.core.TrySetResult((5, default, default, default, default, default, result, default, default, default, default, default));
            }
        }

        static void TryInvokeContinuationT7(WhenAnyPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> self, in LuminTaskAwaiter<T7> awaiter)
        {
            T7 result;
            try
            {
                result = awaiter.GetResult();
            }
            catch (Exception ex)
            {
                self.core.TrySetException(ex);
                return;
            }

            if (Interlocked.Increment(ref self.completedCount) == 1)
            {
                self.core.TrySetResult((6, default, default, default, default, default, default, result, default, default, default, default));
            }
        }

        static void TryInvokeContinuationT8(WhenAnyPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> self, in LuminTaskAwaiter<T8> awaiter)
        {
            T8 result;
            try
            {
                result = awaiter.GetResult();
            }
            catch (Exception ex)
            {
                self.core.TrySetException(ex);
                return;
            }

            if (Interlocked.Increment(ref self.completedCount) == 1)
            {
                self.core.TrySetResult((7, default, default, default, default, default, default, default, result, default, default, default));
            }
        }

        static void TryInvokeContinuationT9(WhenAnyPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> self, in LuminTaskAwaiter<T9> awaiter)
        {
            T9 result;
            try
            {
                result = awaiter.GetResult();
            }
            catch (Exception ex)
            {
                self.core.TrySetException(ex);
                return;
            }

            if (Interlocked.Increment(ref self.completedCount) == 1)
            {
                self.core.TrySetResult((8, default, default, default, default, default, default, default, default, result, default, default));
            }
        }

        static void TryInvokeContinuationT10(WhenAnyPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> self, in LuminTaskAwaiter<T10> awaiter)
        {
            T10 result;
            try
            {
                result = awaiter.GetResult();
            }
            catch (Exception ex)
            {
                self.core.TrySetException(ex);
                return;
            }

            if (Interlocked.Increment(ref self.completedCount) == 1)
            {
                self.core.TrySetResult((9, default, default, default, default, default, default, default, default, default, result, default));
            }
        }

        static void TryInvokeContinuationT11(WhenAnyPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> self, in LuminTaskAwaiter<T11> awaiter)
        {
            T11 result;
            try
            {
                result = awaiter.GetResult();
            }
            catch (Exception ex)
            {
                self.core.TrySetException(ex);
                return;
            }

            if (Interlocked.Increment(ref self.completedCount) == 1)
            {
                self.core.TrySetResult((10, default, default, default, default, default, default, default, default, default, default, result));
            }
        }


        public (int, T1 result1, T2 result2, T3 result3, T4 result4, T5 result5, T6 result6, T7 result7, T8 result8, T9 result9, T10 result10, T11 result11) GetResult(short token)
        {
            TaskTracker.RemoveTracking(this);
            GC.SuppressFinalize(this);
            return core.GetResult(token);
        }

        public LuminTaskStatus GetStatus(short token)
        {
            return core.GetStatus(token);
        }

        public void OnCompleted(Action<object> continuation, object state, short token)
        {
            core.OnCompleted(continuation, state, token);
        }

        public LuminTaskStatus UnsafeGetStatus()
        {
            return core.UnsafeGetStatus();
        }

        void ILuminTaskSource.GetResult(short token)
        {
            GetResult(token);
        }
    }

    public static LuminTask<(int winArgumentIndex, T1 result1, T2 result2, T3 result3, T4 result4, T5 result5, T6 result6, T7 result7, T8 result8, T9 result9, T10 result10, T11 result11, T12 result12)> WhenAny<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(LuminTask<T1> task1, LuminTask<T2> task2, LuminTask<T3> task3, LuminTask<T4> task4, LuminTask<T5> task5, LuminTask<T6> task6, LuminTask<T7> task7, LuminTask<T8> task8, LuminTask<T9> task9, LuminTask<T10> task10, LuminTask<T11> task11, LuminTask<T12> task12)
    {
        return new LuminTask<(int winArgumentIndex, T1 result1, T2 result2, T3 result3, T4 result4, T5 result5, T6 result6, T7 result7, T8 result8, T9 result9, T10 result10, T11 result11, T12 result12)>(new WhenAnyPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(task1, task2, task3, task4, task5, task6, task7, task8, task9, task10, task11, task12), 0);
    }

    sealed class WhenAnyPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> : ILuminTaskSource<(int, T1 result1, T2 result2, T3 result3, T4 result4, T5 result5, T6 result6, T7 result7, T8 result8, T9 result9, T10 result10, T11 result11, T12 result12)>
    {
        int completedCount;
        LuminTaskCompletionSourceCore<(int, T1 result1, T2 result2, T3 result3, T4 result4, T5 result5, T6 result6, T7 result7, T8 result8, T9 result9, T10 result10, T11 result11, T12 result12)> core;

        public WhenAnyPromise(LuminTask<T1> task1, LuminTask<T2> task2, LuminTask<T3> task3, LuminTask<T4> task4, LuminTask<T5> task5, LuminTask<T6> task6, LuminTask<T7> task7, LuminTask<T8> task8, LuminTask<T9> task9, LuminTask<T10> task10, LuminTask<T11> task11, LuminTask<T12> task12)
        {
            TaskTracker.TrackActiveTask(this, 3);

            this.completedCount = 0;
            {
                var awaiter = task1.GetAwaiter();

                if (awaiter.IsCompleted)
                {
                    TryInvokeContinuationT1(this, awaiter);
                }
                else
                {
                    awaiter.SourceOnCompleted(static state =>
                    {
                        using (var t = (StateTuple<WhenAnyPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>, LuminTaskAwaiter<T1>>)state)
                        {
                            TryInvokeContinuationT1(t.Item1, t.Item2);
                        }
                    }, StateTuple.Create(this, awaiter));
                }
            }
            {
                var awaiter = task2.GetAwaiter();

                if (awaiter.IsCompleted)
                {
                    TryInvokeContinuationT2(this, awaiter);
                }
                else
                {
                    awaiter.SourceOnCompleted(static state =>
                    {
                        using (var t = (StateTuple<WhenAnyPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>, LuminTaskAwaiter<T2>>)state)
                        {
                            TryInvokeContinuationT2(t.Item1, t.Item2);
                        }
                    }, StateTuple.Create(this, awaiter));
                }
            }
            {
                var awaiter = task3.GetAwaiter();

                if (awaiter.IsCompleted)
                {
                    TryInvokeContinuationT3(this, awaiter);
                }
                else
                {
                    awaiter.SourceOnCompleted(static state =>
                    {
                        using (var t = (StateTuple<WhenAnyPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>, LuminTaskAwaiter<T3>>)state)
                        {
                            TryInvokeContinuationT3(t.Item1, t.Item2);
                        }
                    }, StateTuple.Create(this, awaiter));
                }
            }
            {
                var awaiter = task4.GetAwaiter();

                if (awaiter.IsCompleted)
                {
                    TryInvokeContinuationT4(this, awaiter);
                }
                else
                {
                    awaiter.SourceOnCompleted(static state =>
                    {
                        using (var t = (StateTuple<WhenAnyPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>, LuminTaskAwaiter<T4>>)state)
                        {
                            TryInvokeContinuationT4(t.Item1, t.Item2);
                        }
                    }, StateTuple.Create(this, awaiter));
                }
            }
            {
                var awaiter = task5.GetAwaiter();

                if (awaiter.IsCompleted)
                {
                    TryInvokeContinuationT5(this, awaiter);
                }
                else
                {
                    awaiter.SourceOnCompleted(static state =>
                    {
                        using (var t = (StateTuple<WhenAnyPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>, LuminTaskAwaiter<T5>>)state)
                        {
                            TryInvokeContinuationT5(t.Item1, t.Item2);
                        }
                    }, StateTuple.Create(this, awaiter));
                }
            }
            {
                var awaiter = task6.GetAwaiter();

                if (awaiter.IsCompleted)
                {
                    TryInvokeContinuationT6(this, awaiter);
                }
                else
                {
                    awaiter.SourceOnCompleted(static state =>
                    {
                        using (var t = (StateTuple<WhenAnyPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>, LuminTaskAwaiter<T6>>)state)
                        {
                            TryInvokeContinuationT6(t.Item1, t.Item2);
                        }
                    }, StateTuple.Create(this, awaiter));
                }
            }
            {
                var awaiter = task7.GetAwaiter();

                if (awaiter.IsCompleted)
                {
                    TryInvokeContinuationT7(this, awaiter);
                }
                else
                {
                    awaiter.SourceOnCompleted(static state =>
                    {
                        using (var t = (StateTuple<WhenAnyPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>, LuminTaskAwaiter<T7>>)state)
                        {
                            TryInvokeContinuationT7(t.Item1, t.Item2);
                        }
                    }, StateTuple.Create(this, awaiter));
                }
            }
            {
                var awaiter = task8.GetAwaiter();

                if (awaiter.IsCompleted)
                {
                    TryInvokeContinuationT8(this, awaiter);
                }
                else
                {
                    awaiter.SourceOnCompleted(static state =>
                    {
                        using (var t = (StateTuple<WhenAnyPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>, LuminTaskAwaiter<T8>>)state)
                        {
                            TryInvokeContinuationT8(t.Item1, t.Item2);
                        }
                    }, StateTuple.Create(this, awaiter));
                }
            }
            {
                var awaiter = task9.GetAwaiter();

                if (awaiter.IsCompleted)
                {
                    TryInvokeContinuationT9(this, awaiter);
                }
                else
                {
                    awaiter.SourceOnCompleted(static state =>
                    {
                        using (var t = (StateTuple<WhenAnyPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>, LuminTaskAwaiter<T9>>)state)
                        {
                            TryInvokeContinuationT9(t.Item1, t.Item2);
                        }
                    }, StateTuple.Create(this, awaiter));
                }
            }
            {
                var awaiter = task10.GetAwaiter();

                if (awaiter.IsCompleted)
                {
                    TryInvokeContinuationT10(this, awaiter);
                }
                else
                {
                    awaiter.SourceOnCompleted(static state =>
                    {
                        using (var t = (StateTuple<WhenAnyPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>, LuminTaskAwaiter<T10>>)state)
                        {
                            TryInvokeContinuationT10(t.Item1, t.Item2);
                        }
                    }, StateTuple.Create(this, awaiter));
                }
            }
            {
                var awaiter = task11.GetAwaiter();

                if (awaiter.IsCompleted)
                {
                    TryInvokeContinuationT11(this, awaiter);
                }
                else
                {
                    awaiter.SourceOnCompleted(static state =>
                    {
                        using (var t = (StateTuple<WhenAnyPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>, LuminTaskAwaiter<T11>>)state)
                        {
                            TryInvokeContinuationT11(t.Item1, t.Item2);
                        }
                    }, StateTuple.Create(this, awaiter));
                }
            }
            {
                var awaiter = task12.GetAwaiter();

                if (awaiter.IsCompleted)
                {
                    TryInvokeContinuationT12(this, awaiter);
                }
                else
                {
                    awaiter.SourceOnCompleted(static state =>
                    {
                        using (var t = (StateTuple<WhenAnyPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>, LuminTaskAwaiter<T12>>)state)
                        {
                            TryInvokeContinuationT12(t.Item1, t.Item2);
                        }
                    }, StateTuple.Create(this, awaiter));
                }
            }
        }

        static void TryInvokeContinuationT1(WhenAnyPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> self, in LuminTaskAwaiter<T1> awaiter)
        {
            T1 result;
            try
            {
                result = awaiter.GetResult();
            }
            catch (Exception ex)
            {
                self.core.TrySetException(ex);
                return;
            }

            if (Interlocked.Increment(ref self.completedCount) == 1)
            {
                self.core.TrySetResult((0, result, default, default, default, default, default, default, default, default, default, default, default));
            }
        }

        static void TryInvokeContinuationT2(WhenAnyPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> self, in LuminTaskAwaiter<T2> awaiter)
        {
            T2 result;
            try
            {
                result = awaiter.GetResult();
            }
            catch (Exception ex)
            {
                self.core.TrySetException(ex);
                return;
            }

            if (Interlocked.Increment(ref self.completedCount) == 1)
            {
                self.core.TrySetResult((1, default, result, default, default, default, default, default, default, default, default, default, default));
            }
        }

        static void TryInvokeContinuationT3(WhenAnyPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> self, in LuminTaskAwaiter<T3> awaiter)
        {
            T3 result;
            try
            {
                result = awaiter.GetResult();
            }
            catch (Exception ex)
            {
                self.core.TrySetException(ex);
                return;
            }

            if (Interlocked.Increment(ref self.completedCount) == 1)
            {
                self.core.TrySetResult((2, default, default, result, default, default, default, default, default, default, default, default, default));
            }
        }

        static void TryInvokeContinuationT4(WhenAnyPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> self, in LuminTaskAwaiter<T4> awaiter)
        {
            T4 result;
            try
            {
                result = awaiter.GetResult();
            }
            catch (Exception ex)
            {
                self.core.TrySetException(ex);
                return;
            }

            if (Interlocked.Increment(ref self.completedCount) == 1)
            {
                self.core.TrySetResult((3, default, default, default, result, default, default, default, default, default, default, default, default));
            }
        }

        static void TryInvokeContinuationT5(WhenAnyPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> self, in LuminTaskAwaiter<T5> awaiter)
        {
            T5 result;
            try
            {
                result = awaiter.GetResult();
            }
            catch (Exception ex)
            {
                self.core.TrySetException(ex);
                return;
            }

            if (Interlocked.Increment(ref self.completedCount) == 1)
            {
                self.core.TrySetResult((4, default, default, default, default, result, default, default, default, default, default, default, default));
            }
        }

        static void TryInvokeContinuationT6(WhenAnyPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> self, in LuminTaskAwaiter<T6> awaiter)
        {
            T6 result;
            try
            {
                result = awaiter.GetResult();
            }
            catch (Exception ex)
            {
                self.core.TrySetException(ex);
                return;
            }

            if (Interlocked.Increment(ref self.completedCount) == 1)
            {
                self.core.TrySetResult((5, default, default, default, default, default, result, default, default, default, default, default, default));
            }
        }

        static void TryInvokeContinuationT7(WhenAnyPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> self, in LuminTaskAwaiter<T7> awaiter)
        {
            T7 result;
            try
            {
                result = awaiter.GetResult();
            }
            catch (Exception ex)
            {
                self.core.TrySetException(ex);
                return;
            }

            if (Interlocked.Increment(ref self.completedCount) == 1)
            {
                self.core.TrySetResult((6, default, default, default, default, default, default, result, default, default, default, default, default));
            }
        }

        static void TryInvokeContinuationT8(WhenAnyPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> self, in LuminTaskAwaiter<T8> awaiter)
        {
            T8 result;
            try
            {
                result = awaiter.GetResult();
            }
            catch (Exception ex)
            {
                self.core.TrySetException(ex);
                return;
            }

            if (Interlocked.Increment(ref self.completedCount) == 1)
            {
                self.core.TrySetResult((7, default, default, default, default, default, default, default, result, default, default, default, default));
            }
        }

        static void TryInvokeContinuationT9(WhenAnyPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> self, in LuminTaskAwaiter<T9> awaiter)
        {
            T9 result;
            try
            {
                result = awaiter.GetResult();
            }
            catch (Exception ex)
            {
                self.core.TrySetException(ex);
                return;
            }

            if (Interlocked.Increment(ref self.completedCount) == 1)
            {
                self.core.TrySetResult((8, default, default, default, default, default, default, default, default, result, default, default, default));
            }
        }

        static void TryInvokeContinuationT10(WhenAnyPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> self, in LuminTaskAwaiter<T10> awaiter)
        {
            T10 result;
            try
            {
                result = awaiter.GetResult();
            }
            catch (Exception ex)
            {
                self.core.TrySetException(ex);
                return;
            }

            if (Interlocked.Increment(ref self.completedCount) == 1)
            {
                self.core.TrySetResult((9, default, default, default, default, default, default, default, default, default, result, default, default));
            }
        }

        static void TryInvokeContinuationT11(WhenAnyPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> self, in LuminTaskAwaiter<T11> awaiter)
        {
            T11 result;
            try
            {
                result = awaiter.GetResult();
            }
            catch (Exception ex)
            {
                self.core.TrySetException(ex);
                return;
            }

            if (Interlocked.Increment(ref self.completedCount) == 1)
            {
                self.core.TrySetResult((10, default, default, default, default, default, default, default, default, default, default, result, default));
            }
        }

        static void TryInvokeContinuationT12(WhenAnyPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> self, in LuminTaskAwaiter<T12> awaiter)
        {
            T12 result;
            try
            {
                result = awaiter.GetResult();
            }
            catch (Exception ex)
            {
                self.core.TrySetException(ex);
                return;
            }

            if (Interlocked.Increment(ref self.completedCount) == 1)
            {
                self.core.TrySetResult((11, default, default, default, default, default, default, default, default, default, default, default, result));
            }
        }


        public (int, T1 result1, T2 result2, T3 result3, T4 result4, T5 result5, T6 result6, T7 result7, T8 result8, T9 result9, T10 result10, T11 result11, T12 result12) GetResult(short token)
        {
            TaskTracker.RemoveTracking(this);
            GC.SuppressFinalize(this);
            return core.GetResult(token);
        }

        public LuminTaskStatus GetStatus(short token)
        {
            return core.GetStatus(token);
        }

        public void OnCompleted(Action<object> continuation, object state, short token)
        {
            core.OnCompleted(continuation, state, token);
        }

        public LuminTaskStatus UnsafeGetStatus()
        {
            return core.UnsafeGetStatus();
        }

        void ILuminTaskSource.GetResult(short token)
        {
            GetResult(token);
        }
    }

    public static LuminTask<(int winArgumentIndex, T1 result1, T2 result2, T3 result3, T4 result4, T5 result5, T6 result6, T7 result7, T8 result8, T9 result9, T10 result10, T11 result11, T12 result12, T13 result13)> WhenAny<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(LuminTask<T1> task1, LuminTask<T2> task2, LuminTask<T3> task3, LuminTask<T4> task4, LuminTask<T5> task5, LuminTask<T6> task6, LuminTask<T7> task7, LuminTask<T8> task8, LuminTask<T9> task9, LuminTask<T10> task10, LuminTask<T11> task11, LuminTask<T12> task12, LuminTask<T13> task13)
    {
        return new LuminTask<(int winArgumentIndex, T1 result1, T2 result2, T3 result3, T4 result4, T5 result5, T6 result6, T7 result7, T8 result8, T9 result9, T10 result10, T11 result11, T12 result12, T13 result13)>(new WhenAnyPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(task1, task2, task3, task4, task5, task6, task7, task8, task9, task10, task11, task12, task13), 0);
    }

    sealed class WhenAnyPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13> : ILuminTaskSource<(int, T1 result1, T2 result2, T3 result3, T4 result4, T5 result5, T6 result6, T7 result7, T8 result8, T9 result9, T10 result10, T11 result11, T12 result12, T13 result13)>
    {
        int completedCount;
        LuminTaskCompletionSourceCore<(int, T1 result1, T2 result2, T3 result3, T4 result4, T5 result5, T6 result6, T7 result7, T8 result8, T9 result9, T10 result10, T11 result11, T12 result12, T13 result13)> core;

        public WhenAnyPromise(LuminTask<T1> task1, LuminTask<T2> task2, LuminTask<T3> task3, LuminTask<T4> task4, LuminTask<T5> task5, LuminTask<T6> task6, LuminTask<T7> task7, LuminTask<T8> task8, LuminTask<T9> task9, LuminTask<T10> task10, LuminTask<T11> task11, LuminTask<T12> task12, LuminTask<T13> task13)
        {
            TaskTracker.TrackActiveTask(this, 3);

            this.completedCount = 0;
            {
                var awaiter = task1.GetAwaiter();

                if (awaiter.IsCompleted)
                {
                    TryInvokeContinuationT1(this, awaiter);
                }
                else
                {
                    awaiter.SourceOnCompleted(static state =>
                    {
                        using (var t = (StateTuple<WhenAnyPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>, LuminTaskAwaiter<T1>>)state)
                        {
                            TryInvokeContinuationT1(t.Item1, t.Item2);
                        }
                    }, StateTuple.Create(this, awaiter));
                }
            }
            {
                var awaiter = task2.GetAwaiter();

                if (awaiter.IsCompleted)
                {
                    TryInvokeContinuationT2(this, awaiter);
                }
                else
                {
                    awaiter.SourceOnCompleted(static state =>
                    {
                        using (var t = (StateTuple<WhenAnyPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>, LuminTaskAwaiter<T2>>)state)
                        {
                            TryInvokeContinuationT2(t.Item1, t.Item2);
                        }
                    }, StateTuple.Create(this, awaiter));
                }
            }
            {
                var awaiter = task3.GetAwaiter();

                if (awaiter.IsCompleted)
                {
                    TryInvokeContinuationT3(this, awaiter);
                }
                else
                {
                    awaiter.SourceOnCompleted(static state =>
                    {
                        using (var t = (StateTuple<WhenAnyPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>, LuminTaskAwaiter<T3>>)state)
                        {
                            TryInvokeContinuationT3(t.Item1, t.Item2);
                        }
                    }, StateTuple.Create(this, awaiter));
                }
            }
            {
                var awaiter = task4.GetAwaiter();

                if (awaiter.IsCompleted)
                {
                    TryInvokeContinuationT4(this, awaiter);
                }
                else
                {
                    awaiter.SourceOnCompleted(static state =>
                    {
                        using (var t = (StateTuple<WhenAnyPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>, LuminTaskAwaiter<T4>>)state)
                        {
                            TryInvokeContinuationT4(t.Item1, t.Item2);
                        }
                    }, StateTuple.Create(this, awaiter));
                }
            }
            {
                var awaiter = task5.GetAwaiter();

                if (awaiter.IsCompleted)
                {
                    TryInvokeContinuationT5(this, awaiter);
                }
                else
                {
                    awaiter.SourceOnCompleted(static state =>
                    {
                        using (var t = (StateTuple<WhenAnyPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>, LuminTaskAwaiter<T5>>)state)
                        {
                            TryInvokeContinuationT5(t.Item1, t.Item2);
                        }
                    }, StateTuple.Create(this, awaiter));
                }
            }
            {
                var awaiter = task6.GetAwaiter();

                if (awaiter.IsCompleted)
                {
                    TryInvokeContinuationT6(this, awaiter);
                }
                else
                {
                    awaiter.SourceOnCompleted(static state =>
                    {
                        using (var t = (StateTuple<WhenAnyPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>, LuminTaskAwaiter<T6>>)state)
                        {
                            TryInvokeContinuationT6(t.Item1, t.Item2);
                        }
                    }, StateTuple.Create(this, awaiter));
                }
            }
            {
                var awaiter = task7.GetAwaiter();

                if (awaiter.IsCompleted)
                {
                    TryInvokeContinuationT7(this, awaiter);
                }
                else
                {
                    awaiter.SourceOnCompleted(static state =>
                    {
                        using (var t = (StateTuple<WhenAnyPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>, LuminTaskAwaiter<T7>>)state)
                        {
                            TryInvokeContinuationT7(t.Item1, t.Item2);
                        }
                    }, StateTuple.Create(this, awaiter));
                }
            }
            {
                var awaiter = task8.GetAwaiter();

                if (awaiter.IsCompleted)
                {
                    TryInvokeContinuationT8(this, awaiter);
                }
                else
                {
                    awaiter.SourceOnCompleted(static state =>
                    {
                        using (var t = (StateTuple<WhenAnyPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>, LuminTaskAwaiter<T8>>)state)
                        {
                            TryInvokeContinuationT8(t.Item1, t.Item2);
                        }
                    }, StateTuple.Create(this, awaiter));
                }
            }
            {
                var awaiter = task9.GetAwaiter();

                if (awaiter.IsCompleted)
                {
                    TryInvokeContinuationT9(this, awaiter);
                }
                else
                {
                    awaiter.SourceOnCompleted(static state =>
                    {
                        using (var t = (StateTuple<WhenAnyPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>, LuminTaskAwaiter<T9>>)state)
                        {
                            TryInvokeContinuationT9(t.Item1, t.Item2);
                        }
                    }, StateTuple.Create(this, awaiter));
                }
            }
            {
                var awaiter = task10.GetAwaiter();

                if (awaiter.IsCompleted)
                {
                    TryInvokeContinuationT10(this, awaiter);
                }
                else
                {
                    awaiter.SourceOnCompleted(static state =>
                    {
                        using (var t = (StateTuple<WhenAnyPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>, LuminTaskAwaiter<T10>>)state)
                        {
                            TryInvokeContinuationT10(t.Item1, t.Item2);
                        }
                    }, StateTuple.Create(this, awaiter));
                }
            }
            {
                var awaiter = task11.GetAwaiter();

                if (awaiter.IsCompleted)
                {
                    TryInvokeContinuationT11(this, awaiter);
                }
                else
                {
                    awaiter.SourceOnCompleted(static state =>
                    {
                        using (var t = (StateTuple<WhenAnyPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>, LuminTaskAwaiter<T11>>)state)
                        {
                            TryInvokeContinuationT11(t.Item1, t.Item2);
                        }
                    }, StateTuple.Create(this, awaiter));
                }
            }
            {
                var awaiter = task12.GetAwaiter();

                if (awaiter.IsCompleted)
                {
                    TryInvokeContinuationT12(this, awaiter);
                }
                else
                {
                    awaiter.SourceOnCompleted(static state =>
                    {
                        using (var t = (StateTuple<WhenAnyPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>, LuminTaskAwaiter<T12>>)state)
                        {
                            TryInvokeContinuationT12(t.Item1, t.Item2);
                        }
                    }, StateTuple.Create(this, awaiter));
                }
            }
            {
                var awaiter = task13.GetAwaiter();

                if (awaiter.IsCompleted)
                {
                    TryInvokeContinuationT13(this, awaiter);
                }
                else
                {
                    awaiter.SourceOnCompleted(static state =>
                    {
                        using (var t = (StateTuple<WhenAnyPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>, LuminTaskAwaiter<T13>>)state)
                        {
                            TryInvokeContinuationT13(t.Item1, t.Item2);
                        }
                    }, StateTuple.Create(this, awaiter));
                }
            }
        }

        static void TryInvokeContinuationT1(WhenAnyPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13> self, in LuminTaskAwaiter<T1> awaiter)
        {
            T1 result;
            try
            {
                result = awaiter.GetResult();
            }
            catch (Exception ex)
            {
                self.core.TrySetException(ex);
                return;
            }

            if (Interlocked.Increment(ref self.completedCount) == 1)
            {
                self.core.TrySetResult((0, result, default, default, default, default, default, default, default, default, default, default, default, default));
            }
        }

        static void TryInvokeContinuationT2(WhenAnyPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13> self, in LuminTaskAwaiter<T2> awaiter)
        {
            T2 result;
            try
            {
                result = awaiter.GetResult();
            }
            catch (Exception ex)
            {
                self.core.TrySetException(ex);
                return;
            }

            if (Interlocked.Increment(ref self.completedCount) == 1)
            {
                self.core.TrySetResult((1, default, result, default, default, default, default, default, default, default, default, default, default, default));
            }
        }

        static void TryInvokeContinuationT3(WhenAnyPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13> self, in LuminTaskAwaiter<T3> awaiter)
        {
            T3 result;
            try
            {
                result = awaiter.GetResult();
            }
            catch (Exception ex)
            {
                self.core.TrySetException(ex);
                return;
            }

            if (Interlocked.Increment(ref self.completedCount) == 1)
            {
                self.core.TrySetResult((2, default, default, result, default, default, default, default, default, default, default, default, default, default));
            }
        }

        static void TryInvokeContinuationT4(WhenAnyPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13> self, in LuminTaskAwaiter<T4> awaiter)
        {
            T4 result;
            try
            {
                result = awaiter.GetResult();
            }
            catch (Exception ex)
            {
                self.core.TrySetException(ex);
                return;
            }

            if (Interlocked.Increment(ref self.completedCount) == 1)
            {
                self.core.TrySetResult((3, default, default, default, result, default, default, default, default, default, default, default, default, default));
            }
        }

        static void TryInvokeContinuationT5(WhenAnyPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13> self, in LuminTaskAwaiter<T5> awaiter)
        {
            T5 result;
            try
            {
                result = awaiter.GetResult();
            }
            catch (Exception ex)
            {
                self.core.TrySetException(ex);
                return;
            }

            if (Interlocked.Increment(ref self.completedCount) == 1)
            {
                self.core.TrySetResult((4, default, default, default, default, result, default, default, default, default, default, default, default, default));
            }
        }

        static void TryInvokeContinuationT6(WhenAnyPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13> self, in LuminTaskAwaiter<T6> awaiter)
        {
            T6 result;
            try
            {
                result = awaiter.GetResult();
            }
            catch (Exception ex)
            {
                self.core.TrySetException(ex);
                return;
            }

            if (Interlocked.Increment(ref self.completedCount) == 1)
            {
                self.core.TrySetResult((5, default, default, default, default, default, result, default, default, default, default, default, default, default));
            }
        }

        static void TryInvokeContinuationT7(WhenAnyPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13> self, in LuminTaskAwaiter<T7> awaiter)
        {
            T7 result;
            try
            {
                result = awaiter.GetResult();
            }
            catch (Exception ex)
            {
                self.core.TrySetException(ex);
                return;
            }

            if (Interlocked.Increment(ref self.completedCount) == 1)
            {
                self.core.TrySetResult((6, default, default, default, default, default, default, result, default, default, default, default, default, default));
            }
        }

        static void TryInvokeContinuationT8(WhenAnyPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13> self, in LuminTaskAwaiter<T8> awaiter)
        {
            T8 result;
            try
            {
                result = awaiter.GetResult();
            }
            catch (Exception ex)
            {
                self.core.TrySetException(ex);
                return;
            }

            if (Interlocked.Increment(ref self.completedCount) == 1)
            {
                self.core.TrySetResult((7, default, default, default, default, default, default, default, result, default, default, default, default, default));
            }
        }

        static void TryInvokeContinuationT9(WhenAnyPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13> self, in LuminTaskAwaiter<T9> awaiter)
        {
            T9 result;
            try
            {
                result = awaiter.GetResult();
            }
            catch (Exception ex)
            {
                self.core.TrySetException(ex);
                return;
            }

            if (Interlocked.Increment(ref self.completedCount) == 1)
            {
                self.core.TrySetResult((8, default, default, default, default, default, default, default, default, result, default, default, default, default));
            }
        }

        static void TryInvokeContinuationT10(WhenAnyPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13> self, in LuminTaskAwaiter<T10> awaiter)
        {
            T10 result;
            try
            {
                result = awaiter.GetResult();
            }
            catch (Exception ex)
            {
                self.core.TrySetException(ex);
                return;
            }

            if (Interlocked.Increment(ref self.completedCount) == 1)
            {
                self.core.TrySetResult((9, default, default, default, default, default, default, default, default, default, result, default, default, default));
            }
        }

        static void TryInvokeContinuationT11(WhenAnyPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13> self, in LuminTaskAwaiter<T11> awaiter)
        {
            T11 result;
            try
            {
                result = awaiter.GetResult();
            }
            catch (Exception ex)
            {
                self.core.TrySetException(ex);
                return;
            }

            if (Interlocked.Increment(ref self.completedCount) == 1)
            {
                self.core.TrySetResult((10, default, default, default, default, default, default, default, default, default, default, result, default, default));
            }
        }

        static void TryInvokeContinuationT12(WhenAnyPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13> self, in LuminTaskAwaiter<T12> awaiter)
        {
            T12 result;
            try
            {
                result = awaiter.GetResult();
            }
            catch (Exception ex)
            {
                self.core.TrySetException(ex);
                return;
            }

            if (Interlocked.Increment(ref self.completedCount) == 1)
            {
                self.core.TrySetResult((11, default, default, default, default, default, default, default, default, default, default, default, result, default));
            }
        }

        static void TryInvokeContinuationT13(WhenAnyPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13> self, in LuminTaskAwaiter<T13> awaiter)
        {
            T13 result;
            try
            {
                result = awaiter.GetResult();
            }
            catch (Exception ex)
            {
                self.core.TrySetException(ex);
                return;
            }

            if (Interlocked.Increment(ref self.completedCount) == 1)
            {
                self.core.TrySetResult((12, default, default, default, default, default, default, default, default, default, default, default, default, result));
            }
        }


        public (int, T1 result1, T2 result2, T3 result3, T4 result4, T5 result5, T6 result6, T7 result7, T8 result8, T9 result9, T10 result10, T11 result11, T12 result12, T13 result13) GetResult(short token)
        {
            TaskTracker.RemoveTracking(this);
            GC.SuppressFinalize(this);
            return core.GetResult(token);
        }

        public LuminTaskStatus GetStatus(short token)
        {
            return core.GetStatus(token);
        }

        public void OnCompleted(Action<object> continuation, object state, short token)
        {
            core.OnCompleted(continuation, state, token);
        }

        public LuminTaskStatus UnsafeGetStatus()
        {
            return core.UnsafeGetStatus();
        }

        void ILuminTaskSource.GetResult(short token)
        {
            GetResult(token);
        }
    }

    public static LuminTask<(int winArgumentIndex, T1 result1, T2 result2, T3 result3, T4 result4, T5 result5, T6 result6, T7 result7, T8 result8, T9 result9, T10 result10, T11 result11, T12 result12, T13 result13, T14 result14)> WhenAny<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(LuminTask<T1> task1, LuminTask<T2> task2, LuminTask<T3> task3, LuminTask<T4> task4, LuminTask<T5> task5, LuminTask<T6> task6, LuminTask<T7> task7, LuminTask<T8> task8, LuminTask<T9> task9, LuminTask<T10> task10, LuminTask<T11> task11, LuminTask<T12> task12, LuminTask<T13> task13, LuminTask<T14> task14)
    {
        return new LuminTask<(int winArgumentIndex, T1 result1, T2 result2, T3 result3, T4 result4, T5 result5, T6 result6, T7 result7, T8 result8, T9 result9, T10 result10, T11 result11, T12 result12, T13 result13, T14 result14)>(new WhenAnyPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(task1, task2, task3, task4, task5, task6, task7, task8, task9, task10, task11, task12, task13, task14), 0);
    }

    sealed class WhenAnyPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14> : ILuminTaskSource<(int, T1 result1, T2 result2, T3 result3, T4 result4, T5 result5, T6 result6, T7 result7, T8 result8, T9 result9, T10 result10, T11 result11, T12 result12, T13 result13, T14 result14)>
    {
        int completedCount;
        LuminTaskCompletionSourceCore<(int, T1 result1, T2 result2, T3 result3, T4 result4, T5 result5, T6 result6, T7 result7, T8 result8, T9 result9, T10 result10, T11 result11, T12 result12, T13 result13, T14 result14)> core;

        public WhenAnyPromise(LuminTask<T1> task1, LuminTask<T2> task2, LuminTask<T3> task3, LuminTask<T4> task4, LuminTask<T5> task5, LuminTask<T6> task6, LuminTask<T7> task7, LuminTask<T8> task8, LuminTask<T9> task9, LuminTask<T10> task10, LuminTask<T11> task11, LuminTask<T12> task12, LuminTask<T13> task13, LuminTask<T14> task14)
        {
            TaskTracker.TrackActiveTask(this, 3);

            this.completedCount = 0;
            {
                var awaiter = task1.GetAwaiter();

                if (awaiter.IsCompleted)
                {
                    TryInvokeContinuationT1(this, awaiter);
                }
                else
                {
                    awaiter.SourceOnCompleted(static state =>
                    {
                        using (var t = (StateTuple<WhenAnyPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>, LuminTaskAwaiter<T1>>)state)
                        {
                            TryInvokeContinuationT1(t.Item1, t.Item2);
                        }
                    }, StateTuple.Create(this, awaiter));
                }
            }
            {
                var awaiter = task2.GetAwaiter();

                if (awaiter.IsCompleted)
                {
                    TryInvokeContinuationT2(this, awaiter);
                }
                else
                {
                    awaiter.SourceOnCompleted(static state =>
                    {
                        using (var t = (StateTuple<WhenAnyPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>, LuminTaskAwaiter<T2>>)state)
                        {
                            TryInvokeContinuationT2(t.Item1, t.Item2);
                        }
                    }, StateTuple.Create(this, awaiter));
                }
            }
            {
                var awaiter = task3.GetAwaiter();

                if (awaiter.IsCompleted)
                {
                    TryInvokeContinuationT3(this, awaiter);
                }
                else
                {
                    awaiter.SourceOnCompleted(static state =>
                    {
                        using (var t = (StateTuple<WhenAnyPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>, LuminTaskAwaiter<T3>>)state)
                        {
                            TryInvokeContinuationT3(t.Item1, t.Item2);
                        }
                    }, StateTuple.Create(this, awaiter));
                }
            }
            {
                var awaiter = task4.GetAwaiter();

                if (awaiter.IsCompleted)
                {
                    TryInvokeContinuationT4(this, awaiter);
                }
                else
                {
                    awaiter.SourceOnCompleted(static state =>
                    {
                        using (var t = (StateTuple<WhenAnyPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>, LuminTaskAwaiter<T4>>)state)
                        {
                            TryInvokeContinuationT4(t.Item1, t.Item2);
                        }
                    }, StateTuple.Create(this, awaiter));
                }
            }
            {
                var awaiter = task5.GetAwaiter();

                if (awaiter.IsCompleted)
                {
                    TryInvokeContinuationT5(this, awaiter);
                }
                else
                {
                    awaiter.SourceOnCompleted(static state =>
                    {
                        using (var t = (StateTuple<WhenAnyPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>, LuminTaskAwaiter<T5>>)state)
                        {
                            TryInvokeContinuationT5(t.Item1, t.Item2);
                        }
                    }, StateTuple.Create(this, awaiter));
                }
            }
            {
                var awaiter = task6.GetAwaiter();

                if (awaiter.IsCompleted)
                {
                    TryInvokeContinuationT6(this, awaiter);
                }
                else
                {
                    awaiter.SourceOnCompleted(static state =>
                    {
                        using (var t = (StateTuple<WhenAnyPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>, LuminTaskAwaiter<T6>>)state)
                        {
                            TryInvokeContinuationT6(t.Item1, t.Item2);
                        }
                    }, StateTuple.Create(this, awaiter));
                }
            }
            {
                var awaiter = task7.GetAwaiter();

                if (awaiter.IsCompleted)
                {
                    TryInvokeContinuationT7(this, awaiter);
                }
                else
                {
                    awaiter.SourceOnCompleted(static state =>
                    {
                        using (var t = (StateTuple<WhenAnyPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>, LuminTaskAwaiter<T7>>)state)
                        {
                            TryInvokeContinuationT7(t.Item1, t.Item2);
                        }
                    }, StateTuple.Create(this, awaiter));
                }
            }
            {
                var awaiter = task8.GetAwaiter();

                if (awaiter.IsCompleted)
                {
                    TryInvokeContinuationT8(this, awaiter);
                }
                else
                {
                    awaiter.SourceOnCompleted(static state =>
                    {
                        using (var t = (StateTuple<WhenAnyPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>, LuminTaskAwaiter<T8>>)state)
                        {
                            TryInvokeContinuationT8(t.Item1, t.Item2);
                        }
                    }, StateTuple.Create(this, awaiter));
                }
            }
            {
                var awaiter = task9.GetAwaiter();

                if (awaiter.IsCompleted)
                {
                    TryInvokeContinuationT9(this, awaiter);
                }
                else
                {
                    awaiter.SourceOnCompleted(static state =>
                    {
                        using (var t = (StateTuple<WhenAnyPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>, LuminTaskAwaiter<T9>>)state)
                        {
                            TryInvokeContinuationT9(t.Item1, t.Item2);
                        }
                    }, StateTuple.Create(this, awaiter));
                }
            }
            {
                var awaiter = task10.GetAwaiter();

                if (awaiter.IsCompleted)
                {
                    TryInvokeContinuationT10(this, awaiter);
                }
                else
                {
                    awaiter.SourceOnCompleted(static state =>
                    {
                        using (var t = (StateTuple<WhenAnyPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>, LuminTaskAwaiter<T10>>)state)
                        {
                            TryInvokeContinuationT10(t.Item1, t.Item2);
                        }
                    }, StateTuple.Create(this, awaiter));
                }
            }
            {
                var awaiter = task11.GetAwaiter();

                if (awaiter.IsCompleted)
                {
                    TryInvokeContinuationT11(this, awaiter);
                }
                else
                {
                    awaiter.SourceOnCompleted(static state =>
                    {
                        using (var t = (StateTuple<WhenAnyPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>, LuminTaskAwaiter<T11>>)state)
                        {
                            TryInvokeContinuationT11(t.Item1, t.Item2);
                        }
                    }, StateTuple.Create(this, awaiter));
                }
            }
            {
                var awaiter = task12.GetAwaiter();

                if (awaiter.IsCompleted)
                {
                    TryInvokeContinuationT12(this, awaiter);
                }
                else
                {
                    awaiter.SourceOnCompleted(static state =>
                    {
                        using (var t = (StateTuple<WhenAnyPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>, LuminTaskAwaiter<T12>>)state)
                        {
                            TryInvokeContinuationT12(t.Item1, t.Item2);
                        }
                    }, StateTuple.Create(this, awaiter));
                }
            }
            {
                var awaiter = task13.GetAwaiter();

                if (awaiter.IsCompleted)
                {
                    TryInvokeContinuationT13(this, awaiter);
                }
                else
                {
                    awaiter.SourceOnCompleted(static state =>
                    {
                        using (var t = (StateTuple<WhenAnyPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>, LuminTaskAwaiter<T13>>)state)
                        {
                            TryInvokeContinuationT13(t.Item1, t.Item2);
                        }
                    }, StateTuple.Create(this, awaiter));
                }
            }
            {
                var awaiter = task14.GetAwaiter();

                if (awaiter.IsCompleted)
                {
                    TryInvokeContinuationT14(this, awaiter);
                }
                else
                {
                    awaiter.SourceOnCompleted(static state =>
                    {
                        using (var t = (StateTuple<WhenAnyPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>, LuminTaskAwaiter<T14>>)state)
                        {
                            TryInvokeContinuationT14(t.Item1, t.Item2);
                        }
                    }, StateTuple.Create(this, awaiter));
                }
            }
        }

        static void TryInvokeContinuationT1(WhenAnyPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14> self, in LuminTaskAwaiter<T1> awaiter)
        {
            T1 result;
            try
            {
                result = awaiter.GetResult();
            }
            catch (Exception ex)
            {
                self.core.TrySetException(ex);
                return;
            }

            if (Interlocked.Increment(ref self.completedCount) == 1)
            {
                self.core.TrySetResult((0, result, default, default, default, default, default, default, default, default, default, default, default, default, default));
            }
        }

        static void TryInvokeContinuationT2(WhenAnyPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14> self, in LuminTaskAwaiter<T2> awaiter)
        {
            T2 result;
            try
            {
                result = awaiter.GetResult();
            }
            catch (Exception ex)
            {
                self.core.TrySetException(ex);
                return;
            }

            if (Interlocked.Increment(ref self.completedCount) == 1)
            {
                self.core.TrySetResult((1, default, result, default, default, default, default, default, default, default, default, default, default, default, default));
            }
        }

        static void TryInvokeContinuationT3(WhenAnyPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14> self, in LuminTaskAwaiter<T3> awaiter)
        {
            T3 result;
            try
            {
                result = awaiter.GetResult();
            }
            catch (Exception ex)
            {
                self.core.TrySetException(ex);
                return;
            }

            if (Interlocked.Increment(ref self.completedCount) == 1)
            {
                self.core.TrySetResult((2, default, default, result, default, default, default, default, default, default, default, default, default, default, default));
            }
        }

        static void TryInvokeContinuationT4(WhenAnyPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14> self, in LuminTaskAwaiter<T4> awaiter)
        {
            T4 result;
            try
            {
                result = awaiter.GetResult();
            }
            catch (Exception ex)
            {
                self.core.TrySetException(ex);
                return;
            }

            if (Interlocked.Increment(ref self.completedCount) == 1)
            {
                self.core.TrySetResult((3, default, default, default, result, default, default, default, default, default, default, default, default, default, default));
            }
        }

        static void TryInvokeContinuationT5(WhenAnyPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14> self, in LuminTaskAwaiter<T5> awaiter)
        {
            T5 result;
            try
            {
                result = awaiter.GetResult();
            }
            catch (Exception ex)
            {
                self.core.TrySetException(ex);
                return;
            }

            if (Interlocked.Increment(ref self.completedCount) == 1)
            {
                self.core.TrySetResult((4, default, default, default, default, result, default, default, default, default, default, default, default, default, default));
            }
        }

        static void TryInvokeContinuationT6(WhenAnyPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14> self, in LuminTaskAwaiter<T6> awaiter)
        {
            T6 result;
            try
            {
                result = awaiter.GetResult();
            }
            catch (Exception ex)
            {
                self.core.TrySetException(ex);
                return;
            }

            if (Interlocked.Increment(ref self.completedCount) == 1)
            {
                self.core.TrySetResult((5, default, default, default, default, default, result, default, default, default, default, default, default, default, default));
            }
        }

        static void TryInvokeContinuationT7(WhenAnyPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14> self, in LuminTaskAwaiter<T7> awaiter)
        {
            T7 result;
            try
            {
                result = awaiter.GetResult();
            }
            catch (Exception ex)
            {
                self.core.TrySetException(ex);
                return;
            }

            if (Interlocked.Increment(ref self.completedCount) == 1)
            {
                self.core.TrySetResult((6, default, default, default, default, default, default, result, default, default, default, default, default, default, default));
            }
        }

        static void TryInvokeContinuationT8(WhenAnyPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14> self, in LuminTaskAwaiter<T8> awaiter)
        {
            T8 result;
            try
            {
                result = awaiter.GetResult();
            }
            catch (Exception ex)
            {
                self.core.TrySetException(ex);
                return;
            }

            if (Interlocked.Increment(ref self.completedCount) == 1)
            {
                self.core.TrySetResult((7, default, default, default, default, default, default, default, result, default, default, default, default, default, default));
            }
        }

        static void TryInvokeContinuationT9(WhenAnyPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14> self, in LuminTaskAwaiter<T9> awaiter)
        {
            T9 result;
            try
            {
                result = awaiter.GetResult();
            }
            catch (Exception ex)
            {
                self.core.TrySetException(ex);
                return;
            }

            if (Interlocked.Increment(ref self.completedCount) == 1)
            {
                self.core.TrySetResult((8, default, default, default, default, default, default, default, default, result, default, default, default, default, default));
            }
        }

        static void TryInvokeContinuationT10(WhenAnyPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14> self, in LuminTaskAwaiter<T10> awaiter)
        {
            T10 result;
            try
            {
                result = awaiter.GetResult();
            }
            catch (Exception ex)
            {
                self.core.TrySetException(ex);
                return;
            }

            if (Interlocked.Increment(ref self.completedCount) == 1)
            {
                self.core.TrySetResult((9, default, default, default, default, default, default, default, default, default, result, default, default, default, default));
            }
        }

        static void TryInvokeContinuationT11(WhenAnyPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14> self, in LuminTaskAwaiter<T11> awaiter)
        {
            T11 result;
            try
            {
                result = awaiter.GetResult();
            }
            catch (Exception ex)
            {
                self.core.TrySetException(ex);
                return;
            }

            if (Interlocked.Increment(ref self.completedCount) == 1)
            {
                self.core.TrySetResult((10, default, default, default, default, default, default, default, default, default, default, result, default, default, default));
            }
        }

        static void TryInvokeContinuationT12(WhenAnyPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14> self, in LuminTaskAwaiter<T12> awaiter)
        {
            T12 result;
            try
            {
                result = awaiter.GetResult();
            }
            catch (Exception ex)
            {
                self.core.TrySetException(ex);
                return;
            }

            if (Interlocked.Increment(ref self.completedCount) == 1)
            {
                self.core.TrySetResult((11, default, default, default, default, default, default, default, default, default, default, default, result, default, default));
            }
        }

        static void TryInvokeContinuationT13(WhenAnyPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14> self, in LuminTaskAwaiter<T13> awaiter)
        {
            T13 result;
            try
            {
                result = awaiter.GetResult();
            }
            catch (Exception ex)
            {
                self.core.TrySetException(ex);
                return;
            }

            if (Interlocked.Increment(ref self.completedCount) == 1)
            {
                self.core.TrySetResult((12, default, default, default, default, default, default, default, default, default, default, default, default, result, default));
            }
        }

        static void TryInvokeContinuationT14(WhenAnyPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14> self, in LuminTaskAwaiter<T14> awaiter)
        {
            T14 result;
            try
            {
                result = awaiter.GetResult();
            }
            catch (Exception ex)
            {
                self.core.TrySetException(ex);
                return;
            }

            if (Interlocked.Increment(ref self.completedCount) == 1)
            {
                self.core.TrySetResult((13, default, default, default, default, default, default, default, default, default, default, default, default, default, result));
            }
        }


        public (int, T1 result1, T2 result2, T3 result3, T4 result4, T5 result5, T6 result6, T7 result7, T8 result8, T9 result9, T10 result10, T11 result11, T12 result12, T13 result13, T14 result14) GetResult(short token)
        {
            TaskTracker.RemoveTracking(this);
            GC.SuppressFinalize(this);
            return core.GetResult(token);
        }

        public LuminTaskStatus GetStatus(short token)
        {
            return core.GetStatus(token);
        }

        public void OnCompleted(Action<object> continuation, object state, short token)
        {
            core.OnCompleted(continuation, state, token);
        }

        public LuminTaskStatus UnsafeGetStatus()
        {
            return core.UnsafeGetStatus();
        }

        void ILuminTaskSource.GetResult(short token)
        {
            GetResult(token);
        }
    }

    public static LuminTask<(int winArgumentIndex, T1 result1, T2 result2, T3 result3, T4 result4, T5 result5, T6 result6, T7 result7, T8 result8, T9 result9, T10 result10, T11 result11, T12 result12, T13 result13, T14 result14, T15 result15)> WhenAny<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(LuminTask<T1> task1, LuminTask<T2> task2, LuminTask<T3> task3, LuminTask<T4> task4, LuminTask<T5> task5, LuminTask<T6> task6, LuminTask<T7> task7, LuminTask<T8> task8, LuminTask<T9> task9, LuminTask<T10> task10, LuminTask<T11> task11, LuminTask<T12> task12, LuminTask<T13> task13, LuminTask<T14> task14, LuminTask<T15> task15)
    {
        return new LuminTask<(int winArgumentIndex, T1 result1, T2 result2, T3 result3, T4 result4, T5 result5, T6 result6, T7 result7, T8 result8, T9 result9, T10 result10, T11 result11, T12 result12, T13 result13, T14 result14, T15 result15)>(new WhenAnyPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(task1, task2, task3, task4, task5, task6, task7, task8, task9, task10, task11, task12, task13, task14, task15), 0);
    }

    sealed class WhenAnyPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15> : ILuminTaskSource<(int, T1 result1, T2 result2, T3 result3, T4 result4, T5 result5, T6 result6, T7 result7, T8 result8, T9 result9, T10 result10, T11 result11, T12 result12, T13 result13, T14 result14, T15 result15)>
    {
        int completedCount;
        LuminTaskCompletionSourceCore<(int, T1 result1, T2 result2, T3 result3, T4 result4, T5 result5, T6 result6, T7 result7, T8 result8, T9 result9, T10 result10, T11 result11, T12 result12, T13 result13, T14 result14, T15 result15)> core;

        public WhenAnyPromise(LuminTask<T1> task1, LuminTask<T2> task2, LuminTask<T3> task3, LuminTask<T4> task4, LuminTask<T5> task5, LuminTask<T6> task6, LuminTask<T7> task7, LuminTask<T8> task8, LuminTask<T9> task9, LuminTask<T10> task10, LuminTask<T11> task11, LuminTask<T12> task12, LuminTask<T13> task13, LuminTask<T14> task14, LuminTask<T15> task15)
        {
            TaskTracker.TrackActiveTask(this, 3);

            this.completedCount = 0;
            {
                var awaiter = task1.GetAwaiter();

                if (awaiter.IsCompleted)
                {
                    TryInvokeContinuationT1(this, awaiter);
                }
                else
                {
                    awaiter.SourceOnCompleted(static state =>
                    {
                        using (var t = (StateTuple<WhenAnyPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>, LuminTaskAwaiter<T1>>)state)
                        {
                            TryInvokeContinuationT1(t.Item1, t.Item2);
                        }
                    }, StateTuple.Create(this, awaiter));
                }
            }
            {
                var awaiter = task2.GetAwaiter();

                if (awaiter.IsCompleted)
                {
                    TryInvokeContinuationT2(this, awaiter);
                }
                else
                {
                    awaiter.SourceOnCompleted(static state =>
                    {
                        using (var t = (StateTuple<WhenAnyPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>, LuminTaskAwaiter<T2>>)state)
                        {
                            TryInvokeContinuationT2(t.Item1, t.Item2);
                        }
                    }, StateTuple.Create(this, awaiter));
                }
            }
            {
                var awaiter = task3.GetAwaiter();

                if (awaiter.IsCompleted)
                {
                    TryInvokeContinuationT3(this, awaiter);
                }
                else
                {
                    awaiter.SourceOnCompleted(static state =>
                    {
                        using (var t = (StateTuple<WhenAnyPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>, LuminTaskAwaiter<T3>>)state)
                        {
                            TryInvokeContinuationT3(t.Item1, t.Item2);
                        }
                    }, StateTuple.Create(this, awaiter));
                }
            }
            {
                var awaiter = task4.GetAwaiter();

                if (awaiter.IsCompleted)
                {
                    TryInvokeContinuationT4(this, awaiter);
                }
                else
                {
                    awaiter.SourceOnCompleted(static state =>
                    {
                        using (var t = (StateTuple<WhenAnyPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>, LuminTaskAwaiter<T4>>)state)
                        {
                            TryInvokeContinuationT4(t.Item1, t.Item2);
                        }
                    }, StateTuple.Create(this, awaiter));
                }
            }
            {
                var awaiter = task5.GetAwaiter();

                if (awaiter.IsCompleted)
                {
                    TryInvokeContinuationT5(this, awaiter);
                }
                else
                {
                    awaiter.SourceOnCompleted(static state =>
                    {
                        using (var t = (StateTuple<WhenAnyPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>, LuminTaskAwaiter<T5>>)state)
                        {
                            TryInvokeContinuationT5(t.Item1, t.Item2);
                        }
                    }, StateTuple.Create(this, awaiter));
                }
            }
            {
                var awaiter = task6.GetAwaiter();

                if (awaiter.IsCompleted)
                {
                    TryInvokeContinuationT6(this, awaiter);
                }
                else
                {
                    awaiter.SourceOnCompleted(static state =>
                    {
                        using (var t = (StateTuple<WhenAnyPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>, LuminTaskAwaiter<T6>>)state)
                        {
                            TryInvokeContinuationT6(t.Item1, t.Item2);
                        }
                    }, StateTuple.Create(this, awaiter));
                }
            }
            {
                var awaiter = task7.GetAwaiter();

                if (awaiter.IsCompleted)
                {
                    TryInvokeContinuationT7(this, awaiter);
                }
                else
                {
                    awaiter.SourceOnCompleted(static state =>
                    {
                        using (var t = (StateTuple<WhenAnyPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>, LuminTaskAwaiter<T7>>)state)
                        {
                            TryInvokeContinuationT7(t.Item1, t.Item2);
                        }
                    }, StateTuple.Create(this, awaiter));
                }
            }
            {
                var awaiter = task8.GetAwaiter();

                if (awaiter.IsCompleted)
                {
                    TryInvokeContinuationT8(this, awaiter);
                }
                else
                {
                    awaiter.SourceOnCompleted(static state =>
                    {
                        using (var t = (StateTuple<WhenAnyPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>, LuminTaskAwaiter<T8>>)state)
                        {
                            TryInvokeContinuationT8(t.Item1, t.Item2);
                        }
                    }, StateTuple.Create(this, awaiter));
                }
            }
            {
                var awaiter = task9.GetAwaiter();

                if (awaiter.IsCompleted)
                {
                    TryInvokeContinuationT9(this, awaiter);
                }
                else
                {
                    awaiter.SourceOnCompleted(static state =>
                    {
                        using (var t = (StateTuple<WhenAnyPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>, LuminTaskAwaiter<T9>>)state)
                        {
                            TryInvokeContinuationT9(t.Item1, t.Item2);
                        }
                    }, StateTuple.Create(this, awaiter));
                }
            }
            {
                var awaiter = task10.GetAwaiter();

                if (awaiter.IsCompleted)
                {
                    TryInvokeContinuationT10(this, awaiter);
                }
                else
                {
                    awaiter.SourceOnCompleted(static state =>
                    {
                        using (var t = (StateTuple<WhenAnyPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>, LuminTaskAwaiter<T10>>)state)
                        {
                            TryInvokeContinuationT10(t.Item1, t.Item2);
                        }
                    }, StateTuple.Create(this, awaiter));
                }
            }
            {
                var awaiter = task11.GetAwaiter();

                if (awaiter.IsCompleted)
                {
                    TryInvokeContinuationT11(this, awaiter);
                }
                else
                {
                    awaiter.SourceOnCompleted(static state =>
                    {
                        using (var t = (StateTuple<WhenAnyPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>, LuminTaskAwaiter<T11>>)state)
                        {
                            TryInvokeContinuationT11(t.Item1, t.Item2);
                        }
                    }, StateTuple.Create(this, awaiter));
                }
            }
            {
                var awaiter = task12.GetAwaiter();

                if (awaiter.IsCompleted)
                {
                    TryInvokeContinuationT12(this, awaiter);
                }
                else
                {
                    awaiter.SourceOnCompleted(static state =>
                    {
                        using (var t = (StateTuple<WhenAnyPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>, LuminTaskAwaiter<T12>>)state)
                        {
                            TryInvokeContinuationT12(t.Item1, t.Item2);
                        }
                    }, StateTuple.Create(this, awaiter));
                }
            }
            {
                var awaiter = task13.GetAwaiter();

                if (awaiter.IsCompleted)
                {
                    TryInvokeContinuationT13(this, awaiter);
                }
                else
                {
                    awaiter.SourceOnCompleted(static state =>
                    {
                        using (var t = (StateTuple<WhenAnyPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>, LuminTaskAwaiter<T13>>)state)
                        {
                            TryInvokeContinuationT13(t.Item1, t.Item2);
                        }
                    }, StateTuple.Create(this, awaiter));
                }
            }
            {
                var awaiter = task14.GetAwaiter();

                if (awaiter.IsCompleted)
                {
                    TryInvokeContinuationT14(this, awaiter);
                }
                else
                {
                    awaiter.SourceOnCompleted(static state =>
                    {
                        using (var t = (StateTuple<WhenAnyPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>, LuminTaskAwaiter<T14>>)state)
                        {
                            TryInvokeContinuationT14(t.Item1, t.Item2);
                        }
                    }, StateTuple.Create(this, awaiter));
                }
            }
            {
                var awaiter = task15.GetAwaiter();

                if (awaiter.IsCompleted)
                {
                    TryInvokeContinuationT15(this, awaiter);
                }
                else
                {
                    awaiter.SourceOnCompleted(static state =>
                    {
                        using (var t = (StateTuple<WhenAnyPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>, LuminTaskAwaiter<T15>>)state)
                        {
                            TryInvokeContinuationT15(t.Item1, t.Item2);
                        }
                    }, StateTuple.Create(this, awaiter));
                }
            }
        }

        static void TryInvokeContinuationT1(WhenAnyPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15> self, in LuminTaskAwaiter<T1> awaiter)
        {
            T1 result;
            try
            {
                result = awaiter.GetResult();
            }
            catch (Exception ex)
            {
                self.core.TrySetException(ex);
                return;
            }

            if (Interlocked.Increment(ref self.completedCount) == 1)
            {
                self.core.TrySetResult((0, result, default, default, default, default, default, default, default, default, default, default, default, default, default, default));
            }
        }

        static void TryInvokeContinuationT2(WhenAnyPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15> self, in LuminTaskAwaiter<T2> awaiter)
        {
            T2 result;
            try
            {
                result = awaiter.GetResult();
            }
            catch (Exception ex)
            {
                self.core.TrySetException(ex);
                return;
            }

            if (Interlocked.Increment(ref self.completedCount) == 1)
            {
                self.core.TrySetResult((1, default, result, default, default, default, default, default, default, default, default, default, default, default, default, default));
            }
        }

        static void TryInvokeContinuationT3(WhenAnyPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15> self, in LuminTaskAwaiter<T3> awaiter)
        {
            T3 result;
            try
            {
                result = awaiter.GetResult();
            }
            catch (Exception ex)
            {
                self.core.TrySetException(ex);
                return;
            }

            if (Interlocked.Increment(ref self.completedCount) == 1)
            {
                self.core.TrySetResult((2, default, default, result, default, default, default, default, default, default, default, default, default, default, default, default));
            }
        }

        static void TryInvokeContinuationT4(WhenAnyPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15> self, in LuminTaskAwaiter<T4> awaiter)
        {
            T4 result;
            try
            {
                result = awaiter.GetResult();
            }
            catch (Exception ex)
            {
                self.core.TrySetException(ex);
                return;
            }

            if (Interlocked.Increment(ref self.completedCount) == 1)
            {
                self.core.TrySetResult((3, default, default, default, result, default, default, default, default, default, default, default, default, default, default, default));
            }
        }

        static void TryInvokeContinuationT5(WhenAnyPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15> self, in LuminTaskAwaiter<T5> awaiter)
        {
            T5 result;
            try
            {
                result = awaiter.GetResult();
            }
            catch (Exception ex)
            {
                self.core.TrySetException(ex);
                return;
            }

            if (Interlocked.Increment(ref self.completedCount) == 1)
            {
                self.core.TrySetResult((4, default, default, default, default, result, default, default, default, default, default, default, default, default, default, default));
            }
        }

        static void TryInvokeContinuationT6(WhenAnyPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15> self, in LuminTaskAwaiter<T6> awaiter)
        {
            T6 result;
            try
            {
                result = awaiter.GetResult();
            }
            catch (Exception ex)
            {
                self.core.TrySetException(ex);
                return;
            }

            if (Interlocked.Increment(ref self.completedCount) == 1)
            {
                self.core.TrySetResult((5, default, default, default, default, default, result, default, default, default, default, default, default, default, default, default));
            }
        }

        static void TryInvokeContinuationT7(WhenAnyPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15> self, in LuminTaskAwaiter<T7> awaiter)
        {
            T7 result;
            try
            {
                result = awaiter.GetResult();
            }
            catch (Exception ex)
            {
                self.core.TrySetException(ex);
                return;
            }

            if (Interlocked.Increment(ref self.completedCount) == 1)
            {
                self.core.TrySetResult((6, default, default, default, default, default, default, result, default, default, default, default, default, default, default, default));
            }
        }

        static void TryInvokeContinuationT8(WhenAnyPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15> self, in LuminTaskAwaiter<T8> awaiter)
        {
            T8 result;
            try
            {
                result = awaiter.GetResult();
            }
            catch (Exception ex)
            {
                self.core.TrySetException(ex);
                return;
            }

            if (Interlocked.Increment(ref self.completedCount) == 1)
            {
                self.core.TrySetResult((7, default, default, default, default, default, default, default, result, default, default, default, default, default, default, default));
            }
        }

        static void TryInvokeContinuationT9(WhenAnyPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15> self, in LuminTaskAwaiter<T9> awaiter)
        {
            T9 result;
            try
            {
                result = awaiter.GetResult();
            }
            catch (Exception ex)
            {
                self.core.TrySetException(ex);
                return;
            }

            if (Interlocked.Increment(ref self.completedCount) == 1)
            {
                self.core.TrySetResult((8, default, default, default, default, default, default, default, default, result, default, default, default, default, default, default));
            }
        }

        static void TryInvokeContinuationT10(WhenAnyPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15> self, in LuminTaskAwaiter<T10> awaiter)
        {
            T10 result;
            try
            {
                result = awaiter.GetResult();
            }
            catch (Exception ex)
            {
                self.core.TrySetException(ex);
                return;
            }

            if (Interlocked.Increment(ref self.completedCount) == 1)
            {
                self.core.TrySetResult((9, default, default, default, default, default, default, default, default, default, result, default, default, default, default, default));
            }
        }

        static void TryInvokeContinuationT11(WhenAnyPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15> self, in LuminTaskAwaiter<T11> awaiter)
        {
            T11 result;
            try
            {
                result = awaiter.GetResult();
            }
            catch (Exception ex)
            {
                self.core.TrySetException(ex);
                return;
            }

            if (Interlocked.Increment(ref self.completedCount) == 1)
            {
                self.core.TrySetResult((10, default, default, default, default, default, default, default, default, default, default, result, default, default, default, default));
            }
        }

        static void TryInvokeContinuationT12(WhenAnyPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15> self, in LuminTaskAwaiter<T12> awaiter)
        {
            T12 result;
            try
            {
                result = awaiter.GetResult();
            }
            catch (Exception ex)
            {
                self.core.TrySetException(ex);
                return;
            }

            if (Interlocked.Increment(ref self.completedCount) == 1)
            {
                self.core.TrySetResult((11, default, default, default, default, default, default, default, default, default, default, default, result, default, default, default));
            }
        }

        static void TryInvokeContinuationT13(WhenAnyPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15> self, in LuminTaskAwaiter<T13> awaiter)
        {
            T13 result;
            try
            {
                result = awaiter.GetResult();
            }
            catch (Exception ex)
            {
                self.core.TrySetException(ex);
                return;
            }

            if (Interlocked.Increment(ref self.completedCount) == 1)
            {
                self.core.TrySetResult((12, default, default, default, default, default, default, default, default, default, default, default, default, result, default, default));
            }
        }

        static void TryInvokeContinuationT14(WhenAnyPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15> self, in LuminTaskAwaiter<T14> awaiter)
        {
            T14 result;
            try
            {
                result = awaiter.GetResult();
            }
            catch (Exception ex)
            {
                self.core.TrySetException(ex);
                return;
            }

            if (Interlocked.Increment(ref self.completedCount) == 1)
            {
                self.core.TrySetResult((13, default, default, default, default, default, default, default, default, default, default, default, default, default, result, default));
            }
        }

        static void TryInvokeContinuationT15(WhenAnyPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15> self, in LuminTaskAwaiter<T15> awaiter)
        {
            T15 result;
            try
            {
                result = awaiter.GetResult();
            }
            catch (Exception ex)
            {
                self.core.TrySetException(ex);
                return;
            }

            if (Interlocked.Increment(ref self.completedCount) == 1)
            {
                self.core.TrySetResult((14, default, default, default, default, default, default, default, default, default, default, default, default, default, default, result));
            }
        }


        public (int, T1 result1, T2 result2, T3 result3, T4 result4, T5 result5, T6 result6, T7 result7, T8 result8, T9 result9, T10 result10, T11 result11, T12 result12, T13 result13, T14 result14, T15 result15) GetResult(short token)
        {
            TaskTracker.RemoveTracking(this);
            GC.SuppressFinalize(this);
            return core.GetResult(token);
        }

        public LuminTaskStatus GetStatus(short token)
        {
            return core.GetStatus(token);
        }

        public void OnCompleted(Action<object> continuation, object state, short token)
        {
            core.OnCompleted(continuation, state, token);
        }

        public LuminTaskStatus UnsafeGetStatus()
        {
            return core.UnsafeGetStatus();
        }

        void ILuminTaskSource.GetResult(short token)
        {
            GetResult(token);
        }
    }
    
    
    public static LuminTask<(bool hasResultLeft, T result)> WhenAny<T>(LuminTask<T> leftTask, LuminTask rightTask)
    {
        return new LuminTask<(bool, T)>(new WhenAnyLRPromise<T>(leftTask, rightTask), 0);
    }
    
    sealed class WhenAnyLRPromise<T> : ILuminTaskSource<(bool, T)>
    {
        int completedCount;
        LuminTaskCompletionSourceCore<(bool, T)> core;

        public WhenAnyLRPromise(LuminTask<T> leftTask, LuminTask rightTask)
        {
            TaskTracker.TrackActiveTask(this, 3);

            {
                LuminTaskAwaiter<T> awaiter;
                try
                {
                    awaiter = leftTask.GetAwaiter();
                }
                catch (Exception ex)
                {
                    core.TrySetException(ex);
                    goto RIGHT;
                }

                if (awaiter.IsCompleted)
                {
                    TryLeftInvokeContinuation(this, awaiter);
                }
                else
                {
                    awaiter.SourceOnCompleted(static state =>
                    {
                        using (var t = (StateTuple<WhenAnyLRPromise<T>, LuminTaskAwaiter<T>>)state)
                        {
                            TryLeftInvokeContinuation(t.Item1, t.Item2);
                        }
                    }, StateTuple.Create(this, awaiter));
                }
            }
            RIGHT:
            {
                LuminTaskAwaiter awaiter;
                try
                {
                    awaiter = rightTask.GetAwaiter();
                }
                catch (Exception ex)
                {
                    core.TrySetException(ex);
                    return;
                }

                if (awaiter.IsCompleted)
                {
                    TryRightInvokeContinuation(this, awaiter);
                }
                else
                {
                    awaiter.SourceOnCompleted(static state =>
                    {
                        using (var t = (StateTuple<WhenAnyLRPromise<T>, LuminTaskAwaiter>)state)
                        {
                            TryRightInvokeContinuation(t.Item1, t.Item2);
                        }
                    }, StateTuple.Create(this, awaiter));
                }
            }
        }

        static void TryLeftInvokeContinuation(WhenAnyLRPromise<T> self, in LuminTaskAwaiter<T> awaiter)
        {
            T result;
            try
            {
                result = awaiter.GetResult();
            }
            catch (Exception ex)
            {
                self.core.TrySetException(ex);
                return;
            }

            if (Interlocked.Increment(ref self.completedCount) == 1)
            {
                self.core.TrySetResult((true, result));
            }
        }

        static void TryRightInvokeContinuation(WhenAnyLRPromise<T> self, in LuminTaskAwaiter awaiter)
        {
            try
            {
                awaiter.GetResult();
            }
            catch (Exception ex)
            {
                self.core.TrySetException(ex);
                return;
            }

            if (Interlocked.Increment(ref self.completedCount) == 1)
            {
                self.core.TrySetResult((false, default));
            }
        }

        public (bool, T) GetResult(short token)
        {
            TaskTracker.RemoveTracking(this);
            GC.SuppressFinalize(this);
            return core.GetResult(token);
        }

        public LuminTaskStatus GetStatus(short token)
        {
            return core.GetStatus(token);
        }

        public void OnCompleted(Action<object> continuation, object state, short token)
        {
            core.OnCompleted(continuation, state, token);
        }

        public LuminTaskStatus UnsafeGetStatus()
        {
            return core.UnsafeGetStatus();
        }

        void ILuminTaskSource.GetResult(short token)
        {
            GetResult(token);
        }
    }
    
    public static LuminTask<int> WhenAny<T>(LuminTask task1, LuminTask task2)
    {
        return new LuminTask<int>(new WhenAnyPromise(task1, task2), 0);
    }
    
    public static LuminTask<int> WhenAny(LuminTask task1, LuminTask task2, LuminTask task3)
    {
        return new LuminTask<int>(new WhenAnyPromise(task1, task2, task3), 0);
    }

    public static LuminTask<int> WhenAny(LuminTask task1, LuminTask task2, LuminTask task3, LuminTask task4)
    {
        return new LuminTask<int>(new WhenAnyPromise(task1, task2, task3, task4), 0);
    }

    public static LuminTask<int> WhenAny(LuminTask task1, LuminTask task2, LuminTask task3, LuminTask task4, LuminTask task5)
    {
        return new LuminTask<int>(new WhenAnyPromise(task1, task2, task3, task4, task5), 0);
    }

    public static LuminTask<int> WhenAny(LuminTask task1, LuminTask task2, LuminTask task3, LuminTask task4, LuminTask task5, LuminTask task6)
    {
        return new LuminTask<int>(new WhenAnyPromise(task1, task2, task3, task4, task5, task6), 0);
    }

    public static LuminTask<int> WhenAny(LuminTask task1, LuminTask task2, LuminTask task3, LuminTask task4, LuminTask task5, LuminTask task6, LuminTask task7)
    {
        return new LuminTask<int>(new WhenAnyPromise(task1, task2, task3, task4, task5, task6, task7), 0);
    }

    public static LuminTask<int> WhenAny(LuminTask task1, LuminTask task2, LuminTask task3, LuminTask task4, LuminTask task5, LuminTask task6, LuminTask task7, LuminTask task8)
    {
        return new LuminTask<int>(new WhenAnyPromise(task1, task2, task3, task4, task5, task6, task7, task8), 0);
    }

    public static LuminTask<int> WhenAny(LuminTask task1, LuminTask task2, LuminTask task3, LuminTask task4, LuminTask task5, LuminTask task6, LuminTask task7, LuminTask task8, LuminTask task9)
    {
        return new LuminTask<int>(new WhenAnyPromise(task1, task2, task3, task4, task5, task6, task7, task8, task9), 0);
    }

    public static LuminTask<int> WhenAny(LuminTask task1, LuminTask task2, LuminTask task3, LuminTask task4, LuminTask task5, LuminTask task6, LuminTask task7, LuminTask task8, LuminTask task9, LuminTask task10)
    {
        return new LuminTask<int>(new WhenAnyPromise(task1, task2, task3, task4, task5, task6, task7, task8, task9, task10), 0);
    }

    public static LuminTask<int> WhenAny(LuminTask task1, LuminTask task2, LuminTask task3, LuminTask task4, LuminTask task5, LuminTask task6, LuminTask task7, LuminTask task8, LuminTask task9, LuminTask task10, LuminTask task11)
    {
        return new LuminTask<int>(new WhenAnyPromise(task1, task2, task3, task4, task5, task6, task7, task8, task9, task10, task11), 0);
    }

    public static LuminTask<int> WhenAny(LuminTask task1, LuminTask task2, LuminTask task3, LuminTask task4, LuminTask task5, LuminTask task6, LuminTask task7, LuminTask task8, LuminTask task9, LuminTask task10, LuminTask task11, LuminTask task12)
    {
        return new LuminTask<int>(new WhenAnyPromise(task1, task2, task3, task4, task5, task6, task7, task8, task9, task10, task11, task12), 0);
    }

    public static LuminTask<int> WhenAny(LuminTask task1, LuminTask task2, LuminTask task3, LuminTask task4, LuminTask task5, LuminTask task6, LuminTask task7, LuminTask task8, LuminTask task9, LuminTask task10, LuminTask task11, LuminTask task12, LuminTask task13)
    {
        return new LuminTask<int>(new WhenAnyPromise(task1, task2, task3, task4, task5, task6, task7, task8, task9, task10, task11, task12, task13), 0);
    }

    public static LuminTask<int> WhenAny(LuminTask task1, LuminTask task2, LuminTask task3, LuminTask task4, LuminTask task5, LuminTask task6, LuminTask task7, LuminTask task8, LuminTask task9, LuminTask task10, LuminTask task11, LuminTask task12, LuminTask task13, LuminTask task14)
    {
        return new LuminTask<int>(new WhenAnyPromise(task1, task2, task3, task4, task5, task6, task7, task8, task9, task10, task11, task12, task13, task14), 0);
    }

    public static LuminTask<int> WhenAny(LuminTask task1, LuminTask task2, LuminTask task3, LuminTask task4, LuminTask task5, LuminTask task6, LuminTask task7, LuminTask task8, LuminTask task9, LuminTask task10, LuminTask task11, LuminTask task12, LuminTask task13, LuminTask task14, LuminTask task15)
    {
        return new LuminTask<int>(new WhenAnyPromise(task1, task2, task3, task4, task5, task6, task7, task8, task9, task10, task11, task12, task13, task14, task15), 0);
    }
    
    sealed class WhenAnyPromise : ILuminTaskSource<int>
    {
        int completedCount;
        LuminTaskCompletionSourceCore<int> core;

        public WhenAnyPromise(LuminTask task1, LuminTask task2)
        {
            TaskTracker.TrackActiveTask(this, 3);

            SetAwaiter(task1, 0);
            SetAwaiter(task2, 1);
        }

        public WhenAnyPromise(LuminTask t0, LuminTask t1, LuminTask t2)
        {
            const int count = 3;
            TaskTracker.TrackActiveTask(this, count);
            SetAwaiter(t0, 0);
            SetAwaiter(t1, 1);
            SetAwaiter(t2, 2);
        }

        public WhenAnyPromise(LuminTask t0, LuminTask t1, LuminTask t2, LuminTask t3)
        {
            const int count = 4;
            TaskTracker.TrackActiveTask(this, count);
            SetAwaiter(t0, 0); SetAwaiter(t1, 1); SetAwaiter(t2, 2); SetAwaiter(t3, 3);
        }

        public WhenAnyPromise(LuminTask t0, LuminTask t1, LuminTask t2, LuminTask t3, LuminTask t4)
        {
            const int count = 5;
            TaskTracker.TrackActiveTask(this, count);
            SetAwaiter(t0, 0); SetAwaiter(t1, 1); SetAwaiter(t2, 2); SetAwaiter(t3, 3); SetAwaiter(t4, 4);
        }

        public WhenAnyPromise(LuminTask t0, LuminTask t1, LuminTask t2,
            LuminTask t3, LuminTask t4, LuminTask t5)
        {
            const int count = 6;
            TaskTracker.TrackActiveTask(this, count);
            SetAwaiter(t0, 0); SetAwaiter(t1, 1); SetAwaiter(t2, 2);
            SetAwaiter(t3, 3); SetAwaiter(t4, 4); SetAwaiter(t5, 5);
        }

        public WhenAnyPromise(LuminTask t0, LuminTask t1, LuminTask t2,
            LuminTask t3, LuminTask t4, LuminTask t5, LuminTask t6)
        {
            const int count = 7;
            TaskTracker.TrackActiveTask(this, count);
            SetAwaiter(t0, 0); SetAwaiter(t1, 1); SetAwaiter(t2, 2);
            SetAwaiter(t3, 3); SetAwaiter(t4, 4); SetAwaiter(t5, 5); SetAwaiter(t6, 6);
        }

        public WhenAnyPromise(LuminTask t0, LuminTask t1, LuminTask t2, LuminTask t3,
            LuminTask t4, LuminTask t5, LuminTask t6, LuminTask t7)
        {
            const int count = 8;
            TaskTracker.TrackActiveTask(this, count);
            SetAwaiter(t0, 0); SetAwaiter(t1, 1); SetAwaiter(t2, 2); SetAwaiter(t3, 3);
            SetAwaiter(t4, 4); SetAwaiter(t5, 5); SetAwaiter(t6, 6); SetAwaiter(t7, 7);
        }

        public WhenAnyPromise(LuminTask t0, LuminTask t1, LuminTask t2, LuminTask t3,
            LuminTask t4, LuminTask t5, LuminTask t6, LuminTask t7,
            LuminTask t8)
        {
            const int count = 9;
            TaskTracker.TrackActiveTask(this, count);
            SetAwaiter(t0, 0); SetAwaiter(t1, 1); SetAwaiter(t2, 2); SetAwaiter(t3, 3);
            SetAwaiter(t4, 4); SetAwaiter(t5, 5); SetAwaiter(t6, 6); SetAwaiter(t7, 7); SetAwaiter(t8, 8);
        }

        public WhenAnyPromise(LuminTask t0, LuminTask t1, LuminTask t2, LuminTask t3,
            LuminTask t4, LuminTask t5, LuminTask t6, LuminTask t7,
            LuminTask t8, LuminTask t9)
        {
            const int count = 10;
            TaskTracker.TrackActiveTask(this, count);
            SetAwaiter(t0, 0); SetAwaiter(t1, 1); SetAwaiter(t2, 2); SetAwaiter(t3, 3);
            SetAwaiter(t4, 4); SetAwaiter(t5, 5); SetAwaiter(t6, 6); SetAwaiter(t7, 7);
            SetAwaiter(t8, 8); SetAwaiter(t9, 9);
        }

        public WhenAnyPromise(LuminTask t0 , LuminTask t1 , LuminTask t2 , LuminTask t3 ,
            LuminTask t4 , LuminTask t5 , LuminTask t6 , LuminTask t7 ,
            LuminTask t8 , LuminTask t9 , LuminTask t10)
        {
            const int count = 11;
            TaskTracker.TrackActiveTask(this, count);
            SetAwaiter(t0 , 0);  SetAwaiter(t1 , 1);  SetAwaiter(t2 , 2);  SetAwaiter(t3 , 3);
            SetAwaiter(t4 , 4);  SetAwaiter(t5 , 5);  SetAwaiter(t6 , 6);  SetAwaiter(t7 , 7);
            SetAwaiter(t8 , 8);  SetAwaiter(t9 , 9);  SetAwaiter(t10, 10);
        }
        
        public WhenAnyPromise(LuminTask t0 , LuminTask t1 , LuminTask t2 , LuminTask t3 ,
            LuminTask t4 , LuminTask t5 , LuminTask t6 , LuminTask t7 ,
            LuminTask t8 , LuminTask t9 , LuminTask t10, LuminTask t11)
        {
            const int count = 12;
            TaskTracker.TrackActiveTask(this, count);
            SetAwaiter(t0 , 0);  SetAwaiter(t1 , 1);  SetAwaiter(t2 , 2);  SetAwaiter(t3 , 3);
            SetAwaiter(t4 , 4);  SetAwaiter(t5 , 5);  SetAwaiter(t6 , 6);  SetAwaiter(t7 , 7);
            SetAwaiter(t8 , 8);  SetAwaiter(t9 , 9);  SetAwaiter(t10, 10); SetAwaiter(t11, 11);
        }
        
        public WhenAnyPromise(LuminTask t0 , LuminTask t1 , LuminTask t2 , LuminTask t3 ,
            LuminTask t4 , LuminTask t5 , LuminTask t6 , LuminTask t7 ,
            LuminTask t8 , LuminTask t9 , LuminTask t10, LuminTask t11,
            LuminTask t12)
        {
            const int count = 13;
            TaskTracker.TrackActiveTask(this, count);
            SetAwaiter(t0 , 0);  SetAwaiter(t1 , 1);  SetAwaiter(t2 , 2);  SetAwaiter(t3 , 3);
            SetAwaiter(t4 , 4);  SetAwaiter(t5 , 5);  SetAwaiter(t6 , 6);  SetAwaiter(t7 , 7);
            SetAwaiter(t8 , 8);  SetAwaiter(t9 , 9);  SetAwaiter(t10, 10); SetAwaiter(t11, 11);
            SetAwaiter(t12, 12);
        }
        
        public WhenAnyPromise(LuminTask t0 , LuminTask t1 , LuminTask t2 , LuminTask t3 ,
            LuminTask t4 , LuminTask t5 , LuminTask t6 , LuminTask t7 ,
            LuminTask t8 , LuminTask t9 , LuminTask t10, LuminTask t11,
            LuminTask t12, LuminTask t13)
        {
            const int count = 14;
            TaskTracker.TrackActiveTask(this, count);
            SetAwaiter(t0 , 0);  SetAwaiter(t1 , 1);  SetAwaiter(t2 , 2);  SetAwaiter(t3 , 3);
            SetAwaiter(t4 , 4);  SetAwaiter(t5 , 5);  SetAwaiter(t6 , 6);  SetAwaiter(t7 , 7);
            SetAwaiter(t8 , 8);  SetAwaiter(t9 , 9);  SetAwaiter(t10, 10); SetAwaiter(t11, 11);
            SetAwaiter(t12, 12); SetAwaiter(t13, 13);
        }
        
        public WhenAnyPromise(LuminTask t0 , LuminTask t1 , LuminTask t2 , LuminTask t3 ,
            LuminTask t4 , LuminTask t5 , LuminTask t6 , LuminTask t7 ,
            LuminTask t8 , LuminTask t9 , LuminTask t10, LuminTask t11,
            LuminTask t12, LuminTask t13, LuminTask t14)
        {
            const int count = 15;
            TaskTracker.TrackActiveTask(this, count);
            SetAwaiter(t0 , 0);  SetAwaiter(t1 , 1);  SetAwaiter(t2 , 2);  SetAwaiter(t3 , 3);
            SetAwaiter(t4 , 4);  SetAwaiter(t5 , 5);  SetAwaiter(t6 , 6);  SetAwaiter(t7 , 7);
            SetAwaiter(t8 , 8);  SetAwaiter(t9 , 9);  SetAwaiter(t10, 10); SetAwaiter(t11, 11);
            SetAwaiter(t12, 12); SetAwaiter(t13, 13); SetAwaiter(t14, 14);
        }

        void SetAwaiter(LuminTask task, int index)
        {
            LuminTaskAwaiter awaiter = default;
            try
            {
                awaiter = task.GetAwaiter();
            }
            catch (Exception ex)
            {
                core.TrySetException(ex);
            }

            if (awaiter.IsCompleted)
            {
                TryInvokeContinuation(this, awaiter, index);
            }
            else
            {
                awaiter.SourceOnCompleted(static state =>
                {
                    using (var t = (StateTuple<WhenAnyPromise, LuminTaskAwaiter, int>)state)
                    {
                        TryInvokeContinuation(t.Item1, t.Item2, t.Item3);
                    }
                }, StateTuple.Create(this, awaiter, index));
            }
        }

        static void TryInvokeContinuation(WhenAnyPromise self, in LuminTaskAwaiter awaiter, int i)
        {
            try
            {
                awaiter.GetResult();
            }
            catch (Exception ex)
            {
                self.core.TrySetException(ex);
                return;
            }

            if (Interlocked.Increment(ref self.completedCount) == 1)
            {
                self.core.TrySetResult(i);
            }
        }

        public int GetResult(short token)
        {
            TaskTracker.RemoveTracking(this);
            GC.SuppressFinalize(this);
            return core.GetResult(token);
        }

        public LuminTaskStatus GetStatus(short token)
        {
            return core.GetStatus(token);
        }

        public void OnCompleted(Action<object> continuation, object state, short token)
        {
            core.OnCompleted(continuation, state, token);
        }

        public LuminTaskStatus UnsafeGetStatus()
        {
            return core.UnsafeGetStatus();
        }

        void ILuminTaskSource.GetResult(short token)
        {
            GetResult(token);
        }
    }
}