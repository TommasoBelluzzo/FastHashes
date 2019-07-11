namespace FastHashes
{
    /// <summary>Specifies the variant of FastPositiveHash.</summary>
    public enum FastPositiveHashVariant
    {
        #region Values
        /// <summary>The variant 0 of FastPositiveHash.</summary>
        V0,
        /// <summary>The variant 1 of FastPositiveHash.</summary>
        V1,
        /// <summary>The variant 2 of FastPositiveHash.</summary>
        V2
        #endregion
    }

    /// <summary>Specifies the MurmurHash engine.</summary>
    public enum MurmurHashEngine
    {
        #region Values
        /// <summary>The engine selection is automatically performed and based on the process bitness. See <see cref="P:System.Environment.Is64BitProcess"/>.</summary>
        Auto,
        /// <summary>The engine x86 of MurmurHash.</summary>
        x86,
        /// <summary>The engine x64 of MurmurHash.</summary>
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

    /// <summary>Specifies the variant of SipHash.</summary>
    public enum SipHashVariant
    {
        #region Values
        /// <summary>The variant 1-3 of SipHash.</summary>
        V13,
        /// <summary>The variant 2-4 of SipHash.</summary>
        V24
        #endregion
    }
}