#region Using Directives
using System;
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
        /// <summary>Gets the variant of the hashing algorithm.</summary>
        /// <value>An enumerator value of type <see cref="T:FastHashes.MetroHashVariant"/>.</value>
        public MetroHashVariant Variant => m_Engine.Variant;

        /// <inheritdoc/>
        public override Int32 Length => 64;

        /// <summary>Gets the seed used by the hashing algorithm.</summary>
        /// <value>An <see cref="T:System.UInt32"/> value.</value>
        public UInt32 Seed => m_Engine.Seed;
        #endregion

        #region Constructors
        /// <summary>Initializes a new instance using the specified variant and seed.</summary>
        /// <param name="variant">The enumerator value of type <see cref="T:FastHashes.MetroHashVariant"/> representing the variant of the hashing algorithm.</param>
        /// <param name="seed">The <see cref="T:System.UInt32"/> seed used by the hashing algorithm.</param>
        /// <exception cref="T:System.ArgumentException">Thrown when the value of <paramref name="variant">variant</paramref> is undefined.</exception>
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
        public MetroHash64() : this(MetroHashVariant.V1, 0u) { }

        /// <summary>Initializes a new instance using the specified variant and a seed value of <c>0</c>.</summary>
        /// <param name="variant">The enumerator value of type <see cref="T:FastHashes.MetroHashVariant"/> representing the variant of the hashing algorithm.</param>
        /// <exception cref="T:System.ArgumentException">Thrown when the value of <paramref name="variant">variant</paramref> is undefined.</exception>
        public MetroHash64(MetroHashVariant variant) : this(variant, 0u) { }

        /// <summary>Initializes a new instance using <see cref="F:FastHashes.MetroHashVariant.V1"/> and the specified <see cref="T:System.UInt32"/> seed.</summary>
        /// <param name="seed">The <see cref="T:System.UInt32"/> seed used by the hashing algorithm.</param>
        public MetroHash64(UInt32 seed) : this(MetroHashVariant.V1, seed) { }
        #endregion

        #region Methods
        /// <inheritdoc/>
        protected override Byte[] ComputeHashInternal(Byte[] buffer, Int32 offset, Int32 count)
        {
            return m_Engine.ComputeHash(buffer, offset, count);
        }

        /// <inheritdoc/>
        public override String ToString()
        {
            return $"{GetType().Name}-{m_Engine.Name}";
        }
        #endregion

        #region Nesting (Classes)
        private abstract class Engine
        {
            #region Members
            protected readonly UInt32 m_Seed;
            #endregion

            #region Properties
            public UInt32 Seed => m_Seed;
            #endregion

            #region Properties (Abstract)
            public abstract MetroHashVariant Variant { get; }

            public abstract String Name { get; }
            #endregion

            #region Constructors
            protected Engine(UInt32 seed)
            {
                m_Seed = seed;
            }
            #endregion

            #region Methods (Abstract)
            public abstract Byte[] ComputeHash(Byte[] data, Int32 offset, Int32 length);
            #endregion

            #region Methods (Static)
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            protected static UInt64 Mix8(UInt64 v1, Byte v2, Int32 r, UInt64 k1, UInt64 k2)
            {
                v1 += v2 * k1;
                v1 ^= RotateRight(v1, r) * k2;

                return v1;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            protected static UInt64 Mix16(UInt64 v1, UInt16 v2, Int32 r, UInt64 k1, UInt64 k2)
            {
                v1 += v2 * k1;
                v1 ^= RotateRight(v1, r) * k2;

                return v1;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            protected static UInt64 Mix32(UInt64 v1, UInt32 v2, Int32 r, UInt64 k1, UInt64 k2)
            {
                v1 += v2 * k1;
                v1 ^= RotateRight(v1, r) * k2;

                return v1;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            protected static UInt64 Mix64(UInt64 v1, UInt64 v2, Int32 r, UInt64 k1, UInt64 k2)
            {
                v1 += v2 * k1;
                v1 ^= RotateRight(v1, r) * k2;

                return v1;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            protected static UInt64 Mix128(UInt64 v1, UInt64 v2, Int32 r, UInt64 k1, UInt64 k2)
            {
                v1 += v2 * k1;
                v1 = RotateRight(v1, r) * k2;

                return v1;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            protected static UInt64 Mix256(UInt64 v1, UInt64 v2, UInt64 v3, Int32 r, UInt64 k)
            {
                v1 += v2 * k;
                v1 = RotateRight(v1, r) + v3;

                return v1;
            }
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
            public override MetroHashVariant Variant => MetroHashVariant.V1;

            public override String Name => "V1";
            #endregion

            #region Constructors
            public Engine1(UInt32 seed) : base(seed) { }
            #endregion

            #region Methods
            public override Byte[] ComputeHash(Byte[] data, Int32 offset, Int32 length)
            {
                UInt64 hash = (m_Seed + K2) * K0;

                if (length == 0)
                    goto Finalize;

                hash += (UInt64)length;

                unsafe
                {
                    fixed (Byte* buffer = &data[offset])
                    {
                        Byte* pointer = buffer;
                        Byte* limit = pointer + length;

                        if (length >= 32)
                        {
                            UInt64 v1 = hash;
                            UInt64 v2 = hash;
                            UInt64 v3 = hash;
                            UInt64 v4 = hash;

                            do
                            {
                                v1 = Mix256(v1, Read64(ref pointer), v3, 29, K0);
                                v2 = Mix256(v2, Read64(ref pointer), v4, 29, K1);
                                v3 = Mix256(v3, Read64(ref pointer), v1, 29, K2);
                                v4 = Mix256(v4, Read64(ref pointer), v2, 29, K3);         
                            }
                            while ((limit - 32) >= pointer);

                            v3 ^= RotateRight(((v1 + v4) * K0) + v2, 33) * K1;
                            v4 ^= RotateRight(((v2 + v3) * K1) + v1, 33) * K0;
                            v1 ^= RotateRight(((v1 + v3) * K0) + v4, 33) * K1;
                            v2 ^= RotateRight(((v2 + v4) * K1) + v3, 33) * K0;

                            hash += v1 ^ v2;
                        }

                        if ((limit - pointer) >= 16)
                        {
                            UInt64 v1 = hash;
                            UInt64 v2 = hash;

                            v1 = Mix128(v1, Read64(ref pointer), 33, K0, K1);
                            v2 = Mix128(v2, Read64(ref pointer), 33, K1, K2);

                            v1 ^= RotateRight(v1 * K0, 35) + v2;
                            v2 ^= RotateRight(v2 * K3, 35) + v1;

                            hash += v2;
                        }

                        if ((limit - pointer) >= 8)
                            hash = Mix64(hash, Read64(ref pointer), 33, K3, K1);

                        if ((limit - pointer) >= 4)
                            hash = Mix32(hash, Read32(ref pointer), 15, K3, K1);

                        if ((limit - pointer) >= 2)
                            hash = Mix16(hash, Read16(ref pointer), 13, K3, K1);

                        if ((limit - pointer) >= 1)
                            hash = Mix8(hash, pointer[0], 25, K3, K1);
                    }
                }

Finalize:

                hash ^= RotateRight(hash, 33);
                hash *= K0;
                hash ^= RotateRight(hash, 33);

                Byte[] result = ToByteArray64(hash);

                return result;
            }
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
            public override MetroHashVariant Variant => MetroHashVariant.V2;

            public override String Name => "V2";
            #endregion

            #region Constructors
            public Engine2(UInt32 seed) : base(seed) { }
            #endregion

            #region Methods
            public override Byte[] ComputeHash(Byte[] data, Int32 offset, Int32 length)
            {
                UInt64 hash = (m_Seed + K2) * K0;

                if (length == 0)
                    goto Finalize;

                hash += (UInt64)length;

                unsafe
                {
                    fixed (Byte* buffer = &data[offset])
                    {
                        Byte* pointer = buffer;
                        Byte* limit = pointer + length;

                        if (length >= 32)
                        {
                            UInt64 v1 = hash;
                            UInt64 v2 = hash;
                            UInt64 v3 = hash;
                            UInt64 v4 = hash;

                            do
                            {
                                v1 = Mix256(v1, Read64(ref pointer), v3, 29, K0);
                                v2 = Mix256(v2, Read64(ref pointer), v4, 29, K1);
                                v3 = Mix256(v3, Read64(ref pointer), v1, 29, K2);
                                v4 = Mix256(v4, Read64(ref pointer), v2, 29, K3);         
                            }
                            while ((limit - 32) >= pointer);

                            v3 ^= RotateRight(((v1 + v4) * K0) + v2, 30) * K1;
                            v4 ^= RotateRight(((v2 + v3) * K1) + v1, 30) * K0;
                            v1 ^= RotateRight(((v1 + v3) * K0) + v4, 30) * K1;
                            v2 ^= RotateRight(((v2 + v4) * K1) + v3, 30) * K0;

                            hash += v1 ^ v2;
                        }

                        if ((limit - pointer) >= 16)
                        {
                            UInt64 v1 = hash;
                            UInt64 v2 = hash;

                            v1 = Mix128(v1, Read64(ref pointer), 29, K2, K3);
                            v2 = Mix128(v2, Read64(ref pointer), 29, K2, K3);

                            v1 ^= RotateRight(v1 * K0, 34) + v2;
                            v2 ^= RotateRight(v2 * K3, 34) + v1;

                            hash += v2;
                        }

                        if ((limit - pointer) >= 8)
                            hash = Mix64(hash, Read64(ref pointer), 36, K3, K1);

                        if ((limit - pointer) >= 4)
                            hash = Mix32(hash, Read32(ref pointer), 15, K3, K1);

                        if ((limit - pointer) >= 2)
                            hash = Mix16(hash, Read16(ref pointer), 15, K3, K1);

                        if ((limit - pointer) >= 1)
                            hash = Mix8(hash, pointer[0], 23, K3, K1);
                    }
                }

Finalize:

                hash ^= RotateRight(hash, 28);
                hash *= K0;
                hash ^= RotateRight(hash, 29);

                Byte[] result = ToByteArray64(hash);

                return result;
            }
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
        /// <summary>Gets the variant of the hashing algorithm.</summary>
        /// <value>An enumerator value of type <see cref="T:FastHashes.MetroHashVariant"/>.</value>
        public MetroHashVariant Variant => m_Engine.Variant;

        /// <inheritdoc/>
        public override Int32 Length => 128;

        /// <summary>Gets the seed used by the hashing algorithm.</summary>
        /// <value>An <see cref="T:System.UInt32"/> value.</value>
        public UInt32 Seed => m_Engine.Seed;
        #endregion

        #region Constructors
        /// <summary>Initializes a new instance using the specified variant and seed.</summary>
        /// <param name="variant">The enumerator value of type <see cref="T:FastHashes.MetroHashVariant"/> representing the variant of the hashing algorithm.</param>
        /// <param name="seed">The <see cref="T:System.UInt32"/> seed used by the hashing algorithm.</param>
        /// <exception cref="T:System.ArgumentException">Thrown when the value of <paramref name="variant">variant</paramref> is undefined.</exception>
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
        public MetroHash128() : this(MetroHashVariant.V1, 0u) { }

        /// <summary>Initializes a new instance using the specified variant and a seed value of <c>0</c>.</summary>
        /// <param name="variant">The enumerator value of type <see cref="T:FastHashes.MetroHashVariant"/> representing the variant of the hashing algorithm.</param>
        /// <exception cref="T:System.ArgumentException">Thrown when the value of <paramref name="variant">variant</paramref> is undefined.</exception>
        public MetroHash128(MetroHashVariant variant) : this(variant, 0u) { }

        /// <summary>Initializes a new instance using <see cref="F:FastHashes.MetroHashVariant.V1"/> and the specified seed.</summary>
        /// <param name="seed">The <see cref="T:System.UInt32"/> seed used by the hashing algorithm.</param>
        public MetroHash128(UInt32 seed) : this(MetroHashVariant.V1, seed) { }
        #endregion

        #region Methods
        /// <inheritdoc/>
        protected override Byte[] ComputeHashInternal(Byte[] buffer, Int32 offset, Int32 count)
        {
            return m_Engine.ComputeHash(buffer, offset, count);
        }

        /// <inheritdoc/>
        public override String ToString()
        {
            return $"{GetType().Name}-{m_Engine.Name}";
        }
        #endregion

        #region Nesting (Classes)
        private abstract class Engine
        {
            #region Members
            protected readonly UInt32 m_Seed;
            #endregion

            #region Properties
            public UInt32 Seed => m_Seed;
            #endregion

            #region Properties (Abstract)
            public abstract MetroHashVariant Variant { get; }

            public abstract String Name { get; }
            #endregion

            #region Constructors
            protected Engine(UInt32 seed)
            {
                m_Seed = seed;
            }
            #endregion

            #region Methods (Abstract)
            public abstract Byte[] ComputeHash(Byte[] data, Int32 offset, Int32 length);
            #endregion

            #region Methods (Static)
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            protected static UInt64 Fin(UInt64 v1, UInt64 v2, Int32 r, UInt64 k)
            {
                return (v1 + RotateRight((v1 * k) + v2, r));
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            protected static UInt64 Mix(UInt64 v1, UInt64 v2, UInt64 v3, Int32 r1, Int32 r2, UInt64 k1, UInt64 k2, UInt64 k3, UInt64 k4)
            {
                v1 += v3 * k1;
                v1 = RotateRight(v1, r1) * k2;
                v1 ^= RotateRight((v1 * k3) + v2, r2) * k4;

                return v1;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            protected static UInt64 Mix128(UInt64 v1, UInt64 v2, Int32 r, UInt64 k1, UInt64 k2)
            {
                v1 += v2 * k1;
                v1 = RotateRight(v1, r) * k2;

                return v1;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            protected static UInt64 Mix256(UInt64 v1, UInt64 v2, UInt64 v3, UInt64 k)
            {
                v1 += v3 * k; 
                v1 = RotateRight(v1, 29) + v2;

                return v1;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            protected static UInt64 Xor128(UInt64 v1, UInt64 v2, Int32 r, UInt64 k1, UInt64 k2)
            {
                return (v1 ^ RotateRight((v1 * k1) + v2, r) * k2);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            protected static UInt64 Xor256(UInt64 v1, UInt64 v2, UInt64 v3, UInt64 v4, Int32 r, UInt64 k1, UInt64 k2)
            {
                return (v1 ^ RotateRight(((v2 + v3) * k1) + v4, r) * k2);
            }
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
            public override MetroHashVariant Variant => MetroHashVariant.V1;

            public override String Name => "V1";
            #endregion

            #region Constructors
            public Engine1(UInt32 seed) : base(seed) { }
            #endregion

            #region Methods
            public override Byte[] ComputeHash(Byte[] data, Int32 offset, Int32 length)
            {
                UInt64 hash1 = (m_Seed - K0) * K3;
                UInt64 hash2 = (m_Seed + K1) * K2;

                if (length == 0)
                    goto Finalize;

                UInt64 lengthUnsigned = (UInt64)length;
                hash1 += lengthUnsigned;
                hash2 += lengthUnsigned;

                unsafe
                {
                    fixed (Byte* buffer = &data[offset])
                    {
                        Byte* pointer = buffer;
                        Byte* limit = pointer + length;

                        if (length >= 32)
                        {
                            UInt64 v1 = ((m_Seed + K0) * K2) + lengthUnsigned;
                            UInt64 v2 = ((m_Seed - K1) * K3) + lengthUnsigned;

                            do
                            {
                                hash1 = Mix256(hash1, v1, Read64(ref pointer), K0);
                                hash2 = Mix256(hash2, v2, Read64(ref pointer), K1);      
                                v1 = Mix256(v1, hash1, Read64(ref pointer), K2);
                                v2 = Mix256(v2, hash2, Read64(ref pointer), K3);        
                            }
                            while ((limit - 32) >= pointer);

                            v1 = Xor256(v1, hash1, v2, hash2, 26, K0, K1);
                            v2 = Xor256(v2, hash2, v1, hash1, 26, K1, K0);
                            hash1 = Xor256(hash1, hash1, v1, v2, 26, K0, K1);
                            hash2 = Xor256(hash2, hash2, v2, v1, 30, K1, K0);
                        }

                        if ((limit - pointer) >= 16)
                        {
                            hash1 = Mix128(hash1, Read64(ref pointer), 33, K2, K3);
                            hash2 = Mix128(hash2, Read64(ref pointer), 33, K2, K3);                  
                            hash1 = Xor128(hash1, hash2, 17, K2, K1);
                            hash2 = Xor128(hash2, hash1, 17, K3, K0);
                        }

                        if ((limit - pointer) >= 8)
                            hash1 = Mix(hash1, hash2, Read64(ref pointer), 33, 20, K2, K3, K2, K1);

                        if ((limit - pointer) >= 4)
                            hash2 = Mix(hash2, hash1, Read32(ref pointer), 33, 18, K2, K3, K3, K0);

                        if ((limit - pointer) >= 2)
                            hash1 = Mix(hash1, hash2, Read16(ref pointer), 33, 24, K2, K3, K2, K1);

                        if ((limit - pointer) >= 1)
                            hash2 = Mix(hash2, hash1, pointer[0], 33, 24, K2, K3, K3, K0);
                    }
                }

Finalize:

                hash1 = Fin(hash1, hash2, 13, K0);
                hash2 = Fin(hash2, hash1, 37, K1);
                hash1 = Fin(hash1, hash2, 13, K2);
                hash2 = Fin(hash2, hash1, 37, K3);

                Byte[] result = ToByteArray64(hash1, hash2);

                return result;
            }
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
            public override MetroHashVariant Variant => MetroHashVariant.V2;

            public override String Name => "V2";
            #endregion

            #region Constructors
            public Engine2(UInt32 seed) : base(seed) { }
            #endregion

            #region Methods
            public override Byte[] ComputeHash(Byte[] data, Int32 offset, Int32 length)
            {
                UInt64 hash1 = (m_Seed - K0) * K3;
                UInt64 hash2 = (m_Seed + K1) * K2;

                if (length == 0)
                    goto Finalize;

                UInt64 lengthUnsigned = (UInt64)length;
                hash1 += lengthUnsigned;
                hash2 += lengthUnsigned;

                unsafe
                {
                    fixed (Byte* buffer = &data[offset])
                    {
                        Byte* pointer = buffer;
                        Byte* limit = pointer + length;

                        if (length >= 32)
                        {
                            UInt64 v1 = ((m_Seed + K0) * K2) + lengthUnsigned;
                            UInt64 v2 = ((m_Seed - K1) * K3) + lengthUnsigned;

                            do
                            {
                                hash1 = Mix256(hash1, v1, Read64(ref pointer), K0);
                                hash2 = Mix256(hash2, v2, Read64(ref pointer), K1);      
                                v1 = Mix256(v1, hash1, Read64(ref pointer), K2);
                                v2 = Mix256(v2, hash2, Read64(ref pointer), K3);        
                            }
                            while ((limit - 32) >= pointer);

                            v1 = Xor256(v1, hash1, v2, hash2, 33, K0, K1);
                            v2 = Xor256(v2, hash2, v1, hash1, 33, K1, K0);
                            hash1 = Xor256(hash1, hash1, v1, v2, 33, K0, K1);
                            hash2 = Xor256(hash2, hash2, v2, v1, 33, K1, K0);
                        }

                        if ((limit - pointer) >= 16)
                        {
                            hash1 = Mix128(hash1, Read64(ref pointer), 29, K2, K3);
                            hash2 = Mix128(hash2, Read64(ref pointer), 29, K2, K3);                  
                            hash1 = Xor128(hash1, hash2, 29, K2, K1);
                            hash2 = Xor128(hash2, hash1, 29, K3, K0);
                        }

                        if ((limit - pointer) >= 8)
                            hash1 = Mix(hash1, hash2, Read64(ref pointer), 29, 29, K2, K3, K2, K1);

                        if ((limit - pointer) >= 4)
                            hash2 = Mix(hash2, hash1, Read32(ref pointer), 29, 25, K2, K3, K3, K0);

                        if ((limit - pointer) >= 2)
                            hash1 = Mix(hash1, hash2, Read16(ref pointer), 29, 30, K2, K3, K2, K1);

                        if ((limit - pointer) >= 1)
                            hash2 = Mix(hash2, hash1, pointer[0], 29, 18, K2, K3, K3, K0);
                    }
                }

Finalize:

                hash1 = Fin(hash1, hash2, 33, K0);
                hash2 = Fin(hash2, hash1, 33, K1);
                hash1 = Fin(hash1, hash2, 33, K2);
                hash2 = Fin(hash2, hash1, 33, K3);

                Byte[] result = ToByteArray64(hash1, hash2);

                return result;
            }
            #endregion
        }
        #endregion
    }
}