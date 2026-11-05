#region Using Directives
using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
#endregion

namespace FastHashes
{
    /// <summary>Represents the MumHash implementation. This class cannot be derived.</summary>
    public sealed class MumHash : Hash<UInt64>
    {
        #region Constants
        private const UInt64 BSP = 0xC42B5E2E6480B23Bul;
        private const UInt64 UP = 0x7B51EC3D22F7096Ful;
        private const UInt64 TP = 0xAF47D47C99B1461Bul;

        private static readonly UInt64[] P =
        {
            0x9EBDCAE10D981691ul, 0x32B9B9B97A27AC7Dul, 0x29B5584D83D35BBDul, 0x4B04E0E61401255Ful,
            0x25E8F7B1F1C9D027ul, 0x80D4C8C000F3E881ul, 0xBD1255431904B9DDul, 0x8A3BD4485EEE6D81ul,
        };
        #endregion

        #region Members
        private readonly UInt64 m_Seed;
        #endregion

        #region Properties


        /// <summary>Gets the seed used by the hashing algorithm.</summary>
        /// <value>An <see cref="T:System.UInt64"/> value.</value>
        [ExcludeFromCodeCoverage]
        public UInt64 Seed => m_Seed;
        #endregion

        #region Constructors
        /// <summary>Initializes a new instance using the specified seed.</summary>
        /// <param name="seed">The <see cref="T:System.UInt64"/> seed used by the hashing algorithm.</param>
        [ExcludeFromCodeCoverage]
        public MumHash(UInt32 seed)
        {
            m_Seed = seed;
        }

        /// <summary>Initializes a new instance using a seed value of <c>0</c>.</summary>
        [ExcludeFromCodeCoverage]
        public MumHash() : this(0u) { }
        #endregion

        #region Methods
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static UInt64 Mum(UInt64 v1, UInt64 v2)
        {
#if NET5_0_OR_GREATER
            UInt64 hi = Math.BigMul(v1, v2, out UInt64 lo);
#else
            UInt64 hv1 = v1 >> 32;
            UInt64 lv1 = unchecked((UInt32)v1);

            UInt64 hv2 = v2 >> 32;
            UInt64 lv2 = unchecked((UInt32)v2);

            UInt64 rh = hv1 * hv2;
            UInt64 rl = lv1 * lv2;

            UInt64 rm0 = hv1 * lv2;
            UInt64 rm1 = hv2 * lv1;

            UInt64 carry = 0ul;

            UInt64 t = unchecked(rl + (rm0 << 32));
            carry += (t < rl) ? 1ul : 0ul;

            UInt64 lo = unchecked(t + (rm1 << 32));
            carry += (lo < t) ? 1ul : 0ul;

            UInt64 hi = rh + (rm0 >> 32) + (rm1 >> 32) + carry;
#endif

            UInt64 v = unchecked(hi + lo);

            return v;
        }

        /// <inheritdoc/>
        protected override UInt64 ComputeHashValueInternal(ReadOnlySpan<Byte> buffer)
        {
            Int32 offset = 0;
            Int32 count = buffer.Length;

            UInt64 hash = Mum(unchecked(m_Seed + (UInt64)count), BSP);

            if (count == 0)
                goto Finalize;

            while (count > 64)
            {
                for (Int32 i = 0; i < 8; i += 2)
                {
                    UInt64 v1 = BinaryOperations.Read64(buffer, offset) ^ P[i];
                    UInt64 v2 = BinaryOperations.Read64(buffer, offset + 8) ^ P[i + 1];

                    if (v1 == 0ul)
                        v1 = P[i];

                    if (v2 == 0ul)
                        v2 = P[i + 1];

                    hash ^= Mum(v1, v2);
                    offset += 16;
                }

                hash = Mum(hash, UP);
                count -= 64;
            }

            Int32 blocks = count / 8;

            for (Int32 i = 0; i < blocks; ++i)
            {
                UInt64 v = unchecked(BinaryOperations.Read64(buffer, offset) + P[i]);
                hash ^= Mum(v, P[i]);
                offset += 8;
            }

            Int32 remaining = count - (blocks * 8);

            if (remaining > 0)
            {
                UInt64 v = unchecked(BinaryOperations.ReadTail64(buffer, offset) + P[7 - remaining]);
                hash ^= Mum(v, TP);
            }

        Finalize:

            hash = Mum(hash, hash);

            return hash;
        }

        #endregion
    }
}