#region Using Directives
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security;
#endregion

namespace FastHashes.Benchmarks
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

        [DllImport("Kernel32.dll", CallingConvention=CallingConvention.StdCall, CharSet=CharSet.Unicode, ExactSpelling=true, SetLastError=true)]
        private static extern UInt32 GetCurrentThreadId();
        #endregion

        #region Methods
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
        #endregion
    }
}