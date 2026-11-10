#region Using Directives
using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
#endregion

namespace FastHashes
{
    /// <summary>Represents the base class from which all the A5Hash implementations must derive. This class is abstract.</summary>
    public abstract class A5Hash<T> : Hash<T> where T : unmanaged
    {
        #region Constants
        /// <summary>Represents the V01 value. This field is constant.</summary>
        protected const UInt64 V01 = 0x5555555555555555ul;
        /// <summary>Represents the V10 value. This field is constant.</summary>
        protected const UInt64 V10 = 0xAAAAAAAAAAAAAAAAul;
        #endregion

        #region Methods
        /// <summary>Represents an auxiliary hashing function used by derived classes.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected static UInt64 ReadPair(ReadOnlySpan<Byte> buffer, Int32 offset1, Int32 offset2)
        {
            UInt64 v = ((UInt64)BinaryOperations.Read32(buffer, offset1) << 32) | BinaryOperations.Read32(buffer, offset2);

            return v;
        }

        /// <summary>Represents an auxiliary hashing function used by derived classes.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected static void Multiply64(UInt32 x, UInt32 y, out UInt32 lo, out UInt32 hi)
        {
            UInt64 product = (UInt64)x * y;
            lo = unchecked((UInt32)product);
            hi = (UInt32)(product >> 32);
        }
        #endregion
    }

    /// <summary>Represents the A5Hash32 implementation. This class cannot be derived.</summary>
    public sealed class A5Hash32 : A5Hash<UInt32>
    {
        #region Members
        private readonly UInt32 m_Seed;
        #endregion

        #region Properties
        /// <summary>Gets the seed used by the hashing algorithm.</summary>
        /// <value>An <see cref="T:System.UInt32"/> value.</value>
        [ExcludeFromCodeCoverage]
        public UInt32 Seed => m_Seed;
        #endregion

        #region Constructors
        /// <summary>Initializes a new instance using the specified seed.</summary>
        /// <param name="seed">The <see cref="T:System.UInt32"/> seed used by the hashing algorithm.</param>
        [ExcludeFromCodeCoverage]
        public A5Hash32(UInt32 seed)
        {
            m_Seed = seed;
        }

        /// <summary>Initializes a new instance using a seed value of <c>0</c>.</summary>
        [ExcludeFromCodeCoverage]
        public A5Hash32() : this(0u) { }
        #endregion

        #region Methods
        /// <inheritdoc/>
        protected override UInt32 ComputeHashValueInternal(ReadOnlySpan<Byte> buffer)
        {
            Int32 offset = 0;
            Int32 count = buffer.Length;

            UInt32 x, y;

            UInt32 v01 = unchecked((UInt32)V01);
            UInt32 v10 = unchecked((UInt32)V10);
            UInt32 seed1 = 0x243F6A88u ^ (UInt32)count;
            UInt32 seed2 = 0x85A308D3u ^ (UInt32)count;

            x = 0x452821E6u;
            y = 0x38D01377u;
            Multiply64(x, y, out UInt32 seed3, out UInt32 seed4);

            x = seed2 ^ (m_Seed & v10);
            y = seed1 ^ (m_Seed & v01);
            Multiply64(x, y, out seed1, out seed2);

            UInt32 a = 0;
            UInt32 b = 0;

            if (count < 17)
            {
                if (count > 3)
                {
                    a = BinaryOperations.Read32(buffer, 0);
                    b = BinaryOperations.Read32(buffer, count - 4);

                    if (count >= 9)
                    {
                        Int32 mo = count >> 3;
                        UInt32 c = BinaryOperations.Read32(buffer, mo * 4);
                        UInt32 d = BinaryOperations.Read32(buffer, count - (mo * 4) - 4);

                        x = unchecked(c + seed3);
                        y = unchecked(d + seed4);
                        Multiply64(x, y, out seed3, out seed4);
                    }
                }
                else
                {
                    if (count > 0)
                        a = buffer[0];

                    if (count > 1)
                        a |= (UInt32)buffer[1] << 8;

                    if (count > 2)
                        a |= (UInt32)buffer[2] << 16;
                }
            }
            else
            {
                v01 ^= seed1;
                v10 ^= seed2;

                do
                {
                    UInt32 s1 = seed1;
                    UInt32 s4 = seed4;

                    x = unchecked(BinaryOperations.Read32(buffer, offset) + seed1);
                    y = unchecked(BinaryOperations.Read32(buffer, offset + 4) + seed2);
                    Multiply64(x, y, out seed1, out seed2);
                    
                    x = unchecked(BinaryOperations.Read32(buffer, offset + 8) + seed3);
                    y = unchecked(BinaryOperations.Read32(buffer, offset + 12) + seed4);
                    Multiply64(x, y, out seed3, out seed4);

                    seed1 = unchecked(seed1 + v01);
                    seed2 = unchecked(seed2 + s4);
                    seed3 = unchecked(seed3 + s1);
                    seed4 = unchecked(seed4 + v10);

                    count -= 16;
                    offset += 16;
                }
                while (count > 16);

                a = BinaryOperations.Read32(buffer, offset + count - 8);
                b = BinaryOperations.Read32(buffer, offset + count - 4);

                if (count >= 9)
                {
                    UInt32 c = BinaryOperations.Read32(buffer, offset + count - 16);
                    UInt32 d = BinaryOperations.Read32(buffer, offset + count - 12);

                    x = unchecked(c + seed3);
                    y = unchecked(d + seed4);
                    Multiply64(x, y, out seed3, out seed4);
                }
            }

            seed1 ^= seed3;
            seed2 ^= seed4;

            x = unchecked(a + seed1);
            y = unchecked(b + seed2);
            Multiply64(x, y, out seed1, out seed2);
            
            x = v01 ^ seed1;
            y = seed2;
            Multiply64(x, y, out a, out b);

            UInt32 hash = a ^ b;

            return hash;
        }
        #endregion
    }

    /// <summary>Represents the A5Hash64 implementation. This class cannot be derived.</summary>
    public sealed class A5Hash64 : A5Hash<UInt64>
    {
        #region Members
        private readonly UInt64 m_Seed;
        #endregion

        #region Properties
        /// <summary>Gets the seed used by the hashing algorithm.</summary>
        /// <value>An <see cref="T:System.UInt64"/> value.</value>
        [ExcludeFromCodeCoverage]
        public UInt64 Seed => m_Seed;
        #endregion

        #region Constructors
        /// <summary>Initializes a new instance using the specified seed.</summary>
        /// <param name="seed">The <see cref="T:System.UInt64"/> seed used by the hashing algorithm.</param>
        [ExcludeFromCodeCoverage]
        public A5Hash64(UInt64 seed)
        {
            m_Seed = seed;
        }

        /// <summary>Initializes a new instance using a seed value of <c>0</c>.</summary>
        [ExcludeFromCodeCoverage]
        public A5Hash64() : this(0ul) { }
        #endregion

        #region Methods
        /// <inheritdoc/>
        protected override UInt64 ComputeHashValueInternal(ReadOnlySpan<Byte> buffer)
        {
            Int32 offset = 0;
            Int32 count = buffer.Length;

            UInt64 x, y;

            UInt64 v01 = V01;
            UInt64 v10 = V10;
            UInt64 seed1 = 0x243F6A8885A308D3ul ^ (UInt64)count;
            UInt64 seed2 = 0x452821E638D01377ul ^ (UInt64)count;

            x = seed2 ^ (m_Seed & v10);
            y = seed1 ^ (m_Seed & v01);
            BinaryOperations.Multiply128(x, y, out seed1, out seed2);

            if (count > 16)
            {
                v01 ^= seed1;
                v10 ^= seed2;

                do
                {
                    x = ReadPair(buffer, offset, offset + 4) ^ seed1;
                    y = ReadPair(buffer, offset + 8, offset + 12) ^ seed2;
                    BinaryOperations.Multiply128(x, y, out seed1, out seed2);

                    seed1 = unchecked(seed1 + v01);
                    seed2 = unchecked(seed2 + v10);

                    count -= 16;
                    offset += 16;
                }
                while (count > 16);
            }

            if (count > 3)
            {
                Int32 mo = count >> 3;
                seed1 ^= ReadPair(buffer, offset, offset + count - 4);
                seed2 ^= ReadPair(buffer, offset + (mo * 4), offset + count - (mo * 4) - 4);
            }
            else
            {
                if (count > 0)
                    seed1 ^= buffer[offset];
                
                if (count > 1)
                    seed1 ^= (UInt64)buffer[offset + 1] << 8;
                
                if (count > 2)
                    seed1 ^= (UInt64)buffer[offset + 2] << 16;
            }

            x = seed1;
            y = seed2;
            BinaryOperations.Multiply128(x, y, out seed1, out seed2);
            
            x = v01 ^ seed1;
            y = seed2;
            BinaryOperations.Multiply128(x, y, out seed1, out seed2);

            UInt64 hash = seed1 ^ seed2;

            return hash;
        }
        #endregion
    }

    /// <summary>Represents the A5Hash128 implementation. This class cannot be derived.</summary>
    public sealed class A5Hash128 : A5Hash<(UInt64, UInt64)>
    {
        #region Members
        private readonly UInt64 m_Seed;
        #endregion

        #region Properties
        /// <summary>Gets the seed used by the hashing algorithm.</summary>
        /// <value>An <see cref="T:System.UInt64"/> value.</value>
        [ExcludeFromCodeCoverage]
        public UInt64 Seed => m_Seed;
        #endregion

        #region Constructors
        /// <summary>Initializes a new instance using the specified seed.</summary>
        /// <param name="seed">The <see cref="T:System.UInt64"/> seed used by the hashing algorithm.</param>
        [ExcludeFromCodeCoverage]
        public A5Hash128(UInt64 seed)
        {
            m_Seed = seed;
        }

        /// <summary>Initializes a new instance using a seed value of <c>0</c>.</summary>
        [ExcludeFromCodeCoverage]
        public A5Hash128() : this(0ul) { }
        #endregion

        #region Methods
        /// <inheritdoc/>
        protected override (UInt64, UInt64) ComputeHashValueInternal(ReadOnlySpan<Byte> buffer)
        {
            Int32 offset = 0;
            Int32 count = buffer.Length;

            UInt64 x, y;

            UInt64 v01 = V01;
            UInt64 v10 = V10;
            UInt64 seed1 = 0x243F6A8885A308D3ul ^ (UInt64)count;
            UInt64 seed2 = 0x452821E638D01377ul ^ (UInt64)count;
            UInt64 seed3 = 0xA4093822299F31D0ul;
            UInt64 seed4 = 0xC0AC29B7C97C50DDul;

            x = seed2 ^ (m_Seed & v10);
            y = seed1 ^ (m_Seed & v01);
            BinaryOperations.Multiply128(x, y, out seed1, out seed2);

            UInt64 a;
            UInt64 b;

            if (count < 17)
            {
                if (count > 3)
                {
                    Int32 mo = count >> 3;
                    a = ReadPair(buffer, 0, count - 4);
                    b = ReadPair(buffer, mo * 4, count - 4 - mo * 4);
                }
                else
                {
                    a = 0;
                    b = 0;
                    
                    if (count > 0)
                        a = buffer[0];
                    
                    if (count > 1)
                        a |= (UInt64)buffer[1] << 8;
                    
                    if (count > 2)
                        a |= (UInt64)buffer[2] << 16;
                }
            }
            else if (count < 33)
            {
                a = ReadPair(buffer, 0, 4);
                b = ReadPair(buffer, 8, 12);
                UInt64 c = ReadPair(buffer, count - 16, count - 12);
                UInt64 d = ReadPair(buffer, count - 8, count - 4);
                
                x = unchecked(c + seed3);
                y = unchecked(d + seed4);
                BinaryOperations.Multiply128(x, y, out seed3, out seed4);
                
                seed1 ^= seed3;
                seed2 ^= seed4;
            }
            else
            {
                v01 ^= seed1;
                v10 ^= seed2;

                if (count > 64)
                {
                    UInt64 seed5 = 0x082EFA98EC4E6C89ul;
                    UInt64 seed6 = 0x3F84D5B5B5470917ul;
                    UInt64 seed7 = 0x13198A2E03707344ul;
                    UInt64 seed8 = 0xBE5466CF34E90C6Cul;

                    do
                    {
                        UInt64 s1 = seed1;
                        UInt64 s3 = seed3;
                        UInt64 s5 = seed5;

                        x = unchecked(BinaryOperations.Read64(buffer, offset) + seed1);
                        y = unchecked(BinaryOperations.Read64(buffer, offset + 32) + seed2);
                        BinaryOperations.Multiply128(x, y, out seed1, out seed2);

                        seed1 = unchecked(seed1 + v01);
                        seed2 = unchecked(seed2 + seed8);

                        x = unchecked(BinaryOperations.Read64(buffer, offset + 8) + seed3);
                        y = unchecked(BinaryOperations.Read64(buffer, offset + 40) + seed4);
                        BinaryOperations.Multiply128(x, y, out seed3, out seed4);
                        
                        seed3 = unchecked(seed3 + s1);
                        seed4 = unchecked(seed4 + v10);

                        x = unchecked(BinaryOperations.Read64(buffer, offset + 16) + seed5);
                        y = unchecked(BinaryOperations.Read64(buffer, offset + 48) + seed6);
                        BinaryOperations.Multiply128(x, y, out seed5, out seed6);

                        x = unchecked(BinaryOperations.Read64(buffer, offset + 24) + seed7);
                        y = unchecked(BinaryOperations.Read64(buffer, offset + 56) + seed8);
                        BinaryOperations.Multiply128(x, y, out seed7, out seed8);

                        seed5 = unchecked(seed5 + s3);
                        seed6 = unchecked(seed6 + v10);
                        seed7 = unchecked(seed7 + s5);
                        seed8 = unchecked(seed8 + v10);

                        count -= 64;
                        offset += 64;
                    }
                    while (count > 64);

                    seed1 ^= seed5;
                    seed2 ^= seed6;
                    seed3 ^= seed7;
                    seed4 ^= seed8;
                }

                if (count > 32)
                {
                    UInt64 s1 = seed1;

                    x = unchecked(BinaryOperations.Read64(buffer, offset) + seed1);
                    y = unchecked(BinaryOperations.Read64(buffer, offset + 8) + seed2);
                    BinaryOperations.Multiply128(x, y, out seed1, out seed2);
                    
                    seed1 = unchecked(seed1 + v01);
                    seed2 = unchecked(seed2 + seed4);

                    x = unchecked(BinaryOperations.Read64(buffer, offset + 16) + seed3);
                    y = unchecked(BinaryOperations.Read64(buffer, offset + 24) + seed4);
                    BinaryOperations.Multiply128(x, y, out seed3, out seed4);

                    seed3 = unchecked(seed3 + s1);
                    seed4 = unchecked(seed4 + v10);

                    count -= 32;
                    offset += 32;
                }

                a = BinaryOperations.Read64(buffer, offset + count - 16);
                b = BinaryOperations.Read64(buffer, offset + count - 8);

                if (count >= 17)
                {
                    UInt64 c = BinaryOperations.Read64(buffer, offset + count - 32);
                    UInt64 d = BinaryOperations.Read64(buffer, offset + count - 24);

                    x = unchecked(c + seed3);
                    y = unchecked(d + seed4);
                    BinaryOperations.Multiply128(x, y, out seed3, out seed4);
                }

                seed1 ^= seed3;
                seed2 ^= seed4;
            }

            x = unchecked(a + seed1);
            y = unchecked(b + seed2);
            BinaryOperations.Multiply128(x, y, out seed1, out seed2);

            x = v01 ^ seed1;
            y = seed2;
            BinaryOperations.Multiply128(x, y, out a, out b);
            
            UInt64 hash1 = a ^ b;

            x = seed1 ^ seed3;
            y = seed2 ^ seed4;
            BinaryOperations.Multiply128(x, y, out seed3, out seed4);

            UInt64 hash2 = seed3 ^ seed4;

            return (hash1, hash2);
        }
        #endregion
    }
}