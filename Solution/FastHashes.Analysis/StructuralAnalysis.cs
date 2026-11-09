#region Using Directives
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Numerics;
using System.Text;
#endregion

namespace FastHashes.Analysis
{
    internal static class StructuralAnalysis
    {
        #region Setup
        private const Int32 MAXIMUM_PROBES = 20000;
        #endregion

        #region Methods
        private static Byte[] Repeat(Byte[] pattern, Int32 length, Int32 phase)
        {
            Int32 patternLength = pattern.Length;

            Byte[] result = new Byte[length];

            for (Int32 i = 0; i < length; ++i)
                result[i] = pattern[(i + phase) % patternLength];

            return result;
        }

        private static IEnumerable<Int32> BoundaryLengths(Int32 maximumLength)
        {
            Int32[] thresholds = [0, 1, 2, 3, 4, 8, 16, 32, 64, 128, 256, 512];

            SortedSet<Int32> lengths = [];

            foreach (Int32 threshold in thresholds)
            {
                for (Int32 delta = -1; delta <= 1; ++delta)
                {
                    Int32 length = threshold + delta;

                    if ((length >= 0) && (length <= maximumLength))
                        lengths.Add(length);
                }
            }

            foreach (Int32 length in lengths)
                yield return length;
        }

        private static IEnumerable<StructuralPair> CreatePairsBoundaries(Int32 maximumLength)
        {
            foreach (Int32 length in BoundaryLengths(maximumLength))
            {
                if (length == 0)
                    continue;

                Byte[] a = new Byte[length - 1];
                Array.Fill(a, (Byte)0xA5);

                Byte[] b = new Byte[length];
                Array.Fill(b, (Byte)0xA5);

                yield return new StructuralPair("boundaries", $"lengths:{length - 1}:{length}:A5", a, b);
            }
        }

        private static IEnumerable<StructuralPair> CreatePairsCyclic(Int32 keysLength)
        {
            Byte[][] patterns = [[0x00, 0xFF], [0xAA, 0x55], [0x01, 0x02, 0x03], [0x00, 0x01, 0x00, 0xFF], [0x10, 0x20, 0x30, 0x40, 0x50]];

            foreach (Byte[] pattern in patterns)
            {
                Int32 patternLength = pattern.Length;
                Byte[] a = Repeat(pattern, keysLength, 0);

                for (Int32 phase = 1; phase < Math.Min(patternLength, 3); ++phase)
                {
                    Byte[] shifted = Repeat(pattern, keysLength, phase);
                    yield return new StructuralPair("cyclic", $"period:{patternLength}:phase:{phase}", a, shifted);
                }

                Int32 shorterLength = patternLength - 1;
                Byte[] shorter = new Byte[shorterLength];
                Array.Copy(pattern, shorter, shorterLength);

                Byte[] changedPeriod = Repeat(shorter, keysLength, 0);

                yield return new StructuralPair("cyclic", $"period:{patternLength}:shortened:{shorterLength}", a, changedPeriod);
            }
        }

        private static IEnumerable<StructuralPair> CreatePairsPermutation(Int32 keysLength)
        {
            Int32 width = Math.Min(8, keysLength);
            Byte[] baseline = new Byte[keysLength];

            for (Int32 i = 0; i < width; ++i)
                baseline[i] = (Byte)(i + 1);

            for (Int32 i = 0; i < width; ++i)
            {
                for (Int32 j = i + 1; j < width; ++j)
                {
                    Byte[] altered = (Byte[])baseline.Clone();
                    (altered[i], altered[j]) = (altered[j], altered[i]);

                    yield return new StructuralPair("permutation", $"swap:{i}:{j}", baseline, altered);
                }
            }
        }

        private static IEnumerable<StructuralPair> CreatePairsSparse(Int32 keysLength)
        {
            Int32 bits = checked(keysLength * 8);

            for (Int32 bit = 0; bit < bits; ++bit)
            {
                Byte[] a = new Byte[keysLength];
                Byte[] b = new Byte[keysLength];
                b[bit / 8] = (Byte)(1 << (bit % 8));

                yield return new StructuralPair("sparse", $"single-bit:{bit}", a, b);
            }

            for (Int32 bit = 0; bit < bits; ++bit)
            {
                Int32 other = (bit + Math.Max(1, bits / 2)) % bits;
                if (other <= bit)
                    continue;

                Byte[] a = new Byte[keysLength];
                Byte[] b = new Byte[keysLength];
                b[bit / 8] |= (Byte)(1 << (bit % 8));
                b[other / 8] |= (Byte)(1 << (other % 8));

                yield return new StructuralPair("sparse", $"two-bits:{bit}:{other}", a, b);
            }
        }

        private static IEnumerable<StructuralPair> CreatePairsTwoBytes(Int32 keysLength)
        {
            SortedSet<Int32> positions = [0, 1, keysLength / 2, keysLength - 2, keysLength - 1];
            Byte[] values = [1, 0x80, 0xFF];

            foreach (Int32 p in positions)
            {
                foreach (Int32 q in positions)
                {
                    if (q <= p)
                        continue;

                    foreach (Byte value in values)
                    {
                        Byte[] a = new Byte[keysLength];
                        a[p] = value;
                        a[q] = value;

                        Byte[] first = (Byte[])a.Clone();
                        first[p] ^= 0xFF;
                        yield return new StructuralPair("twobytes", $"positions:{p}:{q}:value:{value:X2}:first", a, first);

                        Byte[] second = (Byte[])a.Clone();
                        second[q] ^= 0xFF;
                        yield return new StructuralPair("twobytes", $"positions:{p}:{q}:value:{value:X2}:second", a, second);

                        Byte[] both = (Byte[])a.Clone();
                        both[p] ^= 0xFF;
                        both[q] ^= 0xFF;
                        yield return new StructuralPair("twobytes", $"positions:{p}:{q}:value:{value:X2}:both", a, both);
                    }
                }
            }
        }

        private static IEnumerable<StructuralPair> CreatePairsWindow(Int32 keyLength)
        {
            for (Int32 offset = 0; offset <= keyLength - 2; ++offset)
            {
                foreach (UInt16 value in new UInt16[] { 1, 0x0100, 0x8001, 0xFFFF })
                {
                    Byte[] a = new Byte[keyLength];
                    Byte[] b = new Byte[keyLength];
                    b[offset] = (Byte)(value & 0xFF);
                    b[offset + 1] = (Byte)(value >> 8);

                    yield return new StructuralPair("window", $"offset:{offset}:value:{value:X4}", a, b);
                }
            }
        }

        private static IEnumerable<StructuralPair> CreatePairsZeroes(Int32 maximumLength)
        {
            foreach (Int32 length in BoundaryLengths(maximumLength))
            {
                if (length == 0)
                    continue;

                Byte[] a = new Byte[length - 1];
                Byte[] b = new Byte[length];

                yield return new StructuralPair("zeroes", $"zero-lengths:{length - 1}:{length}", a, b);
            }
        }

        private static StructuralResult Evaluate(Hash hash, String family, Int32 keysLength)
        {
            IEnumerable<StructuralPair> pairs = family switch
            {
                "boundaries" => CreatePairsBoundaries(keysLength),
                "cyclic" => CreatePairsCyclic(keysLength),
                "permutation" => CreatePairsPermutation(keysLength),
                "sparse" => CreatePairsSparse(keysLength),
                "twobytes" => CreatePairsTwoBytes(keysLength),
                "window" => CreatePairsWindow(keysLength),
                "zeroes" => CreatePairsZeroes(keysLength),
                _ => throw new ArgumentException("Unsupported structural family specified."),
            };

            List<StructuralObservation> observations = [];
            Int32 probes = 0;
            Int64 totalDistance = 0;
            Int32 minimumDistance = Int32.MaxValue;
            Int32 maximumDistance = 0;
            Int32 equalDigests = 0;

            foreach (StructuralPair pair in pairs)
            {
                ++probes;

                StructuralObservation observation = Measure(hash, "default", pair);
                observations.Add(observation);

                Int32 distance = observation.HammingDistance;
                totalDistance += distance;
                minimumDistance = Math.Min(minimumDistance, distance);
                maximumDistance = Math.Max(maximumDistance, distance);

                if (distance == 0)
                    ++equalDigests;
            }

            StructuralResult result = new
            (
                family,
                probes, 0,
                (Double)totalDistance / probes,
                minimumDistance,
                maximumDistance,
                equalDigests,
                [],
                [.. observations]
            );

            return result;
        }

        private static StructuralResult EvaluateSeeds(String hashName, Int32 keysLength)
        {
            (Func<UInt64, Hash> ctor, Int32 bits) = Hash.GetInitializerSeeded(hashName);

            UInt64[] seeds = bits switch
            {
                32 => [0ul, 1ul, 0x55555555ul, 0xAAAAAAAAul, UInt32.MaxValue],
                64 => [0ul, 1ul, 0x5555555555555555ul, 0xAAAAAAAAAAAAAAAAul, UInt64.MaxValue],
                _ => throw new NotSupportedException($"Unsupported seed width: {bits}.")
            };

            Int32 length = Math.Max(1, keysLength);
            Byte[] zero = new Byte[length];
            Byte[] tail = new Byte[length];
            tail[^1] = 1;

            Byte[] uniform = new Byte[length];
            Byte[] changed = new Byte[length];
            Array.Fill(uniform, (Byte)0xA5);
            Array.Fill(changed, (Byte)0xA5);
            changed[0] ^= 1;

            List<StructuralPair> pairs =
            [
                new StructuralPair("seed", "zero-vs-last-bit", zero, tail),
                new StructuralPair("seed", "pattern-vs-first-bit", uniform, changed)
            ];

            List<StructuralObservation> observations = [];
            Int32 probes = 0, skipped = 0;
            Int64 totalDistance = 0;
            Int32 minimumDistance = Int32.MaxValue;
            Int32 maximumDistance = 0;
            Int32 equalDigests = 0;
            List<String> notes = [];

            foreach (UInt64 seed in seeds)
            {
                Hash hash;

                try
                {
                    hash = ctor(seed);
                }
                catch (Exception exception)
                {
                    ++skipped;
                    notes.Add($"Seed {seed}: {exception.GetType().Name}: {exception.Message}");

                    continue;
                }

                foreach (StructuralPair pair in pairs)
                {
                    ++probes;

                    StructuralObservation observation = Measure(hash, seed.ToString(CultureInfo.InvariantCulture), pair);
                    observations.Add(observation);

                    Int32 distance = observation.HammingDistance;
                    totalDistance += distance;
                    minimumDistance = Math.Min(minimumDistance, distance);
                    maximumDistance = Math.Max(maximumDistance, distance);

                    if (distance == 0)
                        ++equalDigests;
                }
            }

            StructuralResult result = new
            (
                "seed",
                probes, skipped,
                (probes == 0) ? null : (Double)totalDistance / probes,
                (probes == 0) ? null : minimumDistance,
                (probes == 0) ? null : maximumDistance,
                equalDigests,
                [.. notes],
                [.. observations]
            );

            return result;
        }

        private static StructuralObservation Measure(Hash hash, String configuration, StructuralPair pair)
        {
            if (pair.InputA.AsSpan().SequenceEqual(pair.InputB))
                throw new InvalidOperationException($"Structural probe '{pair.Family}/{pair.Case}' contains identical input keys.");

            Byte[] hashA = hash.ComputeHashBytes(pair.InputA);
            Byte[] hashB = hash.ComputeHashBytes(pair.InputB);
            Int32 distance = 0;

            for (Int32 i = 0; i < hashA.Length; ++i)
                distance += BitOperations.PopCount((UInt32)(hashA[i] ^ hashB[i]));

            return new StructuralObservation
            (
                pair.Family, pair.Case, configuration,
                Convert.ToHexString(pair.InputA), Convert.ToHexString(pair.InputB), Convert.ToHexString(hashA), Convert.ToHexString(hashB),
                distance, distance == 0
            );
        }

        public static String Run(Config config, String outputDirectory = null)
        {
            Hash hash = config.Hash;
            String hashName = config.HashName;
            Int32 keysLength = config.KeysLength;

            Int32 inputBits = checked(keysLength * 8);
            Int32 outputBits = hash.Length;

            Int64 estimatedProbes = (2L * 8 * keysLength) + (4L * (keysLength - 1)) + 100;

            if (estimatedProbes > MAXIMUM_PROBES)
                throw new ArgumentException($"Structural workload exceeds {MAXIMUM_PROBES} probes; reduce the --keys-length parameter value.");

            List<StructuralResult> results =
            [
                Evaluate(hash, "boundaries", keysLength),
                Evaluate(hash, "cyclic", keysLength),
                Evaluate(hash, "permutation", keysLength),
                Evaluate(hash, "sparse", keysLength),
                Evaluate(hash, "twobytes", keysLength),
                Evaluate(hash, "zeroes", keysLength)
            ];

            StructuralResult seedResult = EvaluateSeeds(hashName, keysLength);
            results.Add(seedResult);

            StructuralResult windowResult = Evaluate(hash, "window", keysLength);
            results.Add(windowResult);

            Int64 probes = 0;
            Int64 equalDigestsTotal = 0;

            foreach (StructuralResult result in results)
            {
                probes += result.Probes;
                equalDigestsTotal += result.EqualDigests;
            }

            Int64 hashEvaluations = checked(probes * 2L);

            Dictionary<String, Object> metadata = Utilities.CreateMetadata("structural", config);
            metadata["InputBits"] = inputBits;
            metadata["OutputBits"] = outputBits;
            metadata["Probes"] = probes;
            metadata["HashEvaluations"] = hashEvaluations;
            metadata["EqualDigestsTotal"] = equalDigestsTotal;
            metadata["Families"] = results.ConvertAll(result => new
            {
                result.Family,
                result.Probes,
                result.Skipped,
                result.MeanHamming,
                result.MinimumHamming,
                result.MaximumHamming,
                result.EqualDigests,
                result.Notes
            });
            metadata["OutputFiles"] = new[] {
                "structural.json",
                "structural-summary.csv", "structural-families.csv",
                "structural-ss.svg", "structural-ws.svg"
            };

            void writeFunction(String directory)
            {
                String filePath;
                Encoding encoding = new UTF8Encoding(false);

                filePath = Path.Combine(directory, "structural.json");
                File.WriteAllText(filePath, Utilities.SerializeJson(metadata) + "\n", encoding);

                filePath = Path.Combine(directory, "structural-summary.csv");

                using (StreamWriter writer = new(filePath, false, encoding))
                {
                    writer.WriteLine("Family,Probes,Skipped,MeanHamming,MinimumHamming,MaximumHamming,EqualDigests");

                    foreach (StructuralResult result in results)
                    {
                        writer.WriteLine(String.Join(",",
                            result.Family,
                            Utilities.FormatCsvInteger(result.Probes),
                            Utilities.FormatCsvInteger(result.Skipped),
                            result.MeanHamming.HasValue ? Utilities.FormatCsvDouble(result.MeanHamming.Value) : String.Empty,
                            result.MinimumHamming.HasValue ? Utilities.FormatCsvInteger(result.MinimumHamming.Value) : String.Empty,
                            result.MaximumHamming.HasValue ? Utilities.FormatCsvInteger(result.MaximumHamming.Value) : String.Empty,
                            Utilities.FormatCsvInteger(result.EqualDigests)
                        ));
                    }
                }

                filePath = Path.Combine(directory, "structural-families.csv");

                using (StreamWriter writer = new(filePath, false, encoding))
                {
                    writer.WriteLine("Family,Case,Seed,InputA,InputB,HashA,HashB,HammingDistance,EqualDigest");

                    foreach (StructuralResult result in results)
                    {
                        foreach (StructuralObservation observation in result.Observations)
                        {
                            writer.WriteLine(String.Join(",",
                                observation.Family,
                                observation.Case,
                                observation.Configuration,
                                observation.InputA,
                                observation.InputB,
                                observation.HashA,
                                observation.HashB,
                                Utilities.FormatCsvInteger(observation.HammingDistance),
                                observation.EqualDigest ? "true" : "false"
                            ));
                        }
                    }
                }

                filePath = Path.Combine(directory, "structural-ss.svg");
                PlotSs(filePath, hashName, outputBits, seedResult);

                filePath = Path.Combine(directory, "structural-ws.svg");
                PlotWs(filePath, outputBits, windowResult);
            }

            return Utilities.WriteArtifacts(hashName, writeFunction, outputDirectory);
        }
        #endregion

        #region Plots
        private static void PlotSs(String path, String hashName, Int32 outputBits, StructuralResult seedResult)
        {
            Int32 bits = Hash.GetInitializerSeeded(hashName).Bits;

            UInt64[] seeds = bits switch
            {
                32 => [0ul, 1ul, 0x55555555ul, 0xAAAAAAAAul, UInt32.MaxValue],
                64 => [0ul, 1ul, 0x5555555555555555ul, 0xAAAAAAAAAAAAAAAAul, UInt64.MaxValue],
                _ => throw new NotSupportedException($"Unsupported seed width: {bits}.")
            };

            String[] cases = ["zero-vs-last-bit", "pattern-vs-first-bit"];
            String[] colors = ["#1f77b4", "#d62728"];

            List<(Double Value, String Label)> ticks = [];

            for (Int32 i = 0; i < seeds.Length; ++i)
                ticks.Add((i, "0x" + seeds[i].ToString(bits == 32 ? "X8" : "X16", CultureInfo.InvariantCulture)));

            using SvgPlot plot = new(path, 1200, 820);

            plot.PlotArea(150.0d, 185.0d, 920.0d, 445.0d);
            plot.DataRange(-0.5d, 4.5d, 0.0d, outputBits);

            plot.ConfigureTicksX("Seed Configuration", ticks);
            plot.ConfigureDiscreteAxisY("Changed Output Bits", 0.0d, outputBits, intervals: 4, format: "0");

            plot.ConfigureBottomLegend([
                new SvgPlot.LegendItem("Zero vs Last Bit", colors[0], false),
                new SvgPlot.LegendItem("Pattern vs First Bit", colors[1], false),
                new SvgPlot.LegendItem("Ideal Mean", plot.ReferenceColor, true, true)
            ]);

            plot.Title("SEED SENSITIVITY");
            plot.Subtitle("Hamming distance for two-input perturbations under different hash seeds.");
            plot.Metadata(
                $"Output Bits: {outputBits:N0} | Seed Bits: {bits:N0} | Probes: {seedResult.Probes:N0}",
                $"Mean: {seedResult.MeanHamming:0.###} | Minimum: {seedResult.MinimumHamming:N0} | Maximum: {seedResult.MaximumHamming:N0}"
            );

            plot.PrepareLayout();
            plot.DrawGrid();

            plot.BeginPlot();

            for (Int32 i = 0; i < seeds.Length; ++i)
            {
                String configuration = seeds[i].ToString(CultureInfo.InvariantCulture);

                foreach (StructuralObservation observation in seedResult.Observations)
                {
                    if (observation.Configuration != configuration)
                        continue;

                    for (Int32 j = 0; j < cases.Length; ++j)
                    {
                        if (observation.Case != cases[j])
                            continue;

                        Double x = plot.X(i + (j == 0 ? -0.12d : 0.12d));
                        Double y = plot.Y(observation.HammingDistance);

                        plot.Rectangle(x - 5.0d, y - 5.0d, 10.0d, 10.0d, colors[j]);

                        break;
                    }
                }
            }

            plot.HorizontalReference(outputBits / 2.0d, plot.ReferenceColor, 2.0d, dashed: true);

            plot.EndPlot();

            plot.DrawBorder();
            plot.DrawAxes();
            plot.DrawBottomLegend();
        }

        private static void PlotWs(String path, Int32 outputBits, StructuralResult windowResult)
        {
            String[] values = ["0001", "0100", "8001", "FFFF"];
            String[] colors = ["#1f77b4", "#d62728", "#2ca02c", "#9467bd"];

            List<(Double X, Double Y)>[] series = [[], [], [], []];
            Int32 maximumOffset = 0;

            foreach (StructuralObservation observation in windowResult.Observations)
            {
                String[] parts = observation.Case.Split(':');
                Int32 offset = Int32.Parse(parts[1], CultureInfo.InvariantCulture);
                String value = parts[3];

                maximumOffset = Math.Max(maximumOffset, offset);

                for (Int32 i = 0; i < values.Length; ++i)
                {
                    if (value == values[i])
                    {
                        series[i].Add((offset, observation.HammingDistance));
                        break;
                    }
                }
            }

            maximumOffset = Math.Max(1, maximumOffset);

            foreach (List<(Double X, Double Y)> points in series)
                points.Sort((left, right) => left.X.CompareTo(right.X));

            using SvgPlot plot = new(path, 1200, 900);

            plot.PlotArea(150.0d, 170.0d, 920.0d, 620.0d);
            plot.DataRange(0.0d, maximumOffset, 0.0d, outputBits);

            plot.Title("WINDOW SENSITIVITY");
            plot.Subtitle("Hamming distance for shifted 16-bit windows; dashed line indicates the ideal mean.");
            plot.Metadata(
                $"Output Bits: {outputBits:N0} | Probes: {windowResult.Probes:N0}",
                $"Mean: {windowResult.MeanHamming:0.###} | Minimum: {windowResult.MinimumHamming:N0} | Maximum: {windowResult.MaximumHamming:N0}"
            );

            plot.PrepareLayout();

            Double left = plot.PlotLeft;
            Double top = plot.PlotTop;
            Double width = plot.PlotWidth;
            Double gap = 35.0d;
            Double panelHeight = (plot.PlotHeight - 3.0d * gap) / 4.0d;
            Double idealMean = outputBits / 2.0d;
            Int32 xIntervals = Math.Min(8, maximumOffset);

            for (Int32 i = 0; i < values.Length; ++i)
            {
                Double panelTop = top + (i * (panelHeight + gap));
                Double panelBottom = panelTop + panelHeight;

                plot.Text(left, panelTop - 10.0d, $"0x{values[i]}", 14, colors[i], true);

                for (Int32 tick = 0; tick <= 4; ++tick)
                {
                    Double value = outputBits * tick / 4.0d;
                    Double y = panelBottom - value / outputBits * panelHeight;

                    plot.Line(left, y, left + width, y, plot.GridColor);
                    plot.Label(left - 12.0d, y + 4.0d, value.ToString("0", CultureInfo.InvariantCulture), 12, "end");
                }

                for (Int32 tick = 0; tick <= xIntervals; ++tick)
                {
                    Double value = (Double)maximumOffset * tick / xIntervals;
                    Double x = left + value / maximumOffset * width;

                    plot.Line(x, panelTop, x, panelBottom, plot.GridColor);

                    if (i == values.Length - 1)
                        plot.Label(x, panelBottom + 21.0d, value.ToString("0", CultureInfo.InvariantCulture), 12, "middle");
                }

                Double referenceY = panelBottom - idealMean / outputBits * panelHeight;
                plot.Line(left, referenceY, left + width, referenceY, plot.ReferenceColor, 2.0d, "7 6");

                List<(Double X, Double Y)> points = series[i];

                for (Int32 j = 1; j < points.Count; ++j)
                {
                    Double x1 = left + points[j - 1].X / maximumOffset * width;
                    Double y1 = panelBottom - points[j - 1].Y / outputBits * panelHeight;
                    Double x2 = left + points[j].X / maximumOffset * width;
                    Double y2 = panelBottom - points[j].Y / outputBits * panelHeight;

                    plot.Line(x1, y1, x2, y2, colors[i], 2.3d);
                }

                if (points.Count <= 128)
                {
                    foreach ((Double x, Double y) in points)
                    {
                        Double px = left + x / maximumOffset * width;
                        Double py = panelBottom - y / outputBits * panelHeight;

                        plot.Rectangle(px - 2.5d, py - 2.5d, 5.0d, 5.0d, colors[i]);
                    }
                }

                plot.Border(left, panelTop, width, panelHeight);
            }

            plot.Text(left + width / 2.0d, top + plot.PlotHeight + 65.0d, "Window Byte Offset", 14, plot.TextColor, true, "middle");
        }
        #endregion

        #region Records
        private readonly record struct StructuralObservation(
            String Family,
            String Case,
            String Configuration,
            String InputA,
            String InputB,
            String HashA,
            String HashB,
            Int32 HammingDistance,
            Boolean EqualDigest
        );

        private readonly record struct StructuralPair(
            String Family,
            String Case,
            Byte[] InputA,
            Byte[] InputB
        );

        private readonly record struct StructuralResult(
            String Family,
            Int32 Probes,
            Int32 Skipped,
            Double? MeanHamming,
            Int32? MinimumHamming,
            Int32? MaximumHamming,
            Int32 EqualDigests,
            String[] Notes,
            StructuralObservation[] Observations
        );
        #endregion
    }
}