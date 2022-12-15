#region Using Directives
using System;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
#endregion

namespace FastHashes
{
    /// <summary>Represents the base class from which all the FarmHash implementations with more than 32 bits of output must derive. This class is abstract.</summary>
    public abstract class FarmHashOver32 : Hash
    {
        #region Constants
        /// <summary>Represents the K0 value. This field is constant.</summary>
        protected const UInt64 K0 = 0xC3A5C85C97CB3127ul;
        /// <summary>Represents the K1 value. This field is constant.</summary>
        protected const UInt64 K1 = 0xB492B66FBE98F273ul;
        /// <summary>Represents the K2 value. This field is constant.</summary>
        protected const UInt64 K2 = 0x9AE16A3B2F90404Ful;
        /// <summary>Represents the K3 value. This field is constant.</summary>
        protected const UInt64 K3 = 0x9DDFEA08EB382D69ul;
        #endregion

        #region Members
        /// <summary>Represents the seeds used by the hashing algorithm. This field is read-only.</summary>
        protected readonly ReadOnlyCollection<UInt64> m_Seeds;
        #endregion

        #region Properties
        /// <summary>Gets the seeds used by the hashing algorithm.</summary>
        /// <value>A <see cref="System.Collections.ObjectModel.ReadOnlyCollection{T}"/> containing <c>0</c> or <c>2</c> <see cref="T:System.UInt64"/> values.</value>
        [ExcludeFromCodeCoverage]
        public ReadOnlyCollection<UInt64> Seeds => m_Seeds;
        #endregion

        #region Constructors
        /// <summary>Represents the base constructor without seeds used by derived classes.</summary>
        [ExcludeFromCodeCoverage]
        protected FarmHashOver32()
        {
            m_Seeds = new ReadOnlyCollection<UInt64>(new UInt64[0]);
        }

        /// <summary>Represents the base constructor with two seeds used by derived classes.</summary>
        /// <param name="seed1">The first <see cref="T:System.UInt64"/> seed used by the hashing algorithm.</param>
        /// <param name="seed2">The second <see cref="T:System.UInt64"/> seed used by the hashing algorithm.</param>
        [ExcludeFromCodeCoverage]
        protected FarmHashOver32(UInt64 seed1, UInt64 seed2)
        {
            m_Seeds = new ReadOnlyCollection<UInt64>(new[] { seed1, seed2 });
        }
        #endregion

        #region Methods
        /// <summary>Represents an auxiliary hashing function used by derived classes.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected static UInt64 HashLength16(UInt64 v1, UInt64 v2, UInt64 m)
        {
            UInt64 a = ShiftMix((v1 ^ v2) * m);
            return ShiftMix((v2 ^ a) * m) * m;
        }

        /// <summary>Represents an auxiliary hashing function used by derived classes.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected static UInt64 ShiftMix(UInt64 v)
        {
            return (v ^ (v >> 47));
        }

        /// <summary>Represents an auxiliary hashing function used by derived classes.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected static void HashWeak32(out UInt64 v1, out UInt64 v2, ReadOnlySpan<Byte> buffer, Int32 offset, UInt64 v3, UInt64 v4)
        {
            UInt64 w = BinaryOperations.Read64(buffer, offset);
            UInt64 x = BinaryOperations.Read64(buffer, offset + 8);
            UInt64 y = BinaryOperations.Read64(buffer, offset + 16);
            UInt64 z = BinaryOperations.Read64(buffer, offset + 24);

            v3 += w;
            v4 = BinaryOperations.RotateRight(v4 + v3 + z, 21);

            UInt64 c = v3;

            v3 += x;
            v3 += y;
            v4 += BinaryOperations.RotateRight(v3, 44);

            v1 = v3 + z;
            v2 = v4 + c;
        }

        /// <summary>Represents an auxiliary hashing function used by derived classes.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected static void Update(ref UInt64 x, ref UInt64 y, ref UInt64 z, ReadOnlySpan<Byte> buffer, Int32 offset, UInt64 v0, UInt64 v1, UInt64 w0, UInt64 w1, UInt64 m, UInt64 c)
        {
            x = BinaryOperations.RotateRight(x + y + v0 + BinaryOperations.Read64(buffer, offset + 8), 37) * m;
            y = BinaryOperations.RotateRight(y + v1 + BinaryOperations.Read64(buffer, offset + 48), 42) * m;
            x ^= w1 * c;
            y += (v0 * c) + BinaryOperations.Read64(buffer, offset + 40);
            z = BinaryOperations.RotateRight(z + w0, 33) * m;
        }
        #endregion
    }

    /// <summary>Represents the FarmHash32 implementation. This class cannot be derived.</summary>
    public sealed class FarmHash32 : Hash
    {
        #region Constants
        private const UInt32 C1 = 0xCC9E2D51u;
        private const UInt32 C2 = 0x1B873593u;
        private const UInt32 F1 = 0x85EBCA6Bu;
        private const UInt32 F2 = 0xC2B2AE35u;
        private const UInt32 N = 0xE6546B64u;
        #endregion

        #region Members
        private readonly UInt32? m_Seed;
        #endregion

        #region Properties
        /// <inheritdoc/>
        [ExcludeFromCodeCoverage]
        public override Int32 Length => 32;

        /// <summary>Gets the seed used by the hashing algorithm.</summary>
        /// <value>An <see cref="T:System.UInt32"/>? value.</value>
        [ExcludeFromCodeCoverage]
        public UInt32? Seed => m_Seed;
        #endregion

        #region Constructors
        /// <summary>Initializes a new instance without seed.</summary>
        [ExcludeFromCodeCoverage]
        public FarmHash32()
        {
            m_Seed = null;
        }

        /// <summary>Initializes a new instance using the specified seed.</summary>
        /// <param name="seed">The <see cref="T:System.UInt32"/> seed used by the hashing algorithm.</param>
        [ExcludeFromCodeCoverage]
        public FarmHash32(UInt32 seed)
        {
            m_Seed = seed;
        }
        #endregion

        #region Methods
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static UInt32 Fin(UInt32 hash)
        {
            hash ^= hash >> 16;
            hash *= F1;
            hash ^= hash >> 13;
            hash *= F2;
            hash ^= hash >> 16;

            return hash;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static UInt32 Hash0To4(ReadOnlySpan<Byte> buffer, Int32 offset, Int32 count, UInt32 seed)
        {
            UInt32 a = seed;
            UInt32 b = 9u;

            for (Int32 i = 0; i < count; ++i)
            {
                a = (a * C1) + buffer[offset + i];
                b ^= a;
            }

            UInt32 hash = Mur(b, (UInt32)count);
            hash = Mur(hash, a);
            hash = Fin(hash);

            return hash;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static UInt32 Hash5To12(ReadOnlySpan<Byte> buffer, Int32 offset, Int32 count, UInt32 seed)
        {
            UInt32 a = (UInt32)count;
            UInt32 b = a * 5u;
            UInt32 c = 9u;
            UInt32 d = b + seed;

            a += BinaryOperations.Read32(buffer, offset);
            b += BinaryOperations.Read32(buffer, offset + count - 4);
            c += BinaryOperations.Read32(buffer, offset + ((count >> 1) & 4));

            UInt32 hash = Mur(d, a);
            hash = Mur(hash, b);
            hash = Mur(hash, c);
            hash = Fin(seed ^ hash);

            return hash;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static UInt32 Hash13To24(ReadOnlySpan<Byte> buffer, Int32 offset, Int32 count, UInt32 seed)
        {
            UInt32 a = BinaryOperations.Read32(buffer, offset - 4 + (count >> 1));
            UInt32 b = BinaryOperations.Read32(buffer, offset + 4);
            UInt32 c = BinaryOperations.Read32(buffer, offset + count - 8);
            UInt32 d = BinaryOperations.Read32(buffer, offset + (count >> 1));
            UInt32 e = BinaryOperations.Read32(buffer, offset);
            UInt32 f = BinaryOperations.Read32(buffer, offset + count - 4);

            UInt32 hash = (d * C1) + (UInt32)count + seed;
            a = BinaryOperations.RotateRight(a, 12) + f;
            hash = Mur(hash, c) + a;
            a = BinaryOperations.RotateRight(a, 3) + c;
            hash = Mur(hash, e) + a;
            a = BinaryOperations.RotateRight(a + f, 12) + d;
            hash = Mur(hash, b ^ seed) + a;
            hash = Fin(hash);

            return hash;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static UInt32 Hash24ToEnd(ReadOnlySpan<Byte> buffer, Int32 offset, Int32 count)
        {
            UInt32 hash = (UInt32)count;
            UInt32 g = C1 * hash;
            UInt32 f = g;

            hash = Mur(hash, BinaryOperations.Read32(buffer, offset + count - 4));
            g = Mur(g, BinaryOperations.Read32(buffer, offset + count - 8));
            hash = Mur(hash, BinaryOperations.Read32(buffer, offset + count - 16));
            g = Mur(g, BinaryOperations.Read32(buffer, offset + count - 12));

            f += BinaryOperations.RotateRight(BinaryOperations.Read32(buffer, offset + count - 20) * C1, 17) * C2;
            f = BinaryOperations.RotateRight(f, 19) + 113u;

            Int32 blocks = (count - 1) / 20;

            do
            {
                UInt32 a = BinaryOperations.Read32(buffer, offset);
                offset += 4;
                UInt32 b = BinaryOperations.Read32(buffer, offset);
                offset += 4;
                UInt32 c = BinaryOperations.Read32(buffer, offset);
                offset += 4;
                UInt32 d = BinaryOperations.Read32(buffer, offset);
                offset += 4;
                UInt32 e = BinaryOperations.Read32(buffer, offset);
                offset += 4;

                hash += a;
                g += b;
                f += c;

                hash = Mur(hash, d) + e;
                g = Mur(g, c) + a;
                f = Mur(f, b + e * C1) + d;

                f += g;
                g += f;
            }
            while (--blocks > 0);

            g = BinaryOperations.RotateRight(g, 11) * C1;
            g = BinaryOperations.RotateRight(g, 17) * C1;
            f = BinaryOperations.RotateRight(f, 11) * C1;
            f = BinaryOperations.RotateRight(f, 17) * C1;

            hash = BinaryOperations.RotateRight(hash + g, 19);
            hash = (hash * 5u) + N;
            hash = BinaryOperations.RotateRight(hash, 17) * C1;
            hash = BinaryOperations.RotateRight(hash + f, 19);
            hash = (hash * 5u) + N;
            hash = BinaryOperations.RotateRight(hash, 17) * C1;

            return hash;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static UInt32 Mur(UInt32 v1, UInt32 v2)
        {
            v2 *= C1;
            v2 = BinaryOperations.RotateRight(v2, 17);
            v2 *= C2;

            v1 ^= v2;
            v1 = BinaryOperations.RotateRight(v1, 19);
            v1 = (v1 * 5u) + N;

            return v1;
        }

        /// <inheritdoc/>
        protected override Byte[] ComputeHashInternal(ReadOnlySpan<Byte> buffer)
        {
            Int32 offset = 0;
            Int32 count = buffer.Length;

            UInt32 hash;

            if (m_Seed.HasValue)
            {
                UInt32 seed = m_Seed.Value;

                if (count <= 4)
                    hash = Hash0To4(buffer, offset, count, seed);
                else if (count <= 12)
                    hash = Hash5To12(buffer, offset, count, seed);
                else if (count <= 24)
                    hash = Hash13To24(buffer, offset, count, seed * C1);
                else
                {
                    Int32 countDiff = count - 24;
                    UInt32 v1, v2;

                    v1 = Hash13To24(buffer, offset, 24, seed ^ (UInt32)count);
                    v2 = seed;

                    if (countDiff <= 4)
                        v2 += Hash0To4(buffer, offset + 24, countDiff, 0u);
                    else if (countDiff <= 12)
                        v2 += Hash5To12(buffer, offset + 24, countDiff, 0u);
                    else if (countDiff <= 24)
                        v2 += Hash13To24(buffer, offset + 24, countDiff, 0u);
                    else
                        v2 += Hash24ToEnd(buffer, offset + 24, countDiff);

                    hash = Mur(v1, v2);
                }
            }
            else
            {
                if (count <= 4)
                    hash = Hash0To4(buffer, offset, count, 0u);
                else if (count <= 12)
                    hash = Hash5To12(buffer, offset, count, 0u);
                else if (count <= 24)
                    hash = Hash13To24(buffer, offset, count, 0u);
                else
                    hash = Hash24ToEnd(buffer, offset, count);
            }

            Byte[] result = BinaryOperations.ToArray32(hash);

            return result;
        }
        #endregion
    }

    /// <summary>Represents the FarmHash64 implementation. This class cannot be derived.</summary>
    public sealed class FarmHash64 : FarmHashOver32
    {
        #region Properties
        /// <inheritdoc/>
        [ExcludeFromCodeCoverage]
        public override Int32 Length => 64;
        #endregion

        #region Constructors
        /// <summary>Initializes a new instance without seeds.</summary>
        [ExcludeFromCodeCoverage]
        public FarmHash64() { }

        /// <summary>Initializes a new instance using <see cref="F:FastHashes.FarmHashG32.K2"/> as first seed and the specified value as second seed.</summary>
        /// <param name="seed">The <see cref="T:System.UInt64"/> seed used by the hashing algorithm.</param>
        [ExcludeFromCodeCoverage]
        public FarmHash64(UInt64 seed) : base(K2, seed) { }

        /// <summary>Initializes a new instance using the specified seeds.</summary>
        /// <param name="seed1">The first <see cref="T:System.UInt64"/> seed used by the hashing algorithm.</param>
        /// <param name="seed2">The second <see cref="T:System.UInt64"/> seed used by the hashing algorithm.</param>
        [ExcludeFromCodeCoverage]
        public FarmHash64(UInt64 seed1, UInt64 seed2) : base(seed1, seed2) { }
        #endregion

        #region Methods
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static UInt64 Hash1To3(ReadOnlySpan<Byte> buffer, Int32 offset, Int32 count)
        {
            UInt32 a = buffer[offset] + ((UInt32)buffer[offset + (count >> 1)] << 8);
            UInt32 b = (UInt32)count + ((UInt32)buffer[offset + (count - 1)] << 2);
            UInt64 hash = ShiftMix((a * K2) ^ (b * K0)) * K2;

            return hash;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static UInt64 Hash4To7(ReadOnlySpan<Byte> buffer, Int32 offset, Int32 count)
        {
            UInt64 length = (UInt64)count;
            UInt64 m = K2 + (length * 2ul);

            UInt64 a = length + ((UInt64)BinaryOperations.Read32(buffer, offset) << 3);
            UInt64 b = BinaryOperations.Read32(buffer, offset + count - 4);

            UInt64 hash = HashLength16(a, b, m);

            return hash;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static UInt64 Hash8To16(ReadOnlySpan<Byte> buffer, Int32 offset, Int32 count)
        {
            UInt64 length = (UInt64)count;
            UInt64 m = K2 + (length * 2ul);

            UInt64 a = BinaryOperations.Read64(buffer, offset) + K2;
            UInt64 b = BinaryOperations.Read64(buffer, offset + count - 8);
            UInt64 c = (BinaryOperations.RotateRight(b, 37) * m) + a;
            UInt64 d = (BinaryOperations.RotateRight(a, 25) + b) * m;

            UInt64 hash = HashLength16(c, d, m);

            return hash;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static UInt64 Hash17To32(ReadOnlySpan<Byte> buffer, Int32 offset, Int32 count)
        {
            UInt64 m = K2 + ((UInt64)count * 2ul);

            UInt64 a = BinaryOperations.Read64(buffer, offset) * K1;
            UInt64 b = BinaryOperations.Read64(buffer, offset + 8);
            UInt64 c = BinaryOperations.Read64(buffer, offset + count - 8) * m;
            UInt64 d = BinaryOperations.Read64(buffer, offset + count - 16) * K2;
            UInt64 e = BinaryOperations.RotateRight(a + b, 43) + BinaryOperations.RotateRight(c, 30) + d;
            UInt64 f = a + BinaryOperations.RotateRight(b + K2, 18) + c;

            UInt64 hash = HashLength16(e, f, m);

            return hash;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static UInt64 Hash33To64(ReadOnlySpan<Byte> buffer, Int32 offset, Int32 count)
        {
            UInt64 m = K2 + ((UInt64)count * 2ul);

            UInt64 a = BinaryOperations.Read64(buffer, offset) * K2;
            UInt64 b = BinaryOperations.Read64(buffer, offset + 8);
            UInt64 c = BinaryOperations.Read64(buffer, offset + count - 8) * m;
            UInt64 d = BinaryOperations.Read64(buffer, offset + count - 16) * K2;
            UInt64 e = BinaryOperations.Read64(buffer, offset + 16) * m;
            UInt64 f = BinaryOperations.Read64(buffer, offset + 24);

            UInt64 y = BinaryOperations.RotateRight(a + b, 43) + BinaryOperations.RotateRight(c, 30) + d;
            UInt64 z = HashLength16(y, a + BinaryOperations.RotateRight(b + K2, 18) + c, m);

            UInt64 g = (y + BinaryOperations.Read64(buffer, offset + count - 32)) * m;
            UInt64 h = (z + BinaryOperations.Read64(buffer, offset + count - 24)) * m;
            UInt64 i = BinaryOperations.RotateRight(e + f, 43) + BinaryOperations.RotateRight(g, 30) + h;
            UInt64 j = e + BinaryOperations.RotateRight(f + a, 18) + g;

            UInt64 hash = HashLength16(i, j, m);

            return hash;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static UInt64 Hash65ToEnd(ReadOnlySpan<Byte> buffer, Int32 offset, Int32 count)
        {
            const UInt64 X0 = unchecked(81u * K2);
            const UInt64 Y0 = unchecked(81u * K1) + 113ul;

            UInt64 v0 = 0ul, v1 = 0ul, w0 = 0ul, w1 = 0ul;
            UInt64 x = X0 + BinaryOperations.Read64(buffer, offset);
            UInt64 y = Y0;
            UInt64 z = ShiftMix((y * K2) + 113ul) * K2;

            Int32 end = offset + (((count - 1) / 64) * 64);

            do
            {
                Update(ref x, ref y, ref z, buffer, offset, v0, v1, w0, w1, K1, 1ul);

                HashWeak32(out v0, out v1, buffer, offset, v1 * K1, x + w0);
                HashWeak32(out w0, out w1, buffer, offset + 32, z + w1, y + BinaryOperations.Read64(buffer, offset + 16));
                BinaryOperations.Swap(ref z, ref x);

                offset += 64;
            }
            while (offset < end);

            offset = end + ((count - 1) & 63) - 63;

            w0 += ((UInt64)count - 1) & 63;
            v0 += w0;
            w0 += v0;

            UInt64 m = K1 + ((z & 0x00000000000000FFul) << 1);

            Update(ref x, ref y, ref z, buffer, offset, v0, v1, w0, w1, m, 9ul);

            HashWeak32(out v0, out v1, buffer, offset, v1 * m, x + w0);
            HashWeak32(out w0, out w1, buffer, offset + 32, z + w1, y + BinaryOperations.Read64(buffer, offset + 16));
            BinaryOperations.Swap(ref z, ref x);

            UInt64 a = HashLength16(v0, w0, m) + (ShiftMix(y) * K0) + z;
            UInt64 b = HashLength16(v1, w1, m) + x;

            UInt64 hash = HashLength16(a, b, m);

            return hash;
        }

        /// <inheritdoc/>
        protected override Byte[] ComputeHashInternal(ReadOnlySpan<Byte> buffer)
        {
            Int32 offset = 0;
            Int32 count = buffer.Length;

            UInt64 hash;

            if (count == 0)
                hash = K2;
            else if (count <= 3)
                hash = Hash1To3(buffer, offset, count);
            else if (count <= 7)
                hash = Hash4To7(buffer, offset, count);
            else if (count <= 16)
                hash = Hash8To16(buffer, offset, count);
            else if (count <= 32)
                hash = Hash17To32(buffer, offset, count);
            else if (count <= 64)
                hash = Hash33To64(buffer, offset, count);
            else
                hash = Hash65ToEnd(buffer, offset, count);

            if (m_Seeds.Count > 0)
                hash = HashLength16(hash - m_Seeds[0], m_Seeds[1], K3);

            Byte[] result = BinaryOperations.ToArray64(hash);

            return result;
        }
        #endregion
    }

    /// <summary>Represents the FarmHash128 implementation. This class cannot be derived.</summary>
    public sealed class FarmHash128 : FarmHashOver32
    {
        #region Properties
        /// <inheritdoc/>
        [ExcludeFromCodeCoverage]
        public override Int32 Length => 128;
        #endregion

        #region Constructors
        /// <summary>Initializes a new instance without seeds.</summary>
        [ExcludeFromCodeCoverage]
        public FarmHash128() { }

        /// <summary>Initializes a new instance using the specified <see cref="T:System.UInt64"/> value for both seeds.</summary>
        /// <param name="seed">The <see cref="T:System.UInt64"/> seed used by the hashing algorithm.</param>
        [ExcludeFromCodeCoverage]
        public FarmHash128(UInt64 seed) : base(seed, seed) { }

        /// <summary>Initializes a new instance using the specified <see cref="T:System.UInt64"/> seeds.</summary>
        /// <param name="seed1">The first <see cref="T:System.UInt64"/> seed used by the hashing algorithm.</param>
        /// <param name="seed2">The second <see cref="T:System.UInt64"/> seed used by the hashing algorithm.</param>
        [ExcludeFromCodeCoverage]
        public FarmHash128(UInt64 seed1, UInt64 seed2) : base(seed1, seed2) { }
        #endregion

        #region Methods
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void Hash0(out UInt64 hash1, out UInt64 hash2, UInt64 seed1, UInt64 seed2)
        { 
            UInt64 a = ShiftMix(seed1 * K1) * K1;
            UInt64 b = seed2;
            UInt64 c = (b * K1) + K2;
            UInt64 d = ShiftMix(a + c);
            UInt64 e = HashLength16(a, c, K3);
            UInt64 f = HashLength16(d, b, K3);

            hash1 = e ^ f;
            hash2 = HashLength16(f, e, K3);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void Hash1To3(out UInt64 hash1, out UInt64 hash2, ReadOnlySpan<Byte> buffer, Int32 offset, Int32 count, UInt64 seed1, UInt64 seed2)
        {
            Byte v0 = buffer[offset];
            Byte v1 = buffer[offset + (count >> 1)];
            Byte v2 = buffer[offset + (count - 1)];
            UInt32 v3 = v0 + ((UInt32)v1 << 8);
            UInt32 v4 = (UInt32)count + ((UInt32)v2 << 2);
            UInt64 k = ShiftMix(v3 * K2 ^ v4 * K0) * K2;

            UInt64 a = ShiftMix(seed1 * K1) * K1;
            UInt64 b = seed2;
            UInt64 c = (b * K1) + k;
            UInt64 d = ShiftMix(a + c);
            UInt64 e = HashLength16(a, c, K3);
            UInt64 f = HashLength16(d, b, K3);

            hash1 = e ^ f;
            hash2 = HashLength16(f, e, K3);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void Hash4To7(out UInt64 hash1, out UInt64 hash2, ReadOnlySpan<Byte> buffer, Int32 offset, Int32 count, UInt64 seed1, UInt64 seed2)
        {
            UInt64 length = (UInt64)count;

            UInt64 v0 = K2 + (length * 2ul);
            UInt64 v1 = length + ((UInt64)BinaryOperations.Read32(buffer, offset) << 3);
            UInt64 v2 = BinaryOperations.Read32(buffer, offset + count - 4);
            UInt64 k = HashLength16(v1, v2, v0);

            UInt64 a = ShiftMix(seed1 * K1) * K1;
            UInt64 b = seed2;
            UInt64 c = (b * K1) + k;
            UInt64 d = ShiftMix(a + c);
            UInt64 e = HashLength16(a, c, K3);
            UInt64 f = HashLength16(d, b, K3);

            hash1 = e ^ f;
            hash2 = HashLength16(f, e, K3);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void Hash8To16(out UInt64 hash1, out UInt64 hash2, ReadOnlySpan<Byte> buffer, Int32 offset, Int32 count, UInt64 seed1, UInt64 seed2)
        {
            UInt64 length = (UInt64)count;

            UInt64 v0 = K2 + (length * 2ul);
            UInt64 v1 = BinaryOperations.Read64(buffer, offset) + K2;
            UInt64 v2 = BinaryOperations.Read64(buffer, offset + count - 8);
            UInt64 v3 = (BinaryOperations.RotateRight(v2, 37) * v0) + v1;
            UInt64 v4 = (BinaryOperations.RotateRight(v1, 25) + v2) * v0;
            UInt64 k = HashLength16(v3, v4, v0);

            UInt64 a = ShiftMix(seed1 * K1) * K1;
            UInt64 b = seed2;
            UInt64 c = (b * K1) + k;
            UInt64 d = ShiftMix(a + BinaryOperations.Read64(buffer, offset));
            UInt64 e = HashLength16(a, c, K3);
            UInt64 f = HashLength16(d, b, K3);

            hash1 = e ^ f;
            hash2 = HashLength16(f, e, K3);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void Hash17To127(out UInt64 hash1, out UInt64 hash2, ReadOnlySpan<Byte> buffer, Int32 offset, Int32 count, UInt64 seed1, UInt64 seed2)
        {
            UInt64 a = seed1;
            UInt64 b = seed2;
            UInt64 c = HashLength16(BinaryOperations.Read64(buffer, offset + count - 8) + K1, a, K3);
            UInt64 d = HashLength16(b + (UInt64)count, c + BinaryOperations.Read64(buffer, offset + count - 16), K3);

            a += d;

            Int32 remainder = count - 16;

            do
            {
                a ^= ShiftMix(BinaryOperations.Read64(buffer, offset) * K1) * K1;
                a *= K1;
                b ^= a;
                c ^= ShiftMix(BinaryOperations.Read64(buffer, offset + 8) * K1) * K1;
                c *= K1;
                d ^= c;

                offset += 16;
                remainder -= 16;
            }
            while (remainder > 0);

            UInt64 e = HashLength16(a, c, K3);
            UInt64 f = HashLength16(d, b, K3);

            hash1 = e ^ f;
            hash2 = HashLength16(f, e, K3);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void Hash128ToEnd(out UInt64 hash1, out UInt64 hash2, ReadOnlySpan<Byte> buffer, Int32 offset, Int32 count, UInt64 seed1, UInt64 seed2)
        {
            UInt64 x = seed1;
            UInt64 y = seed2;
            UInt64 z = (UInt64)count * K1;

            UInt64 v0 = (BinaryOperations.RotateRight(y ^ K1, 49) * K1) + BinaryOperations.Read64(buffer, offset);
            UInt64 v1 = (BinaryOperations.RotateRight(v0, 42) * K1) + BinaryOperations.Read64(buffer, offset + 8);
            UInt64 w0 = (BinaryOperations.RotateRight(y + z, 35) * K1) + x;
            UInt64 w1 = BinaryOperations.RotateRight(x + BinaryOperations.Read64(buffer, offset + 88), 53) * K1;

            do
            {
                for (Int32 i = 0; i < 2; ++i)
                {
                    Update(ref x, ref y, ref z, buffer, offset, v0, v1, w0, w1, K1, 1ul);

                    HashWeak32(out v0, out v1, buffer, offset, v1 * K1, x + w0);
                    HashWeak32(out w0, out w1, buffer, offset + 32, z + w1, y + BinaryOperations.Read64(buffer, offset + 16));
                    BinaryOperations.Swap(ref z, ref x);

                    offset += 64;
                }

                count -= 128;
            }
            while (count >= 128);

            x += BinaryOperations.RotateRight(v0 + z, 49) * K0;
            y = (y * K0) + BinaryOperations.RotateRight(w1, 37);
            z = (z * K0) + BinaryOperations.RotateRight(w0, 27);
            w0 *= 9ul;
            v0 *= K0;

            Int32 t = 0;

            while (t < count)
            {
                t += 32;

                Int32 countDiff = count - t;

                y = (BinaryOperations.RotateRight(x + y, 42) * K0) + v1;
                w0 += BinaryOperations.Read64(buffer, offset + countDiff + 16);
                x = (x * K0) + w0;
                z += w1 + BinaryOperations.Read64(buffer, offset + countDiff);
                w1 += v0;

                HashWeak32(out v0, out v1, buffer, offset + countDiff, v0 + z, v1);
                v0 *= K0;
            }

            x = HashLength16(x, v0, K3);
            y = HashLength16(y + z, w0, K3);

            hash1 = HashLength16(x + v1, w1, K3) + y;
            hash2 = HashLength16(x + w1, y + v1, K3);
        }

        /// <inheritdoc/>
        protected override Byte[] ComputeHashInternal(ReadOnlySpan<Byte> buffer)
        {
            Int32 offset = 0;
            Int32 count = buffer.Length;

            UInt64 seed1, seed2;
            UInt64 hash1, hash2;

            if (count == 0)
            {
                if (m_Seeds.Count == 0)
                {
                    seed1 = K0;
                    seed2 = K1;
                }
                else
                {
                    seed1 = m_Seeds[0];
                    seed2 = m_Seeds[1];
                }

                Hash0(out hash1, out hash2, seed1, seed2);

                goto Finalize;
            }

            if (m_Seeds.Count == 0)
            {
                if (count >= 16)
                {
                    seed1 = BinaryOperations.Read64(buffer, offset);
                    seed2 = BinaryOperations.Read64(buffer, offset + 8) + K0;

                    offset += 16;
                    count -= 16;
                }
                else
                {
                    seed1 = K0;
                    seed2 = K1;
                }
            }
            else
            {
                seed1 = m_Seeds[0];
                seed2 = m_Seeds[1];
            }

            if (count == 0)
                Hash0(out hash1, out hash2, seed1, seed2);
            else if (count <= 3)
                Hash1To3(out hash1, out hash2, buffer, offset, count, seed1, seed2);
            else if (count <= 7)
                Hash4To7(out hash1, out hash2, buffer, offset, count, seed1, seed2);
            else if (count <= 16)
                Hash8To16(out hash1, out hash2, buffer, offset, count, seed1, seed2);
            else if (count <= 127)
                Hash17To127(out hash1, out hash2, buffer, offset, count, seed1, seed2);
            else
                Hash128ToEnd(out hash1, out hash2, buffer, offset, count, seed1, seed2);

            Finalize:

            Byte[] result = BinaryOperations.ToArray64(hash1, hash2);

            return result;
        }
        #endregion
    }
}