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
        protected override Byte[] ComputeHashInternal(ReadOnlySpan<Byte> buffer)
        {
            return (new Byte[Length / 8]);
        }
        #endregion
    }
}