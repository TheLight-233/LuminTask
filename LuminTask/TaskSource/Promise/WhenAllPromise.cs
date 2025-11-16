
using System;
using System.Threading;
using System.Collections.Generic;
using System.Linq;

namespace LuminThread.TaskSource.Promise;

public sealed unsafe class WhenAllPromise<T1, T2>
{
    T1 t1 = default;
    T2 t2 = default;
    int completedCount;
    internal LuminTaskSourceCore<(T1, T2)>* core;

    public static WhenAllPromise<T1, T2> Create(LuminTask<T1> task1, LuminTask<T2> task2)
    {
        return new WhenAllPromise<T1, T2>(task1, task2);
    }
    
    public WhenAllPromise(LuminTask<T1> task1, LuminTask<T2> task2)
    {
        core = LuminTaskSourceCore<(T1, T2)>.Create();
        
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
                    var t = (Tuple<WhenAllPromise<T1, T2>, LuminTaskAwaiter<T1>>)state;
                    TryInvokeContinuationT1(t.Item1, t.Item2);
                }, Tuple.Create(this, awaiter));
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
                    var t = (Tuple<WhenAllPromise<T1, T2>, LuminTaskAwaiter<T2>>)state;
                    TryInvokeContinuationT2(t.Item1, t.Item2);
                }, Tuple.Create(this, awaiter));
            }
        }
    }

    static void TryInvokeContinuationT1(WhenAllPromise<T1, T2> self, in LuminTaskAwaiter<T1> awaiter)
    {
        try
        {
            self.t1 = awaiter.GetResult();
        }
        catch (Exception ex)
        {
            LuminTaskSourceCore<(T1, T2)>.TrySetException(self.core, ex);
            LuminTaskSourceCore<(T1, T2)>.Dispose(self.core);
            return;
        }
                
        if (Interlocked.Increment(ref self.completedCount) == 2)
        {
            LuminTaskSourceCore<(T1, T2)>.TrySetResult(self.core, (self.t1, self.t2));
            LuminTaskSourceCore<(T1, T2)>.Dispose(self.core);
        }
    }

    static void TryInvokeContinuationT2(WhenAllPromise<T1, T2> self, in LuminTaskAwaiter<T2> awaiter)
    {
        try
        {
            self.t2 = awaiter.GetResult();
        }
        catch (Exception ex)
        {
            LuminTaskSourceCore<(T1, T2)>.TrySetException(self.core, ex);
            LuminTaskSourceCore<(T1, T2)>.Dispose(self.core);
            return;
        }
                
        if (Interlocked.Increment(ref self.completedCount) == 2)
        {
            LuminTaskSourceCore<(T1, T2)>.TrySetResult(self.core, (self.t1, self.t2));
            LuminTaskSourceCore<(T1, T2)>.Dispose(self.core);
        }
    }

}

public sealed unsafe class WhenAllPromise<T1, T2, T3>
{
    T1 t1 = default;
    T2 t2 = default;
    T3 t3 = default;
    int completedCount;
    internal LuminTaskSourceCore<(T1, T2, T3)>* core;

    public static WhenAllPromise<T1, T2, T3> Create(LuminTask<T1> task1, LuminTask<T2> task2, LuminTask<T3> task3)
    {
        return new WhenAllPromise<T1, T2, T3>(task1, task2, task3);
    }
    
    public WhenAllPromise(LuminTask<T1> task1, LuminTask<T2> task2, LuminTask<T3> task3)
    {
        core = LuminTaskSourceCore<(T1, T2, T3)>.Create();
        
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
                    var t = (Tuple<WhenAllPromise<T1, T2, T3>, LuminTaskAwaiter<T1>>)state;
                    TryInvokeContinuationT1(t.Item1, t.Item2);
                }, Tuple.Create(this, awaiter));
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
                    var t = (Tuple<WhenAllPromise<T1, T2, T3>, LuminTaskAwaiter<T2>>)state;
                    TryInvokeContinuationT2(t.Item1, t.Item2);
                }, Tuple.Create(this, awaiter));
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
                    var t = (Tuple<WhenAllPromise<T1, T2, T3>, LuminTaskAwaiter<T3>>)state;
                    TryInvokeContinuationT3(t.Item1, t.Item2);
                }, Tuple.Create(this, awaiter));
            }
        }
    }

    static void TryInvokeContinuationT1(WhenAllPromise<T1, T2, T3> self, in LuminTaskAwaiter<T1> awaiter)
    {
        try
        {
            self.t1 = awaiter.GetResult();
        }
        catch (Exception ex)
        {
            LuminTaskSourceCore<(T1, T2, T3)>.TrySetException(self.core, ex);
            LuminTaskSourceCore<(T1, T2, T3)>.Dispose(self.core);
            return;
        }
                
        if (Interlocked.Increment(ref self.completedCount) == 3)
        {
            LuminTaskSourceCore<(T1, T2, T3)>.TrySetResult(self.core, (self.t1, self.t2, self.t3));
            LuminTaskSourceCore<(T1, T2, T3)>.Dispose(self.core);
        }
    }

    static void TryInvokeContinuationT2(WhenAllPromise<T1, T2, T3> self, in LuminTaskAwaiter<T2> awaiter)
    {
        try
        {
            self.t2 = awaiter.GetResult();
        }
        catch (Exception ex)
        {
            LuminTaskSourceCore<(T1, T2, T3)>.TrySetException(self.core, ex);
            LuminTaskSourceCore<(T1, T2, T3)>.Dispose(self.core);
            return;
        }
                
        if (Interlocked.Increment(ref self.completedCount) == 3)
        {
            LuminTaskSourceCore<(T1, T2, T3)>.TrySetResult(self.core, (self.t1, self.t2, self.t3));
            LuminTaskSourceCore<(T1, T2, T3)>.Dispose(self.core);
        }
    }

    static void TryInvokeContinuationT3(WhenAllPromise<T1, T2, T3> self, in LuminTaskAwaiter<T3> awaiter)
    {
        try
        {
            self.t3 = awaiter.GetResult();
        }
        catch (Exception ex)
        {
            LuminTaskSourceCore<(T1, T2, T3)>.TrySetException(self.core, ex);
            LuminTaskSourceCore<(T1, T2, T3)>.Dispose(self.core);
            return;
        }
                
        if (Interlocked.Increment(ref self.completedCount) == 3)
        {
            LuminTaskSourceCore<(T1, T2, T3)>.TrySetResult(self.core, (self.t1, self.t2, self.t3));
            LuminTaskSourceCore<(T1, T2, T3)>.Dispose(self.core);
        }
    }

}

public sealed unsafe class WhenAllPromise<T1, T2, T3, T4>
{
    T1 t1 = default;
    T2 t2 = default;
    T3 t3 = default;
    T4 t4 = default;
    int completedCount;
    internal LuminTaskSourceCore<(T1, T2, T3, T4)>* core;

    public static WhenAllPromise<T1, T2, T3, T4> Create(LuminTask<T1> task1, LuminTask<T2> task2, LuminTask<T3> task3, LuminTask<T4> task4)
    {
        return new WhenAllPromise<T1, T2, T3, T4>(task1, task2, task3, task4);
    }
    
    public WhenAllPromise(LuminTask<T1> task1, LuminTask<T2> task2, LuminTask<T3> task3, LuminTask<T4> task4)
    {
        core = LuminTaskSourceCore<(T1, T2, T3, T4)>.Create();
        
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
                    var t = (Tuple<WhenAllPromise<T1, T2, T3, T4>, LuminTaskAwaiter<T1>>)state;
                    TryInvokeContinuationT1(t.Item1, t.Item2);
                }, Tuple.Create(this, awaiter));
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
                    var t = (Tuple<WhenAllPromise<T1, T2, T3, T4>, LuminTaskAwaiter<T2>>)state;
                    TryInvokeContinuationT2(t.Item1, t.Item2);
                }, Tuple.Create(this, awaiter));
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
                    var t = (Tuple<WhenAllPromise<T1, T2, T3, T4>, LuminTaskAwaiter<T3>>)state;
                    TryInvokeContinuationT3(t.Item1, t.Item2);
                }, Tuple.Create(this, awaiter));
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
                    var t = (Tuple<WhenAllPromise<T1, T2, T3, T4>, LuminTaskAwaiter<T4>>)state;
                    TryInvokeContinuationT4(t.Item1, t.Item2);
                }, Tuple.Create(this, awaiter));
            }
        }
    }

    static void TryInvokeContinuationT1(WhenAllPromise<T1, T2, T3, T4> self, in LuminTaskAwaiter<T1> awaiter)
    {
        try
        {
            self.t1 = awaiter.GetResult();
        }
        catch (Exception ex)
        {
            LuminTaskSourceCore<(T1, T2, T3, T4)>.TrySetException(self.core, ex);
            LuminTaskSourceCore<(T1, T2, T3, T4)>.Dispose(self.core);
            return;
        }
                
        if (Interlocked.Increment(ref self.completedCount) == 4)
        {
            LuminTaskSourceCore<(T1, T2, T3, T4)>.TrySetResult(self.core, (self.t1, self.t2, self.t3, self.t4));
            LuminTaskSourceCore<(T1, T2, T3, T4)>.Dispose(self.core);
        }
    }

    static void TryInvokeContinuationT2(WhenAllPromise<T1, T2, T3, T4> self, in LuminTaskAwaiter<T2> awaiter)
    {
        try
        {
            self.t2 = awaiter.GetResult();
        }
        catch (Exception ex)
        {
            LuminTaskSourceCore<(T1, T2, T3, T4)>.TrySetException(self.core, ex);
            LuminTaskSourceCore<(T1, T2, T3, T4)>.Dispose(self.core);
            return;
        }
                
        if (Interlocked.Increment(ref self.completedCount) == 4)
        {
            LuminTaskSourceCore<(T1, T2, T3, T4)>.TrySetResult(self.core, (self.t1, self.t2, self.t3, self.t4));
            LuminTaskSourceCore<(T1, T2, T3, T4)>.Dispose(self.core);
        }
    }

    static void TryInvokeContinuationT3(WhenAllPromise<T1, T2, T3, T4> self, in LuminTaskAwaiter<T3> awaiter)
    {
        try
        {
            self.t3 = awaiter.GetResult();
        }
        catch (Exception ex)
        {
            LuminTaskSourceCore<(T1, T2, T3, T4)>.TrySetException(self.core, ex);
            LuminTaskSourceCore<(T1, T2, T3, T4)>.Dispose(self.core);
            return;
        }
                
        if (Interlocked.Increment(ref self.completedCount) == 4)
        {
            LuminTaskSourceCore<(T1, T2, T3, T4)>.TrySetResult(self.core, (self.t1, self.t2, self.t3, self.t4));
            LuminTaskSourceCore<(T1, T2, T3, T4)>.Dispose(self.core);
        }
    }

    static void TryInvokeContinuationT4(WhenAllPromise<T1, T2, T3, T4> self, in LuminTaskAwaiter<T4> awaiter)
    {
        try
        {
            self.t4 = awaiter.GetResult();
        }
        catch (Exception ex)
        {
            LuminTaskSourceCore<(T1, T2, T3, T4)>.TrySetException(self.core, ex);
            LuminTaskSourceCore<(T1, T2, T3, T4)>.Dispose(self.core);
            return;
        }
                
        if (Interlocked.Increment(ref self.completedCount) == 4)
        {
            LuminTaskSourceCore<(T1, T2, T3, T4)>.TrySetResult(self.core, (self.t1, self.t2, self.t3, self.t4));
            LuminTaskSourceCore<(T1, T2, T3, T4)>.Dispose(self.core);
        }
    }

}

public sealed unsafe class WhenAllPromise<T1, T2, T3, T4, T5>
{
    T1 t1 = default;
    T2 t2 = default;
    T3 t3 = default;
    T4 t4 = default;
    T5 t5 = default;
    int completedCount;
    internal LuminTaskSourceCore<(T1, T2, T3, T4, T5)>* core;

    public static WhenAllPromise<T1, T2, T3, T4, T5> Create(LuminTask<T1> task1, LuminTask<T2> task2, LuminTask<T3> task3, LuminTask<T4> task4, LuminTask<T5> task5)
    {
        return new WhenAllPromise<T1, T2, T3, T4, T5>(task1, task2, task3, task4, task5);
    }
    
    public WhenAllPromise(LuminTask<T1> task1, LuminTask<T2> task2, LuminTask<T3> task3, LuminTask<T4> task4, LuminTask<T5> task5)
    {
        core = LuminTaskSourceCore<(T1, T2, T3, T4, T5)>.Create();
        
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
                    var t = (Tuple<WhenAllPromise<T1, T2, T3, T4, T5>, LuminTaskAwaiter<T1>>)state;
                    TryInvokeContinuationT1(t.Item1, t.Item2);
                }, Tuple.Create(this, awaiter));
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
                    var t = (Tuple<WhenAllPromise<T1, T2, T3, T4, T5>, LuminTaskAwaiter<T2>>)state;
                    TryInvokeContinuationT2(t.Item1, t.Item2);
                }, Tuple.Create(this, awaiter));
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
                    var t = (Tuple<WhenAllPromise<T1, T2, T3, T4, T5>, LuminTaskAwaiter<T3>>)state;
                    TryInvokeContinuationT3(t.Item1, t.Item2);
                }, Tuple.Create(this, awaiter));
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
                    var t = (Tuple<WhenAllPromise<T1, T2, T3, T4, T5>, LuminTaskAwaiter<T4>>)state;
                    TryInvokeContinuationT4(t.Item1, t.Item2);
                }, Tuple.Create(this, awaiter));
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
                    var t = (Tuple<WhenAllPromise<T1, T2, T3, T4, T5>, LuminTaskAwaiter<T5>>)state;
                    TryInvokeContinuationT5(t.Item1, t.Item2);
                }, Tuple.Create(this, awaiter));
            }
        }
    }

    static void TryInvokeContinuationT1(WhenAllPromise<T1, T2, T3, T4, T5> self, in LuminTaskAwaiter<T1> awaiter)
    {
        try
        {
            self.t1 = awaiter.GetResult();
        }
        catch (Exception ex)
        {
            LuminTaskSourceCore<(T1, T2, T3, T4, T5)>.TrySetException(self.core, ex);
            LuminTaskSourceCore<(T1, T2, T3, T4, T5)>.Dispose(self.core);
            return;
        }
                
        if (Interlocked.Increment(ref self.completedCount) == 5)
        {
            LuminTaskSourceCore<(T1, T2, T3, T4, T5)>.TrySetResult(self.core, (self.t1, self.t2, self.t3, self.t4, self.t5));
            LuminTaskSourceCore<(T1, T2, T3, T4, T5)>.Dispose(self.core);
        }
    }

    static void TryInvokeContinuationT2(WhenAllPromise<T1, T2, T3, T4, T5> self, in LuminTaskAwaiter<T2> awaiter)
    {
        try
        {
            self.t2 = awaiter.GetResult();
        }
        catch (Exception ex)
        {
            LuminTaskSourceCore<(T1, T2, T3, T4, T5)>.TrySetException(self.core, ex);
            LuminTaskSourceCore<(T1, T2, T3, T4, T5)>.Dispose(self.core);
            return;
        }
                
        if (Interlocked.Increment(ref self.completedCount) == 5)
        {
            LuminTaskSourceCore<(T1, T2, T3, T4, T5)>.TrySetResult(self.core, (self.t1, self.t2, self.t3, self.t4, self.t5));
            LuminTaskSourceCore<(T1, T2, T3, T4, T5)>.Dispose(self.core);
        }
    }

    static void TryInvokeContinuationT3(WhenAllPromise<T1, T2, T3, T4, T5> self, in LuminTaskAwaiter<T3> awaiter)
    {
        try
        {
            self.t3 = awaiter.GetResult();
        }
        catch (Exception ex)
        {
            LuminTaskSourceCore<(T1, T2, T3, T4, T5)>.TrySetException(self.core, ex);
            LuminTaskSourceCore<(T1, T2, T3, T4, T5)>.Dispose(self.core);
            return;
        }
                
        if (Interlocked.Increment(ref self.completedCount) == 5)
        {
            LuminTaskSourceCore<(T1, T2, T3, T4, T5)>.TrySetResult(self.core, (self.t1, self.t2, self.t3, self.t4, self.t5));
            LuminTaskSourceCore<(T1, T2, T3, T4, T5)>.Dispose(self.core);
        }
    }

    static void TryInvokeContinuationT4(WhenAllPromise<T1, T2, T3, T4, T5> self, in LuminTaskAwaiter<T4> awaiter)
    {
        try
        {
            self.t4 = awaiter.GetResult();
        }
        catch (Exception ex)
        {
            LuminTaskSourceCore<(T1, T2, T3, T4, T5)>.TrySetException(self.core, ex);
            LuminTaskSourceCore<(T1, T2, T3, T4, T5)>.Dispose(self.core);
            return;
        }
                
        if (Interlocked.Increment(ref self.completedCount) == 5)
        {
            LuminTaskSourceCore<(T1, T2, T3, T4, T5)>.TrySetResult(self.core, (self.t1, self.t2, self.t3, self.t4, self.t5));
            LuminTaskSourceCore<(T1, T2, T3, T4, T5)>.Dispose(self.core);
        }
    }

    static void TryInvokeContinuationT5(WhenAllPromise<T1, T2, T3, T4, T5> self, in LuminTaskAwaiter<T5> awaiter)
    {
        try
        {
            self.t5 = awaiter.GetResult();
        }
        catch (Exception ex)
        {
            LuminTaskSourceCore<(T1, T2, T3, T4, T5)>.TrySetException(self.core, ex);
            LuminTaskSourceCore<(T1, T2, T3, T4, T5)>.Dispose(self.core);
            return;
        }
                
        if (Interlocked.Increment(ref self.completedCount) == 5)
        {
            LuminTaskSourceCore<(T1, T2, T3, T4, T5)>.TrySetResult(self.core, (self.t1, self.t2, self.t3, self.t4, self.t5));
            LuminTaskSourceCore<(T1, T2, T3, T4, T5)>.Dispose(self.core);
        }
    }

}

public sealed unsafe class WhenAllPromise<T1, T2, T3, T4, T5, T6>
{
    T1 t1 = default;
    T2 t2 = default;
    T3 t3 = default;
    T4 t4 = default;
    T5 t5 = default;
    T6 t6 = default;
    int completedCount;
    internal LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6)>* core;

    public static WhenAllPromise<T1, T2, T3, T4, T5, T6> Create(LuminTask<T1> task1, LuminTask<T2> task2, LuminTask<T3> task3, LuminTask<T4> task4, LuminTask<T5> task5, LuminTask<T6> task6)
    {
        return new WhenAllPromise<T1, T2, T3, T4, T5, T6>(task1, task2, task3, task4, task5, task6);
    }
    
    public WhenAllPromise(LuminTask<T1> task1, LuminTask<T2> task2, LuminTask<T3> task3, LuminTask<T4> task4, LuminTask<T5> task5, LuminTask<T6> task6)
    {
        core = LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6)>.Create();
        
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
                    var t = (Tuple<WhenAllPromise<T1, T2, T3, T4, T5, T6>, LuminTaskAwaiter<T1>>)state;
                    TryInvokeContinuationT1(t.Item1, t.Item2);
                }, Tuple.Create(this, awaiter));
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
                    var t = (Tuple<WhenAllPromise<T1, T2, T3, T4, T5, T6>, LuminTaskAwaiter<T2>>)state;
                    TryInvokeContinuationT2(t.Item1, t.Item2);
                }, Tuple.Create(this, awaiter));
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
                    var t = (Tuple<WhenAllPromise<T1, T2, T3, T4, T5, T6>, LuminTaskAwaiter<T3>>)state;
                    TryInvokeContinuationT3(t.Item1, t.Item2);
                }, Tuple.Create(this, awaiter));
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
                    var t = (Tuple<WhenAllPromise<T1, T2, T3, T4, T5, T6>, LuminTaskAwaiter<T4>>)state;
                    TryInvokeContinuationT4(t.Item1, t.Item2);
                }, Tuple.Create(this, awaiter));
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
                    var t = (Tuple<WhenAllPromise<T1, T2, T3, T4, T5, T6>, LuminTaskAwaiter<T5>>)state;
                    TryInvokeContinuationT5(t.Item1, t.Item2);
                }, Tuple.Create(this, awaiter));
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
                    var t = (Tuple<WhenAllPromise<T1, T2, T3, T4, T5, T6>, LuminTaskAwaiter<T6>>)state;
                    TryInvokeContinuationT6(t.Item1, t.Item2);
                }, Tuple.Create(this, awaiter));
            }
        }
    }

    static void TryInvokeContinuationT1(WhenAllPromise<T1, T2, T3, T4, T5, T6> self, in LuminTaskAwaiter<T1> awaiter)
    {
        try
        {
            self.t1 = awaiter.GetResult();
        }
        catch (Exception ex)
        {
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6)>.TrySetException(self.core, ex);
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6)>.Dispose(self.core);
            return;
        }
                
        if (Interlocked.Increment(ref self.completedCount) == 6)
        {
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6)>.TrySetResult(self.core, (self.t1, self.t2, self.t3, self.t4, self.t5, self.t6));
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6)>.Dispose(self.core);
        }
    }

    static void TryInvokeContinuationT2(WhenAllPromise<T1, T2, T3, T4, T5, T6> self, in LuminTaskAwaiter<T2> awaiter)
    {
        try
        {
            self.t2 = awaiter.GetResult();
        }
        catch (Exception ex)
        {
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6)>.TrySetException(self.core, ex);
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6)>.Dispose(self.core);
            return;
        }
                
        if (Interlocked.Increment(ref self.completedCount) == 6)
        {
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6)>.TrySetResult(self.core, (self.t1, self.t2, self.t3, self.t4, self.t5, self.t6));
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6)>.Dispose(self.core);
        }
    }

    static void TryInvokeContinuationT3(WhenAllPromise<T1, T2, T3, T4, T5, T6> self, in LuminTaskAwaiter<T3> awaiter)
    {
        try
        {
            self.t3 = awaiter.GetResult();
        }
        catch (Exception ex)
        {
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6)>.TrySetException(self.core, ex);
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6)>.Dispose(self.core);
            return;
        }
                
        if (Interlocked.Increment(ref self.completedCount) == 6)
        {
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6)>.TrySetResult(self.core, (self.t1, self.t2, self.t3, self.t4, self.t5, self.t6));
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6)>.Dispose(self.core);
        }
    }

    static void TryInvokeContinuationT4(WhenAllPromise<T1, T2, T3, T4, T5, T6> self, in LuminTaskAwaiter<T4> awaiter)
    {
        try
        {
            self.t4 = awaiter.GetResult();
        }
        catch (Exception ex)
        {
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6)>.TrySetException(self.core, ex);
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6)>.Dispose(self.core);
            return;
        }
                
        if (Interlocked.Increment(ref self.completedCount) == 6)
        {
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6)>.TrySetResult(self.core, (self.t1, self.t2, self.t3, self.t4, self.t5, self.t6));
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6)>.Dispose(self.core);
        }
    }

    static void TryInvokeContinuationT5(WhenAllPromise<T1, T2, T3, T4, T5, T6> self, in LuminTaskAwaiter<T5> awaiter)
    {
        try
        {
            self.t5 = awaiter.GetResult();
        }
        catch (Exception ex)
        {
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6)>.TrySetException(self.core, ex);
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6)>.Dispose(self.core);
            return;
        }
                
        if (Interlocked.Increment(ref self.completedCount) == 6)
        {
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6)>.TrySetResult(self.core, (self.t1, self.t2, self.t3, self.t4, self.t5, self.t6));
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6)>.Dispose(self.core);
        }
    }

    static void TryInvokeContinuationT6(WhenAllPromise<T1, T2, T3, T4, T5, T6> self, in LuminTaskAwaiter<T6> awaiter)
    {
        try
        {
            self.t6 = awaiter.GetResult();
        }
        catch (Exception ex)
        {
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6)>.TrySetException(self.core, ex);
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6)>.Dispose(self.core);
            return;
        }
                
        if (Interlocked.Increment(ref self.completedCount) == 6)
        {
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6)>.TrySetResult(self.core, (self.t1, self.t2, self.t3, self.t4, self.t5, self.t6));
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6)>.Dispose(self.core);
        }
    }

}

public sealed unsafe class WhenAllPromise<T1, T2, T3, T4, T5, T6, T7>
{
    T1 t1 = default;
    T2 t2 = default;
    T3 t3 = default;
    T4 t4 = default;
    T5 t5 = default;
    T6 t6 = default;
    T7 t7 = default;
    int completedCount;
    internal LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7)>* core;

    public static WhenAllPromise<T1, T2, T3, T4, T5, T6, T7> Create(LuminTask<T1> task1, LuminTask<T2> task2, LuminTask<T3> task3, LuminTask<T4> task4, LuminTask<T5> task5, LuminTask<T6> task6, LuminTask<T7> task7)
    {
        return new WhenAllPromise<T1, T2, T3, T4, T5, T6, T7>(task1, task2, task3, task4, task5, task6, task7);
    }
    
    public WhenAllPromise(LuminTask<T1> task1, LuminTask<T2> task2, LuminTask<T3> task3, LuminTask<T4> task4, LuminTask<T5> task5, LuminTask<T6> task6, LuminTask<T7> task7)
    {
        core = LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7)>.Create();
        
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
                    var t = (Tuple<WhenAllPromise<T1, T2, T3, T4, T5, T6, T7>, LuminTaskAwaiter<T1>>)state;
                    TryInvokeContinuationT1(t.Item1, t.Item2);
                }, Tuple.Create(this, awaiter));
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
                    var t = (Tuple<WhenAllPromise<T1, T2, T3, T4, T5, T6, T7>, LuminTaskAwaiter<T2>>)state;
                    TryInvokeContinuationT2(t.Item1, t.Item2);
                }, Tuple.Create(this, awaiter));
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
                    var t = (Tuple<WhenAllPromise<T1, T2, T3, T4, T5, T6, T7>, LuminTaskAwaiter<T3>>)state;
                    TryInvokeContinuationT3(t.Item1, t.Item2);
                }, Tuple.Create(this, awaiter));
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
                    var t = (Tuple<WhenAllPromise<T1, T2, T3, T4, T5, T6, T7>, LuminTaskAwaiter<T4>>)state;
                    TryInvokeContinuationT4(t.Item1, t.Item2);
                }, Tuple.Create(this, awaiter));
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
                    var t = (Tuple<WhenAllPromise<T1, T2, T3, T4, T5, T6, T7>, LuminTaskAwaiter<T5>>)state;
                    TryInvokeContinuationT5(t.Item1, t.Item2);
                }, Tuple.Create(this, awaiter));
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
                    var t = (Tuple<WhenAllPromise<T1, T2, T3, T4, T5, T6, T7>, LuminTaskAwaiter<T6>>)state;
                    TryInvokeContinuationT6(t.Item1, t.Item2);
                }, Tuple.Create(this, awaiter));
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
                    var t = (Tuple<WhenAllPromise<T1, T2, T3, T4, T5, T6, T7>, LuminTaskAwaiter<T7>>)state;
                    TryInvokeContinuationT7(t.Item1, t.Item2);
                }, Tuple.Create(this, awaiter));
            }
        }
    }

    static void TryInvokeContinuationT1(WhenAllPromise<T1, T2, T3, T4, T5, T6, T7> self, in LuminTaskAwaiter<T1> awaiter)
    {
        try
        {
            self.t1 = awaiter.GetResult();
        }
        catch (Exception ex)
        {
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7)>.TrySetException(self.core, ex);
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7)>.Dispose(self.core);
            return;
        }
                
        if (Interlocked.Increment(ref self.completedCount) == 7)
        {
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7)>.TrySetResult(self.core, (self.t1, self.t2, self.t3, self.t4, self.t5, self.t6, self.t7));
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7)>.Dispose(self.core);
        }
    }

    static void TryInvokeContinuationT2(WhenAllPromise<T1, T2, T3, T4, T5, T6, T7> self, in LuminTaskAwaiter<T2> awaiter)
    {
        try
        {
            self.t2 = awaiter.GetResult();
        }
        catch (Exception ex)
        {
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7)>.TrySetException(self.core, ex);
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7)>.Dispose(self.core);
            return;
        }
                
        if (Interlocked.Increment(ref self.completedCount) == 7)
        {
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7)>.TrySetResult(self.core, (self.t1, self.t2, self.t3, self.t4, self.t5, self.t6, self.t7));
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7)>.Dispose(self.core);
        }
    }

    static void TryInvokeContinuationT3(WhenAllPromise<T1, T2, T3, T4, T5, T6, T7> self, in LuminTaskAwaiter<T3> awaiter)
    {
        try
        {
            self.t3 = awaiter.GetResult();
        }
        catch (Exception ex)
        {
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7)>.TrySetException(self.core, ex);
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7)>.Dispose(self.core);
            return;
        }
                
        if (Interlocked.Increment(ref self.completedCount) == 7)
        {
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7)>.TrySetResult(self.core, (self.t1, self.t2, self.t3, self.t4, self.t5, self.t6, self.t7));
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7)>.Dispose(self.core);
        }
    }

    static void TryInvokeContinuationT4(WhenAllPromise<T1, T2, T3, T4, T5, T6, T7> self, in LuminTaskAwaiter<T4> awaiter)
    {
        try
        {
            self.t4 = awaiter.GetResult();
        }
        catch (Exception ex)
        {
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7)>.TrySetException(self.core, ex);
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7)>.Dispose(self.core);
            return;
        }
                
        if (Interlocked.Increment(ref self.completedCount) == 7)
        {
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7)>.TrySetResult(self.core, (self.t1, self.t2, self.t3, self.t4, self.t5, self.t6, self.t7));
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7)>.Dispose(self.core);
        }
    }

    static void TryInvokeContinuationT5(WhenAllPromise<T1, T2, T3, T4, T5, T6, T7> self, in LuminTaskAwaiter<T5> awaiter)
    {
        try
        {
            self.t5 = awaiter.GetResult();
        }
        catch (Exception ex)
        {
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7)>.TrySetException(self.core, ex);
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7)>.Dispose(self.core);
            return;
        }
                
        if (Interlocked.Increment(ref self.completedCount) == 7)
        {
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7)>.TrySetResult(self.core, (self.t1, self.t2, self.t3, self.t4, self.t5, self.t6, self.t7));
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7)>.Dispose(self.core);
        }
    }

    static void TryInvokeContinuationT6(WhenAllPromise<T1, T2, T3, T4, T5, T6, T7> self, in LuminTaskAwaiter<T6> awaiter)
    {
        try
        {
            self.t6 = awaiter.GetResult();
        }
        catch (Exception ex)
        {
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7)>.TrySetException(self.core, ex);
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7)>.Dispose(self.core);
            return;
        }
                
        if (Interlocked.Increment(ref self.completedCount) == 7)
        {
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7)>.TrySetResult(self.core, (self.t1, self.t2, self.t3, self.t4, self.t5, self.t6, self.t7));
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7)>.Dispose(self.core);
        }
    }

    static void TryInvokeContinuationT7(WhenAllPromise<T1, T2, T3, T4, T5, T6, T7> self, in LuminTaskAwaiter<T7> awaiter)
    {
        try
        {
            self.t7 = awaiter.GetResult();
        }
        catch (Exception ex)
        {
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7)>.TrySetException(self.core, ex);
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7)>.Dispose(self.core);
            return;
        }
                
        if (Interlocked.Increment(ref self.completedCount) == 7)
        {
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7)>.TrySetResult(self.core, (self.t1, self.t2, self.t3, self.t4, self.t5, self.t6, self.t7));
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7)>.Dispose(self.core);
        }
    }

}

public sealed unsafe class WhenAllPromise<T1, T2, T3, T4, T5, T6, T7, T8>
{
    T1 t1 = default;
    T2 t2 = default;
    T3 t3 = default;
    T4 t4 = default;
    T5 t5 = default;
    T6 t6 = default;
    T7 t7 = default;
    T8 t8 = default;
    int completedCount;
    internal LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8)>* core;

    public static WhenAllPromise<T1, T2, T3, T4, T5, T6, T7, T8> Create(LuminTask<T1> task1, LuminTask<T2> task2, LuminTask<T3> task3, LuminTask<T4> task4, LuminTask<T5> task5, LuminTask<T6> task6, LuminTask<T7> task7, LuminTask<T8> task8)
    {
        return new WhenAllPromise<T1, T2, T3, T4, T5, T6, T7, T8>(task1, task2, task3, task4, task5, task6, task7, task8);
    }
    
    public WhenAllPromise(LuminTask<T1> task1, LuminTask<T2> task2, LuminTask<T3> task3, LuminTask<T4> task4, LuminTask<T5> task5, LuminTask<T6> task6, LuminTask<T7> task7, LuminTask<T8> task8)
    {
        core = LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8)>.Create();
        
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
                    var t = (Tuple<WhenAllPromise<T1, T2, T3, T4, T5, T6, T7, T8>, LuminTaskAwaiter<T1>>)state;
                    TryInvokeContinuationT1(t.Item1, t.Item2);
                }, Tuple.Create(this, awaiter));
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
                    var t = (Tuple<WhenAllPromise<T1, T2, T3, T4, T5, T6, T7, T8>, LuminTaskAwaiter<T2>>)state;
                    TryInvokeContinuationT2(t.Item1, t.Item2);
                }, Tuple.Create(this, awaiter));
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
                    var t = (Tuple<WhenAllPromise<T1, T2, T3, T4, T5, T6, T7, T8>, LuminTaskAwaiter<T3>>)state;
                    TryInvokeContinuationT3(t.Item1, t.Item2);
                }, Tuple.Create(this, awaiter));
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
                    var t = (Tuple<WhenAllPromise<T1, T2, T3, T4, T5, T6, T7, T8>, LuminTaskAwaiter<T4>>)state;
                    TryInvokeContinuationT4(t.Item1, t.Item2);
                }, Tuple.Create(this, awaiter));
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
                    var t = (Tuple<WhenAllPromise<T1, T2, T3, T4, T5, T6, T7, T8>, LuminTaskAwaiter<T5>>)state;
                    TryInvokeContinuationT5(t.Item1, t.Item2);
                }, Tuple.Create(this, awaiter));
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
                    var t = (Tuple<WhenAllPromise<T1, T2, T3, T4, T5, T6, T7, T8>, LuminTaskAwaiter<T6>>)state;
                    TryInvokeContinuationT6(t.Item1, t.Item2);
                }, Tuple.Create(this, awaiter));
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
                    var t = (Tuple<WhenAllPromise<T1, T2, T3, T4, T5, T6, T7, T8>, LuminTaskAwaiter<T7>>)state;
                    TryInvokeContinuationT7(t.Item1, t.Item2);
                }, Tuple.Create(this, awaiter));
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
                    var t = (Tuple<WhenAllPromise<T1, T2, T3, T4, T5, T6, T7, T8>, LuminTaskAwaiter<T8>>)state;
                    TryInvokeContinuationT8(t.Item1, t.Item2);
                }, Tuple.Create(this, awaiter));
            }
        }
    }

    static void TryInvokeContinuationT1(WhenAllPromise<T1, T2, T3, T4, T5, T6, T7, T8> self, in LuminTaskAwaiter<T1> awaiter)
    {
        try
        {
            self.t1 = awaiter.GetResult();
        }
        catch (Exception ex)
        {
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8)>.TrySetException(self.core, ex);
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8)>.Dispose(self.core);
            return;
        }
                
        if (Interlocked.Increment(ref self.completedCount) == 8)
        {
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8)>.TrySetResult(self.core, (self.t1, self.t2, self.t3, self.t4, self.t5, self.t6, self.t7, self.t8));
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8)>.Dispose(self.core);
        }
    }

    static void TryInvokeContinuationT2(WhenAllPromise<T1, T2, T3, T4, T5, T6, T7, T8> self, in LuminTaskAwaiter<T2> awaiter)
    {
        try
        {
            self.t2 = awaiter.GetResult();
        }
        catch (Exception ex)
        {
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8)>.TrySetException(self.core, ex);
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8)>.Dispose(self.core);
            return;
        }
                
        if (Interlocked.Increment(ref self.completedCount) == 8)
        {
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8)>.TrySetResult(self.core, (self.t1, self.t2, self.t3, self.t4, self.t5, self.t6, self.t7, self.t8));
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8)>.Dispose(self.core);
        }
    }

    static void TryInvokeContinuationT3(WhenAllPromise<T1, T2, T3, T4, T5, T6, T7, T8> self, in LuminTaskAwaiter<T3> awaiter)
    {
        try
        {
            self.t3 = awaiter.GetResult();
        }
        catch (Exception ex)
        {
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8)>.TrySetException(self.core, ex);
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8)>.Dispose(self.core);
            return;
        }
                
        if (Interlocked.Increment(ref self.completedCount) == 8)
        {
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8)>.TrySetResult(self.core, (self.t1, self.t2, self.t3, self.t4, self.t5, self.t6, self.t7, self.t8));
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8)>.Dispose(self.core);
        }
    }

    static void TryInvokeContinuationT4(WhenAllPromise<T1, T2, T3, T4, T5, T6, T7, T8> self, in LuminTaskAwaiter<T4> awaiter)
    {
        try
        {
            self.t4 = awaiter.GetResult();
        }
        catch (Exception ex)
        {
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8)>.TrySetException(self.core, ex);
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8)>.Dispose(self.core);
            return;
        }
                
        if (Interlocked.Increment(ref self.completedCount) == 8)
        {
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8)>.TrySetResult(self.core, (self.t1, self.t2, self.t3, self.t4, self.t5, self.t6, self.t7, self.t8));
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8)>.Dispose(self.core);
        }
    }

    static void TryInvokeContinuationT5(WhenAllPromise<T1, T2, T3, T4, T5, T6, T7, T8> self, in LuminTaskAwaiter<T5> awaiter)
    {
        try
        {
            self.t5 = awaiter.GetResult();
        }
        catch (Exception ex)
        {
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8)>.TrySetException(self.core, ex);
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8)>.Dispose(self.core);
            return;
        }
                
        if (Interlocked.Increment(ref self.completedCount) == 8)
        {
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8)>.TrySetResult(self.core, (self.t1, self.t2, self.t3, self.t4, self.t5, self.t6, self.t7, self.t8));
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8)>.Dispose(self.core);
        }
    }

    static void TryInvokeContinuationT6(WhenAllPromise<T1, T2, T3, T4, T5, T6, T7, T8> self, in LuminTaskAwaiter<T6> awaiter)
    {
        try
        {
            self.t6 = awaiter.GetResult();
        }
        catch (Exception ex)
        {
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8)>.TrySetException(self.core, ex);
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8)>.Dispose(self.core);
            return;
        }
                
        if (Interlocked.Increment(ref self.completedCount) == 8)
        {
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8)>.TrySetResult(self.core, (self.t1, self.t2, self.t3, self.t4, self.t5, self.t6, self.t7, self.t8));
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8)>.Dispose(self.core);
        }
    }

    static void TryInvokeContinuationT7(WhenAllPromise<T1, T2, T3, T4, T5, T6, T7, T8> self, in LuminTaskAwaiter<T7> awaiter)
    {
        try
        {
            self.t7 = awaiter.GetResult();
        }
        catch (Exception ex)
        {
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8)>.TrySetException(self.core, ex);
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8)>.Dispose(self.core);
            return;
        }
                
        if (Interlocked.Increment(ref self.completedCount) == 8)
        {
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8)>.TrySetResult(self.core, (self.t1, self.t2, self.t3, self.t4, self.t5, self.t6, self.t7, self.t8));
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8)>.Dispose(self.core);
        }
    }

    static void TryInvokeContinuationT8(WhenAllPromise<T1, T2, T3, T4, T5, T6, T7, T8> self, in LuminTaskAwaiter<T8> awaiter)
    {
        try
        {
            self.t8 = awaiter.GetResult();
        }
        catch (Exception ex)
        {
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8)>.TrySetException(self.core, ex);
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8)>.Dispose(self.core);
            return;
        }
                
        if (Interlocked.Increment(ref self.completedCount) == 8)
        {
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8)>.TrySetResult(self.core, (self.t1, self.t2, self.t3, self.t4, self.t5, self.t6, self.t7, self.t8));
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8)>.Dispose(self.core);
        }
    }

}

public sealed unsafe class WhenAllPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9>
{
    T1 t1 = default;
    T2 t2 = default;
    T3 t3 = default;
    T4 t4 = default;
    T5 t5 = default;
    T6 t6 = default;
    T7 t7 = default;
    T8 t8 = default;
    T9 t9 = default;
    int completedCount;
    internal LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9)>* core;

    public static WhenAllPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9> Create(LuminTask<T1> task1, LuminTask<T2> task2, LuminTask<T3> task3, LuminTask<T4> task4, LuminTask<T5> task5, LuminTask<T6> task6, LuminTask<T7> task7, LuminTask<T8> task8, LuminTask<T9> task9)
    {
        return new WhenAllPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9>(task1, task2, task3, task4, task5, task6, task7, task8, task9);
    }
    
    public WhenAllPromise(LuminTask<T1> task1, LuminTask<T2> task2, LuminTask<T3> task3, LuminTask<T4> task4, LuminTask<T5> task5, LuminTask<T6> task6, LuminTask<T7> task7, LuminTask<T8> task8, LuminTask<T9> task9)
    {
        core = LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9)>.Create();
        
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
                    var t = (Tuple<WhenAllPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9>, LuminTaskAwaiter<T1>>)state;
                    TryInvokeContinuationT1(t.Item1, t.Item2);
                }, Tuple.Create(this, awaiter));
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
                    var t = (Tuple<WhenAllPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9>, LuminTaskAwaiter<T2>>)state;
                    TryInvokeContinuationT2(t.Item1, t.Item2);
                }, Tuple.Create(this, awaiter));
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
                    var t = (Tuple<WhenAllPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9>, LuminTaskAwaiter<T3>>)state;
                    TryInvokeContinuationT3(t.Item1, t.Item2);
                }, Tuple.Create(this, awaiter));
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
                    var t = (Tuple<WhenAllPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9>, LuminTaskAwaiter<T4>>)state;
                    TryInvokeContinuationT4(t.Item1, t.Item2);
                }, Tuple.Create(this, awaiter));
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
                    var t = (Tuple<WhenAllPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9>, LuminTaskAwaiter<T5>>)state;
                    TryInvokeContinuationT5(t.Item1, t.Item2);
                }, Tuple.Create(this, awaiter));
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
                    var t = (Tuple<WhenAllPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9>, LuminTaskAwaiter<T6>>)state;
                    TryInvokeContinuationT6(t.Item1, t.Item2);
                }, Tuple.Create(this, awaiter));
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
                    var t = (Tuple<WhenAllPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9>, LuminTaskAwaiter<T7>>)state;
                    TryInvokeContinuationT7(t.Item1, t.Item2);
                }, Tuple.Create(this, awaiter));
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
                    var t = (Tuple<WhenAllPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9>, LuminTaskAwaiter<T8>>)state;
                    TryInvokeContinuationT8(t.Item1, t.Item2);
                }, Tuple.Create(this, awaiter));
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
                    var t = (Tuple<WhenAllPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9>, LuminTaskAwaiter<T9>>)state;
                    TryInvokeContinuationT9(t.Item1, t.Item2);
                }, Tuple.Create(this, awaiter));
            }
        }
    }

    static void TryInvokeContinuationT1(WhenAllPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9> self, in LuminTaskAwaiter<T1> awaiter)
    {
        try
        {
            self.t1 = awaiter.GetResult();
        }
        catch (Exception ex)
        {
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9)>.TrySetException(self.core, ex);
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9)>.Dispose(self.core);
            return;
        }
                
        if (Interlocked.Increment(ref self.completedCount) == 9)
        {
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9)>.TrySetResult(self.core, (self.t1, self.t2, self.t3, self.t4, self.t5, self.t6, self.t7, self.t8, self.t9));
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9)>.Dispose(self.core);
        }
    }

    static void TryInvokeContinuationT2(WhenAllPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9> self, in LuminTaskAwaiter<T2> awaiter)
    {
        try
        {
            self.t2 = awaiter.GetResult();
        }
        catch (Exception ex)
        {
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9)>.TrySetException(self.core, ex);
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9)>.Dispose(self.core);
            return;
        }
                
        if (Interlocked.Increment(ref self.completedCount) == 9)
        {
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9)>.TrySetResult(self.core, (self.t1, self.t2, self.t3, self.t4, self.t5, self.t6, self.t7, self.t8, self.t9));
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9)>.Dispose(self.core);
        }
    }

    static void TryInvokeContinuationT3(WhenAllPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9> self, in LuminTaskAwaiter<T3> awaiter)
    {
        try
        {
            self.t3 = awaiter.GetResult();
        }
        catch (Exception ex)
        {
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9)>.TrySetException(self.core, ex);
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9)>.Dispose(self.core);
            return;
        }
                
        if (Interlocked.Increment(ref self.completedCount) == 9)
        {
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9)>.TrySetResult(self.core, (self.t1, self.t2, self.t3, self.t4, self.t5, self.t6, self.t7, self.t8, self.t9));
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9)>.Dispose(self.core);
        }
    }

    static void TryInvokeContinuationT4(WhenAllPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9> self, in LuminTaskAwaiter<T4> awaiter)
    {
        try
        {
            self.t4 = awaiter.GetResult();
        }
        catch (Exception ex)
        {
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9)>.TrySetException(self.core, ex);
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9)>.Dispose(self.core);
            return;
        }
                
        if (Interlocked.Increment(ref self.completedCount) == 9)
        {
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9)>.TrySetResult(self.core, (self.t1, self.t2, self.t3, self.t4, self.t5, self.t6, self.t7, self.t8, self.t9));
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9)>.Dispose(self.core);
        }
    }

    static void TryInvokeContinuationT5(WhenAllPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9> self, in LuminTaskAwaiter<T5> awaiter)
    {
        try
        {
            self.t5 = awaiter.GetResult();
        }
        catch (Exception ex)
        {
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9)>.TrySetException(self.core, ex);
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9)>.Dispose(self.core);
            return;
        }
                
        if (Interlocked.Increment(ref self.completedCount) == 9)
        {
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9)>.TrySetResult(self.core, (self.t1, self.t2, self.t3, self.t4, self.t5, self.t6, self.t7, self.t8, self.t9));
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9)>.Dispose(self.core);
        }
    }

    static void TryInvokeContinuationT6(WhenAllPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9> self, in LuminTaskAwaiter<T6> awaiter)
    {
        try
        {
            self.t6 = awaiter.GetResult();
        }
        catch (Exception ex)
        {
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9)>.TrySetException(self.core, ex);
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9)>.Dispose(self.core);
            return;
        }
                
        if (Interlocked.Increment(ref self.completedCount) == 9)
        {
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9)>.TrySetResult(self.core, (self.t1, self.t2, self.t3, self.t4, self.t5, self.t6, self.t7, self.t8, self.t9));
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9)>.Dispose(self.core);
        }
    }

    static void TryInvokeContinuationT7(WhenAllPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9> self, in LuminTaskAwaiter<T7> awaiter)
    {
        try
        {
            self.t7 = awaiter.GetResult();
        }
        catch (Exception ex)
        {
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9)>.TrySetException(self.core, ex);
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9)>.Dispose(self.core);
            return;
        }
                
        if (Interlocked.Increment(ref self.completedCount) == 9)
        {
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9)>.TrySetResult(self.core, (self.t1, self.t2, self.t3, self.t4, self.t5, self.t6, self.t7, self.t8, self.t9));
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9)>.Dispose(self.core);
        }
    }

    static void TryInvokeContinuationT8(WhenAllPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9> self, in LuminTaskAwaiter<T8> awaiter)
    {
        try
        {
            self.t8 = awaiter.GetResult();
        }
        catch (Exception ex)
        {
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9)>.TrySetException(self.core, ex);
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9)>.Dispose(self.core);
            return;
        }
                
        if (Interlocked.Increment(ref self.completedCount) == 9)
        {
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9)>.TrySetResult(self.core, (self.t1, self.t2, self.t3, self.t4, self.t5, self.t6, self.t7, self.t8, self.t9));
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9)>.Dispose(self.core);
        }
    }

    static void TryInvokeContinuationT9(WhenAllPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9> self, in LuminTaskAwaiter<T9> awaiter)
    {
        try
        {
            self.t9 = awaiter.GetResult();
        }
        catch (Exception ex)
        {
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9)>.TrySetException(self.core, ex);
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9)>.Dispose(self.core);
            return;
        }
                
        if (Interlocked.Increment(ref self.completedCount) == 9)
        {
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9)>.TrySetResult(self.core, (self.t1, self.t2, self.t3, self.t4, self.t5, self.t6, self.t7, self.t8, self.t9));
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9)>.Dispose(self.core);
        }
    }

}

public sealed unsafe class WhenAllPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>
{
    T1 t1 = default;
    T2 t2 = default;
    T3 t3 = default;
    T4 t4 = default;
    T5 t5 = default;
    T6 t6 = default;
    T7 t7 = default;
    T8 t8 = default;
    T9 t9 = default;
    T10 t10 = default;
    int completedCount;
    internal LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10)>* core;

    public static WhenAllPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> Create(LuminTask<T1> task1, LuminTask<T2> task2, LuminTask<T3> task3, LuminTask<T4> task4, LuminTask<T5> task5, LuminTask<T6> task6, LuminTask<T7> task7, LuminTask<T8> task8, LuminTask<T9> task9, LuminTask<T10> task10)
    {
        return new WhenAllPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(task1, task2, task3, task4, task5, task6, task7, task8, task9, task10);
    }
    
    public WhenAllPromise(LuminTask<T1> task1, LuminTask<T2> task2, LuminTask<T3> task3, LuminTask<T4> task4, LuminTask<T5> task5, LuminTask<T6> task6, LuminTask<T7> task7, LuminTask<T8> task8, LuminTask<T9> task9, LuminTask<T10> task10)
    {
        core = LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10)>.Create();
        
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
                    var t = (Tuple<WhenAllPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>, LuminTaskAwaiter<T1>>)state;
                    TryInvokeContinuationT1(t.Item1, t.Item2);
                }, Tuple.Create(this, awaiter));
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
                    var t = (Tuple<WhenAllPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>, LuminTaskAwaiter<T2>>)state;
                    TryInvokeContinuationT2(t.Item1, t.Item2);
                }, Tuple.Create(this, awaiter));
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
                    var t = (Tuple<WhenAllPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>, LuminTaskAwaiter<T3>>)state;
                    TryInvokeContinuationT3(t.Item1, t.Item2);
                }, Tuple.Create(this, awaiter));
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
                    var t = (Tuple<WhenAllPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>, LuminTaskAwaiter<T4>>)state;
                    TryInvokeContinuationT4(t.Item1, t.Item2);
                }, Tuple.Create(this, awaiter));
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
                    var t = (Tuple<WhenAllPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>, LuminTaskAwaiter<T5>>)state;
                    TryInvokeContinuationT5(t.Item1, t.Item2);
                }, Tuple.Create(this, awaiter));
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
                    var t = (Tuple<WhenAllPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>, LuminTaskAwaiter<T6>>)state;
                    TryInvokeContinuationT6(t.Item1, t.Item2);
                }, Tuple.Create(this, awaiter));
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
                    var t = (Tuple<WhenAllPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>, LuminTaskAwaiter<T7>>)state;
                    TryInvokeContinuationT7(t.Item1, t.Item2);
                }, Tuple.Create(this, awaiter));
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
                    var t = (Tuple<WhenAllPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>, LuminTaskAwaiter<T8>>)state;
                    TryInvokeContinuationT8(t.Item1, t.Item2);
                }, Tuple.Create(this, awaiter));
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
                    var t = (Tuple<WhenAllPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>, LuminTaskAwaiter<T9>>)state;
                    TryInvokeContinuationT9(t.Item1, t.Item2);
                }, Tuple.Create(this, awaiter));
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
                    var t = (Tuple<WhenAllPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>, LuminTaskAwaiter<T10>>)state;
                    TryInvokeContinuationT10(t.Item1, t.Item2);
                }, Tuple.Create(this, awaiter));
            }
        }
    }

    static void TryInvokeContinuationT1(WhenAllPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> self, in LuminTaskAwaiter<T1> awaiter)
    {
        try
        {
            self.t1 = awaiter.GetResult();
        }
        catch (Exception ex)
        {
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10)>.TrySetException(self.core, ex);
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10)>.Dispose(self.core);
            return;
        }
                
        if (Interlocked.Increment(ref self.completedCount) == 10)
        {
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10)>.TrySetResult(self.core, (self.t1, self.t2, self.t3, self.t4, self.t5, self.t6, self.t7, self.t8, self.t9, self.t10));
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10)>.Dispose(self.core);
        }
    }

    static void TryInvokeContinuationT2(WhenAllPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> self, in LuminTaskAwaiter<T2> awaiter)
    {
        try
        {
            self.t2 = awaiter.GetResult();
        }
        catch (Exception ex)
        {
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10)>.TrySetException(self.core, ex);
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10)>.Dispose(self.core);
            return;
        }
                
        if (Interlocked.Increment(ref self.completedCount) == 10)
        {
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10)>.TrySetResult(self.core, (self.t1, self.t2, self.t3, self.t4, self.t5, self.t6, self.t7, self.t8, self.t9, self.t10));
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10)>.Dispose(self.core);
        }
    }

    static void TryInvokeContinuationT3(WhenAllPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> self, in LuminTaskAwaiter<T3> awaiter)
    {
        try
        {
            self.t3 = awaiter.GetResult();
        }
        catch (Exception ex)
        {
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10)>.TrySetException(self.core, ex);
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10)>.Dispose(self.core);
            return;
        }
                
        if (Interlocked.Increment(ref self.completedCount) == 10)
        {
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10)>.TrySetResult(self.core, (self.t1, self.t2, self.t3, self.t4, self.t5, self.t6, self.t7, self.t8, self.t9, self.t10));
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10)>.Dispose(self.core);
        }
    }

    static void TryInvokeContinuationT4(WhenAllPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> self, in LuminTaskAwaiter<T4> awaiter)
    {
        try
        {
            self.t4 = awaiter.GetResult();
        }
        catch (Exception ex)
        {
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10)>.TrySetException(self.core, ex);
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10)>.Dispose(self.core);
            return;
        }
                
        if (Interlocked.Increment(ref self.completedCount) == 10)
        {
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10)>.TrySetResult(self.core, (self.t1, self.t2, self.t3, self.t4, self.t5, self.t6, self.t7, self.t8, self.t9, self.t10));
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10)>.Dispose(self.core);
        }
    }

    static void TryInvokeContinuationT5(WhenAllPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> self, in LuminTaskAwaiter<T5> awaiter)
    {
        try
        {
            self.t5 = awaiter.GetResult();
        }
        catch (Exception ex)
        {
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10)>.TrySetException(self.core, ex);
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10)>.Dispose(self.core);
            return;
        }
                
        if (Interlocked.Increment(ref self.completedCount) == 10)
        {
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10)>.TrySetResult(self.core, (self.t1, self.t2, self.t3, self.t4, self.t5, self.t6, self.t7, self.t8, self.t9, self.t10));
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10)>.Dispose(self.core);
        }
    }

    static void TryInvokeContinuationT6(WhenAllPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> self, in LuminTaskAwaiter<T6> awaiter)
    {
        try
        {
            self.t6 = awaiter.GetResult();
        }
        catch (Exception ex)
        {
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10)>.TrySetException(self.core, ex);
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10)>.Dispose(self.core);
            return;
        }
                
        if (Interlocked.Increment(ref self.completedCount) == 10)
        {
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10)>.TrySetResult(self.core, (self.t1, self.t2, self.t3, self.t4, self.t5, self.t6, self.t7, self.t8, self.t9, self.t10));
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10)>.Dispose(self.core);
        }
    }

    static void TryInvokeContinuationT7(WhenAllPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> self, in LuminTaskAwaiter<T7> awaiter)
    {
        try
        {
            self.t7 = awaiter.GetResult();
        }
        catch (Exception ex)
        {
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10)>.TrySetException(self.core, ex);
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10)>.Dispose(self.core);
            return;
        }
                
        if (Interlocked.Increment(ref self.completedCount) == 10)
        {
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10)>.TrySetResult(self.core, (self.t1, self.t2, self.t3, self.t4, self.t5, self.t6, self.t7, self.t8, self.t9, self.t10));
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10)>.Dispose(self.core);
        }
    }

    static void TryInvokeContinuationT8(WhenAllPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> self, in LuminTaskAwaiter<T8> awaiter)
    {
        try
        {
            self.t8 = awaiter.GetResult();
        }
        catch (Exception ex)
        {
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10)>.TrySetException(self.core, ex);
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10)>.Dispose(self.core);
            return;
        }
                
        if (Interlocked.Increment(ref self.completedCount) == 10)
        {
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10)>.TrySetResult(self.core, (self.t1, self.t2, self.t3, self.t4, self.t5, self.t6, self.t7, self.t8, self.t9, self.t10));
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10)>.Dispose(self.core);
        }
    }

    static void TryInvokeContinuationT9(WhenAllPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> self, in LuminTaskAwaiter<T9> awaiter)
    {
        try
        {
            self.t9 = awaiter.GetResult();
        }
        catch (Exception ex)
        {
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10)>.TrySetException(self.core, ex);
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10)>.Dispose(self.core);
            return;
        }
                
        if (Interlocked.Increment(ref self.completedCount) == 10)
        {
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10)>.TrySetResult(self.core, (self.t1, self.t2, self.t3, self.t4, self.t5, self.t6, self.t7, self.t8, self.t9, self.t10));
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10)>.Dispose(self.core);
        }
    }

    static void TryInvokeContinuationT10(WhenAllPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> self, in LuminTaskAwaiter<T10> awaiter)
    {
        try
        {
            self.t10 = awaiter.GetResult();
        }
        catch (Exception ex)
        {
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10)>.TrySetException(self.core, ex);
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10)>.Dispose(self.core);
            return;
        }
                
        if (Interlocked.Increment(ref self.completedCount) == 10)
        {
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10)>.TrySetResult(self.core, (self.t1, self.t2, self.t3, self.t4, self.t5, self.t6, self.t7, self.t8, self.t9, self.t10));
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10)>.Dispose(self.core);
        }
    }

}

public sealed unsafe class WhenAllPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>
{
    T1 t1 = default;
    T2 t2 = default;
    T3 t3 = default;
    T4 t4 = default;
    T5 t5 = default;
    T6 t6 = default;
    T7 t7 = default;
    T8 t8 = default;
    T9 t9 = default;
    T10 t10 = default;
    T11 t11 = default;
    int completedCount;
    internal LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11)>* core;

    public static WhenAllPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> Create(LuminTask<T1> task1, LuminTask<T2> task2, LuminTask<T3> task3, LuminTask<T4> task4, LuminTask<T5> task5, LuminTask<T6> task6, LuminTask<T7> task7, LuminTask<T8> task8, LuminTask<T9> task9, LuminTask<T10> task10, LuminTask<T11> task11)
    {
        return new WhenAllPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(task1, task2, task3, task4, task5, task6, task7, task8, task9, task10, task11);
    }
    
    public WhenAllPromise(LuminTask<T1> task1, LuminTask<T2> task2, LuminTask<T3> task3, LuminTask<T4> task4, LuminTask<T5> task5, LuminTask<T6> task6, LuminTask<T7> task7, LuminTask<T8> task8, LuminTask<T9> task9, LuminTask<T10> task10, LuminTask<T11> task11)
    {
        core = LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11)>.Create();
        
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
                    var t = (Tuple<WhenAllPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>, LuminTaskAwaiter<T1>>)state;
                    TryInvokeContinuationT1(t.Item1, t.Item2);
                }, Tuple.Create(this, awaiter));
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
                    var t = (Tuple<WhenAllPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>, LuminTaskAwaiter<T2>>)state;
                    TryInvokeContinuationT2(t.Item1, t.Item2);
                }, Tuple.Create(this, awaiter));
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
                    var t = (Tuple<WhenAllPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>, LuminTaskAwaiter<T3>>)state;
                    TryInvokeContinuationT3(t.Item1, t.Item2);
                }, Tuple.Create(this, awaiter));
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
                    var t = (Tuple<WhenAllPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>, LuminTaskAwaiter<T4>>)state;
                    TryInvokeContinuationT4(t.Item1, t.Item2);
                }, Tuple.Create(this, awaiter));
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
                    var t = (Tuple<WhenAllPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>, LuminTaskAwaiter<T5>>)state;
                    TryInvokeContinuationT5(t.Item1, t.Item2);
                }, Tuple.Create(this, awaiter));
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
                    var t = (Tuple<WhenAllPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>, LuminTaskAwaiter<T6>>)state;
                    TryInvokeContinuationT6(t.Item1, t.Item2);
                }, Tuple.Create(this, awaiter));
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
                    var t = (Tuple<WhenAllPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>, LuminTaskAwaiter<T7>>)state;
                    TryInvokeContinuationT7(t.Item1, t.Item2);
                }, Tuple.Create(this, awaiter));
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
                    var t = (Tuple<WhenAllPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>, LuminTaskAwaiter<T8>>)state;
                    TryInvokeContinuationT8(t.Item1, t.Item2);
                }, Tuple.Create(this, awaiter));
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
                    var t = (Tuple<WhenAllPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>, LuminTaskAwaiter<T9>>)state;
                    TryInvokeContinuationT9(t.Item1, t.Item2);
                }, Tuple.Create(this, awaiter));
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
                    var t = (Tuple<WhenAllPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>, LuminTaskAwaiter<T10>>)state;
                    TryInvokeContinuationT10(t.Item1, t.Item2);
                }, Tuple.Create(this, awaiter));
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
                    var t = (Tuple<WhenAllPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>, LuminTaskAwaiter<T11>>)state;
                    TryInvokeContinuationT11(t.Item1, t.Item2);
                }, Tuple.Create(this, awaiter));
            }
        }
    }

    static void TryInvokeContinuationT1(WhenAllPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> self, in LuminTaskAwaiter<T1> awaiter)
    {
        try
        {
            self.t1 = awaiter.GetResult();
        }
        catch (Exception ex)
        {
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11)>.TrySetException(self.core, ex);
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11)>.Dispose(self.core);
            return;
        }
                
        if (Interlocked.Increment(ref self.completedCount) == 11)
        {
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11)>.TrySetResult(self.core, (self.t1, self.t2, self.t3, self.t4, self.t5, self.t6, self.t7, self.t8, self.t9, self.t10, self.t11));
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11)>.Dispose(self.core);
        }
    }

    static void TryInvokeContinuationT2(WhenAllPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> self, in LuminTaskAwaiter<T2> awaiter)
    {
        try
        {
            self.t2 = awaiter.GetResult();
        }
        catch (Exception ex)
        {
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11)>.TrySetException(self.core, ex);
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11)>.Dispose(self.core);
            return;
        }
                
        if (Interlocked.Increment(ref self.completedCount) == 11)
        {
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11)>.TrySetResult(self.core, (self.t1, self.t2, self.t3, self.t4, self.t5, self.t6, self.t7, self.t8, self.t9, self.t10, self.t11));
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11)>.Dispose(self.core);
        }
    }

    static void TryInvokeContinuationT3(WhenAllPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> self, in LuminTaskAwaiter<T3> awaiter)
    {
        try
        {
            self.t3 = awaiter.GetResult();
        }
        catch (Exception ex)
        {
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11)>.TrySetException(self.core, ex);
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11)>.Dispose(self.core);
            return;
        }
                
        if (Interlocked.Increment(ref self.completedCount) == 11)
        {
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11)>.TrySetResult(self.core, (self.t1, self.t2, self.t3, self.t4, self.t5, self.t6, self.t7, self.t8, self.t9, self.t10, self.t11));
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11)>.Dispose(self.core);
        }
    }

    static void TryInvokeContinuationT4(WhenAllPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> self, in LuminTaskAwaiter<T4> awaiter)
    {
        try
        {
            self.t4 = awaiter.GetResult();
        }
        catch (Exception ex)
        {
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11)>.TrySetException(self.core, ex);
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11)>.Dispose(self.core);
            return;
        }
                
        if (Interlocked.Increment(ref self.completedCount) == 11)
        {
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11)>.TrySetResult(self.core, (self.t1, self.t2, self.t3, self.t4, self.t5, self.t6, self.t7, self.t8, self.t9, self.t10, self.t11));
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11)>.Dispose(self.core);
        }
    }

    static void TryInvokeContinuationT5(WhenAllPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> self, in LuminTaskAwaiter<T5> awaiter)
    {
        try
        {
            self.t5 = awaiter.GetResult();
        }
        catch (Exception ex)
        {
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11)>.TrySetException(self.core, ex);
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11)>.Dispose(self.core);
            return;
        }
                
        if (Interlocked.Increment(ref self.completedCount) == 11)
        {
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11)>.TrySetResult(self.core, (self.t1, self.t2, self.t3, self.t4, self.t5, self.t6, self.t7, self.t8, self.t9, self.t10, self.t11));
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11)>.Dispose(self.core);
        }
    }

    static void TryInvokeContinuationT6(WhenAllPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> self, in LuminTaskAwaiter<T6> awaiter)
    {
        try
        {
            self.t6 = awaiter.GetResult();
        }
        catch (Exception ex)
        {
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11)>.TrySetException(self.core, ex);
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11)>.Dispose(self.core);
            return;
        }
                
        if (Interlocked.Increment(ref self.completedCount) == 11)
        {
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11)>.TrySetResult(self.core, (self.t1, self.t2, self.t3, self.t4, self.t5, self.t6, self.t7, self.t8, self.t9, self.t10, self.t11));
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11)>.Dispose(self.core);
        }
    }

    static void TryInvokeContinuationT7(WhenAllPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> self, in LuminTaskAwaiter<T7> awaiter)
    {
        try
        {
            self.t7 = awaiter.GetResult();
        }
        catch (Exception ex)
        {
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11)>.TrySetException(self.core, ex);
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11)>.Dispose(self.core);
            return;
        }
                
        if (Interlocked.Increment(ref self.completedCount) == 11)
        {
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11)>.TrySetResult(self.core, (self.t1, self.t2, self.t3, self.t4, self.t5, self.t6, self.t7, self.t8, self.t9, self.t10, self.t11));
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11)>.Dispose(self.core);
        }
    }

    static void TryInvokeContinuationT8(WhenAllPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> self, in LuminTaskAwaiter<T8> awaiter)
    {
        try
        {
            self.t8 = awaiter.GetResult();
        }
        catch (Exception ex)
        {
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11)>.TrySetException(self.core, ex);
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11)>.Dispose(self.core);
            return;
        }
                
        if (Interlocked.Increment(ref self.completedCount) == 11)
        {
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11)>.TrySetResult(self.core, (self.t1, self.t2, self.t3, self.t4, self.t5, self.t6, self.t7, self.t8, self.t9, self.t10, self.t11));
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11)>.Dispose(self.core);
        }
    }

    static void TryInvokeContinuationT9(WhenAllPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> self, in LuminTaskAwaiter<T9> awaiter)
    {
        try
        {
            self.t9 = awaiter.GetResult();
        }
        catch (Exception ex)
        {
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11)>.TrySetException(self.core, ex);
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11)>.Dispose(self.core);
            return;
        }
                
        if (Interlocked.Increment(ref self.completedCount) == 11)
        {
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11)>.TrySetResult(self.core, (self.t1, self.t2, self.t3, self.t4, self.t5, self.t6, self.t7, self.t8, self.t9, self.t10, self.t11));
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11)>.Dispose(self.core);
        }
    }

    static void TryInvokeContinuationT10(WhenAllPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> self, in LuminTaskAwaiter<T10> awaiter)
    {
        try
        {
            self.t10 = awaiter.GetResult();
        }
        catch (Exception ex)
        {
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11)>.TrySetException(self.core, ex);
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11)>.Dispose(self.core);
            return;
        }
                
        if (Interlocked.Increment(ref self.completedCount) == 11)
        {
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11)>.TrySetResult(self.core, (self.t1, self.t2, self.t3, self.t4, self.t5, self.t6, self.t7, self.t8, self.t9, self.t10, self.t11));
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11)>.Dispose(self.core);
        }
    }

    static void TryInvokeContinuationT11(WhenAllPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> self, in LuminTaskAwaiter<T11> awaiter)
    {
        try
        {
            self.t11 = awaiter.GetResult();
        }
        catch (Exception ex)
        {
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11)>.TrySetException(self.core, ex);
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11)>.Dispose(self.core);
            return;
        }
                
        if (Interlocked.Increment(ref self.completedCount) == 11)
        {
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11)>.TrySetResult(self.core, (self.t1, self.t2, self.t3, self.t4, self.t5, self.t6, self.t7, self.t8, self.t9, self.t10, self.t11));
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11)>.Dispose(self.core);
        }
    }

}

public sealed unsafe class WhenAllPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>
{
    T1 t1 = default;
    T2 t2 = default;
    T3 t3 = default;
    T4 t4 = default;
    T5 t5 = default;
    T6 t6 = default;
    T7 t7 = default;
    T8 t8 = default;
    T9 t9 = default;
    T10 t10 = default;
    T11 t11 = default;
    T12 t12 = default;
    int completedCount;
    internal LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12)>* core;

    public static WhenAllPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> Create(LuminTask<T1> task1, LuminTask<T2> task2, LuminTask<T3> task3, LuminTask<T4> task4, LuminTask<T5> task5, LuminTask<T6> task6, LuminTask<T7> task7, LuminTask<T8> task8, LuminTask<T9> task9, LuminTask<T10> task10, LuminTask<T11> task11, LuminTask<T12> task12)
    {
        return new WhenAllPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(task1, task2, task3, task4, task5, task6, task7, task8, task9, task10, task11, task12);
    }
    
    public WhenAllPromise(LuminTask<T1> task1, LuminTask<T2> task2, LuminTask<T3> task3, LuminTask<T4> task4, LuminTask<T5> task5, LuminTask<T6> task6, LuminTask<T7> task7, LuminTask<T8> task8, LuminTask<T9> task9, LuminTask<T10> task10, LuminTask<T11> task11, LuminTask<T12> task12)
    {
        core = LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12)>.Create();
        
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
                    var t = (Tuple<WhenAllPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>, LuminTaskAwaiter<T1>>)state;
                    TryInvokeContinuationT1(t.Item1, t.Item2);
                }, Tuple.Create(this, awaiter));
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
                    var t = (Tuple<WhenAllPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>, LuminTaskAwaiter<T2>>)state;
                    TryInvokeContinuationT2(t.Item1, t.Item2);
                }, Tuple.Create(this, awaiter));
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
                    var t = (Tuple<WhenAllPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>, LuminTaskAwaiter<T3>>)state;
                    TryInvokeContinuationT3(t.Item1, t.Item2);
                }, Tuple.Create(this, awaiter));
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
                    var t = (Tuple<WhenAllPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>, LuminTaskAwaiter<T4>>)state;
                    TryInvokeContinuationT4(t.Item1, t.Item2);
                }, Tuple.Create(this, awaiter));
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
                    var t = (Tuple<WhenAllPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>, LuminTaskAwaiter<T5>>)state;
                    TryInvokeContinuationT5(t.Item1, t.Item2);
                }, Tuple.Create(this, awaiter));
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
                    var t = (Tuple<WhenAllPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>, LuminTaskAwaiter<T6>>)state;
                    TryInvokeContinuationT6(t.Item1, t.Item2);
                }, Tuple.Create(this, awaiter));
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
                    var t = (Tuple<WhenAllPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>, LuminTaskAwaiter<T7>>)state;
                    TryInvokeContinuationT7(t.Item1, t.Item2);
                }, Tuple.Create(this, awaiter));
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
                    var t = (Tuple<WhenAllPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>, LuminTaskAwaiter<T8>>)state;
                    TryInvokeContinuationT8(t.Item1, t.Item2);
                }, Tuple.Create(this, awaiter));
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
                    var t = (Tuple<WhenAllPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>, LuminTaskAwaiter<T9>>)state;
                    TryInvokeContinuationT9(t.Item1, t.Item2);
                }, Tuple.Create(this, awaiter));
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
                    var t = (Tuple<WhenAllPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>, LuminTaskAwaiter<T10>>)state;
                    TryInvokeContinuationT10(t.Item1, t.Item2);
                }, Tuple.Create(this, awaiter));
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
                    var t = (Tuple<WhenAllPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>, LuminTaskAwaiter<T11>>)state;
                    TryInvokeContinuationT11(t.Item1, t.Item2);
                }, Tuple.Create(this, awaiter));
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
                    var t = (Tuple<WhenAllPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>, LuminTaskAwaiter<T12>>)state;
                    TryInvokeContinuationT12(t.Item1, t.Item2);
                }, Tuple.Create(this, awaiter));
            }
        }
    }

    static void TryInvokeContinuationT1(WhenAllPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> self, in LuminTaskAwaiter<T1> awaiter)
    {
        try
        {
            self.t1 = awaiter.GetResult();
        }
        catch (Exception ex)
        {
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12)>.TrySetException(self.core, ex);
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12)>.Dispose(self.core);
            return;
        }
                
        if (Interlocked.Increment(ref self.completedCount) == 12)
        {
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12)>.TrySetResult(self.core, (self.t1, self.t2, self.t3, self.t4, self.t5, self.t6, self.t7, self.t8, self.t9, self.t10, self.t11, self.t12));
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12)>.Dispose(self.core);
        }
    }

    static void TryInvokeContinuationT2(WhenAllPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> self, in LuminTaskAwaiter<T2> awaiter)
    {
        try
        {
            self.t2 = awaiter.GetResult();
        }
        catch (Exception ex)
        {
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12)>.TrySetException(self.core, ex);
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12)>.Dispose(self.core);
            return;
        }
                
        if (Interlocked.Increment(ref self.completedCount) == 12)
        {
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12)>.TrySetResult(self.core, (self.t1, self.t2, self.t3, self.t4, self.t5, self.t6, self.t7, self.t8, self.t9, self.t10, self.t11, self.t12));
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12)>.Dispose(self.core);
        }
    }

    static void TryInvokeContinuationT3(WhenAllPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> self, in LuminTaskAwaiter<T3> awaiter)
    {
        try
        {
            self.t3 = awaiter.GetResult();
        }
        catch (Exception ex)
        {
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12)>.TrySetException(self.core, ex);
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12)>.Dispose(self.core);
            return;
        }
                
        if (Interlocked.Increment(ref self.completedCount) == 12)
        {
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12)>.TrySetResult(self.core, (self.t1, self.t2, self.t3, self.t4, self.t5, self.t6, self.t7, self.t8, self.t9, self.t10, self.t11, self.t12));
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12)>.Dispose(self.core);
        }
    }

    static void TryInvokeContinuationT4(WhenAllPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> self, in LuminTaskAwaiter<T4> awaiter)
    {
        try
        {
            self.t4 = awaiter.GetResult();
        }
        catch (Exception ex)
        {
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12)>.TrySetException(self.core, ex);
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12)>.Dispose(self.core);
            return;
        }
                
        if (Interlocked.Increment(ref self.completedCount) == 12)
        {
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12)>.TrySetResult(self.core, (self.t1, self.t2, self.t3, self.t4, self.t5, self.t6, self.t7, self.t8, self.t9, self.t10, self.t11, self.t12));
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12)>.Dispose(self.core);
        }
    }

    static void TryInvokeContinuationT5(WhenAllPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> self, in LuminTaskAwaiter<T5> awaiter)
    {
        try
        {
            self.t5 = awaiter.GetResult();
        }
        catch (Exception ex)
        {
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12)>.TrySetException(self.core, ex);
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12)>.Dispose(self.core);
            return;
        }
                
        if (Interlocked.Increment(ref self.completedCount) == 12)
        {
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12)>.TrySetResult(self.core, (self.t1, self.t2, self.t3, self.t4, self.t5, self.t6, self.t7, self.t8, self.t9, self.t10, self.t11, self.t12));
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12)>.Dispose(self.core);
        }
    }

    static void TryInvokeContinuationT6(WhenAllPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> self, in LuminTaskAwaiter<T6> awaiter)
    {
        try
        {
            self.t6 = awaiter.GetResult();
        }
        catch (Exception ex)
        {
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12)>.TrySetException(self.core, ex);
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12)>.Dispose(self.core);
            return;
        }
                
        if (Interlocked.Increment(ref self.completedCount) == 12)
        {
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12)>.TrySetResult(self.core, (self.t1, self.t2, self.t3, self.t4, self.t5, self.t6, self.t7, self.t8, self.t9, self.t10, self.t11, self.t12));
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12)>.Dispose(self.core);
        }
    }

    static void TryInvokeContinuationT7(WhenAllPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> self, in LuminTaskAwaiter<T7> awaiter)
    {
        try
        {
            self.t7 = awaiter.GetResult();
        }
        catch (Exception ex)
        {
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12)>.TrySetException(self.core, ex);
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12)>.Dispose(self.core);
            return;
        }
                
        if (Interlocked.Increment(ref self.completedCount) == 12)
        {
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12)>.TrySetResult(self.core, (self.t1, self.t2, self.t3, self.t4, self.t5, self.t6, self.t7, self.t8, self.t9, self.t10, self.t11, self.t12));
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12)>.Dispose(self.core);
        }
    }

    static void TryInvokeContinuationT8(WhenAllPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> self, in LuminTaskAwaiter<T8> awaiter)
    {
        try
        {
            self.t8 = awaiter.GetResult();
        }
        catch (Exception ex)
        {
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12)>.TrySetException(self.core, ex);
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12)>.Dispose(self.core);
            return;
        }
                
        if (Interlocked.Increment(ref self.completedCount) == 12)
        {
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12)>.TrySetResult(self.core, (self.t1, self.t2, self.t3, self.t4, self.t5, self.t6, self.t7, self.t8, self.t9, self.t10, self.t11, self.t12));
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12)>.Dispose(self.core);
        }
    }

    static void TryInvokeContinuationT9(WhenAllPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> self, in LuminTaskAwaiter<T9> awaiter)
    {
        try
        {
            self.t9 = awaiter.GetResult();
        }
        catch (Exception ex)
        {
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12)>.TrySetException(self.core, ex);
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12)>.Dispose(self.core);
            return;
        }
                
        if (Interlocked.Increment(ref self.completedCount) == 12)
        {
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12)>.TrySetResult(self.core, (self.t1, self.t2, self.t3, self.t4, self.t5, self.t6, self.t7, self.t8, self.t9, self.t10, self.t11, self.t12));
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12)>.Dispose(self.core);
        }
    }

    static void TryInvokeContinuationT10(WhenAllPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> self, in LuminTaskAwaiter<T10> awaiter)
    {
        try
        {
            self.t10 = awaiter.GetResult();
        }
        catch (Exception ex)
        {
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12)>.TrySetException(self.core, ex);
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12)>.Dispose(self.core);
            return;
        }
                
        if (Interlocked.Increment(ref self.completedCount) == 12)
        {
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12)>.TrySetResult(self.core, (self.t1, self.t2, self.t3, self.t4, self.t5, self.t6, self.t7, self.t8, self.t9, self.t10, self.t11, self.t12));
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12)>.Dispose(self.core);
        }
    }

    static void TryInvokeContinuationT11(WhenAllPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> self, in LuminTaskAwaiter<T11> awaiter)
    {
        try
        {
            self.t11 = awaiter.GetResult();
        }
        catch (Exception ex)
        {
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12)>.TrySetException(self.core, ex);
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12)>.Dispose(self.core);
            return;
        }
                
        if (Interlocked.Increment(ref self.completedCount) == 12)
        {
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12)>.TrySetResult(self.core, (self.t1, self.t2, self.t3, self.t4, self.t5, self.t6, self.t7, self.t8, self.t9, self.t10, self.t11, self.t12));
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12)>.Dispose(self.core);
        }
    }

    static void TryInvokeContinuationT12(WhenAllPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> self, in LuminTaskAwaiter<T12> awaiter)
    {
        try
        {
            self.t12 = awaiter.GetResult();
        }
        catch (Exception ex)
        {
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12)>.TrySetException(self.core, ex);
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12)>.Dispose(self.core);
            return;
        }
                
        if (Interlocked.Increment(ref self.completedCount) == 12)
        {
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12)>.TrySetResult(self.core, (self.t1, self.t2, self.t3, self.t4, self.t5, self.t6, self.t7, self.t8, self.t9, self.t10, self.t11, self.t12));
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12)>.Dispose(self.core);
        }
    }

}

public sealed unsafe class WhenAllPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>
{
    T1 t1 = default;
    T2 t2 = default;
    T3 t3 = default;
    T4 t4 = default;
    T5 t5 = default;
    T6 t6 = default;
    T7 t7 = default;
    T8 t8 = default;
    T9 t9 = default;
    T10 t10 = default;
    T11 t11 = default;
    T12 t12 = default;
    T13 t13 = default;
    int completedCount;
    internal LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13)>* core;

    public static WhenAllPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13> Create(LuminTask<T1> task1, LuminTask<T2> task2, LuminTask<T3> task3, LuminTask<T4> task4, LuminTask<T5> task5, LuminTask<T6> task6, LuminTask<T7> task7, LuminTask<T8> task8, LuminTask<T9> task9, LuminTask<T10> task10, LuminTask<T11> task11, LuminTask<T12> task12, LuminTask<T13> task13)
    {
        return new WhenAllPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(task1, task2, task3, task4, task5, task6, task7, task8, task9, task10, task11, task12, task13);
    }
    
    public WhenAllPromise(LuminTask<T1> task1, LuminTask<T2> task2, LuminTask<T3> task3, LuminTask<T4> task4, LuminTask<T5> task5, LuminTask<T6> task6, LuminTask<T7> task7, LuminTask<T8> task8, LuminTask<T9> task9, LuminTask<T10> task10, LuminTask<T11> task11, LuminTask<T12> task12, LuminTask<T13> task13)
    {
        core = LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13)>.Create();
        
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
                    var t = (Tuple<WhenAllPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>, LuminTaskAwaiter<T1>>)state;
                    TryInvokeContinuationT1(t.Item1, t.Item2);
                }, Tuple.Create(this, awaiter));
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
                    var t = (Tuple<WhenAllPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>, LuminTaskAwaiter<T2>>)state;
                    TryInvokeContinuationT2(t.Item1, t.Item2);
                }, Tuple.Create(this, awaiter));
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
                    var t = (Tuple<WhenAllPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>, LuminTaskAwaiter<T3>>)state;
                    TryInvokeContinuationT3(t.Item1, t.Item2);
                }, Tuple.Create(this, awaiter));
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
                    var t = (Tuple<WhenAllPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>, LuminTaskAwaiter<T4>>)state;
                    TryInvokeContinuationT4(t.Item1, t.Item2);
                }, Tuple.Create(this, awaiter));
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
                    var t = (Tuple<WhenAllPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>, LuminTaskAwaiter<T5>>)state;
                    TryInvokeContinuationT5(t.Item1, t.Item2);
                }, Tuple.Create(this, awaiter));
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
                    var t = (Tuple<WhenAllPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>, LuminTaskAwaiter<T6>>)state;
                    TryInvokeContinuationT6(t.Item1, t.Item2);
                }, Tuple.Create(this, awaiter));
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
                    var t = (Tuple<WhenAllPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>, LuminTaskAwaiter<T7>>)state;
                    TryInvokeContinuationT7(t.Item1, t.Item2);
                }, Tuple.Create(this, awaiter));
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
                    var t = (Tuple<WhenAllPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>, LuminTaskAwaiter<T8>>)state;
                    TryInvokeContinuationT8(t.Item1, t.Item2);
                }, Tuple.Create(this, awaiter));
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
                    var t = (Tuple<WhenAllPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>, LuminTaskAwaiter<T9>>)state;
                    TryInvokeContinuationT9(t.Item1, t.Item2);
                }, Tuple.Create(this, awaiter));
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
                    var t = (Tuple<WhenAllPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>, LuminTaskAwaiter<T10>>)state;
                    TryInvokeContinuationT10(t.Item1, t.Item2);
                }, Tuple.Create(this, awaiter));
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
                    var t = (Tuple<WhenAllPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>, LuminTaskAwaiter<T11>>)state;
                    TryInvokeContinuationT11(t.Item1, t.Item2);
                }, Tuple.Create(this, awaiter));
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
                    var t = (Tuple<WhenAllPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>, LuminTaskAwaiter<T12>>)state;
                    TryInvokeContinuationT12(t.Item1, t.Item2);
                }, Tuple.Create(this, awaiter));
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
                    var t = (Tuple<WhenAllPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>, LuminTaskAwaiter<T13>>)state;
                    TryInvokeContinuationT13(t.Item1, t.Item2);
                }, Tuple.Create(this, awaiter));
            }
        }
    }

    static void TryInvokeContinuationT1(WhenAllPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13> self, in LuminTaskAwaiter<T1> awaiter)
    {
        try
        {
            self.t1 = awaiter.GetResult();
        }
        catch (Exception ex)
        {
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13)>.TrySetException(self.core, ex);
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13)>.Dispose(self.core);
            return;
        }
                
        if (Interlocked.Increment(ref self.completedCount) == 13)
        {
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13)>.TrySetResult(self.core, (self.t1, self.t2, self.t3, self.t4, self.t5, self.t6, self.t7, self.t8, self.t9, self.t10, self.t11, self.t12, self.t13));
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13)>.Dispose(self.core);
        }
    }

    static void TryInvokeContinuationT2(WhenAllPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13> self, in LuminTaskAwaiter<T2> awaiter)
    {
        try
        {
            self.t2 = awaiter.GetResult();
        }
        catch (Exception ex)
        {
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13)>.TrySetException(self.core, ex);
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13)>.Dispose(self.core);
            return;
        }
                
        if (Interlocked.Increment(ref self.completedCount) == 13)
        {
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13)>.TrySetResult(self.core, (self.t1, self.t2, self.t3, self.t4, self.t5, self.t6, self.t7, self.t8, self.t9, self.t10, self.t11, self.t12, self.t13));
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13)>.Dispose(self.core);
        }
    }

    static void TryInvokeContinuationT3(WhenAllPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13> self, in LuminTaskAwaiter<T3> awaiter)
    {
        try
        {
            self.t3 = awaiter.GetResult();
        }
        catch (Exception ex)
        {
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13)>.TrySetException(self.core, ex);
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13)>.Dispose(self.core);
            return;
        }
                
        if (Interlocked.Increment(ref self.completedCount) == 13)
        {
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13)>.TrySetResult(self.core, (self.t1, self.t2, self.t3, self.t4, self.t5, self.t6, self.t7, self.t8, self.t9, self.t10, self.t11, self.t12, self.t13));
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13)>.Dispose(self.core);
        }
    }

    static void TryInvokeContinuationT4(WhenAllPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13> self, in LuminTaskAwaiter<T4> awaiter)
    {
        try
        {
            self.t4 = awaiter.GetResult();
        }
        catch (Exception ex)
        {
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13)>.TrySetException(self.core, ex);
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13)>.Dispose(self.core);
            return;
        }
                
        if (Interlocked.Increment(ref self.completedCount) == 13)
        {
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13)>.TrySetResult(self.core, (self.t1, self.t2, self.t3, self.t4, self.t5, self.t6, self.t7, self.t8, self.t9, self.t10, self.t11, self.t12, self.t13));
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13)>.Dispose(self.core);
        }
    }

    static void TryInvokeContinuationT5(WhenAllPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13> self, in LuminTaskAwaiter<T5> awaiter)
    {
        try
        {
            self.t5 = awaiter.GetResult();
        }
        catch (Exception ex)
        {
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13)>.TrySetException(self.core, ex);
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13)>.Dispose(self.core);
            return;
        }
                
        if (Interlocked.Increment(ref self.completedCount) == 13)
        {
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13)>.TrySetResult(self.core, (self.t1, self.t2, self.t3, self.t4, self.t5, self.t6, self.t7, self.t8, self.t9, self.t10, self.t11, self.t12, self.t13));
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13)>.Dispose(self.core);
        }
    }

    static void TryInvokeContinuationT6(WhenAllPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13> self, in LuminTaskAwaiter<T6> awaiter)
    {
        try
        {
            self.t6 = awaiter.GetResult();
        }
        catch (Exception ex)
        {
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13)>.TrySetException(self.core, ex);
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13)>.Dispose(self.core);
            return;
        }
                
        if (Interlocked.Increment(ref self.completedCount) == 13)
        {
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13)>.TrySetResult(self.core, (self.t1, self.t2, self.t3, self.t4, self.t5, self.t6, self.t7, self.t8, self.t9, self.t10, self.t11, self.t12, self.t13));
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13)>.Dispose(self.core);
        }
    }

    static void TryInvokeContinuationT7(WhenAllPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13> self, in LuminTaskAwaiter<T7> awaiter)
    {
        try
        {
            self.t7 = awaiter.GetResult();
        }
        catch (Exception ex)
        {
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13)>.TrySetException(self.core, ex);
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13)>.Dispose(self.core);
            return;
        }
                
        if (Interlocked.Increment(ref self.completedCount) == 13)
        {
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13)>.TrySetResult(self.core, (self.t1, self.t2, self.t3, self.t4, self.t5, self.t6, self.t7, self.t8, self.t9, self.t10, self.t11, self.t12, self.t13));
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13)>.Dispose(self.core);
        }
    }

    static void TryInvokeContinuationT8(WhenAllPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13> self, in LuminTaskAwaiter<T8> awaiter)
    {
        try
        {
            self.t8 = awaiter.GetResult();
        }
        catch (Exception ex)
        {
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13)>.TrySetException(self.core, ex);
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13)>.Dispose(self.core);
            return;
        }
                
        if (Interlocked.Increment(ref self.completedCount) == 13)
        {
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13)>.TrySetResult(self.core, (self.t1, self.t2, self.t3, self.t4, self.t5, self.t6, self.t7, self.t8, self.t9, self.t10, self.t11, self.t12, self.t13));
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13)>.Dispose(self.core);
        }
    }

    static void TryInvokeContinuationT9(WhenAllPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13> self, in LuminTaskAwaiter<T9> awaiter)
    {
        try
        {
            self.t9 = awaiter.GetResult();
        }
        catch (Exception ex)
        {
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13)>.TrySetException(self.core, ex);
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13)>.Dispose(self.core);
            return;
        }
                
        if (Interlocked.Increment(ref self.completedCount) == 13)
        {
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13)>.TrySetResult(self.core, (self.t1, self.t2, self.t3, self.t4, self.t5, self.t6, self.t7, self.t8, self.t9, self.t10, self.t11, self.t12, self.t13));
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13)>.Dispose(self.core);
        }
    }

    static void TryInvokeContinuationT10(WhenAllPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13> self, in LuminTaskAwaiter<T10> awaiter)
    {
        try
        {
            self.t10 = awaiter.GetResult();
        }
        catch (Exception ex)
        {
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13)>.TrySetException(self.core, ex);
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13)>.Dispose(self.core);
            return;
        }
                
        if (Interlocked.Increment(ref self.completedCount) == 13)
        {
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13)>.TrySetResult(self.core, (self.t1, self.t2, self.t3, self.t4, self.t5, self.t6, self.t7, self.t8, self.t9, self.t10, self.t11, self.t12, self.t13));
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13)>.Dispose(self.core);
        }
    }

    static void TryInvokeContinuationT11(WhenAllPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13> self, in LuminTaskAwaiter<T11> awaiter)
    {
        try
        {
            self.t11 = awaiter.GetResult();
        }
        catch (Exception ex)
        {
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13)>.TrySetException(self.core, ex);
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13)>.Dispose(self.core);
            return;
        }
                
        if (Interlocked.Increment(ref self.completedCount) == 13)
        {
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13)>.TrySetResult(self.core, (self.t1, self.t2, self.t3, self.t4, self.t5, self.t6, self.t7, self.t8, self.t9, self.t10, self.t11, self.t12, self.t13));
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13)>.Dispose(self.core);
        }
    }

    static void TryInvokeContinuationT12(WhenAllPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13> self, in LuminTaskAwaiter<T12> awaiter)
    {
        try
        {
            self.t12 = awaiter.GetResult();
        }
        catch (Exception ex)
        {
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13)>.TrySetException(self.core, ex);
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13)>.Dispose(self.core);
            return;
        }
                
        if (Interlocked.Increment(ref self.completedCount) == 13)
        {
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13)>.TrySetResult(self.core, (self.t1, self.t2, self.t3, self.t4, self.t5, self.t6, self.t7, self.t8, self.t9, self.t10, self.t11, self.t12, self.t13));
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13)>.Dispose(self.core);
        }
    }

    static void TryInvokeContinuationT13(WhenAllPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13> self, in LuminTaskAwaiter<T13> awaiter)
    {
        try
        {
            self.t13 = awaiter.GetResult();
        }
        catch (Exception ex)
        {
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13)>.TrySetException(self.core, ex);
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13)>.Dispose(self.core);
            return;
        }
                
        if (Interlocked.Increment(ref self.completedCount) == 13)
        {
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13)>.TrySetResult(self.core, (self.t1, self.t2, self.t3, self.t4, self.t5, self.t6, self.t7, self.t8, self.t9, self.t10, self.t11, self.t12, self.t13));
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13)>.Dispose(self.core);
        }
    }

}

public sealed unsafe class WhenAllPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>
{
    T1 t1 = default;
    T2 t2 = default;
    T3 t3 = default;
    T4 t4 = default;
    T5 t5 = default;
    T6 t6 = default;
    T7 t7 = default;
    T8 t8 = default;
    T9 t9 = default;
    T10 t10 = default;
    T11 t11 = default;
    T12 t12 = default;
    T13 t13 = default;
    T14 t14 = default;
    int completedCount;
    internal LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14)>* core;

    public static WhenAllPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14> Create(LuminTask<T1> task1, LuminTask<T2> task2, LuminTask<T3> task3, LuminTask<T4> task4, LuminTask<T5> task5, LuminTask<T6> task6, LuminTask<T7> task7, LuminTask<T8> task8, LuminTask<T9> task9, LuminTask<T10> task10, LuminTask<T11> task11, LuminTask<T12> task12, LuminTask<T13> task13, LuminTask<T14> task14)
    {
        return new WhenAllPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(task1, task2, task3, task4, task5, task6, task7, task8, task9, task10, task11, task12, task13, task14);
    }
    
    public WhenAllPromise(LuminTask<T1> task1, LuminTask<T2> task2, LuminTask<T3> task3, LuminTask<T4> task4, LuminTask<T5> task5, LuminTask<T6> task6, LuminTask<T7> task7, LuminTask<T8> task8, LuminTask<T9> task9, LuminTask<T10> task10, LuminTask<T11> task11, LuminTask<T12> task12, LuminTask<T13> task13, LuminTask<T14> task14)
    {
        core = LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14)>.Create();
        
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
                    var t = (Tuple<WhenAllPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>, LuminTaskAwaiter<T1>>)state;
                    TryInvokeContinuationT1(t.Item1, t.Item2);
                }, Tuple.Create(this, awaiter));
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
                    var t = (Tuple<WhenAllPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>, LuminTaskAwaiter<T2>>)state;
                    TryInvokeContinuationT2(t.Item1, t.Item2);
                }, Tuple.Create(this, awaiter));
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
                    var t = (Tuple<WhenAllPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>, LuminTaskAwaiter<T3>>)state;
                    TryInvokeContinuationT3(t.Item1, t.Item2);
                }, Tuple.Create(this, awaiter));
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
                    var t = (Tuple<WhenAllPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>, LuminTaskAwaiter<T4>>)state;
                    TryInvokeContinuationT4(t.Item1, t.Item2);
                }, Tuple.Create(this, awaiter));
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
                    var t = (Tuple<WhenAllPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>, LuminTaskAwaiter<T5>>)state;
                    TryInvokeContinuationT5(t.Item1, t.Item2);
                }, Tuple.Create(this, awaiter));
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
                    var t = (Tuple<WhenAllPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>, LuminTaskAwaiter<T6>>)state;
                    TryInvokeContinuationT6(t.Item1, t.Item2);
                }, Tuple.Create(this, awaiter));
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
                    var t = (Tuple<WhenAllPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>, LuminTaskAwaiter<T7>>)state;
                    TryInvokeContinuationT7(t.Item1, t.Item2);
                }, Tuple.Create(this, awaiter));
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
                    var t = (Tuple<WhenAllPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>, LuminTaskAwaiter<T8>>)state;
                    TryInvokeContinuationT8(t.Item1, t.Item2);
                }, Tuple.Create(this, awaiter));
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
                    var t = (Tuple<WhenAllPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>, LuminTaskAwaiter<T9>>)state;
                    TryInvokeContinuationT9(t.Item1, t.Item2);
                }, Tuple.Create(this, awaiter));
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
                    var t = (Tuple<WhenAllPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>, LuminTaskAwaiter<T10>>)state;
                    TryInvokeContinuationT10(t.Item1, t.Item2);
                }, Tuple.Create(this, awaiter));
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
                    var t = (Tuple<WhenAllPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>, LuminTaskAwaiter<T11>>)state;
                    TryInvokeContinuationT11(t.Item1, t.Item2);
                }, Tuple.Create(this, awaiter));
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
                    var t = (Tuple<WhenAllPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>, LuminTaskAwaiter<T12>>)state;
                    TryInvokeContinuationT12(t.Item1, t.Item2);
                }, Tuple.Create(this, awaiter));
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
                    var t = (Tuple<WhenAllPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>, LuminTaskAwaiter<T13>>)state;
                    TryInvokeContinuationT13(t.Item1, t.Item2);
                }, Tuple.Create(this, awaiter));
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
                    var t = (Tuple<WhenAllPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>, LuminTaskAwaiter<T14>>)state;
                    TryInvokeContinuationT14(t.Item1, t.Item2);
                }, Tuple.Create(this, awaiter));
            }
        }
    }

    static void TryInvokeContinuationT1(WhenAllPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14> self, in LuminTaskAwaiter<T1> awaiter)
    {
        try
        {
            self.t1 = awaiter.GetResult();
        }
        catch (Exception ex)
        {
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14)>.TrySetException(self.core, ex);
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14)>.Dispose(self.core);
            return;
        }
                
        if (Interlocked.Increment(ref self.completedCount) == 14)
        {
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14)>.TrySetResult(self.core, (self.t1, self.t2, self.t3, self.t4, self.t5, self.t6, self.t7, self.t8, self.t9, self.t10, self.t11, self.t12, self.t13, self.t14));
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14)>.Dispose(self.core);
        }
    }

    static void TryInvokeContinuationT2(WhenAllPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14> self, in LuminTaskAwaiter<T2> awaiter)
    {
        try
        {
            self.t2 = awaiter.GetResult();
        }
        catch (Exception ex)
        {
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14)>.TrySetException(self.core, ex);
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14)>.Dispose(self.core);
            return;
        }
                
        if (Interlocked.Increment(ref self.completedCount) == 14)
        {
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14)>.TrySetResult(self.core, (self.t1, self.t2, self.t3, self.t4, self.t5, self.t6, self.t7, self.t8, self.t9, self.t10, self.t11, self.t12, self.t13, self.t14));
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14)>.Dispose(self.core);
        }
    }

    static void TryInvokeContinuationT3(WhenAllPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14> self, in LuminTaskAwaiter<T3> awaiter)
    {
        try
        {
            self.t3 = awaiter.GetResult();
        }
        catch (Exception ex)
        {
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14)>.TrySetException(self.core, ex);
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14)>.Dispose(self.core);
            return;
        }
                
        if (Interlocked.Increment(ref self.completedCount) == 14)
        {
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14)>.TrySetResult(self.core, (self.t1, self.t2, self.t3, self.t4, self.t5, self.t6, self.t7, self.t8, self.t9, self.t10, self.t11, self.t12, self.t13, self.t14));
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14)>.Dispose(self.core);
        }
    }

    static void TryInvokeContinuationT4(WhenAllPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14> self, in LuminTaskAwaiter<T4> awaiter)
    {
        try
        {
            self.t4 = awaiter.GetResult();
        }
        catch (Exception ex)
        {
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14)>.TrySetException(self.core, ex);
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14)>.Dispose(self.core);
            return;
        }
                
        if (Interlocked.Increment(ref self.completedCount) == 14)
        {
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14)>.TrySetResult(self.core, (self.t1, self.t2, self.t3, self.t4, self.t5, self.t6, self.t7, self.t8, self.t9, self.t10, self.t11, self.t12, self.t13, self.t14));
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14)>.Dispose(self.core);
        }
    }

    static void TryInvokeContinuationT5(WhenAllPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14> self, in LuminTaskAwaiter<T5> awaiter)
    {
        try
        {
            self.t5 = awaiter.GetResult();
        }
        catch (Exception ex)
        {
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14)>.TrySetException(self.core, ex);
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14)>.Dispose(self.core);
            return;
        }
                
        if (Interlocked.Increment(ref self.completedCount) == 14)
        {
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14)>.TrySetResult(self.core, (self.t1, self.t2, self.t3, self.t4, self.t5, self.t6, self.t7, self.t8, self.t9, self.t10, self.t11, self.t12, self.t13, self.t14));
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14)>.Dispose(self.core);
        }
    }

    static void TryInvokeContinuationT6(WhenAllPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14> self, in LuminTaskAwaiter<T6> awaiter)
    {
        try
        {
            self.t6 = awaiter.GetResult();
        }
        catch (Exception ex)
        {
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14)>.TrySetException(self.core, ex);
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14)>.Dispose(self.core);
            return;
        }
                
        if (Interlocked.Increment(ref self.completedCount) == 14)
        {
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14)>.TrySetResult(self.core, (self.t1, self.t2, self.t3, self.t4, self.t5, self.t6, self.t7, self.t8, self.t9, self.t10, self.t11, self.t12, self.t13, self.t14));
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14)>.Dispose(self.core);
        }
    }

    static void TryInvokeContinuationT7(WhenAllPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14> self, in LuminTaskAwaiter<T7> awaiter)
    {
        try
        {
            self.t7 = awaiter.GetResult();
        }
        catch (Exception ex)
        {
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14)>.TrySetException(self.core, ex);
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14)>.Dispose(self.core);
            return;
        }
                
        if (Interlocked.Increment(ref self.completedCount) == 14)
        {
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14)>.TrySetResult(self.core, (self.t1, self.t2, self.t3, self.t4, self.t5, self.t6, self.t7, self.t8, self.t9, self.t10, self.t11, self.t12, self.t13, self.t14));
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14)>.Dispose(self.core);
        }
    }

    static void TryInvokeContinuationT8(WhenAllPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14> self, in LuminTaskAwaiter<T8> awaiter)
    {
        try
        {
            self.t8 = awaiter.GetResult();
        }
        catch (Exception ex)
        {
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14)>.TrySetException(self.core, ex);
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14)>.Dispose(self.core);
            return;
        }
                
        if (Interlocked.Increment(ref self.completedCount) == 14)
        {
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14)>.TrySetResult(self.core, (self.t1, self.t2, self.t3, self.t4, self.t5, self.t6, self.t7, self.t8, self.t9, self.t10, self.t11, self.t12, self.t13, self.t14));
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14)>.Dispose(self.core);
        }
    }

    static void TryInvokeContinuationT9(WhenAllPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14> self, in LuminTaskAwaiter<T9> awaiter)
    {
        try
        {
            self.t9 = awaiter.GetResult();
        }
        catch (Exception ex)
        {
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14)>.TrySetException(self.core, ex);
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14)>.Dispose(self.core);
            return;
        }
                
        if (Interlocked.Increment(ref self.completedCount) == 14)
        {
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14)>.TrySetResult(self.core, (self.t1, self.t2, self.t3, self.t4, self.t5, self.t6, self.t7, self.t8, self.t9, self.t10, self.t11, self.t12, self.t13, self.t14));
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14)>.Dispose(self.core);
        }
    }

    static void TryInvokeContinuationT10(WhenAllPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14> self, in LuminTaskAwaiter<T10> awaiter)
    {
        try
        {
            self.t10 = awaiter.GetResult();
        }
        catch (Exception ex)
        {
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14)>.TrySetException(self.core, ex);
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14)>.Dispose(self.core);
            return;
        }
                
        if (Interlocked.Increment(ref self.completedCount) == 14)
        {
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14)>.TrySetResult(self.core, (self.t1, self.t2, self.t3, self.t4, self.t5, self.t6, self.t7, self.t8, self.t9, self.t10, self.t11, self.t12, self.t13, self.t14));
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14)>.Dispose(self.core);
        }
    }

    static void TryInvokeContinuationT11(WhenAllPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14> self, in LuminTaskAwaiter<T11> awaiter)
    {
        try
        {
            self.t11 = awaiter.GetResult();
        }
        catch (Exception ex)
        {
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14)>.TrySetException(self.core, ex);
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14)>.Dispose(self.core);
            return;
        }
                
        if (Interlocked.Increment(ref self.completedCount) == 14)
        {
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14)>.TrySetResult(self.core, (self.t1, self.t2, self.t3, self.t4, self.t5, self.t6, self.t7, self.t8, self.t9, self.t10, self.t11, self.t12, self.t13, self.t14));
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14)>.Dispose(self.core);
        }
    }

    static void TryInvokeContinuationT12(WhenAllPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14> self, in LuminTaskAwaiter<T12> awaiter)
    {
        try
        {
            self.t12 = awaiter.GetResult();
        }
        catch (Exception ex)
        {
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14)>.TrySetException(self.core, ex);
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14)>.Dispose(self.core);
            return;
        }
                
        if (Interlocked.Increment(ref self.completedCount) == 14)
        {
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14)>.TrySetResult(self.core, (self.t1, self.t2, self.t3, self.t4, self.t5, self.t6, self.t7, self.t8, self.t9, self.t10, self.t11, self.t12, self.t13, self.t14));
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14)>.Dispose(self.core);
        }
    }

    static void TryInvokeContinuationT13(WhenAllPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14> self, in LuminTaskAwaiter<T13> awaiter)
    {
        try
        {
            self.t13 = awaiter.GetResult();
        }
        catch (Exception ex)
        {
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14)>.TrySetException(self.core, ex);
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14)>.Dispose(self.core);
            return;
        }
                
        if (Interlocked.Increment(ref self.completedCount) == 14)
        {
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14)>.TrySetResult(self.core, (self.t1, self.t2, self.t3, self.t4, self.t5, self.t6, self.t7, self.t8, self.t9, self.t10, self.t11, self.t12, self.t13, self.t14));
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14)>.Dispose(self.core);
        }
    }

    static void TryInvokeContinuationT14(WhenAllPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14> self, in LuminTaskAwaiter<T14> awaiter)
    {
        try
        {
            self.t14 = awaiter.GetResult();
        }
        catch (Exception ex)
        {
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14)>.TrySetException(self.core, ex);
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14)>.Dispose(self.core);
            return;
        }
                
        if (Interlocked.Increment(ref self.completedCount) == 14)
        {
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14)>.TrySetResult(self.core, (self.t1, self.t2, self.t3, self.t4, self.t5, self.t6, self.t7, self.t8, self.t9, self.t10, self.t11, self.t12, self.t13, self.t14));
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14)>.Dispose(self.core);
        }
    }

}

public sealed unsafe class WhenAllPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>
{
    T1 t1 = default;
    T2 t2 = default;
    T3 t3 = default;
    T4 t4 = default;
    T5 t5 = default;
    T6 t6 = default;
    T7 t7 = default;
    T8 t8 = default;
    T9 t9 = default;
    T10 t10 = default;
    T11 t11 = default;
    T12 t12 = default;
    T13 t13 = default;
    T14 t14 = default;
    T15 t15 = default;
    int completedCount;
    internal LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15)>* core;

    public static WhenAllPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15> Create(LuminTask<T1> task1, LuminTask<T2> task2, LuminTask<T3> task3, LuminTask<T4> task4, LuminTask<T5> task5, LuminTask<T6> task6, LuminTask<T7> task7, LuminTask<T8> task8, LuminTask<T9> task9, LuminTask<T10> task10, LuminTask<T11> task11, LuminTask<T12> task12, LuminTask<T13> task13, LuminTask<T14> task14, LuminTask<T15> task15)
    {
        return new WhenAllPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(task1, task2, task3, task4, task5, task6, task7, task8, task9, task10, task11, task12, task13, task14, task15);
    }
    
    public WhenAllPromise(LuminTask<T1> task1, LuminTask<T2> task2, LuminTask<T3> task3, LuminTask<T4> task4, LuminTask<T5> task5, LuminTask<T6> task6, LuminTask<T7> task7, LuminTask<T8> task8, LuminTask<T9> task9, LuminTask<T10> task10, LuminTask<T11> task11, LuminTask<T12> task12, LuminTask<T13> task13, LuminTask<T14> task14, LuminTask<T15> task15)
    {
        core = LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15)>.Create();
        
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
                    var t = (Tuple<WhenAllPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>, LuminTaskAwaiter<T1>>)state;
                    TryInvokeContinuationT1(t.Item1, t.Item2);
                }, Tuple.Create(this, awaiter));
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
                    var t = (Tuple<WhenAllPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>, LuminTaskAwaiter<T2>>)state;
                    TryInvokeContinuationT2(t.Item1, t.Item2);
                }, Tuple.Create(this, awaiter));
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
                    var t = (Tuple<WhenAllPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>, LuminTaskAwaiter<T3>>)state;
                    TryInvokeContinuationT3(t.Item1, t.Item2);
                }, Tuple.Create(this, awaiter));
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
                    var t = (Tuple<WhenAllPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>, LuminTaskAwaiter<T4>>)state;
                    TryInvokeContinuationT4(t.Item1, t.Item2);
                }, Tuple.Create(this, awaiter));
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
                    var t = (Tuple<WhenAllPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>, LuminTaskAwaiter<T5>>)state;
                    TryInvokeContinuationT5(t.Item1, t.Item2);
                }, Tuple.Create(this, awaiter));
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
                    var t = (Tuple<WhenAllPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>, LuminTaskAwaiter<T6>>)state;
                    TryInvokeContinuationT6(t.Item1, t.Item2);
                }, Tuple.Create(this, awaiter));
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
                    var t = (Tuple<WhenAllPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>, LuminTaskAwaiter<T7>>)state;
                    TryInvokeContinuationT7(t.Item1, t.Item2);
                }, Tuple.Create(this, awaiter));
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
                    var t = (Tuple<WhenAllPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>, LuminTaskAwaiter<T8>>)state;
                    TryInvokeContinuationT8(t.Item1, t.Item2);
                }, Tuple.Create(this, awaiter));
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
                    var t = (Tuple<WhenAllPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>, LuminTaskAwaiter<T9>>)state;
                    TryInvokeContinuationT9(t.Item1, t.Item2);
                }, Tuple.Create(this, awaiter));
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
                    var t = (Tuple<WhenAllPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>, LuminTaskAwaiter<T10>>)state;
                    TryInvokeContinuationT10(t.Item1, t.Item2);
                }, Tuple.Create(this, awaiter));
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
                    var t = (Tuple<WhenAllPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>, LuminTaskAwaiter<T11>>)state;
                    TryInvokeContinuationT11(t.Item1, t.Item2);
                }, Tuple.Create(this, awaiter));
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
                    var t = (Tuple<WhenAllPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>, LuminTaskAwaiter<T12>>)state;
                    TryInvokeContinuationT12(t.Item1, t.Item2);
                }, Tuple.Create(this, awaiter));
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
                    var t = (Tuple<WhenAllPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>, LuminTaskAwaiter<T13>>)state;
                    TryInvokeContinuationT13(t.Item1, t.Item2);
                }, Tuple.Create(this, awaiter));
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
                    var t = (Tuple<WhenAllPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>, LuminTaskAwaiter<T14>>)state;
                    TryInvokeContinuationT14(t.Item1, t.Item2);
                }, Tuple.Create(this, awaiter));
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
                    var t = (Tuple<WhenAllPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>, LuminTaskAwaiter<T15>>)state;
                    TryInvokeContinuationT15(t.Item1, t.Item2);
                }, Tuple.Create(this, awaiter));
            }
        }
    }

    static void TryInvokeContinuationT1(WhenAllPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15> self, in LuminTaskAwaiter<T1> awaiter)
    {
        try
        {
            self.t1 = awaiter.GetResult();
        }
        catch (Exception ex)
        {
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15)>.TrySetException(self.core, ex);
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15)>.Dispose(self.core);
            return;
        }
                
        if (Interlocked.Increment(ref self.completedCount) == 15)
        {
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15)>.TrySetResult(self.core, (self.t1, self.t2, self.t3, self.t4, self.t5, self.t6, self.t7, self.t8, self.t9, self.t10, self.t11, self.t12, self.t13, self.t14, self.t15));
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15)>.Dispose(self.core);
        }
    }

    static void TryInvokeContinuationT2(WhenAllPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15> self, in LuminTaskAwaiter<T2> awaiter)
    {
        try
        {
            self.t2 = awaiter.GetResult();
        }
        catch (Exception ex)
        {
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15)>.TrySetException(self.core, ex);
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15)>.Dispose(self.core);
            return;
        }
                
        if (Interlocked.Increment(ref self.completedCount) == 15)
        {
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15)>.TrySetResult(self.core, (self.t1, self.t2, self.t3, self.t4, self.t5, self.t6, self.t7, self.t8, self.t9, self.t10, self.t11, self.t12, self.t13, self.t14, self.t15));
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15)>.Dispose(self.core);
        }
    }

    static void TryInvokeContinuationT3(WhenAllPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15> self, in LuminTaskAwaiter<T3> awaiter)
    {
        try
        {
            self.t3 = awaiter.GetResult();
        }
        catch (Exception ex)
        {
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15)>.TrySetException(self.core, ex);
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15)>.Dispose(self.core);
            return;
        }
                
        if (Interlocked.Increment(ref self.completedCount) == 15)
        {
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15)>.TrySetResult(self.core, (self.t1, self.t2, self.t3, self.t4, self.t5, self.t6, self.t7, self.t8, self.t9, self.t10, self.t11, self.t12, self.t13, self.t14, self.t15));
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15)>.Dispose(self.core);
        }
    }

    static void TryInvokeContinuationT4(WhenAllPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15> self, in LuminTaskAwaiter<T4> awaiter)
    {
        try
        {
            self.t4 = awaiter.GetResult();
        }
        catch (Exception ex)
        {
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15)>.TrySetException(self.core, ex);
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15)>.Dispose(self.core);
            return;
        }
                
        if (Interlocked.Increment(ref self.completedCount) == 15)
        {
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15)>.TrySetResult(self.core, (self.t1, self.t2, self.t3, self.t4, self.t5, self.t6, self.t7, self.t8, self.t9, self.t10, self.t11, self.t12, self.t13, self.t14, self.t15));
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15)>.Dispose(self.core);
        }
    }

    static void TryInvokeContinuationT5(WhenAllPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15> self, in LuminTaskAwaiter<T5> awaiter)
    {
        try
        {
            self.t5 = awaiter.GetResult();
        }
        catch (Exception ex)
        {
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15)>.TrySetException(self.core, ex);
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15)>.Dispose(self.core);
            return;
        }
                
        if (Interlocked.Increment(ref self.completedCount) == 15)
        {
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15)>.TrySetResult(self.core, (self.t1, self.t2, self.t3, self.t4, self.t5, self.t6, self.t7, self.t8, self.t9, self.t10, self.t11, self.t12, self.t13, self.t14, self.t15));
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15)>.Dispose(self.core);
        }
    }

    static void TryInvokeContinuationT6(WhenAllPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15> self, in LuminTaskAwaiter<T6> awaiter)
    {
        try
        {
            self.t6 = awaiter.GetResult();
        }
        catch (Exception ex)
        {
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15)>.TrySetException(self.core, ex);
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15)>.Dispose(self.core);
            return;
        }
                
        if (Interlocked.Increment(ref self.completedCount) == 15)
        {
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15)>.TrySetResult(self.core, (self.t1, self.t2, self.t3, self.t4, self.t5, self.t6, self.t7, self.t8, self.t9, self.t10, self.t11, self.t12, self.t13, self.t14, self.t15));
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15)>.Dispose(self.core);
        }
    }

    static void TryInvokeContinuationT7(WhenAllPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15> self, in LuminTaskAwaiter<T7> awaiter)
    {
        try
        {
            self.t7 = awaiter.GetResult();
        }
        catch (Exception ex)
        {
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15)>.TrySetException(self.core, ex);
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15)>.Dispose(self.core);
            return;
        }
                
        if (Interlocked.Increment(ref self.completedCount) == 15)
        {
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15)>.TrySetResult(self.core, (self.t1, self.t2, self.t3, self.t4, self.t5, self.t6, self.t7, self.t8, self.t9, self.t10, self.t11, self.t12, self.t13, self.t14, self.t15));
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15)>.Dispose(self.core);
        }
    }

    static void TryInvokeContinuationT8(WhenAllPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15> self, in LuminTaskAwaiter<T8> awaiter)
    {
        try
        {
            self.t8 = awaiter.GetResult();
        }
        catch (Exception ex)
        {
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15)>.TrySetException(self.core, ex);
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15)>.Dispose(self.core);
            return;
        }
                
        if (Interlocked.Increment(ref self.completedCount) == 15)
        {
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15)>.TrySetResult(self.core, (self.t1, self.t2, self.t3, self.t4, self.t5, self.t6, self.t7, self.t8, self.t9, self.t10, self.t11, self.t12, self.t13, self.t14, self.t15));
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15)>.Dispose(self.core);
        }
    }

    static void TryInvokeContinuationT9(WhenAllPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15> self, in LuminTaskAwaiter<T9> awaiter)
    {
        try
        {
            self.t9 = awaiter.GetResult();
        }
        catch (Exception ex)
        {
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15)>.TrySetException(self.core, ex);
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15)>.Dispose(self.core);
            return;
        }
                
        if (Interlocked.Increment(ref self.completedCount) == 15)
        {
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15)>.TrySetResult(self.core, (self.t1, self.t2, self.t3, self.t4, self.t5, self.t6, self.t7, self.t8, self.t9, self.t10, self.t11, self.t12, self.t13, self.t14, self.t15));
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15)>.Dispose(self.core);
        }
    }

    static void TryInvokeContinuationT10(WhenAllPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15> self, in LuminTaskAwaiter<T10> awaiter)
    {
        try
        {
            self.t10 = awaiter.GetResult();
        }
        catch (Exception ex)
        {
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15)>.TrySetException(self.core, ex);
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15)>.Dispose(self.core);
            return;
        }
                
        if (Interlocked.Increment(ref self.completedCount) == 15)
        {
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15)>.TrySetResult(self.core, (self.t1, self.t2, self.t3, self.t4, self.t5, self.t6, self.t7, self.t8, self.t9, self.t10, self.t11, self.t12, self.t13, self.t14, self.t15));
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15)>.Dispose(self.core);
        }
    }

    static void TryInvokeContinuationT11(WhenAllPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15> self, in LuminTaskAwaiter<T11> awaiter)
    {
        try
        {
            self.t11 = awaiter.GetResult();
        }
        catch (Exception ex)
        {
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15)>.TrySetException(self.core, ex);
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15)>.Dispose(self.core);
            return;
        }
                
        if (Interlocked.Increment(ref self.completedCount) == 15)
        {
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15)>.TrySetResult(self.core, (self.t1, self.t2, self.t3, self.t4, self.t5, self.t6, self.t7, self.t8, self.t9, self.t10, self.t11, self.t12, self.t13, self.t14, self.t15));
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15)>.Dispose(self.core);
        }
    }

    static void TryInvokeContinuationT12(WhenAllPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15> self, in LuminTaskAwaiter<T12> awaiter)
    {
        try
        {
            self.t12 = awaiter.GetResult();
        }
        catch (Exception ex)
        {
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15)>.TrySetException(self.core, ex);
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15)>.Dispose(self.core);
            return;
        }
                
        if (Interlocked.Increment(ref self.completedCount) == 15)
        {
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15)>.TrySetResult(self.core, (self.t1, self.t2, self.t3, self.t4, self.t5, self.t6, self.t7, self.t8, self.t9, self.t10, self.t11, self.t12, self.t13, self.t14, self.t15));
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15)>.Dispose(self.core);
        }
    }

    static void TryInvokeContinuationT13(WhenAllPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15> self, in LuminTaskAwaiter<T13> awaiter)
    {
        try
        {
            self.t13 = awaiter.GetResult();
        }
        catch (Exception ex)
        {
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15)>.TrySetException(self.core, ex);
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15)>.Dispose(self.core);
            return;
        }
                
        if (Interlocked.Increment(ref self.completedCount) == 15)
        {
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15)>.TrySetResult(self.core, (self.t1, self.t2, self.t3, self.t4, self.t5, self.t6, self.t7, self.t8, self.t9, self.t10, self.t11, self.t12, self.t13, self.t14, self.t15));
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15)>.Dispose(self.core);
        }
    }

    static void TryInvokeContinuationT14(WhenAllPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15> self, in LuminTaskAwaiter<T14> awaiter)
    {
        try
        {
            self.t14 = awaiter.GetResult();
        }
        catch (Exception ex)
        {
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15)>.TrySetException(self.core, ex);
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15)>.Dispose(self.core);
            return;
        }
                
        if (Interlocked.Increment(ref self.completedCount) == 15)
        {
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15)>.TrySetResult(self.core, (self.t1, self.t2, self.t3, self.t4, self.t5, self.t6, self.t7, self.t8, self.t9, self.t10, self.t11, self.t12, self.t13, self.t14, self.t15));
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15)>.Dispose(self.core);
        }
    }

    static void TryInvokeContinuationT15(WhenAllPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15> self, in LuminTaskAwaiter<T15> awaiter)
    {
        try
        {
            self.t15 = awaiter.GetResult();
        }
        catch (Exception ex)
        {
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15)>.TrySetException(self.core, ex);
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15)>.Dispose(self.core);
            return;
        }
                
        if (Interlocked.Increment(ref self.completedCount) == 15)
        {
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15)>.TrySetResult(self.core, (self.t1, self.t2, self.t3, self.t4, self.t5, self.t6, self.t7, self.t8, self.t9, self.t10, self.t11, self.t12, self.t13, self.t14, self.t15));
            LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15)>.Dispose(self.core);
        }
    }

}


public sealed unsafe class WhenAllPromise<T>
{
    private T[] _results;
    private int _completedCount;
    private int _totalCount;
    internal LuminTaskSourceCore<T[]>* _core;

    public static WhenAllPromise<T> Create(params LuminTask<T>[] tasks)
    {
        return new WhenAllPromise<T>(tasks);
    }
    
    public static WhenAllPromise<T> Create(IEnumerable<LuminTask<T>> tasks)
    {
        return new WhenAllPromise<T>(tasks.ToArray());
    }
    
    public WhenAllPromise(LuminTask<T>[]? tasks)
    {
        if (tasks == null || tasks.Length == 0)
        {
            _results = Array.Empty<T>();
            _core = LuminTaskSourceCore<T[]>.Create();
            LuminTaskSourceCore<T[]>.TrySetResult(_core, _results);
            return;
        }

        _totalCount = tasks.Length;
        _results = new T[_totalCount];
        _core = LuminTaskSourceCore<T[]>.Create();
        _completedCount = 0;

        for (int i = 0; i < tasks.Length; i++)
        {
            var index = i;
            var awaiter = tasks[i].GetAwaiter();
            
            if (awaiter.IsCompleted)
            {
                TryInvokeContinuation(this, awaiter, index);
            }
            else
            {
                awaiter.SourceOnCompleted(static state =>
                {
                    var tuple = (Tuple<WhenAllPromise<T>, LuminTaskAwaiter<T>, int>)state;
                    TryInvokeContinuation(tuple.Item1, tuple.Item2, tuple.Item3);
                }, Tuple.Create(this, awaiter, index));
            }
        }
    }

    static void TryInvokeContinuation(WhenAllPromise<T> self, in LuminTaskAwaiter<T> awaiter, int index)
    {
        try
        {
            self._results[index] = awaiter.GetResult();
        }
        catch (Exception ex)
        {
            LuminTaskSourceCore<T[]>.TrySetException(self._core, ex);
            LuminTaskSourceCore<T[]>.Dispose(self._core);
            return;
        }
                
        if (Interlocked.Increment(ref self._completedCount) == self._totalCount)
        {
            LuminTaskSourceCore<T[]>.TrySetResult(self._core, self._results);
            LuminTaskSourceCore<T[]>.Dispose(self._core);
        }
    }
}

public sealed unsafe class WhenAllPromise
{
    private int _completedCount;
    private int _totalCount;
    internal LuminTaskSourceCore<bool>* _core;

    public static WhenAllPromise Create(params LuminTask[] tasks)
    {
        return new WhenAllPromise(tasks);
    }
    
    public static WhenAllPromise Create(IEnumerable<LuminTask> tasks)
    {
        return new WhenAllPromise(tasks.ToArray());
    }
    
    public WhenAllPromise(LuminTask[]? tasks)
    {
        if (tasks == null || tasks.Length == 0)
        {
            _core = LuminTaskSourceCore<bool>.Create();
            LuminTaskSourceCore<bool>.TrySetResult(_core, true);
            return;
        }

        _totalCount = tasks.Length;
        _core = LuminTaskSourceCore<bool>.Create();
        _completedCount = 0;

        for (int i = 0; i < tasks.Length; i++)
        {
            var awaiter = tasks[i].GetAwaiter();
            
            if (awaiter.IsCompleted)
            {
                TryInvokeContinuation(this, awaiter);
            }
            else
            {
                awaiter.SourceOnCompleted(static state =>
                {
                    var tuple = (Tuple<WhenAllPromise, LuminTaskAwaiter>)state;
                    TryInvokeContinuation(tuple.Item1, tuple.Item2);
                }, Tuple.Create(this, awaiter));
            }
        }
    }

    static void TryInvokeContinuation(WhenAllPromise self, in LuminTaskAwaiter awaiter)
    {
        try
        {
            awaiter.GetResult();
        }
        catch (Exception ex)
        {
            LuminTaskSourceCore<bool>.TrySetException(self._core, ex);
            LuminTaskSourceCore<bool>.Dispose(self._core);
            return;
        }
                
        if (Interlocked.Increment(ref self._completedCount) == self._totalCount)
        {
            LuminTaskSourceCore<bool>.TrySetResult(self._core, true);
            LuminTaskSourceCore<bool>.Dispose(self._core);
        }
    }
}