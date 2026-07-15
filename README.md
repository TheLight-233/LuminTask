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
* 支持 `Task`、`ValueTask` 与 `LuminTask` 之间的互操作
* 提供 `AsyncLock`、`AsyncSemaphore`、`AsyncLazy`、异步事件等工具类型
* 支持可配置的 `PlayerLoopTiming`、线程池调度和主线程切换
* 保留可选的 Unity 扩展，覆盖常用 Unity、Addressables、TextMeshPro 和 YooAsset 场景
* Debug 构建包含任务泄漏跟踪器，帮助发现未消费的任务源
* 支持 .NET Standard 2.1、.NET 8 和 .NET 9

## 📦 安装

从 NuGet 安装稳定版本：

```bash
dotnet add package LuminTask --version 1.0.2
```

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
```

对于只需要忽略结果的后台任务，可以使用 `Forget()`。如果需要处理后台任务异常，可以传入异常处理回调。

## 🧰 AsyncEx 工具类型

`LuminThread.AsyncEx` 命名空间提供了一组基于 LuminTask 的异步同步原语：

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
* Addressables、TextMeshPro 和 YooAsset 扩展
* Unity PlayerLoop 接入与主线程调度

这些文件不参与当前 .NET 解决方案的默认构建，使用 Unity 时可按项目需要导入对应源文件。

## ⚠️ 使用注意

LuminTask 遵循类似 `ValueTask` 的单次消费约定：一个任务实例应被 `await` 或 `Forget()` 消费一次，不应重复等待同一个实例。

对于 `LuminTask<T>`，只有同步完成的任务可以读取 `Result`；对于可能异步挂起的任务，请使用 `await` 或 awaiter 获取结果。

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
│   └── LuminTask/             # 核心库与 NuGet 包项目
├── tests/
│   └── LuminTask.Tests/       # 单元测试、压力测试和泄漏测试
├── benchmarks/
│   └── LuminTask.Benchmarks/  # BenchmarkDotNet 基准测试
├── Unity/                     # 可选 Unity 集成扩展
├── LuminTask.sln              # Visual Studio / dotnet 解决方案
├── global.json                # .NET SDK 版本约束
├── LICENSE
└── README.md
```

## 🔮 后续计划

* 持续补充 API 文档和典型使用场景
* 完善跨平台 CI 构建、测试与 NuGet 发布流程
* 增加更多统一环境下的基准测试报告
* 持续优化异步组合操作和任务源池

## 📄 许可证

本项目基于 MIT License 发布，详情见 [`LICENSE`](LICENSE)。

项目地址：[TheLight-233/LuminTask](https://github.com/TheLight-233/LuminTask)
