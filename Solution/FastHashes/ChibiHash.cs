#region Using Directives
using System;
using System.Diagnostics.CodeAnalysis;
#endregion

namespace FastHashes
{
    /// <summary>Represents the ChibiHash implementation. This class cannot be derived.</summary>
    public sealed class ChibiHash : Hash<UInt64>
    {
        #region Constants
        private const UInt64 K = 0x2B7E151628AED2A7ul;
        private const UInt64 K2 = unchecked((K * K) ^ K);
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
        public ChibiHash(UInt64 seed)
        {
            m_Seed = seed;
        }

        /// <summary>Initializes a new instance using a seed value of <c>0</c>.</summary>
        [ExcludeFromCodeCoverage]
        public ChibiHash() : this(0ul) { }
        #endregion

        #region Methods
        /// <inheritdoc/>
        protected override UInt64 ComputeHashValueInternal(ReadOnlySpan<Byte> buffer)
        {
            Int32 offset = 0;
            Int32 count = buffer.Length;

            UInt64 seed = m_Seed;
            UInt64 v = unchecked(seed - K);
            UInt64 seed2 = unchecked(BinaryOperations.RotateLeft(v, 15) + BinaryOperations.RotateLeft(v, 47));

            UInt64 h0 = seed;
            UInt64 h1 = unchecked(seed + K);
            UInt64 h2 = seed2;
            UInt64 h3 = unchecked(seed2 + K2);

            while (count >= 32)
            {
                UInt64 stripe = BinaryOperations.Read64(buffer, offset);
                h0 = unchecked((stripe + h0) * K);
                h1 = unchecked(h1 + BinaryOperations.RotateLeft(stripe, 27));

                stripe = BinaryOperations.Read64(buffer, offset + 8);
                h1 = unchecked((stripe + h1) * K);
                h2 = unchecked(h2 + BinaryOperations.RotateLeft(stripe, 27));

                stripe = BinaryOperations.Read64(buffer, offset + 16);
                h2 = unchecked((stripe + h2) * K);
                h3 = unchecked(h3 + BinaryOperations.RotateLeft(stripe, 27));

                stripe = BinaryOperations.Read64(buffer, offset + 24);
                h3 = unchecked((stripe + h3) * K);
                h0 = unchecked(h0 + BinaryOperations.RotateLeft(stripe, 27));

                count -= 32;
                offset += 32;
            }

            while (count >= 8)
            {
                h0 ^= BinaryOperations.Read32(buffer, offset);
                h0 = unchecked(h0 * K);
                h1 ^= BinaryOperations.Read32(buffer, offset + 4);
                h1 = unchecked(h1 * K);

                count -= 8;
                offset += 8;
            }

            if (count >= 4)
            {
                h2 ^= BinaryOperations.Read32(buffer, offset);
                h3 ^= BinaryOperations.Read32(buffer, offset + count - 4);
            }
            else if (count > 0)
            {
                h2 ^= buffer[offset];
                h3 ^= (UInt64)buffer[offset + (count / 2)] | ((UInt64)buffer[offset + count - 1] << 8);
            }

            h0 = unchecked(h0 + (BinaryOperations.RotateLeft(unchecked(h2 * K), 31) ^ (h2 >> 31)));
            h1 = unchecked(h1 + (BinaryOperations.RotateLeft(unchecked(h3 * K), 31) ^ (h3 >> 31)));
            h0 = unchecked(h0 * K);
            h0 ^= h0 >> 31;
            h1 = unchecked(h1 + h0);

            UInt64 hash = unchecked((UInt64)buffer.Length * K);
            hash ^= BinaryOperations.RotateLeft(hash, 29);
            hash = unchecked(hash + seed);
            hash ^= h1;
            hash ^= BinaryOperations.RotateLeft(hash, 15) ^ BinaryOperations.RotateLeft(hash, 42);
            hash = unchecked(hash * K);
            hash ^= BinaryOperations.RotateLeft(hash, 13) ^ BinaryOperations.RotateLeft(hash, 31);

            return hash;
        }
        #endregion
    }
}