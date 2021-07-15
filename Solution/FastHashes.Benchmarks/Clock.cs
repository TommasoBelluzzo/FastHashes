using System;
using System.Diagnostics;
using System.Threading;

namespace FastHashes.Benchmarks
{
    public sealed class Clock : IDisposable  
    {
        #region Constants
        private const Int64 TICKS_MULTIPLIER = 1000 * TimeSpan.TicksPerMillisecond;
        #endregion

        #region Members
        private readonly Int64 m_MaximumIdleTime;
        private readonly ThreadLocal<DateTime> m_StartTime;
        private readonly ThreadLocal<Double> m_StartTimestamp;
        private Boolean m_IsDisposed;
        #endregion
        
        #region Constructors
        public Clock(Int32 maximumIdleTime)
        {
            m_MaximumIdleTime = TimeSpan.FromSeconds(maximumIdleTime).Ticks;
            m_StartTime = new ThreadLocal<DateTime>(() => DateTime.UtcNow, false);
            m_StartTimestamp = new ThreadLocal<Double>(() => Stopwatch.GetTimestamp(), false);
        }
        #endregion

        #region Destructors
        ~Clock()
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
            {
                m_StartTime?.Dispose();
                m_StartTimestamp?.Dispose();
            }

            m_IsDisposed = true;
        }

        public DateTime GetTime()
        {
            Double endTimestamp = Stopwatch.GetTimestamp();
            Int64 durationTicks = (Int64)(((endTimestamp - m_StartTimestamp.Value) / Stopwatch.Frequency) * TICKS_MULTIPLIER);

            if (durationTicks < m_MaximumIdleTime)
                return m_StartTime.Value.AddTicks(durationTicks);

            m_StartTime.Value = DateTime.UtcNow;
            m_StartTimestamp.Value = Stopwatch.GetTimestamp();

            return m_StartTime.Value;

        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);  
        }
        #endregion
    }
}
