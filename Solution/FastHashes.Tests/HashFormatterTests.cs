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
        #endregion
    }
}
