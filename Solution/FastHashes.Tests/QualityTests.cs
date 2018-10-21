#region Using Directives
using System;
using System.Collections.Generic;
#endregion

namespace FastHashes.Tests
{
    public static class QualityTests
    {
        #region Constants
        private const Int32 BICT_REPETITIONS = 2000000;
        private const Int32 BICT_KEYSBYTES = 11;
        private const Int32 DT_MAXIMUMERRORS = 100000;
        #endregion

        #region Members
        private static readonly dynamic[] AT_PARAMETERS =
        {
            new { KeysBytes = 0, Repetitions = 300000 },
            new { KeysBytes = 4, Repetitions = 300000 },
            new { KeysBytes = 8, Repetitions = 300000 },
            new { KeysBytes = 16, Repetitions = 300000 },
            new { KeysBytes = 32, Repetitions = 300000 },
            new { KeysBytes = 64, Repetitions = 300000 },
            new { KeysBytes = 128, Repetitions = 300000 }
        };

        private static readonly dynamic[] DT_PARAMETERS =
        {
            new { Bits = 5, KeysBytes = 8, Repetitions = 1000 },
            new { Bits = 4,KeysBytes = 16, Repetitions = 1000 },
            new { Bits = 3,KeysBytes = 32, Repetitions = 1000 }
        };
        #endregion

        #region Methods
        private static Int32 DifferentialTestIgnored(List<Byte[]> diffs, ref Boolean result)
        {
            Int32 diffsCount = diffs.Count;
            Int32 ignoredCollisions = 0;

            if (diffsCount == 0)
                return ignoredCollisions;

            diffs.Sort(NativeMethods.CompareSequences);

            Byte[] diff = diffs[0];
            Int32 collisionsCount = 1;

            for (Int32 i = 1; i < diffsCount; ++i)
            {
                if (NativeMethods.EqualSequences(diff, diffs[i]))
                    ++collisionsCount;
                else
                {
                    if (collisionsCount > 1)
                        result = false;
                    else 
                        ++ignoredCollisions;

                    diff = diffs[i];
                    collisionsCount = 1;
                }
            }

            if (collisionsCount < 2)
                ++ignoredCollisions;

            return ignoredCollisions;
        }

        private static UInt64 DifferentialTestRecursion(List<Byte[]> diffs, Hash hash, Int32 keysBytes, Byte[] k1, Byte[] k2, Int32 hashBytes, Byte[] h1, Byte[] h2, Int32 start, Int32 bitsleft)
        {
            UInt64 skipped = 0;

            for (Int32 i = start; i < (keysBytes * 8); ++i)
            {
                --bitsleft;

                BitsUtilities.FlipBit(k2, 0, keysBytes, i);

                Byte[] h = hash.ComputeHash(k2);
                Buffer.BlockCopy(h, 0, h2, 0, hashBytes);

                if (NativeMethods.EqualSequences(h1, h2))
                {
                    if (diffs.Count < DT_MAXIMUMERRORS)
                    {
                        Byte[] diff = new Byte[keysBytes];

                        for (Int32 j = 0; j < keysBytes; ++j)
                            diff[j] = (Byte)(k1[j] ^ k2[j]);

                        diffs.Add(diff);
                    }
                    else
                        ++skipped;
                }

                if (bitsleft > 0)
                    skipped += DifferentialTestRecursion(diffs, hash, keysBytes, k1, k2, hashBytes, h1, h2, i + 1, bitsleft);

                BitsUtilities.FlipBit(k2, 0, keysBytes, i);

                ++bitsleft;
            }

            return skipped;
        }

        public static void AvalancheTest(HashInfo hashInfo)
        {
            RandomXS r = new RandomXS(0x000000B0u);
            Hash hash = hashInfo.Initializer(r.NextValue());
            Int32 hashBytes = hashInfo.Length / 8;
            Int32 hashBits = hashInfo.Length;

            Console.WriteLine("[[ AVALANCHE TEST ]]");

            Boolean resultOverall = true;

            for (Int32 i = 0; i < AT_PARAMETERS.Length; ++i)
            {
                dynamic p = AT_PARAMETERS[i];

                Int32 keyBytes = p.KeysBytes;
                Int32 keyBits = keyBytes * 8;
                Byte[] key = new Byte[p.KeysBytes];

                Int32 repetitions = p.Repetitions;
                Int32 step = repetitions / 10;

                Console.WriteLine($"> Keys Length: {keyBytes} Bytes");
                Console.WriteLine($"  Repetitions: {repetitions}");
                Console.Write("  Result: ");

                Int32 binsCount = keyBits * hashBits;
                Int32[] bins = new Int32[keyBits * hashBits];

                for (Int32 j = 0; j < repetitions; ++j)
                {
                    if ((j % step) == 0)
                        Console.Write(".");

                    r.NextBytes(key);

                    Byte[] h0 = hash.ComputeHash(key);

                    Int32 binsIndex = 0;

                    for (Int32 keyBit = 0; keyBit < keyBits; ++keyBit)
                    {
                        BitsUtilities.FlipBit(key, 0, keyBytes, keyBit);
                        Byte[] hi = hash.ComputeHash(key);
                        BitsUtilities.FlipBit(key, 0, keyBytes, keyBit);

                        for (Int32 hashBit = 0; hashBit < hashBits; ++hashBit)
                        {
                            Byte a = BitsUtilities.GetBit(h0, 0, hashBytes, hashBit);
                            Byte b = BitsUtilities.GetBit(hi, 0, hashBytes, hashBit);

                            bins[binsIndex++] += a ^ b;
                        }
                    }
                }

                Double worstBias = 0.0d;

                for (Int32 j = 0; j < binsCount; ++j)
                {
                    Double c = (Double)bins[j] / repetitions;
                    Double bias = Math.Abs((c * 2.0d) - 1.0d);
      
                    if (bias > worstBias)
                        worstBias = bias;
                }

                Boolean result = (worstBias <= 0.01d);
                Console.WriteLine(result ? " PASSED" : " FAILED");
                Console.WriteLine($"   - Worst Bias: {(worstBias.Equals(0.0d) ? "Unbiased" : $"{(worstBias * 100.0d):F2}%")}");

                resultOverall &= result;
            }

            Console.WriteLine($"Test Result: {(resultOverall ? "PASSED" : "FAILED")}");
        }

        public static void BitIndependenceCriterionTest(HashInfo hashInfo)
        {
            RandomXS r = new RandomXS(0x000000B1u);

            Hash hash = hashInfo.Initializer(r.NextValue());
            Int32 hashBytes = hashInfo.Length / 8;
            Int32 hashBits = hashInfo.Length;
            Int32 pageSize = hashBits*hashBits*4;

            Int32 keyBytes = BICT_KEYSBYTES;
            Int32 keyBits = keyBytes * 8;
            Byte[] key = new Byte[keyBytes];

            Double biasFactor = BICT_REPETITIONS / 2.0d;

            Int32 step = keyBits / 10;

            Console.WriteLine("[[ BIT INDEPENDENCE CRITERION TEST ]]");
            Console.Write("Result: ");

            Int32[] bins = new Int32[keyBits * pageSize];

            for (Int32 keyBit = 0; keyBit < keyBits; ++keyBit)
            {
                if((keyBit % step) == 0)
                    Console.Write(".");

                Int32 pageOffset = keyBit * pageSize;

                for (Int32 irep = 0; irep < BICT_REPETITIONS; ++irep)
                {
                    r.NextBytes(key);

                    Byte[] h1 = hash.ComputeHash(key);
                    BitsUtilities.FlipBit(key, 0, keyBytes, keyBit);
                    Byte[] h2 = hash.ComputeHash(key);

                    Byte[] d = new Byte[hashBytes];

                    for (Int32 i = 0; i < hashBytes; ++i)
                        d[i] = (Byte)(h1[i] ^ h2[i]);

                    for (Int32 hashBit1 = 0; hashBit1 < hashBits - 1; ++hashBit1)
                    for (Int32 hashBit2 = hashBit1 + 1; hashBit2 < hashBits; ++hashBit2)
                    {
                        Int32 x = BitsUtilities.GetBit(d, 0, hashBytes, hashBit1) | (BitsUtilities.GetBit(d, 0, hashBytes, hashBit2) << 1);
                        ++bins[pageOffset + (((hashBit1 * hashBits) + hashBit2) * 4) + x];
                    }
                }
            }

            Double worstBias = 0.0d;
            Int32 worstKeyBit = -1;
            Int32 worstHashBit1 = -1;
            Int32 worstHashBit2 = -1;

            for (Int32 hashBit1 = 0; hashBit1 < hashBits - 1; ++hashBit1)
            for (Int32 hashBit2 = hashBit1 + 1; hashBit2 < hashBits; ++hashBit2)
            for (Int32 keyBit = 0; keyBit < keyBits; ++keyBit)
            {
                Int32 binsOffset = (keyBit * pageSize) + ((hashBit1 * hashBits) + hashBit2) * 4;

                for (Int32 b = 0; b < 4; ++b)
                {
                    Double c = bins[binsOffset + b] / biasFactor;
                    Double bias = Math.Abs((c * 2.0d) - 1.0d);

                    if (bias > worstBias)
                    {
                        worstBias = bias;
                        worstKeyBit = keyBit;
                        worstHashBit1 = hashBit1;
                        worstHashBit2 = hashBit2;
                    } 
                }
            }

            Boolean result = (worstBias <= 0.05d);
            Console.WriteLine(result ? " PASSED" : " FAILED");
            Console.WriteLine($"  - Worst Bias: {(worstBias.Equals(0.0d) ? "Unbiased" : $"{(worstBias * 100.0d):F2}% (Key Bit: {worstKeyBit} | Hash Bit 1: {worstHashBit1} | Hash Bit 2: {worstHashBit2})")}");
        }

        public static void DifferentialTest(HashInfo hashInfo)
        {
            RandomXS r = new RandomXS(0x000000B2u);
            Int32 hashBytes = hashInfo.Length / 8;
            Int32 hashBits = hashInfo.Length;

            Console.WriteLine("[[ DIFFERENTIAL TEST ]]");
            Console.WriteLine($"Maximum Errors: {DT_MAXIMUMERRORS}");

            Boolean resultOverall = true;

            for (Int32 i = 0; i < DT_PARAMETERS.Length; ++i)
            {
                dynamic p = DT_PARAMETERS[i];

                Int32 keysBytes = p.KeysBytes;
                Int32 keysBits = keysBytes * 8;
                Byte[] key1 = new Byte[keysBytes];
                Byte[] key2 = new Byte[keysBytes];

                Int32 bits = p.Bits;
                Int32 repetitions = p.Repetitions;
                Int32 steps = repetitions / 10;

                Double tests = StatsUtilities.ChooseUpToK(keysBits, p.Bits) * repetitions;
                Double expectedCollisions = Math.Round(tests / Math.Pow(2.0d, hashBits));

                Console.WriteLine($"> Bits: 1-{bits}");
                Console.WriteLine($"  Repetitions: {repetitions}");
                Console.WriteLine($"  Tests: {tests:F0}");
                Console.WriteLine($"  Expected Collisions: {expectedCollisions:F0}");
                Console.Write("  Result: ");

                Byte[] h2 = new Byte[hashBytes];
                UInt64 skipped = 0;
                List<Byte[]> diffs = new List<Byte[]>(DT_MAXIMUMERRORS);

                for (Int32 j = 0; j < repetitions; ++j)
                {
                    if ((j % steps) == 0)
                        Console.Write((skipped > 0) ? "!" : ".");

                    r.NextBytes(key1);
                    Buffer.BlockCopy(key1, 0, key2, 0, keysBytes);

                    Hash hash = hashInfo.Initializer(r.NextValue());
                    Byte[] h1 = hash.ComputeHash(key1);

                    skipped += DifferentialTestRecursion(diffs, hash, keysBytes, key1, key2, hashBytes, h1, h2, 0, p.Bits);
                }

                Boolean result = true;

                if (skipped > 0)
                {
                    result = false;

                    Console.WriteLine(" FAILED");
                    Console.WriteLine("   - Errors Threshold Exceeded");
                }
                else
                {
                    Int32 ignoredCollisions = DifferentialTestIgnored(diffs, ref result);

                    Console.WriteLine(result ? " PASSED" : " FAILED");
                    Console.WriteLine($"   - Observed Collisions: {diffs.Count} ({ignoredCollisions} Ignored)");
                }

                resultOverall &= result;
            }

            Console.WriteLine($"Test Result: {(resultOverall ? "PASSED" : "FAILED")}");
        }
        #endregion
    }
}