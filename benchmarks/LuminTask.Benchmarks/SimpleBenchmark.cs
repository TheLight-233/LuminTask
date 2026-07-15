namespace LuminTaskBenchmark;

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using Cysharp.Threading.Tasks;
using LuminThread;

[HideColumns("StdDev", "RatioSD", "Error")]
[MinColumn, MaxColumn]
[MemoryDiagnoser]
public class SimpleBenchmark
{
    public const int ITERATIONS = 100000;
    public const int WARMUP_ITERATIONS = 1000;
    
    [MethodImpl(MethodImplOptions.NoOptimization)]
    public static async UniTask<int> Unitask_ComplexWorkflow()
    {
        int result = 0;
        for (int i = 0; i < 1; i++)
        {
            result += i;
        }
        
        return await UniTask.FromResult(result);
    }

    [MethodImpl(MethodImplOptions.NoOptimization)]
    public static async LuminTask<int> LuminTask_ComplexWorkflow()
    {
        int result = 0;
        for (int i = 0; i < 1; i++)
        {
            result += i;
        }
        return await LuminTask<int>.FromResult(result);
    }
    
    [Benchmark]
    public bool TestLuminAwaiterIsCompleted() {
        var awaiter = LuminTask.FromResult(42).GetAwaiter();
        bool result = false;
        for (int i = 0; i < 100000; i++) {
            result = awaiter.IsCompleted; // 直接测试属性
        }
        return result;
    }

    [Benchmark]
    public bool TestUniAwaiterIsCompleted() {
        var awaiter = UniTask.FromResult(42).GetAwaiter();
        bool result = false;
        for (int i = 0; i < 100000; i++) {
            result = awaiter.IsCompleted;
        }
        return result;
    }
}
