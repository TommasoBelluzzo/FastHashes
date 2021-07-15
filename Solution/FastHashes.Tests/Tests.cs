#region Using Directives
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Xunit;
using Xunit.Abstractions;
#endregion

namespace FastHashes.Tests
{
    public sealed class Tests : IClassFixture<Fixture>
    {
        #region Test Cases
        private static readonly List<TestCase> s_TestCases = new List<TestCase>
        {
            new TestCase("FarmHash32", seed => new FarmHash32(seed), 0x0DC9AF39u),
            new TestCase("FarmHash64", seed => new FarmHash64(seed), 0xEBC4A679u),
            new TestCase("FarmHash128", seed => new FarmHash128(seed), 0xA93EBF71u),
            new TestCase("FastHash32", seed => new FastHash32(seed), 0xE9481AFCu),
            new TestCase("FastHash64", seed => new FastHash64(seed), 0xA16231A7u),
            new TestCase("FastPositiveHash-V0", seed => new FastPositiveHash(FastPositiveHashVariant.V0, seed), 0x7F7D7B29u),
            new TestCase("FastPositiveHash-V1", seed => new FastPositiveHash(FastPositiveHashVariant.V1, seed), 0xD6836381u),
            new TestCase("FastPositiveHash-V2", seed => new FastPositiveHash(FastPositiveHashVariant.V2, seed), 0x8F16C948u),
            new TestCase("HalfSipHash", seed => new HalfSipHash(seed), 0xDA2A194Cu),
            new TestCase("HighwayHash64", seed => new HighwayHash64(seed), 0xCD809D2Du),
            new TestCase("HighwayHash128", seed => new HighwayHash128(seed), 0x7C86214Cu),
            new TestCase("HighwayHash256", seed => new HighwayHash256(seed), 0xF48F6052u),
            new TestCase("MetroHash64-V1", seed => new MetroHash64(MetroHashVariant.V1, seed), 0xEE88F7D2u),
            new TestCase("MetroHash64-V2", seed => new MetroHash64(MetroHashVariant.V2, seed), 0xE1FC7C6Eu),
            new TestCase("MetroHash128-V1", seed => new MetroHash128(MetroHashVariant.V1, seed), 0x20E8A1D7u),
            new TestCase("MetroHash128-V2", seed => new MetroHash128(MetroHashVariant.V2, seed), 0x5437C684u),
            new TestCase("MurmurHash32", seed => new MurmurHash32(seed), 0xB0F57EE3u),
            new TestCase("MurmurHash64-X86", seed => new MurmurHash64(MurmurHashEngine.x86, seed), 0x33E834ECu),
            new TestCase("MurmurHash64-X64", seed => new MurmurHash64(MurmurHashEngine.x64, seed), 0x443D8C6Du),
            new TestCase("MurmurHash128-X86", seed => new MurmurHash128(MurmurHashEngine.x86, seed), 0xB3ECE62Au),
            new TestCase("MurmurHash128-X64", seed => new MurmurHash128(MurmurHashEngine.x64, seed), 0x6384BA69u),
            new TestCase("MumHash", seed => new MumHash(seed), 0xA973C6C0u),
            new TestCase("SipHash-13", seed => new SipHash(SipHashVariant.V13, seed), 0xF694F5B2u),
            new TestCase("SipHash-24", seed => new SipHash(SipHashVariant.V24, seed), 0xBC31DC92u),
            new TestCase("SpookyHash32", seed => new SpookyHash32(seed), 0x3F798BBBu),
            new TestCase("SpookyHash64", seed => new SpookyHash64(seed), 0xA7F955F1u),
            new TestCase("SpookyHash128", seed => new SpookyHash128(seed), 0x8D263080u),
            new TestCase("xxHash32", seed => new XxHash32(seed), 0xBA88B743u),
            new TestCase("xxHash64", seed => new XxHash64(seed), 0x024B7CF4u)
        };

        public static IEnumerable<Object[]> DataCollision()
        {
            foreach (TestCase testCase in s_TestCases)
                yield return (new Object[] { testCase.HashName });
        }

        public static IEnumerable<Object[]> DataValidation()
        {
            foreach (TestCase testCase in s_TestCases)
                yield return (new Object[] { testCase.HashName, testCase.HashValue });
        }
        #endregion

        #region Members
        private readonly Fixture m_Fixture;
        private readonly ITestOutputHelper m_Output;
        private readonly RandomXorShift m_Random;
        #endregion

        #region Constructors
        public Tests(Fixture fixture, ITestOutputHelper output)
        {
            m_Fixture = fixture;
            m_Output = output;
            m_Random = new RandomXorShift();
        }
        #endregion

        #region Methods
        [Theory(DisplayName="Collision Tests")]
        [MemberData(nameof(DataCollision))]
        public void CollisionTests(String hashName)
        {
            string dir = AppDomain.CurrentDomain.BaseDirectory;

            string asd = String.Join('\n',Directory.GetFiles(dir, "*.*", SearchOption.AllDirectories));
                    
            m_Output.WriteLine(asd);

            Int32 wordsCount = m_Fixture.Words.Count();

            Assert.False(wordsCount == 0, "Fixture Words Empty");

            Func<UInt32,Hash> hashInitializer = s_TestCases.Single(x => x.HashName == hashName).HashInitializer;
            
            Hash hash = hashInitializer(m_Random.NextValue());
            Int32 hashBytes = hash.Length / 8;

            Byte filler = Convert.ToByte('!');
            List<Byte[]> hashes = new List<Byte[]>(wordsCount * 5);

            foreach (String word in m_Fixture.Words)
            {
                Byte[] lineBytes = Encoding.UTF8.GetBytes(word);
                Int32 lineBytesLength = lineBytes.Length;

                Byte[] buffer = new Byte[lineBytes.Length + 5];
                Utilities.FillBuffer(buffer, filler);
                UnsafeBuffer.BlockCopy(lineBytes, 0, buffer, 0, lineBytes.Length);

                for (Int32 j = 0; j <= 5; ++j)
                    hashes.Add(hash.ComputeHash(buffer, 0, lineBytesLength + j));
            }

            Assert.False(Utilities.CollisionsThresholdExceeded(hashes, hashBytes), "Collisions Threshold Exceeded");
        }

        [Theory(DisplayName="Validation Tests")]
        [MemberData(nameof(DataValidation))]
        public void ValidationTests(String hashName, UInt32 hashValue)
        {
            Func<UInt32,Hash> hashInitializer = s_TestCases.Single(x => x.HashName == hashName).HashInitializer;

            Hash hash0 = hashInitializer(0u);
            Int32 hashBytes = hash0.Length / 8;
        
            Byte[] buffer = new Byte[256];
            Byte[] bufferFinal = new Byte[hashBytes * 256];
        
            for (Int32 i = 0; i < 256; ++i)
            {
                buffer[i] = (Byte)i;
        
                Hash hashi = hashInitializer((UInt32)(256 - i));
                Byte[] hi = hashi.ComputeHash(buffer, 0, i);
        
                UnsafeBuffer.BlockCopy(hi, 0, bufferFinal, i * hashBytes, hashBytes);
            }
        
            Byte[] h0 = hash0.ComputeHash(bufferFinal);   
            UInt32 actualValue = (UInt32)((h0[0] << 0) | (h0[1] << 8) | (h0[2] << 16) | (h0[3] << 24));
        
            Assert.Equal(hashValue, actualValue);
        }
        #endregion
    }
}