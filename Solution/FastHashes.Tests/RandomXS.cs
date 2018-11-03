#region Using Directives
using System;
using System.Collections.Generic;
#endregion

namespace FastHashes.Tests
{
    public sealed class RandomXS
    {
        #region Constants
        private const UInt32 Y = 0x159A55E5u;
        private const UInt32 Z = 0x1F123BB5u;
        private const UInt32 W = 0x05491333u;
        #endregion

        #region Members
        private UInt32 m_X;
        private UInt32 m_Y;
        private UInt32 m_Z;
        private UInt32 m_W;
        private readonly Queue<Byte> m_Bytes;
        #endregion

        #region Constructors
        public RandomXS(UInt32 seed)
        {
            m_Bytes = new Queue<Byte>();

            m_X = seed;
            m_Y = Y;
            m_Z = Z;
            m_W = W;
        }

        public RandomXS() : this(0u) { }
        #endregion

        #region Methods
        private unsafe void NextBytes(Byte* pointer, Int32 length)
        {
            Int32 offset = 0;

            while ((m_Bytes.Count > 0) && (offset < length))
                pointer[offset++] = m_Bytes.Dequeue();

            Int32 blocks = (length - offset) / 4;

            if (blocks > 0)
            {
                UInt32* start = (UInt32*)(pointer + offset);
                UInt32* end = start + blocks;

                while (start < end)
                    *(start++) = NextValue();

                offset += blocks * 4;
            }

            while (offset < length)
            {
                if (m_Bytes.Count == 0)
                {
                    UInt32 value = NextValue();
 
                    m_Bytes.Enqueue((Byte)(value & 0x000000FFu));
                    m_Bytes.Enqueue((Byte)((value >> 8) & 0x000000FFu));
                    m_Bytes.Enqueue((Byte)((value >> 16) & 0x000000FFu));
                    m_Bytes.Enqueue((Byte)((value >> 24) & 0x000000FFu));
                }
 
                pointer[offset++] = m_Bytes.Dequeue();
            }
        }

        public UInt32 NextValue()
        {
            UInt32 t = m_X ^ (m_X << 11);

            m_X = m_Y;
            m_Y = m_Z;
            m_Z = m_W;
            m_W = m_W ^ (m_W >> 19) ^ (t ^ (t >> 8));

            return m_W;
        }

        public void NextBytes(Byte[] array)
        {
            if (array == null)
                throw new ArgumentNullException(nameof(array));

            if (array.Length == 0)
                return;

            unsafe
            {
                fixed (Byte* pin = array)
                {
                    Byte* pointer = pin;
                    NextBytes(pointer, array.Length);
                }       
            }
        }

        public void NextBytes(Byte[] array, Int32 length)
        {
            if (array == null)
                throw new ArgumentNullException(nameof(array));

            if (array.Length == 0)
                return;

            if ((length < 0) || (length > array.Length))
                throw new ArgumentOutOfRangeException(nameof(length), "The length parameter must be between zero and the number of elements in the array.");

            unsafe
            {
                fixed (Byte* pin = array)
                {
                    Byte* pointer = pin;
                    NextBytes(pointer, length);
                }
            }
        }

        public void NextBytes(Byte[] array, Int32 offset, Int32 length)
        {
            if (array == null)
                throw new ArgumentNullException(nameof(array));

            if (array.Length == 0)
                return;

            if ((offset < 0) || (offset > array.Length))
                throw new ArgumentOutOfRangeException(nameof(offset), "The offset parameter must be within the bounds of the array.");

            if ((length < 0) || (length > array.Length))
                throw new ArgumentOutOfRangeException(nameof(length), "The length parameter must be between zero and the number of elements in the array.");

            if (length > (array.Length - offset))
                throw new InvalidOperationException("The block defined by offset and length parameters must be within the bounds of the array.");

            unsafe
            {
                fixed (Byte* pin = &array[offset])
                {
                    Byte* pointer = pin;
                    NextBytes(pointer, length);
                }
            }
        }
        #endregion
    }
}
