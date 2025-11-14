using System.Collections.Generic;
using System.Runtime.CompilerServices;
using LuminThread.Interface;
using LuminThread.TaskSource.Promise;

namespace LuminThread;

public readonly partial struct LuminTask
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ILuminTaskAsyncEnumerable<WhenEachResult<T>> WhenEach<T>(LuminTask<T>[] tasks)
    {
        return new WhenEachPromise<T>(tasks);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ILuminTaskAsyncEnumerable<WhenEachResult<T>> WhenEach<T>(IEnumerable<LuminTask<T>> tasks)
    {
        return new WhenEachPromise<T>(tasks);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ILuminTaskAsyncEnumerable<WhenEachResult<T>> WhenEach<T>(LuminTask<T> task1)
    {
        return new WhenEachPromise<T>(task1);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ILuminTaskAsyncEnumerable<WhenEachResult<T>> WhenEach<T>(LuminTask<T> task1, LuminTask<T> task2)
    {
        return new WhenEachPromise<T>(task1, task2);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ILuminTaskAsyncEnumerable<WhenEachResult<T>> WhenEach<T>(LuminTask<T> task1, LuminTask<T> task2, LuminTask<T> task3)
    {
        return new WhenEachPromise<T>(task1, task2, task3);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ILuminTaskAsyncEnumerable<WhenEachResult<T>> WhenEach<T>(LuminTask<T> task1, LuminTask<T> task2, LuminTask<T> task3, LuminTask<T> task4)
    {
        return new WhenEachPromise<T>(task1, task2, task3, task4);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ILuminTaskAsyncEnumerable<WhenEachResult<T>> WhenEach<T>(LuminTask<T> task1, LuminTask<T> task2, LuminTask<T> task3, LuminTask<T> task4, LuminTask<T> task5)
    {
        return new WhenEachPromise<T>(task1, task2, task3, task4, task5);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ILuminTaskAsyncEnumerable<WhenEachResult<T>> WhenEach<T>(LuminTask<T> task1, LuminTask<T> task2, LuminTask<T> task3, LuminTask<T> task4, LuminTask<T> task5, LuminTask<T> task6)
    {
        return new WhenEachPromise<T>(task1, task2, task3, task4, task5, task6);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ILuminTaskAsyncEnumerable<WhenEachResult<T>> WhenEach<T>(LuminTask<T> task1, LuminTask<T> task2, LuminTask<T> task3, LuminTask<T> task4, LuminTask<T> task5, LuminTask<T> task6, LuminTask<T> task7)
    {
        return new WhenEachPromise<T>(task1, task2, task3, task4, task5, task6, task7);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ILuminTaskAsyncEnumerable<WhenEachResult<T>> WhenEach<T>(LuminTask<T> task1, LuminTask<T> task2, LuminTask<T> task3, LuminTask<T> task4, LuminTask<T> task5, LuminTask<T> task6, LuminTask<T> task7, LuminTask<T> task8)
    {
        return new WhenEachPromise<T>(task1, task2, task3, task4, task5, task6, task7, task8);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ILuminTaskAsyncEnumerable<WhenEachResult<T>> WhenEach<T>(LuminTask<T> task1, LuminTask<T> task2, LuminTask<T> task3, LuminTask<T> task4, LuminTask<T> task5, LuminTask<T> task6, LuminTask<T> task7, LuminTask<T> task8, LuminTask<T> task9)
    {
        return new WhenEachPromise<T>(task1, task2, task3, task4, task5, task6, task7, task8, task9);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ILuminTaskAsyncEnumerable<WhenEachResult<T>> WhenEach<T>(LuminTask<T> task1, LuminTask<T> task2, LuminTask<T> task3, LuminTask<T> task4, LuminTask<T> task5, LuminTask<T> task6, LuminTask<T> task7, LuminTask<T> task8, LuminTask<T> task9, LuminTask<T> task10)
    {
        return new WhenEachPromise<T>(task1, task2, task3, task4, task5, task6, task7, task8, task9, task10);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ILuminTaskAsyncEnumerable<WhenEachResult<T>> WhenEach<T>(LuminTask<T> task1, LuminTask<T> task2, LuminTask<T> task3, LuminTask<T> task4, LuminTask<T> task5, LuminTask<T> task6, LuminTask<T> task7, LuminTask<T> task8, LuminTask<T> task9, LuminTask<T> task10, LuminTask<T> task11)
    {
        return new WhenEachPromise<T>(task1, task2, task3, task4, task5, task6, task7, task8, task9, task10, task11);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ILuminTaskAsyncEnumerable<WhenEachResult<T>> WhenEach<T>(LuminTask<T> task1, LuminTask<T> task2, LuminTask<T> task3, LuminTask<T> task4, LuminTask<T> task5, LuminTask<T> task6, LuminTask<T> task7, LuminTask<T> task8, LuminTask<T> task9, LuminTask<T> task10, LuminTask<T> task11, LuminTask<T> task12)
    {
        return new WhenEachPromise<T>(task1, task2, task3, task4, task5, task6, task7, task8, task9, task10, task11, task12);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ILuminTaskAsyncEnumerable<WhenEachResult<T>> WhenEach<T>(LuminTask<T> task1, LuminTask<T> task2, LuminTask<T> task3, LuminTask<T> task4, LuminTask<T> task5, LuminTask<T> task6, LuminTask<T> task7, LuminTask<T> task8, LuminTask<T> task9, LuminTask<T> task10, LuminTask<T> task11, LuminTask<T> task12, LuminTask<T> task13)
    {
        return new WhenEachPromise<T>(task1, task2, task3, task4, task5, task6, task7, task8, task9, task10, task11, task12, task13);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ILuminTaskAsyncEnumerable<WhenEachResult<T>> WhenEach<T>(LuminTask<T> task1, LuminTask<T> task2, LuminTask<T> task3, LuminTask<T> task4, LuminTask<T> task5, LuminTask<T> task6, LuminTask<T> task7, LuminTask<T> task8, LuminTask<T> task9, LuminTask<T> task10, LuminTask<T> task11, LuminTask<T> task12, LuminTask<T> task13, LuminTask<T> task14)
    {
        return new WhenEachPromise<T>(task1, task2, task3, task4, task5, task6, task7, task8, task9, task10, task11, task12, task13, task14);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ILuminTaskAsyncEnumerable<WhenEachResult<T>> WhenEach<T>(LuminTask<T> task1, LuminTask<T> task2, LuminTask<T> task3, LuminTask<T> task4, LuminTask<T> task5, LuminTask<T> task6, LuminTask<T> task7, LuminTask<T> task8, LuminTask<T> task9, LuminTask<T> task10, LuminTask<T> task11, LuminTask<T> task12, LuminTask<T> task13, LuminTask<T> task14, LuminTask<T> task15)
    {
        return new WhenEachPromise<T>(task1, task2, task3, task4, task5, task6, task7, task8, task9, task10, task11, task12, task13, task14, task15);
    }
}