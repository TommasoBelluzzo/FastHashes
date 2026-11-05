
#region Using Directives
using System;
using Xunit;
#endregion

namespace FastHashes.Tests
{
    public sealed class HashFormatterTests
    {
        #region Constants
        private const String HASH_1 = "00DEADBEEF";
        private const String HASH_2 = "F1D00505054CC105000501";
        #endregion

        #region Tests
        [Theory]
        [InlineData(null, HashFormat.Hexadecimal, typeof(ArgumentNullException), "hash")]
        [InlineData("01", (HashFormat)(-1), typeof(ArgumentOutOfRangeException), "format")]
        [InlineData("01", (HashFormat)Int32.MaxValue, typeof(ArgumentOutOfRangeException), "format")]
        public void FormatInvalidTest(String hashHex, HashFormat format, Type exceptionType, String parameterName)
        {
            Byte[] hash = hashHex == null ? null : Convert.FromHexString(hashHex);

            Exception exception = Record.Exception(() => HashFormatter.Format(hash, format));

            Assert.NotNull(exception);
            Assert.Equal(exceptionType, exception.GetType());
            Assert.Equal(parameterName, ((ArgumentException)exception).ParamName);

            if (hash != null)
            {
                exception = Record.Exception(() => HashFormatter.Format(new ReadOnlySpan<Byte>(hash), format));

                Assert.NotNull(exception);
                Assert.Equal(exceptionType, exception.GetType());
                Assert.Equal(parameterName, ((ArgumentException)exception).ParamName);
            }
        }

        [Theory]
        [InlineData("", HashFormat.Base64, "")]
        [InlineData("", HashFormat.Base64Url, "")]
        [InlineData("", HashFormat.Binary, "")]
        [InlineData("", HashFormat.Bytes, "")]
        [InlineData("", HashFormat.BytesLower, "")]
        [InlineData("", HashFormat.Hexadecimal, "")]
        [InlineData("", HashFormat.HexadecimalLower, "")]
        [InlineData("", HashFormat.HexadecimalPrefixed, "")]
        [InlineData("", HashFormat.HexadecimalLowerPrefixed, "")]
        [InlineData("FF00FF", HashFormat.Base64, "/wD/")]
        [InlineData("FF00FF", HashFormat.Base64Url, "_wD_")]
        [InlineData("0001", HashFormat.Binary, "0000000000000001")]
        [InlineData("0001", HashFormat.Bytes, "00-01")]
        [InlineData(HASH_1, HashFormat.Base64, "AN6tvu8=")]
        [InlineData(HASH_1, HashFormat.Base64Url, "AN6tvu8")]
        [InlineData(HASH_1, HashFormat.Binary, "0000000011011110101011011011111011101111")]
        [InlineData(HASH_1, HashFormat.Bytes, "00-DE-AD-BE-EF")]
        [InlineData(HASH_1, HashFormat.BytesLower, "00-de-ad-be-ef")]
        [InlineData(HASH_1, HashFormat.Hexadecimal, "00DEADBEEF")]
        [InlineData(HASH_1, HashFormat.HexadecimalLower, "00deadbeef")]
        [InlineData(HASH_1, HashFormat.HexadecimalPrefixed, "0x00DEADBEEF")]
        [InlineData(HASH_1, HashFormat.HexadecimalLowerPrefixed, "0x00deadbeef")]
        [InlineData(HASH_2, HashFormat.Base64, "8dAFBQVMwQUABQE=")]
        [InlineData(HASH_2, HashFormat.Base64Url, "8dAFBQVMwQUABQE")]
        [InlineData(HASH_2, HashFormat.Binary, "1111000111010000000001010000010100000101010011001100000100000101000000000000010100000001")]
        [InlineData(HASH_2, HashFormat.Bytes, "F1-D0-05-05-05-4C-C1-05-00-05-01")]
        [InlineData(HASH_2, HashFormat.BytesLower, "f1-d0-05-05-05-4c-c1-05-00-05-01")]
        [InlineData(HASH_2, HashFormat.Hexadecimal, "F1D00505054CC105000501")]
        [InlineData(HASH_2, HashFormat.HexadecimalLower, "f1d00505054cc105000501")]
        [InlineData(HASH_2, HashFormat.HexadecimalPrefixed, "0xF1D00505054CC105000501")]
        [InlineData(HASH_2, HashFormat.HexadecimalLowerPrefixed, "0xf1d00505054cc105000501")]
        public void FormatValidTest(String hashHex, HashFormat format, String expected)
        {
            Byte[] hash = Convert.FromHexString(hashHex);

            Assert.Equal(expected, HashFormatter.Format(hash, format));
            Assert.Equal(expected, HashFormatter.Format(new ReadOnlySpan<Byte>(hash), format));
        }

        [Theory]
        [InlineData(64)]
        [InlineData(65)]
        public void FormatLongTest(Int32 length)
        {
            Byte[] hash = new Byte[length];
            Array.Fill(hash, (Byte)0xFF);

            String expectedBinary = new('1', length * 8);
            Assert.Equal(expectedBinary, HashFormatter.Format(hash, HashFormat.Binary));
            Assert.Equal(expectedBinary, HashFormatter.Format(hash.AsSpan(), HashFormat.Binary));

            String expectedHexadecimal = new('F', length * 2);
            Assert.Equal(expectedHexadecimal, HashFormatter.Format(hash, HashFormat.Hexadecimal));
            Assert.Equal(expectedHexadecimal, HashFormatter.Format(hash.AsSpan(), HashFormat.Hexadecimal));
        }
        #endregion
    }
}
