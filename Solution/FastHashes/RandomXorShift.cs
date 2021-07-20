#region Using Directives
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

#endregion

namespace FastHashes
{
    /// <summary>Represents a pseudorandom numbers generator based on a variant of the XorShift approach.</summary>
    public sealed class RandomXorShift
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

        #region Properties
        /// <summary>The seed used by the pseudorandom numbers generator.</summary>
        /// <value>An <see cref="T:System.UInt32"/> value.</value>
        [ExcludeFromCodeCoverage] 
        public UInt32 Seed => m_X;
        #endregion

        #region Constructors
        /// <summary>Initializes a new instance using the specified seed.</summary>
        /// <param name="seed">The <see cref="T:System.UInt32"/> seed used to calculate the starting value of the pseudorandom numbers sequence.</param>
        [ExcludeFromCodeCoverage]
        public RandomXorShift(UInt32 seed)
        {
            m_Bytes = new Queue<Byte>();

            m_X = seed;
            m_Y = Y;
            m_Z = Z;
            m_W = W;
        }

        /// <summary>Initializes a new instance using a seed value of <c>0</c>.</summary>
        [ExcludeFromCodeCoverage]
        public RandomXorShift() : this(0u) { }
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

        /// <inheritdoc/>
        [ExcludeFromCodeCoverage]
        public override String ToString()
        {
            return $"{GetType().Name}: {m_X}";
        }

        /// <summary>Returns a random 4-byte unsigned integer.</summary>
        /// <returns>An <see cref="T:System.UInt32"/> value.</returns>
        public UInt32 NextValue()
        {
            UInt32 t = m_X ^ (m_X << 11);

            m_X = m_Y;
            m_Y = m_Z;
            m_Z = m_W;
            m_W ^= (m_W >> 19) ^ (t ^ (t >> 8));

            return m_W;
        }

        /// <summary>Fills a byte array with random numbers.</summary>
        /// <param name="buffer">The <see cref="T:System.Byte"/>[] to fill.</param>
        /// <exception cref="T:System.ArgumentNullException">Thrown when <paramref name="buffer">buffer</paramref> is <c>null</c>.</exception>
        public void NextBytes(Byte[] buffer)
        {
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));

            NextBytes(buffer, 0, buffer.Length);
        }

        /// <summary>Fills the specified number of elements of a byte array with random numbers, starting at the first element.</summary>
        /// <param name="buffer">The <see cref="T:System.Byte"/>[] to fill.</param>
        /// <param name="count">The number of bytes in the array to fill.</param>
        /// <exception cref="T:System.ArgumentException">Thrown when the number of bytes in <paramref name="buffer">buffer</paramref> is less than <paramref name="count">count</paramref>.</exception>
        /// <exception cref="T:System.ArgumentNullException">Thrown when <paramref name="buffer">buffer</paramref> is <c>null</c>.</exception>
        /// <exception cref="T:System.ArgumentOutOfRangeException">Thrown when <paramref name="count">count</paramref> is less than <c>0</c>.</exception>
        public void NextBytes(Byte[] buffer, Int32 count)
        {
            NextBytes(buffer, 0, count);
        }

        /// <summary>Fills the specified region of a byte array with random numbers.</summary>
        /// <param name="buffer">The <see cref="T:System.Byte"/>[] to fill.</param>
        /// <param name="offset">The offset into the byte array from which to begin the fill operation.</param>
        /// <param name="count">The number of bytes in the array to fill.</param>
        /// <exception cref="T:System.ArgumentException">Thrown when the number of bytes in <paramref name="buffer">buffer</paramref> is less than <paramref name="offset">sourceOffset</paramref> plus <paramref name="count">count</paramref>.</exception>
        /// <exception cref="T:System.ArgumentNullException">Thrown when <paramref name="buffer">buffer</paramref> is <c>null</c>.</exception>
        /// <exception cref="T:System.ArgumentOutOfRangeException">Thrown when <paramref name="offset">offset</paramref> is not within the bounds of <paramref name="buffer">buffer</paramref> or when <paramref name="count">count</paramref> is less than <c>0</c>.</exception>
        public void NextBytes(Byte[] buffer, Int32 offset, Int32 count)
        {
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));

            if (buffer.Length == 0)
                return;

            if ((offset < 0) || (offset > buffer.Length))
                throw new ArgumentOutOfRangeException(nameof(offset), "The offset parameter must be within the bounds of the array.");

            if ((count < 0) || (count > buffer.Length))
                throw new ArgumentOutOfRangeException(nameof(count), "The count parameter must be between zero and the number of elements in the array.");

            if (count > (buffer.Length - offset))
                throw new ArgumentException("The block defined by offset and count parameters must be within the bounds of the array.");

            unsafe
            {
                fixed (Byte* pin = &buffer[offset])
                {
                    Byte* pointer = pin;
                    NextBytes(pointer, count);
                }
            }
        }
        #endregion
    }
}