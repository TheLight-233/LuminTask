# LuminTask 基准测试

`SimpleBenchmark` 覆盖同步完成、挂起后完成、`WhenAll(2/100)` 和 `WhenAny`，并提供 `Task`、`ValueTask`、`UniTask` 基线。运行：

```bash
dotnet run --project benchmarks/LuminTask.Benchmarks/LuminTask.Benchmarks.csproj -c Release
```

基准使用 BenchmarkDotNet 的 `MemoryDiagnoser` 输出分配对比。结果依赖 SDK、运行时、处理器和操作系统，不在文档中固定填写数值；请保留完整输出与环境信息再比较。
