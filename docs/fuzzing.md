# Operation fuzzing

The fuzzer generates seeded operation sequences with expected results, status, cancellation tokens, and ownership. It exercises concurrent completion and trimming, stale handles, wide joins, early enumeration disposal, wait timing and idle-cache capacity.

```sh
dotnet run --project tests/LuminTask.Fuzzing -c Release -- 20261120 2000 artifacts/fuzz/repro.txt
```

The arguments are the seed, iteration count and failure report path. The report includes the seed, iteration, case, replay command and exception. Replay repeats the inputs, while thread scheduling can vary. For a timing-dependent failure, repeat the seed and turn the isolated ordering into a focused regression test.

The Fuzz workflow runs three seeds in Release and Debug, with larger iteration counts on pushes to `refactor`, the weekly schedule and manual dispatch.
