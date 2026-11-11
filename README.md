# FastHashes

FastHashes is a suite of .NET non-cryptographic hash functions.

<table>
  <tr>
    <td align="right">Status:</td>
    <td align="left">
      <a href="https://github.com/TommasoBelluzzo/FastHashes/actions/workflows/continuous_integration.yml"><img alt="Build" src="https://img.shields.io/github/actions/workflow/status/TommasoBelluzzo/FastHashes/continuous_integration.yml?branch=master&style=flat&label=Build&color=1081C2"/></a>
      <a href="https://coveralls.io/github/TommasoBelluzzo/FastHashes?branch=master"><img alt="Coverage" src="https://img.shields.io/coveralls/github/TommasoBelluzzo/FastHashes?style=flat&label=Coverage&color=1081C2"/></a>
    </td>
  </tr>
  <tr>
    <td align="right">Info:</td>
    <td align="left">
      <a href="https://github.com/TommasoBelluzzo/FastHashes/blob/master/LICENSE.md"><img alt="License" src="https://img.shields.io/github/license/TommasoBelluzzo/FastHashes?style=flat&label=License&color=1081C2"/></a>
      <a href="#"><img alt="Size" src="https://img.shields.io/github/repo-size/TommasoBelluzzo/FastHashes?style=flat&label=Size&color=1081C2"/></a>
    </td>
  </tr>
  <tr>
    <td align="right">NuGet:</td>
    <td align="left">
      <a href="https://www.nuget.org/packages/FastHashes/"><img alt="Version" src="https://img.shields.io/nuget/v/FastHashes?style=flat&label=Version"/></a>
      <a href="https://www.nuget.org/packages/FastHashes/"><img alt="Downloads" src="https://img.shields.io/nuget/dt/FastHashes?style=flat&label=Downloads&color=1081C2"/></a>
    </td>
  </tr>
  <tr>
    <td align="right">Donation:</td>
    <td align="left">
      <a href="https://www.paypal.com/cgi-bin/webscr?cmd=_s-xclick&hosted_button_id=T8X35C2FED9SU"><img alt="PayPal" src="https://www.paypalobjects.com/en_US/i/btn/btn_donate_LG.gif"/></a>
    </td>
  </tr>
</table>

## Main Features

* `High Performing`: optimized implementations make extensive use of method inlining and efficient memory access to maximize hashing throughput.
* `Hardware Accelerated`: selected algorithms leverage SIMD and hardware intrinsics (SSE2, AVX2, PCLMULQDQ) when available, with portable scalar fallbacks.
* `Allocation Free`: hashing computations avoid unnecessary intermediate allocations, minimizing garbage collection overhead.
* `Endian-Agnostic`: all hashes produce consistent results regardless of the machine's byte order.
* `Platform-Agnostic`: the library targets .NET Standard and modern .NET, with support for both x86 and x64 architectures.
* `Fully Managed`: since version 3.0.0, the library no longer requires unsafe memory access.

 ## Hashes

The library includes the following non-cryptographic hashes:

 * `A5Hash`
   * 32/64/128 Bits Output
   * Reference Implementation: [https://github.com/avaneev/a5hash](https://github.com/avaneev/a5hash)
 * `ChibiHash`
   * 64 Bits Output
   * V2 Version
   * Reference Implementation: [https://github.com/N-R-K/ChibiHash](https://github.com/N-R-K/ChibiHash)
 * `FarmHash`
   * 32/64/128 Bits Output
   * Fingerprint Versions
   * Reference Implementation: [https://github.com/google/farmhash](https://github.com/google/farmhash)
 * `FastHash`
   * 32/64 Bits Output
   * Reference Implementation: [https://github.com/ztanml/fast-hash](https://github.com/ztanml/fast-hash)
 * `FastPositiveHash (T1HA)`
   * 64/128 Bits Output
   * 0/1/2 Variants
   * Reference Implementation: [https://github.com/erthink/t1ha](https://github.com/erthink/t1ha)
 * `HalfSipHash`
   * 32/64 Bits Output
   * Reference Implementation: [https://github.com/veorq/SipHash](https://github.com/veorq/SipHash)
 * `HalftimeHash`
   * 64 Bits Output
   * 64/128/256/512 Bits Processing Styles
   * Reference Implementation: [https://github.com/jbapple/HalftimeHash](https://github.com/jbapple/HalftimeHash)
 * `HighwayHash`
   * 64/128/256 Bits Output
   * Reference Implementation: [https://github.com/google/highwayhash](https://github.com/google/highwayhash)
 * `KomiHash`
   * 64 Bits Output
   * Version 5.34
   * Reference Implementation: [https://github.com/avaneev/komihash](https://github.com/avaneev/komihash)
 * `LaneHash`
   * 64/128 Bits Output
   * Reference Implementation: [https://github.com/ccipherx/lanehash](https://github.com/ccipherx/lanehash)
 * `MetroHash`
   * 64/128 Bits Output
   * 1/2 Variants
   * Reference Implementation: [https://github.com/jandrewrogers/MetroHash](https://github.com/jandrewrogers/MetroHash)
 * `MirHash`
   * 64 Bits Output
   * Strict Version
   * Reference Implementation: [https://github.com/vnmakarov/mir](https://github.com/vnmakarov/mir)
 * `MumHash`
   * 64 Bits Output
   * V4 Quality Version
   * Reference Implementation: [https://github.com/vnmakarov/mum-hash](https://github.com/vnmakarov/mum-hash)
 * `MurmurHash`
   * 32/64/128 Bits Output
   * x86/x64 Variants
   * Reference Implementation: [https://github.com/aappleby/smhasher](https://github.com/aappleby/smhasher)
 * `Mx3Hash`
   * 64 Bits Output
   * Reference Implementation: [https://github.com/jonmaiga/mx3](https://github.com/jonmaiga/mx3)
 * `NmHash`
   * 32 Bits Output
   * Standard/X Variants
   * Reference Implementation: [https://github.com/gzm55/hash-garage](https://github.com/gzm55/hash-garage)
 * `PengyHash`
   * 64 Bits Output
   * Reference Implementation: [https://github.com/tinypeng/pengyhash](https://github.com/tinypeng/pengyhash)
 * `PolymurHash`
   * 64 Bits Output
   * Reference Implementation: [https://github.com/orlp/polymur-hash](https://github.com/orlp/polymur-hash)
 * `RapidHash`
   * 64 Bits Output
   * 1/2/3 Variants
   * Reference Implementation: [https://github.com/Nicoshev/rapidhash](https://github.com/Nicoshev/rapidhash)
 * `SipHash`
   * 64/128 Bits Output
   * 1-3/2-4 Variants
   * Reference Implementation: [https://github.com/veorq/SipHash](https://github.com/veorq/SipHash)
 * `SpookyHash`
   * 32/64/128 Bits Output
   * Reference Implementation: [http://burtleburtle.net/bob/hash/spooky.html](http://burtleburtle.net/bob/hash/spooky.html)
 * `Umash`
   * 64/128 Bits Output
   * Reference Implementation: [https://github.com/backtrace-labs/umash](https://github.com/backtrace-labs/umash)
 * `W1Hash`
   * 64 Bits Output
   * Reference Implementation: [https://github.com/PeterRK/w1hash](https://github.com/PeterRK/w1hash)
 * `WyHash`
   * 32/64 Bits Output
   * Reference Implementation: [https://github.com/wangyi-fudan/wyhash](https://github.com/wangyi-fudan/wyhash)
 * `XxHash`
   * 32/64 Bits Output
   * Reference Implementation: [https://github.com/Cyan4973/xxHash](https://github.com/Cyan4973/xxHash)
 * `XxHash3`
   * 64/128 Bits Output
   * Reference Implementation: [https://github.com/Cyan4973/xxHash](https://github.com/Cyan4973/xxHash)

#### Compatibility

FastHashes produces deterministic outputs for explicitly selected algorithms, variants, seeds, and inputs.\
Algorithm upgrades or corrections may change computed hash values.\
Applications that persist hash values or depend on reproducible results across library versions should verify output compatibility before upgrading.

## Installation & Requirements

FastHashes is available on [NuGet](https://www.nuget.org/packages/FastHashes/) and can be installed as follows:

```console
dotnet add package FastHashes
```

The following frameworks are supported:

* Library: `.NET Standard 2.0`, `.NET Standard 2.1`, `.NET 10.0`
* Auxiliary Projects: `.NET 10.0`

The following NuGet packages are used by the unit testing project:

* [xunit.v3.mtp-v2](https://www.nuget.org/packages/xunit.v3.mtp-v2)
* [Microsoft.Testing.Platform](https://www.nuget.org/packages/Microsoft.Testing.Platform)
* [coverlet.MTP](https://www.nuget.org/packages/coverlet.MTP)

## Usage

FastHashes supports strongly typed, byte-array, and formatted string hash outputs.

The generic `Hash<T>` API provides strongly typed results through `ComputeHashValue` method:

```csharp
using System;
using FastHashes;

Hash<UInt64> hash = new XxHash64();

UInt64 result = hash.ComputeHashValue("Hello World");
```

The non-generic `Hash` API supports dynamic algorithm selection and provides byte-array and formatted string outputs:

```csharp
using System;
using FastHashes;

Hash hash = Hash.Create("XxHash64");

Byte[] resultBytes = hash.ComputeHashBytes("Hello World");
String resultString = hash.ComputeHashString("Hello World", HashFormat.Hexadecimal);
```

The generic API also inherits `ComputeHashBytes` and `ComputeHashString` methods from the non-generic base class.

## Performance Benchmarks

| Rank | Hash                  | Bulk Throughput ↓ | Chunk Throughput |
| ---: | :-------------------- | ----------------: | ---------------: |
|    1 | DummyHash             |        13.92 TB/s |       43.30 GB/s |
|    2 | NmHash-X              |        27.01 GB/s |        5.40 GB/s |
|    3 | NmHash-S              |        26.60 GB/s |        2.07 GB/s |
|    4 | W1Hash                |        20.92 GB/s |        8.36 GB/s |
|    5 | RapidHash-V2          |        20.73 GB/s |        8.01 GB/s |
|    6 | XxHashThree128        |        20.54 GB/s |        4.60 GB/s |
|    7 | RapidHash-V1          |        20.37 GB/s |        8.38 GB/s |
|    8 | XxHashThree64         |        20.22 GB/s |        6.25 GB/s |
|    9 | RapidHash-V3          |        20.10 GB/s |        8.00 GB/s |
|   10 | A5Hash128             |        19.85 GB/s |        6.72 GB/s |
|   11 | KomiHash              |        19.54 GB/s |        7.80 GB/s |
|   12 | XxHash64              |        19.08 GB/s |        6.78 GB/s |
|   13 | WyHash64              |        18.72 GB/s |        7.75 GB/s |
|   14 | HighwayHash128        |        18.33 GB/s |        1.97 GB/s |
|   15 | HighwayHash256        |        18.00 GB/s |        1.43 GB/s |
|   16 | HighwayHash64         |        17.96 GB/s |        2.53 GB/s |
|   17 | ChibiHash             |        17.07 GB/s |        7.22 GB/s |
|   18 | MetroHash64-V2        |        17.03 GB/s |        6.67 GB/s |
|   19 | MetroHash64-V1        |        16.91 GB/s |        6.91 GB/s |
|   20 | MetroHash128-V2       |        16.69 GB/s |        5.50 GB/s |
|   21 | MetroHash128-V1       |        16.14 GB/s |        5.47 GB/s |
|   22 | FarmHash64            |        14.89 GB/s |        6.34 GB/s |
|   23 | FastPositiveHash64-V2 |        14.69 GB/s |        6.01 GB/s |
|   24 | FastPositiveHash64-V1 |        14.67 GB/s |        6.16 GB/s |
|   25 | FastPositiveHash128   |        14.47 GB/s |        5.51 GB/s |
|   26 | FarmHash128           |        14.00 GB/s |        4.98 GB/s |
|   27 | MumHash               |        12.15 GB/s |        6.01 GB/s |
|   28 | PengyHash             |        11.98 GB/s |        3.07 GB/s |
|   29 | PolymurHash           |        11.91 GB/s |        5.36 GB/s |
|   30 | Mx3Hash               |        11.32 GB/s |        6.21 GB/s |
|   31 | XxHash32              |        11.02 GB/s |        5.84 GB/s |
|   32 | A5Hash64              |        11.00 GB/s |        5.68 GB/s |
|   33 | MurmurHash64-X64      |        10.05 GB/s |        5.19 GB/s |
|   34 | MurmurHash128-X64     |         9.98 GB/s |        4.92 GB/s |
|   35 | FastHash32            |         9.03 GB/s |        6.05 GB/s |
|   36 | FastHash64            |         9.02 GB/s |        5.70 GB/s |
|   37 | LaneHash128           |         8.96 GB/s |        3.74 GB/s |
|   38 | LaneHash64            |         8.79 GB/s |        4.16 GB/s |
|   39 | Umash64               |         8.14 GB/s |        5.07 GB/s |
|   40 | A5Hash32              |         8.09 GB/s |        5.01 GB/s |
|   41 | FastPositiveHash64-V0 |         7.56 GB/s |        4.27 GB/s |
|   42 | SpookyHash128         |         7.56 GB/s |        2.85 GB/s |
|   43 | SpookyHash64          |         7.54 GB/s |        3.01 GB/s |
|   44 | SpookyHash32          |         7.43 GB/s |        3.02 GB/s |
|   45 | FarmHash32            |         6.82 GB/s |        4.38 GB/s |
|   46 | WyHash32              |         6.82 GB/s |        5.01 GB/s |
|   47 | MirHash               |         6.48 GB/s |        4.15 GB/s |
|   48 | MurmurHash128-X86     |         6.07 GB/s |        3.38 GB/s |
|   49 | MurmurHash64-X86      |         6.04 GB/s |        3.58 GB/s |
|   50 | Umash128              |         5.82 GB/s |        3.52 GB/s |
|   51 | HalftimeHash-S512     |         4.50 GB/s |      310.01 MB/s |
|   52 | MurmurHash32          |         4.23 GB/s |        3.70 GB/s |
|   53 | HalftimeHash-S256     |         4.18 GB/s |      324.33 MB/s |
|   54 | HalftimeHash-S128     |         3.97 GB/s |      340.18 MB/s |
|   55 | SipHash64-13          |         3.79 GB/s |        1.87 GB/s |
|   56 | SipHash128-13         |         3.65 GB/s |        1.56 GB/s |
|   57 | HalftimeHash-S64      |         2.43 GB/s |      297.52 MB/s |
|   58 | SipHash64-24          |         2.11 GB/s |        1.02 GB/s |
|   59 | SipHash128-24         |         2.01 GB/s |      909.05 MB/s |
|   60 | HalfSipHash32         |         1.94 GB/s |        1.57 GB/s |
|   61 | HalfSipHash64         |         1.94 GB/s |        1.30 GB/s |

The benchmarks above were conducted using the following hardware and software configuration:
 * `CPU:` Intel Core i9-13900K @3.00 GHz (24 Cores, 32 Threads, 32 MB L2 Cache, 36 MB L3 Cache)
 * `RAM:` 32 GB DDR5-6000 (2×16 GB Corsair DIMM @3000 MHz)
 * `OS:` Microsoft Windows 11 Home 64-Bit
 * `Runtime:` .NET 10.0

 Performance may vary depending on the runtime, processor architecture, input size and execution environment.
