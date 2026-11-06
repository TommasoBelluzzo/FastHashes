#region Using Directives
using System;
#endregion

namespace FastHashes.Benchmarks
{
    internal sealed class DummyHash : Hash<UInt64>
    {
        #region Methods
        protected override UInt64 ComputeHashValueInternal(ReadOnlySpan<byte> buffer)
        {
            return 0ul;
        }
        #endregion
    }
}