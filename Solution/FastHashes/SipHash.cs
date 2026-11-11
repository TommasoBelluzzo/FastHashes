#region Using Directives
using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
#endregion

namespace FastHashes
{
    /// <summary>Represents the base class from which all the HalfSipHash implementations must derive. This class is abstract.</summary>
    public abstract class HalfSipHash<T> : Hash<T> where T : unmanaged
    {
        #region Constants
        private const UInt32 I0 = 0x6C796765u;
        private const UInt32 I1 = 0x74656462u;
        #endregion

        #region Members
        private readonly UInt32 m_Seed1;
        private readonly UInt32 m_Seed2;
        #endregion

        #region Properties
        /// <summary>Gets the first seed used by the hashing algorithm.</summary>
        /// <value>An <see cref="T:System.UInt32"/> value.</value>
        [ExcludeFromCodeCoverage]
        public UInt32 Seed1 => m_Seed1;

        /// <summary>Gets the second seed used by the hashing algorithm.</summary>
        /// <value>An <see cref="T:System.UInt32"/> value.</value>
        [ExcludeFromCodeCoverage]
        public UInt32 Seed2 => m_Seed2;
        #endregion

        #region Constructors
        /// <summary>Represents the base constructor used by derived classes.</summary>
        /// <param name="seed1">The first <see cref="T:System.UInt32"/> seed used by the hashing algorithm.</param>
        /// <param name="seed2">The second <see cref="T:System.UInt32"/> seed used by the hashing algorithm.</param>
        [ExcludeFromCodeCoverage]
        protected HalfSipHash(UInt32 seed1, UInt32 seed2)
        {
            m_Seed1 = seed1;
            m_Seed2 = seed2;
        }
        #endregion

        #region Methods
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void Mix(ref UInt32 v0, ref UInt32 v1, ref UInt32 v2, ref UInt32 v3)
        {
            v0 = unchecked(v0 + v1);
            v1 = BinaryOperations.RotateLeft(v1, 5);
            v1 ^= v0;
            v0 = BinaryOperations.RotateLeft(v0, 16);

            v2 = unchecked(v2 + v3);
            v3 = BinaryOperations.RotateLeft(v3, 8);
            v3 ^= v2;

            v0 = unchecked(v0 + v3);
            v3 = BinaryOperations.RotateLeft(v3, 7);
            v3 ^= v0;

            v2 = unchecked(v2 + v1);
            v1 = BinaryOperations.RotateLeft(v1, 13);
            v1 ^= v2;
            v2 = BinaryOperations.RotateLeft(v2, 16);
        }

        /// <summary>Represents an auxiliary hashing function used by derived classes.</summary>
        protected (UInt32, UInt32) ComputeHashParts(ReadOnlySpan<Byte> buffer, Boolean extended)
        {
            Int32 offset = 0;
            Int32 count = buffer.Length;

            UInt32 b = (UInt32)count << 24;

            UInt32 v0 = m_Seed1;
            UInt32 v1 = m_Seed2;
            UInt32 v2 = I0 ^ m_Seed1;
            UInt32 v3 = I1 ^ m_Seed2;

            if (extended)
                v1 ^= 0x000000EEu;

            Int32 blocks = count / 4;

            for (Int32 blockIndex = 0; blockIndex < blocks; ++blockIndex)
            {
                UInt32 m = BinaryOperations.Read32(buffer, offset);
                offset += 4;

                v3 ^= m;

                for (Int32 i = 0; i < 2; ++i)
                    Mix(ref v0, ref v1, ref v2, ref v3);

                v0 ^= m;
            }

            if (offset < count)
                b |= BinaryOperations.ReadTail32(buffer, offset);

            v3 ^= b;

            for (Int32 i = 0; i < 2; ++i)
                Mix(ref v0, ref v1, ref v2, ref v3);

            v0 ^= b;
            v2 ^= extended ? 0x000000EEu : 0x000000FFu;

            for (Int32 i = 0; i < 4; ++i)
                Mix(ref v0, ref v1, ref v2, ref v3);

            UInt32 hash1 = v1 ^ v3;

            if (!extended)
                return (hash1, 0u);

            v1 ^= 0x000000DDu;

            for (Int32 i = 0; i < 4; ++i)
                Mix(ref v0, ref v1, ref v2, ref v3);

            UInt32 hash2 = v1 ^ v3;

            return (hash1, hash2);
        }
        #endregion
    }

    /// <summary>Represents the HalfSipHash32 implementation. This class cannot be derived.</summary>
    public sealed class HalfSipHash32 : HalfSipHash<UInt32>
    {
        #region Constructors
        /// <summary>Initializes a new instance using a value of <c>0</c> for both seeds.</summary>
        [ExcludeFromCodeCoverage]
        public HalfSipHash32() : base(0u, 0u) { }

        /// <summary>Initializes a new instance using the specified value for both seeds.</summary>
        /// <param name="seed">The <see cref="T:System.UInt32"/> seed used by the hashing algorithm.</param>
        [ExcludeFromCodeCoverage]
        public HalfSipHash32(UInt32 seed) : base(seed, seed) { }

        /// <summary>Initializes a new instance using the specified seeds.</summary>
        /// <param name="seed1">The first <see cref="T:System.UInt32"/> seed used by the hashing algorithm.</param>
        /// <param name="seed2">The second <see cref="T:System.UInt32"/> seed used by the hashing algorithm.</param>
        [ExcludeFromCodeCoverage]
        public HalfSipHash32(UInt32 seed1, UInt32 seed2) : base(seed1, seed2) { }
        #endregion

        #region Methods
        /// <inheritdoc/>
        protected override UInt32 ComputeHashValueInternal(ReadOnlySpan<Byte> buffer)
        {
            (UInt32 hash, _) = ComputeHashParts(buffer, false);
            return hash;
        }
        #endregion
    }

    /// <summary>Represents the HalfSipHash64 implementation. This class cannot be derived.</summary>
    public sealed class HalfSipHash64 : HalfSipHash<UInt64>
    {
        #region Constructors
        /// <summary>Initializes a new instance using a value of <c>0</c> for both seeds.</summary>
        [ExcludeFromCodeCoverage]
        public HalfSipHash64() : base(0u, 0u) { }

        /// <summary>Initializes a new instance using the specified value for both seeds.</summary>
        /// <param name="seed">The <see cref="T:System.UInt32"/> seed used by the hashing algorithm.</param>
        [ExcludeFromCodeCoverage]
        public HalfSipHash64(UInt32 seed) : base(seed, seed) { }

        /// <summary>Initializes a new instance using the specified seeds.</summary>
        /// <param name="seed1">The first <see cref="T:System.UInt32"/> seed used by the hashing algorithm.</param>
        /// <param name="seed2">The second <see cref="T:System.UInt32"/> seed used by the hashing algorithm.</param>
        [ExcludeFromCodeCoverage]
        public HalfSipHash64(UInt32 seed1, UInt32 seed2) : base(seed1, seed2) { }
        #endregion

        #region Methods
        /// <inheritdoc/>
        protected override UInt64 ComputeHashValueInternal(ReadOnlySpan<Byte> buffer)
        {
            (UInt32 hash1, UInt32 hash2) = ComputeHashParts(buffer, true);
            UInt64 hash = hash1 | ((UInt64)hash2 << 32);

            return hash;
        }
        #endregion
    }

    /// <summary>Represents the base class from which all the SipHash implementations must derive. This class is abstract.</summary>
    public abstract class SipHash<T> : Hash<T> where T : unmanaged
    {
        #region Constants
        private const UInt64 K0 = 0x736F6D6570736575ul;
        private const UInt64 K1 = 0x646F72616E646F6Dul;
        private const UInt64 K2 = 0x6C7967656E657261ul;
        private const UInt64 K3 = 0x7465646279746573ul;
        #endregion

        #region Members
        private readonly Int32 m_R1;
        private readonly Int32 m_R2;
        private readonly SipHashVariant m_Variant;
        private readonly UInt64 m_Seed1;
        private readonly UInt64 m_Seed2;
        #endregion

        #region Properties
        /// <summary>Gets the variant of the hashing algorithm.</summary>
        /// <value>An enumeration value of type <see cref="T:FastHashes.SipHashVariant"/>.</value>
        [ExcludeFromCodeCoverage]
        public SipHashVariant Variant => m_Variant;

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
        /// <param name="variant">The <see cref="T:FastHashes.SipHashVariant"/> value specifying the variant of the hashing algorithm.</param>
        /// <param name="seed1">The first <see cref="T:System.UInt32"/> seed used by the hashing algorithm.</param>
        /// <param name="seed2">The second <see cref="T:System.UInt32"/> seed used by the hashing algorithm.</param>
        [ExcludeFromCodeCoverage]
        protected SipHash(SipHashVariant variant, UInt64 seed1, UInt64 seed2)
        {
            if (!Enum.IsDefined(typeof(SipHashVariant), variant))
                throw new ArgumentOutOfRangeException(nameof(variant), "Invalid variant specified.");

            if (variant == SipHashVariant.V13)
            {
                m_R1 = 1;
                m_R2 = 3;
            }
            else
            {
                m_R1 = 2;
                m_R2 = 4;
            }

            m_Variant = variant;
            m_Seed1 = seed1;
            m_Seed2 = seed2;
        }
        #endregion

        #region Methods
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void Mix(ref UInt64 v0, ref UInt64 v1, ref UInt64 v2, ref UInt64 v3)
        {
            v0 = unchecked(v0 + v1);
            v2 = unchecked(v2 + v3);

            v1 = BinaryOperations.RotateLeft(v1, 13);
            v3 = BinaryOperations.RotateLeft(v3, 16);

            v1 ^= v0;
            v3 ^= v2;

            v0 = BinaryOperations.RotateLeft(v0, 32);

            v2 = unchecked(v2 + v1);
            v0 = unchecked(v0 + v3);

            v1 = BinaryOperations.RotateLeft(v1, 17);
            v3 = BinaryOperations.RotateLeft(v3, 21);

            v1 ^= v2;
            v3 ^= v0;

            v2 = BinaryOperations.RotateLeft(v2, 32);
        }

        /// <summary>Represents an auxiliary hashing function used by derived classes.</summary>
        protected (UInt64, UInt64) ComputeHashParts(ReadOnlySpan<Byte> buffer, Boolean extended)
        {
            Int32 offset = 0;
            Int32 count = buffer.Length;
            UInt64 b = (UInt64)(count & 0x000000FF) << 56;

            UInt64 v0 = m_Seed1 ^ K0;
            UInt64 v1 = m_Seed2 ^ K1;
            UInt64 v2 = m_Seed1 ^ K2;
            UInt64 v3 = m_Seed2 ^ K3;

            if (extended)
                v1 ^= 0x00000000000000EEul;

            Int32 blocks = count / 8;

            for (Int32 blockIndex = 0; blockIndex < blocks; ++blockIndex)
            {
                UInt64 m = BinaryOperations.Read64(buffer, offset);
                offset += 8;
                v3 ^= m;

                for (Int32 i = 0; i < m_R1; ++i)
                    Mix(ref v0, ref v1, ref v2, ref v3);

                v0 ^= m;
            }

            if (offset < count)
                b |= BinaryOperations.ReadTail64(buffer, offset);

            v3 ^= b;

            for (Int32 i = 0; i < m_R1; ++i)
                Mix(ref v0, ref v1, ref v2, ref v3);

            v0 ^= b;
            v2 ^= extended ? 0x00000000000000EEul : 0x00000000000000FFul;

            for (Int32 i = 0; i < m_R2; ++i)
                Mix(ref v0, ref v1, ref v2, ref v3);

            UInt64 hash1 = v0 ^ v1 ^ v2 ^ v3;

            if (!extended)
                return (hash1, 0ul);

            v1 ^= 0x00000000000000DDul;

            for (Int32 i = 0; i < m_R2; ++i)
                Mix(ref v0, ref v1, ref v2, ref v3);

            UInt64 hash2 = v0 ^ v1 ^ v2 ^ v3;

            return (hash1, hash2);
        }

        /// <inheritdoc/>
        [ExcludeFromCodeCoverage]
        public override String ToString()
        {
            return $"{GetType().Name}-{m_Variant.ToString().Substring(1)}";
        }
        #endregion
    }

    /// <summary>Represents the SipHash64 implementation. This class cannot be derived.</summary>
    public sealed class SipHash64 : SipHash<UInt64>
    {
        #region Constructors
        /// <summary>Initializes a new instance using <see cref="F:FastHashes.SipHashVariant.V24"/> and a value of <c>0</c> for both seeds.</summary>
        [ExcludeFromCodeCoverage]
        public SipHash64() : base(SipHashVariant.V24, 0ul, 0ul) { }

        /// <summary>Initializes a new instance using the specified variant and a value of <c>0</c> for both seeds.</summary>
        /// <param name="variant">The <see cref="T:FastHashes.SipHashVariant"/> value specifying the variant of the hashing algorithm.</param>
        /// <exception cref="T:System.ArgumentOutOfRangeException">Thrown when <paramref name="variant">variant</paramref> is not a defined enumeration value.</exception>
        public SipHash64(SipHashVariant variant) : base(variant, 0ul, 0ul) { }

        /// <summary>Initializes a new instance using <see cref="F:FastHashes.SipHashVariant.V24"/> and the specified value for both seeds.</summary>
        [ExcludeFromCodeCoverage]
        public SipHash64(UInt64 seed) : base(SipHashVariant.V24, seed, seed) { }

        /// <summary>Initializes a new instance using <see cref="F:FastHashes.SipHashVariant.V24"/> and the specified seeds.</summary>
        [ExcludeFromCodeCoverage]
        public SipHash64(UInt64 seed1, UInt64 seed2) : base(SipHashVariant.V24, seed1, seed2) { }

        /// <summary>Initializes a new instance using the specified variant and the specified value for both seeds.</summary>
        /// <exception cref="T:System.ArgumentOutOfRangeException">Thrown when <paramref name="variant">variant</paramref> is not a defined enumeration value.</exception>
        [ExcludeFromCodeCoverage]
        public SipHash64(SipHashVariant variant, UInt64 seed) : base(variant, seed, seed) { }

        /// <summary>Initializes a new instance using the specified variant and seeds.</summary>
        /// <param name="variant">The <see cref="T:FastHashes.SipHashVariant"/> value specifying the variant of the hashing algorithm.</param>
        /// <param name="seed1">The first <see cref="T:System.UInt64"/> seed used by the hashing algorithm.</param>
        /// <param name="seed2">The second <see cref="T:System.UInt64"/> seed used by the hashing algorithm.</param>
        /// <exception cref="T:System.ArgumentOutOfRangeException">Thrown when <paramref name="variant">variant</paramref> is not a defined enumeration value.</exception>
        [ExcludeFromCodeCoverage]
        public SipHash64(SipHashVariant variant, UInt64 seed1, UInt64 seed2) : base(variant, seed1, seed2) { }
        #endregion

        #region Methods
        /// <inheritdoc/>
        protected override UInt64 ComputeHashValueInternal(ReadOnlySpan<Byte> buffer)
        {
            (UInt64 hash, _) = ComputeHashParts(buffer, false);
            return hash;
        }
        #endregion
    }

    /// <summary>Represents the SipHash128 implementation. This class cannot be derived.</summary>
    public sealed class SipHash128 : SipHash<(UInt64, UInt64)>
    {
        #region Constructors
        /// <summary>Initializes a new instance using <see cref="F:FastHashes.SipHashVariant.V24"/> and a value of <c>0</c> for both seeds.</summary>
        [ExcludeFromCodeCoverage]
        public SipHash128() : base(SipHashVariant.V24, 0ul, 0ul) { }

        /// <summary>Initializes a new instance using the specified variant and a value of <c>0</c> for both seeds.</summary>
        /// <param name="variant">The <see cref="T:FastHashes.SipHashVariant"/> value specifying the variant of the hashing algorithm.</param>
        /// <exception cref="T:System.ArgumentOutOfRangeException">Thrown when <paramref name="variant">variant</paramref> is not a defined enumeration value.</exception>
        public SipHash128(SipHashVariant variant) : base(variant, 0ul, 0ul) { }

        /// <summary>Initializes a new instance using <see cref="F:FastHashes.SipHashVariant.V24"/> and the specified value for both seeds.</summary>
        [ExcludeFromCodeCoverage]
        public SipHash128(UInt64 seed) : base(SipHashVariant.V24, seed, seed) { }

        /// <summary>Initializes a new instance using <see cref="F:FastHashes.SipHashVariant.V24"/> and the specified seeds.</summary>
        [ExcludeFromCodeCoverage]
        public SipHash128(UInt64 seed1, UInt64 seed2) : base(SipHashVariant.V24, seed1, seed2) { }

        /// <summary>Initializes a new instance using the specified variant and the specified value for both seeds.</summary>
        /// <exception cref="T:System.ArgumentOutOfRangeException">Thrown when <paramref name="variant">variant</paramref> is not a defined enumeration value.</exception>
        [ExcludeFromCodeCoverage]
        public SipHash128(SipHashVariant variant, UInt64 seed) : base(variant, seed, seed) { }

        /// <summary>Initializes a new instance using the specified variant and seeds.</summary>
        /// <param name="variant">The <see cref="T:FastHashes.SipHashVariant"/> value specifying the variant of the hashing algorithm.</param>
        /// <param name="seed1">The first <see cref="T:System.UInt64"/> seed used by the hashing algorithm.</param>
        /// <param name="seed2">The second <see cref="T:System.UInt64"/> seed used by the hashing algorithm.</param>
        /// <exception cref="T:System.ArgumentOutOfRangeException">Thrown when <paramref name="variant">variant</paramref> is not a defined enumeration value.</exception>
        [ExcludeFromCodeCoverage]
        public SipHash128(SipHashVariant variant, UInt64 seed1, UInt64 seed2) : base(variant, seed1, seed2) { }
        #endregion

        #region Methods
        /// <inheritdoc/>
        protected override (UInt64, UInt64) ComputeHashValueInternal(ReadOnlySpan<Byte> buffer)
        {
            return ComputeHashParts(buffer, true);
        }
        #endregion
    }
}