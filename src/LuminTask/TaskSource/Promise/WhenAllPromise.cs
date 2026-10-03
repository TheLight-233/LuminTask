
using System;
using System.Threading;
using System.Collections.Generic;
using System.Linq;
using LuminThread.Utility;

namespace LuminThread.TaskSource.Promise;

internal sealed class WhenAllPromise<T1, T2> : CombinerNode
{
    T1 t1 = default!;
    T2 t2 = default!;
    private LuminTask<T1> _task1;
    private LuminTask<T2> _task2;

    public static WhenAllPromise<T1, T2> Create(LuminTask<T1> task1, LuminTask<T2> task2)
    {
        return new WhenAllPromise<T1, T2>(task1, task2);
    }

    public WhenAllPromise(LuminTask<T1> task1, LuminTask<T2> task2)
    {
        {
            _task1 = task1;
            if (task1.IsCompleted)
            {
                TryInvokeContinuationT1(this);
            }
            else
            {
                task1.SourceOnCompleted(static state => TryInvokeContinuationT1((WhenAllPromise<T1, T2>)state), this);
            }
        }
        {
            _task2 = task2;
            if (task2.IsCompleted)
            {
                TryInvokeContinuationT2(this);
            }
            else
            {
                task2.SourceOnCompleted(static state => TryInvokeContinuationT2((WhenAllPromise<T1, T2>)state), this);
            }
        }
    }

    static void TryInvokeContinuationT1(WhenAllPromise<T1, T2> self)
    {
        try
        {
            self.t1 = self._task1.GetResult();
        }
        catch (Exception ex)
        {
            self.Fault(ex);
            return;
        }

        if (self.IncrementCompleted(2))
        {
            var result = (self.t1, self.t2);
            self.Complete(in result);
        }
    }

    static void TryInvokeContinuationT2(WhenAllPromise<T1, T2> self)
    {
        try
        {
            self.t2 = self._task2.GetResult();
        }
        catch (Exception ex)
        {
            self.Fault(ex);
            return;
        }

        if (self.IncrementCompleted(2))
        {
            var result = (self.t1, self.t2);
            self.Complete(in result);
        }
    }

}

internal sealed class WhenAllPromise<T1, T2, T3> : CombinerNode
{
    T1 t1 = default!;
    T2 t2 = default!;
    T3 t3 = default!;
    private LuminTask<T1> _task1;
    private LuminTask<T2> _task2;
    private LuminTask<T3> _task3;

    public static WhenAllPromise<T1, T2, T3> Create(LuminTask<T1> task1, LuminTask<T2> task2, LuminTask<T3> task3)
    {
        return new WhenAllPromise<T1, T2, T3>(task1, task2, task3);
    }

    public WhenAllPromise(LuminTask<T1> task1, LuminTask<T2> task2, LuminTask<T3> task3)
    {
        {
            _task1 = task1;
            if (task1.IsCompleted)
            {
                TryInvokeContinuationT1(this);
            }
            else
            {
                task1.SourceOnCompleted(static state => TryInvokeContinuationT1((WhenAllPromise<T1, T2, T3>)state), this);
            }
        }
        {
            _task2 = task2;
            if (task2.IsCompleted)
            {
                TryInvokeContinuationT2(this);
            }
            else
            {
                task2.SourceOnCompleted(static state => TryInvokeContinuationT2((WhenAllPromise<T1, T2, T3>)state), this);
            }
        }
        {
            _task3 = task3;
            if (task3.IsCompleted)
            {
                TryInvokeContinuationT3(this);
            }
            else
            {
                task3.SourceOnCompleted(static state => TryInvokeContinuationT3((WhenAllPromise<T1, T2, T3>)state), this);
            }
        }
    }

    static void TryInvokeContinuationT1(WhenAllPromise<T1, T2, T3> self)
    {
        try
        {
            self.t1 = self._task1.GetResult();
        }
        catch (Exception ex)
        {
            self.Fault(ex);
            return;
        }

        if (self.IncrementCompleted(3))
        {
            var result = (self.t1, self.t2, self.t3);
            self.Complete(in result);
        }
    }

    static void TryInvokeContinuationT2(WhenAllPromise<T1, T2, T3> self)
    {
        try
        {
            self.t2 = self._task2.GetResult();
        }
        catch (Exception ex)
        {
            self.Fault(ex);
            return;
        }

        if (self.IncrementCompleted(3))
        {
            var result = (self.t1, self.t2, self.t3);
            self.Complete(in result);
        }
    }

    static void TryInvokeContinuationT3(WhenAllPromise<T1, T2, T3> self)
    {
        try
        {
            self.t3 = self._task3.GetResult();
        }
        catch (Exception ex)
        {
            self.Fault(ex);
            return;
        }

        if (self.IncrementCompleted(3))
        {
            var result = (self.t1, self.t2, self.t3);
            self.Complete(in result);
        }
    }

}

internal sealed class WhenAllPromise<T1, T2, T3, T4> : CombinerNode
{
    T1 t1 = default!;
    T2 t2 = default!;
    T3 t3 = default!;
    T4 t4 = default!;
    private LuminTask<T1> _task1;
    private LuminTask<T2> _task2;
    private LuminTask<T3> _task3;
    private LuminTask<T4> _task4;

    public static WhenAllPromise<T1, T2, T3, T4> Create(LuminTask<T1> task1, LuminTask<T2> task2, LuminTask<T3> task3, LuminTask<T4> task4)
    {
        return new WhenAllPromise<T1, T2, T3, T4>(task1, task2, task3, task4);
    }

    public WhenAllPromise(LuminTask<T1> task1, LuminTask<T2> task2, LuminTask<T3> task3, LuminTask<T4> task4)
    {
        {
            _task1 = task1;
            if (task1.IsCompleted)
            {
                TryInvokeContinuationT1(this);
            }
            else
            {
                task1.SourceOnCompleted(static state => TryInvokeContinuationT1((WhenAllPromise<T1, T2, T3, T4>)state), this);
            }
        }
        {
            _task2 = task2;
            if (task2.IsCompleted)
            {
                TryInvokeContinuationT2(this);
            }
            else
            {
                task2.SourceOnCompleted(static state => TryInvokeContinuationT2((WhenAllPromise<T1, T2, T3, T4>)state), this);
            }
        }
        {
            _task3 = task3;
            if (task3.IsCompleted)
            {
                TryInvokeContinuationT3(this);
            }
            else
            {
                task3.SourceOnCompleted(static state => TryInvokeContinuationT3((WhenAllPromise<T1, T2, T3, T4>)state), this);
            }
        }
        {
            _task4 = task4;
            if (task4.IsCompleted)
            {
                TryInvokeContinuationT4(this);
            }
            else
            {
                task4.SourceOnCompleted(static state => TryInvokeContinuationT4((WhenAllPromise<T1, T2, T3, T4>)state), this);
            }
        }
    }

    static void TryInvokeContinuationT1(WhenAllPromise<T1, T2, T3, T4> self)
    {
        try
        {
            self.t1 = self._task1.GetResult();
        }
        catch (Exception ex)
        {
            self.Fault(ex);
            return;
        }

        if (self.IncrementCompleted(4))
        {
            var result = (self.t1, self.t2, self.t3, self.t4);
            self.Complete(in result);
        }
    }

    static void TryInvokeContinuationT2(WhenAllPromise<T1, T2, T3, T4> self)
    {
        try
        {
            self.t2 = self._task2.GetResult();
        }
        catch (Exception ex)
        {
            self.Fault(ex);
            return;
        }

        if (self.IncrementCompleted(4))
        {
            var result = (self.t1, self.t2, self.t3, self.t4);
            self.Complete(in result);
        }
    }

    static void TryInvokeContinuationT3(WhenAllPromise<T1, T2, T3, T4> self)
    {
        try
        {
            self.t3 = self._task3.GetResult();
        }
        catch (Exception ex)
        {
            self.Fault(ex);
            return;
        }

        if (self.IncrementCompleted(4))
        {
            var result = (self.t1, self.t2, self.t3, self.t4);
            self.Complete(in result);
        }
    }

    static void TryInvokeContinuationT4(WhenAllPromise<T1, T2, T3, T4> self)
    {
        try
        {
            self.t4 = self._task4.GetResult();
        }
        catch (Exception ex)
        {
            self.Fault(ex);
            return;
        }

        if (self.IncrementCompleted(4))
        {
            var result = (self.t1, self.t2, self.t3, self.t4);
            self.Complete(in result);
        }
    }

}

internal sealed class WhenAllPromise1 : CombinerNode
{
    private int _totalCount = 1;
    private LuminTask _task1;

    public WhenAllPromise1(LuminTask task1)
    {
        {
            _task1 = task1;
            if (task1.IsCompleted)
            {
                TryInvokeContinuationT1(this);
            }
            else
            {
                task1.SourceOnCompleted(static state => TryInvokeContinuationT1((WhenAllPromise1)state), this);
            }
        }
    }

    static void TryInvokeContinuationT1(WhenAllPromise1 self)
    {
        try
        {
            self._task1.GetResult();
        }
        catch (Exception ex)
        {
            self.Fault(ex);
            return;
        }

        if (self.IncrementCompleted(self._totalCount))
        {
            self.Complete();
        }
    }
}

internal sealed class WhenAllPromise2 : CombinerNode
{
    private int _totalCount = 2;
    private LuminTask _task1;
    private LuminTask _task2;

    public WhenAllPromise2(LuminTask task1, LuminTask task2)
    {
        {
            _task1 = task1;
            if (task1.IsCompleted)
            {
                TryInvokeContinuationT1(this);
            }
            else
            {
                task1.SourceOnCompleted(static state => TryInvokeContinuationT1((WhenAllPromise2)state), this);
            }
        }
        {
            _task2 = task2;
            if (task2.IsCompleted)
            {
                TryInvokeContinuationT2(this);
            }
            else
            {
                task2.SourceOnCompleted(static state => TryInvokeContinuationT2((WhenAllPromise2)state), this);
            }
        }
    }

    static void TryInvokeContinuationT1(WhenAllPromise2 self)
    {
        try
        {
            self._task1.GetResult();
        }
        catch (Exception ex)
        {
            self.Fault(ex);
            return;
        }

        if (self.IncrementCompleted(self._totalCount))
        {
            self.Complete();
        }
    }
    static void TryInvokeContinuationT2(WhenAllPromise2 self)
    {
        try
        {
            self._task2.GetResult();
        }
        catch (Exception ex)
        {
            self.Fault(ex);
            return;
        }

        if (self.IncrementCompleted(self._totalCount))
        {
            self.Complete();
        }
    }
}

internal sealed class WhenAllPromise3 : CombinerNode
{
    private int _totalCount = 3;
    private LuminTask _task1;
    private LuminTask _task2;
    private LuminTask _task3;

    public WhenAllPromise3(LuminTask task1, LuminTask task2, LuminTask task3)
    {
        {
            _task1 = task1;
            if (task1.IsCompleted)
            {
                TryInvokeContinuationT1(this);
            }
            else
            {
                task1.SourceOnCompleted(static state => TryInvokeContinuationT1((WhenAllPromise3)state), this);
            }
        }
        {
            _task2 = task2;
            if (task2.IsCompleted)
            {
                TryInvokeContinuationT2(this);
            }
            else
            {
                task2.SourceOnCompleted(static state => TryInvokeContinuationT2((WhenAllPromise3)state), this);
            }
        }
        {
            _task3 = task3;
            if (task3.IsCompleted)
            {
                TryInvokeContinuationT3(this);
            }
            else
            {
                task3.SourceOnCompleted(static state => TryInvokeContinuationT3((WhenAllPromise3)state), this);
            }
        }
    }

    static void TryInvokeContinuationT1(WhenAllPromise3 self)
    {
        try
        {
            self._task1.GetResult();
        }
        catch (Exception ex)
        {
            self.Fault(ex);
            return;
        }

        if (self.IncrementCompleted(self._totalCount))
        {
            self.Complete();
        }
    }
    static void TryInvokeContinuationT2(WhenAllPromise3 self)
    {
        try
        {
            self._task2.GetResult();
        }
        catch (Exception ex)
        {
            self.Fault(ex);
            return;
        }

        if (self.IncrementCompleted(self._totalCount))
        {
            self.Complete();
        }
    }
    static void TryInvokeContinuationT3(WhenAllPromise3 self)
    {
        try
        {
            self._task3.GetResult();
        }
        catch (Exception ex)
        {
            self.Fault(ex);
            return;
        }

        if (self.IncrementCompleted(self._totalCount))
        {
            self.Complete();
        }
    }
}

internal sealed class WhenAllPromise4 : CombinerNode
{
    private int _totalCount = 4;
    private LuminTask _task1;
    private LuminTask _task2;
    private LuminTask _task3;
    private LuminTask _task4;

    public WhenAllPromise4(LuminTask task1, LuminTask task2, LuminTask task3, LuminTask task4)
    {
        {
            _task1 = task1;
            if (task1.IsCompleted)
            {
                TryInvokeContinuationT1(this);
            }
            else
            {
                task1.SourceOnCompleted(static state => TryInvokeContinuationT1((WhenAllPromise4)state), this);
            }
        }
        {
            _task2 = task2;
            if (task2.IsCompleted)
            {
                TryInvokeContinuationT2(this);
            }
            else
            {
                task2.SourceOnCompleted(static state => TryInvokeContinuationT2((WhenAllPromise4)state), this);
            }
        }
        {
            _task3 = task3;
            if (task3.IsCompleted)
            {
                TryInvokeContinuationT3(this);
            }
            else
            {
                task3.SourceOnCompleted(static state => TryInvokeContinuationT3((WhenAllPromise4)state), this);
            }
        }
        {
            _task4 = task4;
            if (task4.IsCompleted)
            {
                TryInvokeContinuationT4(this);
            }
            else
            {
                task4.SourceOnCompleted(static state => TryInvokeContinuationT4((WhenAllPromise4)state), this);
            }
        }
    }

    static void TryInvokeContinuationT1(WhenAllPromise4 self)
    {
        try
        {
            self._task1.GetResult();
        }
        catch (Exception ex)
        {
            self.Fault(ex);
            return;
        }

        if (self.IncrementCompleted(self._totalCount))
        {
            self.Complete();
        }
    }
    static void TryInvokeContinuationT2(WhenAllPromise4 self)
    {
        try
        {
            self._task2.GetResult();
        }
        catch (Exception ex)
        {
            self.Fault(ex);
            return;
        }

        if (self.IncrementCompleted(self._totalCount))
        {
            self.Complete();
        }
    }
    static void TryInvokeContinuationT3(WhenAllPromise4 self)
    {
        try
        {
            self._task3.GetResult();
        }
        catch (Exception ex)
        {
            self.Fault(ex);
            return;
        }

        if (self.IncrementCompleted(self._totalCount))
        {
            self.Complete();
        }
    }
    static void TryInvokeContinuationT4(WhenAllPromise4 self)
    {
        try
        {
            self._task4.GetResult();
        }
        catch (Exception ex)
        {
            self.Fault(ex);
            return;
        }

        if (self.IncrementCompleted(self._totalCount))
        {
            self.Complete();
        }
    }
}

internal sealed class WhenAllPromise<T> : CombinerNode
{
    private T[] _results;
    private int _totalCount;

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
            Complete(in _results);
            return;
        }

        _totalCount = tasks.Length;
        _results = new T[_totalCount];

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
                    var tuple = (StateTuple<(WhenAllPromise<T>, LuminTask<T>, int)>)state;
                    var owner = tuple.Item.Item1;
                    var awaiter = tuple.Item.Item2;
                    var index = tuple.Item.Item3;
                    tuple.Dispose();
                    TryInvokeContinuation(owner, awaiter, index);
                }, StateTuple.Create((this, awaiter, index)));
            }
        }
    }

    static void TryInvokeContinuation(WhenAllPromise<T> self, in LuminTask<T> awaiter, int index)
    {
        try
        {
            self._results[index] = awaiter.GetResult();
        }
        catch (Exception ex)
        {
            self.Fault(ex);
            return;
        }

        if (self.IncrementCompleted(self._totalCount))
        {
            self.Complete(in self._results);
        }
    }
}

internal sealed class WhenAllPromise : CombinerNode
{
    private int _totalCount;

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
            Complete();
            return;
        }

        _totalCount = tasks.Length;

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
                    var tuple = (StateTuple<(WhenAllPromise, LuminTask)>)state;
                    var owner = tuple.Item.Item1;
                    var awaiter = tuple.Item.Item2;
                    tuple.Dispose();
                    TryInvokeContinuation(owner, awaiter);
                }, StateTuple.Create(this, awaiter));
            }
        }
    }

    static void TryInvokeContinuation(WhenAllPromise self, in LuminTask awaiter)
    {
        try
        {
            awaiter.GetResult();
        }
        catch (Exception ex)
        {
            self.Fault(ex);
            return;
        }

        if (self.IncrementCompleted(self._totalCount))
        {
            self.Complete();
        }
    }
}
