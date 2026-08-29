# LuminTask

LuminTask 是一个面向 .NET 的高性能异步任务库，为 C# 提供可直接用于 `async` / `await` 的 `LuminTask` 与 `LuminTask<T>` 类型。

项目通过自定义异步方法构建器、任务源池和非托管任务状态存储，减少高频异步路径中的托管堆分配。它适合游戏、框架、基础设施和对延迟敏感的服务代码，也可以作为 `Task` / `ValueTask` 的补充使用。

> 项目公开 API 的命名空间是 `LuminThread`，NuGet 包名称是 `LuminTask`。

## ✨ 主要特性

* 支持 `LuminTask` 与 `LuminTask<T>` 自定义 awaitable 类型
* 支持同步完成、异常、取消、延迟和让出执行权
* 提供 `WhenAll`、`WhenAny`、`WhenEach` 等异步组合操作
* 提供 `LuminTaskCompletionSource<T>`，便于桥接回调式 API
* 提供 `Forget`、`ContinueWith`、`Timeout` 和外部取消令牌绑定
* 支持 `ConfigureAwait`、顺序 `WhenAll`、可取消重试和嵌套任务 `Unwrap`
* 支持 `Task`、`ValueTask` 与 `LuminTask` 之间的互操作
* 支持将 `CancellationToken` 直接转换为可等待的 `LuminTask`
* 提供 `AsyncLock`、`AsyncSemaphore`、`AsyncLazy`、异步事件等工具类型
* 支持可配置的 `PlayerLoopTiming`、线程池调度和主线程切换
* 保留可选的 Unity 扩展，覆盖常用 Unity、Addressables、DotWeen、TextMeshPro 和 YooAsset 场景
* Debug 构建包含任务泄漏跟踪器，帮助发现未消费的任务源
* 支持 .NET Standard 2.1、.NET 8、.NET 9 和 .NET 10

## 📦 安装

从 NuGet 安装稳定版本：

```bash
dotnet add package LuminTask --version 1.0.5
```

> 安装即自带编译期契约分析器（见下文「契约分析器」），违反消费契约的代码会直接编译报错。

或者从源码构建：

```bash
dotnet build src/LuminTask/LuminTask.csproj -c Release
```

## 🚀 快速开始

### 自定义异步方法

`LuminTask` 和 `LuminTask<T>` 可以直接作为异步方法的返回值：

```csharp
using LuminThread;

static async LuminTask<int> CalculateAsync()
{
    await LuminTask.Yield();
    return 42;
}

var result = await CalculateAsync();
Console.WriteLine(result); // 42
```

### 同步完成任务

同步完成路径可以直接返回结果，不需要创建额外的异步状态机：

```csharp
using LuminThread;

static LuminTask<int> GetValue() => LuminTask.FromResult(123);
```

### 延迟、取消与异常

```csharp
using LuminThread;

static async LuminTask WorkAsync(CancellationToken cancellationToken)
{
    await LuminTask.Delay(100, cancellationToken: cancellationToken);
    await LuminTask.Yield(cancellationToken);
}
```

## 🧩 常用 API

| API | 用途 |
| --- | --- |
| `LuminTask.CompletedTask()` | 返回已完成的无返回值任务 |
| `LuminTask.FromResult<T>(value)` | 创建带结果的已完成任务 |
| `LuminTask.FromException(...)` | 创建异常任务 |
| `LuminTask.FromCanceled(...)` | 创建取消任务 |
| `LuminTask.Delay(...)` | 按时间或调度时机异步等待 |
| `LuminTask.Yield(...)` | 将后续执行让出到指定调度时机 |
| `LuminTask.Run(...)` | 执行同步或异步委托 |
| `LuminTask.WhenAll(...)` | 等待多个任务全部完成 |
| `LuminTask.WhenAny(...)` | 等待任一任务完成并返回获胜索引 |
| `LuminTask.WhenEach(...)` | 按完成顺序消费多个任务的结果 |
| `LuminTask.WaitUntil(...)` | 等待条件满足 |
| `LuminTaskCompletionSource<T>` | 手动控制任务完成、异常和取消 |

## 🔄 Task / ValueTask 互操作

LuminTask 提供常用的转换扩展：

```csharp
using LuminThread;

static async LuminTask<int> GetNumberAsync() => 42;

Task<int> task = GetNumberAsync().AsTask();
LuminTask<int> luminTask = task.AsLuminTask();
ValueTask<int> valueTask = luminTask.AsValueTask();
LuminTask<int> fromValueTask = valueTask.AsLuminTask();
```

取消令牌也可以直接等待，并选择正常完成或以取消状态完成：

```csharp
using LuminThread.Utility;

await cancellationToken.WaitAsync();
await cancellationToken.WaitAsync(completeAsCanceled: true);

await luminTask.WaitAsync(cancellationToken);
await luminTask.WaitAsync(TimeSpan.FromSeconds(5), cancellationToken);
```

由 `OperationCanceledException` 结束的异步方法、完成源和 `AsTask()` 转换都会保留取消状态与原始取消令牌。

任务源采用“完成后发布、首次消费后释放”的生命周期，并为每个结果类型保留一个无锁原生快槽；顺序异步工作负载在预热后可避免反复调用原生内存分配器。`WhenAll` / `WhenAny` 的回调状态使用有界池复用。

对于只需要忽略结果的后台任务，可以使用 `Forget()`。如果需要处理后台任务异常，可以传入异常处理回调。

## 🛡️ 契约分析器（LuminTask.Analyzer）

仓库包含一个基于 Roslyn 的源生成器分析器（`analyzer/LuminTask.Analyzer/`），在编译期强制 LuminTask 的单次消费契约，违反契约时直接编译报错：

| 诊断 | 严重级别 | 契约 |
| --- | --- | --- |
| `LUM1001` | Error | LuminTask 从未被消费（await / Forget / 转移所有权），任务源将泄漏 |
| `LUM1002` | Error | LuminTask 被多次消费（含副本别名、部分路径、循环内重复消费） |
| `LUM1003` | Error | 在非 `FromResult` 表达式上访问 `LuminTask<T>.Result` |
| `LUM1004` | Error | LuminTaskCompletionSource 创建后从未完成 |
| `LUM1005` | Error | 将 LuminTask 赋给丢弃符 `_`（不会释放任务源） |
| `LUM1006` | Warning | LuminTaskCompletionSource 在同一路径上被多次完成 |
| `LUM1007` | Warning | LuminTask 存储在字段中，无法静态跟踪单次消费生命周期 |
| `LUM9999` | Warning | 分析器内部错误（请提交可复现代码） |

### 自动生效（无需任何配置）

分析器随 LuminTask 主包一起发布：**`LuminTask` 的 NuGet 包内直接携带分析器 dll（`analyzers/dotnet/cs/`），消费者只需安装 LuminTask 一个包，分析器即自动生效**，无需额外安装任何东西。编译时分析器通过 `ReportDiagnostic` 在不遵守契约的位置直接报错（红线）。

```bash
dotnet add package LuminTask
# 完成。违反契约的代码现在会编译报错 LUM1001~LUM1006
```

主库自身构建时同样启用分析器（dogfooding，通过 ProjectReference 引用分析器项目，仅作用于库自身的编译）。

### 实现细节

* 基于 `IIncrementalGenerator`，目标框架 `netstandard2.0`，Roslyn 依赖锁定在 `4.3.0`（Unity 支持的最高版本），可直接用于 Unity 2021.2+。
* 分析器只分析使用方的代码；未引用 LuminTask 的编译不会产生任何诊断。
* 分析采用保守的控制流推断（分支合并、部分路径、循环重赋值、提前返回），遇到无法静态确认的合法模式可用 `#pragma warning disable LUMxxxx` 局部抑制。
* 主库内部实现（PlayerLoop 轮询延迟消费等）通过项目级 `NoWarn` 屏蔽 LUM 诊断。

测试项目 `tests/LuminTask.AnalyzerTests/` 通过内存编译运行分析器并断言诊断结果：

```bash
dotnet run --project tests/LuminTask.AnalyzerTests/LuminTask.AnalyzerTests.csproj -c Release
```

## 🧰 AsyncEx 工具类型`LuminThread.AsyncEx` 命名空间提供了一组基于 LuminTask 的异步同步原语：

* `AsyncLock`
* `AsyncReaderWriterLock`
* `AsyncSemaphore`
* `AsyncLazy<T>`
* `AsyncManualResetEvent` / `AsyncAutoResetEvent`
* `AsyncCountdownEvent`
* `AsyncConditionVariable` / `AsyncMonitor`

这些类型适合在不希望使用阻塞线程的场景中组织并发访问。

## 🎮 Unity 扩展

仓库中的 `Unity/` 目录提供可选的 Unity 集成代码，包括：

* Unity 常用对象和组件的异步扩展
* Addressables、DotWeen、TextMeshPro 和 YooAsset 扩展
* Unity PlayerLoop 接入与主线程调度

这些文件不参与当前 .NET 解决方案的默认构建，使用 Unity 时可按项目需要导入对应源文件。

## ⚠️ 使用注意

LuminTask 遵循类似 `ValueTask` 的单次消费约定：一个任务实例应被 `await` 或 `Forget()` 消费一次，不应重复等待同一个实例。

对于 `LuminTask<T>`，只有同步完成的任务可以读取 `Result`；对于可能异步挂起的任务，请使用 `await` 或 awaiter 获取结果。

任务槽位 id 采用 `short`，同时存活的任务源上限为 32767。达到上限时库会抛出包含当前存活数量的诊断异常；这也是库面向客户端/游戏高频短任务场景的容量边界。

Release 构建默认关闭 token 生命周期校验以保持低开销。需要在生产环境捕获重复消费或 token 失配时，可设置 `LuminTask.EnableRuntimeValidation = true`。

库内部使用非托管内存和 `unsafe` 代码，但普通调用方不需要在业务代码中开启 `unsafe`。

## ⚡ 性能与基准测试

仓库包含独立的 BenchmarkDotNet 项目。基准结果会受到处理器、操作系统、.NET 版本和编译配置影响，因此不在 README 中固定展示未经统一环境复现的数据。

运行基准测试：

```bash
dotnet run --project benchmarks/LuminTask.Benchmarks/LuminTask.Benchmarks.csproj -c Release
```

建议使用 Release 配置，并记录测试环境和完整输出后再进行不同库之间的比较。

## 🛠️ 从源码构建与测试

仓库使用 .NET SDK，版本约束见 [`global.json`](global.json)。

```bash
dotnet restore
dotnet build LuminTask.sln -c Release
dotnet run --project tests/LuminTask.Tests/LuminTask.Tests.csproj -c Release
dotnet pack src/LuminTask/LuminTask.csproj -c Release
```

测试项目是一个独立的控制台测试运行器，包含基础行为、组合操作、完成源、异步同步原语、压力和泄漏相关测试。

## 📁 项目结构

```text
LuminTask/
├── src/
│   └── LuminTask/             # 核心库与 NuGet 包项目（包内自带契约分析器）
├── analyzer/
│   └── LuminTask.Analyzer/    # Roslyn 契约分析器（源生成器，编译期强制消费契约）
├── tests/
│   ├── LuminTask.Tests/       # 单元测试、压力测试和泄漏测试
│   └── LuminTask.AnalyzerTests/  # 分析器内存编译测试
├── benchmarks/
│   └── LuminTask.Benchmarks/  # BenchmarkDotNet 基准测试
├── Unity/                     # 可选 Unity 集成扩展
├── LuminTask.sln              # Visual Studio / dotnet 解决方案（含 LuminTask / Tests / Benchmarks）
├── global.json                # .NET SDK 版本约束
├── LICENSE
└── README.md
```

## 🔮 后续计划

* 持续补充 Unity 版本矩阵和平台专项示例
* 根据真实工作负载继续调优任务源池容量与调度参数
* 在统一硬件上积累可复现的基准历史数据

## 📄 许可证

本项目基于 MIT License 发布，详情见 [`LICENSE`](LICENSE)。

早期实现中曾学习并借鉴过 [UniTask](https://github.com/Cysharp/UniTask)（MIT，作者 Cysharp）的设计思路，部分异步同步原语（AsyncEx 系列）也参考了 [AsyncEx](https://github.com/StephenCleary/AsyncEx)（MIT，作者 Stephen Cleary）的思想。
