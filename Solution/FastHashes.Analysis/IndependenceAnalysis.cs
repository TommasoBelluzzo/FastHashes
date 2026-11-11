#region Using Directives
using System;
using System.Collections.Generic;
using System.Globalization;
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
        private const Int64 MAXIMUM_PAIR_TRIALS = 20000000000;
        private const Double PLOT_HEATMAP_SCALE = 0.10d;
        #endregion

        #region Methods
        private static Boolean ExpectedCountsSufficient(Int32 n00, Int32 n01, Int32 n10, Int32 n11, Int32 samples)
        {
            Int32 rowZero = n00 + n01;
            Int32 rowOne = n10 + n11;
            Int32 columnZero = n00 + n10;
            Int32 columnOne = n01 + n11;

            Boolean sufficient =
                    (Double)rowZero * columnZero / samples >= 5.0d &&
                   (Double)rowZero * columnOne / samples >= 5.0d &&
                   (Double)rowOne * columnZero / samples >= 5.0d &&
                   (Double)rowOne * columnOne / samples >= 5.0d;

            return sufficient;
        }

        private static void PlotBic(
            String path, Int32 samples, Int32 inputBits, Int32 outputBits,
            Int64 validTests, Int64 significantTests,
            Int32[] outputDefinedPhi, Double[] outputMeanAbsolutePhi,
            Double meanAbsolutePhi, Double maximumAbsolutePhi
        )
        {
            String mean = Double.IsNaN(meanAbsolutePhi) ? "N/A" : meanAbsolutePhi.ToString("0.######", CultureInfo.InvariantCulture);
            String maximum = Double.IsNaN(maximumAbsolutePhi) ? "N/A" : maximumAbsolutePhi.ToString("0.######", CultureInfo.InvariantCulture);

            using SvgPlot plot = new(path, 1200, 900);

            plot.DefinePlotArea(125.0d, 180.0d, 610.0d, 610.0d);
            plot.DefineDataRange(0.0d, outputBits, 0.0d, outputBits);

            Int32 step = outputBits <= 64 ? 8 : outputBits <= 128 ? 16 : 32;
            plot.ConfigureIndexedAxisX("Output Bit A (LSB-first)", outputBits, step);
            plot.ConfigureIndexedAxisY("Output Bit B (LSB-first)", outputBits, step);
            
            plot.ConfigureColorbar(
                "Mean |phi|",
                0.0d, PLOT_HEATMAP_SCALE,
                "#ffffff", plot.ObservedColor,
                intervals: 3, saturatedUpper: true, format: "0.##"
            );

            plot.DrawTitle("BIT INDEPENDENCE CRITERION");
            plot.DrawSubtitle("Mean absolute phi across input-bit perturbations; black denotes undefined pairs and the excluded diagonal.");

            plot.DrawMetadata(
                $"Samples: {samples:N0} | Input Bits: {inputBits:N0} | Output Bits: {outputBits:N0}",
                $"Significant Tests: {significantTests:N0} / {validTests:N0}",
                $"Mean |phi|: {mean} | Maximum |phi|: {maximum}"
            );

            plot.PrepareLayout();
            
            plot.BeginPlot();

            plot.Rectangle(plot.PlotLeft, plot.PlotTop, plot.PlotWidth, plot.PlotHeight, "#000000");

            Int32 pairIndex = 0;

            for (Int32 a = 0; a < outputBits; ++a)
            {
                for (Int32 b = a + 1; b < outputBits; ++b)
                {
                    if (outputDefinedPhi[pairIndex] > 0)
                    {
                        String color = plot.GetColor(outputMeanAbsolutePhi[pairIndex], 0.0d, PLOT_HEATMAP_SCALE, "#ffffff", plot.ObservedColor);
                        plot.HeatmapCell(a, b, outputBits, outputBits, color);
                        plot.HeatmapCell(b, a, outputBits, outputBits, color);
                    }

                    ++pairIndex;
                }
            }

            for (Int32 bit = 0; bit < outputBits; ++bit)
                plot.HeatmapCell(bit, bit, outputBits, outputBits, "#000000", 0.0d);

            for (Int32 bit = 8; bit < outputBits; bit += 8)
            {
                Double x = plot.PlotLeft + bit * plot.PlotWidth / outputBits;
                Double y = plot.PlotTop + bit * plot.PlotHeight / outputBits;
                plot.Line(x, plot.PlotTop, x, plot.PlotTop + plot.PlotHeight, plot.MutedColor, 0.8d, opacity: 0.22d);
                plot.Line(plot.PlotLeft, y, plot.PlotLeft + plot.PlotWidth, y, plot.MutedColor, 0.8d, opacity: 0.22d);
            }

            plot.EndPlot();

            plot.DrawBorder();
            plot.DrawAxes();
            plot.DrawColorbar();
        }

        public static String Run(Config config, String outputDirectory = null)
        {
            Hash hash = config.Hash;
            String hashName = config.HashName;
            UInt64 generatorSeed = config.GeneratorSeed;
            Int32 samples = config.Samples;
            Int32 keysLength = config.KeysLength;
            Double significance = config.Significance;

            Int32 inputBits = checked(keysLength * 8);
            Int32 outputBits = hash.Length;
            Int32 outputBytes = outputBits / 8;
            Int32 outputPairs = checked((outputBits * (outputBits - 1)) / 2);

            Int64 bitFlipTrials = (Int64)samples * inputBits;
            Int64 hashEvaluations = samples + bitFlipTrials;
            Int64 flipCells = (Int64)inputBits * outputBits;
            Int64 pairCells = (Int64)inputBits * outputPairs;

            if (hashEvaluations > MAXIMUM_HASH_EVALUATIONS)
                throw new ArgumentException($"Independence workload exceeds {MAXIMUM_HASH_EVALUATIONS} hash evaluations; reduce --samples and/or --keys-length parameter value.");

            if (pairCells > MAXIMUM_PAIR_CELLS)
                throw new ArgumentException($"Independence matrix exceeds {MAXIMUM_PAIR_CELLS} pair cells; reduce the --keys-length parameter value.");

            Int64 pairTrials = pairCells * samples;

            if (pairTrials > MAXIMUM_PAIR_TRIALS)
                throw new ArgumentException($"Independence workload exceeds {MAXIMUM_PAIR_TRIALS} pair trials; reduce --samples and/or --keys-length parameter value.");

            Int32[] flipCounts = new Int32[checked((Int32)flipCells)];
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

            Int64 validTests = 0;
            Int64 degenerateTests = 0;
            Int64 insufficientTests = 0;
            Int64 significantTests = 0;

            Int64[] inputValidTests = new Int64[inputBits];
            Int64[] inputDegenerateTests = new Int64[inputBits];
            Int64[] inputInsufficientTests = new Int64[inputBits];
            Int64[] inputSignificantTests = new Int64[inputBits];

            Int32[] outputDefinedPhi = new Int32[outputPairs];
            Double[] outputMeanAbsolutePhi = new Double[outputPairs];

            Int32[] inputDefinedPhi = new Int32[inputBits];
            Double[] inputSumAbsolutePhi = new Double[inputBits];
            Double[] inputMaximumAbsolutePhi = new Double[inputBits];
            Int32[] inputMaximumPhiOutputBitA = new Int32[inputBits];
            Int32[] inputMaximumPhiOutputBitB = new Int32[inputBits];

            Double sumAbsolutePhi = 0.0d;
            Double maximumAbsolutePhi = 0.0d;
            Int32 maximumPhiInputBit = 0;
            Int32 maximumPhiOutputBitA = 0;
            Int32 maximumPhiOutputBitB = 1;

            PriorityQueue<IndependenceExtreme, Double> extremes = new();

            for (Int32 ib = 0; ib < inputBits; ++ib)
            {
                Int32 pairIndex = 0;
                Int32 flipOffset = ib * outputBits;
                Int32 jointOffset = ib * outputPairs;

                for (Int32 a = 0; a < outputBits; ++a)
                {
                    for (Int32 b = a + 1; b < outputBits; ++b)
                    {
                        Int32 n11 = jointCounts[jointOffset + pairIndex];
                        Int32 n10 = flipCounts[flipOffset + a] - n11;
                        Int32 n01 = flipCounts[flipOffset + b] - n11;
                        Int32 n00 = samples - n11 - n10 - n01;
                        Double phi = MathUtilities.PhiCoefficient(n00, n01, n10, n11);

                        if (Double.IsNaN(phi))
                        {
                            ++degenerateTests;
                            ++inputDegenerateTests[ib];
                        }
                        else
                        {
                            Double phiAbsolute = Math.Abs(phi);
                            sumAbsolutePhi += phiAbsolute;

                            ++outputDefinedPhi[pairIndex];
                            outputMeanAbsolutePhi[pairIndex] += phiAbsolute;

                            ++inputDefinedPhi[ib];
                            inputSumAbsolutePhi[ib] += phiAbsolute;

                            if ((inputDefinedPhi[ib] == 1) || (phiAbsolute > inputMaximumAbsolutePhi[ib]))
                            {
                                inputMaximumAbsolutePhi[ib] = phiAbsolute;
                                inputMaximumPhiOutputBitA[ib] = a;
                                inputMaximumPhiOutputBitB[ib] = b;
                            }

                            IndependenceExtreme extreme = new(ib, a, b, n00, n01, n10, n11, phi);
                            
                            if (extremes.Count < MAXIMUM_EXTREMES)
                                extremes.Enqueue(extreme, phiAbsolute);
                            else if (extremes.TryPeek(out _, out Double smallestExtreme) && (phiAbsolute > smallestExtreme))
                                extremes.DequeueEnqueue(extreme, phiAbsolute);

                            if (phiAbsolute > maximumAbsolutePhi)
                            {
                                maximumAbsolutePhi = phiAbsolute;
                                maximumPhiInputBit = ib;
                                maximumPhiOutputBitA = a;
                                maximumPhiOutputBitB = b;
                            }

                            if (ExpectedCountsSufficient(n00, n01, n10, n11, samples))
                            {
                                ++validTests;
                                ++inputValidTests[ib];
                            }
                            else
                            {
                                ++insufficientTests;
                                ++inputInsufficientTests[ib];
                            }
                        }

                        ++pairIndex;
                    }
                }
            }

            Double meanAbsolutePhi = degenerateTests == pairCells
                ? Double.NaN
                : (sumAbsolutePhi / (pairCells - degenerateTests));

            for (Int32 i = 0; i < outputPairs; ++i)
                if (outputDefinedPhi[i] != 0)
                    outputMeanAbsolutePhi[i] /= outputDefinedPhi[i];

            if (validTests > 0)
            {
                for (Int32 ib = 0; ib < inputBits; ++ib)
                {
                    Int32 pairIndex = 0;
                    Int32 flipOffset = ib * outputBits;
                    Int32 jointOffset = ib * outputPairs;
                    
                    for (Int32 a = 0; a < outputBits; ++a)
                    {
                        for (Int32 b = a + 1; b < outputBits; ++b)
                        {
                            Int32 n11 = jointCounts[jointOffset + pairIndex++];
                            Int32 n10 = flipCounts[flipOffset + a] - n11;
                            Int32 n01 = flipCounts[flipOffset + b] - n11;
                            Int32 n00 = samples - n11 - n10 - n01;
                            
                            if (!ExpectedCountsSufficient(n00, n01, n10, n11, samples))
                                continue;
                            
                            Double phi = MathUtilities.PhiCoefficient(n00, n01, n10, n11);
                            Double pValue = MathUtilities.Erfc(Math.Sqrt(samples / 2.0d) * Math.Abs(phi));
                            
                            if (Math.Min(1.0d, pValue * validTests) < significance)
                            {
                                ++significantTests;
                                ++inputSignificantTests[ib];
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
                
                if (comparison != 0)
                    return comparison;

                comparison = left.InputBit.CompareTo(right.InputBit);
                
                if (comparison != 0)
                    return comparison;

                comparison = left.OutputBitA.CompareTo(right.OutputBitA);
                comparison = (comparison != 0) ? comparison : left.OutputBitB.CompareTo(right.OutputBitB);

                return comparison;
            });

            Boolean hasDefinedPhi = degenerateTests < pairCells;

            Dictionary<String, Object> metadata = Utilities.CreateMetadata("independence", config);
            metadata["InputBits"] = inputBits;
            metadata["OutputBits"] = outputBits;
            metadata["OutputPairs"] = outputPairs;
            metadata["BitFlipTrials"] = bitFlipTrials;
            metadata["HashEvaluations"] = hashEvaluations;
            metadata["PairCells"] = pairCells;
            metadata["ValidTests"] = validTests;
            metadata["DegenerateTests"] = degenerateTests;
            metadata["InsufficientTests"] = insufficientTests;
            metadata["SignificantTests"] = significantTests;
            metadata["HasDefinedPhi"] = hasDefinedPhi;
            metadata["MeanAbsolutePhi"] = hasDefinedPhi ? meanAbsolutePhi : null;
            metadata["MaximumAbsolutePhi"] = hasDefinedPhi ? maximumAbsolutePhi : null;
            metadata["MaximumPhiInputBit"] = hasDefinedPhi ? maximumPhiInputBit : null;
            metadata["MaximumPhiOutputBitA"] = hasDefinedPhi ? maximumPhiOutputBitA : null;
            metadata["MaximumPhiOutputBitB"] = hasDefinedPhi ? maximumPhiOutputBitB : null;
            metadata["OutputFiles"] = new[] {
                "independence.json",
                "independence-inputs.csv", "independence-pairs.csv", "independence-extremes.csv",
                "independence-bic.svg"
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
                    writer.WriteLine("InputBit,ValidTests,DegenerateTests,InsufficientTests,SignificantTests,DefinedPhi,MeanAbsolutePhi,MaximumAbsolutePhi,MaximumOutputBitA,MaximumOutputBitB");

                    for (Int32 inputBit = 0; inputBit < inputBits; ++inputBit)
                    {
                        Int32 count = inputDefinedPhi[inputBit];

                        writer.WriteLine(String.Join(",",
                            Utilities.FormatCsvInteger(inputBit),
                            Utilities.FormatCsvInteger(inputValidTests[inputBit]),
                            Utilities.FormatCsvInteger(inputDegenerateTests[inputBit]),
                            Utilities.FormatCsvInteger(inputInsufficientTests[inputBit]),
                            Utilities.FormatCsvInteger(inputSignificantTests[inputBit]),
                            Utilities.FormatCsvInteger(count),
                            (count == 0) ? String.Empty : Utilities.FormatCsvDouble(inputSumAbsolutePhi[inputBit] / count),
                            (count == 0) ? String.Empty : Utilities.FormatCsvDouble(inputMaximumAbsolutePhi[inputBit]),
                            (count == 0) ? String.Empty : Utilities.FormatCsvInteger(inputMaximumPhiOutputBitA[inputBit]),
                            (count == 0) ? String.Empty : Utilities.FormatCsvInteger(inputMaximumPhiOutputBitB[inputBit])
                        ));
                    }
                }

                filePath = Path.Combine(directory, "independence-pairs.csv");

                using (StreamWriter writer = new(filePath, false, encoding))
                {
                    writer.WriteLine("OutputBitA,OutputBitB,DefinedPhi,MeanAbsolutePhi");

                    Int32 pairIndex = 0;

                    for (Int32 a = 0; a < outputBits; ++a)
                    {
                        for (Int32 b = a + 1; b < outputBits; ++b)
                        {
                            writer.WriteLine(String.Join(",",
                                Utilities.FormatCsvInteger(a),
                                Utilities.FormatCsvInteger(b),
                                Utilities.FormatCsvInteger(outputDefinedPhi[pairIndex]),
                                (outputDefinedPhi[pairIndex] == 0) ? String.Empty : Utilities.FormatCsvDouble(outputMeanAbsolutePhi[pairIndex])
                            ));

                            ++pairIndex;
                        }
                    }
                }

                filePath = Path.Combine(directory, "independence-extremes.csv");

                using (StreamWriter writer = new(filePath, false, encoding))
                {
                    writer.WriteLine("InputBit,OutputBitA,OutputBitB,Trials,N00,N01,N10,N11,TestStatus,Phi,AbsolutePhi,PValue,BonferroniPValue");

                    foreach (IndependenceExtreme extreme in strongest)
                    {
                        Boolean valid = ExpectedCountsSufficient(extreme.N00, extreme.N01, extreme.N10, extreme.N11, samples);
                        Double pValue = valid ? MathUtilities.Erfc(Math.Sqrt(samples / 2.0d) * Math.Abs(extreme.Phi)) : Double.NaN;

                        writer.WriteLine(String.Join(",",
                            Utilities.FormatCsvInteger(extreme.InputBit),
                            Utilities.FormatCsvInteger(extreme.OutputBitA),
                            Utilities.FormatCsvInteger(extreme.OutputBitB),
                            Utilities.FormatCsvInteger(samples),
                            Utilities.FormatCsvInteger(extreme.N00),
                            Utilities.FormatCsvInteger(extreme.N01),
                            Utilities.FormatCsvInteger(extreme.N10),
                            Utilities.FormatCsvInteger(extreme.N11),
                            valid ? "Valid" : "InsufficientExpectedCounts",
                            Utilities.FormatCsvDouble(extreme.Phi),
                            Utilities.FormatCsvDouble(Math.Abs(extreme.Phi)),
                            valid ? Utilities.FormatCsvDouble(pValue) : String.Empty,
                            valid ? Utilities.FormatCsvDouble(Math.Min(1.0d, pValue * validTests)) : String.Empty
                        ));
                    }
                }

                filePath = Path.Combine(directory, "independence-bic.svg");
                PlotBic(filePath, samples, inputBits, outputBits, validTests, significantTests, outputDefinedPhi, outputMeanAbsolutePhi, meanAbsolutePhi, maximumAbsolutePhi);
            }

            return Utilities.WriteArtifacts(hashName, writeFunction, outputDirectory);
        }
        #endregion

        #region Records
        private readonly record struct IndependenceExtreme(
            Int32 InputBit,
            Int32 OutputBitA,
            Int32 OutputBitB,
            Int32 N00,
            Int32 N01,
            Int32 N10,
            Int32 N11,
            Double Phi
        );
        #endregion
    }
}