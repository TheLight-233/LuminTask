```

BenchmarkDotNet v0.15.6, Windows 11 (10.0.26100.7171/24H2/2024Update/HudsonValley)
13th Gen Intel Core i7-13650HX 2.60GHz, 1 CPU, 20 logical and 14 physical cores
.NET SDK 8.0.101 [C:\Program Files\dotnet\sdk]
  [Host] : .NET 9.0.3 (9.0.3, 9.0.325.11113), X86 RyuJIT x86-64-v3

Error=NA  

```
| Method                      | Mean | Min | Max |
|---------------------------- |-----:|----:|----:|
| TestLuminAwaiterIsCompleted |   NA |  NA |  NA |
| TestUniAwaiterIsCompleted   |   NA |  NA |  NA |

Benchmarks with issues:
  SimpleBenchmark.TestLuminAwaiterIsCompleted: DefaultJob
  SimpleBenchmark.TestUniAwaiterIsCompleted: DefaultJob
