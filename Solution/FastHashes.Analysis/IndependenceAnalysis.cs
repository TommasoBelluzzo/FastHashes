#region Using Directives
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
#endregion

namespace FastHashes.Analysis
{
    internal static class IndependenceAnalysis
    {
        #region Setup
        private const Int32 MAXIMUM_EXTREMES = 100;
        private const Int64 MAXIMUM_HASH_EVALUATIONS = 20000000;
        private const Int64 MAXIMUM_PAIR_CELLS = 20000000;
        private const Int64 MAXIMUM_PAIR_TRIALS = 2000000000;
        private const Double SIGNIFICANCE_LEVEL = 0.05d;
        #endregion

        #region Methods
        public static String Run(Config config, String outputDirectory = null)
        {
            Hash hash = config.Hash;
            String hashName = config.HashName;
            UInt64 generatorSeed = config.GeneratorSeed;
            Int32 samples = config.Samples;
            Int32 keysLength = config.KeysLength;

            Int32 inputBits = checked(keysLength * 8);
            Int32 outputBits = hash.Length;
            Int32 outputBytes = outputBits / 8;
            Int32 outputPairs = checked(outputBits * (outputBits - 1) / 2);

            Int64 evaluations = (Int64)samples * (inputBits + 1L);
            Int64 pairCells = (Int64)inputBits * outputPairs;
            Int64 pairTrials = pairCells * samples;

            if (evaluations > MAXIMUM_HASH_EVALUATIONS)
                throw new ArgumentException($"Independence workload exceeds {MAXIMUM_HASH_EVALUATIONS} hash evaluations; reduce --samples and/or --keys-length parameter value.");

            if (pairCells > MAXIMUM_PAIR_CELLS)
                throw new ArgumentException($"Independence matrix exceeds {MAXIMUM_PAIR_CELLS} pair cells; reduce the --keys-length parameter value.");

            if (pairTrials > MAXIMUM_PAIR_TRIALS)
                throw new ArgumentException($"Independence workload exceeds {MAXIMUM_PAIR_TRIALS} pair trials; reduce --samples and/or --keys-length parameter value.");

            Int32[] flipCounts = new Int32[checked(inputBits * outputBits)];
            Int32[] jointCounts = new Int32[checked((Int32)pairCells)];
            Int32[] flippedBits = new Int32[outputBits];

            RandomSplitMix generator = new(generatorSeed);
            Byte[] key = new Byte[keysLength];

            for (Int32 sample = 0; sample < samples; ++sample)
            {
                generator.NextBytes(key);

                Byte[] baseline = hash.ComputeHashBytes(key);

                for (Int32 ib = 0; ib < inputBits; ++ib)
                {
                    Int32 keyIndex = ib / 8;

                    Byte mask = (Byte)(1 << (ib % 8));
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

                    Int32 count = 0;
                    Int32 flipOffset = ib * outputBits;
                    Int32 pairOffset = ib * outputPairs;

                    for (Int32 ob = 0; ob < outputBytes; ++ob)
                    {
                        Byte difference = (Byte)(baseline[ob] ^ mutated[ob]);

                        for (Int32 bit = 0; bit < 8; ++bit)
                        {
                            if ((difference & (1 << bit)) == 0)
                                continue;

                            Int32 outputBit = ob * 8 + bit;
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
            Int64 testCount = (Int64)inputBits * outputPairs;
            Double[] meanAbsoluteCorrelation = new Double[outputPairs];
            Int32[] pairValid = new Int32[outputPairs];
            Int64 validTests = 0;
            Int64 degenerateTests = 0;
            Int64 insufficientExpectedTests = 0;
            Double sumAbsoluteCorrelation = 0.0d;
            Double maximumAbsoluteCorrelation = 0.0d;
            Int32 maximumInputBit = 0;
            Int32 maximumOutputA = 0;
            Int32 maximumOutputB = 1;
            Int64[] inputValidTests = new Int64[inputBits];
            Int64[] inputDegenerateTests = new Int64[inputBits];
            Int64[] inputInsufficientTests = new Int64[inputBits];
            Int64[] inputSignificantTests = new Int64[inputBits];
            Int32[] inputDefinedCorrelations = new Int32[inputBits];
            Double[] inputSumAbsoluteCorrelation = new Double[inputBits];
            Double[] inputMaximumAbsoluteCorrelation = new Double[inputBits];
            Int32[] inputMaximumOutputA = new Int32[inputBits];
            Int32[] inputMaximumOutputB = new Int32[inputBits];
            PriorityQueue<IndependenceExtreme, Double> extremes = new();

            for (Int32 inputBit = 0; inputBit < inputBits; ++inputBit)
            {
                Int32 pairIndex = 0;
                Int32 flipOffset = inputBit * outputBits;
                Int32 jointOffset = inputBit * outputPairs;

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
                        {
                            ++degenerateTests;
                            ++inputDegenerateTests[inputBit];
                        }
                        else
                        {
                            Double absolute = Math.Abs(correlation);
                            sumAbsoluteCorrelation += absolute;
                            meanAbsoluteCorrelation[pairIndex] += absolute;
                            ++pairValid[pairIndex];
                            inputSumAbsoluteCorrelation[inputBit] += absolute;
                            ++inputDefinedCorrelations[inputBit];

                            if (inputDefinedCorrelations[inputBit] == 1 || absolute > inputMaximumAbsoluteCorrelation[inputBit])
                            {
                                inputMaximumAbsoluteCorrelation[inputBit] = absolute;
                                inputMaximumOutputA[inputBit] = a;
                                inputMaximumOutputB[inputBit] = b;
                            }

                            IndependenceExtreme extreme = new(inputBit, a, b, n00, n01, n10, n11, correlation);
                            if (extremes.Count < MAXIMUM_EXTREMES)
                                extremes.Enqueue(extreme, absolute);
                            else if (extremes.TryPeek(out _, out Double smallest) && absolute > smallest)
                                extremes.DequeueEnqueue(extreme, absolute);

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
                                ++inputValidTests[inputBit];
                            }
                            else
                            {
                                ++insufficientExpectedTests;
                                ++inputInsufficientTests[inputBit];
                            }
                        }

                        ++pairIndex;
                    }
                }
            }

            Int64 significantTests = 0;
            Double meanAbsolute = (degenerateTests == testCount) ? Double.NaN : (sumAbsoluteCorrelation / (testCount - degenerateTests));
            Boolean hasDefinedCorrelations = degenerateTests < testCount;

            for (Int32 i = 0; i < outputPairs; ++i)
                if (pairValid[i] != 0)
                    meanAbsoluteCorrelation[i] /= pairValid[i];

            // Bonferroni significance requires the total valid-test count from the first pass.
            if (validTests > 0)
            {
                for (Int32 inputBit = 0; inputBit < inputBits; ++inputBit)
                {
                    Int32 pairIndex = 0;
                    Int32 flipOffset = inputBit * outputBits;
                    Int32 jointOffset = inputBit * outputPairs;
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
                            {
                                ++significantTests;
                                ++inputSignificantTests[inputBit];
                            }
                        }
                    }
                }
            }

            List<IndependenceExtreme> strongest = [];

            while (extremes.TryDequeue(out IndependenceExtreme extreme, out _))
                strongest.Add(extreme);

            strongest.Sort((left, right) =>
            {
                Int32 comparison = Math.Abs(right.Phi).CompareTo(Math.Abs(left.Phi));
                if (comparison != 0) return comparison;
                comparison = left.InputBit.CompareTo(right.InputBit);
                if (comparison != 0) return comparison;
                comparison = left.OutputBitA.CompareTo(right.OutputBitA);
                return comparison != 0 ? comparison : left.OutputBitB.CompareTo(right.OutputBitB);
            });

            Dictionary<String, Object> metadata = Utilities.CreateMetadata("independence", config);
            metadata["SignificanceLevel"] = SIGNIFICANCE_LEVEL;
            metadata["InputBits"] = inputBits;
            metadata["OutputBits"] = outputBits;
            metadata["Evaluations"] = evaluations;




            metadata["OutputPairs"] = outputPairs;
            metadata["PairCells"] = testCount;
            metadata["HashEvaluations"] = (Int64)samples * (inputBits + 1L);
            metadata["BitFlipTrials"] = (Int64)samples * inputBits;
            metadata["BitNumbering"] = "Least significant bit first within each byte; bytes follow ComputeHashBytes order";
            metadata["Statistic"] = "Phi coefficient for pairwise output-bit flip indicators conditioned on each input bit";
            metadata["SignificanceTest"] = "Asymptotic Pearson chi-square (one degree of freedom), upper tail computed as erfc(sqrt(n/2)*abs(phi)); Bonferroni correction across valid tests";
            metadata["SignificanceLevel"] = SIGNIFICANCE_LEVEL;
            metadata["MinimumExpectedCellCount"] = 5;
            metadata["ValidTests"] = validTests;
            metadata["DegenerateTests"] = degenerateTests;
            metadata["InsufficientExpectedTests"] = insufficientExpectedTests;
            metadata["SignificantTests"] = significantTests;
            metadata["MeanAbsoluteCorrelation"] = hasDefinedCorrelations ? (Double?)meanAbsolute : null;
            metadata["MaximumAbsoluteCorrelation"] = hasDefinedCorrelations ? (Double?)maximumAbsoluteCorrelation : null;
            metadata["MaximumCorrelationInputBit"] = hasDefinedCorrelations ? (Int32?)maximumInputBit : null;
            metadata["MaximumCorrelationOutputBitA"] = hasDefinedCorrelations ? (Int32?)maximumOutputA : null;
            metadata["MaximumCorrelationOutputBitB"] = hasDefinedCorrelations ? (Int32?)maximumOutputB : null;
            metadata["InterpretationNotes"] = "No significant result does not prove bit independence; the heatmap is descriptive and testing uses each input-bit/output-pair contingency table";
            metadata["OutputFiles"] = new String[]
            {
                "independence.json",
                "independence-inputs.csv",
                "independence-pairs.csv",
                "independence-extremes.csv",
                "independence-heatmap.svg"
            };

            void writeFunction(String directory)
            {
                String filePath;
                Encoding encoding = new UTF8Encoding(false);

                filePath = Path.Combine(directory, "independence.json");
                File.WriteAllText(filePath, Utilities.SerializeJson(metadata) + "\n", encoding);

                filePath = Path.Combine(directory, "independence-inputs.csv");

                using (StreamWriter writer = new(filePath, false, encoding))
                {
                    writer.WriteLine("InputBit,ValidTests,DegenerateTests,InsufficientExpectedTests,MeanAbsolutePhi,MaximumAbsolutePhi,MaximumOutputBitA,MaximumOutputBitB,SignificantTests");

                    for (Int32 inputBit = 0; inputBit < inputBits; ++inputBit)
                    {
                        Int32 count = inputDefinedCorrelations[inputBit];

                        writer.WriteLine(String.Join(",",
                            Utilities.FormatCsvInteger(inputBit),
                            Utilities.FormatCsvInteger(inputValidTests[inputBit]),
                            Utilities.FormatCsvInteger(inputDegenerateTests[inputBit]),
                            Utilities.FormatCsvInteger(inputInsufficientTests[inputBit]),
                            count == 0 ? "" : Utilities.FormatCsvDouble(inputSumAbsoluteCorrelation[inputBit] / count),
                            count == 0 ? "" : Utilities.FormatCsvDouble(inputMaximumAbsoluteCorrelation[inputBit]),
                            count == 0 ? "" : Utilities.FormatCsvInteger(inputMaximumOutputA[inputBit]),
                            count == 0 ? "" : Utilities.FormatCsvInteger(inputMaximumOutputB[inputBit]),
                            Utilities.FormatCsvInteger(inputSignificantTests[inputBit])
                        ));
                    }
                }

                filePath = Path.Combine(directory, "independence-pairs.csv");

                using (StreamWriter writer = new(filePath, false, encoding))
                {
                    writer.WriteLine("OutputBitA,OutputBitB,MeanAbsolutePhi,DefinedInputBits");
                    Int32 pairIndex = 0;

                    for (Int32 a = 0; a < outputBits; ++a)
                    {
                        for (Int32 b = a + 1; b < outputBits; ++b)
                        {
                            writer.WriteLine(String.Join(",",
                                Utilities.FormatCsvInteger(a),
                                Utilities.FormatCsvInteger(b),
                                pairValid[pairIndex] == 0 ? "" : Utilities.FormatCsvDouble(meanAbsoluteCorrelation[pairIndex]),
                                Utilities.FormatCsvInteger(pairValid[pairIndex])
                            ));
                            ++pairIndex;
                        }
                    }
                }

                filePath = Path.Combine(directory, "independence-extremes.csv");

                using (StreamWriter writer = new(filePath, false, encoding))
                {
                    writer.WriteLine("InputBit,OutputBitA,OutputBitB,N00,N01,N10,N11,Trials,Phi,AbsolutePhi,PValue,BonferroniPValue,TestStatus");

                    foreach (IndependenceExtreme extreme in strongest)
                    {
                        Boolean valid = ExpectedCountsValid(extreme.N00, extreme.N01, extreme.N10, extreme.N11, samples);
                        Double p = valid ? PValue(extreme.Phi, samples) : Double.NaN;
                        writer.WriteLine(String.Join(",",
                            Utilities.FormatCsvInteger(extreme.InputBit),
                            Utilities.FormatCsvInteger(extreme.OutputBitA),
                            Utilities.FormatCsvInteger(extreme.OutputBitB),
                            Utilities.FormatCsvInteger(extreme.N00),
                            Utilities.FormatCsvInteger(extreme.N01),
                            Utilities.FormatCsvInteger(extreme.N10),
                            Utilities.FormatCsvInteger(extreme.N11),
                            Utilities.FormatCsvInteger(samples),
                            Utilities.FormatCsvDouble(extreme.Phi),
                            Utilities.FormatCsvDouble(Math.Abs(extreme.Phi)),
                            valid ? Utilities.FormatCsvDouble(p) : "",
                            valid ? Utilities.FormatCsvDouble(Math.Min(1.0d, p * validTests)) : "",
                            valid ? "Valid" : "InsufficientExpectedCounts"
                        ));
                    }
                }

                filePath = Path.Combine(directory, "independence-heatmap.svg");
                IndependenceSvg.WriteHeatmap(filePath, meanAbsoluteCorrelation, pairValid, outputBits, samples, inputBits,
                    meanAbsolute, maximumAbsoluteCorrelation, significantTests, validTests);
            }

            return Utilities.WriteArtifacts(hashName, writeFunction, outputDirectory);
        }

        private readonly record struct IndependenceExtreme(Int32 InputBit, Int32 OutputBitA, Int32 OutputBitB,
            Int32 N00, Int32 N01, Int32 N10, Int32 N11, Double Phi);

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
        // Use the nonalternating erf series at small z and the Laplace continued
        // fraction for erfc at large z, avoiding cancellation in the upper tail.
        private static Double PValue(Double phi, Int32 samples)
        {
            Double z = Math.Sqrt(samples / 2.0d) * Math.Abs(phi);
            const Double TWO_OVER_SQRT_PI = 1.1283791670955126d;
            const Double INV_SQRT_PI = 0.5641895835477563d;

            if (z == 0.0d)
                return 1.0d;

            if (z < 2.0d)
            {
                Double term = z;
                Double sum = z;
                for (Int32 n = 0; n < 100; ++n)
                {
                    term *= 2.0d * z * z / (2 * n + 3);
                    sum += term;
                    if (term <= sum * 1.0e-16d)
                        break;
                }
                return Math.Max(0.0d, 1.0d - TWO_OVER_SQRT_PI * Math.Exp(-z * z) * sum);
            }

            // erfc(z) = exp(-z*z)/sqrt(pi) / (z + 1/(2z + 2/(z + 3/(2z + ...)))).
            // Truncation counts are chosen to preserve double precision at the
            // low end of each interval while keeping repeated BIC tests economical.
            Int32 terms = z < 3.0d ? 64 : z < 4.0d ? 32 : z < 5.0d ? 24 : 16;
            Double fraction = 0.0d;
            for (Int32 n = terms; n >= 1; --n)
                fraction = n / ((n % 2 == 1 ? 2.0d * z : z) + fraction);

            return Math.Max(0.0d, Math.Min(1.0d, Math.Exp(-z * z) * INV_SQRT_PI / (z + fraction)));
        }

        #endregion
    }
}