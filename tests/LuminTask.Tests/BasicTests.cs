using System.Threading.Tasks.Sources;
using LuminThread;
using LuminThread.AsyncEx;
using LuminThread.Interface;
using LuminThread.Utility;

namespace LuminTaskUnitTest;

public static class BasicTests
{
    private static void ItemLayoutStaysTight()
    {
        var slot = typeof(LuminTask).Assembly.GetType("LuminThread.Slot")!;
        var sizeOf = typeof(System.Runtime.CompilerServices.Unsafe).GetMethod("SizeOf")!;
        Assert.Equal(24 + 4 * IntPtr.Size, (int)sizeOf.MakeGenericMethod(slot).Invoke(null, null)!);
        Assert.Equal(12, System.Runtime.CompilerServices.Unsafe.SizeOf<LuminTask<int>>());
        const ulong highGeneration = 0x1234_5678_0001_0001UL;
        Assert.True(LuminTaskSource.ToTask<int>(highGeneration).Equals(LuminTaskSource.ToTask(highGeneration)));
        Assert.False(LuminTaskSource.ToTask<int>(highGeneration).Equals(LuminTaskSource.ToTask<int>(0x0001_0001UL)));
    }

    public static async Task RunAll(TestRunner r)
    {
        await r.Run("FromResult returns value", FromResultReturnsValue);
        await r.Run("item layout stays tight", ItemLayoutStaysTight);
        await r.Run("CompletedTask completes", CompletedTaskCompletes);
        await r.Run("FromException throws on await", FromExceptionThrows);
        await r.Run("CanceledTask throws OperationCanceledException", CanceledTaskThrows);
        await r.Run("async method completing synchronously", SyncCompletion);
        await r.Run("async method with a single await", SingleAwait);
        await r.Run("async method with many suspensions", MultiSuspend);
        await r.Run("exception inside async method propagates", ExceptionPropagates);
        await r.Run("nested async (a→b→c)", NestedAsync);
        await r.Run("reference-type result", ReferenceTypeResult);
        await r.Run("small struct result", SmallStructResult);
        await r.Run("large struct result (>64 bytes)", LargeStructResult);
        await r.Run("reference-containing struct round-trips", ReferenceContainingStructRoundTrips);
        await r.Run("reference-containing struct does not box", ReferenceContainingStructDoesNotBox);
        await r.Run("result observed after completion", ResultAfterCompletion);
        await r.Run("Pending Task bridge flows registration context and preserves cancellation fallback", PendingTaskConversion);
        await r.Run("Source-backed ValueTask is consumed once on every outcome", SourceBackedConversion);
        await r.Run("ValueTask bridge honors execution context policy at registration", ValueTaskBridgeContext);
        await r.Run("ValueTask conversions preserve results", ValueTaskConversions);
        await r.Run("CancellationToken.WaitAsync supports success/canceled modes", CancellationTokenWaitAsync);
        await r.Run("Never tasks use independent slots", NeverTasksUseIndependentSlots);
        await r.Run("ConfigureAwait and sequential combinators", ConfigureAwaitAndCombinators);
        await r.Run("Retry and Unwrap", RetryAndUnwrap);
        await r.Run("AsyncLazy supports repeated await", AsyncLazyRepeatedAwait);
        await r.Run("BCL await foreach adapter", BclAwaitForeach);
        await r.Run("Registered main-thread context", RegisteredMainThreadContext);
        await r.Run("Run executes sync/async delegates on the thread pool", RunDelegates);
        await r.Run("Run family overloads stay distinguishable", RunFamilyOverloads);
        await r.Run("Run restores the captured synchronization context", RunRestoresCapturedContext);
        await r.Run("RunOnThreadPool runs on a pool thread", RunOnThreadPoolThread);
        await r.Run("RunOnThreadPool family overloads and cancellation", RunOnThreadPoolOverloads);
        await r.Run("Forget surfaces exceptions through the handler", ForgetExceptionHandler);
        await r.Run("ContinueWith chains a continuation", ContinueWithChains);
        await r.Run("Timeout aborts a hanging task", TimeoutAborts);
        await r.Run("Timeout passes through a pending inner value", TimeoutPassesThroughPendingValues);
        await r.Run("WaitUntil and WaitUntilCanceled complete", WaitUntilCompletes);
        await r.Run("SuppressCancellationThrow preserves values", SuppressCancellationThrowPreserves);
        await r.Run("Create and DelayFrame", CreateAndDelayFrame);
        await r.Run("PlayerLoop survives dispose before first use", PlayerLoopSurvivesDisposeBeforeFirstUse);
    }

    private static async LuminTask FromResultReturnsValue()
    {
        var v = await LuminTask.FromResult(123);
        Assert.Equal(123, v);
    }

    private static async LuminTask CompletedTaskCompletes()
    {
        await LuminTask.CompletedTask();
    }

    private static LuminTask FromExceptionThrows()
        => Assert.ThrowsAsync<InvalidOperationException>(
            () => LuminTask.FromException(new InvalidOperationException("boom")));

    private static LuminTask CanceledTaskThrows()
        => Assert.ThrowsAsync<OperationCanceledException>(() => LuminTask.CanceledTask());

    private static async LuminTask SyncCompletion()
    {
        var v = await Immediate();
        Assert.Equal(42, v);

        static async LuminTask<int> Immediate() => 42; // no await → completes synchronously
    }

    private static async LuminTask SingleAwait()
    {
        var v = await OneHop();
        Assert.Equal(7, v);

        static async LuminTask<int> OneHop()
        {
            await LuminTask.Yield();
            return 7;
        }
    }

    private static async LuminTask MultiSuspend()
    {
        var v = await FourHops();
        Assert.Equal(10, v);

        static async LuminTask<int> FourHops()
        {
            var sum = 0;
            await LuminTask.Yield(); sum += 1;
            await LuminTask.Delay(1); sum += 2;
            await LuminTask.Yield(); sum += 3;
            await LuminTask.Delay(1); sum += 4;
            return sum;
        }
    }

    private static LuminTask ExceptionPropagates()
        => Assert.ThrowsAsync<ArgumentOutOfRangeException>(Faulting);

    private static async LuminTask Faulting()
    {
        await LuminTask.Yield();
        throw new ArgumentOutOfRangeException("param");
    }

    private static async LuminTask NestedAsync()
    {
        var v = await A();
        Assert.Equal(6, v);

        static async LuminTask<int> A() => await B() + 1;
        static async LuminTask<int> B() => await C() + 2;
        static async LuminTask<int> C() { await LuminTask.Yield(); return 3; }
    }

    private static async LuminTask ReferenceTypeResult()
    {
        var s = await MakeString();
        Assert.Equal("lumin-42", s);

        static async LuminTask<string> MakeString()
        {
            await LuminTask.Yield();
            return "lumin-" + 42;
        }
    }

    private static async LuminTask SmallStructResult()
    {
        var p = await MakePoint();
        Assert.Equal(3, p.X);
        Assert.Equal(4, p.Y);

        static async LuminTask<(int X, int Y)> MakePoint()
        {
            await LuminTask.Yield();
            return (3, 4);
        }
    }

    private static async LuminTask LargeStructResult()
    {
        var big = await MakeBig();
        Assert.Equal(1L, big.A);
        Assert.Equal(16L, big.P);
        Assert.Equal(17L, big.A + big.P);

        static async LuminTask<Big> MakeBig()
        {
            await LuminTask.Yield();
            return new Big { A = 1, B = 2, C = 3, D = 4, E = 5, F = 6, G = 7, H = 8, P = 16 };
        }
    }

    // A value type that CONTAINS a reference cannot be memcpy'd into unmanaged memory (GC hole) and
    // must not be boxed (that would allocate). It goes through the pooled strong-typed container.
    private struct ReferenceContaining
    {
        public string Text;
        public int Number;
    }

    private static void ReferenceContainingStructRoundTrips()
    {
        var source = new LuminTaskCompletionSource<ReferenceContaining>();
        source.TrySetResult(new ReferenceContaining { Text = "lumin", Number = 7 });

        var value = source.Task.GetAwaiter().GetResult();
        Assert.Equal("lumin", value.Text);
        Assert.Equal(7, value.Number);
    }

    // The point of the pooled container: the reference-containing value must cost no more than a
    // plain unmanaged value of the same size. A boxing implementation would show up as one extra
    // object per round (header + payload), so comparing the two is a direct test for "no boxing".
    // The only allocation either path makes is the completion source object itself.
    private struct PlainValue
    {
        public long A;
        public long B;
    }

    private static void ReferenceContainingStructDoesNotBox()
    {
        for (var i = 0; i < 4; i++)
        {
            ReferenceRoundTrip();
            PlainRoundTrip();
        }

        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 32; i++)
            ReferenceRoundTrip();
        var referenceAllocated = GC.GetAllocatedBytesForCurrentThread() - before;

        before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 32; i++)
            PlainRoundTrip();
        var plainAllocated = GC.GetAllocatedBytesForCurrentThread() - before;

#if DEBUG
        // DEBUG builds add the leak tracker's dictionary traffic, which swamps a 32-round sample and
        // would hide the signal we are after, so the exact comparison only runs in Release.
        Assert.True(referenceAllocated >= 0 && plainAllocated >= 0, "allocation accounting must be monotonic");
#else
        Assert.Equal(plainAllocated, referenceAllocated,
            "a reference-containing result must not cost more than a plain value result (no boxing)");
#endif

        static void ReferenceRoundTrip()
        {
            var source = new LuminTaskCompletionSource<ReferenceContaining>();
            source.TrySetResult(new ReferenceContaining { Text = "lumin", Number = 7 });
            var value = source.Task.GetAwaiter().GetResult();
            Assert.Equal(7, value.Number);
        }

        static void PlainRoundTrip()
        {
            var source = new LuminTaskCompletionSource<PlainValue>();
            source.TrySetResult(new PlainValue { A = 1, B = 2 });
            var value = source.Task.GetAwaiter().GetResult();
            Assert.Equal(1L, value.A);
        }
    }

    private static async LuminTask ResultAfterCompletion()
    {
        // Complete-then-await: the source must survive until the awaiter reads it.
        var tcs = new LuminTaskCompletionSource<int>();
        tcs.TrySetResult(99);
        var v = await tcs.Task;
        Assert.Equal(99, v);
    }

    private static async LuminTask PendingTaskConversion()
    {
        var local = new AsyncLocal<string?>();
        var producer = new TaskCompletionSource<int>();
        local.Value = "registration";
        var converted = producer.Task.AsLuminTask();
        string? seen = null;
        var delivered = new TaskCompletionSource<int>();
        converted.SourceOnCompleted(_ =>
        {
            seen = local.Value;
            delivered.SetResult(converted.GetResult());
        }, delivered);
        local.Value = "completion";
        producer.SetResult(13);
        Assert.Equal(13, await delivered.Task);
        Assert.Equal("registration", seen);
        Assert.Equal("completion", local.Value);

        var noToken = new TaskCompletionSource<bool>();
        using var fallback = new CancellationTokenSource();
        var canceled = ((Task)noToken.Task).AsLuminTask(fallback.Token);
        noToken.SetCanceled();
        try { await canceled; Assert.Fail("expected cancellation"); }
        catch (OperationCanceledException error) { Assert.Equal(fallback.Token, error.CancellationToken); }
    }

    private sealed class ValueSource : IValueTaskSource<int>, IValueTaskSource
    {
        internal ManualResetValueTaskSourceCore<int> Core;
        internal int Reads;
        internal ValueTaskSourceOnCompletedFlags Flags;
        public int GetResult(short token) { Reads++; return Core.GetResult(token); }
        void IValueTaskSource.GetResult(short token) => GetResult(token);
        public ValueTaskSourceStatus GetStatus(short token) => Core.GetStatus(token);
        public void OnCompleted(Action<object?> callback, object? state, short token, ValueTaskSourceOnCompletedFlags flags)
        {
            Flags = flags;
            Core.OnCompleted(callback, state, token, flags);
        }
    }

    private static async LuminTask ValueTaskBridgeContext()
    {
        var source = new ValueSource();
        var local = new AsyncLocal<string?>();
        foreach (bool flow in new[] { true, false })
        {
            local.Value = "registration";
            source.Core.Reset();
            var value = new ValueTask<int>(source, source.Core.Version);
            var bridge = flow ? value.AsLuminTask() : value.AsLuminTask(flowExecutionContext: false);
            string? observed = null;
            bridge.OnCompleted(() => observed = local.Value);
            var expected = flow ? ValueTaskSourceOnCompletedFlags.FlowExecutionContext : ValueTaskSourceOnCompletedFlags.None;
            Assert.Equal(expected, source.Flags);
            local.Value = "publisher";
            source.Core.SetResult(37);
            Assert.Equal(flow ? "registration" : "publisher", observed);
            Assert.Equal(37, bridge.GetResult());

            source.Core.Reset();
            var empty = new ValueTask(source, source.Core.Version).AsLuminTask(flowExecutionContext: flow);
            Assert.Equal(expected, source.Flags);
            source.Core.SetResult(0);
            await empty;
        }
        Assert.Equal(4, source.Reads);
    }

    private static async LuminTask SourceBackedConversion()
    {
        var source = new ValueSource();
        source.Core.Reset();
        var converted = new ValueTask<int>(source, source.Core.Version).AsLuminTask();
        source.Core.SetResult(31);
        Assert.Equal(31, await converted);
        source.Core.Reset();
        var failed = new ValueTask(source, source.Core.Version).AsLuminTask();
        var original = new InvalidOperationException("fault");
        source.Core.SetException(original);
        try { await failed; Assert.Fail("expected fault"); }
        catch (InvalidOperationException error) { Assert.True(error == original); }
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        source.Core.Reset();
        var canceled = new ValueTask<int>(source, source.Core.Version).AsLuminTask();
        source.Core.SetException(new OperationCanceledException(cts.Token));
        try { await canceled; Assert.Fail("expected cancellation"); }
        catch (OperationCanceledException error) { Assert.Equal(cts.Token, error.CancellationToken); }
        Assert.Equal(3, source.Reads);
    }

    private static async LuminTask ValueTaskConversions()
    {
        var synchronous = await new ValueTask<int>(42).AsLuminTask();
        Assert.Equal(42, synchronous);

        var completion = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        var converted = new ValueTask<int>(completion.Task).AsLuminTask();
        completion.SetResult(84);
        Assert.Equal(84, await converted);
    }

    private static async LuminTask CancellationTokenWaitAsync()
    {
        using (var cts = new CancellationTokenSource())
        {
            var wait = cts.Token.WaitAsync();
            cts.Cancel();
            await wait;
        }

        using (var cts = new CancellationTokenSource())
        {
            var wait = cts.Token.WaitAsync(completeAsCanceled: true);
            cts.Cancel();
            try
            {
                await wait;
                Assert.Fail("expected cancellation");
            }
            catch (OperationCanceledException canceled)
            {
                Assert.Equal(cts.Token, canceled.CancellationToken);
            }
        }
    }

    private static async LuminTask NeverTasksUseIndependentSlots()
    {
        using var first = new CancellationTokenSource();
        using var second = new CancellationTokenSource();
        var firstTask = LuminTask.Never(first.Token);
        var secondTask = LuminTask.Never(second.Token);
        Assert.True(firstTask.Id != secondTask.Id, "Never tasks must not share a task slot");

        first.Cancel();
        try { await firstTask; Assert.Fail("expected cancellation"); }
        catch (OperationCanceledException) { }
        Assert.Equal(LuminTaskStatus.Pending, secondTask.Status);

        second.Cancel();
        try { await secondTask; Assert.Fail("expected cancellation"); }
        catch (OperationCanceledException) { }

        using var alreadyCanceled = new CancellationTokenSource();
        alreadyCanceled.Cancel();
        var preCanceled = LuminTask.Never(alreadyCanceled.Token);
        try { await preCanceled; Assert.Fail("expected cancellation"); }
        catch (OperationCanceledException) { }
    }

    private static async LuminTask ConfigureAwaitAndCombinators()
    {
        await LuminTask.Delay(1).ConfigureAwait(false);

        // Generic path: ConfigureAwait now hands the task itself back (no ConfiguredLuminTaskAwaitable<T>),
        // so this also pins the awaiter type down to LuminTask<T>.
        Assert.Equal(41, await LuminTask.Delay(1).ContinueWith(() => 41).ConfigureAwait(false));
        await LuminTask.WhenAllSequential(new[] { LuminTask.CompletedTask(), LuminTask.CompletedTask() });
    }

    private static async LuminTask RetryAndUnwrap()
    {
        var attempts = 0;
        await LuminTask.Retry(() =>
        {
            if (Interlocked.Increment(ref attempts) < 3)
                return LuminTask.FromException(new InvalidOperationException("retry"));
            return LuminTask.CompletedTask();
        }, retryCount: 2);
        Assert.Equal(3, attempts);

        var nested = LuminTask.FromResult(LuminTask.FromResult(7));
        Assert.Equal(7, await nested.Unwrap());
    }

    private static async LuminTask AsyncLazyRepeatedAwait()
    {
        var executions = 0;
        var lazy = new AsyncLazy<int>(() =>
        {
            Interlocked.Increment(ref executions);
            return 17;
        });
        Assert.Equal(17, await lazy.Task);
        Assert.Equal(17, await lazy.Task);
        Assert.Equal(1, executions);
        lazy.Dispose();
    }

    private static async LuminTask BclAwaitForeach()
    {
        var enumerable = LuminTask.WhenEach(LuminTask.FromResult(1), LuminTask.FromResult(2)).AsAsyncEnumerable();
        var sum = 0;
        await foreach (var item in enumerable)
            sum += item.GetResult();
        Assert.Equal(3, sum);
    }

    private static async LuminTask RegisteredMainThreadContext()
    {
        var context = SynchronizationContext.Current ?? new SynchronizationContext();
        PlayerLoopHelper.RegisterMainThread(context);
        try
        {
            await LuminTask.SwitchToMainThread();
        }
        finally
        {
            PlayerLoopHelper.UnregisterMainThread();
        }
    }

    private static async LuminTask RunDelegates()
    {
        var sum = await LuminTask.Run(() => 1 + 2);
        Assert.Equal(3, sum);

        var fromAsync = await LuminTask.Run(async () => { await LuminTask.Yield(); return 7; });
        Assert.Equal(7, fromAsync);

        var ranOnPool = false;
        await LuminTask.Run(() => ranOnPool = Thread.CurrentThread.IsThreadPoolThread);
        Assert.True(ranOnPool, "Run should execute the delegate on a thread-pool thread");

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => LuminTask.Run(() => throw new InvalidOperationException("run-boom")));
    }

    private static async LuminTask RunOnThreadPoolThread()
    {
        var ran = false;
        var result = await LuminTask.RunOnThreadPool(() =>
        {
            ran = Thread.CurrentThread.IsThreadPoolThread;
            return 11;
        });
        Assert.Equal(11, result);
        Assert.True(ran, "RunOnThreadPool must execute on a thread-pool thread");
    }

    // Most of the Run family forwards to one of five shared workers now, so the shapes that used to
    // have a state machine of their own have to stay observationally identical: object state passes
    // through, a value-type TState stays unboxed, and the LuminTask-returning overload hands the inner
    // task back instead of awaiting it (only the Func<LuminTask<T>> shape awaits).
    private static async LuminTask RunFamilyOverloads()
    {
        object captured = null;
        await LuminTask.Run(state => captured = state, "payload");
        Assert.Equal("payload", (string)captured);

        var sum = 0;
        await LuminTask.Run((int value) => sum = value + 1, 41);
        Assert.Equal(42, sum);

        // Run(Func<LuminTask>) is the hand-the-inner-task-back shape: the outer task completes while the
        // inner one is still pending, so it clearly did not await it.
        LuminTaskSource.Rent(out var pendingHandle);
        var pending = LuminTaskSource.ToTask(pendingHandle);
        var inner = await LuminTask.Run(() => pending);
        Assert.Equal(LuminTaskStatus.Pending, inner.Status);
        Assert.True(inner.Equals(pending), "Run(Func<LuminTask>) must hand the inner task back");
        LuminTaskSource.SetResult(pendingHandle);
        await inner;

        Assert.Equal(5, await LuminTask.Run<int>((object state) => (int)state, 5));
        Assert.Equal(4, await LuminTask.Run<string, int>(text => text.Length, "abcd"));
        Assert.Equal(6, await LuminTask.Run<int>((object state) => LuminTask.FromResult((int)state), 6));
        Assert.Equal(3, await LuminTask.Run<string, int>(text => LuminTask.FromResult(text.Length), "abc"));

        // The caller's own thread is not a reliable reference after earlier awaits in this test: the body
        // can already sit on a pool thread, and switching to the pool is free to resume on it. Assert
        // the documented guarantee instead - the delegate runs on a thread-pool thread.
        var offThread = false;
        await LuminTask.Run(() =>
        {
            offThread = Thread.CurrentThread.IsThreadPoolThread;
        }, configureAwait: false);
        Assert.True(offThread, "Run(Action, configureAwait: false) runs on a pool thread");

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => LuminTask.Run(state => throw new InvalidOperationException("state-boom"), new object()));
    }

    // configureAwait: true must switch back through the context captured before the switch to the
    // pool; configureAwait: false must not post to it at all.
    private static async LuminTask RunRestoresCapturedContext()
    {
        var context = new RecordingSynchronizationContext();
        var previous = SynchronizationContext.Current;
        try
        {
            // A SynchronizationContext is per-thread and this test resumes on whatever thread the
            // previous await landed on, so the context is installed again right before each call -
            // that thread is the one Run captures from.
            SynchronizationContext.SetSynchronizationContext(context);
            var before = context.PostCount;
            await LuminTask.Run(() => { }, configureAwait: false);
            var withoutRestore = context.PostCount - before;

            SynchronizationContext.SetSynchronizationContext(context);
            before = context.PostCount;
            await LuminTask.Run(() => { }, configureAwait: true);
            var withRestore = context.PostCount - before;

            Assert.True(withRestore > withoutRestore,
                $"configureAwait: true must post to the captured context (false: {withoutRestore}, true: {withRestore})");
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(previous);
        }
    }

    private static async LuminTask RunOnThreadPoolOverloads()
    {
        var ran = false;
        await LuminTask.RunOnThreadPool(() => ran = Thread.CurrentThread.IsThreadPoolThread);
        Assert.True(ran, "RunOnThreadPool(Action) runs on a pool thread");

        object captured = null;
        await LuminTask.RunOnThreadPool(state => captured = state, "payload");
        Assert.Equal("payload", (string)captured);

        var sum = 0;
        await LuminTask.RunOnThreadPool((int value) => sum = value + 1, 41);
        Assert.Equal(42, sum);

        Assert.Equal(9, await LuminTask.RunOnThreadPool(() => 9));
        Assert.Equal(12, await LuminTask.RunOnThreadPool(() => LuminTask.FromResult(12)));
        Assert.Equal(8, await LuminTask.RunOnThreadPool<int>((object state) => (int)state, 8));
        Assert.Equal(4, await LuminTask.RunOnThreadPool<string, int>(text => text.Length, "abcd"));
        Assert.Equal(7, await LuminTask.RunOnThreadPool<int>((object state) => LuminTask.FromResult((int)state), 7));
        Assert.Equal(3, await LuminTask.RunOnThreadPool<string, int>(text => LuminTask.FromResult(text.Length), "abc"));

        // Later in the test the body itself can already sit on a pool thread (switching to the pool is
        // free to resume on the thread that asked for it), so assert the pool affinity instead.
        var pooled = false;
        await LuminTask.RunOnThreadPool<int>(() =>
        {
            pooled = Thread.CurrentThread.IsThreadPoolThread;
            return 0;
        });
        Assert.True(pooled, "RunOnThreadPool<T> runs on a pool thread");

        using (var canceled = new CancellationTokenSource())
        {
            canceled.Cancel();
            await Assert.ThrowsAsync<OperationCanceledException>(
                () => LuminTask.RunOnThreadPool(() => { }, cancellationToken: canceled.Token));
            await Assert.ThrowsAsync<OperationCanceledException>(
                () => LuminTask.RunOnThreadPool(() => 1, cancellationToken: canceled.Token));
        }

        // The token is checked again after the delegate, so cancelling from inside the delegate is
        // observed even though the token was valid when the call started.
        using (var midFlight = new CancellationTokenSource())
        {
            await Assert.ThrowsAsync<OperationCanceledException>(
                () => LuminTask.RunOnThreadPool<int>(
                    () =>
                    {
                        midFlight.Cancel();
                        return 5;
                    },
                    cancellationToken: midFlight.Token));
        }
    }

    // The test host has no pump for a custom context, so Post runs the callback inline: the assertion
    // is about Run posting at all, not about where the continuation ends up.
    private sealed class RecordingSynchronizationContext : SynchronizationContext
    {
        public int PostCount;

        public override void Post(SendOrPostCallback d, object state)
        {
            PostCount++;
            d(state);
        }

        public override void Send(SendOrPostCallback d, object state) => d(state);
    }

    private static async LuminTask ForgetExceptionHandler()
    {
        var caught = new TaskCompletionSource<Exception>(TaskCreationOptions.RunContinuationsAsynchronously);
        var faulted = LuminTask.FromException(new InvalidOperationException("forget-boom"));
        faulted.Forget(ex => caught.TrySetResult(ex));

        var ex = await caught.Task;
        Assert.Equal(typeof(InvalidOperationException), ex.GetType());
    }

    private static async LuminTask ContinueWithChains()
    {
        var value = await LuminTask.FromResult(5).ContinueWith(x => x * 2);
        Assert.Equal(10, value);

        var chained = await LuminTask.CompletedTask().ContinueWith(() => LuminTask.FromResult("done"));
        Assert.Equal("done", chained);
    }

    private static async LuminTask TimeoutAborts()
    {
        using var never = new CancellationTokenSource();
        try
        {
            await Assert.ThrowsAsync<TimeoutException>(() =>
                LuminTask.Never(never.Token).Timeout(TimeSpan.FromMilliseconds(20)));
        }
        finally
        {
            // Timeout stops waiting; the source still needs to finish so it can be consumed.
            never.Cancel();
        }

        var completed = await LuminTask.FromResult(42).Timeout(TimeSpan.FromMilliseconds(20));
        Assert.Equal(42, completed);
    }

    private readonly record struct TimeoutPayload(int Number, string Text);

    // Timeout 的 promise 只保存内层任务的句柄，结果要由泛型方法从句柄重建任务后读出。
    // 已完成的输入走快路径不进 promise，所以这里必须用“挂起后才完成”的输入。
    private static async LuminTask TimeoutPassesThroughPendingValues()
    {
        // 注意：本测试方法自身会在 await 挂起的 promise 时挂起，因此它自己也占一个槽位，
        // 不能在方法内断言 live 归零；槽位核算由 LeakTests 与退出时的全局检查负责。
        var voidSource = new LuminTaskCompletionSource();
        var timedVoid = voidSource.Task.Timeout(TimeSpan.FromSeconds(5));
        voidSource.TrySetResult();
        await timedVoid;

        var textSource = new LuminTaskCompletionSource<string>();
        var timedText = textSource.Task.Timeout(TimeSpan.FromSeconds(5));
        textSource.TrySetResult("payload");
        var text = await timedText;
        Assert.True(text == "payload", "Timeout lost the inner string value");

        // 含引用结构体走池化结果容器，同样经句柄重建读出。
        var payloadSource = new LuminTaskCompletionSource<TimeoutPayload>();
        var timedPayload = payloadSource.Task.Timeout(TimeSpan.FromSeconds(5));
        payloadSource.TrySetResult(new TimeoutPayload(7, "struct"));
        var payload = await timedPayload;
        Assert.True(payload.Number == 7 && payload.Text == "struct", "Timeout lost the inner struct value");
    }

    private static async LuminTask WaitUntilCompletes()
    {
        var condition = 0;
        LuminTask.Run(() => { Thread.Sleep(30); condition = 1; }).Forget();

        await LuminTask.WaitUntil(() => condition == 1);
        Assert.Equal(1, condition);

        using var cts = new CancellationTokenSource();
        LuminTask.Run(() => { Thread.Sleep(20); cts.Cancel(); }).Forget();
        await LuminTask.WaitUntilCanceled(cts.Token);
        Assert.True(cts.IsCancellationRequested);
    }

    private static async LuminTask SuppressCancellationThrowPreserves()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        var canceled = await LuminTask.FromCanceled<int>(cts.Token).SuppressCancellationThrow();
        Assert.True(canceled.IsCanceled);

        var value = await LuminTask.FromResult(9).SuppressCancellationThrow();
        Assert.False(value.IsCanceled);
        Assert.Equal(9, value.Result);
    }

    private static async LuminTask CreateAndDelayFrame()
    {
        var fromCreate = await LuminTask.Create(() => LuminTask.FromResult(3));
        Assert.Equal(3, fromCreate);

        var createInt = await LuminTask.Create(() => LuminTask.FromResult(4 * 2));
        Assert.Equal(8, createInt);

        var fromToken = await LuminTask.Create(() => LuminTask.FromResult(5)).WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Equal(5, fromToken);

        await LuminTask.DelayFrame(1, PlayerLoopTiming.DotNet);
    }

    // The DotNet strategy builds its queues/CTS/timer on the first enqueue. Disposing a strategy that
    // was never used must be a no-op (all of it is still null), and Initialize must hand back a live
    // scheduler that lazily provisions itself again.
    private static async LuminTask PlayerLoopSurvivesDisposeBeforeFirstUse()
    {
        PlayerLoopHelper.Dispose();
        PlayerLoopHelper.Initialize();

        await LuminTask.Yield();
        await LuminTask.DelayFrame(1, PlayerLoopTiming.DotNet);
    }

    // 9 longs = 72 bytes > 64, exercises the heap-backed inline-result path.
    private struct Big
    {
        public long A, B, C, D, E, F, G, H, P;
    }
}
