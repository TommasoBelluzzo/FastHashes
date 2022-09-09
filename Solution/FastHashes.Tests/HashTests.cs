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

            Hash hash = hashInitializer((UInt32)(new Random()).Next());
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

            Boolean cte = Utilities.CollisionsThresholdExceeded(hashes, hashBytes);

            m_Output.WriteLine($"NAME: {hashName}");
            m_Output.WriteLine($"RESULT: {(cte ? "FAILURE" : "SUCCESS")}");

            Assert.False(cte, "Collisions Threshold Exceeded");
        }

        [Fact]
        public void ExceptionTest()
        {
            Hash hash = new FarmHash128();

            Assert.Throws<ArgumentNullException>(() => { Byte[] buffer = null; hash.ComputeHash(buffer); });
            Assert.Throws<ArgumentNullException>(() => { Byte[] buffer = null; hash.ComputeHash(buffer, 0, 10); });
            Assert.Throws<ArgumentOutOfRangeException>(() => { Byte[] buffer = new Byte[10]; hash.ComputeHash(buffer, -1, 10); });
            Assert.Throws<ArgumentOutOfRangeException>(() => { Byte[] buffer = new Byte[10]; hash.ComputeHash(buffer, 12, 10); });
            Assert.Throws<ArgumentOutOfRangeException>(() => { Byte[] buffer = new Byte[10]; hash.ComputeHash(buffer, 0, -1); });
            Assert.Throws<ArgumentException>(() => { Byte[] buffer = new Byte[10]; hash.ComputeHash(buffer, 8, 5); });
            Assert.Throws<ArgumentNullException>(() => { ReadOnlySpan<Byte> buffer = null; hash.ComputeHash(buffer); });
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