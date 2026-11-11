#region Using Directives
using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Text;
#endregion

namespace FastHashes.Analysis
{
    internal static class AvalancheAnalysis
    {
        #region Setup
        private const Int64 MAXIMUM_HASH_EVALUATIONS = 20000000;
        private const Int64 MAXIMUM_MATRIX_CELLS = 1000000;
        private const Double PLOT_HEATMAP_SCALE = 0.10d;
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
            Double significance = config.Significance;

            Int32 inputBits = checked(keysLength * 8);
            Int32 outputBits = hashLength;
            Int32 outputBytes = outputBits / 8;

            Int64 bitFlipTrials = (Int64)samples * inputBits;
            Int64 hashEvaluations = samples + bitFlipTrials;
            Int64 sacCells = (Int64)inputBits * outputBits;

            if (hashEvaluations > MAXIMUM_HASH_EVALUATIONS)
                throw new ArgumentException($"Avalanche workload exceeds {MAXIMUM_HASH_EVALUATIONS} hash evaluations; reduce --samples and/or --keys-length parameter value.");

            if (sacCells > MAXIMUM_MATRIX_CELLS)
                throw new ArgumentException($"Avalanche matrix exceeds {MAXIMUM_MATRIX_CELLS} cells; reduce the --keys-length parameter value.");

            Int32[] changed = new Int32[checked((Int32)sacCells)];
            Int64[] hamming = new Int64[outputBits + 1];

            RandomSplitMix generator = new(generatorSeed);
            Byte[] key = new Byte[keysLength];

            for (Int32 s = 0; s < samples; ++s)
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

                    Int32 distance = 0;
                    Int32 rowStart = ib * outputBits;

                    for (Int32 ob = 0; ob < outputBytes; ++ob)
                    {
                        Byte difference = (Byte)(baseline[ob] ^ mutated[ob]);
                        distance += BitOperations.PopCount((UInt32)difference);

                        for (Int32 bit = 0; bit < 8; ++bit)
                        {
                            if ((difference & (1 << bit)) != 0)
                                ++changed[rowStart + (ob * 8) + bit];
                        }
                    }

                    ++hamming[distance];
                }
            }

            Double[] probabilities = MathUtilities.BinomialTailProbabilities(samples);
            
            Int64 significantCells = 0;
            Double maximumAbsoluteBias = 0.0d;
            Double sumAbsoluteBias = 0.0d;
            Int32 maximumBiasInputBit = 0;
            Int32 maximumBiasOutputBit = 0;
            Double sumDistance = 0.0d;

            for (Int32 ib = 0; ib < inputBits; ++ib)
            {
                for (Int32 ob = 0; ob < outputBits; ++ob)
                {
                    Int32 count = changed[ib * outputBits + ob];
                    Double absoluteBias = Math.Abs(((Double)count / samples) - 0.5d);
                    Double pValue = probabilities[Math.Min(count, samples - count)];

                    if (Math.Min(1.0d, pValue * sacCells) < significance)
                        ++significantCells;

                    sumAbsoluteBias += absoluteBias;

                    if (absoluteBias > maximumAbsoluteBias)
                    {
                        maximumAbsoluteBias = absoluteBias;
                        maximumBiasInputBit = ib;
                        maximumBiasOutputBit = ob;
                    }
                }
            }

            for (Int32 d = 0; d <= outputBits; ++d)
                sumDistance += d * (Double)hamming[d];

            Double meanAbsoluteBias = sumAbsoluteBias / sacCells;
            Double meanChangedOutputBits = sumDistance / bitFlipTrials;

            Dictionary<String, Object> metadata = Utilities.CreateMetadata("avalanche", config);
            metadata["InputBits"] = inputBits;
            metadata["OutputBits"] = outputBits;
            metadata["BitFlipTrials"] = bitFlipTrials;
            metadata["HashEvaluations"] = hashEvaluations;
            metadata["SacCells"] = sacCells;
            metadata["SignificantCells"] = significantCells;
            metadata["MaximumAbsoluteBias"] = maximumAbsoluteBias;
            metadata["MeanAbsoluteBias"] = meanAbsoluteBias;
            metadata["MaximumBiasInputBit"] = maximumBiasInputBit;
            metadata["MaximumBiasOutputBit"] = maximumBiasOutputBit;
            metadata["MeanChangedOutputBits"] = meanChangedOutputBits;
            metadata["MeanChangedOutputBitsIdeal"] = outputBits / 2.0d;
            metadata["OutputFiles"] = new[] {
                "avalanche.json",
                "avalanche.csv", "avalanche-hamming.csv",
                "avalanche-hd.svg", "avalanche-sac.svg"
            };

            void writeFunction(String directory)
            {
                String filePath;
                Encoding encoding = new UTF8Encoding(false);

                filePath = Path.Combine(directory, "avalanche.json");
                File.WriteAllText(filePath, Utilities.SerializeJson(metadata) + "\n", encoding);

                filePath = Path.Combine(directory, "avalanche.csv");

                using (StreamWriter writer = new(filePath, false, encoding))
                {
                    writer.WriteLine("InputBit,OutputBit,Changed,Trials,Probability,Bias,AbsoluteBias,PValue,PValueBonferroni");

                    for (Int32 ib = 0; ib < inputBits; ++ib)
                    {
                        for (Int32 ob = 0; ob < outputBits; ++ob)
                        {
                            Int32 count = changed[(ib * outputBits) + ob];
                            Double probability = (Double)count / samples;
                            Double bias = probability - 0.5d;
                            Double absoluteBias = Math.Abs(bias);
                            Double pValue = probabilities[Math.Min(count, samples - count)];
                            Double pValueBonferroni = Math.Min(1.0d, pValue * sacCells);

                            writer.WriteLine(String.Join(",",
                                Utilities.FormatCsvInteger(ib),
                                Utilities.FormatCsvInteger(ob),
                                Utilities.FormatCsvInteger(count),
                                Utilities.FormatCsvInteger(samples),
                                Utilities.FormatCsvDouble(probability),
                                Utilities.FormatCsvDouble(bias),
                                Utilities.FormatCsvDouble(absoluteBias),
                                Utilities.FormatCsvDouble(pValue),
                                Utilities.FormatCsvDouble(pValueBonferroni)
                            ));
                        }
                    }
                }

                filePath = Path.Combine(directory, "avalanche-hamming.csv");

                using (StreamWriter writer = new(filePath, false, encoding))
                {
                    writer.WriteLine("ChangedBits,Count,Probability");

                    for (Int32 d = 0; d <= outputBits; ++d)
                    {
                        Int64 hammingCurrent = hamming[d];
                        Double probability = (Double)hammingCurrent / bitFlipTrials;

                        writer.WriteLine(String.Join(",",
                            Utilities.FormatCsvInteger(d),
                            Utilities.FormatCsvInteger(hammingCurrent),
                            Utilities.FormatCsvDouble(probability)
                        ));
                    }
                }

                filePath = Path.Combine(directory, "avalanche-hd.svg");
                PlotHd(filePath, hamming, bitFlipTrials, outputBits);

                filePath = Path.Combine(directory, "avalanche-sac.svg");
                PlotSac(filePath, changed, inputBits, outputBits, samples, meanAbsoluteBias, maximumAbsoluteBias, significantCells);
            }

            return Utilities.WriteArtifacts(hashName, writeFunction, outputDirectory);
        }
        #endregion

        #region Plots
        private static void PlotHd(String path, Int64[] hamming, Int64 trials, Int32 outputBits)
        {
            Int32 k = hamming.Length;

            Double mean = 0.0d;
            Double secondMoment = 0.0d;
            Int64 maximumCount = 0;

            for (Int32 i = 0; i < k; ++i)
            {
                mean += (Double)i * hamming[i];
                secondMoment += (Double)i * i * hamming[i];
                maximumCount = Math.Max(maximumCount, hamming[i]);
            }

            mean /= trials;

            Double standardDeviation = Math.Sqrt(Math.Max(0.0d, (secondMoment / trials) - (mean * mean)));

            Double[] logFactorials = new Double[outputBits + 1];

            for (Int32 i = 2; i <= outputBits; ++i)
                logFactorials[i] = logFactorials[i - 1] + Math.Log(i);

            Double[] expected = new Double[outputBits + 1];
            Double expectedMaximum = 0.0d;

            for (Int32 i = 0; i <= outputBits; ++i)
            {
                expected[i] = trials * Math.Exp(logFactorials[outputBits] - logFactorials[i] - logFactorials[outputBits - i] - outputBits * Math.Log(2.0d));
                expectedMaximum = Math.Max(expectedMaximum, expected[i]);
            }

            Double ceiling = Math.Max(maximumCount, expectedMaximum) * 1.12d;
            Int32 xStep = outputBits <= 64 ? 8 : outputBits <= 128 ? 16 : 32;

            List<(Double, Double)> expectedPoints = new(outputBits + 1);

            for (Int32 i = 0; i <= outputBits; ++i)
                expectedPoints.Add((i + 0.5d, expected[i]));

            using SvgPlot plot = new(path, 1200, 805);

            plot.DefinePlotArea(120, 178, 980, 500);
            plot.DefineDataRange(0.0d, k, 0.0d, ceiling);

            plot.ConfigureIndexedAxisX("Changed Output Bits", k, xStep);
            plot.ConfigureDiscreteAxisY("Number of Trials", 0.0d, ceiling, intervals: 5, format: "N0");

            plot.ConfigureLegend([
                new SvgPlot.LegendItem("Observed", plot.ObservedColor, false),
                new SvgPlot.LegendItem("Ideal Binomial", plot.ReferenceColor, true),
                new SvgPlot.LegendItem("Ideal Mean", plot.HighlightColor, true, true)
            ]);

            plot.DrawTitle("OUTPUT HAMMING DISTANCE");
            plot.DrawSubtitle("Observed distribution versus the ideal ~Binomial(output bits, 0.5) model.");
            plot.DrawMetadata(
                $"Trials: {trials:N0} | Peak Count: {maximumCount:N0}",
                $"Observed Mean: {mean:0.00} | Ideal Mean: {outputBits / 2.0d:0.00}",
                $"Observed SD: {standardDeviation:0.00} | Ideal SD: {Math.Sqrt(outputBits * 0.25d):0.00}"
            );

            plot.PrepareLayout();
            plot.DrawGrid();

            plot.BeginPlot();

            for (Int32 i = 0; i < k; ++i)
                plot.Bar(i, k, hamming[i], plot.ObservedColor);

            plot.Polyline(expectedPoints, plot.ReferenceColor, 2.8d);
            plot.VerticalReference(outputBits / 2.0d + 0.5d, plot.HighlightColor, 2.0d, true);

            plot.EndPlot();

            plot.DrawBorder();
            plot.DrawAxes();
            plot.DrawLegend();
        }

        private static void PlotSac(String path, Int32[] changed, Int32 inputBits, Int32 outputBits, Int32 samples, Double meanAbsoluteBias, Double maximumAbsoluteBias, Int64 significantCells)
        {
            using SvgPlot plot = new(path, 1200, 900);

            plot.DefinePlotArea(125, 180, 850, 610);
            plot.DefineDataRange(0.0d, outputBits, 0.0d, inputBits);

            Int32 xStep = (outputBits <= 64) ? 8 : (outputBits <= 128) ? 16 : 32;
            plot.ConfigureIndexedAxisX("Output Bit Index (LSB-first)", outputBits, xStep);

            Int32 yStep = (inputBits <= 64) ? 8 : (inputBits <= 256) ? 32 : 64;
            plot.ConfigureIndexedAxisY("Input Bit Index", inputBits, yStep);

            plot.ConfigureColorbar(
                "Absolute Bias",
                0.0d, PLOT_HEATMAP_SCALE * 100.0d,
                "#ffffff", plot.ObservedColor,
                intervals: 5, saturatedUpper: true, format: "0.##", suffix: "%"
            );

            plot.DrawTitle("STRICT AVALANCHE CRITERION");
            plot.DrawSubtitle("Absolute bit-flip bias: |P(output bit changes) - 0.5| for each input/output bit pair.");
            plot.DrawMetadata(
                $"Samples: {samples:N0} | Input Bits: {inputBits:N0} | Output Bits: {outputBits:N0}",
                $"Mean Bias: {meanAbsoluteBias:P2} | Maximum Bias: {maximumAbsoluteBias:P2} | Significant Cells: {significantCells:N0}"
            );

            plot.PrepareLayout();

            plot.BeginPlot();

            for (Int32 ib = 0; ib < inputBits; ++ib)
            {
                for (Int32 ob = 0; ob < outputBits; ++ob)
                {
                    Double probability = (Double)changed[(ib * outputBits) + ob] / samples;
                    Double absoluteBias = Math.Abs(probability - 0.5d);

                    plot.HeatmapCell(
                        ob, ib, outputBits, inputBits,
                        plot.GetColor(absoluteBias, 0.0d, PLOT_HEATMAP_SCALE, "#ffffff", plot.ObservedColor)
                    );
                }
            }

            for (Int32 bit = 8; bit < outputBits; bit += 8)
            {
                Double x = plot.PlotLeft + bit * plot.PlotWidth / outputBits;
                plot.Line(x, plot.PlotTop, x, plot.PlotTop + plot.PlotHeight, plot.MutedColor, 0.8d, opacity: 0.25d);
            }

            for (Int32 bit = 64; bit < inputBits; bit += 64)
            {
                Double y = plot.PlotTop + bit * plot.PlotHeight / inputBits;
                plot.Line(plot.PlotLeft, y, plot.PlotLeft + plot.PlotWidth, y, plot.MutedColor, 0.8d, opacity: 0.25d);
            }

            plot.EndPlot();

            plot.DrawBorder();
            plot.DrawAxes();
            plot.DrawColorbar();
        }
        #endregion
    }
}