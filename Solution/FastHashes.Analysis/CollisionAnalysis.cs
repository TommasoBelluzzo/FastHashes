#region Using Directives
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
#endregion

namespace FastHashes.Analysis
{
    /// <summary>Full-width collision and projected-bucket occupancy analysis over three deterministic corpora.</summary>
    internal static class CollisionAnalysis
    {
        #region Setup
        private const Int32 BUCKET_BITS = 16;
        private const Int32 BUCKET_COUNT = 1 << BUCKET_BITS;
        private const Int32 MAXIMUM_EXAMPLES_PER_TYPE = 1000;
        #endregion

        #region Methods
        public static void Validate(Int32 hashLength, Int32 corpusSize, Int32 keyLength)
        {
            if ((hashLength < BUCKET_BITS) || ((hashLength % 8) != 0))
                throw new NotSupportedException("Collision analysis requires a byte-aligned hash length of at least 16 bits.");

            if ((corpusSize < 1) || (corpusSize > 1000000))
                throw new ArgumentOutOfRangeException(nameof(corpusSize), "--corpus-size must be between 1 and 1000000.");

            if ((keyLength < 1) || (keyLength > 4096))
                throw new ArgumentOutOfRangeException(nameof(keyLength), "Collision random-byte keys require 1 to 4096 bytes.");

            if (!File.Exists(WordsPath()))
                throw new FileNotFoundException("The shared Words.txt corpus was not found beside the application (Data/Words.txt).", WordsPath());
        }

        public static String Run(Hash hash, String hashName, UInt64? hashSeed, String hashVersion, UInt64 generatorSeed, Int32 corpusSize, Int32 keyLength, String outputDirectory = null)
        {
            Validate(hash.Length, corpusSize, keyLength);
            List<CorpusResult> results = [];

            CorpusResult words = new("words-padded", hash.Length);
            foreach (String word in File.ReadLines(WordsPath()))
            {
                Byte[] input = Encoding.UTF8.GetBytes(word);
                Byte[] padded = new Byte[input.Length + 5];
                Array.Fill(padded, (Byte)'!');
                input.CopyTo(padded, 0);
                for (Int32 suffix = 0; suffix <= 5; ++suffix)
                    Observe(words, hash, padded.AsSpan(0, input.Length + suffix).ToArray());
            }
            if (words.TotalInputs == 0)
                throw new InvalidOperationException("The shared Words.txt corpus contains no entries.");
            results.Add(words);

            CorpusResult numbers = new("numbers", hash.Length);
            for (Int32 i = 1; i <= corpusSize; ++i)
                Observe(numbers, hash, Encoding.UTF8.GetBytes(i.ToString(CultureInfo.InvariantCulture)));
            results.Add(numbers);

            CorpusResult random = new("random-bytes", hash.Length);
            RandomSplitMix generator = new(generatorSeed);
            Byte[] buffer = new Byte[keyLength];
            // Exactly corpusSize distinct inputs are requested; duplicates are regenerated.
            // A one-byte key cannot provide more than 256 distinct values.
            if (keyLength == 1 && corpusSize > 256)
                throw new ArgumentOutOfRangeException(nameof(corpusSize), "A one-byte random corpus cannot contain more than 256 distinct keys.");

            Int64 attempts = 0;
            while (random.DistinctInputs < corpusSize)
            {
                if (++attempts > (Int64)corpusSize * 100 + 1000)
                    throw new InvalidOperationException("Could not generate the requested number of distinct random keys.");
                generator.NextBytes(buffer);
                Observe(random, hash, buffer);
            }
            results.Add(random);

            // Everything above, including file loading and hashing, completes before the output directory is created.
            return Utilities.WriteArtifacts(hashName, directory => WriteReports(directory, hashName, hashSeed, hashVersion, generatorSeed, corpusSize, keyLength, hash.Length, results), outputDirectory);
        }

        private static String WordsPath()
        {
            return Path.Combine(AppContext.BaseDirectory, "Data", "Words.txt");
        }

        private static void Observe(CorpusResult corpus, Hash hash, Byte[] input)
        {
            Observe(corpus, hash, (ReadOnlySpan<Byte>)input);
        }

        private static void Observe(CorpusResult corpus, Hash hash, ReadOnlySpan<Byte> input)
        {
            ++corpus.TotalInputs;
            String inputHex = Convert.ToHexString(input);
            if (!corpus.Inputs.Add(inputHex))
            {
                ++corpus.DuplicateInputs;
                return;
            }

            ++corpus.DistinctInputs;
            Byte[] digest = hash.ComputeHashBytes(input);
            if (digest == null || digest.Length != corpus.HashBits / 8)
                throw new InvalidOperationException("Hash returned an invalid digest length.");
            String hashHex = Convert.ToHexString(digest);
            Int32 bucket = digest[0] | (digest[1] << 8);

            if (corpus.FullGroups.TryGetValue(hashHex, out CollisionGroup group))
            {
                corpus.FullCollidingPairs += group.Count;
                ++corpus.FullDuplicateOutputs;
                if (corpus.FullExamples < MAXIMUM_EXAMPLES_PER_TYPE)
                {
                    corpus.Examples.Add(new CollisionExample(corpus.Name, "Full", hashHex, bucket, group.FirstInput, inputHex));
                    ++corpus.FullExamples;
                }
                group.Count++;
            }
            else
                corpus.FullGroups.Add(hashHex, new CollisionGroup(inputHex));

            if (corpus.Buckets[bucket] > 0 && corpus.ProjectedExamples < MAXIMUM_EXAMPLES_PER_TYPE)
            {
                corpus.Examples.Add(new CollisionExample(corpus.Name, "Projected16", hashHex, bucket, corpus.FirstBucketInputs[bucket], inputHex));
                ++corpus.ProjectedExamples;
            }
            if (corpus.Buckets[bucket] == 0)
                corpus.FirstBucketInputs[bucket] = inputHex;
            corpus.ProjectedCollidingPairs += corpus.Buckets[bucket];
            ++corpus.Buckets[bucket];
        }

        private static void WriteReports(String directory, String hashName, UInt64? hashSeed, String hashVersion, UInt64 generatorSeed, Int32 corpusSize, Int32 keyLength, Int32 hashBits, List<CorpusResult> results)
        {
            using (StreamWriter writer = CreateWriter(Path.Combine(directory, "collision-summary.csv")))
            {
                writer.WriteLine("Corpus,TotalInputs,DistinctInputs,DuplicateInputs,FullDuplicateOutputs,ExpectedFullDuplicateOutputs,FullCollidingPairs,ExpectedFullPairs,ProjectedBits,Buckets,EmptyBuckets,SingletonBuckets,CollisionBuckets,MaximumBucketLoad,ProjectedDuplicateOutputs,ExpectedProjectedDuplicateOutputs,ProjectedCollisionPairs,ExpectedProjectedPairs,ChiSquare,MeanLoad,LoadVariance");
                foreach (CorpusResult result in results)
                {
                    BucketStats stats = ComputeStats(result);
                    Double n = result.DistinctInputs;
                    writer.WriteLine(String.Join(",", result.Name, result.TotalInputs, result.DistinctInputs, result.DuplicateInputs,
                        result.FullDuplicateOutputs, F(ExpectedDuplicateOutputs(n, hashBits)), result.FullCollidingPairs, F(ExpectedPairs(n, hashBits)), BUCKET_BITS, BUCKET_COUNT,
                        stats.Empty, stats.Singleton, stats.Colliding, stats.Maximum,
                        result.DistinctInputs - BUCKET_COUNT + stats.Empty, F(ExpectedDuplicateOutputs(n, BUCKET_BITS)),
                        result.ProjectedCollidingPairs, F(ExpectedPairs(n, BUCKET_BITS)), F(stats.ChiSquare), F(stats.Mean), F(stats.Variance)));
                }
            }

            using (StreamWriter writer = CreateWriter(Path.Combine(directory, "collision-buckets.csv")))
            {
                writer.WriteLine("Corpus,Bucket,Load");
                foreach (CorpusResult result in results)
                    for (Int32 bucket = 0; bucket < BUCKET_COUNT; ++bucket)
                        writer.WriteLine($"{result.Name},{bucket},{result.Buckets[bucket]}");
            }

            using (StreamWriter writer = CreateWriter(Path.Combine(directory, "collision-examples.csv")))
            {
                writer.WriteLine("Corpus,CollisionType,HashHex,ProjectedBucket,InputAHex,InputBHex");
                foreach (CorpusResult result in results)
                    foreach (CollisionExample example in result.Examples)
                        writer.WriteLine($"{example.Corpus},{example.Type},{example.Hash},{example.Bucket},{example.InputA},{example.InputB}");
            }

            foreach (CorpusResult result in results)
            {
                CollisionSvg.WriteMap(Path.Combine(directory, $"collision-{result.Name}-linear.svg"), result.Name, result.Buckets, false, result.DistinctInputs);
                CollisionSvg.WriteMap(Path.Combine(directory, $"collision-{result.Name}-hilbert.svg"), result.Name, result.Buckets, true, result.DistinctInputs);
            }

            List<String> files = ["collision-summary.csv", "collision-buckets.csv", "collision-examples.csv"];
            foreach (CorpusResult result in results)
            {
                files.Add($"collision-{result.Name}-linear.svg");
                files.Add($"collision-{result.Name}-hilbert.svg");
            }
            files.Add("collision.json");
            files.Add("collision.txt");

            var metadata = new
            {
                SchemaVersion = 1,
                Analysis = "collision",
                Status = "Completed",
                QualityAssessment = "StatisticalEvidenceOnly",
                HashName = hashName,
                HashSeed = hashSeed,
                HashLengthBits = hashBits,
                GeneratorAlgorithm = "SplitMix64",
                GeneratorSeed = generatorSeed,
                CorpusSize = corpusSize,
                RandomKeyLengthBytes = keyLength,
                ProjectionBits = BUCKET_BITS,
                Projection = "First two bytes of ComputeHashBytes interpreted as unsigned little-endian",
                DuplicateInputPolicy = "Exact duplicate input bytes are excluded from collision statistics",
                WordCorpus = "Data/Words.txt, UTF-8, suffixes of zero to five ASCII exclamation marks",
                ExamplesPerCorpusPerTypeLimit = MAXIMUM_EXAMPLES_PER_TYPE,
                ExpectedModel = "Independent uniform hash outputs; expected pair collisions N(N-1)/(2*2^bits)",
                UtcTimestamp = DateTimeOffset.UtcNow,
                LibraryVersion = hashVersion,
                Runtime = RuntimeInformation.FrameworkDescription,
                OperatingSystem = RuntimeInformation.OSDescription,
                ProcessArchitecture = RuntimeInformation.ProcessArchitecture.ToString(),
                Corpora = results.ConvertAll(result =>
                {
                    BucketStats stats = ComputeStats(result);
                    return new
                    {
                        result.Name, result.TotalInputs, result.DistinctInputs, result.DuplicateInputs,
                        result.FullDuplicateOutputs, result.FullCollidingPairs,
                        ExpectedFullCollidingPairs = ExpectedPairs(result.DistinctInputs, hashBits),
                        ExpectedFullDuplicateOutputs = ExpectedDuplicateOutputs(result.DistinctInputs, hashBits),
                        ProjectedDuplicateOutputs = result.DistinctInputs - BUCKET_COUNT + stats.Empty,
                        result.ProjectedCollidingPairs,
                        ExpectedProjectedCollidingPairs = ExpectedPairs(result.DistinctInputs, BUCKET_BITS),
                        ExpectedProjectedDuplicateOutputs = ExpectedDuplicateOutputs(result.DistinctInputs, BUCKET_BITS),
                        stats.Empty, stats.Singleton, stats.Colliding, stats.Maximum,
                        stats.Mean, stats.Variance, stats.ChiSquare
                    };
                }),
                OutputFiles = files
            };

            File.WriteAllText(Path.Combine(directory, "collision.json"), JsonSerializer.Serialize(metadata, new JsonSerializerOptions { WriteIndented = true }) + "\n", new UTF8Encoding(false));
            StringBuilder report = new("Analysis: collision (AN-003)\n");
            report.AppendLine($"Hash: {hashName} ({hashBits} bits)");
            report.AppendLine($"Hash seed: {(hashSeed.HasValue ? hashSeed.Value.ToString(CultureInfo.InvariantCulture) : "default configuration")}");
            report.AppendLine($"Generator seed: {generatorSeed}");
            report.AppendLine("Projection: digest low 16 bits, 65,536 buckets");
            foreach (CorpusResult result in results)
            {
                BucketStats stats = ComputeStats(result);
                report.AppendLine($"\n[{result.Name}]");
                report.AppendLine($"Distinct inputs: {result.DistinctInputs}; duplicate inputs excluded: {result.DuplicateInputs}");
                report.AppendLine($"Full duplicate outputs: {result.FullDuplicateOutputs}; full colliding pairs: {result.FullCollidingPairs}; ideal expected full duplicate outputs: {F(ExpectedDuplicateOutputs(result.DistinctInputs, hashBits))}; ideal expected full pairs: {F(ExpectedPairs(result.DistinctInputs, hashBits))}");
                report.AppendLine($"Projected duplicate outputs: {result.DistinctInputs - BUCKET_COUNT + stats.Empty}; ideal expected duplicates: {F(ExpectedDuplicateOutputs(result.DistinctInputs, BUCKET_BITS))}");
                report.AppendLine($"Projected colliding pairs: {result.ProjectedCollidingPairs}; ideal expected pairs: {F(ExpectedPairs(result.DistinctInputs, BUCKET_BITS))}");
                report.AppendLine($"Empty: {stats.Empty}; singleton: {stats.Singleton}; colliding buckets: {stats.Colliding}; maximum load: {stats.Maximum}");
                report.AppendLine($"Pearson chi-square occupancy statistic: {F(stats.ChiSquare)} (descriptive; no automatic pass/fail)");
            }
            report.AppendLine("\nNotes: Projected collisions are expected; maps are descriptive diagnostics, not statistical tests.");
            File.WriteAllText(Path.Combine(directory, "collision.txt"), report.ToString(), new UTF8Encoding(false));
        }

        private static BucketStats ComputeStats(CorpusResult result)
        {
            Int32 empty = 0, singleton = 0, colliding = 0, maximum = 0;
            Double mean = (Double)result.DistinctInputs / BUCKET_COUNT;
            Double squared = 0.0d;
            foreach (Int32 load in result.Buckets)
            {
                if (load == 0) ++empty;
                else if (load == 1) ++singleton;
                else ++colliding;
                maximum = Math.Max(maximum, load);
                Double difference = load - mean;
                squared += difference * difference;
            }
            return new BucketStats(empty, singleton, colliding, maximum, mean, squared / BUCKET_COUNT,
                mean == 0.0d ? 0.0d : squared / mean);
        }

        private static Double ExpectedPairs(Double count, Int32 bits)
        {
            return count * (count - 1.0d) * Math.Pow(2.0d, -bits - 1.0d);
        }

        private static Double ExpectedDuplicateOutputs(Double count, Int32 bits)
        {
            Double space = Math.Pow(2.0d, bits);
            if (count / space < 1.0e-6d)
                return ExpectedPairs(count, bits);

            Double probability = 1.0d / space;
            Double log = probability < 1.0e-4d
                ? -probability - probability * probability / 2.0d - probability * probability * probability / 3.0d
                : Math.Log(1.0d - probability);
            Double value = count * log;
            Double occupiedFraction = Math.Abs(value) < 1.0e-4d
                ? -value - value * value / 2.0d - value * value * value / 6.0d - value * value * value * value / 24.0d
                : 1.0d - Math.Exp(value);
            return Math.Max(0.0d, count - space * occupiedFraction);
        }

        private static StreamWriter CreateWriter(String path) => new(path, false, new UTF8Encoding(false));
        private static String F(Double value) => value.ToString("G17", CultureInfo.InvariantCulture);
        #endregion

        #region Nested Types
        private sealed class CorpusResult
        {
            public readonly String Name;
            public readonly Int32 HashBits;
            public readonly Int32[] Buckets = new Int32[BUCKET_COUNT];
            public readonly String[] FirstBucketInputs = new String[BUCKET_COUNT];
            public readonly HashSet<String> Inputs = new(StringComparer.Ordinal);
            public readonly Dictionary<String, CollisionGroup> FullGroups = new(StringComparer.Ordinal);
            public readonly List<CollisionExample> Examples = [];
            public Int64 TotalInputs;
            public Int64 DistinctInputs;
            public Int64 DuplicateInputs;
            public Int64 FullDuplicateOutputs;
            public Int64 FullCollidingPairs;
            public Int64 ProjectedCollidingPairs;
            public Int32 FullExamples;
            public Int32 ProjectedExamples;
            public CorpusResult(String name, Int32 hashBits) { Name = name; HashBits = hashBits; }
        }

        private sealed class CollisionGroup
        {
            public readonly String FirstInput;
            public Int32 Count = 1;
            public CollisionGroup(String firstInput) { FirstInput = firstInput; }
        }

        private readonly record struct CollisionExample(String Corpus, String Type, String Hash, Int32 Bucket, String InputA, String InputB);
        private readonly record struct BucketStats(Int32 Empty, Int32 Singleton, Int32 Colliding, Int32 Maximum, Double Mean, Double Variance, Double ChiSquare);
        #endregion
    }
}
