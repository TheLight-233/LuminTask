using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using LuminThread.TaskSource;
using LuminThread.TaskSource.Promise;

namespace LuminThread;

public readonly partial struct LuminTask
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe LuminTask<int> WhenAny(LuminTask[]? tasks)
    {
        if (tasks is null || tasks.Length == 0)
            return LuminTask<int>.FromResult(0);
        
        var promise = new WhenAnyPromise(tasks);
        return new LuminTask<int>(LuminTaskSourceCore<int>.MethodTable, promise._core, promise._core->Id);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe LuminTask<int> WhenAny(IEnumerable<LuminTask>? tasks)
    {
        if (tasks is null)
            return LuminTask<int>.FromResult(0);
        
        LuminTask[] array;
        
        if (tasks is LuminTask[] tasksArray)
        {
            array = tasksArray;
        }
        else
        {
            array = tasks.ToArray();
        }
        
        if (array.Length == 0)
            return LuminTask<int>.FromResult(0);
        
        var promise = new WhenAnyPromise(array);
        return new LuminTask<int>(LuminTaskSourceCore<int>.MethodTable, promise._core, promise._core->Id);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe LuminTask<(int winArgumentIndex, T result)> WhenAny<T>(LuminTask<T>[]? tasks)
    {
        if (tasks is null || tasks.Length == 0)
            return LuminTask<(int winArgumentIndex, T result)>.FromResult(default);
        
        var promise = new WhenAnyPromise<T>(tasks);
        return new LuminTask<(int winArgumentIndex, T result)>(LuminTaskSourceCore<(int, T)>.MethodTable, promise._core, promise._core->Id);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe LuminTask<(int winArgumentIndex, T result)> WhenAny<T>(IEnumerable<LuminTask<T>>? tasks)
    {
        if (tasks is null)
            return LuminTask<(int winArgumentIndex, T result)>.FromResult(default);

        LuminTask<T>[] array;
        
        if (tasks is LuminTask<T>[] tasksArray)
        {
            array = tasksArray;
        }
        else
        {
            array = tasks.ToArray();
        }
        
        if (array.Length == 0)
            return LuminTask<(int winArgumentIndex, T result)>.FromResult(default);
        
        var promise = new WhenAnyPromise<T>(array);
        return new LuminTask<(int winArgumentIndex, T result)>(LuminTaskSourceCore<(int, T)>.MethodTable, promise._core, promise._core->Id);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe LuminTask<(int winArgumentIndex, T1 result1, T2 result2)> WhenAny<T1, T2>(LuminTask<T1> task1, LuminTask<T2> task2)
    {
        var promise = new WhenAnyPromise<T1, T2>(task1, task2);
        return new LuminTask<(int winArgumentIndex, T1 result1, T2 result2)>(LuminTaskSourceCore<(int, T1, T2)>.MethodTable, promise.core, promise.core->Id);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe LuminTask<(int winArgumentIndex, T1 result1, T2 result2, T3 result3)> WhenAny<T1, T2, T3>(LuminTask<T1> task1, LuminTask<T2> task2, LuminTask<T3> task3)
    {
        var promise = new WhenAnyPromise<T1, T2, T3>(task1, task2, task3);
        return new LuminTask<(int winArgumentIndex, T1 result1, T2 result2, T3 result3)>(LuminTaskSourceCore<(int, T1, T2, T3)>.MethodTable, promise.core, promise.core->Id);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe LuminTask<(int winArgumentIndex, T1 result1, T2 result2, T3 result3, T4 result4)> WhenAny<T1, T2, T3, T4>(LuminTask<T1> task1, LuminTask<T2> task2, LuminTask<T3> task3, LuminTask<T4> task4)
    {
        var promise = new WhenAnyPromise<T1, T2, T3, T4>(task1, task2, task3, task4);
        return new LuminTask<(int winArgumentIndex, T1 result1, T2 result2, T3 result3, T4 result4)>(LuminTaskSourceCore<(int, T1, T2, T3, T4)>.MethodTable, promise.core, promise.core->Id);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe LuminTask<(int winArgumentIndex, T1 result1, T2 result2, T3 result3, T4 result4, T5 result5)> WhenAny<T1, T2, T3, T4, T5>(LuminTask<T1> task1, LuminTask<T2> task2, LuminTask<T3> task3, LuminTask<T4> task4, LuminTask<T5> task5)
    {
        var promise = new WhenAnyPromise<T1, T2, T3, T4, T5>(task1, task2, task3, task4, task5);
        return new LuminTask<(int winArgumentIndex, T1 result1, T2 result2, T3 result3, T4 result4, T5 result5)>(LuminTaskSourceCore<(int, T1, T2, T3, T4, T5)>.MethodTable, promise.core, promise.core->Id);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe LuminTask<(int winArgumentIndex, T1 result1, T2 result2, T3 result3, T4 result4, T5 result5, T6 result6)> WhenAny<T1, T2, T3, T4, T5, T6>(LuminTask<T1> task1, LuminTask<T2> task2, LuminTask<T3> task3, LuminTask<T4> task4, LuminTask<T5> task5, LuminTask<T6> task6)
    {
        var promise = new WhenAnyPromise<T1, T2, T3, T4, T5, T6>(task1, task2, task3, task4, task5, task6);
        return new LuminTask<(int winArgumentIndex, T1 result1, T2 result2, T3 result3, T4 result4, T5 result5, T6 result6)>(LuminTaskSourceCore<(int, T1, T2, T3, T4, T5, T6)>.MethodTable, promise.core, promise.core->Id);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe LuminTask<(int winArgumentIndex, T1 result1, T2 result2, T3 result3, T4 result4, T5 result5, T6 result6, T7 result7)> WhenAny<T1, T2, T3, T4, T5, T6, T7>(LuminTask<T1> task1, LuminTask<T2> task2, LuminTask<T3> task3, LuminTask<T4> task4, LuminTask<T5> task5, LuminTask<T6> task6, LuminTask<T7> task7)
    {
        var promise = new WhenAnyPromise<T1, T2, T3, T4, T5, T6, T7>(task1, task2, task3, task4, task5, task6, task7);
        return new LuminTask<(int winArgumentIndex, T1 result1, T2 result2, T3 result3, T4 result4, T5 result5, T6 result6, T7 result7)>(LuminTaskSourceCore<(int, T1, T2, T3, T4, T5, T6, T7)>.MethodTable, promise.core, promise.core->Id);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe LuminTask<(int winArgumentIndex, T1 result1, T2 result2, T3 result3, T4 result4, T5 result5, T6 result6, T7 result7, T8 result8)> WhenAny<T1, T2, T3, T4, T5, T6, T7, T8>(LuminTask<T1> task1, LuminTask<T2> task2, LuminTask<T3> task3, LuminTask<T4> task4, LuminTask<T5> task5, LuminTask<T6> task6, LuminTask<T7> task7, LuminTask<T8> task8)
    {
        var promise = new WhenAnyPromise<T1, T2, T3, T4, T5, T6, T7, T8>(task1, task2, task3, task4, task5, task6, task7, task8);
        return new LuminTask<(int winArgumentIndex, T1 result1, T2 result2, T3 result3, T4 result4, T5 result5, T6 result6, T7 result7, T8 result8)>(LuminTaskSourceCore<(int, T1, T2, T3, T4, T5, T6, T7, T8)>.MethodTable, promise.core, promise.core->Id);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe LuminTask<(int winArgumentIndex, T1 result1, T2 result2, T3 result3, T4 result4, T5 result5, T6 result6, T7 result7, T8 result8, T9 result9)> WhenAny<T1, T2, T3, T4, T5, T6, T7, T8, T9>(LuminTask<T1> task1, LuminTask<T2> task2, LuminTask<T3> task3, LuminTask<T4> task4, LuminTask<T5> task5, LuminTask<T6> task6, LuminTask<T7> task7, LuminTask<T8> task8, LuminTask<T9> task9)
    {
        var promise = new WhenAnyPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9>(task1, task2, task3, task4, task5, task6, task7, task8, task9);
        return new LuminTask<(int winArgumentIndex, T1 result1, T2 result2, T3 result3, T4 result4, T5 result5, T6 result6, T7 result7, T8 result8, T9 result9)>(LuminTaskSourceCore<(int, T1, T2, T3, T4, T5, T6, T7, T8, T9)>.MethodTable, promise.core, promise.core->Id);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe LuminTask<(int winArgumentIndex, T1 result1, T2 result2, T3 result3, T4 result4, T5 result5, T6 result6, T7 result7, T8 result8, T9 result9, T10 result10)> WhenAny<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(LuminTask<T1> task1, LuminTask<T2> task2, LuminTask<T3> task3, LuminTask<T4> task4, LuminTask<T5> task5, LuminTask<T6> task6, LuminTask<T7> task7, LuminTask<T8> task8, LuminTask<T9> task9, LuminTask<T10> task10)
    {
        var promise = new WhenAnyPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(task1, task2, task3, task4, task5, task6, task7, task8, task9, task10);
        return new LuminTask<(int winArgumentIndex, T1 result1, T2 result2, T3 result3, T4 result4, T5 result5, T6 result6, T7 result7, T8 result8, T9 result9, T10 result10)>(LuminTaskSourceCore<(int, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10)>.MethodTable, promise.core, promise.core->Id);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe LuminTask<(int winArgumentIndex, T1 result1, T2 result2, T3 result3, T4 result4, T5 result5, T6 result6, T7 result7, T8 result8, T9 result9, T10 result10, T11 result11)> WhenAny<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(LuminTask<T1> task1, LuminTask<T2> task2, LuminTask<T3> task3, LuminTask<T4> task4, LuminTask<T5> task5, LuminTask<T6> task6, LuminTask<T7> task7, LuminTask<T8> task8, LuminTask<T9> task9, LuminTask<T10> task10, LuminTask<T11> task11)
    {
        var promise = new WhenAnyPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(task1, task2, task3, task4, task5, task6, task7, task8, task9, task10, task11);
        return new LuminTask<(int winArgumentIndex, T1 result1, T2 result2, T3 result3, T4 result4, T5 result5, T6 result6, T7 result7, T8 result8, T9 result9, T10 result10, T11 result11)>(LuminTaskSourceCore<(int, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11)>.MethodTable, promise.core, promise.core->Id);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe LuminTask<(int winArgumentIndex, T1 result1, T2 result2, T3 result3, T4 result4, T5 result5, T6 result6, T7 result7, T8 result8, T9 result9, T10 result10, T11 result11, T12 result12)> WhenAny<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(LuminTask<T1> task1, LuminTask<T2> task2, LuminTask<T3> task3, LuminTask<T4> task4, LuminTask<T5> task5, LuminTask<T6> task6, LuminTask<T7> task7, LuminTask<T8> task8, LuminTask<T9> task9, LuminTask<T10> task10, LuminTask<T11> task11, LuminTask<T12> task12)
    {
        var promise = new WhenAnyPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(task1, task2, task3, task4, task5, task6, task7, task8, task9, task10, task11, task12);
        return new LuminTask<(int winArgumentIndex, T1 result1, T2 result2, T3 result3, T4 result4, T5 result5, T6 result6, T7 result7, T8 result8, T9 result9, T10 result10, T11 result11, T12 result12)>(LuminTaskSourceCore<(int, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12)>.MethodTable, promise.core, promise.core->Id);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe LuminTask<(int winArgumentIndex, T1 result1, T2 result2, T3 result3, T4 result4, T5 result5, T6 result6, T7 result7, T8 result8, T9 result9, T10 result10, T11 result11, T12 result12, T13 result13)> WhenAny<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(LuminTask<T1> task1, LuminTask<T2> task2, LuminTask<T3> task3, LuminTask<T4> task4, LuminTask<T5> task5, LuminTask<T6> task6, LuminTask<T7> task7, LuminTask<T8> task8, LuminTask<T9> task9, LuminTask<T10> task10, LuminTask<T11> task11, LuminTask<T12> task12, LuminTask<T13> task13)
    {
        var promise = new WhenAnyPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(task1, task2, task3, task4, task5, task6, task7, task8, task9, task10, task11, task12, task13);
        return new LuminTask<(int winArgumentIndex, T1 result1, T2 result2, T3 result3, T4 result4, T5 result5, T6 result6, T7 result7, T8 result8, T9 result9, T10 result10, T11 result11, T12 result12, T13 result13)>(LuminTaskSourceCore<(int, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13)>.MethodTable, promise.core, promise.core->Id);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe LuminTask<(int winArgumentIndex, T1 result1, T2 result2, T3 result3, T4 result4, T5 result5, T6 result6, T7 result7, T8 result8, T9 result9, T10 result10, T11 result11, T12 result12, T13 result13, T14 result14)> WhenAny<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(LuminTask<T1> task1, LuminTask<T2> task2, LuminTask<T3> task3, LuminTask<T4> task4, LuminTask<T5> task5, LuminTask<T6> task6, LuminTask<T7> task7, LuminTask<T8> task8, LuminTask<T9> task9, LuminTask<T10> task10, LuminTask<T11> task11, LuminTask<T12> task12, LuminTask<T13> task13, LuminTask<T14> task14)
    {
        var promise = new WhenAnyPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(task1, task2, task3, task4, task5, task6, task7, task8, task9, task10, task11, task12, task13, task14);
        return new LuminTask<(int winArgumentIndex, T1 result1, T2 result2, T3 result3, T4 result4, T5 result5, T6 result6, T7 result7, T8 result8, T9 result9, T10 result10, T11 result11, T12 result12, T13 result13, T14 result14)>(LuminTaskSourceCore<(int, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14)>.MethodTable, promise.core, promise.core->Id);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe LuminTask<(int winArgumentIndex, T1 result1, T2 result2, T3 result3, T4 result4, T5 result5, T6 result6, T7 result7, T8 result8, T9 result9, T10 result10, T11 result11, T12 result12, T13 result13, T14 result14, T15 result15)> WhenAny<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(LuminTask<T1> task1, LuminTask<T2> task2, LuminTask<T3> task3, LuminTask<T4> task4, LuminTask<T5> task5, LuminTask<T6> task6, LuminTask<T7> task7, LuminTask<T8> task8, LuminTask<T9> task9, LuminTask<T10> task10, LuminTask<T11> task11, LuminTask<T12> task12, LuminTask<T13> task13, LuminTask<T14> task14, LuminTask<T15> task15)
    {
        var promise = new WhenAnyPromise<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(task1, task2, task3, task4, task5, task6, task7, task8, task9, task10, task11, task12, task13, task14, task15);
        return new LuminTask<(int winArgumentIndex, T1 result1, T2 result2, T3 result3, T4 result4, T5 result5, T6 result6, T7 result7, T8 result8, T9 result9, T10 result10, T11 result11, T12 result12, T13 result13, T14 result14, T15 result15)>(LuminTaskSourceCore<(int, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15)>.MethodTable, promise.core, promise.core->Id);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe LuminTask<int> WhenAny(LuminTask task1, LuminTask task2)
    {
        var promise = new WhenAnyPromise2(task1, task2);
        return new LuminTask<int>(LuminTaskSourceCore<int>.MethodTable, promise._core, promise._core->Id);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe LuminTask<int> WhenAny(LuminTask task1, LuminTask task2, LuminTask task3)
    {
        var promise = new WhenAnyPromise3(task1, task2, task3);
        return new LuminTask<int>(LuminTaskSourceCore<int>.MethodTable, promise._core, promise._core->Id);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe LuminTask<int> WhenAny(LuminTask task1, LuminTask task2, LuminTask task3, LuminTask task4)
    {
        var promise = new WhenAnyPromise4(task1, task2, task3, task4);
        return new LuminTask<int>(LuminTaskSourceCore<int>.MethodTable, promise._core, promise._core->Id);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe LuminTask<int> WhenAny(LuminTask task1, LuminTask task2, LuminTask task3, LuminTask task4, LuminTask task5)
    {
        var promise = new WhenAnyPromise5(task1, task2, task3, task4, task5);
        return new LuminTask<int>(LuminTaskSourceCore<int>.MethodTable, promise._core, promise._core->Id);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe LuminTask<int> WhenAny(LuminTask task1, LuminTask task2, LuminTask task3, LuminTask task4, LuminTask task5, LuminTask task6)
    {
        var promise = new WhenAnyPromise6(task1, task2, task3, task4, task5, task6);
        return new LuminTask<int>(LuminTaskSourceCore<int>.MethodTable, promise._core, promise._core->Id);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe LuminTask<int> WhenAny(LuminTask task1, LuminTask task2, LuminTask task3, LuminTask task4, LuminTask task5, LuminTask task6, LuminTask task7)
    {
        var promise = new WhenAnyPromise7(task1, task2, task3, task4, task5, task6, task7);
        return new LuminTask<int>(LuminTaskSourceCore<int>.MethodTable, promise._core, promise._core->Id);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe LuminTask<int> WhenAny(LuminTask task1, LuminTask task2, LuminTask task3, LuminTask task4, LuminTask task5, LuminTask task6, LuminTask task7, LuminTask task8)
    {
        var promise = new WhenAnyPromise8(task1, task2, task3, task4, task5, task6, task7, task8);
        return new LuminTask<int>(LuminTaskSourceCore<int>.MethodTable, promise._core, promise._core->Id);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe LuminTask<int> WhenAny(LuminTask task1, LuminTask task2, LuminTask task3, LuminTask task4, LuminTask task5, LuminTask task6, LuminTask task7, LuminTask task8, LuminTask task9)
    {
        var promise = new WhenAnyPromise9(task1, task2, task3, task4, task5, task6, task7, task8, task9);
        return new LuminTask<int>(LuminTaskSourceCore<int>.MethodTable, promise._core, promise._core->Id);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe LuminTask<int> WhenAny(LuminTask task1, LuminTask task2, LuminTask task3, LuminTask task4, LuminTask task5, LuminTask task6, LuminTask task7, LuminTask task8, LuminTask task9, LuminTask task10)
    {
        var promise = new WhenAnyPromise10(task1, task2, task3, task4, task5, task6, task7, task8, task9, task10);
        return new LuminTask<int>(LuminTaskSourceCore<int>.MethodTable, promise._core, promise._core->Id);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe LuminTask<int> WhenAny(LuminTask task1, LuminTask task2, LuminTask task3, LuminTask task4, LuminTask task5, LuminTask task6, LuminTask task7, LuminTask task8, LuminTask task9, LuminTask task10, LuminTask task11)
    {
        var promise = new WhenAnyPromise11(task1, task2, task3, task4, task5, task6, task7, task8, task9, task10, task11);
        return new LuminTask<int>(LuminTaskSourceCore<int>.MethodTable, promise._core, promise._core->Id);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe LuminTask<int> WhenAny(LuminTask task1, LuminTask task2, LuminTask task3, LuminTask task4, LuminTask task5, LuminTask task6, LuminTask task7, LuminTask task8, LuminTask task9, LuminTask task10, LuminTask task11, LuminTask task12)
    {
        var promise = new WhenAnyPromise12(task1, task2, task3, task4, task5, task6, task7, task8, task9, task10, task11, task12);
        return new LuminTask<int>(LuminTaskSourceCore<int>.MethodTable, promise._core, promise._core->Id);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe LuminTask<int> WhenAny(LuminTask task1, LuminTask task2, LuminTask task3, LuminTask task4, LuminTask task5, LuminTask task6, LuminTask task7, LuminTask task8, LuminTask task9, LuminTask task10, LuminTask task11, LuminTask task12, LuminTask task13)
    {
        var promise = new WhenAnyPromise13(task1, task2, task3, task4, task5, task6, task7, task8, task9, task10, task11, task12, task13);
        return new LuminTask<int>(LuminTaskSourceCore<int>.MethodTable, promise._core, promise._core->Id);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe LuminTask<int> WhenAny(LuminTask task1, LuminTask task2, LuminTask task3, LuminTask task4, LuminTask task5, LuminTask task6, LuminTask task7, LuminTask task8, LuminTask task9, LuminTask task10, LuminTask task11, LuminTask task12, LuminTask task13, LuminTask task14)
    {
        var promise = new WhenAnyPromise14(task1, task2, task3, task4, task5, task6, task7, task8, task9, task10, task11, task12, task13, task14);
        return new LuminTask<int>(LuminTaskSourceCore<int>.MethodTable, promise._core, promise._core->Id);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe LuminTask<int> WhenAny(LuminTask task1, LuminTask task2, LuminTask task3, LuminTask task4, LuminTask task5, LuminTask task6, LuminTask task7, LuminTask task8, LuminTask task9, LuminTask task10, LuminTask task11, LuminTask task12, LuminTask task13, LuminTask task14, LuminTask task15)
    {
        var promise = new WhenAnyPromise15(task1, task2, task3, task4, task5, task6, task7, task8, task9, task10, task11, task12, task13, task14, task15);
        return new LuminTask<int>(LuminTaskSourceCore<int>.MethodTable, promise._core, promise._core->Id);
    }
}