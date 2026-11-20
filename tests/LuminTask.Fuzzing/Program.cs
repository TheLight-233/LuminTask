using LuminThread;
using LuminThread.Utility;

if (args.Length > 3)
    throw new ArgumentException("Usage: LuminTask.Fuzzing [seed] [iterations] [repro-file]");

int seed = args.Length > 0 ? int.Parse(args[0]) : 20261120;
int iterations = args.Length > 1 ? int.Parse(args[1]) : 2000;
ArgumentOutOfRangeException.ThrowIfNegativeOrZero(iterations);
string repro = args.Length > 2 ? args[2] : "artifacts/fuzz/repro.txt";
var random = new Random(seed);
using var loop = new ManagedPlayerLoop();
PlayerLoopHelper.SetStrategy(PlayerLoopTiming.Update, loop);
// The external-cancellation bridge uses the default timing.
PlayerLoopHelper.SetStrategy(PlayerLoopTiming.DotNet, loop);

int scenario = -1, iteration = 0;
try
{
    for (; iteration < iterations; iteration++)
    {
        if (iteration % 17 == 0)
            LuminTaskIdleCache.MaxIdleObjectBytes = random.Next(2) == 0 ? 0 : random.Next(64, 65537);
        scenario = random.Next(5);
        switch (scenario)
        {
            case 0: await RaceCompletion(random.Next(), random.Next(3)); break;
            case 1: Join(random, any: false); break;
            case 2: Join(random, any: true); break;
            case 3: ObserveEach(random); break;
            case 4: DriveWaits(loop, random); break;
        }

        Check(LuminTaskSource.GetStorageStatistics().ActiveSlots == 0, "an operation left a slot rented");
        Check(LuminTaskIdleCache.RetainedBytes <= LuminTaskIdleCache.MaxIdleObjectBytes, "cache budget exceeded");
        if (iteration % 11 == 0)
        {
            LuminTaskSource.Trim();
            var stats = LuminTaskSource.GetStorageStatistics();
            Check(stats.SlotArrayBytes == 0 && stats.SchedulerArrayBytes == 0 && LuminTaskIdleCache.RetainedBytes == 0,
                "trim retained idle storage");
        }
    }
    Console.WriteLine($"seed={seed} iterations={iterations} active-slots=0");
    return 0;
}
catch (Exception error)
{
    Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(repro))!);
    string replay = $"dotnet run --project tests/LuminTask.Fuzzing -c {(IsDebug() ? "Debug" : "Release")} -- {seed} {iteration + 1}";
    string report = $"seed={seed} iteration={iteration} scenario={scenario}{Environment.NewLine}{replay}{Environment.NewLine}{error}";
    File.WriteAllText(repro, report);
    Console.Error.WriteLine(report);
    return 1;
}

static async Task RaceCompletion(int value, int order)
{
    using var cancellation = new CancellationTokenSource();
    cancellation.Cancel();
    var input = LuminTaskSource.Create<Payload>(out ulong handle);
    var expected = new Payload(value, value + 1L, value.ToString());
    Task<Payload>? observed = order == 0 ? Observe(input).AsTask() : null;
    bool succeeded = false, canceled = false;
    await Task.WhenAll(
        Task.Run(() => succeeded = LuminTaskSource.SetResult(handle, expected)),
        Task.Run(() => canceled = LuminTaskSource.SetCanceled(handle, cancellation.Token)),
        Task.Run(() => LuminTaskSource.Trim())).WaitAsync(TimeSpan.FromSeconds(15));
    observed ??= Observe(input).AsTask();
    Check(succeeded ^ canceled, "exactly one producer must win");
    try
    {
        var valueRead = await observed.WaitAsync(TimeSpan.FromSeconds(15));
        Check(succeeded && valueRead == expected, "result changed during completion");
    }
    catch (OperationCanceledException error)
    {
        Check(canceled && error.CancellationToken == cancellation.Token, "cancellation token changed");
    }

    if (order == 2) LuminTaskSource.Trim();
    var next = LuminTaskSource.Create<Payload>(out ulong replacement);
    Check(!LuminTaskSource.SetResult(handle, expected), "stale handle completed the replacement");
    Check(LuminTaskSource.SetResult(replacement, expected) && next.GetResult() == expected, "replacement failed");
}

static async LuminTask<Payload> Observe(LuminTask<Payload> input) => await input;

static void Join(Random random, bool any)
{
    int count = random.Next(1, 97);
    var inputs = new LuminTask<int>[count];
    var handles = new ulong[count];
    var order = Enumerable.Range(0, count).ToArray();
    random.Shuffle(order.AsSpan());
    for (int i = 0; i < count; i++)
        inputs[i] = LuminTaskSource.Create<int>(out handles[i]);

    // An already completed input and an initially pending input must agree on the winner.
    int completed = random.Next(2);
    if (completed != 0) Complete(handles, order[0]);
    if (any)
    {
        var joined = LuminTask.WhenAny(inputs);
        for (int i = completed; i < count; i++) Complete(handles, order[i]);
        var (index, result) = joined.GetResult();
        Check(index == order[0] && result == 3 * index, "WhenAny winner differs from completion order");
    }
    else
    {
        var joined = LuminTask.WhenAll(inputs);
        for (int i = completed; i < count; i++) Complete(handles, order[i]);
        var result = joined.GetResult();
        Check(result.Length == count, "WhenAll result count changed");
        for (int i = 0; i < count; i++) Check(result[i] == 3 * i, "WhenAll reordered inputs");
    }
}

static void ObserveEach(Random random)
{
    int count = random.Next(3, 40);
    int take = random.Next(count + 1);
    var inputs = new LuminTask<int>[count];
    var handles = new ulong[count];
    for (int i = 0; i < count; i++) inputs[i] = LuminTaskSource.Create<int>(out handles[i]);
    var enumeration = LuminTask.WhenEach(inputs).GetAsyncEnumerator();
    for (int i = 0; i < take; i++)
    {
        int index = count - 1 - i;
        var move = enumeration.MoveNextAsync();
        Complete(handles, index);
        Check(move.GetResult() && enumeration.Current.Result == 3 * index, "WhenEach yielded the wrong result");
    }
    enumeration.DisposeAsync().GetResult();
    for (int i = 0; i < count - take; i++) Complete(handles, i);
}

static void DriveWaits(ManagedPlayerLoop loop, Random random)
{
    using var cancellation = new CancellationTokenSource();
    int frames = random.Next(1, 9);
    int tick = 0, ready = random.Next(1, 9), cancel = random.Next(1, 10);
    var delay = LuminTask.DelayFrame(frames, PlayerLoopTiming.Update, cancellation.Token);
    var predicate = LuminTask.WaitUntil(() => tick >= ready, PlayerLoopTiming.Update, cancellation.Token);
    var signal = LuminTask.WaitUntilCanceled(cancellation.Token, PlayerLoopTiming.Update, random.Next(2) == 0);
    for (tick = 1; tick <= 9; tick++)
    {
        if (tick == cancel) cancellation.Cancel();
        loop.RunCore();
        Check(delay.IsCompleted == (tick >= Math.Min(frames, cancel)), "wrong DelayFrame completion tick");
        Check(predicate.IsCompleted == (tick >= Math.Min(ready, cancel)), "wrong WaitUntil completion tick");
    }
    Check(delay.SuppressCancellationThrow().GetResult() == (cancel <= frames), "wrong DelayFrame outcome");
    Check(predicate.SuppressCancellationThrow().GetResult() == (cancel <= ready), "wrong WaitUntil outcome");
    signal.GetResult();

    var inner = LuminTaskSource.Create<int>(out ulong handle);
    var forwarded = inner.WaitAsync(cancellation.Token);
    LuminTaskSource.SetResult(handle, ready);
    loop.RunCore();
    Check(forwarded.SuppressCancellationThrow().GetResult().IsCanceled, "external cancellation was lost");
}

static void Complete(ulong[] handles, int index) =>
    Check(LuminTaskSource.SetResult(handles[index], 3 * index), "child was already complete");

static void Check(bool valid, string message)
{
    if (!valid) throw new InvalidOperationException(message);
}

static bool IsDebug()
{
#if DEBUG
    return true;
#else
    return false;
#endif
}

readonly record struct Payload(long First, long Second, string Name);
