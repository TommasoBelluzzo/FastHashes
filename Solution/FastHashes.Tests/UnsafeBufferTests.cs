#region Using Directives
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using Xunit.Abstractions;
#endregion

namespace FastHashes.Tests
{
    public sealed class UnsafeBufferTests
    {
        #region Test Cases
        private static readonly List<TestCase> s_TestCases = new List<TestCase>
        {
            new TestCase(101213u, 5, 0, 10, 0, 3, new Byte[] { 164, 43, 30, 0, 0, 0, 0, 0, 0, 0 }),
            new TestCase(9076u, 5, 0, 10, 1, 4, new Byte[] { 0, 109, 139, 83, 4, 0, 0, 0, 0, 0 }),
            new TestCase(17u, 5, 0, 10, 3, 5, new Byte[] { 0, 0, 0, 3, 155, 73, 5, 50, 0, 0 }),
            new TestCase(6112u, 5, 0, 10, 3, 5, new Byte[] { 0, 0, 0, 109, 187, 246, 5, 75, 0, 0 }),
            new TestCase(337180u, 5, 0, 10, 8, 2, new Byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 67, 251 }),
            new TestCase(0u, 10, 0, 10, 0, 2, new Byte[] { 154, 19, 0, 0, 0, 0, 0, 0, 0, 0 }),
            new TestCase(0u, 10, 0, 10, 0, 4, new Byte[] { 154, 19, 73, 5, 0, 0, 0, 0, 0, 0 }),
            new TestCase(0u, 10, 0, 10, 0, 6, new Byte[] { 154, 19, 73, 5, 171, 91, 0, 0, 0, 0 }),
            new TestCase(0u, 10, 0, 10, 0, 8, new Byte[] { 154, 19, 73, 5, 171, 91, 187, 194, 0, 0 }),
            new TestCase(0u, 10, 0, 10, 0, 10, new Byte[] { 154, 19, 73, 5, 171, 91, 187, 194, 218, 31 }),
            new TestCase(4004u, 20, 0, 10, 5, 5, new Byte[] { 0, 0, 0, 0, 0, 17, 65, 52, 5, 47 }),
            new TestCase(4004u, 20, 10, 10, 0, 5, new Byte[] { 135, 76, 121, 31, 26, 0, 0, 0, 0, 0 }),
            new TestCase(4004u, 20, 0, 10, 0, 10, new Byte[] { 17, 65, 52, 5, 47, 9, 198, 194, 81, 77 }),
            new TestCase(241u, null, 0, 10, 0, 10, typeof(ArgumentNullException)),
            new TestCase(241u, 10, 0, null, 0, 10, typeof(ArgumentNullException)),
            new TestCase(241u, 10, -1, 10, 0, 10, typeof(ArgumentOutOfRangeException)),
            new TestCase(241u, 10, 0, 10, 15, 10, typeof(ArgumentOutOfRangeException)),
            new TestCase(241u, 10, 0, 10, 0, -5, typeof(ArgumentOutOfRangeException)),
            new TestCase(241u, 10, 2, 10, 0, 9, typeof(ArgumentException)),
            new TestCase(241u, 10, 0, 10, 2, 9, typeof(ArgumentException))
        };

        public static IEnumerable<Object[]> Data()
        {
            foreach (TestCase testCase in s_TestCases)
                yield return (new Object[] { testCase.Seed, testCase.SourceLength, testCase.SourceOffset, testCase.DestinationLength, testCase.DestinationOffset, testCase.Count, testCase.ExpectedResult });
        }
        #endregion

        #region Members
        private readonly ITestOutputHelper m_Output;
        #endregion

        #region Constructors
        public UnsafeBufferTests(ITestOutputHelper output)
        {
            m_Output = output;
        }
        #endregion

        #region Methods
        [Theory]
        [MemberData(nameof(Data))]
        public void Test(UInt32 seed, Int32? sourceLegth, Int32 sourceOffset, Int32? destinationLength, Int32 destinationOffset, Int32 count, Object expectedResult)
        {
            Byte[] source;

            if (sourceLegth.HasValue)
            {
                source = new Byte[sourceLegth.Value];

                RandomXorShift random = new RandomXorShift(seed);
                random.NextBytes(source);
            }
            else
                source = null;

            Byte[] destination = destinationLength.HasValue ? new Byte[destinationLength.Value] : null;

            Object actualResult;

            try
            {
                UnsafeBuffer.BlockCopy(source, sourceOffset, destination, destinationOffset, count);
                actualResult = destination;
            }
            catch (Exception e)
            {
                actualResult = e.GetType();
            }

            if (expectedResult is Byte[] expectedBytes)
                m_Output.WriteLine($"EXPECTED={{ {String.Join(", ", expectedBytes.Select(x => x.ToString()))} }}");
            else
            {
                Type expectedType = (Type)expectedResult;
                m_Output.WriteLine($"EXPECTED={((expectedType == null) ? String.Empty : expectedType.Name)}");
            }

            if (actualResult is Byte[] actualBytes)
                m_Output.WriteLine($"ACTUAL={{ {String.Join(", ", actualBytes.Select(x => x.ToString()))} }}");
            else
            {
                Type actualType = (Type)actualResult;
                m_Output.WriteLine($"ACTUAL={((actualType == null) ? String.Empty : actualType.Name)}");
            }

            Assert.Equal(expectedResult, actualResult);
        }
        #endregion

        #region Nested Classes
        public sealed class TestCase
        {
            #region Members
            private readonly Int32 m_Count;
            private readonly Int32? m_DestinationLength;
            private readonly Int32 m_DestinationOffset;
            private readonly Int32? m_SourceLegth;
            private readonly Int32 m_SourceOffset;
            private readonly Object m_ExpectedResult;
            private readonly UInt32 m_Seed;
            #endregion

            #region Properties
            public Int32 Count => m_Count;
            public Int32? DestinationLength => m_DestinationLength;
            public Int32 DestinationOffset => m_DestinationOffset;
            public Int32? SourceLength => m_SourceLegth;
            public Int32 SourceOffset => m_SourceOffset;
            public Object ExpectedResult => m_ExpectedResult;
            public UInt32 Seed => m_Seed;
            #endregion

            #region Constructors
            public TestCase(UInt32 seed, Int32? sourceLegth, Int32 sourceOffset, Int32? destinationLength, Int32 destinationOffset, Int32 count, Object expectedResult)
            {
                m_Count = count;
                m_DestinationLength = destinationLength;
                m_DestinationOffset = destinationOffset;
                m_SourceLegth = sourceLegth;
                m_SourceOffset = sourceOffset;
                m_ExpectedResult = expectedResult;
                m_Seed = seed;
            }
            #endregion

            #region Methods
            public override String ToString()
            {
                String sourceLength = m_SourceLegth.HasValue ? m_SourceLegth.Value.ToString() : "NULL";
                String destinationLength = m_DestinationLength.HasValue ? m_DestinationLength.Value.ToString() : "NULL";
                String expectedResult = m_ExpectedResult.GetType().Name;

                return $"{GetType().Name}: {nameof(Seed)}={m_Seed} {nameof(SourceLength)}={sourceLength} {nameof(SourceOffset)}={m_SourceOffset} {nameof(DestinationLength)}={destinationLength} {nameof(DestinationOffset)}={m_DestinationOffset} {nameof(Count)}={m_Count} {nameof(ExpectedResult)}={expectedResult}";
            }
            #endregion
        }
        #endregion
    }
}