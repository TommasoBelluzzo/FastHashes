#region Using Directives
using System;
using System.Buffers.Binary;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
#endregion

namespace FastHashes
{
    /// <summary>Represents the base class from which all the LaneHash implementations must derive. This class is abstract.</summary>
    public abstract class LaneHash : Hash
    {
        #region Constants
        private const UInt64 SEED0 = 0x997F2CF145907CBAul;
        private const UInt64 SEED1 = 0xF76C91B34799A125ul;

        private static readonly UInt64[] CT =
        {
            0x3E8A1EB08B0E9E6Cul, 0x274B31BDC17715D7ul, 0x605C6055DA2FAF78ul, 0x94AB55AAF32555E6ul,
            0x4014E86362984857ul, 0xB610AB2A6A39CA55ul, 0xCEE84111345CCCB4ul, 0x93E9727CAF8654A1ul,
            0x2ABC6F631114EEB3ul, 0xF63118745DC5A92Bul, 0x1E93879B163E5CCEul, 0x5CCF246C33BAD6AFul,
            0x778695288153327Aul, 0xAFB94B6B98488F3Bul, 0x932128661BE8BFC4ul, 0x91A921FBCC09D861ul,
            0x3280EC5D60AC7C48ul, 0xB17585E95D5D84EFul, 0x881B65EB022326DCul, 0xC5AC96D3813E8923ul,
            0x3942F483F36F6D0Ful, 0x042084A482440B2Eul, 0x5E9B1F9E4AF0C869ul, 0x9A6CE9F64268C621ul,
            0xF088D3AB619C0C67ul, 0x682F54D8D2A0516Aul, 0xA33351AB28A70F96ul, 0xE43B7A136C0BEF6Eul,
            0x982AFB7E50F03BBAul, 0x7601AF391D65F1A1ul, 0x880E43823E59CA66ul, 0xB49F6F451986EE8Cul,
            0xBE5E8B3BC3A5847Dul, 0x7320C185D8756FE0ul, 0xA66AC1569F441A40ul, 0x06773F3662AAD34Eul,
            0x3D029B4272DFFE1Bul, 0x48120AD024D7D037ul, 0x9BC0F149D3EA0FDBul, 0x7B1B9980C9725307ul,
            0xF7DEE8F6D879D425ul, 0x3B4C79B61A50FEE3ul, 0xBA06C004BDE06C97ul, 0xC4609F40B64FA9C1ul,
            0x63246A19C29E5C5Eul, 0xB5536C3EAF6FFB68ul, 0x6FEC523BEBB23913ul, 0x2C95309B1F51FC6Dul,
            0x09BD5EAF444581CCul, 0xFD4A33DE04D0E3BEul, 0xB34B2E1907280F66ul, 0x0F74C84557A8CBC0ul,
            0xDBFBD3B9395F0BD2ul, 0x0A32601ABDC07955ul, 0x79722C40C600A1D6ul, 0xCCA31FFBFE259F67ul,
            0xF82232DBF8E9A58Eul, 0x156B61FDDF16753Cul, 0xAB5205ADC81E502Ful, 0x608723FDFAB53D32ul,
            0x82DF003E487B3153ul, 0xA08C6FCABB575C9Eul, 0xDB6917DF2E56871Aul, 0xC3FF7E28F6A842D5ul
        };

        private static readonly UInt64[] KS =
        {
            0xE8C6E4CE4113FB62ul, 0x014C7736DACA20EFul, 0xB41FF12BFE9E7ED0ul, 0x989190AE4DDADB95ul,
            0xA0D5936B718EADEAul, 0xE025C7AFD0D18ED0ul, 0xB794758E2F5B3C8Eul, 0x642B12F2FBE2F68Ful
        };

        private static readonly UInt64[] Q =
        {
            0x73554F8CC63267ACul, 0xC858CABBB0275B69ul, 0xA011F0B85DA3FFE1ul, 0xB88321FD983DFA10ul,
            0x5BD3D12D6CB5FC4Aul, 0x6545F8B679E4539Aul, 0x9097FB4BBC498ED2ul, 0x337ECBA4DAF2DDE1ul
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
        protected LaneHash(UInt64 seed)
        {
            m_Seed = seed;
        }
        #endregion

        #region Methods
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void Mul128(UInt64 a, UInt64 b, out UInt64 lo, out UInt64 hi)
        {
#if NET5_0_OR_GREATER
            hi = Math.BigMul(a, b, out lo);
#else
            UInt64 a0 = (UInt32)a;
            UInt64 a1 = a >> 32;

            UInt64 b0 = (UInt32)b;
            UInt64 b1 = b >> 32;

            UInt64 p0 = a0 * b0;
            UInt64 p1 = a0 * b1;
            UInt64 p2 = a1 * b0;
            UInt64 p3 = a1 * b1;

            UInt64 mid = unchecked((p0 >> 32) + (UInt32)p1 + (UInt32)p2);
            lo = (mid << 32) | (UInt32)p0;
            hi = unchecked(p3 + (p1 >> 32) + (p2 >> 32) + (mid >> 32));
#endif
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void Mum(ref UInt64 a, ref UInt64 b)
        {
            Mul128(a, b, out UInt64 lo, out UInt64 hi);
            a ^= lo;
            b ^= hi;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static UInt64 Step(UInt64 a, UInt64 w)
        {
            UInt64 x = a ^ w;
            UInt64 v = unchecked(x + (UInt64)(UInt32)x * (x >> 32));

            return v;
        }

        /// <summary>Represents an auxiliary hashing function used by derived classes.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected static UInt64 Fold(UInt64 a, UInt64 b)
        {
            Mul128(a, b, out UInt64 lo, out UInt64 hi);
            UInt64 v = lo ^ hi;

            return v;
        }

        /// <summary>Represents an auxiliary hashing function used by derived classes.</summary>
        protected static void HashLong(ReadOnlySpan<Byte> buffer, UInt64 seed, Span<UInt64> merged, out UInt64 ks)
        {
            Int32 count = buffer.Length;
            Int32 stripes = (count - 1) / 64;

            ks = Fold(seed ^ SEED0, SEED1);

            merged.Clear();

            if (stripes < 8)
            {
                for (Int32 stripe = 0; stripe < stripes; ++stripe)
                {
                    for (Int32 i = 0; i < 8; ++i)
                    {
                        UInt64 value = Step(unchecked(CT[(stripe * 8) + i] + ks), BinaryOperations.Read64(buffer, (stripe * 64) + (i * 8)));
                        merged[i] ^= Step(value, Q[i]);
                    }
                }

                for (Int32 i = 0; i < 8; ++i)
                {
                    UInt64 value = Step(unchecked(CT[(stripes * 8) + i] + ks), BinaryOperations.Read64(buffer, count - 64 + (i * 8)) ^ (UInt64)count);
                    merged[i] ^= Step(value, Q[i]);
                }
            }
            else
            {
                Span<UInt64> state = stackalloc UInt64[64];

                for (Int32 chain = 0; chain < 8; ++chain)
                {
                    for (Int32 i = 0; i < 8; ++i)
                        state[(chain * 8) + i] = Step(unchecked(CT[(chain * 8) + i] + ks), BinaryOperations.Read64(buffer, (chain * 64) + (i * 8)));
                }

                for (Int32 stripe = 8; stripe < stripes; ++stripe)
                {
                    Int32 chain = (stripe & 7) * 8;

                    for (Int32 i = 0; i < 8; ++i)
                        state[chain + i] = Step(state[chain + i], BinaryOperations.Read64(buffer, (stripe * 64) + (i * 8)));
                }

                Int32 lastChain = (stripes & 7) * 8;

                for (Int32 i = 0; i < 8; ++i)
                    state[lastChain + i] = Step(state[lastChain + i], BinaryOperations.Read64(buffer, count - 64 + (i * 8)) ^ (UInt64)count);

                for (Int32 chain = 0; chain < 8; ++chain)
                {
                    for (Int32 i = 0; i < 8; ++i)
                        merged[i] ^= Step(state[(chain * 8) + i], Q[i]);
                }
            }
        }

        /// <summary>Represents an auxiliary hashing function used by derived classes.</summary>
        protected static void HashShort(ReadOnlySpan<Byte> buffer, UInt64 seed, out UInt64 x, out UInt64 y)
        {
            Int32 count = buffer.Length;
            UInt64 z = (UInt64)count;

            UInt64 ks = Fold(seed ^ SEED0, SEED1);

            if (count <= 16)
            {
                UInt64 w0, w1;

                if (count == 0)
                {
                    w0 = 0ul;
                    w1 = 0ul;
                }
                else if (count <= 3)
                {
                    w0 = (UInt64)buffer[0] | ((UInt64)buffer[count / 2] << 8) | ((UInt64)buffer[count - 1] << 16);
                    w1 = 0ul;
                }
                else if (count <= 8)
                {
                    w0 = (UInt64)BinaryOperations.Read32(buffer, 0) | ((UInt64)BinaryOperations.Read32(buffer, count - 4) << 32);
                    w1 = 0ul;
                }
                else
                {
                    w0 = BinaryOperations.Read64(buffer, 0);
                    w1 = BinaryOperations.Read64(buffer, count - 8);
                }

                w0 ^= KS[0];
                w1 ^= KS[1] ^ z ^ ks;

                Mum(ref w0, ref w1);

                x = w0;
                y = w1;
            }
            else if (count <= 32)
            {
                UInt64 a0 = BinaryOperations.Read64(buffer, 0) ^ KS[0];
                UInt64 b0 = BinaryOperations.Read64(buffer, 8) ^ KS[1];
                UInt64 a1 = BinaryOperations.Read64(buffer, count - 16) ^ KS[2];
                UInt64 b1 = BinaryOperations.Read64(buffer, count - 8) ^ (KS[3] ^ z ^ ks);

                Mum(ref a0, ref b0);
                Mum(ref a1, ref b1);

                x = a0 ^ b1;
                y = b0 ^ a1;
            }
            else
            {
                UInt64 a0 = BinaryOperations.Read64(buffer, 0) ^ KS[0];
                UInt64 b0 = BinaryOperations.Read64(buffer, 8) ^ KS[1];
                UInt64 a1 = BinaryOperations.Read64(buffer, 16) ^ KS[2];
                UInt64 b1 = BinaryOperations.Read64(buffer, 24) ^ KS[3];
                UInt64 a2 = BinaryOperations.Read64(buffer, count - 32) ^ KS[4];
                UInt64 b2 = BinaryOperations.Read64(buffer, count - 24) ^ KS[5];
                UInt64 a3 = BinaryOperations.Read64(buffer, count - 16) ^ KS[6];
                UInt64 b3 = BinaryOperations.Read64(buffer, count - 8) ^ (KS[7] ^ z ^ ks);

                Mum(ref a0, ref b0);
                Mum(ref a1, ref b1);
                Mum(ref a2, ref b2);
                Mum(ref a3, ref b3);

                x = a0 ^ b1 ^ a2 ^ b3;
                y = b0 ^ a1 ^ b2 ^ a3;
            }
        }
        #endregion
    }

    /// <summary>Represents the LaneHash64 implementation. This class cannot be derived.</summary>
    public sealed class LaneHash64 : LaneHash
    {
        #region Constants
        private static readonly UInt64[] KF =
        {
            0x1CF00D9012B88888ul, 0x1CC38F68A05EAD4Ful
        };

        private static readonly UInt64[] S =
        {
            0xD0319F29223809A4ul, 0x896C4EEC98FA2E08ul, 0x7713D038E6212845ul, 0x6C0CE934CF6654BEul,
            0xDD507CC9B729ACC0ul, 0x170947B5B5D5843Ful, 0x1BFB7989D9D51692ul, 0xACB5DF98A60B31D1ul,
            0xB7DF1AD0DB72FD2Ful
        };
        #endregion

        #region Properties
        /// <inheritdoc/>
        [ExcludeFromCodeCoverage]
        public override Int32 Length => 64;
        #endregion

        #region Constructors
        /// <summary>Initializes a new instance using the specified seed.</summary>
        /// <param name="seed">The <see cref="T:System.UInt64"/> seed used by the hashing algorithm.</param>
        [ExcludeFromCodeCoverage]
        public LaneHash64(UInt64 seed) : base(seed) { }

        /// <summary>Initializes a new instance using a seed value of <c>0</c>.</summary>
        [ExcludeFromCodeCoverage]
        public LaneHash64() : this(0ul) { }
        #endregion

        #region Methods
        private UInt64 ComputeHashValue(ReadOnlySpan<Byte> buffer)
        {
            Int32 count = buffer.Length;

            UInt64 hash;

            if (buffer.Length <= 64)
            {
                HashShort(buffer, m_Seed, out UInt64 x, out UInt64 y);
                hash = Fold(x ^ (KF[0] ^ (UInt64)count), y ^ KF[1]);
            }
            else
            {
                Span<UInt64> merged = stackalloc UInt64[8];
                HashLong(buffer, m_Seed, merged, out UInt64 ks);

                UInt64 f1 = Fold(merged[0] ^ S[0], merged[1] ^ S[1]);
                UInt64 f2 = Fold(merged[2] ^ S[2], merged[3] ^ S[3]);
                UInt64 f3 = Fold(merged[4] ^ S[4], merged[5] ^ S[5]);
                UInt64 f4 = Fold(merged[6] ^ S[6], merged[7] ^ S[7]);
                UInt64 f = f1 ^ f2 ^ f3 ^ f4;

                hash = Fold(f ^ (UInt64)count, ks ^ S[8]);
            }

            return hash;
        }

        /// <inheritdoc/>
        protected override void ComputeHashInternal(ReadOnlySpan<Byte> buffer, Span<Byte> destination)
        {
            UInt64 hash = ComputeHashValue(buffer);
            BinaryPrimitives.WriteUInt64LittleEndian(destination, hash);
        }
        #endregion
    }

    /// <summary>Represents the LaneHash128 implementation. This class cannot be derived.</summary>
    public sealed class LaneHash128 : LaneHash
    {
        #region Constants
        private static readonly UInt64[] KF =
        {
            0x1CF00D9012B88888ul, 0x1CC38F68A05EAD4Ful, 0xADC1A8B391F1CFD1ul, 0x77170EBE18222F2Ful
        };

        private static readonly UInt64[] S =
        {
            0xD0319F29223809A4ul, 0x896C4EEC98FA2E08ul, 0x7713D038E6212845ul, 0x6C0CE934CF6654BEul,
            0xDD507CC9B729ACC0ul, 0x170947B5B5D5843Ful, 0x1BFB7989D9D51692ul, 0xACB5DF98A60B31D1ul,
            0xB7DF1AD0DB72FD2Ful, 0x967E266AEDAFE1B8ul, 0x16FC8E85E2F20108ul, 0x694E5771D8206963ul,
            0x7E3D93F4A3FE58A4ul, 0x58B68E728F74950Dul, 0xEE4A158258CD8B71ul, 0xB5595AC21DA4547Bul,
            0x1360F22A39D5309Cul, 0xF085602823B0D1C5ul, 0xEF38DBB8187941CAul
        };
        #endregion

        #region Properties
        /// <inheritdoc/>
        [ExcludeFromCodeCoverage]
        public override Int32 Length => 128;
        #endregion

        #region Constructors
        /// <summary>Initializes a new instance using the specified seed.</summary>
        /// <param name="seed">The <see cref="T:System.UInt64"/> seed used by the hashing algorithm.</param>
        [ExcludeFromCodeCoverage]
        public LaneHash128(UInt64 seed) : base(seed) { }

        /// <summary>Initializes a new instance using a seed value of <c>0</c>.</summary>
        [ExcludeFromCodeCoverage]
        public LaneHash128() : this(0ul) { }
        #endregion

        #region Methods
        private (UInt64 Hash1, UInt64 Hash2) ComputeHashValue(ReadOnlySpan<Byte> buffer)
        {
            Int32 count = buffer.Length;
            UInt64 z = (UInt64)count;
            UInt64 hash1, hash2;

            if (count <= 64)
            {
                HashShort(buffer, m_Seed, out UInt64 x, out UInt64 y);
                hash1 = Fold(x ^ (KF[0] ^ z), y ^ KF[1]);
                hash2 = Fold(x ^ KF[2], y ^ (KF[3] ^ z));
            }
            else
            {
                Span<UInt64> merged = stackalloc UInt64[8];
                HashLong(buffer, m_Seed, merged, out UInt64 ks);

                UInt64 f1 = Fold(merged[0] ^ S[0], merged[1] ^ S[1]);
                UInt64 f2 = Fold(merged[2] ^ S[2], merged[3] ^ S[3]);
                UInt64 f3 = Fold(merged[4] ^ S[4], merged[5] ^ S[5]);
                UInt64 f4 = Fold(merged[6] ^ S[6], merged[7] ^ S[7]);
                UInt64 f = f1 ^ f2 ^ f3 ^ f4;

                hash1 = Fold(f ^ z, ks ^ S[8]);

                f1 = Fold(merged[0] ^ S[10], merged[2] ^ S[11]);
                f2 = Fold(merged[1] ^ S[12], merged[3] ^ S[13]);
                f3 = Fold(merged[4] ^ S[14], merged[6] ^ S[15]);
                f4 = Fold(merged[5] ^ S[16], merged[7] ^ S[17]);
                f = f1 ^ f2 ^ f3 ^ f4;

                hash2 = Fold(f ^ z, ks ^ S[18]);
            }

            return (hash1, hash2);
        }

        /// <inheritdoc/>
        protected override void ComputeHashInternal(ReadOnlySpan<Byte> buffer, Span<Byte> destination)
        {
            (UInt64 hash1, UInt64 hash2) = ComputeHashValue(buffer);
            BinaryPrimitives.WriteUInt64LittleEndian(destination, hash1);
            BinaryPrimitives.WriteUInt64LittleEndian(destination.Slice(8), hash2);
        }
        #endregion
    }
}
