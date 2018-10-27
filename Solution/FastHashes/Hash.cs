#region Using Directives
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
#endregion

namespace FastHashes
{
    public abstract class Hash
    {
        #region Members (Static)
        private static readonly Boolean s_AllowsUnalignedRead = AllowsUnalignedRead();
        private static readonly Boolean s_IsLittleEndian = BitConverter.IsLittleEndian;
        #endregion

        #region Properties (Abstract)
        public abstract Int32 Length { get; }
        #endregion

        #region Methods
        public Byte[] ComputeHash(Byte[] data)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            return ComputeHashInternal(data, 0, data.Length);
        }

        public Byte[] ComputeHash(Byte[] data, Int32 offset, Int32 length)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            Int32 dataLength = data.Length;

            if ((offset < 0) || (offset >= dataLength))
                throw new ArgumentOutOfRangeException(nameof(offset), "The offset parameter must be within the bounds of the data array.");

            if (length < 0)
                throw new ArgumentOutOfRangeException(nameof(length), "The length parameter must be greater than or equal to 0.");

            if ((offset + length) > dataLength)
                throw new InvalidOperationException("The block defined by offset and length parameters must be within the bounds of the data array.");

            return ComputeHashInternal(data, offset, length);
        }

        public override String ToString()
        {
            return GetType().Name;
        }
        #endregion

        #region Methods (Abstract)
        protected abstract Byte[] ComputeHashInternal(Byte[] data, Int32 offset, Int32 length);
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
                LoadUserProfile = false,
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected static UInt32 RotateLeft(UInt32 value, Int32 rotation)
        {
            rotation &= 0x1F;
            return (value << rotation) | (value >> (32 - rotation));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected static UInt64 RotateLeft(UInt64 value, Int32 rotation)
        {
            rotation &= 0x3F;
            return (value << rotation) | (value >> (64 - rotation));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected static UInt32 RotateRight(UInt32 value, Int32 rotation)
        {
            rotation &= 0x1F;
            return (value >> rotation) | (value << (32 - rotation));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected static UInt64 RotateRight(UInt64 value, Int32 rotation)
        {
            rotation &= 0x3F;
            return (value >> rotation) | (value << (64 - rotation));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected static void Swap(ref UInt16 value1, ref UInt16 value2)
        {
            UInt16 tmp = value1;
            value1 = value2;
            value2 = tmp;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected static void Swap(ref UInt32 value1, ref UInt32 value2)
        {
            UInt32 tmp = value1;
            value1 = value2;
            value2 = tmp;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected static void Swap(ref UInt64 value1, ref UInt64 value2)
        {
            UInt64 tmp = value1;
            value1 = value2;
            value2 = tmp;
        }
        #endregion
    }
}