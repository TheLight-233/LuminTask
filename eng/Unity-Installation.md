# Install LuminTask in Unity

1. Select the .NET Standard 2.1 API compatibility level in Player Settings.
2. Copy `Assets/LuminTask` into the project's `Assets` directory. If the project already provides `System.Runtime.CompilerServices.Unsafe.dll`, keep a single compatible assembly.
3. To enable an optional adapter, copy its directory and its matching `.meta` file from `Optional` into `Assets/LuminTask`. Install the corresponding Unity or third-party package first.
4. To enable contract diagnostics, copy `Optional/LuminTask.Analyzer.dll`. Disable its runtime platform imports in the Inspector and assign the `RoslynAnalyzer` asset label.

Use `using LuminThread;` for tasks and `using LuminThread.Unity;` for Unity adapters. Unity initializes the PlayerLoop at startup.
