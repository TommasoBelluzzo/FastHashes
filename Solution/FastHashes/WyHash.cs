#region Using Directives
using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
#endregion

namespace FastHashes
{
    /// <summary>Represents the WyHash32 implementation. This class cannot be derived.</summary>
    public sealed class WyHash32 : Hash
    {
        #region Constants
        private const UInt32 P1 = 0X53C5CA59u;
        private const UInt32 P2 = 0X74743C1Bu;
        #endregion

        #region Members
        private readonly UInt32 m_Seed;
        #endregion

        #region Properties
        /// <inheritdoc/>
        [ExcludeFromCodeCoverage]
        public override Int32 Length => 32;

        /// <summary>Gets the seed used by the hashing algorithm.</summary>
        /// <value>An <see cref="T:System.UInt32"/> value.</value>
        [ExcludeFromCodeCoverage]
        public UInt32 Seed => m_Seed;
        #endregion

        #region Constructors
        /// <summary>Initializes a new instance using the specified seed.</summary>
        /// <param name="seed">The <see cref="T:System.UInt64"/> seed used by the hashing algorithm.</param>
        [ExcludeFromCodeCoverage]
        public WyHash32(UInt32 seed)
        {
            m_Seed = seed;
        }

        /// <summary>Initializes a new instance using a seed value of <c>0</c>.</summary>
        [ExcludeFromCodeCoverage]
        public WyHash32() : this(0u) { }
        #endregion

        #region Methods
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void WyMix(ref UInt32 a, ref UInt32 b)
        {
            UInt64 c = (UInt64)(a ^ P1) * (b ^ P2);

            a = (UInt32)c;
            b = (UInt32)(c >> 32);
        }

        /// <inheritdoc/>
        protected override Byte[] ComputeHashInternal(ReadOnlySpan<Byte> buffer)
        {
            Int32 offset = 0;
            Int32 count = buffer.Length;

            Int32 i = count;

            UInt32 seed = m_Seed ^ (UInt32)((UInt64)count >> 32);
            UInt32 s0 = (UInt32)count;

            WyMix(ref seed, ref s0);

            while (i > 8)
            {
                seed ^= BinaryOperations.Read32(buffer, offset);
                s0 ^= BinaryOperations.Read32(buffer, offset + 4);

                WyMix(ref seed, ref s0);

                i -= 8;
                offset += 8;
            }

            if (i >= 4)
            {
                seed ^= BinaryOperations.Read32(buffer, offset);
                s0 ^= BinaryOperations.Read32(buffer, offset + i - 4);
            }
            else if (i > 0)
                seed ^= ((UInt32)buffer[offset] << 16) | ((UInt32)buffer[offset + (i >> 1)] << 8) | (UInt32)buffer[offset + i - 1];

            WyMix(ref seed, ref s0);
            WyMix(ref seed, ref s0);

            UInt32 hash = seed ^ s0;
            Byte[] result = BinaryOperations.ToArray32(hash);

            return result;
        }
        #endregion
    }

    /// <summary>Represents the WyHash64 implementation. This class cannot be derived.</summary>
    public sealed class WyHash64 : Hash
    {
        #region Constants
        private static readonly UInt64[] DEFAULT_SECRET = new UInt64[] { 0XA0761D6478BD642ful, 0XE7037ED1A0B428DBul, 0X8EBC6AF09C88C6E3ul, 0X589965CC75374CC3ul };
        #endregion

        #region Members
        private readonly UInt64[] m_Secret;
        private readonly UInt64 m_Seed;
        #endregion

        #region Properties
        /// <inheritdoc/>
        [ExcludeFromCodeCoverage]
        public override Int32 Length => 64;

        /// <summary>Gets the seed used by the hashing algorithm.</summary>
        /// <value>An <see cref="T:System.UInt64"/> value.</value>
        [ExcludeFromCodeCoverage]
        public UInt64 Seed => m_Seed;
        #endregion

        #region Constructors
        /// <summary>Initializes a new instance using the specified seed.</summary>
        /// <param name="seed">The <see cref="T:System.UInt64"/> seed used by the hashing algorithm.</param>
        /// <param name="secret">The <see cref="T:System.UInt64"/>[] representing the seed used by the hashing algorithm.</param>
        /// <exception cref="T:System.ArgumentException">Thrown when the number of values in <paramref name="secret">secret</paramref> is not equal to <c>4</c>.</exception>
        /// <exception cref="T:System.ArgumentNullException">Thrown when <paramref name="secret">secret</paramref> is <c>null</c>.</exception>
        [ExcludeFromCodeCoverage]
        public WyHash64(UInt64 seed, UInt64[] secret)
        {
            if (secret == null)
                throw new ArgumentNullException(nameof(secret));

            if (secret.Length != 4)
                throw new ArgumentException("The specified array must contains 4 values.", nameof(secret));

            m_Secret = secret;
            m_Seed = seed;
        }

        /// <summary>Initializes a new instance using a seed value of <c>0</c> and the default secret.</summary>
        [ExcludeFromCodeCoverage]
        public WyHash64() : this(0ul, DEFAULT_SECRET) { }

        /// <summary>Initializes a new instance using the specified <see cref="T:System.UInt64"/> seed and the default secret.</summary>
        /// <param name="seed">The <see cref="T:System.UInt64"/> seed used by the hashing algorithm.</param>
        [ExcludeFromCodeCoverage]
        public WyHash64(UInt64 seed) : this(seed, DEFAULT_SECRET) { }

        /// <summary>Initializes a new instance using a seed value of <c>0</c> and the specified <see cref="T:System.UInt64"/>[] secret.</summary>
        /// <param name="secret">The <see cref="T:System.UInt64"/>[] representing the seed used by the hashing algorithm.</param>
        /// <exception cref="T:System.ArgumentException">Thrown when the number of values in <paramref name="secret">secret</paramref> is not equal to <c>4</c>.</exception>
        /// <exception cref="T:System.ArgumentNullException">Thrown when <paramref name="secret">secret</paramref> is <c>null</c>.</exception>
        [ExcludeFromCodeCoverage]
        public WyHash64(UInt64[] secret) : this(0ul, secret) { }
        #endregion

        #region Methods
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static UInt64 WyMix(UInt64 a, UInt64 b)
        {
            WyMum(ref a, ref b);
            UInt64 v = a ^ b;

            return v;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void WyMum(ref UInt64 a, ref UInt64 b)
        {
            UInt64 ha = a >> 32;
            UInt64 hb = b >> 32;

            UInt64 la = (UInt32)a;
            UInt64 lb = (UInt32)b;

            UInt64 rh = ha * hb;
            UInt64 rm0 = ha * lb;
            UInt64 rm1 = hb * la;
            UInt64 rl = la * lb;
            UInt64 t = rl + (rm0 << 32);

            UInt64 lo = t + (rm1 << 32);
            UInt64 hi = rh + (rm0 >> 32) + (rm1 >> 32) + ((t < rl) ? 1ul : 0ul) + ((lo < t) ? 1ul : 0ul);

            a ^= lo;
            b ^= hi;
        }

        /// <inheritdoc/>
        protected override Byte[] ComputeHashInternal(ReadOnlySpan<Byte> buffer)
        {
            Int32 offset = 0;
            Int32 count = buffer.Length;

            UInt64 seed = m_Seed ^ m_Secret[0];
            UInt64 a, b;

            if (count <= 16)
            {
                if (count >= 4)
                {
                    a = ((UInt64)BinaryOperations.Read32(buffer, 0) << 32) | ((UInt64)BinaryOperations.Read32(buffer, (count >> 3) << 2));
                    b = ((UInt64)BinaryOperations.Read32(buffer, count - 4) << 32) | ((UInt64)BinaryOperations.Read32(buffer, count - 4 - ((count >> 3) << 2)));
                }
                else if (count > 0)
                {
                    a = ((UInt64)buffer[0] << 16) | (((UInt64)buffer[count >> 1]) << 8) | (UInt64)buffer[count - 1];
                    b = 0ul;
                }
                else
                {
                    a = 0ul;
                    b = 0ul;
                }
            }
            else
            {
                Int32 i = count;

                if (i > 48)
                {
                    UInt64 s1 = seed;
                    UInt64 s2 = seed;

                    do
                    {
                        seed = WyMix(BinaryOperations.Read64(buffer, offset) ^ m_Secret[1], BinaryOperations.Read64(buffer, offset + 8) ^ seed);
                        s1 = WyMix(BinaryOperations.Read64(buffer, offset + 16) ^ m_Secret[2], BinaryOperations.Read64(buffer, offset + 24) ^ s1);
                        s2 = WyMix(BinaryOperations.Read64(buffer, offset + 32) ^ m_Secret[3], BinaryOperations.Read64(buffer, offset + 40) ^ s2);

                        i -= 48;
                        offset += 48;
                    }
                    while (i > 48);

                    seed ^= s1 ^ s2;
                }

                while (i > 16)
                {
                    seed = WyMix(BinaryOperations.Read64(buffer, offset) ^ m_Secret[1], BinaryOperations.Read64(buffer, offset + 8) ^ seed);

                    i -= 16;
                    offset += 16;
                }

                a = BinaryOperations.Read64(buffer, offset + i - 16);
                b = BinaryOperations.Read64(buffer, offset + i - 8);
            }

            UInt64 h1 = m_Secret[1] ^ (UInt64)count;
            UInt64 h2 = WyMix(a ^ m_Secret[1], b ^ seed);
            UInt64 hash = WyMix(h1, h2);

            Byte[] result = BinaryOperations.ToArray64(hash);

            return result;
        }
        #endregion
    }
}