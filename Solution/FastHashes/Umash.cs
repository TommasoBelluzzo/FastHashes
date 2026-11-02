#region Using Directives
using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
#endregion

namespace FastHashes
{
    /// <summary>Represents the base class from which all the Umash implementations must derive. This class is abstract.</summary>
    public abstract class Umash : Hash
    {
        #region Constants
        private const Int32 BLOCK_SIZE = 256;
        private const UInt64 MIX1 = 0xBF58476D1CE4E5B9ul;
        private const UInt64 MIX2 = 0x94D049BB133111EBul;
        #endregion

        #region Members
        private static readonly UInt64[] s_Poly =
        {
            0x1750289755934E3Aul, 0x0D1B5522F4059E62ul,
            0x08D5C6EDBB37B832ul, 0x0DAAB0FD57364132ul
        };

        private static readonly UInt64[] s_Oh =
        {
            0x50CF4D1A31F6A7C2ul, 0x9125C205CF7BFBFDul,
            0x34B5A29915027BD9ul, 0x4064DB0605947D66ul,
            0xDFC3AA6B349CC9F8ul, 0xCA46D07129E74931ul,
            0x183F8FD8552A62D0ul, 0x2916A957B5ACA803ul,
            0x77C3ADB78088F946ul, 0x4EC3286E27BD1E4Aul,
            0x435018964199E279ul, 0x961ACC4E8EF00050ul,
            0x8897F0876D175DF1ul, 0x0704E01B94943390ul,
            0xC296E3A20BD6003Cul, 0x67EEAB1FFAAA34CBul,
            0xD4281A801ED2A70Aul, 0xFFFBFE48FFF2619Aul,
            0x55562AC75E839705ul, 0xC8C709E6F9102E85ul,
            0x25923B558F59C7E0ul, 0x6C5118D78C2A323Eul,
            0x4E716EAB314E397Aul, 0xBC5B1FB6AE2A2AC1ul,
            0x6D28C944D3F5A552ul, 0xFC7A62968C512B59ul,
            0xA920582BE95D7874ul, 0xBAE9A930A8AD2706ul,
            0x290477BC432047E3ul, 0xEBB75BE5124E9E0Eul,
            0x99449FE997B86C82ul, 0x2BADC1034F1ED132ul,
            0xAFA8FC171FFFE6DDul, 0xCE31841DA9DC1647ul
        };

        private readonly UInt64 m_Seed;
        #endregion

        #region Properties
        /// <summary>Gets the seed used by the hashing algorithm.</summary>
        /// <value>An <see cref="T:System.UInt64"/> value.</value>
        [ExcludeFromCodeCoverage]
        public UInt64 Seed => m_Seed;

        /// <summary>Gets whether the implementation computes a 128-bit fingerprint.</summary>
        protected abstract Boolean Fingerprint { get; }
        #endregion

        #region Constructors
        /// <summary>Initializes the UMASH engine with the specified seed.</summary>
        /// <param name="seed">The <see cref="T:System.UInt64"/> seed used by the hashing algorithm.</param>
        [ExcludeFromCodeCoverage]
        protected Umash(UInt64 seed)
        {
            m_Seed = seed;
        }
        #endregion

        #region Methods
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static UInt64 AddModFast(UInt64 x, UInt64 y)
        {
            UInt64 sum = unchecked(x + y);
            return sum < x ? unchecked(sum + 8ul) : sum;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static UInt64 AddModSlow(UInt64 x, UInt64 y)
        {
            UInt64 sum = unchecked(x + y);
            UInt64 fixup = sum < x ? 8ul : 0ul;

            if (sum < UInt64.MaxValue - 15ul)
                return unchecked(sum + fixup);

            if (sum >= UInt64.MaxValue - 7ul)
                sum = unchecked(sum + 8ul);

            sum = unchecked(sum + fixup);
            if (sum >= UInt64.MaxValue - 7ul)
                sum = unchecked(sum + 8ul);

            return sum;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void Mul128(UInt64 x, UInt64 y, out UInt64 lo, out UInt64 hi)
        {
#if NET5_0_OR_GREATER
            hi = Math.BigMul(x, y, out lo);
#else
            UInt64 x0 = (UInt32)x;
            UInt64 x1 = x >> 32;
            UInt64 y0 = (UInt32)y;
            UInt64 y1 = y >> 32;
            UInt64 p0 = x0 * y0;
            UInt64 p1 = x0 * y1;
            UInt64 p2 = x1 * y0;
            UInt64 p3 = x1 * y1;
            UInt64 mid = unchecked((p0 >> 32) + (UInt32)p1 + (UInt32)p2);

            lo = (mid << 32) | (UInt32)p0;
            hi = unchecked(p3 + (p1 >> 32) + (p2 >> 32) + (mid >> 32));
#endif
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static UInt64 MulModFast(UInt64 x, UInt64 y)
        {
            Mul128(x, y, out UInt64 lo, out UInt64 hi);
            return AddModFast(lo, unchecked(8ul * hi));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static UInt64 Horner(UInt64 acc, UInt64 m0, UInt64 m1, UInt64 x, UInt64 y)
        {
            acc = AddModFast(acc, x);
            return AddModSlow(MulModFast(m0, acc), MulModFast(m1, y));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static UInt64 Finalize(UInt64 value)
        {
            return (value ^ BinaryOperations.RotateLeft(value, 8)) ^ BinaryOperations.RotateLeft(value, 33);
        }

        // Portable GF(2) carry-less 64 x 64 -> 128 multiplication.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void ClMul(UInt64 x, UInt64 y, out UInt64 lo, out UInt64 hi)
        {
            lo = 0ul;
            hi = 0ul;

            for (Int32 i = 0; i < 64; ++i)
            {
                if (((y >> i) & 1ul) == 0ul)
                    continue;

                lo ^= x << i;
                if (i != 0)
                    hi ^= x >> (64 - i);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static UInt64 ReadShort(ReadOnlySpan<Byte> buffer)
        {
            Int32 count = buffer.Length;
            UInt32 lo;
            UInt32 hi;

            if (count >= 4)
            {
                lo = BinaryOperations.Read32(buffer, 0);
                hi = BinaryOperations.Read32(buffer, count - 4);
            }
            else
            {
                lo = (count & 1) != 0 ? buffer[0] : 0u;
                hi = (count & 2) != 0 ? (UInt32)(buffer[count - 2] | (buffer[count - 1] << 8)) : 0u;
            }

            return ((UInt64)hi << 32) | unchecked((UInt32)(lo + hi));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static UInt64 HashShort(ReadOnlySpan<Byte> buffer, UInt64 seed, Int32 shift)
        {
            UInt64 h = ReadShort(buffer);
            seed = unchecked(seed + s_Oh[buffer.Length + shift]);
            h ^= h >> 30;
            h = unchecked(h * MIX1);
            h = (h ^ seed) ^ (h >> 27);
            h = unchecked(h * MIX2);
            return h ^ (h >> 31);
        }

        private static void HashShortFingerprint(ReadOnlySpan<Byte> buffer, UInt64 seed, out UInt64 h0, out UInt64 h1)
        {
            h0 = unchecked(seed + s_Oh[buffer.Length]);
            h1 = unchecked(seed + s_Oh[buffer.Length + 4]);

            UInt64 h = ReadShort(buffer);
            h ^= h >> 30;
            h = unchecked(h * MIX1);
            h ^= h >> 27;

            h0 = unchecked((h0 ^ h) * MIX2);
            h1 = unchecked((h1 ^ h) * MIX2);
            h0 ^= h0 >> 31;
            h1 ^= h1 >> 31;
        }

        private static UInt64 HashMedium(ReadOnlySpan<Byte> buffer, UInt64 seed)
        {
            UInt64 x = unchecked(BinaryOperations.Read64(buffer, 0) + s_Oh[0]);
            UInt64 y = unchecked(BinaryOperations.Read64(buffer, buffer.Length - 8) + s_Oh[1]);

            Mul128(x, y, out UInt64 lo, out UInt64 hi);
            hi = unchecked(hi + (seed ^ (UInt64)buffer.Length));
            hi ^= lo;

            return Finalize(Horner(0ul, s_Poly[0], s_Poly[1], lo, hi));
        }

        private static void HashMediumFingerprint(ReadOnlySpan<Byte> buffer, UInt64 seed, out UInt64 h0, out UInt64 h1)
        {
            UInt64 x = BinaryOperations.Read64(buffer, 0);
            UInt64 y = BinaryOperations.Read64(buffer, buffer.Length - 8);

            ClMul(s_Oh[32] ^ x ^ s_Oh[0], s_Oh[33] ^ y ^ s_Oh[1], out UInt64 cl0, out UInt64 cl1);
            Mul128(unchecked(x + s_Oh[0]), unchecked(y + s_Oh[1]), out UInt64 lo, out UInt64 hi);
            hi = unchecked(hi + (seed ^ (UInt64)buffer.Length));
            hi ^= lo;

            h0 = Finalize(Horner(0ul, s_Poly[0], s_Poly[1], lo, hi));
            h1 = Finalize(Horner(0ul, s_Poly[2], s_Poly[3], lo ^ cl0, hi ^ cl1));
        }

        private static void Compress(ReadOnlySpan<Byte> buffer, Int32 offset, Int32 count, UInt64 tag, Boolean fingerprint,
            out UInt64 oh0, out UInt64 oh1, out UInt64 twisted0, out UInt64 twisted1)
        {
            Int32 remaining = 1 + ((count - 1) % 16);
            Int32 endPairs = (count - remaining) / 8;
            Int32 lastOffset = offset + count - 16;

            oh0 = 0ul;
            oh1 = 0ul;
            twisted0 = 0ul;
            twisted1 = 0ul;

            UInt64 lrc0 = s_Oh[32];
            UInt64 lrc1 = s_Oh[33];

            for (Int32 i = 0; i < endPairs; i += 2)
            {
                UInt64 x = BinaryOperations.Read64(buffer, offset + i * 8) ^ s_Oh[i];
                UInt64 y = BinaryOperations.Read64(buffer, offset + i * 8 + 8) ^ s_Oh[i + 1];

                if (fingerprint)
                {
                    lrc0 ^= x;
                    lrc1 ^= y;
                }

                ClMul(x, y, out UInt64 lo, out UInt64 hi);
                oh0 ^= lo;
                oh1 ^= hi;

                if (fingerprint && i + 2 < endPairs)
                {
                    twisted0 = unchecked((twisted0 ^ lo) << 1);
                    twisted1 = unchecked((twisted1 ^ hi) << 1);
                }
            }

            UInt64 lastX = BinaryOperations.Read64(buffer, lastOffset);
            UInt64 lastY = BinaryOperations.Read64(buffer, lastOffset + 8);

            if (fingerprint)
            {
                lrc0 ^= lastX ^ s_Oh[endPairs];
                lrc1 ^= lastY ^ s_Oh[endPairs + 1];
                twisted0 = unchecked((twisted0 ^ oh0) << 1);
                twisted1 = unchecked((twisted1 ^ oh1) << 1);
                ClMul(lrc0, lrc1, out UInt64 cl0, out UInt64 cl1);
                twisted0 ^= cl0;
                twisted1 ^= cl1;
            }

            Mul128(unchecked(lastX + s_Oh[endPairs]), unchecked(lastY + s_Oh[endPairs + 1]), out UInt64 enhLo, out UInt64 enhHi);
            enhHi = unchecked(enhHi + tag);
            enhHi ^= enhLo;

            oh0 ^= enhLo;
            oh1 ^= enhHi;
            twisted0 ^= enhLo;
            twisted1 ^= enhHi;
        }

        private static void HashLong(ReadOnlySpan<Byte> buffer, UInt64 seed, Boolean fingerprint, out UInt64 h0, out UInt64 h1)
        {
            UInt64 acc0 = 0ul;
            UInt64 acc1 = 0ul;
            Int32 offset = 0;
            Int32 remaining = buffer.Length;

            while (remaining > BLOCK_SIZE)
            {
                Compress(buffer, offset, BLOCK_SIZE, seed, fingerprint,
                    out UInt64 oh0, out UInt64 oh1, out UInt64 twisted0, out UInt64 twisted1);

                acc0 = Horner(acc0, s_Poly[0], s_Poly[1], oh0, oh1);
                if (fingerprint)
                    acc1 = Horner(acc1, s_Poly[2], s_Poly[3], twisted0, twisted1);

                offset += BLOCK_SIZE;
                remaining -= BLOCK_SIZE;
            }

            Compress(buffer, offset, remaining, seed ^ (Byte)remaining, fingerprint,
                out UInt64 last0, out UInt64 last1, out UInt64 lastTwisted0, out UInt64 lastTwisted1);

            h0 = Finalize(Horner(acc0, s_Poly[0], s_Poly[1], last0, last1));
            h1 = fingerprint ? Finalize(Horner(acc1, s_Poly[2], s_Poly[3], lastTwisted0, lastTwisted1)) : 0ul;
        }

        /// <inheritdoc/>
        protected override Byte[] ComputeHashInternal(ReadOnlySpan<Byte> buffer)
        {
            UInt64 h0;
            UInt64 h1 = 0ul;

            if (buffer.Length <= 8)
            {
                if (Fingerprint)
                    HashShortFingerprint(buffer, m_Seed, out h0, out h1);
                else
                    h0 = HashShort(buffer, m_Seed, 0);
            }
            else if (buffer.Length <= 16)
            {
                if (Fingerprint)
                    HashMediumFingerprint(buffer, m_Seed, out h0, out h1);
                else
                    h0 = HashMedium(buffer, m_Seed);
            }
            else
            {
                HashLong(buffer, m_Seed, Fingerprint, out h0, out h1);
            }

            return Fingerprint ? BinaryOperations.ToArray64(h0, h1) : BinaryOperations.ToArray64(h0);
        }
        #endregion
    }

    /// <summary>Represents the Umash64 implementation. This class cannot be derived.</summary>
    public sealed class Umash64 : Umash
    {
        #region Properties
        /// <inheritdoc/>
        [ExcludeFromCodeCoverage]
        public override Int32 Length => 64;

        /// <inheritdoc/>
        protected override Boolean Fingerprint => false;
        #endregion

        #region Constructors
        /// <summary>Initializes a new instance using a seed value equal to <c>0</c>.</summary>
        [ExcludeFromCodeCoverage]
        public Umash64() : this(0ul) { }

        /// <summary>Initializes a new instance using the specified seed.</summary>
        /// <param name="seed">The <see cref="T:System.UInt64"/> seed used by the hashing algorithm.</param>
        [ExcludeFromCodeCoverage]
        public Umash64(UInt64 seed) : base(seed) { }
        #endregion
    }

    /// <summary>Represents the Umash128 implementation. This class cannot be derived.</summary>
    public sealed class Umash128 : Umash
    {
        #region Properties
        /// <inheritdoc/>
        [ExcludeFromCodeCoverage]
        public override Int32 Length => 128;

        /// <inheritdoc/>
        protected override Boolean Fingerprint => true;
        #endregion

        #region Constructors
        /// <summary>Initializes a new instance using a seed value equal to <c>0</c>.</summary>
        [ExcludeFromCodeCoverage]
        public Umash128() : this(0ul) { }

        /// <summary>Initializes a new instance using the specified seed.</summary>
        /// <param name="seed">The <see cref="T:System.UInt64"/> seed used by the hashing algorithm.</param>
        [ExcludeFromCodeCoverage]
        public Umash128(UInt64 seed) : base(seed) { }
        #endregion
    }
}
