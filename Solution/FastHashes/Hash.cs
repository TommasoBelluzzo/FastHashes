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
        private static ReadOnlySpan<Byte> GetSpan(Byte[] buffer, Int32 offset, Int32 count)
        {
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));

            Int32 bufferLength = buffer.Length;

            if ((offset < 0) || (offset > bufferLength))
                throw new ArgumentOutOfRangeException(nameof(offset), "The offset parameter must be within the bounds of the data array.");

            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count), "The count parameter must be greater than or equal to 0.");

            if (count > (bufferLength - offset))
                throw new ArgumentException("The block defined by offset and count parameters must be within the bounds of the data array.");

            return new ReadOnlySpan<Byte>(buffer, offset, count);
        }

        /// <summary>Represents the core hashing function of the algorithm.</summary>
        /// <param name="buffer">The <see cref="T:System.ReadOnlySpan`1{T}">ReadOnlySpan&lt;byte&gt;</see> whose hash must be computed.</param>
        /// <returns>A <see cref="T:System.Byte"/>[] representing the computed hash.</returns>
        protected abstract Byte[] ComputeHashInternal(ReadOnlySpan<Byte> buffer);

        /// <summary>Computes the hash of the specified byte array and returns its <see cref="T:System.Byte"/>[] representation.</summary>
        /// <param name="buffer">The <see cref="T:System.Byte"/>[] whose hash must be computed.</param>
        /// <returns>A <see cref="T:System.Byte"/>[] representing the computed hash.</returns>
        /// <exception cref="T:System.ArgumentNullException">Thrown when <paramref name="buffer">buffer</paramref> is <c>null</c>.</exception>
        public Byte[] ComputeHash(Byte[] buffer)
        {
            return ComputeHash(GetSpan(buffer, 0, buffer?.Length ?? 0));
        }

        /// <summary>Computes the hash of the specified number of elements of a byte array, starting at the first element, and returns its <see cref="T:System.Byte"/>[] representation.</summary>
        /// <param name="buffer">The <see cref="T:System.Byte"/>[] whose hash must be computed.</param>
        /// <param name="count">The number of bytes in the array to use as data.</param>
        /// <returns>A <see cref="T:System.Byte"/>[] representing the computed hash.</returns>
        /// <exception cref="T:System.ArgumentException">Thrown when the number of bytes in <paramref name="buffer">buffer</paramref> is less than <paramref name="count">count</paramref>.</exception>
        /// <exception cref="T:System.ArgumentNullException">Thrown when <paramref name="buffer">buffer</paramref> is <c>null</c>.</exception>
        /// <exception cref="T:System.ArgumentOutOfRangeException">Thrown when <paramref name="count">count</paramref> is less than <c>0</c>.</exception>
        public Byte[] ComputeHash(Byte[] buffer, Int32 count)
        {
            return ComputeHash(GetSpan(buffer, 0, count));
        }

        /// <summary>Computes the hash of the specified region of a byte array and returns its <see cref="T:System.Byte"/>[] representation.</summary>
        /// <param name="buffer">The <see cref="T:System.Byte"/>[] whose hash must be computed.</param>
        /// <param name="offset">The offset into the byte array from which to begin using data.</param>
        /// <param name="count">The number of bytes in the array to use as data.</param>
        /// <returns>A <see cref="T:System.Byte"/>[] representing the computed hash.</returns>
        /// <exception cref="T:System.ArgumentException">Thrown when the region defined by <paramref name="offset">offset</paramref> and <paramref name="count">count</paramref> exceeds the bounds of <paramref name="buffer">buffer</paramref>.</exception>
        /// <exception cref="T:System.ArgumentNullException">Thrown when <paramref name="buffer">buffer</paramref> is <c>null</c>.</exception>
        /// <exception cref="T:System.ArgumentOutOfRangeException">Thrown when <paramref name="offset">offset</paramref> is not within the bounds of <paramref name="buffer">buffer</paramref> or when <paramref name="count">count</paramref> is less than <c>0</c>.</exception>
        public Byte[] ComputeHash(Byte[] buffer, Int32 offset, Int32 count)
        {
            return ComputeHash(GetSpan(buffer, offset, count));
        }

        /// <summary>Computes the hash of the specified byte span and returns its <see cref="T:System.Byte"/>[] representation.</summary>
        /// <param name="buffer">The <see cref="T:System.ReadOnlySpan`1{T}">ReadOnlySpan&lt;byte&gt;</see> whose hash must be computed.</param>
        /// <returns>A <see cref="T:System.Byte"/>[] representing the computed hash.</returns>
        public Byte[] ComputeHash(ReadOnlySpan<Byte> buffer)
        {
            return ComputeHashInternal(buffer);
        }

        /// <summary>Computes the hash of the specified byte array and returns its <see cref="T:System.String"/> representation.</summary>
        /// <param name="buffer">The <see cref="T:System.Byte"/>[] whose hash must be computed.</param>
        /// <param name="format">The <see cref="T:FastHashes.HashFormat"/> value specifying the format of the resulting hash.</param>
        /// <returns>A <see cref="T:System.String"/> representing the computed hash.</returns>
        /// <exception cref="T:System.ArgumentNullException">Thrown when <paramref name="buffer">buffer</paramref> is <c>null</c>.</exception>
        /// <exception cref="T:System.ArgumentOutOfRangeException">Thrown when <paramref name="format">format</paramref> is not a defined enumeration value.</exception>
        public String ComputeHash(Byte[] buffer, HashFormat format)
        {
            return ComputeHash(GetSpan(buffer, 0, buffer?.Length ?? 0), format);
        }

        /// <summary>Computes the hash of the specified number of elements of a byte array, starting at the first element, and returns its <see cref="T:System.String"/> representation.</summary>
        /// <param name="buffer">The <see cref="T:System.Byte"/>[] whose hash must be computed.</param>
        /// <param name="count">The number of bytes in the array to use as data.</param>
        /// <param name="format">The <see cref="T:FastHashes.HashFormat"/> value specifying the format of the resulting hash.</param>
        /// <returns>A <see cref="T:System.String"/> representing the computed hash.</returns>
        /// <exception cref="T:System.ArgumentException">Thrown when the number of bytes in <paramref name="buffer">buffer</paramref> is less than <paramref name="count">count</paramref>.</exception>
        /// <exception cref="T:System.ArgumentNullException">Thrown when <paramref name="buffer">buffer</paramref> is <c>null</c>.</exception>
        /// <exception cref="T:System.ArgumentOutOfRangeException">Thrown when <paramref name="count">count</paramref> is less than <c>0</c> or when <paramref name="format">format</paramref> is not a defined enumeration value.</exception>
        public String ComputeHash(Byte[] buffer, Int32 count, HashFormat format)
        {
            return ComputeHash(GetSpan(buffer, 0, count), format);
        }

        /// <summary>Computes the hash of the specified region of a byte array and returns its <see cref="T:System.String"/> representation.</summary>
        /// <param name="buffer">The <see cref="T:System.Byte"/>[] whose hash must be computed.</param>
        /// <param name="offset">The offset into the byte array from which to begin using data.</param>
        /// <param name="count">The number of bytes in the array to use as data.</param>
        /// <param name="format">The <see cref="T:FastHashes.HashFormat"/> value specifying the format of the resulting hash.</param>
        /// <returns>A <see cref="T:System.String"/> representing the computed hash.</returns>
        /// <exception cref="T:System.ArgumentException">Thrown when the region defined by <paramref name="offset">offset</paramref> and <paramref name="count">count</paramref> exceeds the bounds of <paramref name="buffer">buffer</paramref>.</exception>
        /// <exception cref="T:System.ArgumentNullException">Thrown when <paramref name="buffer">buffer</paramref> is <c>null</c>.</exception>
        /// <exception cref="T:System.ArgumentOutOfRangeException">Thrown when <paramref name="offset">offset</paramref> is not within the bounds of <paramref name="buffer">buffer</paramref>, when <paramref name="count">count</paramref> is less than <c>0</c>, or when <paramref name="format">format</paramref> is not a defined enumeration value.</exception>
        public String ComputeHash(Byte[] buffer, Int32 offset, Int32 count, HashFormat format)
        {
            return ComputeHash(GetSpan(buffer, offset, count), format);
        }

        /// <summary>Computes the hash of the specified byte span and returns its <see cref="T:System.String"/> representation.</summary>
        /// <param name="buffer">The <see cref="T:System.ReadOnlySpan`1{T}">ReadOnlySpan&lt;byte&gt;</see> whose hash must be computed.</param>
        /// <param name="format">The <see cref="T:FastHashes.HashFormat"/> value specifying the format of the resulting hash.</param>
        /// <returns>A <see cref="T:System.String"/> representing the computed hash.</returns>
        /// <exception cref="T:System.ArgumentOutOfRangeException">Thrown when <paramref name="format">format</paramref> is not a defined enumeration value.</exception>
        public String ComputeHash(ReadOnlySpan<Byte> buffer, HashFormat format)
        {
            return HashFormatter.Format(ComputeHashInternal(buffer), format);
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