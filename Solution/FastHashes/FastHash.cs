#region Using Directives
using System;
using System.Runtime.CompilerServices;
#endregion

namespace FastHashes
{
    public abstract class FastHash : Hash
    {
        #region Constants
        private const UInt64 M = 0x880355f21e6d1965ul;
        private const UInt64 N = 0x2127599bf4325c37ul;
        #endregion

        #region Properties
        protected UInt64 Seed { get; }
        #endregion

        #region Constructors
        protected FastHash(UInt64 seed)
        {
            Seed = seed;
        }
        #endregion

        #region Methods
        protected override Byte[] ComputeHashInternal(Byte[] data, Int32 offset, Int32 length)
        {
            UInt64 hash = Seed;

            if (length == 0)
                goto Finalize;

            hash ^= (UInt64)length * M;

            unsafe
            {
                fixed (Byte* pin = &data[offset])
                {
                    Byte* pointer = pin;

                    Int32 blocks = length / 8;
                    Int32 remainder = length & 7;

                    while (blocks-- > 0)
                        hash = Mix(hash, Read64(ref pointer));

                    UInt64 v = 0u;

                    switch (remainder)
                    {
                        case 7: v ^= (UInt64)pointer[6] << 48; goto case 6;
                        case 6: v ^= (UInt64)pointer[5] << 40; goto case 5;
                        case 5: v ^= (UInt64)pointer[4] << 32; goto case 4;
                        case 4: v ^= (UInt64)pointer[3] << 24; goto case 3;
                        case 3: v ^= (UInt64)pointer[2] << 16; goto case 2;
                        case 2: v ^= (UInt64)pointer[1] << 8; goto case 1;
                        case 1:
                            v ^= pointer[0];
                            hash = Mix(hash, v);
                            break;
                    }
                }
            }

            Finalize:

            hash ^= hash >> 23;
            hash *= N;
            hash ^= hash >> 47;

            return GetHash(hash);
        }
        #endregion

        #region Methods (Abstract)
        protected abstract Byte[] GetHash(UInt64 hash);
        #endregion

        #region Methods (Static)
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
        #endregion
    }

    public sealed class FastHash32 : FastHash
    {
        #region Properties
        public override Int32 Length => 32;
        #endregion

        #region Constructors
        public FastHash32() : base(0ul) { }

        public FastHash32(UInt64 seed) : base(seed) { }
        #endregion

        #region Methods
        protected override Byte[] GetHash(UInt64 hash)
        {
            Byte[] result = new Byte[4];

            unsafe
            {
                fixed (Byte* pointer = result)
                    *((UInt32*)pointer) = (UInt32)(hash - (hash >> 32));
            }

            return result;
        }
        #endregion
    }

    public sealed class FastHash64 : FastHash
    {
        #region Properties
        public override Int32 Length => 64;
        #endregion

        #region Constructors
        public FastHash64() : base(0ul) { }

        public FastHash64(UInt64 seed) : base(seed) { }
        #endregion

        #region Methods
        protected override Byte[] GetHash(UInt64 hash)
        {
            Byte[] result = new Byte[8];

            unsafe
            {
                fixed (Byte* pointer = result)
                    *((UInt64*)pointer) = hash;
            }

            return result;
        }
        #endregion
    }
}