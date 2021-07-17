#region Using Directives
using System;
using System.Runtime.CompilerServices;
#endregion

namespace FastHashes
{
    /// <summary>Represents the FastPositiveHash implementation. This class cannot be derived.</summary>
    public sealed class FastPositiveHash : Hash
    {
        #region Members
        private readonly Engine m_Engine;
        #endregion

        #region Properties
        /// <summary>Gets the variant of the hashing algorithm.</summary>
        /// <value>An enumerator value of type <see cref="T:FastHashes.FastPositiveHashVariant"/>.</value>
        public FastPositiveHashVariant Variant => m_Engine.Variant;

        /// <inheritdoc/>
        public override Int32 Length => 64;

        /// <summary>Gets the seed used by the hashing algorithm.</summary>
        /// <value>An <see cref="T:System.UInt64"/> value.</value>
        public UInt64 Seed => m_Engine.Seed;
        #endregion

        #region Constructors
        /// <summary>Initializes a new instance using the specified variant and seed.</summary>
        /// <param name="variant">The enumerator value of type <see cref="T:FastHashes.FastPositiveHashVariant"/> representing the variant of the hashing algorithm.</param>
        /// <param name="seed">The <see cref="T:System.UInt64"/> seed used by the hashing algorithm.</param>
        /// <exception cref="T:System.ArgumentException ">Thrown when the value of <paramref name="variant">variant</paramref> is undefined.</exception>
        public FastPositiveHash(FastPositiveHashVariant variant, UInt64 seed)
        {
            if (!Enum.IsDefined(typeof(FastPositiveHashVariant), variant))
                throw new ArgumentException("Invalid variant specified.", nameof(variant));

            switch (variant)
            {
                case FastPositiveHashVariant.V0:
                    m_Engine = new EngineV0(seed);
                    break;

                case FastPositiveHashVariant.V1:
                    m_Engine = new EngineV1(seed);
                    break;

                default:
                    m_Engine = new EngineV2(seed);
                    break;
            }
        }

        /// <summary>Initializes a new instance using <see cref="F:FastHashes.FastPositiveHashVariant.V2"/> and a seed value of <c>0</c>.</summary>
        public FastPositiveHash() : this(FastPositiveHashVariant.V2, 0ul) { }

        /// <summary>Initializes a new instance using the specified variant and a seed value of <c>0</c>.</summary>
        /// <param name="variant">The enumerator value of type <see cref="T:FastHashes.FastPositiveHashVariant"/> representing the variant of the hashing algorithm.</param>
        /// <exception cref="T:System.ArgumentException ">Thrown when the value of <paramref name="variant">variant</paramref> is undefined.</exception>
        public FastPositiveHash(FastPositiveHashVariant variant) : this(variant, 0ul) { }

        /// <summary>Initializes a new instance using <see cref="F:FastHashes.FastPositiveHashVariant.V2"/> and the specified seed.</summary>
        /// <param name="seed">The <see cref="T:System.UInt64"/> seed used by the hashing algorithm.</param>
        public FastPositiveHash(UInt64 seed) : this(FastPositiveHashVariant.V2, seed) { }
        #endregion  

        #region Methods
        /// <inheritdoc/>
        protected override Byte[] ComputeHashInternal(Byte[] buffer, Int32 offset, Int32 count)
        {
            return m_Engine.ComputeHash(buffer, offset, count);
        }

        /// <inheritdoc/>
        public override String ToString()
        {
            return $"{GetType().Name}-{m_Engine.Name}";
        }
        #endregion

        #region Nesting (Classes)
        private abstract class Engine
        {
            #region Constants
            protected const UInt64 P640 = 0xEC99BF0D8372CAABul;
            protected const UInt64 P641 = 0x82434FE90EDCEF39ul;
            protected const UInt64 P642 = 0xD4F06DB99D67BE4Bul;
            protected const UInt64 P643 = 0xBD9CACC22C6E9571ul;
            protected const UInt64 P644 = 0x9C06FAF4D023E3ABul;
            protected const UInt64 P645 = 0xC060724A8424F345ul;
            protected const UInt64 P646 = 0xCB5AF53AE3AAAC31ul;
            #endregion

            #region Members
            protected readonly UInt64 m_Seed;
            #endregion

            #region Properties
            public UInt64 Seed => m_Seed;
            #endregion

            #region Properties (Abstract)
            public abstract FastPositiveHashVariant Variant { get; }

            public abstract String Name { get; }
            #endregion

            #region Constructors
            protected Engine(UInt64 seed)
            {
                m_Seed = seed;
            }
            #endregion

            #region Methods (Abstract)
            public abstract Byte[] ComputeHash(Byte[] data, Int32 offset, Int32 length);
            #endregion

            #region Methods (Static)
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            protected static void Mux(out UInt64 l, out UInt64 h, UInt64 v, UInt64 p)
            {
                UInt64 a = v >> 32;
                UInt64 b = v & 0x00000000FFFFFFFFul;
                UInt64 c = p >> 32;
                UInt64 d = p & 0x00000000FFFFFFFFul;

                UInt64 ad = a * d;
                UInt64 bd = b * d;
                UInt64 adbc = ad + (b * c);
                UInt64 carry = (adbc < ad) ? 1ul : 0ul;

                l = bd + (adbc << 32);
                h = (a * c) + (adbc >> 32) + (carry << 32) + ((l < bd) ? 1ul : 0ul);
            }
            #endregion
        }

        private sealed class EngineV0 : Engine
        {
            #region Constants
            private const UInt32 P320 = 0x92D78269;
            private const UInt32 P321 = 0xCA9B4735;
            private const UInt32 P322 = 0xA4ABA1C3;
            private const UInt32 P323 = 0xF6499843;
            private const UInt32 P324 = 0x86F0FD61;
            private const UInt32 P325 = 0xCA2DA6FB;
            private const UInt32 P326 = 0xC4BB3575;
            #endregion

            #region Constructors
            public EngineV0(UInt64 seed) : base(seed) { }
            #endregion

            #region Properties
            public override FastPositiveHashVariant Variant => FastPositiveHashVariant.V0;

            public override String Name => "V0";
            #endregion

            #region Methods
            public override Byte[] ComputeHash(Byte[] data, Int32 offset, Int32 length)
            {
                UInt32 unsignedLength = (UInt32)length;
                UInt32 a = RotateRight(unsignedLength, 17) + (UInt32)m_Seed;
                UInt32 b = unsignedLength ^ (UInt32)(m_Seed >> 32);

                if (length == 0)
                    goto Finalize;

                unsafe
                {
                    fixed (Byte* pin = &data[offset])
                    {
                        Byte* pointer = pin;

                        Int32 blocks, remainder;

                        if (length > 16)
                        {
                            blocks = length / 16;
                            remainder = length & 15;
                        }
                        else
                        {
                            blocks = 0;
                            remainder = length;
                        }

                        if (blocks > 0)
                        {
                            UInt32 c = ~a;
                            UInt32 d = RotateRight(b, 5);

                            while (blocks-- > 0)
                            {
                                UInt32[] w =
                                {
                                    Read32(ref pointer),
                                    Read32(ref pointer),
                                    Read32(ref pointer),
                                    Read32(ref pointer)
                                };

                                MixB(ref a, ref b, ref c, ref d, w);
                            }

                            c += a;
                            d += b;
                            a ^= P326 * (RotateRight(c, 16) + d);
                            b ^= P325 * (c + RotateRight(d, 16));
                        }

                        switch (remainder)
                        {
                            case 16:
                            case 15:
                            case 14:
                            case 13:
                                MixT(ref a, ref b, Read32(ref pointer), P324);
                                goto case 9;

                            case 12:
                            case 11:
                            case 10:
                            case 9:
                                MixT(ref b, ref a, Read32(ref pointer), P323);
                                goto case 5;

                            case 8:
                            case 7:
                            case 6:
                            case 5:
                                MixT(ref a, ref b, Read32(ref pointer), P322);
                                goto case 1;

                            case 4:
                            case 3:
                            case 2:
                            case 1:
                                UInt32 v = Read32(ref pointer) & (~0u >> (((4 - remainder) & 3) << 3));
                                MixT(ref b, ref a, v, P321);
                                break;
                        }
                    }
                }

Finalize:

                UInt64 hash = (b ^ RotateRight(a, 13)) | ((UInt64)a << 32);
                hash *= P640;
                hash ^= hash >> 41;
                hash *= P644;
                hash ^= hash >> 47;
                hash *= P646;

                Byte[] result = ToByteArray64(hash);

                return result;
            }
            #endregion

            #region Methods (Static)
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static void MixB(ref UInt32 a, ref UInt32 b, ref UInt32 c, ref UInt32 d, UInt32[] w)
            {
                UInt32 d13 = w[1] + RotateRight(w[3] + d, 17);
                UInt32 c02 = w[0] ^ RotateRight(w[2] + c, 11);

                d ^= RotateRight(a + w[0], 3);
                c ^= RotateRight(b + w[1], 7);
                b = P321 * (c02 + w[3]);
                a = P320 * (d13 ^ w[2]);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static void MixT(ref UInt32 v1, ref UInt32 v2, UInt32 v3, UInt32 p)
            {
                UInt64 l = (v2 + v3) * (UInt64)p;

                v1 ^= (UInt32)l;
                v2 += (UInt32)(l >> 32);
            }
            #endregion
        }

        private sealed class EngineV1 : Engine
        {
            #region Properties
            public override FastPositiveHashVariant Variant => FastPositiveHashVariant.V1;

            public override String Name => "V1";
            #endregion

            #region Constructors
            public EngineV1(UInt64 seed) : base(seed) { }
            #endregion

            #region Methods
            public override Byte[] ComputeHash(Byte[] data, Int32 offset, Int32 length)
            {
                UInt64 unsignedLength = (UInt64)length;
                UInt64 a = m_Seed;
                UInt64 b = unsignedLength;

                if (length == 0)
                    goto Finalize;

                unsafe
                {
                    fixed (Byte* pin = &data[offset])
                    {
                        Byte* pointer = pin;

                        Int32 blocks, remainder;

                        if (length > 32)
                        {
                            blocks = length / 32;
                            remainder = length & 31;
                        }
                        else
                        {
                            blocks = 0;
                            remainder = length;
                        }

                        if (blocks > 0)
                        {
                            UInt64 c = RotateRight(unsignedLength, 17) + m_Seed;
                            UInt64 d = unsignedLength ^ RotateRight(m_Seed, 17);

                            while (blocks-- > 0)
                            {
                                UInt64[] w =
                                {
                                    Read64(ref pointer),
                                    Read64(ref pointer),
                                    Read64(ref pointer),
                                    Read64(ref pointer)
                                };

                                MixB(ref a, ref b, ref c, ref d, w);
                            }

                            a ^= P646 * (RotateRight(c, 17) + d);
                            b ^= P645 * (c + RotateRight(d, 17));
                        }

                        switch (remainder)
                        {
                            case 32:
                            case 31:
                            case 30:
                            case 29:
                            case 28:
                            case 27:
                            case 26:
                            case 25:
                                b += MixT(Read64(ref pointer), P644);
                                goto case 17;

                            case 24:
                            case 23:
                            case 22:
                            case 21:
                            case 20:
                            case 19:
                            case 18:
                            case 17:
                                a += MixT(Read64(ref pointer), P643);
                                goto case 9;
    
                            case 16:
                            case 15:
                            case 14:
                            case 13:
                            case 12:
                            case 11:
                            case 10:
                            case 9:
                                b += MixT(Read64(ref pointer), P642);
                                goto case 1;

                            case 8:
                            case 7:
                            case 6:
                            case 5:
                            case 4:
                            case 3:
                            case 2:
                            case 1:
                                UInt64 v = Read64(ref pointer) & (~0ul >> (((8 - remainder) & 7) << 3));
                                a += MixT(v, P641);
                                break;
                        }
                    }
                }

Finalize:

                UInt64 h0 = (a ^ b) * P640;
                UInt64 h1 = MixT(RotateRight(a + b, 17), P644) + (h0 ^ RotateRight(h0, 41));

                Byte[] result = ToByteArray64(h1);

                return result;
            }
            #endregion

            #region Methods (Static)
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static void MixB(ref UInt64 a, ref UInt64 b, ref UInt64 c, ref UInt64 d, UInt64[] w)
            {
                UInt64 d02 = w[0] ^ RotateRight(w[2] + d, 17);
                UInt64 c13 = w[1] ^ RotateRight(w[3] + c, 17);

                d -= b ^ RotateRight(w[1], 31);
                c += a ^ RotateRight(w[0], 41);
                b ^= P640 * (c13 + w[2]);
                a ^= P641 * (d02 + w[3]);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static UInt64 MixT(UInt64 v, UInt64 p)
            {
                Mux(out UInt64 l, out UInt64 h, v, p);
                return l ^ h;
            }
            #endregion
        }

        private sealed class EngineV2 : Engine
        {
            #region Properties
            public override FastPositiveHashVariant Variant => FastPositiveHashVariant.V2;

            public override String Name => "V2";
            #endregion

            #region Constructors
            public EngineV2(UInt64 seed) : base(seed) { }
            #endregion

            #region Methods
            public override Byte[] ComputeHash(Byte[] data, Int32 offset, Int32 length)
            {
                UInt64 unsignedLength = (UInt64)length;
                UInt64 a = m_Seed;
                UInt64 b = unsignedLength;

                if (length == 0)
                    goto Finalize;

                unsafe
                {
                    fixed (Byte* pin = &data[offset])
                    {
                        Byte* pointer = pin;

                        Int32 blocks, remainder;

                        if (length > 32)
                        {
                            blocks = length / 32;
                            remainder = length & 31;
                        }
                        else
                        {
                            blocks = 0;
                            remainder = length;
                        }

                        if (blocks > 0)
                        {
                            UInt64 c = RotateRight(unsignedLength, 23) + ~m_Seed;
                            UInt64 d = ~unsignedLength + RotateRight(m_Seed, 19);

                            while (blocks-- > 0)
                            {
                                UInt64[] w =
                                {
                                    Read64(ref pointer),
                                    Read64(ref pointer),
                                    Read64(ref pointer),
                                    Read64(ref pointer)
                                };

                                MixB(ref a, ref b, ref c, ref d, w);
                            }

                            a ^= P646 * (c + RotateRight(d, 23));
                            b ^= P645 * (RotateRight(c, 19) + d);
                        }

                        switch (remainder)
                        {
                            case 32:
                            case 31:
                            case 30:
                            case 29:
                            case 28:
                            case 27:
                            case 26:
                            case 25:
                                MixT(ref a, ref b, Read64(ref pointer), P644);
                                goto case 17;

                            case 24:
                            case 23:
                            case 22:
                            case 21:
                            case 20:
                            case 19:
                            case 18:
                            case 17:
                                MixT(ref b, ref a, Read64(ref pointer), P643);
                                goto case 9;
    
                            case 16:
                            case 15:
                            case 14:
                            case 13:
                            case 12:
                            case 11:
                            case 10:
                            case 9:
                                MixT(ref a, ref b, Read64(ref pointer), P642);
                                goto case 1;

                            case 8:
                            case 7:
                            case 6:
                            case 5:
                            case 4:
                            case 3:
                            case 2:
                            case 1:
                                UInt64 v = Read64(ref pointer) & (~0ul >> (((8 - remainder) & 7) << 3));
                                MixT(ref b, ref a, v, P641);
                                break;
                        }
                    }
                }

Finalize:

                UInt64 h0 = (a + RotateRight(b, 41)) * P640;
                UInt64 h1 = (RotateRight(a, 23) + b) * P646;
                Mux(out UInt64 l, out UInt64 h, h0 ^ h1, P645);

                UInt64 h2 = l ^ h;

                Byte[] result = ToByteArray64(h2);

                return result;
            }
            #endregion

            #region Methods (Static)
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static void MixB(ref UInt64 a, ref UInt64 b, ref UInt64 c, ref UInt64 d, UInt64[] w)
            {
                UInt64 d02 = w[0] + RotateRight(w[2] + d, 56);
                UInt64 c13 = w[1] + RotateRight(w[3] + c, 19);

                d ^= b + RotateRight(w[1], 38);                
                c ^= a + RotateRight(w[0], 57);                
                b ^= P646 * (c13 + w[2]);                  
                a ^= P645 * (d02 + w[3]);                  
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static void MixT(ref UInt64 v1, ref UInt64 v2, UInt64 v3, UInt64 p)
            {
                Mux(out UInt64 l, out UInt64 h, v3 + v2, p);
                v1 ^= l;
                v2 += h;              
            }
            #endregion
        }
        #endregion
    }
}
