using System.Reflection;
using System.Runtime.InteropServices;
using LuminThread;
using LuminThread.Utility;

namespace LuminTaskUnitTest;

public static class ResultStorageTests
{
    public static async Task RunAll(TestRunner runner)
    {
        await runner.Run("inline and typed managed results round-trip", ResultLayoutBoundary);
        await runner.Run("mixed result kinds survive item reuse", MixedResults);
        await runner.Run("suspended results survive cross-thread completion", CrossThreadResults);
        await runner.Run("loop state shares managed result storage", LoopState);
        await runner.Run("cancellation registration can race completion", CancellationRegistrationRace);
        await runner.Run("empty pages are reclaimed with another page still live", IdleColdStorage);
        await runner.Run("zero inline result keeps its cold block alive", ZeroResultSurvivesTrim);
        await runner.Run("page allocator preserves exclusive ownership under contention", ConcurrentRentalAndTrim);
        await runner.Run("page allocator reaches its full 16-bit capacity", FullCapacity);
        await runner.Run("trimming an idle source drops every page without reviving handles", TrimDirectory);
        await runner.Run("exhausted generations retire without leaks or stale aliases", RetiredGeneration);
        await runner.Run("pending and duplicate consumption are rejected", ConsumeRequiresTerminalState);
        await runner.Run("shared cache respects the global byte cap", StatePoolBound);
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private readonly record struct ThreeBytes(byte A, short B);

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private readonly record struct NineBytes(long A, byte B);

    private readonly record struct SixteenBytes(long A, long B);

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private readonly record struct SeventeenBytes(long A, long B, byte C);

    private readonly record struct Large(long A, long B, long C, long D, long E, long F, long G, long H, long I);
    private readonly record struct WithReference(string Text, int Number);
    private readonly record struct PoolBoundValue(int Number);
    private sealed class DisposableResult : IDisposable
    {
        public bool Disposed;
        public void Dispose() => Disposed = true;
    }

    private static void ResultLayoutBoundary()
    {
        RoundTrip(0);
        RoundTrip(-1);
        RoundTrip(byte.MaxValue);
        RoundTrip(short.MinValue);
        RoundTrip(new ThreeBytes(0xAB, short.MinValue));
        RoundTrip(float.NegativeInfinity);
        RoundTrip(long.MinValue);
        RoundTrip(BitConverter.Int64BitsToDouble(unchecked((long)0xFFF0123456789ABC)));
        RoundTrip(new NineBytes(long.MinValue, 0xCD));
        RoundTrip(new SixteenBytes(long.MinValue, long.MaxValue));
        RoundTrip(new SeventeenBytes(long.MinValue, long.MaxValue, 0xCD));
        RoundTrip(new Large(1, 2, 3, 4, 5, 6, 7, 8, long.MinValue));
    }

    private static void RoundTrip<T>(T expected)
    {
        var task = LuminTaskSource.Create<T>(out var handle);
        Assert.True(LuminTaskSource.SetResult(handle, expected));
        Assert.Equal(expected, task.GetAwaiter().GetResult());
        Assert.False(LuminTaskSource.SetResult(handle, expected), "stale producer must be rejected");
    }

    private static async LuminTask<T> AwaitResult<T>(LuminTask<T> task) => await task;

    private static void MixedResults()
    {
        int live = LuminTaskBag.GetLiveCount();
        var disposable = new DisposableResult();
        for (int i = 0; i < 1000; i++)
        {
            RoundTrip(0);
            RoundTrip(new NineBytes(-i, 0xEF));
            RoundTrip(new SeventeenBytes(-i, long.MaxValue, 0xAB));
            RoundTrip(string.Empty);
            RoundTrip<string?>(null);
            RoundTrip(new WithReference("kept alive", i));
            RoundTrip(disposable);

            var source = LuminTaskSource.Create<long>(out var handle);
            var task = AwaitResult(source);
            long expected = long.MinValue + i;
            Assert.True(LuminTaskSource.SetResult(handle, expected));
            Assert.Equal(expected, task.GetAwaiter().GetResult());
        }
        Assert.False(disposable.Disposed, "user results must not be disposed by the library");
        Assert.Equal(live, LuminTaskBag.GetLiveCount());
    }

    private static async LuminTask CrossThreadResults()
    {
        for (int round = 0; round < 20; round++)
        {
            var handles = new ulong[128];
            var tasks = new LuminTask<long>[handles.Length];
            for (int i = 0; i < handles.Length; i++)
                tasks[i] = AwaitResult(LuminTaskSource.Create<long>(out handles[i]));

            var producer = Task.Run(() => Parallel.For(0, handles.Length, i =>
                Assert.True(LuminTaskSource.SetResult(handles[i], long.MinValue + i))));
            for (int i = 0; i < tasks.Length; i++)
                Assert.Equal(long.MinValue + i, await tasks[i]);
            await producer;
        }
    }

    private static async LuminTask CancellationRegistrationRace()
    {
        for (int i = 0; i < 1000; i++)
        {
            using var cts = new CancellationTokenSource();
            var cancel = Task.Run(cts.Cancel);
            var task = LuminTask.Never(cts.Token);
            try
            {
                await task;
                Assert.Fail("expected cancellation");
            }
            catch (OperationCanceledException exception)
            {
                Assert.Equal(cts.Token, exception.CancellationToken);
            }
            await cancel;
        }
    }

    private static async LuminTask LoopState()
    {
        await LuminTask.WaitUntil(0, static value => { Assert.Equal(0, value); return true; });
        await LuminTask.WaitWhile(long.MinValue, static value => { Assert.Equal(long.MinValue, value); return false; });
        await LuminTask.WaitUntil(new NineBytes(-123, 0xEF), static value =>
        {
            Assert.Equal(new NineBytes(-123, 0xEF), value);
            return true;
        });
        await LuminTask.WaitUntil(new WithReference("state", 123), static value =>
        {
            Assert.Equal(new WithReference("state", 123), value);
            return true;
        });

        using var cts = new CancellationTokenSource();
        var canceled = LuminTask.WaitUntil(0, static _ => false,
            cancellationToken: cts.Token, cancelImmediately: true);
        cts.Cancel();
        try
        {
            await canceled;
            Assert.Fail("expected cancellation");
        }
        catch (OperationCanceledException exception)
        {
            Assert.Equal(cts.Token, exception.CancellationToken);
        }
    }

    private static void IdleColdStorage()
    {
        LuminTaskSource.Trim();
        var held = LuminTaskSource.Create<string>(out var live);
        var handles = new ulong[128];
        for (int i = 0; i < handles.Length; i++) LuminTaskSource.Rent(out handles[i]);
        foreach (ulong h in handles) { LuminTaskSource.SetResult(h); LuminTaskSource.ToTask(h).GetResult(); }
        int peak = LuminTaskBag.Capacity;
        LuminTaskSource.Trim();
        Assert.True(LuminTaskBag.Capacity < peak);
        Assert.True(LuminTaskSource.SetResult(live, "kept"));
        Assert.Equal("kept", held.GetResult());
        var liveHandles = new ulong[handles.Length];
        for (int i = 0; i < handles.Length; i++) LuminTaskSource.Rent(out liveHandles[i]);
        foreach (ulong h in handles) Assert.False(LuminTaskSource.SetResult(h));
        foreach (ulong h in liveHandles) { LuminTaskSource.SetResult(h); LuminTaskSource.ToTask(h).GetResult(); }
    }

    private static void ZeroResultSurvivesTrim()
    {
        var task = LuminTaskSource.Create<int>(out var handle);
        Assert.True(LuminTaskSource.SetResult(handle, 0));
        LuminTaskSource.Trim();
        Assert.Equal(0, task.GetResult());
    }

    private static void ConcurrentRentalAndTrim()
    {
        int live = LuminTaskBag.GetLiveCount();
        var owners = new int[short.MaxValue];
        using var stop = new CancellationTokenSource();
        var trimmer = Task.Run(() =>
        {
            while (!stop.IsCancellationRequested)
            {
                LuminTaskSource.Trim();
                Thread.Yield();
            }
        });
        try
        {
            Parallel.For(0, 16, worker =>
            {
                for (int i = 0; i < 10_000; i++)
                {
                    var task = LuminTaskSource.Create<long>(out var handle);
                    int id = (int)(handle & 0xFFFF) - 1;
                    Assert.Equal(0, Interlocked.Exchange(ref owners[id], 1), "item rented twice");
                    long value = ((long)worker << 32) | (uint)i;
                    Assert.True(LuminTaskSource.SetResult(handle, value));
                    Volatile.Write(ref owners[id], 0);
                    Assert.Equal(value, task.GetAwaiter().GetResult());
                }
            });
        }
        finally
        {
            stop.Cancel();
            trimmer.GetAwaiter().GetResult();
        }
        Assert.Equal(live, LuminTaskBag.GetLiveCount());
    }

    private static void FullCapacity()
    {
        Assert.Equal(0, LuminTaskBag.GetLiveCount());
        var handles = new ulong[short.MaxValue];
        var seen = new bool[short.MaxValue];
        int count = 0;
        try
        {
            for (; count < handles.Length; count++)
            {
                LuminTaskSource.Rent(out handles[count]);
                int id = (ushort)handles[count] - 1;
                Assert.False(seen[id]);
                seen[id] = true;
            }
            Assert.Equal(0, LuminTaskBag.GetAvailableCount());
            try { LuminTaskSource.Rent(out _); Assert.Fail("expected capacity failure"); }
            catch (InvalidOperationException) { }
        }
        finally
        {
            for (int i = 0; i < count; i++)
            {
                LuminTaskSource.SetResult(handles[i]);
                LuminTaskSource.ToTask(handles[i]).GetResult();
            }
        }
        Assert.Equal(0, LuminTaskBag.GetLiveCount());
    }

    private static void TrimDirectory()
    {
        var handles = new ulong[257];
        for (int i = 0; i < handles.Length; i++) LuminTaskSource.Rent(out handles[i]);
        foreach (ulong handle in handles)
        {
            Assert.True(LuminTaskSource.SetResult(handle));
            LuminTaskSource.ToTask(handle).GetResult();
        }
        LuminTaskSource.Trim();
        var idle = LuminTaskSource.GetStorageStatistics();
        Assert.Equal(0, idle.BackedSlots);
        Assert.Equal(0, idle.DirectoryEntries);
        var fresh = new ulong[handles.Length];
        for (int i = 0; i < fresh.Length; i++) LuminTaskSource.Rent(out fresh[i]);
        foreach (ulong handle in handles) Assert.False(LuminTaskSource.SetResult(handle));
        foreach (ulong handle in fresh)
        {
            Assert.True(LuminTaskSource.SetResult(handle));
            LuminTaskSource.ToTask(handle).GetResult();
        }
    }

    private static void RetiredGeneration()
    {
        LuminTaskSource.Trim();
        var warm = LuminTaskSource.Create(out var seed);
        LuminTaskSource.SetResult(seed);
        warm.GetResult();
        int pageSize = LuminTaskBag.Capacity;
        var store = typeof(LuminTask).Assembly.GetType("LuminThread.SlotStore")!;
        var directory = (Array)store.GetField("_directory", BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null)!;
        var entry = directory.GetValue(0)!;
        entry.GetType().GetField("Generation", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(entry, (1L << 48) - 2);
        directory.SetValue(entry, 0);
        var task = LuminTaskSource.Create(out var last);
        Assert.Equal((1UL << 48) - 1, last >> 16);
        LuminTaskSource.SetResult(last);
        task.GetResult();
        var next = LuminTaskSource.Create(out var fresh);
        Assert.True((ushort)fresh > pageSize);
        Assert.False(LuminTaskSource.SetResult(last));
        LuminTaskSource.SetResult(fresh);
        next.GetResult();
    }

    private static void ConsumeRequiresTerminalState()
    {
        var task = LuminTaskSource.Create(out var handle);
        try { LuminTaskSource.ToTask(handle).GetResult(); Assert.Fail("pending GetResult"); }
        catch (InvalidOperationException) { }
        int callbacks = 0;
        task.OnCompleted(() => callbacks++);
        try { task.OnCompleted(() => callbacks += 100); Assert.Fail("duplicate registration"); }
        catch (InvalidOperationException) { }
        LuminTaskSource.SetResult(handle);
        Assert.Equal(1, callbacks);
        task.GetResult();
        try { LuminTaskSource.ToTask(handle).GetResult(); Assert.Fail("duplicate consumption"); }
        catch (InvalidOperationException) { }
    }

    private static void StatePoolBound()
    {
        int previous = LuminTaskIdleCache.MaxIdleObjectBytes;
        try
        {
            LuminTaskSource.Trim();
            LuminTaskIdleCache.MaxIdleObjectBytes = 256;
            var items = new StateTuple<PoolBoundValue>[64];
            for (int i = 0; i < items.Length; i++) items[i] = StatePool<PoolBoundValue>.Create(new PoolBoundValue(i));
            foreach (var item in items) item.Dispose();
            Assert.True(LuminTaskIdleCache.RetainedBytes > 0 && LuminTaskIdleCache.RetainedBytes <= 256);
            LuminTaskSource.Trim();
            Assert.Equal(0, LuminTaskIdleCache.RetainedBytes);
        }
        finally { LuminTaskIdleCache.MaxIdleObjectBytes = previous; }
    }
}
