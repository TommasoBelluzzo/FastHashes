# FastHashes

FashHashes is a pure C# porting of the following non-cryptographic hashes:

 - *__FarmHash__*
   - 32/64/128 Bits Output
   - Fingerprint Versions
   - Reference Implementation: [https://github.com/google/farmhash](https://github.com/google/farmhash)
 - *__FastHash__*
   - 32/64 Bits Output
   - Reference Implementation: [https://github.com/ZilongTan/fast-hash](https://github.com/ZilongTan/fast-hash)
 - *__FastPositiveHash / TH1A__*
   - 64 Bits Output
   - 0/1/2 Variants
   - Reference Implementation: [https://github.com/leo-yuriev/t1ha](https://github.com/leo-yuriev/t1ha)
 - *__HalfSipHash__*
   - 32 Bits Output
   - Reference Implementation: [https://github.com/veorq/SipHash](https://github.com/veorq/SipHash)
 - *__HighwayHash__*
   - 64/128/256 Bits Output
   - Reference Implementation: [https://github.com/google/highwayhash](https://github.com/google/highwayhash)
 - *__MetroHash__*
   - 64/128 Bits Output
   - Reference Implementation: [https://github.com/jandrewrogers/MetroHash](https://github.com/jandrewrogers/MetroHash)
 - *__MumHash__*
   - 64 Bits Output
   - Reference Implementation: [https://github.com/vnmakarov/mum-hash](https://github.com/vnmakarov/mum-hash)
 - *__MurmurHash (v3)__*
   - 32/64/128 Bits Output
   - x86/x64 Variants
   - Reference Implementation: [https://github.com/aappleby/smhasher](https://github.com/aappleby/smhasher)
 - *__SipHash__*
   - 64 Bits Output
   - 1-3/2-4 Variants
   - Reference Implementation: [https://github.com/veorq/SipHash](https://github.com/veorq/SipHash)
 - *__SpookyHash__*
   - 32/64/128 Bits Output
   - Reference Implementation: [http://burtleburtle.net/bob/hash/spooky.html](http://burtleburtle.net/bob/hash/spooky.html)
 - *__xxHash__*
   - 32/64 Bits Output
   - Reference Implementation: [https://github.com/Cyan4973/xxHash](https://github.com/Cyan4973/xxHash)

The key characteristics of the FashHashes library are:
 - Endian-Agnostic Code (it attemps to provide consistent results regardless of the machine byte order, while only moderately affecting the computations performance);
 - Native/Unmanaged Access (where possible, it uses unsafe memory pointers and native Windows API calls to speed up computations);
 - Zero-Allocation Algorithms (all the computations are performed without allocating objects, only primitive types and/or arrays of primitive types are used).
