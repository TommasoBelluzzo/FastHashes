#region Using Directives
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
#endregion

namespace FastHashes.Tests
{
    public static class Program
    {
        #region Members
        private static readonly HashInfo[] s_HashInfos =
        {
            new HashInfo((x) => new DummyHash32(), 0x00000000u),
            new HashInfo((x) => new DummyHash64(), 0x00000000u),
            new HashInfo((x) => new DummyHash128(), 0x00000000u),
            new HashInfo((x) => new DummyHash256(), 0x00000000u),
            new HashInfo((x) => new FarmHash32(x), 0x0DC9AF39u),
            new HashInfo((x) => new FarmHash64(x), 0xEBC4A679u),
            new HashInfo((x) => new FarmHash128(x), 0xA93EBF71u),
            new HashInfo((x) => new FastHash32(x), 0xE9481AFCu),
            new HashInfo((x) => new FastHash64(x), 0xA16231A7u),
            new HashInfo((x) => new HighwayHash64(x), 0xCD809D2Du),
            new HashInfo((x) => new HighwayHash128(x), 0x7C86214Cu),
            new HashInfo((x) => new HighwayHash256(x), 0xF48F6052u),
            new HashInfo((x) => new MetroHash64(MetroHashVariant.V1, x), 0xEE88F7D2u),
            new HashInfo((x) => new MetroHash64(MetroHashVariant.V2, x), 0xE1FC7C6Eu),
            new HashInfo((x) => new MetroHash128(MetroHashVariant.V1, x), 0x20E8A1D7u),
            new HashInfo((x) => new MetroHash128(MetroHashVariant.V2, x), 0x5437C684u),
            new HashInfo((x) => new MurmurHash32(x), 0xB0F57EE3u),
            new HashInfo((x) => new MurmurHash64(MurmurHashEngine.X86, x), 0x33E834ECu),
            new HashInfo((x) => new MurmurHash64(MurmurHashEngine.X64, x), 0x443D8C6Du),
            new HashInfo((x) => new MurmurHash128(MurmurHashEngine.X86, x), 0xB3ECE62Au),
            new HashInfo((x) => new MurmurHash128(MurmurHashEngine.X64, x), 0x6384BA69u),
            new HashInfo((x) => new MumHash(x), 0xA973C6C0u),
            new HashInfo((x) => new HalfSipHash(x), 0xDA2A194Cu),
            new HashInfo((x) => new SipHash(SipHashVariant.V13, x), 0xF694F5B2u),
            new HashInfo((x) => new SipHash(SipHashVariant.V24, x), 0xBC31DC92u),
            new HashInfo((x) => new SpookyHash32(x), 0x3F798BBBu),
            new HashInfo((x) => new SpookyHash64(x), 0xA7F955F1u),
            new HashInfo((x) => new SpookyHash128(x), 0x8D263080u),
            new HashInfo((x) => new T1HA(T1HAVariant.V0, x), 0x7F7D7B29u),
            new HashInfo((x) => new T1HA(T1HAVariant.V1, x), 0xD6836381u),
            new HashInfo((x) => new T1HA(T1HAVariant.V2, x), 0x8F16C948u),
            new HashInfo((x) => new xxHash32(x), 0xBA88B743u),
            new HashInfo((x) => new xxHash64(x), 0x024B7CF4u)
        };
        #endregion

        #region Entry Point
        public static void Main(String[] args)
        {
            if ((args == null) || (args.Length == 0) || ((args.Length == 1) && String.Equals(args[0], "-help", StringComparison.Ordinal)))
            {
                CommandLineUtilities.PrintHelp(s_HashInfos);
                return;
            }

            Dictionary<String,String[]> arguments = CommandLineUtilities.ParseArguments(args);

            if ((arguments.Count != 2) || !arguments.ContainsKey("hashes") || !arguments.ContainsKey("tests"))
            {
                Console.WriteLine("ERROR: malformed or unrecognized command.");
                return;
            }

            String result = CommandLineUtilities.TryGetHashes(arguments, s_HashInfos, out String[] hashes);

            if (!String.IsNullOrEmpty(result))
            {
                Console.WriteLine(result);
                return;
            }

            result = CommandLineUtilities.TryGetTests(arguments, out Boolean qualityTests, out Boolean speedTests, out Boolean validationTests);

            if (!String.IsNullOrEmpty(result))
            {
                Console.WriteLine(result);
                return;
            }

            RunTests(hashes.ToArray(), validationTests, qualityTests, speedTests);
        }
        #endregion

        #region Methods
        private static void RunTests(String[] hashes, Boolean validationTests, Boolean qualityTests, Boolean speedTests)
        {
            Process process = Process.GetCurrentProcess();

            IntPtr affinityNew = (IntPtr)(1 << (Environment.ProcessorCount - 1));
            IntPtr affinityOld = process.ProcessorAffinity;

            process.ProcessorAffinity = affinityNew;

            Thread.BeginThreadAffinity();
            ProcessThread thread = NativeMethods.GetCurrentThread();

            if (thread != null)
                thread.ProcessorAffinity = affinityNew;

            for (Int32 i = 0; i < s_HashInfos.Length; ++i)
            {
                HashInfo hashInfo = s_HashInfos[i];
                String hashInfoName = hashInfo.Name;

                if (!hashes.Contains(hashInfoName))
                    continue;

                String title = $"# HASH: {hashInfoName} ({hashInfo.Length} Bits) #";
                String frame = new String('#', title.Length);

                Console.WriteLine(frame);
                Console.WriteLine(title);
                Console.WriteLine(frame);

                if (validationTests)
                {
                    Console.WriteLine();
                    ValidationTests.VerificationTest(hashInfo);
                
                    Console.WriteLine();
                    ValidationTests.CombinationsTest(hashInfo);
                
                    Console.WriteLine();
                    ValidationTests.CyclicKeysTest(hashInfo);
                
                    Console.WriteLine();
                    ValidationTests.FlippedKeysTest(hashInfo);
                
                    Console.WriteLine();
                    ValidationTests.KeySetsTest(hashInfo);
                
                    Console.WriteLine();
                    ValidationTests.PaddedKeysTest(hashInfo);
                  
                    Console.WriteLine();
                    ValidationTests.SparseKeysTest(hashInfo);
                  
                    Console.WriteLine();
                    ValidationTests.TwoBytesTest(hashInfo);
                
                    Console.WriteLine();
                    ValidationTests.UniformKeysTest(hashInfo);
                
                    Console.WriteLine();
                    ValidationTests.WindowedKeysTest(hashInfo);
                
                    Console.WriteLine();
                    ValidationTests.WordsTest(hashInfo);
                }

                if (qualityTests)
                {
                    Console.WriteLine();
                    QualityTests.AvalancheTest(hashInfo);
                
                    Console.WriteLine();
                    QualityTests.BitIndependenceCriterionTest(hashInfo);
                
                    Console.WriteLine();
                    QualityTests.DifferentialTest(hashInfo);
                }

                if (speedTests)
                {
                    Console.WriteLine();
                    SpeedTests.BulkSpeedTest(hashInfo);
                
                    Console.WriteLine();
                    SpeedTests.ChunksSpeedTest(hashInfo);
                }

                Int32 hashInfoIndex = Array.IndexOf(hashes, hashInfoName);

                if (hashInfoIndex != (hashes.Length - 1))
                    Console.WriteLine();
            }

            process.ProcessorAffinity = affinityOld;

            if (thread != null)
                thread.ProcessorAffinity = affinityOld;

            Thread.EndThreadAffinity();
        }
        #endregion
    }
}