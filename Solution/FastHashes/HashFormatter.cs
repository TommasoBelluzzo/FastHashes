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
        private static String ConvertToBase64(Byte[] hash, Boolean url)
        {
            String text = Convert.ToBase64String(hash);

            if (url)
                text = text.TrimEnd('=').Replace('+', '-').Replace('/', '_');

            return text;
        }

        private static String ConvertToBinary(Byte[] hash)
        {
            Int32 hashLength = hash.Length;
            Char[] chars = new Char[hashLength * 8];

            for (Int32 i = 0; i < hashLength; ++i)
            {
                Byte value = hash[i];
                Int32 offset = i * 8;

                for (Int32 j = 0; j < 8; ++j)
                    chars[offset + j] = ((value & (1 << (7 - j))) != 0) ? '1' : '0';
            }

            String text = new String(chars);

            return text;
        }

        private static String ConvertToHexadecimal(Byte[] hash, Boolean lowerCase, Boolean prefixed)
        {
            if (hash.Length == 0)
                return String.Empty;

#if NET9_0_OR_GREATER
            String text = lowerCase ? Convert.ToHexStringLower(hash) : Convert.ToHexString(hash);
#else
            String digits = lowerCase ? LOWER_DIGITS : UPPER_DIGITS;
            Char[] chars = new Char[hash.Length * 2];

            for (Int32 i = 0; i < hash.Length; ++i)
            {
                Byte value = hash[i];
                chars[i * 2] = digits[value >> 4];
                chars[(i * 2) + 1] = digits[value & 0x0F];
            }

            String text = new String(chars);
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
        public static String Format(Byte[] hash, HashFormat format)
        {
            if (hash == null)
                throw new ArgumentNullException(nameof(hash));

            switch (format)
            {
                case HashFormat.Base64:
                    return ConvertToBase64(hash, false);

                case HashFormat.Base64Url:
                    return ConvertToBase64(hash, true);

                case HashFormat.Binary:
                    return ConvertToBinary(hash);

                case HashFormat.Bytes:
                    return BitConverter.ToString(hash);

                case HashFormat.BytesLower:
                    return BitConverter.ToString(hash).ToLowerInvariant();

                case HashFormat.Hexadecimal:
                    return ConvertToHexadecimal(hash, false, false);

                case HashFormat.HexadecimalLower:
                    return ConvertToHexadecimal(hash, true, false);

                case HashFormat.HexadecimalPrefixed:
                    return ConvertToHexadecimal(hash, false, true);

                case HashFormat.HexadecimalLowerPrefixed:
                    return ConvertToHexadecimal(hash, true, true);

                default:
                    throw new ArgumentOutOfRangeException(nameof(format), "Invalid format specified.");
            }
        }

        /// <summary>Formats the <see cref="T:System.ReadOnlySpan`1{T}">ReadOnlySpan&lt;byte&gt;</see> representation of a hash as text.</summary>
        /// <param name="hash">The <see cref="T:System.ReadOnlySpan`1{T}">ReadOnlySpan&lt;byte&gt;</see> containing the hash to be formatted.</param>
        /// <param name="format">The <see cref="T:FastHashes.HashFormat"/> value specifying the format of the resulting hash.</param>
        /// <returns>A <see cref="T:System.String"/> representing the formatted hash.</returns>
        /// <exception cref="T:System.ArgumentOutOfRangeException">Thrown when <paramref name="format">format</paramref> is not a defined enumeration value.</exception>
        public static String Format(ReadOnlySpan<Byte> hash, HashFormat format)
        {
            return Format(hash.ToArray(), format);
        }
        #endregion
    }
}