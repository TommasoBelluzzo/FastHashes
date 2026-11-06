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
        #region Methods
        public static void WriteHeatmap(String path, Int32[] changed, Int32 inputBits, Int32 outputBits, Int32 samples)
        {
            const Int32 marginLeft = 85;
            const Int32 marginTop = 70;
            const Int32 marginRight = 35;
            const Int32 marginBottom = 75;
            const Int32 plotWidth = 900;
            const Int32 plotHeight = 700;

            using StreamWriter writer = new(path, false, new UTF8Encoding(false));
            writer.WriteLine($"<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 {marginLeft + plotWidth + marginRight} {marginTop + plotHeight + marginBottom}\" role=\"img\" aria-label=\"Avalanche SAC absolute-bias heatmap\">");
            writer.WriteLine("<rect width=\"100%\" height=\"100%\" fill=\"#ffffff\"/>");
            writer.WriteLine("<text x=\"85\" y=\"32\" font-family=\"sans-serif\" font-size=\"22\">SAC absolute bias |P(flip) - 0.5|</text>");
            writer.WriteLine("<text x=\"85\" y=\"54\" font-family=\"sans-serif\" font-size=\"13\">White = ideal (0), dark red = maximum deviation (0.5)</text>");
            writer.WriteLine($"<g transform=\"translate({marginLeft},{marginTop})\">");

            // Each cell is a direct rectangle in the viewBox, without external plotting dependencies.
            Double cellWidth = (Double)plotWidth / outputBits;
            Double cellHeight = (Double)plotHeight / inputBits;

            for (Int32 inputBit = 0; inputBit < inputBits; ++inputBit)
            {
                for (Int32 outputBit = 0; outputBit < outputBits; ++outputBit)
                {
                    Double probability = (Double)changed[inputBit * outputBits + outputBit] / samples;
                    Double deviation = Math.Abs(probability - 0.5d) * 2.0d;
                    Int32 red = 255;
                    Int32 green = (Int32)Math.Round(255.0d * (1.0d - deviation));
                    Int32 blue = green;
                    writer.WriteLine($"<rect x=\"{F(outputBit * cellWidth)}\" y=\"{F(inputBit * cellHeight)}\" width=\"{F(cellWidth + 0.001d)}\" height=\"{F(cellHeight + 0.001d)}\" fill=\"#{red:X2}{green:X2}{blue:X2}\"/>");
                }
            }

            writer.WriteLine("</g>");
            writer.WriteLine($"<text x=\"{marginLeft + plotWidth / 2}\" y=\"{marginTop + plotHeight + 43}\" text-anchor=\"middle\" font-family=\"sans-serif\" font-size=\"16\">Output bit index (LSB-first per byte)</text>");
            writer.WriteLine($"<text x=\"22\" y=\"{marginTop + plotHeight / 2}\" text-anchor=\"middle\" transform=\"rotate(-90 22 {marginTop + plotHeight / 2})\" font-family=\"sans-serif\" font-size=\"16\">Input bit index (LSB-first per byte)</text>");
            writer.WriteLine($"<text x=\"{marginLeft}\" y=\"{marginTop + plotHeight + 20}\" font-family=\"sans-serif\" font-size=\"12\">0</text>");
            writer.WriteLine($"<text x=\"{marginLeft + plotWidth}\" y=\"{marginTop + plotHeight + 20}\" text-anchor=\"end\" font-family=\"sans-serif\" font-size=\"12\">{outputBits - 1}</text>");
            writer.WriteLine($"<text x=\"{marginLeft - 9}\" y=\"{marginTop + 10}\" text-anchor=\"end\" font-family=\"sans-serif\" font-size=\"12\">0</text>");
            writer.WriteLine($"<text x=\"{marginLeft - 9}\" y=\"{marginTop + plotHeight}\" text-anchor=\"end\" font-family=\"sans-serif\" font-size=\"12\">{inputBits - 1}</text>");
            writer.WriteLine("</svg>");
        }

        public static void WriteHistogram(String path, Int64[] histogram, Int64 trials, Int32 outputBits)
        {
            const Int32 left = 80;
            const Int32 top = 65;
            const Int32 width = 900;
            const Int32 height = 500;
            Int64 maximum = 0;

            foreach (Int64 count in histogram)
                maximum = Math.Max(maximum, count);

            using StreamWriter writer = new(path, false, new UTF8Encoding(false));
            writer.WriteLine("<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 1030 650\" role=\"img\" aria-label=\"Output Hamming-distance histogram\">");
            writer.WriteLine("<rect width=\"100%\" height=\"100%\" fill=\"#ffffff\"/>");
            writer.WriteLine("<text x=\"80\" y=\"33\" font-family=\"sans-serif\" font-size=\"22\">Output Hamming distance under one-bit input flips</text>");
            writer.WriteLine("<text x=\"80\" y=\"52\" font-family=\"sans-serif\" font-size=\"13\">Observed frequency; reference line at half the output width</text>");
            writer.WriteLine($"<path d=\"M {left} {top} V {top + height} H {left + width}\" stroke=\"#222\" stroke-width=\"2\" fill=\"none\"/>");

            Double barWidth = (Double)width / histogram.Length;
            for (Int32 i = 0; i < histogram.Length; ++i)
            {
                Double barHeight = maximum == 0 ? 0.0d : (Double)histogram[i] / maximum * height;
                writer.WriteLine($"<rect x=\"{F(left + i * barWidth)}\" y=\"{F(top + height - barHeight)}\" width=\"{F(Math.Max(0.0d, barWidth - 0.5d))}\" height=\"{F(barHeight)}\" fill=\"#3275ad\"/>");
            }

            Double reference = left + (outputBits / 2.0d + 0.5d) * barWidth;
            writer.WriteLine($"<line x1=\"{F(reference)}\" y1=\"{top}\" x2=\"{F(reference)}\" y2=\"{top + height}\" stroke=\"#bd2525\" stroke-width=\"2\" stroke-dasharray=\"7,5\"/>");
            writer.WriteLine($"<text x=\"{left}\" y=\"{top + height + 20}\" font-family=\"sans-serif\" font-size=\"13\">0</text>");
            writer.WriteLine($"<text x=\"{left + width}\" y=\"{top + height + 20}\" text-anchor=\"end\" font-family=\"sans-serif\" font-size=\"13\">{outputBits}</text>");
            writer.WriteLine($"<text x=\"{left + width / 2}\" y=\"{top + height + 45}\" text-anchor=\"middle\" font-family=\"sans-serif\" font-size=\"16\">Changed output bits</text>");
            writer.WriteLine($"<text x=\"{left}\" y=\"{top - 8}\" font-family=\"sans-serif\" font-size=\"13\">Maximum bin count: {maximum}; trials: {trials}</text>");
            writer.WriteLine("</svg>");
        }

        private static String F(Double value)
        {
            return value.ToString("0.######", CultureInfo.InvariantCulture);
        }
        #endregion
    }
}
