using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using LuminThread.TaskSource.Promise;

namespace LuminThread;

public partial struct LuminTask
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static LuminTask<int> WhenAny(LuminTask[]? tasks)
    {
        if (tasks is null || tasks.Length == 0)
            return FromException<int>(new InvalidOperationException("At least one task is required."));

        return new LuminTask<int>(VoidJoin.Create(true, tasks.Length, tasks));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static LuminTask<int> WhenAny(IEnumerable<LuminTask>? tasks)
    {
        if (tasks is null)
            return FromException<int>(new ArgumentNullException(nameof(tasks)));

        if (tasks is List<LuminTask> { Count: > 0 } list && list.GetType() == typeof(List<LuminTask>))
            return new LuminTask<int>(VoidJoin.Create(true, list.Count, null, list: list));

        return WhenAny(tasks as LuminTask[] ?? tasks.ToArray());
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static LuminTask<(int winArgumentIndex, T result)> WhenAny<T>(LuminTask<T>[]? tasks)
    {
        if (tasks is null || tasks.Length == 0)
            return FromException<(int winArgumentIndex, T result)>(
                new InvalidOperationException("At least one task is required."));

        return new LuminTask<(int winArgumentIndex, T result)>(ArrayJoin<T>.Create(true, tasks.Length, tasks));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static LuminTask<(int winArgumentIndex, T result)> WhenAny<T>(IEnumerable<LuminTask<T>>? tasks)
    {
        if (tasks is null)
            return FromException<(int winArgumentIndex, T result)>(new ArgumentNullException(nameof(tasks)));

        if (tasks is List<LuminTask<T>> { Count: > 0 } list && list.GetType() == typeof(List<LuminTask<T>>))
            return new LuminTask<(int winArgumentIndex, T result)>(ArrayJoin<T>.Create(true, list.Count, null, list: list));

        return WhenAny(tasks as LuminTask<T>[] ?? tasks.ToArray());
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static LuminTask<(int winArgumentIndex, T1 result1, T2 result2)> WhenAny<T1, T2>(LuminTask<T1> task1, LuminTask<T2> task2)
    {
        return new LuminTask<(int winArgumentIndex, T1 result1, T2 result2)>(TupleJoin<T1, T2>.Create(true, task1, task2));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static LuminTask<(int winArgumentIndex, T1 result1, T2 result2, T3 result3)> WhenAny<T1, T2, T3>(LuminTask<T1> task1, LuminTask<T2> task2, LuminTask<T3> task3)
    {
        return new LuminTask<(int winArgumentIndex, T1 result1, T2 result2, T3 result3)>(TupleJoin<T1, T2, T3>.Create(true, task1, task2, task3));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static LuminTask<(int winArgumentIndex, T1 result1, T2 result2, T3 result3, T4 result4)> WhenAny<T1, T2, T3, T4>(LuminTask<T1> task1, LuminTask<T2> task2, LuminTask<T3> task3, LuminTask<T4> task4)
    {
        return new LuminTask<(int winArgumentIndex, T1 result1, T2 result2, T3 result3, T4 result4)>(TupleJoin<T1, T2, T3, T4>.Create(true, task1, task2, task3, task4));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static LuminTask<int> WhenAny(LuminTask task1, LuminTask task2)
    {
        return new LuminTask<int>(VoidJoin.Create(true, 2, null, task1, task2));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static LuminTask<int> WhenAny(LuminTask task1, LuminTask task2, LuminTask task3)
    {
        return new LuminTask<int>(VoidJoin.Create(true, 3, null, task1, task2, task3));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static LuminTask<int> WhenAny(LuminTask task1, LuminTask task2, LuminTask task3, LuminTask task4)
    {
        return new LuminTask<int>(VoidJoin.Create(true, 4, null, task1, task2, task3, task4));
    }
}