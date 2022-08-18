#region Using Directives
using System;
using System.Diagnostics.CodeAnalysis;
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
        [ExcludeFromCodeCoverage]
        public FastPositiveHashVariant Variant => m_Engine.Variant;

        /// <inheritdoc/>
        [ExcludeFromCodeCoverage]
        public override Int32 Length => 64;

        /// <summary>Gets the seed used by the hashing algorithm.</summary>
        /// <value>An <see cref="T:System.UInt64"/> value.</value>
        [ExcludeFromCodeCoverage]
        public UInt64 Seed => m_Engine.Seed;
        #endregion

        #region Constructors
        /// <summary>Initializes a new instance using the specified variant and seed.</summary>
        /// <param name="variant">The enumerator value of type <see cref="T:FastHashes.FastPositiveHashVariant"/> representing the variant of the hashing algorithm.</param>
        /// <param name="seed">The <see cref="T:System.UInt64"/> seed used by the hashing algorithm.</param>
        /// <exception cref="T:System.ArgumentException ">Thrown when the value of <paramref name="variant">variant</paramref> is undefined.</exception>
        [ExcludeFromCodeCoverage]
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

                case FastPositiveHashVariant.V2:
                    m_Engine = new EngineV2(seed);
                    break;

                default:
                    goto case FastPositiveHashVariant.V2;
            }
        }

        /// <summary>Initializes a new instance using <see cref="F:FastHashes.FastPositiveHashVariant.V2"/> and a seed value of <c>0</c>.</summary>
        [ExcludeFromCodeCoverage]
        public FastPositiveHash() : this(FastPositiveHashVariant.V2, 0ul) { }

        /// <summary>Initializes a new instance using the specified variant and a seed value of <c>0</c>.</summary>
        /// <param name="variant">The enumerator value of type <see cref="T:FastHashes.FastPositiveHashVariant"/> representing the variant of the hashing algorithm.</param>
        /// <exception cref="T:System.ArgumentException ">Thrown when the value of <paramref name="variant">variant</paramref> is undefined.</exception>
        [ExcludeFromCodeCoverage]
        public FastPositiveHash(FastPositiveHashVariant variant) : this(variant, 0ul) { }

        /// <summary>Initializes a new instance using <see cref="F:FastHashes.FastPositiveHashVariant.V2"/> and the specified seed.</summary>
        /// <param name="seed">The <see cref="T:System.UInt64"/> seed used by the hashing algorithm.</param>
        [ExcludeFromCodeCoverage]
        public FastPositiveHash(UInt64 seed) : this(FastPositiveHashVariant.V2, seed) { }
        #endregion  

        #region Methods
        /// <inheritdoc/>
        protected override Byte[] ComputeHashInternal(ReadOnlySpan<Byte> buffer)
        {
            return m_Engine.ComputeHash(buffer);
        }

        /// <inheritdoc/>
        [ExcludeFromCodeCoverage]
        public override String ToString()
        {
            return $"{GetType().Name}-{m_Engine.Name}";
        }
        #endregion

        #region Nested Classes
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
            public abstract FastPositiveHashVariant Variant { get; }

            public abstract String Name { get; }

            [ExcludeFromCodeCoverage]
            public UInt64 Seed => m_Seed;
            #endregion

            #region Constructors
            [ExcludeFromCodeCoverage]
            protected Engine(UInt64 seed)
            {
                m_Seed = seed;
            }
            #endregion

            #region Methods
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

            protected static UInt32 ExtractTail32(ReadOnlySpan<Byte> buffer, Int32 offset, Int32 remainder)
            {
                UInt32 t = 0u;

                if (BitConverter.IsLittleEndian)
                {
                    switch (remainder & 3)
                    {
                        case 0:
                            t = BinaryOperations.Read32(buffer, offset);
                            break;

                        case 1:
                            t = buffer[offset];
                            break;

                        case 2:
                            t += BinaryOperations.Read16(buffer, offset);
                            break;

                        case 3:
                            t = (UInt32)buffer[offset + 2] << 16;
                            goto case 2;
                    }
                }
                else
                {
                    switch (remainder & 3)
                    {
                        case 0:
                            t += buffer[offset + 3];
                            t <<= 8;
                            goto case 3;

                        case 1:
                            t += buffer[offset];
                            break;

                        case 2:
                            t += buffer[offset + 1];
                            t <<= 8;
                            goto case 1;

                        case 3:
                            t += buffer[offset + 2];
                            t <<= 8;
                            goto case 2;
                    }
                }

                return t;
            }

            protected static UInt64 ExtractTail64(ReadOnlySpan<Byte> buffer, Int32 offset, Int32 remainder)
            {
                UInt64 t = 0ul;

                if (BitConverter.IsLittleEndian)
                {
                    switch (remainder & 7)
                    {
                        case 0:
                            t = BinaryOperations.Read64(buffer, offset);
                            break;

                        case 7:
                            t = (UInt64)buffer[offset + 6] << 8;
                            goto case 6;

                        case 6:
                            t += buffer[offset + 5];
                            t <<= 8;
                            goto case 5;

                        case 5:
                            t += buffer[offset + 4];
                            t <<= 32;
                            goto case 4;

                        case 4:
                            t += BinaryOperations.Read32(buffer, offset);
                            break;

                        case 3:
                            t = (UInt64)buffer[offset + 2] << 16;
                            goto case 2;

                        case 2:
                            t += BinaryOperations.Read16(buffer, offset);
                            break;

                        case 1:
                            t = buffer[offset];
                            break;
                    }
                }
                else
                {
                    switch (remainder & 7)
                    {
                        case 0:
                            t = (UInt64)buffer[offset + 7] << 8;
                            goto case 7;

                        case 7:
                            t += buffer[offset + 6];
                            t <<= 8;
                            goto case 6;

                        case 6:
                            t += buffer[offset + 5];
                            t <<= 8;
                            goto case 5;

                        case 5:
                            t += buffer[offset + 4];
                            t <<= 8;
                            goto case 4;

                        case 4:
                            t += buffer[offset + 3];
                            t <<= 8;
                            goto case 3;

                        case 3:
                            t += buffer[offset + 2];
                            t <<= 8;
                            goto case 2;

                        case 2:
                            t += buffer[offset + 1];
                            t <<= 8;
                            goto case 1;

                        case 1:
                            t += buffer[offset];
                            break;
                    }
                }

                return t;
            }

            public abstract Byte[] ComputeHash(ReadOnlySpan<Byte> buffer);
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

            #region Properties
            [ExcludeFromCodeCoverage]
            public override FastPositiveHashVariant Variant => FastPositiveHashVariant.V0;

            [ExcludeFromCodeCoverage]
            public override String Name => "V0";
            #endregion

            #region Constructors
            [ExcludeFromCodeCoverage]
            public EngineV0(UInt64 seed) : base(seed) { }
            #endregion

            #region Methods
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static void MixB(ref UInt32 a, ref UInt32 b, ref UInt32 c, ref UInt32 d, UInt32[] w)
            {
                UInt32 d13 = w[1] + BinaryOperations.RotateRight(w[3] + d, 17);
                UInt32 c02 = w[0] ^ BinaryOperations.RotateRight(w[2] + c, 11);

                d ^= BinaryOperations.RotateRight(a + w[0], 3);
                c ^= BinaryOperations.RotateRight(b + w[1], 7);
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

            public override Byte[] ComputeHash(ReadOnlySpan<Byte> buffer)
            {
                Int32 offset = 0;
                Int32 count = buffer.Length;

                UInt32 length = (UInt32)count;
                UInt32 a = BinaryOperations.RotateRight(length, 17) + (UInt32)m_Seed;
                UInt32 b = length ^ (UInt32)(m_Seed >> 32);

                if (count == 0)
                    goto Finalize;

                Int32 blocks, remainder;

                if (count > 16)
                {
                    blocks = count / 16;
                    remainder = count & 15;
                }
                else
                {
                    blocks = 0;
                    remainder = count;
                }

                if (blocks > 0)
                {
                    UInt32 c = ~a;
                    UInt32 d = BinaryOperations.RotateRight(b, 5);

                    while (blocks-- > 0)
                    {
                        UInt32[] w = BinaryOperations.ReadArray32(buffer, offset, 4);
                        offset += 16;

                        MixB(ref a, ref b, ref c, ref d, w);
                    }

                    c += a;
                    d += b;
                    a ^= P326 * (BinaryOperations.RotateRight(c, 16) + d);
                    b ^= P325 * (c + BinaryOperations.RotateRight(d, 16));
                }

                switch (remainder)
                {
                    case 16:
                    case 15:
                    case 14:
                    case 13:
                        MixT(ref a, ref b, BinaryOperations.Read32(buffer, offset), P324);
                        offset += 4;
                        goto case 9;

                    case 12:
                    case 11:
                    case 10:
                    case 9:
                        MixT(ref b, ref a, BinaryOperations.Read32(buffer, offset), P323);
                        offset += 4;
                        goto case 5;

                    case 8:
                    case 7:
                    case 6:
                    case 5:
                        MixT(ref a, ref b, BinaryOperations.Read32(buffer, offset), P322);
                        offset += 4;
                        goto case 1;

                    case 4:
                    case 3:
                    case 2:
                    case 1:
                        UInt32 t = ExtractTail32(buffer, offset, remainder);
                        UInt32 v = t & (~0u >> (((4 - remainder) & 3) << 3));
                        MixT(ref b, ref a, v, P321);
                        break;
                }

                Finalize:

                UInt64 hash = (b ^ BinaryOperations.RotateRight(a, 13)) | ((UInt64)a << 32);
                hash *= P640;
                hash ^= hash >> 41;
                hash *= P644;
                hash ^= hash >> 47;
                hash *= P646;

                Byte[] result = BinaryOperations.ToArray64(hash);

                return result;
            }
            #endregion
        }

        private sealed class EngineV1 : Engine
        {
            #region Properties
            [ExcludeFromCodeCoverage]
            public override FastPositiveHashVariant Variant => FastPositiveHashVariant.V1;

            [ExcludeFromCodeCoverage]
            public override String Name => "V1";
            #endregion

            #region Constructors
            [ExcludeFromCodeCoverage]
            public EngineV1(UInt64 seed) : base(seed) { }
            #endregion

            #region Methods
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static void MixB(ref UInt64 a, ref UInt64 b, ref UInt64 c, ref UInt64 d, UInt64[] w)
            {
                UInt64 d02 = w[0] ^ BinaryOperations.RotateRight(w[2] + d, 17);
                UInt64 c13 = w[1] ^ BinaryOperations.RotateRight(w[3] + c, 17);

                d -= b ^ BinaryOperations.RotateRight(w[1], 31);
                c += a ^ BinaryOperations.RotateRight(w[0], 41);
                b ^= P640 * (c13 + w[2]);
                a ^= P641 * (d02 + w[3]);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static UInt64 MixT(UInt64 v, UInt64 p)
            {
                Mux(out UInt64 l, out UInt64 h, v, p);
                return l ^ h;
            }

            public override Byte[] ComputeHash(ReadOnlySpan<Byte> buffer)
            {
                Int32 offset = 0;
                Int32 count = buffer.Length;

                UInt64 length = (UInt64)count;
                UInt64 a = m_Seed;
                UInt64 b = length;

                if (count == 0)
                    goto Finalize;

                Int32 blocks, remainder;

                if (count > 32)
                {
                    blocks = count / 32;
                    remainder = count & 31;
                }
                else
                {
                    blocks = 0;
                    remainder = count;
                }

                if (blocks > 0)
                {
                    UInt64 c = BinaryOperations.RotateRight(length, 17) + m_Seed;
                    UInt64 d = length ^ BinaryOperations.RotateRight(m_Seed, 17);

                    while (blocks-- > 0)
                    {
                        UInt64[] w = BinaryOperations.ReadArray64(buffer, offset, 4);
                        offset += 32;

                        MixB(ref a, ref b, ref c, ref d, w);
                    }

                    a ^= P646 * (BinaryOperations.RotateRight(c, 17) + d);
                    b ^= P645 * (c + BinaryOperations.RotateRight(d, 17));
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
                        b += MixT(BinaryOperations.Read64(buffer, offset), P644);
                        offset += 8;
                        goto case 17;

                    case 24:
                    case 23:
                    case 22:
                    case 21:
                    case 20:
                    case 19:
                    case 18:
                    case 17:
                        a += MixT(BinaryOperations.Read64(buffer, offset), P643);
                        offset += 8;
                        goto case 9;

                    case 16:
                    case 15:
                    case 14:
                    case 13:
                    case 12:
                    case 11:
                    case 10:
                    case 9:
                        b += MixT(BinaryOperations.Read64(buffer, offset), P642);
                        offset += 8;
                        goto case 1;

                    case 8:
                    case 7:
                    case 6:
                    case 5:
                    case 4:
                    case 3:
                    case 2:
                    case 1:
                        UInt64 t = ExtractTail64(buffer, offset, remainder);
                        UInt64 v = t & (~0ul >> (((8 - remainder) & 7) << 3));
                        a += MixT(v, P641);
                        break;
                }

                Finalize:

                UInt64 h0 = (a ^ b) * P640;

                UInt64 hash = MixT(BinaryOperations.RotateRight(a + b, 17), P644) + (h0 ^ BinaryOperations.RotateRight(h0, 41));
                Byte[] result = BinaryOperations.ToArray64(hash);

                return result;
            }
            #endregion
        }

        private sealed class EngineV2 : Engine
        {
            #region Properties
            [ExcludeFromCodeCoverage]
            public override FastPositiveHashVariant Variant => FastPositiveHashVariant.V2;

            [ExcludeFromCodeCoverage]
            public override String Name => "V2";
            #endregion

            #region Constructors
            [ExcludeFromCodeCoverage]
            public EngineV2(UInt64 seed) : base(seed) { }
            #endregion

            #region Methods
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static void MixB(ref UInt64 a, ref UInt64 b, ref UInt64 c, ref UInt64 d, UInt64[] w)
            {
                UInt64 d02 = w[0] + BinaryOperations.RotateRight(w[2] + d, 56);
                UInt64 c13 = w[1] + BinaryOperations.RotateRight(w[3] + c, 19);

                d ^= b + BinaryOperations.RotateRight(w[1], 38);                
                c ^= a + BinaryOperations.RotateRight(w[0], 57);                
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

            public override Byte[] ComputeHash(ReadOnlySpan<Byte> buffer)
            {
                Int32 offset = 0;
                Int32 count = buffer.Length;

                UInt64 length = (UInt64)count;
                UInt64 a = m_Seed;
                UInt64 b = length;

                if (count == 0)
                    goto Finalize;

                Int32 blocks, remainder;

                if (count > 32)
                {
                    blocks = count / 32;
                    remainder = count & 31;
                }
                else
                {
                    blocks = 0;
                    remainder = count;
                }

                if (blocks > 0)
                {
                    UInt64 c = BinaryOperations.RotateRight(length, 23) + ~m_Seed;
                    UInt64 d = ~length + BinaryOperations.RotateRight(m_Seed, 19);

                    while (blocks-- > 0)
                    {
                        UInt64[] w = BinaryOperations.ReadArray64(buffer, offset, 4);
                        offset += 32;

                        MixB(ref a, ref b, ref c, ref d, w);
                    }

                    a ^= P646 * (c + BinaryOperations.RotateRight(d, 23));
                    b ^= P645 * (BinaryOperations.RotateRight(c, 19) + d);
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
                        MixT(ref a, ref b, BinaryOperations.Read64(buffer, offset), P644);
                        offset += 8;
                        goto case 17;

                    case 24:
                    case 23:
                    case 22:
                    case 21:
                    case 20:
                    case 19:
                    case 18:
                    case 17:
                        MixT(ref b, ref a, BinaryOperations.Read64(buffer, offset), P643);
                        offset += 8;
                        goto case 9;

                    case 16:
                    case 15:
                    case 14:
                    case 13:
                    case 12:
                    case 11:
                    case 10:
                    case 9:
                        MixT(ref a, ref b, BinaryOperations.Read64(buffer, offset), P642);
                        offset += 8;
                        goto case 1;

                    case 8:
                    case 7:
                    case 6:
                    case 5:
                    case 4:
                    case 3:
                    case 2:
                    case 1:
                        UInt64 t = ExtractTail64(buffer, offset, remainder);
                        UInt64 v = t & (~0ul >> (((8 - remainder) & 7) << 3));
                        MixT(ref b, ref a, v, P641);
                        break;
                }

                Finalize:

                UInt64 h0 = (a + BinaryOperations.RotateRight(b, 41)) * P640;
                UInt64 h1 = (BinaryOperations.RotateRight(a, 23) + b) * P646;
                Mux(out UInt64 l, out UInt64 h, h0 ^ h1, P645);

                UInt64 hash = l ^ h;
                Byte[] result = BinaryOperations.ToArray64(hash);

                return result;
            }
            #endregion
        }
        #endregion
    }
}
