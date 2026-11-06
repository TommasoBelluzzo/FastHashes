#region Using Directives
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Numerics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Linq;
#endregion

namespace FastHashes.Analysis
{
    /// <summary>Targeted structural edge-case probes; descriptive only, without global pass/fail thresholds.</summary>
    internal static class StructuralAnalysis
    {
        #region Setup
        private const Int32 MAXIMUM_KEY_LENGTH = 4096;
        private const Int32 MAXIMUM_PROBES = 20000;
        #endregion

        #region Methods
        public static void Validate(Int32 hashLength, Int32 keyLength)
        {
            if ((hashLength <= 0) || ((hashLength % 8) != 0))
                throw new NotSupportedException("Structural analysis requires a positive byte-aligned hash length.");
            if ((keyLength < 2) || (keyLength > MAXIMUM_KEY_LENGTH))
                throw new ArgumentOutOfRangeException(nameof(keyLength), "Structural analysis requires --key-length between 2 and 4096.");
            // Sparse and window experiments scale linearly in key length.
            Int64 estimate = 2L * 8 * keyLength + 4L * (keyLength - 1) + 100;
            if (estimate > MAXIMUM_PROBES)
                throw new ArgumentOutOfRangeException(nameof(keyLength), "Structural workload exceeds the probe cap; reduce --key-length.");
        }

        public static String Run(Hash hash, String hashName, UInt64? hashSeed, String hashVersion, Int32 keyLength, String outputDirectory = null)
        {
            Validate(hash.Length, keyLength);
            List<StructuralResult> results = [];
            List<StructuralObservation> observations = [];
            Evaluate(hash, StructuralInputGenerator.Sparse(keyLength), "sparse", results, observations);
            Evaluate(hash, StructuralInputGenerator.TwoBytes(keyLength), "twobytes", results, observations);
            Evaluate(hash, StructuralInputGenerator.Cyclic(keyLength), "cyclic", results, observations);
            Evaluate(hash, StructuralInputGenerator.Permutation(keyLength), "permutation", results, observations);
            Evaluate(hash, StructuralInputGenerator.Window(keyLength), "window", results, observations);
            Evaluate(hash, StructuralInputGenerator.Zeroes(keyLength), "zeroes", results, observations);
            Evaluate(hash, StructuralInputGenerator.Boundaries(keyLength), "boundaries", results, observations);
            EvaluateSeeds(hashName, keyLength, hash.Length, results, observations);

            return Utilities.WriteArtifacts(hashName, directory => WriteReports(directory, hashName, hashSeed, hashVersion, keyLength, hash.Length, results, observations), outputDirectory);
        }

        private static void Evaluate(Hash hash, IEnumerable<StructuralPair> pairs, String family, List<StructuralResult> results, List<StructuralObservation> observations)
        {
            StructuralResult result = new(family);
            foreach (StructuralPair pair in pairs)
                Measure(hash, pair, "selected-hash-configuration", result, observations);
            results.Add(result);
        }

        private static void EvaluateSeeds(String hashName, Int32 keyLength, Int32 hashLength, List<StructuralResult> results, List<StructuralObservation> observations)
        {
            StructuralResult result = new("seed");
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
                    result.Skipped++;
                    result.Notes.Add($"seed {seed}: {exception.GetType().Name}: {exception.Message}");
                    continue;
                }
                if (seeded.Length != hashLength)
                    throw new InvalidOperationException("A seed configuration unexpectedly changed the hash output length.");
                foreach (StructuralPair pair in StructuralInputGenerator.SeedProbes(keyLength))
                    Measure(seeded, pair, seed.ToString(CultureInfo.InvariantCulture), result, observations);
            }
            results.Add(result);
        }

        private static void Measure(Hash hash, StructuralPair pair, String configuration, StructuralResult result, List<StructuralObservation> observations)
        {
            if (pair.InputA.AsSpan().SequenceEqual(pair.InputB))
                throw new InvalidOperationException($"Structural probe '{pair.Family}/{pair.Case}' contains identical input keys.");

            Byte[] a = hash.ComputeHashBytes(pair.InputA);
            Byte[] b = hash.ComputeHashBytes(pair.InputB);
            if ((a == null) || (b == null) || (a.Length != hash.Length / 8) || (b.Length != hash.Length / 8))
                throw new InvalidOperationException("The hash returned an invalid digest length.");

            Int32 distance = 0;
            for (Int32 i = 0; i < a.Length; ++i)
                distance += BitOperations.PopCount((UInt32)(a[i] ^ b[i]));

            ++result.Probes;
            result.TotalDistance += distance;
            result.MinimumDistance = Math.Min(result.MinimumDistance, distance);
            result.MaximumDistance = Math.Max(result.MaximumDistance, distance);
            if (distance == 0)
                ++result.EqualDigests;
            observations.Add(new StructuralObservation(pair.Family, pair.Case, configuration, pair.InputA.Length, pair.InputB.Length, distance, distance == 0,
                Convert.ToHexString(pair.InputA), Convert.ToHexString(pair.InputB), Convert.ToHexString(a), Convert.ToHexString(b)));
        }

        private static void WriteReports(String directory, String hashName, UInt64? hashSeed, String hashVersion, Int32 keyLength, Int32 hashBits, List<StructuralResult> results, List<StructuralObservation> observations)
        {
            UTF8Encoding utf8 = new(false);
            using (StreamWriter writer = new(Path.Combine(directory, "structural-summary.csv"), false, utf8))
            {
                writer.WriteLine("Family,Probes,SkippedConfigurations,EqualDigests,MinimumHamming,MeanHamming,MaximumHamming,IdealHamming");
                foreach (StructuralResult result in results)
                    writer.WriteLine(String.Join(",", result.Family, result.Probes, result.Skipped, result.EqualDigests,
                        result.Probes == 0 ? "" : result.MinimumDistance.ToString(CultureInfo.InvariantCulture),
                        result.Probes == 0 ? "" : F((Double)result.TotalDistance / result.Probes),
                        result.Probes == 0 ? "" : result.MaximumDistance.ToString(CultureInfo.InvariantCulture), F(hashBits / 2.0d)));
            }

            foreach (String family in new[] { "sparse", "twobytes", "cyclic", "permutation", "window", "zeroes", "seed", "boundaries" })
            {
                using StreamWriter writer = new(Path.Combine(directory, $"structural-{family}.csv"), false, utf8);
                writer.WriteLine("Case,Configuration,InputLengthA,InputLengthB,HammingDistance,EqualDigest,InputAHex,InputBHex,HashAHex,HashBHex");
                foreach (StructuralObservation item in observations)
                {
                    if (item.Family != family)
                        continue;
                    writer.WriteLine(String.Join(",", item.Case, item.Configuration, item.InputLengthA, item.InputLengthB, item.HammingDistance,
                        item.EqualDigest, item.InputAHex, item.InputBHex, item.HashAHex, item.HashBHex));
                }
            }

            List<StructuralSvg.WindowObservation> windowObservations = observations
                .Where(item => item.Family == "window")
                .Select(item =>
                {
                    if (!TryParseWindowCase(item.Case, out Int32 offset, out String value))
                        throw new InvalidOperationException($"Unexpected window case format: {item.Case}");
                    return new StructuralSvg.WindowObservation(offset, value, item.HammingDistance);
                })
                .OrderBy(item => item.Offset)
                .ThenBy(item => item.Value, StringComparer.Ordinal)
                .ToList();

            if (windowObservations.Count > 0)
                StructuralSvg.WriteWindowProfile(Path.Combine(directory, "structural-window.svg"), hashName, hashBits, keyLength, windowObservations);

            String[] files = ["structural-summary.csv", "structural-sparse.csv", "structural-twobytes.csv", "structural-cyclic.csv",
                "structural-permutation.csv", "structural-window.csv", "structural-zeroes.csv", "structural-seed.csv",
                "structural-boundaries.csv", "structural-window.svg", "structural.json", "structural.txt"];
            var metadata = new
            {
                SchemaVersion = 1,
                Analysis = "structural",
                Status = "Completed",
                QualityAssessment = "DescriptiveOnly",
                HashName = hashName,
                HashSeed = hashSeed,
                HashLengthBits = hashBits,
                KeyLengthBytes = keyLength,
                HashEvaluations = observations.Count * 2L,
                Method = "Deterministic input-pair stress probes; no global significance test or pass/fail threshold",
                Caveat = "Mean Hamming distances in small or dependent constructed families cannot independently establish statistical significance.",
                Families = results.ConvertAll(result => new
                {
                    result.Family, result.Probes, result.Skipped, result.EqualDigests,
                    MinimumHamming = result.Probes == 0 ? (Int32?)null : result.MinimumDistance,
                    MeanHamming = result.Probes == 0 ? (Double?)null : (Double)result.TotalDistance / result.Probes,
                    MaximumHamming = result.Probes == 0 ? (Int32?)null : result.MaximumDistance,
                    Notes = result.Notes
                }),
                UtcTimestamp = DateTimeOffset.UtcNow,
                LibraryVersion = hashVersion,
                Runtime = RuntimeInformation.FrameworkDescription,
                OperatingSystem = RuntimeInformation.OSDescription,
                ProcessArchitecture = RuntimeInformation.ProcessArchitecture.ToString(),
                OutputFiles = files
            };
            File.WriteAllText(Path.Combine(directory, "structural.json"), JsonSerializer.Serialize(metadata, new JsonSerializerOptions { WriteIndented = true }) + "\n", utf8);
            StringBuilder report = new();
            report.AppendLine("Analysis: structural (AN-005)").AppendLine($"Hash: {hashName} ({hashBits} bits)")
                .AppendLine($"Hash seed: {(hashSeed.HasValue ? hashSeed.Value.ToString(CultureInfo.InvariantCulture) : "default configuration")}")
                .AppendLine($"Key length: {keyLength} bytes")
                .AppendLine($"Hash evaluations: {observations.Count * 2L}")
                .AppendLine("Interpretation: targeted structural diagnostics, not a statistical quality certification.");
            foreach (StructuralResult result in results)
            {
                report.AppendLine().AppendLine($"[{result.Family.ToUpperInvariant()}]")
                    .AppendLine($"Probes: {result.Probes}; skipped seed configurations: {result.Skipped}; identical digests: {result.EqualDigests}")
                    .AppendLine($"Hamming distance: min {((result.Probes == 0) ? "n/a" : result.MinimumDistance.ToString(CultureInfo.InvariantCulture))}, mean {((result.Probes == 0) ? "n/a" : F((Double)result.TotalDistance / result.Probes))}, max {((result.Probes == 0) ? "n/a" : result.MaximumDistance.ToString(CultureInfo.InvariantCulture))}; ideal marginal mean {F(hashBits / 2.0d)}");
                foreach (String note in result.Notes)
                    report.AppendLine($"Note: {note}");
            }
            File.WriteAllText(Path.Combine(directory, "structural.txt"), report.ToString(), utf8);
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

        private static String F(Double value)
        {
            return value.ToString("G17", CultureInfo.InvariantCulture);
        }
        #endregion

        private sealed class StructuralResult
        {
            public String Family { get; }
            public Int32 Probes { get; set; }
            public Int32 Skipped { get; set; }
            public Int32 EqualDigests { get; set; }
            public Int64 TotalDistance { get; set; }
            public Int32 MinimumDistance { get; set; } = Int32.MaxValue;
            public Int32 MaximumDistance { get; set; }
            public List<String> Notes { get; } = [];

            public StructuralResult(String family)
            {
                Family = family;
            }
        }

        private sealed record StructuralObservation(String Family, String Case, String Configuration, Int32 InputLengthA,
            Int32 InputLengthB, Int32 HammingDistance, Boolean EqualDigest, String InputAHex, String InputBHex, String HashAHex, String HashBHex);
    }
}
