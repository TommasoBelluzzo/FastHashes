#region Using Directives
using System;
using Xunit;
#endregion

namespace FastHashes.Tests
{
    public sealed class BinaryOperationsTests
    {
        #region Constants
        private static readonly Byte[] s_Buffer = [174, 9, 151, 129, 39, 0, 0, 254, 147, 105, 81, 36, 59, 76, 58, 227];
        #endregion

        #region Tests: Reading Operations
        [Theory]
        [InlineData(16, -1)]
        [InlineData(16, 16)]
        [InlineData(16, 15)]
        [InlineData(32, -1)]
        [InlineData(32, 16)]
        [InlineData(32, 13)]
        [InlineData(64, -1)]
        [InlineData(64, 16)]
        [InlineData(64, 9)]
        public void ReadInvalidArgumentsTest(Int32 width, Int32 offset)
        {
            switch (width)
            {
                case 16:
                    Assert.Throws<ArgumentOutOfRangeException>(() => BinaryOperations.Read16(s_Buffer, offset));
                    break;
                case 32:
                    Assert.Throws<ArgumentOutOfRangeException>(() => BinaryOperations.Read32(s_Buffer, offset));
                    break;
                case 64:
                    Assert.Throws<ArgumentOutOfRangeException>(() => BinaryOperations.Read64(s_Buffer, offset));
                    break;
            }
        }

        [Theory]
        [InlineData(5, (UInt16)0)]
        [InlineData(8, (UInt16)27027)]
        public void Read16Test(Int32 offset, UInt16 expected)
        {
            UInt16 actual = BinaryOperations.Read16(s_Buffer, offset);
            Assert.Equal(expected, actual);
        }

        [Theory]
        [InlineData(1, 662804233u)]
        [InlineData(10, 1278944337u)]
        public void Read32Test(Int32 offset, UInt32 expected)
        {
            UInt32 actual = BinaryOperations.Read32(s_Buffer, offset);
            Assert.Equal(expected, actual);
        }

        [Theory]
        [InlineData(0, 18302629055311579566ul)]
        [InlineData(5, 4261620661295644672ul)]
        public void Read64Test(Int32 offset, UInt64 expected)
        {
            UInt64 actual = BinaryOperations.Read64(s_Buffer, offset);
            Assert.Equal(expected, actual);
        }

        [Theory]
        [InlineData(16, 15, 1)]
        [InlineData(16, 0, 9)]
        [InlineData(32, 13, 1)]
        [InlineData(32, 0, 5)]
        [InlineData(64, 9, 1)]
        [InlineData(64, 0, 3)]
        public void ReadArrayInvalidArgumentsTest(Int32 width, Int32 offset, Int32 count)
        {
            switch (width)
            {
                case 16:
                    Assert.Throws<ArgumentOutOfRangeException>(() => BinaryOperations.ReadArray16(s_Buffer, offset, count));
                    break;
                case 32:
                    Assert.Throws<ArgumentOutOfRangeException>(() => BinaryOperations.ReadArray32(s_Buffer, offset, count));
                    break;
                case 64:
                    Assert.Throws<ArgumentOutOfRangeException>(() => BinaryOperations.ReadArray64(s_Buffer, offset, count));
                    break;
            }
        }

        [Theory]
        [InlineData(16)]
        [InlineData(32)]
        [InlineData(64)]
        public void ReadArrayEmptyTest(Int32 width)
        {
            switch (width)
            {
                case 16:
                    Assert.Empty(BinaryOperations.ReadArray16(s_Buffer, s_Buffer.Length, 0));
                    break;
                
                case 32:
                    Assert.Empty(BinaryOperations.ReadArray32(s_Buffer, s_Buffer.Length, 0));
                    break;
                
                case 64:
                    Assert.Empty(BinaryOperations.ReadArray64(s_Buffer, s_Buffer.Length, 0));
                    break;
            }
        }

        [Theory]
        [InlineData(0, 3, new UInt16[] { 2478, 33175, 39 })]
        [InlineData(5, 3, new UInt16[] { 0, 37886, 20841 })]
        public void ReadArray16Test(Int32 offset, Int32 count, UInt16[] expected)
        {
            UInt16[] actual = BinaryOperations.ReadArray16(s_Buffer, offset, count);
            Assert.Equal(expected, actual);
        }

        [Theory]
        [InlineData(1, 2, new UInt32[] { 662804233u, 2482896896u })]
        [InlineData(2, 3, new UInt32[] { 2589079u, 1771306496u, 1278944337u })]
        public void ReadArray32Test(Int32 offset, Int32 count, UInt32[] expected)
        {
            UInt32[] actual = BinaryOperations.ReadArray32(s_Buffer, offset, count);
            Assert.Equal(expected, actual);
        }

        [Theory]
        [InlineData(0, 2, new UInt64[] { 18302629055311579566ul, 16373483212154956179ul })]
        [InlineData(3, 1, new UInt64[] { 5866382708757768065ul })]
        public void ReadArray64Test(Int32 offset, Int32 count, UInt64[] expected)
        {
            UInt64[] actual = BinaryOperations.ReadArray64(s_Buffer, offset, count);
            Assert.Equal(expected, actual);
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(17)]
        public void ReadTailInvalidArgumentsTest(Int32 offset)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => BinaryOperations.ReadTail32(s_Buffer, offset));
            Assert.Throws<ArgumentOutOfRangeException>(() => BinaryOperations.ReadTail64(s_Buffer, offset));
        }

        [Theory]
        [InlineData(0, 0u)]
        [InlineData(12, 3812248635u)]
        [InlineData(13, 14891596u)]
        [InlineData(14, 58170u)]
        [InlineData(15, 227u)]
        [InlineData(16, 0u)]
        public void ReadTail32Test(Int32 offset, UInt32 expected)
        {
            UInt32 actual = BinaryOperations.ReadTail32(s_Buffer, offset);
            Assert.Equal(expected, actual);
        }

        [Theory]
        [InlineData(0, 0ul)]
        [InlineData(8, 16373483212154956179ul)]
        [InlineData(9, 63958918797480297ul)]
        [InlineData(10, 249839526552657ul)]
        [InlineData(11, 975935650596ul)]
        [InlineData(12, 3812248635ul)]
        [InlineData(13, 14891596ul)]
        [InlineData(14, 58170ul)]
        [InlineData(15, 227ul)]
        [InlineData(16, 0ul)]
        public void ReadTail64Test(Int32 offset, UInt64 expected)
        {
            UInt64 actual = BinaryOperations.ReadTail64(s_Buffer, offset);
            Assert.Equal(expected, actual);
        }
        #endregion

        #region Tests: Rotation Operations
        [Theory]
        [InlineData(true, (UInt16)1027, 0, (UInt16)1027)]
        [InlineData(false, (UInt16)1027, 5, (UInt16)6176)]
        [InlineData(true, (UInt16)45561, 3, (UInt16)36813)]
        [InlineData(false, (UInt16)45561, 9, (UInt16)64728)]
        public void Rotation16Test(Boolean left, UInt16 value, Int32 rotation, UInt16 expected)
        {
            UInt16 actual = left ? BinaryOperations.RotateLeft(value, rotation) : BinaryOperations.RotateRight(value, rotation);
            Assert.Equal(expected, actual);
        }

        [Theory]
        [InlineData(true, 10523123u, 0, 10523123u)]
        [InlineData(false, 10523123u, 17, 1224310864u)]
        [InlineData(true, 944209717u, 8, 1199519032u)]
        [InlineData(false, 944209717u, 31, 1888419434u)]
        public void Rotation32Test(Boolean left, UInt32 value, Int32 rotation, UInt32 expected)
        {
            UInt32 actual = left ? BinaryOperations.RotateLeft(value, rotation) : BinaryOperations.RotateRight(value, rotation);
            Assert.Equal(expected, actual);
        }

        [Theory]
        [InlineData(true, 197029066322453301ul, 13, 9195376900806451287ul)]
        [InlineData(false, 197029066322453301ul, 49, 18334763529516253533ul)]
        [InlineData(true, 6955140290117ul, 13, 56976509256638464ul)]
        [InlineData(false, 6955140290117ul, 62, 27820561160468ul)]
        public void Rotation64Test(Boolean left, UInt64 value, Int32 rotation, UInt64 expected)
        {
            UInt64 actual = left ? BinaryOperations.RotateLeft(value, rotation) : BinaryOperations.RotateRight(value, rotation);
            Assert.Equal(expected, actual);
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(15)]
        [InlineData(16)]
        [InlineData(17)]
        [InlineData(31)]
        [InlineData(32)]
        [InlineData(63)]
        [InlineData(64)]
        [InlineData(65)]
        [InlineData(Int32.MaxValue)]
        public void RotationInverseTest(Int32 rotation)
        {
            UInt16 value16 = (UInt16)0xA35C;
            UInt32 value32 = 0x89ABCDEFu;
            UInt64 value64 = 0x0123456789ABCDEFul;

            Assert.Equal(value16, BinaryOperations.RotateRight(BinaryOperations.RotateLeft(value16, rotation), rotation));
            Assert.Equal(value16, BinaryOperations.RotateLeft(BinaryOperations.RotateRight(value16, rotation), rotation));
            Assert.Equal(value32, BinaryOperations.RotateRight(BinaryOperations.RotateLeft(value32, rotation), rotation));
            Assert.Equal(value32, BinaryOperations.RotateLeft(BinaryOperations.RotateRight(value32, rotation), rotation));
            Assert.Equal(value64, BinaryOperations.RotateRight(BinaryOperations.RotateLeft(value64, rotation), rotation));
            Assert.Equal(value64, BinaryOperations.RotateLeft(BinaryOperations.RotateRight(value64, rotation), rotation));
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(13)]
        [InlineData(65)]
        public void RotationPeriodicityTest(Int32 rotation)
        {
            UInt16 value16 = (UInt16)0xA35C;
            UInt32 value32 = 0x89ABCDEFu;
            UInt64 value64 = 0x0123456789ABCDEFul;

            Assert.Equal(BinaryOperations.RotateLeft(value16, rotation), BinaryOperations.RotateLeft(value16, rotation + 16));
            Assert.Equal(BinaryOperations.RotateRight(value16, rotation), BinaryOperations.RotateRight(value16, rotation + 16));
            Assert.Equal(BinaryOperations.RotateLeft(value32, rotation), BinaryOperations.RotateLeft(value32, rotation + 32));
            Assert.Equal(BinaryOperations.RotateRight(value32, rotation), BinaryOperations.RotateRight(value32, rotation + 32));
            Assert.Equal(BinaryOperations.RotateLeft(value64, rotation), BinaryOperations.RotateLeft(value64, rotation + 64));
            Assert.Equal(BinaryOperations.RotateRight(value64, rotation), BinaryOperations.RotateRight(value64, rotation + 64));
        }
        #endregion

        #region Tests: Conversion Operations
        [Theory]
        [InlineData(new UInt32[] { }, "")]
        [InlineData(new UInt32[] { 0x00000000u }, "00000000")]
        [InlineData(new UInt32[] { 0x12345678u }, "78563412")]
        [InlineData(new UInt32[] { 0xFFFFFFFFu }, "FFFFFFFF")]
        [InlineData(new UInt32[] { 0x12345678u, 0x9ABCDEF0u }, "78563412F0DEBC9A")]
        [InlineData(new UInt32[] { 0x00000000u, 0x00000001u, 0x0C000000u, UInt32.MaxValue }, "00000000010000000000000CFFFFFFFF")]
        public void ToArray32Test(UInt32[] values, String expected)
        {
            Byte[] actual = values.Length switch
            {
                1 => BinaryOperations.ToArray32(values[0]),
                2 => BinaryOperations.ToArray32(values[0], values[1]),
                4 => BinaryOperations.ToArray32(values[0], values[1], values[2], values[3]),
                _ => BinaryOperations.ToArray32(values)
            };

            Assert.Equal(expected, Convert.ToHexString(actual));
        }

        [Theory]
        [InlineData(new UInt32[] { }, "")]
        [InlineData(new UInt32[] { 0x00000022u, 0xA0000000u }, "22000000000000A0")]
        public void ToArray32ArrayTest(UInt32[] values, String expected)
        {
            Byte[] actual = BinaryOperations.ToArray32(values);
            Assert.Equal(expected, Convert.ToHexString(actual));
        }

        [Theory]
        [InlineData(0x0123456789ABCDEFul, "EFCDAB89")]
        [InlineData(0xFFFFFFFF00000000ul, "00000000")]
        public void ToArray32TruncationTest(UInt64 value, String expected)
        {
            Byte[] actual = BinaryOperations.ToArray32(value);
            Assert.Equal(expected, Convert.ToHexString(actual));
        }

        [Theory]
        [InlineData(new UInt64[] { }, "")]
        [InlineData(new UInt64[] { 0x0000000000000000ul }, "0000000000000000")]
        [InlineData(new UInt64[] { 0x0123456789ABCDEFul }, "EFCDAB8967452301")]
        [InlineData(new UInt64[] { 0xFFFFFFFFFFFFFFFFul }, "FFFFFFFFFFFFFFFF")]
        [InlineData(new UInt64[] { 0x0123456789ABCDEFul, 0x0102030405060708ul }, "EFCDAB89674523010807060504030201")]
        [InlineData(new UInt64[] { 0x0000000000000000ul, 0x0000000000000001ul, 0x0C00000000000000ul, UInt64.MaxValue }, "00000000000000000100000000000000000000000000000CFFFFFFFFFFFFFFFF")]
        public void ToArray64Test(UInt64[] values, String expected)
        {
            Byte[] actual = values.Length switch
            {
                1 => BinaryOperations.ToArray64(values[0]),
                2 => BinaryOperations.ToArray64(values[0], values[1]),
                4 => BinaryOperations.ToArray64(values[0], values[1], values[2], values[3]),
                _ => BinaryOperations.ToArray64(values)
            };

            Assert.Equal(expected, Convert.ToHexString(actual));
        }

        [Theory]
        [InlineData(new UInt64[] { }, "")]
        [InlineData(new UInt64[] { 0x0000000000000022ul, 0xA000000000000000ul }, "220000000000000000000000000000A0")]
        public void ToArray64ArrayTest(UInt64[] values, String expected)
        {
            Byte[] actual = BinaryOperations.ToArray64(values);
            Assert.Equal(expected, Convert.ToHexString(actual));
        }

        [Theory]
        [InlineData(0x12345678u, "7856341200000000")]
        [InlineData(0xFFFFFFFFu, "FFFFFFFF00000000")]
        public void ToArray64WideningTest(UInt32 value, String expected)
        {
            Byte[] actual = BinaryOperations.ToArray64(value);
            Assert.Equal(expected, Convert.ToHexString(actual));
        }

        [Theory]
        [InlineData(0x00000000u)]
        [InlineData(0x12345678u)]
        [InlineData(UInt32.MaxValue)]
        public void ConversionRoundTrip32Test(UInt32 value)
        {
            Byte[] buffer = BinaryOperations.ToArray32(value);
            Assert.Equal(value, BinaryOperations.Read32(buffer, 0));
        }

        [Theory]
        [InlineData(0x0000000000000000ul)]
        [InlineData(0x0123456789ABCDEFul)]
        [InlineData(UInt64.MaxValue)]
        public void ConversionRoundTrip64Test(UInt64 value)
        {
            Byte[] buffer = BinaryOperations.ToArray64(value);
            Assert.Equal(value, BinaryOperations.Read64(buffer, 0));
        }

        [Fact]
        public void ConversionOverloadsTest()
        {
            UInt32[] values32 = [0x12345678u, 0x9ABCDEF0u, 0x01234567u, 0x89ABCDEFu];
            UInt64[] values64 = [0x0123456789ABCDEFul, 0xFEDCBA9876543210ul, 0x0000000000000001ul, UInt64.MaxValue];

            Assert.Equal(BinaryOperations.ToArray32([values32[0]]), BinaryOperations.ToArray32(values32[0]));
            Assert.Equal(BinaryOperations.ToArray32([values32[0], values32[1]]), BinaryOperations.ToArray32(values32[0], values32[1]));
            Assert.Equal(BinaryOperations.ToArray32(values32), BinaryOperations.ToArray32(values32[0], values32[1], values32[2], values32[3]));
            Assert.Equal(BinaryOperations.ToArray64([values64[0]]), BinaryOperations.ToArray64(values64[0]));
            Assert.Equal(BinaryOperations.ToArray64([values64[0], values64[1]]), BinaryOperations.ToArray64(values64[0], values64[1]));
            Assert.Equal(BinaryOperations.ToArray64(values64), BinaryOperations.ToArray64(values64[0], values64[1], values64[2], values64[3]));
        }
        #endregion

        #region Tests: Swap Operations
        [Theory]
        [InlineData((UInt16)7, (UInt16)4093)]
        [InlineData((UInt16)0, UInt16.MaxValue)]
        public void Swap16Test(UInt16 value1, UInt16 value2)
        {
            UInt16 actual1 = value1;
            UInt16 actual2 = value2;

            BinaryOperations.Swap(ref actual1, ref actual2);

            Assert.Equal(value2, actual1);
            Assert.Equal(value1, actual2);
        }

        [Theory]
        [InlineData(9488124u, 4123321u)]
        [InlineData(0u, UInt32.MaxValue)]
        public void Swap32Test(UInt32 value1, UInt32 value2)
        {
            UInt32 actual1 = value1;
            UInt32 actual2 = value2;

            BinaryOperations.Swap(ref actual1, ref actual2);

            Assert.Equal(value2, actual1);
            Assert.Equal(value1, actual2);
        }

        [Theory]
        [InlineData(234620953293ul, 6430983184821ul)]
        [InlineData(0ul, UInt64.MaxValue)]
        public void Swap64Test(UInt64 value1, UInt64 value2)
        {
            UInt64 actual1 = value1;
            UInt64 actual2 = value2;

            BinaryOperations.Swap(ref actual1, ref actual2);

            Assert.Equal(value2, actual1);
            Assert.Equal(value1, actual2);
        }

        [Fact]
        public void SwapSelfTest()
        {
            UInt16 value16 = (UInt16)0xA35C;
            UInt32 value32 = 0x89ABCDEFu;
            UInt64 value64 = 0x0123456789ABCDEFul;

            BinaryOperations.Swap(ref value16, ref value16);
            BinaryOperations.Swap(ref value32, ref value32);
            BinaryOperations.Swap(ref value64, ref value64);

            Assert.Equal((UInt16)0xA35C, value16);
            Assert.Equal(0x89ABCDEFu, value32);
            Assert.Equal(0x0123456789ABCDEFul, value64);
        }
        #endregion
    }
}