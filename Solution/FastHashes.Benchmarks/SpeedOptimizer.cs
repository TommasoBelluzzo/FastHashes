#region Using Directives
using System;
using System.Diagnostics;
using System.Runtime;
#endregion

namespace FastHashes.Benchmarks
{
    public sealed class SpeedOptimizer : IDisposable
    {
        #region Members
        private Boolean m_IsDisposed;
        private readonly Boolean m_RestoreLatency;
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

            try
            {
                GCSettings.LatencyMode = GCLatencyMode.SustainedLowLatency;
                m_RestoreLatency = true;
            }
            catch
            {
                m_RestoreLatency = false;
            }

            try
            {
                m_Process.PriorityClass = ProcessPriorityClass.AboveNormal;
                m_RestorePriority = true;
            }
            catch
            {
                m_RestorePriority = false;
            }
        }
        #endregion

        #region Methods
        public void Dispose()
        {
            if (m_IsDisposed)
                return;

            if (m_RestorePriority)
            {
                try
                {
                    m_Process.PriorityClass = m_PriorityClass;
                }
                catch { }
            }

            if (m_RestoreLatency)
            {
                try
                {
                    GCSettings.LatencyMode = m_LatencyMode;
                }
                catch { }
            }

            try
            {
                m_Process.Dispose();
            }
            catch { }

            m_IsDisposed = true;

            GC.SuppressFinalize(this);
        }
        #endregion
    }
}