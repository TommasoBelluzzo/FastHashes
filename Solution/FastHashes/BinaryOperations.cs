﻿#region Using Directives
using System;
using System.Buffers.Binary;
using System.Runtime.CompilerServices;
#endregion

namespace FastHashes
{
    /// <summary>Provides static methods for binary operations.</summary>
    public static class BinaryOperations
    {
        #region Members
        private static readonly Boolean s_IsLittleEndian = BitConverter.IsLittleEndian;
        #endregion

        #region Methods
        /// <summary>Converts a 4-bytes unsigned integer to a byte array.</summary>
        /// <param name="value">The <see cref="T:System.UInt32"/> to convert.</param>
        /// <returns>A <see cref="T:System.Byte"/>[] value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Byte[] ToArray32(UInt32 value)
        {
            return BitConverter.GetBytes(value);
        }

        /// <summary>Converts a 8-bytes unsigned integer to a 4-bytes array.</summary>
        /// <param name="value">The <see cref="T:System.UInt64"/> to convert.</param>
        /// <returns>A <see cref="T:System.Byte"/>[] value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Byte[] ToArray32(UInt64 value)
        {
            Byte[] array = new Byte[4];
            Byte[] valueBytes = BitConverter.GetBytes(value);

            Buffer.BlockCopy(valueBytes, 0, array, 0, 4);

            return array;
        }

        /// <summary>Converts an array of a 4-bytes unsigned integers to a byte array.</summary>
        /// <param name="values">The <see cref="T:System.UInt32"/>[] value to convert.</param>
        /// <returns>A <see cref="T:System.Byte"/>[] value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Byte[] ToArray32(params UInt32[] values)
        {
            Int32 length = values.Length;
            Byte[] array = new Byte[4 * length];

            for (Int32 i = 0; i < length; ++i)
            {
                Byte[] valueBytes = BitConverter.GetBytes(values[i]);
                Buffer.BlockCopy(valueBytes, 0, array, i * 4, valueBytes.Length);
            }

            return array;
        }

        /// <summary>Converts a 4-bytes unsigned integer to a 8-bytes array.</summary>
        /// <param name="value">The <see cref="T:System.UInt64"/> to convert.</param>
        /// <returns>A <see cref="T:System.Byte"/>[] value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Byte[] ToArray64(UInt32 value)
        {
            Byte[] array = new Byte[8];
            Byte[] valueBytes = BitConverter.GetBytes(value);

            Buffer.BlockCopy(valueBytes, 0, array, 0, 4);

            return array;
        }

        /// <summary>Converts a 8-bytes unsigned integer to a byte array.</summary>
        /// <param name="value">The <see cref="T:System.UInt64"/> to convert.</param>
        /// <returns>A <see cref="T:System.Byte"/>[] value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Byte[] ToArray64(UInt64 value)
        {
            return BitConverter.GetBytes(value);
        }

        /// <summary>Converts an array of a 8-bytes unsigned integers to a byte array.</summary>
        /// <param name="values">The <see cref="T:System.UInt64"/>[] value to convert.</param>
        /// <returns>A <see cref="T:System.Byte"/>[] value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Byte[] ToArray64(params UInt64[] values)
        {
            Int32 length = values.Length;
            Byte[] array = new Byte[8 * length];

            for (Int32 i = 0; i < length; ++i)
            {
                Byte[] valueBytes = BitConverter.GetBytes(values[i]);
                Buffer.BlockCopy(valueBytes, 0, array, i * 8, valueBytes.Length);
            }

            return array;
        }

        /// <summary>Reads a 2-bytes unsigned integer from the specified byte span.</summary>
        /// <param name="buffer">The <see cref="T:System.ReadOnlySpan`1{T}">ReadOnlySpan&lt;byte&gt;</see> to read.</param>
        /// <param name="offset">The buffer start offset.</param>
        /// <returns>An <see cref="T:System.UInt16"/> value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UInt16 Read16(ReadOnlySpan<Byte> buffer, Int32 offset)
        {
            UInt16 v;

            if (s_IsLittleEndian)
                v = BinaryPrimitives.ReadUInt16LittleEndian(buffer.Slice(offset, 2));
            else
                v = BinaryPrimitives.ReadUInt16BigEndian(buffer.Slice(offset, 2));

            return v;
        }

        /// <summary>Reads a 4-bytes unsigned integer from the specified byte span.</summary>
        /// <param name="buffer">The <see cref="T:System.ReadOnlySpan`1{T}">ReadOnlySpan&lt;byte&gt;</see> to read.</param>
        /// <param name="offset">The buffer start offset.</param>
        /// <returns>An <see cref="T:System.UInt32"/> value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UInt32 Read32(ReadOnlySpan<Byte> buffer, Int32 offset)
        {
            UInt32 v;

            if (s_IsLittleEndian)
                v = BinaryPrimitives.ReadUInt32LittleEndian(buffer.Slice(offset, 4));
            else
                v = BinaryPrimitives.ReadUInt32BigEndian(buffer.Slice(offset, 4));

            return v;
        }

        /// <summary>Reads a 8-bytes unsigned integer from the specified byte span.</summary>
        /// <param name="buffer">The <see cref="T:System.ReadOnlySpan`1{T}">ReadOnlySpan&lt;byte&gt;</see> to read.</param>
        /// <param name="offset">The span start offset.</param>
        /// <returns>An <see cref="T:System.UInt64"/> value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UInt64 Read64(ReadOnlySpan<Byte> buffer, Int32 offset)
        {
            UInt64 v;

            if (s_IsLittleEndian)
                v = BinaryPrimitives.ReadUInt64LittleEndian(buffer.Slice(offset, 8));
            else
                v = BinaryPrimitives.ReadUInt64BigEndian(buffer.Slice(offset, 8));

            return v;
        }

        /// <summary>Reads an array of 2-bytes unsigned integers from the specified byte pointer.</summary>
        /// <param name="buffer">The <see cref="T:System.ReadOnlySpan`1{T}">ReadOnlySpan&lt;byte&gt;</see> to read.</param>
        /// <param name="offset">The span start offset.</param>
        /// <param name="count">The number of values to read.</param>
        /// <returns>An array of <see cref="T:System.UInt16"/> values.</returns>
        public static UInt16[] ReadArray16(ReadOnlySpan<Byte> buffer, Int32 offset, Int32 count)
        {
            UInt16[] array = new UInt16[count];

            for (Int32 i = 0; i < count; ++i)
                array[i] = Read16(buffer, offset + (i * 2));

            return array;
        }

        /// <summary>Reads an array of 4-bytes unsigned integers from the specified byte span.</summary>
        /// <param name="buffer">The <see cref="T:System.ReadOnlySpan`1{T}">ReadOnlySpan&lt;byte&gt;</see> to read.</param>
        /// <param name="offset">The span start offset.</param>
        /// <param name="count">The number of values to read.</param>
        /// <returns>An array of <see cref="T:System.UInt32"/> values.</returns>
        public static UInt32[] ReadArray32(ReadOnlySpan<Byte> buffer, Int32 offset, Int32 count)
        {
            UInt32[] array = new UInt32[count];

            for (Int32 i = 0; i < count; ++i)
                array[i] = Read32(buffer, offset + (i * 4));

            return array;
        }

        /// <summary>Reads an array of 8-bytes unsigned integers from the specified byte span.</summary>
        /// <param name="buffer">The <see cref="T:System.ReadOnlySpan`1{T}">ReadOnlySpan&lt;byte&gt;</see> to read.</param>
        /// <param name="offset">The span start offset.</param>
        /// <param name="count">The number of values to read.</param>
        /// <returns>An array of <see cref="T:System.UInt64"/> values.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UInt64[] ReadArray64(ReadOnlySpan<Byte> buffer, Int32 offset, Int32 count)
        {
            UInt64[] array = new UInt64[count];

            for (Int32 i = 0; i < count; ++i)
                array[i] = Read64(buffer, offset + (i * 8));

            return array;
        }

        /// <summary>Rotates a 2-bytes unsigned integer left by the specified number of bits.</summary>
        /// <param name="value">The <see cref="T:System.UInt16"/> to rotate.</param>
        /// <param name="rotation">The number of bits to rotate.</param>
        /// <returns>An <see cref="T:System.UInt16"/> value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UInt16 RotateLeft(UInt16 value, Int32 rotation)
        {
            rotation &= 0x0F;
            return (UInt16)((value << rotation) | (value >> (16 - rotation)));
        }

        /// <summary>Rotates a 4-bytes unsigned integer left by the specified number of bits.</summary>
        /// <param name="value">The <see cref="T:System.UInt32"/> to rotate.</param>
        /// <param name="rotation">The number of bits to rotate.</param>
        /// <returns>An <see cref="T:System.UInt32"/> value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UInt32 RotateLeft(UInt32 value, Int32 rotation)
        {
            rotation &= 0x1F;
            return (value << rotation) | (value >> (32 - rotation));
        }

        /// <summary>Rotates a 8-bytes unsigned integer left by the specified number of bits.</summary>
        /// <param name="value">The <see cref="T:System.UInt64"/> to rotate.</param>
        /// <param name="rotation">The number of bits to rotate.</param>
        /// <returns>An <see cref="T:System.UInt64"/> value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UInt64 RotateLeft(UInt64 value, Int32 rotation)
        {
            rotation &= 0x3F;
            return (value << rotation) | (value >> (64 - rotation));
        }

        /// <summary>Rotates a 2-bytes unsigned integer right by the specified number of bits.</summary>
        /// <param name="value">The <see cref="T:System.UInt16"/> to rotate.</param>
        /// <param name="rotation">The number of bits to rotate.</param>
        /// <returns>An <see cref="T:System.UInt16"/> value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UInt16 RotateRight(UInt16 value, Int32 rotation)
        {
            rotation &= 0x0F;
            return (UInt16)((value >> rotation) | (value << (16 - rotation)));
        }

        /// <summary>Rotates a 4-bytes unsigned integer right by the specified number of bits.</summary>
        /// <param name="value">The <see cref="T:System.UInt32"/> to rotate.</param>
        /// <param name="rotation">The number of bits to rotate.</param>
        /// <returns>An <see cref="T:System.UInt32"/> value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UInt32 RotateRight(UInt32 value, Int32 rotation)
        {
            rotation &= 0x1F;
            return (value >> rotation) | (value << (32 - rotation));
        }

        /// <summary>Rotates a 8-bytes unsigned integer right by the specified number of bits.</summary>
        /// <param name="value">The <see cref="T:System.UInt64"/> to rotate.</param>
        /// <param name="rotation">The number of bits to rotate.</param>
        /// <returns>An <see cref="T:System.UInt64"/> value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UInt64 RotateRight(UInt64 value, Int32 rotation)
        {
            rotation &= 0x3F;
            return (value >> rotation) | (value << (64 - rotation));
        }

        /// <summary>Swaps the value of two 2-bytes unsigned integers.</summary>
        /// <param name="value1">The first <see cref="T:System.UInt16"/>, whose value is assigned to the second one.</param>
        /// <param name="value2">The second <see cref="T:System.UInt16"/>, whose value is assigned to the first one.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Swap(ref UInt16 value1, ref UInt16 value2)
        {
            UInt16 tmp = value1;
            value1 = value2;
            value2 = tmp;
        }

        /// <summary>Swaps the value of two 4-bytes unsigned integers.</summary>
        /// <param name="value1">The first <see cref="T:System.UInt32"/>, whose value is assigned to the second one.</param>
        /// <param name="value2">The second <see cref="T:System.UInt32"/>, whose value is assigned to the first one.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Swap(ref UInt32 value1, ref UInt32 value2)
        {
            UInt32 tmp = value1;
            value1 = value2;
            value2 = tmp;
        }

        /// <summary>Swaps the value of two 8-bytes unsigned integers.</summary>
        /// <param name="value1">The first <see cref="T:System.UInt64"/>, whose value is assigned to the second one.</param>
        /// <param name="value2">The second <see cref="T:System.UInt64"/>, whose value is assigned to the first one.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Swap(ref UInt64 value1, ref UInt64 value2)
        {
            UInt64 tmp = value1;
            value1 = value2;
            value2 = tmp;
        }
        #endregion
    }
}
