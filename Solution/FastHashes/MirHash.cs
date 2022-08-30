#region Using Directives
using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
#endregion

namespace FastHashes
{
    /// <summary>Represents the MirHash implementation. This class cannot be derived.</summary>
    public sealed class MirHash : Hash
    {
        #region Constants
        private const UInt64 P1 = 0X65862B62BDF5EF4Dul;
        private const UInt64 P2 = 0X288EEA216831E6A7ul;
        #endregion

        #region Members
        private readonly UInt64 m_Seed;
        #endregion

        #region Properties
        /// <inheritdoc/>
        [ExcludeFromCodeCoverage]
        public override Int32 Length => 64;

        /// <summary>Gets the seed used by the hashing algorithm.</summary>
        /// <value>An <see cref="T:System.UInt64"/> value.</value>
        [ExcludeFromCodeCoverage]
        public UInt64 Seed => m_Seed;
        #endregion

        #region Constructors
        /// <summary>Initializes a new instance using the specified seed.</summary>
        /// <param name="seed">The <see cref="T:System.UInt64"/> seed used by the hashing algorithm.</param>
        [ExcludeFromCodeCoverage]
        public MirHash(UInt64 seed)
        {
            m_Seed = seed;
        }

        /// <summary>Initializes a new instance using a seed value of <c>0</c>.</summary>
        [ExcludeFromCodeCoverage]
        public MirHash() : this(0ul) { }
        #endregion

        #region Methods
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static UInt64 GetKeyPart(ReadOnlySpan<Byte> buffer, Int32 offset, Int32 length)
        {
            UInt64 tail = 0ul;

            for (Int32 i = 0; i < length; ++i)
                tail = (tail >> 8) | ((UInt64)buffer[offset + i] << 56);

            return tail;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static UInt64 MirMum(UInt64 v, UInt64 c)
        {
            UInt64 v1 = v >> 32;
            UInt64 v2 = (UInt32)v;

            UInt64 c1 = c >> 32;
            UInt64 c2 = (UInt32)c;

            UInt64 rm = (v2 * c1) + (v1 * c2);
            UInt64 mm = (v1 * c1) + (rm >> 32) + (v2 * c2) + (rm << 32);

            return mm;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static UInt64 MirRound(UInt64 state, UInt64 v)
        {
            state ^= MirMum(v, P1);
            state ^= MirMum(state, P2);

            return state;
        }

        /// <inheritdoc/>
        protected override Byte[] ComputeHashInternal(ReadOnlySpan<Byte> buffer)
        {
            Int32 offset = 0;
            Int32 count = buffer.Length;

            UInt64 r = m_Seed + (UInt64)count;

            if (count == 0)
                goto Finalize;

            while ((count - offset) >= 16)
            {
                UInt64 k1 = GetKeyPart(buffer, offset, 8);
                r ^= MirMum(k1, P1);

                UInt64 k2 = GetKeyPart(buffer, offset + 8, 8);
                r ^= MirMum(k2, P2);

                r ^= MirMum(r, P1);

                offset += 16;
            }

            if ((count - offset) >= 8)
            {
                UInt64 k = GetKeyPart(buffer, offset, 8);
                r ^= MirMum(k, P1);

                offset += 8;
            }

            Int32 delta = count - offset;

            if (delta > 0)
            {
                UInt64 k = GetKeyPart(buffer, offset, delta);
                r ^= MirMum(k, P2);
            }

            Finalize:

            UInt64 hash = MirRound(r, r);
            Byte[] result = BinaryOperations.ToArray64(hash);

            return result;
        }
        #endregion
    }
}