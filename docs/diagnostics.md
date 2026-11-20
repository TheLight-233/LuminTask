# Contract diagnostics

The Roslyn component ships in the NuGet package. For Unity, follow the [analyzer installation steps](../eng/Unity-Installation.md). The runtime also checks slot ownership when compiled without the analyzer.

| ID | Severity | Contract |
| --- | --- | --- |
| `LUM1001` | Error | Consume a task on each path or transfer its ownership. |
| `LUM1002` | Error | Consume a single pending task once, including copies of its handle. |
| `LUM1003` | Error | Reserve `Result` for synchronous task values. |
| `LUM1004` | Error | Complete a created completion source. |
| `LUM1005` | Error | Use `Forget()` for deliberate fire-and-forget. |
| `LUM1006` | Warning | Review multiple completions of the same source. |
| `LUM1007` | Warning | Review ownership of tasks kept in fields. |
| `LUM1008` | Error | Complete a rented task handle. |
| `LUM9999` | Warning | Report analyzer failures with a minimal case. |

The analyzer inspects declared methods and cannot prove every usage through fields, dynamic dispatch or compiler-generated entry points. Keep ownership explicit in those cases.
