# Contributing to LuminTask

Use the .NET 10 SDK. Install the .NET 8 and .NET 9 runtimes to run every supported target locally.

```sh
dotnet build LuminTask.sln -c Release
dotnet run --project tests/LuminTask.Tests -c Release -f net10.0 --no-build
dotnet run --project tests/LuminTask.Tests.Analyzer -c Release --no-build
dotnet run --project tests/LuminTask.Fuzzing -c Release -- 20261120 2000
dotnet run --project tools/LuminTask.RepositoryChecks -c Release --no-build -- . Release
dotnet run --project benchmarks/LuminTask.Benchmarks -c Release -- --verify
```

Describe the affected task lifetime and validation when contributing a change. Include reproducible results for changes to performance or AOT reachability. [Architecture](docs/architecture.md) · [CI and release](docs/ci.md) · [Benchmarks](benchmarks/README.md)

- `src/` contains the runtime, optional async primitives, analyzer and Unity integration.
- `tests/` contains runtime, analyzer, fuzz, package, AOT and Unity checks.
- `benchmarks/` contains managed and Unity fixtures.
- `eng/` contains version metadata and release entry points.
- `tools/` contains repository validation utilities.
