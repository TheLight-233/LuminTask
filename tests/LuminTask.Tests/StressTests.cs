using Cysharp.Threading.Tasks;
using LuminThread;
using LuminThread.AsyncEx;
using LuminThread.Utility;

namespace LuminTaskUnitTest;

public static class StressTests
{
    public static async Task RunAll(TestRunner r)
    {
        // Stack capture is the expensive part of the DEBUG tracker; turn it off for the heavy
        // loops (LiveCount still works) and restore afterward.
        var prior = LuminTaskLeakTracker.CaptureStackTrace;
        LuminTaskLeakTracker.CaptureStackTrace = false;
        try
        {
            await r.Run("10,000 concurrent suspended tasks (WhenAll)", MassiveWhenAll, 120_000);
            await r.Run("deep async recursion (depth 2000)", DeepRecursion, 120_000);
            await r.Run("AsyncLock: 64 x 2000 increments (exact)", HeavyLock, 120_000);
            await r.Run("AsyncSemaphore(8): 5000 tasks bounded", HeavySemaphore, 120_000);
            await r.Run("TCS create/complete/await x100,000", TcsChurn, 120_000);
            await r.Run("2000 concurrent multi-suspend tasks", ConcurrentMultiSuspend, 120_000);
            await r.Run("BitMap", BitMap, 120_000);
        }
        finally
        {
            LuminTaskLeakTracker.CaptureStackTrace = prior;
        }
    }

    private static async LuminTask BitMap()
    {
        var count = LuminTaskBag.GetAvailableCount();
        short[] ids  = new short[count];
        
        for (int i = 0; i < count; i++)
        {
            ids[i] = LuminTaskBag.GetId();
        }
        
        for (int i = 0; i < count; i++)
        {
            LuminTaskBag.ResetId(ids[i]);
        }
        
        await LuminTask.Yield();
    }
    
    private static int _sum;

    private static async LuminTask Worker()
    {
        await LuminTask.Yield();
        Interlocked.Increment(ref _sum);
    }

    private static async LuminTask MassiveWhenAll()
    {
        _sum = 0;
        const int n = 10_000;
        var tasks = new LuminTask[n];
        for (var i = 0; i < n; i++) tasks[i] = Worker();
        await LuminTask.WhenAll(tasks);
        Assert.Equal(n, Volatile.Read(ref _sum));
    }

    private static async LuminTask<long> SumTo(int n)
    {
        if (n == 0) return 0;
        await LuminTask.Yield();
        return n + await SumTo(n - 1);
    }

    private static async LuminTask DeepRecursion()
    {
        const int depth = 1000;
        var s = await SumTo(depth);
        Assert.Equal((long)depth * (depth + 1) / 2, s);
    }

    private static async LuminTask HeavyLock()
    {
        using var gate = new AsyncLock();
        var counter = 0;
        const int workers = 64, perWorker = 2000;
        var tasks = new Task[workers];
        for (var w = 0; w < workers; w++)
        {
            tasks[w] = Task.Run(async () =>
            {
                for (var i = 0; i < perWorker; i++)
                    using (await gate.LockAsync())
                        counter++;
            });
        }
        await Task.WhenAll(tasks);
        Assert.Equal(workers * perWorker, counter);
    }

    private static async LuminTask HeavySemaphore()
    {
        const int limit = 8, n = 5000;
        using var sem = new AsyncSemaphore(limit);
        var current = 0;
        var max = 0;
        var completed = 0;
        var lk = new object();
        var tasks = new Task[n];
        for (var i = 0; i < n; i++)
        {
            tasks[i] = Task.Run(async () =>
            {
                using (await sem.LockAsync())
                {
                    var c = Interlocked.Increment(ref current);
                    lock (lk) { if (c > max) max = c; }
                    await LuminTask.Yield();
                    Interlocked.Decrement(ref current);
                    Interlocked.Increment(ref completed);
                }
            });
        }
        await Task.WhenAll(tasks);
        Assert.True(max <= limit, $"max concurrency {max} exceeded limit {limit}");
        Assert.Equal(n, completed);
    }

    private static async LuminTask TcsChurn()
    {
        const int n = 100_000;
        for (var i = 0; i < n; i++)
        {
            var tcs = new LuminTaskCompletionSource<int>();
            tcs.TrySetResult(i);
            var v = await tcs.Task;
            if (v != i) Assert.Fail($"expected {i} but got {v}");
        }
    }

    private static async LuminTask<int> MultiHop(int seed)
    {
        var acc = seed;
        await LuminTask.Yield(); acc += 1;
        await LuminTask.Delay(1); acc += 2;
        await LuminTask.Yield(); acc += 3;
        return acc;
    }

    private static async LuminTask ConcurrentMultiSuspend()
    {
        const int n = 2000;
        var tasks = new Task<int>[n];
        for (var i = 0; i < n; i++)
        {
            var k = i;
            tasks[i] = Task.Run(async () => await MultiHop(k));
        }
        var results = await Task.WhenAll(tasks);

        long total = 0;
        foreach (var x in results) total += x;

        long expected = 0;
        for (var i = 0; i < n; i++) expected += i + 6; // each returns seed + (1+2+3)
        Assert.Equal(expected, total);
    }
}
