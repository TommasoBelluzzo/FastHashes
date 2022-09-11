#region Using Directives
using System;
using Xunit;
using Xunit.Abstractions;
#endregion

namespace FastHashes.Tests
{
    public sealed class BinaryOperationsTests
    {
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
        [MemberData(nameof(BinaryOperationsTestsCases.DataRead), MemberType=typeof(BinaryOperationsTestsCases))]
        public void ReadTest<T>(Func<T> method, T expectedValue) where T : struct, IComparable, IConvertible, IFormattable
        {
            T actualValue = method();

            m_Output.WriteLine($"EXPECTED: {expectedValue}");
            m_Output.WriteLine($"ACTUAL: {actualValue}");

            Assert.Equal(expectedValue, actualValue);
        }

        [Theory]
        [MemberData(nameof(BinaryOperationsTestsCases.DataReadArray), MemberType=typeof(BinaryOperationsTestsCases))]
        public void ReadArrayTest<T>(Func<T[]> method, T[] expectedValue) where T : struct, IComparable, IConvertible, IFormattable
        {
            T[] actualValue = method();

            m_Output.WriteLine($"EXPECTED: {Utilities.FormatNumericArray(expectedValue)}");
            m_Output.WriteLine($"ACTUAL: {Utilities.FormatNumericArray(actualValue)}");

            Assert.Equal(expectedValue, actualValue);
        }

        [Theory]
        [MemberData(nameof(BinaryOperationsTestsCases.DataReadTail), MemberType=typeof(BinaryOperationsTestsCases))]
        public void ReadTailTest<T>(Func<T> method, T expectedValue) where T : struct, IComparable, IConvertible, IFormattable
        {
            T actualValue = method();

            m_Output.WriteLine($"EXPECTED: {expectedValue}");
            m_Output.WriteLine($"ACTUAL: {actualValue}");

            Assert.Equal(expectedValue, actualValue);
        }

        [Theory]
        [MemberData(nameof(BinaryOperationsTestsCases.DataRotation), MemberType=typeof(BinaryOperationsTestsCases))]
        public void RotationTest<T>(Func<T> method, T expectedValue)
        {
            T actualValue = method();

            m_Output.WriteLine($"EXPECTED: {expectedValue}");
            m_Output.WriteLine($"ACTUAL: {actualValue}");

            Assert.Equal(expectedValue, actualValue);
        }

        [Theory]
        [MemberData(nameof(BinaryOperationsTestsCases.DataSwap), MemberType=typeof(BinaryOperationsTestsCases))]
        public void SwapTest<T>(Func<T> method, T expectedValue)
        {
            T actualValue = method();

            m_Output.WriteLine($"EXPECTED: {expectedValue}");
            m_Output.WriteLine($"ACTUAL: {actualValue}");

            Assert.Equal(expectedValue, actualValue);
        }
        #endregion
    }
}