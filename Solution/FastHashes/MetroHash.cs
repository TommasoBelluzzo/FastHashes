#region Using Directives
using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
#endregion

namespace FastHashes
{
    /// <summary>Represents the MetroHash64 implementation. This class cannot be derived.</summary>
    public sealed class MetroHash64 : Hash
    {
        #region Members
        private readonly Engine m_Engine;
        #endregion

        #region Properties
        /// <inheritdoc/>
        [ExcludeFromCodeCoverage]
        public override Int32 Length => 64;

        /// <summary>Gets the variant of the hashing algorithm.</summary>
        /// <value>An enumerator value of type <see cref="T:FastHashes.MetroHashVariant"/>.</value>
        [ExcludeFromCodeCoverage]
        public MetroHashVariant Variant => m_Engine.Variant;

        /// <summary>Gets the seed used by the hashing algorithm.</summary>
        /// <value>An <see cref="T:System.UInt32"/> value.</value>
        [ExcludeFromCodeCoverage]
        public UInt32 Seed => m_Engine.Seed;
        #endregion

        #region Constructors
        /// <summary>Initializes a new instance using the specified variant and seed.</summary>
        /// <param name="variant">The enumerator value of type <see cref="T:FastHashes.MetroHashVariant"/> representing the variant of the hashing algorithm.</param>
        /// <param name="seed">The <see cref="T:System.UInt32"/> seed used by the hashing algorithm.</param>
        /// <exception cref="T:System.ArgumentException">Thrown when the value of <paramref name="variant">variant</paramref> is undefined.</exception>
        [ExcludeFromCodeCoverage] 
        public MetroHash64(MetroHashVariant variant, UInt32 seed)
        {
            if (!Enum.IsDefined(typeof(MetroHashVariant), variant))
                throw new ArgumentException("Invalid variant specified.", nameof(variant));

            if (variant == MetroHashVariant.V1)
                m_Engine = new Engine1(seed);
            else
                m_Engine = new Engine2(seed);
        }

        /// <summary>Initializes a new instance using <see cref="F:FastHashes.MetroHashVariant.V1"/> and a seed value of <c>0</c>.</summary>
        [ExcludeFromCodeCoverage]
        public MetroHash64() : this(MetroHashVariant.V1, 0u) { }

        /// <summary>Initializes a new instance using the specified variant and a seed value of <c>0</c>.</summary>
        /// <param name="variant">The enumerator value of type <see cref="T:FastHashes.MetroHashVariant"/> representing the variant of the hashing algorithm.</param>
        /// <exception cref="T:System.ArgumentException">Thrown when the value of <paramref name="variant">variant</paramref> is undefined.</exception>
        [ExcludeFromCodeCoverage]
        public MetroHash64(MetroHashVariant variant) : this(variant, 0u) { }

        /// <summary>Initializes a new instance using <see cref="F:FastHashes.MetroHashVariant.V1"/> and the specified <see cref="T:System.UInt32"/> seed.</summary>
        /// <param name="seed">The <see cref="T:System.UInt32"/> seed used by the hashing algorithm.</param>
        [ExcludeFromCodeCoverage]
        public MetroHash64(UInt32 seed) : this(MetroHashVariant.V1, seed) { }
        #endregion

        #region Methods
        /// <inheritdoc/>
        [ExcludeFromCodeCoverage]
        public override String ToString()
        {
            return $"{GetType().Name}-{m_Engine.Name}";
        }
        #endregion

        #region Pointer/Span Fork
		#if NETSTANDARD2_1_OR_GREATER
        /// <inheritdoc/>
        protected override Byte[] ComputeHashInternal(ReadOnlySpan<Byte> buffer)
        {
            return m_Engine.ComputeHash(buffer);
        }
		#else
        /// <inheritdoc/>
        protected override Byte[] ComputeHashInternal(Byte[] buffer, Int32 offset, Int32 count)
        {
            return m_Engine.ComputeHash(buffer, offset, count);
        }
		#endif
        #endregion

        #region Nested Classes
        private abstract class Engine
        {
            #region Members
            protected readonly UInt32 m_Seed;
            #endregion

            #region Properties
            public abstract MetroHashVariant Variant { get; }

            public abstract String Name { get; }

            [ExcludeFromCodeCoverage]
            public UInt32 Seed => m_Seed;
            #endregion

            #region Constructors
            [ExcludeFromCodeCoverage]
            protected Engine(UInt32 seed)
            {
                m_Seed = seed;
            }
            #endregion

            #region Methods
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            protected static UInt64 Mix8(UInt64 v1, Byte v2, Int32 r, UInt64 k1, UInt64 k2)
            {
                v1 += v2 * k1;
                v1 ^= BinaryOperations.RotateRight(v1, r) * k2;

                return v1;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            protected static UInt64 Mix16(UInt64 v1, UInt16 v2, Int32 r, UInt64 k1, UInt64 k2)
            {
                v1 += v2 * k1;
                v1 ^= BinaryOperations.RotateRight(v1, r) * k2;

                return v1;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            protected static UInt64 Mix32(UInt64 v1, UInt32 v2, Int32 r, UInt64 k1, UInt64 k2)
            {
                v1 += v2 * k1;
                v1 ^= BinaryOperations.RotateRight(v1, r) * k2;

                return v1;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            protected static UInt64 Mix64(UInt64 v1, UInt64 v2, Int32 r, UInt64 k1, UInt64 k2)
            {
                v1 += v2 * k1;
                v1 ^= BinaryOperations.RotateRight(v1, r) * k2;

                return v1;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            protected static UInt64 Mix128(UInt64 v1, UInt64 v2, Int32 r, UInt64 k1, UInt64 k2)
            {
                v1 += v2 * k1;
                v1 = BinaryOperations.RotateRight(v1, r) * k2;

                return v1;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            protected static UInt64 Mix256(UInt64 v1, UInt64 v2, UInt64 v3, Int32 r, UInt64 k)
            {
                v1 += v2 * k;
                v1 = BinaryOperations.RotateRight(v1, r) + v3;

                return v1;
            }
            #endregion

            #region Pointer/Span Fork
			#if NETSTANDARD2_1_OR_GREATER
            public abstract Byte[] ComputeHash(ReadOnlySpan<Byte> buffer);
			#else
            public abstract Byte[] ComputeHash(Byte[] data, Int32 offset, Int32 count);
			#endif
            #endregion
        }

        private sealed class Engine1 : Engine
        {
            #region Constants
            private const UInt64 K0 = 0xC83A91E1ul;
            private const UInt64 K1 = 0x8648DBDBul;
            private const UInt64 K2 = 0x7BDEC03Bul;
            private const UInt64 K3 = 0x2F5870A5ul;
            #endregion

            #region Properties
            [ExcludeFromCodeCoverage]
            public override MetroHashVariant Variant => MetroHashVariant.V1;

            [ExcludeFromCodeCoverage]
            public override String Name => "V1";
            #endregion

            #region Constructors
            [ExcludeFromCodeCoverage]
            public Engine1(UInt32 seed) : base(seed) { }
            #endregion

            #region Pointer/Span Fork
			#if NETSTANDARD2_1_OR_GREATER
            public override Byte[] ComputeHash(ReadOnlySpan<Byte> buffer)
            {
                Int32 offset = 0;
                Int32 count = buffer.Length;

                UInt64 hash = (m_Seed + K2) * K0;

                if (count == 0)
                    goto Finalize;

                hash += (UInt64)count;

                if (count >= 32)
                {
                    UInt64 v1 = hash;
                    UInt64 v2 = hash;
                    UInt64 v3 = hash;
                    UInt64 v4 = hash;

                    do
                    {
                        UInt64 z1 = BinaryOperations.Read64(buffer, offset);
                        offset += 8;
                        UInt64 z2 = BinaryOperations.Read64(buffer, offset);
                        offset += 8;
                        UInt64 z3 = BinaryOperations.Read64(buffer, offset);
                        offset += 8;
                        UInt64 z4 = BinaryOperations.Read64(buffer, offset);
                        offset += 8;

                        v1 = Mix256(v1, z1, v3, 29, K0);
                        v2 = Mix256(v2, z2, v4, 29, K1);
                        v3 = Mix256(v3, z3, v1, 29, K2);
                        v4 = Mix256(v4, z4, v2, 29, K3);         
                    }
                    while ((count - 32) >= offset);

                    v3 ^= BinaryOperations.RotateRight(((v1 + v4) * K0) + v2, 33) * K1;
                    v4 ^= BinaryOperations.RotateRight(((v2 + v3) * K1) + v1, 33) * K0;
                    v1 ^= BinaryOperations.RotateRight(((v1 + v3) * K0) + v4, 33) * K1;
                    v2 ^= BinaryOperations.RotateRight(((v2 + v4) * K1) + v3, 33) * K0;

                    hash += v1 ^ v2;
                }

                if ((count - offset) >= 16)
                {
                    UInt64 z1 = BinaryOperations.Read64(buffer, offset);
                    offset += 8;
                    UInt64 z2 = BinaryOperations.Read64(buffer, offset);
                    offset += 8;

                    UInt64 v1 = hash;
                    UInt64 v2 = hash;

                    v1 = Mix128(v1, z1, 33, K0, K1);
                    v2 = Mix128(v2, z2, 33, K1, K2);

                    v1 ^= BinaryOperations.RotateRight(v1 * K0, 35) + v2;
                    v2 ^= BinaryOperations.RotateRight(v2 * K3, 35) + v1;

                    hash += v2;
                }

                if ((count - offset) >= 8)
                {
                    UInt64 z = BinaryOperations.Read64(buffer, offset);
                    offset += 8;

                    hash = Mix64(hash, z, 33, K3, K1);
                }

                if ((count - offset) >= 4)
                {
                    UInt32 z = BinaryOperations.Read32(buffer, offset);
                    offset += 4;

                    hash = Mix32(hash, z, 15, K3, K1);
                }

                if ((count - offset) >= 2)
                {
                    UInt16 z = BinaryOperations.Read16(buffer, offset);
                    offset += 2;

                    hash = Mix16(hash, z, 13, K3, K1);
                }

                if ((count - offset) >= 1)
                    hash = Mix8(hash, buffer[offset], 25, K3, K1);

                Finalize:

                hash ^= BinaryOperations.RotateRight(hash, 33);
                hash *= K0;
                hash ^= BinaryOperations.RotateRight(hash, 33);

                Byte[] result = BinaryOperations.ToArray64(hash);

                return result;
            }
			#else
            public override Byte[] ComputeHash(Byte[] data, Int32 offset, Int32 count)
            {
                UInt64 hash = (m_Seed + K2) * K0;

                if (count == 0)
                    goto Finalize;

                hash += (UInt64)count;

                unsafe
                {
                    fixed (Byte* buffer = &data[offset])
                    {
                        Byte* pointer = buffer;
                        Byte* limit = pointer + count;

                        if (count >= 32)
                        {
                            UInt64 v1 = hash;
                            UInt64 v2 = hash;
                            UInt64 v3 = hash;
                            UInt64 v4 = hash;

                            do
                            {
                                UInt64 z1 = BinaryOperations.Read64(pointer);
                                pointer += 8;
                                UInt64 z2 = BinaryOperations.Read64(pointer);
                                pointer += 8;
                                UInt64 z3 = BinaryOperations.Read64(pointer);
                                pointer += 8;
                                UInt64 z4 = BinaryOperations.Read64(pointer);
                                pointer += 8;

                                v1 = Mix256(v1, z1, v3, 29, K0);
                                v2 = Mix256(v2, z2, v4, 29, K1);
                                v3 = Mix256(v3, z3, v1, 29, K2);
                                v4 = Mix256(v4, z4, v2, 29, K3);         
                            }
                            while ((limit - 32) >= pointer);

                            v3 ^= BinaryOperations.RotateRight(((v1 + v4) * K0) + v2, 33) * K1;
                            v4 ^= BinaryOperations.RotateRight(((v2 + v3) * K1) + v1, 33) * K0;
                            v1 ^= BinaryOperations.RotateRight(((v1 + v3) * K0) + v4, 33) * K1;
                            v2 ^= BinaryOperations.RotateRight(((v2 + v4) * K1) + v3, 33) * K0;

                            hash += v1 ^ v2;
                        }

                        if ((limit - pointer) >= 16)
                        {
                            UInt64 z1 = BinaryOperations.Read64(pointer);
                            pointer += 8;
                            UInt64 z2 = BinaryOperations.Read64(pointer);
                            pointer += 8;

                            UInt64 v1 = hash;
                            UInt64 v2 = hash;

                            v1 = Mix128(v1, z1, 33, K0, K1);
                            v2 = Mix128(v2, z2, 33, K1, K2);

                            v1 ^= BinaryOperations.RotateRight(v1 * K0, 35) + v2;
                            v2 ^= BinaryOperations.RotateRight(v2 * K3, 35) + v1;

                            hash += v2;
                        }

                        if ((limit - pointer) >= 8)
                        {
                            UInt64 z = BinaryOperations.Read64(pointer);
                            pointer += 8;

                            hash = Mix64(hash, z, 33, K3, K1);
                        }    

                        if ((limit - pointer) >= 4)
                        {
                            UInt32 z = BinaryOperations.Read32(pointer);
                            pointer += 4;

                            hash = Mix32(hash, z, 15, K3, K1);
                        }

                        if ((limit - pointer) >= 2)
                        {
                            UInt16 z = BinaryOperations.Read16(pointer);
                            pointer += 2;

                            hash = Mix16(hash, z, 13, K3, K1);
                        }

                        if ((limit - pointer) >= 1)
                            hash = Mix8(hash, pointer[0], 25, K3, K1);
                    }
                }

                Finalize:

                hash ^= BinaryOperations.RotateRight(hash, 33);
                hash *= K0;
                hash ^= BinaryOperations.RotateRight(hash, 33);

                Byte[] result = BinaryOperations.ToArray64(hash);

                return result;
            }
			#endif
            #endregion
        }

        private sealed class Engine2 : Engine
        {
            #region Constants
            private const UInt64 K0 = 0xD6D018F5ul;
            private const UInt64 K1 = 0xA2AA033Bul;
            private const UInt64 K2 = 0x62992FC1ul;
            private const UInt64 K3 = 0x30BC5B29ul;
            #endregion

            #region Properties
            [ExcludeFromCodeCoverage]
            public override MetroHashVariant Variant => MetroHashVariant.V2;

            [ExcludeFromCodeCoverage]
            public override String Name => "V2";
            #endregion

            #region Constructors
            [ExcludeFromCodeCoverage]
            public Engine2(UInt32 seed) : base(seed) { }
            #endregion

            #region Pointer/Span Fork
			#if NETSTANDARD2_1_OR_GREATER
            public override Byte[] ComputeHash(ReadOnlySpan<Byte> buffer)
            {
                Int32 offset = 0;
                Int32 count = buffer.Length;

                UInt64 hash = (m_Seed + K2) * K0;

                if (count == 0)
                    goto Finalize;

                hash += (UInt64)count;

                if (count >= 32)
                {
                    UInt64 v1 = hash;
                    UInt64 v2 = hash;
                    UInt64 v3 = hash;
                    UInt64 v4 = hash;

                    do
                    {
                        UInt64 z1 = BinaryOperations.Read64(buffer, offset);
                        offset += 8;
                        UInt64 z2 = BinaryOperations.Read64(buffer, offset);
                        offset += 8;
                        UInt64 z3 = BinaryOperations.Read64(buffer, offset);
                        offset += 8;
                        UInt64 z4 = BinaryOperations.Read64(buffer, offset);
                        offset += 8;

                        v1 = Mix256(v1, z1, v3, 29, K0);
                        v2 = Mix256(v2, z2, v4, 29, K1);
                        v3 = Mix256(v3, z3, v1, 29, K2);
                        v4 = Mix256(v4, z4, v2, 29, K3);         
                    }
                    while ((count - 32) >= offset);

                    v3 ^= BinaryOperations.RotateRight(((v1 + v4) * K0) + v2, 30) * K1;
                    v4 ^= BinaryOperations.RotateRight(((v2 + v3) * K1) + v1, 30) * K0;
                    v1 ^= BinaryOperations.RotateRight(((v1 + v3) * K0) + v4, 30) * K1;
                    v2 ^= BinaryOperations.RotateRight(((v2 + v4) * K1) + v3, 30) * K0;

                    hash += v1 ^ v2;
                }

                if ((count - offset) >= 16)
                {
                    UInt64 z1 = BinaryOperations.Read64(buffer, offset);
                    offset += 8;
                    UInt64 z2 = BinaryOperations.Read64(buffer, offset);
                    offset += 8;

                    UInt64 v1 = hash;
                    UInt64 v2 = hash;

                    v1 = Mix128(v1, z1, 29, K2, K3);
                    v2 = Mix128(v2, z2, 29, K2, K3);

                    v1 ^= BinaryOperations.RotateRight(v1 * K0, 34) + v2;
                    v2 ^= BinaryOperations.RotateRight(v2 * K3, 34) + v1;

                    hash += v2;
                }

                if ((count - offset) >= 8)
                {
                    UInt64 z = BinaryOperations.Read64(buffer, offset);
                    offset += 8;

                    hash = Mix64(hash, z, 36, K3, K1);
                } 

                if ((count - offset) >= 4)
                {
                    UInt32 z = BinaryOperations.Read32(buffer, offset);
                    offset += 4;

                    hash = Mix32(hash, z, 15, K3, K1);
                } 

                if ((count - offset) >= 2)
                {
                    UInt16 z = BinaryOperations.Read16(buffer, offset);
                    offset += 2;

                    hash = Mix16(hash, z, 15, K3, K1);
                }

                if ((count - offset) >= 1)
                    hash = Mix8(hash, buffer[offset], 23, K3, K1);

                Finalize:

                hash ^= BinaryOperations.RotateRight(hash, 28);
                hash *= K0;
                hash ^= BinaryOperations.RotateRight(hash, 29);

                Byte[] result = BinaryOperations.ToArray64(hash);

                return result;
            }
			#else
            public override Byte[] ComputeHash(Byte[] data, Int32 offset, Int32 count)
            {
                UInt64 hash = (m_Seed + K2) * K0;

                if (count == 0)
                    goto Finalize;

                hash += (UInt64)count;

                unsafe
                {
                    fixed (Byte* buffer = &data[offset])
                    {
                        Byte* pointer = buffer;
                        Byte* limit = pointer + count;

                        if (count >= 32)
                        {
                            UInt64 v1 = hash;
                            UInt64 v2 = hash;
                            UInt64 v3 = hash;
                            UInt64 v4 = hash;

                            do
                            {
                                UInt64 z1 = BinaryOperations.Read64(pointer);
                                pointer += 8;
                                UInt64 z2 = BinaryOperations.Read64(pointer);
                                pointer += 8;
                                UInt64 z3 = BinaryOperations.Read64(pointer);
                                pointer += 8;
                                UInt64 z4 = BinaryOperations.Read64(pointer);
                                pointer += 8;

                                v1 = Mix256(v1, z1, v3, 29, K0);
                                v2 = Mix256(v2, z2, v4, 29, K1);
                                v3 = Mix256(v3, z3, v1, 29, K2);
                                v4 = Mix256(v4, z4, v2, 29, K3);         
                            }
                            while ((limit - 32) >= pointer);

                            v3 ^= BinaryOperations.RotateRight(((v1 + v4) * K0) + v2, 30) * K1;
                            v4 ^= BinaryOperations.RotateRight(((v2 + v3) * K1) + v1, 30) * K0;
                            v1 ^= BinaryOperations.RotateRight(((v1 + v3) * K0) + v4, 30) * K1;
                            v2 ^= BinaryOperations.RotateRight(((v2 + v4) * K1) + v3, 30) * K0;

                            hash += v1 ^ v2;
                        }

                        if ((limit - pointer) >= 16)
                        {
                            UInt64 z1 = BinaryOperations.Read64(pointer);
                            pointer += 8;
                            UInt64 z2 = BinaryOperations.Read64(pointer);
                            pointer += 8;

                            UInt64 v1 = hash;
                            UInt64 v2 = hash;

                            v1 = Mix128(v1, z1, 29, K2, K3);
                            v2 = Mix128(v2, z2, 29, K2, K3);

                            v1 ^= BinaryOperations.RotateRight(v1 * K0, 34) + v2;
                            v2 ^= BinaryOperations.RotateRight(v2 * K3, 34) + v1;

                            hash += v2;
                        }

                        if ((limit - pointer) >= 8)
                        {
                            UInt64 z = BinaryOperations.Read64(pointer);
                            pointer += 8;

                            hash = Mix64(hash, z, 36, K3, K1);
                        } 

                        if ((limit - pointer) >= 4)
                        {
                            UInt32 z = BinaryOperations.Read32(pointer);
                            pointer += 4;

                            hash = Mix32(hash, z, 15, K3, K1);
                        } 

                        if ((limit - pointer) >= 2)
                        {
                            UInt16 z = BinaryOperations.Read16(pointer);
                            pointer += 2;

                            hash = Mix16(hash, z, 15, K3, K1);
                        }

                        if ((limit - pointer) >= 1)
                            hash = Mix8(hash, pointer[0], 23, K3, K1);
                    }
                }

                Finalize:

                hash ^= BinaryOperations.RotateRight(hash, 28);
                hash *= K0;
                hash ^= BinaryOperations.RotateRight(hash, 29);

                Byte[] result = BinaryOperations.ToArray64(hash);

                return result;
            }
			#endif
            #endregion
        }
        #endregion
    }

    /// <summary>Represents the MetroHash128 implementation. This class cannot be derived.</summary>
    public sealed class MetroHash128 : Hash
    {
        #region Members
        private readonly Engine m_Engine;
        #endregion

        #region Properties
        /// <inheritdoc/>
        [ExcludeFromCodeCoverage]
        public override Int32 Length => 128;

        /// <summary>Gets the variant of the hashing algorithm.</summary>
        /// <value>An enumerator value of type <see cref="T:FastHashes.MetroHashVariant"/>.</value>
        [ExcludeFromCodeCoverage]
        public MetroHashVariant Variant => m_Engine.Variant;

        /// <summary>Gets the seed used by the hashing algorithm.</summary>
        /// <value>An <see cref="T:System.UInt32"/> value.</value>
        [ExcludeFromCodeCoverage]
        public UInt32 Seed => m_Engine.Seed;
        #endregion

        #region Constructors
        /// <summary>Initializes a new instance using the specified variant and seed.</summary>
        /// <param name="variant">The enumerator value of type <see cref="T:FastHashes.MetroHashVariant"/> representing the variant of the hashing algorithm.</param>
        /// <param name="seed">The <see cref="T:System.UInt32"/> seed used by the hashing algorithm.</param>
        /// <exception cref="T:System.ArgumentException">Thrown when the value of <paramref name="variant">variant</paramref> is undefined.</exception>
        [ExcludeFromCodeCoverage]
        public MetroHash128(MetroHashVariant variant, UInt32 seed)
        {
            if (!Enum.IsDefined(typeof(MetroHashVariant), variant))
                throw new ArgumentException("Invalid variant specified.", nameof(variant));

            if (variant == MetroHashVariant.V1)
                m_Engine = new Engine1(seed);
            else
                m_Engine = new Engine2(seed);
        }

        /// <summary>Initializes a new instance using <see cref="F:FastHashes.MetroHashVariant.V1"/> and a seed value of <c>0</c>.</summary>
        [ExcludeFromCodeCoverage]
        public MetroHash128() : this(MetroHashVariant.V1, 0u) { }

        /// <summary>Initializes a new instance using the specified variant and a seed value of <c>0</c>.</summary>
        /// <param name="variant">The enumerator value of type <see cref="T:FastHashes.MetroHashVariant"/> representing the variant of the hashing algorithm.</param>
        /// <exception cref="T:System.ArgumentException">Thrown when the value of <paramref name="variant">variant</paramref> is undefined.</exception>
        [ExcludeFromCodeCoverage]
        public MetroHash128(MetroHashVariant variant) : this(variant, 0u) { }

        /// <summary>Initializes a new instance using <see cref="F:FastHashes.MetroHashVariant.V1"/> and the specified seed.</summary>
        /// <param name="seed">The <see cref="T:System.UInt32"/> seed used by the hashing algorithm.</param>
        [ExcludeFromCodeCoverage]
        public MetroHash128(UInt32 seed) : this(MetroHashVariant.V1, seed) { }
        #endregion

        #region Methods
        /// <inheritdoc/>
        [ExcludeFromCodeCoverage]
        public override String ToString()
        {
            return $"{GetType().Name}-{m_Engine.Name}";
        }
        #endregion

        #region Pointer/Span Fork
		#if NETSTANDARD2_1_OR_GREATER
        /// <inheritdoc/>
        protected override Byte[] ComputeHashInternal(ReadOnlySpan<Byte> buffer)
        {
            return m_Engine.ComputeHash(buffer);
        }
		#else
        /// <inheritdoc/>
        protected override Byte[] ComputeHashInternal(Byte[] buffer, Int32 offset, Int32 count)
        {
            return m_Engine.ComputeHash(buffer, offset, count);
        }
		#endif
        #endregion

        #region Nested Classes
        private abstract class Engine
        {
            #region Members
            protected readonly UInt32 m_Seed;
            #endregion

            #region Properties
            public abstract MetroHashVariant Variant { get; }

            public abstract String Name { get; }

            [ExcludeFromCodeCoverage]
            public UInt32 Seed => m_Seed;
            #endregion

            #region Constructors
            [ExcludeFromCodeCoverage]
            protected Engine(UInt32 seed)
            {
                m_Seed = seed;
            }
            #endregion

            #region Methods
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            protected static UInt64 Fin(UInt64 v1, UInt64 v2, Int32 r, UInt64 k)
            {
                return (v1 + BinaryOperations.RotateRight((v1 * k) + v2, r));
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            protected static UInt64 Mix(UInt64 v1, UInt64 v2, UInt64 v3, Int32 r1, Int32 r2, UInt64 k1, UInt64 k2, UInt64 k3, UInt64 k4)
            {
                v1 += v3 * k1;
                v1 = BinaryOperations.RotateRight(v1, r1) * k2;
                v1 ^= BinaryOperations.RotateRight((v1 * k3) + v2, r2) * k4;

                return v1;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            protected static UInt64 Mix128(UInt64 v1, UInt64 v2, Int32 r, UInt64 k1, UInt64 k2)
            {
                v1 += v2 * k1;
                v1 = BinaryOperations.RotateRight(v1, r) * k2;

                return v1;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            protected static UInt64 Mix256(UInt64 v1, UInt64 v2, UInt64 v3, UInt64 k)
            {
                v1 += v3 * k; 
                v1 = BinaryOperations.RotateRight(v1, 29) + v2;

                return v1;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            protected static UInt64 Xor128(UInt64 v1, UInt64 v2, Int32 r, UInt64 k1, UInt64 k2)
            {
                return (v1 ^ BinaryOperations.RotateRight((v1 * k1) + v2, r) * k2);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            protected static UInt64 Xor256(UInt64 v1, UInt64 v2, UInt64 v3, UInt64 v4, Int32 r, UInt64 k1, UInt64 k2)
            {
                return (v1 ^ BinaryOperations.RotateRight(((v2 + v3) * k1) + v4, r) * k2);
            }
            #endregion

            #region Pointer/Span Fork
			#if NETSTANDARD2_1_OR_GREATER
            public abstract Byte[] ComputeHash(ReadOnlySpan<Byte> buffer);
			#else
            public abstract Byte[] ComputeHash(Byte[] data, Int32 offset, Int32 count);
			#endif
            #endregion
        }

        private sealed class Engine1 : Engine
        {
            #region Constants
            private const UInt64 K0 = 0xC83A91E1ul;
            private const UInt64 K1 = 0x8648DBDBul;
            private const UInt64 K2 = 0x7BDEC03Bul;
            private const UInt64 K3 = 0x2F5870A5ul;
            #endregion

            #region Properties
            [ExcludeFromCodeCoverage]
            public override MetroHashVariant Variant => MetroHashVariant.V1;

            [ExcludeFromCodeCoverage]
            public override String Name => "V1";
            #endregion

            #region Constructors
            [ExcludeFromCodeCoverage]
            public Engine1(UInt32 seed) : base(seed) { }
            #endregion

            #region Pointer/Span Fork
			#if NETSTANDARD2_1_OR_GREATER
            public override Byte[] ComputeHash(ReadOnlySpan<Byte> buffer)
            {
                Int32 offset = 0;
                Int32 count = buffer.Length;

                UInt64 hash1 = (m_Seed - K0) * K3;
                UInt64 hash2 = (m_Seed + K1) * K2;

                if (count == 0)
                    goto Finalize;

                UInt64 length = (UInt64)count;
                hash1 += length;
                hash2 += length;

                if (count >= 32)
                {
                    UInt64 v1 = ((m_Seed + K0) * K2) + length;
                    UInt64 v2 = ((m_Seed - K1) * K3) + length;

                    do
                    {
                        UInt64 z1 = BinaryOperations.Read64(buffer, offset);
                        offset += 8;
                        UInt64 z2 = BinaryOperations.Read64(buffer, offset);
                        offset += 8;
                        UInt64 z3 = BinaryOperations.Read64(buffer, offset);
                        offset += 8;
                        UInt64 z4 = BinaryOperations.Read64(buffer, offset);
                        offset += 8;

                        hash1 = Mix256(hash1, v1, z1, K0);
                        hash2 = Mix256(hash2, v2, z2, K1);      
                        v1 = Mix256(v1, hash1, z3, K2);
                        v2 = Mix256(v2, hash2, z4, K3);        
                    }
                    while ((count - 32) >= offset);

                    v1 = Xor256(v1, hash1, v2, hash2, 26, K0, K1);
                    v2 = Xor256(v2, hash2, v1, hash1, 26, K1, K0);
                    hash1 = Xor256(hash1, hash1, v1, v2, 26, K0, K1);
                    hash2 = Xor256(hash2, hash2, v2, v1, 30, K1, K0);
                }

                if ((count - offset) >= 16)
                {
                    UInt64 z1 = BinaryOperations.Read64(buffer, offset);
                    offset += 8;
                    UInt64 z2 = BinaryOperations.Read64(buffer, offset);
                    offset += 8;

                    hash1 = Mix128(hash1, z1, 33, K2, K3);
                    hash2 = Mix128(hash2, z2, 33, K2, K3);
                    hash1 = Xor128(hash1, hash2, 17, K2, K1);
                    hash2 = Xor128(hash2, hash1, 17, K3, K0);
                }

                if ((count - offset) >= 8)
                {
                    UInt64 z = BinaryOperations.Read64(buffer, offset);
                    offset += 8;

                    hash1 = Mix(hash1, hash2, z, 33, 20, K2, K3, K2, K1);
                }

                if ((count - offset) >= 4)
                {
                    UInt32 z = BinaryOperations.Read32(buffer, offset);
                    offset += 4;

                    hash2 = Mix(hash2, hash1, z, 33, 18, K2, K3, K3, K0);
                }  

                if ((count - offset) >= 2)
                {
                    UInt16 z = BinaryOperations.Read16(buffer, offset);
                    offset += 2;

                    hash1 = Mix(hash1, hash2, z, 33, 24, K2, K3, K2, K1);
                }

                if ((count - offset) >= 1)
                    hash2 = Mix(hash2, hash1, buffer[offset], 33, 24, K2, K3, K3, K0);

                Finalize:

                hash1 = Fin(hash1, hash2, 13, K0);
                hash2 = Fin(hash2, hash1, 37, K1);
                hash1 = Fin(hash1, hash2, 13, K2);
                hash2 = Fin(hash2, hash1, 37, K3);

                Byte[] result = BinaryOperations.ToArray64(hash1, hash2);

                return result;
            }
			#else
            public override Byte[] ComputeHash(Byte[] data, Int32 offset, Int32 count)
            {
                UInt64 hash1 = (m_Seed - K0) * K3;
                UInt64 hash2 = (m_Seed + K1) * K2;

                if (count == 0)
                    goto Finalize;

                UInt64 length = (UInt64)count;
                hash1 += length;
                hash2 += length;

                unsafe
                {
                    fixed (Byte* buffer = &data[offset])
                    {
                        Byte* pointer = buffer;
                        Byte* limit = pointer + count;

                        if (count >= 32)
                        {
                            UInt64 v1 = ((m_Seed + K0) * K2) + length;
                            UInt64 v2 = ((m_Seed - K1) * K3) + length;

                            do
                            {
                                UInt64 z1 = BinaryOperations.Read64(pointer);
                                pointer += 8;
                                UInt64 z2 = BinaryOperations.Read64(pointer);
                                pointer += 8;
                                UInt64 z3 = BinaryOperations.Read64(pointer);
                                pointer += 8;
                                UInt64 z4 = BinaryOperations.Read64(pointer);
                                pointer += 8;

                                hash1 = Mix256(hash1, v1, z1, K0);
                                hash2 = Mix256(hash2, v2, z2, K1);      
                                v1 = Mix256(v1, hash1, z3, K2);
                                v2 = Mix256(v2, hash2, z4, K3);        
                            }
                            while ((limit - 32) >= pointer);

                            v1 = Xor256(v1, hash1, v2, hash2, 26, K0, K1);
                            v2 = Xor256(v2, hash2, v1, hash1, 26, K1, K0);
                            hash1 = Xor256(hash1, hash1, v1, v2, 26, K0, K1);
                            hash2 = Xor256(hash2, hash2, v2, v1, 30, K1, K0);
                        }

                        if ((limit - pointer) >= 16)
                        {
                            UInt64 z1 = BinaryOperations.Read64(pointer);
                            pointer += 8;
                            UInt64 z2 = BinaryOperations.Read64(pointer);
                            pointer += 8;

                            hash1 = Mix128(hash1, z1, 33, K2, K3);
                            hash2 = Mix128(hash2, z2, 33, K2, K3);
                            hash1 = Xor128(hash1, hash2, 17, K2, K1);
                            hash2 = Xor128(hash2, hash1, 17, K3, K0);
                        }

                        if ((limit - pointer) >= 8)
                        {
                            UInt64 z = BinaryOperations.Read64(pointer);
                            pointer += 8;

                            hash1 = Mix(hash1, hash2, z, 33, 20, K2, K3, K2, K1);
                        }

                        if ((limit - pointer) >= 4)
                        {
                            UInt32 z = BinaryOperations.Read32(pointer);
                            pointer += 4;

                            hash2 = Mix(hash2, hash1, z, 33, 18, K2, K3, K3, K0);
                        }  

                        if ((limit - pointer) >= 2)
                        {
                            UInt16 z = BinaryOperations.Read16(pointer);
                            pointer += 2;

                            hash1 = Mix(hash1, hash2, z, 33, 24, K2, K3, K2, K1);
                        }

                        if ((limit - pointer) >= 1)
                            hash2 = Mix(hash2, hash1, pointer[0], 33, 24, K2, K3, K3, K0);
                    }
                }

                Finalize:

                hash1 = Fin(hash1, hash2, 13, K0);
                hash2 = Fin(hash2, hash1, 37, K1);
                hash1 = Fin(hash1, hash2, 13, K2);
                hash2 = Fin(hash2, hash1, 37, K3);

                Byte[] result = BinaryOperations.ToArray64(hash1, hash2);

                return result;
            }
			#endif
            #endregion
        }

        private sealed class Engine2 : Engine
        {
            #region Constants
            private const UInt64 K0 = 0xD6D018F5ul;
            private const UInt64 K1 = 0xA2AA033Bul;
            private const UInt64 K2 = 0x62992FC1ul;
            private const UInt64 K3 = 0x30BC5B29ul;
            #endregion

            #region Properties
            [ExcludeFromCodeCoverage]
            public override MetroHashVariant Variant => MetroHashVariant.V2;

            [ExcludeFromCodeCoverage]
            public override String Name => "V2";
            #endregion

            #region Constructors
            [ExcludeFromCodeCoverage]
            public Engine2(UInt32 seed) : base(seed) { }
            #endregion

            #region Pointer/Span Fork
			#if NETSTANDARD2_1_OR_GREATER
            public override Byte[] ComputeHash(ReadOnlySpan<Byte> buffer)
            {
                Int32 offset = 0;
                Int32 count = buffer.Length;

                UInt64 hash1 = (m_Seed - K0) * K3;
                UInt64 hash2 = (m_Seed + K1) * K2;

                if (count == 0)
                    goto Finalize;

                UInt64 length = (UInt64)count;
                hash1 += length;
                hash2 += length;

                if (count >= 32)
                {
                    UInt64 v1 = ((m_Seed + K0) * K2) + length;
                    UInt64 v2 = ((m_Seed - K1) * K3) + length;

                    do
                    {
                        UInt64 z1 = BinaryOperations.Read64(buffer, offset);
                        offset += 8;
                        UInt64 z2 = BinaryOperations.Read64(buffer, offset);
                        offset += 8;
                        UInt64 z3 = BinaryOperations.Read64(buffer, offset);
                        offset += 8;
                        UInt64 z4 = BinaryOperations.Read64(buffer, offset);
                        offset += 8;

                        hash1 = Mix256(hash1, v1, z1, K0);
                        hash2 = Mix256(hash2, v2, z2, K1);      
                        v1 = Mix256(v1, hash1, z3, K2);
                        v2 = Mix256(v2, hash2, z4, K3);        
                    }
                    while ((count - 32) >= offset);

                    v1 = Xor256(v1, hash1, v2, hash2, 33, K0, K1);
                    v2 = Xor256(v2, hash2, v1, hash1, 33, K1, K0);
                    hash1 = Xor256(hash1, hash1, v1, v2, 33, K0, K1);
                    hash2 = Xor256(hash2, hash2, v2, v1, 33, K1, K0);
                }

                if ((count - offset) >= 16)
                {
                    UInt64 z1 = BinaryOperations.Read64(buffer, offset);
                    offset += 8;
                    UInt64 z2 = BinaryOperations.Read64(buffer, offset);
                    offset += 8;

                    hash1 = Mix128(hash1, z1, 29, K2, K3);
                    hash2 = Mix128(hash2, z2, 29, K2, K3);                  
                    hash1 = Xor128(hash1, hash2, 29, K2, K1);
                    hash2 = Xor128(hash2, hash1, 29, K3, K0);
                }

                if ((count - offset) >= 8)
                {
                    UInt64 z = BinaryOperations.Read64(buffer, offset);
                    offset += 8;

                    hash1 = Mix(hash1, hash2, z, 29, 29, K2, K3, K2, K1);
                }

                if ((count - offset) >= 4)
                {
                    UInt32 z = BinaryOperations.Read32(buffer, offset);
                    offset += 4;

                    hash2 = Mix(hash2, hash1, z, 29, 25, K2, K3, K3, K0);
                }

                if ((count - offset) >= 2)
                {
                    UInt16 z = BinaryOperations.Read16(buffer, offset);
                    offset += 2;

                    hash1 = Mix(hash1, hash2, z, 29, 30, K2, K3, K2, K1);
                }

                if ((count - offset) >= 1)
                    hash2 = Mix(hash2, hash1, buffer[offset], 29, 18, K2, K3, K3, K0);

                Finalize:

                hash1 = Fin(hash1, hash2, 33, K0);
                hash2 = Fin(hash2, hash1, 33, K1);
                hash1 = Fin(hash1, hash2, 33, K2);
                hash2 = Fin(hash2, hash1, 33, K3);

                Byte[] result = BinaryOperations.ToArray64(hash1, hash2);

                return result;
            }
			#else
            public override Byte[] ComputeHash(Byte[] data, Int32 offset, Int32 count)
            {
                UInt64 hash1 = (m_Seed - K0) * K3;
                UInt64 hash2 = (m_Seed + K1) * K2;

                if (count == 0)
                    goto Finalize;

                UInt64 length = (UInt64)count;
                hash1 += length;
                hash2 += length;

                unsafe
                {
                    fixed (Byte* buffer = &data[offset])
                    {
                        Byte* pointer = buffer;
                        Byte* limit = pointer + count;

                        if (count >= 32)
                        {
                            UInt64 v1 = ((m_Seed + K0) * K2) + length;
                            UInt64 v2 = ((m_Seed - K1) * K3) + length;

                            do
                            {
                                UInt64 z1 = BinaryOperations.Read64(pointer);
                                pointer += 8;
                                UInt64 z2 = BinaryOperations.Read64(pointer);
                                pointer += 8;
                                UInt64 z3 = BinaryOperations.Read64(pointer);
                                pointer += 8;
                                UInt64 z4 = BinaryOperations.Read64(pointer);
                                pointer += 8;

                                hash1 = Mix256(hash1, v1, z1, K0);
                                hash2 = Mix256(hash2, v2, z2, K1);      
                                v1 = Mix256(v1, hash1, z3, K2);
                                v2 = Mix256(v2, hash2, z4, K3);        
                            }
                            while ((limit - 32) >= pointer);

                            v1 = Xor256(v1, hash1, v2, hash2, 33, K0, K1);
                            v2 = Xor256(v2, hash2, v1, hash1, 33, K1, K0);
                            hash1 = Xor256(hash1, hash1, v1, v2, 33, K0, K1);
                            hash2 = Xor256(hash2, hash2, v2, v1, 33, K1, K0);
                        }

                        if ((limit - pointer) >= 16)
                        {
                            UInt64 z1 = BinaryOperations.Read64(pointer);
                            pointer += 8;
                            UInt64 z2 = BinaryOperations.Read64(pointer);
                            pointer += 8;

                            hash1 = Mix128(hash1, z1, 29, K2, K3);
                            hash2 = Mix128(hash2, z2, 29, K2, K3);                  
                            hash1 = Xor128(hash1, hash2, 29, K2, K1);
                            hash2 = Xor128(hash2, hash1, 29, K3, K0);
                        }

                        if ((limit - pointer) >= 8)
                        {
                            UInt64 z = BinaryOperations.Read64(pointer);
                            pointer += 8;

                            hash1 = Mix(hash1, hash2, z, 29, 29, K2, K3, K2, K1);
                        }

                        if ((limit - pointer) >= 4)
                        {
                            UInt32 z = BinaryOperations.Read32(pointer);
                            pointer += 4;

                            hash2 = Mix(hash2, hash1, z, 29, 25, K2, K3, K3, K0);
                        }

                        if ((limit - pointer) >= 2)
                        {
                            UInt16 z = BinaryOperations.Read16(pointer);
                            pointer += 2;

                            hash1 = Mix(hash1, hash2, z, 29, 30, K2, K3, K2, K1);
                        }

                        if ((limit - pointer) >= 1)
                            hash2 = Mix(hash2, hash1, pointer[0], 29, 18, K2, K3, K3, K0);
                    }
                }

                Finalize:

                hash1 = Fin(hash1, hash2, 33, K0);
                hash2 = Fin(hash2, hash1, 33, K1);
                hash1 = Fin(hash1, hash2, 33, K2);
                hash2 = Fin(hash2, hash1, 33, K3);

                Byte[] result = BinaryOperations.ToArray64(hash1, hash2);

                return result;
            }
			#endif
            #endregion
        }
        #endregion
    }
}