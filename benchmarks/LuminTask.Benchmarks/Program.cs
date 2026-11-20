using BenchmarkDotNet.Running;
using LuminThread;
using LuminTaskBenchmark;

if (args is ["--verify"])
{
    var simple = new SimpleBenchmark();
    int[] results =
    [
        simple.Task_Synchronous(), simple.ValueTask_Synchronous(), simple.LuminTask_Synchronous(),
        simple.UniTask_Synchronous(), simple.Task_Suspended(), simple.ValueTask_Suspended(),
        simple.LuminTask_Suspended(), simple.UniTask_Suspended()
    ];
    if (results.Any(static result => result != 42) || simple.LuminTask_WhenAll2() != 3 ||
        simple.LuminTask_WhenAll100() != 100 || simple.LuminTask_WhenAny() != 0)
        throw new InvalidOperationException("Unexpected async benchmark output.");

    var gameplay = new GameplayBenchmark();
    gameplay.Setup();
    try
    {
        gameplay.Yield();
        gameplay.DelayFrame();
        gameplay.WaitUntil();
    }
    finally { gameplay.Cleanup(); }

    if (LuminTaskSource.GetStorageStatistics().ActiveSlots != 0)
        throw new InvalidOperationException("The benchmark leaked task slots.");
    Console.WriteLine("Benchmarks verified.");
}
else
{
    var summaries = BenchmarkSwitcher.FromAssembly(typeof(SimpleBenchmark).Assembly)
        .Run(args.Length == 0 ? ["--filter", "*"] : args).ToArray();
    bool informationOnly = args.Contains("--list") || args.Contains("--help") || args.Contains("-h");
    if ((!informationOnly && summaries.Length == 0) || summaries.Any(summary =>
            summary.HasCriticalValidationErrors || summary.Reports.Length == 0 ||
            summary.Reports.Any(report => !report.Success)))
        Environment.ExitCode = 1;
}
