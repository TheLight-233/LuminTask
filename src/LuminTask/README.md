# LuminTask

LuminTask 是一个面向 .NET 的高性能异步任务库，为 C# 提供可直接用于 `async` / `await` 的 `LuminTask` 与 `LuminTask<T>` 类型。

项目通过自定义异步方法构建器、任务源池和非托管任务状态存储，减少高频异步路径中的托管堆分配。

> 项目公开 API 的命名空间是 `LuminThread`，NuGet 包名称是 `LuminTask`。

## 主要特性

* 自定义 `LuminTask` 与 `LuminTask<T>` awaitable 类型
* 支持同步完成、异常、取消、延迟和让出执行权
* 支持 `WhenAll`、`WhenAny`、`WhenEach` 等组合操作
* 支持 `Task`、`ValueTask` 与 `LuminTask` 互操作
* 提供异步锁、信号量、事件、倒计时和懒加载工具
* 支持 .NET Standard 2.1、.NET 8 和 .NET 9

## 安装

```bash
dotnet add package LuminTask --version 1.0.2
```

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

## 使用注意

LuminTask 遵循类似 `ValueTask` 的单次消费约定：一个任务实例应被 `await` 或 `Forget()` 消费一次，不应重复等待同一个实例。

## 许可证

本项目基于 MIT License 发布。
