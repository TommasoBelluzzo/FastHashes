#region Using Directives
using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
#endregion

namespace FastHashes
{
    /// <summary>Represents the base class from which all the SpookyHash implementations must derive. This class is abstract.</summary>
    public abstract class SpookyHash<T> : Hash<T> where T : unmanaged
    {
        #region Constants
        private const UInt64 C = 0xDEADBEEFDEADBEEFul;
        #endregion

        #region Members
        private readonly UInt64 m_Seed1;
        private readonly UInt64 m_Seed2;
        #endregion

        #region Properties
        /// <summary>Gets the first seed used by the hashing algorithm.</summary>
        /// <value>An <see cref="T:System.UInt64"/> value.</value>
        [ExcludeFromCodeCoverage]
        public UInt64 Seed1 => m_Seed1;

        /// <summary>Gets the second seed used by the hashing algorithm.</summary>
        /// <value>An <see cref="T:System.UInt64"/> value.</value>
        [ExcludeFromCodeCoverage]
        public UInt64 Seed2 => m_Seed2;
        #endregion

        #region Constructors
        /// <summary>Represents the base constructor used by derived classes.</summary>
        /// <param name="seed1">The first <see cref="T:System.UInt64"/> seed used by the hashing algorithm.</param>
        /// <param name="seed2">The second <see cref="T:System.UInt64"/> seed used by the hashing algorithm.</param>
        [ExcludeFromCodeCoverage]
        protected SpookyHash(UInt64 seed1, UInt64 seed2)
        {
            m_Seed1 = seed1;
            m_Seed2 = seed2;
        }
        #endregion

        #region Methods
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void LongEnd(Span<UInt64> hash)
        {
            hash[11] = unchecked(hash[11] + hash[1]);
            hash[2] ^= hash[11];
            hash[1] = BinaryOperations.RotateLeft(hash[1], 44);

            hash[0] = unchecked(hash[0] + hash[2]);
            hash[3] ^= hash[0];
            hash[2] = BinaryOperations.RotateLeft(hash[2], 15);

            hash[1] = unchecked(hash[1] + hash[3]);
            hash[4] ^= hash[1];
            hash[3] = BinaryOperations.RotateLeft(hash[3], 34);

            hash[2] = unchecked(hash[2] + hash[4]);
            hash[5] ^= hash[2];
            hash[4] = BinaryOperations.RotateLeft(hash[4], 21);

            hash[3] = unchecked(hash[3] + hash[5]);
            hash[6] ^= hash[3];
            hash[5] = BinaryOperations.RotateLeft(hash[5], 38);

            hash[4] = unchecked(hash[4] + hash[6]);
            hash[7] ^= hash[4];
            hash[6] = BinaryOperations.RotateLeft(hash[6], 33);

            hash[5] = unchecked(hash[5] + hash[7]);
            hash[8] ^= hash[5];
            hash[7] = BinaryOperations.RotateLeft(hash[7], 10);

            hash[6] = unchecked(hash[6] + hash[8]);
            hash[9] ^= hash[6];
            hash[8] = BinaryOperations.RotateLeft(hash[8], 13);

            hash[7] = unchecked(hash[7] + hash[9]);
            hash[10] ^= hash[7];
            hash[9] = BinaryOperations.RotateLeft(hash[9], 38);

            hash[8] = unchecked(hash[8] + hash[10]);
            hash[11] ^= hash[8];
            hash[10] = BinaryOperations.RotateLeft(hash[10], 53);

            hash[9] = unchecked(hash[9] + hash[11]);
            hash[0] ^= hash[9];
            hash[11] = BinaryOperations.RotateLeft(hash[11], 42);

            hash[10] = unchecked(hash[10] + hash[0]);
            hash[1] ^= hash[10];
            hash[0] = BinaryOperations.RotateLeft(hash[0], 54);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void LongMix(Span<UInt64> hash, ReadOnlySpan<UInt64> v)
        {
            hash[0] = unchecked(hash[0] + v[0]);
            hash[2] ^= hash[10];
            hash[11] ^= hash[0];
            hash[0] = BinaryOperations.RotateLeft(hash[0], 11);
            hash[11] = unchecked(hash[11] + hash[1]);

            hash[1] = unchecked(hash[1] + v[1]);
            hash[3] ^= hash[11];
            hash[0] ^= hash[1];
            hash[1] = BinaryOperations.RotateLeft(hash[1], 32);
            hash[0] = unchecked(hash[0] + hash[2]);

            hash[2] = unchecked(hash[2] + v[2]);
            hash[4] ^= hash[0];
            hash[1] ^= hash[2];
            hash[2] = BinaryOperations.RotateLeft(hash[2], 43);
            hash[1] = unchecked(hash[1] + hash[3]);

            hash[3] = unchecked(hash[3] + v[3]);
            hash[5] ^= hash[1];
            hash[2] ^= hash[3];
            hash[3] = BinaryOperations.RotateLeft(hash[3], 31);
            hash[2] = unchecked(hash[2] + hash[4]);

            hash[4] = unchecked(hash[4] + v[4]);
            hash[6] ^= hash[2];
            hash[3] ^= hash[4];
            hash[4] = BinaryOperations.RotateLeft(hash[4], 17);
            hash[3] = unchecked(hash[3] + hash[5]);

            hash[5] = unchecked(hash[5] + v[5]);
            hash[7] ^= hash[3];
            hash[4] ^= hash[5];
            hash[5] = BinaryOperations.RotateLeft(hash[5], 28);
            hash[4] = unchecked(hash[4] + hash[6]);

            hash[6] = unchecked(hash[6] + v[6]);
            hash[8] ^= hash[4];
            hash[5] ^= hash[6];
            hash[6] = BinaryOperations.RotateLeft(hash[6], 39);
            hash[5] = unchecked(hash[5] + hash[7]);

            hash[7] = unchecked(hash[7] + v[7]);
            hash[9] ^= hash[5];
            hash[6] ^= hash[7];
            hash[7] = BinaryOperations.RotateLeft(hash[7], 57);
            hash[6] = unchecked(hash[6] + hash[8]);

            hash[8] = unchecked(hash[8] + v[8]);
            hash[10] ^= hash[6];
            hash[7] ^= hash[8];
            hash[8] = BinaryOperations.RotateLeft(hash[8], 55);
            hash[7] = unchecked(hash[7] + hash[9]);

            hash[9] = unchecked(hash[9] + v[9]);
            hash[11] ^= hash[7];
            hash[8] ^= hash[9];
            hash[9] = BinaryOperations.RotateLeft(hash[9], 54);
            hash[8] = unchecked(hash[8] + hash[10]);

            hash[10] = unchecked(hash[10] + v[10]);
            hash[0] ^= hash[8];
            hash[9] ^= hash[10];
            hash[10] = BinaryOperations.RotateLeft(hash[10], 22);
            hash[9] = unchecked(hash[9] + hash[11]);

            hash[11] = unchecked(hash[11] + v[11]);
            hash[1] ^= hash[9];
            hash[10] ^= hash[11];
            hash[11] = BinaryOperations.RotateLeft(hash[11], 46);
            hash[10] = unchecked(hash[10] + hash[0]);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void ShortEnd(Span<UInt64> hash)
        {
            hash[3] ^= hash[2];
            hash[2] = BinaryOperations.RotateLeft(hash[2], 15);
            hash[3] = unchecked(hash[3] + hash[2]);

            hash[0] ^= hash[3];
            hash[3] = BinaryOperations.RotateLeft(hash[3], 52);
            hash[0] = unchecked(hash[0] + hash[3]);

            hash[1] ^= hash[0];
            hash[0] = BinaryOperations.RotateLeft(hash[0], 26);
            hash[1] = unchecked(hash[1] + hash[0]);

            hash[2] ^= hash[1];
            hash[1] = BinaryOperations.RotateLeft(hash[1], 51);
            hash[2] = unchecked(hash[2] + hash[1]);

            hash[3] ^= hash[2];
            hash[2] = BinaryOperations.RotateLeft(hash[2], 28);
            hash[3] = unchecked(hash[3] + hash[2]);

            hash[0] ^= hash[3];
            hash[3] = BinaryOperations.RotateLeft(hash[3], 9);
            hash[0] = unchecked(hash[0] + hash[3]);

            hash[1] ^= hash[0];
            hash[0] = BinaryOperations.RotateLeft(hash[0], 47);
            hash[1] = unchecked(hash[1] + hash[0]);

            hash[2] ^= hash[1];
            hash[1] = BinaryOperations.RotateLeft(hash[1], 54);
            hash[2] = unchecked(hash[2] + hash[1]);

            hash[3] ^= hash[2];
            hash[2] = BinaryOperations.RotateLeft(hash[2], 32);
            hash[3] = unchecked(hash[3] + hash[2]);

            hash[0] ^= hash[3];
            hash[3] = BinaryOperations.RotateLeft(hash[3], 25);
            hash[0] = unchecked(hash[0] + hash[3]);

            hash[1] ^= hash[0];
            hash[0] = BinaryOperations.RotateLeft(hash[0], 63);
            hash[1] = unchecked(hash[1] + hash[0]);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void ShortMix(Span<UInt64> hash)
        {
            hash[2] = BinaryOperations.RotateLeft(hash[2], 50);
            hash[2] = unchecked(hash[2] + hash[3]);
            hash[0] ^= hash[2];

            hash[3] = BinaryOperations.RotateLeft(hash[3], 52);
            hash[3] = unchecked(hash[3] + hash[0]);
            hash[1] ^= hash[3];

            hash[0] = BinaryOperations.RotateLeft(hash[0], 30);
            hash[0] = unchecked(hash[0] + hash[1]);
            hash[2] ^= hash[0];

            hash[1] = BinaryOperations.RotateLeft(hash[1], 41);
            hash[1] = unchecked(hash[1] + hash[2]);
            hash[3] ^= hash[1];

            hash[2] = BinaryOperations.RotateLeft(hash[2], 54);
            hash[2] = unchecked(hash[2] + hash[3]);
            hash[0] ^= hash[2];

            hash[3] = BinaryOperations.RotateLeft(hash[3], 48);
            hash[3] = unchecked(hash[3] + hash[0]);
            hash[1] ^= hash[3];

            hash[0] = BinaryOperations.RotateLeft(hash[0], 38);
            hash[0] = unchecked(hash[0] + hash[1]);
            hash[2] ^= hash[0];

            hash[1] = BinaryOperations.RotateLeft(hash[1], 37);
            hash[1] = unchecked(hash[1] + hash[2]);
            hash[3] ^= hash[1];

            hash[2] = BinaryOperations.RotateLeft(hash[2], 62);
            hash[2] = unchecked(hash[2] + hash[3]);
            hash[0] ^= hash[2];

            hash[3] = BinaryOperations.RotateLeft(hash[3], 34);
            hash[3] = unchecked(hash[3] + hash[0]);
            hash[1] ^= hash[3];

            hash[0] = BinaryOperations.RotateLeft(hash[0], 5);
            hash[0] = unchecked(hash[0] + hash[1]);
            hash[2] ^= hash[0];

            hash[1] = BinaryOperations.RotateLeft(hash[1], 36);
            hash[1] = unchecked(hash[1] + hash[2]);
            hash[3] ^= hash[1];
        }

        private (UInt64, UInt64) ComputeHashLong(ReadOnlySpan<Byte> buffer)
        {
            Int32 offset = 0;
            Int32 count = buffer.Length;

            Int32 blocks = count / 96;
            Int32 remainder = count % 96;

            Span<UInt64> hash = stackalloc UInt64[12];
            hash[0] = hash[3] = hash[6] = hash[9] = m_Seed1;
            hash[1] = hash[4] = hash[7] = hash[10] = m_Seed2;
            hash[2] = hash[5] = hash[8] = hash[11] = C;

            Span<UInt64> v = stackalloc UInt64[12];

            for (Int32 blockIndex = 0; blockIndex < blocks; ++blockIndex)
            {
                for (Int32 i = 0; i < 12; ++i)
                    v[i] = BinaryOperations.Read64(buffer, offset + (i * 8));

                offset += 96;

                LongMix(hash, v);
            }

            Span<Byte> data = stackalloc Byte[96];
            data.Clear();

            buffer.Slice(offset, remainder).CopyTo(data);
            data[95] = (Byte)remainder;

            for (Int32 i = 0; i < 12; ++i)
                v[i] = BinaryOperations.Read64(data, i * 8);

            for (Int32 i = 0; i < 12; ++i)
                hash[i] = unchecked(hash[i] + v[i]);

            for (Int32 i = 0; i < 3; ++i)
                LongEnd(hash);

            return (hash[0], hash[1]);
        }

        private (UInt64, UInt64) ComputeHashShort(ReadOnlySpan<Byte> buffer)
        {
            Int32 offset = 0;
            Int32 count = buffer.Length;

            Span<UInt64> hash = stackalloc UInt64[4] { m_Seed1, m_Seed2, C, C };

            Int32 remainder = count % 32;

            if (count > 15)
            {
                Int32 blocks = count / 32;

                for (Int32 blockIndex = 0; blockIndex < blocks; ++blockIndex)
                {
                    hash[2] = unchecked(hash[2] + BinaryOperations.Read64(buffer, offset));
                    offset += 8;
                    hash[3] = unchecked(hash[3] + BinaryOperations.Read64(buffer, offset));
                    offset += 8;

                    ShortMix(hash);

                    hash[0] = unchecked(hash[0] + BinaryOperations.Read64(buffer, offset));
                    offset += 8;
                    hash[1] = unchecked(hash[1] + BinaryOperations.Read64(buffer, offset));
                    offset += 8;
                }

                if (remainder >= 16)
                {
                    hash[2] = unchecked(hash[2] + BinaryOperations.Read64(buffer, offset));
                    offset += 8;
                    hash[3] = unchecked(hash[3] + BinaryOperations.Read64(buffer, offset));
                    offset += 8;

                    ShortMix(hash);

                    remainder -= 16;
                }
            }

            hash[3] = unchecked(hash[3] + ((UInt64)count << 56));

            switch (remainder)
            {
                case 15: hash[3] = unchecked(hash[3] + ((UInt64)buffer[offset + 14] << 48)); goto case 14;
                case 14: hash[3] = unchecked(hash[3] + ((UInt64)buffer[offset + 13] << 40)); goto case 13;
                case 13: hash[3] = unchecked(hash[3] + ((UInt64)buffer[offset + 12] << 32)); goto case 12;
                case 12:
                    hash[2] = unchecked(hash[2] + BinaryOperations.Read64(buffer, offset));
                    offset += 8;
                    hash[3] = unchecked(hash[3] + BinaryOperations.Read32(buffer, offset));
                    break;
                case 11: hash[3] = unchecked(hash[3] + ((UInt64)buffer[offset + 10] << 16)); goto case 10;
                case 10: hash[3] = unchecked(hash[3] + ((UInt64)buffer[offset + 9] << 8)); goto case 9;
                case 9: hash[3] = unchecked(hash[3] + buffer[offset + 8]); goto case 8;
                case 8:
                    hash[2] = unchecked(hash[2] + BinaryOperations.Read64(buffer, offset));
                    break;
                case 7: hash[2] = unchecked(hash[2] + ((UInt64)buffer[offset + 6] << 48)); goto case 6;
                case 6: hash[2] = unchecked(hash[2] + ((UInt64)buffer[offset + 5] << 40)); goto case 5;
                case 5: hash[2] = unchecked(hash[2] + ((UInt64)buffer[offset + 4] << 32)); goto case 4;
                case 4:
                    hash[2] = unchecked(hash[2] + BinaryOperations.Read32(buffer, offset));
                    break;
                case 3: hash[2] = unchecked(hash[2] + ((UInt64)buffer[offset + 2] << 16)); goto case 2;
                case 2: hash[2] = unchecked(hash[2] + ((UInt64)buffer[offset + 1] << 8)); goto case 1;
                case 1:
                    hash[2] = unchecked(hash[2] + buffer[offset]);
                    break;
                case 0:
                    hash[2] = unchecked(hash[2] + C);
                    hash[3] = unchecked(hash[3] + C);
                    break;
            }

            ShortEnd(hash);

            return (hash[0], hash[1]);
        }

        /// <summary>Represents an auxiliary hashing function used by derived classes.</summary>
        protected (UInt64, UInt64) ComputeHashParts(ReadOnlySpan<Byte> buffer)
        {
            return buffer.Length < 192
                ? ComputeHashShort(buffer)
                : ComputeHashLong(buffer);
        }
        #endregion
    }

    /// <summary>Represents the SpookyHash32 implementation. This class cannot be derived.</summary>
    public sealed class SpookyHash32 : SpookyHash<UInt32>
    {
        #region Constructors
        /// <summary>Initializes a new instance using a value of <c>0</c> for both seeds.</summary>
        [ExcludeFromCodeCoverage]
        public SpookyHash32() : base(0ul, 0ul) { }

        /// <summary>Initializes a new instance using the specified value for both seeds.</summary>
        /// <param name="seed">The <see cref="T:System.UInt64"/> seed used by the hashing algorithm.</param>
        [ExcludeFromCodeCoverage]
        public SpookyHash32(UInt32 seed) : base(seed, seed) { }

        /// <summary>Initializes a new instance using the specified seeds.</summary>
        /// <param name="seed1">The first <see cref="T:System.UInt64"/> seed used by the hashing algorithm.</param>
        /// <param name="seed2">The second <see cref="T:System.UInt64"/> seed used by the hashing algorithm.</param>
        [ExcludeFromCodeCoverage]
        public SpookyHash32(UInt64 seed1, UInt64 seed2) : base(seed1, seed2) { }
        #endregion

        #region Methods
        /// <inheritdoc/>
        protected override UInt32 ComputeHashValueInternal(ReadOnlySpan<Byte> buffer)
        {
            (UInt64 hash64, _) = ComputeHashParts(buffer);
            UInt32 hash = unchecked((UInt32)hash64);

            return hash;
        }
        #endregion
    }

    /// <summary>Represents the SpookyHash64 implementation. This class cannot be derived.</summary>
    public sealed class SpookyHash64 : SpookyHash<UInt64>
    {
        #region Constructors
        /// <summary>Initializes a new instance using a value of <c>0</c> for both seeds.</summary>
        [ExcludeFromCodeCoverage]
        public SpookyHash64() : base(0ul, 0ul) { }

        /// <summary>Initializes a new instance using the specified value for both seeds.</summary>
        /// <param name="seed">The <see cref="T:System.UInt64"/> seed used by the hashing algorithm.</param>
        [ExcludeFromCodeCoverage]
        public SpookyHash64(UInt64 seed) : base(seed, seed) { }

        /// <summary>Initializes a new instance using the specified seeds.</summary>
        /// <param name="seed1">The first <see cref="T:System.UInt64"/> seed used by the hashing algorithm.</param>
        /// <param name="seed2">The second <see cref="T:System.UInt64"/> seed used by the hashing algorithm.</param>
        [ExcludeFromCodeCoverage]
        public SpookyHash64(UInt64 seed1, UInt64 seed2) : base(seed1, seed2) { }
        #endregion

        #region Methods
        /// <inheritdoc/>
        protected override UInt64 ComputeHashValueInternal(ReadOnlySpan<Byte> buffer)
        {
            (UInt64 hash, _) = ComputeHashParts(buffer);
            return hash;
        }
        #endregion
    }

    /// <summary>Represents the SpookyHash128 implementation. This class cannot be derived.</summary>
    public sealed class SpookyHash128 : SpookyHash<(UInt64, UInt64)>
    {
        #region Constructors
        /// <summary>Initializes a new instance using a value of <c>0</c> for both seeds.</summary>
        [ExcludeFromCodeCoverage]
        public SpookyHash128() : base(0ul, 0ul) { }

        /// <summary>Initializes a new instance using the specified value for both seeds.</summary>
        /// <param name="seed">The <see cref="T:System.UInt64"/> seed used by the hashing algorithm.</param>
        [ExcludeFromCodeCoverage]
        public SpookyHash128(UInt64 seed) : base(seed, seed) { }

        /// <summary>Initializes a new instance using the specified seeds.</summary>
        /// <param name="seed1">The first <see cref="T:System.UInt64"/> seed used by the hashing algorithm.</param>
        /// <param name="seed2">The second <see cref="T:System.UInt64"/> seed used by the hashing algorithm.</param>
        [ExcludeFromCodeCoverage]
        public SpookyHash128(UInt64 seed1, UInt64 seed2) : base(seed1, seed2) { }
        #endregion

        #region Methods
        /// <inheritdoc/>
        protected override (UInt64, UInt64) ComputeHashValueInternal(ReadOnlySpan<Byte> buffer)
        {
            return ComputeHashParts(buffer);
        }
        #endregion
    }
}