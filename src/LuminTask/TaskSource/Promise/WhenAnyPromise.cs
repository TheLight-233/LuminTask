
using System;
using System.Threading;
using System.Collections.Generic;
using System.Linq;
using LuminThread.Utility;

namespace LuminThread.TaskSource.Promise;

internal sealed class WhenAnyPromise<T1, T2> : CombinerNode
{
    T1 t1 = default!;
    T2 t2 = default!;
    private LuminTask<T1> _task1;
    private LuminTask<T2> _task2;

    public static WhenAnyPromise<T1, T2> Create(LuminTask<T1> task1, LuminTask<T2> task2)
    {
        return new WhenAnyPromise<T1, T2>(task1, task2);
    }

    public WhenAnyPromise(LuminTask<T1> task1, LuminTask<T2> task2)
    {
        {
            _task1 = task1;
            if (task1.IsCompleted)
            {
                TryInvokeContinuationT1(this);
            }
            else
            {
                task1.SourceOnCompleted(static state => TryInvokeContinuationT1((WhenAnyPromise<T1, T2>)state), this);
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
                task2.SourceOnCompleted(static state => TryInvokeContinuationT2((WhenAnyPromise<T1, T2>)state), this);
            }
        }
    }

    static void TryInvokeContinuationT1(WhenAnyPromise<T1, T2> self)
    {
        if (!self.IncrementCompleted(1))
        {
            LuminTaskHandle loser = self._task1._handle;
            if (!loser.IsNull())
                LuminTaskCoreSource.Forget(loser);
            return;
        }

        try
        {
            self.t1 = self._task1.GetResult();
        }
        catch (Exception ex)
        {
            self.Fault(ex);
            return;
        }

        var result = (0, self.t1, self.t2);
        self.Complete(in result);
    }

    static void TryInvokeContinuationT2(WhenAnyPromise<T1, T2> self)
    {
        if (!self.IncrementCompleted(1))
        {
            LuminTaskHandle loser = self._task2._handle;
            if (!loser.IsNull())
                LuminTaskCoreSource.Forget(loser);
            return;
        }

        try
        {
            self.t2 = self._task2.GetResult();
        }
        catch (Exception ex)
        {
            self.Fault(ex);
            return;
        }

        var result = (1, self.t1, self.t2);
        self.Complete(in result);
    }

}

internal sealed class WhenAnyPromise<T1, T2, T3> : CombinerNode
{
    T1 t1 = default!;
    T2 t2 = default!;
    T3 t3 = default!;
    private LuminTask<T1> _task1;
    private LuminTask<T2> _task2;
    private LuminTask<T3> _task3;

    public static WhenAnyPromise<T1, T2, T3> Create(LuminTask<T1> task1, LuminTask<T2> task2, LuminTask<T3> task3)
    {
        return new WhenAnyPromise<T1, T2, T3>(task1, task2, task3);
    }

    public WhenAnyPromise(LuminTask<T1> task1, LuminTask<T2> task2, LuminTask<T3> task3)
    {
        {
            _task1 = task1;
            if (task1.IsCompleted)
            {
                TryInvokeContinuationT1(this);
            }
            else
            {
                task1.SourceOnCompleted(static state => TryInvokeContinuationT1((WhenAnyPromise<T1, T2, T3>)state), this);
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
                task2.SourceOnCompleted(static state => TryInvokeContinuationT2((WhenAnyPromise<T1, T2, T3>)state), this);
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
                task3.SourceOnCompleted(static state => TryInvokeContinuationT3((WhenAnyPromise<T1, T2, T3>)state), this);
            }
        }
    }

    static void TryInvokeContinuationT1(WhenAnyPromise<T1, T2, T3> self)
    {
        if (!self.IncrementCompleted(1))
        {
            LuminTaskHandle loser = self._task1._handle;
            if (!loser.IsNull())
                LuminTaskCoreSource.Forget(loser);
            return;
        }

        try
        {
            self.t1 = self._task1.GetResult();
        }
        catch (Exception ex)
        {
            self.Fault(ex);
            return;
        }

        var result = (0, self.t1, self.t2, self.t3);
        self.Complete(in result);
    }

    static void TryInvokeContinuationT2(WhenAnyPromise<T1, T2, T3> self)
    {
        if (!self.IncrementCompleted(1))
        {
            LuminTaskHandle loser = self._task2._handle;
            if (!loser.IsNull())
                LuminTaskCoreSource.Forget(loser);
            return;
        }

        try
        {
            self.t2 = self._task2.GetResult();
        }
        catch (Exception ex)
        {
            self.Fault(ex);
            return;
        }

        var result = (1, self.t1, self.t2, self.t3);
        self.Complete(in result);
    }

    static void TryInvokeContinuationT3(WhenAnyPromise<T1, T2, T3> self)
    {
        if (!self.IncrementCompleted(1))
        {
            LuminTaskHandle loser = self._task3._handle;
            if (!loser.IsNull())
                LuminTaskCoreSource.Forget(loser);
            return;
        }

        try
        {
            self.t3 = self._task3.GetResult();
        }
        catch (Exception ex)
        {
            self.Fault(ex);
            return;
        }

        var result = (2, self.t1, self.t2, self.t3);
        self.Complete(in result);
    }

}

internal sealed class WhenAnyPromise<T1, T2, T3, T4> : CombinerNode
{
    T1 t1 = default!;
    T2 t2 = default!;
    T3 t3 = default!;
    T4 t4 = default!;
    private LuminTask<T1> _task1;
    private LuminTask<T2> _task2;
    private LuminTask<T3> _task3;
    private LuminTask<T4> _task4;

    public static WhenAnyPromise<T1, T2, T3, T4> Create(LuminTask<T1> task1, LuminTask<T2> task2, LuminTask<T3> task3, LuminTask<T4> task4)
    {
        return new WhenAnyPromise<T1, T2, T3, T4>(task1, task2, task3, task4);
    }

    public WhenAnyPromise(LuminTask<T1> task1, LuminTask<T2> task2, LuminTask<T3> task3, LuminTask<T4> task4)
    {
        {
            _task1 = task1;
            if (task1.IsCompleted)
            {
                TryInvokeContinuationT1(this);
            }
            else
            {
                task1.SourceOnCompleted(static state => TryInvokeContinuationT1((WhenAnyPromise<T1, T2, T3, T4>)state), this);
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
                task2.SourceOnCompleted(static state => TryInvokeContinuationT2((WhenAnyPromise<T1, T2, T3, T4>)state), this);
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
                task3.SourceOnCompleted(static state => TryInvokeContinuationT3((WhenAnyPromise<T1, T2, T3, T4>)state), this);
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
                task4.SourceOnCompleted(static state => TryInvokeContinuationT4((WhenAnyPromise<T1, T2, T3, T4>)state), this);
            }
        }
    }

    static void TryInvokeContinuationT1(WhenAnyPromise<T1, T2, T3, T4> self)
    {
        if (!self.IncrementCompleted(1))
        {
            LuminTaskHandle loser = self._task1._handle;
            if (!loser.IsNull())
                LuminTaskCoreSource.Forget(loser);
            return;
        }

        try
        {
            self.t1 = self._task1.GetResult();
        }
        catch (Exception ex)
        {
            self.Fault(ex);
            return;
        }

        var result = (0, self.t1, self.t2, self.t3, self.t4);
        self.Complete(in result);
    }

    static void TryInvokeContinuationT2(WhenAnyPromise<T1, T2, T3, T4> self)
    {
        if (!self.IncrementCompleted(1))
        {
            LuminTaskHandle loser = self._task2._handle;
            if (!loser.IsNull())
                LuminTaskCoreSource.Forget(loser);
            return;
        }

        try
        {
            self.t2 = self._task2.GetResult();
        }
        catch (Exception ex)
        {
            self.Fault(ex);
            return;
        }

        var result = (1, self.t1, self.t2, self.t3, self.t4);
        self.Complete(in result);
    }

    static void TryInvokeContinuationT3(WhenAnyPromise<T1, T2, T3, T4> self)
    {
        if (!self.IncrementCompleted(1))
        {
            LuminTaskHandle loser = self._task3._handle;
            if (!loser.IsNull())
                LuminTaskCoreSource.Forget(loser);
            return;
        }

        try
        {
            self.t3 = self._task3.GetResult();
        }
        catch (Exception ex)
        {
            self.Fault(ex);
            return;
        }

        var result = (2, self.t1, self.t2, self.t3, self.t4);
        self.Complete(in result);
    }

    static void TryInvokeContinuationT4(WhenAnyPromise<T1, T2, T3, T4> self)
    {
        if (!self.IncrementCompleted(1))
        {
            LuminTaskHandle loser = self._task4._handle;
            if (!loser.IsNull())
                LuminTaskCoreSource.Forget(loser);
            return;
        }

        try
        {
            self.t4 = self._task4.GetResult();
        }
        catch (Exception ex)
        {
            self.Fault(ex);
            return;
        }

        var result = (3, self.t1, self.t2, self.t3, self.t4);
        self.Complete(in result);
    }

}

internal sealed class WhenAnyPromise<T> : CombinerNode
{
    private T[] _results;

    public static WhenAnyPromise<T> Create(params LuminTask<T>[] tasks)
    {
        return new WhenAnyPromise<T>(tasks);
    }

    public static WhenAnyPromise<T> Create(IEnumerable<LuminTask<T>> tasks)
    {
        return new WhenAnyPromise<T>(tasks.ToArray());
    }

    public WhenAnyPromise(LuminTask<T>[]? tasks)
    {
        if (tasks == null || tasks.Length == 0)
        {
            _results = Array.Empty<T>();
            Fault(new InvalidOperationException("No tasks provided"));
            return;
        }

        _results = new T[tasks.Length];

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
                    var tuple = (StateTuple<(WhenAnyPromise<T>, LuminTask<T>, int)>)state;
                    var owner = tuple.Item.Item1;
                    var awaiter = tuple.Item.Item2;
                    var index = tuple.Item.Item3;
                    tuple.Dispose();
                    TryInvokeContinuation(owner, awaiter, index);
                }, StateTuple.Create((this, awaiter, index)));
            }
        }
    }

    static void TryInvokeContinuation(WhenAnyPromise<T> self, in LuminTask<T> awaiter, int index)
    {
        if (!self.IncrementCompleted(1))
        {
            LuminTaskHandle loser = awaiter._handle;
            if (!loser.IsNull())
                LuminTaskCoreSource.Forget(loser);
            return;
        }

        try
        {
            self._results[index] = awaiter.GetResult();
        }
        catch (Exception ex)
        {
            self.Fault(ex);
            return;
        }

        var result = (index, self._results[index]);
        self.Complete(in result);
    }
}

internal sealed class WhenAnyPromise2 : CombinerNode
{
    private LuminTask _task1;
    private LuminTask _task2;

    public WhenAnyPromise2(LuminTask task1, LuminTask task2)
    {
        {
            _task1 = task1;
            if (task1.IsCompleted)
            {
                TryInvokeContinuationT1(this);
            }
            else
            {
                task1.SourceOnCompleted(static state => TryInvokeContinuationT1((WhenAnyPromise2)state), this);
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
                task2.SourceOnCompleted(static state => TryInvokeContinuationT2((WhenAnyPromise2)state), this);
            }
        }
    }

    static void TryInvokeContinuationT1(WhenAnyPromise2 self)
    {
        if (!self.IncrementCompleted(1))
        {
            LuminTaskHandle loser = self._task1._handle;
            if (!loser.IsNull())
                LuminTaskCoreSource.Forget(loser);
            return;
        }

        try
        {
            self._task1.GetResult();
        }
        catch (Exception ex)
        {
            self.Fault(ex);
            return;
        }

        self.Complete(0);
    }
    static void TryInvokeContinuationT2(WhenAnyPromise2 self)
    {
        if (!self.IncrementCompleted(1))
        {
            LuminTaskHandle loser = self._task2._handle;
            if (!loser.IsNull())
                LuminTaskCoreSource.Forget(loser);
            return;
        }

        try
        {
            self._task2.GetResult();
        }
        catch (Exception ex)
        {
            self.Fault(ex);
            return;
        }

        self.Complete(1);
    }
}

internal sealed class WhenAnyPromise3 : CombinerNode
{
    private LuminTask _task1;
    private LuminTask _task2;
    private LuminTask _task3;

    public WhenAnyPromise3(LuminTask task1, LuminTask task2, LuminTask task3)
    {
        {
            _task1 = task1;
            if (task1.IsCompleted)
            {
                TryInvokeContinuationT1(this);
            }
            else
            {
                task1.SourceOnCompleted(static state => TryInvokeContinuationT1((WhenAnyPromise3)state), this);
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
                task2.SourceOnCompleted(static state => TryInvokeContinuationT2((WhenAnyPromise3)state), this);
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
                task3.SourceOnCompleted(static state => TryInvokeContinuationT3((WhenAnyPromise3)state), this);
            }
        }
    }

    static void TryInvokeContinuationT1(WhenAnyPromise3 self)
    {
        if (!self.IncrementCompleted(1))
        {
            LuminTaskHandle loser = self._task1._handle;
            if (!loser.IsNull())
                LuminTaskCoreSource.Forget(loser);
            return;
        }

        try
        {
            self._task1.GetResult();
        }
        catch (Exception ex)
        {
            self.Fault(ex);
            return;
        }

        self.Complete(0);
    }
    static void TryInvokeContinuationT2(WhenAnyPromise3 self)
    {
        if (!self.IncrementCompleted(1))
        {
            LuminTaskHandle loser = self._task2._handle;
            if (!loser.IsNull())
                LuminTaskCoreSource.Forget(loser);
            return;
        }

        try
        {
            self._task2.GetResult();
        }
        catch (Exception ex)
        {
            self.Fault(ex);
            return;
        }

        self.Complete(1);
    }
    static void TryInvokeContinuationT3(WhenAnyPromise3 self)
    {
        if (!self.IncrementCompleted(1))
        {
            LuminTaskHandle loser = self._task3._handle;
            if (!loser.IsNull())
                LuminTaskCoreSource.Forget(loser);
            return;
        }

        try
        {
            self._task3.GetResult();
        }
        catch (Exception ex)
        {
            self.Fault(ex);
            return;
        }

        self.Complete(2);
    }
}

internal sealed class WhenAnyPromise4 : CombinerNode
{
    private LuminTask _task1;
    private LuminTask _task2;
    private LuminTask _task3;
    private LuminTask _task4;

    public WhenAnyPromise4(LuminTask task1, LuminTask task2, LuminTask task3, LuminTask task4)
    {
        {
            _task1 = task1;
            if (task1.IsCompleted)
            {
                TryInvokeContinuationT1(this);
            }
            else
            {
                task1.SourceOnCompleted(static state => TryInvokeContinuationT1((WhenAnyPromise4)state), this);
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
                task2.SourceOnCompleted(static state => TryInvokeContinuationT2((WhenAnyPromise4)state), this);
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
                task3.SourceOnCompleted(static state => TryInvokeContinuationT3((WhenAnyPromise4)state), this);
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
                task4.SourceOnCompleted(static state => TryInvokeContinuationT4((WhenAnyPromise4)state), this);
            }
        }
    }

    static void TryInvokeContinuationT1(WhenAnyPromise4 self)
    {
        if (!self.IncrementCompleted(1))
        {
            LuminTaskHandle loser = self._task1._handle;
            if (!loser.IsNull())
                LuminTaskCoreSource.Forget(loser);
            return;
        }

        try
        {
            self._task1.GetResult();
        }
        catch (Exception ex)
        {
            self.Fault(ex);
            return;
        }

        self.Complete(0);
    }
    static void TryInvokeContinuationT2(WhenAnyPromise4 self)
    {
        if (!self.IncrementCompleted(1))
        {
            LuminTaskHandle loser = self._task2._handle;
            if (!loser.IsNull())
                LuminTaskCoreSource.Forget(loser);
            return;
        }

        try
        {
            self._task2.GetResult();
        }
        catch (Exception ex)
        {
            self.Fault(ex);
            return;
        }

        self.Complete(1);
    }
    static void TryInvokeContinuationT3(WhenAnyPromise4 self)
    {
        if (!self.IncrementCompleted(1))
        {
            LuminTaskHandle loser = self._task3._handle;
            if (!loser.IsNull())
                LuminTaskCoreSource.Forget(loser);
            return;
        }

        try
        {
            self._task3.GetResult();
        }
        catch (Exception ex)
        {
            self.Fault(ex);
            return;
        }

        self.Complete(2);
    }
    static void TryInvokeContinuationT4(WhenAnyPromise4 self)
    {
        if (!self.IncrementCompleted(1))
        {
            LuminTaskHandle loser = self._task4._handle;
            if (!loser.IsNull())
                LuminTaskCoreSource.Forget(loser);
            return;
        }

        try
        {
            self._task4.GetResult();
        }
        catch (Exception ex)
        {
            self.Fault(ex);
            return;
        }

        self.Complete(3);
    }
}

internal sealed class WhenAnyPromise : CombinerNode
{
    public static WhenAnyPromise Create(params LuminTask[] tasks)
    {
        return new WhenAnyPromise(tasks);
    }

    public static WhenAnyPromise Create(IEnumerable<LuminTask> tasks)
    {
        return new WhenAnyPromise(tasks.ToArray());
    }

    public WhenAnyPromise(LuminTask[]? tasks)
    {
        if (tasks == null || tasks.Length == 0)
        {
            Fault(new InvalidOperationException("No tasks provided"));
            return;
        }

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
                    var tuple = (StateTuple<(WhenAnyPromise, LuminTask, int)>)state;
                    var owner = tuple.Item.Item1;
                    var awaiter = tuple.Item.Item2;
                    var index = tuple.Item.Item3;
                    tuple.Dispose();
                    TryInvokeContinuation(owner, awaiter, index);
                }, StateTuple.Create((this, awaiter, index)));
            }
        }
    }

    static void TryInvokeContinuation(WhenAnyPromise self, in LuminTask awaiter, int index)
    {
        if (!self.IncrementCompleted(1))
        {
            LuminTaskHandle loser = awaiter._handle;
            if (!loser.IsNull())
                LuminTaskCoreSource.Forget(loser);
            return;
        }

        try
        {
            awaiter.GetResult();
        }
        catch (Exception ex)
        {
            self.Fault(ex);
            return;
        }

        self.Complete(index);
    }
}
