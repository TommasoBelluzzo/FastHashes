#region Using Directives
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime;
using System.Threading;
#endregion

namespace FastHashes.Benchmarks
{
    public static class Program
    {
        #region Constants
        private const Int32 BST_KEYSLENGTH = 256 * 1024;
        private const Int32 BST_REPETITIONS = 5000;
        private const Int32 WARMUP_ITERATIONS = 3;
        #endregion

        #region Members
        private static readonly Double s_Frequency = NativeMethods.GetFrequency();

        private static readonly List<(Func<Int32,Int32>,Int32,Int32)> s_Parameters = new List<(Func<Int32,Int32>,Int32,Int32)>
        {
            (i => i + 1, 32, 200000),
            (i => i + 2, 64, 100000),
            (i => i + 4, 128, 50000),
            (i => i + 8, 256, 25000),
            (i => i * 2, 65536, 12500)
        };

        private static readonly List<(String,Func<UInt32,Hash>)> s_HashInitializers = new List<(String,Func<UInt32,Hash>)>
        {
            ("DummyHash",  x => new DummyHash()),
            ("FarmHash32",  x => new FarmHash32(x)),
            ("FarmHash64",  x => new FarmHash64(x)),
            ("FarmHash128",  x => new FarmHash128(x)),
            ("FastHash32",  x => new FastHash32(x)),
            ("FastHash64",  x => new FastHash64(x)),
            ("FastPositiveHash-V0",  x => new FastPositiveHash(FastPositiveHashVariant.V0, x)),
            ("FastPositiveHash-V1",  x => new FastPositiveHash(FastPositiveHashVariant.V1, x)),
            ("FastPositiveHash-V2",  x => new FastPositiveHash(FastPositiveHashVariant.V2, x)),
            ("HalfSipHash",  x => new HalfSipHash(x)),
            ("HighwayHash64",  x => new HighwayHash64(x)),
            ("HighwayHash128",  x => new HighwayHash128(x)),
            ("HighwayHash256",  x => new HighwayHash256(x)),
            ("MetroHash64-V1",  x => new MetroHash64(MetroHashVariant.V1, x)),
            ("MetroHash64-V2",  x => new MetroHash64(MetroHashVariant.V2, x)),
            ("MetroHash128-V1",  x => new MetroHash128(MetroHashVariant.V1, x)),
            ("MetroHash128-V2",  x => new MetroHash128(MetroHashVariant.V2, x)),
            ("MurmurHash32",  x => new MurmurHash32(x)),
            ("MurmurHash64-x86",  x => new MurmurHash64(MurmurHashEngine.x86, x)),
            ("MurmurHash64-x64",  x => new MurmurHash64(MurmurHashEngine.x64, x)),
            ("MurmurHash128-x86",  x => new MurmurHash128(MurmurHashEngine.x86, x)),
            ("MurmurHash128-x64",  x => new MurmurHash128(MurmurHashEngine.x64, x)),
            ("MumHash",  x => new MumHash(x)),
            ("SipHash-13",  x => new SipHash(SipHashVariant.V13, x)),
            ("SipHash-24",  x => new SipHash(SipHashVariant.V24, x)),
            ("SpookyHash32",  x => new SpookyHash32(x)),
            ("SpookyHash64",  x => new SpookyHash64(x)),
            ("SpookyHash128",  x => new SpookyHash128(x)),
            ("xxHash32",  x => new xxHash32(x)),
            ("xxHash64",  x => new xxHash64(x))
        };
        #endregion

        #region Entry Point
        public static void Main()
        {
            Process process = Process.GetCurrentProcess();

            IntPtr affinityNew = (IntPtr)(1 << (Environment.ProcessorCount - 1));
            IntPtr affinityOld = process.ProcessorAffinity;

            process.ProcessorAffinity = affinityNew;

            Thread.BeginThreadAffinity();
            ProcessThread thread = NativeMethods.GetCurrentThread();

            if (thread != null)
                thread.ProcessorAffinity = affinityNew;

            for (Int32 i = 0; i < s_HashInitializers.Count; ++i)
            {
                var hashInitializer = s_HashInitializers[i];

                String title = $"# HASH: {hashInitializer.Item1} #";
                String frame = new String('#', title.Length);

                Console.WriteLine(frame);
                Console.WriteLine(title);
                Console.WriteLine(frame);

                BulkSpeedTest(hashInitializer.Item2);
                ChunksSpeedTest(hashInitializer.Item2);

                if (i != (s_HashInitializers.Count - 1))
                    Console.WriteLine();
            }

            process.ProcessorAffinity = affinityOld;

            if (thread != null)
                thread.ProcessorAffinity = affinityOld;

            Thread.EndThreadAffinity();
        }
        #endregion

        #region Methods
        private static Double GetAverageSpeed(Func<UInt32,Hash> hashInitializer, Int32 length, Int32 repetitions, Int32 align)
        {
            RandomXorShift r = new RandomXorShift();
            Byte[] key = new Byte[length + 512];

            unsafe
            {
                fixed (Byte* pin = key)
                {
                    UInt64 pinValue = (UInt64)pin;
                    UInt64 alignValue = ((pinValue + 255ul) & 0xFFFFFFFFFFFFFF00ul) + (UInt64)align;
                    Int32 offset = (Int32)(alignValue - pinValue);

                    List<Double> results = new List<Double>(repetitions);

                    for (Int32 i = 0; i < repetitions; ++i)
                    {
                        r.NextBytes(key, offset, length);

                        Hash hash = hashInitializer((UInt32)i);

                        Double start = NativeMethods.GetTime();
                        hash.ComputeHash(key, offset, length);
                        Double end = NativeMethods.GetTime();

                        Double ms = ((end - start + 1.0d) * 1000.0d) / s_Frequency;
                        Double bps = (length * 1000.0d) / ms;

                        if (bps >= 0.0d)
                            results.Add(bps);
                    }

                    Double mean = Utilities.Mean(results);
                    Double threshold = 2.0d * Utilities.StandardDeviation(results, mean);
 
                    for (Int32 i = results.Count - 1; i >= 0; --i)
                    {
                        if (Math.Abs(results[i] - mean) > threshold)
                            results.RemoveAt(i);
                    }
 
                    return Utilities.Mean(results);
                }
            }
        }

        private static IDisposable SpeedTestOptimizer()
        {
            Process process = Process.GetCurrentProcess();
            ProcessPriorityClass lastPriorityClass = process.PriorityClass;
            GCLatencyMode lastLatencyMode = GCSettings.LatencyMode;

            return new DisposableDelegate
            (
                () =>
                {
                    process.PriorityClass = ProcessPriorityClass.RealTime;
                    GCSettings.LatencyMode = GCLatencyMode.SustainedLowLatency;
                },
                () =>
                {
                    GCSettings.LatencyMode = lastLatencyMode;
                    process.PriorityClass = lastPriorityClass;
                }
            );
        }

        private static void BulkSpeedTest(Func<UInt32,Hash> hashInitializer)
        {
            Console.WriteLine("[BULK SPEED TEST]");
            Console.WriteLine($"Frequency: {s_Frequency / 1e6d:F2} MHz");
            Console.WriteLine($"Keys Length: {BST_KEYSLENGTH} Bytes");
            Console.WriteLine($"Repetitions: {BST_REPETITIONS}");

            using (SpeedTestOptimizer())
            {
                for (Int32 i = 0; i < WARMUP_ITERATIONS; ++i)
                    GetAverageSpeed(hashInitializer, BST_KEYSLENGTH, BST_REPETITIONS, 0);

                Double[] speed = new Double[8];

                for (Int32 align = 0; align < 8; ++align)
                {
                    speed[align] = GetAverageSpeed(hashInitializer, BST_KEYSLENGTH, BST_REPETITIONS, align);
                    Console.WriteLine($" - Alignment {align}: {Utilities.FormatSpeed(speed[align])}");
                }

                Console.WriteLine($" - Average Speed: {Utilities.FormatSpeed(Utilities.Mean(speed))}");
            }
        }

        private static void ChunksSpeedTest(Func<UInt32,Hash> hashInitializer)
        {
            Console.WriteLine("[CHUNKS SPEED TEST]");
            Console.WriteLine($"Frequency: {s_Frequency / 1e6d:F2} MHz");
            Console.WriteLine("Keys Length Span: 0-65535 Bytes");

            using (SpeedTestOptimizer())
            {
                for (Int32 i = 0; i < WARMUP_ITERATIONS; ++i)
                    GetAverageSpeed(hashInitializer, BST_KEYSLENGTH, BST_REPETITIONS, 0);

                Double totalSpeed = 0.0d;
                Int32 totalCount = 0;
                Int32 offset = 0;

                for (Int32 i = 0; i < s_Parameters.Count; ++i)
                {
                    var p = s_Parameters[i];
                    Func<Int32,Int32> increment = p.Item1;
                    Int32 keySize = p.Item2;
                    Int32 repetitions = p.Item3;

                    Double speed = 0.0d;
                    Int32 count = 0;
                    Int32 offsetStart = offset;

                    while (offset < keySize)
                    {
                        speed += GetAverageSpeed(hashInitializer, offset, repetitions, 0);
                        ++count;

                        offset = increment(offset);
                    }

                    totalSpeed += speed;
                    totalCount += count;
                    offset = keySize;

                    Console.WriteLine($" - Average Speed {offsetStart}-{offset - 1} Bytes: {Utilities.FormatSpeed(speed / count)}");
                    Console.WriteLine($" - Average Speed Overall: {Utilities.FormatSpeed(totalSpeed / totalCount)}");
                }
            }
        }
        #endregion
    }
}