#region Using Directives
using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
#endregion

namespace FastHashes
{
    /// <summary>Represents the base class from which all the SpookyHash implementations must derive. This class is abstract.</summary>
    public abstract class SpookyHash : Hash
    {
        #region Constants
        private const UInt64 C = 0xDEADBEEFDEADBEEFul;
        private static readonly Int32[] LRE = { 44, 15, 34, 21, 38, 33, 10, 13, 38, 53, 42, 54 };
        private static readonly Int32[] LRM = { 11, 32, 43, 31, 17, 28, 39, 57, 55, 54, 22, 46 };
        private static readonly Int32[] SRE = { 15, 52, 26, 51, 28,  9, 47, 54, 32, 25, 63 };
        private static readonly Int32[] SRM = { 50, 52, 30, 41, 54, 48, 38, 37, 62, 34,  5, 36 };
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
        private static void LongEnd(ref UInt64[] hash)
        {
            for (Int32 j = 0; j < 12; ++j)
            {
                Int32 index1 = (j + 11) % 12;
                Int32 index2 = (j + 1) % 12;

                hash[index1] += hash[index2]; 
                hash[(j + 2) % 12] ^= hash[index1]; 
                hash[index2] = BinaryOperations.RotateLeft(hash[index2], LRE[j]);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void LongMix(ref UInt64[] hash, UInt64[] v)
        {
            for (Int32 i = 0; i < 12; ++i)
            {
                Int32 index = (i + 11) % 12;

                hash[i] += v[i]; 
                hash[(i + 2) % 12] ^= hash[(i + 10) % 12]; 
                hash[index] ^= hash[i];
                hash[i] = BinaryOperations.RotateLeft(hash[i], LRM[i]); 
                hash[index] += hash[(i + 1) % 12];
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void ShortEnd(ref UInt64[] hash)
        {
            for (Int32 i = 0; i < 11; ++i)
            {
                Int32 index1 = (i + 2) % 4;
                Int32 index2 = (i + 3) % 4;

                hash[index2] ^= hash[index1];
                hash[index1] = BinaryOperations.RotateLeft(hash[index1], SRE[i]);
                hash[index2] += hash[index1];
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void ShortMix(ref UInt64[] hash)
        {
            for (Int32 i = 0; i < 12; ++i)
            {
                Int32 index = (i + 2) % 4;

                hash[index] = BinaryOperations.RotateLeft(hash[index], SRM[i]);
                hash[index] += hash[(i + 3) % 4];
                hash[i % 4] ^= hash[index];
            }
        }

        private Byte[] ComputeHashLong(ReadOnlySpan<Byte> buffer)
        {
            Int32 offset = 0;
            Int32 count = buffer.Length;

            Int32 blocks = count / 96;
            Int32 blocksBytes = blocks * 96;

            Int32 dataLength = (blocks + 1) * 96;
            Span<Byte> data = new Span<Byte>(new Byte[dataLength]);

            buffer.Slice(0, blocksBytes).CopyTo(data);
            buffer.Slice(blocksBytes, count - blocksBytes).CopyTo(data.Slice(blocksBytes));
            data[dataLength - 1] = (Byte)(count % 96);

            UInt64[] hash = new UInt64[12];
            hash[0] = hash[3] = hash[6] = hash[9] = m_Seed1;
            hash[1] = hash[4] = hash[7] = hash[10] = m_Seed2;
            hash[2] = hash[5] = hash[8] = hash[11] = C;

            while (blocks-- > 0)
            {
                UInt64[] vb = BinaryOperations.ReadArray64(data, offset, 12);
                offset += 96;

                LongMix(ref hash, vb);
            }

            UInt64[] vr = BinaryOperations.ReadArray64(data, offset, 12);

            LongMix(ref hash, vr);

            for (Int32 i = 0; i < 3; ++i)
                LongEnd(ref hash);

            return GetHash(hash);
        }

        private Byte[] ComputeHashShort(ReadOnlySpan<Byte> buffer)
        {
            Int32 offset = 0;
            Int32 count = buffer.Length;

            UInt64[] hash = { m_Seed1, m_Seed2, C, C };

            Int32 remainder = count % 32;

            if (count > 15)
            {
                Int32 blocks = count / 32;

                while (blocks-- > 0)
                {
                    hash[2] += BinaryOperations.Read64(buffer, offset);
                    offset += 8;
                    hash[3] += BinaryOperations.Read64(buffer, offset);
                    offset += 8;

                    ShortMix(ref hash);

                    hash[0] += BinaryOperations.Read64(buffer, offset);
                    offset += 8;
                    hash[1] += BinaryOperations.Read64(buffer, offset);
                    offset += 8;
                }

                if (remainder >= 16)
                {
                    hash[2] += BinaryOperations.Read64(buffer, offset);
                    offset += 8;
                    hash[3] += BinaryOperations.Read64(buffer, offset);
                    offset += 8;

                    ShortMix(ref hash);

                    remainder -= 16;
                }
            }

            hash[3] = (UInt64)count << 56;

            switch (remainder)
            {
                case 15: hash[3] += (UInt64)buffer[offset + 14] << 48; goto case 14;
                case 14: hash[3] += (UInt64)buffer[offset + 13] << 40; goto case 13;
                case 13: hash[3] += (UInt64)buffer[offset + 12] << 32; goto case 12;
                case 12:
                    hash[2] += BinaryOperations.Read64(buffer, offset);
                    offset += 8;
                    hash[3] += BinaryOperations.Read32(buffer, offset);
                    break;
                case 11: hash[3] += (UInt64)buffer[offset + 10] << 16; goto case 10;
                case 10: hash[3] += (UInt64)buffer[offset + 9] << 8; goto case 9;
                case 9: hash[3] += buffer[offset + 8]; goto case 8;
                case 8:
                    hash[2] += BinaryOperations.Read64(buffer, offset);
                    break;
                case 7: hash[2] += (UInt64)buffer[offset + 6] << 48; goto case 6;
                case 6: hash[2] += (UInt64)buffer[offset + 5] << 40; goto case 5;
                case 5: hash[2] += (UInt64)buffer[offset + 4] << 32; goto case 4;
                case 4:
                    hash[2] += BinaryOperations.Read32(buffer, offset);
                    break;
                case 3: hash[2] += (UInt64)buffer[offset + 2] << 16; goto case 2;
                case 2: hash[2] += (UInt64)buffer[offset + 1] << 8; goto case 1;
                case 1:
                    hash[2] += buffer[offset];
                    break;
                case 0:
                    hash[2] += C;
                    hash[3] += C;
                    break;
            }

            ShortEnd(ref hash);

            Byte[] result = GetHash(hash);

            return result;
        }

        /// <summary>Finalizes any partial computation and returns the hash code.</summary>
        /// <param name="hashData">The <see cref="T:System.UInt64"/>[] representing the hash data.</param>
        /// <returns>A <see cref="T:System.Byte"/>[] representing the hash code.</returns>
        protected abstract Byte[] GetHash(UInt64[] hashData);

        /// <inheritdoc/>
        protected override Byte[] ComputeHashInternal(ReadOnlySpan<Byte> buffer)
        {
            if (buffer.Length < 192)
                return ComputeHashShort(buffer);

            return ComputeHashLong(buffer);
        }
        #endregion
    }

    /// <summary>Represents the SpookyHash32 implementation. This class cannot be derived.</summary>
    public sealed class SpookyHash32 : SpookyHash
    {
        #region Properties
        /// <inheritdoc/>
        [ExcludeFromCodeCoverage]
        public override Int32 Length => 32;
        #endregion

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
        protected override Byte[] GetHash(UInt64[] hashData)
        {
            UInt64 hash = hashData[0];
            Byte[] result = BinaryOperations.ToArray32(hash);

            return result;
        }
        #endregion
    }

    /// <summary>Represents the SpookyHash64 implementation. This class cannot be derived.</summary>
    public sealed class SpookyHash64 : SpookyHash
    {
        #region Properties
        /// <inheritdoc/>
        [ExcludeFromCodeCoverage]
        public override Int32 Length => 64;
        #endregion

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
        protected override Byte[] GetHash(UInt64[] hashData)
        {
            UInt64 hash = hashData[0];
            Byte[] result = BinaryOperations.ToArray64(hash);

            return result;
        }
        #endregion
    }

    /// <summary>Represents the SpookyHash128 implementation. This class cannot be derived.</summary>
    public sealed class SpookyHash128 : SpookyHash
    {
        #region Properties
        /// <inheritdoc/>
        [ExcludeFromCodeCoverage]
        public override Int32 Length => 128;
        #endregion

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
        protected override Byte[] GetHash(UInt64[] hashData)
        {
            Byte[] result = BinaryOperations.ToArray64(hashData);

            return result;
        }
        #endregion
    }
}