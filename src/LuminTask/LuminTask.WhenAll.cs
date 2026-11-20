using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using LuminThread.Interface;
using LuminThread.TaskSource.Promise;

namespace LuminThread;

public partial struct LuminTask
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static LuminTask WhenAll(LuminTask[]? tasks)
    {
        if (tasks == null || tasks.Length == 0)
        {
            return FromResult();
        }

        return new LuminTask(VoidJoin.Create(false, tasks.Length, tasks));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static LuminTask WhenAll(IEnumerable<LuminTask>? tasks)
    {
        if (tasks == null)
            return FromResult();

        if (tasks is List<LuminTask> { Count: > 0 } list && list.GetType() == typeof(List<LuminTask>))
            return new LuminTask(VoidJoin.Create(false, list.Count, null, list: list));

        return WhenAll(tasks as LuminTask[] ?? tasks.ToArray());
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static LuminTask<T[]> WhenAll<T>(LuminTask<T>[]? tasks)
    {
        if (tasks == null || tasks.Length == 0)
        {
            return LuminTask<T[]>.FromResult([]);
        }

        return new LuminTask<T[]>(ArrayJoin<T>.Create(false, tasks.Length, tasks));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static LuminTask<T[]> WhenAll<T>(IEnumerable<LuminTask<T>>? tasks)
    {
        if (tasks == null)
            return LuminTask<T[]>.FromResult([]);

        if (tasks is List<LuminTask<T>> { Count: > 0 } list && list.GetType() == typeof(List<LuminTask<T>>))
            return new LuminTask<T[]>(ArrayJoin<T>.Create(false, list.Count, null, list: list));

        return WhenAll(tasks as LuminTask<T>[] ?? tasks.ToArray());
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static LuminTask<(T1, T2)> WhenAll<T1, T2>(in LuminTask<T1> task1, in LuminTask<T2> task2)
    {
        if (task1.Status.IsCompletedSuccessfully() && task2.Status.IsCompletedSuccessfully())
        {
            return new LuminTask<(T1, T2)>((task1.GetAwaiter().GetResult(), task2.GetAwaiter().GetResult()));
        }

        return new LuminTask<(T1, T2)>(TupleJoin<T1, T2>.Create(false, task1, task2));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static LuminTask<(T1, T2, T3)> WhenAll<T1, T2, T3>(in LuminTask<T1> task1, in LuminTask<T2> task2, in LuminTask<T3> task3)
    {
        if (task1.Status.IsCompletedSuccessfully() && task2.Status.IsCompletedSuccessfully() && task3.Status.IsCompletedSuccessfully())
        {
            return new LuminTask<(T1, T2, T3)>((task1.GetAwaiter().GetResult(), task2.GetAwaiter().GetResult(), task3.GetAwaiter().GetResult()));
        }

        return new LuminTask<(T1, T2, T3)>(TupleJoin<T1, T2, T3>.Create(false, task1, task2, task3));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static LuminTask<(T1, T2, T3, T4)> WhenAll<T1, T2, T3, T4>(in LuminTask<T1> task1, in LuminTask<T2> task2, in LuminTask<T3> task3, in LuminTask<T4> task4)
    {
        if (task1.Status.IsCompletedSuccessfully() && task2.Status.IsCompletedSuccessfully() && task3.Status.IsCompletedSuccessfully() && task4.Status.IsCompletedSuccessfully())
        {
            return new LuminTask<(T1, T2, T3, T4)>((task1.GetAwaiter().GetResult(), task2.GetAwaiter().GetResult(), task3.GetAwaiter().GetResult(), task4.GetAwaiter().GetResult()));
        }

        return new LuminTask<(T1, T2, T3, T4)>(TupleJoin<T1, T2, T3, T4>.Create(false, task1, task2, task3, task4));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static LuminTask WhenAll(in LuminTask task1)
    {
        return task1;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static LuminTask WhenAll(in LuminTask task1, in LuminTask task2)
    {
        if (task1.Status.IsCompletedSuccessfully() && task2.Status.IsCompletedSuccessfully())
        {
            task1.GetAwaiter().GetResult();
            task2.GetAwaiter().GetResult();
            return FromResult();
        }

        return new LuminTask(VoidJoin.Create(false, 2, null, task1, task2));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static LuminTask WhenAll(in LuminTask task1, in LuminTask task2, in LuminTask task3)
    {
        if (task1.Status.IsCompletedSuccessfully() && task2.Status.IsCompletedSuccessfully() && task3.Status.IsCompletedSuccessfully())
        {
            task1.GetAwaiter().GetResult();
            task2.GetAwaiter().GetResult();
            task3.GetAwaiter().GetResult();
            return FromResult();
        }

        return new LuminTask(VoidJoin.Create(false, 3, null, task1, task2, task3));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static LuminTask WhenAll(in LuminTask task1, in LuminTask task2, in LuminTask task3, in LuminTask task4)
    {
        if (task1.Status.IsCompletedSuccessfully() && task2.Status.IsCompletedSuccessfully() && task3.Status.IsCompletedSuccessfully() && task4.Status.IsCompletedSuccessfully())
        {
            task1.GetAwaiter().GetResult();
            task2.GetAwaiter().GetResult();
            task3.GetAwaiter().GetResult();
            task4.GetAwaiter().GetResult();
            return FromResult();
        }

        return new LuminTask(VoidJoin.Create(false, 4, null, task1, task2, task3, task4));
    }
}