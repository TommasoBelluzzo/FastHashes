#region Using Directives
using System;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
#endregion

namespace FastHashes
{
    /// <summary>Represents the base class from which all the FarmHash implementations with more than 32 bits of output must derive. This class is abstract.</summary>
    public abstract class FarmHashG32 : Hash
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
        protected FarmHashG32()
        {
            m_Seeds = new ReadOnlyCollection<UInt64>(new UInt64[0]);
        }

        /// <summary>Represents the base constructor with two seeds used by derived classes.</summary>
        /// <param name="seed1">The first <see cref="T:System.UInt64"/> seed used by the hashing algorithm.</param>
        /// <param name="seed2">The second <see cref="T:System.UInt64"/> seed used by the hashing algorithm.</param>
        protected FarmHashG32(UInt64 seed1, UInt64 seed2)
        {
            m_Seeds = new ReadOnlyCollection<UInt64>(new[] { seed1, seed2 });
        }
        #endregion

        #region Methods (Static)
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
        protected static unsafe void Update(ref UInt64 x, ref UInt64 y, ref UInt64 z, Byte* pointer, UInt64 v0, UInt64 v1, UInt64 w0, UInt64 w1, UInt64 m, UInt64 c)
        {
            x = RotateRight(x + y + v0 + Fetch64(pointer + 8), 37) * m;
            y = RotateRight(y + v1 + Fetch64(pointer + 48), 42) * m;
            x ^= w1 * c;
            y += (v0 * c) + Fetch64(pointer + 40);
            z = RotateRight(z + w0, 33) * m;
        }

        /// <summary>Represents an auxiliary hashing function used by derived classes.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected static unsafe void HashWeak32(out UInt64 v1, out UInt64 v2, Byte* pointer, UInt64 v3, UInt64 v4)
        {
            UInt64 w = Fetch64(pointer);
            UInt64 x = Fetch64(pointer + 8);
            UInt64 y = Fetch64(pointer + 16);
            UInt64 z = Fetch64(pointer + 24);

            v3 += w;
            v4 = RotateRight(v4 + v3 + z, 21);

            UInt64 c = v3;

            v3 += x;
            v3 += y;
            v4 += RotateRight(v3, 44);

            v1 = v3 + z;
            v2 = v4 + c;
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
        public FarmHash32()
        {
            m_Seed = null;
        }

        /// <summary>Initializes a new instance using the specified seed.</summary>
        /// <param name="seed">The <see cref="T:System.UInt32"/> seed used by the hashing algorithm.</param>
        public FarmHash32(UInt32 seed)
        {
            m_Seed = seed;
        }
        #endregion

        #region Methods
        /// <inheritdoc/>
        protected override Byte[] ComputeHashInternal(Byte[] buffer, Int32 offset, Int32 count)
        {
            UInt32 hash;

            unsafe
            {
                fixed (Byte* pin = &buffer[offset])
                {
                    Byte* pointer = pin;

                    if (m_Seed.HasValue)
                        hash = Hash(pointer, count, m_Seed.Value);
                    else
                        hash = Hash(pointer, count);
                }
            }

            Byte[] result = ToByteArray64(hash);

            return result;
        }
        #endregion

        #region Methods (Static)
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
        private static unsafe UInt32 Hash(Byte* pointer, Int32 length)
        {
            UInt32 hash;

            if (length <= 4)
                hash = Hash0To4(pointer, length, 0u);
            else if (length <= 12)
                hash = Hash5To12(pointer, length, 0u);
            else if (length <= 24)
                hash = Hash13To24(pointer, length, 0u);
            else
                hash = Hash24ToEnd(pointer, length);

            return hash;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe UInt32 Hash(Byte* pointer, Int32 length, UInt32 seed)
        {
            UInt32 hash;

            if (length <= 4)
                hash = Hash0To4(pointer, length, seed);
            else if (length <= 12)
                hash = Hash5To12(pointer, length, seed);
            else if (length <= 24)
                hash = Hash13To24(pointer, length, seed * C1);
            else
            {
                UInt32 v1 = Hash13To24(pointer, 24, seed ^ (UInt32)length);
                UInt32 v2 = Hash(pointer + 24, length - 24) + seed;

                hash = Mur(v1, v2);
            }

            return hash;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe UInt32 Hash0To4(Byte* pointer, Int32 length, UInt32 seed)
        {
            UInt32 a = seed;
            UInt32 b = 9u;

            for (Int32 i = 0; i < length; ++i)
            {
                a = a * C1 + pointer[i];
                b ^= a;
            }

            UInt32 hash = Mur(b, (UInt32)length);
            hash = Mur(hash, a);
            hash = Fin(hash);

            return hash;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe UInt32 Hash5To12(Byte* pointer, Int32 length, UInt32 seed)
        {
            UInt32 a = (UInt32)length;
            UInt32 b = a * 5u;
            UInt32 c = 9u;
            UInt32 d = b + seed;

            a += Fetch32(pointer);
            b += Fetch32(pointer + length - 4);
            c += Fetch32(pointer + ((length >> 1) & 4));

            UInt32 hash = Mur(d, a);
            hash = Mur(hash, b);
            hash = Mur(hash, c);
            hash = Fin(seed ^ hash);

            return hash;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe UInt32 Hash13To24(Byte* pointer, Int32 length, UInt32 seed)
        {
            UInt32 a = Fetch32(pointer - 4 + (length >> 1));
            UInt32 b = Fetch32(pointer + 4);
            UInt32 c = Fetch32(pointer + length - 8);
            UInt32 d = Fetch32(pointer + (length >> 1));
            UInt32 e = Fetch32(pointer);
            UInt32 f = Fetch32(pointer + length - 4);

            UInt32 hash = (d * C1) + (UInt32)length + seed;
            a = RotateRight(a, 12) + f;
            hash = Mur(hash, c) + a;
            a = RotateRight(a, 3) + c;
            hash = Mur(hash, e) + a;
            a = RotateRight(a + f, 12) + d;
            hash = Mur(hash, b ^ seed) + a;
            hash = Fin(hash);

            return hash;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe UInt32 Hash24ToEnd(Byte* pointer, Int32 length)
        {
            UInt32 hash = (UInt32)length;
            UInt32 g = C1 * hash;
            UInt32 f = g;

            hash = Mur(hash, Fetch32(pointer + length - 4));
            g = Mur(g, Fetch32(pointer + length - 8));
            hash = Mur(hash, Fetch32(pointer + length - 16));
            g = Mur(g, Fetch32(pointer + length - 12));

            f += RotateRight(Fetch32(pointer + length - 20) * C1, 17) * C2;
            f = RotateRight(f, 19) + 113u;

            Int32 blocks = (length - 1) / 20;

            do
            {
                UInt32 a = Read32(ref pointer);
                UInt32 b = Read32(ref pointer);
                UInt32 c = Read32(ref pointer);
                UInt32 d = Read32(ref pointer);
                UInt32 e = Read32(ref pointer);

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

            g = RotateRight(g, 11) * C1;
            g = RotateRight(g, 17) * C1;
            f = RotateRight(f, 11) * C1;
            f = RotateRight(f, 17) * C1;

            hash = RotateRight(hash + g, 19);
            hash = (hash * 5u) + N;
            hash = RotateRight(hash, 17) * C1;
            hash = RotateRight(hash + f, 19);
            hash = (hash * 5u) + N;
            hash = RotateRight(hash, 17) * C1;

            return hash;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static UInt32 Mur(UInt32 v1, UInt32 v2)
        {
            v2 *= C1;
            v2 = RotateRight(v2, 17);
            v2 *= C2;

            v1 ^= v2;
            v1 = RotateRight(v1, 19);
            v1 = (v1 * 5u) + N;

            return v1;
        }
        #endregion
    }

    /// <summary>Represents the FarmHash64 implementation. This class cannot be derived.</summary>
    public sealed class FarmHash64 : FarmHashG32
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
        /// <inheritdoc/>
        protected override Byte[] ComputeHashInternal(Byte[] buffer, Int32 offset, Int32 count)
        {
            UInt64 hash;

            unsafe
            {
                fixed (Byte* pin = &buffer[offset])
                {
                    Byte* pointer = pin;

                    if (count == 0)
                        hash = K2;
                    else if (count <= 3)
                        hash = Hash1To3(pointer, count);
                    else if (count <= 7)
                        hash = Hash4To7(pointer, count);
                    else if (count <= 16)
                        hash = Hash8To16(pointer, count);
                    else if (count <= 32)
                        hash = Hash17To32(pointer, count);
                    else if (count <= 64)
                        hash = Hash33To64(pointer, count);
                    else
                        hash = Hash65ToEnd(pointer, count);

                    if (m_Seeds.Count > 0)
                        hash = HashLength16(hash - m_Seeds[0], m_Seeds[1], K3);
                }
            }

            Byte[] result = ToByteArray64(hash);

            return result;
        }
        #endregion

        #region Methods (Static)
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe UInt64 Hash1To3(Byte* pointer, Int32 length)
        {
            UInt32 a = pointer[0] + ((UInt32)pointer[length >> 1] << 8);
            UInt32 b = (UInt32)length + ((UInt32)pointer[length - 1] << 2);

            return ShiftMix(a * K2 ^ b * K0) * K2;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe UInt64 Hash4To7(Byte* pointer, Int32 length)
        {
            UInt64 lengthUnsigned = (UInt64)length;
            UInt64 m = K2 + (lengthUnsigned * 2ul);

            UInt64 a = lengthUnsigned + ((UInt64)Fetch32(pointer) << 3);
            UInt64 b = Fetch32(pointer + length - 4);

            return HashLength16(a, b, m);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe UInt64 Hash8To16(Byte* pointer, Int32 length)
        {
            UInt64 lengthUnsigned = (UInt64)length;
            UInt64 m = K2 + (lengthUnsigned * 2ul);

            UInt64 a = Fetch64(pointer) + K2;
            UInt64 b = Fetch64(pointer + length - 8);
            UInt64 c = (RotateRight(b, 37) * m) + a;
            UInt64 d = (RotateRight(a, 25) + b) * m;

            return HashLength16(c, d, m);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe UInt64 Hash17To32(Byte* pointer, Int32 length)
        {
            UInt64 m = K2 + ((UInt64)length * 2ul);

            UInt64 a = Fetch64(pointer) * K1;
            UInt64 b = Fetch64(pointer + 8);
            UInt64 c = Fetch64(pointer + length - 8) * m;
            UInt64 d = Fetch64(pointer + length - 16) * K2;
            UInt64 e = RotateRight(a + b, 43) + RotateRight(c, 30) + d;
            UInt64 f = a + RotateRight(b + K2, 18) + c;

            return HashLength16(e, f, m);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe UInt64 Hash33To64(Byte* pointer, Int32 length)
        {
            UInt64 m = K2 + ((UInt64)length * 2ul);

            UInt64 a = Fetch64(pointer) * K2;
            UInt64 b = Fetch64(pointer + 8);
            UInt64 c = Fetch64(pointer + length - 8) * m;
            UInt64 d = Fetch64(pointer + length - 16) * K2;
            UInt64 e = Fetch64(pointer + 16) * m;
            UInt64 f = Fetch64(pointer + 24);

            UInt64 y = RotateRight(a + b, 43) + RotateRight(c, 30) + d;
            UInt64 z = HashLength16(y, a + RotateRight(b + K2, 18) + c, m);

            UInt64 g = (y + Fetch64(pointer + length - 32)) * m;
            UInt64 h = (z + Fetch64(pointer + length - 24)) * m;
            UInt64 i = RotateRight(e + f, 43) + RotateRight(g, 30) + h;
            UInt64 j = e + RotateRight(f + a, 18) + g;

            return HashLength16(i, j, m);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe UInt64 Hash65ToEnd(Byte* pointer, Int32 length)
        {
            const UInt64 X0 = unchecked(81u * K2);
            const UInt64 Y0 = unchecked(81u * K1) + 113ul;

            UInt64 v0 = 0ul, v1 = 0ul, w0 = 0ul, w1 = 0ul;
            UInt64 x = X0 + Fetch64(pointer);
            UInt64 y = Y0;
            UInt64 z = ShiftMix((y * K2) + 113ul) * K2;

            Byte* end = pointer + (((length - 1) / 64) * 64);

            do
            {
                Update(ref x, ref y, ref z, pointer, v0, v1, w0, w1, K1, 1ul);

                HashWeak32(out v0, out v1, pointer, v1 * K1, x + w0);
                HashWeak32(out w0, out w1, pointer + 32, z + w1, y + Fetch64(pointer + 16));
                Swap(ref z, ref x);

                pointer += 64;
            }
            while (pointer < end);

            pointer = end + ((length - 1) & 63) - 63;

            w0 += ((UInt64)length - 1) & 63;
            v0 += w0;
            w0 += v0;

            UInt64 m = K1 + ((z & 0x00000000000000FFul) << 1);

            Update(ref x, ref y, ref z, pointer, v0, v1, w0, w1, m, 9ul);

            HashWeak32(out v0, out v1, pointer, v1 * m, x + w0);
            HashWeak32(out w0, out w1, pointer + 32, z + w1, y + Fetch64(pointer + 16));
            Swap(ref z, ref x);

            UInt64 a = HashLength16(v0, w0, m) + (ShiftMix(y) * K0) + z;
            UInt64 b = HashLength16(v1, w1, m) + x;

            return HashLength16(a, b, m);
        }
        #endregion
    }

    /// <summary>Represents the FarmHash128 implementation. This class cannot be derived.</summary>
    public sealed class FarmHash128 : FarmHashG32
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
        /// <inheritdoc/>
        protected override Byte[] ComputeHashInternal(Byte[] buffer, Int32 offset, Int32 count)
        {
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

            unsafe
            {
                fixed (Byte* pin = &buffer[offset])
                {
                    Byte* pointer = pin;

                    if (m_Seeds.Count == 0)
                    {
                        if (count >= 16)
                        {
                            seed1 = Fetch64(pointer);
                            seed2 = Fetch64(pointer + 8) + K0;

                            pointer += 16;
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

                    if (count <= 3)
                        Hash1To3(out hash1, out hash2, pointer, count, seed1, seed2);
                    else if (count <= 7)
                        Hash4To7(out hash1, out hash2, pointer, count, seed1, seed2);
                    else if (count <= 16)
                        Hash8To16(out hash1, out hash2, pointer, count, seed1, seed2);
                    else if (count <= 127)
                        Hash17To127(out hash1, out hash2, pointer, count, seed1, seed2);
                    else
                        Hash128ToEnd(out hash1, out hash2, pointer, count, seed1, seed2);
                }
            }

Finalize:

            Byte[] result = ToByteArray64(hash1, hash2);

            return result;
        }
        #endregion

        #region Methods (Static)
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
        private static unsafe void Hash1To3(out UInt64 hash1, out UInt64 hash2, Byte* pointer, Int32 length, UInt64 seed1, UInt64 seed2)
        {
            Byte v0 = pointer[0];
            Byte v1 = pointer[length >> 1];
            Byte v2 = pointer[length - 1];
            UInt32 v3 = v0 + ((UInt32)v1 << 8);
            UInt32 v4 = (UInt32)length + ((UInt32)v2 << 2);
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
        private static unsafe void Hash4To7(out UInt64 hash1, out UInt64 hash2, Byte* pointer, Int32 length, UInt64 seed1, UInt64 seed2)
        {
            UInt64 lengthUnsigned = (UInt64)length;

            UInt64 v0 = K2 + (lengthUnsigned * 2ul);
            UInt64 v1 = lengthUnsigned + ((UInt64)Fetch32(pointer) << 3);
            UInt64 v2 = Fetch32(pointer + length - 4);
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
        private static unsafe void Hash8To16(out UInt64 hash1, out UInt64 hash2, Byte* pointer, Int32 length, UInt64 seed1, UInt64 seed2)
        {
            UInt64 lengthUnsigned = (UInt64)length;

            UInt64 v0 = K2 + (lengthUnsigned * 2ul);
            UInt64 v1 = Fetch64(pointer) + K2;
            UInt64 v2 = Fetch64(pointer + length - 8);
            UInt64 v3 = (RotateRight(v2, 37) * v0) + v1;
            UInt64 v4 = (RotateRight(v1, 25) + v2) * v0;
            UInt64 k = HashLength16(v3, v4, v0);

            UInt64 a = ShiftMix(seed1 * K1) * K1;
            UInt64 b = seed2;
            UInt64 c = (b * K1) + k;
            UInt64 d = ShiftMix(a + Fetch64(pointer));
            UInt64 e = HashLength16(a, c, K3);
            UInt64 f = HashLength16(d, b, K3);

            hash1 = e ^ f;
            hash2 = HashLength16(f, e, K3);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe void Hash17To127(out UInt64 hash1, out UInt64 hash2, Byte* pointer, Int32 length, UInt64 seed1, UInt64 seed2)
        {
            UInt64 a = seed1;
            UInt64 b = seed2;
            UInt64 c = HashLength16(Fetch64(pointer + length - 8) + K1, a, K3);
            UInt64 d = HashLength16(b + (UInt64)length, c + Fetch64(pointer + length - 16), K3);

            a += d;

            Int32 remainder = length - 16;

            do
            {
                a ^= ShiftMix(Fetch64(pointer) * K1) * K1;
                a *= K1;
                b ^= a;
                c ^= ShiftMix(Fetch64(pointer + 8) * K1) * K1;
                c *= K1;
                d ^= c;

                pointer += 16;
                remainder -= 16;
            }
            while (remainder > 0);

            UInt64 e = HashLength16(a, c, K3);
            UInt64 f = HashLength16(d, b, K3);

            hash1 = e ^ f;
            hash2 = HashLength16(f, e, K3);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe void Hash128ToEnd(out UInt64 hash1, out UInt64 hash2, Byte* pointer, Int32 length, UInt64 seed1, UInt64 seed2)
        {
            UInt64 x = seed1;
            UInt64 y = seed2;
            UInt64 z = (UInt64)length * K1;

            UInt64 v0 = (RotateRight(y ^ K1, 49) * K1) + Fetch64(pointer);
            UInt64 v1 = (RotateRight(v0, 42) * K1) + Fetch64(pointer + 8);
            UInt64 w0 = (RotateRight(y + z, 35) * K1) + x;
            UInt64 w1 = RotateRight(x + Fetch64(pointer + 88), 53) * K1;

            do
            {
                for (Int32 i = 0; i < 2; ++i)
                {
                    Update(ref x, ref y, ref z, pointer, v0, v1, w0, w1, K1, 1ul);

                    HashWeak32(out v0, out v1, pointer, v1 * K1, x + w0);
                    HashWeak32(out w0, out w1, pointer + 32, z + w1, y + Fetch64(pointer + 16));
                    Swap(ref z, ref x);

                    pointer += 64;
                }

                length -= 128;
            }
            while (length >= 128);

            x += RotateRight(v0 + z, 49) * K0;
            y = (y * K0) + RotateRight(w1, 37);
            z = (z * K0) + RotateRight(w0, 27);
            w0 *= 9ul;
            v0 *= K0;

            Int32 t = 0;

            while (t < length)
            {
                t += 32;

                Int32 lengthDiff = length - t;

                y = (RotateRight(x + y, 42) * K0) + v1;
                w0 += Fetch64(pointer + lengthDiff + 16);
                x = (x * K0) + w0;
                z += w1 + Fetch64(pointer + lengthDiff);
                w1 += v0;

                HashWeak32(out v0, out v1, pointer + lengthDiff, v0 + z, v1);
                v0 *= K0;
            }

            x = HashLength16(x, v0, K3);
            y = HashLength16(y + z, w0, K3);

            hash1 = HashLength16(x + v1, w1, K3) + y;
            hash2 = HashLength16(x + w1, y + v1, K3);
        }
        #endregion
    }
}