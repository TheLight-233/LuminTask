<div align="center">

# ✨ LuminTask

**为 Unity 小游戏与 .NET 设计的轻量异步运行库**

*预热后热路径零分配 · 全局单一托管任务源 · 有预算的空闲缓存*

[![CI](https://github.com/TheLight-233/LuminTask/actions/workflows/ci.yml/badge.svg)](https://github.com/TheLight-233/LuminTask/actions/workflows/ci.yml)
[![Fuzz](https://github.com/TheLight-233/LuminTask/actions/workflows/fuzz.yml/badge.svg)](https://github.com/TheLight-233/LuminTask/actions/workflows/fuzz.yml)
[![NuGet](https://img.shields.io/nuget/v/LuminTask?style=flat-square&logo=nuget)](https://www.nuget.org/packages/LuminTask)

[English](../README.md) · **简体中文**

[快速开始](#快速开始) · [Unity](#unity) · [文档](#文档)

</div>

`LuminTask` 和 `LuminTask<T>` 同时承担任务值、Builder 和 Awaiter 的职责。同步成功直接返回，挂起的操作使用全局 `LuminTaskSource` 的托管槽。成功热路径在预热且容量足够时可做到 0 B/操作。AOT 生成代码、缓存驻留和操作分配分别量化。

## 快速开始

使用 2.0 发布包，或用[本地发包脚本](ci.md)生成候选包。NuGet 包名为 `LuminTask`，命名空间为 `LuminThread`。支持 .NET Standard 2.1 与 .NET 8、9、10。

```csharp
using LuminThread;

static async LuminTask<int> LoadScoreAsync(CancellationToken cancellationToken)
{
    await LuminTask.Delay(25, cancellationToken: cancellationToken);
    return 42;
}
```

挂起任务的所有权随句柄传递。将任务交给 `await`、`Forget()` 或组合子消费一次。使用 `LuminTaskSource.GetStorageStatistics()` 观察槽位、对象池和调度数组，用 `LuminTaskSource.Trim()` 释放空闲存储。

## Unity

从 Unity 发布 ZIP 复制 `Assets/LuminTask`，并将项目的 API 兼容级别设为 .NET Standard 2.1。适配包括 PlayerLoop、资源和网络请求、UGUI 事件与协程等待。可按项目需要引入 TMP、Addressables、DOTween 或 YooAsset 适配。[安装步骤](../eng/Unity-Installation.md)

```csharp
using LuminThread;
using LuminThread.Unity;

await LuminTask.DelayFrame(2, PlayerLoopTiming.Update, destroyToken);
await LuminTask.WaitUntil(state, static item => item.IsReady, PlayerLoopTiming.Update, destroyToken);
await button.OnClickAsync(destroyToken);
```

## 文档

| 主题 | 说明 |
| --- | --- |
| 等待、组合、取消、互操作和 AsyncEx | [API 指南](usage.md) |
| 单一任务源、槽位与缓存预算 | [架构约束](architecture.cn.md) |
| 从 1.x 迁移 | [迁移说明](migration.md) |
| 测试、CI 和发布 | [工程与发布](ci.md) · [参与贡献](../CONTRIBUTING.md) |
| 随机序列和失败复现 | [Fuzz](fuzzing.md) |
| 性能与分配复测 | [基准](../benchmarks/README.md) |

[MIT 许可证](../LICENSE)
