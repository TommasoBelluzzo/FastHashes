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

## Requirements

The following frameworks are supported:

* Library: `.NET Standard 2.0`, `.NET Standard 2.1`, `.NET 10.0`
* Auxiliary Projects: `.NET 10.0`

The following NuGet packages are used by the unit testing project:

* [xunit.v3.mtp-v2](https://www.nuget.org/packages/xunit.v3.mtp-v2)
* [Microsoft.Testing.Platform](https://www.nuget.org/packages/Microsoft.Testing.Platform)
* [coverlet.MTP](https://www.nuget.org/packages/coverlet.MTP)

## Performance Benchmarks

| Rank | Hash                  | Bulk Throughput ↓ | Chunk Throughput |
| ---: | :-------------------- | ----------------: | ---------------: |
|    1 | DummyHash             |        10.20 TB/s |       43.30 GB/s |
|    2 | RapidHash-V2          |        20.92 GB/s |        8.16 GB/s |
|    3 | RapidHash-V3          |        20.77 GB/s |        8.04 GB/s |
|    4 | W1Hash                |        20.50 GB/s |        8.42 GB/s |
|    5 | RapidHash-V1          |        20.07 GB/s |        8.47 GB/s |
|    6 | KomiHash              |        19.57 GB/s |        7.69 GB/s |
|    7 | A5Hash128             |        19.55 GB/s |        6.71 GB/s |
|    8 | XxHash64              |        19.24 GB/s |        6.76 GB/s |
|    9 | WyHash64              |        18.61 GB/s |        7.81 GB/s |
|   10 | ChibiHash             |        17.43 GB/s |        7.37 GB/s |
|   11 | MetroHash128-V2       |        16.81 GB/s |        5.20 GB/s |
|   12 | MetroHash64-V2        |        16.27 GB/s |        6.63 GB/s |
|   13 | MetroHash128-V1       |        16.16 GB/s |        5.22 GB/s |
|   14 | MetroHash64-V1        |        15.79 GB/s |        6.56 GB/s |
|   15 | FastPositiveHash128   |        14.90 GB/s |        5.61 GB/s |
|   16 | FarmHash64            |        14.86 GB/s |        6.49 GB/s |
|   17 | FastPositiveHash64-V2 |        14.53 GB/s |        6.20 GB/s |
|   18 | FastPositiveHash64-V1 |        14.27 GB/s |        5.61 GB/s |
|   19 | FarmHash128           |        14.20 GB/s |        5.00 GB/s |
|   20 | MumHash               |        12.20 GB/s |        5.97 GB/s |
|   21 | PengyHash             |        11.96 GB/s |        3.09 GB/s |
|   22 | PolymurHash           |        11.95 GB/s |        5.34 GB/s |
|   23 | Mx3Hash               |        11.32 GB/s |        6.27 GB/s |
|   24 | XxHash32              |        11.09 GB/s |        5.96 GB/s |
|   25 | A5Hash64              |        10.33 GB/s |        5.78 GB/s |
|   26 | MurmurHash128-X64     |         9.81 GB/s |        4.67 GB/s |
|   27 | MurmurHash64-X64      |         9.71 GB/s |        5.18 GB/s |
|   28 | FastHash64            |         8.92 GB/s |        5.62 GB/s |
|   29 | LaneHash64            |         8.88 GB/s |        4.34 GB/s |
|   30 | FastHash32            |         8.84 GB/s |        5.69 GB/s |
|   31 | LaneHash128           |         8.84 GB/s |        3.65 GB/s |
|   32 | A5Hash32              |         8.14 GB/s |        4.95 GB/s |
|   33 | FastPositiveHash64-V0 |         7.26 GB/s |        4.23 GB/s |
|   34 | XxHashThree128        |         6.89 GB/s |        3.64 GB/s |
|   35 | FarmHash32            |         6.82 GB/s |        4.38 GB/s |
|   36 | WyHash32              |         6.77 GB/s |        5.11 GB/s |
|   37 | MirHash               |         6.51 GB/s |        4.04 GB/s |
|   38 | XxHashThree64         |         6.43 GB/s |        5.28 GB/s |
|   39 | MurmurHash128-X86     |         5.98 GB/s |        3.26 GB/s |
|   40 | MurmurHash64-X86      |         5.85 GB/s |        3.56 GB/s |
|   41 | MurmurHash32          |         4.23 GB/s |        3.69 GB/s |
|   42 | SipHash64-13          |         3.84 GB/s |        1.85 GB/s |
|   43 | SipHash128-13         |         3.81 GB/s |        1.48 GB/s |
|   44 | HalftimeHash-S512     |         2.52 GB/s |      277.65 MB/s |
|   45 | HalftimeHash-S256     |         2.51 GB/s |      294.33 MB/s |
|   46 | HalftimeHash-S128     |         2.50 GB/s |      313.03 MB/s |
|   47 | HalftimeHash-S64      |         2.40 GB/s |      300.26 MB/s |
|   48 | HighwayHash64         |         2.34 GB/s |      742.65 MB/s |
|   49 | HighwayHash128        |         2.34 GB/s |      614.03 MB/s |
|   50 | HighwayHash256        |         2.32 GB/s |      463.13 MB/s |
|   51 | SpookyHash32          |         2.16 GB/s |      828.73 MB/s |
|   52 | SipHash128-24         |         2.15 GB/s |      882.81 MB/s |
|   53 | SpookyHash128         |         2.14 GB/s |      806.15 MB/s |
|   54 | SpookyHash64          |         2.13 GB/s |      825.05 MB/s |
|   55 | SipHash64-24          |         2.12 GB/s |      998.79 MB/s |
|   56 | HalfSipHash32         |         1.97 GB/s |        1.56 GB/s |
|   57 | HalfSipHash64         |         1.94 GB/s |        1.36 GB/s |
|   58 | NmHash-S              |         1.36 GB/s |        1.11 GB/s |
|   59 | NmHash-X              |         1.36 GB/s |        3.00 GB/s |
|   60 | Umash64               |       102.46 MB/s |      119.12 MB/s |
|   61 | Umash128              |        95.92 MB/s |       86.90 MB/s |

The benchmarks above were conducted using the following hardware and software configuration:
 * `CPU:` Intel Core i9-13900K @3.00 GHz (24 Cores, 32 Threads, 32 MB L2 Cache, 36 MB L3 Cache)
 * `RAM:` 32 GB DDR5-6000 (2×16 GB Corsair DIMM @3000 MHz)
 * `OS:` Microsoft Windows 11 Home 64-Bit

 Performance may vary depending on the runtime, processor architecture, input size and execution environment.
