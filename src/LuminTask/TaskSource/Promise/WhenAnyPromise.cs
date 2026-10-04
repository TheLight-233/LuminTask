
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
    public WhenAnyPromise(LuminTask<T>[]? tasks)
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
                // 挂起中的孩子必定拥有槽位，句柄即可唯一确定它：状态不带 T，与其它结果类型、
                // 与 WhenAll 数组版共用同一个池和一个非泛型委托。
                awaiter.SourceOnCompleted(ChildStateContinuation,
                    StateTuple.Create(new ChildState(this, awaiter._handle, index)));
            }
        }
    }

    internal override void OnSuspendedChildCompleted(LuminTaskHandle handle, int index)
        => TryInvokeContinuation(this, LuminTaskSource.ToTask<T>(handle), index);

    static void TryInvokeContinuation(WhenAnyPromise<T> self, in LuminTask<T> awaiter, int index)
    {
        if (!self.IncrementCompleted(1))
        {
            LuminTaskHandle loser = awaiter._handle;
            if (!loser.IsNull())
                LuminTaskCoreSource.Forget(loser);
            return;
        }

        // Exactly one child wins, and only its value is ever read: it lives in a local instead of
        // an array sized to the whole input, which used to be allocated per call for one element.
        T value;
        try
        {
            value = awaiter.GetResult();
        }
        catch (Exception ex)
        {
            self.Fault(ex);
            return;
        }

        var result = (index, value);
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
                awaiter.SourceOnCompleted(ChildStateContinuation,
                    StateTuple.Create(new ChildState(this, awaiter._handle, index)));
            }
        }
    }

    internal override void OnSuspendedChildCompleted(LuminTaskHandle handle, int index)
        => TryInvokeContinuation(this, LuminTaskSource.ToTask(handle), index);

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
