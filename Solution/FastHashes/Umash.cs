#region Using Directives
using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
#endregion

namespace FastHashes
{
    /// <summary>Represents the base class from which all the Umash implementations must derive. This class is abstract.</summary>
    public abstract class Umash<T> : Hash<T> where T : unmanaged
    {
        #region Constants
        /// <summary>Represents an auxiliary hashing function used by derived classes.</summary>
        protected static readonly UInt64[] OH =
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

        /// <summary>Represents an auxiliary hashing function used by derived classes.</summary>
        protected static readonly UInt64[] POLY =
        {
            0x1750289755934E3Aul, 0x0D1B5522F4059E62ul,
            0x08D5C6EDBB37B832ul, 0x0DAAB0FD57364132ul
        };
        #endregion

        #region Members
        /// <summary>Represents the seeds used by the hashing algorithm. This field is read-only.</summary>
        protected readonly UInt64 m_Seed;
        #endregion

        #region Properties
        /// <summary>Gets the seed used by the hashing algorithm.</summary>
        /// <value>An <see cref="T:System.UInt64"/> value.</value>
        [ExcludeFromCodeCoverage]
        public UInt64 Seed => m_Seed;
        #endregion

        #region Constructors
        /// <summary>Represents the compact base constructor used by derived classes.</summary>
        /// <param name="seed">The <see cref="T:System.UInt64"/> seed used by the hashing algorithm.</param>
        [ExcludeFromCodeCoverage]
        protected Umash(UInt64 seed)
        {
            m_Seed = seed;
        }
        #endregion

        #region Methods
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static UInt64 AddMod(UInt64 x, UInt64 y)
        {
            UInt64 sum = unchecked(x + y);
            sum = sum < x ? unchecked(sum + 8ul) : sum;

            return sum;
        }

        /// <summary>Represents an auxiliary hashing function used by derived classes.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected static UInt64 Finalize(UInt64 value)
        {
            UInt64 a = value ^ BinaryOperations.RotateLeft(value, 8);
            UInt64 b = BinaryOperations.RotateLeft(value, 33);

            return a ^ b;
        }

        /// <summary>Represents an auxiliary hashing function used by derived classes.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected static void HashMedium(ReadOnlySpan<Byte> buffer, UInt64 seed, out UInt64 lo, out UInt64 hi)
        {
            Int32 count = buffer.Length;

            UInt64 x = unchecked(BinaryOperations.Read64(buffer, 0) + OH[0]);
            UInt64 y = unchecked(BinaryOperations.Read64(buffer, count - 8) + OH[1]);

            BinaryOperations.Multiply128(x, y, out lo, out hi);
            hi = unchecked(hi + (seed ^ (UInt64)count));
            hi ^= lo;
        }

        /// <summary>Represents an auxiliary hashing function used by derived classes.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected static UInt64 HashShort(ReadOnlySpan<Byte> buffer, UInt64 seed, Int32 shift)
        {
            Int32 count = buffer.Length;

            seed = unchecked(seed + OH[count + shift]);

            UInt32 lo, hi;

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

            UInt64 hash = ((UInt64)hi << 32) | unchecked((UInt32)(lo + hi));
            hash ^= hash >> 30;
            hash = unchecked(hash * 0xBF58476D1CE4E5B9ul);
            hash = (hash ^ seed) ^ (hash >> 27);
            hash = unchecked(hash * 0x94D049BB133111EBul);
            hash ^= hash >> 31;

            return hash;
        }

        /// <summary>Represents an auxiliary hashing function used by derived classes.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected static UInt64 Horner(UInt64 accumulator, UInt64 m0, UInt64 m1, UInt64 x, UInt64 y)
        {
            UInt64 lo, hi;

            BinaryOperations.Multiply128(m0, AddMod(accumulator, x), out lo, out hi);
            UInt64 v1 = AddMod(lo, unchecked(8ul * hi));

            BinaryOperations.Multiply128(m1, y, out lo, out hi);
            UInt64 v2 = AddMod(lo, unchecked(8ul * hi));

            UInt64 sum = unchecked(v1 + v2);
            UInt64 fixup = sum < v1 ? 8ul : 0ul;

            if (sum < (UInt64.MaxValue - 15ul))
                return unchecked(sum + fixup);

            if (sum >= (UInt64.MaxValue - 7ul))
                sum = unchecked(sum + 8ul);

            sum = unchecked(sum + fixup);

            if (sum >= (UInt64.MaxValue - 7ul))
                sum = unchecked(sum + 8ul);

            return sum;
        }

        /// <summary>Represents an auxiliary hashing function used by derived classes.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected static void ClMul(UInt64 x, UInt64 y, out UInt64 lo, out UInt64 hi)
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
        #endregion
    }

    /// <summary>Represents the Umash64 implementation. This class cannot be derived.</summary>
    public sealed class Umash64 : Umash<UInt64>
    {

        #region Constructors
        /// <summary>Initializes a new instance using the specified seed.</summary>
        /// <param name="seed">The <see cref="T:System.UInt64"/> seed used by the hashing algorithm.</param>
        [ExcludeFromCodeCoverage]
        public Umash64(UInt64 seed) : base(seed) { }

        /// <summary>Initializes a new instance using a seed value of <c>0</c>.</summary>
        [ExcludeFromCodeCoverage]
        public Umash64() : this(0ul) { }
        #endregion

        #region Methods
        private static UInt64 HashLong(ReadOnlySpan<Byte> buffer, UInt64 seed)
        {
            UInt64 p0 = POLY[0];
            UInt64 p1 = POLY[1];

            Int32 offset = 0;
            Int32 remaining = buffer.Length;

            UInt64 acc = 0ul;
            UInt64 oh0, oh1;

            while (remaining > 256)
            {
                Compress(buffer, offset, 256, seed, out oh0, out oh1);
                acc = Horner(acc, p0, p1, oh0, oh1);

                offset += 256;
                remaining -= 256;
            }

            Compress(buffer, offset, remaining, seed ^ unchecked((Byte)remaining), out oh0, out oh1);

            UInt64 hash = Finalize(Horner(acc, p0, p1, oh0, oh1));

            return hash;
        }

        private static void Compress(ReadOnlySpan<Byte> buffer, Int32 offset, Int32 count, UInt64 tag, out UInt64 oh0, out UInt64 oh1)
        {
            Int32 remaining = 1 + ((count - 1) % 16);
            Int32 endPairs = (count - remaining) / 8;
            Int32 lastOffset = offset + count - 16;

            UInt64 lo, hi;

            oh0 = 0ul;
            oh1 = 0ul;

            for (Int32 i = 0; i < endPairs; i += 2)
            {
                UInt64 x = BinaryOperations.Read64(buffer, offset + i * 8) ^ OH[i];
                UInt64 y = BinaryOperations.Read64(buffer, offset + i * 8 + 8) ^ OH[i + 1];

                ClMul(x, y, out lo, out hi);
                oh0 ^= lo;
                oh1 ^= hi;
            }

            UInt64 lastX = BinaryOperations.Read64(buffer, lastOffset);
            UInt64 lastY = BinaryOperations.Read64(buffer, lastOffset + 8);

            BinaryOperations.Multiply128(unchecked(lastX + OH[endPairs]), unchecked(lastY + OH[endPairs + 1]), out lo, out hi);
            hi = unchecked(hi + tag);
            hi ^= lo;

            oh0 ^= lo;
            oh1 ^= hi;
        }

        /// <inheritdoc/>
        protected override UInt64 ComputeHashValueInternal(ReadOnlySpan<Byte> buffer)
        {
            Int32 count = buffer.Length;

            UInt64 hash;

            if (count <= 8)
                hash = HashShort(buffer, m_Seed, 0);
            else if (count <= 16)
            {
                HashMedium(buffer, m_Seed, out UInt64 lo, out UInt64 hi);
                hash = Finalize(Horner(0ul, POLY[0], POLY[1], lo, hi));
            }
            else
                hash = HashLong(buffer, m_Seed);

            return hash;
        }

        #endregion
    }

    /// <summary>Represents the Umash128 implementation. This class cannot be derived.</summary>
    public sealed class Umash128 : Umash<(UInt64, UInt64)>
    {

        #region Constructors
        /// <summary>Initializes a new instance using the specified seed.</summary>
        /// <param name="seed">The <see cref="T:System.UInt64"/> seed used by the hashing algorithm.</param>
        [ExcludeFromCodeCoverage]
        public Umash128(UInt64 seed) : base(seed) { }

        /// <summary>Initializes a new instance using a seed value of <c>0</c>.</summary>
        [ExcludeFromCodeCoverage]
        public Umash128() : this(0ul) { }
        #endregion

        #region Methods
        private static void HashLong(ReadOnlySpan<Byte> buffer, UInt64 seed, out UInt64 hash1, out UInt64 hash2)
        {
            UInt64 p0 = POLY[0];
            UInt64 p1 = POLY[1];
            UInt64 p2 = POLY[2];
            UInt64 p3 = POLY[3];

            Int32 offset = 0;
            Int32 remaining = buffer.Length;

            UInt64 acc0 = 0ul;
            UInt64 acc1 = 0ul;
            UInt64 oh0, oh1;
            UInt64 tw0, tw1;

            while (remaining > 256)
            {
                Compress(buffer, offset, 256, seed, out oh0, out oh1, out tw0, out tw1);
                acc0 = Horner(acc0, p0, p1, oh0, oh1);
                acc1 = Horner(acc1, p2, p3, tw0, tw1);

                offset += 256;
                remaining -= 256;
            }

            Compress(buffer, offset, remaining, seed ^ unchecked((Byte)remaining), out oh0, out oh1, out tw0, out tw1);

            hash1 = Finalize(Horner(acc0, p0, p1, oh0, oh1));
            hash2 = Finalize(Horner(acc1, p2, p3, tw0, tw1));
        }

        private static void Compress(ReadOnlySpan<Byte> buffer, Int32 offset, Int32 count, UInt64 tag, out UInt64 oh0, out UInt64 oh1, out UInt64 tw0, out UInt64 tw1)
        {
            Int32 remaining = 1 + ((count - 1) % 16);
            Int32 endPairs = (count - remaining) / 8;
            Int32 lastOffset = offset + count - 16;

            UInt64 lrc0 = OH[32];
            UInt64 lrc1 = OH[33];

            UInt64 lo, hi;

            oh0 = 0ul;
            oh1 = 0ul;
            tw0 = 0ul;
            tw1 = 0ul;

            for (Int32 i = 0; i < endPairs; i += 2)
            {
                UInt64 x = BinaryOperations.Read64(buffer, offset + (i * 8)) ^ OH[i];
                UInt64 y = BinaryOperations.Read64(buffer, offset + (i * 8) + 8) ^ OH[i + 1];

                lrc0 ^= x;
                lrc1 ^= y;

                ClMul(x, y, out lo, out hi);
                oh0 ^= lo;
                oh1 ^= hi;

                if (i + 2 < endPairs)
                {
                    tw0 = unchecked((tw0 ^ lo) << 1);
                    tw1 = unchecked((tw1 ^ hi) << 1);
                }
            }

            UInt64 lastX = BinaryOperations.Read64(buffer, lastOffset);
            UInt64 lastY = BinaryOperations.Read64(buffer, lastOffset + 8);

            lrc0 ^= lastX ^ OH[endPairs];
            lrc1 ^= lastY ^ OH[endPairs + 1];

            tw0 = unchecked((tw0 ^ oh0) << 1);
            tw1 = unchecked((tw1 ^ oh1) << 1);

            ClMul(lrc0, lrc1, out UInt64 cl0, out UInt64 cl1);
            tw0 ^= cl0;
            tw1 ^= cl1;

            BinaryOperations.Multiply128(unchecked(lastX + OH[endPairs]), unchecked(lastY + OH[endPairs + 1]), out lo, out hi);
            hi = unchecked(hi + tag);
            hi ^= lo;

            oh0 ^= lo;
            oh1 ^= hi;
            tw0 ^= lo;
            tw1 ^= hi;
        }

        /// <inheritdoc/>
        protected override (UInt64, UInt64) ComputeHashValueInternal(ReadOnlySpan<Byte> buffer)
        {
            Int32 count = buffer.Length;
            UInt64 hash1, hash2;

            if (count <= 8)
            {
                hash1 = HashShort(buffer, m_Seed, 0);
                hash2 = HashShort(buffer, m_Seed, 4);
            }
            else if (count <= 16)
            {
                UInt64 x = BinaryOperations.Read64(buffer, 0);
                UInt64 y = BinaryOperations.Read64(buffer, count - 8);
                ClMul(OH[32] ^ x ^ OH[0], OH[33] ^ y ^ OH[1], out UInt64 cl0, out UInt64 cl1);

                HashMedium(buffer, m_Seed, out UInt64 lo, out UInt64 hi);
                hash1 = Finalize(Horner(0ul, POLY[0], POLY[1], lo, hi));
                hash2 = Finalize(Horner(0ul, POLY[2], POLY[3], lo ^ cl0, hi ^ cl1));
            }
            else
                HashLong(buffer, m_Seed, out hash1, out hash2);

            return (hash1, hash2);
        }

        #endregion
    }
}