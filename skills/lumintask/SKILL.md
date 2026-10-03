---
name: lumintask
description: Teaches correct usage of the LuminTask 0-GC async library, including await rules, combinators, cancellation, and Unity integration. Use when writing or reviewing code that uses LuminTask. Do not use for general C# async coding.
---

# LuminTask 使用说明书

LuminTask 是一个零分配（0-GC）的高性能 async/await 库，命名空间为 `LuminThread`。
用 `LuminTask` / `LuminTask<T>` 替代 `Task` / `ValueTask`，热路径不产生 GC 分配。
本 skill 只讲**怎么用**，不含内部源码实现。

## 1. 基础用法

写普通 `async` 方法，返回 `LuminTask` / `LuminTask<T>`：

```csharp
using LuminThread;
using System.Threading.Tasks;   // 需要 Task 互操作时

async LuminTask<int> SumAsync()
{
    // Run：切到线程池执行
    var a = await LuminTask.Run(() => 1 + 2);
    var b = await LuminTask.Delay(100).ContinueWith(() => 3);
    return a + b;
}
```

`await` 语法、`try/catch`、`CancellationToken` 与 `Task` 完全一致。`LuminTask` / `LuminTask<T>` 自己
就是编译器要求的 builder（`[AsyncMethodBuilder(typeof(LuminTask))]`）：挂起的 StateMachineBox 与同步
异常都放在任务槽位里，任务结构体本身仍是 8 字节（`LuminTask<T>` 为 8 + `sizeof(T)`，多出的部分是
同步完成的结果）。

## 2. 已完成的工厂方法

| API | 说明 |
|---|---|
| `LuminTask.CompletedTask()` | 已完成 |
| `LuminTask.FromResult<T>(T)` / `FromResult()` | 已完成并带值（同步路径零分配） |
| `LuminTask.FromException(ex)` / `FromCanceled(ct)` | 已完成但失败 |
| `LuminTask.Never(ct)` | 永不完成，直到令牌取消 |
| `LuminTask.Run(Action/Func, configureAwait = true)` | 线程池执行（`configureAwait: false` 可避免回主线程） |
| `LuminTask.Create(factory)` / `Create(state, factory)` | 免闭包延迟创建 |

## 3. 必须遵守的使用规则（违反会导致泄漏）

- **每个返回的 `LuminTask` 必须恰好被消费一次**：要么 `await`，要么显式 `task.Forget()`。
  任务槽位由 `LuminTaskBag` 管理，**没有 GC 兜底**；漏加一个 await/Forget 就泄漏一个槽位（DEBUG 下
  `LuminTaskLeakTracker` 会在进程退出时报告它和它的创建栈）。
- `LuminTask<T>.Result` **只对同步完成的任务有效**（如 `FromResult`）；异步结果必须
  `await` 或 `GetAwaiter().GetResult()`，语义同 `ValueTask`。
- 句柄自带槽位 generation：重复消费或使用已被回收的陈旧句柄，在任何构建配置下都会抛异常，
  不需要也不存在校验开关。
- **禁止 fire-and-forget**。安装 NuGet 包后契约分析器自动生效，编译器会直接报错，不要绕过它。
  规则一览（`LuminTask.Contract` 分类）：

  | 规则 | 触发场景 |
  |---|---|
  | `LUM1001` | 任务从未被消费，或只在部分路径上被消费 |
  | `LUM1002` | 任务被重复消费（含 `var b = a;` 这类别名副本） |
  | `LUM1003` | 对非同步完成的任务读取 `.Result` |
  | `LUM1004` | `LuminTaskCompletionSource` 从未完成 |
  | `LUM1005` | 把任务赋给丢弃符 `_`（丢弃不会释放任务源） |
  | `LUM1006` | 完成源在同一路径上被多次完成 |
  | `LUM1007` | 任务存进字段（逃逸出方法级生命周期跟踪） |
  | `LUM1008` | `LuminTaskSource.Rent` / `Create` 租出的句柄从未被使用 |

  `ConfigureAwait(bool)`、`Unwrap()`、`WhenAll`/`WhenAny` 的参数传递、`return` 转发、
  `yield return`、被 `await` 的 lambda 捕获，都算合法消费，不会误报。

#### 手动驱动完成（handle 路径）

需要从 PlayerLoop 回调、事件或定时器里自己驱动完成时，用句柄入口而不是 `TaskCompletionSource`：

```csharp
var task = LuminTaskSource.Create<int>(out var handle);   // 或 Rent(out var handle) 只租不建任务
// …把 handle 存进状态对象/闭包，稍后由回调完成：
LuminTaskSource.SetResult(handle, 42);                    // SetException / SetCanceled 同理
await task;

// 只保留了 handle 的场合可以随时重建任务（ToLuminTask 幂等的写法）
var again = LuminTaskSource.ToTask<int>(handle);
```

每个租出的句柄必须有**恰好一条**完成路径；句柄在方法内完全没被引用时，`LUM1008` 会报错。

## 4. 组合与算子

```csharp
// WhenAll：等待全部完成，返回结果数组
LuminTask<int[]> all = LuminTask.WhenAll(t1, t2, t3);          // 1~4 参数异构重载、数组、IEnumerable

// WhenAny：任意一个完成，返回索引
int idx = await LuminTask.WhenAny(t1, t2);

// 元组并发（无需装箱、零分配）
(int a, int b) = await (taskA, taskB);                          // 最多 4 元组

// WhenEach：逐个处理完成的
await foreach (var task in LuminTask.WhenEach(tasks))
    Use(await task);

// 超时 / 附加外部取消 / 取消令牌
await task.Timeout(TimeSpan.FromSeconds(5));
await task.AttachExternalCancellation(ct);
await ct.WaitAsync();                                           // 令牌触发即完成

// 连续操作
await task.ContinueWith(x => Finish(x), PlayerLoopTiming.Update); // 可指定调度时机
```

## 5. 时间与帧等待

```csharp
await LuminTask.Delay(500);                       // 毫秒；可用 TimeSpan，可选 CancellationToken
await LuminTask.Yield();                          // 让出当前循环
await LuminTask.Yield(PlayerLoopTiming.Update);   // 指定下一帧时机（Unity 下常用）
await LuminTask.WaitUntil(() => ready);           // 轮询条件，直到为 true
await LuminTask.WaitWhile(() => loading);         // 轮询条件，直到为 false
```

> 非 Unity 环境只有 `PlayerLoopTiming.DotNet` 被注册；直接用 `Update` 等时机

会抛 `InvalidOperationException`。Unity 中则全部可用（见第 7 节）。

## 6. 异步流（await foreach）

库自带 async 枚举契约 `ILuminTaskAsyncEnumerable<T>`，可无缝走进 BCL 的 `await foreach`：

```csharp
// 生产者：实现 ILuminTaskAsyncEnumerable<T>
public sealed class Counter : ILuminTaskAsyncEnumerable<int> { ... }

// 消费者
await foreach (var item in source.WithCancellation(ct))   // 带取消
    Use(item);

// 或转为标准 IAsyncEnumerable，并控制上下文捕获
await foreach (var item in source.AsAsyncEnumerable()) { }
await foreach (var item in source.ConfigureAwait(false)) { }
```

## 7. Unity 集成

Unity 桥（内置 `[RuntimeInitializeOnLoadMethod]` 自动初始化），主线程调度全自动，所有
`PlayerLoopTiming` 可用。Unity 对象直接 `.ToLuminTask()` 等待：

```csharp
// 任意 AsyncOperation / 资源加载 / 网络请求 / Job / Addressables / 协程
await assetBundle.CreateRequest().ToLuminTask();
await unityWebRequest.SendWebRequest().ToLuminTask();   // 或直接用 AwaitFor(AsyncOperation)
var tex = await Addressables.LoadAssetAsync<Sprite>(key).ToLuminTask<Texture2D>(); // AsyncOperationHandle
await jobHandle.ToLuminTask();                          // Jobs
await enumerator.ToLuminTask();                         // 用 LuminTask 替代协程
await new WaitForSeconds(1f);                           // 通过 GetAwaiter 扩展支持
```

各扩展默认时机：`PlayerLoopTiming.Update`，可传 `IProgress<float>` 上报进度，
可传 `CancellationToken`（`cancelImmediately` 控制取消后是否立即完成）。
另有 Dotween / TextMeshPro / UGUI 等专项扩展。

## 8. Async 同步原语（AsyncEx）

线程安全、零分配的异步原语，替代 lock/ManualResetEvent 的 async 版本：

```csharp
using (await _lock.LockAsync())            // AsyncLock（可重入）
    Use(sharedState);

await _semaphore.WaitAsync();              // AsyncSemaphore
await _countdown.SignalAsync();            // AsyncCountdownEvent
await _readerLock.ReaderLockAsync();       // AsyncReaderWriterLock
var value = await _lazy.Value;             // AsyncLazy，首次求值一次
await _prop.WaitAsync(x => x > 0);         // AsyncReactiveProperty 变体
```

## 9. 与 Task / ValueTask 互操作

```csharp
LuminTask lt = bclTask.AsLuminTask();          // Task -> LuminTask
LuminTask<T> ltT = bclTask.AsLuminTask<T>();
ValueTask vt = lt;                              // LuminTask 隐式转 ValueTask

// 注：LuminTask 装了 LeakTracker 时请用 AsLuminTask() 立刻消费，避免中间泄漏
```

## 10. 性能提示

- `LuminTask.Run(factory, configureAwait: false)` 或 `ConfigureAwait(false)` 可避免回主线程的上下文切换。
- `ConfigureAwait(bool)` 返回任务自身（不再产生 `ConfiguredLuminTaskAwaitable` 包装类型），
  `await t.ConfigureAwait(false)` 与 `await t` 走同一条 awaiter 路径，不引入额外的 await 方法实例化。
- 热路径避免闭包捕获：优先 `Create(state, factory)`、静态 lambda、`Action<object>`。
- 已完成的 `FromResult` / `CompletedTask` 本身零分配；组合子（WhenAll/WhenAny/元组）也零分配。
- `LuminTask.Yield()` 是一个真实任务（会租一个槽位，下一轮归还），不再是裸的 continuation 注册；
  换来了更少的等待器类型与方法实例化。高频 yield 的场景请留意槽位租还是否在预算内。
- 线程切换只保留两个等待器类型：`SwitchToAwaitable`（`SwitchToMainThread` / `SwitchToThreadPool` /
  `SwitchToTaskPool` / `SwitchToSynchronizationContext`）与 `ReturnToAwaitable`（`ReturnToMainThread` /
  `ReturnToSynchronizationContext` / `ReturnToCurrentSynchronizationContext`）。方法名不变，只是原先
  四个/两个等待器类型合并了；显式写类型名的代码需要跟着改。
- 用 DEBUG 构建的 leak tracker（`LuminTaskLeakTracker`）在开发期抓"未被消费"的任务。

## 常见陷阱

1. `async LuminTask` 方法体没有 await 语句，但仍返回任务——方法本身没问题，只要调用处 await/Forget 即可。
2. 两个 `await` 之间比较 `LuminTask` 时用 `Equals`/`==` 语义注意：struct 相等比较字段（id 相同才算同一任务）。
3. 循环里连续 `LuminTask.Delay`/`Yield` 未消费（如条件不成立时忘 Forget）会累积泄漏——DEBUG 下盯 LiveCount。