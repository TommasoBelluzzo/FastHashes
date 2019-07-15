#region Using Directives
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
#endregion

namespace FastHashes
{
    /// <summary>Represents the HalfSipHash implementation. This class cannot be derived.</summary>
    public sealed class HalfSipHash : Hash
    {
        #region Constants
        private const UInt32 I0 = 0x6C796765u;
        private const UInt32 I1 = 0x74656462u;
        #endregion

        #region Members
        private readonly UInt32 m_Seed1;
        private readonly UInt32 m_Seed2;
        #endregion

        #region Properties
        /// <inheritdoc/>
        public override Int32 Length => 32;

        /// <summary>Gets the first seed used by the hashing algorithm.</summary>
        /// <value>A <see cref="T:System.UInt64"/> value.</value>
        public UInt64 Seed1 => m_Seed1;

        /// <summary>Gets the second seed used by the hashing algorithm.</summary>
        /// <value>A <see cref="T:System.UInt64"/> value.</value>
        public UInt64 Seed2 => m_Seed2;
        #endregion

        #region Constructors
        /// <summary>Initializes a new instance of <see cref="T:FastHashes.HalfSipHash"/> using the specified seeds.</summary>
        /// <param name="seed1">The first <see cref="T:System.UInt64"/> seed used by the hashing algorithm.</param>
        /// <param name="seed2">The second <see cref="T:System.UInt64"/> seed used by the hashing algorithm.</param>
        public HalfSipHash(UInt64 seed1, UInt64 seed2)
        {
            m_Seed1 = (UInt32)(seed1 - (seed1 >> 32));
            m_Seed2 = (UInt32)(seed2 - (seed2 >> 32));
        }

        /// <summary>Initializes a new instance of <see cref="T:FastHashes.HalfSipHash"/> using a value of <c>0</c> for both seeds.</summary>
        public HalfSipHash() : this(0ul, 0ul) { }

        /// <summary>Initializes a new instance of <see cref="T:FastHashes.HalfSipHash"/> using the specified value for both seeds.</summary>
        /// <param name="seed">The <see cref="T:System.UInt64"/> seed used by the hashing algorithm.</param>
        public HalfSipHash(UInt64 seed) : this(seed, seed) { }
        #endregion

        #region Methods
        /// <inheritdoc/>
        protected override Byte[] ComputeHashInternal(Byte[] buffer, Int32 offset, Int32 count)
        {
            UInt32 b = (UInt32)count << 24;

            UInt32[] v =
            {
                m_Seed1,
                m_Seed2,
                I0 ^ m_Seed1,
                I1 ^ m_Seed2
            };

            if (count == 0)
                goto Finalize;

            unsafe
            {
                fixed (Byte* pin = &buffer[offset])
                {
                    Byte* pointer = pin;

                    Int32 blocks = count / 4;
                    Int32 remainder = count & 3;

                    while (blocks-- > 0)
                    {
                        UInt32 m = Read32(ref pointer);

                        v[3] ^= m;

                        for (Int32 i = 0; i < 2; ++i)
                            Mix(ref v);

                        v[0] ^= m;
                    }

                    switch (remainder)
                    {
                        case 3: b |= (UInt32)pointer[2] << 16; goto case 2;
                        case 2: b |= (UInt32)pointer[1] << 8; goto case 1;
                        case 1: b |= pointer[0]; break;
                    }
                }
            }

            Finalize:

            v[3] ^= b;

            for (Int32 i = 0; i < 2; ++i)
                Mix(ref v);

            v[0] ^= b;
            v[2] ^= 0x000000FFu;

            for (Int32 i = 0; i < 4; ++i)
                Mix(ref v);

            Byte[] result = new Byte[4];

            unsafe
            {
                fixed (Byte* pointer = result)
                    *((UInt32*)pointer) = v[1] ^ v[3];
            }

            return result;
        }
        #endregion

        #region Methods (Static)
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void Mix(ref UInt32[] v)
        {
            v[0] += v[1];
            v[1] = RotateLeft(v[1], 5);
            v[1] ^= v[0];
            v[0] = RotateLeft(v[0], 16);
            v[2] += v[3];    
            v[3] = RotateLeft(v[3], 8);
            v[3] ^= v[2];
            v[0] += v[3];
            v[3] = RotateLeft(v[3], 7);
            v[3] ^= v[0];
            v[2] += v[1];
            v[1] = RotateLeft(v[1], 13);
            v[1] ^= v[2];
            v[2] = RotateLeft(v[2], 16);
        }
        #endregion
    }

    /// <summary>Represents the SipHash implementation. This class cannot be derived.</summary>
    public sealed class SipHash : Hash
    {
        #region Constants
        private const UInt64 K0 = 0x736F6D6570736575ul;
        private const UInt64 K1 = 0x646F72616E646F6Dul;
        private const UInt64 K2 = 0x6C7967656E657261ul;
        private const UInt64 K3 = 0x7465646279746573ul;
        #endregion

        #region Members
        private readonly Int32 m_R1;
        private readonly Int32 m_R2;
        private readonly SipHashVariant m_Variant;
        private readonly UInt64 m_Seed1;
        private readonly UInt64 m_Seed2;
        #endregion

        #region Properties
        /// <inheritdoc/>
        public override Int32 Length => 64;

        /// <summary>Gets the variant of the hashing algorithm.</summary>
        /// <value>An enumeration value representing the variant of the hashing algorithm. See <see cref="T:FastHashes.SipHashVariant"/>.</value>
        public SipHashVariant Variant => m_Variant;

        /// <summary>Gets the first seed used by the hashing algorithm.</summary>
        /// <value>A <see cref="T:System.UInt64"/> value.</value>
        public UInt64 Seed1 => m_Seed1;

        /// <summary>Gets the second seed used by the hashing algorithm.</summary>
        /// <value>A <see cref="T:System.UInt64"/> value.</value>
        public UInt64 Seed2 => m_Seed2;
        #endregion

        #region Constructors
        /// <summary>Initializes a new instance of <see cref="T:FastHashes.SipHash"/> using the specified variant and seeds.</summary>
        /// <param name="variant">The variant of the hashing algorithm. See <see cref="T:FastHashes.SipHashVariant"/>.</param>
        /// <param name="seed1">The first <see cref="T:System.UInt64"/> seed used by the hashing algorithm.</param>
        /// <param name="seed2">The second <see cref="T:System.UInt64"/> seed used by the hashing algorithm.</param>
        /// <exception cref="T:System.ComponentModel.InvalidEnumArgumentException">Thrown when the value of <paramref name="variant">variant</paramref> is undefined.</exception>
        public SipHash(SipHashVariant variant, UInt64 seed1, UInt64 seed2)
        {
            if (!Enum.IsDefined(typeof(SipHashVariant), variant))
                throw new InvalidEnumArgumentException("Invalid variant specified.");

            if (variant == SipHashVariant.V13)
            {
                m_R1 = 1;
                m_R2 = 3;
            }
            else
            {
                m_R1 = 2;
                m_R2 = 4;
            }

            m_Variant = variant;
            m_Seed1 = seed1;
            m_Seed2 = seed2;
        }

        /// <summary>Initializes a new instance of <see cref="T:FastHashes.SipHash"/> using <see cref="F:FastHashes.SipHashVariant.V24"/> and a value of <c>0</c> for both seeds.</summary>
        public SipHash() : this(SipHashVariant.V24, 0ul, 0ul) { }

        /// <summary>Initializes a new instance of <see cref="T:FastHashes.SipHash"/> using the specified variant and a value of <c>0</c> for both seeds.</summary>
        /// <param name="variant">The variant of the hashing algorithm. See <see cref="T:FastHashes.SipHashVariant"/>.</param>
        /// <exception cref="T:System.ComponentModel.InvalidEnumArgumentException">Thrown when the value of <paramref name="variant">variant</paramref> is undefined.</exception>
        public SipHash(SipHashVariant variant) : this(variant, 0ul, 0ul) { }

        /// <summary>Initializes a new instance of <see cref="T:FastHashes.SipHash"/> using the specified variant and the specified value for both seeds.</summary>
        /// <param name="variant">The variant of the hashing algorithm. See <see cref="T:FastHashes.SipHashVariant"/>.</param>
        /// <param name="seed">The <see cref="T:System.UInt64"/> seed used by the hashing algorithm.</param>
        /// <exception cref="T:System.ComponentModel.InvalidEnumArgumentException">Thrown when the value of <paramref name="variant">variant</paramref> is undefined.</exception>
        public SipHash(SipHashVariant variant, UInt64 seed) : this(variant, seed, seed) { }

        /// <summary>Initializes a new instance of <see cref="T:FastHashes.SipHash"/> using <see cref="F:FastHashes.SipHashVariant.V24"/> and the specified value for both seeds.</summary>
        /// <param name="seed">The <see cref="T:System.UInt64"/> seed used by the hashing algorithm.</param>
        public SipHash(UInt64 seed) : this(SipHashVariant.V24, seed, seed) { }

        /// <summary>Initializes a new instance of <see cref="T:FastHashes.SipHash"/> using <see cref="F:FastHashes.SipHashVariant.V24"/> and the specified seeds.</summary>
        /// <param name="seed1">The first <see cref="T:System.UInt64"/> seed used by the hashing algorithm.</param>
        /// <param name="seed2">The second <see cref="T:System.UInt64"/> seed used by the hashing algorithm.</param>
        public SipHash(UInt64 seed1, UInt64 seed2) : this(SipHashVariant.V24, seed1, seed2) { }
        #endregion

        #region Methods
        /// <inheritdoc/>
        protected override Byte[] ComputeHashInternal(Byte[] buffer, Int32 offset, Int32 count)
        {
            UInt64 b = (UInt64)(count & 0x000000FF) << 56;

            UInt64[] v =
            {
                m_Seed1 ^ K0,
                m_Seed2 ^ K1,
                m_Seed1 ^ K2,
                m_Seed2 ^ K3
            };

            if (count == 0)
                goto Finalize;

            unsafe
            {
                fixed (Byte* pin = &buffer[offset])
                {
                    Byte* pointer = pin;

                    Int32 blocks = count / 8;
                    Int32 remainder = count & 7;

                    while (blocks-- > 0)
                    {
                        UInt64 m = Read64(ref pointer);

                        v[3] ^= m;

                        for (Int32 i = 0; i < m_R1; ++i)
                            Mix(ref v);

                        v[0] ^= m;
                    }

                    switch (remainder)
                    {
                        case 7: b |= (UInt64)pointer[6] << 48; goto case 6;
                        case 6: b |= (UInt64)pointer[5] << 40; goto case 5;
                        case 5: b |= (UInt64)pointer[4] << 32; goto case 4;
                        case 4: b |= (UInt64)pointer[3] << 24; goto case 3;
                        case 3: b |= (UInt64)pointer[2] << 16; goto case 2;
                        case 2: b |= (UInt64)pointer[1] << 8; goto case 1;
                        case 1: b |= pointer[0]; break;
                    }
                }
            }

            Finalize:

            v[3] ^= b;

            for (Int32 i = 0; i < m_R1; ++i)
                Mix(ref v);

            v[0] ^= b;
            v[2] ^= 0x000000000000000000FFul;

            for (Int32 i = 0; i < m_R2; ++i)
                Mix(ref v);

            Byte[] result = new Byte[8];

            unsafe
            {
                fixed (Byte* pointer = result)
                    *((UInt64*)pointer) = v[0] ^ v[1] ^ v[2] ^ v[3];
            }

            return result;
        }

        /// <inheritdoc/>
        public override String ToString()
        {
            if (m_Variant == SipHashVariant.V13)
                return String.Concat(GetType().Name, "-13");

            return String.Concat(GetType().Name, "-24");
        }
        #endregion

        #region Methods (Static)
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void Mix(ref UInt64[] v)
        {
            v[0] += v[1];
            v[2] += v[3];
            v[1] = RotateLeft(v[1], 13);
            v[3] = RotateLeft(v[3], 16);
            v[1] ^= v[0];
            v[3] ^= v[2];
            v[0] = RotateLeft(v[0], 32);
            v[2] += v[1];
            v[0] += v[3];
            v[1] = RotateLeft(v[1], 17);
            v[3] = RotateLeft(v[3], 21);
            v[1] ^= v[2];
            v[3] ^= v[0];
            v[2] = RotateLeft(v[2], 32);
        }
        #endregion
    }
}