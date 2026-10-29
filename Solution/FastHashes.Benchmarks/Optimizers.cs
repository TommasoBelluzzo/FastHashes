#region Using Directives
using System;
using System.Diagnostics;
using System.Runtime;
#endregion

namespace FastHashes.Benchmarks
{
    public abstract class Optimizer : IDisposable
    {
        #region Members
        private Boolean m_IsDisposed;
        #endregion

        #region Methods
        protected virtual void DisposeInternal() { }

        public void Dispose()
        {
            if (m_IsDisposed)
                return;

            DisposeInternal();
            m_IsDisposed = true;
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

            try
            {
                m_Process.PriorityClass = ProcessPriorityClass.AboveNormal;
                m_RestorePriority = true;
            }
            catch
            {
                m_RestorePriority = false;
            }

            m_LatencyMode = GCSettings.LatencyMode;
            GCSettings.LatencyMode = GCLatencyMode.SustainedLowLatency;
        }
        #endregion

        #region Methods
        protected override void DisposeInternal()
        {
            if (m_RestorePriority)
            {
                try
                {
                    m_Process.PriorityClass = m_PriorityClass;
                }
                catch { }
            }

            try
            {
                m_Process.Dispose();
            }
            catch { }

            try
            {
                GCSettings.LatencyMode = m_LatencyMode;
            }
            catch { }
        }
        #endregion
    }
}