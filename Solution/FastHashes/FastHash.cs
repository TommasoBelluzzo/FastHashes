﻿#region Using Directives
using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
#endregion

namespace FastHashes
{
    /// <summary>Represents the base class from which all the FastHash implementations must derive. This class is abstract.</summary>
    public abstract class FastHash : Hash
    {
        #region Constants
        private const UInt64 M = 0x880355f21e6d1965ul;
        private const UInt64 N = 0x2127599bf4325c37ul;
        #endregion

        #region Members
        /// <summary>Represents the seeds used by the hashing algorithm. This field is read-only.</summary>
        protected readonly UInt64 m_Seed;
        #endregion

        #region Properties
        /// <summary>Gets the seed used by the hashing algorithm.</summary>
        /// <value>An <see cref="T:System.UInt64"/> value.</value>
        [ExcludeFromCodeCoverage]
        protected UInt64 Seed => m_Seed;
        #endregion

        #region Constructors
        /// <summary>Represents the base constructor used by derived classes.</summary>
        /// <param name="seed">The <see cref="T:System.UInt64"/> seed used by the hashing algorithm.</param>
        [ExcludeFromCodeCoverage]
        protected FastHash(UInt64 seed)
        {
            m_Seed = seed;
        }
        #endregion

        #region Methods
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static UInt64 Mix(UInt64 v1, UInt64 v2)
        {
            v2 ^= v2 >> 23;
            v2 *= N;
            v2 ^= v2 >> 47;

            v1 ^= v2;
            v1 *= M;

            return v1;
        }

        /// <summary>Finalizes any partial computation and returns the hash code.</summary>
        /// <param name="hashData">The <see cref="T:System.UInt64"/> value representing the hash data.</param>
        /// <returns>A <see cref="T:System.Byte"/>[] representing the hash code.</returns>
        protected abstract Byte[] GetHash(UInt64 hashData);

        /// <inheritdoc/>
        protected override Byte[] ComputeHashInternal(ReadOnlySpan<Byte> buffer)
        {
            Int32 offset = 0;
            Int32 count = buffer.Length;

            UInt64 hash = m_Seed;

            if (count == 0)
                goto Finalize;

            hash ^= (UInt64)count * M;

            Int32 blocks = count / 8;

            while (blocks-- > 0)
            {
                hash = Mix(hash, BinaryOperations.Read64(buffer, offset));
                offset += 8;
            }

            Int32 remainder = count & 7;

            if (remainder > 0)
            {
                UInt64 v = BinaryOperations.ReadTail64(buffer, offset);
                hash = Mix(hash, v);
            }

            Finalize:

            hash ^= hash >> 23;
            hash *= N;
            hash ^= hash >> 47;

            Byte[] result = GetHash(hash);

            return result;
        }
        #endregion
    }

    /// <summary>Represents the FastHash32 implementation. This class cannot be derived.</summary>
    public sealed class FastHash32 : FastHash
    {
        #region Properties
        /// <inheritdoc/>
        [ExcludeFromCodeCoverage]
        public override Int32 Length => 32;
        #endregion

        #region Constructors
        /// <summary>Initializes a new instance using a seed value of <c>0</c>.</summary>
        [ExcludeFromCodeCoverage]
        public FastHash32() : base(0ul) { }

        /// <summary>Initializes a new instance using the specified seed.</summary>
        /// <param name="seed">The <see cref="T:System.UInt64"/> seed used by the hashing algorithm.</param>
        [ExcludeFromCodeCoverage]
        public FastHash32(UInt64 seed) : base(seed) { }
        #endregion

        #region Methods
        /// <inheritdoc/>
        protected override Byte[] GetHash(UInt64 hashData)
        {
            UInt64 hash = hashData - (hashData >> 32);
            Byte[] result = BinaryOperations.ToArray32(hash);

            return result;
        }
        #endregion
    }

    /// <summary>Represents the FastHash64 implementation. This class cannot be derived.</summary>
    public sealed class FastHash64 : FastHash
    {
        #region Properties
        /// <inheritdoc/>
        [ExcludeFromCodeCoverage]
        public override Int32 Length => 64;
        #endregion

        #region Constructors
        /// <summary>Initializes a new instance using a seed value of <c>0</c>.</summary>
        [ExcludeFromCodeCoverage]
        public FastHash64() : base(0ul) { }

        /// <summary>Initializes a new instance using the specified seed.</summary>
        /// <param name="seed">The <see cref="T:System.UInt64"/> seed used by the hashing algorithm.</param>
        [ExcludeFromCodeCoverage]
        public FastHash64(UInt64 seed) : base(seed) { }
        #endregion

        #region Methods
        /// <inheritdoc/>
        protected override Byte[] GetHash(UInt64 hashData)
        {
            UInt64 hash = hashData;
            Byte[] result = BinaryOperations.ToArray64(hash);

            return result;
        }
        #endregion
    }
}