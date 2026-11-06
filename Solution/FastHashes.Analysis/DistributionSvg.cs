#region Using Directives
using System;
using System.Globalization;
using System.IO;
using System.Text;
#endregion

namespace FastHashes.Analysis
{
    /// <summary>Dependency-free SVG visualizations of output distribution diagnostics.</summary>
    internal static class DistributionSvg
    {
        #region Methods
        public static void WriteBitBalance(String path, Int32[] ones, Int32 samples)
        {
            const Int32 left = 100;
            const Int32 top = 170;
            const Int32 width = 920;
            const Int32 height = 340;
            Int32 bits = ones.Length;
            Double maximumBias = 0.0d;
            Double meanBias = 0.0d;
            foreach (Int32 count in ones)
            {
                Double bias = Math.Abs((Double)count / samples - 0.5d);
                maximumBias = Math.Max(maximumBias, bias);
                meanBias += bias;
            }
            meanBias /= bits;

            using StreamWriter writer = Writer(path);
            Begin(writer, 1130, 625, "Output-bit balance profile");
            Text(writer, 65, 47, 24, "Output Distribution | Output-bit balance", true);
            Text(writer, 65, 75, 14, "Each point shows the observed fraction of one-bits across independent input samples.");
            Text(writer, 65, 104, 13, $"Samples: {samples:N0} | Output bits: {bits} | Mean absolute bias: {F(meanBias)} | Maximum absolute bias: {F(maximumBias)}");
            Text(writer, 65, 128, 12, "Reference line: ideal 50% | Fixed y-axis: 0%–100% for comparison across hashes");

            for (Int32 tick = 0; tick <= 4; ++tick)
            {
                Double fraction = tick / 4.0d;
                Double y = top + height * (1.0d - fraction);
                writer.WriteLine($"<line x1=\"{left}\" x2=\"{left + width}\" y1=\"{F(y)}\" y2=\"{F(y)}\" stroke=\"#e5eaf1\"/>");
                Text(writer, left - 12, y + 5, 12, $"{tick * 25}%", false, "end");
            }
            Double referenceY = top + height * 0.5d;
            writer.WriteLine($"<line x1=\"{left}\" x2=\"{left + width}\" y1=\"{F(referenceY)}\" y2=\"{F(referenceY)}\" stroke=\"#cc3d36\" stroke-width=\"2\" stroke-dasharray=\"8 5\"/>");
            Double barWidth = (Double)width / bits;
            for (Int32 bit = 0; bit < bits; ++bit)
            {
                Double probability = (Double)ones[bit] / samples;
                Double center = left + (bit + 0.5d) * barWidth;
                Double y = top + height * (1.0d - probability);
                Double stroke = Math.Max(1.2d, Math.Min(8.0d, barWidth * 0.8d));
                writer.WriteLine($"<line x1=\"{F(center)}\" y1=\"{F(referenceY)}\" x2=\"{F(center)}\" y2=\"{F(y)}\" stroke=\"#2e77ae\" stroke-width=\"{F(stroke)}\"/>");
            }
            Axes(writer, left, top, width, height);
            for (Int32 bit = 0; bit < bits; bit += Math.Max(1, (bits + 15) / 16))
                Text(writer, left + (bit + 0.5d) * barWidth, top + height + 23, 11, bit.ToString(CultureInfo.InvariantCulture), false, "middle");
            Text(writer, left + width / 2, 574, 15, "Output bit index (LSB-first within each byte)", false, "middle");
            writer.WriteLine($"<text x=\"28\" y=\"{top + height / 2}\" transform=\"rotate(-90 28 {top + height / 2})\" text-anchor=\"middle\" font-family=\"Segoe UI,Arial,sans-serif\" font-size=\"14\" fill=\"#243b56\">One-bit frequency</text>");
            End(writer);
        }

        public static void WriteByteHeatmap(String path, Int32[] counts, Int32 samples, Int32 outputBytes)
        {
            const Int32 left = 118;
            const Int32 top = 175;
            const Int32 plotWidth = 896;
            Int32 plotHeight = Math.Max(170, outputBytes * 24);
            Int32 totalHeight = top + plotHeight + 125;
            Double expected = samples / 256.0d;
            Double sd = Math.Sqrt(samples * (1.0d / 256.0d) * (255.0d / 256.0d));
            Double cellWidth = (Double)plotWidth / 256;
            Double cellHeight = (Double)plotHeight / outputBytes;

            using StreamWriter writer = Writer(path);
            Begin(writer, 1200, totalHeight, "Output-byte frequency residual heatmap");
            Text(writer, 65, 47, 24, "Output Distribution | Byte-frequency heatmap", true);
            Text(writer, 65, 75, 14, "Standardized deviations from uniform byte frequencies, by digest-byte position.");
            Text(writer, 65, 104, 13, $"Samples per position: {samples:N0} | Output bytes: {outputBytes} | Expected occurrences per value: {F(expected)}");
            Text(writer, 65, 128, 12, "Fixed residual scale -4 to +4; blue = fewer observations, white = expected, red = more observations.");

            writer.WriteLine($"<g transform=\"translate({left},{top})\">");
            for (Int32 position = 0; position < outputBytes; ++position)
            {
                for (Int32 value = 0; value < 256; ++value)
                {
                    Double residual = (counts[position * 256 + value] - expected) / sd;
                    Double intensity = Math.Min(1.0d, Math.Abs(residual) / 4.0d);
                    Int32 pale = (Int32)Math.Round(255.0d - 180.0d * intensity);
                    String color = residual >= 0.0d ? $"#FF{pale:X2}{pale:X2}" : $"#{pale:X2}{pale:X2}FF";
                    writer.WriteLine($"<rect x=\"{F(value * cellWidth)}\" y=\"{F(position * cellHeight)}\" width=\"{F(cellWidth + 0.002d)}\" height=\"{F(cellHeight + 0.002d)}\" fill=\"{color}\"/>");
                }
            }
            for (Int32 value = 0; value <= 256; value += 32)
                writer.WriteLine($"<line x1=\"{F(value * cellWidth)}\" x2=\"{F(value * cellWidth)}\" y1=\"0\" y2=\"{plotHeight}\" stroke=\"#65778a\" stroke-opacity=\"0.26\"/>");
            writer.WriteLine($"<rect x=\"0\" y=\"0\" width=\"{plotWidth}\" height=\"{plotHeight}\" fill=\"none\" stroke=\"#9aa9bb\"/>");
            writer.WriteLine("</g>");

            for (Int32 value = 0; value <= 224; value += 32)
                Text(writer, left + (value + 0.5d) * cellWidth, top + plotHeight + 22, 12, value.ToString(CultureInfo.InvariantCulture), false, "middle");
            Text(writer, left + plotWidth, top + plotHeight + 22, 12, "255", false, "end");
            for (Int32 position = 0; position < outputBytes; position += Math.Max(1, outputBytes / 12))
                Text(writer, left - 12, top + (position + 0.5d) * cellHeight + 4, 12, position.ToString(CultureInfo.InvariantCulture), false, "end");
            Text(writer, left + plotWidth / 2, top + plotHeight + 54, 15, "Output byte value (0–255)", false, "middle");
            writer.WriteLine($"<text x=\"28\" y=\"{top + plotHeight / 2}\" transform=\"rotate(-90 28 {top + plotHeight / 2})\" text-anchor=\"middle\" font-family=\"Segoe UI,Arial,sans-serif\" font-size=\"14\" fill=\"#243b56\">Output byte position</text>");
            Text(writer, left, totalHeight - 23, 12, "Residuals describe marginal byte frequencies, not independence between output positions.");
            End(writer);
        }

        public static void WriteEntropy(String path, Double[] entropies, Int32 samples)
        {
            const Int32 left = 100;
            const Int32 top = 165;
            const Int32 width = 900;
            const Int32 height = 365;
            Int32 bytes = entropies.Length;
            Double minimum = 8.0d;
            Double sum = 0.0d;
            foreach (Double entropy in entropies)
            {
                minimum = Math.Min(minimum, entropy);
                sum += entropy;
            }
            Double mean = sum / bytes;

            using StreamWriter writer = Writer(path);
            Begin(writer, 1130, 660, "Empirical output-byte entropy by position");
            Text(writer, 65, 47, 24, "Output Distribution | Empirical byte entropy", true);
            Text(writer, 65, 75, 14, "Plug-in Shannon entropy computed separately for each output-byte position.");
            Text(writer, 65, 104, 13, $"Samples per position: {samples:N0} | Output bytes: {bytes} | Mean: {F(mean)} bits | Minimum: {F(minimum)} bits");
            Text(writer, 65, 128, 12, "Ideal reference: 8 bits. Finite-sample plug-in entropy estimates are downward biased.");

            for (Int32 tick = 0; tick <= 8; ++tick)
            {
                Double y = top + height * (1.0d - tick / 8.0d);
                writer.WriteLine($"<line x1=\"{left}\" x2=\"{left + width}\" y1=\"{F(y)}\" y2=\"{F(y)}\" stroke=\"#e7ebf1\"/>");
                Text(writer, left - 13, y + 5, 12, tick.ToString(CultureInfo.InvariantCulture), false, "end");
            }

            Double barWidth = (Double)width / bytes;
            for (Int32 position = 0; position < bytes; ++position)
            {
                Double heightBar = entropies[position] / 8.0d * height;
                writer.WriteLine($"<rect x=\"{F(left + position * barWidth + 1)}\" y=\"{F(top + height - heightBar)}\" width=\"{F(Math.Max(0.5d, barWidth - 2.0d))}\" height=\"{F(heightBar)}\" fill=\"#347bb5\"/>");
            }
            Axes(writer, left, top, width, height);
            for (Int32 position = 0; position < bytes; position += Math.Max(1, (bytes + 15) / 16))
                Text(writer, left + (position + 0.5d) * barWidth, top + height + 24, 12, position.ToString(CultureInfo.InvariantCulture), false, "middle");
            Text(writer, left + width / 2, 586, 15, "Output byte position", false, "middle");
            writer.WriteLine($"<text x=\"30\" y=\"{top + height / 2}\" transform=\"rotate(-90 30 {top + height / 2})\" text-anchor=\"middle\" font-family=\"Segoe UI,Arial,sans-serif\" font-size=\"14\" fill=\"#243b56\">Entropy (bits)</text>");
            Text(writer, left, 622, 12, "This is a marginal frequency diagnostic, not a cryptographic entropy or unpredictability guarantee.");
            End(writer);
        }

        private static void Begin(StreamWriter writer, Int32 width, Int32 height, String label)
        {
            writer.WriteLine($"<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"{width}\" height=\"{height}\" viewBox=\"0 0 {width} {height}\" role=\"img\" aria-label=\"{label}\">");
            writer.WriteLine($"<rect width=\"{width}\" height=\"{height}\" fill=\"#ffffff\"/>");
        }

        private static void End(StreamWriter writer)
        {
            writer.WriteLine("</svg>");
        }

        private static void Axes(StreamWriter writer, Int32 left, Int32 top, Int32 width, Int32 height)
        {
            writer.WriteLine($"<path d=\"M {left} {top} V {top + height} H {left + width}\" fill=\"none\" stroke=\"#324357\" stroke-width=\"1.5\"/>");
        }

        private static void Text(StreamWriter writer, Double x, Double y, Int32 size, String value, Boolean bold = false, String anchor = "start")
        {
            writer.WriteLine($"<text x=\"{F(x)}\" y=\"{F(y)}\" text-anchor=\"{anchor}\" font-family=\"Segoe UI,Arial,sans-serif\" font-size=\"{size}\" font-weight=\"{(bold ? "700" : "400")}\" fill=\"#23364f\">{value}</text>");
        }

        private static StreamWriter Writer(String path)
        {
            return new StreamWriter(path, false, new UTF8Encoding(false));
        }

        private static String F(Double value)
        {
            return value.ToString("0.######", CultureInfo.InvariantCulture);
        }
        #endregion
    }
}
