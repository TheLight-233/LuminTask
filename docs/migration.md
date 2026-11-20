# Migrate to LuminTask 2.0

- Keep the `LuminThread` namespace. Update the .NET package or replace the Unity assemblies and adapters together from the release ZIP.
- Move native allocator hook or raw slot code to `LuminTaskSource.Create<T>` or `Rent<T>`. Complete with the same `T`. The producer completes the handle and the caller consumes the task.
- Consume a pending result once with `await` or a supported bridge. Use `AsTask()` for a shared .NET result.
- Inspect scene teardown with `LuminTaskSource.GetStorageStatistics()`. Set the idle budget with `LuminTaskIdleCache.MaxIdleObjectBytes` in `LuminThread.Utility`. Call `Trim()` at an idle boundary.
- The Unity adapter sources now live in `src/LuminTask.Unity`. The release ZIP includes its core adapters and an optional adapter directory.
