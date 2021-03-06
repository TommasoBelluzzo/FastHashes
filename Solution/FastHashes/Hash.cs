﻿#region Using Directives
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
#endregion

namespace FastHashes
{
    /// <summary>Represents the base class from which all the hash algorithms must derive. This class is abstract.</summary>
    public abstract class Hash
    {
        #region Members (Static)
        private static readonly Boolean s_AllowsUnalignedRead = AllowsUnalignedRead();
        private static readonly Boolean s_IsLittleEndian = BitConverter.IsLittleEndian;
        #endregion

        #region Properties (Abstract)
        /// <summary>Gets the size, in bits, of the computed hash code.</summary>
        /// <value>An <see cref="T:System.Int32"/> value, greater than or equal to <c>32</c>.</value>
        public abstract Int32 Length { get; }
        #endregion

        #region Methods
        private static Boolean AllowsUnalignedRead()
        {
            if ((new[] {"x86", "amd64"}).Contains(Environment.GetEnvironmentVariable("PROCESSOR_ARCHITECTURE"), StringComparer.OrdinalIgnoreCase))
                return true;

            ProcessStartInfo si = new ProcessStartInfo
            {
                CreateNoWindow = true,
                ErrorDialog = false,
                FileName = "uname",
                RedirectStandardOutput = true,
                UseShellExecute = false
            };

            try
            {
                using (Process process = new Process())
                {
                    process.StartInfo = si;
                    process.StartInfo.Arguments = "-p";

                    process.Start();

                    using (StreamReader stream = process.StandardOutput)
                    {
                        String output = stream.ReadLine();

                        if (!String.IsNullOrWhiteSpace(output))
                        {
                            output = output.Trim();

                            if ((new[] {"amd64", "i386", "x64", "x86_64"}).Contains(output, StringComparer.OrdinalIgnoreCase))
                                return true;
                        }
                    }
                }
            }
            catch { }

            try
            {
                using (Process process = new Process())
                {
                    process.StartInfo = si;
                    process.StartInfo.Arguments = "-m";

                    process.Start();

                    using (StreamReader stream = process.StandardOutput)
                    {
                        String output = stream.ReadLine();

                        if (!String.IsNullOrWhiteSpace(output))
                        {
                            output = output.Trim();

                            if ((new[] {"amd64", "x64", "x86_64"}).Contains(output, StringComparer.OrdinalIgnoreCase))
                                return true;

                            if ((new Regex(@"i\d86")).IsMatch(output))
                                return true;
                        }
                    }
                }
            }
            catch { }

            return false;
        }

        /// <summary>Reads a 2-bytes unsigned integer from the specified byte pointer, without increment.</summary>
        /// <param name="pointer">The <see cref="T:System.Byte"/>* to read.</param>
        /// <returns>An <see cref="T:System.UInt16"/> value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected static unsafe UInt16 Fetch16(Byte* pointer)
        {
            UInt16 v;

            if (s_IsLittleEndian)
            { 
                if (s_AllowsUnalignedRead || (((Int64)pointer & 7) == 0))
                    v = *((UInt16*)pointer);
                else
                    v = (UInt16)(pointer[0] | (pointer[1] << 8));
            }
            else
                v = (UInt16)((pointer[0] << 8) | pointer[1]);

            return v;
        }

        /// <summary>Reads a 4-bytes unsigned integer from the specified byte pointer, without increment.</summary>
        /// <param name="pointer">The <see cref="T:System.Byte"/>* to read.</param>
        /// <returns>An <see cref="T:System.UInt32"/> value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected static unsafe UInt32 Fetch32(Byte* pointer)
        {
            UInt32 v;

            if (s_IsLittleEndian)
            { 
                if (s_AllowsUnalignedRead || (((Int64)pointer & 7) == 0))
                    v = *((UInt32*)pointer);
                else
                    v = (UInt32)(pointer[0] | (pointer[1] << 8) | (pointer[2] << 16) | (pointer[3] << 24));
            }
            else
                v = (UInt32)((pointer[0] << 24) | (pointer[1] << 16) | (pointer[2] << 8) | pointer[3]);

            return v;
        }
        
        /// <summary>Reads a 8-bytes unsigned integer from the specified byte pointer, without increment.</summary>
        /// <param name="pointer">The <see cref="T:System.Byte"/>* to read.</param>
        /// <returns>An <see cref="T:System.UInt64"/> value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected static unsafe UInt64 Fetch64(Byte* pointer)
        {
            UInt64 v;

            if (s_IsLittleEndian)
            { 
                if (s_AllowsUnalignedRead || (((Int64)pointer & 7) == 0))
                    v = *((UInt64*)pointer);
                else
                    v = (UInt64)(pointer[0] | (pointer[1] << 8) | (pointer[2] << 16) | (pointer[3] << 24) | (pointer[4] << 32) | (pointer[5] << 40) | (pointer[6] << 48) | (pointer[7] << 56));
            }
            else
                v = (UInt64)((pointer[0] << 56) | (pointer[1] << 48) | (pointer[2] << 40) | (pointer[3] << 32) | (pointer[4] << 24) | (pointer[5] << 16) | (pointer[6] << 8) | pointer[7]);

            return v;
        }

        /// <summary>Reads a 2-bytes unsigned integer from the specified byte pointer, with increment.</summary>
        /// <param name="pointer">The <see cref="T:System.Byte"/>* to read.</param>
        /// <returns>An <see cref="T:System.UInt16"/> value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected static unsafe UInt16 Read16(ref Byte* pointer)
        {
            UInt16 v;

            if (s_IsLittleEndian)
            { 
                if (s_AllowsUnalignedRead || (((Int64)pointer & 7) == 0))
                    v = *((UInt16*)pointer);
                else
                    v = (UInt16)(pointer[0] | (pointer[1] << 8));
            }
            else
                v = (UInt16)((pointer[0] << 8) | pointer[1]);

            pointer += 2;

            return v;
        }

        /// <summary>Reads a 4-bytes unsigned integer from the specified byte pointer, with increment.</summary>
        /// <param name="pointer">The <see cref="T:System.Byte"/>* to read.</param>
        /// <returns>An <see cref="T:System.UInt32"/> value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected static unsafe UInt32 Read32(ref Byte* pointer)
        {
            UInt32 v;

            if (s_IsLittleEndian)
            { 
                if (s_AllowsUnalignedRead || (((Int64)pointer & 7) == 0))
                    v = *((UInt32*)pointer);
                else
                    v = (UInt32)(pointer[0] | (pointer[1] << 8) | (pointer[2] << 16) | (pointer[3] << 24));
            }
            else
                v = (UInt32)((pointer[0] << 24) | (pointer[1] << 16) | (pointer[2] << 8) | pointer[3]);

            pointer += 4;

            return v;
        }

        /// <summary>Reads a 8-bytes unsigned integer from the specified byte pointer, with increment.</summary>
        /// <param name="pointer">The <see cref="T:System.Byte"/>* to read.</param>
        /// <returns>An <see cref="T:System.UInt64"/> value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected static unsafe UInt64 Read64(ref Byte* pointer)
        {
            UInt64 v;

            if (s_IsLittleEndian)
            { 
                if (s_AllowsUnalignedRead || (((Int64)pointer & 7) == 0))
                    v = *((UInt64*)pointer);
                else
                    v = (UInt64)(pointer[0] | (pointer[1] << 8) | (pointer[2] << 16) | (pointer[3] << 24) | (pointer[4] << 32) | (pointer[5] << 40) | (pointer[6] << 48) | (pointer[7] << 56));
            }
            else
                v = (UInt64)((pointer[0] << 56) | (pointer[1] << 48) | (pointer[2] << 40) | (pointer[3] << 32) | (pointer[4] << 24) | (pointer[5] << 16) | (pointer[6] << 8) | pointer[7]);

            pointer += 8;

            return v;
        }

        /// <summary>Rotates a 4-bytes unsigned integer left by the specified number of bits.</summary>
        /// <param name="value">The <see cref="T:System.UInt32"/> to rotate.</param>
        /// <param name="rotation">The number of bits to rotate.</param>
        /// <returns>An <see cref="T:System.UInt32"/> value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected static UInt32 RotateLeft(UInt32 value, Int32 rotation)
        {
            rotation &= 0x1F;
            return (value << rotation) | (value >> (32 - rotation));
        }

        /// <summary>Rotates a 8-bytes unsigned integer left by the specified number of bits.</summary>
        /// <param name="value">The <see cref="T:System.UInt64"/> to rotate.</param>
        /// <param name="rotation">The number of bits to rotate.</param>
        /// <returns>An <see cref="T:System.UInt64"/> value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected static UInt64 RotateLeft(UInt64 value, Int32 rotation)
        {
            rotation &= 0x3F;
            return (value << rotation) | (value >> (64 - rotation));
        }

        /// <summary>Rotates a 4-bytes unsigned integer right by the specified number of bits.</summary>
        /// <param name="value">The <see cref="T:System.UInt32"/> to rotate.</param>
        /// <param name="rotation">The number of bits to rotate.</param>
        /// <returns>An <see cref="T:System.UInt32"/> value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected static UInt32 RotateRight(UInt32 value, Int32 rotation)
        {
            rotation &= 0x1F;
            return (value >> rotation) | (value << (32 - rotation));
        }

        /// <summary>Rotates a 8-bytes unsigned integer right by the specified number of bits.</summary>
        /// <param name="value">The <see cref="T:System.UInt64"/> to rotate.</param>
        /// <param name="rotation">The number of bits to rotate.</param>
        /// <returns>An <see cref="T:System.UInt64"/> value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected static UInt64 RotateRight(UInt64 value, Int32 rotation)
        {
            rotation &= 0x3F;
            return (value >> rotation) | (value << (64 - rotation));
        }

        /// <summary>Swaps the value of two 2-bytes unsigned integers.</summary>
        /// <param name="value1">The first <see cref="T:System.UInt16"/>, whose value is assigned to the second one.</param>
        /// <param name="value2">The second <see cref="T:System.UInt16"/>, whose value is assigned to the first one.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected static void Swap(ref UInt16 value1, ref UInt16 value2)
        {
            UInt16 tmp = value1;
            value1 = value2;
            value2 = tmp;
        }

        /// <summary>Swaps the value of two 4-bytes unsigned integers.</summary>
        /// <param name="value1">The first <see cref="T:System.UInt32"/>, whose value is assigned to the second one.</param>
        /// <param name="value2">The second <see cref="T:System.UInt32"/>, whose value is assigned to the first one.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected static void Swap(ref UInt32 value1, ref UInt32 value2)
        {
            UInt32 tmp = value1;
            value1 = value2;
            value2 = tmp;
        }

        /// <summary>Swaps the value of two 8-bytes unsigned integers.</summary>
        /// <param name="value1">The first <see cref="T:System.UInt64"/>, whose value is assigned to the second one.</param>
        /// <param name="value2">The second <see cref="T:System.UInt64"/>, whose value is assigned to the first one.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected static void Swap(ref UInt64 value1, ref UInt64 value2)
        {
            UInt64 tmp = value1;
            value1 = value2;
            value2 = tmp;
        }

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

            return ComputeHashInternal(buffer, offset, count);
        }

        /// <summary>Returns the text representation of the current instance.</summary>
        /// <returns>A <see cref="T:System.String"/> representing the current instance.</returns>
        public override String ToString()
        {
            return GetType().Name;
        }
        #endregion

        #region Methods (Abstract)
        /// <summary>Represents the core hashing function of the algorithm.</summary>
        /// <param name="buffer">The <see cref="T:System.Byte"/>[] whose hash must be computed.</param>
        /// <param name="offset">The offset into the byte array from which to begin using data.</param>
        /// <param name="count">The number of bytes in the array to use as data.</param>
        /// <returns>A <see cref="T:System.Byte"/>[] representing the computed hash.</returns>
        protected abstract Byte[] ComputeHashInternal(Byte[] buffer, Int32 offset, Int32 count);
        #endregion
    }
}
