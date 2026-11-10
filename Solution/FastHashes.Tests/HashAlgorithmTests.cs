#region Using Directives
using System;
using System.Numerics;
using Xunit;
#endregion

namespace FastHashes.Tests
{
    public sealed class HashAlgorithmTests
    {
        #region Helpers
        private sealed class TestUmash : Umash<UInt64>
        {
            public TestUmash() : base(0ul) { }

            public static UInt64 Evaluate(UInt64 x, UInt64 y)
            {
                return Horner(0ul, 1ul, 1ul, x, y);
            }

            protected override UInt64 ComputeHashValueInternal(ReadOnlySpan<Byte> buffer)
            {
                throw new NotSupportedException();
            }
        }
        #endregion

        #region Tests
        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(4)]
        [InlineData(5)]
        [InlineData(12)]
        [InlineData(13)]
        [InlineData(15)]
        [InlineData(16)]
        [InlineData(17)]
        [InlineData(24)]
        [InlineData(25)]
        [InlineData(32)]
        [InlineData(127)]
        [InlineData(128)]
        [InlineData(144)]
        public void FarmHashUnseededTest(Int32 length)
        {
            RandomXorShift random = new();
            Byte[] buffer = new Byte[length];
            random.NextBytes(buffer);

            Hash[] hashes = [new FarmHash32(), new FarmHash64(), new FarmHash128()];

            foreach (Hash hash in hashes)
            {
                Byte[] hashBytes = hash.ComputeHashBytes(buffer);
                Assert.Equal(hash.Length / 8, hashBytes.Length);
            }
        }

        [Fact]
        public void MumHashZeroWordTest()
        {
            UInt64[] words = [0x9EBDCAE10D981691ul, 0x32B9B9B97A27AC7Dul];

            for (Int32 i = 0; i < 2; ++i)
            {
                Byte[] buffer = new Byte[65];
                UInt64 word = words[i];

                for (Int32 j = 0; j < sizeof(UInt64); ++j)
                    buffer[(i * sizeof(UInt64)) + j] = unchecked((Byte)(word >> (j * 8)));

                MumHash hash = new();

                UInt64 expected = hash.ComputeHashValue(new Byte[buffer.Length]);
                UInt64 actual = hash.ComputeHashValue(buffer);

                Assert.Equal(expected, actual);
            }
        }

        [Theory]
        [InlineData(0ul, 2ul)]
        [InlineData(0ul, UInt64.MaxValue - 8ul)]
        [InlineData(0ul, UInt64.MaxValue)]
        [InlineData(UInt64.MaxValue, UInt64.MaxValue)]
        [InlineData(UInt64.MaxValue, UInt64.MaxValue - 7ul)]
        public void UmashHornerReductionTest(UInt64 x, UInt64 y)
        {
            BigInteger modulus = (BigInteger.One << 64) - 8;
            UInt64 expected = (UInt64)(((BigInteger)x + y) % modulus);
            UInt64 actual = TestUmash.Evaluate(x, y);

            Assert.Equal(expected, actual);
        }
        #endregion
    }
}
