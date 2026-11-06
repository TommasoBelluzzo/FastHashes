#region Using Directives
using System;
using Xunit;
#endregion

namespace FastHashes.Tests
{
    public sealed class RandomGeneratorsTests
    {
        #region Tests: Buffer Operations
        [Theory]
        [InlineData(null, 0, 10, typeof(ArgumentNullException))]
        [InlineData(10, -1, 4, typeof(ArgumentOutOfRangeException))]
        [InlineData(10, 15, 1, typeof(ArgumentOutOfRangeException))]
        [InlineData(10, 2, -1, typeof(ArgumentOutOfRangeException))]
        [InlineData(10, 2, 12, typeof(ArgumentException))]
        [InlineData(10, 5, 9, typeof(ArgumentException))]
        public void BufferExceptionTest(Int32? bufferLength, Int32 offset, Int32 count, Type exceptionType)
        {
            Byte[] buffer = bufferLength.HasValue ? new Byte[bufferLength.Value] : null;

            RandomSplitMix splitMix = new(0ul);
            Assert.Throws(exceptionType, () => splitMix.NextBytes(buffer, offset, count));

            RandomXorShift xorShift = new(0u);
            Assert.Throws(exceptionType, () => xorShift.NextBytes(buffer, offset, count));
        }

        [Theory]
        [InlineData(-1, 0, typeof(ArgumentOutOfRangeException))]
        [InlineData(1, 0, typeof(ArgumentOutOfRangeException))]
        [InlineData(0, -1, typeof(ArgumentOutOfRangeException))]
        [InlineData(0, 1, typeof(ArgumentException))]
        [InlineData(Int32.MaxValue, 0, typeof(ArgumentOutOfRangeException))]
        [InlineData(0, Int32.MaxValue, typeof(ArgumentException))]
        public void BufferInvalidArgumentsTest(Int32 offset, Int32 count, Type exceptionType)
        {
            Byte[] buffer = [];

            RandomSplitMix splitMix = new(0ul);
            Assert.Throws(exceptionType, () => splitMix.NextBytes(buffer, offset, count));

            RandomXorShift xorShift = new(0u);
            Assert.Throws(exceptionType, () => xorShift.NextBytes(buffer, offset, count));
        }

        [Fact]
        public void BufferEmptyTest()
        {
            Byte[] buffer;

            RandomSplitMix splitMix = new(0ul);
            RandomSplitMix splitMixReference = new(0ul);

            buffer = [];
            splitMix.NextBytes(buffer);
            splitMix.NextBytes(buffer, 0);
            splitMix.NextBytes(buffer, 0, 0);
            Assert.Empty(buffer);
            Assert.Equal(splitMix.NextValue(), splitMixReference.NextValue());

            RandomXorShift xorShift = new(0u);
            RandomXorShift xorShiftReference = new(0u);

            buffer = [];
            xorShift.NextBytes(buffer);
            xorShift.NextBytes(buffer, 0);
            xorShift.NextBytes(buffer, 0, 0);
            Assert.Empty(buffer);
            Assert.Equal(xorShift.NextValue(), xorShiftReference.NextValue());
        }

        [Theory]
        [InlineData(0ul, 8, 0, 8, new Byte[] { 175, 205, 29, 123, 57, 168, 32, 226 })]
        [InlineData(9ul, 0, 0, 0, new Byte[] { })]
        [InlineData(266ul, 10, 0, 3, new Byte[] { 216, 254, 157, 0, 0, 0, 0, 0, 0, 0 })]
        [InlineData(148077ul, 10, 0, 10, new Byte[] { 35, 246, 120, 90, 224, 121, 178, 212, 11, 138 })]
        [InlineData(8018ul, 10, 1, 5, new Byte[] { 0, 46, 111, 153, 25, 112, 0, 0, 0, 0 })]
        [InlineData(5946ul, 10, 3, 2, new Byte[] { 0, 0, 0, 57, 115, 0, 0, 0, 0, 0 })]
        [InlineData(139ul, 10, 7, 1, new Byte[] { 0, 0, 0, 0, 0, 0, 0, 227, 0, 0 })]
        public void BufferOutputSplitMixTest(UInt64 seed, Int32 bufferLength, Int32 offset, Int32 count, Byte[] expected)
        {
            RandomSplitMix random = new(seed);

            Byte[] actual = new Byte[bufferLength];
            random.NextBytes(actual, offset, count);

            Assert.Equal(expected, actual);
        }

        [Theory]
        [InlineData(9u, 0, 0, 0, new Byte[] { })]
        [InlineData(266u, 10, 0, 3, new Byte[] { 193, 74, 65, 0, 0, 0, 0, 0, 0, 0 })]
        [InlineData(148077u, 10, 0, 10, new Byte[] { 221, 40, 74, 23, 172, 98, 184, 208, 157, 36 })]
        [InlineData(8018u, 10, 1, 5, new Byte[] { 0, 71, 102, 179, 5, 105, 0, 0, 0, 0 })]
        [InlineData(5946u, 10, 3, 2, new Byte[] { 0, 0, 0, 103, 109, 0, 0, 0, 0, 0 })]
        [InlineData(139u, 10, 7, 1, new Byte[] { 0, 0, 0, 0, 0, 0, 0, 73, 0, 0 })]
        public void BufferOutputXorShiftTest(UInt32 seed, Int32 bufferLength, Int32 offset, Int32 count, Byte[] expected)
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
        [InlineData(8)]
        [InlineData(9)]
        [InlineData(16)]
        public void BufferOverloadsTest(Int32 count)
        {
            Byte[] buffer1, buffer2, buffer3, buffer4;

            buffer1 = new Byte[count];
            buffer2 = new Byte[count];
            buffer3 = new Byte[count];
            buffer4 = new Byte[count];

            new RandomSplitMix(123ul).NextBytes(buffer1);
            new RandomSplitMix(123ul).NextBytes(buffer2, count);
            new RandomSplitMix(123ul).NextBytes(buffer3, 0, count);
            new RandomSplitMix(123ul).NextBytes(buffer4.AsSpan());
            Assert.Equal(buffer1, buffer2);
            Assert.Equal(buffer1, buffer3);
            Assert.Equal(buffer1, buffer4);

            buffer1 = new Byte[count];
            buffer2 = new Byte[count];
            buffer3 = new Byte[count];
            buffer4 = new Byte[count];

            new RandomXorShift(123u).NextBytes(buffer1);
            new RandomXorShift(123u).NextBytes(buffer2, count);
            new RandomXorShift(123u).NextBytes(buffer3, 0, count);
            new RandomXorShift(123u).NextBytes(buffer4.AsSpan());
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
            Byte[] buffer = new Byte[10];
            
            Array.Fill(buffer, (Byte)0xAA);
            new RandomSplitMix(123ul).NextBytes(buffer, offset, count);

            for (Int32 i = 0; i < buffer.Length; ++i)
            {
                if ((i < offset) || (i >= offset + count))
                    Assert.Equal((Byte)0xAA, buffer[i]);
            }

            Array.Fill(buffer, (Byte)0xAA);
            new RandomXorShift(123u).NextBytes(buffer, offset, count);

            for (Int32 i = 0; i < buffer.Length; ++i)
            {
                if ((i < offset) || (i >= (offset + count)))
                    Assert.Equal((Byte)0xAA, buffer[i]);
            }
        }
        #endregion

        #region Tests: Value Operations
        [Theory]
        [InlineData(0ul, 16294208416658607535ul)]
        [InlineData(1ul, 10451216379200822465ul)]
        [InlineData(123ul, 13032462758197477675ul)]
        [InlineData(UInt64.MaxValue, 16490336266968443936ul)]
        public void ValueOutputSplitMixTest(UInt64 seed, UInt64 expected)
        {
            RandomSplitMix random = new(seed);
            UInt64 actual = random.NextValue();

            Assert.Equal(expected, actual);
        }

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
        public void ValueOutputXorShiftTest(UInt32 seed, UInt32 expected)
        {
            RandomXorShift random = new(seed);
            UInt32 actual = random.NextValue();

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void ValueSequenceTest()
        {
            RandomSplitMix splitMix = new(42ul);
            RandomSplitMix splitMixReference = new(42ul);
            UInt64[] splitMixExpected = new UInt64[16];

            for (Int32 i = 0; i < splitMixExpected.Length; ++i)
                splitMixExpected[i] = splitMixReference.NextValue();

            for (Int32 i = 0; i < splitMixExpected.Length; ++i)
                Assert.Equal(splitMixExpected[i], splitMix.NextValue());

            RandomXorShift xorShift = new(42u);
            RandomXorShift xorShiftReference = new(42u);
            UInt32[] xorShiftExpected = new UInt32[16];

            for (Int32 i = 0; i < xorShiftExpected.Length; ++i)
                xorShiftExpected[i] = xorShiftReference.NextValue();

            for (Int32 i = 0; i < xorShiftExpected.Length; ++i)
                Assert.Equal(xorShiftExpected[i], xorShift.NextValue());
        }
        #endregion

        #region Tests: Queue & State Management
        [Fact]
        public void QueueEmptyTest()
        {
            Byte[] empty = [];

            RandomSplitMix splitMix = new(88ul);
            RandomSplitMix splitMixReference = new(88ul);

            splitMix.NextBytes(new Byte[7]);
            Assert.False(splitMix.EmptyQueue);

            splitMix.NextBytes(empty);
            splitMix.NextBytes(empty, 0);
            splitMix.NextBytes(empty, 0, 0);
            Assert.False(splitMix.EmptyQueue);

            Byte[] splitMixExpected = new Byte[8];
            Byte[] splitMixActual = new Byte[1];
            splitMix.NextBytes(splitMixActual);
            splitMixReference.NextBytes(splitMixExpected);
            Assert.Equal(splitMixExpected[7], splitMixActual[0]);
            Assert.True(splitMix.EmptyQueue);

            RandomXorShift xorShift = new(123u);
            RandomXorShift xorShiftReference = new(123u);

            xorShift.NextBytes(new Byte[3]);
            Assert.False(xorShift.EmptyQueue);

            xorShift.NextBytes(empty);
            xorShift.NextBytes(empty, 0);
            xorShift.NextBytes(empty, 0, 0);
            Assert.False(xorShift.EmptyQueue);

            Byte[] xorShiftExpected = new Byte[4];
            Byte[] xorShiftActual = new Byte[1];
            xorShift.NextBytes(xorShiftActual);
            xorShiftReference.NextBytes(xorShiftExpected);
            Assert.Equal(xorShiftExpected[3], xorShiftActual[0]);
            Assert.True(xorShift.EmptyQueue);
        }

        [Fact]
        public void QueueStateTest()
        {
            Byte[] buffer1 = new Byte[7];
            Byte[] buffer2 = new Byte[7];

            RandomSplitMix splitMix = new(0ul);
            splitMix.NextBytes(buffer1, 0, 7);
            splitMix.NextBytes(buffer2, 0, 7);
            Assert.False(splitMix.EmptyQueue);

            RandomXorShift xorShift = new(0u);
            xorShift.NextBytes(buffer1, 0, 7);
            xorShift.NextBytes(buffer2, 0, 7);
            Assert.False(xorShift.EmptyQueue);
        }

        [Theory]
        [InlineData(1, false, false)]
        [InlineData(2, false, false)]
        [InlineData(3, false, false)]
        [InlineData(4, false, true)]
        [InlineData(5, false, false)]
        [InlineData(7, false, false)]
        [InlineData(8, true, true)]
        [InlineData(9, false, false)]
        [InlineData(16, true, true)]
        public void QueueConsumptionTest(Int32 count, Boolean splitMixExpected, Boolean xorShiftExpected)
        {
            Byte[] buffer = new Byte[count];

            RandomSplitMix splitMix = new(0ul);
            splitMix.NextBytes(buffer);
            Assert.Equal(splitMixExpected, splitMix.EmptyQueue);

            RandomXorShift xorShift = new(0u);
            xorShift.NextBytes(buffer);
            Assert.Equal(xorShiftExpected, xorShift.EmptyQueue);
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
            Byte[] actual = new Byte[17];
            Byte[] expected = new Byte[17];

            RandomSplitMix splitMix = new(123ul);
            RandomSplitMix splitMixReference = new(123ul);

            splitMix.NextBytes(actual, 0, firstCount);
            splitMix.NextBytes(actual, firstCount, secondCount);
            splitMixReference.NextBytes(expected);
            Assert.Equal(expected, actual);

            Array.Clear(actual, 0, actual.Length);
            Array.Clear(expected, 0, expected.Length);

            RandomXorShift xorShift = new(123u);
            RandomXorShift xorShiftReference = new(123u);

            xorShift.NextBytes(actual, 0, firstCount);
            xorShift.NextBytes(actual, firstCount, secondCount);
            xorShiftReference.NextBytes(expected);
            Assert.Equal(expected, actual);
        }
        #endregion
    }
}