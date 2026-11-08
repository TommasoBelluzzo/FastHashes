#region Using Directives
using System;
#endregion

namespace FastHashes.Analysis
{
    internal static class IndependenceSvg
    {
        #region Setup
        private const Double HEATMAP_SCALE = 0.15d;
        private const String UNDEFINED_COLOR = "#edf2f7";
        private const String DIAGONAL_COLOR = "#cbd5e1";
        #endregion

        #region Methods
        public static void WriteHeatmap(String path, Double[] correlations, Int32[] validPairs, Int32 outputBits, Int32 samples,
            Int32 inputBits, Double meanAbsoluteCorrelation, Double maximumAbsoluteCorrelation, Int64 significant, Int64 validTests)
        {
            using SvgPlot plot = new(path, 1200, 900, "Bit independence mean absolute phi correlation heatmap");

            plot.PlotArea(125.0d, 180.0d, 610.0d, 610.0d);
            plot.DataRange(0.0d, outputBits, 0.0d, outputBits);

            Int32 step = outputBits <= 64 ? 8 : outputBits <= 128 ? 16 : 32;
            plot.ConfigureIndexedXAxis("Output Bit A (LSB-first)", outputBits, step);
            plot.ConfigureIndexedYAxis("Output Bit B (LSB-first)", outputBits, step);
            plot.ConfigureRightColorbar("Mean |phi|", 0.0d, HEATMAP_SCALE, "#ffffff", plot.ObservedColor,
                intervals: 3, saturated: true, format: "0.##");

            plot.Title("BIT INDEPENDENCE CRITERION");
            plot.Subtitle("Mean absolute phi across input-bit perturbations; gray denotes undefined pairs and the excluded diagonal.");
            String mean = Double.IsNaN(meanAbsoluteCorrelation) ? "N/A" : meanAbsoluteCorrelation.ToString("0.######", System.Globalization.CultureInfo.InvariantCulture);
            String maximum = Double.IsNaN(meanAbsoluteCorrelation) ? "N/A" : maximumAbsoluteCorrelation.ToString("0.######", System.Globalization.CultureInfo.InvariantCulture);

            plot.Metadata(
                $"Samples: {samples:N0} | Input Bits: {inputBits:N0} | Output Bits: {outputBits:N0}",
                $"Mean |phi|: {mean} | Maximum |phi|: {maximum} | Significant Tests: {significant:N0} / {validTests:N0}"
            );

            plot.PrepareLayout();
            plot.BeginPlot();
            plot.Rectangle(plot.PlotLeft, plot.PlotTop, plot.PlotWidth, plot.PlotHeight, UNDEFINED_COLOR);

            Int32 pairIndex = 0;

            for (Int32 a = 0; a < outputBits; ++a)
            {
                for (Int32 b = a + 1; b < outputBits; ++b)
                {
                    if (validPairs[pairIndex] > 0)
                    {
                        String color = plot.Color(correlations[pairIndex], 0.0d, HEATMAP_SCALE, "#ffffff", plot.ObservedColor);
                        plot.HeatmapCell(a, b, outputBits, outputBits, color);
                        plot.HeatmapCell(b, a, outputBits, outputBits, color);
                    }

                    ++pairIndex;
                }
            }

            for (Int32 bit = 0; bit < outputBits; ++bit)
                plot.HeatmapCell(bit, bit, outputBits, outputBits, DIAGONAL_COLOR, 0.0d);

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
            plot.DrawRightColorbar();
        }
        #endregion
    }
}