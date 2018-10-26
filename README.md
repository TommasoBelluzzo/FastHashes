# FastHashes Framework

## The Library

FashHashes is a pure C# porting of the following non-cryptographic hashes:

 - `FarmHash`
   - 32/64/128 Bits Output
   - Fingerprint Versions
   - Reference Implementation: [https://github.com/google/farmhash](https://github.com/google/farmhash)
 - `FastHash`
   - 32/64 Bits Output
   - Reference Implementation: [https://github.com/ZilongTan/fast-hash](https://github.com/ZilongTan/fast-hash)
 - `FastPositiveHash / TH1A`
   - 64 Bits Output
   - 0/1/2 Variants
   - Reference Implementation: [https://github.com/leo-yuriev/t1ha](https://github.com/leo-yuriev/t1ha)
 - `HalfSipHash`
   - 32 Bits Output
   - Reference Implementation: [https://github.com/veorq/SipHash](https://github.com/veorq/SipHash)
 - `HighwayHash`
   - 64/128/256 Bits Output
   - Reference Implementation: [https://github.com/google/highwayhash](https://github.com/google/highwayhash)
 - `MetroHash`
   - 64/128 Bits Output
   - 0/1 Variants
   - Reference Implementation: [https://github.com/jandrewrogers/MetroHash](https://github.com/jandrewrogers/MetroHash)
 - `MumHash`
   - 64 Bits Output
   - Reference Implementation: [https://github.com/vnmakarov/mum-hash](https://github.com/vnmakarov/mum-hash)
 - `MurmurHash (v3)`
   - 32/64/128 Bits Output
   - x86/x64 Variants
   - Reference Implementation: [https://github.com/aappleby/smhasher](https://github.com/aappleby/smhasher)
 - `SipHash`
   - 64 Bits Output
   - 1-3/2-4 Variants
   - Reference Implementation: [https://github.com/veorq/SipHash](https://github.com/veorq/SipHash)
 - `SpookyHash`
   - 32/64/128 Bits Output
   - Reference Implementation: [http://burtleburtle.net/bob/hash/spooky.html](http://burtleburtle.net/bob/hash/spooky.html)
 - `xxHash`
   - 32/64 Bits Output
   - Reference Implementation: [https://github.com/Cyan4973/xxHash](https://github.com/Cyan4973/xxHash)

The key characteristics of the FashHashes library are:
 - `Endian-Agnostic Code` (it attemps to provide consistent results regardless of the machine byte order, while only moderately affecting the computations performance);
 - `Native/Unmanaged Memory Access` (where possible, it uses unsafe memory pointers and native Windows API calls to speed up computations);
 - `Zero-Allocation Algorithms` (all the computations are performed without allocating objects, only primitive types and/or arrays of primitive types are used).
 
### Requirements
 
The library is platform-agnostic, therefore it can be used on both x86 and x64 environments. The solution model targets Visual Studio 2017 and the projects are compiled under .NET Framework 4.7.1, therefore it can be used on every machine equipped with Windows 7 or greater.

### Performance Benchmarks

| Hash Name             | Bulk Speed Test Average ↓ | Chunks Speed Test Average |
| :---:                 | :---:                     | :---:                     |
| DummyHash (Reference) | 609.34 GB/s               | 2.06 GB/s                 |
| FarmHash32            | 4.27 GB/s                 | 394.83 MB/s               |
| FarmHash64            | 11.47 GB/s                | 626.60 MB/s               |
| FarmHash128           | 11.17 GB/s                | 638.03 MB/s               |
| FastHash32            | 5.34 GB/s                 | 444.45 MB/s               |
| FastHash64            | 5.26 GB/s                 | 440.91 MB/s               |
| FastPositiveHash_0    | 2.29 GB/s                 | 320.69 MB/s               |
| FastPositiveHash_1    | 4.10 GB/s                 | 386.51 MB/s               |
| FastPositiveHash_2    | 3.99 GB/s                 | 386.33 MB/s               |
| HalfSipHash           | X                         | X                         |
| HighwayHash64         | 899.65 MB/s               | 151.54 MB/s               |
| HighwayHash128        | 883.51 MB/s               | 143.84 MB/s               |
| HighwayHash256        | 914.16 MB/s               | 125.78 MB/s               |
| MetroHash64_1         | 11.09 GB/s                | 636.52 MB/s               |
| MetroHash64_2         | git diff                  | git diff                  |
| MetroHash128_1        | git status                | git status                |
| MetroHash128_2        | git diff                  | git diff                  |
| MurmurHash32          | git status                | git status                |
| MurmurHash64_X86      | git diff                  | git diff                  | 
| MurmurHash64_X64      | git diff                  | git diff                  |
| MurmurHash128_X86     | git status                | git status                |
| MurmurHash128_X64     | git diff                  | git diff                  |
| MetroHash128_1        | git status                | git status                |
| MetroHash128_2        | git diff                  | git diff                  |
| MumHash               | git status                | git status                |
| SipHash_13            | git status                | git status                |
| SipHash_24            | git status                | git status                |
| SpookyHash32          | git status                | git status                |
| SpookyHash64          | git status                | git status                |
| SpookyHash128         | git status                | git status                |
| xxHash32              | git status                | git status                |
| xxHash64              | git status                | git status                |

## The Testing Suite
