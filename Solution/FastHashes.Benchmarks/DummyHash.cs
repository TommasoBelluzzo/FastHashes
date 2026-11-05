#region Using Directives
using System;
#endregion

namespace FastHashes.Benchmarks
{
    public sealed class DummyHash : Hash
    {
        #region Properties
        public override Int32 Length => 32;
        #endregion

        #region Methods
        protected override void ComputeHashInternal(ReadOnlySpan<Byte> buffer, Span<Byte> destination)
        {
            destination.Clear();
        }
        #endregion
    }
}