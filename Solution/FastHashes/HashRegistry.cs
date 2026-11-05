#region Using Directives
using System;
using System.Collections.Generic;
#endregion

namespace FastHashes
{
    /// <summary>Provides the registered hash algorithms and their constructors.</summary>
    internal static class HashRegistry
    {
        #region Constants
        private static readonly Dictionary<String, (Func<Hash> Default, Func<UInt64, Hash> Seeded)> s_Algorithms = new(StringComparer.Ordinal)
        {
            ["FarmHash32"] =
            (
                () => new FarmHash32(),
                seed => new FarmHash32(checked((UInt32)seed))
            ),
            ["FarmHash64"] =
            (
                () => new FarmHash64(),
                seed => new FarmHash64(seed)
            ),
            ["FarmHash128"] =
            (
                () => new FarmHash128(),
                seed => new FarmHash128(seed)
            ),
            ["FastHash32"] =
            (
                () => new FastHash32(),
                seed => new FastHash32(seed)
            ),
            ["FastHash64"] =
            (
                () => new FastHash64(),
                seed => new FastHash64(seed)
            ),
            ["FastPositiveHash-V0"] =
            (
                () => new FastPositiveHash(FastPositiveHashVariant.V0),
                seed => new FastPositiveHash(FastPositiveHashVariant.V0, seed)
            ),
            ["FastPositiveHash-V1"] =
            (
                () => new FastPositiveHash(FastPositiveHashVariant.V1),
                seed => new FastPositiveHash(FastPositiveHashVariant.V1, seed)
            ),
            ["FastPositiveHash-V2"] =
            (
                () => new FastPositiveHash(FastPositiveHashVariant.V2),
                seed => new FastPositiveHash(FastPositiveHashVariant.V2, seed)
            ),
            ["HalfSipHash"] =
            (
                () => new HalfSipHash(),
                seed => new HalfSipHash(checked((UInt32)seed))
            ),
            ["HalftimeHash-S64"] =
            (
                () => new HalftimeHash(HalftimeHashStyle.S64),
                seed => new HalftimeHash(HalftimeHashStyle.S64, seed)
            ),
            ["HalftimeHash-S128"] =
            (
                () => new HalftimeHash(HalftimeHashStyle.S128),
                seed => new HalftimeHash(HalftimeHashStyle.S128, seed)
            ),
            ["HalftimeHash-S256"] =
            (
                () => new HalftimeHash(HalftimeHashStyle.S256),
                seed => new HalftimeHash(HalftimeHashStyle.S256, seed)
            ),
            ["HalftimeHash-S512"] =
            (
                () => new HalftimeHash(HalftimeHashStyle.S512),
                seed => new HalftimeHash(HalftimeHashStyle.S512, seed)
            ),
            ["HighwayHash64"] =
            (
                () => new HighwayHash64(),
                seed => new HighwayHash64(seed)
            ),
            ["HighwayHash128"] =
            (
                () => new HighwayHash128(),
                seed => new HighwayHash128(seed)
            ),
            ["HighwayHash256"] =
            (
                () => new HighwayHash256(),
                seed => new HighwayHash256(seed)
            ),
            ["KomiHash"] =
            (
                () => new KomiHash(),
                seed => new KomiHash(seed)
            ),
            ["LaneHash64"] =
            (
                () => new LaneHash64(),
                seed => new LaneHash64(seed)
            ),
            ["LaneHash128"] =
            (
                () => new LaneHash128(),
                seed => new LaneHash128(seed)
            ),
            ["MetroHash64-V1"] =
            (
                () => new MetroHash64(MetroHashVariant.V1),
                seed => new MetroHash64(MetroHashVariant.V1, checked((UInt32)seed))
            ),
            ["MetroHash64-V2"] =
            (
                () => new MetroHash64(MetroHashVariant.V2),
                seed => new MetroHash64(MetroHashVariant.V2, checked((UInt32)seed))
            ),
            ["MetroHash128-V1"] =
            (
                () => new MetroHash128(MetroHashVariant.V1),
                seed => new MetroHash128(MetroHashVariant.V1, checked((UInt32)seed))
            ),
            ["MetroHash128-V2"] =
            (
                () => new MetroHash128(MetroHashVariant.V2),
                seed => new MetroHash128(MetroHashVariant.V2, checked((UInt32)seed))
            ),
            ["MirHash"] =
            (
                () => new MirHash(),
                seed => new MirHash(seed)
            ),
            ["MurmurHash32"] =
            (
                () => new MurmurHash32(),
                seed => new MurmurHash32(checked((UInt32)seed))
            ),
            ["MurmurHash64-X86"] =
            (
                () => new MurmurHash64(MurmurHashEngine.x86),
                seed => new MurmurHash64(MurmurHashEngine.x86, checked((UInt32)seed))
            ),
            ["MurmurHash64-X64"] =
            (
                () => new MurmurHash64(MurmurHashEngine.x64),
                seed => new MurmurHash64(MurmurHashEngine.x64, checked((UInt32)seed))
            ),
            ["MurmurHash128-X86"] =
            (
                () => new MurmurHash128(MurmurHashEngine.x86),
                seed => new MurmurHash128(MurmurHashEngine.x86, checked((UInt32)seed))
            ),
            ["MurmurHash128-X64"] =
            (
                () => new MurmurHash128(MurmurHashEngine.x64),
                seed => new MurmurHash128(MurmurHashEngine.x64, checked((UInt32)seed))
            ),
            ["MumHash"] =
            (
                () => new MumHash(),
                seed => new MumHash(checked((UInt32)seed))
            ),
            ["Mx3Hash"] =
            (
                () => new Mx3Hash(),
                seed => new Mx3Hash(seed)
            ),
            ["NmHash-S"] =
            (
                () => new NmHash(NmHashVariant.S),
                seed => new NmHash(NmHashVariant.S, checked((UInt32)seed))
            ),
            ["NmHash-X"] =
            (
                () => new NmHash(NmHashVariant.X),
                seed => new NmHash(NmHashVariant.X, checked((UInt32)seed))
            ),
            ["PengyHash"] =
            (
                () => new PengyHash(),
                seed => new PengyHash(checked((UInt32)seed))
            ),
            ["PolymurHash"] =
            (
                () => new PolymurHash(),
                seed => new PolymurHash(seed)
            ),
            ["RapidHash-V1"] =
            (
                () => new RapidHash(RapidHashVariant.V1),
                seed => new RapidHash(RapidHashVariant.V1, seed)
            ),
            ["RapidHash-V2"] =
            (
                () => new RapidHash(RapidHashVariant.V2),
                seed => new RapidHash(RapidHashVariant.V2, seed)
            ),
            ["RapidHash-V3"] =
            (
                () => new RapidHash(RapidHashVariant.V3),
                seed => new RapidHash(RapidHashVariant.V3, seed)
            ),
            ["SipHash-13"] =
            (
                () => new SipHash(SipHashVariant.V13),
                seed => new SipHash(SipHashVariant.V13, seed)
            ),
            ["SipHash-24"] =
            (
                () => new SipHash(SipHashVariant.V24),
                seed => new SipHash(SipHashVariant.V24, seed)
            ),
            ["SpookyHash32"] =
            (
                () => new SpookyHash32(),
                seed => new SpookyHash32(seed, seed)
            ),
            ["SpookyHash64"] =
            (
                () => new SpookyHash64(),
                seed => new SpookyHash64(seed)
            ),
            ["SpookyHash128"] =
            (
                () => new SpookyHash128(),
                seed => new SpookyHash128(seed)
            ),
            ["Umash64"] =
            (
                () => new Umash64(),
                seed => new Umash64(seed)
            ),
            ["Umash128"] =
            (
                () => new Umash128(),
                seed => new Umash128(seed)
            ),
            ["WyHash32"] =
            (
                () => new WyHash32(),
                seed => new WyHash32(checked((UInt32)seed))
            ),
            ["WyHash64"] =
            (
                () => new WyHash64(),
                seed => new WyHash64(seed)
            ),
            ["XxHash32"] =
            (
                () => new XxHash32(),
                seed => new XxHash32(checked((UInt32)seed))
            ),
            ["XxHash64"] =
            (
                () => new XxHash64(),
                seed => new XxHash64(seed)
            ),
            ["XxHashThree64"] =
            (
                () => new XxHashThree64(),
                seed => new XxHashThree64(seed)
            ),
            ["XxHashThree128"] =
            (
                () => new XxHashThree128(),
                seed => new XxHashThree128(seed)
            )
        };
        #endregion

        #region Methods
        internal static Boolean TryGetInitializers(String name, out (Func<Hash> Default, Func<UInt64, Hash> Seeded) initializers)
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name));

            return s_Algorithms.TryGetValue(name, out initializers);
        }

        internal static IReadOnlyList<String> GetIdentifiers()
        {
            List<String> identifiers = new(s_Algorithms.Keys);
            identifiers.Sort(StringComparer.Ordinal);

            return identifiers.AsReadOnly();
        }
        #endregion
    }
}