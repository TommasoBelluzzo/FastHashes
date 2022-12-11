#region Using Directives
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;
using Xunit.Abstractions;
#endregion

namespace FastHashes.Tests
{
    public sealed class HashTests : IClassFixture<Fixture>
    {
        #region Members
        private readonly Fixture m_Fixture;
        private readonly ITestOutputHelper m_Output;
        #endregion

        #region Constructors
        public HashTests(Fixture fixture, ITestOutputHelper output)
        {
            m_Fixture = fixture;
            m_Output = output;
        }
        #endregion

        #region Methods
        [Theory]
        [MemberData(nameof(HashTestsCases.DataCollision), MemberType=typeof(HashTestsCases))]
        public void CollisionTest(String hashName, Func<UInt32,Hash> hashInitializer)
        {
            Int32 wordsCount = m_Fixture.Words.Count();

            Assert.False(wordsCount == 0, "Fixture Words Empty");

            Hash hash = hashInitializer((UInt32)((new Random()).Next()));
            Int32 hashBytes = hash.Length / 8;

            Byte filler = Convert.ToByte('!');
            List<(String,Byte[])> hashes = new List<(String,Byte[])>(wordsCount * 5);

            foreach (String word in m_Fixture.Words)
            {
                Byte[] lineBytes = Encoding.UTF8.GetBytes(word);
                Int32 lineBytesLength = lineBytes.Length;

                Byte[] buffer = new Byte[lineBytes.Length + 5];
                Int32 bufferLength = buffer.Length;

                for (Int32 i = 0; i < bufferLength; ++i)
                    buffer[i] = filler;

                Buffer.BlockCopy(lineBytes, 0, buffer, 0, lineBytesLength);

                for (Int32 j = 0; j <= 5; ++j)
                    hashes.Add((Encoding.UTF8.GetString(buffer, 0, lineBytesLength + j), hash.ComputeHash(buffer, 0, lineBytesLength + j)));
            }

            Boolean cte = MathUtilities.CollisionsThresholdExceeded(hashes, hashBytes);

            m_Output.WriteLine($"NAME: {hashName}");
            m_Output.WriteLine($"RESULT: {(cte ? "FAILURE" : "SUCCESS")}");

            Assert.False(cte, "Collisions Threshold Exceeded");
        }

        [Fact]
        public void ExceptionTest()
        {
            Hash h = new FarmHash32();

            Assert.Throws<ArgumentNullException>(() => { Byte[] buffer = null; h.ComputeHash(buffer); });
            Assert.Throws<ArgumentNullException>(() => { Byte[] buffer = null; h.ComputeHash(buffer, 0, 10); });
            Assert.Throws<ArgumentOutOfRangeException>(() => { Byte[] buffer = new Byte[10]; h.ComputeHash(buffer, -1, 10); });
            Assert.Throws<ArgumentOutOfRangeException>(() => { Byte[] buffer = new Byte[10]; h.ComputeHash(buffer, 12, 10); });
            Assert.Throws<ArgumentOutOfRangeException>(() => { Byte[] buffer = new Byte[10]; h.ComputeHash(buffer, 0, -1); });
            Assert.Throws<ArgumentException>(() => { Byte[] buffer = new Byte[10]; h.ComputeHash(buffer, 8, 5); });
            Assert.Throws<ArgumentNullException>(() => { ReadOnlySpan<Byte> buffer = null; h.ComputeHash(buffer); });
        }

        [Theory]
        [MemberData(nameof(HashTestsCases.DataLength), MemberType = typeof(HashTestsCases))]
        public void LengthTest(String hashName, Func<UInt32, Hash> hashInitializer, Int32 maximumLength)
        {
            UInt32 seed = (UInt32)((new Random()).Next());
            Hash hash = hashInitializer(seed);
            RandomXorShift random = new RandomXorShift(seed);

            List<String> errorLengths = new List<String>();

            for (Int32 i = 0; i <= maximumLength; ++i)
            {
                Byte[] buffer = new Byte[i];
                random.NextBytes(buffer);

                try
                {
                    hash.ComputeHash(buffer);
                }
                catch
                {
                    errorLengths.Add(i.ToString());
                }
            }

            m_Output.WriteLine($"NAME: {hashName}");

            if (errorLengths.Count > 0)
                m_Output.WriteLine($"ERROR LENGTHS: {String.Join(", ", errorLengths)}");
            else
                m_Output.WriteLine("NO ERROR LENGTHS");

            Assert.True(errorLengths.Count == 0);
        }

        [Fact]
        public void OutputTest()
        {
            Byte[] buffer = new Byte[] { 23, 134, 0, 237, 0, 81, 64, 64, 39, 5 };

            Hash h = new FarmHash32();
            Byte[] hash1 = h.ComputeHash(buffer);
            Byte[] hash2 = h.ComputeHash(buffer, buffer.Length);
            Byte[] hash3 = h.ComputeHash(buffer, 0, buffer.Length);
            Byte[] hash4 = h.ComputeHash(new ReadOnlySpan<Byte>(buffer));

            Assert.Equal(hash1, hash2);
            Assert.Equal(hash1, hash3);
            Assert.Equal(hash1, hash4);
        }

        [Theory]
        [MemberData(nameof(HashTestsCases.DataValidation), MemberType=typeof(HashTestsCases))]
        public void ValidationTest(String hashName, Func<UInt32,Hash> hashInitializer, String expectedValue)
        {
            Hash hash0 = hashInitializer(0u);
            Int32 hashBytes = hash0.Length / 8;
        
            Byte[] buffer = new Byte[256];
            Byte[] bufferFinal = new Byte[hashBytes * 256];
        
            for (Int32 i = 0; i < 256; ++i)
            {
                buffer[i] = (Byte)i;
        
                Hash hashi = hashInitializer((UInt32)(256 - i));
                Byte[] hi = hashi.ComputeHash(buffer, 0, i);

                Buffer.BlockCopy(hi, 0, bufferFinal, i * hashBytes, hashBytes);
            }
        
            Byte[] h0 = hash0.ComputeHash(bufferFinal);   
            String actualValue = String.Concat("0x", BitConverter.ToString(h0).Replace("-", String.Empty));

            m_Output.WriteLine($"NAME: {hashName}");
            m_Output.WriteLine($"EXPECTED: {expectedValue}");
            m_Output.WriteLine($"ACTUAL: {actualValue}");

            Assert.Equal(expectedValue, actualValue);
        }
        #endregion
    }
}