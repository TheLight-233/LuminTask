# LuminTask

Read [CONTRIBUTING.md](CONTRIBUTING.md) for commands and [docs/architecture.md](docs/architecture.md) for runtime constraints.

Keep the managed global task source and generation-checked ownership protocol. Preserve callback execution outside slot and pool gates. Core cannot depend on Unity or AsyncEx. Use the shared idle budget for pooled structures and clear references before returning them.

When changing a runtime path, check the corresponding runtime, fuzz or Unity fixture. For changes to allocation or retained storage, compare identical warmed workloads and record the hardware, runtime configuration and measurement scope. For packaging changes, build and test a release candidate with `eng/release.ps1`.
