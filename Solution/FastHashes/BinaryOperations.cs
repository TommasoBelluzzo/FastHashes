#region Using Directives
using System;
using System.Runtime.CompilerServices;

#if NETSTANDARD2_1_OR_GREATER
using System.Buffers.Binary;
#endif
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

        #region Pointer/Span Fork
		#if NETSTANDARD2_1_OR_GREATER
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
		#else
        /// <summary>Converts a 4-bytes unsigned integer to a byte array.</summary>
        /// <param name="value">The <see cref="T:System.UInt32"/> to convert.</param>
        /// <returns>A <see cref="T:System.Byte"/>[] value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe Byte[] ToArray32(UInt32 value)
        {
            Byte[] array = new Byte[4];

            fixed (Byte* pointer = array)
                *((UInt32*)pointer) = value;

            return array;
        }

        /// <summary>Converts a 8-bytes unsigned integer to a 4-bytes array.</summary>
        /// <param name="value">The <see cref="T:System.UInt64"/> to convert.</param>
        /// <returns>A <see cref="T:System.Byte"/>[] value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe Byte[] ToArray32(UInt64 value)
        {
            Byte[] array = new Byte[4];

            fixed (Byte* pointer = array)
                *((UInt32*)pointer) = (UInt32)value;

            return array;
        }

        /// <summary>Converts an array of a 4-bytes unsigned integers to a byte array.</summary>
        /// <param name="values">The <see cref="T:System.UInt32"/>[] value to convert.</param>
        /// <returns>A <see cref="T:System.Byte"/>[] value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe Byte[] ToArray32(params UInt32[] values)
        {
            Int32 length = values.Length;
            Byte[] array = new Byte[4 * length];

            fixed (Byte* pin = array)
            {
                UInt32* pointer = (UInt32*)pin;

                for (Int32 i = 0; i < length; ++i)
                    pointer[i] = values[i];
            }

            return array;
        }

        /// <summary>Converts a 4-bytes unsigned integer to a 8-bytes array.</summary>
        /// <param name="value">The <see cref="T:System.UInt64"/> to convert.</param>
        /// <returns>A <see cref="T:System.Byte"/>[] value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe Byte[] ToArray64(UInt32 value)
        {
            Byte[] array = new Byte[8];

            fixed (Byte* pointer = array)
                *((UInt64*)pointer) = value;

            return array;
        }

        /// <summary>Converts a 8-bytes unsigned integer to a byte array.</summary>
        /// <param name="value">The <see cref="T:System.UInt64"/> to convert.</param>
        /// <returns>A <see cref="T:System.Byte"/>[] value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe Byte[] ToArray64(UInt64 value)
        {
            Byte[] array = new Byte[8];

            fixed (Byte* pointer = array)
                *((UInt64*)pointer) = value;

            return array;
        }

        /// <summary>Converts an array of a 8-bytes unsigned integers to a byte array.</summary>
        /// <param name="values">The <see cref="T:System.UInt64"/>[] value to convert.</param>
        /// <returns>A <see cref="T:System.Byte"/>[] value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe Byte[] ToArray64(params UInt64[] values)
        {
            Int32 length = values.Length;
            Byte[] array = new Byte[8 * length];

            fixed (Byte* pin = array)
            {
                UInt64* pointer = (UInt64*)pin;

                for (Int32 i = 0; i < length; ++i)
                    pointer[i] = values[i];
            }

            return array;
        }

        /// <summary>Reads a 2-bytes unsigned integer from the specified byte pointer.</summary>
        /// <param name="pointer">The <see cref="T:System.Byte"/>* to read.</param>
        /// <returns>An <see cref="T:System.UInt16"/> value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe UInt16 Read16(Byte* pointer)
        {
            UInt16 v;

            if (s_IsLittleEndian)
                v = *((UInt16*)pointer);
            else
                v = (UInt16)((pointer[0] << 8) | pointer[1]);

            return v;
        }

        /// <summary>Reads a 4-bytes unsigned integer from the specified byte pointer.</summary>
        /// <param name="pointer">The <see cref="T:System.Byte"/>* to read.</param>
        /// <returns>An <see cref="T:System.UInt32"/> value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe UInt32 Read32(Byte* pointer)
        {
            UInt32 v;

            if (s_IsLittleEndian)
                v = *((UInt32*)pointer);
            else
                v = ((UInt32)pointer[0] << 24) | ((UInt32)pointer[1] << 16) | ((UInt32)pointer[2] << 8) | pointer[3];

            return v;
        }

        /// <summary>Reads a 8-bytes unsigned integer from the specified byte pointer.</summary>
        /// <param name="pointer">The <see cref="T:System.Byte"/>* to read.</param>
        /// <returns>An <see cref="T:System.UInt64"/> value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe UInt64 Read64(Byte* pointer)
        {
            UInt64 v;

            if (s_IsLittleEndian)
                v = *((UInt64*)pointer);
            else
                v = ((UInt64)pointer[0] << 56) | ((UInt64)pointer[1] << 48) | ((UInt64)pointer[2] << 40) | ((UInt64)pointer[3] << 32) | ((UInt64)pointer[4] << 24) | ((UInt64)pointer[5] << 16) | ((UInt64)pointer[6] << 8) | pointer[7];

            return v;
        }

        /// <summary>Reads an array of 2-bytes unsigned integers from the specified byte pointer.</summary>
        /// <param name="pointer">The <see cref="T:System.Byte"/>* to read.</param>
        /// <param name="count">The number of values to read.</param>
        /// <returns>An array of <see cref="T:System.UInt16"/> values.</returns>
        public static unsafe UInt16[] ReadArray16(Byte* pointer, Int32 count)
        {
            UInt16[] array = new UInt16[count];

            for (Int32 i = 0; i < count; ++i)
                array[i] = Read16(pointer + (i * 2));
            
            return array;
        }

        /// <summary>Reads an array of 4-bytes unsigned integers from the specified byte pointer.</summary>
        /// <param name="pointer">The <see cref="T:System.Byte"/>* to read.</param>
        /// <param name="count">The number of values to read.</param>
        /// <returns>An array of <see cref="T:System.UInt32"/> values.</returns>
        public static unsafe UInt32[] ReadArray32(Byte* pointer, Int32 count)
        {
            UInt32[] array = new UInt32[count];

            for (Int32 i = 0; i < count; ++i)
                array[i] = Read32(pointer + (i * 4));

            return array;
        }

        /// <summary>Reads an array of 8-bytes unsigned integers from the specified byte pointer.</summary>
        /// <param name="pointer">The <see cref="T:System.Byte"/>* to read.</param>
        /// <param name="count">The number of values to read.</param>
        /// <returns>An array of <see cref="T:System.UInt64"/> values.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe UInt64[] ReadArray64(Byte* pointer, Int32 count)
        {
            UInt64[] array = new UInt64[count];

            for (Int32 i = 0; i < count; ++i)
                array[i] = Read64(pointer + (i * 8));

            return array;
        }

        /// <summary>Copies the specified region of a source array into the specified region of a destination array.</summary>
        /// <param name="source">The source <see cref="T:System.Byte"/>[].</param>
        /// <param name="sourceOffset">The zero-based offset into <paramref name="source">source</paramref>.</param>
        /// <param name="destination">The destination <see cref="T:System.Byte"/>[].</param>
        /// <param name="destinationOffset">The zero-based offset into <paramref name="destination">destination</paramref>.</param>
        /// <param name="count">The number of bytes to copy.</param>
        /// <exception cref="T:System.ArgumentException">Thrown when the number of bytes in <paramref name="source">source</paramref> is less than <paramref name="sourceOffset">sourceOffset</paramref> plus <paramref name="count">count</paramref> or when the number of bytes in <paramref name="destination">destination</paramref> is less than <paramref name="destinationOffset">destinationOffset</paramref> plus <paramref name="count">count</paramref>.</exception>
        /// <exception cref="T:System.ArgumentNullException">Thrown when <paramref name="source">source</paramref> and <paramref name="destination">destination</paramref> are <c>null</c>.</exception>
        /// <exception cref="T:System.ArgumentOutOfRangeException">Thrown when <paramref name="sourceOffset">sourceOffset</paramref> is not within the bounds of <paramref name="source">source</paramref>, when <paramref name="destinationOffset">destinationOffset</paramref> is not within the bounds of <paramref name="destination">destination</paramref> or when <paramref name="count">count</paramref> is less than <c>0</c>.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void BlockCopy(Byte[] source, Int32 sourceOffset, Byte[] destination, Int32 destinationOffset, Int32 count)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            if (destination == null)
                throw new ArgumentNullException(nameof(destination));

            Int32 sourceLength = source.Length;
            Int32 destinationLength = destination.Length;

            if ((sourceOffset < 0) || (sourceOffset >= sourceLength))
                throw new ArgumentOutOfRangeException(nameof(sourceOffset), "The source offset parameter must be within the bounds of the source array.");

            if ((destinationOffset < 0) || (destinationOffset >= destinationLength))
                throw new ArgumentOutOfRangeException(nameof(destinationOffset), "The destination offset parameter must be within the bounds of the destination array.");

            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count), "The count parameter must be greater than or equal to 1.");

            if ((sourceOffset + count) > sourceLength)
                throw new ArgumentException("The block defined by source offset and count parameters must be within the bounds of the source array.");

            if ((destinationOffset + count) > destinationLength)
                throw new ArgumentException("The block defined by destination offset and count parameters must be within the bounds of the destination array.");

            fixed (Byte* pinSource = &source[sourceOffset])
            fixed (Byte* pinDestination = &destination[destinationOffset])
            {
                Byte* pointerSource = pinSource;
                Byte* pointerDestination = pinDestination;
                
                LengthSwitch:

                switch (count)
                {
                    case 0:
                        return;

                    case 1:
                        *pointerDestination = *pointerSource;
                        return;

                    case 2:
                        *(Int16*)pointerDestination = *(Int16*)pointerSource;
                        return;

                    case 3:
                        *(Int16*)(pointerDestination + 0) = *(Int16*)(pointerSource + 0);
                        *(pointerDestination + 2) = *(pointerSource + 2);
                        return;

                    case 4:
                        *(Int32*)pointerDestination = *(Int32*)pointerSource;
                        return;

                    case 5:
                        *(Int32*)(pointerDestination + 0) = *(Int32*)(pointerSource + 0);
                        *(pointerDestination + 4) = *(pointerSource + 4);
                        return;

                    case 6:
                        *(Int32*)(pointerDestination + 0) = *(Int32*)(pointerSource + 0);
                        *(Int16*)(pointerDestination + 4) = *(Int16*)(pointerSource + 4);
                        return;

                    case 7:
                        *(Int32*)(pointerDestination + 0) = *(Int32*)(pointerSource + 0);
                        *(Int16*)(pointerDestination + 4) = *(Int16*)(pointerSource + 4);
                        *(pointerDestination + 6) = *(pointerSource + 6);
                        return;

                    case 8:
                        *(Int64*)pointerDestination = *(Int64*)pointerSource;
                        return;

                    case 9:
                        *(Int64*)(pointerDestination + 0) = *(Int64*)(pointerSource + 0);
                        *(pointerDestination + 8) = *(pointerSource + 8);
                        return;

                    case 10:
                        *(Int64*)(pointerDestination + 0) = *(Int64*)(pointerSource + 0);
                        *(Int16*)(pointerDestination + 8) = *(Int16*)(pointerSource + 8);
                        return;

                    case 11:
                        *(Int64*)(pointerDestination + 0) = *(Int64*)(pointerSource + 0);
                        *(Int16*)(pointerDestination + 8) = *(Int16*)(pointerSource + 8);
                        *(pointerDestination + 10) = *(pointerSource + 10);
                        return;

                    case 12:
                        *(Int64*)pointerDestination = *(Int64*)pointerSource;
                        *(Int32*)(pointerDestination + 8) = *(Int32*)(pointerSource + 8);
                        return;

                    case 13:
                        *(Int64*)(pointerDestination + 0) = *(Int64*)(pointerSource + 0);
                        *(Int32*)(pointerDestination + 8) = *(Int32*)(pointerSource + 8);
                        *(pointerDestination + 12) = *(pointerSource + 12);
                        return;

                    case 14:
                        *(Int64*)(pointerDestination + 0) = *(Int64*)(pointerSource + 0);
                        *(Int32*)(pointerDestination + 8) = *(Int32*)(pointerSource + 8);
                        *(Int16*)(pointerDestination + 12) = *(Int16*)(pointerSource + 12);
                        return;

                    case 15:
                        *(Int64*)(pointerDestination + 0) = *(Int64*)(pointerSource + 0);
                        *(Int32*)(pointerDestination + 8) = *(Int32*)(pointerSource + 8);
                        *(Int16*)(pointerDestination + 12) = *(Int16*)(pointerSource + 12);
                        *(pointerDestination + 14) = *(pointerSource + 14);
                        return;

                    case 16:
                        *(Int64*)pointerDestination = *(Int64*)pointerSource;
                        *(Int64*)(pointerDestination + 8) = *(Int64*)(pointerSource + 8);
                        return;

                    case 17:
                        *(Int64*)pointerDestination = *(Int64*)pointerSource;
                        *(Int64*)(pointerDestination + 8) = *(Int64*)(pointerSource + 8);
                        *(pointerDestination + 16) = *(pointerSource + 16);
                        return;

                    case 18:
                        *(Int64*)pointerDestination = *(Int64*)pointerSource;
                        *(Int64*)(pointerDestination + 8) = *(Int64*)(pointerSource + 8);
                        *(Int16*)(pointerDestination + 16) = *(Int16*)(pointerSource + 16);
                        return;

                    case 19:
                        *(Int64*)pointerDestination = *(Int64*)pointerSource;
                        *(Int64*)(pointerDestination + 8) = *(Int64*)(pointerSource + 8);
                        *(Int16*)(pointerDestination + 16) = *(Int16*)(pointerSource + 16);
                        *(pointerDestination + 18) = *(pointerSource + 18);
                        return;

                    case 20:
                        *(Int64*)pointerDestination = *(Int64*)pointerSource;
                        *(Int64*)(pointerDestination + 8) = *(Int64*)(pointerSource + 8);
                        *(Int32*)(pointerDestination + 16) = *(Int32*)(pointerSource + 16);
                        return;

                    case 21:
                        *(Int64*)pointerDestination = *(Int64*)pointerSource;
                        *(Int64*)(pointerDestination + 8) = *(Int64*)(pointerSource + 8);
                        *(Int32*)(pointerDestination + 16) = *(Int32*)(pointerSource + 16);
                        *(pointerDestination + 20) = *(pointerSource + 20);
                        return;

                    case 22:
                        *(Int64*)pointerDestination = *(Int64*)pointerSource;
                        *(Int64*)(pointerDestination + 8) = *(Int64*)(pointerSource + 8);
                        *(Int32*)(pointerDestination + 16) = *(Int32*)(pointerSource + 16);
                        *(Int16*)(pointerDestination + 20) = *(Int16*)(pointerSource + 20);
                        return;

                    case 23:
                        *(Int64*)pointerDestination = *(Int64*)pointerSource;
                        *(Int64*)(pointerDestination + 8) = *(Int64*)(pointerSource + 8);
                        *(Int32*)(pointerDestination + 16) = *(Int32*)(pointerSource + 16);
                        *(Int16*)(pointerDestination + 20) = *(Int16*)(pointerSource + 20);
                        *(pointerDestination + 22) = *(pointerSource + 22);
                        return;

                    case 24:
                        *(Int64*)pointerDestination = *(Int64*)pointerSource;
                        *(Int64*)(pointerDestination + 8) = *(Int64*)(pointerSource + 8);
                        *(Int64*)(pointerDestination + 16) = *(Int64*)(pointerSource + 16);
                        return;

                    case 25:
                        *(Int64*)pointerDestination = *(Int64*)pointerSource;
                        *(Int64*)(pointerDestination + 8) = *(Int64*)(pointerSource + 8);
                        *(Int64*)(pointerDestination + 16) = *(Int64*)(pointerSource + 16);
                        *(pointerDestination + 24) = *(pointerSource + 24);
                        return;

                    case 26:
                        *(Int64*)pointerDestination = *(Int64*)pointerSource;
                        *(Int64*)(pointerDestination + 8) = *(Int64*)(pointerSource + 8);
                        *(Int64*)(pointerDestination + 16) = *(Int64*)(pointerSource + 16);
                        *(Int16*)(pointerDestination + 24) = *(Int16*)(pointerSource + 24);
                        return;

                    case 27:
                        *(Int64*)pointerDestination = *(Int64*)pointerSource;
                        *(Int64*)(pointerDestination + 8) = *(Int64*)(pointerSource + 8);
                        *(Int64*)(pointerDestination + 16) = *(Int64*)(pointerSource + 16);
                        *(Int16*)(pointerDestination + 24) = *(Int16*)(pointerSource + 24);
                        *(pointerDestination + 26) = *(pointerSource + 26);
                        return;

                    case 28:
                        *(Int64*)pointerDestination = *(Int64*)pointerSource;
                        *(Int64*)(pointerDestination + 8) = *(Int64*)(pointerSource + 8);
                        *(Int64*)(pointerDestination + 16) = *(Int64*)(pointerSource + 16);
                        *(Int32*)(pointerDestination + 24) = *(Int32*)(pointerSource + 24);
                        return;

                    case 29:
                        *(Int64*)pointerDestination = *(Int64*)pointerSource;
                        *(Int64*)(pointerDestination + 8) = *(Int64*)(pointerSource + 8);
                        *(Int64*)(pointerDestination + 16) = *(Int64*)(pointerSource + 16);
                        *(Int32*)(pointerDestination + 24) = *(Int32*)(pointerSource + 24);
                        *(pointerDestination + 28) = *(pointerSource + 28);
                        return;

                    case 30:
                        *(Int64*)pointerDestination = *(Int64*)pointerSource;
                        *(Int64*)(pointerDestination + 8) = *(Int64*)(pointerSource + 8);
                        *(Int64*)(pointerDestination + 16) = *(Int64*)(pointerSource + 16);
                        *(Int32*)(pointerDestination + 24) = *(Int32*)(pointerSource + 24);
                        *(Int16*)(pointerDestination + 28) = *(Int16*)(pointerSource + 28);
                        return;

                    case 31:
                        *(Int64*)pointerDestination = *(Int64*)pointerSource;
                        *(Int64*)(pointerDestination + 8) = *(Int64*)(pointerSource + 8);
                        *(Int64*)(pointerDestination + 16) = *(Int64*)(pointerSource + 16);
                        *(Int32*)(pointerDestination + 24) = *(Int32*)(pointerSource + 24);
                        *(Int16*)(pointerDestination + 28) = *(Int16*)(pointerSource + 28);
                        *(pointerDestination + 30) = *(pointerSource + 30);
                        return;

                    case 32:
                        *(Int64*)pointerDestination = *(Int64*)pointerSource;
                        *(Int64*)(pointerDestination + 8) = *(Int64*)(pointerSource + 8);
                        *(Int64*)(pointerDestination + 16) = *(Int64*)(pointerSource + 16);
                        *(Int64*)(pointerDestination + 24) = *(Int64*)(pointerSource + 24);
                        return;
                }
    
                Int64* pointerSourceLong = (Int64*)pointerSource;
                Int64* pointerDestinationLong = (Int64*)pointerDestination;

                while (count >= 64)
                {
                    *(pointerDestinationLong + 0) = *(pointerSourceLong + 0);
                    *(pointerDestinationLong + 1) = *(pointerSourceLong + 1);
                    *(pointerDestinationLong + 2) = *(pointerSourceLong + 2);
                    *(pointerDestinationLong + 3) = *(pointerSourceLong + 3);
                    *(pointerDestinationLong + 4) = *(pointerSourceLong + 4);
                    *(pointerDestinationLong + 5) = *(pointerSourceLong + 5);
                    *(pointerDestinationLong + 6) = *(pointerSourceLong + 6);
                    *(pointerDestinationLong + 7) = *(pointerSourceLong + 7);

                    if (count == 64)
                        return;

                    pointerSourceLong += 8;
                    pointerDestinationLong += 8;

                    count -= 64;
                }

                if (count > 32)
                {
                    *(pointerDestinationLong + 0) = *(pointerSourceLong + 0);
                    *(pointerDestinationLong + 1) = *(pointerSourceLong + 1);
                    *(pointerDestinationLong + 2) = *(pointerSourceLong + 2);
                    *(pointerDestinationLong + 3) = *(pointerSourceLong + 3);

                    pointerSourceLong += 4;
                    pointerDestinationLong += 4;

                    count -= 32;
                }
                
                pointerSource = (Byte*)pointerSourceLong;
                pointerDestination = (Byte*)pointerDestinationLong;

                goto LengthSwitch;
            }
        }
		#endif
        #endregion
    }
}
