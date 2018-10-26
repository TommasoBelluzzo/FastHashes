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

| Hash Name          | Bulk Speed Test Average ↓ | Chunks Speed Test Average |
| :---:              | :---:                     | :---:                     |
| DummyHash          | git status                | git status                |
| FarmHash32         | git diff                  | git diff                  |
| FarmHash64         | git status                | git status                |
| FarmHash128        | git diff                  | git diff                  |
| FastHash32         | git status                | git status                |
| FastHash64         | git diff                  | git diff                  |
| FastPositiveHash_0 | git status                | git status                |
| FastPositiveHash_1 | git diff                  | git diff                  |
| FastPositiveHash_2 | git status                | git status                |
| HalfSipHash        | git diff                  | git diff                  |
| HighwayHash64      | git diff                  | git diff                  |
| HighwayHash128     | git status                | git status                |
| HighwayHash256     | git diff                  | git diff                  |
| MetroHash64_1      | git status                | git status                |
| MetroHash64_2      | git diff                  | git diff                  |
| MetroHash128_1     | git status                | git status                |
| MetroHash128_2     | git diff                  | git diff                  |
| MurmurHash32       | git status                | git status                |
| MurmurHash64_X86   | git diff                  | git diff                  | 
| MurmurHash64_X64   | git diff                  | git diff                  |
| MurmurHash128_X86  | git status                | git status                |
| MurmurHash128_X64  | git diff                  | git diff                  |
| MetroHash128_1     | git status                | git status                |
| MetroHash128_2     | git diff                  | git diff                  |
| MumHash            | git status                | git status                |
| SipHash13          | git status                | git status                |
| SipHash24          | git status                | git status                |
| SpookyHash32       | git status                | git status                |
| SpookyHash64       | git status                | git status                |
| SpookyHash128      | git status                | git status                |
| xxHash32           | git status                | git status                |
| xxHash64           | git status                | git status                |

## The Testing Suite
