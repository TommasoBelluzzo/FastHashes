#region Using Directives
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
#endregion

namespace FastHashes.Analysis
{
    internal static class CollisionAnalysis
    {
        #region Setup
        private const Int32 MAXIMUM_CORPUS_EXAMPLES = 1000;
        private const Int32 PROJECTION_BITS = 16;
        private const Int32 PROJECTION_BUCKETS = 1 << PROJECTION_BITS;
        #endregion

        #region Methods
        public static String Run(Config config, String outputDirectory = null)
        {
            String libraryVersion = config.LibraryVersion;
            Hash hash = config.Hash;
            Int32 hashLength = hash.Length;
            String hashName = config.HashName;
            UInt64? hashSeed = config.HashSeed;
            UInt64 generatorSeed = config.GeneratorSeed;
            Int32 samples = config.Samples;
            Int32 keysLength = config.KeysLength;
            Int32 corpusSize = config.CorpusSize;

            List<CorpusResult> results = [];


            RandomSplitMix generator = new(generatorSeed);
            
            CorpusResult random = new("random-bytes", hash.Length);

            Byte[] buffer = new Byte[keysLength];

            Int64 attempts = 0;
            
            while (random.DistinctInputs < corpusSize)
            {
                if (++attempts > (((Int64)corpusSize * 100) + 1000))
                    throw new InvalidOperationException("Could not generate the requested number of distinct random keys.");
                
                generator.NextBytes(buffer);
                Observe(random, hash, buffer);
            }

            results.Add(random);




            String wordsPath = Path.Combine(AppContext.BaseDirectory, "Data", "Words.txt");
            CorpusResult words = new("words-padded", hashLength);
            
            foreach (String word in File.ReadLines(wordsPath))
            {
                Byte[] input = Encoding.UTF8.GetBytes(word);
                
                Byte[] padded = new Byte[input.Length + 5];
                Array.Fill(padded, (Byte)'!');
                
                input.CopyTo(padded, 0);
                
                for (Int32 suffix = 0; suffix <= 5; ++suffix)
                    Observe(words, hash, padded.AsSpan(0, input.Length + suffix).ToArray());
            }

            results.Add(words);

            CorpusResult numbers = new("numbers", hashLength);
            
            for (Int32 i = 1; i <= corpusSize; ++i)
                Observe(numbers, hash, Encoding.UTF8.GetBytes(i.ToString(CultureInfo.InvariantCulture)));
            
            results.Add(numbers);



            Dictionary<String, Object> metadata = Utilities.CreateMetadata("collision", config);
            metadata["ProjectionBits"] = PROJECTION_BITS;
            metadata["ProjectionBuckets"] = PROJECTION_BUCKETS;
            metadata["MaximumCorpusExamples"] = MAXIMUM_CORPUS_EXAMPLES;
            metadata["Corpora"] = results.ConvertAll(result =>
            {
                BucketStats stats = ComputeStats(result);

                return new
                {
                    result.Name,
                    result.TotalInputs,
                    result.DistinctInputs,
                    result.DuplicateInputs,
                    result.FullDuplicateOutputs,
                    result.FullCollidingPairs,
                    ExpectedFullCollidingPairs = ExpectedPairs(result.DistinctInputs, hash.Length),
                    ExpectedFullDuplicateOutputs = ExpectedDuplicateOutputs(result.DistinctInputs, hashLength),
                    ProjectedDuplicateOutputs = result.DistinctInputs - PROJECTION_BUCKETS + stats.Empty,
                    result.ProjectedCollidingPairs,
                    ExpectedProjectedCollidingPairs = ExpectedPairs(result.DistinctInputs, PROJECTION_BITS),
                    ExpectedProjectedDuplicateOutputs = ExpectedDuplicateOutputs(result.DistinctInputs, PROJECTION_BITS),
                    EmptyBuckets = stats.Empty,
                    SingletonBuckets = stats.Singleton, 
                    CollidingBuckets = stats.Colliding,
                    MaximumBucketLoad = stats.Maximum,
                    MeanBucketLoad = stats.Mean,
                    BucketLoadVariance = stats.Variance,
                    OccupancyChiSquare = stats.ChiSquare
                };
            });
            metadata["OutputFiles"] = new String[]
            {
                "collision-summary.csv",
                "collision-buckets.csv",
                "collision-examples.csv"
            }
            .Concat(results.SelectMany(result => new String[]
            {
                $"collision-{result.Name}-linear.svg",
                $"collision-{result.Name}-hilbert.svg",
                $"collision-{result.Name}-histogram.svg"
            }))
            .ToArray();

            void writeFunction(String directory)
            {
                String filePath;
                Encoding encoding = new UTF8Encoding(false);

                filePath = Path.Combine(directory, "collision.json");
                File.WriteAllText(filePath, Utilities.SerializeJson(metadata) + "\n", encoding);

                using (StreamWriter writer = CreateWriter(Path.Combine(directory, "collision-summary.csv")))
                {
                    writer.WriteLine("Corpus,TotalInputs,DistinctInputs,DuplicateInputs,FullDuplicateOutputs,ExpectedFullDuplicateOutputs,FullCollidingPairs,ExpectedFullPairs,ProjectedBits,Buckets,EmptyBuckets,SingletonBuckets,CollisionBuckets,MaximumBucketLoad,ProjectedDuplicateOutputs,ExpectedProjectedDuplicateOutputs,ProjectedCollisionPairs,ExpectedProjectedPairs,ChiSquare,MeanLoad,LoadVariance");
                    foreach (CorpusResult result in results)
                    {
                        BucketStats stats = ComputeStats(result);
                        Double n = result.DistinctInputs;
                        writer.WriteLine(String.Join(",", result.Name, result.TotalInputs, result.DistinctInputs, result.DuplicateInputs,
                            result.FullDuplicateOutputs, F(ExpectedDuplicateOutputs(n, hashLength)), result.FullCollidingPairs, F(ExpectedPairs(n, hashLength)), PROJECTION_BITS, PROJECTION_BUCKETS,
                            stats.Empty, stats.Singleton, stats.Colliding, stats.Maximum,
                            result.DistinctInputs - PROJECTION_BUCKETS + stats.Empty, F(ExpectedDuplicateOutputs(n, PROJECTION_BITS)),
                            result.ProjectedCollidingPairs, F(ExpectedPairs(n, PROJECTION_BITS)), F(stats.ChiSquare), F(stats.Mean), F(stats.Variance)));
                    }
                }

                using (StreamWriter writer = CreateWriter(Path.Combine(directory, "collision-buckets.csv")))
                {
                    writer.WriteLine("Corpus,Bucket,Load");
                    foreach (CorpusResult result in results)
                        for (Int32 bucket = 0; bucket < PROJECTION_BUCKETS; ++bucket)
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
                    CollisionSvg.WriteHistogram(Path.Combine(directory, $"collision-{result.Name}-histogram.svg"), result.Name, result.Buckets, result.DistinctInputs);
                }
            }

            return Utilities.WriteArtifacts(hashName, writeFunction, outputDirectory);
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
                if (corpus.FullExamples < MAXIMUM_CORPUS_EXAMPLES)
                {
                    corpus.Examples.Add(new CollisionExample(corpus.Name, "Full", hashHex, bucket, group.FirstInput, inputHex));
                    ++corpus.FullExamples;
                }
                group.Count++;
            }
            else
                corpus.FullGroups.Add(hashHex, new CollisionGroup(inputHex));

            if (corpus.Buckets[bucket] > 0 && corpus.ProjectedExamples < MAXIMUM_CORPUS_EXAMPLES)
            {
                corpus.Examples.Add(new CollisionExample(corpus.Name, "Projected16", hashHex, bucket, corpus.FirstBucketInputs[bucket], inputHex));
                ++corpus.ProjectedExamples;
            }
            if (corpus.Buckets[bucket] == 0)
                corpus.FirstBucketInputs[bucket] = inputHex;
            corpus.ProjectedCollidingPairs += corpus.Buckets[bucket];
            ++corpus.Buckets[bucket];
        }

        private static BucketStats ComputeStats(CorpusResult result)
        {
            Int32 empty = 0, singleton = 0, colliding = 0, maximum = 0;
            Double mean = (Double)result.DistinctInputs / PROJECTION_BUCKETS;
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
            return new BucketStats(empty, singleton, colliding, maximum, mean, squared / PROJECTION_BUCKETS,
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
            public readonly Int32[] Buckets = new Int32[PROJECTION_BUCKETS];
            public readonly String[] FirstBucketInputs = new String[PROJECTION_BUCKETS];
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
