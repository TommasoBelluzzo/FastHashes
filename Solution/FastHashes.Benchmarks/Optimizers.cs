#region Using Directives
using System;
using System.Diagnostics;
using System.Runtime;
using System.Threading;
#endregion

namespace FastHashes.Benchmarks
{
    public abstract class Optimizer : IDisposable
    {
        #region Members
        private Boolean m_IsDisposed;
        #endregion

        #region Destructors
        ~Optimizer()
        {
            Dispose(false);
        }
        #endregion

        #region Methods
        private void Dispose(Boolean disposing)
        {
            if (m_IsDisposed)
                return;

            if (disposing)
                Finalization();

            m_IsDisposed = true;
        }

        protected virtual void Finalization() { }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);  
        }
        #endregion
    }

    public sealed class AffinityOptimizer : Optimizer
    {
        #region Members
        #if !MACOS && (NETCOREAPP2_0 || NETCOREAPP2_1 || NETCOREAPP2_2 || NETCOREAPP3_0 || NETCOREAPP3_1)
        private readonly IntPtr m_ProcessAffinity;
        private readonly Process m_Process;
        private readonly IntPtr m_ThreadAffinity;
        #endif
        #endregion

        #region Constructors
        public AffinityOptimizer()
        {
            #if !MACOS && (NETCOREAPP2_0 || NETCOREAPP2_1 || NETCOREAPP2_2 || NETCOREAPP3_0 || NETCOREAPP3_1)
            m_Process = Process.GetCurrentProcess();
            m_ProcessAffinity = m_Process.ProcessorAffinity;
            m_ThreadAffinity = IntPtr.Zero;

            IntPtr affinity = (IntPtr)(1 << (Environment.ProcessorCount - 1));

            m_Process.ProcessorAffinity = affinity;

            Thread.BeginThreadAffinity();
            m_ThreadAffinity = NativeMethods.SetThreadAffinity(affinity);
            #endif
        }
        #endregion

        #region Methods
        protected override void Finalization()
        {
            #if !MACOS && (NETCOREAPP2_0 || NETCOREAPP2_1 || NETCOREAPP2_2 || NETCOREAPP3_0 || NETCOREAPP3_1)
            NativeMethods.SetThreadAffinity(m_ThreadAffinity);
            Thread.EndThreadAffinity();

            m_Process.ProcessorAffinity = m_ProcessAffinity;
            #endif
        }
        #endregion
    }

    public sealed class SpeedOptimizer : Optimizer
    {
        #region Members
        private readonly Boolean m_RestorePriority;
        private readonly Process m_Process; 
        private readonly ProcessPriorityClass m_PriorityClass; 
        private readonly GCLatencyMode m_LatencyMode; 
        #endregion

        #region Constructors
        public SpeedOptimizer()
        {
            m_Process = Process.GetCurrentProcess();
            m_PriorityClass = m_Process.PriorityClass;
            m_LatencyMode = GCSettings.LatencyMode;
            m_RestorePriority = false;

            try
            {
                m_Process.PriorityClass = ProcessPriorityClass.RealTime;
                m_RestorePriority = true;
            }
            catch
            {
                try
                {
                    m_Process.PriorityClass = ProcessPriorityClass.High;
                    m_RestorePriority = true;
                }
                catch
                {
                    try
                    {
                        m_Process.PriorityClass = ProcessPriorityClass.AboveNormal;
                        m_RestorePriority = true;
                    }
                    catch { }
                }
            }

            GCSettings.LatencyMode = GCLatencyMode.SustainedLowLatency;
        }
        #endregion

        #region Methods
        protected override void Finalization()
        {
            GCSettings.LatencyMode = m_LatencyMode;

            if (m_RestorePriority)
            {
                try
                {
                    m_Process.PriorityClass = m_PriorityClass;
                }
                catch { }
            }
        }
        #endregion
    }
}