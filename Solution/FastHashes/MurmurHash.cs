#region Using Directives
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
#endregion

namespace FastHashes
{
    /// <summary>Represents the base class from which all implementations of MurmurHash with more than 32 bits of output must derive. This class is abstract.</summary>
    public abstract class MurmurHashG32 : Hash
    {
        #region Members
        private readonly Engine m_Engine;
        #endregion

        #region Properties
        /// <summary>Gets the engine category of the hashing algorithm.</summary>
        /// <value>An enumeration value representing the engine category of the hashing algorithm. See <see cref="T:FastHashes.MurmurHashEngine"/>.</value>
        public MurmurHashEngine Category => m_Engine.Category;

        /// <inheritdoc/>
        public override Int32 Length => 64;

        /// <summary>Gets the seed used by the hashing algorithm.</summary>
        /// <value>A <see cref="T:System.UInt32"/> value.</value>
        public UInt32 Seed => m_Engine.Seed;
        #endregion

        #region Constructors
        /// <summary>Represents the base constructor used by derived classes.</summary>
        /// <param name="engine">The engine category used by the hashing algorithm. See <see cref="T:FastHashes.MurmurHashEngine"/>.</param>
        /// <param name="seed">The <see cref="T:System.UInt32"/> seed used by the hashing algorithm.</param>
        /// <exception cref="T:System.ComponentModel.InvalidEnumArgumentException">Thrown when the value of <paramref name="engine">engine</paramref> is undefined.</exception>
        protected MurmurHashG32(MurmurHashEngine engine, UInt32 seed)
        {
            if (!Enum.IsDefined(typeof(MurmurHashEngine), engine))
                throw new InvalidEnumArgumentException("Invalid engine specified.");

            switch (engine)
            {
                case MurmurHashEngine.x64:
                    m_Engine = new Engine64(seed);
                    break;

                case MurmurHashEngine.x86:
                    m_Engine = new Engine86(seed);
                    break;

                default:
                {
                    if (Environment.Is64BitProcess)
                        m_Engine = new Engine64(seed);
                    else
                        m_Engine = new Engine86(seed);

                    break;
                }
            }
        }
        #endregion

        #region Methods
        /// <inheritdoc/>
        protected override Byte[] ComputeHashInternal(Byte[] buffer, Int32 offset, Int32 count)
        {
            return GetHash(m_Engine.ComputeHash(buffer, offset, count));
        }

        /// <inheritdoc/>
        public override String ToString()
        {
            return String.Concat(GetType().Name, "-", m_Engine.Name);
        }
        #endregion

        #region Methods (Abstract)
        /// <summary>Finalizes any partial computation and returns the hash code.</summary>
        /// <param name="hashData">A <see cref="T:System.Byte"/>[] representing the hash data.</param>
        /// <returns>A <see cref="T:System.Byte"/>[] representing the hash code.</returns>
        protected abstract Byte[] GetHash(Byte[] hashData);
        #endregion

        #region Nesting (Classes)
        private abstract class Engine
        {
            #region Members
            private readonly UInt32 m_OriginalSeed;
            #endregion

            #region Properties (Abstract)
            public UInt32 Seed => m_OriginalSeed;
            #endregion

            #region Properties (Abstract)
            public abstract MurmurHashEngine Category { get; }

            public abstract String Name { get; }
            #endregion

            #region Constructors
            public Engine(UInt32 seed)
            {
                m_OriginalSeed = seed;
            }
            #endregion

            #region Methods (Abstract)
            public abstract Byte[] ComputeHash(Byte[] data, Int32 offset, Int32 length);
            #endregion
        }

        private sealed class Engine64 : Engine
        {
            #region Constants
            private const UInt64 C1 = 0x87C37B91114253D5ul;
            private const UInt64 C2 = 0x4CF5AD432745937Ful;
            private const UInt64 F1 = 0xFF51AFD7ED558CCDul;
            private const UInt64 F2 = 0xC4CEB9FE1A85EC53ul;
            private const UInt64 N1 = 0x52DCE729ul;
            private const UInt64 N2 = 0x38495AB5ul;
            #endregion

            #region Members
            private readonly UInt64 m_Seed1;
            private readonly UInt64 m_Seed2;
            #endregion

            #region Properties
            public override MurmurHashEngine Category => MurmurHashEngine.x64;

            public override String Name => "x64";
            #endregion

            #region Constructors
            public Engine64(UInt32 seed) : base(seed)
            {
                m_Seed1 = seed;
                m_Seed2 = seed;
            }
            #endregion

            #region Methods
            public override Byte[] ComputeHash(Byte[] data, Int32 offset, Int32 length)
            {
                UInt64 hash1 = m_Seed1;
                UInt64 hash2 = m_Seed2;

                if (length == 0)
                    goto Finalize;

                unsafe
                {
                    fixed (Byte* pin = &data[offset])
                    {
                        Byte* pointer = pin;

                        Int32 blocks = length / 16;
                        Int32 remainder = length & 15;

                        while (blocks-- > 0)
                        {
                            UInt64 v = Mix(Read64(ref pointer), C1, C2, 31);
                            hash1 = Mur(hash1, hash2, v, 27, N1);

                            v = Mix(Read64(ref pointer), C2, C1, 33);
                            hash2 = Mur(hash2, hash1, v, 31, N2);
                        }

                        UInt64 v1 = 0ul;
                        UInt64 v2 = 0ul;

                        switch (remainder)
                        {
                            case 15: v2 ^= (UInt64)pointer[14] << 48; goto case 14;
                            case 14: v2 ^= (UInt64)pointer[13] << 40; goto case 13;
                            case 13: v2 ^= (UInt64)pointer[12] << 32; goto case 12;
                            case 12: v2 ^= (UInt64)pointer[11] << 24; goto case 11;
                            case 11: v2 ^= (UInt64)pointer[10] << 16; goto case 10;
                            case 10: v2 ^= (UInt64)pointer[9] << 8; goto case 9;
                            case 9:
                                v2 ^= pointer[8];
                                hash2 ^= Mix(v2, C2, C1, 33);
                                goto case 8;
                            case 8: v1 ^= (UInt64)pointer[7] << 56; goto case 7;
                            case 7: v1 ^= (UInt64)pointer[6] << 48; goto case 6;
                            case 6: v1 ^= (UInt64)pointer[5] << 40; goto case 5;
                            case 5: v1 ^= (UInt64)pointer[4] << 32; goto case 4;
                            case 4: v1 ^= (UInt64)pointer[3] << 24; goto case 3;
                            case 3: v1 ^= (UInt64)pointer[2] << 16; goto case 2;
                            case 2: v1 ^= (UInt64)pointer[1] << 8; goto case 1;
                            case 1:
                                v1 ^= pointer[0];
                                hash1 ^= Mix(v1, C1, C2, 31);
                                break;
                        }
                    }
                }

                Finalize:

                UInt64 lengthUnsigned = (UInt64)length;
                hash1 ^= lengthUnsigned;
                hash2 ^= lengthUnsigned;

                hash1 += hash2;
                hash2 += hash1;

                hash1 = Fin(hash1);
                hash2 = Fin(hash2);

                hash1 += hash2;
                hash2 += hash1;

                Byte[] result = new Byte[16];

                unsafe
                {
                    fixed (Byte* pin = result)
                    {
                        UInt64* pointer = (UInt64*)pin;
                        pointer[0] = hash1;
                        pointer[1] = hash2;
                    }
                }

                return result;
            }
            #endregion

            #region Methods (Static)
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static UInt64 Fin(UInt64 hash)
            {
                hash ^= hash >> 33;
                hash *= F1;
                hash ^= hash >> 33;
                hash *= F2;
                hash ^= hash >> 33;

                return hash;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static UInt64 Mix(UInt64 v, UInt64 c1, UInt64 c2, Int32 r)
            {
                v *= c1;
                v = RotateLeft(v, r);
                v *= c2;

                return v;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static UInt64 Mur(UInt64 v1, UInt64 v2, UInt64 v3, Int32 r, UInt64 n)
            {
                v1 ^= v3;
                v1 = RotateLeft(v1, r);
                v1 += v2;
                v1 = (v1 * 5ul) + n;

                return v1;
            }
            #endregion
        }

        private sealed class Engine86 : Engine
        {
            #region Constants
            private const UInt32 C1 = 0x239B961Bu;
            private const UInt32 C2 = 0xAB0E9789u;
            private const UInt32 C3 = 0x38B34AE5u;
            private const UInt32 C4 = 0xA1E38B93u;
            private const UInt32 F1 = 0x85EBCA6Bu;
            private const UInt32 F2 = 0xC2B2AE35u;
            private const UInt32 N1 = 0x561CCD1Bu;
            private const UInt32 N2 = 0x0BCAA747u;
            private const UInt32 N3 = 0x96CD1C35u;
            private const UInt32 N4 = 0x32AC3B17u;
            #endregion

            #region Members
            private readonly UInt32 m_Seed1;
            private readonly UInt32 m_Seed2;
            private readonly UInt32 m_Seed3;
            private readonly UInt32 m_Seed4;
            #endregion

            #region Properties
            public override MurmurHashEngine Category => MurmurHashEngine.x86;

            public override String Name => "x86";
            #endregion

            #region Constructors
            public Engine86(UInt32 seed) : base(seed)
            {
                m_Seed1 = seed;
                m_Seed2 = seed;
                m_Seed3 = seed;
                m_Seed4 = seed;
            }
            #endregion

            #region Methods
            public override Byte[] ComputeHash(Byte[] data, Int32 offset, Int32 length)
            {
                UInt32 hash1 = m_Seed1;
                UInt32 hash2 = m_Seed2;
                UInt32 hash3 = m_Seed3;
                UInt32 hash4 = m_Seed4;

                if (length == 0)
                    goto Finalize;

                unsafe
                {
                    fixed (Byte* pin = &data[offset])
                    {
                        Byte* pointer = pin;

                        Int32 blocks = length / 16;
                        Int32 remainder = length & 15;

                        while (blocks-- > 0)
                        {
                            UInt32 v = Mix(Read32(ref pointer), C1, C2, 15);
                            hash1 = Mur(hash1, hash2, v, 19, N1);

                            v = Mix(Read32(ref pointer), C2, C3, 16);
                            hash2 = Mur(hash2, hash3, v, 17, N2);

                            v = Mix(Read32(ref pointer), C3, C4, 17);
                            hash3 = Mur(hash3, hash4, v, 15, N3);

                            v = Mix(Read32(ref pointer), C4, C1, 18);
                            hash4 = Mur(hash4, hash1, v, 13, N4);
                        }

                        UInt32 v1 = 0u;
                        UInt32 v2 = 0u;
                        UInt32 v3 = 0u;
                        UInt32 v4 = 0u;

                        switch (remainder)
                        {
                            case 15: v4 ^= (UInt32)pointer[14] << 16; goto case 14;
                            case 14: v4 ^= (UInt32)pointer[13] << 8; goto case 13;
                            case 13:
                                v4 ^= pointer[12];
                                hash4 ^= Mix(v4, C4, C1, 18);
                                goto case 12;
                            case 12: v3 ^= (UInt32)pointer[11] << 24; goto case 11;
                            case 11: v3 ^= (UInt32)pointer[10] << 16; goto case 10;
                            case 10: v3 ^= (UInt32)pointer[9] << 8; goto case 9;
                            case 9:
                                v3 ^= pointer[8];
                                hash3 ^= Mix(v3, C3, C4, 17);
                                goto case 8;
                            case 8: v2 ^= (UInt32)pointer[7] << 24; goto case 7;
                            case 7: v2 ^= (UInt32)pointer[6] << 16; goto case 6;
                            case 6: v2 ^= (UInt32)pointer[5] << 8; goto case 5;
                            case 5:
                                v2 ^= pointer[4];
                                hash2 ^= Mix(v2, C2, C3, 16);
                                goto case 4;
                            case 4: v1 ^= (UInt32)pointer[3] << 24; goto case 3;
                            case 3: v1 ^= (UInt32)pointer[2] << 16; goto case 2;
                            case 2: v1 ^= (UInt32)pointer[1] << 8; goto case 1;
                            case 1:
                                v1 ^= pointer[0];
                                hash1 ^= Mix(v1, C1, C2, 15);
                                break;
                        }
                    }
                }

                Finalize:

                UInt32 lengthUnsigned = (UInt32)length;
                hash1 ^= lengthUnsigned;
                hash2 ^= lengthUnsigned;
                hash3 ^= lengthUnsigned;
                hash4 ^= lengthUnsigned;

                hash1 += hash2 + hash3 + hash4;
                hash2 += hash1;
                hash3 += hash1;
                hash4 += hash1;

                hash1 = Fin(hash1);
                hash2 = Fin(hash2);
                hash3 = Fin(hash3);
                hash4 = Fin(hash4);

                hash1 += hash2 + hash3 + hash4;
                hash2 += hash1;
                hash3 += hash1;
                hash4 += hash1;

                Byte[] result = new Byte[16];

                unsafe
                {
                    fixed (Byte* pin = result)
                    {
                        UInt32* pointer = (UInt32*)pin;
                        pointer[0] = hash1;
                        pointer[1] = hash2;
                        pointer[2] = hash3;
                        pointer[3] = hash4;
                    }
                }

                return result;
            }
            #endregion

            #region Methods (Static)
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static UInt32 Fin(UInt32 hash)
            {
                hash ^= hash >> 16;
                hash *= F1;
                hash ^= hash >> 13;
                hash *= F2;
                hash ^= hash >> 16;

                return hash;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static UInt32 Mix(UInt32 v, UInt32 c1, UInt32 c2, Int32 r)
            {
                v *= c1;
                v = RotateLeft(v, r);
                v *= c2;

                return v;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static UInt32 Mur(UInt32 v1, UInt32 v2, UInt32 v3, Int32 r, UInt32 n)
            {
                v1 ^= v3;
                v1 = RotateLeft(v1, r);
                v1 += v2;
                v1 = (v1 * 5u) + n;

                return v1;
            }
            #endregion
        }
        #endregion
    }

    /// <summary>Represents the MurmurHash32 implementation. This class cannot be derived.</summary>
    public sealed class MurmurHash32 : Hash
    {
        #region Constants
        private const UInt32 C1 = 0xCC9E2D51u;
        private const UInt32 C2 = 0x1B873593u;
        private const UInt32 F1 = 0x85EBCA6Bu;
        private const UInt32 F2 = 0xC2B2AE35u;
        private const UInt32 N = 0xE6546B64u;
        #endregion

        #region Members
        private readonly UInt32 m_Seed;
        #endregion

        #region Properties
        /// <inheritdoc/>
        public override Int32 Length => 32;

        /// <summary>Gets the seed used by the hashing algorithm.</summary>
        /// <value>A <see cref="T:System.UInt32"/> value.</value>
        public UInt32 Seed => m_Seed;
        #endregion

        #region Constructors
        /// <summary>Initializes a new instance of <see cref="T:FastHashes.MurmurHash32"/> using the specified seed.</summary>
        /// <param name="seed">The <see cref="T:System.UInt32"/> seed used by the hashing algorithm.</param>
        public MurmurHash32(UInt32 seed)
        {
            m_Seed = seed;
        }

        /// <summary>Initializes a new instance of <see cref="T:FastHashes.MurmurHash32"/> using a seed value of <c>0</c>.</summary>
        public MurmurHash32() : this(0u) { }
        #endregion

        #region Methods
        /// <inheritdoc/>
        protected override Byte[] ComputeHashInternal(Byte[] buffer, Int32 offset, Int32 count)
        {
            UInt32 hash = m_Seed;

            if (count == 0)
                goto Finalize;

            unsafe
            {
                fixed (Byte* pin = &buffer[offset])
                {
                    Byte* pointer = pin;

                    Int32 blocks = count / 4;
                    Int32 remainder = count & 3;

                    while (blocks-- > 0)
                        hash = Mur(hash, Read32(ref pointer));

                    UInt32 v = 0u;

                    switch (remainder)
                    {
                        case 3: v ^= (UInt32)pointer[2] << 16; goto case 2;
                        case 2: v ^= (UInt32)pointer[1] << 8; goto case 1;
                        case 1:
                            v ^= pointer[0];
                            hash ^= Mix(v);
                            break;
                    }
                }
            }

            Finalize:

            hash ^= (UInt32)count;
            hash ^= hash >> 16;
            hash *= F1;
            hash ^= hash >> 13;
            hash *= F2;
            hash ^= hash >> 16;

            Byte[] result = new Byte[4];

            unsafe
            {
                fixed (Byte* pointer = result)
                    *((UInt32*)pointer) = hash;
            }

            return result;
        }
        #endregion

        #region Methods (Static)
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static UInt32 Mix(UInt32 v)
        {
            v *= C1;
            v = RotateLeft(v, 15);
            v *= C2;

            return v;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static UInt32 Mur(UInt32 v1, UInt32 v2)
        {
            v1 ^= Mix(v2);
            v1 = RotateLeft(v1, 13);
            v1 = (v1 * 5u) + N;

            return v1;
        }
        #endregion
    }

    /// <summary>Represents the MurmurHash64 implementation. This class cannot be derived.</summary>
    public sealed class MurmurHash64 : MurmurHashG32
    {
        #region Properties
        /// <inheritdoc/>
        public override Int32 Length => 64;
        #endregion

        #region Constructors
        /// <summary>Initializes a new instance of <see cref="T:FastHashes.MurmurHash64"/> using the automatic engine selection and a seed value of <c>0</c>.</summary>
        public MurmurHash64() : base(MurmurHashEngine.Auto, 0u) { }
        
        /// <summary>Initializes a new instance of <see cref="T:FastHashes.MurmurHash64"/> using the specified engine category and a seed value of <c>0</c>.</summary>
        /// <param name="engine">The engine of the hashing algorithm. See <see cref="T:FastHashes.MurmurHashEngine"/>.</param>
        public MurmurHash64(MurmurHashEngine engine) : base(engine, 0u) {}
        
        /// <summary>Initializes a new instance of <see cref="T:FastHashes.MurmurHash64"/> using the automatic engine category selection and the specified seed.</summary>
        /// <param name="seed">The <see cref="T:System.UInt32"/> seed used by the hashing algorithm.</param>
        public MurmurHash64(UInt32 seed) : base(MurmurHashEngine.Auto, seed) { }

        /// <summary>Initializes a new instance of <see cref="T:FastHashes.MurmurHash64"/> using the specified engine category and seed.</summary>
        /// <param name="engine">The engine of the hashing algorithm. See <see cref="T:FastHashes.MurmurHashEngine"/>.</param>
        /// <param name="seed">The <see cref="T:System.UInt32"/> seed used by the hashing algorithm.</param>
        /// <exception cref="T:System.ComponentModel.InvalidEnumArgumentException">Thrown when the value of <paramref name="engine">engine</paramref> is undefined.</exception>
        public MurmurHash64(MurmurHashEngine engine, UInt32 seed) : base(engine, seed) {}
        #endregion

        #region Methods
        /// <inheritdoc/>
        protected override Byte[] GetHash(Byte[] hashData)
        {
            Byte[] result = new Byte[8];
            UnsafeBuffer.BlockCopy(hashData, 0, result, 0, 8);

            return result;
        }
        #endregion
    }

    /// <summary>Represents the MurmurHash128 implementation. This class cannot be derived.</summary>
    public sealed class MurmurHash128 : MurmurHashG32
    {
        #region Properties
        /// <inheritdoc/>
        public override Int32 Length => 128;
        #endregion

        #region Constructors
        /// <summary>Initializes a new instance of <see cref="T:FastHashes.MurmurHash128"/> using the automatic engine selection and a seed value of <c>0</c>.</summary>
        public MurmurHash128() : base(MurmurHashEngine.Auto, 0u) { }

        /// <summary>Initializes a new instance of <see cref="T:FastHashes.MurmurHash128"/> using the specified engine category and a seed value of <c>0</c>.</summary>
        /// <param name="engine">The engine of the hashing algorithm. See <see cref="T:FastHashes.MurmurHashEngine"/>.</param>
        public MurmurHash128(MurmurHashEngine engine) : base(engine, 0u) { }

        /// <summary>Initializes a new instance of <see cref="T:FastHashes.MurmurHash128"/> using the automatic engine selection and the specified seed.</summary>
        /// <param name="seed">The <see cref="T:System.UInt32"/> seed used by the hashing algorithm.</param>
        public MurmurHash128(UInt32 seed) : base(MurmurHashEngine.Auto, seed) { }

        /// <summary>Initializes a new instance of <see cref="T:FastHashes.MurmurHash128"/> using the specified engine category and seed.</summary>
        /// <param name="engine">The engine of the hashing algorithm. See <see cref="T:FastHashes.MurmurHashEngine"/>.</param>
        /// <param name="seed">The <see cref="T:System.UInt32"/> seed used by the hashing algorithm.</param>
        /// <exception cref="T:System.ComponentModel.InvalidEnumArgumentException">Thrown when the value of <paramref name="engine">engine</paramref> is undefined.</exception>
        public MurmurHash128(MurmurHashEngine engine, UInt32 seed) : base(engine, seed) { }
        #endregion

        #region Methods
        /// <inheritdoc/>
        protected override Byte[] GetHash(Byte[] hashData)
        {
            return hashData;
        }
        #endregion
    }
}