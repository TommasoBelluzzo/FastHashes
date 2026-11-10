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
        private static readonly Dictionary<String, (Func<Hash> Default, (Func<UInt64, Hash> Ctor, Int32 Bits) Seeded)> s_Algorithms = new(StringComparer.Ordinal)
        {
            ["A5Hash32"] =
            (
                () => new A5Hash32(),
                (seed => new A5Hash32(checked((UInt32)seed)), 32)
            ),
            ["A5Hash64"] =
            (
                () => new A5Hash64(),
                (seed => new A5Hash64(seed), 64)
            ),
            ["A5Hash128"] =
            (
                () => new A5Hash128(),
                (seed => new A5Hash128(seed), 64)
            ),
            ["FarmHash32"] =
            (
                () => new FarmHash32(),
                (seed => new FarmHash32(checked((UInt32)seed)), 32)
            ),
            ["FarmHash64"] =
            (
                () => new FarmHash64(),
                (seed => new FarmHash64(seed), 64)
            ),
            ["FarmHash128"] =
            (
                () => new FarmHash128(),
                (seed => new FarmHash128(seed), 64)
            ),
            ["FastHash32"] =
            (
                () => new FastHash32(),
                (seed => new FastHash32(seed), 64)
            ),
            ["FastHash64"] =
            (
                () => new FastHash64(),
                (seed => new FastHash64(seed), 64)
            ),
            ["FastPositiveHash-V0"] =
            (
                () => new FastPositiveHash(FastPositiveHashVariant.V0),
                (seed => new FastPositiveHash(FastPositiveHashVariant.V0, seed), 64)
            ),
            ["FastPositiveHash-V1"] =
            (
                () => new FastPositiveHash(FastPositiveHashVariant.V1),
                (seed => new FastPositiveHash(FastPositiveHashVariant.V1, seed), 64)
            ),
            ["FastPositiveHash-V2"] =
            (
                () => new FastPositiveHash(FastPositiveHashVariant.V2),
                (seed => new FastPositiveHash(FastPositiveHashVariant.V2, seed), 64)
            ),
            ["HalfSipHash"] =
            (
                () => new HalfSipHash(),
                (seed => new HalfSipHash(checked((UInt32)seed)), 32)
            ),
            ["HalftimeHash-S64"] =
            (
                () => new HalftimeHash(HalftimeHashStyle.S64),
                (seed => new HalftimeHash(HalftimeHashStyle.S64, seed), 64)
            ),
            ["HalftimeHash-S128"] =
            (
                () => new HalftimeHash(HalftimeHashStyle.S128),
                (seed => new HalftimeHash(HalftimeHashStyle.S128, seed), 64)
            ),
            ["HalftimeHash-S256"] =
            (
                () => new HalftimeHash(HalftimeHashStyle.S256),
                (seed => new HalftimeHash(HalftimeHashStyle.S256, seed), 64)
            ),
            ["HalftimeHash-S512"] =
            (
                () => new HalftimeHash(HalftimeHashStyle.S512),
                (seed => new HalftimeHash(HalftimeHashStyle.S512, seed), 64)
            ),
            ["HighwayHash64"] =
            (
                () => new HighwayHash64(),
                (seed => new HighwayHash64(seed), 64)
            ),
            ["HighwayHash128"] =
            (
                () => new HighwayHash128(),
                (seed => new HighwayHash128(seed), 64)
            ),
            ["HighwayHash256"] =
            (
                () => new HighwayHash256(),
                (seed => new HighwayHash256(seed), 64)
            ),
            ["KomiHash"] =
            (
                () => new KomiHash(),
                (seed => new KomiHash(seed), 64)
            ),
            ["LaneHash64"] =
            (
                () => new LaneHash64(),
                (seed => new LaneHash64(seed), 64)
            ),
            ["LaneHash128"] =
            (
                () => new LaneHash128(),
                (seed => new LaneHash128(seed), 64)
            ),
            ["MetroHash64-V1"] =
            (
                () => new MetroHash64(MetroHashVariant.V1),
                (seed => new MetroHash64(MetroHashVariant.V1, checked((UInt32)seed)), 32)
            ),
            ["MetroHash64-V2"] =
            (
                () => new MetroHash64(MetroHashVariant.V2),
                (seed => new MetroHash64(MetroHashVariant.V2, checked((UInt32)seed)), 32)
            ),
            ["MetroHash128-V1"] =
            (
                () => new MetroHash128(MetroHashVariant.V1),
                (seed => new MetroHash128(MetroHashVariant.V1, checked((UInt32)seed)), 32)
            ),
            ["MetroHash128-V2"] =
            (
                () => new MetroHash128(MetroHashVariant.V2),
                (seed => new MetroHash128(MetroHashVariant.V2, checked((UInt32)seed)), 32)
            ),
            ["MirHash"] =
            (
                () => new MirHash(),
                (seed => new MirHash(seed), 64)
            ),
            ["MurmurHash32"] =
            (
                () => new MurmurHash32(),
                (seed => new MurmurHash32(checked((UInt32)seed)), 32)
            ),
            ["MurmurHash64-X86"] =
            (
                () => new MurmurHash64(MurmurHashEngine.x86),
                (seed => new MurmurHash64(MurmurHashEngine.x86, checked((UInt32)seed)), 32)
            ),
            ["MurmurHash64-X64"] =
            (
                () => new MurmurHash64(MurmurHashEngine.x64),
                (seed => new MurmurHash64(MurmurHashEngine.x64, checked((UInt32)seed)), 32)
            ),
            ["MurmurHash128-X86"] =
            (
                () => new MurmurHash128(MurmurHashEngine.x86),
                (seed => new MurmurHash128(MurmurHashEngine.x86, checked((UInt32)seed)), 32)
            ),
            ["MurmurHash128-X64"] =
            (
                () => new MurmurHash128(MurmurHashEngine.x64),
                (seed => new MurmurHash128(MurmurHashEngine.x64, checked((UInt32)seed)), 32)
            ),
            ["MumHash"] =
            (
                () => new MumHash(),
                (seed => new MumHash(checked((UInt32)seed)), 32)
            ),
            ["Mx3Hash"] =
            (
                () => new Mx3Hash(),
                (seed => new Mx3Hash(seed), 64)
            ),
            ["NmHash-S"] =
            (
                () => new NmHash(NmHashVariant.S),
                (seed => new NmHash(NmHashVariant.S, checked((UInt32)seed)), 32)
            ),
            ["NmHash-X"] =
            (
                () => new NmHash(NmHashVariant.X),
                (seed => new NmHash(NmHashVariant.X, checked((UInt32)seed)), 32)
            ),
            ["PengyHash"] =
            (
                () => new PengyHash(),
                (seed => new PengyHash(checked((UInt32)seed)), 32)
            ),
            ["PolymurHash"] =
            (
                () => new PolymurHash(),
                (seed => new PolymurHash(seed), 64)
            ),
            ["RapidHash-V1"] =
            (
                () => new RapidHash(RapidHashVariant.V1),
                (seed => new RapidHash(RapidHashVariant.V1, seed), 64)
            ),
            ["RapidHash-V2"] =
            (
                () => new RapidHash(RapidHashVariant.V2),
                (seed => new RapidHash(RapidHashVariant.V2, seed), 64)
            ),
            ["RapidHash-V3"] =
            (
                () => new RapidHash(RapidHashVariant.V3),
                (seed => new RapidHash(RapidHashVariant.V3, seed), 64)
            ),
            ["SipHash-13"] =
            (
                () => new SipHash(SipHashVariant.V13),
                (seed => new SipHash(SipHashVariant.V13, seed), 64)
            ),
            ["SipHash-24"] =
            (
                () => new SipHash(SipHashVariant.V24),
                (seed => new SipHash(SipHashVariant.V24, seed), 64)
            ),
            ["SpookyHash32"] =
            (
                () => new SpookyHash32(),
                (seed => new SpookyHash32(seed, seed), 64)
            ),
            ["SpookyHash64"] =
            (
                () => new SpookyHash64(),
                (seed => new SpookyHash64(seed), 64)
            ),
            ["SpookyHash128"] =
            (
                () => new SpookyHash128(),
                (seed => new SpookyHash128(seed), 64)
            ),
            ["Umash64"] =
            (
                () => new Umash64(),
                (seed => new Umash64(seed), 64)
            ),
            ["Umash128"] =
            (
                () => new Umash128(),
                (seed => new Umash128(seed), 64)
            ),
            ["WyHash32"] =
            (
                () => new WyHash32(),
                (seed => new WyHash32(checked((UInt32)seed)), 32)
            ),
            ["W1Hash"] =
            (
                () => new W1Hash(),
                (seed => new W1Hash(seed), 64)
            ),
            ["WyHash64"] =
            (
                () => new WyHash64(),
                (seed => new WyHash64(seed), 64)
            ),
            ["XxHash32"] =
            (
                () => new XxHash32(),
                (seed => new XxHash32(checked((UInt32)seed)), 32)
            ),
            ["XxHash64"] =
            (
                () => new XxHash64(),
                (seed => new XxHash64(seed), 64)
            ),
            ["XxHashThree64"] =
            (
                () => new XxHashThree64(),
                (seed => new XxHashThree64(seed), 64)
            ),
            ["XxHashThree128"] =
            (
                () => new XxHashThree128(),
                (seed => new XxHashThree128(seed), 64)
            )
        };
        #endregion

        #region Methods
        internal static Boolean TryGetInitializers(String name, out (Func<Hash> Default, (Func<UInt64, Hash> Ctor, Int32 Bits) Seeded) initializers)
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name));

            return s_Algorithms.TryGetValue(name, out initializers);
        }

        internal static IReadOnlyList<String> GetIdentifiers()
        {
            return new List<String>(s_Algorithms.Keys).AsReadOnly();
        }
        #endregion
    }
}