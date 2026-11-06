#region Using Directives
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
#endregion

namespace FastHashes
{
    /// <summary>Represents the abstract base class for non-cryptographic pseudorandom number generators.</summary>
    public abstract class RandomGenerator<T> where T : unmanaged
    {
        #region Members
        private readonly Queue<Byte> m_Bytes;
        private readonly T m_Seed;
        #endregion

        #region Properties
        /// <summary>A value indicating whether the internal bytes queue is empty.</summary>
        /// <value>An <see cref="T:System.Boolean"/> value.</value>
        [ExcludeFromCodeCoverage]
        public Boolean EmptyQueue => m_Bytes.Count == 0;

        /// <summary>The seed used by the pseudorandom number generator.</summary>
        /// <value>The seed used to initialize the generator.</value>
        [ExcludeFromCodeCoverage]
        public T Seed => m_Seed;
        #endregion

        #region Constructors
        /// <summary>Initializes a new instance using the specified seed.</summary>
        /// <param name="seed">The seed used to initialize the generator.</param>
        [ExcludeFromCodeCoverage]
        protected RandomGenerator(T seed)
        {
            Type t = typeof(T);

            if ((t != typeof(UInt32)) && (t != typeof(UInt64)))
                throw new NotSupportedException("Only UInt32 and UInt64 pseudorandom number generators are supported.");

            m_Bytes = new Queue<Byte>();
            m_Seed = seed;
        }
        #endregion

        #region Methods
        /// <summary>Returns the next pseudorandom value.</summary>
        /// <returns>The next generated value.</returns>
        public abstract T NextValue();

        /// <inheritdoc/>
        [ExcludeFromCodeCoverage]
        public override String ToString()
        {
            return $"{GetType().Name}: {m_Seed}";
        }

        /// <summary>Fills a byte array with pseudorandom numbers.</summary>
        /// <param name="buffer">The <see cref="T:System.Byte"/>[] to fill.</param>
        /// <exception cref="T:System.ArgumentNullException">Thrown when <paramref name="buffer">buffer</paramref> is <c>null</c>.</exception>
        [ExcludeFromCodeCoverage]
        public void NextBytes(Byte[] buffer)
        {
            NextBytes(buffer, 0, buffer?.Length ?? 0);
        }

        /// <summary>Fills the first <paramref name="count">count</paramref> bytes of the specified array with pseudorandom numbers.</summary>
        /// <param name="buffer">The <see cref="T:System.Byte"/>[] to fill.</param>
        /// <param name="count">The number of array bytes to fill.</param>
        /// <exception cref="T:System.ArgumentException">Thrown when <paramref name="count">count</paramref> exceeds the length of <paramref name="buffer">buffer</paramref>.</exception>
        /// <exception cref="T:System.ArgumentNullException">Thrown when <paramref name="buffer">buffer</paramref> is <c>null</c>.</exception>
        /// <exception cref="T:System.ArgumentOutOfRangeException">Thrown when <paramref name="count">count</paramref> is negative.</exception>
        [ExcludeFromCodeCoverage]
        public void NextBytes(Byte[] buffer, Int32 count)
        {
            NextBytes(buffer, 0, count);
        }

        /// <summary>Fills the specified region of a byte array with pseudorandom numbers.</summary>
        /// <param name="buffer">The <see cref="T:System.Byte"/>[] to fill.</param>
        /// <param name="offset">The zero-based starting index in <paramref name="buffer">buffer</paramref>.</param>
        /// <param name="count">The number of array bytes to fill.</param>
        /// <exception cref="T:System.ArgumentException">Thrown when the region defined by <paramref name="offset">offset</paramref> and <paramref name="count">count</paramref> exceeds the bounds of <paramref name="buffer">buffer</paramref>.</exception>
        /// <exception cref="T:System.ArgumentNullException">Thrown when <paramref name="buffer">buffer</paramref> is <c>null</c>.</exception>
        /// <exception cref="T:System.ArgumentOutOfRangeException">Thrown when <paramref name="offset">offset</paramref> is outside the valid range or <paramref name="count">count</paramref> is negative.</exception>
        public void NextBytes(Byte[] buffer, Int32 offset, Int32 count)
        {
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));

            Int32 bufferLength = buffer.Length;

            if ((offset < 0) || (offset > bufferLength))
                throw new ArgumentOutOfRangeException(nameof(offset), "The offset parameter must be within the bounds of the array.");

            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count), "The count parameter must be greater than or equal to 0.");

            if (count > (bufferLength - offset))
                throw new ArgumentException("The block defined by offset and count parameters must be within the bounds of the array.");

            NextBytes(buffer.AsSpan(offset, count));
        }

        /// <summary>Fills the specified byte span with pseudorandom numbers.</summary>
        /// <param name="buffer">The byte span to fill.</param>
        public void NextBytes(Span<Byte> buffer)
        {
            Int32 count = buffer.Length;

            if (count == 0)
                return;

            Int32 index = 0;

            while ((m_Bytes.Count > 0) && (index < count))
                buffer[index++] = m_Bytes.Dequeue();

            Span<T> values = stackalloc T[1];
            Span<Byte> bytes = MemoryMarshal.AsBytes(values);

            while (index < count)
            {
                values[0] = NextValue();

                for (Int32 i = 0; i < bytes.Length; ++i)
                {
                    Byte current = bytes[BitConverter.IsLittleEndian ? i : (bytes.Length - 1 - i)];

                    if (index < count)
                        buffer[index++] = current;
                    else
                        m_Bytes.Enqueue(current);
                }
            }
        }
        #endregion
    }

    /// <summary>Represents a deterministic, non-cryptographic pseudorandom number generator based on the SplitMix64 algorithm.</summary>
    public sealed class RandomSplitMix : RandomGenerator<UInt64>
    {
        #region Members
        private UInt64 m_State;
        #endregion

        #region Constructors
        /// <summary>Initializes a new instance using the specified seed.</summary>
        /// <param name="seed">The <see cref="T:System.UInt64"/> seed used to calculate the starting value of the pseudorandom numbers sequence.</param>
        [ExcludeFromCodeCoverage]
        public RandomSplitMix(UInt64 seed) : base(seed)
        {
            m_State = seed;
        }

        /// <summary>Initializes a new instance using a seed value of <c>0</c>.</summary>
        [ExcludeFromCodeCoverage]
        public RandomSplitMix() : this(0ul) { }
        #endregion

        #region Methods
        /// <inheritdoc/>
        public override UInt64 NextValue()
        {
            unchecked
            {
                m_State += 0x9E3779B97F4A7C15ul;

                UInt64 value = m_State;
                value = (value ^ (value >> 30)) * 0xBF58476D1CE4E5B9ul;
                value = (value ^ (value >> 27)) * 0x94D049BB133111EBul;
                value ^= value >> 31;

                return value;
            }
        }
        #endregion
    }

    /// <summary>Represents a deterministic, non-cryptographic pseudorandom number generator based on a four-state XorShift algorithm.</summary>
    public sealed class RandomXorShift : RandomGenerator<UInt32>
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
        #endregion

        #region Constructors
        /// <summary>Initializes a new instance using the specified seed.</summary>
        /// <param name="seed">The <see cref="T:System.UInt32"/> seed used to calculate the starting value of the pseudorandom numbers sequence.</param>
        [ExcludeFromCodeCoverage]
        public RandomXorShift(UInt32 seed) : base(seed)
        {
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
        /// <inheritdoc/>
        public override UInt32 NextValue()
        {
            UInt32 t = m_X ^ (m_X << 11);

            m_X = m_Y;
            m_Y = m_Z;
            m_Z = m_W;
            m_W ^= (m_W >> 19) ^ (t ^ (t >> 8));

            return m_W;
        }
        #endregion
    }
}