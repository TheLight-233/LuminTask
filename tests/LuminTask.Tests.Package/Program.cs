using LuminThread;
using LuminThread.AsyncEx;

#if INVALID_CONSUMPTION
internal static class InvalidConsumer
{
    public static void Main()
    {
        var pending = LuminTask.Yield();
    }
}
#else
using var loop = new ManagedPlayerLoop();
PlayerLoopHelper.SetStrategy(PlayerLoopTiming.Update, loop);
var output = StartRead(out ulong handle);
if (output.IsCompleted || !LuminTaskSource.SetResult(handle, ("release", 42L)))
    throw new InvalidOperationException("Consumer did not suspend.");
loop.RunCore();
if (output.GetResult() != ("release", 42L))
    throw new InvalidOperationException("The package changed the asynchronous result.");

var mutex = new AsyncLock();
using (await mutex.LockAsync())
{
    if (LuminTaskSource.GetStorageStatistics().ActiveSlots != 0)
        throw new InvalidOperationException("Consumer leaked task slots.");
}
Console.WriteLine("LuminTask package consumer passed.");

static LuminTask<(string, long)> StartRead(out ulong handle)
{
    return Read(LuminTaskSource.Create<(string, long)>(out handle));
}

static async LuminTask<(string, long)> Read(LuminTask<(string, long)> source)
{
    var result = await source;
    await LuminTask.DelayFrame(1);
    return result;
}
#endif
