using BenchmarkDotNet.Attributes;
using LuminThread;

namespace LuminTaskBenchmark;

[MemoryDiagnoser]
public class GameplayBenchmark
{
    private const PlayerLoopTiming Timing = PlayerLoopTiming.Update;
    private bool _ready;
    private ManagedPlayerLoop _loop = null!;

    [GlobalSetup]
    public void Setup()
    {
        _loop = new ManagedPlayerLoop();
        PlayerLoopHelper.SetStrategy(Timing, _loop);
    }

    [GlobalCleanup]
    public void Cleanup() => _loop.Dispose();

    [Benchmark]
    public void Yield()
    {
        var task = LuminTask.Yield(Timing);
        _loop.RunCore();
        task.GetResult();
    }

    [Benchmark]
    public void DelayFrame()
    {
        var task = LuminTask.DelayFrame(2, Timing);
        _loop.RunCore();
        _loop.RunCore();
        task.GetResult();
    }

    [Benchmark]
    public void WaitUntil()
    {
        _ready = false;
        var task = LuminTask.WaitUntil(this, static benchmark => benchmark._ready, Timing);
        _loop.RunCore();
        _ready = true;
        _loop.RunCore();
        task.GetResult();
    }
}
