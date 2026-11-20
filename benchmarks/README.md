# LuminTask 基准测试

`SimpleBenchmark` 覆盖同步完成、挂起后完成、`WhenAll(2/100)` 和 `WhenAny`，并提供 `Task`、`ValueTask`、`UniTask` 基线。`GameplayBenchmark` 通过手动 PlayerLoop 测量 `Yield`、`DelayFrame` 和带状态的 `WaitUntil`。CI 的 `--verify` 检查结果和活动任务槽。BenchmarkDotNet 支持按用例筛选。

```bash
dotnet run --project benchmarks/LuminTask.Benchmarks -c Release -- --filter "*GameplayBenchmark*"
dotnet run --project benchmarks/LuminTask.Benchmarks -c Release -- --verify
```

基准使用 BenchmarkDotNet 的 `MemoryDiagnoser` 输出分配对比。结果依赖 SDK、运行时、处理器和操作系统，不在文档中固定填写数值；请保留完整输出与环境信息再比较。

挂起组先启动等待，再由调用方显式完成输入，最后同步消费结果。计量包含输入完成源的创建成本。ValueTask 输入包装 .NET Task，以便由相同类型的完成源驱动。WhenAll(100) 复用调用方提供的已完成输入数组。

## 挂起路径分段与长窗对照

`LuminTask.SuspensionBenchmarks` 不依赖 BenchmarkDotNet，包含 source 的租用/完成/消费分段、真实回调挂起、三步结果链、手动泵驱动的 Yield，以及 Task 对照。测量循环使用预分配存储，输出每操作耗时、当前线程托管分配、gen0 次数和最终 Bag 占用。

```powershell
dotnet build benchmarks/LuminTask.SuspensionBenchmarks/LuminTask.SuspensionBenchmarks.csproj -c Release -o artifacts/perf-after
$env:DOTNET_TieredCompilation = '0'
dotnet artifacts/perf-after/LuminTask.SuspensionBenchmarks.dll 1000000 5
# 可选第三个参数：逗号分隔的用例名。
dotnet artifacts/perf-after/LuminTask.SuspensionBenchmarks.dll 1000000 5 'Yield-Lumin,Yield-Task'
```

比较两个版本时，使用同一份基准程序、相同运行时和相同 JIT 设置，仅替换被测 LuminTask.dll，交错运行并保留原始输出。分段中的“完成”在挂起版本中会内联执行内层消费和状态机恢复，不能理解成纯生产端耗时。`sync-int` 可被 JIT 大幅简化，只作为同步路径烟测。

每次对照保留运行时、硬件、JIT 设置、测试参数和原始样本。基准工作流会上传计时与分配结果。

## Unity IL2CPP

`Unity/` 是独立播放器夹具，测试 int、UnityEngine.Vector3、Quaternion 的真实回调挂起，以及 Task<int> 对照。额外执行实际 Unity PlayerLoop 的 Yield、DelayFrame、带值 WaitUntil 和取消校验。`prepare.ps1` 只准备指定的临时项目，不改动现有游戏工程。

以下命令从仓库根目录执行，需要已安装 Unity Windows IL2CPP 模块和 C++ 构建工具。修改 Unity 路径以匹配本机安装；本次对照使用 Unsafe 6.0.0 的 netstandard2.0 程序集，两臂完全相同。

```powershell
dotnet build src/LuminTask/LuminTask.csproj -c Release -f netstandard2.1
$unityExe = 'C:/Program Files/Unity/Hub/Editor/6000.0.43f1/Editor/Unity.exe'
$unsafeDll = "$env:USERPROFILE/.nuget/packages/system.runtime.compilerservices.unsafe/6.0.0/lib/netstandard2.0/System.Runtime.CompilerServices.Unsafe.dll"
./benchmarks/Unity/prepare.ps1 -CoreAssembly src/LuminTask/bin/Release/netstandard2.1/LuminTask.dll -UnsafeAssembly $unsafeDll
$repo = (Get-Location).Path
$buildArgs = "-batchmode -nographics -quit -projectPath `"$repo/artifacts/unity-extreme`" -executeMethod BenchBuild.Run -benchBuildOut `"$repo/artifacts/unity-run/Bench.exe`" -logFile `"$repo/artifacts/unity-build.log`""
$build = Start-Process $unityExe -ArgumentList $buildArgs -WindowStyle Hidden -Wait -PassThru
if ($build.ExitCode -ne 0) { throw 'Unity build failed; inspect unity-build.log' }
$runArgs = "-batchmode -nographics -benchOut `"$repo/artifacts/unity-run/results.txt`" -logFile `"$repo/artifacts/unity-run/player.log`""
$player = Start-Process "$repo/artifacts/unity-run/Bench.exe" -ArgumentList $runArgs -WindowStyle Hidden -Wait -PassThru
if ($player.ExitCode -ne 0) { throw 'Unity player failed; inspect player.log' }
Get-Content artifacts/unity-run/results.txt
```

构建为 IL2CPP、Release、High stripping、OptimizeSize，启用 CleanBuildCache。添加 `-bench32` 构建 Windows x86；默认 x64。比较两版时只更换 `-CoreAssembly` 和输出目录，保持夹具及构建设置一致，关闭编译工作后以 A/B/B/A 顺序运行。

耗时每项预热 20,000 次、测量 5 轮，每轮 1,000,000 次，保留正常 GC。分配另以 100,000 次测量：先收集再暂时禁用 GC，取 `GC.GetTotalMemory(false)` 差值。这是进程级、按堆块统计的净占用变化，不能等同 .NET 的精确线程累计分配；`collections` 也不是 .NET 的分代 GC 概念。Unity 6000.0.43f1 IL2CPP 的 `GC.GetAllocatedBytesForCurrentThread()` 未实现，会触发原生中断，不能使用。

成功结果必须包含 `player_loop=pass`、`live=0`、匹配的 checksum 和退出码 0。包体只比较随包文件；排除 PDB、日志、`Bench_BackUpThisFolder_ButDontShipItWithYourGame` 和 `Bench_BurstDebugInformation_DoNotShip` 等构建调试产物。`BuildReport.totalSize` 不能直接用作发布包大小。
