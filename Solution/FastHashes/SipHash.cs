#region Using Directives
using System;
using System.Runtime.CompilerServices;
#endregion

namespace FastHashes
{
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
        public override Int32 Length => 32;
        #endregion

        #region Constructors
        public HalfSipHash(UInt64 seed1, UInt64 seed2)
        {
            m_Seed1 = (UInt32)(seed1 - (seed1 >> 32));
            m_Seed2 = (UInt32)(seed2 - (seed2 >> 32));
        }

        public HalfSipHash() : this(0ul, 0ul) { }

        public HalfSipHash(UInt64 seed) : this(seed, seed) { }
        #endregion

        #region Methods
        protected override Byte[] ComputeHashInternal(Byte[] data, Int32 offset, Int32 length)
        {
            UInt32 b = (UInt32)length << 24;

            UInt32[] v =
            {
                m_Seed1,
                m_Seed2,
                I0 ^ m_Seed1,
                I1 ^ m_Seed2
            };

            if (length == 0)
                goto Finalize;

            unsafe
            {
                fixed (Byte* pin = &data[offset])
                {
                    Byte* pointer = pin;

                    Int32 blocks = length / 4;
                    Int32 remainder = length & 3;

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
        public override Int32 Length => 64;
        #endregion

        #region Constructors
        public SipHash(SipHashVariant variant, UInt64 seed1, UInt64 seed2)
        {
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

        public SipHash() : this(SipHashVariant.V24, 0ul, 0ul) { }

        public SipHash(SipHashVariant variant) : this(variant, 0ul, 0ul) { }

        public SipHash(SipHashVariant variant, UInt64 seed) : this(variant, seed, seed) { }

        public SipHash(UInt64 seed) : this(SipHashVariant.V24, seed, seed) { }

        public SipHash(UInt64 seed1, UInt64 seed2) : this(SipHashVariant.V24, seed1, seed2) { }
        #endregion

        #region Methods
        protected override Byte[] ComputeHashInternal(Byte[] data, Int32 offset, Int32 length)
        {
            UInt64 b = (UInt64)(length & 0x000000FF) << 56;

            UInt64[] v =
            {
                m_Seed1 ^ K0,
                m_Seed2 ^ K1,
                m_Seed1 ^ K2,
                m_Seed2 ^ K3
            };

            if (length == 0)
                goto Finalize;

            unsafe
            {
                fixed (Byte* pin = &data[offset])
                {
                    Byte* pointer = pin;

                    Int32 blocks = length / 8;
                    Int32 remainder = length & 7;

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

        public override String ToString()
        {
            if (m_Variant == SipHashVariant.V13)
                return String.Concat(GetType().Name, "_13");

            return String.Concat(GetType().Name, "_24");
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