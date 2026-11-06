#region Using Directives
using System;
using System.Globalization;
using System.IO;
using System.Text;
#endregion

namespace FastHashes.Analysis
{
    internal static class AvalancheSvg
    {
        #region Setup
        private const String FONT = "Arial, Helvetica, sans-serif";
        private const Double HEATMAP_SCALE = 0.10d;
        #endregion

        #region Methods
        public static void WriteHeatmap(String path, Int32[] changed, Int32 inputBits, Int32 outputBits, Int32 samples, Double meanBias, Double maximumBias, Int64 significant)
        {
            const Int32 plotLeft = 125;
            const Int32 plotTop = 180;
            const Int32 plotWidth = 850;
            const Int32 plotHeight = 610;
            const Int32 legendLeft = 1035;
            const Int32 legendWidth = 24;
            const Int32 legendHeight = 390;

            using StreamWriter writer = new(path, false, new UTF8Encoding(false));
            Begin(writer, 1190, 900, "SAC absolute-bias heatmap");
            Text(writer, 66, 42, "Strict Avalanche Criterion | Absolute bit-flip bias", 26, "#172235", true);
            Text(writer, 66, 70, "Each cell represents |P(output bit changes) - 0.5| for one flipped input bit.", 14, "#556174");
            Text(writer, 66, 102, $"Samples per input bit: {samples:N0}   |   Input bits: {inputBits:N0}   |   Output bits: {outputBits:N0}", 14, "#303c50");
            Text(writer, 66, 125, $"Mean bias: {F(meanBias * 100.0d)}%   |   Maximum bias: {F(maximumBias * 100.0d)}%   |   Significant cells: {significant:N0}", 14, "#303c50");
            writer.WriteLine($"<rect x=\"{plotLeft}\" y=\"{plotTop}\" width=\"{plotWidth}\" height=\"{plotHeight}\" fill=\"#fff\"/>");

            Double cellWidth = (Double)plotWidth / outputBits;
            Double cellHeight = (Double)plotHeight / inputBits;

            for (Int32 inputBit = 0; inputBit < inputBits; ++inputBit)
            {
                for (Int32 outputBit = 0; outputBit < outputBits; ++outputBit)
                {
                    Double probability = (Double)changed[inputBit * outputBits + outputBit] / samples;
                    Double intensity = Math.Min(1.0d, Math.Abs(probability - 0.5d) / HEATMAP_SCALE);
                    writer.WriteLine($"<rect x=\"{F(plotLeft + outputBit * cellWidth)}\" y=\"{F(plotTop + inputBit * cellHeight)}\" width=\"{F(cellWidth + 0.008d)}\" height=\"{F(cellHeight + 0.008d)}\" fill=\"{HeatColor(intensity)}\"/>");
                }
            }

            // Light byte guides aid structure detection without obscuring individual cells.
            for (Int32 bit = 8; bit < outputBits; bit += 8)
            {
                Double x = plotLeft + bit * cellWidth;
                writer.WriteLine($"<line x1=\"{F(x)}\" y1=\"{plotTop}\" x2=\"{F(x)}\" y2=\"{plotTop + plotHeight}\" stroke=\"#64748b\" stroke-opacity=\"0.25\" stroke-width=\"0.8\"/>");
            }

            for (Int32 bit = 64; bit < inputBits; bit += 64)
            {
                Double y = plotTop + bit * cellHeight;
                writer.WriteLine($"<line x1=\"{plotLeft}\" y1=\"{F(y)}\" x2=\"{plotLeft + plotWidth}\" y2=\"{F(y)}\" stroke=\"#64748b\" stroke-opacity=\"0.22\" stroke-width=\"0.8\"/>");
            }

            Border(writer, plotLeft, plotTop, plotWidth, plotHeight);
            Int32 xStep = outputBits <= 64 ? 8 : outputBits <= 128 ? 16 : 32;
            for (Int32 bit = 0; bit < outputBits; bit += xStep)
            {
                Double x = plotLeft + (bit + 0.5d) * cellWidth;
                Label(writer, x, plotTop + plotHeight + 23, bit.ToString(CultureInfo.InvariantCulture), 12, "middle");
            }
            Label(writer, plotLeft + plotWidth - cellWidth / 2.0d, plotTop + plotHeight + 23, (outputBits - 1).ToString(CultureInfo.InvariantCulture), 12, "middle");

            Int32 yStep = inputBits <= 64 ? 8 : inputBits <= 256 ? 32 : 64;
            for (Int32 bit = 0; bit < inputBits; bit += yStep)
            {
                Double y = plotTop + (bit + 0.5d) * cellHeight + 4;
                Label(writer, plotLeft - 12, y, bit.ToString(CultureInfo.InvariantCulture), 12, "end");
            }
            Label(writer, plotLeft - 12, plotTop + plotHeight, (inputBits - 1).ToString(CultureInfo.InvariantCulture), 12, "end");

            Label(writer, plotLeft + plotWidth / 2, 860, "Output bit index (LSB-first within each byte)", 16, "middle");
            writer.WriteLine($"<text x=\"32\" y=\"{plotTop + plotHeight / 2}\" transform=\"rotate(-90 32 {plotTop + plotHeight / 2})\" text-anchor=\"middle\" font-family=\"{FONT}\" font-size=\"16\" fill=\"#303c50\">Input bit index</text>");

            Text(writer, legendLeft - 5, plotTop + 8, "Absolute bias", 14, "#303c50", true);
            for (Int32 i = 0; i < 100; ++i)
            {
                Double y = plotTop + 30 + (99 - i) * (Double)legendHeight / 100.0d;
                writer.WriteLine($"<rect x=\"{legendLeft}\" y=\"{F(y)}\" width=\"{legendWidth}\" height=\"{F((Double)legendHeight / 100.0d + 0.04d)}\" fill=\"{HeatColor(i / 99.0d)}\"/>");
            }
            Border(writer, legendLeft, plotTop + 30, legendWidth, legendHeight);
            for (Int32 i = 0; i <= 5; ++i)
            {
                Double y = plotTop + 30 + legendHeight - (Double)i / 5 * legendHeight;
                Label(writer, legendLeft + 34, y + 4, F(100.0d * HEATMAP_SCALE * i / 5) + "%", 12, "start");
            }
            Text(writer, legendLeft - 5, plotTop + 30 + legendHeight + 31, "Values above 10%", 12, "#64748b");
            Text(writer, legendLeft - 5, plotTop + 30 + legendHeight + 49, "are saturated.", 12, "#64748b");
            End(writer);
        }

        public static void WriteHistogram(String path, Int64[] histogram, Int64 trials, Int32 outputBits)
        {
            const Int32 plotLeft = 120;
            const Int32 plotTop = 178;
            const Int32 plotWidth = 980;
            const Int32 plotHeight = 465;

            Double mean = 0.0d;
            Double secondMoment = 0.0d;
            Int64 maximumCount = 0;
            for (Int32 i = 0; i < histogram.Length; ++i)
            {
                mean += (Double)i * histogram[i];
                secondMoment += (Double)i * i * histogram[i];
                maximumCount = Math.Max(maximumCount, histogram[i]);
            }
            mean /= trials;
            Double standardDeviation = Math.Sqrt(Math.Max(0.0d, secondMoment / trials - mean * mean));

            Double[] expected = new Double[outputBits + 1];
            Double[] logFactorials = new Double[outputBits + 1];
            for (Int32 i = 2; i <= outputBits; ++i)
                logFactorials[i] = logFactorials[i - 1] + Math.Log(i);
            Double expectedMaximum = 0.0d;
            for (Int32 i = 0; i <= outputBits; ++i)
            {
                expected[i] = trials * Math.Exp(logFactorials[outputBits] - logFactorials[i] - logFactorials[outputBits - i] - outputBits * Math.Log(2.0d));
                expectedMaximum = Math.Max(expectedMaximum, expected[i]);
            }
            Double ceiling = Math.Max(maximumCount, expectedMaximum) * 1.12d;

            using StreamWriter writer = new(path, false, new UTF8Encoding(false));
            Begin(writer, 1200, 770, "Output Hamming-distance distribution");
            Text(writer, 66, 43, "Output Hamming distance | Single-bit input mutations", 26, "#172235", true);
            Text(writer, 66, 70, "Observed distribution compared with the ideal Binomial(output bits, 0.5) model.", 14, "#556174");
            Text(writer, 66, 104, $"Trials: {trials:N0}    |    Observed mean: {F(mean)}    |    Ideal mean: {F(outputBits / 2.0d)}", 14, "#303c50");
            Text(writer, 66, 127, $"Observed SD: {F(standardDeviation)}    |    Ideal SD: {F(Math.Sqrt(outputBits * 0.25d))}    |    Peak count: {maximumCount:N0}", 14, "#303c50");

            for (Int32 tick = 0; tick <= 5; ++tick)
            {
                Double y = plotTop + plotHeight - tick * (Double)plotHeight / 5;
                writer.WriteLine($"<line x1=\"{plotLeft}\" y1=\"{F(y)}\" x2=\"{plotLeft + plotWidth}\" y2=\"{F(y)}\" stroke=\"#e3e9f1\" stroke-width=\"1\"/>");
                Label(writer, plotLeft - 15, y + 5, Math.Round(ceiling * tick / 5).ToString("N0", CultureInfo.InvariantCulture), 12, "end");
            }

            Double barWidth = (Double)plotWidth / histogram.Length;
            for (Int32 i = 0; i < histogram.Length; ++i)
            {
                Double height = histogram[i] / ceiling * plotHeight;
                writer.WriteLine($"<rect x=\"{F(plotLeft + i * barWidth + 0.5d)}\" y=\"{F(plotTop + plotHeight - height)}\" width=\"{F(Math.Max(0.5d, barWidth - 1.0d))}\" height=\"{F(height)}\" fill=\"#4683ad\"/>");
            }

            StringBuilder expectedPath = new();
            for (Int32 i = 0; i <= outputBits; ++i)
            {
                Double x = plotLeft + (i + 0.5d) * barWidth;
                Double y = plotTop + plotHeight - expected[i] / ceiling * plotHeight;
                expectedPath.Append(i == 0 ? "M " : " L ").Append(F(x)).Append(' ').Append(F(y));
            }
            writer.WriteLine($"<path d=\"{expectedPath}\" stroke=\"#ed8a37\" stroke-width=\"2.8\" fill=\"none\" stroke-linejoin=\"round\"/>");

            Double referenceX = plotLeft + (outputBits / 2.0d + 0.5d) * barWidth;
            writer.WriteLine($"<line x1=\"{F(referenceX)}\" y1=\"{plotTop}\" x2=\"{F(referenceX)}\" y2=\"{plotTop + plotHeight}\" stroke=\"#b43f53\" stroke-width=\"2\" stroke-dasharray=\"7 6\"/>");
            Border(writer, plotLeft, plotTop, plotWidth, plotHeight);

            Int32 step = outputBits <= 64 ? 8 : outputBits <= 128 ? 16 : 32;
            for (Int32 i = 0; i <= outputBits; i += step)
                Label(writer, plotLeft + (i + 0.5d) * barWidth, plotTop + plotHeight + 25, i.ToString(CultureInfo.InvariantCulture), 12, "middle");
            if (outputBits % step != 0)
                Label(writer, plotLeft + (outputBits + 0.5d) * barWidth, plotTop + plotHeight + 25, outputBits.ToString(CultureInfo.InvariantCulture), 12, "middle");

            Label(writer, plotLeft + plotWidth / 2, 710, "Changed output bits", 16, "middle");
            writer.WriteLine($"<text x=\"28\" y=\"{plotTop + plotHeight / 2}\" transform=\"rotate(-90 28 {plotTop + plotHeight / 2})\" text-anchor=\"middle\" font-family=\"{FONT}\" font-size=\"16\" fill=\"#303c50\">Number of trials</text>");
            writer.WriteLine("<rect x=\"265\" y=\"739\" width=\"17\" height=\"12\" fill=\"#4683ad\"/>");
            Text(writer, 291, 750, "Observed", 13, "#303c50");
            writer.WriteLine("<line x1=\"421\" y1=\"745\" x2=\"451\" y2=\"745\" stroke=\"#ed8a37\" stroke-width=\"3\"/>");
            Text(writer, 460, 750, "Ideal binomial", 13, "#303c50");
            writer.WriteLine("<line x1=\"637\" y1=\"745\" x2=\"667\" y2=\"745\" stroke=\"#b43f53\" stroke-width=\"2\" stroke-dasharray=\"7 5\"/>");
            Text(writer, 676, 750, "Ideal mean", 13, "#303c50");
            End(writer);
        }

        private static String HeatColor(Double intensity)
        {
            Double t = Math.Max(0.0d, Math.Min(1.0d, intensity));
            Int32 red = (Int32)Math.Round(255.0d + (153.0d - 255.0d) * t);
            Int32 green = (Int32)Math.Round(255.0d + (39.0d - 255.0d) * t);
            Int32 blue = (Int32)Math.Round(255.0d + (57.0d - 255.0d) * t);
            return $"#{red:X2}{green:X2}{blue:X2}";
        }

        private static void Begin(StreamWriter writer, Int32 width, Int32 height, String description)
        {
            writer.WriteLine($"<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 {width} {height}\" role=\"img\" aria-label=\"{description}\">");
            writer.WriteLine("<rect width=\"100%\" height=\"100%\" fill=\"#ffffff\"/>");
        }

        private static void End(StreamWriter writer)
        {
            writer.WriteLine("</svg>");
        }

        private static void Border(StreamWriter writer, Double x, Double y, Double width, Double height)
        {
            writer.WriteLine($"<rect x=\"{F(x)}\" y=\"{F(y)}\" width=\"{F(width)}\" height=\"{F(height)}\" fill=\"none\" stroke=\"#8b99ab\" stroke-width=\"1\"/>");
        }

        private static void Text(StreamWriter writer, Double x, Double y, String value, Int32 size, String color, Boolean bold = false)
        {
            writer.WriteLine($"<text x=\"{F(x)}\" y=\"{F(y)}\" font-family=\"{FONT}\" font-size=\"{size}\" fill=\"{color}\"{(bold ? " font-weight=\"bold\"" : "")}>{value}</text>");
        }

        private static void Label(StreamWriter writer, Double x, Double y, String value, Int32 size, String anchor)
        {
            writer.WriteLine($"<text x=\"{F(x)}\" y=\"{F(y)}\" text-anchor=\"{anchor}\" font-family=\"{FONT}\" font-size=\"{size}\" fill=\"#556174\">{value}</text>");
        }

        private static String F(Double value)
        {
            return value.ToString("0.######", CultureInfo.InvariantCulture);
        }
        #endregion
    }
}
