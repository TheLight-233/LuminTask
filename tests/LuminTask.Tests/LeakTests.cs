using LuminThread;
using LuminThread.Utility;

namespace LuminTaskUnitTest;

// These compare LuminTaskLeakTracker.LiveCount before/after an operation. In RELEASE the tracker
// is compiled out (LiveCount == 0 always) so they pass trivially; in DEBUG they verify that every
// source created by the operation was released.
//
// IMPORTANT: each test method is itself an `async LuminTask`. The instant it awaits, the builder
// allocates ONE source for the method's own frame, which stays alive until the method returns
// (the runner consumes it afterwards). So we `await LuminTask.Yield()` *before* taking the first
// snapshot — that materialises the method's own source so it is present in BOTH snapshots and
// cancels out of the delta. Without this warm-up the method's own frame shows up as a phantom "+1".
public static class LeakTests
{
    public static async Task RunAll(TestRunner r)
    {
        // Counting is all we need here; skip the (expensive) per-create stack capture so DEBUG runs fast.
        var prior = LuminTaskLeakTracker.CaptureStackTrace;
        LuminTaskLeakTracker.CaptureStackTrace = false;
        try
        {
            await r.Run("awaited tasks do not leak", AwaitedNoLeak);
            await r.Run("forgotten completed task does not leak", ForgetCompletedNoLeak);
            await r.Run("forgotten incomplete task does not leak", ForgetIncompleteNoLeak);
            await r.Run("WhenAll releases its inputs", WhenAllNoLeak);
            await r.Run("exception path releases the source", FaultNoLeak);
            await r.Run("external cancellation drains inner source", ExternalCancellationDrainsInner);
            await r.Run("timeout drains inner source", TimeoutDrainsInner);
            await r.Run("reactive property dispose race does not lose source", ReactivePropertyDisposeRace);
            await r.Run("channel completion and repeated waits do not leak", ChannelLifecycleNoLeak);
        }
        finally
        {
            LuminTaskLeakTracker.CaptureStackTrace = prior;
        }
    }

    private static async LuminTask AwaitedNoLeak()
    {
        await LuminTask.Yield();                 // materialise this frame's own source first
        var before = LuminTaskLeakTracker.LiveCount;
        for (var i = 0; i < 1000; i++)
            await SuspendOnce();
        var after = LuminTaskLeakTracker.LiveCount;
        Assert.Equal(before, after, "awaited LuminTasks leaked their sources");

        static async LuminTask<int> SuspendOnce()
        {
            await LuminTask.Yield();
            return 1;
        }
    }

    private static async LuminTask ForgetCompletedNoLeak()
    {
        await LuminTask.Yield();
        var before = LuminTaskLeakTracker.LiveCount;
        for (var i = 0; i < 1000; i++)
            LuminTask.FromResult(i).Forget();
        // Synchronously-completed tasks have no source; this also proves Forget on a completed
        // task neither throws nor leaks.
        var after = LuminTaskLeakTracker.LiveCount;
        Assert.Equal(before, after, "forgotten completed tasks leaked");
    }

    private static async LuminTask ForgetIncompleteNoLeak()
    {
        await LuminTask.Yield();
        var before = LuminTaskLeakTracker.LiveCount;

        var tcs = new LuminTaskCompletionSource<int>();
        tcs.Task.Forget();              // forget while pending: registers the pooled continuation
        tcs.TrySetResult(1);            // completing fires it → GetResult → release
        await Task.Delay(30);           // let the continuation settle (it fires inline; this is safety)

        var after = LuminTaskLeakTracker.LiveCount;
        Assert.Equal(before, after, "forgotten incomplete task leaked after completion");
    }

    private static async LuminTask WhenAllNoLeak()
    {
        await LuminTask.Yield();
        var before = LuminTaskLeakTracker.LiveCount;
        for (var round = 0; round < 100; round++)
        {
            var arr = new LuminTask[20];
            for (var i = 0; i < 20; i++) arr[i] = SuspendOnceVoid();
            await LuminTask.WhenAll(arr);
        }

        // WhenAll frees its aggregate core producer-side, immediately AFTER firing our continuation,
        // so the final round's core can still be alive the instant we resume. Let it drain.
        for (var i = 0; i < 50 && LuminTaskLeakTracker.LiveCount > before; i++)
            await Task.Delay(5);

        var after = LuminTaskLeakTracker.LiveCount;
        Assert.Equal(before, after, "WhenAll leaked its input sources");

        static async LuminTask SuspendOnceVoid() => await LuminTask.Yield();
    }

    private static async LuminTask FaultNoLeak()
    {
        await LuminTask.Yield();
        var before = LuminTaskLeakTracker.LiveCount;
        for (var i = 0; i < 200; i++)
        {
            try { await Faulting(); }
            catch (InvalidOperationException) { }
        }
        var after = LuminTaskLeakTracker.LiveCount;
        Assert.Equal(before, after, "faulted tasks leaked their sources");

        static async LuminTask Faulting()
        {
            await LuminTask.Yield();
            throw new InvalidOperationException("leak-check");
        }
    }

    private static async LuminTask ExternalCancellationDrainsInner()
    {
        await LuminTask.Yield();
        var before = LuminTaskLeakTracker.LiveCount;

        var inner = new LuminTaskCompletionSource<int>();
        using var cts = new CancellationTokenSource();
        var waiting = inner.Task.AttachExternalCancellation(cts.Token);
        cts.Cancel();

        try { await waiting; }
        catch (OperationCanceledException) { }

        inner.TrySetResult(42);
        await WaitForLiveCount(before);
        Assert.Equal(before, LuminTaskLeakTracker.LiveCount,
            "AttachExternalCancellation abandoned its inner source");
    }

    private static async LuminTask TimeoutDrainsInner()
    {
        await LuminTask.Yield();
        var before = LuminTaskLeakTracker.LiveCount;

        var inner = new LuminTaskCompletionSource<int>();
        try { await inner.Task.Timeout(TimeSpan.FromMilliseconds(20)); }
        catch (TimeoutException) { }

        inner.TrySetResult(42);
        await WaitForLiveCount(before);
        Assert.Equal(before, LuminTaskLeakTracker.LiveCount,
            "Timeout abandoned its inner source");
    }

    private static async LuminTask ReactivePropertyDisposeRace()
    {
        await LuminTask.Yield();
        var before = LuminTaskLeakTracker.LiveCount;

        for (var i = 0; i < 500; i++)
        {
            using var property = new LuminThread.AsyncEx.AsyncReactiveProperty<int>(0);
            var dispose = Task.Run(property.Dispose);
            var waiting = property.WaitAsync();
            await dispose;
            try { await waiting; }
            catch (OperationCanceledException) { }
        }

        await WaitForLiveCount(before);
        Assert.Equal(before, LuminTaskLeakTracker.LiveCount,
            "AsyncReactiveProperty lost a source during Dispose/WaitAsync race");
    }

    private static async LuminTask ChannelLifecycleNoLeak()
    {
        await LuminTask.Yield();
        var before = LuminTaskLeakTracker.LiveCount;

        var channel = LuminChannel.CreateSingleConsumerUnbounded<int>();
        for (var i = 0; i < 200; i++)
        {
            var waiting = channel.Reader.WaitToReadAsync();
            Assert.True(channel.Writer.TryWrite(i));
            Assert.True(await waiting);
            Assert.True(channel.Reader.TryRead(out var value));
            Assert.Equal(i, value);
        }

        var completion = channel.Reader.Completion;
        Assert.True(channel.Writer.TryComplete());
        await completion;
        await channel.Reader.Completion;
        ((IDisposable)channel).Dispose();

        await WaitForLiveCount(before);
        Assert.Equal(before, LuminTaskLeakTracker.LiveCount,
            "LuminChannel retained a source or reused a dangling waiter");
    }

    private static async LuminTask WaitForLiveCount(int expected)
    {
        for (var i = 0; i < 100 && LuminTaskLeakTracker.LiveCount > expected; i++)
            await Task.Delay(5);
    }
}
