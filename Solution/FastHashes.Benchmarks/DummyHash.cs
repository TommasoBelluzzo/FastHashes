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
        #if NETCOREAPP3_1_OR_GREATER
        protected override Byte[] ComputeHashInternal(ReadOnlySpan<Byte> buffer)
        #else
        protected override Byte[] ComputeHashInternal(Byte[] buffer, Int32 offset, Int32 count)
        #endif
        {
            return (new Byte[Length / 8]);
        }
        #endregion
    }
}