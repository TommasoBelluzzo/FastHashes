﻿#region Using Directives
using System;
using System.Collections.Generic;
using System.Linq;
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
            new TestCase("FarmHash32", typeof(FarmHash32), seed => new FarmHash32(seed), "0x39AFC90D"),
            new TestCase("FarmHash64", typeof(FarmHash64), seed => new FarmHash64(seed), "0x79A6C4EB62E22A5C"),
            new TestCase("FarmHash128", typeof(FarmHash128), seed => new FarmHash128(seed), "0x71BF3EA9A4D9F2351533AF0F4A96CEFB"),
            new TestCase("FastHash32", typeof(FastHash32), seed => new FastHash32(seed), "0xFC1A48E9"),
            new TestCase("FastHash64", typeof(FastHash64), seed => new FastHash64(seed), "0xA73162A1DA6BDCBD"),
            new TestCase("FastPositiveHash-V0", typeof(FastPositiveHash), seed => new FastPositiveHash(FastPositiveHashVariant.V0, seed), "0x297B7D7F81EC046C"),
            new TestCase("FastPositiveHash-V1", typeof(FastPositiveHash), seed => new FastPositiveHash(FastPositiveHashVariant.V1, seed), "0x816383D664638705"),
            new TestCase("FastPositiveHash-V2", typeof(FastPositiveHash), seed => new FastPositiveHash(FastPositiveHashVariant.V2, seed), "0x48C9168F5572BC11"),
            new TestCase("HalfSipHash", typeof(HalfSipHash), seed => new HalfSipHash(seed), "0x4C192ADA"),
            new TestCase("HighwayHash64", typeof(HighwayHash64), seed => new HighwayHash64(seed), "0x2D9D80CDBD9F39E2"),
            new TestCase("HighwayHash128", typeof(HighwayHash128), seed => new HighwayHash128(seed), "0x4C21867CD2DD8E1882AC86A53705C53C"),
            new TestCase("HighwayHash256", typeof(HighwayHash256), seed => new HighwayHash256(seed), "0x52608FF47620F0694CC650484BF6C530A05CDEC658AF6CE27667F8F75729FED8"),
            new TestCase("KomiHash", typeof(KomiHash), seed => new KomiHash(seed), "0xEDE8694AFBD7BD3B"),
            new TestCase("MetroHash64-V1", typeof(MetroHash64), seed => new MetroHash64(MetroHashVariant.V1, seed), "0xD2F788EE75E62D6B"),
            new TestCase("MetroHash64-V2", typeof(MetroHash64), seed => new MetroHash64(MetroHashVariant.V2, seed), "0x6E7CFCE14A183741"),
            new TestCase("MetroHash128-V1", typeof(MetroHash128), seed => new MetroHash128(MetroHashVariant.V1, seed), "0xD7A1E820E745C1A85B293EF5C2C876BC"),
            new TestCase("MetroHash128-V2", typeof(MetroHash128), seed => new MetroHash128(MetroHashVariant.V2, seed), "0x84C63754893C3AC63BAE5D82559AB27C"),
            new TestCase("MirHash", typeof(MirHash), seed => new MirHash(seed), "0xFC662A422C002E99"),
            new TestCase("MurmurHash32", typeof(MurmurHash32), seed => new MurmurHash32(seed), "0xE37EF5B0"),
            new TestCase("MurmurHash64-X86", typeof(MurmurHash64), seed => new MurmurHash64(MurmurHashEngine.x86, seed), "0xEC34E83397E0F193"),
            new TestCase("MurmurHash64-X64", typeof(MurmurHash64), seed => new MurmurHash64(MurmurHashEngine.x64, seed), "0x6D8C3D44B40A4391"),
            new TestCase("MurmurHash128-X86", typeof(MurmurHash128), seed => new MurmurHash128(MurmurHashEngine.x86, seed), "0x2AE6ECB33A231AD6384F94C332CAC079"),
            new TestCase("MurmurHash128-X64", typeof(MurmurHash128), seed => new MurmurHash128(MurmurHashEngine.x64, seed), "0x69BA846303DEF3632DED84E68C879271"),
            new TestCase("MumHash", typeof(MumHash), seed => new MumHash(seed), "0xC0C673A922821163"),
            new TestCase("Mx3Hash", typeof(Mx3Hash), seed => new Mx3Hash(seed), "0x657B287B75CBFD8B"),
            new TestCase("PengyHash", typeof(PengyHash), seed => new PengyHash(seed), "0xE316824706F463CE"),
            new TestCase("SipHash-13", typeof(SipHash), seed => new SipHash(SipHashVariant.V13, seed), "0xB2F594F65E5F1529"),
            new TestCase("SipHash-24", typeof(SipHash), seed => new SipHash(SipHashVariant.V24, seed), "0x92DC31BC9483210F"),
            new TestCase("SpookyHash32", typeof(SpookyHash32), seed => new SpookyHash32(seed), "0xBB8B793F"),
            new TestCase("SpookyHash64", typeof(SpookyHash64), seed => new SpookyHash64(seed), "0xF155F9A7B939EE62"),
            new TestCase("SpookyHash128", typeof(SpookyHash128), seed => new SpookyHash128(seed), "0x8030268DFD1A1430978CC2342FFEDAAA6D08879F228067F71E74FF83D5903E3DE7D57A67C25848034C46C21EB8361F59B08A199D9B25B73EFF7D4A9233A7C701F54331F2293E674524CD1A9D8550A1270473AE2C1925EAB0DA9F2B641458E626"),
            new TestCase("WyHash32", typeof(WyHash32), seed => new WyHash32(seed), "0x6680DE09"),
            new TestCase("WyHash64", typeof(WyHash64), seed => new WyHash64(seed), "0xD7BA2DA80326AF7D"),
            new TestCase("xxHash32", typeof(XxHash32), seed => new XxHash32(seed), "0x43B788BA"),
            new TestCase("xxHash64", typeof(XxHash64), seed => new XxHash64(seed), "0xF47C4B0200F12795")
        };

        public static IEnumerable<Object[]> DataCollision()
        {
            foreach (TestCase testCase in s_TestCases)
                yield return (new Object[] { testCase.HashName, testCase.HashInitializer });
        }

        public static IEnumerable<Object[]> DataLength()
        {
            foreach (var testCase in s_TestCases.Select(x => new { x.HashType, x.HashInitializer }).Distinct())
                yield return (new Object[] { testCase.HashType, testCase.HashInitializer, MAXIMUM_LENGTH });
        }

        public static IEnumerable<Object[]> DataValidation()
        {
            foreach (TestCase testCase in s_TestCases)
                yield return (new Object[] { testCase.HashName, testCase.HashInitializer, testCase.ExpectedValue });
        }
        #endregion

        #region Nested Classes
        private sealed class TestCase
        {
            #region Members
            private readonly Func<UInt32,Hash> m_HashInitializer;
            private readonly String m_HashName;
            private readonly String m_ExpectedValue;
            private readonly Type m_HashType;
            #endregion

            #region Properties
            public Func<UInt32,Hash> HashInitializer => m_HashInitializer;
            public String HashName => m_HashName;
            public String ExpectedValue => m_ExpectedValue;
            public Type HashType => m_HashType;
            #endregion

            #region Constructors
            public TestCase(String hashName, Type hashType, Func<UInt32,Hash> hashInitializer, String expectedValue)
            {
                if (String.IsNullOrWhiteSpace(hashName))
                    throw new ArgumentException("Invalid hash name specified.", nameof(hashName));

                if (hashType == null)
                    throw new ArgumentException("Invalid hash type specified.", nameof(hashType));

                if (hashInitializer == null)
                    throw new ArgumentException("Invalid hash initializer specified.", nameof(hashInitializer));

                if (String.IsNullOrWhiteSpace(hashName))
                    throw new ArgumentException("Invalid expected value specified.", nameof(expectedValue));

                m_HashInitializer = hashInitializer;
                m_HashName = hashName;
                m_ExpectedValue = expectedValue;
                m_HashType = hashType;
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