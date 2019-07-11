#region Using Directives
using System;
#endregion

namespace FastHashes.Benchmarks
{
    public abstract class DummyHash : Hash
    {
        #region Methods
        protected override Byte[] ComputeHashInternal(Byte[] buffer, Int32 offset, Int32 count)
        {
            return (new Byte[Length / 8]);
        }
        #endregion
    }

    public sealed class DummyHash32 : DummyHash
    {
        #region Properties
        public override Int32 Length => 32;
        #endregion
    }

    public sealed class DummyHash64 : DummyHash
    {
        #region Properties
        public override Int32 Length => 64;
        #endregion
    }

    public sealed class DummyHash128 : DummyHash
    {
        #region Properties
        public override Int32 Length => 128;
        #endregion
    }

    public sealed class DummyHash256 : DummyHash
    {
        #region Properties
        public override Int32 Length => 256;
        #endregion
    }
}