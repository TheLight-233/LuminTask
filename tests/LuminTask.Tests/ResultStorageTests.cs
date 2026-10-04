using System.Reflection;
using System.Runtime.InteropServices;
using LuminThread;
using LuminThread.Utility;

namespace LuminTaskUnitTest;

public static class ResultStorageTests
{
    public static async Task RunAll(TestRunner runner)
    {
        await runner.Run("small results avoid native allocations; large results are freed", NativeAllocationBoundary);
        await runner.Run("mixed result kinds survive item reuse", MixedResults);
        await runner.Run("suspended results survive cross-thread completion", CrossThreadResults);
        await runner.Run("loop state shares inline and native result storage", LoopState);
        await runner.Run("cancellation registration can race completion", CancellationRegistrationRace);
        await runner.Run("idle cold storage is released and rebuilt", IdleColdStorage);
        await runner.Run("zero inline result keeps its cold block alive", ZeroResultSurvivesTrim);
        await runner.Run("packed allocator preserves exclusive ownership under contention", ConcurrentRentalAndTrim);
        await runner.Run("packed allocator reaches its full 16-bit capacity", FullCapacity);
        await runner.Run("exhausted generations retire without leaks or stale aliases", RetiredGeneration);
        await runner.Run("status alone never permits result consumption", CompletionRequiresSentinel);
        await runner.Run("state pool includes its fast item in the retention cap", StatePoolBound);
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

    private static int _nativeAllocations;
    private static int _nativeFrees;

    private static unsafe void* Allocate(nuint size)
    {
        Interlocked.Increment(ref _nativeAllocations);
        return NativeMemory.Alloc(size);
    }

    private static unsafe void Free(void* pointer)
    {
        Interlocked.Increment(ref _nativeFrees);
        NativeMemory.Free(pointer);
    }

    private static unsafe void NativeAllocationBoundary()
    {
        _nativeAllocations = _nativeFrees = 0;
        MemoryHelper.Set(&Allocate, &Free);
        try
        {
            RoundTrip(0);
            RoundTrip(-1);
            RoundTrip(byte.MaxValue);
            RoundTrip(short.MinValue);
            RoundTrip(new ThreeBytes(0xAB, short.MinValue));
            RoundTrip(float.NegativeInfinity);
            Assert.Equal(0, _nativeAllocations);

            RoundTrip(long.MinValue);
            RoundTrip(BitConverter.Int64BitsToDouble(unchecked((long)0xFFF0123456789ABC)));
            RoundTrip(new NineBytes(long.MinValue, 0xCD));
            RoundTrip(new SixteenBytes(long.MinValue, long.MaxValue));
            Assert.Equal(0, _nativeAllocations);

            RoundTrip(new SeventeenBytes(long.MinValue, long.MaxValue, 0xCD));
            RoundTrip(new Large(1, 2, 3, 4, 5, 6, 7, 8, long.MinValue));
            Assert.Equal(2, _nativeAllocations);
            Assert.Equal(_nativeAllocations, _nativeFrees);
        }
        finally
        {
            MemoryHelper.Set(null, null);
        }
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

    private const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;

    private const BindingFlags StaticHidden = BindingFlags.Static | BindingFlags.NonPublic;
    private static readonly FieldInfo IdleSince = typeof(LuminTaskBag).GetField("_idleSinceTimestamp", StaticHidden)!;
    private static readonly Action TrimCallback = (Action)typeof(LuminTaskBag).GetMethod("TrimColdBlocks", StaticHidden)!
        .CreateDelegate(typeof(Action));

    private static void Trim()
    {
        IdleSince.SetValue(null, unchecked(Environment.TickCount - 2000));
        TrimCallback();
    }

    private static Array Table(LuminTaskCold cold, string name)
        => (Array)typeof(LuminTaskCold).GetField(name, Hidden)!.GetValue(cold)!;

    private static void IdleColdStorage()
    {
        var task = LuminTaskSource.Create<string>(out var handle);
        Assert.True(LuminTaskSource.SetResult(handle, "before trim"));
        var cold = LuminTaskBag.GetCold((int)(handle & 0xFFFF) - 1);
        Assert.Equal("before trim", task.GetAwaiter().GetResult());
        Assert.NotNull(Table(cold, "_ref"));
        Trim();
        Assert.True(typeof(LuminTaskCold).GetField("_ref", Hidden)!.GetValue(cold) is null);
        RoundTrip("after trim");
        RoundTrip(new NineBytes(long.MinValue, 1));
    }

    private static void ZeroResultSurvivesTrim()
    {
        var task = LuminTaskSource.Create<int>(out var handle);
        Assert.True(LuminTaskSource.SetResult(handle, 0));
        int id = (int)(handle & 0xFFFF) - 1;
        var cold = LuminTaskBag.GetCold(id);
        var table = Table(cold, "_payload");
        int blockIndex = (id & 255) >> 6;
        var block = table.GetValue(blockIndex);
        Assert.NotNull(block);
        Trim();
        Assert.True(ReferenceEquals(block, table.GetValue(blockIndex)), "zero is still a live result");
        Assert.Equal(0, task.GetAwaiter().GetResult());
        Trim();
        Assert.True(typeof(LuminTaskCold).GetField("_payload", Hidden)!.GetValue(cold) is null);
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
                Trim();
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
        var ids = new short[short.MaxValue];
        var seen = new bool[short.MaxValue];
        int count = 0;
        try
        {
            while (count < ids.Length && LuminTaskBag.TryGetId(out short id))
            {
                ids[count++] = id;
                Assert.False(seen[id], "duplicate live id");
                seen[id] = true;
            }
            Assert.Equal(short.MaxValue, count);
            Assert.Equal(short.MaxValue, LuminTaskBag.GetLiveCount());
            Assert.Equal(0, LuminTaskBag.GetAvailableCount());
            Assert.False(LuminTaskBag.TryGetId(out _));
        }
        finally
        {
            for (int i = 0; i < count; i++) LuminTaskBag.ResetId(ids[i]);
        }
        Assert.Equal(0, LuminTaskBag.GetLiveCount());
    }

    private static void RetiredGeneration()
    {
        int available = LuminTaskBag.GetAvailableCount();
        short id = LuminTaskBag.GetId();
        ref var item = ref LuminTaskBag.GetItem(id);
        item.Reset();
        item.SyncState = (long)(uint.MaxValue - 1) << 16;
        ulong handle = ((ulong)(uint.MaxValue - 1) << 16) | (uint)(id + 1);
        Assert.True(LuminTaskSource.SetResult(handle, 123));
        Assert.Equal(123, LuminTaskSource.ToTask<int>(handle).GetAwaiter().GetResult());
        Assert.Equal(0, LuminTaskBag.GetLiveCount());
        Assert.Equal(available - 1, LuminTaskBag.GetAvailableCount());
        Assert.False(LuminTaskSource.SetResult(handle, 456));
        try
        {
            LuminTaskSource.ToTask<int>(handle).GetAwaiter().GetResult();
            Assert.Fail("last issued generation must become stale");
        }
        catch (InvalidOperationException) { }
        short next = LuminTaskBag.GetId();
        Assert.True(next != id);
        LuminTaskBag.ResetId(next);
    }

    private static void CompletionRequiresSentinel()
    {
        int live = LuminTaskBag.GetLiveCount();
        var task = LuminTaskSource.Create(out var handle);
        ref var item = ref LuminTaskBag.GetItem((int)(handle & 0xFFFF) - 1);
        item.Status = LuminThread.Interface.LuminTaskStatus.Succeeded;
        Assert.False(task.IsCompleted);
        try
        {
            task.GetAwaiter().GetResult();
            Assert.Fail("status was published but producer has not drained");
        }
        catch (InvalidOperationException) { }
        Assert.Equal(live + 1, LuminTaskBag.GetLiveCount());
        item.Status = LuminThread.Interface.LuminTaskStatus.Pending;
        Assert.True(LuminTaskSource.SetResult(handle));
        LuminTaskSource.ToTask(handle).GetAwaiter().GetResult();
        Assert.Equal(live, LuminTaskBag.GetLiveCount());
    }

    private static void StatePoolBound()
    {
        var items = new StateTuple<PoolBoundValue>[64];
        for (int i = 0; i < items.Length; i++) items[i] = StatePool<PoolBoundValue>.Create(new PoolBoundValue(i));
        foreach (var item in items) item.Dispose();
        var pool = typeof(StatePool<PoolBoundValue>);
        int overflow = (int)pool.GetField("_overflowCount", StaticHidden)!.GetValue(null)!;
        int fast = pool.GetField("_fastItem", StaticHidden)!.GetValue(null) is null ? 0 : 1;
        Assert.Equal(32, overflow + fast);
    }
}
