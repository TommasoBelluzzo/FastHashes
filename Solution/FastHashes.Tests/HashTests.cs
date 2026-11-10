#region Using Directives
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Xunit;
#endregion

namespace FastHashes.Tests
{
    public sealed class HashTests : IClassFixture<ConformanceFixture>
    {
        #region Constants
        private const Int32 MAXIMUM_LENGTH = 1024;
        private const UInt32 TEST_SEED = 0x9E3779B9u;
        #endregion

        #region Members
        private readonly ConformanceFixture m_ConformanceFixture;
        #endregion

        #region Constructors
        public HashTests(ConformanceFixture conformanceFixture)
        {
            m_ConformanceFixture = conformanceFixture;
        }
        #endregion

        #region Tests: Length
        [Theory]
        [MemberData(nameof(TestsCases.Hashes), MemberType = typeof(TestsCases))]
        public void LengthBoundaryTest(String hashName, Func<UInt64, Hash> hashInitializer, String _)
        {
            Int32[] lengths = [0, 15, 16, 17, 31, 32, 33, 511, 512, 513, 1023, 1024, 1025, 4095, 4096, 4097, 16383, 16384, 16385, 65535, 65536, 65537];

            RandomXorShift random = new(TEST_SEED);
            Hash hash = hashInitializer(TEST_SEED);

            foreach (Int32 length in lengths)
            {
                Byte[] buffer = new Byte[length];
                random.NextBytes(buffer);

                Byte[] bytes = hash.ComputeHashBytes(buffer);

                Assert.Equal(hash.Length / 8, bytes.Length);
            }
        }

        [Theory]
        [MemberData(nameof(TestsCases.Hashes), MemberType = typeof(TestsCases))]
        public void LengthRobustnessTest(String hashName, Func<UInt64, Hash> hashInitializer, String _)
        {
            UInt32[] seeds = [0u, TEST_SEED];

            foreach (UInt32 seed in seeds)
            {
                RandomXorShift random = new(TEST_SEED);
                Hash hash = hashInitializer(seed);

                Int32 failed = 0;

                for (Int32 i = 0; i <= MAXIMUM_LENGTH; ++i)
                {
                    Byte[] buffer = new Byte[i];
                    random.NextBytes(buffer);

                    try
                    {
                        hash.ComputeHashBytes(buffer);
                    }
                    catch
                    {
                        ++failed;
                    }
                }

                Assert.True(failed == 0, $"Length test failed for {hashName}, seed 0x{seed:X8}: {failed} of {MAXIMUM_LENGTH + 1} lengths failed.");
            }
        }
        #endregion

        #region Tests: Correctness
        [Theory]
        [MemberData(nameof(TestsCases.Hashes), MemberType = typeof(TestsCases))]
        public void ValidationTest(String hashName, Func<UInt64, Hash> hashInitializer, String expected)
        {
            Hash hash0 = hashInitializer(0u);
            Int32 hashBytes = hash0.Length / 8;

            Byte[] buffer = new Byte[256];
            Byte[] bufferFinal = new Byte[hashBytes * 256];

            for (Int32 i = 0; i < 256; ++i)
            {
                buffer[i] = (Byte)i;

                Hash hashi = hashInitializer((UInt32)(256 - i));
                Byte[] hi = hashi.ComputeHashBytes(buffer, 0, i);

                Buffer.BlockCopy(hi, 0, bufferFinal, i * hashBytes, hashBytes);
            }

            String actual = hash0.ComputeHashString(bufferFinal, HashFormat.HexadecimalPrefixed);

            Assert.Equal(expected, actual);
        }

        [Theory]
        [MemberData(nameof(TestsCases.Hashes), MemberType = typeof(TestsCases))]
        public void ConformanceTest(String hashName, Func<UInt64, Hash> hashInitializer, String _)
        {
            IEnumerable<ConformanceCase> cases = m_ConformanceFixture.GetCases(hashName);

            Int32 total = 0;
            Int32 failed = 0;

            foreach (var (_, _, Seed, Input, Expected, _) in cases)
            {
                ++total;

                UInt64 seedValue = UInt64.Parse(Seed.StartsWith("0x", StringComparison.OrdinalIgnoreCase) ? Seed[2..] : Seed, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                Hash hash = hashInitializer(seedValue);

                Byte[] buffer = Convert.FromHexString(Input);
                Byte[] actual = hash.ComputeHashBytes(buffer);
                Byte[] expected = Convert.FromHexString(Expected);

                if ((expected.Length != (hash.Length / 8)) || !actual.AsSpan().SequenceEqual(expected))
                    ++failed;
            }

            Assert.True(failed == 0, $"Conformance test failed for {hashName}: {failed} of {total} cases failed.");
        }
        #endregion
    }
}
