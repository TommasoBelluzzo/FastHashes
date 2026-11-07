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
        private const Int32 PLOT_SIZE = 256;
        private const Int32 PLOT_CELLS = PROJECTION_BUCKETS;
        #endregion

        #region Methods
        private static CorpusResult AnalyzeCorpus(String name, Hash hash, IEnumerable<Byte[]> inputs, Int64? distinctLimit = null)
        {
            Int32 hashLength = hash.Length;

            Int64 inputsTotal = 0;
            Int64 inputsDistinct = 0;
            Int64 inputsDuplicates = 0;
            Int64 fullDuplicates = 0;
            Int64 fullPairs = 0;
            Int64 projectedPairs = 0;
            
            Int32 fullExamples = 0;
            Int32 projectedExamples = 0;

            Int32[] loads = new Int32[PROJECTION_BUCKETS];
            List<CollisionExample> examples = [];

            String[] firstBucketInputs = new String[PROJECTION_BUCKETS];
            HashSet<String> uniqueInputs = new(StringComparer.Ordinal);
            Dictionary<String, (String FirstInput, Int32 Count)> fullGroups = new(StringComparer.Ordinal);

            foreach (Byte[] input in inputs)
            {
                ++inputsTotal;

                String inputHex = Convert.ToHexString(input);

                if (!uniqueInputs.Add(inputHex))
                {
                    ++inputsDuplicates;
                    continue;
                }

                ++inputsDistinct;

                Byte[] hashBytes = hash.ComputeHashBytes(input);
                Int32 bucket = hashBytes[0] | (hashBytes[1] << 8);

                String hashHex = Convert.ToHexString(hashBytes);

                if (fullGroups.TryGetValue(hashHex, out var group))
                {
                    fullPairs += group.Count;
                    ++fullDuplicates;

                    if (fullExamples < MAXIMUM_CORPUS_EXAMPLES)
                    {
                        examples.Add(new CollisionExample(name, "full", bucket, hashHex, group.FirstInput, inputHex));
                        ++fullExamples;
                    }

                    fullGroups[hashHex] = (group.FirstInput, group.Count + 1);
                }
                else
                    fullGroups.Add(hashHex, (inputHex, 1));

                if ((loads[bucket] > 0) && (projectedExamples < MAXIMUM_CORPUS_EXAMPLES))
                {
                    examples.Add(new CollisionExample(name, "projected_16", bucket, hashHex, firstBucketInputs[bucket], inputHex));
                    ++projectedExamples;
                }

                if (loads[bucket] == 0)
                    firstBucketInputs[bucket] = inputHex;

                projectedPairs += loads[bucket];
                ++loads[bucket];

                if (distinctLimit.HasValue && (inputsDistinct >= distinctLimit.Value))
                    break;
            }

            if (distinctLimit.HasValue && (inputsDistinct < distinctLimit.Value))
                throw new InvalidOperationException("Could not generate the requested number of distinct random keys.");

            Int32 empty = 0, singleton = 0, colliding = 0, maximum = 0;
            Double mean = (Double)inputsDistinct / PROJECTION_BUCKETS;
            Double squared = 0.0d;

            foreach (Int32 load in loads)
            {
                if (load == 0)
                    ++empty;
                else if (load == 1)
                    ++singleton;
                else
                    ++colliding;

                maximum = Math.Max(maximum, load);

                Double difference = load - mean;
                squared += difference * difference;
            }

            BucketStats buckets = new(
                empty, singleton, colliding, maximum, mean,
                squared / PROJECTION_BUCKETS,
                (mean == 0.0d) ? 0.0d : (squared / mean)
            );

            CorpusObservations observations = new(inputsTotal, inputsDistinct, inputsDuplicates);
            
            CollisionMetrics full = new(
                fullDuplicates,
                MathUtilities.ExpectedCollisionDuplicates(inputsDistinct, hashLength),
                fullPairs,
                MathUtilities.ExpectedCollisionPairs(inputsDistinct, hashLength)
            );

            CollisionMetrics projected = new(
                inputsDistinct - PROJECTION_BUCKETS + empty,
                MathUtilities.ExpectedCollisionDuplicates(inputsDistinct, PROJECTION_BITS),
                projectedPairs,
                MathUtilities.ExpectedCollisionPairs(inputsDistinct, PROJECTION_BITS)
            );

            return new CorpusResult(name, observations, full, projected, buckets, loads, examples);
        }

        private static IEnumerable<Byte[]> GenerateNumbers(Int32 count)
        {
            for (Int64 i = 1; i <= count; ++i)
            {
                String s = i.ToString(CultureInfo.InvariantCulture);
                Byte[] buffer = Encoding.UTF8.GetBytes(s);
                
                yield return buffer;
            }
                
        }

        private static IEnumerable<Byte[]> GenerateRandomBytes(Int32 length, UInt64 seed, Int64 maximumAttempts)
        {
            RandomSplitMix generator = new(seed);

            for (Int64 attempt = 0; attempt < maximumAttempts; ++attempt)
            {
                Byte[] buffer = new Byte[length];
                generator.NextBytes(buffer);
                
                yield return buffer;
            }
        }

        private static IEnumerable<Byte[]> GeneratePaddedWords()
        {
            String wordsPath = Path.Combine(AppContext.BaseDirectory, "Data", "Words.txt");

            foreach (String word in File.ReadLines(wordsPath))
            {
                Byte[] input = Encoding.UTF8.GetBytes(word);
                
                Byte[] padded = new Byte[input.Length + 5];
                Array.Fill(padded, (Byte)'!');
                
                input.CopyTo(padded, 0);

                for (Int32 suffix = 0; suffix <= 5; ++suffix)
                {
                    Byte[] output = new Byte[input.Length + suffix];
                    Array.Copy(padded, output, output.Length);
                    
                    yield return output;
                }
            }
        }

        public static String Run(Config config, String outputDirectory = null)
        {
            Hash hash = config.Hash;
            String hashName = config.HashName;
            UInt64 generatorSeed = config.GeneratorSeed;
            Int32 keysLength = config.KeysLength;
            Int32 corpusSize = config.CorpusSize;
            
            Int64 maximumAttempts = ((Int64)corpusSize * 100) + 1000;

            List<CorpusResult> results =
            [
                AnalyzeCorpus("numbers", hash, GenerateNumbers(corpusSize)),
                AnalyzeCorpus("random-bytes", hash, GenerateRandomBytes(keysLength, generatorSeed, maximumAttempts), corpusSize),
                AnalyzeCorpus("padded-words", hash, GeneratePaddedWords())
            ];

            Dictionary<String, Object> metadata = Utilities.CreateMetadata("collision", config);
            metadata["SchemaVersion"] = 3;
            metadata["ProjectionBits"] = PROJECTION_BITS;
            metadata["ProjectionBuckets"] = PROJECTION_BUCKETS;
            metadata["Corpora"] = results.ConvertAll(result => new
            {
                result.Name,
                result.Inputs,
                result.Full,
                result.Projected,
                result.Buckets
            });
            metadata["OutputFiles"] = new String[]
            {
                "collision.json",
                "collision-summary.csv",
                "collision-buckets.csv",
                "collision-examples.csv"
            }
            .Concat(results.SelectMany(result => new String[]
            {
                $"collision-{result.Name}-hg.svg",
                $"collision-{result.Name}-omh.svg",
                $"collision-{result.Name}-oml.svg"
            }))
            .ToArray();

            void writeFunction(String directory)
            {
                String filePath;
                Encoding encoding = new UTF8Encoding(false);

                filePath = Path.Combine(directory, "collision.json");
                File.WriteAllText(filePath, Utilities.SerializeJson(metadata) + "\n", encoding);

                filePath = Path.Combine(directory, "collision-summary.csv");

                using (StreamWriter writer = new(filePath, false, encoding))
                {
                    writer.WriteLine("Corpus,InputsTotal,InputsDistinct,InputsDuplicates,FullDuplicates,FullExpectedDuplicates,FullPairs,FullExpectedPairs,ProjectedDuplicates,ProjectedExpectedDuplicates,ProjectedPairs,ProjectedExpectedPairs,EmptyBuckets,SingletonBuckets,CollidingBuckets,MaximumBucketLoad,MeanBucketLoad,BucketLoadVariance,OccupancyChiSquare");

                    foreach (CorpusResult result in results)
                    {
                        writer.WriteLine(String.Join(",",
                            result.Name,
                            Utilities.FormatCsvInteger(result.Inputs.Total),
                            Utilities.FormatCsvInteger(result.Inputs.Distinct),
                            Utilities.FormatCsvInteger(result.Inputs.Duplicates),
                            Utilities.FormatCsvInteger(result.Full.Duplicates),
                            Utilities.FormatCsvDouble(result.Full.ExpectedDuplicates),
                            Utilities.FormatCsvInteger(result.Full.Pairs),
                            Utilities.FormatCsvDouble(result.Full.ExpectedPairs),
                            Utilities.FormatCsvInteger(result.Projected.Duplicates),
                            Utilities.FormatCsvDouble(result.Projected.ExpectedDuplicates),
                            Utilities.FormatCsvInteger(result.Projected.Pairs),
                            Utilities.FormatCsvDouble(result.Projected.ExpectedPairs),
                            Utilities.FormatCsvInteger(result.Buckets.Empty),
                            Utilities.FormatCsvInteger(result.Buckets.Singleton),
                            Utilities.FormatCsvInteger(result.Buckets.Colliding),
                            Utilities.FormatCsvInteger(result.Buckets.Maximum),
                            Utilities.FormatCsvDouble(result.Buckets.Mean),
                            Utilities.FormatCsvDouble(result.Buckets.Variance),
                            Utilities.FormatCsvDouble(result.Buckets.ChiSquare)
                        ));
                    }
                }

                filePath = Path.Combine(directory, "collision-buckets.csv");

                using (StreamWriter writer = new(filePath, false, encoding))
                {
                    writer.WriteLine("Corpus,Bucket,Load");
                    
                    foreach (CorpusResult result in results)
                        for (Int32 b = 0; b < PROJECTION_BUCKETS; ++b)
                            writer.WriteLine(String.Join(",",
                                result.Name,
                                Utilities.FormatCsvInteger(b),
                                Utilities.FormatCsvInteger(result.Loads[b])
                            ));
                }

                filePath = Path.Combine(directory, "collision-examples.csv");

                using (StreamWriter writer = new(filePath, false, encoding))
                {
                    writer.WriteLine("Corpus,CollisionType,ProjectedBucket,Hash,InputA,InputB");
                    
                    foreach (CorpusResult result in results)
                        foreach (CollisionExample example in result.Examples)
                            writer.WriteLine(String.Join(",",
                                example.Corpus,
                                example.Type,
                                Utilities.FormatCsvInteger(example.Bucket),
                                example.Hash,
                                example.InputA,
                                example.InputB
                            ));
                }

                foreach (CorpusResult result in results)
                {
                    filePath = Path.Combine(directory, $"collision-{result.Name}-hg.svg");
                    PlotHistogram(filePath, result);

                    filePath = Path.Combine(directory, $"collision-{result.Name}-omh.svg");
                    PlotOccupancyMap(filePath, true, result);

                    filePath = Path.Combine(directory, $"collision-{result.Name}-oml.svg");
                    PlotOccupancyMap(filePath, false, result);
                }
            }

            return Utilities.WriteArtifacts(hashName, writeFunction, outputDirectory);
        }
        #endregion

        #region Plots
        private static void PlotHistogram(String path, CorpusResult result)
        {
            Int32 maximumLoad = result.Buckets.Maximum;

            Int32 lastExact = Math.Min(20, Math.Max(4, maximumLoad));
            Boolean hasTail = maximumLoad > lastExact;
            Int32 groups = lastExact + 1 + (hasTail ? 1 : 0);

            Int32[] observed = new Int32[groups];

            foreach (Int32 load in result.Loads)
                ++observed[Math.Min(load, groups - 1)];

            Double lambda = result.Buckets.Mean;

            Double[] expected = new Double[groups];
            Double probability = Math.Exp(-lambda);
            Double cumulativeProbability = 0.0d;

            for (Int32 load = 0; load <= lastExact; ++load)
            {
                expected[load] = PLOT_CELLS * probability;
                cumulativeProbability += probability;
                probability *= lambda / (load + 1.0d);
            }

            if (hasTail)
                expected[^1] = PLOT_CELLS * Math.Max(0.0d, 1.0d - cumulativeProbability);

            Double ceiling = 1.0d;

            for (Int32 i = 0; i < groups; ++i)
                ceiling = Math.Max(ceiling, Math.Max(observed[i], expected[i]));

            ceiling *= 1.1d;

            List<(Double, String)> ticks = [];
            
            for (Int32 i = 0; i < groups; ++i)
            {
                Boolean last = i == (groups - 1);

                if (((i % 2) == 0) || last)
                {
                    Double value = i + 0.5d;
                    String label = (hasTail && last) ? $"{lastExact + 1}+" : i.ToString(CultureInfo.InvariantCulture);
                    ticks.Add((value, label));
                }
            }

            List<(Double, Double)> expectedPoints = new(groups);
            
            for (Int32 load = 0; load < groups; ++load)
                expectedPoints.Add((load + 0.5d, expected[load]));

            using SvgPlot plot = new(path, 1200, 770, "Collision bucket-load frequency histogram");

            plot.PlotArea(120.0d, 178.0d, 980.0d, 430.0d);
            plot.DataRange(0.0d, groups, 0.0d, ceiling);
            
            plot.ConfigureDiscreteYAxis("Number of Buckets", 0.0d, ceiling, intervals: 4, format: "N0");

            plot.ConfigureXTicks("Inputs per Projected Bucket", ticks);

            plot.ConfigureBottomLegend([
                new SvgPlot.LegendItem("Observed", plot.ObservedColor, false),
                new SvgPlot.LegendItem("Expected", plot.ReferenceColor, true)
            ]);

            plot.Title("COLLISION BUCKET-LOAD HISTOGRAM");
            plot.Subtitle("Observed bucket occupancy versus the theoretical Poisson approximation.");
            plot.Metadata(
                $"Corpus: {result.Name} | Distinct Inputs: {result.Inputs.Distinct:N0} | Buckets: {PLOT_CELLS:N0}",
                $"Mean Load: {lambda:0.###} | Maximum Load: {maximumLoad:N0}"
            );

            plot.PrepareLayout();
            plot.DrawGrid();

            plot.BeginPlot();

            for (Int32 load = 0; load < groups; ++load)
                plot.Bar(load, groups, observed[load], plot.ObservedColor, 8.0d);

            plot.Polyline(expectedPoints, plot.ReferenceColor, 3.0d);

            plot.EndPlot();

            plot.DrawBorder();
            plot.DrawAxes();
            plot.DrawBottomLegend();
        }

        private static void PlotOccupancyMap(String path, Boolean hilbert, CorpusResult result)
        {
            Int32 occupiedBuckets = PROJECTION_BUCKETS - result.Buckets.Empty;
            Int32 maximumLoad = result.Buckets.Maximum;

            Double occupiedBucketsFraction = (Double)occupiedBuckets / PLOT_CELLS;

            List<(Int32, Int32, String)> cellsData = new(occupiedBuckets);

            for (Int32 i = 0; i < PLOT_CELLS; ++i)
            {
                Int32 load = result.Loads[i];
                
                if (load == 0)
                    continue;

                Int32 x, y;

                if (hilbert)
                {
                    Int32 t = i;
                    x = 0; y = 0;

                    for (Int32 s = 1; s < PLOT_SIZE; s *= 2)
                    {
                        Int32 rx = 1 & (t / 2);
                        Int32 ry = 1 & (t ^ rx);
                        
                        if (ry == 0)
                        {
                            if (rx == 1)
                            {
                                x = s - 1 - x;
                                y = s - 1 - y;
                            }
                            
                            (x, y) = (y, x);
                        }
                        
                        x += s * rx;
                        y += s * ry;
                        
                        t /= 4;
                    }
                }
                else
                {
                    x = i % PLOT_SIZE;
                    y = i / PLOT_SIZE;
                }

                Double hue = 360.0d * i / PLOT_CELLS;
                Double saturation = 0.55d + 0.45d * Math.Min(1.0d, Math.Log2(load + 1.0d) / 3.0d);
                Double lightness = 0.78d - 0.35d * Math.Min(1.0d, Math.Log2(load + 1.0d) / 4.0d);

                Double chroma = (1.0d - Math.Abs(2.0d * lightness - 1.0d)) * saturation;
                Double secondary = chroma * (1.0d - Math.Abs(hue / 60.0d % 2.0d - 1.0d));

                Double red = 0.0d, green = 0.0d, blue = 0.0d;
                
                if (hue < 60.0d)
                {
                    red = chroma;
                    green = secondary;
                }
                else if (hue < 120.0d)
                {
                    red = secondary;
                    green = chroma;
                }
                else if (hue < 180.0d)
                {
                    green = chroma;
                    blue = secondary;
                }
                else if (hue < 240.0d)
                {
                    green = secondary;
                    blue = chroma;
                }
                else if (hue < 300.0d)
                {
                    red = secondary;
                    blue = chroma;
                }
                else
                {
                    red = chroma;
                    blue = secondary;
                }

                Double offset = lightness - chroma / 2.0d;
                Int32 r = Math.Clamp((Int32)Math.Round(255.0d * (red + offset)), 0, 255);
                Int32 g = Math.Clamp((Int32)Math.Round(255.0d * (green + offset)), 0, 255);
                Int32 b = Math.Clamp((Int32)Math.Round(255.0d * (blue + offset)), 0, 255);
                String color = $"#{r:X2}{g:X2}{b:X2}";

                cellsData.Add((x, y, color));
            }

            using SvgPlot plot = new(path, 1000, 820, "Collision bucket occupancy map");

            plot.PlotArea(85.0d, 165.0d, 540.0d, 540.0d);
            
            plot.Title($"COLLISION OCCUPANCY MAP ({(hilbert ? "HILBERT" : "LINEAR")})");
            plot.Subtitle("Hue identifies bucket index; color intensity reflects bucket load; white indicates empty buckets.");
            plot.Metadata(
                $"Corpus: {result.Name} | Distinct Inputs: {result.Inputs.Distinct:N0} | Buckets: {PLOT_CELLS:N0}",
                $"Occupied Buckets: {occupiedBuckets:N0} ({occupiedBucketsFraction:P2}) | Maximum Load: {maximumLoad:N0}"
            );
            
            plot.PrepareLayout();

            plot.BeginPlot();

            foreach (var (x, y, color) in cellsData)
                plot.HeatmapCell(x, y, PLOT_SIZE, PLOT_SIZE, color, 0.0d);

            plot.EndPlot();
            
            plot.DrawBorder();
        }
        #endregion

        #region Records
        private readonly record struct BucketStats(
            Int32 Empty,
            Int32 Singleton,
            Int32 Colliding,
            Int32 Maximum,
            Double Mean,
            Double Variance,
            Double ChiSquare
        );

        private readonly record struct CollisionExample(
            String Corpus,
            String Type,
            Int32 Bucket,
            String Hash,
            String InputA,
            String InputB
        );

        private readonly record struct CollisionMetrics(
            Int64 Duplicates,
            Double ExpectedDuplicates,
            Int64 Pairs,
            Double ExpectedPairs
        );

        private readonly record struct CorpusObservations(
            Int64 Total,
            Int64 Distinct,
            Int64 Duplicates
        );

        private sealed record CorpusResult(
            String Name,
            CorpusObservations Inputs,
            CollisionMetrics Full,
            CollisionMetrics Projected,
            BucketStats Buckets,
            Int32[] Loads,
            IReadOnlyList<CollisionExample> Examples
        );
        #endregion
    }
}