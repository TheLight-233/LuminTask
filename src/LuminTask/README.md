# LuminTask

Lightweight async/await for .NET and Unity. The package includes the task runtime, the AsyncEx assembly and a Roslyn analyzer for task ownership.

```csharp
using LuminThread;

static async LuminTask<int> LoadScoreAsync(CancellationToken cancellationToken)
{
    await LuminTask.Delay(25, cancellationToken: cancellationToken);
    return 42;
}
```

The library targets .NET Standard 2.1 and .NET 8, 9 and 10. Synchronous success stays in the task value. Pending operations share a managed task source and an idle cache with a configurable budget. Consume each pending task exactly once.

[Documentation, source and Unity adapters](https://github.com/TheLight-233/LuminTask)
