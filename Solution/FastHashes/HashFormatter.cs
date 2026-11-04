#region Using Directives
using System;
#endregion

namespace FastHashes
{
    /// <summary>Provides methods for converting byte representations of hashes into formatted strings.</summary>
    public static class HashFormatter
    {
        #region Constants
        private const String LOWER_DIGITS = "0123456789abcdef";
        private const String UPPER_DIGITS = "0123456789ABCDEF";
        #endregion

        #region Methods

        private static String ConvertToBase64(ReadOnlySpan<Byte> hash, Boolean url)
        {
#if NETSTANDARD2_0
            String text = Convert.ToBase64String(hash.ToArray());
#else
            String text = Convert.ToBase64String(hash);
#endif

            if (url)
                text = text.TrimEnd('=').Replace('+', '-').Replace('/', '_');

            return text;
        }

        private static String ConvertToBinary(ReadOnlySpan<Byte> hash)
        {
            Int32 hashLength = hash.Length;

            if (hashLength == 0)
                return String.Empty;

            Int32 charsCount = checked(hashLength * 8);

#if NETSTANDARD2_0
            Char[] chars = new Char[charsCount];
#else
            Span<Char> chars = (charsCount <= 512) ? stackalloc Char[charsCount] : new Char[charsCount];
#endif

            for (Int32 i = 0; i < hashLength; ++i)
            {
                Byte value = hash[i];
                Int32 offset = i * 8;

                for (Int32 j = 0; j < 8; ++j)
                    chars[offset + j] = ((value & (1 << (7 - j))) != 0) ? '1' : '0';
            }

            String text = new(chars);

            return text;
        }

        private static String ConvertToBytes(ReadOnlySpan<Byte> hash, Boolean lowerCase)
        {
            Int32 hashLength = hash.Length;

            if (hashLength == 0)
                return String.Empty;

            String digits = lowerCase ? LOWER_DIGITS : UPPER_DIGITS;
            Int32 charsCount = checked((hashLength * 3) - 1);

#if NETSTANDARD2_0
            Char[] chars = new Char[charsCount];
#else
            Span<Char> chars = (charsCount <= 512) ? stackalloc Char[charsCount] : new Char[charsCount];
#endif

            for (Int32 i = 0; i < hashLength; ++i)
            {
                Byte value = hash[i];
                Int32 offset = i * 3;

                chars[offset] = digits[value >> 4];
                chars[offset + 1] = digits[value & 0x0F];

                if (i < hashLength - 1)
                    chars[offset + 2] = '-';
            }

            String text = new(chars);

            return text;
        }

        private static String ConvertToHexadecimal(ReadOnlySpan<Byte> hash, Boolean lowerCase, Boolean prefixed)
        {
            Int32 hashLength = hash.Length;

            if (hashLength == 0)
                return String.Empty;

#if NET9_0_OR_GREATER
            String text = lowerCase ? Convert.ToHexStringLower(hash) : Convert.ToHexString(hash);
#else
            String digits = lowerCase ? LOWER_DIGITS : UPPER_DIGITS;
            Int32 charsCount = checked(hashLength * 2);

    #if NETSTANDARD2_0
            Char[] chars = new Char[charsCount];
    #else
            Span<Char> chars = (charsCount <= 512) ? stackalloc Char[charsCount] : new Char[charsCount];
    #endif

            for (Int32 i = 0; i < hashLength; ++i)
            {
                Byte value = hash[i];
                chars[i * 2] = digits[value >> 4];
                chars[(i * 2) + 1] = digits[value & 0x0F];
            }

            String text = new(chars);
#endif

            if (prefixed)
                text = "0x" + text;

            return text;
        }

        /// <summary>Formats the <see cref="T:System.Byte"/>[] representation of a hash as text.</summary>
        /// <param name="hash">The <see cref="T:System.Byte"/>[] containing the hash to be formatted.</param>
        /// <param name="format">The <see cref="T:FastHashes.HashFormat"/> value specifying the format of the resulting hash.</param>
        /// <returns>A <see cref="T:System.String"/> representing the formatted hash.</returns>
        /// <exception cref="T:System.ArgumentNullException">Thrown when <paramref name="hash">hash</paramref> is <c>null</c>.</exception>
        /// <exception cref="T:System.ArgumentOutOfRangeException">Thrown when <paramref name="format">format</paramref> is not a defined enumeration value.</exception>
        /// <exception cref="T:System.OverflowException">Thrown when the required output length exceeds the supported integer range.</exception>
        public static String Format(Byte[] hash, HashFormat format)
        {
            if (hash == null)
                throw new ArgumentNullException(nameof(hash));

            return format switch
            {
#if NETSTANDARD2_0
                HashFormat.Base64 => Convert.ToBase64String(hash),
                HashFormat.Base64Url => Convert.ToBase64String(hash).TrimEnd('=').Replace('+', '-').Replace('/', '_'),
#endif
                HashFormat.Bytes => BitConverter.ToString(hash),
                HashFormat.BytesLower => BitConverter.ToString(hash).ToLowerInvariant(),
                _ => Format(hash.AsSpan(), format),
            };
        }

        /// <summary>Formats the <see cref="T:System.ReadOnlySpan`1{T}">ReadOnlySpan&lt;byte&gt;</see> representation of a hash as text.</summary>
        /// <param name="hash">The <see cref="T:System.ReadOnlySpan`1{T}">ReadOnlySpan&lt;byte&gt;</see> containing the hash to be formatted.</param>
        /// <param name="format">The <see cref="T:FastHashes.HashFormat"/> value specifying the format of the resulting hash.</param>
        /// <returns>A <see cref="T:System.String"/> representing the formatted hash.</returns>
        /// <exception cref="T:System.ArgumentOutOfRangeException">Thrown when <paramref name="format">format</paramref> is not a defined enumeration value.</exception>
        /// <exception cref="T:System.OverflowException">Thrown when the required output length exceeds the supported integer range.</exception>
        public static String Format(ReadOnlySpan<Byte> hash, HashFormat format)
        {
            return format switch
            {
                HashFormat.Base64 => ConvertToBase64(hash, false),
                HashFormat.Base64Url => ConvertToBase64(hash, true),
                HashFormat.Binary => ConvertToBinary(hash),
                HashFormat.Bytes => ConvertToBytes(hash, false),
                HashFormat.BytesLower => ConvertToBytes(hash, true),
                HashFormat.Hexadecimal => ConvertToHexadecimal(hash, false, false),
                HashFormat.HexadecimalLower => ConvertToHexadecimal(hash, true, false),
                HashFormat.HexadecimalPrefixed => ConvertToHexadecimal(hash, false, true),
                HashFormat.HexadecimalLowerPrefixed => ConvertToHexadecimal(hash, true, true),
                _ => throw new ArgumentOutOfRangeException(nameof(format), "Invalid format specified."),
            };
        }
        #endregion
    }
}