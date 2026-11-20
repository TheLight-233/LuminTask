using System.Diagnostics;
using LuminThread;

namespace LuminTaskUnitTest;

public static class CombinatorTests
{
    public static async Task RunAll(TestRunner r)
    {
        await r.Run("Delay waits roughly the requested duration", DelayTiming);
        await r.Run("Yield resumes execution", YieldResumes);
        await r.Run("WhenAll (array) waits for all", WhenAllArray);
        await r.Run("WhenAll (variadic) waits for all", WhenAllVariadic);
        await r.Run("WhenAll surfaces a faulting task", WhenAllFault);
        await r.Run("WhenAll (tuple) aggregates typed results", WhenAllTuple);
        await r.Run("WhenAny (two) returns a valid winner", WhenAnyTwo);
        await r.Run("WhenAny (tuple) reports the winner and its value", WhenAnyTuple);
        await r.Run("WhenAny (array) returns a valid index", WhenAnyArray);
        await r.Run("WhenAll (array) drains suspended children after a fault", WhenAllArrayFaultDrains);
        await r.Run("WhenAny (array) releases the losers' slots", WhenAnyArrayLosersReleased);
        await r.Run("array combinators serve mixed result types from one state", ArrayCombinatorsMixedResults);
        await r.Run("WhenAll (array) mixes inline and suspended children", WhenAllArrayMixedCompletion);
        await r.Run("list combinators finish after their input lists are reused", ReuseInputLists);
        await r.Run("pending races preserve indices across inline and captured registrations", PendingRaceIndices);
#if !DEBUG
        await r.Run("reused input lists allocate no temporary arrays", ListAllocation);
#endif
        await r.Run("Delay honours cancellation", DelayCancellation);
    }

    private static async LuminTask PendingRaceIndices()
    {
        foreach (int winner in new[] { 0, 30, 31, 32 })
        {
            var handles = new ulong[33];
            var tasks = new LuminTask<int>[handles.Length];
            tasks[0] = LuminTaskSource.Create<int>(out handles[0]).ConfigureAwait(true);
            for (int i = 1; i < tasks.Length; i++)
                tasks[i] = LuminTaskSource.Create<int>(out handles[i]);
            var completion = LuminTask.WhenAny(tasks);
            LuminTaskSource.SetResult(handles[winner], 100 + winner);
            Assert.Equal((winner, 100 + winner), await completion);
            for (int i = 0; i < tasks.Length; i++)
                if (i != winner) LuminTaskSource.SetResult(handles[i], i);
        }
    }

    private static async LuminTask ReuseInputLists()
    {
        var values = new List<LuminTask<int>> { LuminTask.FromResult(3), LuminTaskSource.Create<int>(out var first) };
        var all = LuminTask.WhenAll(values);
        values.Clear();
        values.Add(LuminTaskSource.Create<int>(out var late));
        values.Add(LuminTask.FromResult(9));
        var any = LuminTask.WhenAny(values);
        values.Clear();
        LuminTaskSource.SetResult(first, 4);
        var result = await all;
        Assert.Equal(3, result[0]);
        Assert.Equal(4, result[1]);
        Assert.Equal((1, 9), await any);
        LuminTaskSource.SetResult(late, 5);

        var tasks = new List<LuminTask> { LuminTaskSource.Create(out var a) };
        var allVoid = LuminTask.WhenAll(tasks);
        tasks.Clear();
        tasks.Add(LuminTaskSource.Create(out var b));
        tasks.Add(LuminTask.CompletedTask());
        var anyVoid = LuminTask.WhenAny(tasks);
        tasks.Clear();
        LuminTaskSource.SetResult(a);
        await allVoid;
        Assert.Equal(1, await anyVoid);
        LuminTaskSource.SetResult(b);
    }

#if !DEBUG
    private static void ListAllocation()
    {
        var group = new List<LuminTask> { LuminTask.CompletedTask(), LuminTask.CompletedTask() };
        var race = new List<LuminTask<int>> { LuminTask.FromResult(3), LuminTask.FromResult(7) };
        void Run()
        {
            LuminTask.WhenAll(group).GetResult();
            LuminTask.WhenAny(race).GetResult();
        }
        for (int i = 0; i < 64; i++) Run();
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < 128; i++) Run();
        Assert.Equal(0L, GC.GetAllocatedBytesForCurrentThread() - before);
    }
#endif

    private static async LuminTask DelayTiming()
    {
        var sw = Stopwatch.StartNew();
        await LuminTask.Delay(120);
        sw.Stop();
        Assert.True(sw.ElapsedMilliseconds >= 90,
            $"Delay(120) returned too early: {sw.ElapsedMilliseconds}ms");
    }

    private static async LuminTask YieldResumes()
    {
        var before = Environment.CurrentManagedThreadId;
        await LuminTask.Yield();
        // We don't assert the thread changed (scheduler-dependent), only that we resumed.
        Assert.True(before != 0);
    }

    private static async LuminTask WhenAllArray()
    {
        _counter = 0;
        const int n = 64;
        var tasks = new LuminTask[n];
        for (var i = 0; i < n; i++) tasks[i] = Bump();
        await LuminTask.WhenAll(tasks);
        Assert.Equal(n, Volatile.Read(ref _counter));
    }

    private static async LuminTask WhenAllVariadic()
    {
        _counter = 0;
        await LuminTask.WhenAll(Bump(), Bump(), Bump());
        Assert.Equal(3, Volatile.Read(ref _counter));
    }

    private static LuminTask WhenAllFault()
        => Assert.ThrowsAsync<InvalidOperationException>(() =>
            LuminTask.WhenAll(Ok(), Boom(), Ok()));

    private static async LuminTask WhenAllTuple()
    {
        var (a, b, c) = await LuminTask.WhenAll(DelayValue(30, 1), DelayValue(10, 2), DelayValue(50, 3));
        Assert.True(a == 1 && b == 2 && c == 3, $"unexpected tuple ({a}, {b}, {c})");

        // Externally (synchronously) completed inputs take the fast path.
        var (x, y) = await LuminTask.WhenAll(LuminTask.FromResult(7), LuminTask.FromResult(8));
        Assert.True(x == 7 && y == 8, $"unexpected sync tuple ({x}, {y})");
    }

    private static async LuminTask WhenAnyTuple()
    {
        var (index, r1, r2) = await LuminTask.WhenAny(DelayValue(200, 1), DelayValue(20, 2));
        Assert.True(index is 0 or 1, $"unexpected winner index {index}");

        var winnerValue = index == 0 ? r1 : r2;
        var expectedValue = index == 0 ? 1 : 2;
        Assert.Equal(expectedValue, winnerValue);
    }

    private static async LuminTask<int> DelayValue(int milliseconds, int value)
    {
        await LuminTask.Delay(milliseconds);
        return value;
    }

    private static async LuminTask WhenAnyTwo()
    {
        var idx = await LuminTask.WhenAny(LuminTask.Delay(150), LuminTask.Delay(10));
        Assert.True(idx is 0 or 1, $"unexpected winner index {idx}");
    }

    private static async LuminTask WhenAnyArray()
    {
        var tasks = new[] { LuminTask.Delay(80), LuminTask.Delay(20), LuminTask.Delay(120) };
        var idx = await LuminTask.WhenAny(tasks);
        Assert.True(idx is >= 0 and < 3, $"index out of range: {idx}");
    }

    private readonly record struct Payload(int Number, string Text);

    // 数组组合子的挂起孩子现在只把 (owner, 句柄, 下标) 交给共享续体状态，由 owner 从句柄重建
    // 带 T 的任务。以下四项分别回归：失败后其余孩子仍会被消费、WhenAny 失败者被 Forget、
    // 不同结果类型共用同一个池仍然正确、内联完成与挂起完成混在一起时计数正确。
    private static async LuminTask WhenAllArrayFaultDrains()
    {
        var liveBefore = LuminTaskBag.GetLiveCount();

        var sources = new LuminTaskCompletionSource[4];
        var tasks = new LuminTask[4];
        for (var i = 0; i < sources.Length; i++)
        {
            sources[i] = new LuminTaskCompletionSource();
            tasks[i] = sources[i].Task;
        }

        var whenAll = LuminTask.WhenAll(tasks);

        sources[1].TrySetException(new InvalidOperationException("boom"));
        await Assert.ThrowsAsync<InvalidOperationException>(async () => await whenAll);

        // 迟到的孩子仍然由自己的续体消费，槽位不能留在池里。
        sources[0].TrySetResult();
        sources[2].TrySetResult();
        sources[3].TrySetResult();

        Assert.Equal(liveBefore, LuminTaskBag.GetLiveCount(), "faulted array WhenAll left slots rented");
    }

    private static async LuminTask WhenAnyArrayLosersReleased()
    {
        var liveBefore = LuminTaskBag.GetLiveCount();

        var sources = new LuminTaskCompletionSource<int>[3];
        var tasks = new LuminTask<int>[3];
        for (var i = 0; i < sources.Length; i++)
        {
            sources[i] = new LuminTaskCompletionSource<int>();
            tasks[i] = sources[i].Task;
        }

        var whenAny = LuminTask.WhenAny(tasks);

        sources[0].TrySetResult(42);
        var (index, value) = await whenAny;
        Assert.Equal(0, index);
        Assert.Equal(42, value);

        sources[1].TrySetResult(1);
        sources[2].TrySetResult(2);

        Assert.Equal(liveBefore, LuminTaskBag.GetLiveCount(), "WhenAny losers were not released");
    }

    private static async LuminTask ArrayCombinatorsMixedResults()
    {
        var liveBefore = LuminTaskBag.GetLiveCount();

        for (var round = 0; round < 100; round++)
        {
            var i0 = new LuminTaskCompletionSource<int>();
            var i1 = new LuminTaskCompletionSource<int>();
            var intAll = LuminTask.WhenAll(new[] { i0.Task, i1.Task });
            i1.TrySetResult(20);
            i0.TrySetResult(22);
            var ints = await intAll;
            Assert.Equal(42, ints[0] + ints[1], "int results crossed");

            var s0 = new LuminTaskCompletionSource<string>();
            var s1 = new LuminTaskCompletionSource<string>();
            var strAll = LuminTask.WhenAll(new[] { s0.Task, s1.Task });
            s0.TrySetResult("a");
            s1.TrySetResult("b");
            var strs = await strAll;
            Assert.True(strs[0] == "a" && strs[1] == "b", "string results crossed");

            // 含引用结构体走池化结果容器，与上面的 T 共用同一份续体状态。
            var r0 = new LuminTaskCompletionSource<Payload>();
            var r1 = new LuminTaskCompletionSource<Payload>();
            var payloadAll = LuminTask.WhenAll(new[] { r0.Task, r1.Task });
            r1.TrySetResult(new Payload(2, "b"));
            r0.TrySetResult(new Payload(1, "a"));
            var payloads = await payloadAll;
            Assert.True(payloads[0].Number == 1 && payloads[1].Number == 2, "struct results crossed");

            // WhenAny 数组同样按 T 复用：赢家带值，失败者被消费。
            var w0 = new LuminTaskCompletionSource<string>();
            var w1 = new LuminTaskCompletionSource<string>();
            var anyTask = LuminTask.WhenAny(new[] { w0.Task, w1.Task });
            w1.TrySetResult("winner");
            var (winnerIndex, winnerValue) = await anyTask;
            w0.TrySetResult("loser");
            Assert.Equal(1, winnerIndex);
            Assert.True(winnerValue == "winner", "string winner value crossed");
        }

        Assert.Equal(liveBefore, LuminTaskBag.GetLiveCount(), "mixed result types leaked slots");
    }

    private static async LuminTask WhenAllArrayMixedCompletion()
    {
        var liveBefore = LuminTaskBag.GetLiveCount();

        var src = new LuminTaskCompletionSource();
        var tasks = new[] { LuminTask.CompletedTask(), src.Task, LuminTask.CompletedTask() };

        var whenAll = LuminTask.WhenAll(tasks);
        src.TrySetResult();
        await whenAll;

        Assert.Equal(liveBefore, LuminTaskBag.GetLiveCount(), "inline and suspended mix leaked slots");
    }

    private static LuminTask DelayCancellation()
        => Assert.ThrowsAsync<OperationCanceledException>(async () =>
        {
            using var cts = new CancellationTokenSource();
            var task = LuminTask.Delay(5000, cancellationToken: cts.Token);
            cts.CancelAfter(20);
            await task;
        });

    private static int _counter;

    private static async LuminTask Bump()
    {
        await LuminTask.Yield();
        Interlocked.Increment(ref _counter);
    }

    private static async LuminTask Ok() => await LuminTask.Yield();

    private static async LuminTask Boom()
    {
        await LuminTask.Yield();
        throw new InvalidOperationException("when-all-fault");
    }
}
