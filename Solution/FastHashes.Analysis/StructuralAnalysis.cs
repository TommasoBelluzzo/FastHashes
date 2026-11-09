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

        #region Plots
        private static void PlotWs(String path, Int32 keysLength, Int32 outputBits, StructuralResult result, List<StructuralObservation> observations)
        {
            String[] values = ["0001", "0100", "8001", "FFFF"];
            String[] colors = ["#1f77b4", "#d62728", "#2ca02c", "#9467bd"];
            Int32 maximumOffset = 0;

            foreach (StructuralObservation observation in observations)
            {
                if (observation.Family != "window")
                    continue;

                if (!TryParseWindowCase(observation.Case, out Int32 offset, out _))
                    throw new InvalidOperationException($"Unexpected window case format: {observation.Case}");

                maximumOffset = Math.Max(maximumOffset, offset);
            }

            if (result.Probes == 0)
                throw new InvalidOperationException("Structural window analysis produced no observations.");

            using SvgPlot plot = new(path, 1200, 790);

            plot.PlotArea(125.0d, 180.0d, 950.0d, 455.0d);
            plot.DataRange(0.0d, Math.Max(1, maximumOffset), 0.0d, outputBits);
            plot.ConfigureDiscreteXAxis("Window Byte Offset", 0.0d, Math.Max(1, maximumOffset), intervals: Math.Min(8, Math.Max(1, maximumOffset)), format: "0");
            plot.ConfigureDiscreteYAxis("Changed Output Bits", 0.0d, outputBits, intervals: 4, format: "0");
            plot.ConfigureBottomLegend([
                new SvgPlot.LegendItem("0x0001", colors[0], true),
                new SvgPlot.LegendItem("0x0100", colors[1], true),
                new SvgPlot.LegendItem("0x8001", colors[2], true),
                new SvgPlot.LegendItem("0xFFFF", colors[3], true),
                new SvgPlot.LegendItem("Ideal Mean", plot.ReferenceColor, true, true)
            ]);

            plot.Title("WINDOW SENSITIVITY");
            plot.Subtitle("Output Hamming distance for 16-bit windows shifted across the input key.");
            plot.Metadata(
                $"Key Bytes: {keysLength:N0} | Output Bits: {outputBits:N0} | Window Observations: {result.Probes:N0}",
                $"Mean Hamming: {result.MeanHamming:0.###} | Minimum: {result.MinimumHamming:N0} | Maximum: {result.MaximumHamming:N0}"
            );

            plot.PrepareLayout();
            plot.DrawGrid();
            plot.BeginPlot();

            for (Int32 i = 0; i < values.Length; ++i)
            {
                List<(Double X, Double Y)> points = [];

                foreach (StructuralObservation observation in observations)
                {
                    if (observation.Family != "window")
                        continue;

                    if (!TryParseWindowCase(observation.Case, out Int32 offset, out String value))
                        throw new InvalidOperationException($"Unexpected window case format: {observation.Case}");

                    if (value == values[i])
                        points.Add((offset, observation.HammingDistance));
                }

                points.Sort((left, right) => left.X.CompareTo(right.X));
                plot.Polyline(points, colors[i], 2.3d);

                if (points.Count <= 128)
                {
                    foreach ((Double x, Double y) in points)
                        plot.Rectangle(plot.X(x) - 2.5d, plot.Y(y) - 2.5d, 5.0d, 5.0d, colors[i]);
                }
            }

            plot.HorizontalReference(outputBits / 2.0d, plot.ReferenceColor, 2.0d, dashed: true);
            plot.EndPlot();

            plot.DrawBorder();
            plot.DrawAxes();
            plot.DrawBottomLegend();
        }
        #endregion

        #region Methods
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

            List<StructuralResult> results = [];
            List<StructuralObservation> observations = [];

            Evaluate(hash, "sparse", StructuralInputGenerator.Sparse(keysLength), results, observations);
            Evaluate(hash, "twobytes", StructuralInputGenerator.TwoBytes(keysLength), results, observations);
            Evaluate(hash, "cyclic", StructuralInputGenerator.Cyclic(keysLength), results, observations);
            Evaluate(hash, "permutation", StructuralInputGenerator.Permutation(keysLength), results, observations);
            StructuralResult windowResult = Evaluate(hash, "window", StructuralInputGenerator.Window(keysLength), results, observations);
            Evaluate(hash, "zeroes", StructuralInputGenerator.Zeroes(keysLength), results, observations);
            Evaluate(hash, "boundaries", StructuralInputGenerator.Boundaries(keysLength), results, observations);
            EvaluateSeeds(hashName, keysLength, outputBits, results, observations);

            Int64 probes = observations.Count;
            Int64 hashEvaluations = checked(probes * 2L);
            Int64 equalDigests = 0;

            foreach (StructuralResult result in results)
                equalDigests += result.EqualDigests;

            Dictionary<String, Object> metadata = Utilities.CreateMetadata("structural", config);
            metadata["InputBits"] = inputBits;
            metadata["OutputBits"] = outputBits;
            metadata["Probes"] = probes;
            metadata["HashEvaluations"] = hashEvaluations;
            metadata["EqualDigests"] = equalDigests;
            metadata["Families"] = results;
            metadata["OutputFiles"] = new[] {
                "structural.json", "structural-summary.csv", "structural-families.csv", "structural-ws.svg"
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
                    writer.WriteLine("Family,Probes,SkippedConfigurations,EqualDigests,MinimumHamming,MeanHamming,MaximumHamming");

                    foreach (StructuralResult result in results)
                    {
                        writer.WriteLine(String.Join(",",
                            result.Family,
                            Utilities.FormatCsvInteger(result.Probes),
                            Utilities.FormatCsvInteger(result.Skipped),
                            Utilities.FormatCsvInteger(result.EqualDigests),
                            result.MinimumHamming.HasValue ? Utilities.FormatCsvInteger(result.MinimumHamming.Value) : String.Empty,
                            result.MeanHamming.HasValue ? Utilities.FormatCsvDouble(result.MeanHamming.Value) : String.Empty,
                            result.MaximumHamming.HasValue ? Utilities.FormatCsvInteger(result.MaximumHamming.Value) : String.Empty
                        ));
                    }
                }

                filePath = Path.Combine(directory, "structural-families.csv");

                using (StreamWriter writer = new(filePath, false, encoding))
                {
                    writer.WriteLine("Family,Case,Configuration,InputLengthA,InputLengthB,HammingDistance,EqualDigest,InputAHex,InputBHex,HashAHex,HashBHex");

                    foreach (StructuralObservation observation in observations)
                    {
                        writer.WriteLine(String.Join(",",
                            observation.Family,
                            observation.Case,
                            observation.Configuration,
                            Utilities.FormatCsvInteger(observation.InputLengthA),
                            Utilities.FormatCsvInteger(observation.InputLengthB),
                            Utilities.FormatCsvInteger(observation.HammingDistance),
                            observation.EqualDigest ? "true" : "false",
                            observation.InputAHex,
                            observation.InputBHex,
                            observation.HashAHex,
                            observation.HashBHex
                        ));
                    }
                }

                filePath = Path.Combine(directory, "structural-window.svg");
                PlotWs(filePath, keysLength, outputBits, windowResult, observations);
            }

            return Utilities.WriteArtifacts(hashName, writeFunction, outputDirectory);
        }

        private static StructuralResult Evaluate(Hash hash, String family, IEnumerable<StructuralPair> pairs,
            List<StructuralResult> results, List<StructuralObservation> observations)
        {
            Int32 probes = 0;
            Int32 equalDigests = 0;
            Int64 totalDistance = 0;
            Int32 minimumDistance = Int32.MaxValue;
            Int32 maximumDistance = 0;

            foreach (StructuralPair pair in pairs)
            {
                Int32 distance = Measure(hash, pair, "selected-hash-configuration", observations);

                ++probes;
                totalDistance += distance;
                minimumDistance = Math.Min(minimumDistance, distance);
                maximumDistance = Math.Max(maximumDistance, distance);

                if (distance == 0)
                    ++equalDigests;
            }

            StructuralResult result = new(
                family, probes, 0, equalDigests,
                probes == 0 ? null : minimumDistance,
                probes == 0 ? null : (Double)totalDistance / probes,
                probes == 0 ? null : maximumDistance,
                []
            );

            results.Add(result);
            return result;
        }

        private static void EvaluateSeeds(String hashName, Int32 keysLength, Int32 outputBits,
            List<StructuralResult> results, List<StructuralObservation> observations)
        {
            Int32 probes = 0;
            Int32 skipped = 0;
            Int32 equalDigests = 0;
            Int64 totalDistance = 0;
            Int32 minimumDistance = Int32.MaxValue;
            Int32 maximumDistance = 0;
            List<String> notes = [];
            UInt64[] seeds = [0ul, 1ul, 0x5555555555555555ul, 0xAAAAAAAAAAAAAAAAul, UInt64.MaxValue];

            foreach (UInt64 seed in seeds)
            {
                Hash seeded;

                try
                {
                    seeded = Hash.Create(hashName, seed);
                }
                catch (Exception exception) when (exception is ArgumentException || exception is NotSupportedException || exception is OverflowException)
                {
                    ++skipped;
                    notes.Add($"seed {seed}: {exception.GetType().Name}: {exception.Message}");
                    continue;
                }

                if (seeded.Length != outputBits)
                    throw new InvalidOperationException("A seed configuration unexpectedly changed the hash output length.");

                foreach (StructuralPair pair in StructuralInputGenerator.SeedProbes(keysLength))
                {
                    Int32 distance = Measure(seeded, pair, seed.ToString(CultureInfo.InvariantCulture), observations);

                    ++probes;
                    totalDistance += distance;
                    minimumDistance = Math.Min(minimumDistance, distance);
                    maximumDistance = Math.Max(maximumDistance, distance);

                    if (distance == 0)
                        ++equalDigests;
                }
            }

            results.Add(new StructuralResult(
                "seed", probes, skipped, equalDigests,
                probes == 0 ? null : minimumDistance,
                probes == 0 ? null : (Double)totalDistance / probes,
                probes == 0 ? null : maximumDistance,
                [.. notes]
            ));
        }

        private static Int32 Measure(Hash hash, StructuralPair pair, String configuration,
            List<StructuralObservation> observations)
        {
            if (pair.InputA.AsSpan().SequenceEqual(pair.InputB))
                throw new InvalidOperationException($"Structural probe '{pair.Family}/{pair.Case}' contains identical input keys.");

            Byte[] digestA = hash.ComputeHashBytes(pair.InputA);
            Byte[] digestB = hash.ComputeHashBytes(pair.InputB);
            Int32 distance = 0;

            for (Int32 i = 0; i < digestA.Length; ++i)
                distance += BitOperations.PopCount((UInt32)(digestA[i] ^ digestB[i]));

            observations.Add(new StructuralObservation(
                pair.Family, pair.Case, configuration, pair.InputA.Length, pair.InputB.Length,
                distance, distance == 0, Convert.ToHexString(pair.InputA), Convert.ToHexString(pair.InputB),
                Convert.ToHexString(digestA), Convert.ToHexString(digestB)
            ));

            return distance;
        }

        private static Boolean TryParseWindowCase(String value, out Int32 offset, out String windowValue)
        {
            offset = 0;
            windowValue = String.Empty;
            String[] parts = value.Split(':');

            if ((parts.Length != 4) || (parts[0] != "offset") || (parts[2] != "value"))
                return false;

            if (!Int32.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out offset))
                return false;

            windowValue = parts[3];
            return true;
        }
        #endregion

        #region Records
        private readonly record struct StructuralObservation(
            String Family,
            String Case,
            String Configuration,
            Int32 InputLengthA,
            Int32 InputLengthB,
            Int32 HammingDistance,
            Boolean EqualDigest,
            String InputAHex,
            String InputBHex,
            String HashAHex,
            String HashBHex
        );

        private readonly record struct StructuralResult(
            String Family,
            Int32 Probes,
            Int32 Skipped,
            Int32 EqualDigests,
            Int32? MinimumHamming,
            Double? MeanHamming,
            Int32? MaximumHamming,
            String[] Notes
        );
        #endregion
    }
}