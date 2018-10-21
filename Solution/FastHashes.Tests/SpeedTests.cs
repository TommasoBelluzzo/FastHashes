#region Using Directives
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime;
#endregion

namespace FastHashes.Tests
{
    public static class SpeedTests
    {
        #region Constants
        private const Int32 BST_SIZE = 256 * 1024;
        private const Int32 BST_TRIALS = 5000;
        private const Int32 WARMUP_ITERATIONS = 3;
        #endregion

        #region Members
        private static readonly Double FREQUENCY = NativeMethods.GetFrequency();

        private static readonly dynamic[] CST_PARAMETERS =
        {
            new { Increment = new Func<Int32, Int32>((i) => i + 1), KeysSize = 32, Repetitions = 200000 },
            new { Increment = new Func<Int32, Int32>((i) => i + 2), KeysSize = 64, Repetitions = 100000 },
            new { Increment = new Func<Int32, Int32>((i) => i + 4), KeysSize = 128, Repetitions = 50000 },
            new { Increment = new Func<Int32, Int32>((i) => i + 8), KeysSize = 256, Repetitions = 25000 },
            new { Increment = new Func<Int32, Int32>((i) => i * 2), KeysSize = 65536, Repetitions = 12500 }
        };

        private static readonly String[] SIZE_SUFFIXES = { "B", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB" };
        #endregion

        #region Methods
        private static Double GetAverageSpeed(HashInfo hashInfo, Int32 length, Int32 repetitions, Int32 align)
        {
            RandomXS r = new RandomXS();
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

                        Hash hash = hashInfo.Initializer((UInt32)i);

                        Double start = NativeMethods.GetTime();
                        hash.ComputeHash(key, offset, length);
                        Double end = NativeMethods.GetTime();

                        Double ms = ((end - start + 1.0d) * 1000.0d) / FREQUENCY;
                        Double bps = (length * 1000.0d) / ms;

                        if (bps >= 0.0d)
                            results.Add(bps);
                    }

                    Double mean = StatsUtilities.Mean(results);
                    Double threshold = 2.0d * StatsUtilities.StandardDeviation(results, mean);
 
                    for (Int32 i = results.Count - 1; i >= 0; --i)
                    {
                        if (Math.Abs(results[i] - mean) > threshold)
                            results.RemoveAt(i);
                    }
 
                    return StatsUtilities.Mean(results);
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
                initializer: () =>
                {
                    process.PriorityClass = ProcessPriorityClass.RealTime;
                    GCSettings.LatencyMode = GCLatencyMode.SustainedLowLatency;
                },
                disposer: () =>
                {
                    GCSettings.LatencyMode = lastLatencyMode;
                    process.PriorityClass = lastPriorityClass;
                }
            );
        }

        private static String FormatSpeed(Double speed)
        {
            Int32 magnitude = (Int32)Math.Log(speed, 1024);
            Double adjustedSpeed = speed / (1L << (magnitude * 10));

            if (Math.Round(adjustedSpeed, 2) >= 1000.0d)
            {
                magnitude += 1;
                adjustedSpeed /= 1024.0d;
            }

            return $"{adjustedSpeed:N2} {SIZE_SUFFIXES[magnitude]}/s";
        }

        public static void BulkSpeedTest(HashInfo hashInfo)
        {
            Console.WriteLine("[BULK SPEED TEST]");
            Console.WriteLine($"Frequency: {FREQUENCY / 1e6d:F2} MHz");
            Console.WriteLine($"Keys Length: {BST_SIZE} Bytes");
            Console.WriteLine($"Repetitions: {BST_TRIALS}");

            using (SpeedTestOptimizer())
            {
                for (Int32 i = 0; i < WARMUP_ITERATIONS; ++i)
                    GetAverageSpeed(hashInfo, BST_SIZE, BST_TRIALS, 0);

                Double[] speed = new Double[8];

                for (Int32 align = 0; align < 8; ++align)
                {
                    speed[align] = GetAverageSpeed(hashInfo, BST_SIZE, BST_TRIALS, align);
                    Console.WriteLine($" - Alignment {align}: {FormatSpeed(speed[align])}");
                }

                Console.WriteLine($" - Average Speed: {FormatSpeed(StatsUtilities.Mean(speed))}");
            }
        }

        public static void ChunksSpeedTest(HashInfo hashInfo)
        {
            Console.WriteLine("[CHUNKS SPEED TEST]");
            Console.WriteLine($"Frequency: {FREQUENCY / 1e6d:F2} MHz");
            Console.WriteLine("Keys Length Span: 0-65535 Bytes");

            using (SpeedTestOptimizer())
            {
                for (Int32 i = 0; i < WARMUP_ITERATIONS; ++i)
                    GetAverageSpeed(hashInfo, BST_SIZE, BST_TRIALS, 0);

                Double totalSpeed = 0.0d;
                Int32 totalCount = 0;
                Int32 offset = 0;

                for (Int32 i = 0; i < CST_PARAMETERS.Length; ++i)
                {
                    dynamic p = CST_PARAMETERS[i];

                    Double speed = 0.0d;
                    Int32 count = 0;
                    Int32 offsetStart = offset;

                    while (offset < p.KeysSize)
                    {
                        speed += GetAverageSpeed(hashInfo, offset, p.Repetitions, 0);
                        ++count;

                        offset = p.Increment(offset);
                    }

                    totalSpeed += speed;
                    totalCount += count;
                    offset = p.KeysSize;

                    Console.WriteLine($" - Average Speed {offsetStart}-{offset - 1} Bytes: {FormatSpeed(speed / count)}");
                    Console.WriteLine($" - Average Speed Overall: {FormatSpeed(totalSpeed / totalCount)}");
                }
            }
        }
        #endregion
    }
}