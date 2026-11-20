# LuminTask API guide

## Wait and compose

Use `LuminThread` for core APIs and `LuminThread.Unity` for Unity adapters. Explicitly choose a Unity `PlayerLoopTiming` when the continuation needs the Unity thread. On threaded .NET hosts the default `DotNet` timing uses the timer pump. The Unity WebGL player integrates that timing into its main loop.

```csharp
await LuminTask.Yield(PlayerLoopTiming.Update);
await LuminTask.DelayFrame(3, PlayerLoopTiming.Update, cancellationToken);
await LuminTask.WaitUntil(state, static s => s.Ready, PlayerLoopTiming.Update, cancellationToken);

var (inventory, profile) = await LuminTask.WhenAll(LoadInventory(), LoadProfile());
bool canceled = await LuminTask.DelayFrame(1, PlayerLoopTiming.Update, cancellationToken)
    .SuppressCancellationThrow();
```

`Delay` measures elapsed wall-clock time via `Stopwatch`. `DelayFrame` counts scheduled loop passes. Pass a cached delegate or a static typed predicate to keep caller-side closures out of a repeated wait. Typed array `WhenAll` produces a result array in input order. `WhenAny` owns all inputs and drains late results. `WhenEach` yields outcomes as inputs finish, and owns remaining inputs after enumeration starts.

## Ownership and interop

Consume a pending task once. Use `AsTask()` when the result must be shared by multiple standard .NET consumers. `AsValueTask()` bridges to `ValueTask` and transfers ownership of the pending task. Use `AsLuminTask()` to adapt a .NET task. Use `Forget()` for deliberate fire-and-forget or pass an exception handler.

Plain awaits resume on the producer's execution path. `ConfigureAwait(true)` opts in to execution-context capture. Use explicit Unity scheduling to reach the Unity thread.

The NuGet package includes the Roslyn analyzer. The runtime checks slot generations. Key diagnostics include `LUM1001` for an abandoned task, `LUM1002` for repeated consumption, and `LUM1008` for abandoning a rented source handle. [Diagnostic reference](diagnostics.md)

## Unity and AsyncEx

The Unity extension `GetOnDestroyCancellationToken()` follows the GameObject lifetime. The native coroutine overload uses the host component destroy token on Unity versions that provide it.

One-shot UnityEvent subscription can allocate. Reuse the handler across repeated waits and dispose it with its owner.

```csharp
using var clicks = button.GetAsyncClickEventHandler(cancellationToken);
await clicks.OnClickAsync();
await clicks.OnClickAsync();
```

`LuminThread.AsyncEx` provides `AsyncLock`, `AsyncSemaphore`, `AsyncLazy<T>`, `LuminChannel` and other synchronization primitives. The main NuGet package contains the separate AsyncEx assembly.
