#region Using Directives
using System;
using System.Collections.Generic;
#endregion

namespace FastHashes.Tests
{
    internal static class TestsCases
    {
        #region Constants
        private static readonly Dictionary<String, String> VALIDATION_CASES = new(StringComparer.Ordinal)
        {
            ["A5Hash32"] = "0x1BD148A9",
            ["A5Hash64"] = "0xB379DEADE864EBB4",
            ["A5Hash128"] = "0x116B408952FBEDF107A0D75A03AB167B",
            ["FarmHash32"] = "0x39AFC90D",
            ["FarmHash64"] = "0x79A6C4EB62E22A5C",
            ["FarmHash128"] = "0x71BF3EA9A4D9F2351533AF0F4A96CEFB",
            ["FastHash32"] = "0xFC1A48E9",
            ["FastHash64"] = "0xA73162A1DA6BDCBD",
            ["FastPositiveHash-V0"] = "0x297B7D7F81EC046C",
            ["FastPositiveHash-V1"] = "0x816383D664638705",
            ["FastPositiveHash-V2"] = "0x48C9168F5572BC11",
            ["HalfSipHash"] = "0x4C192ADA",
            ["HalftimeHash-S64"] = "0x8FA0F2557E35A798",
            ["HalftimeHash-S128"] = "0xD3C3EE437B993FB8",
            ["HalftimeHash-S256"] = "0xA9F64B555BEF6362",
            ["HalftimeHash-S512"] = "0x7BF8B40E106BC6AA",
            ["HighwayHash64"] = "0x2D9D80CDBD9F39E2",
            ["HighwayHash128"] = "0x4C21867CD2DD8E1882AC86A53705C53C",
            ["HighwayHash256"] = "0x52608FF47620F0694CC650484BF6C530A05CDEC658AF6CE27667F8F75729FED8",
            ["KomiHash"] = "0x6DFF5781E27E4E69",
            ["LaneHash64"] = "0x2BC248D07188AEAC",
            ["LaneHash128"] = "0x3999F3C22CC641C4938E9F26A9DF90AA",
            ["MetroHash64-V1"] = "0xD2F788EE75E62D6B",
            ["MetroHash64-V2"] = "0x6E7CFCE14A183741",
            ["MetroHash128-V1"] = "0xD7A1E820E745C1A85B293EF5C2C876BC",
            ["MetroHash128-V2"] = "0x84C63754893C3AC63BAE5D82559AB27C",
            ["MirHash"] = "0xFC662A422C002E99",
            ["MurmurHash32"] = "0xE37EF5B0",
            ["MurmurHash64-X86"] = "0xEC34E83397E0F193",
            ["MurmurHash64-X64"] = "0x6D8C3D44B40A4391",
            ["MurmurHash128-X86"] = "0x2AE6ECB33A231AD6384F94C332CAC079",
            ["MurmurHash128-X64"] = "0x69BA846303DEF3632DED84E68C879271",
            ["MumHash"] = "0x1566E067481DA246",
            ["Mx3Hash"] = "0x657B287B75CBFD8B",
            ["NmHash-S"] = "0x5305A312",
            ["NmHash-X"] = "0x270258A8",
            ["PengyHash"] = "0x7B21C21F62359105",
            ["PolymurHash"] = "0xA7B12207F06C150E",
            ["RapidHash-V1"] = "0x4B4C40AF2E277E4B",
            ["RapidHash-V2"] = "0xCC5E2094156B34C5",
            ["RapidHash-V3"] = "0xEE65DC1FA088816D",
            ["SipHash-13"] = "0xB2F594F65E5F1529",
            ["SipHash-24"] = "0x92DC31BC9483210F",
            ["SpookyHash32"] = "0x65E28BA4",
            ["SpookyHash64"] = "0xDC4B2C973FE48012",
            ["SpookyHash128"] = "0xBEFC3C892A16F01B23B81DE576CA399A",
            ["Umash64"] = "0xCD64A23666A52B91",
            ["Umash128"] = "0x057D85637C4D1D0BA7A070DCC9A6516F",
            ["W1Hash"] = "0xF148896451D5889C",
            ["WyHash32"] = "0x6680DE09",
            ["WyHash64"] = "0xD37DAE9D1F46D8DB",
            ["XxHash32"] = "0x43B788BA",
            ["XxHash64"] = "0xF47C4B0200F12795",
            ["XxHashThree64"] = "0x0564639A4BD12D32",
            ["XxHashThree128"] = "0x848EE45A0AA19CBD18910CDBAD010930"
        };
        #endregion

        #region Test Cases
        public static IEnumerable<Object[]> Hashes()
        {
            foreach (String hashName in Hash.GetAlgorithms())
            {
                if (!VALIDATION_CASES.TryGetValue(hashName, out String expected))
                    throw new InvalidOperationException($"Missing validation value for {hashName}.");

                yield return new Object[] { hashName, Hash.GetInitializerSeeded(hashName).Ctor, expected };
            }
        }
        #endregion
    }
}