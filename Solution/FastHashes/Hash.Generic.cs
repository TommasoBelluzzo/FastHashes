#region Using Directives
using System;
using System.Buffers.Binary;
using System.Runtime.InteropServices;
#endregion

namespace FastHashes
{
    /// <summary>Represents a hashing algorithm that computes a strongly typed hash value. This class is abstract.</summary>
    /// <typeparam name="T">The unmanaged type of the computed hash value.</typeparam>
    public abstract class Hash<T> : Hash where T : unmanaged
    {
        #region Properties
        /// <inheritdoc/>
        public sealed override Int32 Length => HashResult<T>.Length;
        #endregion

        #region Methods
        /// <summary>Computes the strongly typed hash value of the specified byte span.</summary>
        /// <param name="buffer">The <see cref="T:System.ReadOnlySpan`1{T}">ReadOnlySpan&lt;byte&gt;</see> containing the data to hash.</param>
        /// <returns>The computed hash value.</returns>
        protected abstract T ComputeHashValue(ReadOnlySpan<Byte> buffer);

        /// <summary>Computes the strongly typed hash value of the specified byte span.</summary>
        /// <param name="buffer">The <see cref="T:System.ReadOnlySpan`1{T}">ReadOnlySpan&lt;byte&gt;</see> containing the data to hash.</param>
        /// <returns>The computed hash value.</returns>
        public T ComputeValue(ReadOnlySpan<Byte> buffer)
        {
            return ComputeHashValue(buffer);
        }

        /// <inheritdoc/>
        protected sealed override void ComputeHashInternal(ReadOnlySpan<Byte> buffer, Span<Byte> destination)
        {
            HashResult<T>.Write(ComputeHashValue(buffer), destination);
        }
        #endregion
    }

    internal static class HashResult<T> where T : unmanaged
    {
        #region Properties
        internal static readonly Int32 Length = GetLength();
        #endregion

        #region Methods
        private static Int32 GetLength()
        {
            if (typeof(T) == typeof(UInt32))
                return 32;

            if (typeof(T) == typeof(UInt64))
                return 64;

            if (typeof(T) == typeof((UInt64, UInt64)))
                return 128;

            if (typeof(T) == typeof((UInt64, UInt64, UInt64, UInt64)))
                return 256;

            throw new NotSupportedException($"The hash result type {typeof(T)} is not supported.");
        }

        internal static void Write(T value, Span<Byte> destination)
        {
            Span<T> source = stackalloc T[1];
            source[0] = value;

            if (typeof(T) == typeof(UInt32))
            {
                UInt32 hash = MemoryMarshal.Cast<T, UInt32>(source)[0];
                BinaryPrimitives.WriteUInt32LittleEndian(destination, hash);
                return;
            }

            if (typeof(T) == typeof(UInt64))
            {
                UInt64 hash = MemoryMarshal.Cast<T, UInt64>(source)[0];
                BinaryPrimitives.WriteUInt64LittleEndian(destination, hash);
                return;
            }

            if (typeof(T) == typeof((UInt64, UInt64)))
            {
                (UInt64 hash1, UInt64 hash2) = MemoryMarshal.Cast<T, (UInt64, UInt64)>(source)[0];
                BinaryPrimitives.WriteUInt64LittleEndian(destination, hash1);
                BinaryPrimitives.WriteUInt64LittleEndian(destination.Slice(8), hash2);
                return;
            }

            if (typeof(T) == typeof((UInt64, UInt64, UInt64, UInt64)))
            {
                (UInt64 hash1, UInt64 hash2, UInt64 hash3, UInt64 hash4) = MemoryMarshal.Cast<T, (UInt64, UInt64, UInt64, UInt64)>(source)[0];
                BinaryPrimitives.WriteUInt64LittleEndian(destination, hash1);
                BinaryPrimitives.WriteUInt64LittleEndian(destination.Slice(8), hash2);
                BinaryPrimitives.WriteUInt64LittleEndian(destination.Slice(16), hash3);
                BinaryPrimitives.WriteUInt64LittleEndian(destination.Slice(24), hash4);
                return;
            }

            throw new NotSupportedException($"The hash result type {typeof(T)} is not supported.");
        }
        #endregion
    }
}