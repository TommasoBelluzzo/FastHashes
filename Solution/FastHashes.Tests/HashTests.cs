
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
        [Fact]
        public void InputNullTest()
        {
            Hash hash = new FarmHash32();

            Assert.Throws<ArgumentNullException>(() => hash.ComputeHash((Byte[])null));
            Assert.Throws<ArgumentNullException>(() => hash.ComputeHash((Byte[])null, 0, 10));
        }

        [Theory]
        [InlineData(-1, 0, typeof(ArgumentOutOfRangeException))]
        [InlineData(-1, 1, typeof(ArgumentOutOfRangeException))]
        [InlineData(11, 0, typeof(ArgumentOutOfRangeException))]
        [InlineData(Int32.MaxValue, 0, typeof(ArgumentOutOfRangeException))]
        [InlineData(0, -1, typeof(ArgumentOutOfRangeException))]
        [InlineData(0, Int32.MaxValue, typeof(ArgumentException))]
        [InlineData(1, Int32.MaxValue, typeof(ArgumentException))]
        [InlineData(8, 3, typeof(ArgumentException))]
        [InlineData(10, 1, typeof(ArgumentException))]
        public void InputInvalidSliceTest(Int32 offset, Int32 count, Type exceptionType)
        {
            Hash hash = new FarmHash32();
            Byte[] buffer = new Byte[10];

            Exception exception = Record.Exception(() => hash.ComputeHash(buffer, offset, count));

            Assert.NotNull(exception);
            Assert.Equal(exceptionType, exception.GetType());
        }

        [Theory]
        [InlineData(0, 0)]
        [InlineData(0, 1)]
        [InlineData(0, 10)]
        [InlineData(1, 0)]
        [InlineData(1, 1)]
        [InlineData(5, 0)]
        [InlineData(5, 5)]
        [InlineData(9, 0)]
        [InlineData(9, 1)]
        [InlineData(10, 0)]
        public void InputValidSliceTest(Int32 offset, Int32 count)
        {
            Hash hash = new FarmHash32();

            Byte[] buffer = new Byte[10];

            for (Int32 i = 0; i < buffer.Length; ++i)
                buffer[i] = (Byte)i;

            Byte[] slice = new Byte[count];
            Array.Copy(buffer, offset, slice, 0, count);

            Byte[] expected = hash.ComputeHash(slice);
            Byte[] actual = hash.ComputeHash(buffer, offset, count);

            Assert.Equal(expected, actual);
        }

        [Theory]
        [MemberData(nameof(HashTestsCases.DataGeneric), MemberType=typeof(HashTestsCases))]
        public void InputEmptyTest(String hashName, Func<UInt64,Hash> hashInitializer)
        {
            Hash hash = hashInitializer(0u);

            Byte[] buffer = Array.Empty<Byte>();
            Byte[] expected = hash.ComputeHash(buffer);

            Assert.Equal(expected, hash.ComputeHash(buffer, 0));
            Assert.Equal(expected, hash.ComputeHash(buffer, 0, 0));

            ReadOnlySpan<Byte> defaultSpan = default;
            Assert.Equal(expected, hash.ComputeHash(defaultSpan));
        }

        [Theory]
        [MemberData(nameof(HashTestsCases.DataGeneric), MemberType=typeof(HashTestsCases))]
        public void OutputTest(String hashName, Func<UInt64,Hash> hashInitializer)
        {
            Byte[] buffer = new Byte[] { 23, 134, 0, 237, 0, 81, 64, 64, 39, 5 };
            Hash hash = hashInitializer(0u);
            Byte[] expected = hash.ComputeHash(buffer);

            Assert.Equal(expected, hash.ComputeHash(buffer, buffer.Length));
            Assert.Equal(expected, hash.ComputeHash(buffer, 0, buffer.Length));
            Assert.Equal(expected, hash.ComputeHash(new ReadOnlySpan<Byte>(buffer)));

            Byte[] paddedBuffer = new Byte[buffer.Length + 10];
            Array.Copy(buffer, 0, paddedBuffer, 5, buffer.Length);

            Assert.Equal(expected, hash.ComputeHash(paddedBuffer, 5, buffer.Length));
            Assert.Equal(expected, hash.ComputeHash(new ReadOnlySpan<Byte>(paddedBuffer, 5, buffer.Length)));
        }

        [Theory]
        [MemberData(nameof(HashTestsCases.DataLength), MemberType=typeof(HashTestsCases))]
        public void LengthTest(String hashName, Func<UInt64,Hash> hashInitializer, Int32 maximumLength)
        {
            UInt32[] seeds = { 0u, TEST_SEED };

            foreach (UInt32 seed in seeds)
            {
                RandomXorShift random = new RandomXorShift(TEST_SEED);
                Hash hash = hashInitializer(seed);

                Int32 failed = 0;

                for (Int32 i = 0; i <= maximumLength; ++i)
                {
                    Byte[] buffer = new Byte[i];
                    random.NextBytes(buffer);

                    try
                    {
                        hash.ComputeHash(buffer);
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
        [MemberData(nameof(HashTestsCases.DataLengthExtended), MemberType=typeof(HashTestsCases))]
        public void LengthExtendedTest(String hashName, Func<UInt64,Hash> hashInitializer, Int32 length)
        {
            Byte[] buffer = new Byte[length];

            RandomXorShift random = new RandomXorShift(TEST_SEED);
            random.NextBytes(buffer);

            Hash hash = hashInitializer(TEST_SEED);

            Byte[] expected = hash.ComputeHash(buffer);
            Byte[] actual = hash.ComputeHash(buffer);

            Assert.Equal(hash.Length / 8, expected.Length);
            Assert.Equal(expected, actual);
        }

        [Theory]
        [MemberData(nameof(HashTestsCases.DataValidation), MemberType=typeof(HashTestsCases))]
        public void ValidationTest(String hashName, Func<UInt64,Hash> hashInitializer, String expectedValue)
        {
            Hash hash0 = hashInitializer(0u);
            Int32 hashBytes = hash0.Length / 8;

            Byte[] buffer = new Byte[256];
            Byte[] bufferFinal = new Byte[hashBytes * 256];

            for (Int32 i = 0; i < 256; ++i)
            {
                buffer[i] = (Byte)i;

                Hash hashi = hashInitializer((UInt32)(256 - i));
                Byte[] hi = hashi.ComputeHash(buffer, 0, i);

                Buffer.BlockCopy(hi, 0, bufferFinal, i * hashBytes, hashBytes);
            }

            Byte[] h0 = hash0.ComputeHash(bufferFinal);
            String actualValue = String.Concat("0x", BitConverter.ToString(h0).Replace("-", String.Empty));

            Assert.Equal(expectedValue, actualValue);
        }

        [Theory]
        [MemberData(nameof(HashTestsCases.DataGeneric), MemberType=typeof(HashTestsCases))]
        public void CollisionTest(String hashName, Func<UInt64,Hash> hashInitializer)
        {
            Int32 wordsCount = m_CollisionFixture.Words.Count();
            Assert.False(wordsCount == 0, "Fixture Words Empty");

            Hash hash = hashInitializer(TEST_SEED);
            Int32 hashBytes = hash.Length / 8;

            List<Byte[]> hashes = new List<Byte[]>(wordsCount * 6);

            foreach (String word in m_CollisionFixture.Words)
            {
                Byte[] lineBytes = Encoding.UTF8.GetBytes(word);
                Int32 lineBytesLength = lineBytes.Length;

                Byte[] buffer = new Byte[lineBytesLength + 5];
                Array.Fill(buffer, (Byte)'!');
                Buffer.BlockCopy(lineBytes, 0, buffer, 0, lineBytesLength);

                for (Int32 j = 0; j <= 5; ++j)
                    hashes.Add(hash.ComputeHash(buffer, 0, lineBytesLength + j));
            }

            Boolean cte = MathUtilities.CollisionsThresholdExceeded(hashes, hashBytes);

            Assert.False(cte, $"Collisions threshold exceeded for {hashName}, seed 0x{TEST_SEED:X8}.");
        }

        [Theory]
        [MemberData(nameof(HashTestsCases.DataGeneric), MemberType=typeof(HashTestsCases))]
        public void ConformanceTest(String hashName, Func<UInt64,Hash> hashInitializer)
        {
            Int32 total = 0;
            Int32 failed = 0;

            foreach (var (_, _, Seed, Input, Expected, _) in m_ConformanceFixture.GetCases(hashName))
            {
                ++total;

                UInt64 seedValue = UInt64.Parse(Seed.StartsWith("0x", StringComparison.OrdinalIgnoreCase) ? Seed[2..] : Seed, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                Hash hash = hashInitializer(seedValue);

                Byte[] buffer = Convert.FromHexString(Input);
                Byte[] actual = hash.ComputeHash(buffer);
                Byte[] expected = Convert.FromHexString(Expected);

                if ((expected.Length != (hash.Length / 8)) || !actual.AsSpan().SequenceEqual(expected))
                    ++failed;
            }

            Assert.True(failed == 0, $"Conformance test failed for {hashName}: {failed} of {total} vectors failed.");
        }
        #endregion
    }
}