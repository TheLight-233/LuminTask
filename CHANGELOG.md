# Changelog

## 2.0.0

- Use one managed task source with generation-checked handles and a shared idle cache. Synchronous successful results stay in the task value.
- Add storage statistics, an idle cache budget and explicit trimming for scene transitions.
- Reduce work and retained queue storage for frame waits. Release Unity subscriptions and settle waits on cancellation and native coroutine host destruction.
- Bundle the core, AsyncEx and the contract analyzer in the NuGet package. Ship a Unity archive with optional adapters.
- Add cross-platform CI, seeded operation fuzzing, reproducible benchmarks and package consumer checks including NativeAOT.
