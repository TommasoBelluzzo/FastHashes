#region Using Directives
using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
#endregion

namespace FastHashes
{
    /// <summary>Represents the xxHash32 implementation. This class cannot be derived.</summary>
    public sealed class XxHash32 : Hash
    {
        #region Constants
        private const UInt32 P1 = 0x9E3779B1u;
        private const UInt32 P2 = 0x85EBCA77u;
        private const UInt32 P3 = 0xC2B2AE3Du;
        private const UInt32 P4 = 0x27D4EB2Fu;
        private const UInt32 P5 = 0x165667B1u;
        #endregion

        #region Members
        private readonly UInt32 m_Seed;
        #endregion

        #region Properties
        /// <inheritdoc/>
        [ExcludeFromCodeCoverage]
        public override Int32 Length => 32;

        /// <summary>Gets the seed used by the hashing algorithm.</summary>
        /// <value>An <see cref="T:System.UInt64"/> value.</value>
        [ExcludeFromCodeCoverage]
        public UInt64 Seed => m_Seed;
        #endregion

        #region Constructors
        /// <summary>Initializes a new instance using the specified seed.</summary>
        /// <param name="seed">The <see cref="T:System.UInt64"/> seed used by the hashing algorithm.</param>
        [ExcludeFromCodeCoverage]
        public XxHash32(UInt32 seed)
        {
            m_Seed = seed;
        }

        /// <summary>Initializes a new instance using a seed value of <c>0</c>.</summary>
        [ExcludeFromCodeCoverage]
        public XxHash32() : this(0u) { }
        #endregion

        #region Methods
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static UInt32 Add(UInt32 v1, UInt32 v2, UInt32 v3, UInt32 v4)
        {
            v1 = BinaryOperations.RotateLeft(v1, 1);
            v2 = BinaryOperations.RotateLeft(v2, 7);
            v3 = BinaryOperations.RotateLeft(v3, 12);
            v4 = BinaryOperations.RotateLeft(v4, 18);

            return v1 + v2 + v3 + v4;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static UInt32 Mix(UInt32 v, UInt32 p1, UInt32 p2, Int32 r, UInt32 k)
        {
            v += k * p1;
            v = BinaryOperations.RotateLeft(v, r) * p2;

            return v;
        }
        #endregion

        #region Pointer/Span Fork
        #if NETSTANDARD2_1_OR_GREATER
        /// <inheritdoc/>
        protected override Byte[] ComputeHashInternal(ReadOnlySpan<Byte> buffer)
        {
            Int32 offset = 0;
            Int32 count = buffer.Length;

            UInt32 hash = m_Seed;

            if (count == 0)
            {
                hash += P5;
                goto Finalize;
            }

            Int32 blocks = count / 16;
            Int32 remainder = count & 15;

            if (blocks > 0)
            {
                UInt32 v1 = hash + P1 + P2;
                UInt32 v2 = hash + P2;
                UInt32 v3 = hash;
                UInt32 v4 = hash - P1;

                while (blocks-- > 0)
                {
                    v1 = Mix(v1, P2, P1, 13, BinaryOperations.Read32(buffer, offset));
                    offset += 4;
                    v2 = Mix(v2, P2, P1, 13, BinaryOperations.Read32(buffer, offset));
                    offset += 4;
                    v3 = Mix(v3, P2, P1, 13, BinaryOperations.Read32(buffer, offset));
                    offset += 4;
                    v4 = Mix(v4, P2, P1, 13, BinaryOperations.Read32(buffer, offset));
                    offset += 4;
                }

                hash = Add(v1, v2, v3, v4);
            }
            else
                hash += P5;

            hash += (UInt32)count;

            if (remainder > 0)
            {
                blocks = remainder / 4;
                remainder &= 3;

                while (blocks-- > 0)
                {
                    hash = Mix(hash, P3, P4, 17, BinaryOperations.Read32(buffer, offset));
                    offset += 4;
                }
            }

            for (Int32 i = 0; i < remainder; ++i)
                hash = Mix(hash, P5, P1, 11, buffer[offset + i]);

            Finalize:

            hash ^= hash >> 15;
            hash *= P2;
            hash ^= hash >> 13;
            hash *= P3;
            hash ^= hash >> 16;

            Byte[] result = BinaryOperations.ToArray64(hash);

            return result;
        }
        #else
        /// <inheritdoc/>
        protected override Byte[] ComputeHashInternal(Byte[] buffer, Int32 offset, Int32 count)
        {
            UInt32 hash = m_Seed;

            if (count == 0)
            {
                hash += P5;
                goto Finalize;
            }

            unsafe
            {
                fixed (Byte* pin = &buffer[offset])
                {
                    Byte* pointer = pin;

                    Int32 blocks = count / 16;
                    Int32 remainder = count & 15;

                    if (blocks > 0)
                    {
                        UInt32 v1 = hash + P1 + P2;
                        UInt32 v2 = hash + P2;
                        UInt32 v3 = hash;
                        UInt32 v4 = hash - P1;

                        while (blocks-- > 0)
                        {
                            v1 = Mix(v1, P2, P1, 13, BinaryOperations.Read32(pointer));
                            pointer += 4;
                            v2 = Mix(v2, P2, P1, 13, BinaryOperations.Read32(pointer));
                            pointer += 4;
                            v3 = Mix(v3, P2, P1, 13, BinaryOperations.Read32(pointer));
                            pointer += 4;
                            v4 = Mix(v4, P2, P1, 13, BinaryOperations.Read32(pointer));
                            pointer += 4;
                        }

                        hash = Add(v1, v2, v3, v4);
                    }
                    else
                        hash += P5;

                    hash += (UInt32)count;

                    if (remainder > 0)
                    {
                        blocks = remainder / 4;
                        remainder &= 3;

                        while (blocks-- > 0)
                        {
                            hash = Mix(hash, P3, P4, 17, BinaryOperations.Read32(pointer));
                            pointer += 4;
                        }
                            
                    }

                    for (Int32 i = 0; i < remainder; ++i)
                        hash = Mix(hash, P5, P1, 11, pointer[i]);
                }
            }

            Finalize:

            hash ^= hash >> 15;
            hash *= P2;
            hash ^= hash >> 13;
            hash *= P3;
            hash ^= hash >> 16;

            Byte[] result = BinaryOperations.ToArray64(hash);

            return result;
        }
        #endif
        #endregion
    }

    /// <summary>Represents the xxHash64 implementation. This class cannot be derived.</summary>
    public sealed class XxHash64 : Hash
    {
        #region Constants
        private const UInt64 P1 = 0x9E3779B185EBCA87ul;
        private const UInt64 P2 = 0xC2B2AE3D27D4EB4Ful;
        private const UInt64 P3 = 0x165667B19E3779F9ul;
        private const UInt64 P4 = 0x85EBCA77C2B2AE63ul;
        private const UInt64 P5 = 0x27D4EB2F165667C5ul;
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
        public XxHash64(UInt64 seed)
        {
            m_Seed = seed;
        }

        /// <summary>Initializes a new instance using a seed value of <c>0</c>.</summary>
        [ExcludeFromCodeCoverage]
        public XxHash64() : this(0ul) { }
        #endregion

        #region Methods
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static UInt64 Add(UInt64 v1, UInt64 v2, UInt64 v3, UInt64 v4)
        {
            v1 = BinaryOperations.RotateLeft(v1, 1);
            v2 = BinaryOperations.RotateLeft(v2, 7);
            v3 = BinaryOperations.RotateLeft(v3, 12);
            v4 = BinaryOperations.RotateLeft(v4, 18);

            return v1 + v2 + v3 + v4;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static UInt64 Mix8(UInt64 v1, Byte v2)
        {
            v1 ^= v2 * P5;
            v1 = BinaryOperations.RotateLeft(v1, 11);
            v1 *= P1;

            return v1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static UInt64 Mix32(UInt64 v1, UInt32 v2)
        {
            v1 ^= v2 * P1;
            v1 = BinaryOperations.RotateLeft(v1, 23);
            v1 *= P2;
            v1 += P3;

            return v1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static UInt64 Mix64(UInt64 v1, UInt64 v2)
        {
            v2 *= P2;
            v2 = BinaryOperations.RotateLeft(v2, 31);
            v2 *= P1;

            v1 ^= v2;
            v1 = BinaryOperations.RotateLeft(v1, 27);
            v1 *= P1;
            v1 += P4;

            return v1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static UInt64 Mix256A(UInt64 v, UInt64 p1, UInt64 p2, UInt64 k)
        {
            v += k * p1;
            v = BinaryOperations.RotateLeft(v, 31);
            v *= p2;

            return v;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static UInt64 Mix256B(UInt64 v1, UInt64 v2)
        {
            v2 *= P2;
            v2 = BinaryOperations.RotateLeft(v2, 31);
            v2 *= P1;

            v1 ^= v2;
            v1 *= P1;
            v1 += P4;

            return v1;
        }
        #endregion

        #region Pointer/Span Fork
        #if NETSTANDARD2_1_OR_GREATER
        /// <inheritdoc/>
        protected override Byte[] ComputeHashInternal(ReadOnlySpan<Byte> buffer)
        {
            Int32 offset = 0;
            Int32 count = buffer.Length;

            UInt64 hash = m_Seed;

            if (count == 0)
            {
                hash += P5;
                goto Finalize;
            }

            Int32 blocks = count / 32;
            Int32 remainder = count & 31;

            if (blocks > 0)
            {
                UInt64 v1 = hash + P1 + P2;
                UInt64 v2 = hash + P2;
                UInt64 v3 = hash;
                UInt64 v4 = hash - P1;

                while (blocks-- > 0)
                {
                    v1 = Mix256A(v1, P2, P1, BinaryOperations.Read64(buffer, offset));
                    offset += 8;
                    v2 = Mix256A(v2, P2, P1, BinaryOperations.Read64(buffer, offset));
                    offset += 8;
                    v3 = Mix256A(v3, P2, P1, BinaryOperations.Read64(buffer, offset));
                    offset += 8;
                    v4 = Mix256A(v4, P2, P1, BinaryOperations.Read64(buffer, offset));
                    offset += 8;
                }

                hash = Add(v1, v2, v3, v4);

                hash = Mix256B(hash, v1);
                hash = Mix256B(hash, v2);
                hash = Mix256B(hash, v3);
                hash = Mix256B(hash, v4);
            }
            else
                hash += P5;

            hash += (UInt64)count;

            if (remainder > 0)
            {
                blocks = remainder / 8;
                remainder &= 7;

                while (blocks-- > 0)
                {
                    hash = Mix64(hash, BinaryOperations.Read64(buffer, offset));
                    offset += 8;
                }
            }

            if (remainder > 0)
            {
                blocks = remainder / 4;
                remainder &= 3;

                while (blocks-- > 0)
                {
                    hash = Mix32(hash, BinaryOperations.Read32(buffer, offset));
                    offset += 4;
                }
            }

            for (Int32 i = 0; i < remainder; ++i)
                hash = Mix8(hash, buffer[offset + i]);

            Finalize:

            hash ^= hash >> 33;
            hash *= P2;
            hash ^= hash >> 29;
            hash *= P3;
            hash ^= hash >> 32;

            Byte[] result = BinaryOperations.ToArray64(hash);

            return result;
        }
        #else
        /// <inheritdoc/>
        protected override Byte[] ComputeHashInternal(Byte[] buffer, Int32 offset, Int32 count)
        {
            UInt64 hash = m_Seed;

            if (count == 0)
            {
                hash += P5;
                goto Finalize;
            }

            unsafe
            {
                fixed (Byte* pin = &buffer[offset])
                {
                    Byte* pointer = pin;

                    Int32 blocks = count / 32;
                    Int32 remainder = count & 31;

                    if (blocks > 0)
                    {
                        UInt64 v1 = hash + P1 + P2;
                        UInt64 v2 = hash + P2;
                        UInt64 v3 = hash;
                        UInt64 v4 = hash - P1;

                        while (blocks-- > 0)
                        {
                            v1 = Mix256A(v1, P2, P1, BinaryOperations.Read64(pointer));
                            pointer += 8;
                            v2 = Mix256A(v2, P2, P1, BinaryOperations.Read64(pointer));
                            pointer += 8;
                            v3 = Mix256A(v3, P2, P1, BinaryOperations.Read64(pointer));
                            pointer += 8;
                            v4 = Mix256A(v4, P2, P1, BinaryOperations.Read64(pointer));
                            pointer += 8;
                        }

                        hash = Add(v1, v2, v3, v4);

                        hash = Mix256B(hash, v1);
                        hash = Mix256B(hash, v2);
                        hash = Mix256B(hash, v3);
                        hash = Mix256B(hash, v4);
                    }
                    else
                        hash += P5;

                    hash += (UInt64)count;

                    if (remainder > 0)
                    {
                        blocks = remainder / 8;
                        remainder &= 7;

                        while (blocks-- > 0)
                        {
                            hash = Mix64(hash, BinaryOperations.Read64(pointer));
                            pointer += 8;
                        }
                    }

                    if (remainder > 0)
                    {
                        blocks = remainder / 4;
                        remainder &= 3;

                        while (blocks-- > 0)
                        {
                            hash = Mix32(hash, BinaryOperations.Read32(pointer));
                            pointer += 4;
                        } 
                    }

                    for (Int32 i = 0; i < remainder; ++i)
                        hash = Mix8(hash, pointer[i]);
                }
            }

            Finalize:

            hash ^= hash >> 33;
            hash *= P2;
            hash ^= hash >> 29;
            hash *= P3;
            hash ^= hash >> 32;

            Byte[] result = BinaryOperations.ToArray64(hash);

            return result;
        }
        #endif
        #endregion
    }
}