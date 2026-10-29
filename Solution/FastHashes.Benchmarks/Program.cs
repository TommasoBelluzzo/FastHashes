#region Using Directives
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
#endregion

namespace FastHashes.Benchmarks
{
    public static class Program
    {
        #region Setup
        private static readonly List<(String HashName, Func<UInt64,Hash> HashInitializer)> s_BenchmarkCases =
        [
            ("DummyHash", seed => new DummyHash()),
            ("FarmHash32", seed => new FarmHash32(checked((UInt32)seed))),
            ("FarmHash64", seed => new FarmHash64(seed)),
            ("FarmHash128", seed => new FarmHash128(seed)),
            ("FastHash32", seed => new FastHash32(seed)),
            ("FastHash64", seed => new FastHash64(seed)),
            ("FastPositiveHash-V0", seed => new FastPositiveHash(FastPositiveHashVariant.V0, seed)),
            ("FastPositiveHash-V1", seed => new FastPositiveHash(FastPositiveHashVariant.V1, seed)),
            ("FastPositiveHash-V2", seed => new FastPositiveHash(FastPositiveHashVariant.V2, seed)),
            ("HalfSipHash", seed => new HalfSipHash(seed)),
            ("HighwayHash64", seed => new HighwayHash64(seed)),
            ("HighwayHash128", seed => new HighwayHash128(seed)),
            ("HighwayHash256", seed => new HighwayHash256(seed)),
            ("KomiHash", seed => new KomiHash(seed)),
            ("MetroHash64-V1", seed => new MetroHash64(MetroHashVariant.V1, checked((UInt32)seed))),
            ("MetroHash64-V2", seed => new MetroHash64(MetroHashVariant.V2, checked((UInt32)seed))),
            ("MetroHash128-V1", seed => new MetroHash128(MetroHashVariant.V1, checked((UInt32)seed))),
            ("MetroHash128-V2", seed => new MetroHash128(MetroHashVariant.V2, checked((UInt32)seed))),
            ("MirHash", seed => new MirHash(seed)),
            ("MurmurHash32", seed => new MurmurHash32(checked((UInt32)seed))),
            ("MurmurHash64-X86", seed => new MurmurHash64(MurmurHashEngine.x86, checked((UInt32)seed))),
            ("MurmurHash64-X64", seed => new MurmurHash64(MurmurHashEngine.x64, checked((UInt32)seed))),
            ("MurmurHash128-X86", seed => new MurmurHash128(MurmurHashEngine.x86, checked((UInt32)seed))),
            ("MurmurHash128-X64", seed => new MurmurHash128(MurmurHashEngine.x64, checked((UInt32)seed))),
            ("MumHash", seed => new MumHash(checked((UInt32)seed))),
            ("Mx3Hash", seed => new Mx3Hash(seed)),
            ("PengyHash", seed => new PengyHash(checked((UInt32)seed))),
            ("SipHash-13", seed => new SipHash(SipHashVariant.V13, seed)),
            ("SipHash-24", seed => new SipHash(SipHashVariant.V24, seed)),
            ("SpookyHash32", seed => new SpookyHash32(seed, seed)),
            ("SpookyHash64", seed => new SpookyHash64(seed)),
            ("SpookyHash128", seed => new SpookyHash128(seed)),
            ("WyHash32", seed => new WyHash32(checked((UInt32)seed))),
            ("WyHash64", seed => new WyHash64(seed)),
            ("xxHash32", seed => new XxHash32(checked((UInt32)seed))),
            ("xxHash64", seed => new XxHash64(seed))
        ];

        private const Int32 BATCH_MAXIMUM_OPERATIONS = 8192;
        private const Int32 BATCH_MAXIMUM_BYTES = 8 * 1024 * 1024;
        private const Int32 BULK_KEYS_LENGTH = 256 * 1024;
        private const Int32 BULK_REPETITIONS = 5000;
        private const Int32 WARMUP_REPETITIONS = 128;

        private static readonly List<(Func<Int32,Int32> Increment, Int32 KeySize, Int32 Repetitions)> s_ChunkParameters =
        [
            (i => i + 1, 32, 200000),
            (i => i + 2, 64, 100000),
            (i => i + 4, 128, 50000),
            (i => i + 8, 256, 25000),
            (i => i * 2, 65536, 12500)
        ];
        #endregion

        #region Entry Point
        public static void Main()
        {
            Console.WriteLine($"Timestamp: {DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss}");
            Console.WriteLine($"OS: {RuntimeInformation.OSDescription}");
            Console.WriteLine($"Runtime: {RuntimeInformation.FrameworkDescription}");
            Console.WriteLine();
            Console.WriteLine();

            List<(String HashName, Double BulkMedianSpeed, String BulkMedianSpeedFormatted, Double ChunkMedianSpeed, String ChunkMedianSpeedFormatted)> results = new(s_BenchmarkCases.Count);

            for (Int32 i = 0; i < s_BenchmarkCases.Count; ++i)
            {
                (String hashName, Func<UInt64,Hash> hashInitializer) = s_BenchmarkCases[i];

                String title = $"# HASH: {hashName} #";
                String frame = new String('#', title.Length);

                Console.WriteLine(frame);
                Console.WriteLine(title);
                Console.WriteLine(frame);

                Warmup(hashInitializer);

                Console.WriteLine();
                Console.WriteLine("[BULK SPEED TEST]");
                Console.WriteLine($"Keys Length: {BULK_KEYS_LENGTH} Bytes");
                Console.WriteLine($"Repetitions: {BULK_REPETITIONS}");

                (Double[] bulkAlignSpeeds, Double bulkMedianSpeed, String bulkMedianSpeedFormatted) = BulkSpeedTest(hashInitializer);

                for (Int32 j = 0; j < bulkAlignSpeeds.Length; ++j)
                    Console.WriteLine($" - Median Speed Alignment {j}: {Utilities.FormatSpeed(bulkAlignSpeeds[j])}");

                Console.WriteLine($" - Median Speed Overall: {bulkMedianSpeedFormatted}");

                Console.WriteLine();
                Console.WriteLine("[CHUNKS SPEED TEST]");
                Console.WriteLine($"Keys Length Span: 0-{s_ChunkParameters.Max(x => x.KeySize)} Bytes");
                Console.WriteLine($"Repetitions Span: {s_ChunkParameters.Min(x => x.Repetitions)}-{s_ChunkParameters.Max(x => x.Repetitions)}");

                (List<(Int32,Int32,Double)> chunkSpeeds, Double chunkMedianSpeed, String chunkMedianSpeedFormatted) = ChunksSpeedTest(hashInitializer);

                foreach ((Int32 chunkOffsetStart, Int32 chunkOffsetEnd, Double chunkSpeed) in chunkSpeeds)
                    Console.WriteLine($" - Median Speed {chunkOffsetStart}-{chunkOffsetEnd} Bytes: {Utilities.FormatSpeed(chunkSpeed)}");

                Console.WriteLine($" - Median Speed Overall: {chunkMedianSpeedFormatted}");

                Console.WriteLine();
                Console.WriteLine();

                results.Add((hashName, bulkMedianSpeed, bulkMedianSpeedFormatted, chunkMedianSpeed, chunkMedianSpeedFormatted));
            }

            results = [.. results
                .OrderByDescending(x => x.BulkMedianSpeed)
                .ThenBy(x => x.HashName)];

            Int32 rankPadding = Math.Max(4, results.Count.ToString().Length);
            Int32 hashNamePadding = 4;
            Int32 bulkMedianSpeedPadding = 4;
            Int32 chunkMedianSpeedPadding = 5;

            foreach ((String HashName, _, String BulkMedianSpeedFormatted, _, String ChunkMedianSpeedFormatted) in results)
            {
                Int32 hashNameLength = HashName.Length;

                if (hashNameLength > hashNamePadding)
                    hashNamePadding = hashNameLength;

                Int32 bulkMedianSpeedLength = BulkMedianSpeedFormatted.Length;

                if (bulkMedianSpeedLength > bulkMedianSpeedPadding)
                    bulkMedianSpeedPadding = bulkMedianSpeedLength;

                Int32 chunkMedianSpeedLength = ChunkMedianSpeedFormatted.Length;

                if (chunkMedianSpeedLength > chunkMedianSpeedPadding)
                    chunkMedianSpeedPadding = chunkMedianSpeedLength;
            }

            Console.WriteLine("###########");
            Console.WriteLine("# SUMMARY #");
            Console.WriteLine("###########");
            Console.WriteLine();
            Console.WriteLine($"{"RANK".PadRight(rankPadding)} | {"HASH".PadRight(hashNamePadding)} | {"BULK".PadRight(bulkMedianSpeedPadding)} | {"CHUNK".PadRight(chunkMedianSpeedPadding)}");

            for (Int32 i = 0; i < results.Count; ++i)
            {
                var (HashName, _, BulkMedianSpeedFormatted, _, ChunkMedianSpeedFormatted) = results[i];
                String rank = (i + 1).ToString().PadRight(rankPadding);
                String hashName = HashName.PadRight(hashNamePadding);
                String bulkAverageSpeed = BulkMedianSpeedFormatted.PadRight(bulkMedianSpeedPadding);
                String chunkAverageSpeed = ChunkMedianSpeedFormatted.PadRight(chunkMedianSpeedPadding);

                Console.WriteLine($"{rank} | {hashName} | {bulkAverageSpeed} | {chunkAverageSpeed}");
            }

            Environment.Exit(0);
        }
        #endregion

        #region Methods
        private static Double GetMedianSpeed(Func<UInt64,Hash> hashInitializer, Int32 length, Int32 repetitions, Int32 align)
        {
            RandomXorShift r = new();

            Int32 stride = Math.Max(256, ((length + 255) / 256) * 256);
            Int32 batchCapacity = Math.Min(BATCH_MAXIMUM_OPERATIONS, Math.Max(1, BATCH_MAXIMUM_BYTES / stride));
            Int32 batchCount = (repetitions + batchCapacity - 1) / batchCapacity;

            Byte[] key = new Byte[(batchCapacity * stride) + 512];
            Hash[] hashes = new Hash[batchCapacity];
            List<Double> results = new(batchCount);

            unsafe
            {
                fixed (Byte* pin = key)
                {
                    UInt64 pinValue = (UInt64)pin;
                    UInt64 alignValue = ((pinValue + 255ul) & 0xFFFFFFFFFFFFFF00ul) + (UInt64)align;
                    Int32 offset = (Int32)(alignValue - pinValue);

                    Int32 completed = 0;

                    for (Int32 batch = 0; batch < batchCount; ++batch)
                    {
                        Int32 count = (repetitions / batchCount) + ((batch < (repetitions % batchCount)) ? 1 : 0);

                        for (Int32 i = 0; i < count; ++i)
                        {
                            r.NextBytes(key, offset + (i * stride), length);
                            hashes[i] = hashInitializer((UInt64)(completed + i));
                        }

                        Int64 start = Stopwatch.GetTimestamp();

                        Byte[] digest = null;

                        for (Int32 i = 0; i < count; ++i)
                            digest = hashes[i].ComputeHash(key, offset + (i * stride), length);

                        Int64 elapsed = Stopwatch.GetTimestamp() - start;

                        GC.KeepAlive(digest);
                        Array.Clear(hashes, 0, count);

                        if (elapsed <= 0)
                            throw new InvalidOperationException("The benchmark timer returned an invalid elapsed duration.");

                        Double speed = ((Double)length * count * Stopwatch.Frequency) / elapsed;
                        results.Add(speed);

                        completed += count;
                    }
                }
            }

            return MathUtilities.Median(results, true);
        }

        private static (Double[], Double, String) BulkSpeedTest(Func<UInt64,Hash> hashInitializer)
        {
            using (new SpeedOptimizer())
            {
                Double[] alignSpeeds = new Double[8];

                for (Int32 align = 0; align < 8; ++align)
                    alignSpeeds[align] = GetMedianSpeed(hashInitializer, BULK_KEYS_LENGTH, BULK_REPETITIONS, align);

                Double medianSpeed = MathUtilities.Median(alignSpeeds, false);
                String medianSpeedFormatted = Utilities.FormatSpeed(medianSpeed);

                return (alignSpeeds, medianSpeed, medianSpeedFormatted);
            }
        }

        private static (List<(Int32,Int32,Double)>, Double, String) ChunksSpeedTest(Func<UInt64,Hash> hashInitializer)
        {
            using (new SpeedOptimizer())
            {
                Int32 offset = 1;

                List<Double> allSpeeds = [];
                List<(Int32,Int32,Double)> chunkSpeeds = new List<(Int32,Int32,Double)>(s_ChunkParameters.Count);

                foreach ((Func<Int32,Int32> increment, Int32 keySize, Int32 repetitions) in s_ChunkParameters)
                {
                    Int32 offsetStart = offset;
                    List<Double> speeds = [];

                    while (offset < keySize)
                    {
                        Double speed = GetMedianSpeed(hashInitializer, offset, repetitions, 0);

                        speeds.Add(speed);
                        allSpeeds.Add(speed);

                        offset = increment(offset);
                    }

                    offset = keySize;

                    chunkSpeeds.Add((offsetStart, offset - 1, MathUtilities.Median(speeds, false)));
                }

                Double medianSpeed = MathUtilities.Median(allSpeeds, false);
                String medianSpeedFormatted = Utilities.FormatSpeed(medianSpeed);

                return (chunkSpeeds, medianSpeed, medianSpeedFormatted);
            }
        }

        private static void Warmup(Func<UInt64,Hash> hashInitializer)
        {
            Int32[] lengths = [0, 1, 4, 16, 32, 64, 128, 256, 1024, 65536, BULK_KEYS_LENGTH];

            RandomXorShift r = new();
            Byte[] key = new Byte[BULK_KEYS_LENGTH];
            r.NextBytes(key);

            for (Int32 i = 0; i < lengths.Length; ++i)
            {
                Int32 length = lengths[i];
                Hash hash = hashInitializer((UInt32)i);

                Byte[] digest = null;

                for (Int32 j = 0; j < WARMUP_REPETITIONS; ++j)
                    digest = hash.ComputeHash(key, 0, length);

                GC.KeepAlive(digest);
            }
        }
        #endregion
    }
}