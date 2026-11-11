#region Using Directives
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime;
using System.Runtime.InteropServices;
using System.Text;
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
        private const Int32 WARMUP_REPETITIONS = 250;
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
            RootCommand command = new("Measures hash algorithm performance using bulk and chunk throughput benchmarks.");

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

        private static Int32 RunBenchmarks(String selectedHash, Int32 bulkKeysLength, Int32 bulkRepetitions, Int32 warmupRepetitions)
        {
            Assembly assembly = typeof(Hash).Assembly;
            String libraryVersion = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
                ?? assembly.GetName().Version?.ToString()
                ?? "Unknown";

            DateTimeOffset startDate = DateTimeOffset.Now;

            Console.WriteLine("[SYSTEM CONFIGURATION]");
            Console.WriteLine($"Timestamp: {startDate:yyyy-MM-dd HH:mm:ss zzz}");
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

                BulkThroughputOutput bto = BulkThroughputTest(hashInitializer, bulkKeysLength, bulkRepetitions);

                for (Int32 j = 0; j < bto.AlignmentThroughputs.Length; ++j)
                {
                    Console.WriteLine($" - Alignment {j} Throughput: {Utilities.FormatSpeed(bto.AlignmentThroughputs[j])}");
                    Console.WriteLine($"   Batch MAD: {Utilities.FormatVariability(bto.BatchMads[j])}");
                }

                Console.WriteLine($" - Median Throughput: {bto.MedianThroughputFormatted}");
                Console.WriteLine($" - Alignment MAD: {Utilities.FormatVariability(MathUtilities.RelativeMedianAbsoluteDeviation(bto.AlignmentThroughputs))}");

                Console.WriteLine();
                Console.WriteLine("[CHUNKS SPEED TEST]");
                Console.WriteLine($"Keys Length Span: 1-{s_ChunkParameters.Max(x => x.KeySize)} Bytes");
                Console.WriteLine($"Repetitions Span: {s_ChunkParameters.Min(x => x.Repetitions)}-{s_ChunkParameters.Max(x => x.Repetitions)}");

                ChunkThroughputOutput cto = ChunksThroughputTest(hashInitializer);

                foreach (ChunkRangeThroughput crt in cto.RangeThroughputs)
                {
                    Console.WriteLine($" - Range {crt.Start}-{crt.End} Throughput: {Utilities.FormatSpeed(crt.Throughput)}");
                    Console.WriteLine($"   Median Batch MAD: {Utilities.FormatVariability(crt.MedianBatchMad)}");
                }

                Console.WriteLine($" - Aggregate Throughput: {cto.AggregateThroughputFormatted}");
                Console.WriteLine($" - Average Throughput: {cto.AverageThroughputFormatted}");

                Console.WriteLine();
                Console.WriteLine();

                results.Add(new (hashName, bto.MedianThroughput, bto.MedianThroughputFormatted, cto.AverageThroughput, cto.AverageThroughputFormatted));
            }

            if (selectedHash != null)
                return 0;

            (String summary, _, _) = ExportSummaries(startDate, results);

            Console.WriteLine("###########");
            Console.WriteLine("# SUMMARY #");
            Console.WriteLine("###########");
            Console.WriteLine();
            Console.WriteLine(summary);

            return 0;
        }
        #endregion

        #region Methods
        private static (String, String, String) ExportSummaries(DateTimeOffset startDate, IReadOnlyList<Result> results)
        {
            String root = Path.Combine(Environment.CurrentDirectory, "Benchmarks");
            String runId = startDate.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture);
            String directory = Path.Combine(root, runId);
            String reservation = directory + ".lock";

            Directory.CreateDirectory(root);

            using FileStream reservationStream = new(reservation, FileMode.CreateNew, FileAccess.Write, FileShare.None);
            
            try
            {
                if (Directory.Exists(directory))
                    throw new IOException($"Benchmarks directory already exists: {directory}.");

                Directory.CreateDirectory(directory);
            }
            finally
            {
                reservationStream.Dispose();
                File.Delete(reservation);
            }

            Result[] resultsSortedBulk = [.. results.OrderBy(x => x.HashName != "DummyHash").ThenByDescending(x => x.BulkMedianThroughput)];
            Result[] resultsSortedChunk = [.. results.OrderBy(x => x.HashName != "DummyHash").ThenByDescending(x => x.ChunkAverageThroughput)];

            String summary = BuildSummary(results);
            File.WriteAllText(Path.Combine(directory, "summary.txt"), summary);

            String summaryBulk = BuildSummary(resultsSortedBulk);
            File.WriteAllText(Path.Combine(directory, "summary-bulk.txt"), summaryBulk);

            String summaryChunk = BuildSummary(resultsSortedChunk);
            File.WriteAllText(Path.Combine(directory, "summary-chunk.txt"), summaryChunk);

            return (summary, summaryBulk, summaryChunk);
        }

        private static BulkThroughputOutput BulkThroughputTest(Func<UInt64, Hash> hashInitializer, Int32 bulkKeysLength, Int32 bulkRepetitions)
        {
            using (new SpeedOptimizer())
            {
                Int64[] bytes = new Int64[8];
                Int64[] ticks = new Int64[8];
                List<Double>[] batchThroughputs = new List<Double>[8];

                for (Int32 alignment = 0; alignment < 8; ++alignment)
                    batchThroughputs[alignment] = [];

                for (Int32 pass = 0; pass < 2; ++pass)
                {
                    for (Int32 position = 0; position < 8; ++position)
                    {
                        Int32 alignment = (pass == 0) ? position : 7 - position;
                        ThroughputMeasurement tm = MeasureThroughput(hashInitializer, bulkKeysLength, bulkRepetitions, alignment);

                        bytes[alignment] += tm.Bytes;
                        ticks[alignment] += tm.Ticks;
                        batchThroughputs[alignment].AddRange(tm.BatchThroughputs);
                    }
                }

                Double[] throughputs = new Double[8];
                Double[] batchMads = new Double[8];

                for (Int32 alignment = 0; alignment < 8; ++alignment)
                {
                    throughputs[alignment] = MathUtilities.Throughput(bytes[alignment], ticks[alignment], Stopwatch.Frequency);
                    batchMads[alignment] = MathUtilities.RelativeMedianAbsoluteDeviation(batchThroughputs[alignment]);
                }

                Double medianThroughput = MathUtilities.Median(throughputs, false);
                String medianThroughputFormatted = Utilities.FormatSpeed(medianThroughput);

                return new (throughputs, batchMads, medianThroughput, medianThroughputFormatted);
            }
        }

        private static ChunkThroughputOutput ChunksThroughputTest(Func<UInt64, Hash> hashInitializer)
        {
            using (new SpeedOptimizer())
            {
                Int32 length = 1;
                Int64 totalBytes = 0;
                Int64 totalTicks = 0;
                List<ChunkRangeThroughput> rangeThroughputs = new(s_ChunkParameters.Count);

                foreach ((Func<Int32, Int32> increment, Int32 keySize, Int32 repetitions) in s_ChunkParameters)
                {
                    Int32 start = length;
                    Int32 end = keySize - 1;

                    Int64 bytes = 0;
                    Int64 ticks = 0;
                    List<Double> batchMads = [];

                    while (length < keySize)
                    {
                        ThroughputMeasurement tm = MeasureThroughput(hashInitializer, length, repetitions, 0);
                        Double batchMad = MathUtilities.RelativeMedianAbsoluteDeviation(tm.BatchThroughputs);

                        if (Double.IsFinite(batchMad))
                            batchMads.Add(batchMad);

                        bytes += tm.Bytes;
                        ticks += tm.Ticks;
                        
                        length = increment(length);
                    }

                    if (keySize == s_ChunkParameters[^1].KeySize)
                    {
                        ThroughputMeasurement tm = MeasureThroughput(hashInitializer, length, repetitions, 0);
                        Double batchMad = MathUtilities.RelativeMedianAbsoluteDeviation(tm.BatchThroughputs);

                        if (Double.IsFinite(batchMad))
                            batchMads.Add(batchMad);

                        bytes += tm.Bytes;
                        ticks += tm.Ticks;
                        end = keySize;
                    }

                    length = keySize;
                    totalBytes += bytes;
                    totalTicks += ticks;

                    Double throughput = MathUtilities.Throughput(bytes, ticks, Stopwatch.Frequency);
                    Double medianBatchMad = (batchMads.Count == 0) ? Double.NaN : MathUtilities.Median(batchMads, false);
                    rangeThroughputs.Add(new (start, end, throughput, medianBatchMad));
                }

                Double aggregateThroughput = MathUtilities.Throughput(totalBytes, totalTicks, Stopwatch.Frequency);
                String aggregateThroughputFormatted = Utilities.FormatSpeed(aggregateThroughput);

                Double averageThroughput = MathUtilities.GeometricMean([.. rangeThroughputs.Select(x => x.Throughput)]);
                String averageThroughputFormatted = Utilities.FormatSpeed(averageThroughput);

                return new (rangeThroughputs, aggregateThroughput, aggregateThroughputFormatted, averageThroughput, averageThroughputFormatted);
            }
        }

        private static ThroughputMeasurement MeasureThroughput(Func<UInt64, Hash> hashInitializer, Int32 length, Int32 repetitions, Int32 alignment)
        {
            RandomXorShift r = new();

            Int32 stride = Math.Max(256, ((length + 255) / 256) * 256);
            Int32 batchCapacity = Math.Min(BATCH_MAXIMUM_OPERATIONS, Math.Max(1, BATCH_MAXIMUM_BYTES / stride));
            Int32 batchCount = Math.Min(repetitions, Math.Max(5, (repetitions + batchCapacity - 1) / batchCapacity));

            Byte[] key = new Byte[(batchCapacity * stride) + 512];
            Hash[] hashes = new Hash[batchCapacity];
            Byte[] hashBytes = new Byte[hashInitializer(0ul).Length / 8];

            Int64 totalTicks = 0, totalBytes = 0;
            List<Double> batchThroughputs = new(batchCount);

            unsafe
            {
                fixed (Byte* pin = key)
                {
                    UInt64 pinValue = (UInt64)pin;
                    UInt64 aligned = ((pinValue + 255ul) & 0xFFFFFFFFFFFFFF00ul) + (UInt64)alignment;
                    Int32 offset = (Int32)(aligned - pinValue);

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

                        for (Int32 i = 0; i < count; ++i)
                            hashes[i].ComputeHashBytes(key.AsSpan(offset + (i * stride), length), hashBytes);

                        Int64 elapsed = Stopwatch.GetTimestamp() - start;

                        GC.KeepAlive(hashBytes);
                        Array.Clear(hashes, 0, count);

                        if (elapsed <= 0)
                            throw new InvalidOperationException("The benchmark timer returned an invalid elapsed duration.");

                        Int64 bytes = (Int64)length * count;

                        totalTicks += elapsed;
                        totalBytes += bytes;

                        batchThroughputs.Add(MathUtilities.Throughput(bytes, elapsed, Stopwatch.Frequency));

                        completed += count;
                    }
                }
            }

            return new (totalBytes, totalTicks, batchThroughputs);
        }

        private static String BuildSummary(IEnumerable<Result> results)
        {
            Result[] rows = [.. results];
            Int32 orderWidth = Math.Max(5, rows.Length.ToString(CultureInfo.InvariantCulture).Length);
            Int32 hashWidth = Math.Max(4, rows.Max(x => x.HashName.Length));
            Int32 bulkWidth = Math.Max(4, rows.Max(x => x.BulkMedianThroughputFormatted.Length));
            Int32 chunkWidth = Math.Max(5, rows.Max(x => x.ChunkAverageThroughputFormatted.Length));

            StringBuilder builder = new();

            builder.AppendLine(
                $"{"ORDER".PadRight(orderWidth)} | "
                + $"{"HASH".PadRight(hashWidth)} | "
                + $"{"BULK".PadRight(bulkWidth)} | "
                + $"{"CHUNK".PadRight(chunkWidth)}"
            );

            for (Int32 i = 0; i < rows.Length; ++i)
            {
                Result result = rows[i];

                builder.AppendLine(
                    $"{(i + 1).ToString(CultureInfo.InvariantCulture).PadRight(orderWidth)} | "
                    + $"{result.HashName.PadRight(hashWidth)} | "
                    + $"{result.BulkMedianThroughputFormatted.PadRight(bulkWidth)} | "
                    + $"{result.ChunkAverageThroughputFormatted.PadRight(chunkWidth)}"
                );
            }

            return builder.ToString();
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

                    for (Int32 alignment = 0; alignment < 8; ++alignment)
                    {
                        Int32 offset = (Int32)(aligned + (UInt64)alignment - pinValue);
                        WarmupLength(hashInitializer, key, offset, bulkKeysLength, warmupRepetitions, seed++);
                    }
                }
            }
        }

        private static void WarmupLength(Func<UInt64, Hash> hashInitializer, Byte[] key, Int32 offset, Int32 length, Int32 repetitions, Int32 seed)
        {
            Hash hash = hashInitializer((UInt64)seed);
            Byte[] digest = new Byte[hash.Length / 8];

            for (Int32 i = 0; i < repetitions; ++i)
                hash.ComputeHashBytes(key.AsSpan(offset, length), digest);

            GC.KeepAlive(digest);
        }
        #endregion
    }
}