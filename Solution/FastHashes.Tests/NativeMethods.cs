#region Using Directives
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security;
#endregion

namespace FastHashes.Tests
{
    [SuppressUnmanagedCodeSecurity]
    internal static class NativeMethods
    {
        #region Imports
        [DllImport("Kernel32.dll", CallingConvention=CallingConvention.StdCall, CharSet=CharSet.Unicode, ExactSpelling=true, SetLastError=true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern Boolean QueryPerformanceCounter([Out] out Int64 frequency);

        [DllImport("Kernel32.dll", CallingConvention=CallingConvention.StdCall, CharSet=CharSet.Unicode, ExactSpelling=true, SetLastError=true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern Boolean QueryPerformanceFrequency([Out] out Int64 frequency);

        [DllImport("MSVCRT.dll", CallingConvention=CallingConvention.Cdecl, ExactSpelling=true)]
        private static extern Int32 memcmp([In] IntPtr pointer1, [In] IntPtr pointer2, [In] UIntPtr count);

        [DllImport("MSVCRT.dll", CallingConvention=CallingConvention.Cdecl, ExactSpelling=true)]
        public static extern IntPtr memset([In] IntPtr pointer, [In] Int32 character, [In] UIntPtr count);

        [DllImport("Kernel32.dll", CallingConvention=CallingConvention.StdCall, CharSet=CharSet.Unicode, ExactSpelling=true, SetLastError=true)]
        private static extern UInt32 GetCurrentThreadId();
        #endregion

        #region Methods
        public static Boolean EqualSequences(Byte[] array1, Byte[] array2)
        {
            if (ReferenceEquals(array1, array2))
                return true;

            if ((array1 == null) || (array2 == null) || (array1.Length != array2.Length))
                return false;

            unsafe
            {
                fixed (Byte* pin1 = array1, pin2 = array2)
                {
                    IntPtr pointer1 = (IntPtr)pin1;
                    IntPtr pointer2 = (IntPtr)pin2;

                    return (memcmp(pointer1, pointer2, (UIntPtr)array1.Length) == 0);
                }
            }
        }

        public static Double GetFrequency()
        {
            Boolean result = QueryPerformanceFrequency(out Int64 frequency);

            if (!result)
                throw new Win32Exception(Marshal.GetLastWin32Error());

            return frequency;
        }

        public static Double GetTime()
        {
            Boolean result = QueryPerformanceCounter(out Int64 time);

            if (!result)
                throw new Win32Exception(Marshal.GetLastWin32Error());

            return time;
        }

        public static Int32 CompareSequences(Byte[] array1, Byte[] array2)
        {
            if (ReferenceEquals(array1, array2))
                return 0;

            if (array1 == null)
                return 1;

            if (array2 == null)
                return -1;

            if (array1.Length != array2.Length)
                return -Math.Min(Math.Max(array1.Length - array2.Length, -1), 1);

            unsafe
            {
                fixed (Byte* pin1 = array1, pin2 = array2)
                {
                    IntPtr pointer1 = (IntPtr)pin1;
                    IntPtr pointer2 = (IntPtr)pin2;

                    return -Math.Min(Math.Max(memcmp(pointer1, pointer2, (UIntPtr)array1.Length), -1), 1);
                }  
            }
        }

        public static ProcessThread GetCurrentThread()
        {
            Int32 threadId = (Int32)GetCurrentThreadId();

            Process process = Process.GetCurrentProcess();
            ProcessThread thread = null;

            for (Int32 i = 0; i < process.Threads.Count; ++i)
            {
                ProcessThread currentThread = process.Threads[i];

                if (currentThread.Id == threadId)
                {
                    thread = currentThread;
                    break;
                }
            }

            return thread;
        }

        public static void FillArray(Byte[] array, Byte value)
        {
            if (array == null)
                throw new ArgumentNullException(nameof(array));

            if (array.Length == 0)
                return;

            unsafe
            {
                fixed (Byte* pin = array)
                {
                    IntPtr pointer = (IntPtr)pin;
                    memset(pointer, value, (UIntPtr)array.Length);
                }
            }
        }
        #endregion
    }
}
