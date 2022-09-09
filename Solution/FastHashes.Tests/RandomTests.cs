#region Using Directives
using System;
using Xunit;
using Xunit.Abstractions;
#endregion

namespace FastHashes.Tests
{
    public sealed class RandomTests
    {
        #region Members
        private readonly ITestOutputHelper m_Output;
        #endregion

        #region Constructors
        public RandomTests(ITestOutputHelper output)
        {
            m_Output = output;
        }
        #endregion

        #region Methods
        [Theory]
        [MemberData(nameof(RandomTestsCases.DataBuffer), MemberType=typeof(RandomTestsCases))]
        public void BufferTest(UInt32 seed, Int32 bufferLength, Int32 offset, Int32 count, Byte[] expectedValue)
        {
            Byte[] actualValue = new Byte[bufferLength];

            RandomXorShift random = new RandomXorShift(seed);
            random.NextBytes(actualValue, offset, count);

            m_Output.WriteLine($"EXPECTED: {Utilities.FormatNumericArray(expectedValue)}");
            m_Output.WriteLine($"ACTUAL: {Utilities.FormatNumericArray(actualValue)}");

            Assert.Equal(expectedValue, actualValue);
        }

        [Fact]
        public void ExceptionTest()
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
        public void QueueTest()
        {
            RandomXorShift random = new RandomXorShift(0u);

            Byte[] buffer1 = new Byte[7];
            random.NextBytes(buffer1, 0, 7);

            Byte[] buffer2 = new Byte[7];
            random.NextBytes(buffer2, 0, 7);

            m_Output.WriteLine($"EXPECTED: {false}");
            m_Output.WriteLine($"ACTUAL: {random.EmptyQueue}");

            Assert.False(random.EmptyQueue);
        }

        [Theory]
        [MemberData(nameof(RandomTestsCases.DataValue), MemberType=typeof(RandomTestsCases))]
        public void ValueTest(UInt32 seed, UInt32 expectedValue)
        {
            RandomXorShift random = new RandomXorShift(seed);
            UInt32 actualValue = random.NextValue();

            m_Output.WriteLine($"EXPECTED: {expectedValue}");
            m_Output.WriteLine($"ACTUAL: {actualValue}");

            Assert.Equal(expectedValue, actualValue);
        }
        #endregion
    }
}