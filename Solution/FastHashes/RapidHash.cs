#region Using Directives
using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
#endregion

namespace FastHashes
{
    /// <summary>Represents the RapidHash implementation. This class cannot be derived.</summary>
    public sealed class RapidHash : Hash
    {
        #region Members
        private readonly Engine m_Engine;
        #endregion

        #region Properties
        /// <summary>Gets the variant of the hashing algorithm.</summary>
        /// <value>An enumerator value of type <see cref="T:FastHashes.RapidHashVariant"/>.</value>
        [ExcludeFromCodeCoverage]
        public RapidHashVariant Variant => m_Engine.Variant;

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
        /// <param name="variant">The enumerator value of type <see cref="T:FastHashes.RapidHashVariant"/> representing the variant of the hashing algorithm.</param>
        /// <param name="seed">The <see cref="T:System.UInt64"/> seed used by the hashing algorithm.</param>
        /// <exception cref="T:System.ArgumentException ">Thrown when the value of <paramref name="variant">variant</paramref> is undefined.</exception>
        [ExcludeFromCodeCoverage]
        public RapidHash(RapidHashVariant variant, UInt64 seed)
        {
            if (!Enum.IsDefined(typeof(RapidHashVariant), variant))
                throw new ArgumentException("Invalid variant specified.", nameof(variant));

            switch (variant)
            {
                case RapidHashVariant.V1:
                    m_Engine = new EngineV1(seed);
                    break;

                case RapidHashVariant.V2:
                    m_Engine = new EngineV2(seed);
                    break;

                case RapidHashVariant.V3:
                    m_Engine = new EngineV3(seed);
                    break;

                default:
                    goto case RapidHashVariant.V3;
            }
        }
        
        /// <summary>Initializes a new instance using <see cref="F:FastHashes.RapidHashVariant.V3"/> and a seed value of <c>0</c>.</summary>
        [ExcludeFromCodeCoverage]
        public RapidHash() : this(RapidHashVariant.V3, 0ul) { }


        /// <summary>Initializes a new instance using the specified variant and a seed value of <c>0</c>.</summary>
        /// <param name="variant">The enumerator value of type <see cref="T:FastHashes.RapidHashVariant"/> representing the variant of the hashing algorithm.</param>
        /// <exception cref="T:System.ArgumentException ">Thrown when the value of <paramref name="variant">variant</paramref> is undefined.</exception>
        [ExcludeFromCodeCoverage]
        public RapidHash(RapidHashVariant variant) : this(variant, 0ul) { }

        /// <summary>Initializes a new instance using <see cref="F:FastHashes.RapidHashVariant.V3"/> and the specified seed.</summary>
        /// <param name="seed">The <see cref="T:System.UInt64"/> seed used by the hashing algorithm.</param>
        [ExcludeFromCodeCoverage]
        public RapidHash(UInt64 seed) : this(RapidHashVariant.V3, seed) { }
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
            protected const UInt64 S0 = 0x2D358DCCAA6C78A5ul;
            protected const UInt64 S1 = 0x8BB84B93962EACC9ul;
            protected const UInt64 S2 = 0x4B33A62ED433D4A3ul;
            protected const UInt64 S3 = 0x4D5A2DA51DE1AA47ul;
            protected const UInt64 S4 = 0xA0761D6478BD642Ful;
            protected const UInt64 S5 = 0xE7037ED1A0B428DBul;
            protected const UInt64 S6 = 0x90ED1765281C388Cul;
            protected const UInt64 S7 = 0xAAAAAAAAAAAAAAAAul;
            #endregion

            #region Members
            protected readonly UInt64 m_Seed;
            #endregion

            #region Properties
            public abstract RapidHashVariant Variant { get; }

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
            protected static void Mum(ref UInt64 a, ref UInt64 b)
            {
                #if NET5_0_OR_GREATER
                UInt64 hi = Math.BigMul(a, b, out UInt64 lo);

                a = lo;
                b = hi;
                #else
                UInt64 ah = a >> 32;
                UInt64 al = unchecked((UInt32)a);
                UInt64 bh = b >> 32;
                UInt64 bl = unchecked((UInt32)b);

                UInt64 ll = al * bl;
                UInt64 m0 = ah * bl;
                UInt64 m1 = bh * al;

                UInt64 t = unchecked(ll + (m0 << 32));

                UInt64 carry = t < ll ? 1ul : 0ul;

                UInt64 lo = unchecked(t + (m1 << 32));
                carry += lo < t ? 1ul : 0ul;

                a = lo;
                b = unchecked((ah * bh) + (m0 >> 32) + (m1 >> 32) + carry);
                #endif
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            protected static UInt64 Mix(UInt64 a, UInt64 b)
            {
                Mum(ref a, ref b);
                return a ^ b;
            }

            public abstract Byte[] ComputeHash(ReadOnlySpan<Byte> buffer);
            #endregion
        }

        private sealed class EngineV1 : Engine
        {
            #region Properties
            [ExcludeFromCodeCoverage]
            public override RapidHashVariant Variant => RapidHashVariant.V1;

            [ExcludeFromCodeCoverage]
            public override String Name => "V1";
            #endregion

            #region Constructors
            [ExcludeFromCodeCoverage]
            public EngineV1(UInt64 seed) : base(seed) { }
            #endregion

            #region Methods
            public override Byte[] ComputeHash(ReadOnlySpan<Byte> buffer)
            {
                Int32 offset = 0;
                Int32 count = buffer.Length;

                UInt64 seed = m_Seed ^ Mix(m_Seed ^ S0, S1) ^ (UInt64)count;
                UInt64 a, b;

                if (count <= 16)
                {
                    if (count >= 4)
                    {
                        Int32 last = count - 4;
                        Int32 delta = (count & 24) >> (count >> 3);

                        a = ((UInt64)BinaryOperations.Read32(buffer, 0) << 32) | BinaryOperations.Read32(buffer, last);
                        b = ((UInt64)BinaryOperations.Read32(buffer, delta) << 32) | BinaryOperations.Read32(buffer, last - delta);
                    }
                    else if (count > 0)
                    {
                        a = ((UInt64)buffer[0] << 56) | ((UInt64)buffer[count >> 1] << 32) | buffer[count - 1];
                        b = 0;
                    }
                    else
                    {
                        a = 0;
                        b = 0;
                    }
                }
                else
                {
                    Int32 remaining = count;

                    if (remaining > 48)
                    {
                        UInt64 seed1 = seed;
                        UInt64 seed2 = seed;

                        do
                        {
                            a = BinaryOperations.Read64(buffer, offset) ^ S0;
                            b = BinaryOperations.Read64(buffer, offset + 8) ^ seed;
                            seed = Mix(a, b);

                            a = BinaryOperations.Read64(buffer, offset + 16) ^ S1;
                            b = BinaryOperations.Read64(buffer, offset + 24) ^ seed1;
                            seed1 = Mix(a, b);

                            a = BinaryOperations.Read64(buffer, offset + 32) ^ S2;
                            b = BinaryOperations.Read64(buffer, offset + 40) ^ seed2;
                            seed2 = Mix(a, b);
                            
                            offset += 48;
                            remaining -= 48;
                        }
                        while (remaining >= 48);

                        seed ^= seed1 ^ seed2;
                    }

                    if (remaining > 16)
                    {
                        a = BinaryOperations.Read64(buffer, offset) ^ S2;
                        b = BinaryOperations.Read64(buffer, offset + 8) ^ seed ^ S1;
                        seed = Mix(a, b);

                        if (remaining > 32)
                        {
                            a = BinaryOperations.Read64(buffer, offset + 16) ^ S2;
                            b = BinaryOperations.Read64(buffer, offset + 24) ^ seed;
                            seed = Mix(a, b);
                        }
                    }

                    a = BinaryOperations.Read64(buffer, offset + remaining - 16);
                    b = BinaryOperations.Read64(buffer, offset + remaining - 8);
                }

                a ^= S1;
                b ^= seed;
                Mum(ref a, ref b);

                a ^= S0 ^ (UInt64)count;
                b ^= S1;

                UInt64 hash = Mix(a, b);
                Byte[] result = BinaryOperations.ToArray64(hash);

                return result;
            }
            #endregion
        }

        private sealed class EngineV2 : Engine
        {
            #region Properties
            [ExcludeFromCodeCoverage]
            public override RapidHashVariant Variant => RapidHashVariant.V2;

            [ExcludeFromCodeCoverage]
            public override String Name => "V2";
            #endregion

            #region Constructors
            [ExcludeFromCodeCoverage]
            public EngineV2(UInt64 seed) : base(seed) { }
            #endregion

            #region Methods
            public override Byte[] ComputeHash(ReadOnlySpan<Byte> buffer)
            {
                Int32 offset = 0;
                Int32 count = buffer.Length;

                UInt64 seed = m_Seed ^ Mix(m_Seed ^ S2, S1) ^ (UInt64)count;
                UInt64 a, b;

                if (count <= 16)
                {
                    if (count >= 4)
                    {
                        if (count >= 8)
                        {
                            a = BinaryOperations.Read64(buffer, 0);
                            b = BinaryOperations.Read64(buffer, count - 8);
                        }
                        else
                        {
                            a = BinaryOperations.Read32(buffer, 0);
                            b = BinaryOperations.Read32(buffer, count - 4);
                        }
                    }
                    else if (count > 0)
                    {
                        a = ((UInt64)buffer[0] << 56) | buffer[count - 1];
                        b = buffer[count >> 1];
                    }
                    else
                    {
                        a = 0;
                        b = 0;
                    }
                }
                else if (count <= 64)
                {
                    a = BinaryOperations.Read64(buffer, 0) ^ S0;
                    b = BinaryOperations.Read64(buffer, 8) ^ seed;
                    seed = Mix(a, b);

                    if (count > 32)
                    {
                        a = BinaryOperations.Read64(buffer, 16) ^ S1;
                        b = BinaryOperations.Read64(buffer, 24) ^ seed;
                        seed = Mix(a, b);

                        if (count > 48)
                        {
                            a = BinaryOperations.Read64(buffer, 32) ^ S1;
                            b = BinaryOperations.Read64(buffer, 40) ^ seed;
                            seed = Mix(a, b);
                        }
                    }

                    a = BinaryOperations.Read64(buffer, count - 16);
                    b = BinaryOperations.Read64(buffer, count - 8);
                }
                else
                {
                    Int32 remaining = count;
                    UInt64 see1 = seed, see2 = seed, see3 = seed;
                    UInt64 see4 = seed, see5 = seed, see6 = seed;

                    while (remaining >= 112)
                    {
                        a = BinaryOperations.Read64(buffer, offset) ^ S0;
                        b = BinaryOperations.Read64(buffer, offset + 8) ^ seed;
                        seed = Mix(a, b);

                        a = BinaryOperations.Read64(buffer, offset + 16) ^ S1;
                        b = BinaryOperations.Read64(buffer, offset + 24) ^ see1;
                        see1 = Mix(a, b);

                        a = BinaryOperations.Read64(buffer, offset + 32) ^ S2;
                        b = BinaryOperations.Read64(buffer, offset + 40) ^ see2;
                        see2 = Mix(a, b);

                        a = BinaryOperations.Read64(buffer, offset + 48) ^ S3;
                        b = BinaryOperations.Read64(buffer, offset + 56) ^ see3;
                        see3 = Mix(a, b);

                        a = BinaryOperations.Read64(buffer, offset + 64) ^ S4;
                        b = BinaryOperations.Read64(buffer, offset + 72) ^ see4;
                        see4 = Mix(a, b);

                        a = BinaryOperations.Read64(buffer, offset + 80) ^ S5;
                        b = BinaryOperations.Read64(buffer, offset + 88) ^ see5;
                        see5 = Mix(a, b);

                        a = BinaryOperations.Read64(buffer, offset + 96) ^ S6;
                        b = BinaryOperations.Read64(buffer, offset + 104) ^ see6;
                        see6 = Mix(a, b);

                        offset += 112;
                        remaining -= 112;
                    }

                    while (remaining >= 48)
                    {
                        a = BinaryOperations.Read64(buffer, offset) ^ S0;
                        b = BinaryOperations.Read64(buffer, offset + 8) ^ seed;
                        seed = Mix(a, b);

                        a = BinaryOperations.Read64(buffer, offset + 16) ^ S1;
                        b = BinaryOperations.Read64(buffer, offset + 24) ^ see1;
                        see1 = Mix(a, b);

                        a = BinaryOperations.Read64(buffer, offset + 32) ^ S2;
                        b = BinaryOperations.Read64(buffer, offset + 40) ^ see2;
                        see2 = Mix(a, b);

                        offset += 48;
                        remaining -= 48;
                    }

                    see3 ^= see4;
                    see5 ^= see6;
                    seed ^= see1;
                    see3 ^= see2;
                    seed ^= see5;
                    seed ^= see3;

                    if (remaining > 16)
                    {
                        a = BinaryOperations.Read64(buffer, offset) ^ S2;
                        b = BinaryOperations.Read64(buffer, offset + 8) ^ seed;
                        seed = Mix(a, b);

                        if (remaining > 32)
                        {
                            a = BinaryOperations.Read64(buffer, offset + 16) ^ S2;
                            b = BinaryOperations.Read64(buffer, offset + 24) ^ seed;
                            seed = Mix(a, b);
                        }
                    }

                    a = BinaryOperations.Read64(buffer, offset + remaining - 16);
                    b = BinaryOperations.Read64(buffer, offset + remaining - 8);
                }

                a ^= S1;
                b ^= seed;
                Mum(ref a, ref b);

                a ^= S7 ^ (UInt64)count;
                b ^= S1;

                UInt64 hash = Mix(a, b);
                Byte[] result = BinaryOperations.ToArray64(hash);

                return result;
            }
            #endregion
        }

        private sealed class EngineV3 : Engine
        {
            #region Properties
            [ExcludeFromCodeCoverage]
            public override RapidHashVariant Variant => RapidHashVariant.V3;

            [ExcludeFromCodeCoverage]
            public override String Name => "V3";
            #endregion

            #region Constructors
            [ExcludeFromCodeCoverage]
            public EngineV3(UInt64 seed) : base(seed) { }
            #endregion

            #region Methods
            public override Byte[] ComputeHash(ReadOnlySpan<Byte> buffer)
            {
                Int32 offset = 0;
                Int32 count = buffer.Length;
                Int32 remaining = count;

                UInt64 seed = m_Seed ^ Mix(m_Seed ^ S2, S1);
                UInt64 a = 0, b = 0;

                if (count <= 16)
                {
                    if (count >= 4)
                    {
                        seed ^= (UInt64)count;

                        if (count >= 8)
                        {
                            a = BinaryOperations.Read64(buffer, 0);
                            b = BinaryOperations.Read64(buffer, count - 8);
                        }
                        else
                        {
                            a = BinaryOperations.Read32(buffer, 0);
                            b = BinaryOperations.Read32(buffer, count - 4);
                        }
                    }
                    else if (count > 0)
                    {
                        a = ((UInt64)buffer[0] << 45) | buffer[count - 1];
                        b = buffer[count >> 1];
                    }
                }
                else
                {
                    if (remaining > 112)
                    {
                        UInt64 seed1 = seed, seed2 = seed, seed3 = seed;
                        UInt64 seed4 = seed, seed5 = seed, seed6 = seed;

                        do
                        {
                            a = BinaryOperations.Read64(buffer, offset) ^ S0;
                            b = BinaryOperations.Read64(buffer, offset + 8) ^ seed;
                            seed = Mix(a, b);

                            a = BinaryOperations.Read64(buffer, offset + 16) ^ S1;
                            b = BinaryOperations.Read64(buffer, offset + 24) ^ seed1;
                            seed1 = Mix(a, b);

                            a = BinaryOperations.Read64(buffer, offset + 32) ^ S2;
                            b = BinaryOperations.Read64(buffer, offset + 40) ^ seed2;
                            seed2 = Mix(a, b);

                            a = BinaryOperations.Read64(buffer, offset + 48) ^ S3;
                            b = BinaryOperations.Read64(buffer, offset + 56) ^ seed3;
                            seed3 = Mix(a, b);

                            a = BinaryOperations.Read64(buffer, offset + 64) ^ S4;
                            b = BinaryOperations.Read64(buffer, offset + 72) ^ seed4;
                            seed4 = Mix(a, b);

                            a = BinaryOperations.Read64(buffer, offset + 80) ^ S5;
                            b = BinaryOperations.Read64(buffer, offset + 88) ^ seed5;
                            seed5 = Mix(a, b);

                            a = BinaryOperations.Read64(buffer, offset + 96) ^ S6;
                            b = BinaryOperations.Read64(buffer, offset + 104) ^ seed6;
                            seed6 = Mix(a, b);

                            offset += 112;
                            remaining -= 112;
                        }
                        while (remaining > 112);

                        seed ^= seed1;
                        seed2 ^= seed3;
                        seed4 ^= seed5;
                        seed ^= seed6;
                        seed2 ^= seed4;
                        seed ^= seed2;
                    }

                    if (remaining > 16)
                    {
                        a = BinaryOperations.Read64(buffer, offset) ^ S2;
                        b = BinaryOperations.Read64(buffer, offset + 8) ^ seed;
                        seed = Mix(a, b);

                        if (remaining > 32)
                        {
                            a = BinaryOperations.Read64(buffer, offset + 16) ^ S2;
                            b = BinaryOperations.Read64(buffer, offset + 24) ^ seed;
                            seed = Mix(a, b);

                            if (remaining > 48)
                            {
                                a = BinaryOperations.Read64(buffer, offset + 32) ^ S1;
                                b = BinaryOperations.Read64(buffer, offset + 40) ^ seed;
                                seed = Mix(a, b);

                                if (remaining > 64)
                                {
                                    a = BinaryOperations.Read64(buffer, offset + 48) ^ S1;
                                    b = BinaryOperations.Read64(buffer, offset + 56) ^ seed;
                                    seed = Mix(a, b);

                                    if (remaining > 80)
                                    {
                                        a = BinaryOperations.Read64(buffer, offset + 64) ^ S2;
                                        b = BinaryOperations.Read64(buffer, offset + 72) ^ seed;
                                        seed = Mix(a, b);

                                        if (remaining > 96)
                                        {
                                            a = BinaryOperations.Read64(buffer, offset + 80) ^ S1;
                                            b = BinaryOperations.Read64(buffer, offset + 88) ^ seed;
                                            seed = Mix(a, b);
                                        }
                                    }
                                }
                            }
                        }
                    }

                    a = BinaryOperations.Read64(buffer, offset + remaining - 16) ^ (UInt64)remaining;
                    b = BinaryOperations.Read64(buffer, offset + remaining - 8);
                }

                a ^= S1;
                b ^= seed;
                Mum(ref a, ref b);

                a ^= S7;
                b ^= S1 ^ (UInt64)remaining;

                UInt64 hash = Mix(a, b);
                Byte[] result = BinaryOperations.ToArray64(hash);

                return result;
            }
            #endregion
        }
        #endregion
    }
}