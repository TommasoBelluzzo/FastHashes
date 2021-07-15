#region Using Directives
using System;
#endregion

namespace FastHashes.Tests
{
    public sealed class TestCase
    {
        #region Members
        private readonly Func<UInt32, Hash> m_HashInitializer;
        private readonly String m_HashName;
        private readonly UInt32 m_HashValue;
        #endregion

        #region Properties
        public Func<UInt32,Hash> HashInitializer => m_HashInitializer;
        public String HashName => m_HashName;
        public UInt32 HashValue => m_HashValue;
        #endregion

        #region Constructors
        public TestCase(String hashName, Func<UInt32,Hash> hashInitializer, UInt32 hashValue)
        {
            if (String.IsNullOrWhiteSpace(hashName))
                throw new ArgumentException("Invalid hash name specified.", nameof(hashName));

            if (hashInitializer == null)
                throw new ArgumentException("Invalid hash initializer specified.", nameof(hashInitializer));

            if (hashValue == 0u)
                throw new ArgumentException("Invalid hash value specified.", nameof(hashValue));

            m_HashInitializer = hashInitializer;
            m_HashName = hashName;
            m_HashValue = hashValue;
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
