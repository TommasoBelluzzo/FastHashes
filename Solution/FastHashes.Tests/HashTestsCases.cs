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

        private static readonly List<TestCase> s_TestCases = new List<TestCase>
        {
            new("FarmHash32", seed => new FarmHash32(checked((UInt32)seed)), "0x39AFC90D"),
            new("FarmHash64", seed => new FarmHash64(seed), "0x79A6C4EB62E22A5C"),
            new("FarmHash128", seed => new FarmHash128(seed), "0x71BF3EA9A4D9F2351533AF0F4A96CEFB"),
            new("FastHash32", seed => new FastHash32(seed), "0xFC1A48E9"),
            new("FastHash64", seed => new FastHash64(seed), "0xA73162A1DA6BDCBD"),
            new("FastPositiveHash-V0", seed => new FastPositiveHash(FastPositiveHashVariant.V0, seed), "0x297B7D7F81EC046C"),
            new("FastPositiveHash-V1", seed => new FastPositiveHash(FastPositiveHashVariant.V1, seed), "0x816383D664638705"),
            new("FastPositiveHash-V2", seed => new FastPositiveHash(FastPositiveHashVariant.V2, seed), "0x48C9168F5572BC11"),
            new("HalfSipHash", seed => new HalfSipHash(seed), "0x4C192ADA"),
            new("HighwayHash64", seed => new HighwayHash64(seed), "0x2D9D80CDBD9F39E2"),
            new("HighwayHash128", seed => new HighwayHash128(seed), "0x4C21867CD2DD8E1882AC86A53705C53C"),
            new("HighwayHash256", seed => new HighwayHash256(seed), "0x52608FF47620F0694CC650484BF6C530A05CDEC658AF6CE27667F8F75729FED8"),
            new("KomiHash", seed => new KomiHash(seed), "0x6DFF5781E27E4E69"),
            new("MetroHash64-V1", seed => new MetroHash64(MetroHashVariant.V1, checked((UInt32)seed)), "0xD2F788EE75E62D6B"),
            new("MetroHash64-V2", seed => new MetroHash64(MetroHashVariant.V2, checked((UInt32)seed)), "0x6E7CFCE14A183741"),
            new("MetroHash128-V1", seed => new MetroHash128(MetroHashVariant.V1, checked((UInt32)seed)), "0xD7A1E820E745C1A85B293EF5C2C876BC"),
            new("MetroHash128-V2", seed => new MetroHash128(MetroHashVariant.V2, checked((UInt32)seed)), "0x84C63754893C3AC63BAE5D82559AB27C"),
            new("MirHash", seed => new MirHash(seed), "0xFC662A422C002E99"),
            new("MurmurHash32", seed => new MurmurHash32(checked((UInt32)seed)), "0xE37EF5B0"),
            new("MurmurHash64-X86", seed => new MurmurHash64(MurmurHashEngine.x86, checked((UInt32)seed)), "0xEC34E83397E0F193"),
            new("MurmurHash64-X64", seed => new MurmurHash64(MurmurHashEngine.x64, checked((UInt32)seed)), "0x6D8C3D44B40A4391"),
            new("MurmurHash128-X86", seed => new MurmurHash128(MurmurHashEngine.x86, checked((UInt32)seed)), "0x2AE6ECB33A231AD6384F94C332CAC079"),
            new("MurmurHash128-X64", seed => new MurmurHash128(MurmurHashEngine.x64, checked((UInt32)seed)), "0x69BA846303DEF3632DED84E68C879271"),
            new("MumHash", seed => new MumHash(checked((UInt32)seed)), "0xD4E2EA3EFC4952F1"),
            new("Mx3Hash", seed => new Mx3Hash(seed), "0x657B287B75CBFD8B"),
            new("PengyHash", seed => new PengyHash(checked((UInt32)seed)), "0x7B21C21F62359105"),
            new("SipHash-13", seed => new SipHash(SipHashVariant.V13, seed), "0xB2F594F65E5F1529"),
            new("SipHash-24", seed => new SipHash(SipHashVariant.V24, seed), "0x92DC31BC9483210F"),
            new("SpookyHash32", seed => new SpookyHash32(seed, seed), "0x65E28BA4"),
            new("SpookyHash64", seed => new SpookyHash64(seed), "0xDC4B2C973FE48012"),
            new("SpookyHash128", seed => new SpookyHash128(seed), "0xBEFC3C892A16F01B23B81DE576CA399A"),
            new("WyHash32", seed => new WyHash32(checked((UInt32)seed)), "0x6680DE09"),
            new("WyHash64", seed => new WyHash64(seed), "0xD37DAE9D1F46D8DB"),
            new("xxHash32", seed => new XxHash32(checked((UInt32)seed)), "0x43B788BA"),
            new("xxHash64", seed => new XxHash64(seed), "0xF47C4B0200F12795")
        };

        public static IEnumerable<Object[]> DataGeneric()
        {
            foreach (TestCase testCase in s_TestCases)
                yield return new Object[] { testCase.HashName, testCase.HashInitializer };
        }

        public static IEnumerable<Object[]> DataLength()
        {
            foreach (TestCase testCase in s_TestCases)
                yield return new Object[] { testCase.HashName, testCase.HashInitializer, MAXIMUM_LENGTH };
        }

        public static IEnumerable<Object[]> DataLengthExtended()
        {
            Int32[] lengths = { 1025, 4095, 4096, 4097, 16383, 16384, 16385, 65535, 65536, 65537 };

            foreach (TestCase testCase in s_TestCases)
                foreach (Int32 length in lengths)
                    yield return new Object[] { testCase.HashName, testCase.HashInitializer, length };
        }

        public static IEnumerable<Object[]> DataValidation()
        {
            foreach (TestCase testCase in s_TestCases)
                yield return new Object[] { testCase.HashName, testCase.HashInitializer, testCase.Expected };
        }
        #endregion

        #region Nested Classes
        private sealed class TestCase
        {
            #region Members
            private readonly Func<UInt64,Hash> m_HashInitializer;
            private readonly String m_Expected;
            private readonly String m_HashName;
            #endregion

            #region Properties
            public Func<UInt64,Hash> HashInitializer => m_HashInitializer;
            public String Expected => m_Expected;
            public String HashName => m_HashName;
            #endregion

            #region Constructors
            public TestCase(String hashName, Func<UInt64,Hash> hashInitializer, String expected)
            {
                if (String.IsNullOrWhiteSpace(hashName))
                    throw new ArgumentException("Invalid hash name specified.", nameof(hashName));

                if (hashInitializer == null)
                    throw new ArgumentException("Invalid hash initializer specified.", nameof(hashInitializer));

                if (String.IsNullOrWhiteSpace(expected))
                    throw new ArgumentException("Invalid expected value specified.", nameof(expected));

                m_HashInitializer = hashInitializer;
                m_Expected = expected;
                m_HashName = hashName;
            }
            #endregion

            #region Methods
            public override String ToString()
            {
                return $"{GetType().Name}: {m_HashName}";
            }
            #endregion
        }
        #endregion
    }
}