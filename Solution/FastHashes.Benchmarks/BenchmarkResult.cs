#region Using Directives
using System;
#endregion

namespace FastHashes.Benchmarks
{
    public sealed class BenchmarkResult
    {
        #region Members
        private readonly Double m_BulkAverageSpeed;
        private readonly Double m_ChunkAverageSpeed;
        private readonly String m_BulkAverageSpeedFormatted;
        private readonly String m_ChunkAverageSpeedFormatted;
        private readonly String m_HashName;
        #endregion

        #region Properties
        public Double BulkAverageSpeed => m_BulkAverageSpeed;
        public Double ChunkAverageSpeed => m_ChunkAverageSpeed;
        public String BulkAverageSpeedFormatted => m_BulkAverageSpeedFormatted;
        public String ChunkAverageSpeedFormatted => m_ChunkAverageSpeedFormatted;
        public String HashName => m_HashName;
        #endregion

        #region Constructors
        public BenchmarkResult(String hashName, Double bulkAverageSpeed, String bulkAverageSpeedFormatted, Double chunkAverageSpeed, String chunkAverageSpeedFormatted)
        {
            if (String.IsNullOrWhiteSpace(hashName))
                throw new ArgumentException("Invalid hash name specified.", nameof(hashName));

            if (!Double.IsFinite(bulkAverageSpeed))
                throw new ArgumentException("Invalid bulk average speed specified.", nameof(bulkAverageSpeed));

            if (bulkAverageSpeed < 0.0d)
                throw new ArgumentOutOfRangeException(nameof(bulkAverageSpeed), "Invalid bulk average speed specified.");

            if (!Double.IsFinite(chunkAverageSpeed))
                throw new ArgumentException("Invalid chunk average speed specified.", nameof(chunkAverageSpeed));

            if (chunkAverageSpeed < 0.0d)
                throw new ArgumentOutOfRangeException(nameof(chunkAverageSpeed), "Invalid chunk average speed specified.");

            if (String.IsNullOrWhiteSpace(bulkAverageSpeedFormatted))
                throw new ArgumentException("Invalid formatted bulk average speed specified.", nameof(bulkAverageSpeedFormatted));

            if (String.IsNullOrWhiteSpace(chunkAverageSpeedFormatted))
                throw new ArgumentException("Invalid formatted chunk average speed specified.", nameof(chunkAverageSpeedFormatted));

            m_BulkAverageSpeed = bulkAverageSpeed;
            m_ChunkAverageSpeed = chunkAverageSpeed;
            m_BulkAverageSpeedFormatted = bulkAverageSpeedFormatted;
            m_ChunkAverageSpeedFormatted = chunkAverageSpeedFormatted;
            m_HashName = hashName;
        }
        #endregion

        #region Methods
        public override String ToString()
        {
            return $"{GetType().Name}: {m_HashName} BULK={m_BulkAverageSpeedFormatted} CHUNK={m_ChunkAverageSpeedFormatted}";
        }
        #endregion
    }
}
