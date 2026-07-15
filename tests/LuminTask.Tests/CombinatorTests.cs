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
        await r.Run("WhenAny (two) returns a valid winner", WhenAnyTwo);
        await r.Run("WhenAny (array) returns a valid index", WhenAnyArray);
        await r.Run("Delay honours cancellation", DelayCancellation);
    }

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
