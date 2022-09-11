#region Using Directives
using System;
using System.Collections.Generic;
#endregion

namespace FastHashes.Tests
{
    public static class BinaryOperationsTestsCases
    {
        #region Test Cases
        private static readonly Byte[] s_Buffer = new Byte[] { 174, 9, 151, 129, 39, 0, 0, 254, 147, 105, 81, 36, 59, 76, 58, 227 };

        private static readonly List<dynamic> s_TestCasesRead = new List<dynamic>
        {
            new TestCase<UInt16>(() => BinaryOperations.Read16(new ReadOnlySpan<Byte>(s_Buffer), 5), 0),
            new TestCase<UInt16>(() => BinaryOperations.Read16(new ReadOnlySpan<Byte>(s_Buffer), 8), 27027),
            new TestCase<UInt32>(() => BinaryOperations.Read32(new ReadOnlySpan<Byte>(s_Buffer), 1), 662804233u),
            new TestCase<UInt32>(() => BinaryOperations.Read32(new ReadOnlySpan<Byte>(s_Buffer), 10), 1278944337u),
            new TestCase<UInt64>(() => BinaryOperations.Read64(new ReadOnlySpan<Byte>(s_Buffer), 0), 18302629055311579566ul),
            new TestCase<UInt64>(() => BinaryOperations.Read64(new ReadOnlySpan<Byte>(s_Buffer), 5), 4261620661295644672ul)
        };

        private static readonly List<dynamic> s_TestCasesReadArray = new List<dynamic>
        {
            new TestCaseArray<UInt16>(() => BinaryOperations.ReadArray16(new ReadOnlySpan<Byte>(s_Buffer), 0, 3), new UInt16[] { 2478, 33175, 39 }),
            new TestCaseArray<UInt16>(() => BinaryOperations.ReadArray16(new ReadOnlySpan<Byte>(s_Buffer), 5, 3), new UInt16[] { 0, 37886, 20841 }),
            new TestCaseArray<UInt32>(() => BinaryOperations.ReadArray32(new ReadOnlySpan<Byte>(s_Buffer), 1, 2), new UInt32[] { 662804233, 2482896896 }),
            new TestCaseArray<UInt32>(() => BinaryOperations.ReadArray32(new ReadOnlySpan<Byte>(s_Buffer), 2, 3), new UInt32[] { 2589079, 1771306496, 1278944337 }),
            new TestCaseArray<UInt64>(() => BinaryOperations.ReadArray64(new ReadOnlySpan<Byte>(s_Buffer), 0, 2), new UInt64[] { 18302629055311579566, 16373483212154956179 }),
            new TestCaseArray<UInt64>(() => BinaryOperations.ReadArray64(new ReadOnlySpan<Byte>(s_Buffer), 3, 1), new UInt64[] { 5866382708757768065 })
        };

        private static readonly List<dynamic> s_TestCasesReadTail = new List<dynamic>
        {
            new TestCase<UInt32>(() => BinaryOperations.ReadTail32(new ReadOnlySpan<Byte>(s_Buffer), 0), 0u),
            new TestCase<UInt32>(() => BinaryOperations.ReadTail32(new ReadOnlySpan<Byte>(s_Buffer), 12), 3812248635u),
            new TestCase<UInt32>(() => BinaryOperations.ReadTail32(new ReadOnlySpan<Byte>(s_Buffer), 13), 14891596u),
            new TestCase<UInt32>(() => BinaryOperations.ReadTail32(new ReadOnlySpan<Byte>(s_Buffer), 14), 58170u),
            new TestCase<UInt32>(() => BinaryOperations.ReadTail32(new ReadOnlySpan<Byte>(s_Buffer), 15), 227u),
            new TestCase<UInt64>(() => BinaryOperations.ReadTail64(new ReadOnlySpan<Byte>(s_Buffer), 0), 0ul),
            new TestCase<UInt64>(() => BinaryOperations.ReadTail64(new ReadOnlySpan<Byte>(s_Buffer), 8), 16373483212154956179ul),
            new TestCase<UInt64>(() => BinaryOperations.ReadTail64(new ReadOnlySpan<Byte>(s_Buffer), 9), 63958918797480297ul),
            new TestCase<UInt64>(() => BinaryOperations.ReadTail64(new ReadOnlySpan<Byte>(s_Buffer), 10), 249839526552657ul),
            new TestCase<UInt64>(() => BinaryOperations.ReadTail64(new ReadOnlySpan<Byte>(s_Buffer), 11), 975935650596ul),
            new TestCase<UInt64>(() => BinaryOperations.ReadTail64(new ReadOnlySpan<Byte>(s_Buffer), 12), 3812248635ul),
            new TestCase<UInt64>(() => BinaryOperations.ReadTail64(new ReadOnlySpan<Byte>(s_Buffer), 13), 14891596ul),
            new TestCase<UInt64>(() => BinaryOperations.ReadTail64(new ReadOnlySpan<Byte>(s_Buffer), 14), 58170ul),
            new TestCase<UInt64>(() => BinaryOperations.ReadTail64(new ReadOnlySpan<Byte>(s_Buffer), 15), 227ul)
        };

        private static readonly List<dynamic> s_TestCasesRotation = new List<dynamic>
        {
            new TestCase<UInt16>(() => BinaryOperations.RotateLeft(1027, 0), 1027),
            new TestCase<UInt16>(() => BinaryOperations.RotateRight(1027, 5), 6176),
            new TestCase<UInt16>(() => BinaryOperations.RotateLeft(45561, 3), 36813),
            new TestCase<UInt16>(() => BinaryOperations.RotateRight(45561, 9), 64728),
            new TestCase<UInt32>(() => BinaryOperations.RotateLeft(10523123u, 0), 10523123u),
            new TestCase<UInt32>(() => BinaryOperations.RotateRight(10523123u, 17), 1224310864u),
            new TestCase<UInt32>(() => BinaryOperations.RotateLeft(944209717u, 8), 1199519032u),
            new TestCase<UInt32>(() => BinaryOperations.RotateRight(944209717u, 31), 1888419434u),
            new TestCase<UInt64>(() => BinaryOperations.RotateLeft(197029066322453301ul, 13), 9195376900806451287ul),
            new TestCase<UInt64>(() => BinaryOperations.RotateRight(197029066322453301ul, 49), 18334763529516253533ul),
            new TestCase<UInt64>(() => BinaryOperations.RotateLeft(6955140290117ul, 13), 56976509256638464ul),
            new TestCase<UInt64>(() => BinaryOperations.RotateRight(6955140290117ul, 62), 27820561160468ul)
        };

        private static readonly List<dynamic> s_TestCasesSwap = new List<dynamic>
        {
            new TestCase<UInt16>(() => { UInt16 a = 7, b = 4093; BinaryOperations.Swap(ref a, ref b); return a; }, 4093),
            new TestCase<UInt32>(() => { UInt32 a = 9488124u, b = 4123321u; BinaryOperations.Swap(ref a, ref b); return a; }, 4123321u),
            new TestCase<UInt64>(() => { UInt64 a = 234620953293ul, b = 6430983184821ul; BinaryOperations.Swap(ref a, ref b); return a; }, 6430983184821ul),
        };

        public static IEnumerable<Object[]> DataRead()
        {
            foreach (dynamic testCase in s_TestCasesRead)
                yield return (new Object[] { testCase.Method, testCase.ExpectedValue });
        }

        public static IEnumerable<Object[]> DataReadArray()
        {
            foreach (dynamic testCase in s_TestCasesReadArray)
                yield return (new Object[] { testCase.Method, testCase.ExpectedValue });
        }

        public static IEnumerable<Object[]> DataReadTail()
        {
            foreach (dynamic testCase in s_TestCasesReadTail)
                yield return (new Object[] { testCase.Method, testCase.ExpectedValue });
        }

        public static IEnumerable<Object[]> DataRotation()
        {
            foreach (dynamic testCase in s_TestCasesRotation)
                yield return (new Object[] { testCase.Method, testCase.ExpectedValue });
        }

        public static IEnumerable<Object[]> DataSwap()
        {
            foreach (dynamic testCase in s_TestCasesSwap)
                yield return (new Object[] { testCase.Method, testCase.ExpectedValue });
        }
        #endregion

        #region Nested Classes
        private sealed class TestCase<T> where T: struct, IComparable, IConvertible, IFormattable
        {
            #region Members
            private readonly Func<T> m_Method;
            private readonly T m_ExpectedValue;
            #endregion

            #region Properties
            public Func<T> Method => m_Method;
            public T ExpectedValue => m_ExpectedValue;
            #endregion

            #region Constructors
            public TestCase(Func<T> method, T expectedValue)
            {
                if (method == null)
                    throw new ArgumentNullException(nameof(method));

                m_Method = method;
                m_ExpectedValue = expectedValue;
            }
            #endregion

            #region Methods
            public override String ToString()
            {
                return $"{GetType().Name}: {nameof(ExpectedValue)}={m_ExpectedValue}";
            }
            #endregion
        }

        private sealed class TestCaseArray<T> where T : struct, IComparable, IConvertible, IFormattable
        {
            #region Members
            private readonly Func<T[]> m_Method;
            private readonly T[] m_ExpectedValue;
            #endregion

            #region Properties
            public Func<T[]> Method => m_Method;
            public T[] ExpectedValue => m_ExpectedValue;
            #endregion

            #region Constructors
            public TestCaseArray(Func<T[]> method, T[] expectedValue)
            {
                if (method == null)
                    throw new ArgumentNullException(nameof(method));

                m_Method = method;
                m_ExpectedValue = expectedValue;
            }
            #endregion

            #region Methods
            public override String ToString()
            {
                return $"{GetType().Name}: {nameof(ExpectedValue)}={Utilities.FormatNumericArray(m_ExpectedValue)}";
            }
            #endregion
        }
        #endregion
    }
}