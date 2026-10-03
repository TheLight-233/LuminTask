# LuminTask

LuminTask 是一个面向 .NET 的高性能异步任务库，为 C# 提供可直接用于 `async` / `await` 的 `LuminTask` 与 `LuminTask<T>` 类型。

项目通过自定义异步方法构建器、任务源池和非托管任务状态存储，减少高频异步路径中的托管堆分配。

> 项目公开 API 的命名空间是 `LuminThread`，NuGet 包名称是 `LuminTask`。

## 主要特性

* 自定义 `LuminTask` 与 `LuminTask<T>` awaitable 类型
* 支持同步完成、异常、取消、延迟和让出执行权
* 支持 `WhenAll`、`WhenAny`、`WhenEach` 等组合操作
* 支持 `Task`、`ValueTask` 与 `LuminTask` 互操作
* 支持 `CancellationToken.WaitAsync()` 异步桥接
* 提供异步锁、信号量、事件、倒计时和懒加载工具
* 支持 .NET Standard 2.1、.NET 8、.NET 9 和 .NET 10

## 安装

```bash
dotnet add package LuminTask
```

契约分析器（Roslyn 源生成器）随包提供于 `analyzers/dotnet/cs`，安装后自动生效，无需额外引用。

## 快速开始

```csharp
using LuminThread;

static async LuminTask<int> CalculateAsync()
{
    await LuminTask.Yield();
    return 42;
}

var result = await CalculateAsync();
```

```csharp
using LuminThread.Utility;

LuminTask<int> task = new ValueTask<int>(42).AsLuminTask();
await cancellationToken.WaitAsync(completeAsCanceled: true);
await task.WaitAsync(TimeSpan.FromSeconds(5), cancellationToken);
```

## 使用注意

LuminTask 遵循类似 `ValueTask` 的单次消费约定：一个任务实例应被 `await` 或 `Forget()` 消费一次，不应重复等待同一个实例。
任务槽位由 `LuminTaskBag` 管理，**没有 GC 兜底**，漏消费即泄漏；DEBUG 构建下 `LuminTaskLeakTracker` 会在进程退出时报告泄漏槽位及其创建栈。

契约分析器在编译期强制该约定：

| 规则 | 含义 |
| --- | --- |
| `LUM1001` | 任务从未被消费（或仅在部分路径上被消费） |
| `LUM1002` | 任务被重复消费 |
| `LUM1003` | 读取 `LuminTask<T>.Result`，但任务并非同步完成 |
| `LUM1004` | 完成源从未被完成 |
| `LUM1005` | 把任务赋给丢弃符 `_` |
| `LUM1006` | 完成源在同一路径上被多次完成 |
| `LUM1007` | 任务存放在字段中，逃逸出方法级生命周期跟踪 |
| `LUM1008` | `LuminTaskSource.Rent` / `Create` 租出的句柄从未被使用 |

### 1.0.7 变更

* `ConfigureAwait(bool)` 直接返回 `LuminTask` / `LuminTask<T>` 自身（捕获策略写在槽位里），原先公开的
  `ConfiguredLuminTaskAwaitable[<T>]` / `ConfiguredLuminTaskAwaiter[<T>]` 已移除。
  `await task.ConfigureAwait(false)` 的写法与语义不变，但每个 `T` 少两个泛型类型与一组 await 方法实例化。
* 移除泛型 `ForgetBlock<T>`：`Forget<T>()` 复用非泛型块并经 `ConsumeVoid` 退出（结果本就被丢弃），
  每个 `T` 少一个泛型类型与每线程 16 个保留对象。
* `LuminTaskCold` 的冷侧存储改为 64 槽子块按需分配：每个被触达字段的驻留从 2,080 B 降到 592 B
  （16 字节元素为 4,128 → 1,104 B）。

## 许可证

本项目基于 MIT License 发布。

早期实现中曾学习并借鉴过 [UniTask](https://github.com/Cysharp/UniTask)（MIT，作者 Cysharp）的设计思路，部分异步同步原语（AsyncEx 系列）也参考了 [AsyncEx](https://github.com/StephenCleary/AsyncEx)（MIT，作者 Stephen Cleary）的思想。
