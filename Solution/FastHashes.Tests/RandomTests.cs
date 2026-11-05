#region Using Directives
using System;
using Xunit;
#endregion

namespace FastHashes.Tests
{
    public sealed class RandomTests
    {
        #region Tests: Buffer Operations
        [Theory]
        [InlineData(null, 0, 10, typeof(ArgumentNullException))]
        [InlineData(10, -1, 4, typeof(ArgumentOutOfRangeException))]
        [InlineData(10, 15, 1, typeof(ArgumentOutOfRangeException))]
        [InlineData(10, 2, -1, typeof(ArgumentOutOfRangeException))]
        [InlineData(10, 2, 12, typeof(ArgumentOutOfRangeException))]
        [InlineData(10, 5, 9, typeof(ArgumentException))]
        public void BufferExceptionTest(Int32? bufferLength, Int32 offset, Int32 count, Type exceptionType)
        {
            RandomXorShift random = new(0u);
            Byte[] buffer = bufferLength.HasValue ? new Byte[bufferLength.Value] : null;

            Assert.Throws(exceptionType, () => random.NextBytes(buffer, offset, count));
        }

        [Theory]
        [InlineData(-1, 0, typeof(ArgumentOutOfRangeException))]
        [InlineData(1, 0, typeof(ArgumentOutOfRangeException))]
        [InlineData(0, -1, typeof(ArgumentOutOfRangeException))]
        [InlineData(0, 1, typeof(ArgumentOutOfRangeException))]
        [InlineData(Int32.MaxValue, 0, typeof(ArgumentOutOfRangeException))]
        [InlineData(0, Int32.MaxValue, typeof(ArgumentOutOfRangeException))]
        public void BufferInvalidArgumentsTest(Int32 offset, Int32 count, Type exceptionType)
        {
            RandomXorShift random = new(0u);
            Byte[] buffer = [];

            Assert.Throws(exceptionType, () => random.NextBytes(buffer, offset, count));
        }

        [Fact]
        public void BufferEmptyTest()
        {
            RandomXorShift random = new(0u);
            RandomXorShift reference = new(0u);
            Byte[] buffer = [];

            random.NextBytes(buffer);
            random.NextBytes(buffer, 0);
            random.NextBytes(buffer, 0, 0);

            Assert.Empty(buffer);
            Assert.Equal(reference.NextValue(), random.NextValue());
        }

        [Theory]
        [InlineData(9u, 0, 0, 0, new Byte[] { })]
        [InlineData(266u, 10, 0, 3, new Byte[] { 193, 74, 65, 0, 0, 0, 0, 0, 0, 0 })]
        [InlineData(148077u, 10, 0, 10, new Byte[] { 221, 40, 74, 23, 172, 98, 184, 208, 157, 36 })]
        [InlineData(8018u, 10, 1, 5, new Byte[] { 0, 71, 102, 179, 5, 105, 0, 0, 0, 0 })]
        [InlineData(5946u, 10, 3, 2, new Byte[] { 0, 0, 0, 103, 109, 0, 0, 0, 0, 0 })]
        [InlineData(139u, 10, 7, 1, new Byte[] { 0, 0, 0, 0, 0, 0, 0, 73, 0, 0 })]
        public void BufferOutputTest(UInt32 seed, Int32 bufferLength, Int32 offset, Int32 count, Byte[] expected)
        {
            RandomXorShift random = new(seed);

            Byte[] actual = new Byte[bufferLength];
            random.NextBytes(actual, offset, count);

            Assert.Equal(expected, actual);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(3)]
        [InlineData(4)]
        [InlineData(7)]
        [InlineData(16)]
        public void BufferOverloadsTest(Int32 count)
        {
            RandomXorShift random1 = new(123u);
            RandomXorShift random2 = new(123u);
            RandomXorShift random3 = new(123u);
            RandomXorShift random4 = new(123u);

            Byte[] buffer1 = new Byte[count];
            Byte[] buffer2 = new Byte[count];
            Byte[] buffer3 = new Byte[count];
            Byte[] buffer4 = new Byte[count];

            random1.NextBytes(buffer1);
            random2.NextBytes(buffer2, count);
            random3.NextBytes(buffer3, 0, count);
            random4.NextBytes(buffer4.AsSpan());

            Assert.Equal(buffer1, buffer2);
            Assert.Equal(buffer1, buffer3);
            Assert.Equal(buffer1, buffer4);
        }

        [Theory]
        [InlineData(0, 3)]
        [InlineData(3, 5)]
        [InlineData(7, 3)]
        [InlineData(10, 0)]
        public void BufferPreservationTest(Int32 offset, Int32 count)
        {
            RandomXorShift random = new(123u);

            Byte[] buffer = new Byte[10];
            Array.Fill(buffer, (Byte)0xAA);

            random.NextBytes(buffer, offset, count);

            for (Int32 i = 0; i < buffer.Length; ++i)
            {
                if ((i < offset) || (i >= offset + count))
                    Assert.Equal((Byte)0xAA, buffer[i]);
            }
        }
        #endregion

        #region Tests: Value Operations
        [Theory]
        [InlineData(1707731u, 3589663199u)]
        [InlineData(26016u, 107436895u)]
        [InlineData(37u, 88619671u)]
        [InlineData(75u, 88820105u)]
        [InlineData(8810u, 73101954u)]
        [InlineData(93455u, 237727616u)]
        [InlineData(0u, 88675226u)]
        [InlineData(740u, 90056284u)]
        [InlineData(128u, 88938266u)]
        [InlineData(8944u, 73377480u)]
        public void ValueOutputTest(UInt32 seed, UInt32 expected)
        {
            RandomXorShift random = new(seed);
            UInt32 actual = random.NextValue();

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void ValueSequenceTest()
        {
            RandomXorShift random = new(123u);
            RandomXorShift reference = new(123u);

            UInt32[] expected = new UInt32[16];

            for (Int32 i = 0; i < expected.Length; ++i)
                expected[i] = reference.NextValue();

            for (Int32 i = 0; i < expected.Length; ++i)
                Assert.Equal(expected[i], random.NextValue());
        }
        #endregion

        #region Tests: Queue & State Management
        [Fact]
        public void QueueEmptyTest()
        {
            RandomXorShift random = new(123u);
            RandomXorShift reference = new(123u);
            
            Byte[] buffer = new Byte[3];
            Byte[] empty = [];
            Byte[] expected = new Byte[4];

            random.NextBytes(buffer);
            reference.NextBytes(expected);

            Assert.False(random.EmptyQueue);

            random.NextBytes(empty);
            random.NextBytes(empty, 0);
            random.NextBytes(empty, 0, 0);

            Assert.False(random.EmptyQueue);

            Byte[] actual = new Byte[1];
            random.NextBytes(actual);

            Assert.Equal(expected[3], actual[0]);
            Assert.True(random.EmptyQueue);
        }

        [Fact]
        public void QueueStateTest()
        {
            RandomXorShift random = new(0u);

            Byte[] buffer1 = new Byte[7];
            random.NextBytes(buffer1, 0, 7);

            Byte[] buffer2 = new Byte[7];
            random.NextBytes(buffer2, 0, 7);

            Assert.False(random.EmptyQueue);
        }

        [Theory]
        [InlineData(1, false)]
        [InlineData(2, false)]
        [InlineData(3, false)]
        [InlineData(4, true)]
        [InlineData(5, false)]
        [InlineData(8, true)]
        public void QueueConsumptionTest(Int32 count, Boolean expected)
        {
            RandomXorShift random = new(0u);

            Byte[] buffer = new Byte[count];
            random.NextBytes(buffer);

            Assert.Equal(expected, random.EmptyQueue);
        }

        [Theory]
        [InlineData(1, 16)]
        [InlineData(2, 15)]
        [InlineData(3, 14)]
        [InlineData(4, 13)]
        [InlineData(5, 12)]
        [InlineData(7, 10)]
        [InlineData(8, 9)]
        [InlineData(16, 1)]
        public void QueuePartitionTest(Int32 firstCount, Int32 secondCount)
        {
            RandomXorShift random = new(123u);
            RandomXorShift reference = new(123u);

            Byte[] actual = new Byte[17];
            Byte[] expected = new Byte[17];

            random.NextBytes(actual, 0, firstCount);
            random.NextBytes(actual, firstCount, secondCount);

            reference.NextBytes(expected);

            Assert.Equal(expected, actual);
        }
        #endregion
    }
}