# LuminTask API 速查表

> 命名空间均为 `LuminThread`。完整签名以源码为准。除已完成的工厂外，所有返回的任务都
> 必须被 `await` 或 `Forget()`。

## 静态工厂（LuminTask.Factory.cs / Run.cs）

| 签名 | 说明 |
|---|---|
| `CompletedTask()` / `FromResult()` | 已完成的 void 任务 |
| `FromResult<T>(T)` | 已完成并带值（同步内联） |
| `FromException(ex)` / `FromCanceled(ct)` | 已完成但失败 |
| `Never(ct)` | 永不完成直到取消 |
| `Run(Action/Func, bool configureAwait = true)` | 线程池执行 |
| `Create(Func<LuminTask>)` / `Create<T>(T state, Func<T, LuminTask>)` | 免闭包延迟创建 |

## 时间 / 帧 / 条件等待

- `Delay(int | TimeSpan, PlayerLoopTiming = DotNet, CancellationToken, bool cancelImmediately = false)`
- `Yield()` / `Yield(PlayerLoopTiming)` / `Yield(ct, cancelImmediately)`
- `WaitUntil(Func<bool>, timing = DotNet, ...)` / `WaitWhile(Func<bool>, ...)`
- `CancellationToken.WaitAsync(bool completeAsCanceled = false)` → `LuminTask`

## 组合子与扩展

- `WhenAll<T>(LuminTask<T>[], IEnumerable)` → `LuminTask<T[]>`；`WhenAll(in LuminTask...)` 1~4 参数异构重载
- `WhenAny(LuminTask[])` 等 → `LuminTask<int>`（完成索引）
- `WhenEach(...)` → 逐个产出完成的任务（配 `await foreach`）
- 元组并发：`await (task1, task2, ...)`（`GetAwaiter` 扩展到 4 元组）
- `ContinueWith(...)`（可选 `PlayerLoopTiming` 调度）
- `Timeout(TimeSpan, ct)`、`AttachExternalCancellation(ct)`、`WaitAsync(ct)`
- `Forget()`（各带 `Action<Exception>` 处理器重载）——唯一合法的"不等待"出口

## 互操作

- `Task.AsLuminTask()` / `ValueTask.AsLuminTask()` → `LuminTask`
- `LuminTask` 隐式转换 `ValueTask`
- `LuminTask<T>.Result` 仅限同步完成的任务；否则用 `await` / `GetAwaiter().GetResult()`

## AsyncEx（async 同步原语）

`AsyncLock`（`LockAsync()` → 可 `using` 的 `ReleaseScope`）、`AsyncSemaphore`、
`AsyncCountdownEvent`、`AsyncResetEvent`、`AsyncReaderWriterLock`（`ReaderLockAsync`/`WriterLockAsync`）、
`AsyncConditionVariable` / `AsyncMonitor`、`AsyncLazy`、`AsyncReactiveProperty`、`AsyncWaitQueue`。

## 异步流

- 接口：`ILuminTaskAsyncEnumerable<T>` / `ILuminTaskAsyncEnumerator<T>` / `ILuminTaskAsyncDisposable`
- `IConnectableLuminTaskAsyncEnumerable<T>`（`Connect()` 返回 `IDisposable`）
- 扩展：`WithCancellation(ct)`、`AsAsyncEnumerable()`、`ConfigureAwait(bool)`（适配 BCL `await foreach`）

## Unity 扩展（.ToLuminTask()）

- `AsyncOperation`（普通异步操作）/ `ResourceRequest` / `AssetBundleCreateRequest` /
  `AssetBundleRequest`（另有 `AwaitForAllAssets` 取全部资源）
- `UnityWebRequestAsyncOperation`（网络请求）
- `AsyncOperationHandle` / `AsyncOperationHandle<T>`（Addressables，取资源）
- `AsyncInstantiateOperation`、`AsyncGPUReadbackRequest`（AsyncGPUReadback）
- `JobHandle`（Jobs）
- `IEnumerator` → 用 LuminTask 代替协程；`WaitForSeconds` 等 Unity 原语可 await
- 全部支持 `IProgress<float>` 进度、`CancellationToken`、`cancelImmediately`、`PlayerLoopTiming` 参数

## 其它

- `YooAsset` 包加载、`Dotween` 动画、`TextMeshPro`、`UGUI` 事件均有专项扩展（Unity 目录）。