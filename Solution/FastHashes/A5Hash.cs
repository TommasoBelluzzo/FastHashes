#region Using Directives
using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
#endregion

namespace FastHashes
{
    /// <summary>Provides common arithmetic operations for the A5Hash implementations.</summary>
    internal static class A5HashOperations
    {
        #region Constants
        internal const UInt64 VAL01 = 0x5555555555555555ul;
        internal const UInt64 VAL10 = 0xAAAAAAAAAAAAAAAAul;
        #endregion


        #region Methods
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void Multiply64(UInt32 x, UInt32 y, out UInt32 lo, out UInt32 hi)
        {
            UInt64 product = (UInt64)x * y;
            lo = unchecked((UInt32)product);
            hi = (UInt32)(product >> 32);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void Multiply128(UInt64 x, UInt64 y, out UInt64 lo, out UInt64 hi)
        {
            BinaryOperations.Multiply128(x, y, out lo, out hi);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static UInt64 ReadPair(ReadOnlySpan<Byte> buffer, Int32 offset1, Int32 offset2)
        {
            return ((UInt64)BinaryOperations.Read32(buffer, offset1) << 32) |
                BinaryOperations.Read32(buffer, offset2);
        }
        #endregion
    }

    /// <summary>Represents the A5Hash 32-bit implementation. This class cannot be derived.</summary>
    public sealed class A5Hash32 : Hash<UInt32>
    {
        #region Members
        private readonly UInt32 m_Seed;
        #endregion

        #region Properties
        /// <summary>Gets the seed used by the hashing algorithm.</summary>
        [ExcludeFromCodeCoverage]
        public UInt32 Seed => m_Seed;
        #endregion

        #region Constructors
        /// <summary>Initializes a new instance using the specified seed.</summary>
        /// <param name="seed">The seed used by the hashing algorithm.</param>
        [ExcludeFromCodeCoverage]
        public A5Hash32(UInt32 seed)
        {
            m_Seed = seed;
        }

        /// <summary>Initializes a new instance using a seed value of <c>0</c>.</summary>
        [ExcludeFromCodeCoverage]
        public A5Hash32() : this(0u) { }
        #endregion

        #region Methods
        /// <inheritdoc/>
        protected override UInt32 ComputeHashValueInternal(ReadOnlySpan<Byte> buffer)
        {
            unchecked
            {
                Int32 offset = 0;
                Int32 count = buffer.Length;
                UInt32 val01 = (UInt32)A5HashOperations.VAL01;
                UInt32 val10 = (UInt32)A5HashOperations.VAL10;
                UInt32 seed1 = 0x243F6A88u ^ (UInt32)count;
                UInt32 seed2 = 0x85A308D3u ^ (UInt32)count;

                // Matches the 64-bit size_t branch of upstream a5hash32().
                A5HashOperations.Multiply64(0x452821E6u, 0x38D01377u, out UInt32 seed3, out UInt32 seed4);

                A5HashOperations.Multiply64(seed2 ^ (m_Seed & val10),
                    seed1 ^ (m_Seed & val01), out seed1, out seed2);

                UInt32 a = 0;
                UInt32 b = 0;

                if (count < 17)
                {
                    if (count > 3)
                    {
                        a = BinaryOperations.Read32(buffer, 0);
                        b = BinaryOperations.Read32(buffer, count - 4);

                        if (count >= 9)
                        {
                            Int32 mo = count >> 3;
                            UInt32 c = BinaryOperations.Read32(buffer, mo * 4);
                            UInt32 d = BinaryOperations.Read32(buffer, count - 4 - mo * 4);
                            A5HashOperations.Multiply64(c + seed3, d + seed4, out seed3, out seed4);
                        }
                    }
                    else
                    {
                        if (count > 0) a = buffer[0];
                        if (count > 1) a |= (UInt32)buffer[1] << 8;
                        if (count > 2) a |= (UInt32)buffer[2] << 16;
                    }
                }
                else
                {
                    val01 ^= seed1;
                    val10 ^= seed2;

                    do
                    {
                        UInt32 s1 = seed1;
                        UInt32 s4 = seed4;

                        A5HashOperations.Multiply64(BinaryOperations.Read32(buffer, offset) + seed1,
                            BinaryOperations.Read32(buffer, offset + 4) + seed2, out seed1, out seed2);
                        A5HashOperations.Multiply64(BinaryOperations.Read32(buffer, offset + 8) + seed3,
                            BinaryOperations.Read32(buffer, offset + 12) + seed4, out seed3, out seed4);

                        count -= 16;
                        offset += 16;

                        seed1 += val01;
                        seed2 += s4;
                        seed3 += s1;
                        seed4 += val10;
                    }
                    while (count > 16);

                    a = BinaryOperations.Read32(buffer, offset + count - 8);
                    b = BinaryOperations.Read32(buffer, offset + count - 4);

                    if (count >= 9)
                    {
                        UInt32 c = BinaryOperations.Read32(buffer, offset + count - 16);
                        UInt32 d = BinaryOperations.Read32(buffer, offset + count - 12);
                        A5HashOperations.Multiply64(c + seed3, d + seed4, out seed3, out seed4);
                    }
                }

                seed1 ^= seed3;
                seed2 ^= seed4;
                A5HashOperations.Multiply64(a + seed1, b + seed2, out seed1, out seed2);
                A5HashOperations.Multiply64(val01 ^ seed1, seed2, out a, out b);

                return a ^ b;
            }
        }
        #endregion
    }

    /// <summary>Represents the A5Hash 64-bit implementation. This class cannot be derived.</summary>
    public sealed class A5Hash64 : Hash<UInt64>
    {
        #region Members
        private readonly UInt64 m_Seed;
        #endregion

        #region Properties
        /// <summary>Gets the seed used by the hashing algorithm.</summary>
        [ExcludeFromCodeCoverage]
        public UInt64 Seed => m_Seed;
        #endregion

        #region Constructors
        /// <summary>Initializes a new instance using the specified seed.</summary>
        /// <param name="seed">The seed used by the hashing algorithm.</param>
        [ExcludeFromCodeCoverage]
        public A5Hash64(UInt64 seed)
        {
            m_Seed = seed;
        }

        /// <summary>Initializes a new instance using a seed value of <c>0</c>.</summary>
        [ExcludeFromCodeCoverage]
        public A5Hash64() : this(0ul) { }
        #endregion

        #region Methods
        /// <inheritdoc/>
        protected override UInt64 ComputeHashValueInternal(ReadOnlySpan<Byte> buffer)
        {
            unchecked
            {
                Int32 offset = 0;
                Int32 count = buffer.Length;
                UInt64 val01 = A5HashOperations.VAL01;
                UInt64 val10 = A5HashOperations.VAL10;
                UInt64 seed1 = 0x243F6A8885A308D3ul ^ (UInt64)count;
                UInt64 seed2 = 0x452821E638D01377ul ^ (UInt64)count;

                A5HashOperations.Multiply128(seed2 ^ (m_Seed & val10),
                    seed1 ^ (m_Seed & val01), out seed1, out seed2);

                if (count > 16)
                {
                    val01 ^= seed1;
                    val10 ^= seed2;

                    do
                    {
                        UInt64 a = A5HashOperations.ReadPair(buffer, offset, offset + 4) ^ seed1;
                        UInt64 b = A5HashOperations.ReadPair(buffer, offset + 8, offset + 12) ^ seed2;
                        A5HashOperations.Multiply128(a, b, out seed1, out seed2);

                        count -= 16;
                        offset += 16;
                        seed1 += val01;
                        seed2 += val10;
                    }
                    while (count > 16);
                }

                if (count > 3)
                {
                    Int32 mo = count >> 3;
                    seed1 ^= A5HashOperations.ReadPair(buffer, offset, offset + count - 4);
                    seed2 ^= A5HashOperations.ReadPair(buffer, offset + mo * 4,
                        offset + count - 4 - mo * 4);
                }
                else
                {
                    if (count > 0) seed1 ^= buffer[offset];
                    if (count > 1) seed1 ^= (UInt64)buffer[offset + 1] << 8;
                    if (count > 2) seed1 ^= (UInt64)buffer[offset + 2] << 16;
                }

                A5HashOperations.Multiply128(seed1, seed2, out seed1, out seed2);
                A5HashOperations.Multiply128(val01 ^ seed1, seed2, out seed1, out seed2);

                return seed1 ^ seed2;
            }
        }
        #endregion
    }

    /// <summary>Represents the A5Hash 128-bit implementation. This class cannot be derived.</summary>
    public sealed class A5Hash128 : Hash<(UInt64, UInt64)>
    {
        #region Members
        private readonly UInt64 m_Seed;
        #endregion

        #region Properties
        /// <summary>Gets the seed used by the hashing algorithm.</summary>
        [ExcludeFromCodeCoverage]
        public UInt64 Seed => m_Seed;
        #endregion

        #region Constructors
        /// <summary>Initializes a new instance using the specified seed.</summary>
        /// <param name="seed">The seed used by the hashing algorithm.</param>
        [ExcludeFromCodeCoverage]
        public A5Hash128(UInt64 seed)
        {
            m_Seed = seed;
        }

        /// <summary>Initializes a new instance using a seed value of <c>0</c>.</summary>
        [ExcludeFromCodeCoverage]
        public A5Hash128() : this(0ul) { }
        #endregion

        #region Methods
        /// <inheritdoc/>
        protected override (UInt64, UInt64) ComputeHashValueInternal(ReadOnlySpan<Byte> buffer)
        {
            unchecked
            {
                Int32 offset = 0;
                Int32 count = buffer.Length;
                UInt64 val01 = A5HashOperations.VAL01;
                UInt64 val10 = A5HashOperations.VAL10;
                UInt64 seed1 = 0x243F6A8885A308D3ul ^ (UInt64)count;
                UInt64 seed2 = 0x452821E638D01377ul ^ (UInt64)count;
                UInt64 seed3 = 0xA4093822299F31D0ul;
                UInt64 seed4 = 0xC0AC29B7C97C50DDul;

                A5HashOperations.Multiply128(seed2 ^ (m_Seed & val10),
                    seed1 ^ (m_Seed & val01), out seed1, out seed2);

                UInt64 a;
                UInt64 b;

                if (count < 17)
                {
                    if (count > 3)
                    {
                        Int32 mo = count >> 3;
                        a = A5HashOperations.ReadPair(buffer, 0, count - 4);
                        b = A5HashOperations.ReadPair(buffer, mo * 4, count - 4 - mo * 4);
                    }
                    else
                    {
                        a = 0;
                        b = 0;
                        if (count > 0) a = buffer[0];
                        if (count > 1) a |= (UInt64)buffer[1] << 8;
                        if (count > 2) a |= (UInt64)buffer[2] << 16;
                    }
                }
                else if (count < 33)
                {
                    a = A5HashOperations.ReadPair(buffer, 0, 4);
                    b = A5HashOperations.ReadPair(buffer, 8, 12);
                    UInt64 c = A5HashOperations.ReadPair(buffer, count - 16, count - 12);
                    UInt64 d = A5HashOperations.ReadPair(buffer, count - 8, count - 4);
                    A5HashOperations.Multiply128(c + seed3, d + seed4, out seed3, out seed4);
                    seed1 ^= seed3;
                    seed2 ^= seed4;
                }
                else
                {
                    val01 ^= seed1;
                    val10 ^= seed2;

                    if (count > 64)
                    {
                        UInt64 seed5 = 0x082EFA98EC4E6C89ul;
                        UInt64 seed6 = 0x3F84D5B5B5470917ul;
                        UInt64 seed7 = 0x13198A2E03707344ul;
                        UInt64 seed8 = 0xBE5466CF34E90C6Cul;

                        do
                        {
                            UInt64 s1 = seed1;
                            UInt64 s3 = seed3;
                            UInt64 s5 = seed5;

                            A5HashOperations.Multiply128(BinaryOperations.Read64(buffer, offset) + seed1,
                                BinaryOperations.Read64(buffer, offset + 32) + seed2, out seed1, out seed2);
                            seed1 += val01;
                            seed2 += seed8;

                            A5HashOperations.Multiply128(BinaryOperations.Read64(buffer, offset + 8) + seed3,
                                BinaryOperations.Read64(buffer, offset + 40) + seed4, out seed3, out seed4);
                            seed3 += s1;
                            seed4 += val10;

                            A5HashOperations.Multiply128(BinaryOperations.Read64(buffer, offset + 16) + seed5,
                                BinaryOperations.Read64(buffer, offset + 48) + seed6, out seed5, out seed6);
                            A5HashOperations.Multiply128(BinaryOperations.Read64(buffer, offset + 24) + seed7,
                                BinaryOperations.Read64(buffer, offset + 56) + seed8, out seed7, out seed8);

                            count -= 64;
                            offset += 64;
                            seed5 += s3;
                            seed6 += val10;
                            seed7 += s5;
                            seed8 += val10;
                        }
                        while (count > 64);

                        seed1 ^= seed5;
                        seed2 ^= seed6;
                        seed3 ^= seed7;
                        seed4 ^= seed8;
                    }

                    if (count > 32)
                    {
                        UInt64 s1 = seed1;
                        A5HashOperations.Multiply128(BinaryOperations.Read64(buffer, offset) + seed1,
                            BinaryOperations.Read64(buffer, offset + 8) + seed2, out seed1, out seed2);
                        seed1 += val01;
                        seed2 += seed4;

                        A5HashOperations.Multiply128(BinaryOperations.Read64(buffer, offset + 16) + seed3,
                            BinaryOperations.Read64(buffer, offset + 24) + seed4, out seed3, out seed4);
                        count -= 32;
                        offset += 32;
                        seed3 += s1;
                        seed4 += val10;
                    }

                    a = BinaryOperations.Read64(buffer, offset + count - 16);
                    b = BinaryOperations.Read64(buffer, offset + count - 8);

                    if (count >= 17)
                    {
                        UInt64 c = BinaryOperations.Read64(buffer, offset + count - 32);
                        UInt64 d = BinaryOperations.Read64(buffer, offset + count - 24);
                        A5HashOperations.Multiply128(c + seed3, d + seed4, out seed3, out seed4);
                    }

                    seed1 ^= seed3;
                    seed2 ^= seed4;
                }

                A5HashOperations.Multiply128(a + seed1, b + seed2, out seed1, out seed2);
                A5HashOperations.Multiply128(val01 ^ seed1, seed2, out a, out b);
                UInt64 low = a ^ b;

                A5HashOperations.Multiply128(seed1 ^ seed3, seed2 ^ seed4, out seed3, out seed4);
                UInt64 high = seed3 ^ seed4;

                return (low, high);
            }
        }
        #endregion
    }
}
