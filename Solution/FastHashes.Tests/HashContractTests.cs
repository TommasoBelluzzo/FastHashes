#region Using Directives
using System;
using System.Text;
using Xunit;
#endregion

namespace FastHashes.Tests
{
    public sealed class HashContractTests
    {
        #region Constants
        private const UInt32 TEST_SEED = 0x9E3779B9u;
        #endregion

        #region Helpers
        private sealed class TestHash32 : Hash<UInt32>
        {
            protected override UInt32 ComputeHashValueInternal(ReadOnlySpan<Byte> buffer)
            {
                return 0x12345678u;
            }
        }

        private sealed class TestHash64 : Hash<UInt64>
        {
            protected override UInt64 ComputeHashValueInternal(ReadOnlySpan<Byte> buffer)
            {
                return 0x0123456789ABCDEFul;
            }
        }

        private sealed class TestHash128 : Hash<(UInt64, UInt64)>
        {
            protected override (UInt64, UInt64) ComputeHashValueInternal(ReadOnlySpan<Byte> buffer)
            {
                return (0x0123456789ABCDEFul, 0x1020304050607080ul);
            }
        }

        private sealed class TestHash256 : Hash<(UInt64, UInt64, UInt64, UInt64)>
        {
            protected override (UInt64, UInt64, UInt64, UInt64) ComputeHashValueInternal(ReadOnlySpan<Byte> buffer)
            {
                return (0x0123456789ABCDEFul, 0x1020304050607080ul, 0xFFEEDDCCBBAA9988ul, 0x8877665544332211ul);
            }
        }

        private sealed class TestHashInvalid : Hash<Guid>
        {
            protected override Guid ComputeHashValueInternal(ReadOnlySpan<Byte> buffer)
            {
                return Guid.Empty;
            }
        }
        #endregion

        #region Tests: Input Operations
        [Fact]
        public void InputExceptionTest()
        {
            Hash hash = new FarmHash32();

            Assert.Throws<ArgumentNullException>(() => hash.ComputeHashBytes((Byte[])null));
            Assert.Throws<ArgumentNullException>(() => hash.ComputeHashBytes((Byte[])null, 0));
            Assert.Throws<ArgumentNullException>(() => hash.ComputeHashBytes((Byte[])null, 0, 10));
            Assert.Throws<ArgumentNullException>(() => hash.ComputeHashBytes((String)null));
            Assert.Throws<ArgumentNullException>(() => hash.ComputeHashBytes((String)null, Encoding.UTF8));
            Assert.Throws<ArgumentNullException>(() => hash.ComputeHashBytes("text", null));

            Assert.Throws<ArgumentNullException>(() => hash.ComputeHashString((Byte[])null, HashFormat.Hexadecimal));
            Assert.Throws<ArgumentNullException>(() => hash.ComputeHashString((Byte[])null, 0, HashFormat.Hexadecimal));
            Assert.Throws<ArgumentNullException>(() => hash.ComputeHashString((Byte[])null, 0, 0, HashFormat.Hexadecimal));
            Assert.Throws<ArgumentNullException>(() => hash.ComputeHashString((String)null, HashFormat.Hexadecimal));
            Assert.Throws<ArgumentNullException>(() => hash.ComputeHashString((String)null, Encoding.UTF8, HashFormat.Hexadecimal));
            Assert.Throws<ArgumentNullException>(() => hash.ComputeHashString("text", null, HashFormat.Hexadecimal));

            Byte[] buffer = new Byte[10];
            Assert.Throws<ArgumentException>(() => hash.ComputeHashString(buffer, 11, HashFormat.Hexadecimal));
            Assert.Throws<ArgumentException>(() => hash.ComputeHashString(buffer, 8, 3, HashFormat.Hexadecimal));
            Assert.Throws<ArgumentOutOfRangeException>(() => hash.ComputeHashString(buffer, -1, HashFormat.Hexadecimal));
            Assert.Throws<ArgumentOutOfRangeException>(() => hash.ComputeHashString(buffer, -1, 0, HashFormat.Hexadecimal));
            Assert.Throws<ArgumentOutOfRangeException>(() => hash.ComputeHashString(buffer, 11, 0, HashFormat.Hexadecimal));
            Assert.Throws<ArgumentOutOfRangeException>(() => hash.ComputeHashString(buffer, 0, -1, HashFormat.Hexadecimal));
            Assert.Throws<ArgumentOutOfRangeException>(() => hash.ComputeHashString(buffer, (HashFormat)(-1)));
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
        public void InputSliceInvalidTest(Int32 offset, Int32 count, Type exceptionType)
        {
            Hash hash = new FarmHash32();
            Byte[] buffer = new Byte[10];

            Exception exception = Record.Exception(() => hash.ComputeHashBytes(buffer, offset, count));

            Assert.NotNull(exception);
            Assert.Equal(exceptionType, exception.GetType());
        }

        [Theory]
        [MemberData(nameof(TestsCases.Hashes), MemberType = typeof(TestsCases))]
        public void InputEmptyTest(String hashName, Func<UInt64, Hash> hashInitializer, String _)
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
        public void InputSliceTest(Int32 offset, Int32 count)
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
        [MemberData(nameof(TestsCases.Hashes), MemberType = typeof(TestsCases))]
        public void InputStringTest(String hashName, Func<UInt64, Hash> hashInitializer, String _)
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
        [MemberData(nameof(TestsCases.Hashes), MemberType = typeof(TestsCases))]
        public void InputStringFormattedTest(String hashName, Func<UInt64, Hash> hashInitializer, String _)
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
        #endregion

        #region Tests: Output Operations
        [Theory]
        [MemberData(nameof(TestsCases.Hashes), MemberType = typeof(TestsCases))]
        public void OutputTest(String hashName, Func<UInt64, Hash> hashInitializer, String _)
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

        [Fact]
        public void OutputFormattedTest()
        {
            Hash hash = new FarmHash32();
            Byte[] buffer = [23, 134, 0, 237, 0, 81, 64, 64, 39, 5];
            Byte[] paddedBuffer = new Byte[buffer.Length + 10];
            Array.Copy(buffer, 0, paddedBuffer, 5, buffer.Length);

            foreach (HashFormat format in Enum.GetValues<HashFormat>())
            {
                String expected = HashFormatter.Format(hash.ComputeHashBytes(buffer), format);

                Assert.Equal(expected, hash.ComputeHashString(buffer, format));
                Assert.Equal(expected, hash.ComputeHashString(buffer, buffer.Length, format));
                Assert.Equal(expected, hash.ComputeHashString(buffer, 0, buffer.Length, format));
                Assert.Equal(expected, hash.ComputeHashString(new ReadOnlySpan<Byte>(buffer), format));
                Assert.Equal(expected, hash.ComputeHashString(paddedBuffer, 5, buffer.Length, format));
            }
        }
        #endregion

        #region Tests: Destination
        [Fact]
        public void DestinationInvalidTest()
        {
            Byte[] buffer = [1, 2, 3, 4];
            Hash[] hashes = [new TestHash32(), new TestHash64(), new TestHash128(), new TestHash256()];

            foreach (Hash hash in hashes)
            {
                Byte[] destination = new Byte[(hash.Length / 8) - 1];

                ArgumentException exception = Assert.Throws<ArgumentException>(() => hash.ComputeHashBytes(buffer, destination));
                Assert.Equal("destination", exception.ParamName);
            }
        }

        [Fact]
        public void DestinationTest()
        {
            Byte[] buffer = [1, 2, 3, 4];
            Hash[] hashes = [new TestHash32(), new TestHash64(), new TestHash128(), new TestHash256()];

            foreach (Hash hash in hashes)
            {
                Byte[] expected = hash.ComputeHashBytes(buffer);
                Byte[] destination = new Byte[hash.Length / 8];

                hash.ComputeHashBytes(buffer, destination);

                Assert.Equal(expected, destination);
            }
        }

        [Fact]
        public void DestinationBoundsTest()
        {
            Byte[] buffer = [1, 2, 3, 4];
            Hash[] hashes = [new TestHash32(), new TestHash64(), new TestHash128(), new TestHash256()];

            foreach (Hash hash in hashes)
            {
                Byte[] expected = hash.ComputeHashBytes(buffer);
                Byte[] destination = new Byte[expected.Length + 6];
                Array.Fill(destination, (Byte)0xA5);

                hash.ComputeHashBytes(buffer, destination.AsSpan(2, expected.Length + 2));

                Assert.Equal(new Byte[] { 0xA5, 0xA5 }, destination.AsSpan(0, 2).ToArray());
                Assert.Equal(expected, destination.AsSpan(2, expected.Length).ToArray());
                Assert.Equal(new Byte[] { 0xA5, 0xA5, 0xA5, 0xA5 }, destination.AsSpan(expected.Length + 2, 4).ToArray());
            }
        }
        #endregion

        #region Tests: Typed Value
        [Fact]
        public void ValueTypeInvalidTest()
        {
            TypeInitializationException exception = Assert.Throws<TypeInitializationException>(() =>
            {
                Hash<Guid> hash = new TestHashInvalid();
                _ = hash.Length;
            });

            Assert.IsType<NotSupportedException>(exception.InnerException);
        }

        [Fact]
        public void ValueInvalidTest()
        {
            Hash<UInt64> hash = new KomiHash();
            Byte[] buffer = new Byte[10];

            Assert.Throws<ArgumentNullException>(() => hash.ComputeHashValue((Byte[])null));
            Assert.Throws<ArgumentNullException>(() => hash.ComputeHashValue((Byte[])null, 0));
            Assert.Throws<ArgumentNullException>(() => hash.ComputeHashValue((Byte[])null, 0, 0));
            Assert.Throws<ArgumentNullException>(() => hash.ComputeHashValue((String)null));
            Assert.Throws<ArgumentNullException>(() => hash.ComputeHashValue((String)null, Encoding.UTF8));
            Assert.Throws<ArgumentNullException>(() => hash.ComputeHashValue("text", null));

            Assert.Throws<ArgumentOutOfRangeException>(() => hash.ComputeHashValue(buffer, -1));
            Assert.Throws<ArgumentException>(() => hash.ComputeHashValue(buffer, 11));
            Assert.Throws<ArgumentOutOfRangeException>(() => hash.ComputeHashValue(buffer, -1, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => hash.ComputeHashValue(buffer, 11, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => hash.ComputeHashValue(buffer, 0, -1));
            Assert.Throws<ArgumentException>(() => hash.ComputeHashValue(buffer, 8, 3));
        }

        [Fact]
        public void ValueLengthTest()
        {
            Assert.Equal(32, new TestHash32().Length);
            Assert.Equal(64, new TestHash64().Length);
            Assert.Equal(128, new TestHash128().Length);
            Assert.Equal(256, new TestHash256().Length);
        }

        [Fact]
        public void ValueOverloadsTest()
        {
            Hash<UInt64> hash = new KomiHash();
            String text = "Hello 世界 🚀";
            Byte[] buffer = Encoding.UTF8.GetBytes(text);
            Byte[] paddedBuffer = new Byte[buffer.Length + 10];
            Array.Copy(buffer, 0, paddedBuffer, 5, buffer.Length);

            UInt64 expected = hash.ComputeHashValue(buffer);

            Assert.Equal(expected, hash.ComputeHashValue(buffer, buffer.Length));
            Assert.Equal(expected, hash.ComputeHashValue(buffer, 0, buffer.Length));
            Assert.Equal(expected, hash.ComputeHashValue(paddedBuffer, 5, buffer.Length));
            Assert.Equal(expected, hash.ComputeHashValue(new ReadOnlySpan<Byte>(paddedBuffer, 5, buffer.Length)));
            Assert.Equal(expected, hash.ComputeHashValue(text));
            Assert.Equal(expected, hash.ComputeHashValue(text, Encoding.UTF8));

            Encoding encoding = Encoding.Unicode;
            UInt64 expectedUnicode = hash.ComputeHashValue(encoding.GetBytes(text));
            Assert.Equal(expectedUnicode, hash.ComputeHashValue(text, encoding));
        }

        [Fact]
        public void ValueSerializationTest()
        {
            Byte[] buffer = [1, 2, 3, 4];

            Hash<UInt32> hash32 = new TestHash32();
            Hash<UInt64> hash64 = new TestHash64();
            Hash<(UInt64, UInt64)> hash128 = new TestHash128();
            Hash<(UInt64, UInt64, UInt64, UInt64)> hash256 = new TestHash256();

            Assert.Equal(0x12345678u, hash32.ComputeHashValue(buffer));
            Assert.Equal(0x0123456789ABCDEFul, hash64.ComputeHashValue(buffer));
            Assert.Equal((0x0123456789ABCDEFul, 0x1020304050607080ul), hash128.ComputeHashValue(buffer));
            Assert.Equal((0x0123456789ABCDEFul, 0x1020304050607080ul, 0xFFEEDDCCBBAA9988ul, 0x8877665544332211ul), hash256.ComputeHashValue(buffer));

            Assert.Equal(new Byte[] { 0x78, 0x56, 0x34, 0x12 }, hash32.ComputeHashBytes(buffer));
            Assert.Equal(new Byte[] { 0xEF, 0xCD, 0xAB, 0x89, 0x67, 0x45, 0x23, 0x01 }, hash64.ComputeHashBytes(buffer));
            Assert.Equal(new Byte[] { 0xEF, 0xCD, 0xAB, 0x89, 0x67, 0x45, 0x23, 0x01, 0x80, 0x70, 0x60, 0x50, 0x40, 0x30, 0x20, 0x10 }, hash128.ComputeHashBytes(buffer));
            Assert.Equal(new Byte[] { 0xEF, 0xCD, 0xAB, 0x89, 0x67, 0x45, 0x23, 0x01, 0x80, 0x70, 0x60, 0x50, 0x40, 0x30, 0x20, 0x10, 0x88, 0x99, 0xAA, 0xBB, 0xCC, 0xDD, 0xEE, 0xFF, 0x11, 0x22, 0x33, 0x44, 0x55, 0x66, 0x77, 0x88 }, hash256.ComputeHashBytes(buffer));

            Assert.Equal("78563412", hash32.ComputeHashString(buffer, HashFormat.Hexadecimal));
            Assert.Equal("EFCDAB8967452301", hash64.ComputeHashString(buffer, HashFormat.Hexadecimal));
        }
        #endregion
    }
}