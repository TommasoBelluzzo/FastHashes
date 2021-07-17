#region Using Directives
using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
#endregion

namespace FastHashes
{
    /// <summary>Represents the base class from which all the SpookyHash implementations must derive. This class is abstract.</summary>
    public abstract class SpookyHash : Hash
    {
        #region Constants
        private const UInt64 C = 0xDEADBEEFDEADBEEFul;
        #endregion

        #region Members
        private readonly UInt64 m_Seed1;
        private readonly UInt64 m_Seed2;
        #endregion

        #region Members (Static)
        private static readonly Int32[] LRE = { 44, 15, 34, 21, 38, 33, 10, 13, 38, 53, 42, 54 };
        private static readonly Int32[] LRM = { 11, 32, 43, 31, 17, 28, 39, 57, 55, 54, 22, 46 };
        private static readonly Int32[] SRE = { 15, 52, 26, 51, 28,  9, 47, 54, 32, 25, 63 };
        private static readonly Int32[] SRM = { 50, 52, 30, 41, 54, 48, 38, 37, 62, 34,  5, 36 };
        #endregion

        #region Properties
        /// <summary>Gets the first seed used by the hashing algorithm.</summary>
        /// <value>An <see cref="T:System.UInt64"/> value.</value>
        [ExcludeFromCodeCoverage]
        public UInt64 Seed1 => m_Seed1;

        /// <summary>Gets the second seed used by the hashing algorithm.</summary>
        /// <value>An <see cref="T:System.UInt64"/> value.</value>
        [ExcludeFromCodeCoverage]
        public UInt64 Seed2 => m_Seed2;
        #endregion

        #region Constructors
        /// <summary>Represents the base constructor used by derived classes.</summary>
        /// <param name="seed1">The first <see cref="T:System.UInt64"/> seed used by the hashing algorithm.</param>
        /// <param name="seed2">The second <see cref="T:System.UInt64"/> seed used by the hashing algorithm.</param>
        protected SpookyHash(UInt64 seed1, UInt64 seed2)
        {
            m_Seed1 = seed1;
            m_Seed2 = seed2;
        }
        #endregion

        #region Methods
        /// <inheritdoc/>
        protected override Byte[] ComputeHashInternal(Byte[] buffer, Int32 offset, Int32 count)
        {
            if (count < 192)
                return ComputeHashShort(buffer, offset, count);

            return ComputeHashLong(buffer, offset, count);
        }

        private Byte[] ComputeHashLong(Byte[] data, Int32 index, Int32 length)
        {
            Int32 blocks = length / 96;
            Int32 blocksBytes = blocks * 96;

            Int32 finalLength = (blocks + 1) * 96;
            Byte[] dataFinal = new Byte[finalLength];

            UnsafeBuffer.BlockCopy(data, index, dataFinal, 0, blocksBytes);
            UnsafeBuffer.BlockCopy(data, blocksBytes, dataFinal, blocksBytes, length - blocksBytes);
            dataFinal[finalLength - 1] = (Byte)(length % 96);

            UInt64[] hash = new UInt64[12];
            hash[0] = hash[3] = hash[6] = hash[9] = m_Seed1;
            hash[1] = hash[4] = hash[7] = hash[10] = m_Seed2;
            hash[2] = hash[5] = hash[8] = hash[11] = C;

            unsafe
            {
                fixed (Byte* pin = dataFinal)
                {
                    Byte* pointer = pin;

                    while (blocks-- > 0)
                    {
                        UInt64[] vb =
                        {
                            Read64(ref pointer), Read64(ref pointer), Read64(ref pointer),
                            Read64(ref pointer), Read64(ref pointer), Read64(ref pointer),
                            Read64(ref pointer), Read64(ref pointer), Read64(ref pointer),
                            Read64(ref pointer), Read64(ref pointer), Read64(ref pointer)
                        };

                        LongMix(ref hash, vb);
                    }

                    UInt64[] vr =
                    {
                        Read64(ref pointer), Read64(ref pointer), Read64(ref pointer),
                        Read64(ref pointer), Read64(ref pointer), Read64(ref pointer),
                        Read64(ref pointer), Read64(ref pointer), Read64(ref pointer),
                        Read64(ref pointer), Read64(ref pointer), Read64(ref pointer)
                    };

                    LongMix(ref hash, vr);
                }
            }

            for (Int32 i = 0; i < 3; ++i)
                LongEnd(ref hash);

            return GetHash(hash);
        }

        private Byte[] ComputeHashShort(Byte[] data, Int32 index, Int32 length)
        {
            UInt64[] hash = { m_Seed1, m_Seed2, C, C };

            unsafe
            {
                fixed (Byte* pin = &data[index])
                {
                    Byte* pointer = pin;

                    Int32 remainder = length % 32;

                    if (length > 15)
                    {
                        Int32 blocks = length / 32;

                        while (blocks-- > 0)
                        {
                            hash[2] += Read64(ref pointer);
                            hash[3] += Read64(ref pointer);
                            ShortMix(ref hash);
                            hash[0] += Read64(ref pointer);
                            hash[1] += Read64(ref pointer);
                        }

                        if (remainder >= 16)
                        {
                            hash[2] += Read64(ref pointer);
                            hash[3] += Read64(ref pointer);
                            ShortMix(ref hash);

                            remainder -= 16;
                        }
                    }

                    hash[3] = (UInt64)length << 56;

                    switch (remainder)
                    {
                        case 15: hash[3] += (UInt64)pointer[14] << 48; goto case 14;
                        case 14: hash[3] += (UInt64)pointer[13] << 40; goto case 13;
                        case 13: hash[3] += (UInt64)pointer[12] << 32; goto case 12;
                        case 12:
                            hash[2] += Read64(ref pointer);
                            hash[3] += Read32(ref pointer);
                            break;
                        case 11: hash[3] += (UInt64)pointer[10] << 16; goto case 10;
                        case 10: hash[3] += (UInt64)pointer[9] << 8; goto case 9;
                        case 9: hash[3] += pointer[8]; goto case 8;
                        case 8:
                            hash[2] += Read64(ref pointer);
                            break;
                        case 7: hash[2] += (UInt64)pointer[6] << 48; goto case 6;
                        case 6: hash[2] += (UInt64)pointer[5] << 40; goto case 5;
                        case 5: hash[2] += (UInt64)pointer[4] << 32; goto case 4;
                        case 4:
                            hash[2] += Read32(ref pointer);
                            break;
                        case 3: hash[2] += (UInt64)pointer[2] << 16; goto case 2;
                        case 2: hash[2] += (UInt64)pointer[1] << 8;  goto case 1;
                        case 1:
                            hash[2] += pointer[0];
                            break;
                        case 0:
                            hash[2] += C;
                            hash[3] += C;
                            break;
                    }
                }
            }

            ShortEnd(ref hash);

            return GetHash(hash);
        }
        #endregion

        #region Methods (Abstract)
        /// <summary>Finalizes any partial computation and returns the hash code.</summary>
        /// <param name="hashData">The <see cref="T:System.UInt64"/>[] representing the hash data.</param>
        /// <returns>A <see cref="T:System.Byte"/>[] representing the hash code.</returns>
        protected abstract Byte[] GetHash(UInt64[] hashData);
        #endregion

        #region Methods (Static)
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void LongEnd(ref UInt64[] hash)
        {
            for (Int32 j = 0; j < 12; ++j)
            {
                Int32 idxA = (j + 11) % 12;
                Int32 idxB = (j + 1) % 12;

                hash[idxA] += hash[idxB]; 
                hash[(j + 2) % 12] ^= hash[idxA]; 
                hash[idxB] = RotateLeft(hash[idxB], LRE[j]);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void LongMix(ref UInt64[] hash, UInt64[] v)
        {
            for (Int32 i = 0; i < 12; ++i)
            {
                Int32 idx = (i + 11) % 12;

                hash[i] += v[i]; 
                hash[(i + 2) % 12] ^= hash[(i + 10) % 12]; 
                hash[idx] ^= hash[i];
                hash[i] = RotateLeft(hash[i], LRM[i]); 
                hash[idx] += hash[(i + 1) % 12];
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void ShortEnd(ref UInt64[] hash)
        {
            for (Int32 i = 0; i < 11; ++i)
            {
                Int32 idxA = (i + 2) % 4;
                Int32 idxB = (i + 3) % 4;

                hash[idxB] ^= hash[idxA];
                hash[idxA] = RotateLeft(hash[idxA], SRE[i]);
                hash[idxB] += hash[idxA];
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void ShortMix(ref UInt64[] hash)
        {
            for (Int32 i = 0; i < 12; ++i)
            {
                Int32 idx = (i + 2) % 4;

                hash[idx] = RotateLeft(hash[idx], SRM[i]);
                hash[idx] += hash[(i + 3) % 4];
                hash[i % 4] ^= hash[idx];
            }
        }
        #endregion
    }

    /// <summary>Represents the SpookyHash32 implementation. This class cannot be derived.</summary>
    public sealed class SpookyHash32 : SpookyHash
    {
        #region Properties
        /// <inheritdoc/>
        [ExcludeFromCodeCoverage]
        public override Int32 Length => 32;
        #endregion

        #region Constructors
        /// <summary>Initializes a new instance using a value of <c>0</c> for both seeds.</summary>
        [ExcludeFromCodeCoverage]
        public SpookyHash32() : base(0ul, 0ul) { }

        /// <summary>Initializes a new instance using the specified value for both seeds.</summary>
        /// <param name="seed">The <see cref="T:System.UInt64"/> seed used by the hashing algorithm.</param>
        [ExcludeFromCodeCoverage]
        public SpookyHash32(UInt32 seed) : base(seed, seed) { }
        
        /// <summary>Initializes a new instance using the specified seeds.</summary>
        /// <param name="seed1">The first <see cref="T:System.UInt64"/> seed used by the hashing algorithm.</param>
        /// <param name="seed2">The second <see cref="T:System.UInt64"/> seed used by the hashing algorithm.</param>
        [ExcludeFromCodeCoverage]
        public SpookyHash32(UInt64 seed1, UInt64 seed2) : base(seed1, seed2) { }
        #endregion

        #region Methods
        /// <inheritdoc/>
        protected override Byte[] GetHash(UInt64[] hashData)
        {
            Byte[] result = ToByte32(hashData[0]);

            return result;
        }
        #endregion
    }

    /// <summary>Represents the SpookyHash64 implementation. This class cannot be derived.</summary>
    public sealed class SpookyHash64 : SpookyHash
    {
        #region Properties
        /// <inheritdoc/>
        [ExcludeFromCodeCoverage]
        public override Int32 Length => 64;
        #endregion

        #region Constructors
        /// <summary>Initializes a new instance using a value of <c>0</c> for both seeds.</summary>
        [ExcludeFromCodeCoverage]
        public SpookyHash64() : base(0ul, 0ul) { }

        /// <summary>Initializes a new instance using the specified value for both seeds.</summary>
        /// <param name="seed">The <see cref="T:System.UInt64"/> seed used by the hashing algorithm.</param>
        [ExcludeFromCodeCoverage]
        public SpookyHash64(UInt64 seed) : base(seed, seed) { }

        /// <summary>Initializes a new instance using the specified seeds.</summary>
        /// <param name="seed1">The first <see cref="T:System.UInt64"/> seed used by the hashing algorithm.</param>
        /// <param name="seed2">The second <see cref="T:System.UInt64"/> seed used by the hashing algorithm.</param>
        [ExcludeFromCodeCoverage]
        public SpookyHash64(UInt64 seed1, UInt64 seed2) : base(seed1, seed2) { }
        #endregion

        #region Methods
        /// <inheritdoc/>
        protected override Byte[] GetHash(UInt64[] hashData)
        {
            Byte[] result = ToByte64(hashData[0]);

            return result;
        }
        #endregion
    }

    /// <summary>Represents the SpookyHash128 implementation. This class cannot be derived.</summary>
    public sealed class SpookyHash128 : SpookyHash
    {
        #region Properties
        /// <inheritdoc/>
        [ExcludeFromCodeCoverage]
        public override Int32 Length => 128;
        #endregion

        #region Constructors
        /// <summary>Initializes a new instance using a value of <c>0</c> for both seeds.</summary>
        [ExcludeFromCodeCoverage]
        public SpookyHash128() : base(0ul, 0ul) { }

        /// <summary>Initializes a new instance using the specified value for both seeds.</summary>
        /// <param name="seed">The <see cref="T:System.UInt64"/> seed used by the hashing algorithm.</param>
        [ExcludeFromCodeCoverage]
        public SpookyHash128(UInt64 seed) : base(seed, seed) { }

        /// <summary>Initializes a new instance using the specified seeds.</summary>
        /// <param name="seed1">The first <see cref="T:System.UInt64"/> seed used by the hashing algorithm.</param>
        /// <param name="seed2">The second <see cref="T:System.UInt64"/> seed used by the hashing algorithm.</param>
        [ExcludeFromCodeCoverage]
        public SpookyHash128(UInt64 seed1, UInt64 seed2) : base(seed1, seed2) { }
        #endregion

        #region Methods
        /// <inheritdoc/>
        protected override Byte[] GetHash(UInt64[] hashData)
        {
            Byte[] result = new Byte[16];

            unsafe
            {
                fixed (Byte* pin = result)
                {
                    UInt64* pointer = (UInt64*)pin;
                    pointer[0] = hashData[0];
                    pointer[1] = hashData[1];
                }
            }

            return result;
        }
        #endregion
    }
}