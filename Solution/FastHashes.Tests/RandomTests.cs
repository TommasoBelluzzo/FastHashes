#region Using Directives
using System;
using Xunit;
#endregion

namespace FastHashes.Tests
{
    public sealed class RandomTests
    {
        #region Methods
        [Theory]
        [MemberData(nameof(RandomTestsCases.DataBuffer), MemberType=typeof(RandomTestsCases))]
        public void BufferOutputTest(UInt32 seed, Int32 bufferLength, Int32 offset, Int32 count, Byte[] expectedValue)
        {
            Byte[] actualValue = new Byte[bufferLength];

            RandomXorShift random = new RandomXorShift(seed);
            random.NextBytes(actualValue, offset, count);

            Assert.Equal(expectedValue, actualValue);
        }

        [Fact]
        public void BufferEmptyTest()
        {
            RandomXorShift random = new RandomXorShift(0u);
            RandomXorShift reference = new RandomXorShift(0u);

            Byte[] buffer = Array.Empty<Byte>();

            random.NextBytes(buffer);
            random.NextBytes(buffer, 0);
            random.NextBytes(buffer, 0, 0);

            Assert.Empty(buffer);
            Assert.Equal(reference.NextValue(), random.NextValue());
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
            RandomXorShift random = new RandomXorShift(0u);
            Byte[] buffer = Array.Empty<Byte>();

            Assert.Throws(exceptionType, () => random.NextBytes(buffer, offset, count));
        }

        [Fact]
        public void BufferExceptionTest()
        {
            RandomXorShift random = new RandomXorShift(0u);

            Assert.Throws<ArgumentNullException>(() => { Byte[] buffer = null; random.NextBytes(buffer, 0, 10); });
            Assert.Throws<ArgumentOutOfRangeException>(() => { Byte[] buffer = new Byte[10]; random.NextBytes(buffer, -1, 4); });
            Assert.Throws<ArgumentOutOfRangeException>(() => { Byte[] buffer = new Byte[10]; random.NextBytes(buffer, 15, 1); });
            Assert.Throws<ArgumentOutOfRangeException>(() => { Byte[] buffer = new Byte[10]; random.NextBytes(buffer, 2, -1); });
            Assert.Throws<ArgumentOutOfRangeException>(() => { Byte[] buffer = new Byte[10]; random.NextBytes(buffer, 2, 12); });
            Assert.Throws<ArgumentException>(() => { Byte[] buffer = new Byte[10]; random.NextBytes(buffer, 5, 9); });
        }

        [Fact]
        public void QueueStateTest()
        {
            RandomXorShift random = new RandomXorShift(0u);

            Byte[] buffer1 = new Byte[7];
            random.NextBytes(buffer1, 0, 7);

            Byte[] buffer2 = new Byte[7];
            random.NextBytes(buffer2, 0, 7);

            Assert.False(random.EmptyQueue);
        }

        [Theory]
        [MemberData(nameof(RandomTestsCases.DataValue), MemberType=typeof(RandomTestsCases))]
        public void ValueOutputTest(UInt32 seed, UInt32 expectedValue)
        {
            RandomXorShift random = new RandomXorShift(seed);
            UInt32 actualValue = random.NextValue();

            Assert.Equal(expectedValue, actualValue);
        }
        #endregion
    }
}