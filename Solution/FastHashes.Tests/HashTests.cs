
#region Using Directives
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;
#endregion

namespace FastHashes.Tests
{
    public sealed class HashTests : IClassFixture<Fixture>
    {
        #region Constants
        private const UInt32 TEST_SEED = 0x9E3779B9u;
        #endregion

        #region Members
        private readonly Fixture m_Fixture;
        private readonly ITestOutputHelper m_Output;
        #endregion

        #region Constructors
        public HashTests(Fixture fixture, ITestOutputHelper output)
        {
            m_Fixture = fixture;
            m_Output = output;
        }
        #endregion

        #region Methods
        [Fact]
        public void ExceptionTest()
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
        public void SliceInvalidArgumentsTest(Int32 offset, Int32 count, Type exceptionType)
        {
            Hash hash = new FarmHash32();
            Byte[] buffer = new Byte[10];

            Exception exception = Record.Exception(() => hash.ComputeHash(buffer, offset, count));

            Assert.NotNull(exception);
            Assert.Equal(exceptionType, exception.GetType());
        }

        [Theory]
        [MemberData(nameof(HashTestsCases.DataOutput), MemberType=typeof(HashTestsCases))]
        public void EmptyInputTest(String hashName, Func<UInt32,Hash> hashInitializer)
        {
            Hash hash = hashInitializer(0u);

            Byte[] buffer = Array.Empty<Byte>();
            Byte[] expected = hash.ComputeHash(buffer);

            Assert.Equal(expected, hash.ComputeHash(buffer, 0));
            Assert.Equal(expected, hash.ComputeHash(buffer, 0, 0));
            Assert.Equal(expected, hash.ComputeHash(ReadOnlySpan<Byte>.Empty));

            ReadOnlySpan<Byte> defaultSpan = default;
            Assert.Equal(expected, hash.ComputeHash(defaultSpan));

            m_Output.WriteLine($"NAME: {hashName}");
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
        public void SliceBoundaryTest(Int32 offset, Int32 count)
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
        [MemberData(nameof(HashTestsCases.DataOutput), MemberType=typeof(HashTestsCases))]
        public void OutputTest(String hashName, Func<UInt32,Hash> hashInitializer)
        {
            Hash hash = hashInitializer(0u);

            Byte[] buffer = new Byte[] { 23, 134, 0, 237, 0, 81, 64, 64, 39, 5 };

            Byte[] expected = hash.ComputeHash(buffer);

            Assert.Equal(expected, hash.ComputeHash(buffer, buffer.Length));
            Assert.Equal(expected, hash.ComputeHash(buffer, 0, buffer.Length));
            Assert.Equal(expected, hash.ComputeHash(new ReadOnlySpan<Byte>(buffer)));

            Byte[] paddedBuffer = new Byte[buffer.Length + 10];
            Array.Copy(buffer, 0, paddedBuffer, 5, buffer.Length);

            Assert.Equal(expected, hash.ComputeHash(paddedBuffer, 5, buffer.Length));
            Assert.Equal(expected, hash.ComputeHash(new ReadOnlySpan<Byte>(paddedBuffer, 5, buffer.Length)));

            m_Output.WriteLine($"NAME: {hashName}");
        }

        [Theory]
        [MemberData(nameof(HashTestsCases.DataLength), MemberType=typeof(HashTestsCases))]
        public void LengthTest(Type hashType, Func<UInt32,Hash> hashInitializer, Int32 maximumLength)
        {
            RandomXorShift random = new RandomXorShift(TEST_SEED);

            Hash hash = (Hash)Activator.CreateInstance(hashType);
            List<String> errorLengths = new List<String>();

            for (Int32 i = 0; i <= maximumLength; ++i)
            {
                Byte[] buffer = new Byte[i];
                random.NextBytes(buffer);

                try
                {
                    hash.ComputeHash(buffer);
                }
                catch (Exception ex)
                {
                    errorLengths.Add($"{i}: {ex.GetType().Name} - {ex.Message}");
                }
            }

            m_Output.WriteLine($"NAME: {hashType.Name}");

            if (errorLengths.Count > 0)
                m_Output.WriteLine($"PLAIN ERROR LENGTHS: {String.Join(", ", errorLengths)}");
            else
                m_Output.WriteLine("NO PLAIN ERROR LENGTHS");

            Assert.True(errorLengths.Count == 0);

            errorLengths.Clear();

            random = new RandomXorShift(TEST_SEED);
            hash = hashInitializer(TEST_SEED);

            for (Int32 i = 0; i <= maximumLength; ++i)
            {
                Byte[] buffer = new Byte[i];
                random.NextBytes(buffer);

                try
                {
                    hash.ComputeHash(buffer);
                }
                catch (Exception ex)
                {
                    errorLengths.Add($"{i}: {ex.GetType().Name} - {ex.Message}");
                }
            }

            m_Output.WriteLine($"NAME: {hashType.Name}");
            m_Output.WriteLine($"SEED: 0x{TEST_SEED:X8}");

            if (errorLengths.Count > 0)
                m_Output.WriteLine($"SEED ERROR LENGTHS: {String.Join(", ", errorLengths)}");
            else
                m_Output.WriteLine("NO SEED ERROR LENGTHS");

            Assert.True(errorLengths.Count == 0);
        }

        [Theory]
        [MemberData(nameof(HashTestsCases.DataLengthExtended), MemberType=typeof(HashTestsCases))]
        public void ExtendedLengthTest(String hashName, Func<UInt32,Hash> hashInitializer, Int32 length)
        {
            Byte[] buffer = new Byte[length];

            RandomXorShift random = new RandomXorShift(TEST_SEED);
            random.NextBytes(buffer);

            Hash hash = hashInitializer(TEST_SEED);

            Byte[] expected = hash.ComputeHash(buffer);
            Byte[] actual = hash.ComputeHash(buffer);

            Boolean validLength = (expected.Length == (hash.Length / 8));
            Boolean validOutput = expected.SequenceEqual(actual);

            m_Output.WriteLine($"NAME: {hashName}");
            m_Output.WriteLine($"SEED: 0x{TEST_SEED:X8}");
            m_Output.WriteLine($"INPUT LENGTH: {length}");
            m_Output.WriteLine($"OUTPUT LENGTH: {expected.Length}");
            m_Output.WriteLine($"EXPECTED LENGTH: {hash.Length / 8}");
            m_Output.WriteLine($"RESULT: {(validLength && validOutput ? "SUCCESS" : "FAILURE")}");

            Assert.True(validLength, "Invalid hash output length.");
            Assert.True(validOutput, "Inconsistent hash output.");
        }

        [Theory]
        [MemberData(nameof(HashTestsCases.DataValidation), MemberType=typeof(HashTestsCases))]
        public void ValidationTest(String hashName, Func<UInt32,Hash> hashInitializer, String expectedValue)
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

            m_Output.WriteLine($"NAME: {hashName}");
            m_Output.WriteLine($"EXPECTED: {expectedValue}");
            m_Output.WriteLine($"ACTUAL: {actualValue}");

            Assert.Equal(expectedValue, actualValue);
        }

        [Theory]
        [MemberData(nameof(HashTestsCases.DataCollision), MemberType=typeof(HashTestsCases))]
        public void CollisionTest(String hashName, Func<UInt32,Hash> hashInitializer)
        {
            Int32 wordsCount = m_Fixture.Words.Count();
            Assert.False(wordsCount == 0, "Fixture Words Empty");

            Hash hash = hashInitializer(TEST_SEED);
            Int32 hashBytes = hash.Length / 8;

            Byte filler = Convert.ToByte('!');
            List<(String,Byte[])> hashes = new List<(String,Byte[])>(wordsCount * 5);

            foreach (String word in m_Fixture.Words)
            {
                Byte[] lineBytes = Encoding.UTF8.GetBytes(word);
                Int32 lineBytesLength = lineBytes.Length;

                Byte[] buffer = new Byte[lineBytes.Length + 5];
                Int32 bufferLength = buffer.Length;

                for (Int32 i = 0; i < bufferLength; ++i)
                    buffer[i] = filler;

                Buffer.BlockCopy(lineBytes, 0, buffer, 0, lineBytesLength);

                for (Int32 j = 0; j <= 5; ++j)
                    hashes.Add((Encoding.UTF8.GetString(buffer, 0, lineBytesLength + j), hash.ComputeHash(buffer, 0, lineBytesLength + j)));
            }

            Boolean cte = MathUtilities.CollisionsThresholdExceeded(hashes, hashBytes);

            m_Output.WriteLine($"NAME: {hashName}");
            m_Output.WriteLine($"SEED: 0x{TEST_SEED:X8}");
            m_Output.WriteLine($"RESULT: {(cte ? "FAILURE" : "SUCCESS")}");

            Assert.False(cte, "Collisions Threshold Exceeded");
        }
        #endregion
    }
}