#region Using Directives
using System;
using System.Runtime.CompilerServices;
#endregion

namespace FastHashes.Tests
{
    public static class BitsUtilities
    {
        #region Methods
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Byte GetBit(Byte[] array, Int32 offset, Int32 length, Int32 bit)
        {
            Int32 byteIndex = bit >> 3;
            Int32 arrayIndex = byteIndex + offset;

            bit &= 7;
  
            if ((byteIndex < length) && (arrayIndex < array.Length))
                return (Byte)((array[arrayIndex] >> bit) & 1);

            return 0;
        }

        public static UInt32 Window(Byte[] array, Int32 offset, Int32 length)
        {
            if (length == 0)
                return 0;

            Int32 bytes = array.Length;

            offset %= (bytes * 8);

            UInt32 t;

            if ((bytes & 3) == 0)
            {
                Int32 size = bytes / 4;
                Int32 c = offset & 31;
                Int32 d = offset / 32;

                unsafe
                {
                    fixed (Byte* pin = array)
                    {
                        UInt32* pointer = (UInt32*)pin;

                        if (c == 0)
                            return (UInt32)(pointer[d] & ((1 << length) - 1));

                        UInt32 a = pointer[(d + 1) % size];
                        UInt32 b = pointer[d % size];
  
                        t = (a << (32 - c)) | (b >> c);
                    }
                }
            }
            else
            {
                Int32 c = offset & 7;
                Int32 d = offset / 8;

                t = 0;

                for (Int32 i = 0; i < 4; ++i)
                {
                    UInt32 a = array[(i + d + 1) % bytes];
                    UInt32 b = array[(i + d) % bytes];
                    UInt32 m = (a << (8 - c)) | (b >> c);

                    t |= (m << (8 * i));
                }
            }

            return (UInt32)(t & ((1 << length) - 1));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void FlipBit(Byte[] array, Int32 offset, Int32 length, Int32 bit)
        {
            Int32 byteIndex = bit >> 3;
            Int32 arrayIndex = byteIndex + offset;

            bit &= 7;
  
            if ((byteIndex < length) && (arrayIndex < array.Length))
                array[arrayIndex] ^= (Byte)(1 << bit);
        }

        public static void RotateLeft(Byte[] array, Int32 length, Int32 bit)
        {
            if (bit == 0)
                return;

            if ((length & 3) == 0)
            {
                Int32 nbytes  = length;
                Int32 ndwords = nbytes / 4;

                Int32 limit = bit / 32;

                bit &= 31;

                unsafe
                {
                    fixed (Byte* pin = array)
                    {
                        UInt32* pointer = (UInt32*)pin;

                        UInt32 t;

                        for (Int32 i = 0; i < limit; ++i)
                        {
                            t = pointer[ndwords - 1];

                            for (Int32 j = ndwords-1; j > 0; --j)
                                pointer[j] = pointer[j-1];

                            pointer[0] = t;
                        }

                        if (bit == 0)
                            return;

                        t = pointer[ndwords - 1];

                        for (Int32 i = ndwords - 1; i >= 0; --i)
                        {
                            UInt32 a = pointer[i];
                            UInt32 b = (i == 0) ? t : pointer[i-1];

                            pointer[i] = (a << bit) | (b >> (32 - bit));
                        }
                    }
                }
            }
            else
            {
                Int32 limit = bit >> 3;

                bit &= 7;

                for (Int32 i = 0; i < limit; ++i)
                {
                    Byte k = array[length - 1];

                    for (Int32 j = length - 1; j > 0; --j)
                        array[j] = array[j - 1];

                    array[0] = k;
                }

                if (bit == 0)
                    return;

                Byte t = array[length - 1];

                for (Int32 i = length - 1; i >= 0; --i)
                {
                    Byte a = array[i];
                    Byte b = (i == 0) ? t : array[i-1];

                    array[i] = (Byte)((a << bit) | (b >> (8 - bit)));
                }
            }
        }
        #endregion
    }
}