# LuminTask

**2.0** 使用一个全局任务源和纯托管分页槽。同步完成直接返回；真正挂起时才租用状态机。空闲对象跨线程共享，默认预算为 64 KiB，使用 `LuminTaskIdleCache.MaxIdleObjectBytes` 配置，场景卸载时可调用 `LuminTaskSource.Trim()`。通过 `LuminTaskSource.GetStorageStatistics()` 可读取活跃槽、已分配槽、槽数组字节数、缓存计费字节数和调度队列容量。原生分配器钩子和裸槽租还接口已移除，写入大值或含引用结构体的生产端请使用 `LuminTaskSource.Create<T>/Rent<T>`。

LuminTask 是一款面向 .NET 与 Unity 的高性能 async/await 库。用 `LuminTask` / `LuminTask<T>`（命名空间 `LuminThread`）替代 `Task` / `ValueTask`，`await`、`try/catch` 与 `CancellationToken` 用法完全不变，但异步热路径的开销被压到接近零。

它的设计初衷是同时解决移动端最关心的两件事：**包体**与**帧率稳定性**。泛型异步代码会为每个具体类型生成元数据与机器码，游戏里每一个 `async` 方法都会让 `global-metadata.dat` 与 `GameAssembly.dll` 变大——小游戏平台对首包体积极其敏感；而 `Task` 在异步路径上的每次分配最终都要由 GC 回收，移动端一次 GC 就是一次掉帧风险。LuminTask 围绕**极致性能、0 GC、更少的元数据、可控的缓存池内存驻留**设计，让中小型游戏项目直接受益。

> 项目公开 API 的命名空间是 `LuminThread`，NuGet 包名称是 `LuminTask`。包内自带编译期契约分析器。

## 📊 与 UniTask 的对比

对比对象为 UniTask 2.5.10。两臂使用同一份同构负载源码（仅机械替换库名），在同一台机器上以 IL2CPP + StandaloneWindows64 做 clean build；LuminTask 侧为最新构建，UniTask 侧工作负载源码未变、复用同机既有数据。构建产物与内存指标为确定性/稳定量；耗时项受会话负载影响，仅作参考。

| 库代码开销（纯库口径，引擎/BCL 不计） | LuminTask | UniTask | 差异 |
| --- | ---: | ---: | ---: |
| 库元数据（类型 / 方法 / 名字 / 参数等） | 138.4 KB | 442.2 KB | **−68.7%** |
| 库生成的 C/C++ 代码（不含泛型实例） | 2.05 MB | 7.18 MB | **−71.5%** |

同一份异步负载代码本身在两边几乎逐字节相同（50.7 KB，差 19 B）——元数据差异 99% 来自库自身（方法定义 775 → 3,541、类型定义 148 → 620）。整包参考（含引擎/BCL）：`global-metadata.dat` 2.66 MB vs 4.86 MB（−45.2%）、`GameAssembly.dll` 13.57 MB vs 41.14 MB（−67.0%），其中约三分之二是 UniTask 默认包连带拉入的 UGUI/TextMeshPro 引擎模块。

| 运行时 | LuminTask | UniTask | 差异 |
| --- | ---: | ---: | ---: |
| 进程保留内存 | 135.1 MB | 151.9 MB | **−11.0%** |
| 进程已分配内存 | 52.81 MB | 61.36 MB | **−13.9%** |
| 运行期净 GC 分配 | 2.78 MB | 4.80 MB | **−42.0%** |
| 托管堆存活（full GC 后） | 905 KB | 995 KB | **−9.1%** |
| 负载 pipeline 耗时 | 1563.4 ms | 1668.9 ms | −6.3% |

> 进程内存取自 `Profiler.GetTotalReservedMemoryLong` / `GetTotalAllocatedMemoryLong`；托管堆取自 `GC.GetTotalMemory(true)`。

优势集中在静态产物规模与分配流量上；托管堆存量（full GC 后）也低于 UniTask，因为 LuminTask 把每次操作的分配换成了有界的池化驻留，换来一条预热后零分配的热路径。

## ✨ 主要特性

* 0 GC 异步热路径：同步完成不分配，会挂起的异步方法在预热后也不再产生托管分配
* 更少的泛型元数据与生成代码：同等异步负载下，纯库口径的库自身元数据 **−68.7%**、生成代码 **−71.5%**，显著缩小 IL2CPP 包体与构建耗时
* 缓存池均有上限，内存驻留可控，不随负载峰值增长
* 8 字节结构体任务值，不持有托管引用，拷贝与传递近乎免费
* 子任务已完成的组合子直接同步返回，不创建对象
* 随包提供契约分析器（`LUM1001`–`LUM1008`），漏消费 / 重复消费在编译期报错
* 回调式 API 的零对象桥接：`LuminTaskSource` 句柄入口（`Rent` / `Create` / `ToTask` / `SetResult`）
* 完整组合子：`WhenAll`（1–4 参数 / 数组 / `IEnumerable`）、`WhenAny`、`WhenEach`、元组并发、`ContinueWith`、`Timeout`、`AttachExternalCancellation`、`WaitAsync`、`Unwrap`、`Retry`
* AOT / IL2CPP 友好，无反射与运行时代码生成
* AsyncEx 异步同步原语：`AsyncLock`、`AsyncSemaphore`、`AsyncLazy`、异步事件等
* DEBUG 构建会在进程退出时报告未被消费的任务及创建位置
* 支持 `netstandard2.1`、`net8.0`、`net9.0`、`net10.0`

## 安装

```bash
dotnet add package LuminTask
```

契约分析器与 AsyncEx 均随主包发布（包内 `analyzers/dotnet/cs/` 与 `lib/<tfm>/LuminTask.AsyncEx.dll`），安装后自动生效，无需额外引用。AsyncEx 是独立程序集且与核心库无依赖，不需要的工程可直接删除该 dll。Unity 项目需启用 .NET Standard 2.1 兼容级别。

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
// 组合、取消与超时
(int a, string b) = await LuminTask.WhenAll(TaskA(), TaskB());
await task.Timeout(TimeSpan.FromSeconds(5));
await task.AttachExternalCancellation(ct);
await task.ConfigureAwait(false);

// 句柄入口：由回调驱动完成，不产生额外对象
var task2 = LuminTaskSource.Create<int>(out var handle);
LuminTaskSource.SetResult(handle, 42);
await task2;
```

## 使用注意

LuminTask 遵循类似 `ValueTask` 的单次消费约定：一个任务实例应被 `await` 或 `Forget()` 消费一次，不应重复等待同一个实例。任务源**没有 GC 兜底**，漏消费即泄漏；DEBUG 构建会在进程退出时报告未被消费的任务及其创建位置。

`LuminTask<T>.Result` 仅对同步完成的任务有效；可能异步挂起的任务必须 `await` 或 `GetAwaiter().GetResult()`。同时存活的任务源上限为 32767。

契约分析器在编译期强制该约定（`LuminTask.Contract` 分类）：

| 规则 | 触发场景 |
| --- | --- |
| `LUM1001` | 任务从未被消费，或只在部分路径上被消费 |
| `LUM1002` | 任务被重复消费（含别名副本） |
| `LUM1003` | 对非同步完成的任务读取 `.Result` |
| `LUM1004` | `LuminTaskCompletionSource` 从未完成 |
| `LUM1005` | 把任务赋给丢弃符 `_` |
| `LUM1006` | 完成源在同一路径上被多次完成 |
| `LUM1007` | 任务存进字段，逃逸出方法级生命周期跟踪 |
| `LUM1008` | `LuminTaskSource.Rent` / `Create` 租出的句柄从未被使用 |

`await`、`Forget()`、`GetAwaiter().GetResult()`、`ConfigureAwait()`、`Unwrap()`、组合子参数传递、`return` 转发、`yield return`、被 `await` 的 lambda 捕获都算合法消费，不会误报。

## 许可证

本项目基于 MIT License 发布。

早期实现中曾学习并借鉴过 [UniTask](https://github.com/Cysharp/UniTask)（MIT，作者 Cysharp）的设计思路，部分异步同步原语（AsyncEx 系列）也参考了 [AsyncEx](https://github.com/StephenCleary/AsyncEx)（MIT，作者 Stephen Cleary）的思想。