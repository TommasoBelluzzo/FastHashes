# FastHashes

FashHashes is a suite of .NET non-cryptographic hash functions.

<table>
  <tr>
    <td align="right">Status:</td>
    <td align="left">
      <a href="https://github.com/TommasoBelluzzo/FastHashes/actions/workflows/continuous_integration.yml"><img alt="Build" src="https://img.shields.io/github/workflow/status/TommasoBelluzzo/FastHashes/Continuous%20Integration?style=flat&label=Build&color=1081C2"/></a>
      <a href="https://coveralls.io/github/TommasoBelluzzo/FastHashes?branch=master"><img alt="Coverage" src="https://img.shields.io/coveralls/github/TommasoBelluzzo/FastHashes?style=flat&label=Coverage&color=1081C2"/></a>
    </td>
  </tr>
  <tr>
    <td align="right">Info:</td>
    <td align="left">
      <a href="#"><img alt="License" src="https://img.shields.io/github/license/TommasoBelluzzo/FastHashes?style=flat&label=License&color=1081C2"/></a>
      <a href="#"><img alt="Lines" src="https://img.shields.io/tokei/lines/github/TommasoBelluzzo/FastHashes?style=flat&label=Lines&color=1081C2"/></a>
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

 * `High Performing`: deeply-optimized code makes extensive use of method inlining and unsafe memory pointers in order to speed up the computations.
 * `Allocation Free`: all the internal functions follow a zero-allocation approach, only primitive types and/or arrays of primitive types are used by the algorithms.
 * `Endian-Agnostic`: all the hashes provide consistent results regardless of the machine byte order, while only moderately affecting the overall performance of the computations.
 * `Platform-Agnostic`: the code has been developed under `.NET Standard` framework and both `x86` and `x64` environments are fully supported.
 * `Fully Managed`: since version `3.0.0`, which has been developed under `.NET Standard 2.1 / .NET Core 3.1` and above, pointers requiring unsafe code have been removed.

 ## Hashes

The library includes the following non-cryptographic hashes:

 * `FarmHash`
   * 32/64/128 Bits Output
   * Fingerprint Versions
   * Reference Implementation: [https://github.com/google/farmhash](https://github.com/google/farmhash)
 * `FastHash`
   * 32/64 Bits Output
   * Reference Implementation: [https://github.com/ZilongTan/fast-hash](https://github.com/ZilongTan/fast-hash)
 * `FastPositiveHash / TH1A`
   * 64 Bits Output
   * 0/1/2 Variants
   * Reference Implementation: [https://github.com/leo-yuriev/t1ha](https://github.com/leo-yuriev/t1ha)
 * `HalfSipHash`
   * 32 Bits Output
   * Reference Implementation: [https://github.com/veorq/SipHash](https://github.com/veorq/SipHash)
 * `HighwayHash`
   * 64/128/256 Bits Output
   * Reference Implementation: [https://github.com/google/highwayhash](https://github.com/google/highwayhash)
 * `MetroHash`
   * 64/128 Bits Output
   * 0/1 Variants
   * Reference Implementation: [https://github.com/jandrewrogers/MetroHash](https://github.com/jandrewrogers/MetroHash)
 * `MumHash`
   * 64 Bits Output
   * Reference Implementation: [https://github.com/vnmakarov/mum-hash](https://github.com/vnmakarov/mum-hash)
 * `MurmurHash (v3)`
   * 32/64/128 Bits Output
   * x86/x64 Variants
   * Reference Implementation: [https://github.com/aappleby/smhasher](https://github.com/aappleby/smhasher)
 * `SipHash`
   * 64 Bits Output
   * 1-3/2-4 Variants
   * Reference Implementation: [https://github.com/veorq/SipHash](https://github.com/veorq/SipHash)
 * `SpookyHash`
   * 32/64/128 Bits Output
   * Reference Implementation: [http://burtleburtle.net/bob/hash/spooky.html](http://burtleburtle.net/bob/hash/spooky.html)
 * `xxHash`
   * 32/64 Bits Output
   * Reference Implementation: [https://github.com/Cyan4973/xxHash](https://github.com/Cyan4973/xxHash)

## Requirements
 
The following `Target Frameworks` are defined:

 * Library: `.NET Standard 2.0`
 * Auxiliary Projects: `.NET Core 2.1`
 
The following `NuGet` packages are required for building the library:

* [Microsoft.SourceLink.GitHub](https://www.nuget.org/packages/Microsoft.SourceLink.GitHub)
 
The following `NuGet` packages are required for performing unit tests:

* [Microsoft.NET.Test.Sdk](https://www.nuget.org/packages/Microsoft.NET.Test.Sdk/)
* [xunit](https://www.nuget.org/packages/xunit/)

## Performance Benchmarks

| Hash Rank | Hash Name               | Bulk Speed Test Average ↓ | Chunks Speed Test Average |
| :---:     | :---:                   | :---:                     | :---:                     |
| *-*       | *DummyHash (Reference)* | *609.34 GB/s*             | *2.06 GB/s*               |
| 1         | FarmHash64              | 11.47 GB/s                | 626.60 MB/s               |
| 2         | FarmHash128             | 11.27 GB/s                | 641.03 MB/s               |
| 3         | MetroHash128-V1         | 11.11 GB/s                | 636.52 MB/s               |
| 4         | MetroHash64-V1          | 11.10 GB/s                | 628.81 MB/s               |
| 5         | MetroHash128-V2         | 11.06 GB/s                | 635.34 MB/s               |
| 6         | MetroHash64-V2          | 11.05 GB/s                | 620.92 MB/s               |
| 7         | XxHash64                | 10.03 GB/s                | 596.90 MB/s               |
| 8         | MurmurHash128-x64       | 6.70 GB/s                 | 485.59 MB/s               |
| 9         | MurmurHash64-x64        | 6.56 GB/s                 | 467.14 MB/s               |
| 10        | XxHash32                | 6.43 GB/s                 | 482.92 MB/s               |
| 11        | FastHash64              | 5.34 GB/s                 | 444.45 MB/s               |
| 12        | FastHash32              | 5.26 GB/s                 | 440.91 MB/s               |
| 13        | FarmHash32              | 4.27 GB/s                 | 394.83 MB/s               |
| 14        | FastPositiveHash-V1     | 4.10 GB/s                 | 386.51 MB/s               |
| 15        | MurmurHash128-x86       | 4.05 GB/s                 | 383.60 MB/s               |
| 16        | MurmurHash64-x86        | 4.01 GB/s                 | 374.73 MB/s               | 
| 17        | FastPositiveHash_V2     | 3.99 GB/s                 | 386.33 MB/s               |
| 18        | MurmurHash32            | 2.82 GB/s                 | 332.35 MB/s               |
| 19        | MumHash                 | 2.44 GB/s                 | 312.24 MB/s               |
| 20        | FastPositiveHash-V0     | 2.29 GB/s                 | 320.69 MB/s               |
| 21        | SipHash-13              | 1.28 GB/s                 | 230.67 MB/s               |
| 22        | HighwayHash256          | 914.16 MB/s               | 125.78 MB/s               |
| 23        | HighwayHash64           | 899.65 MB/s               | 151.54 MB/s               |
| 24        | HighwayHash128          | 883.51 MB/s               | 143.84 MB/s               |
| 25        | SpookyHash32            | 752.43 MB/s               | 156.79 MB/s               |
| 26        | SpookyHash64            | 734.84 MB/s               | 149.36 MB/s               |
| 27        | SipHash-24              | 734.74 MB/s               | 168.43 MB/s               |
| 28        | SpookyHash128           | 723.91 MB/s               | 155.67 MB/s               |
| 29        | HalfSipHash             | 364.78 MB/s               | 119.96 MB/s               |

The tests above have been conducted with the following machine setup:

 * `CPU:` Intel Core i7-7700HQ @2.80GHz (4 Cores, 8 Threads, 256KB L1 Cache)
 * `RAM:` 16 GB x DDR4 SO-DIMM @1200MHz
 * `OS:` Microsoft Windows 10 64-Bit
