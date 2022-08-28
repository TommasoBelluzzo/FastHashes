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
#if WINDOWS
        [DllImport("Kernel32.dll", CallingConvention=CallingConvention.StdCall, ExactSpelling=true, SetLastError=true)]
        private static extern IntPtr GetCurrentThread();

        [DllImport("Kernel32.dll", CallingConvention=CallingConvention.StdCall, ExactSpelling=true, SetLastError=true)]
        private static extern IntPtr SetThreadAffinityMask(IntPtr thread, IntPtr affinity);
#else
        [DllImport("libc", CallingConvention=CallingConvention.Cdecl, EntryPoint="sched_getaffinity", SetLastError=true)]
        private static extern Int32 GetThreadAffinityMask(Int32 processId, IntPtr affinitySize, ref UInt64 affinity);

        [DllImport("libc", CallingConvention=CallingConvention.Cdecl, EntryPoint="sched_setaffinity", SetLastError=true)]
        private static extern Int32 SetThreadAffinityMask(Int32 processId, IntPtr affinitySize, ref UInt64 affinity);
#endif
        #endregion

        #region Methods
        #if WINDOWS
        public static IntPtr SetThreadAffinity(IntPtr affinity)
        {
            return ((affinity == IntPtr.Zero) ? IntPtr.Zero : SetThreadAffinityMask(GetCurrentThread(), affinity));
        }
        #else
        public static IntPtr SetThreadAffinity(IntPtr affinity)
        {
            if (affinity == IntPtr.Zero)
                return IntPtr.Zero;

            IntPtr affinitySize = new IntPtr(8);
            UInt64 affinityMask = (UInt64)affinity.ToInt64();
            UInt64 affinityMaskCurrent = 0u;

            if (NativeMethods.GetThreadAffinityMask(0, affinitySize, ref affinityMaskCurrent) != 0)          
              return IntPtr.Zero;

            if (NativeMethods.SetThreadAffinityMask(0, affinitySize, ref affinityMask) != 0)
              return IntPtr.Zero;

            return new IntPtr((Int64)affinityMaskCurrent);
        }
        #endif
        #endregion
    }
}