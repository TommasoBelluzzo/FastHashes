#region Using Directives
using System;
#endregion

namespace FastHashes.Benchmarks
{
    internal sealed class DummyHash : Hash<UInt64>
    {
        #region Methods
        protected override UInt64 ComputeHashValueInternal(ReadOnlySpan<Byte> buffer)
        {
            Int32 count = buffer.Length;

            if (count == 0)
                return 0ul;

            return (UInt64)(buffer[0] ^ buffer[count / 2] ^ buffer[count - 1]);
        }
        #endregion
    }
}