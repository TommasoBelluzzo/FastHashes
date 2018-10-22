#region Using Directives
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
#endregion

namespace FastHashes.Tests
{
    public static class  ValidationTests
    {
        #region Constants
        private const Char WT_FILLER = '!';
        private const Int32 FKT_LENGTHMIN = 4;
        private const Int32 FKT_LENGTHMAX = 256;
        private const Int32 FKT_PADDING = 16;
        private const Int32 FKT_REPETITIONS = 10;
        private const Int32 PKT_REPETITIONS = 1000;
        private const Int32 PKT_KEYSBYTES = 256;
        private const Int32 TBT_INCREMENT = 4;
        private const Int32 TBT_LENGTHMIN = 4;
        private const Int32 TBT_LENGTHMAX = 16;
        private const Int32 UKT_KEYSBYTES = 256 * 1024;
        private const Int32 UKT_REPETITIONS = 10;
        private const Int32 VT_ITERATIONS = 256;
        private const Int32 WKT_KEYSMULTIPLIER = 2;
        private const Int32 WKT_WINDOWBITS = 20;
        private const Int32 WT_VARIANTS = 5;
        private const String KST_CHARACTERS = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        #endregion

        #region Members
        private static readonly Byte[] s_ValuesUKT = { 0x00, 0xFF };

        private static readonly dynamic[] s_ParametersCKT =
        {
            new { CycleBytes = 0, CycleRepetitions = 8, KeysCount = 10000000 },
            new { CycleBytes = 1, CycleRepetitions = 8, KeysCount = 10000000 },
            new { CycleBytes = 2, CycleRepetitions = 8, KeysCount = 10000000 },
            new { CycleBytes = 3, CycleRepetitions = 8, KeysCount = 10000000 },
            new { CycleBytes = 4, CycleRepetitions = 8, KeysCount = 10000000 }
        };

        private static readonly dynamic[] s_ParametersCT =
        {
            new { Name = "Null Bit + Low Bit", KeysSize = 20, Blocks = new[] { 0x00000000u, 0x00000001u } },
            new { Name = "Null Bit + High Bit", KeysSize = 20, Blocks = new[] { 0x00000000u, 0x80000000u } },
            new { Name = "Low Bits", KeysSize = 8, Blocks = new[] { 0x00000000u, 0x00000001u, 0x00000002u, 0x00000003u, 0x00000004u, 0x00000005u, 0x00000006u, 0x00000007u } },
            new { Name = "High Bits", KeysSize = 8, Blocks = new[] { 0x80000000u, 0x90000000u, 0xA0000000u, 0xB0000000u, 0xC0000000u, 0xD0000000u, 0xE0000000u, 0xF0000000u } },
            new { Name = "High Bits + Low Bits", KeysSize = 6, Blocks = new[] { 0x00000000u, 0x00000001u, 0x00000002u, 0x00000003u, 0x00000004u, 0x00000005u, 0x00000006u, 0x00000007u, 0x80000000u, 0x40000000u, 0xC0000000u, 0x20000000u, 0xA0000000u, 0x60000000u, 0xE0000000u } }
        };

        private static readonly dynamic[] s_ParametersKST =
        {
            new { Prefix = "-", CoreBytes = 4, Suffix = "+" },
            new { Prefix = "-+", CoreBytes = 4, Suffix = "" },
            new { Prefix = "", CoreBytes = 4, Suffix = "-+" }
        };

        private static readonly dynamic[] s_ParametersSKT =
        {
            new { Bits = 6, KeysBytes = 4 },
            new { Bits = 5, KeysBytes = 8 },
            new { Bits = 4, KeysBytes = 16 },
            new { Bits = 3, KeysBytes = 32 },
            new { Bits = 2, KeysBytes = 64 }
        };
        #endregion

        #region Methods
        private static void PrintResult(AnalysisResult result, Boolean paddedResult, Int32 indentation)
        {
            String dataPadding = new String(' ', indentation);
            String resultPadding = paddedResult ? " " : String.Empty;

            Double expectedCollisions = result.ExpectedCollisions;
            Double observedCollisions = result.ObservedCollisions;

            Console.WriteLine($"{resultPadding}{(result.Outcome ? "PASSED" : "FAILED")}");
            Console.WriteLine($"{dataPadding}- Expected Collisions: {expectedCollisions:F0}");
            
            if (observedCollisions.Equals(0.0d))
                Console.WriteLine($"{dataPadding}- Observed Collisions: 0 (0.00%)");
            else if (observedCollisions.Equals(result.HashesCount - 1.0d))
                Console.WriteLine($"{dataPadding}- Observed Collisions: {observedCollisions:F0} (100.00%)");
            else
            {
                if (expectedCollisions.Equals(0.0d))
                    Console.WriteLine($"{dataPadding}- Observed Collisions: {observedCollisions:F0}");
                else
                    Console.WriteLine($"{dataPadding}- Observed Collisions: {observedCollisions:F0} ({(observedCollisions / expectedCollisions):F2}x)");
            }

            if (result.WorstBit >= 0)
                Console.WriteLine($"{dataPadding}- Worst Distribution Bias: {result.WorstBias:F2}% (Bit: {result.WorstBit} | Window: {result.WorstWindow})");
            else
                Console.WriteLine($"{dataPadding}- Worst Distribution Bias: Unbiased");
        }

        private static void CombinationsTestRecursion(List<Byte[]> hashes, Hash hash, UInt32[] key, UInt32[] blocks, Int32 length, Int32 maximumLength)
        {
            if (length == maximumLength)
                return;

            for (Int32 i = 0; i < blocks.Length; ++i)
            {
                key[length] = blocks[i];

                Byte[] keyBytes = new Byte[key.Length * 4];
                Buffer.BlockCopy(key, 0, keyBytes, 0, key.Length * 4);

                hashes.Add(hash.ComputeHash(keyBytes, 0, (length + 1) * 4));

                CombinationsTestRecursion(hashes, hash, key, blocks, length + 1, maximumLength);
            }
        }

        private static void SparseKeysTestRecursion(List<Byte[]> hashes, Hash hash, Byte[] key, Int32 bit, Int32 maximumBit)
        {
            Int32 keyBytes = key.Length;

            for (Int32 i = bit; i < (keyBytes * 8); ++i)
            {
                BitsUtilities.FlipBit(key, 0, keyBytes, i);
                hashes.Add(hash.ComputeHash(key));

                if (maximumBit > 1)
                    SparseKeysTestRecursion(hashes, hash, key, i + 1, maximumBit - 1);

                BitsUtilities.FlipBit(key, 0, keyBytes, i);
            }
        }

        public static void CombinationsTest(HashInfo hashInfo)
        {
            RandomXS r = new RandomXS(0x000000A0u);
            Hash hash = hashInfo.Initializer(r.NextValue());
            Int32 hashBytes = hashInfo.Length / 8;

            Console.WriteLine("[[ COMBINATIONS TEST ]]");

            Boolean resultOverall = true;

            for (Int32 i = 0; i < s_ParametersCT.Length; ++i)
            {
                dynamic p = s_ParametersCT[i];

                Int32 keysSize = p.KeysSize;
                UInt32[] key = new UInt32[keysSize];

                Console.WriteLine($"> Name: \"{p.Name}\"");
                Console.WriteLine($"  Keys Length: {keysSize * 4} Bytes");
                Console.Write("  Result: ");

                List<Byte[]> hashes = new List<Byte[]>();
                CombinationsTestRecursion(hashes, hash, key, p.Blocks, 0, keysSize);

                AnalysisResult result = StatsUtilities.AnalyzeHashes(hashes, hashBytes);
                PrintResult(result, false, 3);

                resultOverall &= result.Outcome;
            }

            Console.WriteLine($"Test Result: {(resultOverall ? "PASSED" : "FAILED")}");
        }

        public static void CyclicKeysTest(HashInfo hashInfo)
        {
            RandomXS r = new RandomXS(0x000000A1u);
            Hash hash = hashInfo.Initializer(r.NextValue());
            Int32 hashBytes = hashInfo.Length / 8;

            Console.WriteLine("[[ CYCLIC KEYS TEST ]]");

            Boolean resultOverall = true;

            for (Int32 i = 0; i < s_ParametersCKT.Length; ++i)
            {
                dynamic p = s_ParametersCKT[i];

                Int32 cycleBytes = hashBytes + p.CycleBytes;
                Byte[] cycle = new Byte[cycleBytes];
                
                Int32 keysBytes = cycleBytes * p.CycleRepetitions;
                Int32 keysCount = p.KeysCount;
                Byte[] key = new Byte[keysBytes];

                Int32 steps = keysCount / 10;

                Console.WriteLine($"> Cycle Length: {cycleBytes} Bytes");
                Console.WriteLine($"  Cycle Repetitions: {p.CycleRepetitions}");
                Console.WriteLine($"  Keys Length: {keysBytes} Bytes");
                Console.WriteLine($"  Keys Count: {keysCount}");
                Console.Write("  Result: ");

                List<Byte[]> hashes = new List<Byte[]>(keysCount);
  
                for (Int32 j = 0; j < keysCount; ++j)
                {
                    if ((j % steps) == 0)
                        Console.Write(".");

                    r.NextBytes(cycle, cycleBytes);

                    for (Int32 y = 0; y < keysBytes; y += cycleBytes)
                        Buffer.BlockCopy(cycle, 0, key, y, cycleBytes);

                    hashes.Add(hash.ComputeHash(key));
                }

                AnalysisResult result = StatsUtilities.AnalyzeHashes(hashes, hashBytes);
                PrintResult(result, true, 3);

                resultOverall &= result.Outcome;
            }

            Console.WriteLine($"Test Result: {(resultOverall ? "PASSED" : "FAILED")}");
        }

        public static void FlippedKeysTest(HashInfo hashInfo)
        {
            RandomXS r = new RandomXS(0x000000A2u);

            Int32 keysBytes = FKT_LENGTHMAX + (FKT_PADDING * 3);
            Byte[] key1 = new Byte[keysBytes];
            Byte[] key2 = new Byte[keysBytes];

            Int32 steps = FKT_REPETITIONS / 10;

            Console.WriteLine("[[ FLIPPED KEYS TEST ]]");
            Console.WriteLine($"Length Span: {FKT_LENGTHMIN}-{FKT_LENGTHMAX} Bytes");
            Console.WriteLine($"Padding: {FKT_PADDING} Bytes");
            Console.WriteLine($"Repetitions: {FKT_REPETITIONS}");
            Console.Write("Test Result: ");

            Int32 count = 0;
            Int32 divergences = 0;
            Int32 collisions = 0;

            for (Int32 i = 0; i < FKT_REPETITIONS; ++i)
            {
                if ((i % steps) == 0)
                    Console.Write(".");

                Hash hash = hashInfo.Initializer(r.NextValue());

                for (Int32 length = FKT_LENGTHMIN; length <= FKT_LENGTHMAX; ++length)
                {
                    for (Int32 offset = FKT_PADDING; offset < (FKT_PADDING * 2); ++offset)
                    {
                        Int32 index = FKT_PADDING + offset;

                        r.NextBytes(key1);
                        r.NextBytes(key2);
                        Buffer.BlockCopy(key1, FKT_PADDING, key2, index, length);

                        Byte[] h = hash.ComputeHash(key1, FKT_PADDING, length);

                        for (Int32 bit = 0; bit < (length * 8); ++bit)
                        {
                            BitsUtilities.FlipBit(key2, index, length, bit);
                            Byte[] hf1 = hash.ComputeHash(key2, index, length);

                            if (NativeMethods.EqualSequences(h, hf1))
                                ++collisions;

                            BitsUtilities.FlipBit(key2, index, length, bit);
                            Byte[] hf2 = hash.ComputeHash(key2, index, length);

                            if (!NativeMethods.EqualSequences(h, hf2))
                                ++divergences;

                            ++count;
                        }
                    }
                }
            }

            Console.WriteLine(((collisions == 0) && (divergences == 0)) ? " PASSED" : " FAILED");
            Console.WriteLine($" - Iterations: {count + 1}");
            Console.WriteLine($" - Collisions: {collisions} ({(((Double)collisions / count) * 100.0d):F2}%)");
            Console.WriteLine($" - Divergences: {divergences} ({(((Double)divergences / count) * 100.0d):F2}%)");
        }

        public static void KeySetsTest(HashInfo hashInfo)
        {
            RandomXS r = new RandomXS(0x000000A3u);
            Int32 hashBytes = hashInfo.Length / 8;

            Byte[] charactersBytes = Encoding.ASCII.GetBytes(KST_CHARACTERS);
            Int32 charactersLength = charactersBytes.Length;

            Console.WriteLine("[[ KEY SETS TEST ]]");
            Console.WriteLine($"Characters: \"{KST_CHARACTERS}\"");

            Boolean resultOverall = true;

            for (Int32 i = 0; i < s_ParametersKST.Length; ++i)
            {
                dynamic p = s_ParametersKST[i];

                Byte[] prefix = Encoding.ASCII.GetBytes(p.Prefix);
                Int32 prefixBytes = prefix.Length;

                Int32 coreBytes = p.CoreBytes;

                Byte[] suffix = Encoding.ASCII.GetBytes(p.Suffix);
                Int32 suffixBytes = suffix.Length;

                Int32 keysCount = (Int32)Math.Pow(charactersLength, coreBytes);
                Byte[] key = new Byte[prefixBytes + coreBytes + suffixBytes];
                Buffer.BlockCopy(prefix, 0, key, 0, prefixBytes);
                Buffer.BlockCopy(suffix, 0, key, prefixBytes + coreBytes, suffixBytes);

                Hash hash = hashInfo.Initializer(r.NextValue());

                Int32 steps = keysCount / 10;

                Console.WriteLine($"> Keys Format: \"{p.Prefix}{new String('X', coreBytes)}{p.Suffix}\"");
                Console.WriteLine($"  Keys Length: {key.Length} Bytes");
                Console.WriteLine($"  Keys Count: {keysCount}");
                Console.Write("  Result: ");

                List<Byte[]> hashes = new List<Byte[]>(keysCount);
  
                for (Int32 j = 0; j < keysCount; ++j)
                {
                    if ((j % steps) == 0)
                        Console.Write(".");

                    Int32 t = j;

                    for (Int32 y = 0; y < coreBytes; ++y)
                    {
                        key[prefixBytes + y] = charactersBytes[t % charactersLength];
                        t /= charactersLength;
                    }

                    hashes.Add(hash.ComputeHash(key));
                }

                AnalysisResult result = StatsUtilities.AnalyzeHashes(hashes, hashBytes);
                PrintResult(result, true, 3);

                resultOverall &= result.Outcome;
            }

            Console.WriteLine($"Test Result: {(resultOverall ? "PASSED" : "FAILED")}");
        }

        public static void PaddedKeysTest(HashInfo hashInfo)
        {
            RandomXS r = new RandomXS(0x000000A4u);
            Int32 hashBytes = hashInfo.Length / 8;
            Int32 steps = PKT_REPETITIONS / 10;

            Console.WriteLine("[[ PADDED KEYS TEST ]]");
            Console.WriteLine($"Keys Length: {PKT_KEYSBYTES} Bytes");
            Console.WriteLine($"Repetitions: {PKT_REPETITIONS}");
            Console.Write("Test Result: ");

            Int32 count = 0;
            Int32 collisions = 0;

            for (Int32 i = 0; i < PKT_REPETITIONS; ++i)
            {
                if ((i % steps) == 0)
                    Console.Write(".");

                Hash hash = hashInfo.Initializer(r.NextValue());

                Byte[] key = new Byte[PKT_KEYSBYTES];
                Byte[] h0 = new Byte[hashBytes];

                for (Int32 inc = 1; inc <= 32; inc *= 2)
                {
                    for (Int32 offset = 0; offset <= 224; offset += inc)
                    {
                        Byte[] hi = hash.ComputeHash(key, 0, 32 + offset);

                        if (offset > 0)
                        {
                            if (NativeMethods.EqualSequences(hi, h0))
                                ++collisions;

                            ++count;
                        }

                        Buffer.BlockCopy(hi, 0, h0, 0, hashBytes);
                    }
                }

                r.NextBytes(key, 32);
            }

            Console.WriteLine((collisions == 0) ? " PASSED" : " FAILED");
            Console.WriteLine($" - Total Tests: {count + 1}");
            Console.WriteLine($" - Collisions: {collisions} ({(((Double)collisions / count) * 100.0d):F2}%)");
        }

        public static void SparseKeysTest(HashInfo hashInfo)
        {
            RandomXS r = new RandomXS(0x000000A5u);
            Hash hash = hashInfo.Initializer(r.NextValue());
            Int32 hashBytes = hashInfo.Length / 8;

            Console.WriteLine("[[ SPARSE KEYS TEST ]]");

            Boolean resultOverall = true;

            for (Int32 i = 0; i < s_ParametersSKT.Length; ++i)
            {
                dynamic p = s_ParametersSKT[i];

                Int32 bits = p.Bits;
                Int32 keysBytes = p.KeysBytes;
                Byte[] key = new Byte[keysBytes];

                Console.WriteLine($"> Bits: {bits}");
                Console.WriteLine($"  Keys Length: {keysBytes} Bytes");
                Console.Write("  Result: ");

                List<Byte[]> hashes = new List<Byte[]> { hash.ComputeHash(key) };
                SparseKeysTestRecursion(hashes, hash, key, 0, bits);
                
                AnalysisResult result = StatsUtilities.AnalyzeHashes(hashes, hashBytes);
                PrintResult(result, false, 3);

                resultOverall &= result.Outcome;
            }

            Console.WriteLine($"Test Result: {(resultOverall ? "PASSED" : "FAILED")}");
        }

        public static void TwoBytesTest(HashInfo hashInfo)
        {
            RandomXS r = new RandomXS(0x000000A6u);
            Hash hash = hashInfo.Initializer(r.NextValue());
            Int32 hashBytes = hashInfo.Length / 8;

            Console.WriteLine("[[ TWO BYTES TEST ]]");

            Boolean resultOverall = true;

            for (Int32 length = TBT_LENGTHMIN; length <= TBT_LENGTHMAX; length += TBT_INCREMENT)
            {
                Int32 keysCount = 0;

                for (Int32 i = 2; i <= length; ++i)
                    keysCount += (Int32)StatsUtilities.ChooseK(i, 2);

                keysCount *= (Int32)Math.Pow(255.0d, 2.0d);

                for (Int32 i = 2; i <= length; ++i)
                    keysCount += i * 255;

                Console.WriteLine($"> Keys Length: {length} Bytes");
                Console.WriteLine($"  Keys Count: {keysCount}");
                Console.Write("  Result: ");

                Byte[] key = new Byte[TBT_LENGTHMAX];
                List<Byte[]> hashes = new List<Byte[]>(keysCount);

                for (Int32 keysBytes = 2; keysBytes <= length; ++keysBytes)
                for (Int32 index = 0; index < keysBytes; ++index)
                {
                    for (Int32 value = 1; value <= 255; ++value)
                    {
                        key[index] = (Byte)value;
                        hashes.Add(hash.ComputeHash(key, 0, keysBytes));
                    }

                    key[index] = 0;
                }

                for (Int32 keysBytes = 2; keysBytes <= length; ++keysBytes)
                for (Int32 index1 = 0; index1 < keysBytes - 1; ++index1)
                for (Int32 index2 = index1 + 1; index2 < keysBytes; ++index2)
                {
                    for (Int32 value1 = 1; value1 <= 255; ++value1)
                    {
                        key[index1] = (Byte)value1;

                        for (Int32 value2 = 1; value2 <= 255; ++value2)
                        {
                            key[index2] = (Byte)value2;
                            hashes.Add(hash.ComputeHash(key, 0, keysBytes));
                        }

                        key[index2] = 0;
                    }

                    key[index1] = 0;
                }

                AnalysisResult result = StatsUtilities.AnalyzeHashes(hashes, hashBytes);
                PrintResult(result, false, 3);

                resultOverall &= result.Outcome;
            }

            Console.WriteLine($"Test Result: {(resultOverall ? "PASSED" : "FAILED")}");
        }

        public static void UniformKeysTest(HashInfo hashInfo)
        {
            RandomXS r = new RandomXS(0x000000A7u);
            Int32 hashBytes = hashInfo.Length / 8;

            Int32 steps = UKT_REPETITIONS / 10;

            Console.WriteLine("[[ UNIFORM KEYS TEST ]] ");
            Console.WriteLine($"Keys Length: 2-{UKT_KEYSBYTES} Bytes");
            Console.WriteLine($"Repetitions: {UKT_REPETITIONS}");

            Boolean resultOverall = true;

            for (Int32 i = 0; i < s_ValuesUKT.Length; ++i)
            {
                Byte value = s_ValuesUKT[i];

                Byte[] key = new Byte[UKT_KEYSBYTES];
                NativeMethods.FillArray(key, value);

                Console.WriteLine($"> Value 0x{value:X2}");
                Console.Write("  Result: ");

                Boolean resultCurrent = true;

                for (Int32 j = 0; j < UKT_REPETITIONS; ++j)
                {
                    if ((j % steps) == 0)
                        Console.Write(".");

                    Hash hash = hashInfo.Initializer(r.NextValue());
                    List<Byte[]> hashes = new List<Byte[]>(UKT_KEYSBYTES - 1);

                    for (Int32 y = 2; y <= UKT_KEYSBYTES; ++y)
                        hashes.Add(hash.ComputeHash(key, 0, y));

                    AnalysisResult result = StatsUtilities.AnalyzeHashes(hashes, hashBytes);
                    resultCurrent &= result.Outcome;
                    resultOverall &= result.Outcome;
                }

                Console.WriteLine(resultCurrent ? " PASSED" : " FAILED");
            }

            Console.WriteLine($"Test Result: {(resultOverall ? "PASSED" : "FAILED")}");
        }

        public static void VerificationTest(HashInfo hashInfo)
        {
            Int32 hashBytes = hashInfo.Length / 8;

            Byte[] buffer = new Byte[VT_ITERATIONS];
            Byte[] bufferVerification = new Byte[hashBytes * VT_ITERATIONS];

            for (Int32 i = 0; i < VT_ITERATIONS; ++i)
            {
                buffer[i] = (Byte)i;

                Hash hash = hashInfo.Initializer((UInt32)(VT_ITERATIONS - i));
                Byte[] hi = hash.ComputeHash(buffer, 0, i);

                Buffer.BlockCopy(hi, 0, bufferVerification, i * hashBytes, hashBytes);
            }

            Hash hashVerification = hashInfo.Initializer(0u);
            Byte[] verificationBytes = hashVerification.ComputeHash(bufferVerification);   
            UInt32 verification = (UInt32)((verificationBytes[0] << 0) | (verificationBytes[1] << 8) | (verificationBytes[2] << 16) | (verificationBytes[3] << 24));

            Console.WriteLine("[[ VERIFICATION TEST ]]");
            Console.WriteLine($"Expected Hash: 0x{hashInfo.Verification:X8}");
            Console.WriteLine($"Observed Hash: 0x{verification:X8}");
            Console.WriteLine($"Test Result: {((hashInfo.Verification == verification) ? "PASSED" : "FAILED")}");
        }

        public static void WindowedKeysTest(HashInfo hashInfo)
        {
            RandomXS r = new RandomXS(0x000000A8u);
            Hash hash = hashInfo.Initializer(r.NextValue());
            Int32 hashBits = hashInfo.Length;
            Int32 hashBytes = hashBits / 8;

            Int32 keysLength = hashBits * WKT_KEYSMULTIPLIER;
            Int32 keysCount = 1 << WKT_WINDOWBITS;

            Int32 steps = keysCount / 10;

            Console.WriteLine("[[ WINDOWED KEYS TEST ]]");
            Console.WriteLine($"Keys Length: {keysLength} Bytes ({WKT_KEYSMULTIPLIER} x Hash Bits)");
            Console.WriteLine($"Keys Count: {keysCount}");
            Console.WriteLine($"Window Bits: {WKT_WINDOWBITS}");

            Boolean resultOverall = true;
    
            for (Int32 i = 0; i <= keysLength; ++i)
            {
                Console.WriteLine($"> Bit {i}");
                Console.Write("  Result: ");

                List<Byte[]> hashes = new List<Byte[]>(keysCount);

                for (Int32 j = 0; j < keysCount; ++j)
                {
                    if ((j % steps) == 0)
                        Console.Write(".");

                    Byte[] key = new Byte[keysLength];

                    unsafe
                    {
                        fixed (Byte* pointer = key)
                            *((Int32*)pointer) = j;
                    }

                    BitsUtilities.RotateLeft(key, keysLength, i);
                    hashes.Add(hash.ComputeHash(key));
                }

                AnalysisResult result = StatsUtilities.AnalyzeHashes(hashes, hashBytes);
                PrintResult(result, true, 3);

                resultOverall &= result.Outcome;
            }

            Console.WriteLine($"Test Result: {(resultOverall ? " PASSED" : " FAILED")}");
        }

        public static void WordsTest(HashInfo hashInfo)
        {
            Console.WriteLine("[[ WORDS TEST ]]");

            String directory = null;

            try
            {
                directory = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            }
            catch { }

            if (directory == null)
            {
                Console.WriteLine("Error: the working directory could not be located.");
                return;
            }

            String file = Path.Combine(directory, "Words.txt");

            if (!File.Exists(file))
            {
                Console.WriteLine("Error: the file \"Words.txt\" could not be found within the working directory.");
                return;
            }

            String[] lines;

            try
            {
                lines = File.ReadAllLines(file);
            }
            catch
            {
                Console.WriteLine("Error: an unexpected error occurred while reading the \"Words.txt\" file.");
                return;
            }

            RandomXS r = new RandomXS(0x000000A9u);
            Hash hash = hashInfo.Initializer(r.NextValue());
            Int32 hashBytes = hash.Length / 8;

            Byte filler = Convert.ToByte(WT_FILLER);

            Int32 linesCount = lines.Length;
            Int32 steps = linesCount / 10;

            Console.WriteLine($"File: \"{file}\"");
            Console.WriteLine($"File Words: {linesCount}");
            Console.WriteLine($"Variants: {WT_VARIANTS}");
            Console.WriteLine($"Variants Filler: 0x{filler:X2}");
            Console.Write("Test Result: ");

            List<Byte[]> hashes = new List<Byte[]>(linesCount * WT_VARIANTS);

            for (Int32 i = 0; i < lines.Length; ++i)
            {
                if ((i % steps) == 0)
                    Console.Write(".");

                Byte[] lineBytes = Encoding.UTF8.GetBytes(lines[i]);
                Int32 lineBytesLength = lineBytes.Length;

                Byte[] buffer = new Byte[lineBytes.Length + WT_VARIANTS];
                NativeMethods.FillArray(buffer, filler);
                Buffer.BlockCopy(lineBytes, 0, buffer, 0, lineBytes.Length);

                for (Int32 j = 0; j <= WT_VARIANTS; ++j)
                    hashes.Add(hash.ComputeHash(buffer, 0, lineBytesLength + j));
            }

            AnalysisResult result = StatsUtilities.AnalyzeHashes(hashes, hashBytes);
            PrintResult(result, true, 1);
        }
        #endregion
    }
}