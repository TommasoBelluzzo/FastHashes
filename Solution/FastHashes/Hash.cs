#region Using Directives
using System;
using System.Diagnostics.CodeAnalysis;
#endregion

namespace FastHashes
{
    /// <summary>Represents the base class from which all the hash algorithms must derive. This class is abstract.</summary>
    public abstract class Hash
    {
        #region Properties
        /// <summary>Gets the size, in bits, of the computed hash code.</summary>
        /// <value>An <see cref="T:System.Int32"/> value, greater than or equal to <c>32</c>.</value>
        public abstract Int32 Length { get; }
        #endregion

        #region Methods
        /// <summary>Represents the core hashing function of the algorithm.</summary>
        /// <param name="buffer">The <see cref="T:System.ReadOnlySpan`1{T}">ReadOnlySpan&lt;byte&gt;</see> whose hash must be computed.</param>
        /// <returns>A <see cref="T:System.Byte"/>[] representing the computed hash.</returns>
        protected abstract Byte[] ComputeHashInternal(ReadOnlySpan<Byte> buffer);

        /// <summary>Computes the hash of the specified byte array.</summary>
        /// <param name="buffer">The <see cref="T:System.Byte"/>[] whose hash must be computed.</param>
        /// <returns>A <see cref="T:System.Byte"/>[] representing the computed hash.</returns>
        /// <exception cref="T:System.ArgumentNullException">Thrown when <paramref name="buffer">buffer</paramref> is <c>null</c>.</exception>
        public Byte[] ComputeHash(Byte[] buffer)
        {
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));

            return ComputeHash(buffer, 0, buffer.Length);
        }

        /// <summary>Computes the hash of the specified number of elements of a byte array, starting at the first element.</summary>
        /// <param name="buffer">The <see cref="T:System.Byte"/>[] whose hash must be computed.</param>
        /// <param name="count">The number of bytes in the array to use as data.</param>
        /// <returns>A <see cref="T:System.Byte"/>[] representing the computed hash.</returns>
        /// <exception cref="T:System.ArgumentException">Thrown when the number of bytes in <paramref name="buffer">buffer</paramref> is less than <paramref name="count">count</paramref>.</exception>
        /// <exception cref="T:System.ArgumentNullException">Thrown when <paramref name="buffer">buffer</paramref> is <c>null</c>.</exception>
        /// <exception cref="T:System.ArgumentOutOfRangeException">Thrown when <paramref name="count">count</paramref> is less than <c>0</c>.</exception>
        public Byte[] ComputeHash(Byte[] buffer, Int32 count)
        {
            return ComputeHash(buffer, 0, count);
        }

        /// <summary>Computes the hash of the specified region of a byte array.</summary>
        /// <param name="buffer">The <see cref="T:System.Byte"/>[] whose hash must be computed.</param>
        /// <param name="offset">The offset into the byte array from which to begin using data.</param>
        /// <param name="count">The number of bytes in the array to use as data.</param>
        /// <returns>A <see cref="T:System.Byte"/>[] representing the computed hash.</returns>
        /// <exception cref="T:System.ArgumentException">Thrown when the number of bytes in <paramref name="buffer">buffer</paramref> is less than <paramref name="offset">sourceOffset</paramref> plus <paramref name="count">count</paramref>.</exception>
        /// <exception cref="T:System.ArgumentNullException">Thrown when <paramref name="buffer">buffer</paramref> is <c>null</c>.</exception>
        /// <exception cref="T:System.ArgumentOutOfRangeException">Thrown when <paramref name="offset">offset</paramref> is not within the bounds of <paramref name="buffer">buffer</paramref> or when <paramref name="count">count</paramref> is less than <c>0</c>.</exception>
        public Byte[] ComputeHash(Byte[] buffer, Int32 offset, Int32 count)
        {
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));

            Int32 bufferLength = buffer.Length;

            if ((offset < 0) || (offset >= bufferLength))
                throw new ArgumentOutOfRangeException(nameof(offset), "The offset parameter must be within the bounds of the data array.");

            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count), "The count parameter must be greater than or equal to 0.");

            if ((offset + count) > bufferLength)
                throw new ArgumentException("The block defined by offset and count parameters must be within the bounds of the data array.");

            ReadOnlySpan<Byte> span = new ReadOnlySpan<Byte>(buffer, offset, count);

            return ComputeHashInternal(span);
        }

        /// <summary>Computes the hash of the specified byte span.</summary>
        /// <param name="buffer">The <see cref="T:System.ReadOnlySpan`1{T}">ReadOnlySpan&lt;byte&gt;</see> whose hash must be computed.</param>
        /// <returns>A <see cref="T:System.Byte"/>[] representing the computed hash.</returns>
        /// <exception cref="T:System.ArgumentNullException">Thrown when <paramref name="buffer">buffer</paramref> is <c>null</c>.</exception>
        public Byte[] ComputeHash(ReadOnlySpan<Byte> buffer)
        {
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));

            return ComputeHashInternal(buffer);
        }

        /// <summary>Returns the text representation of the current instance.</summary>
        /// <returns>A <see cref="T:System.String"/> representing the current instance.</returns>
        [ExcludeFromCodeCoverage]
        public override String ToString()
        {
            return GetType().Name;
        }
        #endregion
    }
}
