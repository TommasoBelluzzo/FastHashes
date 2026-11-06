#region Using Directives
using System;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
#endregion

namespace FastHashes.Analysis
{
    /// <summary>Bit Independence Criterion analysis of output-bit flips under single input-bit perturbations.</summary>
    internal static class IndependenceAnalysis
    {
        #region Setup
        private const Double SIGNIFICANCE_LEVEL = 0.05d;
        private const Int32 MINIMUM_SAMPLES = 100;
        private const Int64 MAXIMUM_HASH_EVALUATIONS = 20000000;
        private const Int64 MAXIMUM_PAIR_CELLS = 20000000;
        private const Int64 MAXIMUM_PAIR_TRIALS = 2000000000;
        #endregion

        #region Methods
        public static void Validate(Int32 hashLength, Int32 samples, Int32 keyLength)
        {
            if (keyLength < 1)
                throw new ArgumentOutOfRangeException(nameof(keyLength), "Independence analysis requires a nonempty key.");

            if (samples < MINIMUM_SAMPLES)
                throw new ArgumentOutOfRangeException(nameof(samples), $"Independence analysis requires at least {MINIMUM_SAMPLES} base inputs for asymptotic testing.");

            if ((hashLength <= 1) || ((hashLength % 8) != 0))
                throw new NotSupportedException("Independence analysis requires a byte-aligned hash longer than one bit.");

            Int64 inputBits = (Int64)keyLength * 8;
            Int64 pairs = (Int64)hashLength * (hashLength - 1) / 2;

            if (samples * (inputBits + 1) > MAXIMUM_HASH_EVALUATIONS)
                throw new ArgumentOutOfRangeException(nameof(samples), "Independence workload exceeds the permitted hash evaluations.");

            if (inputBits * pairs > MAXIMUM_PAIR_CELLS)
                throw new ArgumentOutOfRangeException(nameof(keyLength), "Independence matrix exceeds the permitted pair cells.");

            if (inputBits * pairs * samples > MAXIMUM_PAIR_TRIALS)
                throw new ArgumentOutOfRangeException(nameof(samples), "Independence workload exceeds the permitted pair trials; reduce --samples or --key-length.");
        }

        public static String Run(Hash hash, String hashName, UInt64? hashSeed, UInt64 generatorSeed, Int32 samples, Int32 keyLength)
        {
            Validate(hash.Length, samples, keyLength);

            Int32 inputBits = checked(keyLength * 8);
            Int32 outputBits = hash.Length;
            Int32 outputBytes = outputBits / 8;
            Int32 pairCount = checked(outputBits * (outputBits - 1) / 2);
            Int32[] flipCounts = new Int32[checked(inputBits * outputBits)];
            Int32[] jointCounts = new Int32[checked(inputBits * pairCount)];
            Int32[] flippedBits = new Int32[outputBits];
            Byte[] key = new Byte[keyLength];
            InputGenerator generator = new(generatorSeed);

            // Sampling is completed before any directory or report is created.
            for (Int32 sample = 0; sample < samples; ++sample)
            {
                generator.NextBytes(key);
                Byte[] baseline = hash.ComputeHashBytes(key);
                CheckDigest(baseline, outputBytes);

                for (Int32 inputBit = 0; inputBit < inputBits; ++inputBit)
                {
                    Int32 keyIndex = inputBit / 8;
                    Byte mask = (Byte)(1 << (inputBit % 8));
                    key[keyIndex] ^= mask;
                    Byte[] mutated;

                    try
                    {
                        mutated = hash.ComputeHashBytes(key);
                    }
                    finally
                    {
                        key[keyIndex] ^= mask;
                    }

                    CheckDigest(mutated, outputBytes);
                    Int32 count = 0;
                    Int32 flipOffset = inputBit * outputBits;
                    Int32 pairOffset = inputBit * pairCount;

                    for (Int32 outputByte = 0; outputByte < outputBytes; ++outputByte)
                    {
                        Byte difference = (Byte)(baseline[outputByte] ^ mutated[outputByte]);

                        for (Int32 bit = 0; bit < 8; ++bit)
                        {
                            if ((difference & (1 << bit)) == 0)
                                continue;

                            Int32 outputBit = outputByte * 8 + bit;
                            ++flipCounts[flipOffset + outputBit];
                            flippedBits[count++] = outputBit;
                        }
                    }

                    for (Int32 a = 0; a < count; ++a)
                    {
                        Int32 outputA = flippedBits[a];
                        Int32 baseIndex = outputA * (2 * outputBits - outputA - 1) / 2;

                        for (Int32 b = a + 1; b < count; ++b)
                            ++jointCounts[pairOffset + baseIndex + flippedBits[b] - outputA - 1];
                    }
                }
            }

            // Compute complete pair statistics without writing partial reports.
            Int64 testCount = (Int64)inputBits * pairCount;
            Double[] meanAbsoluteCorrelation = new Double[pairCount];
            Int64 validTests = 0;
            Int64 degenerateTests = 0;
            Int64 insufficientExpectedTests = 0;
            Double sumAbsoluteCorrelation = 0.0d;
            Double maximumAbsoluteCorrelation = 0.0d;
            Int32 maximumInputBit = 0;
            Int32 maximumOutputA = 0;
            Int32 maximumOutputB = 1;

            for (Int32 inputBit = 0; inputBit < inputBits; ++inputBit)
            {
                Int32 pairIndex = 0;
                Int32 flipOffset = inputBit * outputBits;
                Int32 jointOffset = inputBit * pairCount;

                for (Int32 a = 0; a < outputBits; ++a)
                {
                    for (Int32 b = a + 1; b < outputBits; ++b)
                    {
                        Int32 n11 = jointCounts[jointOffset + pairIndex];
                        Int32 n10 = flipCounts[flipOffset + a] - n11;
                        Int32 n01 = flipCounts[flipOffset + b] - n11;
                        Int32 n00 = samples - n11 - n10 - n01;
                        Double correlation = Phi(n00, n01, n10, n11);

                        if (Double.IsNaN(correlation))
                            ++degenerateTests;
                        else
                        {
                            Double absolute = Math.Abs(correlation);
                            sumAbsoluteCorrelation += absolute;
                            meanAbsoluteCorrelation[pairIndex] += absolute;

                            if (absolute > maximumAbsoluteCorrelation)
                            {
                                maximumAbsoluteCorrelation = absolute;
                                maximumInputBit = inputBit;
                                maximumOutputA = a;
                                maximumOutputB = b;
                            }

                            if (ExpectedCountsValid(n00, n01, n10, n11, samples))
                            {
                                ++validTests;
                            }
                            else
                                ++insufficientExpectedTests;
                        }

                        ++pairIndex;
                    }
                }
            }

            Int64 significantTests = 0;
            Double meanAbsolute = degenerateTests == testCount ? Double.NaN : sumAbsoluteCorrelation / (testCount - degenerateTests);
            String[] outputFiles = ["independence.csv", "independence-pairs.csv", "independence-heatmap.svg", "independence.json", "independence.txt"];

            var metadata = new
            {
                SchemaVersion = 1,
                Analysis = "independence",
                Status = "Completed",
                QualityAssessment = "StatisticalEvidenceOnly",
                HashName = hashName,
                HashSeed = hashSeed,
                HashLengthBits = outputBits,
                GeneratorAlgorithm = "SplitMix64",
                GeneratorSeed = generatorSeed,
                SamplesPerInputBit = samples,
                InputLengthBytes = keyLength,
                InputBits = inputBits,
                OutputBits = outputBits,
                OutputPairs = pairCount,
                PairCells = testCount,
                HashEvaluations = (Int64)samples * (inputBits + 1L),
                BitFlipTrials = (Int64)samples * inputBits,
                BitNumbering = "Least significant bit first within each byte; bytes follow ComputeHashBytes order",
                Statistic = "Phi coefficient for pairwise output-bit flip indicators conditioned on each input bit",
                SignificanceTest = "Asymptotic Pearson chi-square (one degree of freedom), two-sided normal-tail approximation; Bonferroni correction across valid tests",
                SignificanceLevel = SIGNIFICANCE_LEVEL,
                MinimumExpectedCellCount = 5,
                ValidTests = validTests,
                DegenerateTests = degenerateTests,
                InsufficientExpectedTests = insufficientExpectedTests,
                MeanAbsoluteCorrelation = Double.IsNaN(meanAbsolute) ? (Double?)null : meanAbsolute,
                MaximumAbsoluteCorrelation = maximumAbsoluteCorrelation,
                MaximumCorrelationInputBit = maximumInputBit,
                MaximumCorrelationOutputBitA = maximumOutputA,
                MaximumCorrelationOutputBitB = maximumOutputB,
                // Filled consistently after CSV emission below; calculate significant count beforehand.
                UtcTimestamp = DateTimeOffset.UtcNow,
                LibraryVersion = typeof(Hash).Assembly.GetName().Version?.ToString(),
                Runtime = RuntimeInformation.FrameworkDescription,
                OperatingSystem = RuntimeInformation.OSDescription,
                ProcessArchitecture = RuntimeInformation.ProcessArchitecture.ToString(),
                OutputFiles = outputFiles
            };

            // Count significant cells separately so metadata is finalized before file creation.
            if (validTests > 0)
            {
                for (Int32 inputBit = 0; inputBit < inputBits; ++inputBit)
                {
                    Int32 pairIndex = 0;
                    Int32 flipOffset = inputBit * outputBits;
                    Int32 jointOffset = inputBit * pairCount;
                    for (Int32 a = 0; a < outputBits; ++a)
                    {
                        for (Int32 b = a + 1; b < outputBits; ++b)
                        {
                            Int32 n11 = jointCounts[jointOffset + pairIndex++];
                            Int32 n10 = flipCounts[flipOffset + a] - n11;
                            Int32 n01 = flipCounts[flipOffset + b] - n11;
                            Int32 n00 = samples - n11 - n10 - n01;
                            if (!ExpectedCountsValid(n00, n01, n10, n11, samples))
                                continue;
                            Double phi = Phi(n00, n01, n10, n11);
                            if (Math.Min(1.0d, PValue(phi, samples) * validTests) < SIGNIFICANCE_LEVEL)
                                ++significantTests;
                        }
                    }
                }
            }

            return AnalysisRunner.WriteArtifacts(hashName, directory =>
            {
                using (StreamWriter writer = CreateWriter(Path.Combine(directory, "independence.csv")))
                {
                    writer.WriteLine("InputBit,OutputBitA,OutputBitB,N00,N01,N10,N11,Trials,Phi,AbsolutePhi,PValue,BonferroniPValue,TestStatus");
                    for (Int32 inputBit = 0; inputBit < inputBits; ++inputBit)
                    {
                        Int32 pairIndex = 0;
                        Int32 flipOffset = inputBit * outputBits;
                        Int32 jointOffset = inputBit * pairCount;
                        for (Int32 a = 0; a < outputBits; ++a)
                        {
                            for (Int32 b = a + 1; b < outputBits; ++b)
                            {
                                Int32 n11 = jointCounts[jointOffset + pairIndex++];
                                Int32 n10 = flipCounts[flipOffset + a] - n11;
                                Int32 n01 = flipCounts[flipOffset + b] - n11;
                                Int32 n00 = samples - n11 - n10 - n01;
                                Double phi = Phi(n00, n01, n10, n11);
                                Boolean valid = !Double.IsNaN(phi) && ExpectedCountsValid(n00, n01, n10, n11, samples);
                                Double p = valid ? PValue(phi, samples) : Double.NaN;
                                String status = Double.IsNaN(phi) ? "Degenerate" : valid ? "Valid" : "InsufficientExpectedCounts";
                                writer.WriteLine(String.Join(",",
                                    inputBit, a, b, n00, n01, n10, n11, samples,
                                    Format(phi), Format(Math.Abs(phi)), Format(p), Format(valid ? Math.Min(1.0d, p * validTests) : Double.NaN), status));
                            }
                        }
                    }
                }

                using (StreamWriter writer = CreateWriter(Path.Combine(directory, "independence-pairs.csv")))
                {
                    writer.WriteLine("OutputBitA,OutputBitB,MeanAbsolutePhi,InputBits");
                    Int32 pairIndex = 0;
                    for (Int32 a = 0; a < outputBits; ++a)
                    {
                        for (Int32 b = a + 1; b < outputBits; ++b)
                        {
                            // Valid correlations for this pair may be fewer than inputBits in degenerate cases.
                            Int32 count = 0;
                            for (Int32 inputBit = 0; inputBit < inputBits; ++inputBit)
                            {
                                Int32 n11 = jointCounts[inputBit * pairCount + pairIndex];
                                Int32 n10 = flipCounts[inputBit * outputBits + a] - n11;
                                Int32 n01 = flipCounts[inputBit * outputBits + b] - n11;
                                Int32 n00 = samples - n11 - n10 - n01;
                                if (!Double.IsNaN(Phi(n00, n01, n10, n11)))
                                    ++count;
                            }
                            writer.WriteLine(String.Join(",", a, b,
                                count == 0 ? "" : Format(meanAbsoluteCorrelation[pairIndex] / count), count));
                            ++pairIndex;
                        }
                    }
                }

                IndependenceSvg.WriteHeatmap(Path.Combine(directory, "independence-heatmap.svg"), meanAbsoluteCorrelation, jointCounts, flipCounts, inputBits, outputBits, pairCount, samples, meanAbsolute, maximumAbsoluteCorrelation, significantTests, validTests);
                var finalMetadata = new
                {
                    metadata.SchemaVersion, metadata.Analysis, metadata.Status, metadata.QualityAssessment,
                    metadata.HashName, metadata.HashSeed, metadata.HashLengthBits, metadata.GeneratorAlgorithm,
                    metadata.GeneratorSeed, metadata.SamplesPerInputBit, metadata.InputLengthBytes,
                    metadata.InputBits, metadata.OutputBits, metadata.OutputPairs, metadata.PairCells,
                    metadata.HashEvaluations, metadata.BitFlipTrials, metadata.BitNumbering,
                    metadata.Statistic, metadata.SignificanceTest, metadata.SignificanceLevel,
                    metadata.MinimumExpectedCellCount, metadata.ValidTests, metadata.DegenerateTests,
                    metadata.InsufficientExpectedTests, metadata.MeanAbsoluteCorrelation,
                    metadata.MaximumAbsoluteCorrelation, metadata.MaximumCorrelationInputBit,
                    metadata.MaximumCorrelationOutputBitA, metadata.MaximumCorrelationOutputBitB,
                    SignificantTests = significantTests, metadata.UtcTimestamp, metadata.LibraryVersion,
                    metadata.Runtime, metadata.OperatingSystem, metadata.ProcessArchitecture, metadata.OutputFiles
                };
                File.WriteAllText(Path.Combine(directory, "independence.json"), JsonSerializer.Serialize(finalMetadata, new JsonSerializerOptions { WriteIndented = true }) + "\n", new UTF8Encoding(false));
                File.WriteAllText(Path.Combine(directory, "independence.txt"),
                    $"Analysis: independence (AN-002)\nHash: {hashName}\nHash seed: {(hashSeed.HasValue ? hashSeed.Value.ToString(CultureInfo.InvariantCulture) : "default configuration")}\nGenerator seed: {generatorSeed}\nSamples per input bit: {samples}\nInput bits: {inputBits}\nOutput bits: {outputBits}\nOutput-bit pairs: {pairCount}\nPair cells: {testCount}\nValid tests: {validTests}\nDegenerate tests: {degenerateTests}\nInsufficient expected counts: {insufficientExpectedTests}\nMean absolute phi: {Format(meanAbsolute)}\nMaximum absolute phi: {Format(maximumAbsoluteCorrelation)} (input bit {maximumInputBit}, output bits {maximumOutputA}/{maximumOutputB})\nBonferroni-significant tests: {significantTests}\nNote: no significant result does not prove bit independence.\n",
                    new UTF8Encoding(false));
            });
        }

        private static void CheckDigest(Byte[] digest, Int32 expectedLength)
        {
            if ((digest == null) || (digest.Length != expectedLength))
                throw new InvalidOperationException("The hash returned an invalid digest length.");
        }

        private static Double Phi(Int32 n00, Int32 n01, Int32 n10, Int32 n11)
        {
            Double a = (Double)(n10 + n11) * (n00 + n01);
            Double b = (Double)(n01 + n11) * (n00 + n10);
            if (a == 0.0d || b == 0.0d)
                return Double.NaN;
            return ((Double)n11 * n00 - (Double)n10 * n01) / Math.Sqrt(a * b);
        }

        private static Boolean ExpectedCountsValid(Int32 n00, Int32 n01, Int32 n10, Int32 n11, Int32 samples)
        {
            Int32 rowZero = n00 + n01;
            Int32 rowOne = n10 + n11;
            Int32 columnZero = n00 + n10;
            Int32 columnOne = n01 + n11;
            return (Double)rowZero * columnZero / samples >= 5.0d &&
                   (Double)rowZero * columnOne / samples >= 5.0d &&
                   (Double)rowOne * columnZero / samples >= 5.0d &&
                   (Double)rowOne * columnOne / samples >= 5.0d;
        }

        // P(ChiSquare_1 >= n*phi^2) = erfc(sqrt(n)*abs(phi)/sqrt(2)).
        // Abramowitz-Stegun 7.1.26 approximation to erfc, absolute error ~1.5e-7.
        private static Double PValue(Double phi, Int32 samples)
        {
            Double z = Math.Sqrt(samples / 2.0d) * Math.Abs(phi);
            Double t = 1.0d / (1.0d + 0.3275911d * z);
            Double polynomial = (((((1.061405429d * t - 1.453152027d) * t) + 1.421413741d) * t - 0.284496736d) * t + 0.254829592d) * t;
            return Math.Max(0.0d, Math.Min(1.0d, polynomial * Math.Exp(-z * z)));
        }

        private static StreamWriter CreateWriter(String path)
        {
            return new StreamWriter(path, false, new UTF8Encoding(false));
        }

        private static String Format(Double value)
        {
            return Double.IsNaN(value) ? "" : value.ToString("G17", CultureInfo.InvariantCulture);
        }
        #endregion
    }
}
