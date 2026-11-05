#region Using Directives
using System;
using Xunit;
#endregion

namespace FastHashes.Tests
{
    public sealed class HashAlgorithmTests
    {
        #region Constants
        private const UInt32 TEST_SEED = 0x9E3779B9u;
        #endregion

        #region Methods
        [Theory]
        [MemberData(nameof(HashTestsCases.DataFarmHashUnseeded), MemberType = typeof(HashTestsCases))]
        public void FarmHashUnseededTest(Int32 length)
        {
            RandomXorShift random = new(TEST_SEED);
            Byte[] buffer = new Byte[length];
            random.NextBytes(buffer);

            Hash[] hashes = [new FarmHash32(), new FarmHash64(), new FarmHash128()];

            foreach (Hash hash in hashes)
            {
                Byte[] expected = hash.ComputeHashBytes(buffer);
                Byte[] actual = hash.ComputeHashBytes(buffer);

                Assert.Equal(hash.Length / 8, expected.Length);
                Assert.Equal(expected, actual);
            }
        }
        #endregion
    }
}
