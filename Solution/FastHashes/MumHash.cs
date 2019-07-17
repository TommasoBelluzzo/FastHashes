#region Using Directives
using System;
using System.Runtime.CompilerServices;
#endregion

namespace FastHashes
{
    /// <summary>Represents the MumHash implementation. This class cannot be derived.</summary>
    public sealed class MumHash : Hash
    {
        #region Constants
        private const UInt64 BSP = 0xC42B5E2E6480B23Bul;
        private const UInt64 FP1 = 0xA9A7AE7CEFF79F3Ful;
        private const UInt64 FP2 = 0xAF47D47C99B1461Bul;
        private const UInt64 UP = 0x7B51EC3D22F7096Ful;
        private const UInt64 TP = 0xAF47D47C99B1461Bul;
        private static readonly UInt64[] P =
        {
            0x9EBDCAE10D981691ul, 0x32B9B9B97A27AC7Dul, 0x29B5584D83D35BBDul, 0x4B04E0E61401255Ful,
            0x25E8F7B1F1C9D027ul, 0x80D4C8C000F3E881ul, 0xBD1255431904B9DDul, 0x8A3BD4485EEE6D81ul,
            0x3BC721B2AAD05197ul, 0x71B1A19B907D6E33ul, 0x525E6C1084A8534Bul, 0x9E4C2CD340C1299Ful,
            0xDE3ADD92E94CAA37ul, 0x7E14EADB1F65311Dul, 0x3F5AA40F89812853ul, 0x33B15A3B587D15C9ul,
        };
        #endregion

        #region Members
        private readonly UInt64 m_Seed;
        #endregion

        #region Properties
        /// <inheritdoc/>
        public override Int32 Length => 64;

        /// <summary>Gets the seed used by the hashing algorithm.</summary>
        /// <value>An <see cref="T:System.UInt64"/> value.</value>
        public UInt64 Seed => m_Seed;
        #endregion

        #region Constructors
        /// <summary>Initializes a new instance using the specified seed.</summary>
        /// <param name="seed">The <see cref="T:System.UInt64"/> seed used by the hashing algorithm.</param>
        public MumHash(UInt32 seed)
        {
            m_Seed = seed;
        }

        /// <summary>Initializes a new instance using a seed value of <c>0</c>.</summary>
        public MumHash() : this(0u) { }
        #endregion

        #region Methods
        /// <inheritdoc/>
        protected override Byte[] ComputeHashInternal(Byte[] buffer, Int32 offset, Int32 count)
        {
            UInt64 hash = Mum(m_Seed + (UInt64)count, BSP);

            if (count == 0)
                goto Finalize;

            unsafe
            {
                fixed (Byte* pin = &buffer[offset])
                {
                    Byte* pointer = pin;

                    while (count > 32)
                    {
                        for (Int32 i = 0; i < 4; ++i)
                            hash ^= Mum(Read64(ref pointer), P[i]);

                        hash = Mum(hash, UP);

                        count -= 32;
                    }

                    Int32 blocks = count / 8;
                    Int32 remainder = count & 7;

                    for (Int32 i = 0; i < blocks; ++i)
                        hash ^= Mum(Read64(ref pointer), P[i]);

                    UInt64 v = 0ul;

                    switch (remainder)
                    {
                        case 7:
                            v = Read32(ref pointer);
                            v |= (UInt64)pointer[0] << 32;
                            v |= (UInt64)pointer[1] << 40;
                            v |= (UInt64)pointer[2] << 48;
                            break;

                        case 6:
                            v = Read32(ref pointer);
                            v |= (UInt64)pointer[0] << 32;
                            v |= (UInt64)pointer[1] << 40;
                            break;

                        case 5:
                            v = Read32(ref pointer);
                            v |= (UInt64)pointer[0] << 32;
                            break;

                        case 4:
                            v = Read32(ref pointer);
                            break;

                        case 3:
                            v = pointer[0];
                            v |= (UInt64)pointer[1] << 8;
                            v |= (UInt64)pointer[2] << 16;
                            break;

                        case 2:
                            v = pointer[0];
                            v |= (UInt64)pointer[1] << 8;
                            break;

                        case 1:
                            v = pointer[0];
                            break;
                    }

                    hash ^= Mum(v, TP);
                }
            }

            Finalize:

            hash ^= Mum(hash, FP1);
            hash ^= Mum(hash, FP2);

            Byte[] result = new Byte[8];

            unsafe
            {
                fixed (Byte* pointer = result)
                    *((UInt64*)pointer) = hash;
            }

            return result;
        }
        #endregion

        #region Methods (Static)
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static UInt64 Mum(UInt64 v1, UInt64 v2)
        {
            UInt64 hv1 = v1 >> 32;
            UInt64 lv1 = (UInt32) v1;

            UInt64 hv2 = v2 >> 32;
            UInt64 lv2 = (UInt32)v2;

            UInt64 rh = hv1 * hv2;
            UInt64 rl = lv1 * lv2;

            UInt64 rm0 = hv1 * lv2;
            UInt64 rm1 = hv2 * lv1;

            UInt64 lo = rl + (rm0 << 32) + (rm1 << 32);
            UInt64 hi = rh + (rm0 >> 32) + (rm1 >> 32);

            return hi + lo;
        }
        #endregion
    }
}