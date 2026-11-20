using System;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Cysharp.Threading.Tasks;
using LuminThread;

namespace LuminTaskBenchmark;

[MemoryDiagnoser]
[HideColumns("StdDev", "RatioSD", "Error")]
public class SimpleBenchmark
{
    private readonly LuminTask[] _completed = new LuminTask[100];

    [Benchmark(Baseline = true)]
    public int Task_Synchronous() => Task.FromResult(42).GetAwaiter().GetResult();

    [Benchmark]
    public int ValueTask_Synchronous() => new ValueTask<int>(42).GetAwaiter().GetResult();

    [Benchmark]
    public int LuminTask_Synchronous() => LuminTask.FromResult(42).GetAwaiter().GetResult();

    [Benchmark]
    public int UniTask_Synchronous() => UniTask.FromResult(42).GetAwaiter().GetResult();

    [Benchmark]
    public int Task_Suspended()
    {
        var source = new TaskCompletionSource<int>();
        var result = TaskWorkflow(source.Task);
        source.SetResult(42);
        return result.GetAwaiter().GetResult();
    }

    [Benchmark]
    public int ValueTask_Suspended()
    {
        var source = new TaskCompletionSource<int>();
        var result = ValueTaskWorkflow(new ValueTask<int>(source.Task));
        source.SetResult(42);
        return result.GetAwaiter().GetResult();
    }

    [Benchmark]
    public int LuminTask_Suspended()
    {
        var input = LuminTaskSource.Create<int>(out ulong handle);
        var result = LuminTaskWorkflow(input);
        LuminTaskSource.SetResult(handle, 42);
        return result.GetResult();
    }

    [Benchmark]
    public int UniTask_Suspended()
    {
        var source = new UniTaskCompletionSource<int>();
        var result = UniTaskWorkflow(source.Task);
        source.TrySetResult(42);
        return result.GetAwaiter().GetResult();
    }

    [Benchmark]
    public int LuminTask_WhenAll2()
    {
        var result = LuminTask.WhenAll(LuminTask.FromResult(1), LuminTask.FromResult(2)).GetAwaiter().GetResult();
        return result.Item1 + result.Item2;
    }

    [Benchmark]
    public int LuminTask_WhenAll100()
    {
        LuminTask.WhenAll(_completed).GetResult();
        return _completed.Length;
    }

    [Benchmark]
    public int LuminTask_WhenAny() => LuminTask.WhenAny(LuminTask.CompletedTask(), LuminTask.CompletedTask()).GetResult();

    private static async Task<int> TaskWorkflow(Task<int> input) => await input;
    private static async ValueTask<int> ValueTaskWorkflow(ValueTask<int> input) => await input;
    private static async LuminTask<int> LuminTaskWorkflow(LuminTask<int> input) => await input;
    private static async UniTask<int> UniTaskWorkflow(UniTask<int> input) => await input;
}
