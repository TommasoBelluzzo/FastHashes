#region Using Directives
using System;
using System.Runtime.CompilerServices;
#endregion

namespace FastHashes
{
    /// <summary>Represents the base class from which all implementations of HighwayHash must derive. This class is abstract.</summary>
    public abstract class HighwayHash : Hash
    {
        #region Constants
        private const UInt64 M00 = 0xDBE6D5D5FE4CCE2Ful;
        private const UInt64 M01 = 0xA4093822299F31D0ul;
        private const UInt64 M02 = 0x13198A2E03707344ul;
        private const UInt64 M03 = 0x243F6A8885A308D3ul;
        private const UInt64 M10 = 0x3BD39E10CB0EF593ul;
        private const UInt64 M11 = 0xC0ACF169B5F18A8Cul;
        private const UInt64 M12 = 0xBE5466CF34E90C6Cul;
        private const UInt64 M13 = 0x452821E638D01377ul;
        #endregion

        #region Members
        private readonly UInt64 m_Seed1;
        private readonly UInt64 m_Seed2;
        private readonly UInt64 m_Seed3;
        private readonly UInt64 m_Seed4;
        #endregion

        #region (Properties)
        /// <summary>Gets the first seed used by the hashing algorithm.</summary>
        /// <value>A <see cref="T:System.UInt64"/> value.</value>
        public UInt64 Seed1 => m_Seed1;

        /// <summary>Gets the second seed used by the hashing algorithm.</summary>
        /// <value>A <see cref="T:System.UInt64"/> value.</value>
        public UInt64 Seed2 => m_Seed2;

        /// <summary>Gets the third seed used by the hashing algorithm.</summary>
        /// <value>A <see cref="T:System.UInt64"/> value.</value>
        public UInt64 Seed3 => m_Seed3;

        /// <summary>Gets the fourth seed used by the hashing algorithm.</summary>
        /// <value>A <see cref="T:System.UInt64"/> value.</value>
        public UInt64 Seed4 => m_Seed4;
        #endregion

        #region Properties (Abstract)
        /// <summary>Gets the number of hash finalization cycles.</summary>
        /// <value>An unsigned integer between <c>0</c> and <see cref="F:System.UInt32.MaxValue"/>.</value>
        protected abstract UInt32 P { get; }
        #endregion

        #region Constructors
        /// <summary>Represents the compact base constructor used by derived classes.</summary>
        /// <param name="seeds">A <see cref="T:System.UInt64"/>[] of seeds used by the hashing algorithm.</param>
        /// <exception cref="T:System.ArgumentException">Thrown when the number of seeds in <paramref name="seeds">seeds</paramref> is not equal to 4.</exception>
        /// <exception cref="T:System.ArgumentNullException">Thrown when <paramref name="seeds">seeds</paramref> is null.</exception>
        protected HighwayHash(UInt64[] seeds)
        {
            if (seeds == null)
                throw new ArgumentNullException(nameof(seeds));

            if (seeds.Length != 4)
                throw new ArgumentException("The specified array must contains 4 seeds.", nameof(seeds));

            m_Seed1 = seeds[0];
            m_Seed2 = seeds[1];
            m_Seed3 = seeds[2];
            m_Seed4 = seeds[3];
        }

        /// <summary>Represents the parametrized base constructor used by derived classes.</summary>
        /// <param name="seed1">The first <see cref="T:System.UInt64"/> seed used by the hashing algorithm.</param>
        /// <param name="seed2">The second <see cref="T:System.UInt64"/> seed used by the hashing algorithm.</param>
        /// <param name="seed3">The third <see cref="T:System.UInt64"/> seed used by the hashing algorithm.</param>
        /// <param name="seed4">The fourth <see cref="T:System.UInt64"/> seed used by the hashing algorithm.</param>
        protected HighwayHash(UInt64 seed1, UInt64 seed2, UInt64 seed3, UInt64 seed4)
        {
            m_Seed1 = seed1;
            m_Seed2 = seed2;
            m_Seed3 = seed3;
            m_Seed4 = seed4;
        }
        #endregion

        #region Methods
        /// <inheritdoc/>
        protected override Byte[] ComputeHashInternal(Byte[] buffer, Int32 offset, Int32 count)
        {
            UInt64[] m0 = { M00, M01, M02, M03 };
            UInt64[] m1 = { M10, M11, M12, M13 };

            UInt64[] v0 =
            {
                M00 ^ m_Seed1,
                M01 ^ m_Seed2,
                M02 ^ m_Seed3,
                M03 ^ m_Seed4
            };

            UInt64[] v1 =
            {
                M10 ^ ((m_Seed1 >> 32) | (m_Seed1 << 32)),
                M11 ^ ((m_Seed2 >> 32) | (m_Seed2 << 32)),
                M12 ^ ((m_Seed3 >> 32) | (m_Seed3 << 32)),
                M13 ^ ((m_Seed4 >> 32) | (m_Seed4 << 32))
            };

            if (count == 0)
                goto Finalize;

            unsafe
            {
                fixed (Byte* pin = &buffer[offset])
                {
                    Byte* pointer = pin;

                    Int32 blocks = count / 32;
                    Int32 remainder = count & 31;

                    while (blocks-- > 0)
                    {
                        UInt64[] k =
                        {
                            Read64(ref pointer), Read64(ref pointer),
                            Read64(ref pointer), Read64(ref pointer)
                        };

                        Update(ref m0, ref m1, ref v0, ref v1, k);
                    }

                    if (remainder > 0)
                    {
                        UInt64 increment = ((UInt64)remainder << 32) + (UInt64)remainder;

                        for (Int32 i = 0; i < 4; ++i)
                        {
                            v0[i]  += increment;
                            v1[i] = Mix(v1[i], remainder);
                        }

                        Int32 diff4 = remainder & ~3;
                        Int32 mod4 = remainder & 3;

                        Byte[] packet = new Byte[32];

                        for (Int32 i = 0; i < diff4; ++i)
                            packet[i] = pointer[i];

                        pointer += diff4;

                        if ((remainder & 16) > 0)
                        {
                            for (Int32 i = 0; i < 4; ++i)
                                packet[28 + i] = pointer[i + mod4 - 4];
                        }
                        else if (mod4 > 0)
                        {
                            packet[16] = pointer[0];
                            packet[17] = pointer[mod4 >> 1];
                            packet[18] = pointer[mod4 - 1];
                        }

                        fixed (Byte* packetPin = &packet[0])
                        {
                            Byte* packetPointer = packetPin;

                            UInt64[] k =
                            {
                                Read64(ref packetPointer), Read64(ref packetPointer),
                                Read64(ref packetPointer), Read64(ref packetPointer)
                            };

                            Update(ref m0, ref m1, ref v0, ref v1, k);
                        }
                    }
                }
            }

            Finalize:

            for (UInt32 i = 0; i < P; ++i)
            {
                UInt64[] k =
                {
                    (v0[2] >> 32) | (v0[2] << 32),
                    (v0[3] >> 32) | (v0[3] << 32),
                    (v0[0] >> 32) | (v0[0] << 32),
                    (v0[1] >> 32) | (v0[1] << 32)
                };

                Update(ref m0, ref m1, ref v0, ref v1, k);
            }

            return GetHash(m0, m1, v0, v1);
        }
        #endregion

        #region Methods (Abstract)
        /// <summary>Finalizes any partial computation and returns the hash code.</summary>
        /// <param name="m0">A <see cref="T:System.UInt64"/>[] representing the hash data M0.</param>
        /// <param name="m1">A <see cref="T:System.UInt64"/>[] representing the hash data M1.</param>
        /// <param name="v0">A <see cref="T:System.UInt64"/>[] representing the hash data V0.</param>
        /// <param name="v1">A <see cref="T:System.UInt64"/>[] representing the hash data V1.</param>
        /// <returns>A <see cref="T:System.Byte"/>[] representing the hash code.</returns>
        protected abstract Byte[] GetHash(UInt64[] m0, UInt64[] m1, UInt64[] v0, UInt64[] v1);
        #endregion

        #region Methods (Static)
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static UInt64 Mix(UInt64 v, Int32 r)
        {
            UInt32 h0 = (UInt32)(v & 0x00000000FFFFFFFFul);
            UInt32 h1 = (UInt32)(v >> 32);

            v = RotateLeft(h0, r);
            v |= (UInt64)RotateLeft(h1, r) << 32;

            return v;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void Mix(ref UInt64 m0, ref UInt64 m1, ref UInt64 v0, ref UInt64 v1, UInt64 k)
        {
            v1 += m0 + k;
            m0 ^= (v1 & 0x00000000FFFFFFFFul) * (v0 >> 32);
            v0 += m1;
            m1 ^= (v0 & 0x00000000FFFFFFFFul) * (v1 >> 32);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void Update(ref UInt64[] m0, ref UInt64[] m1, ref UInt64[] v0, ref UInt64[] v1, UInt64[] k)
        {
            for (Int32 i = 0; i < 4; ++i)
                Mix(ref m0[i], ref m1[i], ref v0[i], ref v1[i], k[i]);

            ZMA(ref v0[0], ref v0[1], v1[1], v1[0]);
            ZMA(ref v0[2], ref v0[3], v1[3], v1[2]);
            ZMA(ref v1[0], ref v1[1], v0[1], v0[0]);
            ZMA(ref v1[2], ref v1[3], v0[3], v0[2]);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void ZMA(ref UInt64 v1, ref UInt64 v2, UInt64 v3, UInt64 v4)
        {
            v1 += (((v4 & 0x00000000FF000000ul) | (v3 & 0x000000FF00000000ul)) >> 24)
                | (((v4 & 0x0000FF0000000000ul) | (v3 & 0x00FF000000000000ul)) >> 16)
                | (v4 & 0x0000000000FF0000ul)
                | ((v4 & 0x000000000000FF00ul) << 32)
                | ((v3 & 0xFF00000000000000ul) >> 8)
                | (v4 << 56);

            v2 += (((v3 & 0x00000000FF000000ul) | (v4 & 0x000000FF00000000ul)) >> 24)
                | (v3 & 0x0000000000FF0000ul)
                | ((v3 & 0x0000FF0000000000ul) >> 16)
                | ((v3 & 0x000000000000FF00ul) << 24)
                | ((v4 & 0x00FF000000000000ul) >> 8)
                | ((v3 & 0x00000000000000FFul) << 48)
                | (v4 & 0xFF00000000000000ul);
        }
        #endregion
    }

    /// <summary>Represents the HighwayHash64 implementation. This class cannot be derived.</summary>
    public sealed class HighwayHash64 : HighwayHash
    {
        #region Properties
        /// <inheritdoc/>
        protected override UInt32 P => 4;

        /// <inheritdoc/>
        public override Int32 Length => 64;
        #endregion

        #region Constructors
        /// <summary>Initializes a new instance of <see cref="T:FastHashes.HighwayHash64"/> using a value of <c>0</c> for all the seeds.</summary>
        public HighwayHash64() : base(0ul, 0ul, 0ul, 0ul) { }

        /// <summary>Initializes a new instance of <see cref="T:FastHashes.HighwayHash64"/> using the specified value for all the seeds.</summary>
        /// <param name="seed">The <see cref="T:System.UInt64"/> seed used by the hashing algorithm.</param>
        public HighwayHash64(UInt64 seed) : base(seed, seed, seed, seed) { }

        /// <summary>Initializes a new instance of <see cref="T:FastHashes.HighwayHash64"/> using the specified <see cref="T:System.UInt64"/> seeds.</summary>
        /// <param name="seeds">A <see cref="T:System.UInt64"/>[] of seeds used by the hashing algorithm.</param>
        /// <exception cref="T:System.ArgumentException">Thrown when the number of seeds in <paramref name="seeds">seeds</paramref> is not equal to 4.</exception>
        /// <exception cref="T:System.ArgumentNullException">Thrown when <paramref name="seeds">seeds</paramref> is null.</exception>
        public HighwayHash64(UInt64[] seeds) : base(seeds) { }

        /// <summary>Initializes a new instance of <see cref="T:FastHashes.HighwayHash64"/> using the specified seeds.</summary>
        /// <param name="seed1">The first <see cref="T:System.UInt64"/> seed used by the hashing algorithm.</param>
        /// <param name="seed2">The second <see cref="T:System.UInt64"/> seed used by the hashing algorithm.</param>
        /// <param name="seed3">The third <see cref="T:System.UInt64"/> seed used by the hashing algorithm.</param>
        /// <param name="seed4">The fourth <see cref="T:System.UInt64"/> seed used by the hashing algorithm.</param>
        public HighwayHash64(UInt64 seed1, UInt64 seed2, UInt64 seed3, UInt64 seed4) : base(seed1, seed2, seed3, seed4) { }
        #endregion

        #region Methods
        /// <inheritdoc/>
        protected override Byte[] GetHash(UInt64[] m0, UInt64[] m1, UInt64[] v0, UInt64[] v1)
        {
            Byte[] result = new Byte[8];

            unsafe
            {
                fixed (Byte* pointer = result)
                    *((UInt64*)pointer) = v0[0] + v1[0] + m0[0] + m1[0];
            }

            return result;
        }
        #endregion
    }

    /// <summary>Represents the HighwayHash128 implementation. This class cannot be derived.</summary>
    public sealed class HighwayHash128 : HighwayHash
    {
        #region Properties
        /// <inheritdoc/>
        protected override UInt32 P => 6;

        /// <inheritdoc/>
        public override Int32 Length => 128;
        #endregion

        #region Constructors
        /// <summary>Initializes a new instance of <see cref="T:FastHashes.HighwayHash128"/> using a value of <c>0</c> for all the seeds.</summary>
        public HighwayHash128() : base(0ul, 0ul, 0ul, 0ul) { }

        /// <summary>Initializes a new instance of <see cref="T:FastHashes.HighwayHash128"/> using the specified value for all the seeds.</summary>
        /// <param name="seed">The <see cref="T:System.UInt64"/> seed used by the hashing algorithm.</param>
        public HighwayHash128(UInt64 seed) : base(seed, seed, seed, seed) { }

        /// <summary>Initializes a new instance of <see cref="T:FastHashes.HighwayHash128"/> using the specified seeds.</summary>
        /// <param name="seeds">A <see cref="T:System.UInt64"/>[] of seeds used by the hashing algorithm.</param>
        /// <exception cref="T:System.ArgumentException">Thrown when the number of seeds in <paramref name="seeds">seeds</paramref> is not equal to 4.</exception>
        /// <exception cref="T:System.ArgumentNullException">Thrown when <paramref name="seeds">seeds</paramref> is null.</exception>
        public HighwayHash128(UInt64[] seeds) : base(seeds) { }

        /// <summary>Initializes a new instance of <see cref="T:FastHashes.HighwayHash128"/> using the specified seeds.</summary>
        /// <param name="seed1">The first <see cref="T:System.UInt64"/> seed used by the hashing algorithm.</param>
        /// <param name="seed2">The second <see cref="T:System.UInt64"/> seed used by the hashing algorithm.</param>
        /// <param name="seed3">The third <see cref="T:System.UInt64"/> seed used by the hashing algorithm.</param>
        /// <param name="seed4">The fourth <see cref="T:System.UInt64"/> seed used by the hashing algorithm.</param>
        public HighwayHash128(UInt64 seed1, UInt64 seed2, UInt64 seed3, UInt64 seed4) : base(seed1, seed2, seed3, seed4) { }
        #endregion

        #region Methods
        /// <inheritdoc/>
        protected override Byte[] GetHash(UInt64[] m0, UInt64[] m1, UInt64[] v0, UInt64[] v1)
        {
            Byte[] result = new Byte[16];

            unsafe
            {
                fixed (Byte* pin = result)
                {
                    UInt64* pointer = (UInt64*)pin;
                    pointer[0] = v0[0] + m0[0] + v1[2] + m1[2];
                    pointer[1] = v0[1] + m0[1] + v1[3] + m1[3];
                }
            }

            return result;
        }
        #endregion
    }

    /// <summary>Represents the HighwayHash256 implementation. This class cannot be derived.</summary>
    public sealed class HighwayHash256 : HighwayHash
    {
        #region Properties
        /// <inheritdoc/>
        protected override UInt32 P => 10;

        /// <inheritdoc/>
        public override Int32 Length => 256;
        #endregion

        #region Constructors
        /// <summary>Initializes a new instance of <see cref="T:FastHashes.HighwayHash256"/> using a value of <c>0</c> for all the seeds.</summary>
        public HighwayHash256() : base(0ul, 0ul, 0ul, 0ul) { }

        /// <summary>Initializes a new instance of <see cref="T:FastHashes.HighwayHash256"/> using the specified value for all the four seeds.</summary>
        /// <param name="seed">The <see cref="T:System.UInt64"/> seed used by the hashing algorithm.</param>
        public HighwayHash256(UInt64 seed) : base(seed, seed, seed, seed) { }

        /// <summary>Initializes a new instance of <see cref="T:FastHashes.HighwayHash256"/> using the specified seeds.</summary>
        /// <param name="seeds">A <see cref="T:System.UInt64"/>[] of seeds used by the hashing algorithm.</param>
        /// <exception cref="T:System.ArgumentException">Thrown when the number of seeds in <paramref name="seeds">seeds</paramref> is not equal to 4.</exception>
        /// <exception cref="T:System.ArgumentNullException">Thrown when <paramref name="seeds">seeds</paramref> is null.</exception>
        public HighwayHash256(UInt64[] seeds) : base(seeds) { }

        /// <summary>Initializes a new instance of <see cref="T:FastHashes.HighwayHash256"/> using the specified seeds.</summary>
        /// <param name="seed1">The first <see cref="T:System.UInt64"/> seed used by the hashing algorithm.</param>
        /// <param name="seed2">The second <see cref="T:System.UInt64"/> seed used by the hashing algorithm.</param>
        /// <param name="seed3">The third <see cref="T:System.UInt64"/> seed used by the hashing algorithm.</param>
        /// <param name="seed4">The fourth <see cref="T:System.UInt64"/> seed used by the hashing algorithm.</param>
        public HighwayHash256(UInt64 seed1, UInt64 seed2, UInt64 seed3, UInt64 seed4) : base(seed1, seed2, seed3, seed4) { }
        #endregion

        #region Methods
        /// <inheritdoc/>
        protected override Byte[] GetHash(UInt64[] m0, UInt64[] m1, UInt64[] v0, UInt64[] v1)
        {
            MR(out UInt64 hash1, out UInt64 hash2, v0[0], m0[0], v0[1], m0[1], v1[0], m1[0], v1[1], m1[1]);
            MR(out UInt64 hash3, out UInt64 hash4, v0[2], m0[2], v0[3], m0[3], v1[2], m1[2], v1[3], m1[3]);

            Byte[] result = new Byte[32];

            unsafe
            {
                fixed (Byte* pin = result)
                {
                    UInt64* pointer = (UInt64*)pin;
                    pointer[0] = hash1;
                    pointer[1] = hash2;
                    pointer[2] = hash3;
                    pointer[3] = hash4;
                }
            }

            return result;
        }
        #endregion

        #region Methods (Static)
        private static void MR(out UInt64 hash1, out UInt64 hash2, UInt64 v1, UInt64 m1, UInt64 v2, UInt64 m2, UInt64 v3, UInt64 m3, UInt64 v4, UInt64 m4)
        {
            UInt64 a0 = v1 + m1;
            UInt64 a1 = v2 + m2;
            UInt64 a2 = v3 + m3;
            UInt64 a3 = (v4 + m4) & 0x3FFFFFFFFFFFFFFFul;

            hash1 = a0 ^ (a2 << 1) ^ (a2 << 2);
            hash2 = a1 ^ ((a3 << 1) | (a2 >> 63)) ^ ((a3 << 2) | (a2 >> 62));
        }
        #endregion
    }
}