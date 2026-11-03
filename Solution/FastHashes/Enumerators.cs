namespace FastHashes
{
    /// <summary>Specifies the variant of <see cref="T:FastHashes.FastPositiveHash"/>.</summary>
    public enum FastPositiveHashVariant
    {
        #region Values
        /// <summary>The variant 0 of <see cref="T:FastHashes.FastPositiveHash"/>.</summary>
        V0,
        /// <summary>The variant 1 of <see cref="T:FastHashes.FastPositiveHash"/>.</summary>
        V1,
        /// <summary>The variant 2 of <see cref="T:FastHashes.FastPositiveHash"/>.</summary>
        V2
        #endregion
    }

    /// <summary>Specifies the internal processing style of <see cref="T:FastHashes.HalftimeHash"/>.</summary>
    public enum HalftimeHashStyle
    {
        #region Values
        /// <summary>The 64-bit internal processing style of <see cref="T:FastHashes.HalftimeHash"/>.</summary>
        S64,
        /// <summary>The 128-bit internal processing style of <see cref="T:FastHashes.HalftimeHash"/>.</summary>
        S128,
        /// <summary>The 256-bit internal processing style of <see cref="T:FastHashes.HalftimeHash"/>.</summary>
        S256,
        /// <summary>The 512-bit internal processing style of <see cref="T:FastHashes.HalftimeHash"/>.</summary>
        S512
        #endregion
    }

    /// <summary>Specifies the text representation of a computed hash.</summary>
    public enum HashFormat
    {
        #region Values
        /// <summary>The standard Base64 representation, including padding.</summary>
        Base64,
        /// <summary>The URL-safe Base64 representation, without padding.</summary>
        Base64Url,
        /// <summary>The binary representation, using eight bits per byte.</summary>
        Binary,
        /// <summary>The uppercase representation of bytes, separated by hyphens.</summary>
        Bytes,
        /// <summary>The lowercase representation of bytes, separated by hyphens.</summary>
        BytesLower,
        /// <summary>The uppercase hexadecimal representation, without separators or prefixes.</summary>
        Hexadecimal,
        /// <summary>The lowercase hexadecimal representation, without separators or prefixes.</summary>
        HexadecimalLower,
        /// <summary>The uppercase hexadecimal representation, prefixed with <c>0x</c>.</summary>
        HexadecimalPrefixed,
        /// <summary>The lowercase hexadecimal representation, prefixed with <c>0x</c>.</summary>
        HexadecimalLowerPrefixed
        #endregion
    }

    /// <summary>Specifies the engine category of MurmurHash.</summary>
    public enum MurmurHashEngine
    {
        #region Values
        /// <summary>The engine selection is automatically performed and based on the process bitness. See <see cref="P:System.Environment.Is64BitProcess"/>.</summary>
        Auto,
        /// <summary>The x86 engine of MurmurHash.</summary>
        x86,
        /// <summary>The x64 engine of MurmurHash.</summary>
        x64
        #endregion
    }

    /// <summary>Specifies the variant of MetroHash.</summary>
    public enum MetroHashVariant
    {
        #region Values
        /// <summary>The variant 1 of MetroHash.</summary>
        V1,
        /// <summary>The variant 2 of MetroHash.</summary>
        V2
        #endregion
    }

    /// <summary>Specifies the variant of <see cref="T:FastHashes.NmHash"/>.</summary>
    public enum NmHashVariant
    {
        #region Values
        /// <summary>The standard <see cref="T:FastHashes.NmHash"/>.</summary>
        S,
        /// <summary>The X variant of <see cref="T:FastHashes.NmHash"/>.</summary>
        X
        #endregion
    }

    /// <summary>Specifies the variant of <see cref="T:FastHashes.RapidHash"/>.</summary>
    public enum RapidHashVariant
    {
        #region Values
        /// <summary>The variant 1 of <see cref="T:FastHashes.RapidHash"/>.</summary>
        V1,
        /// <summary>The variant 2 of <see cref="T:FastHashes.RapidHash"/>.</summary>
        V2,
        /// <summary>The variant 3 of <see cref="T:FastHashes.RapidHash"/>.</summary>
        V3
        #endregion
    }

    /// <summary>Specifies the variant of <see cref="T:FastHashes.SipHash"/>.</summary>
    public enum SipHashVariant
    {
        #region Values
        /// <summary>The variant 1-3 of <see cref="T:FastHashes.SipHash"/>.</summary>
        V13,
        /// <summary>The variant 2-4 of <see cref="T:FastHashes.SipHash"/>.</summary>
        V24
        #endregion
    }

    /// <summary>Specifies the output of <see cref="T:FastHashes.XxHash3"/>.</summary>
    public enum XxHash3Output
    {
        #region Values
        /// <summary>The 64-bit output of <see cref="T:FastHashes.XxHash3"/>.</summary>
        O64,
        /// <summary>The 128-bit output of <see cref="T:FastHashes.XxHash3"/>.</summary>
        O128
        #endregion
    }
}