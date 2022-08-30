#region Using Directives
using System;
using System.Runtime.InteropServices;
using System.Security;
#endregion

namespace FastHashes.Benchmarks
{
    #if !NETCOREAPP1_0 && !NETCOREAPP1_1
    [SuppressUnmanagedCodeSecurity]
    #endif
    internal static class NativeMethods
    {
        #region Imports
        #if WINDOWS && (NETCOREAPP2_0 || NETCOREAPP2_1 || NETCOREAPP2_2 || NETCOREAPP3_0 || NETCOREAPP3_1)
        [DllImport("Kernel32.dll", CallingConvention=CallingConvention.StdCall, ExactSpelling=true, SetLastError=true)]
        private static extern IntPtr GetCurrentThread();

        [DllImport("Kernel32.dll", CallingConvention=CallingConvention.StdCall, ExactSpelling=true, SetLastError=true)]
        private static extern IntPtr SetThreadAffinityMask(IntPtr thread, IntPtr affinity);
        #elif UNIX && (NETCOREAPP2_0 || NETCOREAPP2_1 || NETCOREAPP2_2 || NETCOREAPP3_0 || NETCOREAPP3_1)
        [DllImport("libc", CallingConvention=CallingConvention.Cdecl, EntryPoint="sched_getaffinity", SetLastError=true)]
        private static extern Int32 GetThreadAffinityMask(Int32 processId, IntPtr affinitySize, ref UInt64 affinity);

        [DllImport("libc", CallingConvention=CallingConvention.Cdecl, EntryPoint="sched_setaffinity", SetLastError=true)]
        private static extern Int32 SetThreadAffinityMask(Int32 processId, IntPtr affinitySize, ref UInt64 affinity);
        #endif
        #endregion

        #region Methods
        public static IntPtr SetThreadAffinity(IntPtr affinity)
        {
            if (affinity == IntPtr.Zero) 
                return IntPtr.Zero;

            IntPtr returnValue = IntPtr.Zero;

            #if WINDOWS && (NETCOREAPP2_0 || NETCOREAPP2_1 || NETCOREAPP2_2 || NETCOREAPP3_0 || NETCOREAPP3_1)
            IntPtr currentThread = GetCurrentThread();
            returnValue = SetThreadAffinityMask(currentThread, affinity);
            #elif UNIX && (NETCOREAPP2_0 || NETCOREAPP2_1 || NETCOREAPP2_2 || NETCOREAPP3_0 || NETCOREAPP3_1)
            IntPtr affinitySize = new IntPtr(8);
            UInt64 affinityMask = (UInt64)affinity.ToInt64();
            UInt64 affinityMaskCurrent = 0u;

            if (NativeMethods.GetThreadAffinityMask(0, affinitySize, ref affinityMaskCurrent) == 0)
            {
                if (NativeMethods.SetThreadAffinityMask(0, affinitySize, ref affinityMask) == 0)
                    returnValue = new IntPtr((Int64)affinityMaskCurrent);
            }
            #endif

            return returnValue;
        }
        #endregion
    }
}