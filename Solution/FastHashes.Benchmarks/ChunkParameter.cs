#region Using Directives
using System;
#endregion

namespace FastHashes.Benchmarks
{
    public sealed class ChunkParameter
    {
        #region Members
        private readonly Func<Int32,Int32> m_Increment;
        private readonly Int32 m_KeySize;
        private readonly Int32 m_Repetitions;
        #endregion

        #region Properties
        public Func<Int32,Int32> Increment => m_Increment;
        public Int32 KeySize => m_KeySize;
        public Int32 Repetitions => m_Repetitions;
        #endregion

        #region Constructors
        public ChunkParameter(Func<Int32,Int32> increment, Int32 keySize, Int32 repetitions)
        {
            if (increment == null)
                throw new ArgumentException("Invalid increment specified.", nameof(increment));

            if (keySize == 0)
                throw new ArgumentException("Invalid key size specified.", nameof(keySize));

            if (repetitions == 0)
                throw new ArgumentException("Invalid repetitions specified.", nameof(repetitions));

            m_Increment = increment;
            m_KeySize = keySize;
            m_Repetitions = repetitions;
        }
        #endregion

        #region Methods
        public override String ToString()
        {
            return $"{GetType().Name}: {nameof(KeySize)}={m_KeySize} {nameof(Repetitions)}={m_Repetitions}";
        }
        #endregion
    }
}
