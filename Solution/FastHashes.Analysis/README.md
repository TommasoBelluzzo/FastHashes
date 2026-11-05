# FastHashes.Analysis — AN-000

Infrastructure for the FastHashes statistical-quality analysis workbench. Targets .NET 10 and references the sibling `FastHashes` library project; no new NuGet packages are required.

## Installation

Copy `FastHashes.Analysis` into `Solution/`, alongside `FastHashes.Benchmarks` and `FastHashes.Tests`. From `Solution/` run:

```powershell
dotnet sln FastHashes.sln add FastHashes.Analysis/FastHashes.Analysis.csproj
```

## Commands

```powershell
dotnet run -c Release --project FastHashes.Analysis -- --help
dotnet run -c Release --project FastHashes.Analysis -- --list
dotnet run -c Release --project FastHashes.Analysis -- --hash XxHash64 --analysis smoke --generator-seed 12345 --samples 32 --key-length 64 --output Results
```

Optional `--hash-seed` is passed to the registry's standardized seeded constructor; omit it to use the algorithm's default configuration. Some algorithms constrain the supported seed range.

## Output

A successful smoke run writes `summary.json`, `summary.txt`, and `smoke.csv` under `<output>/<hash>/<UTC timestamp>-<unique suffix>/`. The CSV contains deterministic test inputs and hashes. The run ID and metadata timestamp are not deterministic; regenerate the same CSV with identical algorithm version, algorithm configuration, generator seed, sample count and key length.

**AN-000 does not perform any statistical quality test.** The smoke run verifies execution, deterministic input generation, registered hash instantiation, and file reporting; it is not a conformance-vector suite. The planned five quality drivers (avalanche, bit independence, collisions, distribution, structural robustness) are intentionally not implemented here.

Input generation uses SplitMix64 with explicit little-endian byte extraction and wrapping arithmetic; hash-seed and input-generator seed are distinct. No cryptographic guarantees are claimed.

The project deliberately leaves the existing library, tests, benchmarks, and solution file unchanged. Statistical driver implementations can be introduced in subsequent phases.
