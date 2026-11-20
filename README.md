<div align="center">

# ✨ LuminTask

**Lightweight async/await for Unity games and .NET**

*Zero allocations on warmed-up hot paths · One managed task source · Bounded idle object cache*

[![CI](https://github.com/TheLight-233/LuminTask/actions/workflows/ci.yml/badge.svg)](https://github.com/TheLight-233/LuminTask/actions/workflows/ci.yml)
[![Fuzz](https://github.com/TheLight-233/LuminTask/actions/workflows/fuzz.yml/badge.svg)](https://github.com/TheLight-233/LuminTask/actions/workflows/fuzz.yml)
[![NuGet](https://img.shields.io/nuget/v/LuminTask?style=flat-square&logo=nuget)](https://www.nuget.org/packages/LuminTask)
[![License](https://img.shields.io/github/license/TheLight-233/LuminTask?style=flat-square)](LICENSE)

**English** · [简体中文](docs/README.cn.md)

[Quick start](#quick-start) · [Unity](#unity) · [Documentation](#documentation) · [Benchmarks](benchmarks/README.md)

</div>

LuminTask targets games with tight frame, download and memory budgets. `LuminTask` and `LuminTask<T>` are task-like values, async method builders and awaiters. Synchronous success stays in the value. Pending operations use generation-checked slots in one managed task source.

| Feature | How it works |
| --- | --- |
| **Warm hot paths** | Reuses slots and typed state-machine runners when the working set fits. Growth, faults, cancellation registration and API-owned results have their own allocation costs. |
| **Managed and AOT-friendly** | Keeps small values inline, uses GC-tracked storage for references and shares the slot protocol across result types. |
| **Scene-level memory control** | Exposes the idle-cache budget and storage statistics. `LuminTaskSource.Trim()` releases idle storage at scene boundaries. |
| **Game APIs** | Frame waits, predicate waits, composition, cancellation, .NET task bridges and optional Unity adapters. |
| **Task ownership checks** | The NuGet package bundles the contract analyzer and the separate AsyncEx assembly. |

## Quick start

The package is `LuminTask` and the runtime namespace is `LuminThread`. The 2.0 build targets .NET Standard 2.1 and .NET 8, 9 and 10. Use the 2.0 release package when available or [build a candidate locally](docs/ci.md).

```csharp
using LuminThread;

static async LuminTask<int> LoadScoreAsync(CancellationToken cancellationToken)
{
    await LuminTask.Delay(25, cancellationToken: cancellationToken);
    return 42;
}

int score = await LoadScoreAsync(cancellationToken);
```

Consume each pending task once with `await`, `Forget()` or a consumer such as `WhenAll`. Copying a value copies its handle. [Task ownership and interop](docs/usage.md)

## Unity

Copy `Assets/LuminTask` from the Unity release ZIP and select .NET Standard 2.1 API compatibility. Install optional dependencies for the adapters you use. [Unity installation](eng/Unity-Installation.md)

```csharp
using LuminThread;
using LuminThread.Unity;

await LuminTask.DelayFrame(2, PlayerLoopTiming.Update, destroyToken);
await LuminTask.WaitUntil(state, static item => item.IsReady, PlayerLoopTiming.Update, destroyToken);
await button.OnClickAsync(destroyToken);
```

## Documentation

| Topic | Guide |
| --- | --- |
| Scheduling, cancellation, Unity and AsyncEx | [API guide](docs/usage.md) |
| Slot layout, storage lifetime and design rules | [Architecture](docs/architecture.md) · [中文](docs/architecture.cn.md) |
| Updating from 1.x | [Migration](docs/migration.md) |
| Test projects, CI and release candidates | [CI and release](docs/ci.md) · [Contributing](CONTRIBUTING.md) |
| Randomized operations and replay | [Fuzzing](docs/fuzzing.md) |
| CLR and Unity performance | [Benchmarks](benchmarks/README.md) |

LuminTask learned from [UniTask](https://github.com/Cysharp/UniTask) and [AsyncEx](https://github.com/StephenCleary/AsyncEx). [MIT license](LICENSE)
