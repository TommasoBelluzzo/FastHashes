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
}