#region Using Directives
using System;
#endregion

namespace FastHashes.Benchmarks
{
    public sealed class BenchmarkCase
    {
        #region Members
        private readonly Func<UInt32, Hash> m_HashInitializer;
        private readonly String m_HashName;
        #endregion

        #region Properties
        public Func<UInt32,Hash> HashInitializer => m_HashInitializer;
        public String HashName => m_HashName;
        #endregion

        #region Constructors
        public BenchmarkCase(String hashName, Func<UInt32,Hash> hashInitializer)
        {
            if (String.IsNullOrWhiteSpace(hashName))
                throw new ArgumentException("Invalid hash name specified.", nameof(hashName));

            if (hashInitializer == null)
                throw new ArgumentException("Invalid hash initializer specified.", nameof(hashInitializer));

            m_HashInitializer = hashInitializer;
            m_HashName = hashName;
        }
        #endregion

        #region Methods
        public override String ToString()
        {
            return $"{GetType().Name}: {m_HashName}";
        }
        #endregion
    }
}
