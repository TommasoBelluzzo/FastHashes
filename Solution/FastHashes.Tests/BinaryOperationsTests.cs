#region Using Directives
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using Xunit.Abstractions;
#endregion

namespace FastHashes.Tests
{
    public sealed class BinaryOperationsTests
    {
        #region Test Cases
        private static readonly Byte[] s_Buffer = new Byte[] { 174, 9, 151, 129, 39, 0, 0, 254, 147, 105, 81, 36, 59, 76, 58, 227 };
        private static readonly String[] s_ValidTypes = new String[] { "UInt16", "UInt32", "UInt64" };

        private static readonly List<TestCaseRead> s_TestCasesRead = new List<TestCaseRead>
        {
            new TestCaseRead(5, (UInt16)0),
            new TestCaseRead(8, (UInt16)27027),
            new TestCaseRead(1, 662804233u),
            new TestCaseRead(10, 1278944337u),
            new TestCaseRead(0, 18302629055311579566ul),
            new TestCaseRead(5, 4261620661295644672ul)
        };

        private static readonly List<TestCaseReadArray> s_TestCasesReadArray = new List<TestCaseReadArray>
        {
            new TestCaseReadArray(0, 3, new UInt16[] { 2478, 33175, 39 }),
            new TestCaseReadArray(5, 3, new UInt16[] { 0, 37886, 20841 }),
            new TestCaseReadArray(1, 2, new UInt32[] { 662804233, 2482896896 }),
            new TestCaseReadArray(2, 3, new UInt32[] { 2589079, 1771306496, 1278944337 }),
            new TestCaseReadArray(0, 2, new UInt64[] { 18302629055311579566, 16373483212154956179 }),
            new TestCaseReadArray(3, 1, new UInt64[] { 5866382708757768065 })
        };

        private static readonly List<TestCaseRotation> s_TestCasesRotation = new List<TestCaseRotation>
        {
            new TestCaseRotation((UInt16)1027, 0, 5, (UInt16)1027, (UInt16)6176),
            new TestCaseRotation((UInt16)45561, 3, 9, (UInt16)36813, (UInt16)64728),
            new TestCaseRotation(10523123u, 0, 17, 10523123u, 1224310864u),
            new TestCaseRotation(944209717u, 8, 31, 1199519032u, 1888419434u),
            new TestCaseRotation(197029066322453301ul, 13, 49, 9195376900806451287ul, 18334763529516253533ul),
            new TestCaseRotation(6955140290117ul, 13, 62, 56976509256638464ul, 27820561160468ul)
        };

        private static readonly List<TestCaseSwap> s_TestCasesSwap = new List<TestCaseSwap>
        {
            new TestCaseSwap((UInt16)7, (UInt16)4093),
            new TestCaseSwap(9488124u, 4123321u),
            new TestCaseSwap(234620953293ul, 6430983184821ul)
        };

        public static IEnumerable<Object[]> DataRead()
        {
            foreach (TestCaseRead testCase in s_TestCasesRead)
                yield return (new Object[] { testCase.Method, testCase.ExpectedValue });
        }

        public static IEnumerable<Object[]> DataReadArray()
        {
            foreach (TestCaseReadArray testCase in s_TestCasesReadArray)
                yield return (new Object[] { testCase.Method, testCase.ExpectedValue });
        }

        public static IEnumerable<Object[]> DataRotation()
        {
            foreach (TestCaseRotation testCase in s_TestCasesRotation)
                yield return (new Object[] { testCase.MethodLeft, testCase.MethodRight, testCase.ExpectedValueLeft, testCase.ExpectedValueRight });
        }

        public static IEnumerable<Object[]> DataSwap()
        {
            foreach (TestCaseSwap testCase in s_TestCasesSwap)
                yield return (new Object[] { testCase.Method, testCase.ValueLeft, testCase.ValueRight, testCase.ExpectedValueLeft, testCase.ExpectedValueRight });
        }
        #endregion

        #region Members
        private readonly ITestOutputHelper m_Output;
        #endregion

        #region Constructors
        public BinaryOperationsTests(ITestOutputHelper output)
        {
            m_Output = output;
        }
        #endregion

        #region Methods
        [Theory]
        [MemberData(nameof(DataRead))]
        public void ReadTest(Func<Object> method, Object expectedValue)
        {
            Object actualValue = method();

            m_Output.WriteLine($"EXPECTED: {expectedValue}");
            m_Output.WriteLine($"ACTUAL: {actualValue}");

            Assert.Equal(expectedValue, actualValue);
        }

        [Theory]
        [MemberData(nameof(DataReadArray))]
        public void ReadArrayTest(Func<Object> method, Object expectedValue)
        {
            Object actualValue = method();

            m_Output.WriteLine($"EXPECTED: {Utilities.FormatNumericArray(expectedValue)}");
            m_Output.WriteLine($"ACTUAL: {Utilities.FormatNumericArray(actualValue)}");

            Assert.Equal(expectedValue, actualValue);
        }

        [Theory]
        [MemberData(nameof(DataRotation))]
        public void RotationTest(Func<Object> methodLeft, Func<Object> methodRight, Object expectedValueLeft, Object expectedValueRight)
        {
            Object actualValueLeft = methodLeft();
            Object actualValueRight = methodRight();

            m_Output.WriteLine($"EXPECTED:\n LEFT={expectedValueLeft}\n RIGHT={expectedValueRight}");
            m_Output.WriteLine($"ACTUAL:\n LEFT={actualValueLeft}\n RIGHT={actualValueRight}");

            Assert.Equal(expectedValueLeft, actualValueLeft);
            Assert.Equal(expectedValueRight, actualValueRight);
        }

        [Theory]
        [MemberData(nameof(DataSwap))]
        public void SwapTest(Func<Object,Object,(Object,Object)> method, Object valueLeft, Object valueRight, Object expectedValueLeft, Object expectedValueRight)
        {
            (Object actualValueLeft, Object actualValueRight) = method(valueLeft, valueRight);

            m_Output.WriteLine($"EXPECTED:\n LEFT={expectedValueLeft}\n RIGHT={expectedValueRight}");
            m_Output.WriteLine($"ACTUAL:\n LEFT={actualValueLeft}\n RIGHT={actualValueRight}");

            Assert.Equal(expectedValueLeft, actualValueLeft);
            Assert.Equal(expectedValueRight, actualValueRight);
        }
        #endregion

        #region Nested Classes
        private sealed class TestCaseRead
        {
            #region Members
            private readonly Func<Object> m_Method;
            private readonly Int32 m_Offset;
            private readonly Object m_ExpectedValue;
            #endregion

            #region Properties
            public Func<Object> Method => m_Method;
            public Int32 Offset => m_Offset;
            public Object ExpectedValue => m_ExpectedValue;
            #endregion

            #region Constructors
            public TestCaseRead(Int32 offset, Object expectedValue)
            {
                if (offset < 0)
                    throw new ArgumentException("Invalid offset specified.", nameof(offset));

                if (expectedValue == null)
                    throw new ArgumentException("Invalid expected value specified.", nameof(expectedValue));

                String expectedValueType = expectedValue.GetType().Name;
                
                switch (expectedValueType)
                {
                    case "UInt16":
                        m_Method = () => BinaryOperations.Read16(new ReadOnlySpan<Byte>(s_Buffer), offset);
                        break;

                    case "UInt32":
                        m_Method = () => BinaryOperations.Read32(new ReadOnlySpan<Byte>(s_Buffer), offset);
                        break;

                    case "UInt64":
                        m_Method = () => BinaryOperations.Read64(new ReadOnlySpan<Byte>(s_Buffer), offset);
                        break;

                    default:
                        throw new ArgumentException("Invalid expected value specified.", nameof(expectedValue));
                }

                m_Offset = offset;
                m_ExpectedValue = expectedValue;
            }
            #endregion

            #region Methods
            public override String ToString()
            {
                return $"{GetType().Name}: {nameof(Offset)}={m_Offset} {nameof(ExpectedValue)}={m_ExpectedValue}";
            }
            #endregion
        }

        private sealed class TestCaseReadArray
        {
            #region Members
            private readonly Func<Object> m_Method;
            private readonly Int32 m_Count;
            private readonly Int32 m_Offset;
            private readonly Object m_ExpectedValue;
            #endregion

            #region Properties
            public Func<Object> Method => m_Method;
            public Int32 Count => m_Count;
            public Int32 Offset => m_Offset;
            public Object ExpectedValue => m_ExpectedValue;
            #endregion

            #region Constructors
            public TestCaseReadArray(Int32 offset, Int32 count, Object expectedValue)
            {
                if (offset < 0)
                    throw new ArgumentException("Invalid offset specified.", nameof(offset));

                if (count < 0)
                    throw new ArgumentException("Invalid count specified.", nameof(count));

                if (expectedValue == null)
                    throw new ArgumentException("Invalid expected value specified.", nameof(expectedValue));

                String expectedValueType = expectedValue.GetType().Name;

                switch (expectedValueType)
                {
                    case "UInt16[]":
                        m_Method = () => BinaryOperations.ReadArray16(new ReadOnlySpan<Byte>(s_Buffer), offset, count);
                        break;

                    case "UInt32[]":
                        m_Method = () => BinaryOperations.ReadArray32(new ReadOnlySpan<Byte>(s_Buffer), offset, count);
                        break;

                    case "UInt64[]":
                        m_Method = () => BinaryOperations.ReadArray64(new ReadOnlySpan<Byte>(s_Buffer), offset, count);
                        break;

                    default:
                        throw new ArgumentException("Invalid expected value specified.", nameof(expectedValue));
                }

                m_Count = count;
                m_Offset = offset;
                m_ExpectedValue = expectedValue;
            }
            #endregion

            #region Methods
            public override String ToString()
            {
                return $"{GetType().Name}: {nameof(Offset)}={m_Offset} {nameof(Count)}={m_Count} {nameof(ExpectedValue)}={Utilities.FormatNumericArray(m_ExpectedValue)}";
            }
            #endregion
        }

        private sealed class TestCaseRotation
        {
            #region Members
            private readonly Func<Object> m_MethodLeft;
            private readonly Func<Object> m_MethodRight;
            private readonly Int32 m_RotationLeft;
            private readonly Int32 m_RotationRight;
            private readonly Object m_ExpectedValueLeft;
            private readonly Object m_ExpectedValueRight;
            private readonly Object m_Value;
            #endregion

            #region Properties
            public Func<Object> MethodLeft => m_MethodLeft;
            public Func<Object> MethodRight => m_MethodRight;
            public Int32 RotationLeft => m_RotationLeft;
            public Int32 RotationRight => m_RotationRight;
            public Object ExpectedValueLeft => m_ExpectedValueLeft;
            public Object ExpectedValueRight => m_ExpectedValueRight;
            public Object Value => m_Value;
            #endregion

            #region Constructors
            public TestCaseRotation(Object value, Int32 rotationLeft, Int32 rotationRight, Object expectedValueLeft, Object expectedValueRight)
            {
                if (value == null)
                    throw new ArgumentException("Invalid value specified.", nameof(value));

                String valueType = value.GetType().Name;

                if (!s_ValidTypes.Contains(valueType))
                    throw new ArgumentException("Invalid value specified.", nameof(value));

                if (rotationLeft < 0)
                    throw new ArgumentException("Invalid left rotation specified.", nameof(rotationLeft));

                if (rotationRight < 0)
                    throw new ArgumentException("Invalid right rotation specified.", nameof(rotationRight));

                if ((expectedValueLeft == null) || !String.Equals(expectedValueLeft.GetType().Name, valueType, StringComparison.Ordinal))
                    throw new ArgumentException("Invalid left expected value specified.", nameof(expectedValueLeft));

                if ((expectedValueRight == null) || !String.Equals(expectedValueRight.GetType().Name, valueType, StringComparison.Ordinal))
                    throw new ArgumentException("Invalid right expected value specified.", nameof(expectedValueRight));

                switch (valueType)
                {
                    case "UInt16":
                        m_MethodLeft = () => BinaryOperations.RotateLeft((UInt16)value, rotationLeft);
                        m_MethodRight = () => BinaryOperations.RotateRight((UInt16)value, rotationRight);
                        break;

                    case "UInt32":
                        m_MethodLeft = () => BinaryOperations.RotateLeft((UInt32)value, rotationLeft);
                        m_MethodRight = () => BinaryOperations.RotateRight((UInt32)value, rotationRight);
                        break;

                    default:
                        m_MethodLeft = () => BinaryOperations.RotateLeft((UInt64)value, rotationLeft);
                        m_MethodRight = () => BinaryOperations.RotateRight((UInt64)value, rotationRight);
                        break;
                }

                m_RotationLeft = rotationLeft;
                m_RotationRight = rotationRight;
                m_ExpectedValueLeft = expectedValueLeft;
                m_ExpectedValueRight = expectedValueRight;
                m_Value = value;
            }
            #endregion

            #region Methods
            public override String ToString()
            {
                return $"{GetType().Name}: {nameof(Value)}={m_Value} {nameof(RotationLeft)}={m_RotationLeft} {nameof(RotationRight)}={m_RotationRight} {nameof(ExpectedValueLeft)}={m_ExpectedValueLeft} {nameof(ExpectedValueRight)}={m_ExpectedValueRight}";
            }
            #endregion
        }

        private sealed class TestCaseSwap
        {
            #region Members
            private readonly Func<Object,Object,(Object,Object)> m_Method;
            private readonly Object m_ExpectedValueLeft;
            private readonly Object m_ExpectedValueRight;
            private readonly Object m_ValueLeft;
            private readonly Object m_ValueRight;
            #endregion

            #region Properties
            public Func<Object,Object,(Object,Object)> Method => m_Method;
            public Object ExpectedValueLeft => m_ExpectedValueLeft;
            public Object ExpectedValueRight => m_ExpectedValueRight;
            public Object ValueLeft => m_ValueLeft;
            public Object ValueRight => m_ValueRight;
            #endregion

            #region Constructors
            public TestCaseSwap(Object valueLeft, Object valueRight)
            {
                if (valueLeft == null)
                    throw new ArgumentException("Invalid left value specified.", nameof(valueLeft));

                String valueLeftType = valueLeft.GetType().Name;

                if (!s_ValidTypes.Contains(valueLeftType))
                    throw new ArgumentException("Invalid left value specified.", nameof(valueLeft));

                if (valueRight == null)
                    throw new ArgumentException("Invalid right value specified.", nameof(valueRight));

                String valueRightType = valueRight.GetType().Name;

                if (!s_ValidTypes.Contains(valueRightType))
                    throw new ArgumentException("Invalid right value specified.", nameof(valueRight));

                if (!String.Equals(valueLeftType, valueRightType, StringComparison.Ordinal))
                    throw new ArgumentException("Mismatching values type.");

                switch (valueLeftType)
                {
                    case "UInt16":
                        m_Method = (Object left, Object right) =>
                        {
                            UInt16 leftCast = (UInt16)valueLeft;
                            UInt16 rightCast = (UInt16)valueRight;

                            BinaryOperations.Swap(ref leftCast, ref rightCast);

                            return (leftCast, rightCast);
                        };
                        break;

                    case "UInt32":
                        m_Method = (Object left, Object right) =>
                        {
                            UInt32 leftCast = (UInt32)valueLeft;
                            UInt32 rightCast = (UInt32)valueRight;

                            BinaryOperations.Swap(ref leftCast, ref rightCast);

                            return (leftCast, rightCast);
                        };
                        break;

                    default:
                        m_Method = (Object left, Object right) =>
                        {
                            UInt64 leftCast = (UInt64)valueLeft;
                            UInt64 rightCast = (UInt64)valueRight;

                            BinaryOperations.Swap(ref leftCast, ref rightCast);

                            return (leftCast, rightCast);
                        };
                        break;
                }

                m_ExpectedValueLeft = valueRight;
                m_ExpectedValueRight = valueLeft;
                m_ValueLeft = valueLeft;
                m_ValueRight = valueRight;
            }
            #endregion

            #region Methods
            public override String ToString()
            {
                return $"{GetType().Name}: {nameof(ValueLeft)}={m_ValueLeft} {nameof(ValueRight)}={m_ValueRight} {nameof(ExpectedValueLeft)}={m_ExpectedValueLeft} {nameof(ExpectedValueRight)}={m_ExpectedValueRight}";
            }
            #endregion
        }
        #endregion
    }
}