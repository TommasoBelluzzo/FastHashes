#region Using Directives
using System;
using System.Linq;
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
        public void BufferTest(UInt32 seed, Int32? bufferLength, Int32 offset, Int32 count, dynamic expectedResult)
        {
            RandomXorShift random = new RandomXorShift(seed);
            Byte[] buffer = bufferLength.HasValue ? new Byte[bufferLength.Value] : null;

            Object actualResult;

            try
            {
                random.NextBytes(buffer, offset, count);
                actualResult = buffer;
            }
            catch (Exception e)
            {
                actualResult = e.GetType();
            }

            if (expectedResult is Byte[] expectedBytes)
                m_Output.WriteLine($"EXPECTED: {{ {String.Join(", ", expectedBytes.Select(x => x.ToString()))} }}");
            else
            {
                Type expectedType = (Type)expectedResult;
                m_Output.WriteLine($"EXPECTED: {((expectedType == null) ? String.Empty : expectedType.Name)}");
            }

            if (actualResult is Byte[] actualBytes)
                m_Output.WriteLine($"ACTUAL: {{ {String.Join(", ", actualBytes.Select(x => x.ToString()))} }}");
            else
            {
                Type actualType = (Type)actualResult;
                m_Output.WriteLine($"ACTUAL: {((actualType == null) ? String.Empty : actualType.Name)}");
            }

            Assert.Equal(expectedResult, actualResult);
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