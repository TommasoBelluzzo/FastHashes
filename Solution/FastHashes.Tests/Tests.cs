#region Using Directives
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using Xunit;
#endregion

namespace FastHashes.Tests
{
    [Collection("Tests")]
    public sealed class Tests
    {
        #region Members
        private readonly TestsFixture m_TestsFixture;
        #endregion

        #region Constructors
        public Tests(TestsFixture fixture)
        {
            m_TestsFixture = fixture;
        }
        #endregion

        #region Methods
        [Theory]
        [InlineData("FarmHash32")]
        [InlineData("FarmHash64")]
        [InlineData("FarmHash128")]
        [InlineData("FastHash32")]
        [InlineData("FastHash64")]
        [InlineData("FastPositiveHash-V0")]
        [InlineData("FastPositiveHash-V1")]
        [InlineData("FastPositiveHash-V2")]
        [InlineData("HalfSipHash")]
        [InlineData("HighwayHash64")]
        [InlineData("HighwayHash128")]
        [InlineData("HighwayHash256")]
        [InlineData("MetroHash64-V1")]
        [InlineData("MetroHash64-V2")]
        [InlineData("MetroHash128-V1")]
        [InlineData("MetroHash128-V2")]
        [InlineData("MurmurHash32")]
        [InlineData("MurmurHash64-X86")]
        [InlineData("MurmurHash64-X64")]
        [InlineData("MurmurHash128-X86")]
        [InlineData("MurmurHash128-X64")]
        [InlineData("MumHash")]
        [InlineData("SipHash-13")]
        [InlineData("SipHash-24")]
        [InlineData("SpookyHash32")]
        [InlineData("SpookyHash64")]
        [InlineData("SpookyHash128")]
        [InlineData("xxHash32")]
        [InlineData("xxHash64")]
        public void CollisionTest(String hashIdentifier)
        {
            ReadOnlyCollection<String> words = m_TestsFixture.Words;

            Hash hash = m_TestsFixture.CreateHash(hashIdentifier);
            Int32 hashBytes = hash.Length / 8;

            Byte filler = Convert.ToByte('!');
            List<Byte[]> hashes = new List<Byte[]>(words.Count * 5);

            for (Int32 i = 0; i < words.Count; ++i)
            {
                Byte[] lineBytes = Encoding.UTF8.GetBytes(words[i]);
                Int32 lineBytesLength = lineBytes.Length;

                Byte[] buffer = new Byte[lineBytes.Length + 5];
                Utilities.FillBuffer(buffer, filler);
                UnsafeBuffer.BlockCopy(lineBytes, 0, buffer, 0, lineBytes.Length);

                for (Int32 j = 0; j <= 5; ++j)
                    hashes.Add(hash.ComputeHash(buffer, 0, lineBytesLength + j));
            }

            Assert.False(Utilities.CollisionsThresholdExceeded(hashes, hashBytes));
        }

        [Theory]
        [InlineData("FarmHash32", 0x0DC9AF39u)]
        [InlineData("FarmHash64", 0xEBC4A679u)]
        [InlineData("FarmHash128", 0xA93EBF71u)]
        [InlineData("FastHash32", 0xE9481AFCu)]
        [InlineData("FastHash64", 0xA16231A7u)]
        [InlineData("FastPositiveHash-V0", 0x7F7D7B29u)]
        [InlineData("FastPositiveHash-V1", 0xD6836381u)]
        [InlineData("FastPositiveHash-V2", 0x8F16C948u)]
        [InlineData("HalfSipHash", 0xDA2A194Cu)]
        [InlineData("HighwayHash64", 0xCD809D2Du)]
        [InlineData("HighwayHash128", 0x7C86214Cu)]
        [InlineData("HighwayHash256", 0xF48F6052u)]
        [InlineData("MetroHash64-V1", 0xEE88F7D2u)]
        [InlineData("MetroHash64-V2", 0xE1FC7C6Eu)]
        [InlineData("MetroHash128-V1", 0x20E8A1D7u)]
        [InlineData("MetroHash128-V2", 0x5437C684u)]
        [InlineData("MurmurHash32", 0xB0F57EE3u)]
        [InlineData("MurmurHash64-X86", 0x33E834ECu)]
        [InlineData("MurmurHash64-X64", 0x443D8C6Du)]
        [InlineData("MurmurHash128-X86", 0xB3ECE62Au)]
        [InlineData("MurmurHash128-X64", 0x6384BA69u)]
        [InlineData("MumHash", 0xA973C6C0u)]
        [InlineData("SipHash-13", 0xF694F5B2u)]
        [InlineData("SipHash-24", 0xBC31DC92u)]
        [InlineData("SpookyHash32", 0x3F798BBBu)]
        [InlineData("SpookyHash64", 0xA7F955F1u)]
        [InlineData("SpookyHash128", 0x8D263080u)]
        [InlineData("xxHash32", 0xBA88B743u)]
        [InlineData("xxHash64", 0x024B7CF4u)]
        public void ValidationTest(String hashIdentifier, UInt32 expected)
        {
            Hash hash0 = m_TestsFixture.CreateHash(hashIdentifier, 0u);
            Int32 hashBytes = hash0.Length / 8;

            Byte[] buffer = new Byte[256];
            Byte[] bufferFinal = new Byte[hashBytes * 256];

            for (Int32 i = 0; i < 256; ++i)
            {
                buffer[i] = (Byte)i;

                Hash hashi = m_TestsFixture.CreateHash(hashIdentifier, (UInt32)(256 - i));
                Byte[] hi = hashi.ComputeHash(buffer, 0, i);

                UnsafeBuffer.BlockCopy(hi, 0, bufferFinal, i * hashBytes, hashBytes);
            }

            Byte[] h0 = hash0.ComputeHash(bufferFinal);   
            UInt32 actual = (UInt32)((h0[0] << 0) | (h0[1] << 8) | (h0[2] << 16) | (h0[3] << 24));

            Assert.Equal(expected, actual);
        }
        #endregion
    }
}