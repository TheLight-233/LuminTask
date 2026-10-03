using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using LuminThread.Interface;
using LuminThread.TaskSource.Promise;

namespace LuminThread;

public partial struct LuminTask
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe LuminTask WhenAll(LuminTask[]? tasks)
    {
        if (tasks == null || tasks.Length == 0)
        {
            return FromResult();
        }

        var promise = new WhenAllPromise(tasks);
        return new LuminTask(promise.Handle);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe LuminTask WhenAll(IEnumerable<LuminTask>? tasks)
    {
        if (tasks == null)
            return FromResult();

        LuminTask[] array;

        if (tasks is LuminTask[] tasksArray)
        {
            array = tasksArray;
        }
        else if (tasks is ICollection<LuminTask> col)
        {

            array = new LuminTask[col.Count];
            col.CopyTo(array, 0);
        }
        else
        {
            array = tasks.ToArray();
        }

        if (array.Length == 0)
            return FromResult();

        var promise = new WhenAllPromise(array);
        return new LuminTask(promise.Handle);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe LuminTask<T[]> WhenAll<T>(LuminTask<T>[]? tasks)
    {
        if (tasks == null || tasks.Length == 0)
        {
            return LuminTask<T[]>.FromResult([]);
        }

        var promise = new WhenAllPromise<T>(tasks);
        return new LuminTask<T[]>(promise.Handle);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe LuminTask<T[]> WhenAll<T>(IEnumerable<LuminTask<T>>? tasks)
    {
        if (tasks == null)
            return LuminTask<T[]>.FromResult([]);

        LuminTask<T>[] array;

        if (tasks is LuminTask<T>[] tasksArray)
        {
            array = tasksArray;
        }
        else if (tasks is ICollection<LuminTask<T>> col)
        {

            array = new LuminTask<T>[col.Count];
            col.CopyTo(array, 0);
        }
        else
        {
            array = tasks.ToArray();
        }

        if (array.Length == 0)
            return LuminTask<T[]>.FromResult([]);

        var promise = new WhenAllPromise<T>(array);
        return new LuminTask<T[]>(promise.Handle);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe LuminTask<(T1, T2)> WhenAll<T1, T2>(in LuminTask<T1> task1, in LuminTask<T2> task2)
    {
        if (task1.Status.IsCompletedSuccessfully() && task2.Status.IsCompletedSuccessfully())
        {
            return new LuminTask<(T1, T2)>((task1.GetAwaiter().GetResult(), task2.GetAwaiter().GetResult()));
        }

        var promise = new WhenAllPromise<T1, T2>(task1, task2);
        return new LuminTask<(T1, T2)>(promise.Handle);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe LuminTask<(T1, T2, T3)> WhenAll<T1, T2, T3>(in LuminTask<T1> task1, in LuminTask<T2> task2, in LuminTask<T3> task3)
    {
        if (task1.Status.IsCompletedSuccessfully() && task2.Status.IsCompletedSuccessfully() && task3.Status.IsCompletedSuccessfully())
        {
            return new LuminTask<(T1, T2, T3)>((task1.GetAwaiter().GetResult(), task2.GetAwaiter().GetResult(), task3.GetAwaiter().GetResult()));
        }

        var promise = new WhenAllPromise<T1, T2, T3>(task1, task2, task3);
        return new LuminTask<(T1, T2, T3)>(promise.Handle);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe LuminTask<(T1, T2, T3, T4)> WhenAll<T1, T2, T3, T4>(in LuminTask<T1> task1, in LuminTask<T2> task2, in LuminTask<T3> task3, in LuminTask<T4> task4)
    {
        if (task1.Status.IsCompletedSuccessfully() && task2.Status.IsCompletedSuccessfully() && task3.Status.IsCompletedSuccessfully() && task4.Status.IsCompletedSuccessfully())
        {
            return new LuminTask<(T1, T2, T3, T4)>((task1.GetAwaiter().GetResult(), task2.GetAwaiter().GetResult(), task3.GetAwaiter().GetResult(), task4.GetAwaiter().GetResult()));
        }

        var promise = new WhenAllPromise<T1, T2, T3, T4>(task1, task2, task3, task4);
        return new LuminTask<(T1, T2, T3, T4)>(promise.Handle);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static LuminTask WhenAll(in LuminTask task1)
    {
        return task1;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe LuminTask WhenAll(in LuminTask task1, in LuminTask task2)
    {
        if (task1.Status.IsCompletedSuccessfully() && task2.Status.IsCompletedSuccessfully())
        {
            task1.GetAwaiter().GetResult();
            task2.GetAwaiter().GetResult();
            return FromResult();
        }

        var promise = new WhenAllPromise2(task1, task2);
        return new LuminTask(promise.Handle);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe LuminTask WhenAll(in LuminTask task1, in LuminTask task2, in LuminTask task3)
    {
        if (task1.Status.IsCompletedSuccessfully() && task2.Status.IsCompletedSuccessfully() && task3.Status.IsCompletedSuccessfully())
        {
            task1.GetAwaiter().GetResult();
            task2.GetAwaiter().GetResult();
            task3.GetAwaiter().GetResult();
            return FromResult();
        }

        var promise = new WhenAllPromise3(task1, task2, task3);
        return new LuminTask(promise.Handle);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe LuminTask WhenAll(in LuminTask task1, in LuminTask task2, in LuminTask task3, in LuminTask task4)
    {
        if (task1.Status.IsCompletedSuccessfully() && task2.Status.IsCompletedSuccessfully() && task3.Status.IsCompletedSuccessfully() && task4.Status.IsCompletedSuccessfully())
        {
            task1.GetAwaiter().GetResult();
            task2.GetAwaiter().GetResult();
            task3.GetAwaiter().GetResult();
            task4.GetAwaiter().GetResult();
            return FromResult();
        }

        var promise = new WhenAllPromise4(task1, task2, task3, task4);
        return new LuminTask(promise.Handle);
    }
}