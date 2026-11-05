#region Using Directives
using System;
using System.Buffers.Binary;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
#endregion

namespace FastHashes
{
    /// <summary>Represents the base class from which all the XxHash3 implementations must derive. This class is abstract.</summary>
    public abstract class XxHashThree<T> : Hash<T> where T : unmanaged
    {
        #region Constants
        private static readonly Byte[] SECRET =
        {
            0XB8, 0XFE, 0X6C, 0X39, 0X23, 0XA4, 0X4B, 0XBE, 0X7C, 0X01, 0X81, 0X2C, 0XF7, 0X21, 0XAD, 0X1C,
            0XDE, 0XD4, 0X6D, 0XE9, 0X83, 0X90, 0X97, 0XDB, 0X72, 0X40, 0XA4, 0XA4, 0XB7, 0XB3, 0X67, 0X1F,
            0XCB, 0X79, 0XE6, 0X4E, 0XCC, 0XC0, 0XE5, 0X78, 0X82, 0X5A, 0XD0, 0X7D, 0XCC, 0XFF, 0X72, 0X21,
            0XB8, 0X08, 0X46, 0X74, 0XF7, 0X43, 0X24, 0X8E, 0XE0, 0X35, 0X90, 0XE6, 0X81, 0X3A, 0X26, 0X4C,
            0X3C, 0X28, 0X52, 0XBB, 0X91, 0XC3, 0X00, 0XCB, 0X88, 0XD0, 0X65, 0X8B, 0X1B, 0X53, 0X2E, 0XA3,
            0X71, 0X64, 0X48, 0X97, 0XA2, 0X0D, 0XF9, 0X4E, 0X38, 0X19, 0XEF, 0X46, 0XA9, 0XDE, 0XAC, 0XD8,
            0XA8, 0XFA, 0X76, 0X3F, 0XE3, 0X9C, 0X34, 0X3F, 0XF9, 0XDC, 0XBB, 0XC7, 0XC7, 0X0B, 0X4F, 0X1D,
            0X8A, 0X51, 0XE0, 0X4B, 0XCD, 0XB4, 0X59, 0X31, 0XC8, 0X9F, 0X7E, 0XC9, 0XD9, 0X78, 0X73, 0X64,
            0XEA, 0XC5, 0XAC, 0X83, 0X34, 0XD3, 0XEB, 0XC3, 0XC5, 0X81, 0XA0, 0XFF, 0XFA, 0X13, 0X63, 0XEB,
            0X17, 0X0D, 0XDD, 0X51, 0XB7, 0XF0, 0XDA, 0X49, 0XD3, 0X16, 0X55, 0X26, 0X29, 0XD4, 0X68, 0X9E,
            0X2B, 0X16, 0XBE, 0X58, 0X7D, 0X47, 0XA1, 0XFC, 0X8F, 0XF8, 0XB8, 0XD1, 0X7A, 0XD0, 0X31, 0XCE,
            0X45, 0XCB, 0X3A, 0X8F, 0X95, 0X16, 0X04, 0X28, 0XAF, 0XD7, 0XFB, 0XCA, 0XBB, 0X4B, 0X40, 0X7E
        };

        /// <summary>Represents a constant variable used by derived classes.</summary>
        protected const UInt32 P32_1 = 0x9E3779B1u;
        /// <summary>Represents a constant variable used by derived classes.</summary>
        protected const UInt32 P32_2 = 0x85EBCA77u;
        /// <summary>Represents a constant variable used by derived classes.</summary>
        protected const UInt32 P32_3 = 0xC2B2AE3Du;
        /// <summary>Represents a constant variable used by derived classes.</summary>
        protected const UInt64 P64_1 = 0x9E3779B185EBCA87ul;
        /// <summary>Represents a constant variable used by derived classes.</summary>
        protected const UInt64 P64_2 = 0xC2B2AE3D27D4EB4Ful;
        /// <summary>Represents a constant variable used by derived classes.</summary>
        protected const UInt64 P64_3 = 0x165667B19E3779F9ul;
        /// <summary>Represents a constant variable used by derived classes.</summary>
        protected const UInt64 P64_4 = 0x85EBCA77C2B2AE63ul;
        /// <summary>Represents a constant variable used by derived classes.</summary>
        protected const UInt64 P64_5 = 0x27D4EB2F165667C5ul;
        #endregion

        #region Members
        private readonly Byte[] m_LongSecret;
        private readonly UInt64 m_Seed;
        #endregion

        #region Properties
        /// <summary>Represents a constant variable used by derived classes.</summary>
        protected static ReadOnlySpan<Byte> Secret => SECRET;

        /// <summary>Represents a constant variable used by derived classes.</summary>
        protected ReadOnlySpan<Byte> LongSecret => m_LongSecret;

        /// <summary>Gets the seed used by the hashing algorithm.</summary>
        /// <value>An <see cref="T:System.UInt64"/> value.</value>
        [ExcludeFromCodeCoverage]
        public UInt64 Seed => m_Seed;
        #endregion

        #region Constructors
        /// <summary>Represents the compact base constructor used by derived classes.</summary>
        [ExcludeFromCodeCoverage]
        protected XxHashThree(UInt64 seed)
        {
            m_Seed = seed;

            if (seed == 0)
                m_LongSecret = SECRET;
            else
            {
                m_LongSecret = new Byte[192];

                for (Int32 i = 0; i < 192; i += 16)
                {
                    UInt64 lo = unchecked(BinaryOperations.Read64(SECRET, i) + seed);
                    BinaryPrimitives.WriteUInt64LittleEndian(m_LongSecret.AsSpan(i, 8), lo);

                    UInt64 hi = unchecked(BinaryOperations.Read64(SECRET, i + 8) - seed);
                    BinaryPrimitives.WriteUInt64LittleEndian(m_LongSecret.AsSpan(i + 8, 8), hi);
                }
            }
        }
        #endregion

        #region Methods
        /// <summary>Represents an auxiliary hashing function used by derived classes.</summary>
        protected static void Accumulate(Span<UInt64> accumulator, ReadOnlySpan<Byte> data, Int32 dataOffset, ReadOnlySpan<Byte> secret, Int32 secretOffset)
        {
            for (Int32 i = 0; i < 8; ++i)
            {
                UInt64 v = BinaryOperations.Read64(data, dataOffset + i * 8);
                UInt64 k = v ^ BinaryOperations.Read64(secret, secretOffset + i * 8);

                accumulator[i ^ 1] = unchecked(accumulator[i ^ 1] + v);
                accumulator[i] = unchecked(accumulator[i] + ((UInt64)unchecked((UInt32)k) * (UInt32)(k >> 32)));
            }
        }

        /// <summary>Represents an auxiliary hashing function used by derived classes.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected static UInt64 AvalancheHhx(UInt64 hash)
        {
            hash ^= hash >> 33;
            hash = unchecked(hash * P64_2);
            hash ^= hash >> 29;
            hash = unchecked(hash * P64_3);
            hash ^= hash >> 32;

            return hash;
        }

        /// <summary>Represents an auxiliary hashing function used by derived classes.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected static UInt64 AvalancheHhx3(UInt64 hash)
        {
            hash ^= hash >> 37;
            hash = unchecked(hash * 0x165667919E3779F9ul);
            hash ^= hash >> 32;

            return hash;
        }

        /// <summary>Represents an auxiliary hashing function used by derived classes.</summary>
        protected static UInt64 Merge(Span<UInt64> acc, ReadOnlySpan<Byte> secret, Int32 offset, UInt64 start)
        {
            UInt64 v = start;

            for (Int32 i = 0; i < 4; ++i)
            {
                UInt64 a = acc[i * 2] ^ BinaryOperations.Read64(secret, offset + (i * 16));
                UInt64 b = acc[(i * 2) + 1] ^ BinaryOperations.Read64(secret, offset + (i * 16) + 8);
                v = unchecked(v + MulFold(a, b));
            }

            v = AvalancheHhx3(v);

            return v;
        }

        /// <summary>Represents an auxiliary hashing function used by derived classes.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected static UInt64 Mix16(ReadOnlySpan<Byte> data, Int32 offset, ReadOnlySpan<Byte> secret, Int32 secretOffset, UInt64 seed)
        {
            UInt64 a = BinaryOperations.Read64(data, offset) ^ unchecked(BinaryOperations.Read64(secret, secretOffset) + seed);
            UInt64 b = BinaryOperations.Read64(data, offset + 8) ^ unchecked(BinaryOperations.Read64(secret, secretOffset + 8) - seed);
            UInt64 mf = MulFold(a, b);

            return mf;
        }

        /// <summary>Represents an auxiliary hashing function used by derived classes.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected static UInt64 MulFold(UInt64 a, UInt64 b)
        {
            Mul128(a, b, out UInt64 lo, out UInt64 hi);
            UInt64 mf = lo ^ hi;

            return mf;
        }

        /// <summary>Represents an auxiliary hashing function used by derived classes.</summary>
        protected static void ComputeLong(ReadOnlySpan<Byte> buffer, Span<UInt64> acc, ReadOnlySpan<Byte> secret)
        {
            Int32 offset = 0;
            Int32 count = buffer.Length;

            acc[0] = P32_3;
            acc[1] = P64_1;
            acc[2] = P64_2;
            acc[3] = P64_3;
            acc[4] = P64_4;
            acc[5] = P32_2;
            acc[6] = P64_5;
            acc[7] = P32_1;

            Int32 blocks = (count - 1) / 1024;

            for (Int32 block = 0; block < blocks; ++block)
            {
                for (Int32 stripe = 0; stripe < 16; ++stripe)
                    Accumulate(acc, buffer, offset + (stripe * 64), secret, stripe * 8);

                Scramble(acc, secret, 128);

                offset += 1024;
            }

            Int32 stripes = (count - offset - 1) / 64;

            for (Int32 stripe = 0; stripe < stripes; ++stripe)
                Accumulate(acc, buffer, offset + (stripe * 64), secret, stripe * 8);

            Accumulate(acc, buffer, count - 64, secret, 121);
        }

        /// <summary>Represents an auxiliary hashing function used by derived classes.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected static void Mul128(UInt64 a, UInt64 b, out UInt64 lo, out UInt64 hi)
        {
#if NET5_0_OR_GREATER
            hi = Math.BigMul(a, b, out lo);
#else
            UInt64 a0 = unchecked((UInt32)a);
            UInt64 a1 = a >> 32;

            UInt64 b0 = unchecked((UInt32)b);
            UInt64 b1 = b >> 32;

            UInt64 p00 = a0 * b0;
            UInt64 p01 = a0 * b1;
            UInt64 p10 = a1 * b0;
            UInt64 p11 = a1 * b1;

            UInt64 mid = (p00 >> 32) + unchecked((UInt32)p01) + unchecked((UInt32)p10);
            lo = (mid << 32) | unchecked((UInt32)p00);
            hi = unchecked(p11 + (p01 >> 32) + (p10 >> 32) + (mid >> 32));
#endif
        }

        /// <summary>Represents an auxiliary hashing function used by derived classes.</summary>
        protected static void Scramble(Span<UInt64> acc, ReadOnlySpan<Byte> secret, Int32 offset)
        {
            for (Int32 i = 0; i < 8; ++i)
            {
                UInt64 value = acc[i];
                value ^= value >> 47;
                value ^= BinaryOperations.Read64(secret, offset + i * 8);

                acc[i] = unchecked(value * P32_1);
            }
        }

        #endregion
    }

    /// <summary>Represents the 64-bit XxHash3 implementation. This class cannot be derived.</summary>
    public sealed class XxHashThree64 : XxHashThree<UInt64>
    {
        #region Constructors
        /// <summary>Initializes a new instance using a seed value of <c>0</c>.</summary>
        [ExcludeFromCodeCoverage]
        public XxHashThree64() : this(0ul) { }

        /// <summary>Initializes a new instance using the specified seed.</summary>
        /// <param name="seed">The <see cref="T:System.UInt64"/> seed used by the hashing algorithm.</param>
        [ExcludeFromCodeCoverage]
        public XxHashThree64(UInt64 seed) : base(seed) { }
        #endregion

        #region Methods
        private static UInt64 ComputeMedium(ReadOnlySpan<Byte> buffer, UInt64 seed)
        {
            Int32 count = buffer.Length;
            ReadOnlySpan<Byte> secret = Secret;

            UInt64 hash = unchecked((UInt64)count * P64_1);

            if (count <= 128)
            {
                if (count > 96)
                {
                    hash = unchecked(hash + Mix16(buffer, 48, secret, 96, seed));
                    hash = unchecked(hash + Mix16(buffer, count - 64, secret, 112, seed));
                }

                if (count > 64)
                {
                    hash = unchecked(hash + Mix16(buffer, 32, secret, 64, seed));
                    hash = unchecked(hash + Mix16(buffer, count - 48, secret, 80, seed));
                }

                if (count > 32)
                {
                    hash = unchecked(hash + Mix16(buffer, 16, secret, 32, seed));
                    hash = unchecked(hash + Mix16(buffer, count - 32, secret, 48, seed));
                }

                hash = unchecked(hash + Mix16(buffer, 0, secret, 0, seed));
                hash = unchecked(hash + Mix16(buffer, count - 16, secret, 16, seed));
                hash = AvalancheHhx3(hash);
            }
            else
            {
                Int32 rounds = count / 16;
                UInt64 end = Mix16(buffer, count - 16, secret, 119, seed);

                for (Int32 i = 0; i < 8; ++i)
                    hash = unchecked(hash + Mix16(buffer, i * 16, secret, i * 16, seed));

                hash = AvalancheHhx3(hash);

                for (Int32 i = 8; i < rounds; ++i)
                    end = unchecked(end + Mix16(buffer, i * 16, secret, ((i - 8) * 16) + 3, seed));

                hash = AvalancheHhx3(unchecked(hash + end));
            }

            return hash;
        }

        private static UInt64 ComputeShort(ReadOnlySpan<Byte> buffer, UInt64 seed)
        {
            Int32 count = buffer.Length;
            ReadOnlySpan<Byte> secret = Secret;

            UInt64 hash;

            if (count > 8)
            {
                UInt64 bf1 = unchecked((BinaryOperations.Read64(secret, 24) ^ BinaryOperations.Read64(secret, 32)) + seed);
                UInt64 bf2 = unchecked((BinaryOperations.Read64(secret, 40) ^ BinaryOperations.Read64(secret, 48)) - seed);
                UInt64 lo = BinaryOperations.Read64(buffer, 0) ^ bf1;
                UInt64 hi = BinaryOperations.Read64(buffer, count - 8) ^ bf2;

                hash = AvalancheHhx3(unchecked((UInt64)count + BinaryPrimitives.ReverseEndianness(lo) + hi + MulFold(lo, hi)));
            }
            else if (count >= 4)
            {
                seed ^= (UInt64)BinaryPrimitives.ReverseEndianness(unchecked((UInt32)seed)) << 32;

                UInt64 bf = unchecked((BinaryOperations.Read64(secret, 8) ^ BinaryOperations.Read64(secret, 16)) - seed);

                hash = ((UInt64)BinaryOperations.Read32(buffer, 0) << 32) | BinaryOperations.Read32(buffer, count - 4);
                hash = Rrmxmx(hash ^ bf, (UInt64)count);
            }
            else if (count > 0)
            {
                UInt32 cmb = ((UInt32)buffer[0] << 16) | ((UInt32)buffer[count >> 1] << 24) | buffer[count - 1] | ((UInt32)count << 8);
                UInt64 bf = unchecked((UInt64)(BinaryOperations.Read32(secret, 0) ^ BinaryOperations.Read32(secret, 4)) + seed);

                hash = cmb ^ bf;
                hash = AvalancheHhx(hash);
            }
            else
            {
                hash = seed ^ (BinaryOperations.Read64(secret, 56) ^ BinaryOperations.Read64(secret, 64));
                hash = AvalancheHhx(hash);
            }

            return hash;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static UInt64 Rrmxmx(UInt64 hash, UInt64 length)
        {
            hash ^= BinaryOperations.RotateLeft(hash, 49) ^ BinaryOperations.RotateLeft(hash, 24);
            hash = unchecked(hash * 0x9FB21C651E98DF25ul);
            hash ^= unchecked((hash >> 35) + length);
            hash = unchecked(hash * 0x9FB21C651E98DF25ul);
            hash ^= hash >> 28;

            return hash;
        }

        /// <inheritdoc/>
        protected override UInt64 ComputeHashValue(ReadOnlySpan<Byte> buffer)
        {
            Int32 count = buffer.Length;

            UInt64 hash;

            if (count <= 16)
                hash = ComputeShort(buffer, Seed);
            else if (count <= 240)
                hash = ComputeMedium(buffer, Seed);
            else
            {
                Span<UInt64> accumulator = stackalloc UInt64[8];
                ComputeLong(buffer, accumulator, LongSecret);

                UInt64 start = unchecked((UInt64)count * P64_1);
                hash = Merge(accumulator, LongSecret, 11, start);
            }

            return hash;
        }

        #endregion
    }

    /// <summary>Represents the 128-bit XxHash3 implementation. This class cannot be derived.</summary>
    public sealed class XxHashThree128 : XxHashThree<(UInt64, UInt64)>
    {
        #region Constructors
        /// <summary>Initializes a new instance using a seed value of <c>0</c>.</summary>
        [ExcludeFromCodeCoverage]
        public XxHashThree128() : this(0ul) { }

        /// <summary>Initializes a new instance using the specified seed.</summary>
        /// <param name="seed">The <see cref="T:System.UInt64"/> seed used by the hashing algorithm.</param>
        [ExcludeFromCodeCoverage]
        public XxHashThree128(UInt64 seed) : base(seed) { }
        #endregion

        #region Methods
        private static void ComputeMedium(ReadOnlySpan<Byte> buffer, UInt64 seed, out UInt64 lo, out UInt64 hi)
        {
            Int32 count = buffer.Length;
            ReadOnlySpan<Byte> secret = Secret;

            UInt64 loAccumulator = unchecked((UInt64)count * P64_1);
            UInt64 hiAccumulator = 0;

            if (count <= 128)
            {
                if (count > 96)
                    Mix32(buffer, seed, 48, count - 64, secret, 96, ref loAccumulator, ref hiAccumulator);

                if (count > 64)
                    Mix32(buffer, seed, 32, count - 48, secret, 64, ref loAccumulator, ref hiAccumulator);

                if (count > 32)
                    Mix32(buffer, seed, 16, count - 32, secret, 32, ref loAccumulator, ref hiAccumulator);

                Mix32(buffer, seed, 0, count - 16, secret, 0, ref loAccumulator, ref hiAccumulator);
            }
            else
            {
                for (Int32 i = 32; i < 160; i += 32)
                    Mix32(buffer, seed, i - 32, i - 16, secret, i - 32, ref loAccumulator, ref hiAccumulator);

                loAccumulator = AvalancheHhx3(loAccumulator);
                hiAccumulator = AvalancheHhx3(hiAccumulator);

                for (Int32 i = 160; i <= count; i += 32)
                    Mix32(buffer, seed, i - 32, i - 16, secret, 3 + i - 160, ref loAccumulator, ref hiAccumulator);

                Mix32(buffer, unchecked(0ul - seed), count - 16, count - 32, secret, 103, ref loAccumulator, ref hiAccumulator);
            }

            lo = AvalancheHhx3(unchecked(loAccumulator + hiAccumulator));
            hi = unchecked(0ul - AvalancheHhx3(loAccumulator * P64_1 + hiAccumulator * P64_4 + ((UInt64)count - seed) * P64_2));
        }

        private static void ComputeShort(ReadOnlySpan<Byte> buffer, UInt64 seed, out UInt64 lo, out UInt64 hi)
        {
            Int32 count = buffer.Length;
            ReadOnlySpan<Byte> secret = Secret;

            if (count > 8)
            {
                UInt64 loBf = unchecked((BinaryOperations.Read64(secret, 32) ^ BinaryOperations.Read64(secret, 40)) - seed);
                UInt64 hiBf = unchecked((BinaryOperations.Read64(secret, 48) ^ BinaryOperations.Read64(secret, 56)) + seed);
                UInt64 loIn = BinaryOperations.Read64(buffer, 0);
                UInt64 hiIn = BinaryOperations.Read64(buffer, count - 8);

                Mul128(loIn ^ hiIn ^ loBf, P64_1, out UInt64 mLow, out UInt64 mHigh);

                mLow = unchecked(mLow + ((UInt64)(count - 1) << 54));
                hiIn ^= hiBf;
                mHigh = unchecked(mHigh + hiIn + ((UInt64)unchecked((UInt32)hiIn) * (P32_2 - 1)));
                mLow ^= BinaryPrimitives.ReverseEndianness(mHigh);

                Mul128(mLow, P64_2, out lo, out hi);
                hi = unchecked(hi + mHigh * P64_2);

                lo = AvalancheHhx3(lo);
                hi = AvalancheHhx3(hi);
            }
            else if (count >= 4)
            {
                seed ^= (UInt64)BinaryPrimitives.ReverseEndianness(unchecked((UInt32)seed)) << 32;

                UInt64 bf = unchecked((BinaryOperations.Read64(secret, 16) ^ BinaryOperations.Read64(secret, 24)) + seed);

                UInt64 loIn = BinaryOperations.Read32(buffer, 0);
                UInt64 hiIn = BinaryOperations.Read32(buffer, count - 4);
                UInt64 totIn = loIn | (hiIn << 32);

                Mul128(totIn ^ bf, unchecked(P64_1 + ((UInt64)count << 2)), out lo, out hi);

                hi = unchecked(hi + (lo << 1));
                lo ^= hi >> 3;
                lo ^= lo >> 35;
                lo = unchecked(lo * 0x9FB21C651E98DF25ul);
                lo ^= lo >> 28;
                hi = AvalancheHhx3(hi);
            }
            else if (count > 0)
            {
                UInt32 loCmb = ((UInt32)buffer[0] << 16) | ((UInt32)buffer[count >> 1] << 24) | buffer[count - 1] | ((UInt32)count << 8);
                UInt32 hiCmb = BinaryOperations.RotateLeft(BinaryPrimitives.ReverseEndianness(loCmb), 13);
                UInt64 loBf = unchecked((UInt64)(BinaryOperations.Read32(secret, 0) ^ BinaryOperations.Read32(secret, 4)) + seed);
                UInt64 hiBf = unchecked((UInt64)(BinaryOperations.Read32(secret, 8) ^ BinaryOperations.Read32(secret, 12)) - seed);

                lo = AvalancheHhx(loCmb ^ loBf);
                hi = AvalancheHhx(hiCmb ^ hiBf);
            }
            else
            {
                lo = AvalancheHhx(seed ^ (BinaryOperations.Read64(secret, 64) ^ BinaryOperations.Read64(secret, 72)));
                hi = AvalancheHhx(seed ^ (BinaryOperations.Read64(secret, 80) ^ BinaryOperations.Read64(secret, 88)));
            }
        }

        private static void Mix32(ReadOnlySpan<Byte> buffer, UInt64 seed, Int32 offset1, Int32 offset2, ReadOnlySpan<Byte> secret, Int32 secretOffset, ref UInt64 low, ref UInt64 high)
        {
            low = unchecked(low + Mix16(buffer, offset1, secret, secretOffset, seed));
            low ^= unchecked(BinaryOperations.Read64(buffer, offset2) + BinaryOperations.Read64(buffer, offset2 + 8));

            high = unchecked(high + Mix16(buffer, offset2, secret, secretOffset + 16, seed));
            high ^= unchecked(BinaryOperations.Read64(buffer, offset1) + BinaryOperations.Read64(buffer, offset1 + 8));
        }

        /// <inheritdoc/>
        protected override (UInt64, UInt64) ComputeHashValue(ReadOnlySpan<Byte> buffer)
        {
            Int32 count = buffer.Length;
            UInt64 lo, hi;

            if (count <= 16)
                ComputeShort(buffer, Seed, out lo, out hi);
            else if (count <= 240)
                ComputeMedium(buffer, Seed, out lo, out hi);
            else
            {
                Span<UInt64> accumulator = stackalloc UInt64[8];
                ComputeLong(buffer, accumulator, LongSecret);

                UInt64 loStart = unchecked((UInt64)count * P64_1);
                UInt64 hiStart = ~unchecked((UInt64)count * P64_2);

                lo = Merge(accumulator, LongSecret, 11, loStart);
                hi = Merge(accumulator, LongSecret, 117, hiStart);
            }

            return (lo, hi);
        }
        #endregion
    }
}