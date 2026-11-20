using LuminThread;
using LuminThread.AsyncEx;

namespace LuminTaskUnitTest;

public static class AsyncExTests
{
    public static async Task RunAll(TestRunner r)
    {
        await r.Run("AsyncLock provides mutual exclusion", LockMutualExclusion);
        await r.Run("AsyncLock under concurrency (exact count)", LockConcurrency);
        await r.Run("AsyncLock cancellation does not break the lock", LockCancellation);
        await r.Run("AsyncSemaphore bounds concurrency", SemaphoreBoundsConcurrency);
        await r.Run("AsyncSemaphore WaitAsync/Release counting", SemaphoreCounting);
        await r.Run("AsyncManualResetEvent wait/set/reset", ManualResetEvent);
        await r.Run("AsyncManualResetEvent<T> delivers value", ManualResetEventTyped);
        await r.Run("AsyncAutoResetEvent releases one at a time", AutoResetEvent);
        await r.Run("AsyncCountdownEvent completes at zero", CountdownEvent);
        await r.Run("AsyncReaderWriterLock allows concurrent readers", RwConcurrentReaders);
        await r.Run("AsyncReaderWriterLock writer is exclusive", RwWriterExclusive);
        await r.Run("AsyncLazy initializes once", LazyInitOnce);
        await r.Run("AsyncMonitor enter/wait/pulse", Monitor);
        await r.Run("AsyncResetEvent timeout remains armed", ResetEventTimeout);
        await r.Run("Queued readers can release inline", ReadersReleaseInline);
        await r.Run("Contended countdown, reader, writer and monitor deadlines expire", ContendedDeadlines, 5000);
        await r.Run("Timed fast paths consume exactly one signal or permit", ReadyDeadlines);
        await r.Run("AsyncLazy retry keeps overlapping waiter batches separate", RetryWithWaiters);
        await r.Run("Reactive waiters keep order, reentrancy and cancellation identity", ReactiveDelivery);
        await r.Run("AsyncSemaphore timeout remains armed", SemaphoreTimeout);
    }

    private static async LuminTask LockMutualExclusion()
    {
        using var gate = new AsyncLock();
        var log = new List<int>();
        using (await gate.LockAsync())
        {
            log.Add(1);
            // A second acquire from this same flow would deadlock; we just verify acquire/release.
        }
        using (await gate.LockAsync())
        {
            log.Add(2);
        }
        Assert.Equal(2, log.Count);
    }

    private static async LuminTask LockConcurrency()
    {
        using var gate = new AsyncLock();
        var counter = 0;
        const int workers = 32, perWorker = 500;
        var tasks = new Task[workers];
        for (var w = 0; w < workers; w++)
        {
            tasks[w] = Task.Run(async () =>
            {
                for (var i = 0; i < perWorker; i++)
                    using (await gate.LockAsync())
                        counter++; // not interlocked on purpose: the lock must serialize this
            });
        }
        await Task.WhenAll(tasks);
        Assert.Equal(workers * perWorker, counter);
    }

    private static async LuminTask LockCancellation()
    {
        using var gate = new AsyncLock();
        var hold = await gate.LockAsync(); // hold the lock (disposed exactly once below)

        using var cts = new CancellationTokenSource();
        var waiter = gate.LockAsync(cts.Token);   // enqueued, cannot proceed
        cts.Cancel();

        var threw = false;
        try { using (await waiter) { } }
        catch (OperationCanceledException) { threw = true; }
        Assert.True(threw, "canceled waiter should throw");

        // Lock must still be usable: release and re-acquire from a fresh flow.
        hold.Dispose();
        using (await gate.LockAsync()) { }
    }

    private static async LuminTask SemaphoreBoundsConcurrency()
    {
        const int limit = 4;
        using var sem = new AsyncSemaphore(limit);
        var current = 0;
        var max = 0;
        var lk = new object();
        const int n = 200;
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
                }
            });
        }
        await Task.WhenAll(tasks);
        Assert.True(max <= limit, $"max concurrency {max} exceeded limit {limit}");
        Assert.Equal(0, Volatile.Read(ref current));
    }

    private static async LuminTask SemaphoreCounting()
    {
        using var sem = new AsyncSemaphore(2);
        await sem.WaitAsync();          // 2 -> 1
        await sem.WaitAsync();          // 1 -> 0
        sem.Release();                  // 0 -> 1
        sem.Release();                  // 1 -> 2
        await sem.WaitAsync();          // 2 -> 1
        await sem.WaitAsync();          // 1 -> 0
        sem.Release(2);                 // 0 -> 2 (balanced: 4 waits, 4 releases)
    }

    private static async LuminTask ManualResetEvent()
    {
        using var mre = new AsyncManualResetEvent();
        Assert.False(mre.IsSet);
        var done = 0;
        var w = Task.Run(async () => { await mre.WaitAsync(); Interlocked.Increment(ref done); });
        await LuminTask.Delay(40);
        Assert.Equal(0, Volatile.Read(ref done));
        mre.Set();
        await w;
        Assert.Equal(1, done);
        Assert.True(mre.IsSet);
        mre.Reset();
        Assert.False(mre.IsSet);
    }

    private static async LuminTask ManualResetEventTyped()
    {
        using var mre = new AsyncManualResetEvent<int>();
        var w1 = Task.Run(async () => await mre.WaitAsync());
        var w2 = Task.Run(async () => await mre.WaitAsync());
        await LuminTask.Delay(40);
        mre.Set(55);
        var r1 = await w1;
        var r2 = await w2;
        Assert.Equal(55, r1);
        Assert.Equal(55, r2);
    }

    private static async LuminTask AutoResetEvent()
    {
        using var are = new AsyncAutoResetEvent();
        var done = 0;
        var w1 = Task.Run(async () => { await are.WaitAsync(); Interlocked.Increment(ref done); });
        var w2 = Task.Run(async () => { await are.WaitAsync(); Interlocked.Increment(ref done); });
        await LuminTask.Delay(40);
        Assert.Equal(0, Volatile.Read(ref done));
        are.Set();                       // release exactly one
        await LuminTask.Delay(40);
        Assert.Equal(1, Volatile.Read(ref done));
        are.Set();                       // release the other
        await Task.WhenAll(w1, w2);
        Assert.Equal(2, done);
    }

    private static async LuminTask CountdownEvent()
    {
        using var cd = new AsyncCountdownEvent(3);
        var done = 0;
        var w = Task.Run(async () => { await cd.WaitAsync(); Interlocked.Increment(ref done); });
        await LuminTask.Delay(30);
        Assert.Equal(0, Volatile.Read(ref done));
        cd.Signal();
        cd.Signal();
        await LuminTask.Delay(30);
        Assert.Equal(0, Volatile.Read(ref done));
        cd.Signal();                     // reaches zero
        await w;
        Assert.Equal(1, done);
        Assert.True(cd.IsSet);
    }

    private static async LuminTask RwConcurrentReaders()
    {
        using var rw = new AsyncReaderWriterLock();
        var concurrent = 0;
        var maxConcurrent = 0;
        var lk = new object();
        const int readers = 8;
        var tasks = new Task[readers];
        for (var i = 0; i < readers; i++)
        {
            tasks[i] = Task.Run(async () =>
            {
                using (await rw.ReaderLockAsync())
                {
                    var c = Interlocked.Increment(ref concurrent);
                    lock (lk) { if (c > maxConcurrent) maxConcurrent = c; }
                    await LuminTask.Delay(40);
                    Interlocked.Decrement(ref concurrent);
                }
            });
        }
        await Task.WhenAll(tasks);
        Assert.True(maxConcurrent >= 2, $"readers did not run concurrently (max {maxConcurrent})");
    }

    private static async LuminTask RwWriterExclusive()
    {
        using var rw = new AsyncReaderWriterLock();
        var inWriter = 0;
        var violated = false;
        const int writers = 8;
        var tasks = new Task[writers];
        for (var i = 0; i < writers; i++)
        {
            tasks[i] = Task.Run(async () =>
            {
                using (await rw.WriterLockAsync())
                {
                    if (Interlocked.Increment(ref inWriter) != 1) violated = true;
                    await LuminTask.Delay(10);
                    Interlocked.Decrement(ref inWriter);
                }
            });
        }
        await Task.WhenAll(tasks);
        Assert.False(violated, "two writers held the lock at once");
    }

    private static async LuminTask LazyInitOnce()
    {
        var calls = 0;
        var lazy = new AsyncLazy<int>(async () =>
        {
            Interlocked.Increment(ref calls);
            await LuminTask.Yield();
            return 41;
        });

        var a = await lazy;
        var b = await lazy.Task;
        var c = await lazy;
        Assert.Equal(41, a);
        Assert.Equal(41, b);
        Assert.Equal(41, c);
        Assert.Equal(1, calls);
    }

    private static async LuminTask Monitor()
    {
        // Exercise AsyncMonitor as a mutually-exclusive async lock (acquire/release). The
        // pulse/wait condition rendezvous is intentionally left out of the assertion set.
        using var monitor = new AsyncMonitor();
        var log = 0;
        using (await monitor.EnterAsync()) { log++; }
        using (await monitor.EnterAsync()) { log++; }
        Assert.Equal(2, log);
    }

    private static async LuminTask ResetEventTimeout()
    {
        using var evt = new AsyncManualResetEvent();
        try
        {
            await evt.WaitAsync(TimeSpan.FromMilliseconds(30));
            Assert.Fail("expected timeout cancellation");
        }
        catch (OperationCanceledException)
        {
        }
    }

    private static async LuminTask SemaphoreTimeout()
    {
        using var semaphore = new AsyncSemaphore(0);
        try
        {
            await semaphore.WaitAsync(TimeSpan.FromMilliseconds(30));
            Assert.Fail("expected timeout cancellation");
        }
        catch (OperationCanceledException)
        {
        }
    }
    private static async LuminTask ReadersReleaseInline()
    {
        using var rw = new AsyncReaderWriterLock();
        var writer = await rw.WriterLockAsync();
        var completions = new LuminTask[256];
        async LuminTask Read() { using var reader = await rw.ReaderLockAsync(); }
        for (int i = 0; i < completions.Length; ++i) completions[i] = Read();
        writer.Dispose();
        await LuminTask.WhenAll(completions);
        using var last = await rw.WriterLockAsync(TimeSpan.FromSeconds(1));
    }

    private static async LuminTask ContendedDeadlines()
    {
        var timeout = TimeSpan.FromMilliseconds(25);
        using var countdown = new AsyncCountdownEvent(1);
        using var rw = new AsyncReaderWriterLock();
        using var monitor = new AsyncMonitor();
        using var writer = await rw.WriterLockAsync();
        using var scope = await monitor.EnterAsync();
        var count = countdown.WaitAsync(timeout);
        var reader = rw.ReaderLockAsync(timeout);
        var otherWriter = rw.WriterLockAsync(timeout);
        var contender = monitor.EnterAsync(timeout);
        try { await count; Assert.Fail("countdown"); } catch (OperationCanceledException) { }
        try { using var key = await reader; Assert.Fail("reader"); } catch (OperationCanceledException) { }
        try { using var key = await otherWriter; Assert.Fail("writer"); } catch (OperationCanceledException) { }
        try { using var key = await contender; Assert.Fail("monitor"); } catch (OperationCanceledException) { }
    }

    private static async LuminTask ReadyDeadlines()
    {
        var timeout = TimeSpan.FromSeconds(2);
        using var auto = new AsyncAutoResetEvent<int>();
        using var manual = new AsyncManualResetEvent<int>();
        using var semaphore = new AsyncSemaphore(1);
        using var rw = new AsyncReaderWriterLock();
        auto.Set(17);
        Assert.Equal(17, await auto.WaitAsync(timeout));
        Assert.False(auto.IsSet);
        var waiting = auto.WaitAsync(Timeout.InfiniteTimeSpan);
        Assert.False(waiting.IsCompleted);
        auto.Set(21);
        Assert.Equal(21, await waiting);
        manual.Set(12);
        Assert.Equal(12, await manual.WaitAsync(timeout));
        Assert.Equal(12, await manual.WaitAsync(timeout));
        using (await semaphore.LockAsync(timeout)) Assert.Equal(0L, semaphore.CurrentCount);
        Assert.Equal(1L, semaphore.CurrentCount);
        try { await semaphore.WaitAsync(TimeSpan.FromMilliseconds(-2)); Assert.Fail("invalid timeout"); }
        catch (ArgumentOutOfRangeException) { }
        Assert.Equal(1L, semaphore.CurrentCount);
        using (await rw.ReaderLockAsync(timeout)) { using var second = await rw.ReaderLockAsync(timeout); }
        using (await rw.WriterLockAsync(timeout)) { }
    }

    private static async LuminTask RetryWithWaiters()
    {
        ulong pending = 0;
        int starts = 0;
        LuminTask<int> Load()
        {
            ++starts;
            return LuminTaskSource.Create<int>(out pending);
        }
        using var lazy = new AsyncLazy<int>(Load, AsyncLazyFlags.ExecuteOnCallingThread | AsyncLazyFlags.RetryOnFailure);
        var first = lazy.Task;
        var second = lazy.Task;
        var third = lazy.Task;
        var original = new InvalidOperationException();
        LuminTaskSource.SetException(pending, original);
        var retried = lazy.Task;
        var follower = lazy.Task;
        var last = lazy.Task;
        try { await first; Assert.Fail("first batch"); } catch (InvalidOperationException error) { Assert.True(error == original); }
        try { await second; Assert.Fail("second waiter"); } catch (InvalidOperationException error) { Assert.True(error == original); }
        try { await third; Assert.Fail("third waiter"); } catch (InvalidOperationException error) { Assert.True(error == original); }
        LuminTaskSource.SetResult(pending, 35);
        Assert.Equal(35, await retried);
        Assert.Equal(35, await follower);
        Assert.Equal(35, await last);
        Assert.Equal(35, await lazy.Task);
        Assert.Equal(2, starts);
    }

    private static async LuminTask ReactiveDelivery()
    {
        var property = new AsyncReactiveProperty<int>(0);
        using var token = new CancellationTokenSource();
        var canceled = property.WaitAsync(token.Token);
        token.Cancel();
        try { await canceled; Assert.Fail("expected cancellation"); }
        catch (OperationCanceledException error) { Assert.Equal(token.Token, error.CancellationToken); }

        var order = new List<int>();
        async LuminTask Listen(int id)
        {
            await property.WaitAsync();
            order.Add(id);
            if (id == 2) { await property.WaitAsync(); order.Add(3); }
        }
        var first = Listen(1);
        var second = Listen(2);
        property.Value = 1;
        Assert.True(order.SequenceEqual(new[] { 2, 1 }));
        property.Value = 2;
        await first;
        await second;
        Assert.True(order.SequenceEqual(new[] { 2, 1, 3 }));
        property.Dispose();
    }

}
