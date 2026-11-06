#region Using Directives
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
#endregion

namespace FastHashes.Analysis
{
    internal static class StructuralSvg
    {
        #region Methods
        public static void WriteWindowProfile(String path, String hashName, Int32 hashBits, Int32 keyLength, IReadOnlyList<WindowObservation> observations)
        {
            if ((observations == null) || (observations.Count == 0))
                return;

            Int32 width = 1200;
            Int32 height = 760;
            Int32 plotLeft = 96;
            Int32 plotTop = 170;
            Int32 plotWidth = 940;
            Int32 plotHeight = 470;
            Int32 plotRight = plotLeft + plotWidth;
            Int32 plotBottom = plotTop + plotHeight;

            String[] seriesOrder = ["0001", "0100", "8001", "FFFF"];
            Dictionary<String, String> colors = new()
            {
                ["0001"] = "#1f77b4",
                ["0100"] = "#d62728",
                ["8001"] = "#2ca02c",
                ["FFFF"] = "#9467bd"
            };

            Int32 maxOffset = observations.Max(x => x.Offset);
            Double yReference = hashBits / 2.0d;

            StringBuilder svg = new();
            svg.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
            svg.AppendLine($"<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"{width}\" height=\"{height}\" viewBox=\"0 0 {width} {height}\" role=\"img\" aria-labelledby=\"title desc\">");
            svg.AppendLine("  <title id=\"title\">Structural window-position sensitivity profile</title>");
            svg.AppendLine($"  <desc id=\"desc\">Four line series show the output Hamming distance for 16-bit windows moved across a {keyLength}-byte key when hashing with {Escape(hashName)}. The dashed line marks the ideal marginal mean of {F(yReference)} changed bits for a {hashBits}-bit hash.</desc>");
            svg.AppendLine("  <rect x=\"0\" y=\"0\" width=\"100%\" height=\"100%\" fill=\"white\" />");

            svg.AppendLine("  <text x=\"60\" y=\"52\" font-family=\"Segoe UI, Arial, sans-serif\" font-size=\"28\" font-weight=\"700\" fill=\"#111827\">Structural Window Sensitivity</text>");
            svg.AppendLine($"  <text x=\"60\" y=\"82\" font-family=\"Segoe UI, Arial, sans-serif\" font-size=\"15\" fill=\"#374151\">Hash: {Escape(hashName)} • Analysis: structural (AN-005) • Family: window • 16-bit window moved across a {keyLength}-byte key</text>");
            svg.AppendLine($"  <text x=\"60\" y=\"106\" font-family=\"Segoe UI, Arial, sans-serif\" font-size=\"13\" fill=\"#4b5563\">Y-axis: changed output bits (0–{hashBits}) • X-axis: window byte offset • Dashed line: ideal marginal mean = {F(yReference)}</text>");

            // Summary box.
            svg.AppendLine("  <rect x=\"60\" y=\"118\" width=\"470\" height=\"36\" rx=\"6\" fill=\"#f9fafb\" stroke=\"#d1d5db\" />");
            svg.AppendLine($"  <text x=\"76\" y=\"141\" font-family=\"Segoe UI, Arial, sans-serif\" font-size=\"13\" fill=\"#111827\">Offsets: 0–{maxOffset} • Observations: {observations.Count} • Mean(all): {F(observations.Average(x => x.HammingDistance))} • Min: {observations.Min(x => x.HammingDistance)} • Max: {observations.Max(x => x.HammingDistance)}</text>");

            // Plot frame.
            svg.AppendLine($"  <rect x=\"{plotLeft}\" y=\"{plotTop}\" width=\"{plotWidth}\" height=\"{plotHeight}\" fill=\"#ffffff\" stroke=\"#9ca3af\" />");

            // Gridlines and y-axis labels.
            for (Int32 tick = 0; tick <= hashBits; tick += 8)
            {
                Int32 y = Y(plotTop, plotHeight, hashBits, tick);
                String stroke = (tick == (Int32)Math.Round(yReference, MidpointRounding.AwayFromZero)) ? "#cbd5e1" : "#e5e7eb";
                svg.AppendLine($"  <line x1=\"{plotLeft}\" y1=\"{y}\" x2=\"{plotRight}\" y2=\"{y}\" stroke=\"{stroke}\" stroke-width=\"1\" />");
                svg.AppendLine($"  <text x=\"{plotLeft - 12}\" y=\"{y + 5}\" text-anchor=\"end\" font-family=\"Segoe UI, Arial, sans-serif\" font-size=\"12\" fill=\"#374151\">{tick}</text>");
            }

            // X-axis guides and labels.
            for (Int32 tick = 0; tick <= maxOffset; tick += 8)
            {
                Int32 x = X(plotLeft, plotWidth, maxOffset, tick);
                svg.AppendLine($"  <line x1=\"{x}\" y1=\"{plotTop}\" x2=\"{x}\" y2=\"{plotBottom}\" stroke=\"#f3f4f6\" stroke-width=\"1\" />");
                svg.AppendLine($"  <text x=\"{x}\" y=\"{plotBottom + 24}\" text-anchor=\"middle\" font-family=\"Segoe UI, Arial, sans-serif\" font-size=\"12\" fill=\"#374151\">{tick}</text>");
            }

            // Reference line.
            Int32 yRef = Y(plotTop, plotHeight, hashBits, yReference);
            svg.AppendLine($"  <line x1=\"{plotLeft}\" y1=\"{yRef}\" x2=\"{plotRight}\" y2=\"{yRef}\" stroke=\"#dc2626\" stroke-width=\"2\" stroke-dasharray=\"8 6\" />");
            svg.AppendLine($"  <text x=\"{plotRight - 4}\" y=\"{yRef - 8}\" text-anchor=\"end\" font-family=\"Segoe UI, Arial, sans-serif\" font-size=\"12\" fill=\"#b91c1c\">Ideal mean: {F(yReference)}</text>");

            // Series.
            foreach (String series in seriesOrder)
            {
                List<WindowObservation> data = observations.Where(x => x.Value == series).OrderBy(x => x.Offset).ToList();
                if (data.Count == 0)
                    continue;

                String color = colors[series];
                StringBuilder points = new();
                foreach (WindowObservation point in data)
                {
                    Int32 x = X(plotLeft, plotWidth, maxOffset, point.Offset);
                    Int32 y = Y(plotTop, plotHeight, hashBits, point.HammingDistance);
                    if (points.Length > 0)
                        points.Append(' ');
                    points.Append(x.ToString(CultureInfo.InvariantCulture)).Append(',').Append(y.ToString(CultureInfo.InvariantCulture));
                }
                svg.AppendLine($"  <polyline fill=\"none\" stroke=\"{color}\" stroke-width=\"2.5\" points=\"{points}\" />");
                foreach (WindowObservation point in data)
                {
                    Int32 x = X(plotLeft, plotWidth, maxOffset, point.Offset);
                    Int32 y = Y(plotTop, plotHeight, hashBits, point.HammingDistance);
                    svg.AppendLine($"  <circle cx=\"{x}\" cy=\"{y}\" r=\"3.2\" fill=\"{color}\" stroke=\"white\" stroke-width=\"1\" />");
                }
            }

            // Axis labels.
            svg.AppendLine($"  <text x=\"{plotLeft + (plotWidth / 2)}\" y=\"{plotBottom + 56}\" text-anchor=\"middle\" font-family=\"Segoe UI, Arial, sans-serif\" font-size=\"14\" font-weight=\"600\" fill=\"#111827\">Window byte offset</text>");
            svg.AppendLine($"  <text x=\"28\" y=\"{plotTop + (plotHeight / 2)}\" text-anchor=\"middle\" transform=\"rotate(-90 28 {plotTop + (plotHeight / 2)})\" font-family=\"Segoe UI, Arial, sans-serif\" font-size=\"14\" font-weight=\"600\" fill=\"#111827\">Changed output bits</text>");

            // Legend box.
            Int32 legendX = 760;
            Int32 legendY = 118;
            svg.AppendLine($"  <rect x=\"{legendX}\" y=\"{legendY}\" width=\"276\" height=\"118\" rx=\"8\" fill=\"#f9fafb\" stroke=\"#d1d5db\" />");
            svg.AppendLine($"  <text x=\"{legendX + 14}\" y=\"{legendY + 22}\" font-family=\"Segoe UI, Arial, sans-serif\" font-size=\"14\" font-weight=\"700\" fill=\"#111827\">Window values</text>");
            Int32 legendRow = 0;
            foreach (String series in seriesOrder)
            {
                List<WindowObservation> data = observations.Where(x => x.Value == series).OrderBy(x => x.Offset).ToList();
                if (data.Count == 0)
                    continue;
                Int32 y = legendY + 44 + (legendRow * 18);
                String color = colors[series];
                Double mean = data.Average(x => x.HammingDistance);
                Int32 min = data.Min(x => x.HammingDistance);
                Int32 max = data.Max(x => x.HammingDistance);
                svg.AppendLine($"  <line x1=\"{legendX + 14}\" y1=\"{y - 4}\" x2=\"{legendX + 36}\" y2=\"{y - 4}\" stroke=\"{color}\" stroke-width=\"3\" />");
                svg.AppendLine($"  <circle cx=\"{legendX + 25}\" cy=\"{y - 4}\" r=\"3\" fill=\"{color}\" stroke=\"white\" stroke-width=\"1\" />");
                svg.AppendLine($"  <text x=\"{legendX + 44}\" y=\"{y}\" font-family=\"Segoe UI, Arial, sans-serif\" font-size=\"12\" fill=\"#111827\">0x{series}: mean {F(mean)}, range {min}–{max}</text>");
                ++legendRow;
            }

            svg.AppendLine("</svg>");
            File.WriteAllText(path, svg.ToString(), new UTF8Encoding(false));
        }

        private static Int32 X(Int32 left, Int32 width, Int32 maxOffset, Int32 offset)
        {
            if (maxOffset <= 0)
                return left;
            Double x = left + (width * offset / (Double)maxOffset);
            return (Int32)Math.Round(x, MidpointRounding.AwayFromZero);
        }

        private static Int32 Y(Int32 top, Int32 height, Int32 hashBits, Double value)
        {
            Double clamped = Math.Min(hashBits, Math.Max(0.0d, value));
            Double y = top + height - (height * clamped / hashBits);
            return (Int32)Math.Round(y, MidpointRounding.AwayFromZero);
        }

        private static String Escape(String value)
        {
            return value.Replace("&", "&amp;", StringComparison.Ordinal)
                        .Replace("<", "&lt;", StringComparison.Ordinal)
                        .Replace(">", "&gt;", StringComparison.Ordinal)
                        .Replace("\"", "&quot;", StringComparison.Ordinal)
                        .Replace("'", "&apos;", StringComparison.Ordinal);
        }

        private static String F(Double value)
        {
            return value.ToString("0.###", CultureInfo.InvariantCulture);
        }
        #endregion

        internal sealed record WindowObservation(Int32 Offset, String Value, Int32 HammingDistance);
    }
}
