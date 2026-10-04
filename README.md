# **LuminTask**

## 📑 目录

- [📖 简介](#intro)
- [📊 与 UniTask 的对比](#benchmark)
- [✨ 主要特性](#features)
- [📦 Installation 安装](#installation)
- [🚀 Quick Start 快速开始](#quickstart)
- [🕐 时间、帧与线程调度](#timing)
- [🧩 组合与算子](#combinators)
- [🛑 取消、超时与上下文捕获](#cancellation)
- [🔄 异步流（await foreach）](#async-stream)
- [🔗 Task / ValueTask 互操作](#interop)
- [🛡️ 契约分析器](#analyzer)
- [🧰 AsyncEx 异步同步原语](#asyncex)
- [🎮 Unity 扩展](#unity)
- [⚠️ 使用注意](#notes)
- [🤖 AI Skill](#ai-skill)
- [🔮 后续更新计划](#roadmap)
- [📄 License 许可证](#license)

<a id="intro"></a>
## 📖 简介

LuminTask 是一款面向 .NET 与 Unity 的高性能 async/await 库。用 `LuminTask` / `LuminTask<T>`（命名空间 `LuminThread`）替代 `Task` / `ValueTask` 写异步方法，`await` 语法、`try/catch` 与 `CancellationToken` 用法完全不变，但异步热路径的开销被压到接近零。

### 为什么做 LuminTask

`Task` 是为通用服务端场景设计的：每次异步调用都要产生状态机与任务对象的分配，GC 压力不可避免。放到**手游、小游戏、独立游戏**里，这会变成两个很具体的问题：

*   **包体**：泛型异步代码会为每个具体类型生成一份元数据与机器码，游戏里每一个 `async` 方法都会往 `global-metadata.dat` 与 `GameAssembly.dll` 里加东西。小游戏平台对首包体积极其敏感，这类增长往往直接影响能不能过审上线。
*   **卡顿**：异步路径上的每次分配最终都要由 GC 回收。移动端一次 GC 就是一次掉帧风险，而游戏最怕的恰恰是帧率不稳。

LuminTask 的设计初衷就是同时解决这两件事：**极致性能、0 GC、更少的元数据、可控的缓存池内存驻留**，让中小型游戏项目在包体与帧率稳定性上直接受益。

### 它带来了什么

*   **0 GC 热路径**：同步完成的方法完全不分配；会挂起的异步方法在预热后也不再产生托管分配，异步不再制造 GC 抖动。
*   **更小的包体**：同等异步代码下，库自身的元数据与生成代码显著更少（纯库口径 **−68.7%** / **−71.5%**）。IL2CPP 整包实测 `global-metadata.dat` **−45.2%**、`GameAssembly.dll` **−67.0%**、随包总体积 **−30.4%**（见[下方对比](#benchmark)）。
*   **可控的内存驻留**：所有缓存池都有明确上限，不随负载峰值无限增长——用少量固定驻留换掉持续的分配。
*   **编译期契约**：随包提供分析器，漏消费、重复消费会在编译期报错，而不是变成线上偶发的泄漏。
*   **与 Task 世界互通**：`AsTask` / `AsLuminTask` / `AsValueTask` 双向转换，可以逐个方法平滑替换现有代码，调用方无需改造。

> 项目公开 API 的命名空间是 `LuminThread`，NuGet 包名称是 `LuminTask`。

LuminTask 在早期设计和实现过程中学习、借鉴了 [UniTask](https://github.com/Cysharp/UniTask)（MIT，作者 Cysharp）的设计思路，部分异步同步原语（AsyncEx 系列）也参考了 [AsyncEx](https://github.com/StephenCleary/AsyncEx)（MIT，作者 Stephen Cleary）的思想，随后按自身目标独立演进。

<a id="benchmark"></a>
## 📊 与 UniTask 的对比

对比对象为 UniTask 2.5.10。两臂使用**同一份同构负载源码**（仅机械替换库名，迭代次数、阶段与并发结构完全一致），在同一台机器上以 IL2CPP + StandaloneWindows64 做 clean build；LuminTask 侧为最新构建，UniTask 侧工作负载源码未变、复用同机既有数据。构建产物与内存指标为确定性/稳定量，可直接比较；耗时项受会话负载影响，仅作参考。

### 库代码开销（纯库口径）

> 口径：把玩家的 `global-metadata.dat` 与 C/C++ 生成代码**按程序集逐段归属**，只统计**两库自身**（LuminTask = 核心 + 其 Unity 接入；UniTask = 包全部）与**同一份异步负载代码**；引擎、BCL 与其他模块不计入。

| 指标 | LuminTask | UniTask | 差异 |
| --- | ---: | ---: | ---: |
| 库元数据：库主体 | 85.5 KB（148 类型 / 775 方法） | 391.5 KB（620 类型 / 3,541 方法） | |
| 库元数据：Unity 接入 | 2.2 KB（5 类型 / 18 方法） | 已含在库主体内 | |
| 同一份异步负载代码（含全部状态机） | 50.7 KB（95 类型 / 277 方法） | 50.7 KB（95 类型 / 277 方法） | +19 B |
| **库元数据合计** | **138.4 KB** | **442.2 KB** | **−68.7%（3.2×）** |
| 库生成的 C/C++ 代码（不含泛型实例） | 2.05 MB | 7.18 MB | **−71.5%** |

> 结论：同一份异步代码在两边产生的元数据几乎逐字节相同（差 19 B），**差异 99% 来自库自身**：方法定义 775 → 3,541、类型定义 148 → 620、名字池约 20 KB → 106 KB。
> 整包参考（含引擎/BCL）：`global-metadata.dat` 2.66 MB vs 4.86 MB（−45.2%）、`GameAssembly.dll` 13.57 MB vs 41.14 MB（−67.0%）、随包 22 文件 68.03 MB vs 97.81 MB（−30.4%）、clean build 47.3 s vs 105.9 s（−55.3%）；整包差额里约三分之二是 UniTask 默认包连带拉入的 UGUI/TextMeshPro 引擎模块（约 1.4 MB 元数据与相应 UIElements 泛型代码），不是库自身。
> 口径备注：名字池为长度估算（两臂同法）；自定义特性段未按程序集归属；泛型实例的生成代码在共享 `Generics*` / `GenericMethods*` 文件中，未按程序集切分。

### 运行时

| 指标 | LuminTask | UniTask | 差异 |
| --- | ---: | ---: | ---: |
| 进程保留内存 | 135.1 MB | 151.9 MB | **−11.0%** |
| 进程已分配内存 | 52.81 MB | 61.36 MB | **−13.9%** |
| 运行期净 GC 分配 | 2.78 MB | 4.80 MB | **−42.0%** |
| 托管堆存活（full GC 后） | 905 KB | 995 KB | **−9.1%** |
| 负载 pipeline 耗时 | 1563.4 ms | 1668.9 ms | −6.3% |
| 负载端到端耗时 | 1614.7 ms | 1727.6 ms | −6.5% |

> 口径说明：进程保留/已分配内存取自 `Profiler.GetTotalReservedMemoryLong` / `GetTotalAllocatedMemoryLong`（整进程原生分配器，含 IL2CPP 元数据与引擎）；托管堆存活取自 `GC.GetTotalMemory(true)`。整包体积统计排除 `*BackUpThisFolder*`，两臂产物文件数相同（22 个）。LuminTask 侧为最新构建；UniTask 侧工作负载源码未变、复用同机既有数据（工程侧依赖变动会使其绝对值相对更早记录略有上浮）。

LuminTask 的优势集中在**库自身的元数据与生成代码**（纯库口径 **−68.7%** / **−71.5%**）与**分配流量**（净分配 −42.0%）上：同等异步代码下，泛型元数据与生成代码显著更少；托管堆**存量**（full GC 后）也低于 UniTask 约 9%——缓存池驻留均有明确上限，LuminTask 用少量固定的池化驻留换掉持续的操作级分配，收益是一条**预热后零分配**的热路径。

<a id="features"></a>
## ✨ 主要特性

*   **0 GC 异步热路径**：同步完成不分配；会挂起的异步方法在预热后也不再产生托管分配，长时间运行不累积 GC 压力。
*   **更小的包体与元数据**：相比等价的 `Task` / `ValueTask` / UniTask 代码，泛型元数据与生成代码显著更少，IL2CPP 构建的包体与构建耗时同步下降。
*   **内存驻留可控**：缓存池均有明确上限，不随负载峰值增长；用少量固定驻留换掉持续的分配。
*   **轻量任务值**：`LuminTask` / `LuminTask<T>` 是 8 字节结构体，不持有托管引用，拷贝、传递、放进数组都近乎免费。
*   **即时可用的同步路径**：`FromResult` / `CompletedTask`，以及子任务都已完成的组合子，都会直接同步返回，不创建任何对象。
*   **编译期消费契约**：随包提供契约分析器（`LUM1001`–`LUM1008`），漏消费、重复消费、读取未同步完成的 `Result` 等直接编译期报错。
*   **完整的组合与算子**：`WhenAll`（1–4 参数异构 / 数组 / `IEnumerable`）、`WhenAny`、`WhenEach`、元组并发、`ContinueWith`、`Timeout`、`AttachExternalCancellation`、`WaitAsync`、`Unwrap`、`Retry`。
*   **回调式 API 的零对象桥接**：`LuminTaskSource` 提供句柄入口，供 PlayerLoop 回调、事件等场景驱动完成。
*   **AOT / IL2CPP 友好**：无反射、无运行时代码生成，适配 Unity Mono 与 IL2CPP。
*   **可选 Unity 扩展**：Unity PlayerLoop 接入、主线程调度、常用对象与组件异步化，以及 Addressables、DotWeen、TextMeshPro、YooAsset 集成。
*   **AsyncEx 异步同步原语**：`AsyncLock`、`AsyncSemaphore`、`AsyncLazy`、异步事件、倒计时、条件变量等。
*   **开发期泄漏跟踪**：DEBUG 构建下会自动报告未被消费的任务及其创建位置。
*   **目标框架**：`netstandard2.1`、`net8.0`、`net9.0`、`net10.0`。

<a id="installation"></a>
## 📦 Installation 安装

### .NET 项目

```shell
dotnet add package LuminTask
```

契约分析器与 AsyncEx 均随主包一同发布（包内 `analyzers/dotnet/cs/` 与 `lib/<tfm>/LuminTask.AsyncEx.dll`），安装 `LuminTask` 一个包即自动生效，无需额外引用。

### Unity 项目

Unity 需启用 **.NET Standard 2.1** API 兼容级别。将 `netstandard2.1` 版本的 `LuminTask.dll` 与仓库 `Unity/` 目录下的集成源码放入工程即可；集成代码内置自动初始化，主线程调度无需手动接入。

AsyncEx（`LuminTask.AsyncEx.dll`）已拆为独立程序集，与核心库无任何依赖关系：不需要异步同步原语的工程可以不导入、或直接删除该 dll，核心功能不受影响。

### 从源码引用

```xml
<ItemGroup>
  <ProjectReference Include="path/to/LuminTask/src/LuminTask/LuminTask.csproj" />
</ItemGroup>
```

<a id="quickstart"></a>
## 🚀 Quick Start 快速开始

直接写普通 `async` 方法，把返回类型换成 `LuminTask` / `LuminTask<T>`：

```csharp
using LuminThread;

static async LuminTask<int> CalculateAsync()
{
    await LuminTask.Yield();          // 让出当前循环
    var loaded = await LuminTask.Run(() => 40 + 2);   // 切到线程池
    return loaded;
}

var result = await CalculateAsync();
Console.WriteLine(result);            // 42
```

同步完成的任务不需要状态机，也就不会租用槽位：

```csharp
static LuminTask<int> GetValue() => LuminTask.FromResult(123);
static LuminTask Done() => LuminTask.CompletedTask();
```

工厂方法一览：

| API | 说明 |
| --- | --- |
| `LuminTask.CompletedTask()` | 已完成（零句柄） |
| `LuminTask.FromResult<T>(value)` / `FromResult()` | 已完成并带值（同步路径零分配） |
| `LuminTask.FromException(ex)` / `FromCanceled(ct)` | 已完成但失败 |
| `LuminTask.Run(factory, configureAwait = true)` | 线程池执行 |
| `LuminTask.Create(factory)` / `Create(state, factory)` | 免闭包延迟创建 |
| `LuminTaskSource.Create<T>(out var handle)` | 句柄入口：返回任务并交出完成权 |

<a id="timing"></a>
## 🕐 时间、帧与线程调度

```csharp
await LuminTask.Delay(500);                        // 毫秒，也接受 TimeSpan
await LuminTask.Delay(500, cancellationToken: ct); // 可取消
await LuminTask.Yield();                           // 让出到下一轮
await LuminTask.Yield(PlayerLoopTiming.Update);    // 指定调度时机（Unity 下常用）
await LuminTask.WaitUntil(() => ready);            // 轮询直到条件为真
await LuminTask.WaitWhile(() => loading);          // 轮询直到条件为假
await LuminTask.WaitUntilCanceled(ct);             // 等待令牌被取消
await LuminTask.WaitUntilValueChanged(target, x => x.Health);  // 等待成员变化
```

非 Unity 环境只注册 `PlayerLoopTiming.DotNet`（线程池 pump）；使用未注册的时机（如 `Update`）会抛出 `InvalidOperationException`。Unity 下全部时机可用，由 Unity 扩展自动接入。

<a id="combinators"></a>
## 🧩 组合与算子

```csharp
// WhenAll：全部完成。1~4 个异构参数直接返回元组；数组 / IEnumerable 返回结果数组
(int a, string b) = await LuminTask.WhenAll(TaskA(), TaskB());
int[] all = await LuminTask.WhenAll(new[] { T1(), T2(), T3() });
await LuminTask.WhenAllSequential(tasks);            // 顺序（非并发）等待

// WhenAny：任意一个完成，返回获胜索引
int index = await LuminTask.WhenAny(T1(), T2(), T3(), T4());

// 元组并发：语法糖，等价于 WhenAll
(int x, int y) = await (Tx(), Ty());

// WhenEach：按完成顺序消费
await foreach (var task in LuminTask.WhenEach(tasks))
    Use(await task);

// 链式与重试
await task.ContinueWith(x => Finish(x), PlayerLoopTiming.Update);
await LuminTask.Retry(() => ConnectAsync(), retryCount: 3, initialDelay: TimeSpan.FromMilliseconds(100));
await outer.Unwrap();                                 // LuminTask<LuminTask<T>> → LuminTask<T>
```

全部组合子在**子任务已同步完成**时会短路为同步结果，不分配任何对象。

<a id="cancellation"></a>
## 🛑 取消、超时与上下文捕获

```csharp
await task.Timeout(TimeSpan.FromSeconds(5));            // 超时抛 TimeoutException
await task.AttachExternalCancellation(ct);              // 外部令牌取消该任务
await task.WaitAsync(ct);                               // ct 触发即结束等待
await task.WaitAsync(TimeSpan.FromSeconds(5), ct);      // 超时 + 取消
await ct.WaitAsync();                                   // 直接把令牌变成可等待对象
await ct.WaitAsync(completeAsCanceled: true);           // 以“已取消”状态完成
(bool canceled, T value) = await task.SuppressCancellationThrow();  // 不抛异常地观察取消
```

`ConfigureAwait(bool)` 控制是否捕获当前执行上下文：

```csharp
await task.ConfigureAwait(false);   // 不回主线程 / 不捕获上下文
```

它直接返回任务自身，捕获策略写在槽位里，因此不引入额外的 awaitable / awaiter 类型。

<a id="async-stream"></a>
## 🔄 异步流（await foreach）

库自带 async 枚举契约 `ILuminTaskAsyncEnumerable<T>`，可直接用于 BCL 的 `await foreach`：

```csharp
await foreach (var item in source.WithCancellation(ct))
    Use(item);

await foreach (var item in source.AsAsyncEnumerable()) { }
await foreach (var item in source.ConfigureAwait(false)) { }
```

<a id="interop"></a>
## 🔗 Task / ValueTask 互操作

```csharp
Task<int> bcl = luminTask.AsTask();          // LuminTask -> Task
ValueTask<int> vt = luminTask;               // 隐式转换
LuminTask<int> back = bcl.AsLuminTask();     // Task -> LuminTask
LuminTask<int> fromVt = vt.AsLuminTask();
```

注意 `AsTask()` / `AsLuminTask()` 都会消费（或产生）一个任务源，转换后请按单次消费契约处理。由 `OperationCanceledException` 结束的异步方法、完成源与 `AsTask()` 转换都会保留取消状态与原始取消令牌。

<a id="analyzer"></a>
## 🛡️ 契约分析器

仓库包含基于 Roslyn 的源生成器分析器（`analyzer/LuminTask.Analyzer/`），在编译期强制单次消费契约。它随 LuminTask 主包发布，**消费者只需安装一个包即自动生效**：

| 诊断 | 严重级别 | 契约 |
| --- | --- | --- |
| `LUM1001` | Error | 任务从未被消费（`await` / `Forget` / 转移所有权），或仅在部分路径上被消费 |
| `LUM1002` | Error | 任务被重复消费（含副本别名、部分路径、循环内重复消费） |
| `LUM1003` | Error | 在非同步完成的表达式上访问 `LuminTask<T>.Result` |
| `LUM1004` | Error | `LuminTaskCompletionSource` 创建后从未完成 |
| `LUM1005` | Error | 将任务赋给丢弃符 `_`（不会释放任务源） |
| `LUM1006` | Warning | 完成源在同一路径上被多次完成 |
| `LUM1007` | Warning | 任务存储在字段中，无法静态跟踪单次消费生命周期 |
| `LUM1008` | Error | `LuminTaskSource.Rent` / `Create` 租出的句柄从未被使用（不存在任何完成路径） |
| `LUM9999` | Warning | 分析器内部错误（请提交可复现代码） |

* 基于 `IIncrementalGenerator`，目标框架 `netstandard2.0`，Roslyn 依赖锁定 `4.3.0`（Unity 支持的最高版本），可直接用于 Unity 2021.2+。
* 合法消费不会被误报：`await`、`Forget()`、`GetAwaiter().GetResult()`、`ConfigureAwait()`、`Unwrap()`、组合子参数传递、`return` 转发、`yield return`、被 `await` 的 lambda 捕获都计入消费。
* 分析只作用于使用方代码；未引用 LuminTask 的编译不会产生任何诊断。无法静态确认的合法模式可用 `#pragma warning disable LUMxxxx` 局部抑制。

<a id="asyncex"></a>
## 🧰 AsyncEx 异步同步原语

`LuminThread.AsyncEx` 命名空间提供基于 LuminTask 的异步同步原语：

```csharp
using (await _lock.LockAsync())            // AsyncLock
    Use(sharedState);

await _semaphore.WaitAsync();              // AsyncSemaphore
await _countdown.SignalAsync();            // AsyncCountdownEvent
await _readerLock.ReaderLockAsync();       // AsyncReaderWriterLock
var value = await _lazy.Value;             // AsyncLazy<T>，首次求值一次
await _manualEvent.WaitAsync();            // AsyncManualResetEvent / AsyncAutoResetEvent
await _property.WaitAsync(x => x > 0);     // AsyncReactiveProperty
```

另有 `AsyncConditionVariable` / `AsyncMonitor`、`LuminChannel`（异步通道）等。

<a id="unity"></a>
## 🎮 Unity 扩展

仓库 `Unity/` 目录提供可选的 Unity 集成：内置 `[RuntimeInitializeOnLoadMethod]` 自动初始化，主线程调度全自动，所有 `PlayerLoopTiming` 可用。

```csharp
await assetBundle.CreateRequest().ToLuminTask();                  // 资源加载
await unityWebRequest.SendWebRequest().ToLuminTask();             // 网络请求
await Addressables.LoadAssetAsync<Sprite>(key).ToLuminTask<Texture2D>();
await jobHandle.ToLuminTask();                                    // Jobs
await enumerator.ToLuminTask();                                   // 用 LuminTask 替代协程
await new WaitForSeconds(1f);                                     // 通过 GetAwaiter 扩展
await unityEvent.ToLuminTask();                                   // UGUI 事件
```

各扩展默认时机为 `PlayerLoopTiming.Update`，可传 `IProgress<float>` 上报进度、传 `CancellationToken`（`cancelImmediately` 控制取消后是否立即完成）。另有 Dotween、TextMeshPro、YooAsset 专项扩展。Unity 对象自身的 `.GetAwaiter()` 也已被扩展覆盖，可直接 `await`。

<a id="notes"></a>
## ⚠️ 使用注意

*   **单次消费**：一个任务实例必须被 `await`、`Forget()` 消费一次，或转移所有权（参数 / 返回值 / 字段）。任务源**没有 GC 兜底**，漏消费即泄漏；DEBUG 构建会在进程退出时报告未被消费的任务及其创建位置。
*   **`Result` 仅对同步完成有效**：`LuminTask<T>.Result` 只在任务同步完成时可用（如 `FromResult`）；可能异步挂起的任务必须 `await` 或 `GetAwaiter().GetResult()`，语义同 `ValueTask`。
*   **陈旧任务会被拒绝**：重复消费、或使用一个已被回收的旧任务，在所有构建配置下都会抛出；校验永远开启，没有开关。
*   **容量上限**：同时存活的任务源上限为 32767；达到上限会抛出带当前存活数量的诊断异常。这是面向高频短任务场景的容量边界。
*   **句柄入口的完成权**：`LuminTaskSource.Rent` / `Create` 租出的句柄必须有且只有一条完成路径（`SetResult` / `SetException` / `SetCanceled`），否则槽位不归还（`LUM1008` 会拦下完全未被使用的情况）。
*   **Unity 环境外的调度时机**：非 Unity 环境仅 `PlayerLoopTiming.DotNet` 可用。

<a id="ai-skill"></a>
## 🤖 AI Skill（供 AI 助手使用）

仓库附带一个面向 AI 助手 / Agent 的 Skill：`skills/lumintask/`。它是一份 LuminTask **使用说明书**，目的是让 AI 写出正确使用 LuminTask 的代码，覆盖：

- 基础用法、工厂方法、时间与帧等待
- 必须遵守的单次消费规则与契约分析器 `LUM1001`–`LUM1008` 规则表
- 手动驱动完成的句柄路径（`Rent` / `Create` / `ToTask` / `Set*`）
- 组合子、取消与超时、异步流、AsyncEx 原语
- Unity 集成、与 `Task` / `ValueTask` 互操作、性能提示与常见陷阱

Skill 内含可直接照抄的示例代码，并附 `references/api-surface.md` API 速查表。

### 安装 / 使用

- **支持仓库 `skills/` 目录自动发现的环境**：克隆仓库后即可直接使用，无需额外操作。
- **Agent Skills 兼容环境**：将 `skills/lumintask/` 复制到 IDE 的 Skill 目录（如 `.trae/skills/`、`.claude/skills/`）。
- **手动参考**：直接阅读 `skills/lumintask/SKILL.md` 与 `skills/lumintask/references/api-surface.md`。

<a id="roadmap"></a>
## 🔮 后续更新计划

1.  补充 Unity 版本矩阵与平台专项示例
2.  根据真实工作负载继续调优槽位池与调度参数

<a id="license"></a>
## 📄 License 许可证

本项目基于 MIT License 发布，详情见 [`LICENSE`](LICENSE)。