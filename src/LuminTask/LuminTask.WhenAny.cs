using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using LuminThread.TaskSource.Promise;

namespace LuminThread;

public readonly partial struct LuminTask
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe LuminTask<int> WhenAny(LuminTask[]? tasks)
    {
        if (tasks is null || tasks.Length == 0)
            return FromException<int>(new InvalidOperationException("At least one task is required."));

        var promise = new WhenAnyPromise(tasks);
        return new LuminTask<int>(promise.Handle);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe LuminTask<int> WhenAny(IEnumerable<LuminTask>? tasks)
    {
        if (tasks is null)
            return FromException<int>(new ArgumentNullException(nameof(tasks)));

        LuminTask[] array;

        if (tasks is LuminTask[] tasksArray)
        {
            array = tasksArray;
        }
        else if (tasks is ICollection<LuminTask> collection)
        {
            array = new LuminTask[collection.Count];
            collection.CopyTo(array, 0);
        }
        else
        {
            array = tasks.ToArray();
        }

        if (array.Length == 0)
            return FromException<int>(new InvalidOperationException("At least one task is required."));

        var promise = new WhenAnyPromise(array);
        return new LuminTask<int>(promise.Handle);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe LuminTask<(int winArgumentIndex, T result)> WhenAny<T>(LuminTask<T>[]? tasks)
    {
        if (tasks is null || tasks.Length == 0)
            return FromException<(int winArgumentIndex, T result)>(
                new InvalidOperationException("At least one task is required."));

        var promise = new WhenAnyPromise<T>(tasks);
        return new LuminTask<(int winArgumentIndex, T result)>(promise.Handle);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe LuminTask<(int winArgumentIndex, T result)> WhenAny<T>(IEnumerable<LuminTask<T>>? tasks)
    {
        if (tasks is null)
            return FromException<(int winArgumentIndex, T result)>(new ArgumentNullException(nameof(tasks)));

        LuminTask<T>[] array;

        if (tasks is LuminTask<T>[] tasksArray)
        {
            array = tasksArray;
        }
        else if (tasks is ICollection<LuminTask<T>> collection)
        {
            array = new LuminTask<T>[collection.Count];
            collection.CopyTo(array, 0);
        }
        else
        {
            array = tasks.ToArray();
        }

        if (array.Length == 0)
            return FromException<(int winArgumentIndex, T result)>(
                new InvalidOperationException("At least one task is required."));

        var promise = new WhenAnyPromise<T>(array);
        return new LuminTask<(int winArgumentIndex, T result)>(promise.Handle);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe LuminTask<(int winArgumentIndex, T1 result1, T2 result2)> WhenAny<T1, T2>(LuminTask<T1> task1, LuminTask<T2> task2)
    {
        var promise = new WhenAnyPromise<T1, T2>(task1, task2);
        return new LuminTask<(int winArgumentIndex, T1 result1, T2 result2)>(promise.Handle);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe LuminTask<(int winArgumentIndex, T1 result1, T2 result2, T3 result3)> WhenAny<T1, T2, T3>(LuminTask<T1> task1, LuminTask<T2> task2, LuminTask<T3> task3)
    {
        var promise = new WhenAnyPromise<T1, T2, T3>(task1, task2, task3);
        return new LuminTask<(int winArgumentIndex, T1 result1, T2 result2, T3 result3)>(promise.Handle);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe LuminTask<(int winArgumentIndex, T1 result1, T2 result2, T3 result3, T4 result4)> WhenAny<T1, T2, T3, T4>(LuminTask<T1> task1, LuminTask<T2> task2, LuminTask<T3> task3, LuminTask<T4> task4)
    {
        var promise = new WhenAnyPromise<T1, T2, T3, T4>(task1, task2, task3, task4);
        return new LuminTask<(int winArgumentIndex, T1 result1, T2 result2, T3 result3, T4 result4)>(promise.Handle);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe LuminTask<int> WhenAny(LuminTask task1, LuminTask task2)
    {
        var promise = new WhenAnyPromise2(task1, task2);
        return new LuminTask<int>(promise.Handle);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe LuminTask<int> WhenAny(LuminTask task1, LuminTask task2, LuminTask task3)
    {
        var promise = new WhenAnyPromise3(task1, task2, task3);
        return new LuminTask<int>(promise.Handle);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe LuminTask<int> WhenAny(LuminTask task1, LuminTask task2, LuminTask task3, LuminTask task4)
    {
        var promise = new WhenAnyPromise4(task1, task2, task3, task4);
        return new LuminTask<int>(promise.Handle);
    }
}