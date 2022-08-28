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
        private readonly IntPtr m_ProcessAffinity;
        private readonly Process m_Process;
        private IntPtr m_ThreadAffinity; 
        #endregion

        #region Constructors
        public AffinityOptimizer()
        {
            m_Process = Process.GetCurrentProcess();

            #if NET6_0_OR_GREATER || (NET5_0_OR_GREATER && MACOS)
            m_ProcessAffinity = IntPtr.Zero;
            m_ThreadAffinity = IntPtr.Zero;
            #else
            m_ProcessAffinity = m_Process.ProcessorAffinity;
            m_ThreadAffinity = IntPtr.Zero;
            #endif

            Initialization();
        }
        #endregion

        #region Methods
        
        private void Initialization()
        {
            IntPtr affinity = (IntPtr)(1 << (Environment.ProcessorCount - 1));

            #if !NET6_0_OR_GREATER && (!NET5_0_OR_GREATER || !MACOS)
            m_Process.ProcessorAffinity = affinity;
            #endif

            #if !MACOS && !NETCOREAPP1_0 && !NETCOREAPP1_1
            Thread.BeginThreadAffinity();
            m_ThreadAffinity = NativeMethods.SetThreadAffinity(affinity);
            #endif
        }

        protected override void Finalization()
        {
            #if !MACOS && !NETCOREAPP1_0 && !NETCOREAPP1_1
            NativeMethods.SetThreadAffinity(m_ThreadAffinity);
            Thread.EndThreadAffinity();
            #endif

            #if !NET6_0_OR_GREATER && (!NET5_0_OR_GREATER || !MACOS)
            m_Process.ProcessorAffinity = m_ProcessAffinity;
            #endif
        }
        #endregion
    }

    public sealed class SpeedOptimizer : Optimizer
    {
        #region Members
        private readonly Process m_Process; 
        private readonly ProcessPriorityClass m_PriorityClass; 
        private readonly GCLatencyMode m_LatencyMode; 
        private Boolean m_RestorePriority; 
        #endregion

        #region Constructors
        public SpeedOptimizer()
        {
            m_Process = Process.GetCurrentProcess();
            m_PriorityClass = m_Process.PriorityClass;
            m_LatencyMode = GCSettings.LatencyMode;
            m_RestorePriority = false;

            Initialization();
        }
        #endregion

        #region Methods
        private void Initialization()
        {
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