#region Using Directives
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
#endregion

namespace FastHashes
{
    /// <summary>Represents the base class from which all the hashing algorithms must derive. This class is abstract.</summary>
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

        /// <summary>Attempts to create a new instance of the specified hashing algorithm using its default configuration.</summary>
        /// <param name="name">The <see cref="T:System.String"/> identifying the hashing algorithm.</param>
        /// <param name="hash">The resulting <see cref="T:FastHashes.Hash"/> instance, or <c>null</c> if the hashing algorithm is unknown.</param>
        /// <returns>A <see cref="T:System.Boolean"/> value equal to <c>true</c> if the hashing algorithm was created; <c>false</c> otherwise.</returns>
        /// <exception cref="T:System.ArgumentNullException">Thrown when <paramref name="name">name</paramref> is <c>null</c>.</exception>
        public static Boolean TryCreate(String name, out Hash hash)
        {
            if (!HashRegistry.TryGetInitializers(name, out var initializers))
            {
                hash = null;
                return false;
            }

            hash = initializers.Default();
            return true;
        }

        /// <summary>Attempts to create a new instance of the specified hashing algorithm using a standardized single-seed initialization.</summary>
        /// <param name="name">The <see cref="T:System.String"/> identifying the hashing algorithm.</param>
        /// <param name="seed">The <see cref="T:System.UInt64"/> value mapped to the algorithm-specific seed configuration.</param>
        /// <param name="hash">The resulting <see cref="T:FastHashes.Hash"/> instance, or <c>null</c> if the hashing algorithm is unknown.</param>
        /// <returns>A <see cref="T:System.Boolean"/> value equal to <c>true</c> if the hashing algorithm was created; <c>false</c> otherwise.</returns>
        /// <exception cref="T:System.ArgumentNullException">Thrown when <paramref name="name">name</paramref> is <c>null</c>.</exception>
        /// <exception cref="T:System.OverflowException">Thrown when <paramref name="seed">seed</paramref> cannot be represented by a hashing algorithm requiring a 32-bit seed.</exception>
        public static Boolean TryCreate(String name, UInt64 seed, out Hash hash)
        {
            if (!HashRegistry.TryGetInitializers(name, out var initializers))
            {
                hash = null;
                return false;
            }

            hash = initializers.Seeded(seed);
            return true;
        }

        /// <summary>Gets the default initializer of the specified hashing algorithm.</summary>
        /// <param name="name">The <see cref="T:System.String"/> identifying the hashing algorithm.</param>
        /// <returns>A <see cref="T:System.Func`1"/> delegate of type <c>Func&lt;Hash&gt;</c> that creates a new hash instance using its default configuration.</returns>
        /// <exception cref="T:System.ArgumentException">Thrown when the specified hashing algorithm is unknown.</exception>
        /// <exception cref="T:System.ArgumentNullException">Thrown when <paramref name="name">name</paramref> is <c>null</c>.</exception>
        public static Func<Hash> GetInitializerDefault(String name)
        {
            if (!HashRegistry.TryGetInitializers(name, out var initializers))
                throw new ArgumentException("Unknown hashing algorithm specified.", nameof(name));

            return initializers.Default;
        }

        /// <summary>Gets the standardized single-seed initializer of the specified hashing algorithm.</summary>
        /// <param name="name">The <see cref="T:System.String"/> identifying the hashing algorithm.</param>
        /// <returns>A <see cref="T:System.Func`2"/> delegate of type <c>Func&lt;UInt64, Hash&gt;</c> that creates a new hash instance using a standardized single-seed initialization.</returns>
        /// <exception cref="T:System.ArgumentException">Thrown when the specified hashing algorithm is unknown.</exception>
        /// <exception cref="T:System.ArgumentNullException">Thrown when <paramref name="name">name</paramref> is <c>null</c>.</exception>
        public static Func<UInt64, Hash> GetInitializerSeeded(String name)
        {
            if (!HashRegistry.TryGetInitializers(name, out var initializers))
                throw new ArgumentException("Unknown hashing algorithm specified.", nameof(name));

            return initializers.Seeded;
        }

        /// <summary>Creates a new instance of the specified hashing algorithm using its default configuration.</summary>
        /// <param name="name">The <see cref="T:System.String"/> identifying the hashing algorithm.</param>
        /// <returns>A <see cref="T:FastHashes.Hash"/> instance.</returns>
        /// <exception cref="T:System.ArgumentException">Thrown when the specified hashing algorithm is unknown.</exception>
        /// <exception cref="T:System.ArgumentNullException">Thrown when <paramref name="name">name</paramref> is <c>null</c>.</exception>
        public static Hash Create(String name)
        {
            if (!HashRegistry.TryGetInitializers(name, out var initializers))
                throw new ArgumentException("Unknown hashing algorithm specified.", nameof(name));

            return initializers.Default();
        }

        /// <summary>Creates a new instance of the specified hashing algorithm using a standardized single-seed initialization.</summary>
        /// <param name="name">The <see cref="T:System.String"/> identifying the hashing algorithm.</param>
        /// <param name="seed">The <see cref="T:System.UInt64"/> value mapped to the algorithm-specific seed configuration.</param>
        /// <returns>A <see cref="T:FastHashes.Hash"/> instance.</returns>
        /// <exception cref="T:System.ArgumentException">Thrown when the specified hashing algorithm is unknown.</exception>
        /// <exception cref="T:System.ArgumentNullException">Thrown when <paramref name="name">name</paramref> is <c>null</c>.</exception>
        /// <exception cref="T:System.OverflowException">Thrown when <paramref name="seed">seed</paramref> cannot be represented by a hashing algorithm requiring a 32-bit seed.</exception>
        public static Hash Create(String name, UInt64 seed)
        {
            if (!HashRegistry.TryGetInitializers(name, out var initializers))
                throw new ArgumentException("Unknown hashing algorithm specified.", nameof(name));

            return initializers.Seeded(seed);
        }

        /// <summary>Gets the identifiers of all registered hashing algorithms and their supported variants.</summary>
        /// <returns>A <see cref="T:System.Collections.Generic.IReadOnlyList`1{T}">IReadOnlyList&lt;string&gt;</see> containing all the algorithm identifiers.</returns>
        public static IReadOnlyList<String> GetAlgorithms()
        {
            return HashRegistry.GetIdentifiers();
        }

        /// <summary>Computes the hash of the specified byte span and writes it into the destination byte span.</summary>
        /// <param name="buffer">The <see cref="T:System.ReadOnlySpan`1{T}">ReadOnlySpan&lt;byte&gt;</see> containing the data to hash.</param>
        /// <param name="destination">The <see cref="T:System.Span`1{T}">Span&lt;byte&gt;</see> receiving the computed hash.</param>
        protected abstract void ComputeHashInternal(ReadOnlySpan<Byte> buffer, Span<Byte> destination);

        /// <summary>Computes the hash of the specified byte array.</summary>
        /// <param name="buffer">The <see cref="T:System.Byte"/>[] containing the data to hash.</param>
        /// <returns>A <see cref="T:System.Byte"/>[] containing the computed hash.</returns>
        /// <exception cref="T:System.ArgumentNullException">Thrown when <paramref name="buffer">buffer</paramref> is <c>null</c>.</exception>
        public Byte[] ComputeHash(Byte[] buffer)
        {
            return ComputeHash(GetSpan(buffer, 0, buffer?.Length ?? 0));
        }

        /// <summary>Computes the hash of the first <paramref name="count">count</paramref> bytes of the specified array.</summary>
        /// <param name="buffer">The <see cref="T:System.Byte"/>[] containing the data to hash.</param>
        /// <param name="count">The number of bytes to hash.</param>
        /// <returns>A <see cref="T:System.Byte"/>[] containing the computed hash.</returns>
        /// <exception cref="T:System.ArgumentException">Thrown when <paramref name="count">count</paramref> exceeds the length of <paramref name="buffer">buffer</paramref>.</exception>
        /// <exception cref="T:System.ArgumentNullException">Thrown when <paramref name="buffer">buffer</paramref> is <c>null</c>.</exception>
        /// <exception cref="T:System.ArgumentOutOfRangeException">Thrown when <paramref name="count">count</paramref> is negative.</exception>
        public Byte[] ComputeHash(Byte[] buffer, Int32 count)
        {
            return ComputeHash(GetSpan(buffer, 0, count));
        }

        /// <summary>Computes the hash of the specified region of a byte array.</summary>
        /// <param name="buffer">The <see cref="T:System.Byte"/>[] containing the data to hash.</param>
        /// <param name="offset">The zero-based starting index in <paramref name="buffer">buffer</paramref>.</param>
        /// <param name="count">The number of bytes to hash.</param>
        /// <returns>A <see cref="T:System.Byte"/>[] containing the computed hash.</returns>
        /// <exception cref="T:System.ArgumentException">Thrown when the region defined by <paramref name="offset">offset</paramref> and <paramref name="count">count</paramref> exceeds the bounds of <paramref name="buffer">buffer</paramref>.</exception>
        /// <exception cref="T:System.ArgumentNullException">Thrown when <paramref name="buffer">buffer</paramref> is <c>null</c>.</exception>
        /// <exception cref="T:System.ArgumentOutOfRangeException">Thrown when <paramref name="offset">offset</paramref> is outside the valid range or <paramref name="count">count</paramref> is negative.</exception>
        public Byte[] ComputeHash(Byte[] buffer, Int32 offset, Int32 count)
        {
            return ComputeHash(GetSpan(buffer, offset, count));
        }

        /// <summary>Computes the hash of the specified byte span.</summary>
        /// <param name="buffer">The <see cref="T:System.ReadOnlySpan`1{T}">ReadOnlySpan&lt;byte&gt;</see> containing the data to hash.</param>
        /// <returns>A <see cref="T:System.Byte"/>[] containing the computed hash.</returns>
        public Byte[] ComputeHash(ReadOnlySpan<Byte> buffer)
        {
            Byte[] result = new Byte[Length / 8];
            ComputeHashInternal(buffer, result);

            return result;
        }

        /// <summary>Computes the hash of the specified byte span and writes it into the destination byte span.</summary>
        /// <param name="buffer">The <see cref="T:System.ReadOnlySpan`1{T}">ReadOnlySpan&lt;byte&gt;</see> containing the data to hash.</param>
        /// <param name="destination">The <see cref="T:System.Span`1{T}">Span&lt;byte&gt;</see> receiving the computed hash.</param>
        /// <exception cref="T:System.ArgumentException">Thrown when the destination is too small to contain the computed hash.</exception>
        public void ComputeHash(ReadOnlySpan<Byte> buffer, Span<Byte> destination)
        {
            Int32 length = Length / 8;

            if (destination.Length < length)
                throw new ArgumentException("The destination buffer is too small.", nameof(destination));

            ComputeHashInternal(buffer, destination.Slice(0, length));
        }

        /// <summary>Computes the hash of the specified text using UTF-8 encoding.</summary>
        /// <param name="text">The <see cref="T:System.String"/> containing the text to hash.</param>
        /// <returns>A <see cref="T:System.Byte"/>[] containing the computed hash.</returns>
        /// <exception cref="T:System.ArgumentNullException">Thrown when <paramref name="text">text</paramref> is <c>null</c>.</exception>
        public Byte[] ComputeHash(String text)
        {
            return ComputeHash(text, Encoding.UTF8);
        }

        /// <summary>Computes the hash of the specified text using the specified encoding.</summary>
        /// <param name="text">The <see cref="T:System.String"/> containing the text to hash.</param>
        /// <param name="encoding">The <see cref="T:System.Text.Encoding"/> used to encode the text.</param>
        /// <returns>A <see cref="T:System.Byte"/>[] containing the computed hash.</returns>
        /// <exception cref="T:System.ArgumentNullException">Thrown when <paramref name="text">text</paramref> or <paramref name="encoding">encoding</paramref> is <c>null</c>.</exception>
        public Byte[] ComputeHash(String text, Encoding encoding)
        {
            if (text == null)
                throw new ArgumentNullException(nameof(text));

            if (encoding == null)
                throw new ArgumentNullException(nameof(encoding));

            return ComputeHash(encoding.GetBytes(text).AsSpan());
        }

        /// <summary>Computes the hash of the specified byte array and returns it in the specified format.</summary>
        /// <param name="buffer">The <see cref="T:System.Byte"/>[] containing the data to hash.</param>
        /// <param name="format">The <see cref="T:FastHashes.HashFormat"/> specifying the output format.</param>
        /// <returns>A <see cref="T:System.String"/> containing the formatted hash.</returns>
        /// <exception cref="T:System.ArgumentNullException">Thrown when <paramref name="buffer">buffer</paramref> is <c>null</c>.</exception>
        /// <exception cref="T:System.ArgumentOutOfRangeException">Thrown when <paramref name="format">format</paramref> is not a defined enumeration value.</exception>
        public String ComputeHash(Byte[] buffer, HashFormat format)
        {
            return ComputeHash(GetSpan(buffer, 0, buffer?.Length ?? 0), format);
        }

        /// <summary>Computes the hash of the first <paramref name="count">count</paramref> bytes of the specified array and returns it in the specified format.</summary>
        /// <param name="buffer">The <see cref="T:System.Byte"/>[] containing the data to hash.</param>
        /// <param name="count">The number of bytes to hash.</param>
        /// <param name="format">The <see cref="T:FastHashes.HashFormat"/> specifying the output format.</param>
        /// <returns>A <see cref="T:System.String"/> containing the formatted hash.</returns>
        /// <exception cref="T:System.ArgumentException">Thrown when <paramref name="count">count</paramref> exceeds the length of <paramref name="buffer">buffer</paramref>.</exception>
        /// <exception cref="T:System.ArgumentNullException">Thrown when <paramref name="buffer">buffer</paramref> is <c>null</c>.</exception>
        /// <exception cref="T:System.ArgumentOutOfRangeException">Thrown when <paramref name="count">count</paramref> is negative or <paramref name="format">format</paramref> is not a defined enumeration value.</exception>
        public String ComputeHash(Byte[] buffer, Int32 count, HashFormat format)
        {
            return ComputeHash(GetSpan(buffer, 0, count), format);
        }

        /// <summary>Computes the hash of the specified region of a byte array and returns it in the specified format.</summary>
        /// <param name="buffer">The <see cref="T:System.Byte"/>[] containing the data to hash.</param>
        /// <param name="offset">The zero-based starting index in <paramref name="buffer">buffer</paramref>.</param>
        /// <param name="count">The number of bytes to hash.</param>
        /// <param name="format">The <see cref="T:FastHashes.HashFormat"/> specifying the output format.</param>
        /// <returns>A <see cref="T:System.String"/> containing the formatted hash.</returns>
        /// <exception cref="T:System.ArgumentException">Thrown when the region defined by <paramref name="offset">offset</paramref> and <paramref name="count">count</paramref> exceeds the bounds of <paramref name="buffer">buffer</paramref>.</exception>
        /// <exception cref="T:System.ArgumentNullException">Thrown when <paramref name="buffer">buffer</paramref> is <c>null</c>.</exception>
        /// <exception cref="T:System.ArgumentOutOfRangeException">Thrown when <paramref name="offset">offset</paramref> is outside the valid range, <paramref name="count">count</paramref> is negative, or <paramref name="format">format</paramref> is not a defined enumeration value.</exception>
        public String ComputeHash(Byte[] buffer, Int32 offset, Int32 count, HashFormat format)
        {
            return ComputeHash(GetSpan(buffer, offset, count), format);
        }

        /// <summary>Computes the hash of the specified byte span and returns it in the specified format.</summary>
        /// <param name="buffer">The <see cref="T:System.ReadOnlySpan`1{T}">ReadOnlySpan&lt;byte&gt;</see> containing the data to hash.</param>
        /// <param name="format">The <see cref="T:FastHashes.HashFormat"/> specifying the output format.</param>
        /// <returns>A <see cref="T:System.String"/> containing the formatted hash.</returns>
        /// <exception cref="T:System.ArgumentOutOfRangeException">Thrown when <paramref name="format">format</paramref> is not a defined enumeration value.</exception>
        public String ComputeHash(ReadOnlySpan<Byte> buffer, HashFormat format)
        {
            return HashFormatter.Format(ComputeHash(buffer), format);
        }

        /// <summary>Computes the hash of the specified text using UTF-8 encoding and returns it in the specified format.</summary>
        /// <param name="text">The <see cref="T:System.String"/> containing the text to hash.</param>
        /// <param name="format">The <see cref="T:FastHashes.HashFormat"/> specifying the output format.</param>
        /// <returns>A <see cref="T:System.String"/> containing the formatted hash.</returns>
        /// <exception cref="T:System.ArgumentNullException">Thrown when <paramref name="text">text</paramref> is <c>null</c>.</exception>
        /// <exception cref="T:System.ArgumentOutOfRangeException">Thrown when <paramref name="format">format</paramref> is not a defined enumeration value.</exception>
        public String ComputeHash(String text, HashFormat format)
        {
            return ComputeHash(text, Encoding.UTF8, format);
        }

        /// <summary>Computes the hash of the specified text using the specified encoding and returns it in the specified format.</summary>
        /// <param name="text">The <see cref="T:System.String"/> containing the text to hash.</param>
        /// <param name="encoding">The <see cref="T:System.Text.Encoding"/> used to encode the text.</param>
        /// <param name="format">The <see cref="T:FastHashes.HashFormat"/> specifying the output format.</param>
        /// <returns>A <see cref="T:System.String"/> containing the formatted hash.</returns>
        /// <exception cref="T:System.ArgumentNullException">Thrown when <paramref name="text">text</paramref> or <paramref name="encoding">encoding</paramref> is <c>null</c>.</exception>
        /// <exception cref="T:System.ArgumentOutOfRangeException">Thrown when <paramref name="format">format</paramref> is not a defined enumeration value.</exception>
        public String ComputeHash(String text, Encoding encoding, HashFormat format)
        {
            if (text == null)
                throw new ArgumentNullException(nameof(text));

            if (encoding == null)
                throw new ArgumentNullException(nameof(encoding));

            return HashFormatter.Format(ComputeHash(text, encoding), format);
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