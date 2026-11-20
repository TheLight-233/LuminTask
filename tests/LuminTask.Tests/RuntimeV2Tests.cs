using System.Runtime.CompilerServices;
using LuminThread;
using LuminThread.Interface;
using LuminThread.Utility;

namespace LuminTaskUnitTest;

internal static class RuntimeV2Tests
{
    internal static async Task RunAll(TestRunner runner)
    {
        await runner.Run("inline registration retains the executing runner", InlineRegistration);
        await runner.Run("two consumers and two producers each have exactly one winner", ConcurrentOwnership);
        await runner.Run("trim and cache pressure preserve reference-containing values", ManagedResults);
        await runner.Run("scheduler rejection releases its unpublished source", RejectedAdmission);
        await runner.Run("shutdown drains an accepted continuation's input", Shutdown);
        await runner.Run("ConfigureAwait captures and restores the execution context", CaptureContext);
        await runner.Run("WhenEach delivers queued, waiting and faulted results", WhenEachDelivery);
        await runner.Run("WhenEach disposal drains children and wakes pending MoveNext", EarlyDispose);
#if !DEBUG
        await runner.Run("cross-thread suspension reuses its runner", CrossThreadAllocation);
#endif
    }

    private sealed class ImmediateAwaiter : ICriticalNotifyCompletion
    {
        internal object? Runner;
        internal Action? AfterResume;
        public ImmediateAwaiter GetAwaiter() => this;
        public bool IsCompleted => false;
        public void GetResult() { }
        public void OnCompleted(Action continuation) => UnsafeOnCompleted(continuation);
        public void UnsafeOnCompleted(Action continuation)
        {
            Runner = continuation.Target;
            continuation();
            AfterResume?.Invoke();
        }
    }

    private static async LuminTask<int> Immediate(ImmediateAwaiter awaiter)
    {
        await awaiter;
        return 71;
    }

    private static void InlineRegistration()
    {
        var first = new ImmediateAwaiter();
        first.AfterResume = () =>
        {
            var next = new ImmediateAwaiter();
            Assert.Equal(71, Immediate(next).GetResult());
            Assert.False(ReferenceEquals(first.Runner, next.Runner));
        };
        Assert.Equal(71, Immediate(first).GetResult());
    }

    private static void ConcurrentOwnership()
    {
        for (int i = 0; i < 256; i++)
        {
            LuminTaskSource.Rent<int>(out var handle);
            int writers = 0, readers = 0;
            Parallel.For(0, 2, index =>
            {
                if (LuminTaskSource.SetResult(handle, index))
                    Interlocked.Increment(ref writers);
            });
            Parallel.For(0, 2, _ =>
            {
                try { LuminTaskSource.ToTask<int>(handle).GetResult(); }
                catch (InvalidOperationException) { return; }
                Interlocked.Increment(ref readers);
            });
            Assert.Equal(1, writers);
            Assert.Equal(1, readers);
        }
    }

    private static void ManagedResults()
    {
        var task = LuminTaskSource.Create<(object, long, long)>(out var handle);
        var marker = new object();
        LuminTaskSource.SetResult(handle, (marker, 1L, 2L));
        LuminTaskSource.Trim();
        GC.Collect(2, GCCollectionMode.Forced, blocking: true, compacting: true);
        Assert.True(ReferenceEquals(marker, task.GetResult().Item1));
        var userBox = StateTuple.Create(7);
        var asObject = LuminTaskSource.Create<object>(out handle);
        LuminTaskSource.SetResult<object>(handle, userBox);
        Assert.True(ReferenceEquals(userBox, asObject.GetResult()));
        Assert.Equal(7, userBox.Item);
        userBox.Dispose();
    }

    private static void RejectedAdmission()
    {
        int before = LuminTaskBag.GetLiveCount();
        try { LuminTask.Delay(100, (PlayerLoopTiming)250).Forget(); Assert.Fail("invalid timing"); }
        catch (ArgumentOutOfRangeException) { }
        catch (IndexOutOfRangeException) { }
        Assert.Equal(before, LuminTaskBag.GetLiveCount());
    }

    private static void Shutdown()
    {
        var pump = new ManagedPlayerLoop();
        PlayerLoopHelper.SetStrategy(PlayerLoopTiming.EarlyUpdate, pump);
        var child = LuminTaskSource.Create<int>(out var handle);
        var next = child.ContinueWith(static n => n + 1, PlayerLoopTiming.EarlyUpdate);
        pump.Dispose();
        try { next.GetResult(); Assert.Fail("expected cancellation"); }
        catch (OperationCanceledException) { }
        LuminTaskSource.SetResult(handle, 2);
        Assert.False(LuminTaskSource.SetResult(handle, 3));
    }

    private static async LuminTask<int> CaptureValue(AsyncLocal<int> value, LuminTask signal)
    {
        await signal.ConfigureAwait(true);
        return value.Value;
    }

    private static void CaptureContext()
    {
        var value = new AsyncLocal<int> { Value = 27 };
        var task = CaptureValue(value, LuminTaskSource.Create(out var handle));
        value.Value = 81;
        LuminTaskSource.SetResult(handle);
        Assert.Equal(27, task.GetResult());
        Assert.Equal(81, value.Value);
    }

    private static async LuminTask WhenEachDelivery()
    {
        LuminTaskSource.Rent<string>(out var first);
        LuminTaskSource.Rent<string>(out var second);
        var iterator = LuminTask.WhenEach(new[] { LuminTaskSource.ToTask<string>(first),
            LuminTask.FromResult("inline"), LuminTaskSource.ToTask<string>(second) }).GetAsyncEnumerator();
        Assert.True(await iterator.MoveNextAsync());
        Assert.Equal("inline", iterator.Current.GetResult());
        var move = iterator.MoveNextAsync();
        var error = new FormatException("child");
        LuminTaskSource.SetException(second, error);
        Assert.True(await move);
        Assert.True(ReferenceEquals(error, iterator.Current.Exception));
        LuminTaskSource.SetResult(first, "last");
        Assert.True(await iterator.MoveNextAsync());
        Assert.Equal("last", iterator.Current.GetResult());
        Assert.False(await iterator.MoveNextAsync());
        await iterator.DisposeAsync();
    }

    private static async LuminTask EarlyDispose()
    {
        int before = LuminTaskBag.GetLiveCount();
        LuminTaskSource.Rent<string>(out var handle);
        var each = LuminTask.WhenEach(new[] { LuminTaskSource.ToTask<string>(handle) });
        var iterator = each.GetAsyncEnumerator();
        var move = iterator.MoveNextAsync();
        await iterator.DisposeAsync();
        Assert.False(await move);
        LuminTaskSource.SetResult(handle, "late");
        Assert.Equal(before, LuminTaskBag.GetLiveCount());
    }

    private static async LuminTask<int> Echo(LuminTask<int> task) => await task;

    private static void CrossThreadAllocation()
    {
        const int warm = 128, count = 2048;
        using var start = new AutoResetEvent(false);
        using var finish = new AutoResetEvent(false);
        ulong handle = 0;
        long remoteBytes = 0;
        var worker = new Thread(() =>
        {
            long mark = 0;
            for (int i = 0; i < warm + count; i++)
            {
                if (i == warm) mark = GC.GetAllocatedBytesForCurrentThread();
                start.WaitOne();
                LuminTaskSource.SetResult(handle, i);
                finish.Set();
            }
            remoteBytes = GC.GetAllocatedBytesForCurrentThread() - mark;
        });
        worker.Start();
        long baseline = 0;
        for (int i = 0; i < warm + count; i++)
        {
            if (i == warm) baseline = GC.GetAllocatedBytesForCurrentThread();
            var result = Echo(LuminTaskSource.Create<int>(out handle));
            start.Set();
            finish.WaitOne();
            int received = result.GetAwaiter().GetResult();
            if (received != i) throw new InvalidOperationException();
        }
        long bytes = GC.GetAllocatedBytesForCurrentThread() - baseline;
        worker.Join();
        Assert.True(bytes <= 64 && remoteBytes <= 64, $"caller={bytes}, completer={remoteBytes}");
    }
}
