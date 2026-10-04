
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
                task1.SourceOnCompleted(ChildContinuation0, this);
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
                task2.SourceOnCompleted(ChildContinuation1, this);
            }
        }
    }

    internal override void OnChildCompleted(int index)
    {
        switch (index)
        {
            case 0: TryInvokeContinuationT1(this); break;
            default: TryInvokeContinuationT2(this); break;
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
                task1.SourceOnCompleted(ChildContinuation0, this);
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
                task2.SourceOnCompleted(ChildContinuation1, this);
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
                task3.SourceOnCompleted(ChildContinuation2, this);
            }
        }
    }

    internal override void OnChildCompleted(int index)
    {
        switch (index)
        {
            case 0: TryInvokeContinuationT1(this); break;
            case 1: TryInvokeContinuationT2(this); break;
            default: TryInvokeContinuationT3(this); break;
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
                task1.SourceOnCompleted(ChildContinuation0, this);
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
                task2.SourceOnCompleted(ChildContinuation1, this);
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
                task3.SourceOnCompleted(ChildContinuation2, this);
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
                task4.SourceOnCompleted(ChildContinuation3, this);
            }
        }
    }

    internal override void OnChildCompleted(int index)
    {
        switch (index)
        {
            case 0: TryInvokeContinuationT1(this); break;
            case 1: TryInvokeContinuationT2(this); break;
            case 2: TryInvokeContinuationT3(this); break;
            default: TryInvokeContinuationT4(this); break;
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
                task1.SourceOnCompleted(ChildContinuation0, this);
            }
        }
    }

    internal override void OnChildCompleted(int index)
    {
        switch (index)
        {
            default: TryInvokeContinuationT1(this); break;
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
                task1.SourceOnCompleted(ChildContinuation0, this);
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
                task2.SourceOnCompleted(ChildContinuation1, this);
            }
        }
    }

    internal override void OnChildCompleted(int index)
    {
        switch (index)
        {
            case 0: TryInvokeContinuationT1(this); break;
            default: TryInvokeContinuationT2(this); break;
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
                task1.SourceOnCompleted(ChildContinuation0, this);
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
                task2.SourceOnCompleted(ChildContinuation1, this);
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
                task3.SourceOnCompleted(ChildContinuation2, this);
            }
        }
    }

    internal override void OnChildCompleted(int index)
    {
        switch (index)
        {
            case 0: TryInvokeContinuationT1(this); break;
            case 1: TryInvokeContinuationT2(this); break;
            default: TryInvokeContinuationT3(this); break;
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
                task1.SourceOnCompleted(ChildContinuation0, this);
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
                task2.SourceOnCompleted(ChildContinuation1, this);
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
                task3.SourceOnCompleted(ChildContinuation2, this);
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
                task4.SourceOnCompleted(ChildContinuation3, this);
            }
        }
    }

    internal override void OnChildCompleted(int index)
    {
        switch (index)
        {
            case 0: TryInvokeContinuationT1(this); break;
            case 1: TryInvokeContinuationT2(this); break;
            case 2: TryInvokeContinuationT3(this); break;
            default: TryInvokeContinuationT4(this); break;
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
                // 挂起中的孩子必定拥有槽位（句柄 0 等于同步完成），所以句柄就能唯一确定它，
                // 结果类型 T 不进入回调状态：与其它 T、其它数组组合子共用同一个池与同一个
                // 非泛型委托，而不是每种 T 各有一份元组状态与 lambda 缓存。
                awaiter.SourceOnCompleted(ChildStateContinuation,
                    StateTuple.Create(new ChildState(this, awaiter._handle, index)));
            }
        }
    }

    internal override void OnSuspendedChildCompleted(LuminTaskHandle handle, int index)
        => TryInvokeContinuation(this, LuminTaskSource.ToTask<T>(handle), index);

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
                // 无结果槽位，下标不使用；仍然走同一份共享状态，保持只有一个池。
                awaiter.SourceOnCompleted(ChildStateContinuation,
                    StateTuple.Create(new ChildState(this, awaiter._handle, 0)));
            }
        }
    }

    internal override void OnSuspendedChildCompleted(LuminTaskHandle handle, int index)
        => TryInvokeContinuation(this, LuminTaskSource.ToTask(handle));

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
