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

            Assert.Throws<ArgumentNullException>(() => hash.ComputeHashBytes((Byte[])null));
            Assert.Throws<ArgumentNullException>(() => hash.ComputeHashBytes((Byte[])null, 0, 10));
        }

        [Theory]
        [MemberData(nameof(HashTestsCases.DataSliceInvalid), MemberType = typeof(HashTestsCases))]
        public void InputSliceInvalidTest(Int32 offset, Int32 count, Type exceptionType)
        {
            Hash hash = new FarmHash32();
            Byte[] buffer = new Byte[10];

            Exception exception = Record.Exception(() => hash.ComputeHashBytes(buffer, offset, count));

            Assert.NotNull(exception);
            Assert.Equal(exceptionType, exception.GetType());
        }

        [Theory]
        [MemberData(nameof(HashTestsCases.DataSliceValid), MemberType = typeof(HashTestsCases))]
        public void InputSliceValidTest(Int32 offset, Int32 count)
        {
            Hash hash = new FarmHash32();

            Byte[] buffer = new Byte[10];

            for (Int32 i = 0; i < buffer.Length; ++i)
                buffer[i] = (Byte)i;

            Byte[] slice = new Byte[count];
            Array.Copy(buffer, offset, slice, 0, count);

            Byte[] expected = hash.ComputeHashBytes(slice);
            Byte[] actual = hash.ComputeHashBytes(buffer, offset, count);

            Assert.Equal(expected, actual);
        }

        [Theory]
        [MemberData(nameof(HashTestsCases.DataHashesGeneric), MemberType = typeof(HashTestsCases))]
        public void InputEmptyTest(String hashName, Func<UInt64, Hash> hashInitializer)
        {
            Hash hash = hashInitializer(0u);

            Byte[] buffer = [];
            Byte[] expected = hash.ComputeHashBytes(buffer);

            Assert.Equal(expected, hash.ComputeHashBytes(buffer, 0));
            Assert.Equal(expected, hash.ComputeHashBytes(buffer, 0, 0));

            ReadOnlySpan<Byte> defaultSpan = default;
            Assert.Equal(expected, hash.ComputeHashBytes(defaultSpan));
        }

        [Theory]
        [MemberData(nameof(HashTestsCases.DataHashesGeneric), MemberType = typeof(HashTestsCases))]
        public void InputStringTest(String hashName, Func<UInt64, Hash> hashInitializer)
        {
            Hash hash = hashInitializer(TEST_SEED);

            String[] texts = [String.Empty, "Hello World", "你好世界", "🚀"];
            Encoding[] encodings = [Encoding.UTF8, Encoding.Unicode, Encoding.BigEndianUnicode];

            Byte[] expected, actual;

            foreach (String text in texts)
            {
                expected = hash.ComputeHashBytes(Encoding.UTF8.GetBytes(text));
                actual = hash.ComputeHashBytes(text);

                Assert.Equal(expected, actual);

                foreach (Encoding encoding in encodings)
                {
                    expected = hash.ComputeHashBytes(encoding.GetBytes(text));
                    actual = hash.ComputeHashBytes(text, encoding);

                    Assert.Equal(expected, actual);
                }
            }
        }

        [Theory]
        [MemberData(nameof(HashTestsCases.DataHashesGeneric), MemberType = typeof(HashTestsCases))]
        public void InputStringFormattedTest(String hashName, Func<UInt64, Hash> hashInitializer)
        {
            Hash hash = hashInitializer(TEST_SEED);

            String text = "Hello 世界 🚀";
            Encoding encoding = Encoding.Unicode;

            String expected, actual;

            foreach (HashFormat format in Enum.GetValues<HashFormat>())
            {
                expected = hash.ComputeHashString(Encoding.UTF8.GetBytes(text), format);
                actual = hash.ComputeHashString(text, format);
                Assert.Equal(expected, actual);

                expected = hash.ComputeHashString(encoding.GetBytes(text), format);
                actual = hash.ComputeHashString(text, encoding, format);
                Assert.Equal(expected, actual);
            }
        }

        [Theory]
        [MemberData(nameof(HashTestsCases.DataHashesGeneric), MemberType = typeof(HashTestsCases))]
        public void OutputTest(String hashName, Func<UInt64, Hash> hashInitializer)
        {
            Byte[] buffer = [23, 134, 0, 237, 0, 81, 64, 64, 39, 5];
            Hash hash = hashInitializer(0u);
            Byte[] expected = hash.ComputeHashBytes(buffer);

            Assert.Equal(expected, hash.ComputeHashBytes(buffer, buffer.Length));
            Assert.Equal(expected, hash.ComputeHashBytes(buffer, 0, buffer.Length));
            Assert.Equal(expected, hash.ComputeHashBytes(new ReadOnlySpan<Byte>(buffer)));

            Byte[] paddedBuffer = new Byte[buffer.Length + 10];
            Array.Copy(buffer, 0, paddedBuffer, 5, buffer.Length);

            Assert.Equal(expected, hash.ComputeHashBytes(paddedBuffer, 5, buffer.Length));
            Assert.Equal(expected, hash.ComputeHashBytes(new ReadOnlySpan<Byte>(paddedBuffer, 5, buffer.Length)));
        }

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

        [Theory]
        [MemberData(nameof(HashTestsCases.DataFormatInvalid), MemberType = typeof(HashTestsCases))]
        public void FormatInvalidTest(Byte[] hash, HashFormat format, Type exceptionType, String parameterName)
        {
            Exception exception = Record.Exception(() => HashFormatter.Format(hash, format));

            Assert.NotNull(exception);
            Assert.Equal(exceptionType, exception.GetType());
            Assert.Equal(parameterName, ((ArgumentException)exception).ParamName);

            if (hash != null)
            {
                exception = Record.Exception(() => HashFormatter.Format(new ReadOnlySpan<Byte>(hash), format));

                Assert.NotNull(exception);
                Assert.Equal(exceptionType, exception.GetType());
                Assert.Equal(parameterName, ((ArgumentException)exception).ParamName);
            }
        }

        [Theory]
        [MemberData(nameof(HashTestsCases.DataFormatValid), MemberType = typeof(HashTestsCases))]
        public void FormatValidTest(Byte[] hash, HashFormat format, String expected)
        {
            Assert.Equal(expected, HashFormatter.Format(hash, format));
            Assert.Equal(expected, HashFormatter.Format(new ReadOnlySpan<Byte>(hash), format));
        }

        [Fact]
        public void RegistryTest()
        {
            IReadOnlyList<String> algorithms = Hash.GetAlgorithms();

            Assert.NotEmpty(algorithms);
            Assert.Equal(algorithms.Count, algorithms.Distinct(StringComparer.Ordinal).Count());

            Byte[] buffer = [];

            foreach (String name in algorithms)
            {
                Hash hashDefault = Hash.Create(name);
                Hash hashSeeded = Hash.Create(name, TEST_SEED);

                Hash hashInitializedDefault = Hash.GetInitializerDefault(name)();
                Hash hashInitializedSeeded = Hash.GetInitializerSeeded(name)(TEST_SEED);

                Assert.Equal(hashDefault.GetType(), hashInitializedDefault.GetType());
                Assert.Equal(hashSeeded.GetType(), hashInitializedSeeded.GetType());

                Assert.Equal(hashDefault.ComputeHashBytes(buffer), hashInitializedDefault.ComputeHashBytes(buffer));
                Assert.Equal(hashSeeded.ComputeHashBytes(buffer), hashInitializedSeeded.ComputeHashBytes(buffer));

                Assert.True(Hash.TryCreate(name, out Hash hash1));
                Assert.True(Hash.TryCreate(name, TEST_SEED, out Hash hash2));

                Assert.Equal(hashDefault.ComputeHashBytes(buffer), hash1.ComputeHashBytes(buffer));
                Assert.Equal(hashSeeded.ComputeHashBytes(buffer), hash2.ComputeHashBytes(buffer));
            }
        }


        [Fact]
        public void RegistryInvalidTest()
        {
            Assert.Throws<ArgumentNullException>(() => Hash.Create(null));
            Assert.Throws<ArgumentNullException>(() => Hash.Create(null, TEST_SEED));
            Assert.Throws<ArgumentNullException>(() => Hash.TryCreate(null, out _));
            Assert.Throws<ArgumentNullException>(() => Hash.TryCreate(null, TEST_SEED, out _));
            Assert.Throws<ArgumentNullException>(() => Hash.GetInitializerDefault(null));
            Assert.Throws<ArgumentNullException>(() => Hash.GetInitializerSeeded(null));

            Assert.Throws<ArgumentException>(() => Hash.Create(String.Empty));
            Assert.Throws<ArgumentException>(() => Hash.Create(String.Empty, TEST_SEED));
            Assert.Throws<ArgumentException>(() => Hash.GetInitializerDefault(String.Empty));
            Assert.Throws<ArgumentException>(() => Hash.GetInitializerSeeded(String.Empty));

            Assert.False(Hash.TryCreate(String.Empty, out Hash hash1));
            Assert.Null(hash1);

            Assert.False(Hash.TryCreate(String.Empty, TEST_SEED, out Hash hash2));
            Assert.Null(hash2);

            Assert.Throws<ArgumentException>(() => Hash.Create("UnknownAlgorithm"));
            Assert.Throws<ArgumentException>(() => Hash.Create("UnknownAlgorithm", TEST_SEED));
            Assert.Throws<ArgumentException>(() => Hash.GetInitializerDefault("UnknownAlgorithm"));
            Assert.Throws<ArgumentException>(() => Hash.GetInitializerSeeded("UnknownAlgorithm"));

            Assert.False(Hash.TryCreate("UnknownAlgorithm", out Hash hash3));
            Assert.Null(hash3);

            Assert.False(Hash.TryCreate("UnknownAlgorithm", TEST_SEED, out Hash hash4));
            Assert.Null(hash4);

            Assert.Throws<OverflowException>(() => Hash.Create("XxHash32", UInt64.MaxValue));
            Assert.Throws<OverflowException>(() => Hash.TryCreate("XxHash32", UInt64.MaxValue, out _));
            Assert.Throws<OverflowException>(() => Hash.GetInitializerSeeded("XxHash32")(UInt64.MaxValue));
        }

        [Fact]
        public void RegistryCoverageTest()
        {
            IReadOnlyList<String> registered = Hash.GetAlgorithms();
            String[] validated = [.. HashTestsCases.DataHashesValidation().Select(x => (String)x[0])];

            Assert.Equal(validated.Length, validated.Distinct(StringComparer.Ordinal).Count());
            Assert.Equal(registered.OrderBy(x => x, StringComparer.Ordinal), validated.OrderBy(x => x, StringComparer.Ordinal));
        }

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