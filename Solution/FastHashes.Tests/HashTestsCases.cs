#region Using Directives
using System;
using System.Collections.Generic;
#endregion

namespace FastHashes.Tests
{
    public static class HashTestsCases
    {
        #region Constants
        private const Int32 MAXIMUM_LENGTH = 1024;
        #endregion

        #region Test Cases

        private static readonly List<(String HashName, Func<UInt64,Hash> HashInitializer, String Expected)> s_TestCases =
        [
            ("FarmHash32", seed => new FarmHash32(checked((UInt32)seed)), "0x39AFC90D"),
            ("FarmHash64", seed => new FarmHash64(seed), "0x79A6C4EB62E22A5C"),
            ("FarmHash128", seed => new FarmHash128(seed), "0x71BF3EA9A4D9F2351533AF0F4A96CEFB"),
            ("FastHash32", seed => new FastHash32(seed), "0xFC1A48E9"),
            ("FastHash64", seed => new FastHash64(seed), "0xA73162A1DA6BDCBD"),
            ("FastPositiveHash-V0", seed => new FastPositiveHash(FastPositiveHashVariant.V0, seed), "0x297B7D7F81EC046C"),
            ("FastPositiveHash-V1", seed => new FastPositiveHash(FastPositiveHashVariant.V1, seed), "0x816383D664638705"),
            ("FastPositiveHash-V2", seed => new FastPositiveHash(FastPositiveHashVariant.V2, seed), "0x48C9168F5572BC11"),
            ("HalfSipHash", seed => new HalfSipHash(seed), "0x4C192ADA"),
            ("HighwayHash64", seed => new HighwayHash64(seed), "0x2D9D80CDBD9F39E2"),
            ("HighwayHash128", seed => new HighwayHash128(seed), "0x4C21867CD2DD8E1882AC86A53705C53C"),
            ("HighwayHash256", seed => new HighwayHash256(seed), "0x52608FF47620F0694CC650484BF6C530A05CDEC658AF6CE27667F8F75729FED8"),
            ("KomiHash", seed => new KomiHash(seed), "0x6DFF5781E27E4E69"),
            ("MetroHash64-V1", seed => new MetroHash64(MetroHashVariant.V1, checked((UInt32)seed)), "0xD2F788EE75E62D6B"),
            ("MetroHash64-V2", seed => new MetroHash64(MetroHashVariant.V2, checked((UInt32)seed)), "0x6E7CFCE14A183741"),
            ("MetroHash128-V1", seed => new MetroHash128(MetroHashVariant.V1, checked((UInt32)seed)), "0xD7A1E820E745C1A85B293EF5C2C876BC"),
            ("MetroHash128-V2", seed => new MetroHash128(MetroHashVariant.V2, checked((UInt32)seed)), "0x84C63754893C3AC63BAE5D82559AB27C"),
            ("MirHash", seed => new MirHash(seed), "0xFC662A422C002E99"),
            ("MurmurHash32", seed => new MurmurHash32(checked((UInt32)seed)), "0xE37EF5B0"),
            ("MurmurHash64-X86", seed => new MurmurHash64(MurmurHashEngine.x86, checked((UInt32)seed)), "0xEC34E83397E0F193"),
            ("MurmurHash64-X64", seed => new MurmurHash64(MurmurHashEngine.x64, checked((UInt32)seed)), "0x6D8C3D44B40A4391"),
            ("MurmurHash128-X86", seed => new MurmurHash128(MurmurHashEngine.x86, checked((UInt32)seed)), "0x2AE6ECB33A231AD6384F94C332CAC079"),
            ("MurmurHash128-X64", seed => new MurmurHash128(MurmurHashEngine.x64, checked((UInt32)seed)), "0x69BA846303DEF3632DED84E68C879271"),
            ("MumHash", seed => new MumHash(checked((UInt32)seed)), "0xD4E2EA3EFC4952F1"),
            ("Mx3Hash", seed => new Mx3Hash(seed), "0x657B287B75CBFD8B"),
            ("PengyHash", seed => new PengyHash(checked((UInt32)seed)), "0x7B21C21F62359105"),
            ("SipHash-13", seed => new SipHash(SipHashVariant.V13, seed), "0xB2F594F65E5F1529"),
            ("SipHash-24", seed => new SipHash(SipHashVariant.V24, seed), "0x92DC31BC9483210F"),
            ("SpookyHash32", seed => new SpookyHash32(seed, seed), "0x65E28BA4"),
            ("SpookyHash64", seed => new SpookyHash64(seed), "0xDC4B2C973FE48012"),
            ("SpookyHash128", seed => new SpookyHash128(seed), "0xBEFC3C892A16F01B23B81DE576CA399A"),
            ("WyHash32", seed => new WyHash32(checked((UInt32)seed)), "0x6680DE09"),
            ("WyHash64", seed => new WyHash64(seed), "0xD37DAE9D1F46D8DB"),
            ("xxHash32", seed => new XxHash32(checked((UInt32)seed)), "0x43B788BA"),
            ("xxHash64", seed => new XxHash64(seed), "0xF47C4B0200F12795")
        ];

        public static IEnumerable<Object[]> DataFarmHashUnseeded()
        {
            Int32[] lengths = [0, 1, 4, 5, 12, 13, 15, 16, 17, 24, 25, 32, 127, 128, 144];

            foreach (Int32 length in lengths)
                yield return new Object[] { length };
        }

        public static IEnumerable<Object[]> DataGeneric()
        {
            foreach (var (HashName, HashInitializer, _) in s_TestCases)
                yield return new Object[] { HashName, HashInitializer };
        }

        public static IEnumerable<Object[]> DataLength()
        {
            foreach (var (HashName, HashInitializer, _) in s_TestCases)
                yield return new Object[] { HashName, HashInitializer, MAXIMUM_LENGTH };
        }

        public static IEnumerable<Object[]> DataLengthExtended()
        {
            Int32[] lengths = [1025, 4095, 4096, 4097, 16383, 16384, 16385, 65535, 65536, 65537];

            foreach (var (HashName, HashInitializer, _) in s_TestCases)
                foreach (Int32 length in lengths)
                    yield return new Object[] { HashName, HashInitializer, length };
        }

        public static IEnumerable<Object[]> DataSliceInvalid()
        {
            yield return new Object[] { -1, 0, typeof(ArgumentOutOfRangeException) };
            yield return new Object[] { -1, 1, typeof(ArgumentOutOfRangeException) };
            yield return new Object[] { 11, 0, typeof(ArgumentOutOfRangeException) };
            yield return new Object[] { Int32.MaxValue, 0, typeof(ArgumentOutOfRangeException) };
            yield return new Object[] { 0, -1, typeof(ArgumentOutOfRangeException) };
            yield return new Object[] { 0, Int32.MaxValue, typeof(ArgumentException) };
            yield return new Object[] { 1, Int32.MaxValue, typeof(ArgumentException) };
            yield return new Object[] { 8, 3, typeof(ArgumentException) };
            yield return new Object[] { 10, 1, typeof(ArgumentException) };
        }

        public static IEnumerable<Object[]> DataSliceValid()
        {
            yield return new Object[] { 0, 0 };
            yield return new Object[] { 0, 1 };
            yield return new Object[] { 0, 10 };
            yield return new Object[] { 1, 0 };
            yield return new Object[] { 1, 1 };
            yield return new Object[] { 5, 0 };
            yield return new Object[] { 5, 5 };
            yield return new Object[] { 9, 0 };
            yield return new Object[] { 9, 1 };
            yield return new Object[] { 10, 0 };
        }

        public static IEnumerable<Object[]> DataValidation()
        {
            foreach (var (HashName, HashInitializer, Expected) in s_TestCases)
                yield return new Object[] { HashName, HashInitializer, Expected };
        }
        #endregion
    }
}