#region Using Directives
using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
#endregion

namespace FastHashes
{
    /// <summary>Represents the W1Hash implementation. This class cannot be derived.</summary>
    public sealed class W1Hash : Hash<UInt64>
    {
        #region Constants
        private const UInt64 S0 = 0x2D358DCCAA6C78A5ul;
        private const UInt64 S1 = 0x8BB84B93962EACC9ul;
        private const UInt64 S2 = 0x4B33A62ED433D4A3ul;
        private const UInt64 S3 = 0x4D5A2DA51DE1AA47ul;
        #endregion

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
        public W1Hash(UInt64 seed)
        {
            m_Seed = seed;
        }

        /// <summary>Initializes a new instance using a seed value of <c>0</c>.</summary>
        [ExcludeFromCodeCoverage]
        public W1Hash() : this(0ul) { }
        #endregion

        #region Methods
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static UInt64 Mix(UInt64 value1, UInt64 value2)
        {
            BinaryOperations.Multiply128(value1, value2, out UInt64 lo, out UInt64 hi);

            return lo ^ hi;
        }

        /// <inheritdoc/>
        protected override UInt64 ComputeHashValueInternal(ReadOnlySpan<Byte> buffer)
        {
            Int32 offset = 0;
            Int32 count = buffer.Length;
            UInt64 length = (UInt64)count;

            UInt64 seed = m_Seed ^ Mix(m_Seed ^ S0, length ^ S1);

            if (count > 64)
            {
                UInt64 x = seed;
                UInt64 y = seed;
                UInt64 z = seed;

                do
                {
                    seed = Mix(BinaryOperations.Read64(buffer, offset) ^ S0, BinaryOperations.Read64(buffer, offset + 8) ^ seed);
                    x = Mix(BinaryOperations.Read64(buffer, offset + 16) ^ S1, BinaryOperations.Read64(buffer, offset + 24) ^ x);
                    y = Mix(BinaryOperations.Read64(buffer, offset + 32) ^ S2, BinaryOperations.Read64(buffer, offset + 40) ^ y);
                    z = Mix(BinaryOperations.Read64(buffer, offset + 48) ^ S3, BinaryOperations.Read64(buffer, offset + 56) ^ z);

                    offset += 64;
                    count -= 64;
                }
                while (count > 64);

                seed ^= x ^ y ^ z;
            }

            if (count > 32)
            {
                UInt64 x = seed;
                seed = Mix(BinaryOperations.Read64(buffer, offset) ^ S0, BinaryOperations.Read64(buffer, offset + 8) ^ seed);
                x = Mix(BinaryOperations.Read64(buffer, offset + 16) ^ S1, BinaryOperations.Read64(buffer, offset + 24) ^ x);
                seed ^= x;

                offset += 32;
                count -= 32;
            }

            if (count > 16)
            {
                seed = Mix(BinaryOperations.Read64(buffer, offset) ^ S0, BinaryOperations.Read64(buffer, offset + 8) ^ seed);

                offset += 16;
                count -= 16;
            }

            UInt64 a;
            UInt64 b;

            if (count <= 8)
            {
                a = BinaryOperations.ReadTail64(buffer.Slice(offset, count), 0);
                b = 0ul;
            }
            else
            {
                a = BinaryOperations.Read64(buffer, offset);
                b = BinaryOperations.ReadTail64(buffer.Slice(offset + 8, count - 8), 0);
            }

            BinaryOperations.Multiply128(a ^ S1, b ^ seed, out a, out b);

            UInt64 hash = Mix(a ^ (S0 ^ length), b ^ S1);

            return hash;
        }
        #endregion
    }
}
