#region Using Directives
using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
#endregion

namespace FastHashes
{
    /// <summary>Represents the NmHash implementation. This class cannot be derived.</summary>
    public sealed class NmHash : Hash<UInt32>
    {
        #region Members
        private readonly Engine m_Engine;
        #endregion

        #region Properties
        /// <summary>Gets the variant of the hashing algorithm.</summary>
        /// <value>An enumerator value of type <see cref="T:FastHashes.NmHashVariant"/>.</value>
        [ExcludeFromCodeCoverage]
        public NmHashVariant Variant => m_Engine.Variant;


        /// <summary>Gets the seed used by the hashing algorithm.</summary>
        /// <value>An <see cref="T:System.UInt32"/> value.</value>
        [ExcludeFromCodeCoverage]
        public UInt32 Seed => m_Engine.Seed;
        #endregion

        #region Constructors
        /// <summary>Initializes a new instance using the specified variant and seed.</summary>
        /// <param name="variant">The <see cref="T:FastHashes.NmHashVariant"/> value specifying the variant of the hashing algorithm.</param>
        /// <param name="seed">The <see cref="T:System.UInt32"/> seed used by the hashing algorithm.</param>
        /// <exception cref="T:System.ArgumentOutOfRangeException">Thrown when <paramref name="variant">variant</paramref> is not a defined enumeration value.</exception>
        [ExcludeFromCodeCoverage]
        public NmHash(NmHashVariant variant, UInt32 seed)
        {
            if (!Enum.IsDefined(typeof(NmHashVariant), variant))
                throw new ArgumentOutOfRangeException(nameof(variant), "Invalid variant specified.");

            switch (variant)
            {
                case NmHashVariant.S:
                    m_Engine = new EngineS(seed);
                    break;

                case NmHashVariant.X:
                    m_Engine = new EngineX(seed);
                    break;

                default:
                    goto case NmHashVariant.S;
            }
        }

        /// <summary>Initializes a new instance using <see cref="F:FastHashes.NmHashVariant.S"/> and a seed value of <c>0</c>.</summary>
        [ExcludeFromCodeCoverage]
        public NmHash() : this(NmHashVariant.S, 0u) { }

        /// <summary>Initializes a new instance using the specified variant and a seed value of <c>0</c>.</summary>
        /// <param name="variant">The <see cref="T:FastHashes.NmHashVariant"/> value specifying the variant of the hashing algorithm.</param>
        /// <exception cref="T:System.ArgumentOutOfRangeException">Thrown when <paramref name="variant">variant</paramref> is not a defined enumeration value.</exception>
        [ExcludeFromCodeCoverage]
        public NmHash(NmHashVariant variant) : this(variant, 0u) { }

        /// <summary>Initializes a new instance using <see cref="F:FastHashes.NmHashVariant.S"/> and the specified seed.</summary>
        /// <param name="seed">The <see cref="T:System.UInt32"/> seed used by the hashing algorithm.</param>
        [ExcludeFromCodeCoverage]
        public NmHash(UInt32 seed) : this(NmHashVariant.S, seed) { }
        #endregion

        #region Methods
        /// <inheritdoc/>
        protected override UInt32 ComputeHashValueInternal(ReadOnlySpan<Byte> buffer)
        {
            return m_Engine.ComputeHashValue(buffer);
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
            private static readonly UInt32[] ACC0 =
            {
                0xB8FE6C39u, 0x23A44BBEu, 0x7C01812Cu, 0xF721AD1Cu,
                0xDED46DE9u, 0x839097DBu, 0x7240A4A4u, 0xB7B3671Fu,
                0xCB79E64Eu, 0xCCC0E578u, 0x825AD07Du, 0xCCFF7221u,
                0xB8084674u, 0xF743248Eu, 0xE03590E6u, 0x813A264Cu,
                0x3C2852BBu, 0x91C300CBu, 0x88D0658Bu, 0x1B532EA3u,
                0x71644897u, 0xA20DF94Eu, 0x3819EF46u, 0xA9DEACD8u,
                0xA8FA763Fu, 0xE39C343Fu, 0xF9DCBBC7u, 0xC70B4F1Du,
                0x8A51E04Bu, 0xCDB45931u, 0xC89F7EC9u, 0xD9787364u
            };

            protected const UInt32 M1 = 0xF0D9649Bu;
            protected const UInt32 M2 = 0x29A7935Du;
            protected const UInt32 M3 = 0x55D35831u;
            protected const UInt32 P1 = 0x9E3779B1u;
            protected const UInt32 P2 = 0x85EBCA77u;
            protected const UInt32 P3 = 0xC2B2AE3Du;
            protected const UInt32 P4 = 0x27D4EB2Fu;
            #endregion

            #region Members
            protected readonly UInt32 m_Seed;
            #endregion

            #region Properties
            public abstract NmHashVariant Variant { get; }

            public abstract String Name { get; }

            [ExcludeFromCodeCoverage]
            public UInt32 Seed => m_Seed;
            #endregion

            #region Constructors
            [ExcludeFromCodeCoverage]
            protected Engine(UInt32 seed)
            {
                m_Seed = seed;
            }
            #endregion

            #region Methods
            private static void LongRound(Span<UInt32> x, Span<UInt32> y, ReadOnlySpan<Byte> buffer, Int32 offset)
            {
                for (Int32 i = 0; i < 32; ++i)
                    x[i] ^= BinaryOperations.Read32(buffer, offset + (i * 4));

                for (Int32 i = 0; i < 32; ++i)
                    y[i] ^= BinaryOperations.Read32(buffer, offset + 128 + (i * 4));

                for (Int32 i = 0; i < 32; ++i)
                    x[i] = unchecked(x[i] + y[i]);

                for (Int32 i = 0; i < 32; ++i)
                    y[i] ^= x[i] >> 1;

                for (Int32 i = 0; i < 32; ++i)
                    x[i] = MultiplyHalves(x[i], M1);

                for (Int32 i = 0; i < 32; ++i)
                    x[i] ^= (x[i] << 5) ^ (x[i] >> 13);

                for (Int32 i = 0; i < 32; ++i)
                    x[i] = MultiplyHalves(x[i], M2);

                for (Int32 i = 0; i < 32; ++i)
                    x[i] ^= y[i];

                for (Int32 i = 0; i < 32; ++i)
                    x[i] ^= (x[i] << 11) ^ (x[i] >> 9);

                for (Int32 i = 0; i < 32; ++i)
                    x[i] = MultiplyHalves(x[i], M3);

                for (Int32 i = 0; i < 32; ++i)
                    x[i] ^= (x[i] >> 10) ^ (x[i] >> 20);
            }

            protected static UInt32 ComputeLong(ReadOnlySpan<Byte> buffer, UInt32 seed)
            {
                Int32 count = buffer.Length;

                Span<UInt32> x = stackalloc UInt32[32];
                ACC0.AsSpan().CopyTo(x);

                Span<UInt32> y = stackalloc UInt32[32];
                y.Fill(seed);

                Int32 rounds = (count - 1) / 256;

                for (Int32 i = 0; i < rounds; ++i)
                    LongRound(x, y, buffer, i * 256);

                LongRound(x, y, buffer, count - 256);

                UInt32 sum = 0;

                for (Int32 i = 0; i < 32; ++i)
                    sum = unchecked(sum + (x[i] ^ ACC0[i]));

                return sum ^ (UInt32)count;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            protected static UInt32 MultiplyHalves(UInt32 value, UInt32 multiplier)
            {
                UInt32 lower = (UInt32)unchecked((UInt16)value) * unchecked((UInt16)multiplier);
                UInt32 upper = (UInt32)(UInt16)(value >> 16) * (UInt16)(multiplier >> 16);
                UInt32 m = unchecked((UInt16)lower) | ((UInt32)unchecked((UInt16)upper) << 16);

                return m;
            }

            public abstract UInt32 ComputeHashValue(ReadOnlySpan<Byte> buffer);
            #endregion
        }

        private sealed class EngineS : Engine
        {
            #region Properties
            [ExcludeFromCodeCoverage]
            public override NmHashVariant Variant => NmHashVariant.S;

            [ExcludeFromCodeCoverage]
            public override String Name => "S";
            #endregion

            #region Constructors
            [ExcludeFromCodeCoverage]
            public EngineS(UInt32 seed) : base(seed) { }
            #endregion

            #region Methods
            private static UInt32 Compute9To255(ReadOnlySpan<Byte> buffer, UInt32 seed, Boolean longInput)
            {
                Int32 count = buffer.Length;
                UInt32 sl = unchecked(seed + (UInt32)count);

                Span<UInt32> x = stackalloc UInt32[4] { P1, P2, P3, P4 };

                Span<UInt32> y = stackalloc UInt32[4];
                y.Fill(sl);

                if (longInput)
                {
                    Int32 rounds = (count - 1) / 32;

                    for (Int32 i = 0; i < rounds; ++i)
                    {
                        Int32 offset = i * 32;

                        for (Int32 j = 0; j < 4; ++j)
                        {
                            x[j] ^= BinaryOperations.Read32(buffer, offset + j * 4);
                            y[j] ^= BinaryOperations.Read32(buffer, offset + 16 + j * 4);
                            x[j] = MixLane(unchecked(x[j] + y[j]), y[j]);
                        }
                    }

                    for (Int32 j = 0; j < 4; ++j)
                    {
                        x[j] ^= BinaryOperations.Read32(buffer, count - 32 + j * 4);
                        y[j] ^= BinaryOperations.Read32(buffer, count - 16 + j * 4);
                    }
                }
                else
                {
                    Int32 delta = (count >> 4) << 3;

                    x[0] ^= BinaryOperations.Read32(buffer, 0);
                    x[1] ^= BinaryOperations.Read32(buffer, delta);
                    x[2] ^= BinaryOperations.Read32(buffer, count - 8);
                    x[3] ^= BinaryOperations.Read32(buffer, count - 8 - delta);

                    y[0] ^= BinaryOperations.Read32(buffer, 4);
                    y[1] ^= BinaryOperations.Read32(buffer, delta + 4);
                    y[2] ^= BinaryOperations.Read32(buffer, count - 4);
                    y[3] ^= BinaryOperations.Read32(buffer, count - 4 - delta);
                }

                UInt32 sum = 0;

                for (Int32 j = 0; j < 4; ++j)
                {
                    UInt32 v = y[j];

                    x[j] = unchecked(x[j] + v);
                    y[j] = v ^ (v << 17) ^ (v >> 6);
                    x[j] = MixLane(x[j], y[j]);
                    x[j] ^= j == 0 ? P1 : j == 1 ? P2 : j == 2 ? P3 : P4;

                    sum = unchecked(sum + x[j]);
                }

                sum ^= unchecked(sl + (sl >> 5));
                sum = MultiplyHalves(sum, M3);
                sum ^= (sum >> 10) ^ (sum >> 20);

                return sum;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static UInt32 Mix0To8(UInt32 x, UInt32 seed)
            {
                x ^= (x >> 12) ^ (x >> 6);
                x = MultiplyHalves(x, 0x776BF593u);

                x ^= (x << 11) ^ (x >> 19);
                x = MultiplyHalves(x, 0x3FB39C65u);

                x ^= seed;
                x ^= (x >> 15) ^ (x >> 9);
                x = MultiplyHalves(x, 0xE9139917u);

                x ^= (x << 16) ^ (x >> 11);

                return x;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static UInt32 MixLane(UInt32 x, UInt32 y)
            {
                x = MultiplyHalves(x, M1);

                x ^= (x << 5) ^ (x >> 13);
                x = MultiplyHalves(x, M2);

                x ^= y;
                x ^= (x << 11) ^ (x >> 9);
                x = MultiplyHalves(x, M3);

                x ^= (x >> 10) ^ (x >> 20);

                return x;
            }

            private UInt32 ComputeResult(ReadOnlySpan<Byte> buffer)
            {
                Int32 count = buffer.Length;

                UInt32 seed = m_Seed;
                UInt32 hash;

                if (count <= 32)
                {
                    if (count > 8)
                        hash = Compute9To255(buffer, seed, false);
                    else if (count > 4)
                    {
                        UInt32 x = BinaryOperations.Read32(buffer, 0);
                        UInt32 y = BinaryOperations.Read32(buffer, count - 4) ^ unchecked(P4 + 2u + seed);

                        x = unchecked(x + y);
                        x ^= x << (count + 7);

                        hash = Mix0To8(x, BinaryOperations.RotateLeft(y, 5));
                    }
                    else
                    {
                        UInt32 data;

                        if (count == 4)
                            data = BinaryOperations.Read32(buffer, 0);
                        else if (count == 3)
                            data = BinaryOperations.Read16(buffer, 0) | ((UInt32)buffer[2] << 16);
                        else if (count == 2)
                            data = BinaryOperations.Read16(buffer, 0);
                        else if (count == 1)
                            data = buffer[0];
                        else
                            data = 0u;

                        seed = unchecked(seed + (count == 4 ? P3 : P2 + ((UInt32)count << 24) + ((UInt32)count << 1)));
                        hash = Mix0To8(unchecked(data + seed), BinaryOperations.RotateLeft(seed, 5));
                    }
                }
                else if (count < 256)
                    hash = Compute9To255(buffer, seed, true);
                else
                {
                    hash = ComputeLong(buffer, seed);

                    hash ^= (hash >> 8) ^ (hash >> 21);
                    hash = MultiplyHalves(hash, 0xCCE5196Du);

                    hash ^= (hash << 12) ^ (hash >> 7);
                    hash = MultiplyHalves(hash, 0x464BE229u);

                    hash ^= (hash >> 8) ^ (hash >> 21);
                }

                return hash;
            }

            public override UInt32 ComputeHashValue(ReadOnlySpan<Byte> buffer)
            {
                return ComputeResult(buffer);
            }
            #endregion
        }

        private sealed class EngineX : Engine
        {
            #region Properties
            [ExcludeFromCodeCoverage]
            public override NmHashVariant Variant => NmHashVariant.X;

            [ExcludeFromCodeCoverage]
            public override String Name => "X";
            #endregion

            #region Constructors
            [ExcludeFromCodeCoverage]
            public EngineX(UInt32 seed) : base(seed) { }
            #endregion

            #region Methods
            private static UInt32 Compute9To255(ReadOnlySpan<Byte> buffer, UInt32 seed)
            {
                Int32 count = buffer.Length;

                UInt32 x = P3, y = seed;
                UInt32 a = P4, b = seed;

                Int32 rounds = (count - 1) / 16;

                for (Int32 i = 0; i < rounds; ++i)
                {
                    Int32 offset = i * 16;

                    x ^= BinaryOperations.Read32(buffer, offset);
                    y ^= BinaryOperations.Read32(buffer, offset + 4);
                    x ^= y;
                    x = unchecked(x * 0x11049A7Du);
                    x ^= x >> 23;
                    x = unchecked(x * 0xBCCCDC7Bu);
                    y = BinaryOperations.RotateLeft(y, 4);
                    x ^= y;
                    x ^= x >> 12;
                    x = unchecked(x * 0x065E9DADu);
                    x ^= x >> 12;

                    a ^= BinaryOperations.Read32(buffer, offset + 8);
                    b ^= BinaryOperations.Read32(buffer, offset + 12);
                    a ^= b;
                    a = unchecked(a * 0x11049A7Du);
                    a ^= a >> 23;
                    a = unchecked(a * 0xBCCCDC7Bu);
                    b = BinaryOperations.RotateLeft(b, 3);
                    a ^= b;
                    a ^= a >> 12;
                    a = unchecked(a * 0x065E9DADu);
                    a ^= a >> 12;
                }

                if (((count - 1) & 8) != 0)
                {
                    if (((count - 1) & 4) != 0)
                    {
                        a ^= BinaryOperations.Read32(buffer, rounds * 16);
                        b ^= BinaryOperations.Read32(buffer, rounds * 16 + 4);
                        a ^= b;
                        a = unchecked(a * 0x11049A7Du);
                        a ^= a >> 23;
                        a = unchecked(a * 0xBCCCDC7Bu);
                        a ^= BinaryOperations.RotateLeft(b, 4);
                        a ^= a >> 12;
                        a = unchecked(a * 0x065E9DADu);
                    }
                    else
                    {
                        a ^= unchecked(BinaryOperations.Read32(buffer, rounds * 16) + b);
                        a ^= a >> 16;
                        a = unchecked(a * 0xA52FB2CDu);
                        a ^= a >> 15;
                        a = unchecked(a * 0x551E4D49u);
                    }

                    x ^= BinaryOperations.Read32(buffer, count - 8);
                    y ^= BinaryOperations.Read32(buffer, count - 4);
                    x ^= y;
                    x = unchecked(x * 0x11049A7Du);
                    x ^= x >> 23;
                    x = unchecked(x * 0xBCCCDC7Bu);
                    x ^= BinaryOperations.RotateLeft(y, 3);
                    x ^= x >> 12;
                    x = unchecked(x * 0x065E9DADu);
                }
                else
                {
                    if (((count - 1) & 4) != 0)
                    {
                        a ^= unchecked(BinaryOperations.Read32(buffer, rounds * 16) + b);
                        a ^= a >> 16;
                        a = unchecked(a * 0xA52FB2CDu);
                        a ^= a >> 15;
                        a = unchecked(a * 0x551E4D49u);
                    }

                    x ^= unchecked(BinaryOperations.Read32(buffer, count - 4) + y);
                    x ^= x >> 16;
                    x = unchecked(x * 0xA52FB2CDu);
                    x ^= x >> 15;
                    x = unchecked(x * 0x551E4D49u);
                }

                x ^= (UInt32)count;
                x ^= BinaryOperations.RotateLeft(a, 27);
                x ^= x >> 14;

                x = unchecked(x * 0x141CC535u);

                return x;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static UInt32 Mix0To4(UInt32 x, UInt32 seed)
            {
                x ^= seed;

                x = unchecked(x * 0xBDAB1EA9u);

                x = unchecked(x + BinaryOperations.RotateLeft(seed, 31));
                x ^= x >> 18;

                x = unchecked(x * 0xA7896A1Bu);
                x ^= x >> 12;

                x = unchecked(x * 0x83796A2Du);
                x ^= x >> 16;

                return x;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static UInt32 Mix5To8(ReadOnlySpan<Byte> buffer, UInt32 seed)
            {
                Int32 count = buffer.Length;

                UInt32 x = BinaryOperations.Read32(buffer, 0) ^ P3;
                UInt32 y = BinaryOperations.Read32(buffer, count - 4) ^ seed;

                x = unchecked(x + y);
                x ^= x >> count;

                x = unchecked(x * 0x11049A7Du);
                x ^= x >> 23;

                x = unchecked(x * 0xBCCCDC7Bu);
                x ^= BinaryOperations.RotateLeft(y, 3);
                x ^= x >> 12;

                x = unchecked(x * 0x065E9DADu);
                x ^= x >> 12;

                return x;
            }

            private UInt32 ComputeResult(ReadOnlySpan<Byte> buffer)
            {
                Int32 count = buffer.Length;
                UInt32 seed = m_Seed;
                UInt32 hash;

                if (count <= 8)
                {
                    if (count > 4)
                        hash = Mix5To8(buffer, seed);
                    else
                    {
                        UInt32 data;

                        if (count == 4)
                            data = BinaryOperations.Read32(buffer, 0);
                        else if (count == 3)
                            data = BinaryOperations.Read16(buffer, 0) | ((UInt32)buffer[2] << 16);
                        else if (count == 2)
                            data = BinaryOperations.Read16(buffer, 0);
                        else if (count == 1)
                            data = buffer[0];
                        else
                            data = 0u;

                        seed = unchecked(seed + (count == 4 ? P1 : P2 + ((UInt32)count << 24) + ((UInt32)count << 1)));
                        hash = Mix0To4(data, seed);
                    }
                }
                else if (count < 256)
                    hash = Compute9To255(buffer, seed);
                else
                {
                    hash = ComputeLong(buffer, seed);

                    hash ^= hash >> 15;
                    hash = unchecked(hash * 0xD168AAADu);
                    hash ^= hash >> 15;
                    hash = unchecked(hash * 0xAF723597u);
                    hash ^= hash >> 15;
                }

                return hash;
            }

            public override UInt32 ComputeHashValue(ReadOnlySpan<Byte> buffer)
            {
                return ComputeResult(buffer);
            }
            #endregion
        }
        #endregion
    }
}