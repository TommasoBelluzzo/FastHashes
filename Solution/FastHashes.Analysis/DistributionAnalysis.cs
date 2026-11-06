#region Using Directives
using System;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
#endregion

namespace FastHashes.Analysis
{
    /// <summary>Output-bit balance and output-byte distribution analysis over deterministic random input keys.</summary>
    internal static class DistributionAnalysis
    {
        #region Setup
        private const Double SIGNIFICANCE_LEVEL = 0.05d;
        private const Double MINIMUM_EXPECTED_FREQUENCY = 5.0d;
        #endregion

        #region Methods
        public static void Validate(Int32 hashLength, Int32 samples, Int32 keyLength)
        {
            if ((hashLength <= 0) || ((hashLength % 8) != 0))
                throw new NotSupportedException("Distribution analysis requires a positive, byte-aligned hash length.");

            if (samples < 1)
                throw new ArgumentOutOfRangeException(nameof(samples), "Distribution analysis requires at least one sample.");

            if (keyLength < 1)
                throw new ArgumentOutOfRangeException(nameof(keyLength), "Distribution analysis requires a nonempty input key.");
        }

        public static String Run(Hash hash, String hashName, UInt64? hashSeed, String hashVersion, UInt64 generatorSeed, Int32 samples, Int32 keyLength, String outputDirectory = null)
        {
            Validate(hash.Length, samples, keyLength);

            Int32 outputBits = hash.Length;
            Int32 outputBytes = outputBits / 8;
            Int32[] bitOnes = new Int32[outputBits];
            Int32[] byteCounts = new Int32[checked(outputBytes * 256)];
            Int32[] pooledByteCounts = new Int32[256];
            RandomSplitMix generator = new(generatorSeed);
            Byte[] key = new Byte[keyLength];

            // Complete all hashing and numerical calculations before creating report directories.
            for (Int32 sample = 0; sample < samples; ++sample)
            {
                generator.NextBytes(key);
                Byte[] digest = hash.ComputeHashBytes(key);

                if ((digest == null) || (digest.Length != outputBytes))
                    throw new InvalidOperationException("The hash returned an invalid digest length.");

                for (Int32 position = 0; position < outputBytes; ++position)
                {
                    Byte value = digest[position];
                    ++byteCounts[position * 256 + value];
                    ++pooledByteCounts[value];

                    for (Int32 bit = 0; bit < 8; ++bit)
                    {
                        if ((value & (1 << bit)) != 0)
                            ++bitOnes[position * 8 + bit];
                    }
                }
            }

            Double[] exactTails = BinomialTailProbabilities(samples);
            Double[] byteEntropies = new Double[outputBytes];
            Double[] byteChiSquare = new Double[outputBytes];
            Int64 significantBits = 0;
            Double sumAbsoluteBias = 0.0d;
            Double maximumAbsoluteBias = 0.0d;
            Int32 maximumBiasBit = 0;
            Int64 totalSetBits = 0;

            for (Int32 bit = 0; bit < outputBits; ++bit)
            {
                Int32 ones = bitOnes[bit];
                totalSetBits += ones;
                Double bias = Math.Abs((Double)ones / samples - 0.5d);
                sumAbsoluteBias += bias;
                if (bias > maximumAbsoluteBias)
                {
                    maximumAbsoluteBias = bias;
                    maximumBiasBit = bit;
                }
                Double pValue = exactTails[Math.Min(ones, samples - ones)];
                Double adjusted = Math.Min(1.0d, pValue * outputBits);
                if (adjusted < SIGNIFICANCE_LEVEL)
                    ++significantBits;
            }

            for (Int32 position = 0; position < outputBytes; ++position)
            {
                Double entropy = 0.0d;
                Double chiSquare = 0.0d;
                Double expected = samples / 256.0d;
                for (Int32 value = 0; value < 256; ++value)
                {
                    Int32 count = byteCounts[position * 256 + value];
                    if (count > 0)
                    {
                        Double probability = (Double)count / samples;
                        entropy -= probability * Math.Log2(probability);
                    }
                    Double difference = count - expected;
                    chiSquare += difference * difference / expected;
                }
                byteEntropies[position] = entropy;
                byteChiSquare[position] = chiSquare;
            }

            Double pooledEntropy = Entropy(pooledByteCounts, checked((Int64)samples * outputBytes));
            Double pooledChiSquare = ChiSquare(pooledByteCounts, checked((Int64)samples * outputBytes));
            Double meanEntropy = 0.0d;
            Double minimumEntropy = 8.0d;
            Double maximumEntropy = 0.0d;

            foreach (Double entropy in byteEntropies)
            {
                meanEntropy += entropy;
                minimumEntropy = Math.Min(minimumEntropy, entropy);
                maximumEntropy = Math.Max(maximumEntropy, entropy);
            }
            meanEntropy /= outputBytes;

            Double meanAbsoluteBias = sumAbsoluteBias / outputBits;
            Double observedOneFraction = (Double)totalSetBits / ((Int64)samples * outputBits);
            Boolean byteChiSquareEligible = samples / 256.0d >= MINIMUM_EXPECTED_FREQUENCY;

            return Utilities.WriteArtifacts(hashName, directory =>
            {
                using (StreamWriter writer = Writer(Path.Combine(directory, "distribution-bits.csv")))
                {
                    writer.WriteLine("OutputBit,Ones,Zeros,Trials,OneProbability,SignedBias,AbsoluteBias,PValue,BonferroniPValue");
                    for (Int32 bit = 0; bit < outputBits; ++bit)
                    {
                        Int32 ones = bitOnes[bit];
                        Double probability = (Double)ones / samples;
                        Double pValue = exactTails[Math.Min(ones, samples - ones)];
                        writer.WriteLine(String.Join(",", bit.ToString(CultureInfo.InvariantCulture), ones.ToString(CultureInfo.InvariantCulture),
                            (samples - ones).ToString(CultureInfo.InvariantCulture), samples.ToString(CultureInfo.InvariantCulture),
                            Format(probability), Format(probability - 0.5d), Format(Math.Abs(probability - 0.5d)),
                            Format(pValue), Format(Math.Min(1.0d, pValue * outputBits))));
                    }
                }

                using (StreamWriter writer = Writer(Path.Combine(directory, "distribution-bytes.csv")))
                {
                    writer.WriteLine("OutputByte,ByteValue,Count,Trials,Probability,ExpectedCount,StandardizedResidual");
                    Double expected = samples / 256.0d;
                    Double deviation = Math.Sqrt(samples * (1.0d / 256.0d) * (255.0d / 256.0d));
                    for (Int32 position = 0; position < outputBytes; ++position)
                    {
                        for (Int32 value = 0; value < 256; ++value)
                        {
                            Int32 count = byteCounts[position * 256 + value];
                            writer.WriteLine(String.Join(",", position.ToString(CultureInfo.InvariantCulture), value.ToString(CultureInfo.InvariantCulture),
                                count.ToString(CultureInfo.InvariantCulture), samples.ToString(CultureInfo.InvariantCulture),
                                Format((Double)count / samples), Format(expected), Format((count - expected) / deviation)));
                        }
                    }
                }

                using (StreamWriter writer = Writer(Path.Combine(directory, "distribution-positions.csv")))
                {
                    writer.WriteLine("OutputByte,Trials,EmpiricalEntropyBits,IdealEntropyBits,ChiSquare,DegreesOfFreedom,ChiSquareEligible");
                    for (Int32 position = 0; position < outputBytes; ++position)
                        writer.WriteLine(String.Join(",", position.ToString(CultureInfo.InvariantCulture), samples.ToString(CultureInfo.InvariantCulture),
                            Format(byteEntropies[position]), "8", Format(byteChiSquare[position]), "255", byteChiSquareEligible ? "true" : "false"));
                }

                DistributionSvg.WriteBitBalance(Path.Combine(directory, "distribution-bits.svg"), bitOnes, samples);
                DistributionSvg.WriteByteHeatmap(Path.Combine(directory, "distribution-bytes.svg"), byteCounts, samples, outputBytes);
                DistributionSvg.WriteEntropy(Path.Combine(directory, "distribution-entropy.svg"), byteEntropies, samples);

                var metadata = new
                {
                    SchemaVersion = 1,
                    Analysis = "distribution",
                    Status = "Completed",
                    QualityAssessment = "StatisticalEvidenceOnly",
                    HashName = hashName,
                    HashSeed = hashSeed,
                    HashLengthBits = outputBits,
                    GeneratorAlgorithm = "SplitMix64",
                    GeneratorSeed = generatorSeed,
                    Samples = samples,
                    KeyLengthBytes = keyLength,
                    InputCorpus = "Deterministic SplitMix64 random byte strings, one hash evaluation per sample",
                    HashEvaluations = samples,
                    BitNumbering = "Least significant bit first within each byte; bytes follow ComputeHashBytes order",
                    SignificantBits = significantBits,
                    SignificanceLevel = SIGNIFICANCE_LEVEL,
                    BitSignificanceTest = "Exact two-sided Binomial(n, 0.5); Bonferroni correction across output bits",
                    MeanAbsoluteBitBias = meanAbsoluteBias,
                    MaximumAbsoluteBitBias = maximumAbsoluteBias,
                    MaximumBiasOutputBit = maximumBiasBit,
                    ObservedOneFraction = observedOneFraction,
                    MeanBytePositionEntropyBits = meanEntropy,
                    MinimumBytePositionEntropyBits = minimumEntropy,
                    MaximumBytePositionEntropyBits = maximumEntropy,
                    PooledByteEntropyBits = pooledEntropy,
                    PooledByteChiSquare = pooledChiSquare,
                    PooledByteChiSquareInterpretation = "Descriptive only: bytes within one digest may be dependent",
                    BytePositionChiSquareEligible = byteChiSquareEligible,
                    MinimumExpectedCellCount = MINIMUM_EXPECTED_FREQUENCY,
                    BytePositionChiSquareDegreesOfFreedom = 255,
                    EntropyInterpretation = "Empirical plug-in entropy is downward biased at finite sample sizes; 8 bits is an ideal uniform byte reference",
                    UtcTimestamp = DateTimeOffset.UtcNow,
                    LibraryVersion = hashVersion,
                    Runtime = RuntimeInformation.FrameworkDescription,
                    OperatingSystem = RuntimeInformation.OSDescription,
                    ProcessArchitecture = RuntimeInformation.ProcessArchitecture.ToString(),
                    OutputFiles = new[] { "distribution-bits.csv", "distribution-bytes.csv", "distribution-positions.csv", "distribution-bits.svg", "distribution-bytes.svg", "distribution-entropy.svg", "distribution.json", "distribution.txt" }
                };

                File.WriteAllText(Path.Combine(directory, "distribution.json"),
                    JsonSerializer.Serialize(metadata, new JsonSerializerOptions { WriteIndented = true }) + "\n", new UTF8Encoding(false));
                File.WriteAllText(Path.Combine(directory, "distribution.txt"),
                    $"Analysis: distribution (AN-004)\nHash: {hashName}\nHash seed: {(hashSeed.HasValue ? hashSeed.Value.ToString(CultureInfo.InvariantCulture) : "default configuration")}\n" +
                    $"Generator seed: {generatorSeed}\nSamples: {samples}\nKey length: {keyLength} bytes\nOutput bits: {outputBits}\n" +
                    $"Mean absolute bit bias: {Format(meanAbsoluteBias)}\nMaximum absolute bit bias: {Format(maximumAbsoluteBias)} at output bit {maximumBiasBit}\n" +
                    $"Bonferroni-significant output bits: {significantBits}/{outputBits} (family-wise alpha {Format(SIGNIFICANCE_LEVEL)})\n" +
                    $"Observed fraction of output one-bits: {Format(observedOneFraction)} (ideal 0.5)\n" +
                    $"Empirical mean byte-position entropy: {Format(meanEntropy)} bits (ideal 8; finite-sample downward bias)\n" +
                    $"Empirical pooled byte entropy: {Format(pooledEntropy)} bits\nPooled byte chi-square: {Format(pooledChiSquare)} (descriptive only)\n" +
                    $"Per-position chi-square eligible (expected frequency >= {Format(MINIMUM_EXPECTED_FREQUENCY)}): {byteChiSquareEligible}\n" +
                    "Note: significance and descriptive entropy estimates do not establish random or independent output.\n", new UTF8Encoding(false));
            }, outputDirectory);
        }

        private static Double Entropy(Int32[] counts, Int64 total)
        {
            Double entropy = 0.0d;
            foreach (Int32 count in counts)
            {
                if (count == 0)
                    continue;
                Double probability = (Double)count / total;
                entropy -= probability * Math.Log2(probability);
            }
            return entropy;
        }

        private static Double ChiSquare(Int32[] counts, Int64 total)
        {
            Double expected = (Double)total / counts.Length;
            Double statistic = 0.0d;
            foreach (Int32 count in counts)
            {
                Double difference = count - expected;
                statistic += difference * difference / expected;
            }
            return statistic;
        }

        // Two-sided exact binomial tail for a symmetric Bernoulli null model.
        private static Double[] BinomialTailProbabilities(Int32 samples)
        {
            Double[] logFactorials = new Double[samples + 1];
            for (Int32 i = 2; i <= samples; ++i)
                logFactorials[i] = logFactorials[i - 1] + Math.Log(i);

            Int32 middle = samples / 2;
            Double[] mass = new Double[middle + 1];
            Double[] tails = new Double[middle + 1];
            mass[middle] = Math.Exp(logFactorials[samples] - logFactorials[middle] - logFactorials[samples - middle] - samples * Math.Log(2.0d));
            for (Int32 i = middle; i > 0; --i)
                mass[i - 1] = mass[i] * i / (samples - i + 1.0d);

            Double sum = 0.0d;
            for (Int32 i = 0; i <= middle; ++i)
            {
                sum += mass[i];
                tails[i] = Math.Min(1.0d, 2.0d * sum);
            }
            return tails;
        }

        private static StreamWriter Writer(String path)
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
