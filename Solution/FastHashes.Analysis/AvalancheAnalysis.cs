#region Using Directives
using System;
using System.Globalization;
using System.IO;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
#endregion

namespace FastHashes.Analysis
{
    internal static class AvalancheAnalysis
    {
        #region Setup
        private const Double SIGNIFICANCE_LEVEL = 0.05d;
        private const Int64 MAXIMUM_HASH_EVALUATIONS = 20000000;
        private const Int64 MAXIMUM_MATRIX_CELLS = 1000000;
        #endregion

        #region Methods
        public static void Validate(Int32 hashLength, Int32 samples, Int32 keyLength)
        {
            if (keyLength < 1)
                throw new ArgumentOutOfRangeException(nameof(keyLength), "Avalanche analysis requires a nonempty key.");

            if (samples < 1)
                throw new ArgumentOutOfRangeException(nameof(samples), "At least one base input is required.");

            if ((hashLength <= 0) || ((hashLength % 8) != 0))
                throw new NotSupportedException("Avalanche analysis requires a positive, byte-aligned hash length.");

            Int64 inputBits = (Int64)keyLength * 8;
            Int64 evaluations = (Int64)samples * (1 + inputBits);
            Int64 cells = inputBits * hashLength;

            if (evaluations > MAXIMUM_HASH_EVALUATIONS)
                throw new ArgumentOutOfRangeException(nameof(samples), $"Avalanche workload exceeds {MAXIMUM_HASH_EVALUATIONS} hash evaluations; reduce --samples or --key-length.");

            if (cells > MAXIMUM_MATRIX_CELLS)
                throw new ArgumentOutOfRangeException(nameof(keyLength), $"Avalanche matrix exceeds {MAXIMUM_MATRIX_CELLS} cells; reduce --key-length.");
        }

        public static String Run(Hash hash, String hashName, UInt64? hashSeed, UInt64 generatorSeed, Int32 samples, Int32 keyLength, String outputDirectory = null)
        {
            Validate(hash.Length, samples, keyLength);

            Int32 inputBits = checked(keyLength * 8);
            Int32 outputBits = hash.Length;
            Int32 outputBytes = outputBits / 8;
            Int32[] changed = new Int32[checked(inputBits * outputBits)];
            Int64[] hamming = new Int64[outputBits + 1];
            InputGenerator generator = new(generatorSeed);
            Byte[] key = new Byte[keyLength];

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
                    Int32 distance = 0;
                    Int32 rowStart = inputBit * outputBits;

                    for (Int32 outputByte = 0; outputByte < outputBytes; ++outputByte)
                    {
                        Byte difference = (Byte)(baseline[outputByte] ^ mutated[outputByte]);
                        distance += BitOperations.PopCount((UInt32)difference);

                        for (Int32 bit = 0; bit < 8; ++bit)
                        {
                            if ((difference & (1 << bit)) != 0)
                                ++changed[rowStart + outputByte * 8 + bit];
                        }
                    }

                    ++hamming[distance];
                }
            }

            // The exact two-sided Binomial(n, 0.5) tail is shared by every SAC cell.
            Double[] probabilities = BinomialTailProbabilities(samples);
            Int64 tests = (Int64)inputBits * outputBits;
            Int64 significant = 0;
            Double maximumBias = 0.0d;
            Double sumAbsoluteBias = 0.0d;
            Int32 maximumBiasInputBit = 0;
            Int32 maximumBiasOutputBit = 0;
            Double sumDistance = 0.0d;
            Int64 trials = (Int64)samples * inputBits;

            for (Int32 inputBit = 0; inputBit < inputBits; ++inputBit)
            {
                for (Int32 outputBit = 0; outputBit < outputBits; ++outputBit)
                {
                    Int32 count = changed[inputBit * outputBits + outputBit];
                    Double absoluteBias = Math.Abs((Double)count / samples - 0.5d);
                    Double pValue = probabilities[Math.Min(count, samples - count)];

                    if (Math.Min(1.0d, pValue * tests) < SIGNIFICANCE_LEVEL)
                        ++significant;

                    sumAbsoluteBias += absoluteBias;
                    if (absoluteBias > maximumBias)
                    {
                        maximumBias = absoluteBias;
                        maximumBiasInputBit = inputBit;
                        maximumBiasOutputBit = outputBit;
                    }
                }
            }

            for (Int32 distance = 0; distance <= outputBits; ++distance)
                sumDistance += distance * (Double)hamming[distance];

            Double meanAbsoluteBias = sumAbsoluteBias / tests;
            Double observedHammingMean = sumDistance / trials;
            var metadata = new
            {
                SchemaVersion = 1,
                Analysis = "avalanche",
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
                HashEvaluations = (Int64)samples * (inputBits + 1L),
                BitFlipTrials = trials,
                SacCells = tests,
                BitNumbering = "Least significant bit first within each byte; bytes follow ComputeHashBytes order",
                SignificanceTest = "Exact two-sided binomial test, null p=0.5, Bonferroni family-wise correction",
                SignificanceLevel = SIGNIFICANCE_LEVEL,
                SignificantCells = significant,
                MeanAbsoluteBias = meanAbsoluteBias,
                MaximumAbsoluteBias = maximumBias,
                MaximumBiasInputBit = maximumBiasInputBit,
                MaximumBiasOutputBit = maximumBiasOutputBit,
                MeanChangedOutputBits = observedHammingMean,
                IdealMeanChangedOutputBits = outputBits / 2.0d,
                UtcTimestamp = DateTimeOffset.UtcNow,
                LibraryVersion = typeof(Hash).Assembly.GetName().Version?.ToString(),
                Runtime = RuntimeInformation.FrameworkDescription,
                OperatingSystem = RuntimeInformation.OSDescription,
                ProcessArchitecture = RuntimeInformation.ProcessArchitecture.ToString(),
                OutputFiles = new[] { "avalanche.csv", "avalanche-hamming.csv", "avalanche-heatmap.svg", "avalanche-hamming.svg", "avalanche.json", "avalanche.txt" }
            };

            return Utilities.WriteArtifacts(hashName, directory =>
            {
                using (StreamWriter writer = CreateWriter(Path.Combine(directory, "avalanche.csv")))
                {
                    writer.WriteLine("InputBit,OutputBit,Changed,Trials,Probability,Bias,AbsoluteBias,PValue,BonferroniPValue");
    
                    for (Int32 inputBit = 0; inputBit < inputBits; ++inputBit)
                    {
                        for (Int32 outputBit = 0; outputBit < outputBits; ++outputBit)
                        {
                            Int32 count = changed[inputBit * outputBits + outputBit];
                            Double probability = (Double)count / samples;
                            Double bias = probability - 0.5d;
                            Double absoluteBias = Math.Abs(bias);
                            Double pValue = probabilities[Math.Min(count, samples - count)];
                            Double adjusted = Math.Min(1.0d, pValue * tests);
    
                            writer.WriteLine(String.Join(",",
                                inputBit.ToString(CultureInfo.InvariantCulture),
                                outputBit.ToString(CultureInfo.InvariantCulture),
                                count.ToString(CultureInfo.InvariantCulture),
                                samples.ToString(CultureInfo.InvariantCulture),
                                Format(probability), Format(bias), Format(absoluteBias), Format(pValue), Format(adjusted)));
                        }
                    }
                }
    
                using (StreamWriter writer = CreateWriter(Path.Combine(directory, "avalanche-hamming.csv")))
                {
                    writer.WriteLine("ChangedBits,Count,Probability");
    
                    for (Int32 distance = 0; distance <= outputBits; ++distance)
                    {
                        writer.WriteLine($"{distance},{hamming[distance].ToString(CultureInfo.InvariantCulture)},{Format((Double)hamming[distance] / trials)}");
                    }
                }
    
                AvalancheSvg.WriteHeatmap(Path.Combine(directory, "avalanche-heatmap.svg"), changed, inputBits, outputBits, samples, meanAbsoluteBias, maximumBias, significant);
                AvalancheSvg.WriteHistogram(Path.Combine(directory, "avalanche-hamming.svg"), hamming, trials, outputBits);

                File.WriteAllText(Path.Combine(directory, "avalanche.json"), JsonSerializer.Serialize(metadata, new JsonSerializerOptions { WriteIndented = true }) + "\n", new UTF8Encoding(false));
                File.WriteAllText(Path.Combine(directory, "avalanche.txt"),
                    $"Analysis: avalanche (AN-001)\nHash: {hashName}\nHash seed: {(hashSeed.HasValue ? hashSeed.Value.ToString(CultureInfo.InvariantCulture) : "default configuration")}\nGenerator seed: {generatorSeed}\nSamples per input bit: {samples}\nInput bits: {inputBits}\nOutput bits: {outputBits}\nHash evaluations: {metadata.HashEvaluations}\nBit-flip trials: {trials}\nSAC cells: {tests}\nMean absolute SAC bias: {Format(meanAbsoluteBias)}\nMaximum absolute SAC bias: {Format(maximumBias)} (input bit {maximumBiasInputBit}, output bit {maximumBiasOutputBit})\nBonferroni-significant cells (family-wise alpha 0.05): {significant}\nMean flipped output bits: {Format(observedHammingMean)} (ideal {Format(outputBits / 2.0d)})\nSignificance: exact two-sided binomial, Bonferroni adjusted\nNote: a non-significant result does not establish SAC compliance.\n",
                    new UTF8Encoding(false));
            }, outputDirectory);
        }

        private static void CheckDigest(Byte[] digest, Int32 expectedBytes)
        {
            if ((digest == null) || (digest.Length != expectedBytes))
                throw new InvalidOperationException("The hash returned an invalid digest length.");
        }

        // P(X <= k), for k <= floor(n/2), with X distributed Binomial(n, 0.5).
        // The central mass is computed using a log-factorial table and lower masses by recurrence.
        private static Double[] BinomialTailProbabilities(Int32 samples)
        {
            Double[] logFactorials = new Double[samples + 1];

            for (Int32 i = 2; i <= samples; ++i)
                logFactorials[i] = logFactorials[i - 1] + Math.Log(i);

            Int32 middle = samples / 2;
            Double[] masses = new Double[middle + 1];
            Double[] tails = new Double[middle + 1];
            masses[middle] = Math.Exp(logFactorials[samples] - logFactorials[middle] - logFactorials[samples - middle] - samples * Math.Log(2.0d));

            for (Int32 k = middle; k > 0; --k)
                masses[k - 1] = masses[k] * k / (samples - k + 1.0d);

            Double cumulative = 0.0d;
            for (Int32 k = 0; k <= middle; ++k)
            {
                cumulative += masses[k];
                tails[k] = Math.Min(1.0d, 2.0d * cumulative);
            }

            return tails;
        }

        private static StreamWriter CreateWriter(String path)
        {
            return new StreamWriter(path, false, new UTF8Encoding(false));
        }

        private static String Format(Double value)
        {
            return value.ToString("G17", CultureInfo.InvariantCulture);
        }
        #endregion
    }
}
