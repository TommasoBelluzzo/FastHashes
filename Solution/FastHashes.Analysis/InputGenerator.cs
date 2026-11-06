#region Using Directives
using System;
#endregion

namespace FastHashes.Analysis
{
    /// <summary>Deterministic SplitMix64-based test-input generator, independent of the selected hash seed.</summary>
    internal sealed class InputGenerator
    {
        #region Members
        private UInt64 m_State;
        #endregion

        #region Constructors
        public InputGenerator(UInt64 seed)
        {
            m_State = seed;
        }
        #endregion

        #region Methods
        public void NextBytes(Span<Byte> buffer)
        {
            Int32 position = 0;

            while (position < buffer.Length)
            {
                UInt64 value = NextUInt64();

                for (Int32 i = 0; i < 8 && position < buffer.Length; ++i)
                {
                    buffer[position++] = unchecked((Byte)value);
                    value >>= 8;
                }
            }
        }

        private UInt64 NextUInt64()
        {
            unchecked
            {
                UInt64 value = m_State += 0x9E3779B97F4A7C15ul;
                value = (value ^ (value >> 30)) * 0xBF58476D1CE4E5B9ul;
                value = (value ^ (value >> 27)) * 0x94D049BB133111EBul;
                return value ^ (value >> 31);
            }
        }
        #endregion
    }
}
