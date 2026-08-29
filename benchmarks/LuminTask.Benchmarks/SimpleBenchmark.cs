using System;
using System.Linq;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Cysharp.Threading.Tasks;
using LuminThread;

namespace LuminTaskBenchmark;

[MemoryDiagnoser]
[HideColumns("StdDev", "RatioSD", "Error")]
public class SimpleBenchmark
{
    [Benchmark(Baseline = true)]
    public int Task_Synchronous() => Task.FromResult(42).GetAwaiter().GetResult();

    [Benchmark]
    public int ValueTask_Synchronous() => new ValueTask<int>(42).GetAwaiter().GetResult();

    [Benchmark]
    public int LuminTask_Synchronous() => LuminTask.FromResult(42).GetAwaiter().GetResult();

    [Benchmark]
    public int UniTask_Synchronous() => UniTask.FromResult(42).GetAwaiter().GetResult();

    [Benchmark]
    public int Task_Suspended() => TaskWorkflow().GetAwaiter().GetResult();

    [Benchmark]
    public int ValueTask_Suspended() => ValueTaskWorkflow().GetAwaiter().GetResult();

    [Benchmark]
    public int LuminTask_Suspended() => LuminTaskWorkflow().GetAwaiter().GetResult();

    [Benchmark]
    public int UniTask_Suspended() => UniTaskWorkflow().GetAwaiter().GetResult();

    [Benchmark]
    public int LuminTask_WhenAll2()
    {
        var result = LuminTask.WhenAll(LuminTask.FromResult(1), LuminTask.FromResult(2)).GetAwaiter().GetResult();
        return result.Item1 + result.Item2;
    }

    [Benchmark]
    public int LuminTask_WhenAll100()
    {
        LuminTask.WhenAll(Enumerable.Range(0, 100).Select(_ => LuminTask.CompletedTask())).GetAwaiter().GetResult();
        return 100;
    }

    [Benchmark]
    public int LuminTask_WhenAny() => LuminTask.WhenAny(LuminTask.CompletedTask(), LuminTask.CompletedTask()).AsTask().GetAwaiter().GetResult();

    private static async Task<int> TaskWorkflow()
    {
        await Task.Yield();
        return 42;
    }

    private static async ValueTask<int> ValueTaskWorkflow()
    {
        await Task.Yield();
        return 42;
    }

    private static async LuminTask<int> LuminTaskWorkflow()
    {
        await LuminTask.Yield();
        return 42;
    }

    private static async UniTask<int> UniTaskWorkflow()
    {
        await UniTask.Yield();
        return 42;
    }
}
