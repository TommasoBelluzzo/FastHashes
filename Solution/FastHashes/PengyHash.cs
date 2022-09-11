#region Using Directives
using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
#endregion

namespace FastHashes
{
    /// <summary>Represents the PengyHash implementation. This class cannot be derived.</summary>
    public sealed class PengyHash : Hash
    {
        #region Members
        private readonly UInt32 m_Seed;
        #endregion

        #region Properties
        /// <inheritdoc/>
        [ExcludeFromCodeCoverage]
        public override Int32 Length => 64;

        /// <summary>Gets the seed used by the hashing algorithm.</summary>
        /// <value>An <see cref="T:System.UInt32"/> value.</value>
        [ExcludeFromCodeCoverage]
        public UInt32 Seed => m_Seed;
        #endregion

        #region Constructors
        /// <summary>Initializes a new instance using the specified seed.</summary>
        /// <param name="seed">The <see cref="T:System.UInt32"/> seed used by the hashing algorithm.</param>
        [ExcludeFromCodeCoverage]
        public PengyHash(UInt32 seed)
        {
            m_Seed = seed;
        }

        /// <summary>Initializes a new instance using a seed value of <c>0</c>.</summary>
        [ExcludeFromCodeCoverage]
        public PengyHash() : this(0u) { }
        #endregion

        #region Methods
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void Pengy(UInt64 seed, UInt64[] b, ref UInt64[] s)
        {
            s[0] += s[1] + b[3];
            s[1] = s[0] + BinaryOperations.RotateLeft(s[1], 14) + seed;

            s[2] += s[3] + b[2];
            s[3] = s[2] + BinaryOperations.RotateLeft(s[3], 23);

            s[0] += s[3] + b[1];
            s[3] = s[0] ^ BinaryOperations.RotateLeft(s[3], 16);

            s[2] += s[1] + b[0];
            s[1] = s[2] ^ BinaryOperations.RotateLeft(s[1], 40);
        }

        /// <inheritdoc/>
        protected override Byte[] ComputeHashInternal(ReadOnlySpan<Byte> buffer)
        {
            Int32 offset = 0;
            Int32 count = buffer.Length;

            UInt64[] b = new UInt64[4];
            UInt64[] s = new UInt64[] { 0ul, 0ul, 0ul, (UInt64)count };

            if (count == 0)
                goto Finalize;

            UInt64 seed = (UInt64)m_Seed;

            while ((count - offset) >= 32)
            {
                b = BinaryOperations.ReadArray64(buffer, offset, 4);
                Pengy(0ul, b, ref s);

                offset += 32;
            }

            Span<Byte> residue = new Span<Byte>(BinaryOperations.ToArray64(b));
            buffer.Slice(offset).CopyTo(residue);
            b = BinaryOperations.ReadArray64(residue, 0, 4);

            for (Int32 i = 0; i < 6; ++i)
                Pengy(seed, b, ref s);

            Finalize:

            UInt64 hash = s[0] + s[1] + s[2] + s[3];
            Byte[] result = BinaryOperations.ToArray64(hash);

            return result;
        }
        #endregion
    }
}