#region Using Directives
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Xunit;
#endregion

namespace FastHashes.Tests
{
    public sealed class HashTests : IClassFixture<ConformanceFixture>, IClassFixture<CollisionFixture>
    {
        #region Constants
        private const UInt32 TEST_SEED = 0x9E3779B9u;
        #endregion

        #region Members
        private readonly CollisionFixture m_CollisionFixture;
        private readonly ConformanceFixture m_ConformanceFixture;
        #endregion

        #region Constructors
        public HashTests(ConformanceFixture conformanceFixture, CollisionFixture collisionFixture)
        {
            m_CollisionFixture = collisionFixture;
            m_ConformanceFixture = conformanceFixture;
        }
        #endregion

        #region Methods
        [Theory]
        [MemberData(nameof(HashTestsCases.DataHashesLength), MemberType = typeof(HashTestsCases))]
        public void LengthTest(String hashName, Func<UInt64, Hash> hashInitializer, Int32 maximumLength)
        {
            UInt32[] seeds = [0u, TEST_SEED];

            foreach (UInt32 seed in seeds)
            {
                RandomXorShift random = new(TEST_SEED);
                Hash hash = hashInitializer(seed);

                Int32 failed = 0;

                for (Int32 i = 0; i <= maximumLength; ++i)
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

                Assert.True(failed == 0, $"Length test failed for {hashName}, seed 0x{seed:X8}: {failed} of {maximumLength + 1} lengths failed.");
            }
        }

        [Theory]
        [MemberData(nameof(HashTestsCases.DataHashesLengthExtended), MemberType = typeof(HashTestsCases))]
        public void LengthExtendedTest(String hashName, Func<UInt64, Hash> hashInitializer, Int32 length)
        {
            Byte[] buffer = new Byte[length];

            RandomXorShift random = new(TEST_SEED);
            random.NextBytes(buffer);

            Hash hash = hashInitializer(TEST_SEED);

            Byte[] expected = hash.ComputeHashBytes(buffer);
            Byte[] actual = hash.ComputeHashBytes(buffer);

            Assert.Equal(hash.Length / 8, expected.Length);
            Assert.Equal(expected, actual);
        }

        [Theory]
        [MemberData(nameof(HashTestsCases.DataHashesValidation), MemberType = typeof(HashTestsCases))]
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
        [MemberData(nameof(HashTestsCases.DataHashesGeneric), MemberType = typeof(HashTestsCases))]
        public void CollisionTest(String hashName, Func<UInt64, Hash> hashInitializer)
        {
            Int32 wordsCount = m_CollisionFixture.Words.Count();
            Assert.False(wordsCount == 0, "Fixture contains no words.");

            Hash hash = hashInitializer(TEST_SEED);
            Int32 hashBytes = hash.Length / 8;

            List<Byte[]> hashes = new(wordsCount * 6);

            foreach (String word in m_CollisionFixture.Words)
            {
                Byte[] lineBytes = Encoding.UTF8.GetBytes(word);
                Int32 lineBytesLength = lineBytes.Length;

                Byte[] buffer = new Byte[lineBytesLength + 5];
                Array.Fill(buffer, (Byte)'!');
                Buffer.BlockCopy(lineBytes, 0, buffer, 0, lineBytesLength);

                for (Int32 j = 0; j <= 5; ++j)
                    hashes.Add(hash.ComputeHashBytes(buffer, 0, lineBytesLength + j));
            }

            Boolean cte = MathUtilities.CollisionsThresholdExceeded(hashes, hashBytes);

            Assert.False(cte, $"Collisions threshold exceeded for {hashName}, seed 0x{TEST_SEED:X8}.");
        }

        [Theory]
        [MemberData(nameof(HashTestsCases.DataHashesGeneric), MemberType = typeof(HashTestsCases))]
        public void ConformanceTest(String hashName, Func<UInt64, Hash> hashInitializer)
        {
            var cases = m_ConformanceFixture.GetCases(hashName);

            Assert.SkipWhen(!cases.Any(), $"No reference vectors available for {hashName}.");

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

            Assert.True(failed == 0, $"Conformance test failed for {hashName}: {failed} of {total} vectors failed.");
        }
        #endregion
    }
}
