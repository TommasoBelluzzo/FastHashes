#region Using Directives
using System;
using Xunit;
#endregion

namespace FastHashes.Tests
{
    public sealed class HashFormatterTests
    {
        #region Methods
        [Theory]
        [MemberData(nameof(HashTestsCases.DataFormatInvalid), MemberType = typeof(HashTestsCases))]
        public void FormatInvalidTest(Byte[] hash, HashFormat format, Type exceptionType, String parameterName)
        {
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
        [MemberData(nameof(HashTestsCases.DataFormatValid), MemberType = typeof(HashTestsCases))]
        public void FormatValidTest(Byte[] hash, HashFormat format, String expected)
        {
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
