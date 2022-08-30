#region Using Directives
using System;
using System.Collections.Generic;
using System.Linq;

#endregion

namespace FastHashes.Benchmarks
{
    public static class Program
    {
        #region Setup
        private static readonly List<BenchmarkCase> s_BenchmarkCases = new List<BenchmarkCase>
        {
            new BenchmarkCase("DummyHash", seed => new DummyHash()),
            new BenchmarkCase("FarmHash32", seed => new FarmHash32(seed)),
            new BenchmarkCase("FarmHash64", seed => new FarmHash64(seed)),
            new BenchmarkCase("FarmHash128", seed => new FarmHash128(seed)),
            new BenchmarkCase("FastHash32", seed => new FastHash32(seed)),
            new BenchmarkCase("FastHash64", seed => new FastHash64(seed)),
            new BenchmarkCase("FastPositiveHash-V0", seed => new FastPositiveHash(FastPositiveHashVariant.V0, seed)),
            new BenchmarkCase("FastPositiveHash-V1", seed => new FastPositiveHash(FastPositiveHashVariant.V1, seed)),
            new BenchmarkCase("FastPositiveHash-V2", seed => new FastPositiveHash(FastPositiveHashVariant.V2, seed)),
            new BenchmarkCase("HalfSipHash", seed => new HalfSipHash(seed)),
            new BenchmarkCase("HighwayHash64", seed => new HighwayHash64(seed)),
            new BenchmarkCase("HighwayHash128", seed => new HighwayHash128(seed)),
            new BenchmarkCase("HighwayHash256", seed => new HighwayHash256(seed)),
            new BenchmarkCase("MetroHash64-V1", seed => new MetroHash64(MetroHashVariant.V1, seed)),
            new BenchmarkCase("MetroHash64-V2", seed => new MetroHash64(MetroHashVariant.V2, seed)),
            new BenchmarkCase("MetroHash128-V1", seed => new MetroHash128(MetroHashVariant.V1, seed)),
            new BenchmarkCase("MetroHash128-V2", seed => new MetroHash128(MetroHashVariant.V2, seed)),
            new BenchmarkCase("MurmurHash32", seed => new MurmurHash32(seed)),
            new BenchmarkCase("MurmurHash64-x86", seed => new MurmurHash64(MurmurHashEngine.x86, seed)),
            new BenchmarkCase("MurmurHash64-x64", seed => new MurmurHash64(MurmurHashEngine.x64, seed)),
            new BenchmarkCase("MurmurHash128-x86", seed => new MurmurHash128(MurmurHashEngine.x86, seed)),
            new BenchmarkCase("MurmurHash128-x64", seed => new MurmurHash128(MurmurHashEngine.x64, seed)),
            new BenchmarkCase("MumHash", seed => new MumHash(seed)),
            new BenchmarkCase("SipHash-13", seed => new SipHash(SipHashVariant.V13, seed)),
            new BenchmarkCase("SipHash-24", seed => new SipHash(SipHashVariant.V24, seed)),
            new BenchmarkCase("SpookyHash32", seed => new SpookyHash32(seed)),
            new BenchmarkCase("SpookyHash64", seed => new SpookyHash64(seed)),
            new BenchmarkCase("SpookyHash128", seed => new SpookyHash128(seed)),
            new BenchmarkCase("xxHash32", seed => new XxHash32(seed)),
            new BenchmarkCase("xxHash64", seed => new XxHash64(seed))
        };

        private const Int32 BULK_KEYS_LENGTH = 256 * 1024;
        private const Int32 BULK_REPETITIONS = 5000;
        private const Int32 CLOCK_MAXIMUM_IDLE_TIME = 10;
        private const Int32 WARMUP_ITERATIONS = 3;

        private static readonly List<ChunkParameter> s_ChunkParameters = new List<ChunkParameter>
        {
            new ChunkParameter(i => i + 1, 32, 200000),
            new ChunkParameter(i => i + 2, 64, 100000),
            new ChunkParameter(i => i + 4, 128, 50000),
            new ChunkParameter(i => i + 8, 256, 25000),
            new ChunkParameter(i => i * 2, 65536, 12500)
        };
        #endregion

        #region Entry Point
        public static void Main()
        {
            if (!Clock.IsHighResolution)
            {
                Console.WriteLine("The clock doesn't support high resolution measurements.");
                Environment.Exit(1);
            }

            List<BenchmarkResult> results = new List<BenchmarkResult>(s_BenchmarkCases.Count);

            using (new AffinityOptimizer())
            {
                for (Int32 i = 0; i < s_BenchmarkCases.Count; ++i)
                {
                    BenchmarkCase benchmarkCase = s_BenchmarkCases[i];
                    String hashName = benchmarkCase.HashName;

                    String title = $"# HASH: {hashName} #";
                    String frame = new String('#', title.Length);

                    Console.WriteLine(frame);
                    Console.WriteLine(title);
                    Console.WriteLine(frame);

                    Console.WriteLine();
                    (Double bulkAverageSpeed, String bulkAverageSpeedFormatted) = BulkSpeedTest(benchmarkCase.HashInitializer);

                    Console.WriteLine();
                    (Double chunkAverageSpeed, String chunkAverageSpeedFormatted) = ChunksSpeedTest(benchmarkCase.HashInitializer);

                    results.Add(new BenchmarkResult(hashName, bulkAverageSpeed, bulkAverageSpeedFormatted, chunkAverageSpeed, chunkAverageSpeedFormatted));

                    Console.WriteLine();
                    Console.WriteLine();
                }
            }

            results = results
                .OrderBy(x => x.BulkAverageSpeed)
                .ThenBy(x => x.HashName)
                .ToList();

            Int32 rankPadding = results.Count.ToString().Length;
            Int32 hashNamePadding = 0;
            Int32 bulkAverageSpeedPadding = 0;
            Int32 chunkAverageSpeedPadding = 0;

            foreach (BenchmarkResult result in results)
            {
                Int32 hashNameLength = result.HashName.Length;

                if (hashNameLength > hashNamePadding)
                    hashNamePadding = hashNameLength;

                Int32 bulkAverageSpeedLength = result.BulkAverageSpeedFormatted.Length;

                if (bulkAverageSpeedLength > bulkAverageSpeedPadding)
                    bulkAverageSpeedPadding = bulkAverageSpeedLength;

                Int32 chunkAverageSpeedLength = result.ChunkAverageSpeedFormatted.Length;

                if (chunkAverageSpeedLength > chunkAverageSpeedPadding)
                    chunkAverageSpeedPadding = chunkAverageSpeedLength;
            }

            Console.WriteLine("###########");
            Console.WriteLine("# SUMMARY #");
            Console.WriteLine("###########");
            Console.WriteLine();

            for (Int32 i = 0; i < results.Count; ++i)
            {
                BenchmarkResult result = results[i];
                String rank = (i + 1).ToString().PadLeft(rankPadding);
                String hashName = result.HashName.PadRight(hashNamePadding);
                String bulkAverageSpeed = result.BulkAverageSpeedFormatted.PadRight(bulkAverageSpeedPadding);
                String chunkAverageSpeed = result.ChunkAverageSpeedFormatted.PadRight(chunkAverageSpeedPadding);

                Console.WriteLine($"{rank}) {hashName} {bulkAverageSpeed} {chunkAverageSpeed}");
            }

            Environment.Exit(0);
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

                        using (Clock clock = new Clock(CLOCK_MAXIMUM_IDLE_TIME))
                        {
                            DateTime start = clock.GetTime();
                            hash.ComputeHash(key, offset, length);
                            DateTime end = clock.GetTime();

                            Double ms = (end - start).TotalMilliseconds;
                            Double bps = (length * 1000.0d) / ms;

                            if (bps >= 0.0d)
                                results.Add(bps);
                        }
                    }

                    Double mean = MathUtilities.Mean(results);
                    Double threshold = 2.0d * MathUtilities.StandardDeviation(results, mean);
 
                    for (Int32 i = results.Count - 1; i >= 0; --i)
                    {
                        if (Math.Abs(results[i] - mean) > threshold)
                            results.RemoveAt(i);
                    }
 
                    return MathUtilities.Mean(results);
                }
            }
        }

        private static (Double, String) BulkSpeedTest(Func<UInt32,Hash> hashInitializer)
        {
            Console.WriteLine("[BULK SPEED TEST]");
            Console.WriteLine($"Keys Length: {BULK_KEYS_LENGTH} Bytes");
            Console.WriteLine($"Repetitions: {BULK_REPETITIONS}");

            using (new SpeedOptimizer())
            {
                for (Int32 i = 0; i < WARMUP_ITERATIONS; ++i)
                    GetAverageSpeed(hashInitializer, BULK_KEYS_LENGTH, BULK_REPETITIONS, 0);

                Double[] speed = new Double[8];

                for (Int32 align = 0; align < 8; ++align)
                {
                    speed[align] = GetAverageSpeed(hashInitializer, BULK_KEYS_LENGTH, BULK_REPETITIONS, align);
                    Console.WriteLine($" - Average Speed Alignment {align}: {Utilities.FormatSpeed(speed[align])}");
                }

                Double averageSpeed = MathUtilities.Mean(speed);
                String averageSpeedFormatted = Utilities.FormatSpeed(averageSpeed);

                Console.WriteLine($" - Average Speed Overall: {averageSpeedFormatted}");

                return (averageSpeed, averageSpeedFormatted);
            }
        }

        private static (Double, String) ChunksSpeedTest(Func<UInt32,Hash> hashInitializer)
        {
            Console.WriteLine("[CHUNKS SPEED TEST]");
            Console.WriteLine($"Keys Length Span: 0-{s_ChunkParameters.Max(x => x.KeySize)} Bytes");
            Console.WriteLine($"Repetitions Span: {s_ChunkParameters.Min(x => x.Repetitions)}-{s_ChunkParameters.Max(x => x.Repetitions)}");

            using (new SpeedOptimizer())
            {
                for (Int32 i = 0; i < WARMUP_ITERATIONS; ++i)
                    GetAverageSpeed(hashInitializer, BULK_KEYS_LENGTH, BULK_REPETITIONS, 0);

                Double totalSpeed = 0.0d;
                Int32 totalCount = 0;
                Int32 offset = 0;

                foreach (ChunkParameter chunkParameter in s_ChunkParameters)
                {
                    Func<Int32,Int32> increment = chunkParameter.Increment;
                    Int32 keySize = chunkParameter.KeySize;
                    Int32 repetitions = chunkParameter.Repetitions;

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
                }

                Double averageSpeed = totalSpeed / totalCount;
                String averageSpeedFormatted = Utilities.FormatSpeed(averageSpeed);

                Console.WriteLine($" - Average Speed Overall: {averageSpeedFormatted}");

                return (averageSpeed, averageSpeedFormatted);
            }
        }
        #endregion
    }
}