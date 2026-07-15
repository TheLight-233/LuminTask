using LuminThread;

namespace LuminTaskUnitTest;

public static class CompletionSourceTests
{
    public static async Task RunAll(TestRunner r)
    {
        await r.Run("TCS<T> complete-before-await", CompleteBeforeAwait);
        await r.Run("TCS<T> await-before-complete", AwaitBeforeComplete);
        await r.Run("TCS<T> set exception", SetException);
        await r.Run("TCS<T> set canceled", SetCanceled);
        await r.Run("TCS (void) set result", VoidResult);
        await r.Run("TCS second TrySetResult returns false", SecondSetReturnsFalse);
        await r.Run("TCS bridges a callback API", CallbackBridge);
    }

    private static async LuminTask CompleteBeforeAwait()
    {
        var tcs = new LuminTaskCompletionSource<int>();
        var ok = tcs.TrySetResult(5);
        Assert.True(ok);
        var v = await tcs.Task;
        Assert.Equal(5, v);
    }

    private static async LuminTask AwaitBeforeComplete()
    {
        var tcs = new LuminTaskCompletionSource<int>();
        // Complete from a background thread shortly after we start awaiting.
        _ = Task.Run(async () =>
        {
            await Task.Delay(30);
            tcs.TrySetResult(77);
        });
        var v = await tcs.Task;
        Assert.Equal(77, v);
    }

    private static LuminTask SetException()
    {
        var tcs = new LuminTaskCompletionSource<int>();
        tcs.TrySetException(new InvalidOperationException("tcs-fault"));
        return Assert.ThrowsAsync<InvalidOperationException>(() => tcs.Task);
    }

    private static LuminTask SetCanceled()
    {
        var tcs = new LuminTaskCompletionSource<int>();
        tcs.TrySetCanceled();
        return Assert.ThrowsAsync<OperationCanceledException>(() => tcs.Task);
    }

    private static async LuminTask VoidResult()
    {
        var tcs = new LuminTaskCompletionSource();
        Assert.True(tcs.TrySetResult());
        await tcs.Task;
    }

    private static void SecondSetReturnsFalse()
    {
        var tcs = new LuminTaskCompletionSource<int>();
        Assert.True(tcs.TrySetResult(1), "first set should succeed");
        Assert.False(tcs.TrySetResult(2), "second set should fail");
        // Consume the task so the source is released (single-consume contract).
        tcs.Task.Forget();
    }

    private static async LuminTask CallbackBridge()
    {
        var tcs = new LuminTaskCompletionSource<string>();
        FakeCallbackApi(result => tcs.TrySetResult(result));
        var v = await tcs.Task;
        Assert.Equal("done", v);

        static void FakeCallbackApi(Action<string> callback)
            => ThreadPool.QueueUserWorkItem(_ => callback("done"));
    }
}
