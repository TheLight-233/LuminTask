using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using LuminThread.Interface;
using LuminThread.TaskSource;
using LuminThread.TaskSource.Promise;

namespace LuminThread;

public readonly partial struct LuminTask
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe LuminTask WhenAll(LuminTask[]? tasks)
    {
        if (tasks == null || tasks.Length == 0)
        {
            return FromResult();
        }
        
        var promise = new WhenAllPromise(tasks);
        return new LuminTask(LuminTaskSourceCore<bool>.MethodTable, promise._core, promise._core->Id);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe LuminTask WhenAll(IEnumerable<LuminTask>? tasks)
    {
        if (tasks == null)
            return FromResult();
        
        var array = tasks.ToArray();
        
        if (array.Length == 0)
            return FromResult();
        
        var promise = new WhenAllPromise(array);
        return new LuminTask(LuminTaskSourceCore<bool>.MethodTable, promise._core, promise._core->Id);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe LuminTask<T[]> WhenAll<T>(LuminTask<T>[]? tasks)
    {
        if (tasks == null || tasks.Length == 0)
        {
            return LuminTask<T[]>.FromResult([]);
        }
        
        var promise = new WhenAllPromise<T>(tasks);
        return new LuminTask<T[]>(LuminTaskSourceCore<T[]>.MethodTable, promise._core, promise._core->Id);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe LuminTask<T[]> WhenAll<T>(IEnumerable<LuminTask<T>>? tasks)
    {
        if (tasks == null)
            return LuminTask<T[]>.FromResult([]);
        
        var array = tasks.ToArray();
        
        if (array.Length == 0)
            return LuminTask<T[]>.FromResult([]);
        
        var promise = new WhenAllPromise<T>(array);
        return new LuminTask<T[]>(LuminTaskSourceCore<T[]>.MethodTable, promise._core, promise._core->Id);
    }
    
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe LuminTask<(T1, T2)> WhenAll<T1, T2>(in LuminTask<T1> task1, in LuminTask<T2> task2)
    {
        if (task1.Status.IsCompletedSuccessfully() && task2.Status.IsCompletedSuccessfully())
        {
            return new LuminTask<(T1, T2)>((task1.GetAwaiter().GetResult(), task2.GetAwaiter().GetResult()));
        }

        var promise = new WhenAllPromise<T1, T2>(task1, task2);
        return new LuminTask<(T1, T2)>(LuminTaskSourceCore<(T1, T2)>.MethodTable, promise.core, promise.core->Id);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe LuminTask<(T1, T2, T3)> WhenAll<T1, T2, T3>(in LuminTask<T1> task1, in LuminTask<T2> task2, in LuminTask<T3> task3)
    {
        if (task1.Status.IsCompletedSuccessfully() && task2.Status.IsCompletedSuccessfully() && task3.Status.IsCompletedSuccessfully())
        {
            return new LuminTask<(T1, T2, T3)>((task1.GetAwaiter().GetResult(), task2.GetAwaiter().GetResult(), task3.GetAwaiter().GetResult()));
        }

        var promise = new WhenAllPromise<T1, T2, T3>(task1, task2, task3);
        return new LuminTask<(T1, T2, T3)>(LuminTaskSourceCore<(T1, T2, T3)>.MethodTable, promise.core, promise.core->Id);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe LuminTask<(T1, T2, T3, T4)> WhenAll<T1, T2, T3, T4>(in LuminTask<T1> task1, in LuminTask<T2> task2, in LuminTask<T3> task3, in LuminTask<T4> task4)
    {
        if (task1.Status.IsCompletedSuccessfully() && task2.Status.IsCompletedSuccessfully() && task3.Status.IsCompletedSuccessfully() && task4.Status.IsCompletedSuccessfully())
        {
            return new LuminTask<(T1, T2, T3, T4)>((task1.GetAwaiter().GetResult(), task2.GetAwaiter().GetResult(), task3.GetAwaiter().GetResult(), task4.GetAwaiter().GetResult()));
        }

        var promise = new WhenAllPromise<T1, T2, T3, T4>(task1, task2, task3, task4);
        return new LuminTask<(T1, T2, T3, T4)>(LuminTaskSourceCore<(T1, T2, T3, T4)>.MethodTable, promise.core, promise.core->Id);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe LuminTask<(T1, T2, T3, T4, T5)> WhenAll<T1, T2, T3, T4, T5>(in LuminTask<T1> task1, in LuminTask<T2> task2, in LuminTask<T3> task3, in LuminTask<T4> task4, in LuminTask<T5> task5)
    {
        if (task1.Status.IsCompletedSuccessfully() && task2.Status.IsCompletedSuccessfully() && task3.Status.IsCompletedSuccessfully() && task4.Status.IsCompletedSuccessfully() && task5.Status.IsCompletedSuccessfully())
        {
            return new LuminTask<(T1, T2, T3, T4, T5)>((task1.GetAwaiter().GetResult(), task2.GetAwaiter().GetResult(), task3.GetAwaiter().GetResult(), task4.GetAwaiter().GetResult(), task5.GetAwaiter().GetResult()));
        }

        var promise = new WhenAllPromise<T1, T2, T3, T4, T5>(task1, task2, task3, task4, task5);
        return new LuminTask<(T1, T2, T3, T4, T5)>(LuminTaskSourceCore<(T1, T2, T3, T4, T5)>.MethodTable, promise.core, promise.core->Id);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe LuminTask<(T1, T2, T3, T4, T5, T6)> WhenAll<T1, T2, T3, T4, T5, T6>(in LuminTask<T1> task1, in LuminTask<T2> task2, in LuminTask<T3> task3, in LuminTask<T4> task4, in LuminTask<T5> task5, in LuminTask<T6> task6)
    {
        if (task1.Status.IsCompletedSuccessfully() && task2.Status.IsCompletedSuccessfully() && task3.Status.IsCompletedSuccessfully() && task4.Status.IsCompletedSuccessfully() && task5.Status.IsCompletedSuccessfully() && task6.Status.IsCompletedSuccessfully())
        {
            return new LuminTask<(T1, T2, T3, T4, T5, T6)>((task1.GetAwaiter().GetResult(), task2.GetAwaiter().GetResult(), task3.GetAwaiter().GetResult(), task4.GetAwaiter().GetResult(), task5.GetAwaiter().GetResult(), task6.GetAwaiter().GetResult()));
        }

        var promise = new WhenAllPromise<T1, T2, T3, T4, T5, T6>(task1, task2, task3, task4, task5, task6);
        return new LuminTask<(T1, T2, T3, T4, T5, T6)>(LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6)>.MethodTable, promise.core, promise.core->Id);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe LuminTask<(T1, T2, T3, T4, T5, T6, T7)> WhenAll<T1, T2, T3, T4, T5, T6, T7>(in LuminTask<T1> task1, in LuminTask<T2> task2, in LuminTask<T3> task3, in LuminTask<T4> task4, in LuminTask<T5> task5, in LuminTask<T6> task6, in LuminTask<T7> task7)
    {
        if (task1.Status.IsCompletedSuccessfully() && task2.Status.IsCompletedSuccessfully() && task3.Status.IsCompletedSuccessfully() && task4.Status.IsCompletedSuccessfully() && task5.Status.IsCompletedSuccessfully() && task6.Status.IsCompletedSuccessfully() && task7.Status.IsCompletedSuccessfully())
        {
            return new LuminTask<(T1, T2, T3, T4, T5, T6, T7)>((task1.GetAwaiter().GetResult(), task2.GetAwaiter().GetResult(), task3.GetAwaiter().GetResult(), task4.GetAwaiter().GetResult(), task5.GetAwaiter().GetResult(), task6.GetAwaiter().GetResult(), task7.GetAwaiter().GetResult()));
        }

        var promise = new WhenAllPromise<T1, T2, T3, T4, T5, T6, T7>(task1, task2, task3, task4, task5, task6, task7);
        return new LuminTask<(T1, T2, T3, T4, T5, T6, T7)>(LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7)>.MethodTable, promise.core, promise.core->Id);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe LuminTask<(T1, T2, T3, T4, T5, T6, T7, T8)> WhenAll<T1, T2, T3, T4, T5, T6, T7, T8>(in LuminTask<T1> task1, in LuminTask<T2> task2, in LuminTask<T3> task3, in LuminTask<T4> task4, in LuminTask<T5> task5, in LuminTask<T6> task6, in LuminTask<T7> task7, in LuminTask<T8> task8)
    {
        if (task1.Status.IsCompletedSuccessfully() && task2.Status.IsCompletedSuccessfully() && task3.Status.IsCompletedSuccessfully() && task4.Status.IsCompletedSuccessfully() && task5.Status.IsCompletedSuccessfully() && task6.Status.IsCompletedSuccessfully() && task7.Status.IsCompletedSuccessfully() && task8.Status.IsCompletedSuccessfully())
        {
            return new LuminTask<(T1, T2, T3, T4, T5, T6, T7, T8)>((task1.GetAwaiter().GetResult(), task2.GetAwaiter().GetResult(), task3.GetAwaiter().GetResult(), task4.GetAwaiter().GetResult(), task5.GetAwaiter().GetResult(), task6.GetAwaiter().GetResult(), task7.GetAwaiter().GetResult(), task8.GetAwaiter().GetResult()));
        }

        var promise = new WhenAllPromise<T1, T2, T3, T4, T5, T6, T7, T8>(task1, task2, task3, task4, task5, task6, task7, task8);
        return new LuminTask<(T1, T2, T3, T4, T5, T6, T7, T8)>(LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8)>.MethodTable, promise.core, promise.core->Id);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe LuminTask<(T1, T2, T3, T4, T5, T6, T7, T8, T9)> WhenAll<T1, T2, T3, T4, T5, T6, T7, T8, T9>(in LuminTask<T1> task1, in LuminTask<T2> task2, in LuminTask<T3> task3, in LuminTask<T4> task4, in LuminTask<T5> task5, in LuminTask<T6> task6, in LuminTask<T7> task7, in LuminTask<T8> task8, in LuminTask<T9> task9)
    {
        if (task1.Status.IsCompletedSuccessfully() && task2.Status.IsCompletedSuccessfully() && task3.Status.IsCompletedSuccessfully() && task4.Status.IsCompletedSuccessfully() && task5.Status.IsCompletedSuccessfully() && task6.Status.IsCompletedSuccessfully() && task7.Status.IsCompletedSuccessfully() && task8.Status.IsCompletedSuccessfully() && task9.Status.IsCompletedSuccessfully())
        {
            return new LuminTask<(T1, T2, T3, T4, T5, T6, T7, T8, T9)>((task1.GetAwaiter().GetResult(), task2.GetAwaiter().GetResult(), task3.GetAwaiter().GetResult(), task4.GetAwaiter().GetResult(), task5.GetAwaiter().GetResult(), task6.GetAwaiter().GetResult(), task7.GetAwaiter().GetResult(), task8.GetAwaiter().GetResult(), task9.GetAwaiter().GetResult()));
        }

        var promise = new WhenAllPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9>(task1, task2, task3, task4, task5, task6, task7, task8, task9);
        return new LuminTask<(T1, T2, T3, T4, T5, T6, T7, T8, T9)>(LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9)>.MethodTable, promise.core, promise.core->Id);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe LuminTask<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10)> WhenAll<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(in LuminTask<T1> task1, in LuminTask<T2> task2, in LuminTask<T3> task3, in LuminTask<T4> task4, in LuminTask<T5> task5, in LuminTask<T6> task6, in LuminTask<T7> task7, in LuminTask<T8> task8, in LuminTask<T9> task9, in LuminTask<T10> task10)
    {
        if (task1.Status.IsCompletedSuccessfully() && task2.Status.IsCompletedSuccessfully() && task3.Status.IsCompletedSuccessfully() && task4.Status.IsCompletedSuccessfully() && task5.Status.IsCompletedSuccessfully() && task6.Status.IsCompletedSuccessfully() && task7.Status.IsCompletedSuccessfully() && task8.Status.IsCompletedSuccessfully() && task9.Status.IsCompletedSuccessfully() && task10.Status.IsCompletedSuccessfully())
        {
            return new LuminTask<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10)>((task1.GetAwaiter().GetResult(), task2.GetAwaiter().GetResult(), task3.GetAwaiter().GetResult(), task4.GetAwaiter().GetResult(), task5.GetAwaiter().GetResult(), task6.GetAwaiter().GetResult(), task7.GetAwaiter().GetResult(), task8.GetAwaiter().GetResult(), task9.GetAwaiter().GetResult(), task10.GetAwaiter().GetResult()));
        }

        var promise = new WhenAllPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(task1, task2, task3, task4, task5, task6, task7, task8, task9, task10);
        return new LuminTask<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10)>(LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10)>.MethodTable, promise.core, promise.core->Id);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe LuminTask<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11)> WhenAll<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(in LuminTask<T1> task1, in LuminTask<T2> task2, in LuminTask<T3> task3, in LuminTask<T4> task4, in LuminTask<T5> task5, in LuminTask<T6> task6, in LuminTask<T7> task7, in LuminTask<T8> task8, in LuminTask<T9> task9, in LuminTask<T10> task10, in LuminTask<T11> task11)
    {
        if (task1.Status.IsCompletedSuccessfully() && task2.Status.IsCompletedSuccessfully() && task3.Status.IsCompletedSuccessfully() && task4.Status.IsCompletedSuccessfully() && task5.Status.IsCompletedSuccessfully() && task6.Status.IsCompletedSuccessfully() && task7.Status.IsCompletedSuccessfully() && task8.Status.IsCompletedSuccessfully() && task9.Status.IsCompletedSuccessfully() && task10.Status.IsCompletedSuccessfully() && task11.Status.IsCompletedSuccessfully())
        {
            return new LuminTask<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11)>((task1.GetAwaiter().GetResult(), task2.GetAwaiter().GetResult(), task3.GetAwaiter().GetResult(), task4.GetAwaiter().GetResult(), task5.GetAwaiter().GetResult(), task6.GetAwaiter().GetResult(), task7.GetAwaiter().GetResult(), task8.GetAwaiter().GetResult(), task9.GetAwaiter().GetResult(), task10.GetAwaiter().GetResult(), task11.GetAwaiter().GetResult()));
        }

        var promise = new WhenAllPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(task1, task2, task3, task4, task5, task6, task7, task8, task9, task10, task11);
        return new LuminTask<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11)>(LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11)>.MethodTable, promise.core, promise.core->Id);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe LuminTask<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12)> WhenAll<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(in LuminTask<T1> task1, in LuminTask<T2> task2, in LuminTask<T3> task3, in LuminTask<T4> task4, in LuminTask<T5> task5, in LuminTask<T6> task6, in LuminTask<T7> task7, in LuminTask<T8> task8, in LuminTask<T9> task9, in LuminTask<T10> task10, in LuminTask<T11> task11, in LuminTask<T12> task12)
    {
        if (task1.Status.IsCompletedSuccessfully() && task2.Status.IsCompletedSuccessfully() && task3.Status.IsCompletedSuccessfully() && task4.Status.IsCompletedSuccessfully() && task5.Status.IsCompletedSuccessfully() && task6.Status.IsCompletedSuccessfully() && task7.Status.IsCompletedSuccessfully() && task8.Status.IsCompletedSuccessfully() && task9.Status.IsCompletedSuccessfully() && task10.Status.IsCompletedSuccessfully() && task11.Status.IsCompletedSuccessfully() && task12.Status.IsCompletedSuccessfully())
        {
            return new LuminTask<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12)>((task1.GetAwaiter().GetResult(), task2.GetAwaiter().GetResult(), task3.GetAwaiter().GetResult(), task4.GetAwaiter().GetResult(), task5.GetAwaiter().GetResult(), task6.GetAwaiter().GetResult(), task7.GetAwaiter().GetResult(), task8.GetAwaiter().GetResult(), task9.GetAwaiter().GetResult(), task10.GetAwaiter().GetResult(), task11.GetAwaiter().GetResult(), task12.GetAwaiter().GetResult()));
        }

        var promise = new WhenAllPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(task1, task2, task3, task4, task5, task6, task7, task8, task9, task10, task11, task12);
        return new LuminTask<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12)>(LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12)>.MethodTable, promise.core, promise.core->Id);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe LuminTask<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13)> WhenAll<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(in LuminTask<T1> task1, in LuminTask<T2> task2, in LuminTask<T3> task3, in LuminTask<T4> task4, in LuminTask<T5> task5, in LuminTask<T6> task6, in LuminTask<T7> task7, in LuminTask<T8> task8, in LuminTask<T9> task9, in LuminTask<T10> task10, in LuminTask<T11> task11, in LuminTask<T12> task12, in LuminTask<T13> task13)
    {
        if (task1.Status.IsCompletedSuccessfully() && task2.Status.IsCompletedSuccessfully() && task3.Status.IsCompletedSuccessfully() && task4.Status.IsCompletedSuccessfully() && task5.Status.IsCompletedSuccessfully() && task6.Status.IsCompletedSuccessfully() && task7.Status.IsCompletedSuccessfully() && task8.Status.IsCompletedSuccessfully() && task9.Status.IsCompletedSuccessfully() && task10.Status.IsCompletedSuccessfully() && task11.Status.IsCompletedSuccessfully() && task12.Status.IsCompletedSuccessfully() && task13.Status.IsCompletedSuccessfully())
        {
            return new LuminTask<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13)>((task1.GetAwaiter().GetResult(), task2.GetAwaiter().GetResult(), task3.GetAwaiter().GetResult(), task4.GetAwaiter().GetResult(), task5.GetAwaiter().GetResult(), task6.GetAwaiter().GetResult(), task7.GetAwaiter().GetResult(), task8.GetAwaiter().GetResult(), task9.GetAwaiter().GetResult(), task10.GetAwaiter().GetResult(), task11.GetAwaiter().GetResult(), task12.GetAwaiter().GetResult(), task13.GetAwaiter().GetResult()));
        }

        var promise = new WhenAllPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(task1, task2, task3, task4, task5, task6, task7, task8, task9, task10, task11, task12, task13);
        return new LuminTask<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13)>(LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13)>.MethodTable, promise.core, promise.core->Id);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe LuminTask<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14)> WhenAll<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(in LuminTask<T1> task1, in LuminTask<T2> task2, in LuminTask<T3> task3, in LuminTask<T4> task4, in LuminTask<T5> task5, in LuminTask<T6> task6, in LuminTask<T7> task7, in LuminTask<T8> task8, in LuminTask<T9> task9, in LuminTask<T10> task10, in LuminTask<T11> task11, in LuminTask<T12> task12, in LuminTask<T13> task13, in LuminTask<T14> task14)
    {
        if (task1.Status.IsCompletedSuccessfully() && task2.Status.IsCompletedSuccessfully() && task3.Status.IsCompletedSuccessfully() && task4.Status.IsCompletedSuccessfully() && task5.Status.IsCompletedSuccessfully() && task6.Status.IsCompletedSuccessfully() && task7.Status.IsCompletedSuccessfully() && task8.Status.IsCompletedSuccessfully() && task9.Status.IsCompletedSuccessfully() && task10.Status.IsCompletedSuccessfully() && task11.Status.IsCompletedSuccessfully() && task12.Status.IsCompletedSuccessfully() && task13.Status.IsCompletedSuccessfully() && task14.Status.IsCompletedSuccessfully())
        {
            return new LuminTask<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14)>((task1.GetAwaiter().GetResult(), task2.GetAwaiter().GetResult(), task3.GetAwaiter().GetResult(), task4.GetAwaiter().GetResult(), task5.GetAwaiter().GetResult(), task6.GetAwaiter().GetResult(), task7.GetAwaiter().GetResult(), task8.GetAwaiter().GetResult(), task9.GetAwaiter().GetResult(), task10.GetAwaiter().GetResult(), task11.GetAwaiter().GetResult(), task12.GetAwaiter().GetResult(), task13.GetAwaiter().GetResult(), task14.GetAwaiter().GetResult()));
        }

        var promise = new WhenAllPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(task1, task2, task3, task4, task5, task6, task7, task8, task9, task10, task11, task12, task13, task14);
        return new LuminTask<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14)>(LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14)>.MethodTable, promise.core, promise.core->Id);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe LuminTask<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15)> WhenAll<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(in LuminTask<T1> task1, in LuminTask<T2> task2, in LuminTask<T3> task3, in LuminTask<T4> task4, in LuminTask<T5> task5, in LuminTask<T6> task6, in LuminTask<T7> task7, in LuminTask<T8> task8, in LuminTask<T9> task9, in LuminTask<T10> task10, in LuminTask<T11> task11, in LuminTask<T12> task12, in LuminTask<T13> task13, in LuminTask<T14> task14, in LuminTask<T15> task15)
    {
        if (task1.Status.IsCompletedSuccessfully() && task2.Status.IsCompletedSuccessfully() && task3.Status.IsCompletedSuccessfully() && task4.Status.IsCompletedSuccessfully() && task5.Status.IsCompletedSuccessfully() && task6.Status.IsCompletedSuccessfully() && task7.Status.IsCompletedSuccessfully() && task8.Status.IsCompletedSuccessfully() && task9.Status.IsCompletedSuccessfully() && task10.Status.IsCompletedSuccessfully() && task11.Status.IsCompletedSuccessfully() && task12.Status.IsCompletedSuccessfully() && task13.Status.IsCompletedSuccessfully() && task14.Status.IsCompletedSuccessfully() && task15.Status.IsCompletedSuccessfully())
        {
            return new LuminTask<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15)>((task1.GetAwaiter().GetResult(), task2.GetAwaiter().GetResult(), task3.GetAwaiter().GetResult(), task4.GetAwaiter().GetResult(), task5.GetAwaiter().GetResult(), task6.GetAwaiter().GetResult(), task7.GetAwaiter().GetResult(), task8.GetAwaiter().GetResult(), task9.GetAwaiter().GetResult(), task10.GetAwaiter().GetResult(), task11.GetAwaiter().GetResult(), task12.GetAwaiter().GetResult(), task13.GetAwaiter().GetResult(), task14.GetAwaiter().GetResult(), task15.GetAwaiter().GetResult()));
        }

        var promise = new WhenAllPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(task1, task2, task3, task4, task5, task6, task7, task8, task9, task10, task11, task12, task13, task14, task15);
        return new LuminTask<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15)>(LuminTaskSourceCore<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15)>.MethodTable, promise.core, promise.core->Id);
    }
}
