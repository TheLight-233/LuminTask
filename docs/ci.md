# CI and releases

| Check | Project or entry point |
| --- | --- |
| Runtime semantics, allocation and resource lifetime | `tests/LuminTask.Tests`, .NET 8/9/10 |
| Roslyn contract cases | `tests/LuminTask.Tests.Analyzer` |
| Seeded operation sequences | `tests/LuminTask.Fuzzing` |
| Runtime assembly boundaries | `tools/LuminTask.RepositoryChecks` |
| Published assembly and analyzer installation | `tests/LuminTask.Tests.Package` |
| NativeAOT compilation of the candidate | `tests/LuminTask.Tests.Aot` |
| Windows IL2CPP installation and behaviour | `tests/LuminTask.Tests.Unity` |
| Benchmark correctness and measurements | `benchmarks/LuminTask.Benchmarks`, `benchmarks/LuminTask.SuspensionBenchmarks` |

CI builds on Windows, Linux and macOS and runs Debug on Linux. The manual benchmark workflow stores the raw results. The manual Unity workflow targets a licensed Windows runner with Unity 2022.3 IL2CPP and Visual C++ build tools.

## Candidate

Use the .NET 10 SDK and PowerShell 7. Choose a new output directory.

```powershell
./eng/release.ps1 -Version 2.0.0-rc.1 -Output artifacts/rc.1 -NativeAot
./eng/test-unity.ps1 -UnityEditor $editor -Archive artifacts/rc.1/packages/LuminTask.Unity.2.0.0-rc.1.zip
```

The script checks assemblies and symbols for every target, installs the exact package from an isolated feed, and exercises the installed analyzer. `-NativeAot` tests the candidate with the platform's native toolchain. The candidate includes the NuGet package and symbols, Unity ZIP, checksums and changelog.

## Tagged release

1. Set the version in `eng/Versions.props`, update `CHANGELOG.md`, and check the release candidate.
2. Configure the GitHub `nuget` environment, `NUGET_USER` variable and a NuGet.org Trusted Publishing policy for the repository and `release.yml`. [Trusted Publishing documentation](https://learn.microsoft.com/en-us/nuget/nuget-org/trusted-publishing)
3. Push the matching `v<version>` tag when ready to publish. The tag workflow validates the version and verifies artifact checksums before publishing. The GitHub Release attaches the Unity ZIP and candidate files.

The Unity workflow uses a self-hosted runner with the labels `Windows`, `X64` and `unity-il2cpp`. Set `LUMINTASK_UNITY_EDITOR` to the editor's absolute path.
