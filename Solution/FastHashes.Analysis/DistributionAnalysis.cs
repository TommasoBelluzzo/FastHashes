#region Using Directives
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
#endregion

namespace FastHashes.Analysis
{
    internal static class DistributionAnalysis
    {
        #region Setup
        private const Double MINIMUM_EXPECTED_FREQUENCY = 5.0d;
        private const Double PLOT_RESIDUAL_SCALE = 5.0d;
        #endregion

        #region Plots
        private static void PlotEntropy(String path, Int32 samples, Int32 outputBytes, Double[] byteEntropies, Double meanEntropy, Double minimumEntropy)
        {
            Double meanDeficit = 8.0d - meanEntropy;
            Double maximumDeficit = 8.0d - minimumEntropy;
            Double ceiling = Math.Max(0.01d, maximumDeficit * 1.2d);

            using SvgPlot plot = new(path, 1200, 770);

            plot.PlotArea(125.0d, 180.0d, 950.0d, 435.0d);
            plot.DataRange(0.0d, outputBytes, 0.0d, ceiling);

            Int32 xStep = Math.Max(1, (outputBytes + 15) / 16);
            plot.ConfigureIndexedXAxis("Output Byte Position", outputBytes, xStep);
            plot.ConfigureDiscreteYAxis("Entropy Deficit", 0.0d, ceiling, intervals: 4, format: "0.###");

            plot.Title("OUTPUT-BYTE ENTROPY DEFICIT");
            plot.Subtitle("Ideal 8-bit entropy minus empirical plug-in entropy; finite-sample estimates are downward biased.");
            plot.Metadata(
                $"Output Bytes: {outputBytes:N0}",
                $"Mean Deficit: {meanDeficit:0.######} bits | Maximum Deficit: {maximumDeficit:0.######} bits"
            );

            plot.PrepareLayout();
            plot.DrawGrid();

            plot.BeginPlot();

            Double markerSize = Math.Max(3.0d, Math.Min(8.0d, plot.PlotWidth / outputBytes * 0.5d));

            for (Int32 position = 0; position < outputBytes; ++position)
            {
                Double deficit = Math.Max(0.0d, 8.0d - byteEntropies[position]);
                Double center = plot.CellX(position, outputBytes);
                Double top = plot.Y(deficit);

                plot.Line(center, plot.Y(0.0d), center, top, plot.ObservedColor, 2.0d);
                plot.Rectangle(center - markerSize / 2.0d, top - markerSize / 2.0d, markerSize, markerSize, plot.ObservedColor);
            }

            plot.EndPlot();

            plot.DrawBorder();
            plot.DrawAxes();
        }

        private static void PlotObb(String path, Int32 samples, Int32 outputBits, Int32[] bitOnes, Double meanAbsoluteBias, Double maximumAbsoluteBias)
        {
            Double limit = Math.Max(0.01d, maximumAbsoluteBias * 1.2d);

            using SvgPlot plot = new(path, 1200, 770);

            plot.PlotArea(125.0d, 180.0d, 950.0d, 435.0d);
            plot.DataRange(0.0d, outputBits, -limit, limit);

            Int32 step = outputBits <= 64 ? 8 : outputBits <= 128 ? 16 : 32;
            plot.ConfigureIndexedXAxis("Output Bit (LSB-first)", outputBits, step);
            plot.ConfigureDiscreteYAxis("Signed One-bit Bias", -limit, limit, intervals: 4, format: "P1");

            plot.ConfigureBottomLegend([
                new SvgPlot.LegendItem("Observed Bias", plot.ObservedColor, false),
                new SvgPlot.LegendItem("Zero Reference", plot.ReferenceColor, true, true)
            ]);

            plot.Title("OUTPUT-BIT BIAS");
            plot.Subtitle("Signed deviation from the ideal 50% one-bit frequency.");
            plot.Metadata(
                $"Output Bits: {outputBits:N0}",
                $"Mean Absolute Bias: {meanAbsoluteBias:0.######} | Maximum Absolute Bias: {maximumAbsoluteBias:0.######}"
            );

            plot.PrepareLayout();
            plot.DrawGrid();

            plot.BeginPlot();

            for (Int32 bit = 0; bit < outputBits; ++bit)
            {
                Double bias = (Double)bitOnes[bit] / samples - 0.5d;
                Double center = plot.CellX(bit, outputBits);
                Double stroke = Math.Max(1.2d, Math.Min(8.0d, plot.PlotWidth / outputBits * 0.8d));
                
                plot.Line(center, plot.Y(0.0d), center, plot.Y(bias), plot.ObservedColor, stroke);
            }

            plot.HorizontalReference(0.0d, plot.ReferenceColor, 2.0d, dashed: true);
            
            plot.EndPlot();

            plot.DrawBorder();
            plot.DrawAxes();
            plot.DrawBottomLegend();
        }

        private static void PlotObf(String path, Int32 samples, Int32 outputBytes, Int32[] byteCounts)
        {
            Double expectedCount = samples / 256.0d;
            Double deviation = Math.Sqrt(samples * (1.0d / 256.0d) * (255.0d / 256.0d));
            
            Int32 plotHeight = Math.Max(180, outputBytes * 24);

            using SvgPlot plot = new(path, 1200, plotHeight + 350);

            plot.PlotArea(125.0d, 180.0d, 810.0d, plotHeight);
            plot.DataRange(0.0d, 256.0d, 0.0d, outputBytes);

            Int32 yStep = Math.Max(1, (outputBytes + 11) / 12);
            plot.ConfigureIndexedXAxis("Output Byte Value", 256, 32);
            plot.ConfigureIndexedYAxis("Output Byte Position", outputBytes, yStep);

            plot.ConfigureRightColorbar(
                "Standardized Residual",
                -PLOT_RESIDUAL_SCALE, PLOT_RESIDUAL_SCALE,
                plot.ObservedColor, plot.HighlightColor,
                intervals: 4, saturated: true, format: "0", middleColor: "#ffffff", center: 0.0d
            );

            plot.Title("OUTPUT-BYTE FREQUENCY");
            plot.Subtitle("Standardized residuals versus uniform byte frequencies; blue indicates fewer observations, red indicates more observations.");
            plot.Metadata(
                $"Output Bytes: {outputBytes:N0}",
                $"Expected Occurrences/Value: {expectedCount:0.###}"
            );

            plot.PrepareLayout();
            
            plot.BeginPlot();

            for (Int32 position = 0; position < outputBytes; ++position)
            {
                for (Int32 value = 0; value < 256; ++value)
                {
                    Double residual = (byteCounts[(position * 256) + value] - expectedCount) / deviation;
                    String color = plot.DivergingColor(residual, -PLOT_RESIDUAL_SCALE, 0.0d, PLOT_RESIDUAL_SCALE, plot.ObservedColor, "#ffffff", plot.HighlightColor);

                    plot.HeatmapCell(value, position, 256, outputBytes, color);
                }
            }

            for (Int32 value = 32; value < 256; value += 32)
            {
                Double x = plot.PlotLeft + ((value * plot.PlotWidth) / 256);
                plot.Line(x, plot.PlotTop, x, plot.PlotTop + plot.PlotHeight, plot.MutedColor, 0.8d, opacity: 0.25d);
            }

            plot.EndPlot();

            plot.DrawBorder();
            plot.DrawAxes();
            plot.DrawRightColorbar();
        }
        #endregion

        #region Methods
        public static String Run(Config config, String outputDirectory = null)
        {
            Hash hash = config.Hash;
            String hashName = config.HashName;
            UInt64 generatorSeed = config.GeneratorSeed;
            Int32 samples = config.Samples;
            Int32 keysLength = config.KeysLength;
            Double significance = config.Significance;

            Int32 outputBits = hash.Length;
            Int32 outputBytes = outputBits / 8;
            Int64 byteObservations = (Int64)samples * outputBytes;

            Int32[] bitOnes = new Int32[outputBits];
            Int32[] byteCounts = new Int32[checked(outputBytes * 256)];
            Int32[] pooledByteCounts = new Int32[256];

            RandomSplitMix generator = new(generatorSeed);
            Byte[] key = new Byte[keysLength];

            for (Int32 sample = 0; sample < samples; ++sample)
            {
                generator.NextBytes(key);
                
                Byte[] hashBytes = hash.ComputeHashBytes(key);

                for (Int32 position = 0; position < outputBytes; ++position)
                {
                    Byte value = hashBytes[position];
                    ++byteCounts[(position * 256) + value];
                    ++pooledByteCounts[value];

                    for (Int32 bit = 0; bit < 8; ++bit)
                        if ((value & (1 << bit)) != 0)
                            ++bitOnes[(position * 8) + bit];
                }
            }

            Double[] binomialTails = MathUtilities.BinomialTailProbabilities(samples);
            Double[] byteEntropies = new Double[outputBytes];
            Double[] byteChiSquare = new Double[outputBytes];

            Int64 significantBits = 0;
            Int64 totalSetBits = 0;
            Double sumAbsoluteBias = 0.0d;
            Double maximumAbsoluteBias = 0.0d;
            Int32 maximumBiasBit = 0;

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

                Double pValue = binomialTails[Math.Min(ones, samples - ones)];
                
                if (Math.Min(1.0d, pValue * outputBits) < significance)
                    ++significantBits;
            }

            Double expectedCount = samples / 256.0d;

            for (Int32 position = 0; position < outputBytes; ++position)
            {
                Double entropy = 0.0d;
                Double chiSquare = 0.0d;

                for (Int32 value = 0; value < 256; ++value)
                {
                    Int32 count = byteCounts[(position * 256) + value];

                    if (count > 0)
                    {
                        Double probability = (Double)count / samples;
                        entropy -= probability * Math.Log2(probability);
                    }

                    Double difference = count - expectedCount;
                    chiSquare += difference * difference / expectedCount;
                }

                byteEntropies[position] = entropy;
                byteChiSquare[position] = chiSquare;
            }

            Double pooledEntropy = MathUtilities.Entropy(pooledByteCounts, byteObservations);
            Double pooledChiSquare = MathUtilities.ChiSquare(pooledByteCounts, byteObservations);
            Double sumEntropy = 0.0d;
            Double minimumEntropy = 8.0d;
            Double maximumEntropy = 0.0d;

            foreach (Double entropy in byteEntropies)
            {
                sumEntropy += entropy;
                minimumEntropy = Math.Min(minimumEntropy, entropy);
                maximumEntropy = Math.Max(maximumEntropy, entropy);
            }

            Double meanEntropy = sumEntropy / outputBytes;
            Double meanAbsoluteBias = sumAbsoluteBias / outputBits;
            Double observedOneFraction = (Double)totalSetBits / ((Int64)samples * outputBits);
            Boolean chiSquareEligible = expectedCount >= MINIMUM_EXPECTED_FREQUENCY;

            Dictionary<String, Object> metadata = Utilities.CreateMetadata("distribution", config);
            metadata["MinimumExpectedFrequency"] = MINIMUM_EXPECTED_FREQUENCY;
            metadata["OutputBits"] = outputBits;
            metadata["OutputBytes"] = outputBytes;
            metadata["ByteObservations"] = byteObservations;
            metadata["SignificantBits"] = significantBits;
            metadata["MeanAbsoluteBias"] = meanAbsoluteBias;
            metadata["MaximumAbsoluteBias"] = maximumAbsoluteBias;
            metadata["MaximumBiasBit"] = maximumBiasBit;
            metadata["ObservedOneFraction"] = observedOneFraction;
            metadata["MeanEntropy"] = meanEntropy;
            metadata["MinimumEntropy"] = minimumEntropy;
            metadata["MaximumEntropy"] = maximumEntropy;
            metadata["PooledEntropy"] = pooledEntropy;
            metadata["PooledChiSquare"] = pooledChiSquare;
            metadata["ChiSquareEligible"] = chiSquareEligible;
            metadata["OutputFiles"] = new [] {
                "distribution.json",
                "distribution-bits.csv", "distribution-bytes.csv", "distribution-positions.csv",
                "distribution-ed.svg", "distribution-obb.svg", "distribution-obf.svg"
            };

            void writeFunction(String directory)
            {
                String filePath;
                Encoding encoding = new UTF8Encoding(false);

                filePath = Path.Combine(directory, "distribution.json");
                File.WriteAllText(filePath, Utilities.SerializeJson(metadata) + "\n", encoding);

                filePath = Path.Combine(directory, "distribution-bits.csv");

                using (StreamWriter writer = new(filePath, false, encoding))
                {
                    writer.WriteLine("OutputBit,Trials,Ones,Zeros,OneProbability,ZeroProbability,SignedBias,AbsoluteBias,PValue,PValueBonferroni");

                    for (Int32 bit = 0; bit < outputBits; ++bit)
                    {
                        Int32 ones = bitOnes[bit];
                        Double probability = (Double)ones / samples;
                        Double pValue = binomialTails[Math.Min(ones, samples - ones)];

                        writer.WriteLine(String.Join(",",
                            Utilities.FormatCsvInteger(bit),
                            Utilities.FormatCsvInteger(samples),
                            Utilities.FormatCsvInteger(ones),
                            Utilities.FormatCsvInteger(samples - ones),
                            Utilities.FormatCsvDouble(probability),
                            Utilities.FormatCsvDouble(1.0d - probability),
                            Utilities.FormatCsvDouble(probability - 0.5d),
                            Utilities.FormatCsvDouble(Math.Abs(probability - 0.5d)),
                            Utilities.FormatCsvDouble(pValue),
                            Utilities.FormatCsvDouble(Math.Min(1.0d, pValue * outputBits))
                        ));
                    }
                }

                filePath = Path.Combine(directory, "distribution-bytes.csv");

                using (StreamWriter writer = new(filePath, false, encoding))
                {
                    writer.WriteLine("OutputByte,ByteValue,Count,Probability,ExpectedCount,StandardizedResidual");
                    
                    Double deviation = Math.Sqrt(samples * (1.0d / 256.0d) * (255.0d / 256.0d));

                    for (Int32 position = 0; position < outputBytes; ++position)
                    {
                        for (Int32 value = 0; value < 256; ++value)
                        {
                            Int32 count = byteCounts[(position * 256) + value];

                            writer.WriteLine(String.Join(",",
                                Utilities.FormatCsvInteger(position),
                                Utilities.FormatCsvInteger(value),
                                Utilities.FormatCsvInteger(count),
                                Utilities.FormatCsvDouble((Double)count / samples),
                                Utilities.FormatCsvDouble(expectedCount),
                                Utilities.FormatCsvDouble((count - expectedCount) / deviation)
                            ));
                        }
                    }
                }

                filePath = Path.Combine(directory, "distribution-positions.csv");

                using (StreamWriter writer = new(filePath, false, encoding))
                {
                    writer.WriteLine("OutputByte,Entropy,ChiSquare");

                    for (Int32 position = 0; position < outputBytes; ++position)
                    {
                        writer.WriteLine(String.Join(",",
                            Utilities.FormatCsvInteger(position),
                            Utilities.FormatCsvDouble(byteEntropies[position]),
                            Utilities.FormatCsvDouble(byteChiSquare[position])
                        ));
                    }
                }

                filePath = Path.Combine(directory, "distribution-ed.svg");
                PlotEntropy(filePath, samples, outputBytes, byteEntropies, meanEntropy, minimumEntropy);

                filePath = Path.Combine(directory, "distribution-obb.svg");
                PlotObb(filePath, samples, outputBits, bitOnes, meanAbsoluteBias, maximumAbsoluteBias);

                filePath = Path.Combine(directory, "distribution-obf.svg");
                PlotObf(filePath, samples, outputBytes, byteCounts);
            }

            return Utilities.WriteArtifacts(hashName, writeFunction, outputDirectory);
        }
        #endregion
    }
}