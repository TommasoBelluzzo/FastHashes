# FastHashes

FashHashes is a pure C# porting of the following non-cryptographic hashes:

 - __SpookyHash__
  - 32/64/128 Bits Outputs
  - Reference: [http://burtleburtle.net/bob/hash/spooky.html](http://burtleburtle.net/bob/hash/spooky.html)
 - __TH1A / FastPositiveHash__
 - __xxHash__
  - 32/64 Bits Outputs
  - Reference: [https://github.com/Cyan4973/xxHash](https://github.com/Cyan4973/xxHash)



The key difference compared to other similar projects, e.g. Guava hashing, is that this has no object allocations during the hash computation and does not use ThreadLocal.

The implementation utilises native access where possible, but is also platform-endianness-agnostic. This provides consistent results whatever the byte order, while only moderately affecting performance.

A .NET implementation of various non-cryptographic hashes.
