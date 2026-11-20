using System.Runtime.CompilerServices;
using LuminThread;
using LuminThread.AsyncEx;
using LuminThread.Interface;
using LuminThread.Utility;

namespace LuminTaskUnitTest;

internal static class RuntimeV2Tests
{
    internal static async Task RunAll(TestRunner runner)
    {
        await runner.Run("inline registration retains the executing runner", InlineRegistration);
        await runner.Run("a callback delivered after failed registration cannot resume a recycled machine", FailedRegistration);
        await runner.Run("two consumers and two producers each have exactly one winner", ConcurrentOwnership);
        await runner.Run("trim and cache pressure preserve reference-containing values", ManagedResults);
        await runner.Run("the shared cache budgets its index and preserves typed chains", CacheIndex);
        await runner.Run("scheduler trimming releases empty rings and preserves in-flight work", TrimScheduler);
        await runner.Run("a late WhenAny loser and idle runner release the winner", WinnerCanBeCollected);
        await runner.Run("scheduler rejection releases its unpublished source", RejectedAdmission);
        await runner.Run("shutdown drains an accepted continuation's input", Shutdown);
        await runner.Run("ConfigureAwait captures and restores the execution context", CaptureContext);
        await runner.Run("WhenEach delivers queued, waiting and faulted results", WhenEachDelivery);
        await runner.Run("WhenEach disposal drains children and wakes pending MoveNext", EarlyDispose);
        await runner.Run("suppressed cancellation and forgotten results release the right outcome", CancellationConsumption);
        await runner.Run("WhenEach snapshots inline inputs and isolates reused sessions", EnumerationOwnership);
        await runner.Run("a reentrant WhenEach consumer can close and start another enumeration", ReentrantEnumeration);
        await runner.Run("forwarding preserves cells, references, faults and cancellation tokens", ForwardedOutcomes);
        await runner.Run("disposed lazy copies release values and honor admitted waiters", LazyLifetime);
        await runner.Run("wide WhenAny retains the winning position", WideAny);
#if !DEBUG
        await runner.Run("canceling warm tasks without observing exceptions allocates zero", CancellationAllocation);
        await runner.Run("cross-thread suspension reuses its runner", CrossThreadAllocation);
#endif
    }

    private sealed class Node<T> : ILuminCacheNode
    {
        public ILuminCacheNode? CacheNext { get; set; }
    }

    private static void TrimScheduler()
    {
        using var pump = new ManagedPlayerLoop();
        PlayerLoopHelper.SetStrategy(PlayerLoopTiming.PreUpdate, pump);
        LuminTaskSource.Trim();
        long baseline = LuminTaskSource.GetStorageStatistics().SchedulerArrayBytes;
        int ticks = 0;
        pump.AddAction(default, (in LuminTaskState _) =>
        {
            LuminTaskSource.Trim();
            return ++ticks == 1;
        });
        pump.RunCore();
        pump.RunCore();
        Assert.Equal(2, ticks);
        pump.AddContinuation(() => ticks++);
        pump.RunCore();
        LuminTaskSource.Trim();
        Assert.Equal(baseline, LuminTaskSource.GetStorageStatistics().SchedulerArrayBytes);
        pump.AddContinuation(() => ticks++);
        pump.RunCore();
        Assert.Equal(4, ticks);
    }

    private static void CacheIndex()
    {
        int limit = LuminTaskIdleCache.MaxIdleObjectBytes;
        ILuminCacheNode[] nodes = [new Node<int>(), new Node<string>(), new Node<byte>(), new Node<double>(),
            new Node<short>(), new Node<object>(), new Node<long>(), new Node<char>(), new Node<float>()];
        try
        {
            LuminTaskSource.Trim();
            LuminTaskIdleCache.MaxIdleObjectBytes = 4096;
            for (int round = 0; round < nodes.Length; round++)
            {
                foreach (var node in nodes) LuminTaskIdleCache.Return(node, 48);
                var s = LuminTaskSource.GetStorageStatistics();
                Assert.Equal(nodes.Length * 48, s.IdleObjectBytes);
                Assert.True(s.IdleIndexBytes > 0);
                for (int i = 0; i < nodes.Length; i++)
                {
                    var expected = nodes[(i * 5 + round) % nodes.Length];
                    Assert.True(ReferenceEquals(expected, LuminTaskIdleCache.Take(expected.GetType())));
                    Assert.True(expected.CacheNext == null);
                }
            }
            LuminTaskIdleCache.MaxIdleObjectBytes = 96;
            foreach (var node in nodes) LuminTaskIdleCache.Return(node, 48);
            Assert.Equal(48, LuminTaskIdleCache.RetainedBytes);
            Assert.Equal(0, LuminTaskSource.GetStorageStatistics().IdleIndexBytes);
        }
        finally
        {
            LuminTaskSource.Trim();
            LuminTaskIdleCache.MaxIdleObjectBytes = limit;
        }
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

    private sealed class ThrowingAwaiter : ICriticalNotifyCompletion
    {
        internal Action? Late;
        internal bool Continued;
        public ThrowingAwaiter GetAwaiter() => this;
        public bool IsCompleted => false;
        public void GetResult() { }
        public void OnCompleted(Action continuation) => UnsafeOnCompleted(continuation);
        public void UnsafeOnCompleted(Action continuation)
        {
            Late = continuation;
            throw new FormatException("registration");
        }
    }

    private static async LuminTask AfterFailure(ThrowingAwaiter source)
    {
        await source;
        source.Continued = true;
    }

    private static void FailedRegistration()
    {
        var first = new ThrowingAwaiter();
        try { AfterFailure(first).GetResult(); Assert.Fail("expected registration failure"); }
        catch (FormatException) { }
        var second = new ThrowingAwaiter();
        try { AfterFailure(second).GetResult(); Assert.Fail("expected registration failure"); }
        catch (FormatException) { }
        Assert.False(ReferenceEquals(first.Late!.Target, second.Late!.Target));
        first.Late();
        second.Late();
        Assert.False(first.Continued || second.Continued);
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

    private static async LuminTask<object> HoldUntil(LuminTask wait, object value)
    {
        await wait;
        return value;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference ConsumeWinner(out ulong loser)
    {
        var value = new object();
        LuminTaskSource.Rent<object>(out loser);
        var joined = LuminTask.WhenAny<object>(new[] {
            HoldUntil(LuminTaskSource.Create(out var signal), value), LuminTaskSource.ToTask<object>(loser)
        });
        LuminTaskSource.SetResult(signal);
        Assert.True(ReferenceEquals(value, joined.GetResult().result));
        return new WeakReference(value);
    }

    private static void WinnerCanBeCollected()
    {
        var weak = ConsumeWinner(out var loser);
        try
        {
            GC.Collect(2, GCCollectionMode.Forced, blocking: true, compacting: true);
            Assert.False(weak.IsAlive);
        }
        finally { LuminTaskSource.SetResult<object>(loser, null!); }
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

    private static void CancellationConsumption()
    {
        int live = LuminTaskBag.GetLiveCount();
        using var source = new CancellationTokenSource();
        source.Cancel();
        foreach (var token in new[] { default, source.Token })
        {
            var pending = LuminTaskSource.Create(out var handle).SuppressCancellationThrow();
            LuminTaskSource.SetCanceled(handle, token);
            Assert.True(pending.GetResult());
            Assert.True(LuminTask.FromCanceled<string>(token).SuppressCancellationThrow().GetResult().IsCanceled);

            Exception? seen = null;
            var forgotten = LuminTaskSource.Create<(object, long, long)>(out handle);
            forgotten.Forget(error => seen = error);
            LuminTaskSource.SetCanceled(handle, token);
            Assert.Equal(token, ((OperationCanceledException)seen!).CancellationToken);
            seen = null;
            LuminTask.FromCanceled(token).Forget(error => seen = error);
            Assert.Equal(token, ((OperationCanceledException)seen!).CancellationToken);
        }
        var failure = new FormatException();
        var checkedResult = LuminTaskSource.Create<int>(out var failed).SuppressCancellationThrow();
        LuminTaskSource.SetException(failed, failure);
        try { checkedResult.GetResult(); Assert.Fail("expected the original fault"); }
        catch (FormatException ex) { Assert.True(ReferenceEquals(failure, ex)); }

        // A result implementing the pool interface belongs to the caller.
        var userBox = StateTuple.Create(37);
        var reference = LuminTaskSource.Create<object>(out var resultHandle);
        LuminTaskSource.SetResult(resultHandle, (object)userBox);
        reference.Forget();
        Assert.Equal(37, userBox.Item);
        userBox.Dispose();
        Assert.Equal(live, LuminTaskBag.GetLiveCount());
    }

    private static void EnumerationOwnership()
    {
        var inline = new[] { LuminTask.FromResult("first"), LuminTask.FromResult("second") };
        var enumerable = LuminTask.WhenEach(inline);
        var first = enumerable.GetAsyncEnumerator();
        Array.Clear(inline);
        try { enumerable.GetAsyncEnumerator(); Assert.Fail("expected single consumption"); }
        catch (InvalidOperationException) { }
        Assert.True(first.MoveNextAsync().GetResult());
        Assert.Equal("first", first.Current.Result);
        Assert.True(first.MoveNextAsync().GetResult());
        Assert.Equal("second", first.Current.Result);
        first.DisposeAsync().GetResult();
        Assert.False(first.MoveNextAsync().GetResult());

        var old = LuminTask.WhenEach(LuminTaskSource.Create<string>(out var early)).GetAsyncEnumerator();
        var waiting = old.MoveNextAsync();
        old.DisposeAsync().GetResult();
        Assert.False(waiting.GetResult());
        var next = LuminTask.WhenEach(LuminTaskSource.Create<string>(out var current)).GetAsyncEnumerator();
        var pending = next.MoveNextAsync();
        LuminTaskSource.SetCanceled(early);
        Assert.False(pending.IsCompleted);
        LuminTaskSource.SetResult(current, "next");
        Assert.True(pending.GetResult());
        Assert.Equal("next", next.Current.Result);
        next.DisposeAsync().GetResult();

        var reused = LuminTask.WhenEach(LuminTaskSource.Create<string>(out var last)).GetAsyncEnumerator();
        old.DisposeAsync().GetResult();
        Assert.False(next.MoveNextAsync().GetResult());
        LuminTaskSource.SetResult(last, "last");
        Assert.True(reused.MoveNextAsync().GetResult());
        Assert.Equal("last", reused.Current.Result);
        reused.DisposeAsync().GetResult();
    }

    private static void ReentrantEnumeration()
    {
        var old = LuminTask.WhenEach(LuminTaskSource.Create<string>(out var handle)).GetAsyncEnumerator();
        var next = old;
        var move = old.MoveNextAsync();
        ulong second = 0;
        move.OnCompleted(() =>
        {
            Assert.True(move.GetResult());
            Assert.Equal("one", old.Current.Result);
            old.DisposeAsync().GetResult();
            next = LuminTask.WhenEach(LuminTaskSource.Create<string>(out second)).GetAsyncEnumerator();
        });
        LuminTaskSource.SetResult(handle, "one");
        Assert.False(old.MoveNextAsync().GetResult());
        var waiting = next.MoveNextAsync();
        LuminTaskSource.SetResult(second, "two");
        Assert.True(waiting.GetResult());
        Assert.Equal("two", next.Current.Result);
        next.DisposeAsync().GetResult();

        using var lifetime = new CancellationTokenSource();
        var cancelable = LuminTask.WhenEach(LuminTaskSource.Create<string>(out var late))
            .GetAsyncEnumerator(lifetime.Token);
        var canceledMove = cancelable.MoveNextAsync();
        lifetime.Cancel();
        try { canceledMove.GetResult(); Assert.Fail("expected canceled MoveNext"); }
        catch (OperationCanceledException ex) { Assert.Equal(lifetime.Token, ex.CancellationToken); }
        cancelable.DisposeAsync().GetResult();
        LuminTaskSource.SetCanceled(late, lifetime.Token);
    }

    private static async LuminTask ForwardedOutcomes()
    {
        using var lifetime = new CancellationTokenSource();
        var limit = TimeSpan.FromSeconds(10);
        var marker = new object();
        var large = LuminTaskSource.Create<(object, long, long)>(out var h).WaitAsync(lifetime.Token).Timeout(limit);
        LuminTaskSource.SetResult(h, (marker, 41L, 73L));
        Assert.Equal((marker, 41L, 73L), await large);

        var reference = LuminTaskSource.Create<object>(out h).Timeout(limit).WaitAsync(lifetime.Token);
        LuminTaskSource.SetResult(h, marker);
        Assert.True(ReferenceEquals(marker, await reference));

        var viewed = (LuminTask)LuminTaskSource.Create<(object, long, long)>(out h);
        var discard = viewed.Timeout(limit);
        LuminTaskSource.SetResult(h, (marker, 1L, 2L));
        await discard;

        var error = new FormatException("original");
        var fault = LuminTaskSource.Create<int>(out h).Timeout(limit).WaitAsync(lifetime.Token)
            .ContinueWith(static _ => throw new AssertException("faulted input ran a callback"), PlayerLoopTiming.DotNet);
        LuminTaskSource.SetException(h, error);
        try { await fault; Assert.Fail("expected the original fault"); }
        catch (FormatException ex) { Assert.True(ReferenceEquals(error, ex)); }

        using var canceled = new CancellationTokenSource();
        canceled.Cancel();
        var input = LuminTaskSource.Create<int>(out h).WaitAsync(lifetime.Token).Timeout(limit);
        LuminTaskSource.SetCanceled(h, canceled.Token);
        try { await input; Assert.Fail("expected cancellation"); }
        catch (OperationCanceledException ex) { Assert.Equal(canceled.Token, ex.CancellationToken); }

        var callback = LuminTask.FromResult(3).ContinueWith(static n => LuminTask.FromResult((new object(), 1L, (long)n)));
        Assert.Equal(3L, (await callback).Item3);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference DisposedLazyValue(out AsyncLazy<object> copy)
    {
        var value = new object();
        var lazy = new AsyncLazy<object>(() => value, AsyncLazyFlags.ExecuteOnCallingThread);
        copy = lazy;
        Assert.True(ReferenceEquals(value, lazy.Task.GetResult()));
        lazy.Dispose();
        return new WeakReference(value);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference DisposePendingFactory(out LuminTask<int> wait, out ulong handle)
    {
        var marker = new object();
        LuminTaskSource.Rent<int>(out handle);
        var input = handle;
        var lazy = new AsyncLazy<int>(() => { GC.KeepAlive(marker); return LuminTaskSource.ToTask<int>(input); }, AsyncLazyFlags.ExecuteOnCallingThread);
        wait = lazy.Task;
        lazy.Dispose();
        return new WeakReference(marker);
    }

    private static void LazyLifetime()
    {
        var weak = DisposedLazyValue(out var retainedCopy);
        GC.Collect(2, GCCollectionMode.Forced, blocking: true, compacting: true);
        Assert.False(weak.IsAlive);
        GC.KeepAlive(retainedCopy);

        var capture = DisposePendingFactory(out var pending, out var producer);
        GC.Collect(2, GCCollectionMode.Forced, blocking: true, compacting: true);
        bool retainedFactory = capture.IsAlive;
        LuminTaskSource.SetResult(producer, 13);
        Assert.Equal(13, pending.GetResult());
        Assert.False(retainedFactory, "pending resolver retained the disposed factory");

        ulong handle = 0;
        var lazy = new AsyncLazy<int>(() => LuminTaskSource.Create<int>(out handle), AsyncLazyFlags.ExecuteOnCallingThread);
        var first = lazy.Task;
        var second = lazy.Task;
        lazy.Dispose();
        try { lazy.Task.Forget(); Assert.Fail("expected disposed lazy"); }
        catch (ObjectDisposedException) { }
        LuminTaskSource.SetResult(handle, 7);
        Assert.Equal(7, first.GetResult());
        Assert.Equal(7, second.GetResult());
    }

    private static void WideAny()
    {
        var tasks = new LuminTask[96];
        var handles = new ulong[tasks.Length];
        for (int i = 0; i < tasks.Length; i++) tasks[i] = LuminTaskSource.Create(out handles[i]);
        var winner = LuminTask.WhenAny(tasks);
        LuminTaskSource.SetResult(handles[81]);
        Assert.Equal(81, winner.GetResult());
        for (int i = 0; i < tasks.Length; i++)
            if (i != 81) LuminTaskSource.SetCanceled(handles[i]);
    }

    private static void CancellationAllocation()
    {
        using var source = new CancellationTokenSource();
        source.Cancel();
        var token = source.Token;
        long mark = 0;
        for (int i = 0; i < 1152; i++)
        {
            if (i == 128) mark = GC.GetAllocatedBytesForCurrentThread();
            ConsumeCanceledPair(token);
        }
        Assert.Equal(0L, GC.GetAllocatedBytesForCurrentThread() - mark);
    }

    private static void ConsumeCanceledPair(CancellationToken token)
    {
        var checkedTask = LuminTaskSource.Create<int>(out var handle).SuppressCancellationThrow();
        LuminTaskSource.SetCanceled(handle, token);
        var result = checkedTask.GetResult();
        Assert.True(result.IsCanceled);
        LuminTask.FromCanceled(token).Forget();
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
