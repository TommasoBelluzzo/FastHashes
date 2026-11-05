#region Using Directives
using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
#endregion

namespace FastHashes
{
    /// <summary>Represents the PolymurHash implementation. This class cannot be derived.</summary>
    public sealed class PolymurHash : Hash<UInt64>
    {
        #region Members
        private readonly UInt64 m_K;
        private readonly UInt64 m_K2;
        private readonly UInt64 m_K7;
        private readonly UInt64 m_KS;
        private readonly UInt64 m_Seed;
        private readonly UInt64 m_Tweak;
        #endregion

        #region Properties


        /// <summary>Gets the seed used by the hashing algorithm.</summary>
        /// <value>An <see cref="T:System.UInt64"/> value.</value>
        [ExcludeFromCodeCoverage]
        public UInt64 Seed => m_Seed;

        /// <summary>Gets the tweak applied during the finalization step of the hashing algorithm.</summary>
        /// <value>An <see cref="T:System.UInt64"/> value.</value>
        [ExcludeFromCodeCoverage]
        public UInt64 Tweak => m_Tweak;
        #endregion

        #region Constructors
        /// <summary>Initializes a new instance using the specified seed.</summary>
        /// <param name="seed">The <see cref="T:System.UInt64"/> seed used by the hashing algorithm.</param>
        /// <param name="tweak">The <see cref="T:System.UInt64"/> tweak applied during the finalization step of the hashing algorithm.</param>
        [ExcludeFromCodeCoverage]
        public PolymurHash(UInt64 seed, UInt64 tweak)
        {
            m_Seed = seed;
            m_Tweak = tweak;

            UInt64 kSeed = Mix(unchecked(seed + 0x3C6EF372FE94F82Bul));
            UInt64 sSeed = Mix(unchecked(seed + 0xA54FF53A5F1D36F1ul));

            m_KS = sSeed ^ 0x6A09E667F3BCC908ul;

            Span<UInt64> powers = stackalloc UInt64[64];
            powers[0] = 37ul;
            powers[32] = 559096694736811184ul;

            for (Int32 i = 0; i < 31; ++i)
            {
                powers[i + 1] = ReduceExtra(MulReduce(powers[i], powers[i]));
                powers[i + 33] = ReduceExtra(MulReduce(powers[i + 32], powers[i + 32]));
            }

            UInt64 k7m = (1ul << 60) - (1ul << 56);

            while (true)
            {
                kSeed = unchecked(kSeed + 0xBB67AE8584CAA73Bul);

                UInt64 e = (kSeed >> 3) | 1ul;

                if (((e % 3) == 0) || ((e % 5) == 0) || ((e % 7) == 0))
                    continue;

                if (((e % 11) == 0) || ((e % 13) == 0) || ((e % 31) == 0))
                    continue;

                if (((e % 41) == 0) || ((e % 61) == 0) || ((e % 151) == 0) || ((e % 331) == 0) || ((e % 1321) == 0))
                    continue;

                UInt64 ka = 1ul;
                UInt64 kb = 1ul;

                for (Int32 i = 0; e != 0; i += 2, e >>= 2)
                {
                    if ((e & 1ul) != 0)
                        ka = ReduceExtra(MulReduce(ka, powers[i]));

                    if ((e & 2ul) != 0)
                        kb = ReduceExtra(MulReduce(kb, powers[i + 1]));
                }

                UInt64 k = ReduceExtra(MulReduce(ka, kb));
                UInt64 k2 = ReduceExtra(MulReduce(k, k));
                UInt64 k3 = MulReduce(k, k2);
                UInt64 k4 = MulReduce(k2, k2);
                UInt64 k7 = ReduceExtra(MulReduce(k3, k4));

                if (k7 < k7m)
                {
                    m_K = k;
                    m_K2 = k2;
                    m_K7 = k7;

                    break;
                }
            }
        }

        /// <summary>Initializes a new instance using seed and tweak values equal to <c>0</c>.</summary>
        [ExcludeFromCodeCoverage]
        public PolymurHash() : this(0ul, 0ul) { }

        /// <summary>Initializes a new instance with the specified seed and a tweak value of <c>0</c>.</summary>
        /// <param name="seed">The <see cref="T:System.UInt64"/> seed used by the hashing algorithm.</param>
        [ExcludeFromCodeCoverage]
        public PolymurHash(UInt64 seed) : this(seed, 0ul) { }
        #endregion

        #region Methods
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static UInt64 Mix(UInt64 x)
        {
            x ^= x >> 32;
            x = unchecked(x * 0xE9846AF9B1A615Dul);
            x ^= x >> 32;
            x = unchecked(x * 0xE9846AF9B1A615Dul);
            x ^= x >> 28;

            return x;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static UInt64 MulReduce(UInt64 a, UInt64 b)
        {
            Mul128(a, b, out UInt64 lo, out UInt64 hi);
            UInt64 r = Reduce(lo, hi);

            return r;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static UInt64 Read56(ReadOnlySpan<Byte> buffer, Int32 offset)
        {
            return BinaryOperations.Read64(buffer, offset) & 0x00FFFFFFFFFFFFFFul;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static UInt64 ReadShort(ReadOnlySpan<Byte> buffer)
        {
            Int32 count = buffer.Length;

            if (count == 0)
                return 0ul;

            UInt64 v;

            if (count < 4)
            {
                v = buffer[0];
                v |= (UInt64)buffer[count / 2] << (8 * (count / 2));
                v |= (UInt64)buffer[count - 1] << (8 * (count - 1));
            }
            else
            {
                UInt64 lo = BinaryOperations.Read32(buffer, 0);
                UInt64 hi = BinaryOperations.Read32(buffer, count - 4);
                v = lo | (hi << (8 * (count - 4)));
            }

            return v;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static UInt64 Reduce(UInt64 lo, UInt64 hi)
        {
            return unchecked((lo & ((1ul << 61) - 1ul)) + ((lo >> 61) | (hi << 3)));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static UInt64 ReduceExtra(UInt64 x)
        {
            return unchecked((x & ((1ul << 61) - 1ul)) + (x >> 61));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void Add128(ref UInt64 lo, ref UInt64 hi, UInt64 loAddon, UInt64 hiAddon)
        {
            UInt64 prior = lo;
            lo = unchecked(lo + loAddon);
            hi = unchecked(hi + hiAddon + (lo < prior ? 1ul : 0ul));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void Mul128(UInt64 a, UInt64 b, out UInt64 lo, out UInt64 hi)
        {
#if NET5_0_OR_GREATER
            hi = Math.BigMul(a, b, out lo);
#else
            UInt64 a0 = unchecked((UInt32)a);
            UInt64 a1 = a >> 32;

            UInt64 b0 = unchecked((UInt32)b);
            UInt64 b1 = b >> 32;

            UInt64 p0 = a0 * b0;
            UInt64 p1 = a0 * b1;
            UInt64 p2 = a1 * b0;
            UInt64 p3 = a1 * b1;

            UInt64 cross = (p0 >> 32) + unchecked((UInt32)p1) + p2;
            lo = (cross << 32) | unchecked((UInt32)p0);
            hi = unchecked((p1 >> 32) + (cross >> 32) + p3);
#endif
        }

        /// <inheritdoc/>
        protected override UInt64 ComputeHashValueInternal(ReadOnlySpan<Byte> buffer)
        {
            Int32 offset = 0;
            Int32 count = buffer.Length;

            UInt64 hash = m_Tweak;

            if (count <= 7)
                hash = unchecked(hash + MulReduce(unchecked(m_K + ReadShort(buffer)), unchecked(m_K2 + (UInt64)count)));
            else
            {
                UInt64 k3 = MulReduce(m_K, m_K2);
                UInt64 k4 = MulReduce(m_K2, m_K2);

                if (count >= 50)
                {
                    UInt64 k5 = ReduceExtra(MulReduce(m_K, k4));
                    UInt64 k6 = ReduceExtra(MulReduce(m_K2, k4));

                    k3 = ReduceExtra(k3);
                    k4 = ReduceExtra(k4);

                    UInt64 h = 0ul;

                    do
                    {
                        UInt64 m0 = Read56(buffer, offset);
                        UInt64 m1 = Read56(buffer, offset + 7);
                        UInt64 m2 = Read56(buffer, offset + 14);
                        UInt64 m3 = Read56(buffer, offset + 21);
                        UInt64 m4 = Read56(buffer, offset + 28);
                        UInt64 m5 = Read56(buffer, offset + 35);
                        UInt64 m6 = Read56(buffer, offset + 42);

                        Mul128(unchecked(m_K + m0), unchecked(k6 + m1), out UInt64 lo, out UInt64 hi);
                        Mul128(unchecked(m_K2 + m2), unchecked(k5 + m3), out UInt64 lo1, out UInt64 hi1);
                        Add128(ref lo, ref hi, lo1, hi1);
                        Mul128(unchecked(k3 + m4), unchecked(k4 + m5), out lo1, out hi1);
                        Add128(ref lo, ref hi, lo1, hi1);
                        Mul128(unchecked(h + m6), m_K7, out lo1, out hi1);
                        Add128(ref lo, ref hi, lo1, hi1);

                        h = Reduce(lo, hi);

                        count -= 49;
                        offset += 49;
                    }
                    while (count >= 50);

                    UInt64 k14 = MulReduce(m_K7, m_K7);
                    UInt64 hk14 = MulReduce(ReduceExtra(h), k14);

                    hash = unchecked(hash + ReduceExtra(hk14));
                }

                if (count >= 8)
                {
                    UInt64 m0 = Read56(buffer, offset);
                    UInt64 m1 = Read56(buffer, offset + (count - 7) / 2);
                    UInt64 m2 = BinaryOperations.Read64(buffer, offset + count - 8) >> 8;

                    Mul128(unchecked(m_K2 + m0), unchecked(m_K7 + m1), out UInt64 lo0, out UInt64 hi0);
                    Mul128(unchecked(m_K + m2), unchecked(k3 + (UInt64)count), out UInt64 lo1, out UInt64 hi1);

                    if (count <= 21)
                    {
                        Add128(ref lo0, ref hi0, lo1, hi1);
                        hash = unchecked(hash + Reduce(lo0, hi0));
                    }
                    else
                    {
                        UInt64 m3 = Read56(buffer, offset + 7);
                        UInt64 m4 = Read56(buffer, offset + 14);
                        UInt64 m5 = Read56(buffer, offset + count - 21);
                        UInt64 m6 = Read56(buffer, offset + count - 14);
                        UInt64 t0r = Reduce(lo0, hi0);

                        Mul128(unchecked(m_K2 + m3), unchecked(m_K7 + m4), out UInt64 lo2, out UInt64 hi2);
                        Mul128(unchecked(t0r + m5), unchecked(k4 + m6), out UInt64 lo3, out UInt64 hi3);
                        Add128(ref lo1, ref hi1, lo2, hi2);
                        Add128(ref lo1, ref hi1, lo3, hi3);

                        hash = unchecked(hash + Reduce(lo1, hi1));
                    }
                }
                else
                    hash = unchecked(hash + MulReduce(unchecked(m_K + ReadShort(buffer.Slice(offset, count))), unchecked(m_K2 + (UInt64)count)));
            }

            hash = unchecked(Mix(hash) + m_KS);

            return hash;
        }

        #endregion
    }
}