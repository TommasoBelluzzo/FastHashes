#region Using Directives
using System;
using Xunit;
#endregion

namespace FastHashes.Tests
{
    public sealed class BinaryOperationsTests
    {
        #region Methods
        [Theory]
        [MemberData(nameof(BinaryOperationsTestsCases.DataRead), MemberType=typeof(BinaryOperationsTestsCases))]
        public void ReadTest<T>(Func<T> method, T expected) where T : struct, IComparable, IConvertible, IFormattable
        {
            T actual = method();
            Assert.Equal(expected, actual);
        }

        [Theory]
        [MemberData(nameof(BinaryOperationsTestsCases.DataReadArray), MemberType=typeof(BinaryOperationsTestsCases))]
        public void ReadArrayTest<T>(Func<T[]> method, T[] expected) where T : struct, IComparable, IConvertible, IFormattable
        {
            T[] actual = method();
            Assert.Equal(expected, actual);
        }

        [Theory]
        [MemberData(nameof(BinaryOperationsTestsCases.DataReadTail), MemberType=typeof(BinaryOperationsTestsCases))]
        public void ReadTailTest<T>(Func<T> method, T expected) where T : struct, IComparable, IConvertible, IFormattable
        {
            T actual = method();
            Assert.Equal(expected, actual);
        }

        [Theory]
        [MemberData(nameof(BinaryOperationsTestsCases.DataRotation), MemberType=typeof(BinaryOperationsTestsCases))]
        public void RotationTest<T>(Func<T> method, T expected)
        {
            T actual = method();
            Assert.Equal(expected, actual);
        }

        [Theory]
        [MemberData(nameof(BinaryOperationsTestsCases.DataSwap), MemberType=typeof(BinaryOperationsTestsCases))]
        public void SwapTest<T>(Func<T> method, T expected)
        {
            T actual = method();
            Assert.Equal(expected, actual);
        }
        #endregion
    }
}