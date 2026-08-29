using LuminThread;
using LuminThread.AsyncEx;
using LuminThread.Interface;
using LuminThread.Utility;

namespace LuminTaskUnitTest;

public static class BasicTests
{
    public static async Task RunAll(TestRunner r)
    {
        await r.Run("FromResult returns value", FromResultReturnsValue);
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
        await r.Run("result observed after completion", ResultAfterCompletion);
        await r.Run("ValueTask conversions preserve results", ValueTaskConversions);
        await r.Run("CancellationToken.WaitAsync supports success/canceled modes", CancellationTokenWaitAsync);
        await r.Run("Never tasks use independent slots", NeverTasksUseIndependentSlots);
        await r.Run("ConfigureAwait and sequential combinators", ConfigureAwaitAndCombinators);
        await r.Run("Retry and Unwrap", RetryAndUnwrap);
        await r.Run("AsyncLazy supports repeated await", AsyncLazyRepeatedAwait);
        await r.Run("BCL await foreach adapter", BclAwaitForeach);
        await r.Run("Registered main-thread context", RegisteredMainThreadContext);
        await r.Run("Run executes sync/async delegates on the thread pool", RunDelegates);
        await r.Run("RunOnThreadPool runs on a pool thread", RunOnThreadPoolThread);
        await r.Run("Forget surfaces exceptions through the handler", ForgetExceptionHandler);
        await r.Run("ContinueWith chains a continuation", ContinueWithChains);
        await r.Run("Timeout aborts a hanging task", TimeoutAborts);
        await r.Run("WaitUntil and WaitUntilCanceled complete", WaitUntilCompletes);
        await r.Run("SuppressCancellationThrow preserves values", SuppressCancellationThrowPreserves);
        await r.Run("Create and DelayFrame", CreateAndDelayFrame);
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

    private static async LuminTask ResultAfterCompletion()
    {
        // Complete-then-await: the source must survive until the awaiter reads it.
        var tcs = new LuminTaskCompletionSource<int>();
        tcs.TrySetResult(99);
        var v = await tcs.Task;
        Assert.Equal(99, v);
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
        var mainThreadId = Environment.CurrentManagedThreadId;
        await LuminTask.Run(() => ranOnPool = Environment.CurrentManagedThreadId != mainThreadId);
        Assert.True(ranOnPool, "Run should execute the delegate off the calling thread");

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => LuminTask.Run(() => throw new InvalidOperationException("run-boom")));
    }

    private static async LuminTask RunOnThreadPoolThread()
    {
        var mainThreadId = Environment.CurrentManagedThreadId;
        var ran = false;
        var result = await LuminTask.RunOnThreadPool(() =>
        {
            ran = Environment.CurrentManagedThreadId != mainThreadId;
            return 11;
        });
        Assert.Equal(11, result);
        Assert.True(ran, "RunOnThreadPool must execute on a thread-pool thread");
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
        await Assert.ThrowsAsync<TimeoutException>(() =>
            LuminTask.Never(never.Token).Timeout(TimeSpan.FromMilliseconds(20)));

        var completed = await LuminTask.FromResult(42).Timeout(TimeSpan.FromMilliseconds(20));
        Assert.Equal(42, completed);
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

    // 9 longs = 72 bytes > 64, exercises the heap-backed inline-result path.
    private struct Big
    {
        public long A, B, C, D, E, F, G, H, P;
    }
}
