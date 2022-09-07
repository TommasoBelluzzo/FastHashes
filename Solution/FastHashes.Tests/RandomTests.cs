#region Using Directives
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using Xunit.Abstractions;
#endregion

namespace FastHashes.Tests
{
    public sealed class RandomTests
    {
        #region Test Cases
        private static readonly List<TestCaseBuffer> s_TestCasesBuffer = new List<TestCaseBuffer>
        {
            new TestCaseBuffer(9u, 0, 0, 0, Array.Empty<Byte>()),
            new TestCaseBuffer(266u, 10, 0, 3, new Byte[] { 193, 74, 65, 0, 0, 0, 0, 0, 0, 0 }),
            new TestCaseBuffer(148077u, 10, 0, 10, new Byte[] { 221, 40, 74, 23, 172, 98, 184, 208, 157, 36 }),
            new TestCaseBuffer(8018u, 10, 1, 5, new Byte[] { 0, 71, 102, 179, 5, 105, 0, 0, 0, 0 }),
            new TestCaseBuffer(5946u, 10, 3, 2, new Byte[] { 0, 0, 0, 103, 109, 0, 0, 0, 0, 0 }),
            new TestCaseBuffer(139u, 10, 7, 1, new Byte[] { 0, 0, 0, 0, 0, 0, 0, 73, 0, 0 }),
            new TestCaseBuffer(0u, null, 0, 10, typeof(ArgumentNullException)),
            new TestCaseBuffer(0u, 10, -1, 4, typeof(ArgumentOutOfRangeException)),
            new TestCaseBuffer(0u, 10, 15, 1, typeof(ArgumentOutOfRangeException)),
            new TestCaseBuffer(0u, 10, 2, -1, typeof(ArgumentOutOfRangeException)),
            new TestCaseBuffer(0u, 10, 2, 12, typeof(ArgumentOutOfRangeException)),
            new TestCaseBuffer(0u, 10, 5, 9, typeof(ArgumentException))
        };

        private static readonly List<TestCaseValue> s_TestCasesValue = new List<TestCaseValue>
        {
            new TestCaseValue(1707731u, 3589663199u),
            new TestCaseValue(26016u, 107436895u),
            new TestCaseValue(37u, 88619671u),
            new TestCaseValue(75u, 88820105u),
            new TestCaseValue(8810u, 73101954u),
            new TestCaseValue(93455u, 237727616u),
            new TestCaseValue(0u, 88675226u),
            new TestCaseValue(740u, 90056284u),
            new TestCaseValue(128u, 88938266u),
            new TestCaseValue(8944u, 73377480u)
        };

        public static IEnumerable<Object[]> DataBuffer()
        {
            foreach (TestCaseBuffer testCase in s_TestCasesBuffer)
                yield return (new Object[] { testCase.Seed, testCase.BufferLegth, testCase.Offset, testCase.Count, testCase.ExpectedResult });
        }

        public static IEnumerable<Object[]> DataValue()
        {
            foreach (TestCaseValue testCase in s_TestCasesValue)
                yield return (new Object[] { testCase.Seed, testCase.ExpectedValue });
        }
        #endregion

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
        [MemberData(nameof(DataBuffer))]
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
        [MemberData(nameof(DataValue))]
        public void ValueTest(UInt32 seed, UInt32 expectedValue)
        {
            RandomXorShift random = new RandomXorShift(seed);
            UInt32 actualValue = random.NextValue();

            m_Output.WriteLine($"EXPECTED: {expectedValue}");
            m_Output.WriteLine($"ACTUAL: {actualValue}");

            Assert.Equal(expectedValue, actualValue);
        }
        #endregion

        #region Nested Classes
        private sealed class TestCaseBuffer
        {
            #region Members
            private readonly Int32 m_Count;
            private readonly Int32 m_Offset;
            private readonly Int32? m_BufferLegth;
            private readonly UInt32 m_Seed;
            private readonly dynamic m_ExpectedResult;
            #endregion

            #region Properties
            public Int32 Count => m_Count;
            public Int32 Offset => m_Offset;
            public Int32? BufferLegth => m_BufferLegth;
            public UInt32 Seed => m_Seed;
            public dynamic ExpectedResult => m_ExpectedResult;
            #endregion

            #region Constructors
            public TestCaseBuffer(UInt32 seed, Int32? bufferLegth, Int32 offset, Int32 count, dynamic expectedResult)
            {
                if (bufferLegth.HasValue && (bufferLegth.Value < 0))
                    throw new ArgumentException("Invalid buffer legth specified.", nameof(bufferLegth));

                if (expectedResult == null)
                    throw new ArgumentNullException(nameof(expectedResult));

                String expectedResultType = expectedResult.GetType().Name;

                if (!String.Equals(expectedResultType, "Byte[]") && !String.Equals(expectedResultType, "RuntimeType"))
                    throw new ArgumentException("Invalid expected result specified.", nameof(expectedResult));

                m_BufferLegth = bufferLegth;
                m_Count = count;
                m_Offset = offset;
                m_Seed = seed;
                m_ExpectedResult = expectedResult;
            }
            #endregion

            #region Methods
            public override String ToString()
            {
                String bufferLength = m_BufferLegth.HasValue ? m_BufferLegth.Value.ToString() : "NULL";
                String expectedResult = m_ExpectedResult.GetType().Name;

                return $"{GetType().Name}: {nameof(Seed)}={m_Seed} {nameof(BufferLegth)}={bufferLength} {nameof(Offset)}={m_Offset} {nameof(Count)}={m_Count} {nameof(ExpectedResult)}={expectedResult}";
            }
            #endregion
        }

        private sealed class TestCaseValue
        {
            #region Members
            private readonly UInt32 m_ExpectedValue;
            private readonly UInt32 m_Seed;
            #endregion

            #region Properties
            public UInt32 ExpectedValue => m_ExpectedValue;
            public UInt32 Seed => m_Seed;
            #endregion

            #region Constructors
            public TestCaseValue(UInt32 seed, UInt32 expectedValue)
            {
                m_ExpectedValue = expectedValue;
                m_Seed = seed;
            }
            #endregion

            #region Methods
            public override String ToString()
            {
                return $"{GetType().Name}: {nameof(Seed)}={m_Seed} {nameof(ExpectedValue)}={m_ExpectedValue}";
            }
            #endregion
        }
        #endregion
    }
}