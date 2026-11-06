#region Using Directives
using System;
using System.Globalization;
using System.IO;
using System.Text;
#endregion

namespace FastHashes.Analysis
{
    internal static class IndependenceSvg
    {
        #region Setup
        private const String FONT = "Arial, Helvetica, sans-serif";
        private const Double HEATMAP_SCALE = 0.15d;
        #endregion

        #region Methods
        public static void WriteHeatmap(String path, Double[] correlations, Int32[] jointCounts, Int32[] flipCounts, Int32 inputBits, Int32 outputBits, Int32 pairCount, Int32 samples, Double meanAbsoluteCorrelation, Double maximumAbsoluteCorrelation, Int64 significant, Int64 validTests)
        {
            const Int32 plotLeft = 150;
            const Int32 plotTop = 185;
            const Int32 plotSize = 635;
            const Int32 legendLeft = 875;
            const Int32 legendTop = 275;
            const Int32 legendHeight = 350;
            const Int32 legendWidth = 26;

            using StreamWriter writer = new(path, false, new UTF8Encoding(false));
            writer.WriteLine("<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 1120 935\" role=\"img\" aria-label=\"Bit independence mean absolute phi correlation heatmap\">");
            writer.WriteLine("<rect width=\"100%\" height=\"100%\" fill=\"#ffffff\"/>");
            Text(writer, 68, 43, "Bit Independence Criterion | Pairwise output-bit correlation", 25, "#172235", true);
            Text(writer, 68, 72, "Each cell averages |phi| across input-bit perturbations; diagonal cells are excluded.", 14, "#556174");
            Text(writer, 68, 105, $"Samples per input bit: {samples:N0}   |   Input bits: {inputBits:N0}   |   Output bits: {outputBits:N0}", 14, "#303c50");
            Text(writer, 68, 130, $"Mean |phi|: {F(meanAbsoluteCorrelation)}   |   Maximum |phi|: {F(maximumAbsoluteCorrelation)}   |   Significant tests: {significant:N0} / {validTests:N0}", 14, "#303c50");
            Double cell = (Double)plotSize / outputBits;
            Int32[] pairValid = new Int32[pairCount];

            for (Int32 inputBit = 0; inputBit < inputBits; ++inputBit)
            {
                Int32 index = 0;
                for (Int32 a = 0; a < outputBits; ++a)
                {
                    for (Int32 b = a + 1; b < outputBits; ++b)
                    {
                        Int32 n11 = jointCounts[inputBit * pairCount + index];
                        Int32 n10 = flipCounts[inputBit * outputBits + a] - n11;
                        Int32 n01 = flipCounts[inputBit * outputBits + b] - n11;
                        Int32 n00 = samples - n11 - n10 - n01;
                        if ((n10 + n11) > 0 && (n00 + n01) > 0 && (n01 + n11) > 0 && (n00 + n10) > 0)
                            ++pairValid[index];
                        ++index;
                    }
                }
            }

            writer.WriteLine($"<rect x=\"{plotLeft}\" y=\"{plotTop}\" width=\"{plotSize}\" height=\"{plotSize}\" fill=\"#f2f5f9\"/>");
            Int32 pairIndex = 0;
            for (Int32 a = 0; a < outputBits; ++a)
            {
                for (Int32 b = a + 1; b < outputBits; ++b)
                {
                    String color = pairValid[pairIndex] == 0 ? "#e2e8f0" : Color(correlations[pairIndex] / pairValid[pairIndex] / HEATMAP_SCALE);
                    writer.WriteLine($"<rect x=\"{F(plotLeft + a * cell)}\" y=\"{F(plotTop + b * cell)}\" width=\"{F(cell + 0.009d)}\" height=\"{F(cell + 0.009d)}\" fill=\"{color}\"/>");
                    writer.WriteLine($"<rect x=\"{F(plotLeft + b * cell)}\" y=\"{F(plotTop + a * cell)}\" width=\"{F(cell + 0.009d)}\" height=\"{F(cell + 0.009d)}\" fill=\"{color}\"/>");
                    ++pairIndex;
                }
            }

            for (Int32 bit = 8; bit < outputBits; bit += 8)
            {
                Double position = bit * cell;
                writer.WriteLine($"<line x1=\"{F(plotLeft + position)}\" y1=\"{plotTop}\" x2=\"{F(plotLeft + position)}\" y2=\"{plotTop + plotSize}\" stroke=\"#64748b\" stroke-opacity=\"0.22\" stroke-width=\"0.8\"/>");
                writer.WriteLine($"<line x1=\"{plotLeft}\" y1=\"{F(plotTop + position)}\" x2=\"{plotLeft + plotSize}\" y2=\"{F(plotTop + position)}\" stroke=\"#64748b\" stroke-opacity=\"0.22\" stroke-width=\"0.8\"/>");
            }
            writer.WriteLine($"<rect x=\"{plotLeft}\" y=\"{plotTop}\" width=\"{plotSize}\" height=\"{plotSize}\" fill=\"none\" stroke=\"#8b99ab\" stroke-width=\"1\"/>");
            Int32 step = outputBits <= 64 ? 8 : outputBits <= 128 ? 16 : 32;
            for (Int32 bit = 0; bit < outputBits; bit += step)
            {
                Double pos = (bit + 0.5d) * cell;
                Label(writer, plotLeft + pos, plotTop + plotSize + 24, bit.ToString(CultureInfo.InvariantCulture), 12, "middle");
                Label(writer, plotLeft - 12, plotTop + pos + 4, bit.ToString(CultureInfo.InvariantCulture), 12, "end");
            }
            Label(writer, plotLeft + plotSize - cell / 2.0d, plotTop + plotSize + 24, (outputBits - 1).ToString(CultureInfo.InvariantCulture), 12, "middle");
            Label(writer, plotLeft - 12, plotTop + plotSize, (outputBits - 1).ToString(CultureInfo.InvariantCulture), 12, "end");
            Label(writer, plotLeft + plotSize / 2.0d, 877, "Output bit A (LSB-first within each byte)", 16, "middle");
            writer.WriteLine($"<text x=\"44\" y=\"{plotTop + plotSize / 2}\" transform=\"rotate(-90 44 {plotTop + plotSize / 2})\" text-anchor=\"middle\" font-family=\"{FONT}\" font-size=\"16\" fill=\"#303c50\">Output bit B</text>");
            Text(writer, legendLeft - 4, legendTop - 22, "Mean |phi|", 15, "#303c50", true);
            for (Int32 i = 0; i < 100; ++i)
            {
                Double y = legendTop + (99 - i) * (Double)legendHeight / 100.0d;
                writer.WriteLine($"<rect x=\"{legendLeft}\" y=\"{F(y)}\" width=\"{legendWidth}\" height=\"{F((Double)legendHeight / 100.0d + 0.05d)}\" fill=\"{Color(i / 99.0d)}\"/>");
            }
            writer.WriteLine($"<rect x=\"{legendLeft}\" y=\"{legendTop}\" width=\"{legendWidth}\" height=\"{legendHeight}\" fill=\"none\" stroke=\"#8b99ab\" stroke-width=\"1\"/>");
            for (Int32 i = 0; i <= 3; ++i)
            {
                Double value = HEATMAP_SCALE * (3 - i) / 3.0d;
                Label(writer, legendLeft + legendWidth + 12, legendTop + i * legendHeight / 3.0d + 5, F(value), 12, "start");
            }
            Text(writer, 850, 693, "Fixed scale: 0 to 0.15", 13, "#556174");
            Text(writer, 850, 715, "Grey: undefined phi", 13, "#556174");
            Text(writer, 850, 737, "Diagonal: excluded", 13, "#556174");
            Text(writer, 150, 916, "Descriptive heatmap only. Significance testing uses each input-bit/output-pair contingency table.", 13, "#556174");
            writer.WriteLine("</svg>");
        }

        private static String Color(Double intensity)
        {
            Double t = Math.Max(0.0d, Math.Min(1.0d, intensity));
            Int32 red = (Int32)Math.Round(255.0d + (37.0d - 255.0d) * t);
            Int32 green = (Int32)Math.Round(255.0d + (73.0d - 255.0d) * t);
            Int32 blue = (Int32)Math.Round(255.0d + (128.0d - 255.0d) * t);
            return $"#{red:X2}{green:X2}{blue:X2}";
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
