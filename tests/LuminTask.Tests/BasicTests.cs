using LuminThread;

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

    // 9 longs = 72 bytes > 64, exercises the heap-backed inline-result path.
    private struct Big
    {
        public long A, B, C, D, E, F, G, H, P;
    }
}
