#region Using Directives
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime;
using System.Runtime.InteropServices;
#endregion

namespace FastHashes.Benchmarks
{
    public static class Program
    {
        #region Setup
        private const Int32 BATCH_MAXIMUM_OPERATIONS = 8 * 1024;
        private const Int32 BATCH_MAXIMUM_BYTES = 8 * 1024 * 1024;
        private const Int32 BULK_KEYS_LENGTH = 256 * 1024;
        private const Int32 BULK_KEYS_LENGTH_MINIMUM = 64 * 1024;
        private const Int32 BULK_KEYS_LENGTH_MAXIMUM = 1024 * 1024;
        private const Int32 BULK_REPETITIONS = 5000;
        private const Int32 BULK_REPETITIONS_MINIMUM = 1000;
        private const Int32 BULK_REPETITIONS_MAXIMUM = 50000;
        private const Int32 WARMUP_REPETITIONS = 100;
        private const Int32 WARMUP_REPETITIONS_MINIMUM = 100;
        private const Int32 WARMUP_REPETITIONS_MAXIMUM = 1000;

        private static readonly List<(Func<Int32, Int32> Increment, Int32 KeySize, Int32 Repetitions)> s_ChunkParameters =
        [
            (i => i + 1, 32, 200000),
            (i => i + 2, 64, 100000),
            (i => i + 4, 128, 50000),
            (i => i + 8, 256, 25000),
            (i => i * 2, 65536, 12500)
        ];
        #endregion

        #region Entry Point
        public static Int32 Main(String[] args)
        {
            RootCommand command = new("Measures hash algorithms performance using bulk throughput and chunk latency benchmarks.");

            Option<Boolean> listOption = new("--list")
            {
                Description = "List all registered algorithms without running benchmarks."
            };

            Option<String> hashOption = new("--hash")
            {
                Description = "Run only the specified registered hash; by default, all registered hashes are benchmarked, with a dummy hash used as the baseline."
            };

            Option<Int32> bulkKeysLengthOption = new("--bulk-keys-length")
            {
                Description = $"Bulk keys length in bytes. [valid range: {BULK_KEYS_LENGTH_MINIMUM}-{BULK_KEYS_LENGTH_MAXIMUM}]",
                DefaultValueFactory = _ => BULK_KEYS_LENGTH
            };

            Option<Int32> bulkRepetitionsOption = new("--bulk-repetitions")
            {
                Description = $"Bulk repetitions per alignment. [valid range: {BULK_REPETITIONS_MINIMUM}-{BULK_REPETITIONS_MAXIMUM}]",
                DefaultValueFactory = _ => BULK_REPETITIONS
            };

            Option<Int32> warmupRepetitionsOption = new("--warmup-repetitions")
            {
                Description = $"Warmup repetitions per length. [valid range: {WARMUP_REPETITIONS_MINIMUM}-{WARMUP_REPETITIONS_MAXIMUM}]",
                DefaultValueFactory = _ => WARMUP_REPETITIONS
            };

            hashOption.Validators.Add(result =>
            {
                String hashName = result.GetValueOrDefault<String>();

                if (String.IsNullOrWhiteSpace(hashName))
                    result.AddError("Parameter --hash requires a non-empty value.");
                else if (!Hash.GetAlgorithms().Contains(hashName))
                    result.AddError($"Unknown --hash parameter value: {hashName}; hash names are case-sensitive and must match the registry.");
            });

            bulkKeysLengthOption.Validators.Add(result =>
            {
                Int32 value = result.GetValueOrDefault<Int32>();

                if ((value < BULK_KEYS_LENGTH_MINIMUM) || (value > BULK_KEYS_LENGTH_MAXIMUM))
                    result.AddError($"Parameter --bulk-keys-length must be between {BULK_KEYS_LENGTH_MINIMUM} and {BULK_KEYS_LENGTH_MAXIMUM}.");
            });

            bulkRepetitionsOption.Validators.Add(result =>
            {
                Int32 value = result.GetValueOrDefault<Int32>();

                if ((value < BULK_REPETITIONS_MINIMUM) || (value > BULK_REPETITIONS_MAXIMUM))
                    result.AddError($"Parameter --bulk-repetitions must be between {BULK_REPETITIONS_MINIMUM} and {BULK_REPETITIONS_MAXIMUM}.");
            });

            warmupRepetitionsOption.Validators.Add(result =>
            {
                Int32 value = result.GetValueOrDefault<Int32>();

                if ((value < WARMUP_REPETITIONS_MINIMUM) || (value > WARMUP_REPETITIONS_MAXIMUM))
                    result.AddError($"Parameter --warmup-repetitions must be between {WARMUP_REPETITIONS_MINIMUM} and {WARMUP_REPETITIONS_MAXIMUM}.");
            });

            command.Options.Add(listOption);
            command.Options.Add(hashOption);
            command.Options.Add(bulkKeysLengthOption);
            command.Options.Add(bulkRepetitionsOption);
            command.Options.Add(warmupRepetitionsOption);

            command.Validators.Add(result =>
            {
                if (!result.GetValue(listOption))
                    return;

                Option[] benchmarkOptions = [hashOption, bulkKeysLengthOption, bulkRepetitionsOption, warmupRepetitionsOption];

                foreach (Option option in benchmarkOptions)
                {
                    if (result.GetResult(option)?.Implicit == false)
                    {
                        result.AddError("Parameter --list cannot be combined with other options.");
                        return;
                    }
                }
            });

            command.SetAction
            (
                parseResult =>
                {
                    if (parseResult.GetValue(listOption))
                    {
                        foreach (String algorithm in Hash.GetAlgorithms())
                            Console.WriteLine(algorithm);

                        return 0;
                    }

                    return RunBenchmarks
                    (
                        parseResult.GetValue(hashOption),
                        parseResult.GetValue(bulkKeysLengthOption),
                        parseResult.GetValue(bulkRepetitionsOption),
                        parseResult.GetValue(warmupRepetitionsOption)
                    );
                }
            );

            return command.Parse(args).Invoke();
        }
        #endregion

        #region Methods
        private static Int32 RunBenchmarks(String selectedHash, Int32 bulkKeysLength, Int32 bulkRepetitions, Int32 warmupRepetitions)
        {
            Assembly assembly = typeof(Hash).Assembly;
            String libraryVersion = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
                ?? assembly.GetName().Version?.ToString()
                ?? "Unknown";

            Console.WriteLine("[SYSTEM CONFIGURATION]");
            Console.WriteLine($"Timestamp: {DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss zzz}");
            Console.WriteLine($"OS: {RuntimeInformation.OSDescription}");
            Console.WriteLine($"OS Architecture: {RuntimeInformation.OSArchitecture}");
            Console.WriteLine($"Process Architecture: {RuntimeInformation.ProcessArchitecture}");
            Console.WriteLine($"Runtime: {RuntimeInformation.FrameworkDescription}");
            Console.WriteLine($"Library Version: {libraryVersion}");
            Console.WriteLine($"Available Processors: {Environment.ProcessorCount}");
            Console.WriteLine($"Server GC: {GCSettings.IsServerGC}");
            Console.WriteLine($"Initial GC Latency Mode: {GCSettings.LatencyMode}");
            Console.WriteLine($"Stopwatch Frequency: {Stopwatch.Frequency} Hz");
            Console.WriteLine();

            Console.WriteLine("[BENCHMARK CONFIGURATION]");
            Console.WriteLine($"Mode: {(selectedHash == null ? "All Hashes" : "Single Hash")}");
            Console.WriteLine($"Batch Maximum Operations: {BATCH_MAXIMUM_OPERATIONS}");
            Console.WriteLine($"Batch Maximum Bytes: {BATCH_MAXIMUM_BYTES}");
            Console.WriteLine($"Warmup Repetitions/Length: {warmupRepetitions}");
            Console.WriteLine("Bulk Parameters:");
            Console.WriteLine($" - Keys Length: {bulkKeysLength} Bytes");
            Console.WriteLine($" - Repetitions/Alignment: {bulkRepetitions}");
            Console.WriteLine($" - Alignments: 8 (0-7), 2 Passes");
            Console.WriteLine("Chunk Parameters:");

            Int32 chunkStart = 1;

            foreach (var (_, keySize, repetitions) in s_ChunkParameters)
            {
                Console.WriteLine($" - {chunkStart}-{keySize - 1} Bytes ({repetitions} Repetitions/Length)");
                chunkStart = keySize;
            }

            Console.WriteLine($" - Endpoint: {s_ChunkParameters[^1].KeySize} Bytes ({s_ChunkParameters[^1].Repetitions} Repetitions)");
            Console.WriteLine();
            Console.WriteLine();

            IReadOnlyList<String> algorithms = Hash.GetAlgorithms();
            List<String> selectedAlgorithms = (selectedHash == null) ? ["DummyHash", .. algorithms] : [selectedHash];
            List<Result> results = new(algorithms.Count + 1);

            for (Int32 i = 0; i < selectedAlgorithms.Count; ++i)
            {
                String hashName = selectedAlgorithms[i];
                Func<UInt64, Hash> hashInitializer = String.Equals(hashName, "DummyHash", StringComparison.Ordinal)
                    ? seed => new DummyHash()
                    : Hash.GetInitializerSeeded(hashName).Ctor;

                String title = $"# HASH: {hashName} #";
                String frame = new('#', title.Length);

                Console.WriteLine(frame);
                Console.WriteLine(title);
                Console.WriteLine(frame);

                Warmup(hashInitializer, bulkKeysLength, warmupRepetitions);

                Console.WriteLine();
                Console.WriteLine("[BULK SPEED TEST]");
                Console.WriteLine($"Keys Length: {bulkKeysLength} Bytes");
                Console.WriteLine($"Repetitions: {bulkRepetitions} (2 Passes)");

                BulkSpeedOutput bso = BulkSpeedTest(hashInitializer, bulkKeysLength, bulkRepetitions);

                for (Int32 j = 0; j < bso.BulkAlignSpeeds.Length; ++j)
                {
                    Console.WriteLine($" - Aggregate Speed Alignment {j}: {Utilities.FormatSpeed(bso.BulkAlignSpeeds[j])}");
                    Console.WriteLine($"   Batch MAD: {Utilities.FormatVariability(bso.BulkVariability[j])}");
                }

                Console.WriteLine($" - Median Speed Overall: {bso.BulkMedianSpeedFormatted}");
                Console.WriteLine($" - Alignment MAD: {Utilities.FormatVariability(MathUtilities.RelativeMedianAbsoluteDeviation(bso.BulkAlignSpeeds))}");

                Console.WriteLine();
                Console.WriteLine("[CHUNKS SPEED TEST]");
                Console.WriteLine($"Keys Length Span: 1-{s_ChunkParameters.Max(x => x.KeySize)} Bytes");
                Console.WriteLine($"Repetitions Span: {s_ChunkParameters.Min(x => x.Repetitions)}-{s_ChunkParameters.Max(x => x.Repetitions)}");

                ChunkSpeedOutput cso = ChunksSpeedTest(hashInitializer);

                foreach (ChunkSpeedEntry entry in cso.ChunkSpeeds)
                {
                    Console.WriteLine($" - Aggregate Speed {entry.Start}-{entry.End} Bytes: {Utilities.FormatSpeed(entry.Speed)}");
                    Console.WriteLine($"   Median Batch MAD: {Utilities.FormatVariability(entry.Variability)}");
                }

                Console.WriteLine($" - Aggregate Speed Overall: {cso.ChunkAggregateSpeedFormatted}");
                Console.WriteLine($" - Geometric Mean Speed: {cso.ChunkNormalizedSpeedFormatted}");

                Console.WriteLine();
                Console.WriteLine();

                results.Add(new (hashName, bso.BulkMedianSpeedFormatted, cso.ChunkNormalizedSpeedFormatted));
            }

            if (selectedHash != null)
                return 0;

            Int32 orderPadding = Math.Max(5, results.Count.ToString().Length);
            Int32 hashNamePadding = 4;
            Int32 bulkMedianSpeedPadding = 4;
            Int32 chunkNormalizedSpeedPadding = 5;

            foreach ((String HashName, String BulkMedianSpeedFormatted, String ChunkNormalizedSpeedFormatted) in results)
            {
                hashNamePadding = Math.Max(hashNamePadding, HashName.Length);
                bulkMedianSpeedPadding = Math.Max(bulkMedianSpeedPadding, BulkMedianSpeedFormatted.Length);
                chunkNormalizedSpeedPadding = Math.Max(chunkNormalizedSpeedPadding, ChunkNormalizedSpeedFormatted.Length);
            }

            Console.WriteLine("###########");
            Console.WriteLine("# SUMMARY #");
            Console.WriteLine("###########");
            Console.WriteLine();
            Console.WriteLine($"{"ORDER".PadRight(orderPadding)} | {"HASH".PadRight(hashNamePadding)} | {"BULK".PadRight(bulkMedianSpeedPadding)} | {"CHUNK".PadRight(chunkNormalizedSpeedPadding)}");

            for (Int32 i = 0; i < results.Count; ++i)
            {
                var (hashName, bulkMedianSpeedFormatted, chunkNormalizedSpeedFormatted) = results[i];

                String order = (i + 1).ToString().PadRight(orderPadding);
                String hash = hashName.PadRight(hashNamePadding);
                String bulkSpeed = bulkMedianSpeedFormatted.PadRight(bulkMedianSpeedPadding);
                String chunkSpeed = chunkNormalizedSpeedFormatted.PadRight(chunkNormalizedSpeedPadding);

                Console.WriteLine($"{order} | {hash} | {bulkSpeed} | {chunkSpeed}");
            }

            return 0;
        }
        #endregion

        #region Methods
        private static SpeedOutput GetSpeed(Func<UInt64, Hash> hashInitializer, Int32 length, Int32 repetitions, Int32 align)
        {
            RandomXorShift r = new();

            Int32 stride = Math.Max(256, ((length + 255) / 256) * 256);
            Int32 batchCapacity = Math.Min(BATCH_MAXIMUM_OPERATIONS, Math.Max(1, BATCH_MAXIMUM_BYTES / stride));
            Int32 batchCount = Math.Min(repetitions, Math.Max(5, (repetitions + batchCapacity - 1) / batchCapacity));

            Byte[] key = new Byte[(batchCapacity * stride) + 512];
            Hash[] hashes = new Hash[batchCapacity];
            Int64 totalTicks = 0, totalBytes = 0;
            List<Double> batchSpeeds = new(batchCount);

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
                            digest = hashes[i].ComputeHashBytes(key, offset + (i * stride), length);

                        Int64 elapsed = Stopwatch.GetTimestamp() - start;

                        GC.KeepAlive(digest);
                        Array.Clear(hashes, 0, count);

                        if (elapsed <= 0)
                            throw new InvalidOperationException("The benchmark timer returned an invalid elapsed duration.");

                        Int64 bytes = (Int64)length * count;

                        totalTicks += elapsed;
                        totalBytes += bytes;

                        batchSpeeds.Add(MathUtilities.Throughput(bytes, elapsed, Stopwatch.Frequency));

                        completed += count;
                    }
                }
            }

            return new (totalBytes, totalTicks, batchSpeeds);
        }

        private static BulkSpeedOutput BulkSpeedTest(Func<UInt64, Hash> hashInitializer, Int32 bulkKeysLength, Int32 bulkRepetitions)
        {
            using (new SpeedOptimizer())
            {
                Double[] alignSpeeds = new Double[8];
                Double[] alignVariability = new Double[8];
                Int64[] alignBytes = new Int64[8];
                Int64[] alignTicks = new Int64[8];
                List<Double>[] alignBatchSpeeds = new List<Double>[8];

                for (Int32 align = 0; align < 8; ++align)
                    alignBatchSpeeds[align] = [];

                for (Int32 pass = 0; pass < 2; ++pass)
                {
                    for (Int32 position = 0; position < 8; ++position)
                    {
                        Int32 align = (pass == 0) ? position : 7 - position;
                        SpeedOutput so = GetSpeed(hashInitializer, bulkKeysLength, bulkRepetitions, align);

                        alignBytes[align] += so.Bytes;
                        alignTicks[align] += so.Ticks;
                        alignBatchSpeeds[align].AddRange(so.BatchSpeeds);
                    }
                }

                for (Int32 align = 0; align < 8; ++align)
                {
                    alignSpeeds[align] = MathUtilities.Throughput(alignBytes[align], alignTicks[align], Stopwatch.Frequency);
                    alignVariability[align] = MathUtilities.RelativeMedianAbsoluteDeviation(alignBatchSpeeds[align]);
                }

                Double medianSpeed = MathUtilities.Median(alignSpeeds, false);
                String medianSpeedFormatted = Utilities.FormatSpeed(medianSpeed);

                return new (alignSpeeds, alignVariability, medianSpeed, medianSpeedFormatted);
            }
        }

        private static ChunkSpeedOutput ChunksSpeedTest(Func<UInt64, Hash> hashInitializer)
        {
            using (new SpeedOptimizer())
            {
                Int32 length = 1;
                Int64 totalBytes = 0;
                Int64 totalTicks = 0;
                List<ChunkSpeedEntry> chunkSpeeds = new(s_ChunkParameters.Count);

                foreach ((Func<Int32, Int32> increment, Int32 keySize, Int32 repetitions) in s_ChunkParameters)
                {
                    Int32 lengthStart = length;
                    Int64 rangeBytes = 0;
                    Int64 rangeTicks = 0;
                    List<Double> lengthVariability = [];

                    while (length < keySize)
                    {
                        SpeedOutput so = GetSpeed(hashInitializer, length, repetitions, 0);
                        Double variability = MathUtilities.RelativeMedianAbsoluteDeviation(so.BatchSpeeds);

                        if (Double.IsFinite(variability))
                            lengthVariability.Add(variability);

                        rangeBytes += so.Bytes;
                        rangeTicks += so.Ticks;
                        length = increment(length);
                    }

                    Int32 lengthEnd = keySize - 1;

                    if (keySize == s_ChunkParameters[^1].KeySize)
                    {
                        SpeedOutput so = GetSpeed(hashInitializer, length, repetitions, 0);
                        Double variability = MathUtilities.RelativeMedianAbsoluteDeviation(so.BatchSpeeds);

                        if (Double.IsFinite(variability))
                            lengthVariability.Add(variability);

                        rangeBytes += so.Bytes;
                        rangeTicks += so.Ticks;
                        lengthEnd = keySize;
                    }

                    length = keySize;
                    totalBytes += rangeBytes;
                    totalTicks += rangeTicks;

                    Double rangeSpeed = MathUtilities.Throughput(rangeBytes, rangeTicks, Stopwatch.Frequency);
                    Double rangeVariability = lengthVariability.Count == 0 ? Double.NaN : MathUtilities.Median(lengthVariability, false);
                    chunkSpeeds.Add(new (lengthStart, lengthEnd, rangeSpeed, rangeVariability));
                }

                Double aggregateSpeed = MathUtilities.Throughput(totalBytes, totalTicks, Stopwatch.Frequency);
                String aggregateSpeedFormatted = Utilities.FormatSpeed(aggregateSpeed);

                Double normalizedSpeed = MathUtilities.GeometricMean([.. chunkSpeeds.Select(x => x.Speed)]);
                String normalizedSpeedFormatted = Utilities.FormatSpeed(normalizedSpeed);

                return new (chunkSpeeds, aggregateSpeed, aggregateSpeedFormatted, normalizedSpeed, normalizedSpeedFormatted);
            }
        }

        private static void Warmup(Func<UInt64, Hash> hashInitializer, Int32 bulkKeysLength, Int32 warmupRepetitions)
        {
            RandomXorShift r = new();
            Byte[] key = new Byte[Math.Max(65536, bulkKeysLength) + 512];
            r.NextBytes(key);

            Int32 seed = 0;
            WarmupLength(hashInitializer, key, 0, 0, warmupRepetitions, seed++);

            Int32 length = 1;

            foreach ((Func<Int32, Int32> increment, Int32 keySize, _) in s_ChunkParameters)
            {
                while (length < keySize)
                {
                    WarmupLength(hashInitializer, key, 0, length, warmupRepetitions, seed++);
                    length = increment(length);
                }

                if (keySize == s_ChunkParameters[^1].KeySize)
                    WarmupLength(hashInitializer, key, 0, keySize, warmupRepetitions, seed++);

                length = keySize;
            }

            unsafe
            {
                fixed (Byte* pin = key)
                {
                    UInt64 pinValue = (UInt64)pin;
                    UInt64 aligned = (pinValue + 255ul) & 0xFFFFFFFFFFFFFF00ul;

                    for (Int32 align = 0; align < 8; ++align)
                    {
                        Int32 offset = (Int32)(aligned + (UInt64)align - pinValue);
                        WarmupLength(hashInitializer, key, offset, bulkKeysLength, warmupRepetitions, seed++);
                    }
                }
            }
        }

        private static void WarmupLength(Func<UInt64, Hash> hashInitializer, Byte[] key, Int32 offset, Int32 length, Int32 repetitions, Int32 seed)
        {
            Hash hash = hashInitializer((UInt64)seed);
            Byte[] digest = null;

            for (Int32 i = 0; i < repetitions; ++i)
                digest = hash.ComputeHashBytes(key, offset, length);

            GC.KeepAlive(digest);
        }
        #endregion
    }
}