#region Using Directives
using System;
using System.Globalization;
using System.IO;
using System.Text;
#endregion

namespace FastHashes.Analysis
{
    /// <summary>Dependency-free 256-by-256 occupancy visualizations with linear and Hilbert layouts.</summary>
    internal static class CollisionSvg
    {
        #region Setup
        private const Int32 SIZE = 256;
        private const Int32 CELLS = SIZE * SIZE;
        #endregion

        #region Methods
        public static void WriteMap(String path, String corpus, Int32[] buckets, Boolean hilbert, Int64 inputs)
        {
            if (buckets.Length != CELLS)
                throw new ArgumentException("A collision map requires 65,536 bucket loads.", nameof(buckets));

            Int32 maximum = 0;
            Int32 occupied = 0;
            foreach (Int32 load in buckets)
            {
                maximum = Math.Max(maximum, load);
                if (load != 0)
                    ++occupied;
            }

            using StreamWriter writer = new(path, false, new UTF8Encoding(false));
            writer.WriteLine("<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 1020 1120\" role=\"img\" aria-label=\"Collision bucket occupancy map\">");
            writer.WriteLine("<rect width=\"100%\" height=\"100%\" fill=\"#ffffff\"/>");
            Text(writer, 72, 48, $"Collision analysis | {corpus} | {(hilbert ? "Hilbert" : "Linear")} map", 25, true);
            Text(writer, 72, 78, $"65,536 buckets (low 16 digest bits) | {inputs:N0} distinct keys | {occupied:N0} occupied | maximum load {maximum:N0}", 15);
            Text(writer, 72, 107, "Hue follows bucket index; white denotes empty buckets; saturation increases with occupancy.", 14);

            // A 256x256 grid with three SVG display units per logical cell.
            const Int32 left = 126;
            const Int32 top = 160;
            const Int32 scale = 3;
            writer.WriteLine($"<rect x=\"{left}\" y=\"{top}\" width=\"{SIZE * scale}\" height=\"{SIZE * scale}\" fill=\"#ffffff\"/>");

            for (Int32 bucket = 0; bucket < CELLS; ++bucket)
            {
                Int32 load = buckets[bucket];
                if (load == 0)
                    continue;

                (Int32 x, Int32 y) = hilbert ? HilbertCoordinates(bucket) : (bucket % SIZE, bucket / SIZE);
                Double hue = 360.0d * bucket / CELLS;
                Double saturation = 0.55d + 0.45d * Math.Min(1.0d, Math.Log2(load + 1.0d) / 3.0d);
                Double lightness = 0.78d - 0.35d * Math.Min(1.0d, Math.Log2(load + 1.0d) / 4.0d);
                writer.WriteLine($"<rect x=\"{left + x * scale}\" y=\"{top + y * scale}\" width=\"{scale}\" height=\"{scale}\" fill=\"{Hsl(hue, saturation, lightness)}\"/>");
            }

            writer.WriteLine($"<rect x=\"{left}\" y=\"{top}\" width=\"{SIZE * scale}\" height=\"{SIZE * scale}\" fill=\"none\" stroke=\"#738499\" stroke-width=\"1\"/>");
            Text(writer, 126, 963, "Pixel = projected hash bucket; identical bucket colors in both map layouts.", 14);
            Text(writer, 126, 991, hilbert ? "Hilbert mapping preserves proximity in bucket-index space." : "Linear mapping uses consecutive buckets left-to-right, top-to-bottom.", 14);
            Text(writer, 126, 1032, "Colors are descriptive; use collision-buckets.csv for exact loads.", 14);
            writer.WriteLine("</svg>");
        }

        // Standard Hilbert distance-to-coordinate mapping for a 256x256 square.
        internal static (Int32 X, Int32 Y) HilbertCoordinates(Int32 distance)
        {
            Int32 x = 0;
            Int32 y = 0;
            Int32 t = distance;
            for (Int32 s = 1; s < SIZE; s *= 2)
            {
                Int32 rx = 1 & (t / 2);
                Int32 ry = 1 & (t ^ rx);
                if (ry == 0)
                {
                    if (rx == 1)
                    {
                        x = s - 1 - x;
                        y = s - 1 - y;
                    }
                    (x, y) = (y, x);
                }
                x += s * rx;
                y += s * ry;
                t /= 4;
            }
            return (x, y);
        }

        private static String Hsl(Double hue, Double saturation, Double lightness)
        {
            Double chroma = (1.0d - Math.Abs(2.0d * lightness - 1.0d)) * saturation;
            Double secondary = chroma * (1.0d - Math.Abs(hue / 60.0d % 2.0d - 1.0d));
            Double red = 0.0d, green = 0.0d, blue = 0.0d;
            if (hue < 60.0d) { red = chroma; green = secondary; }
            else if (hue < 120.0d) { red = secondary; green = chroma; }
            else if (hue < 180.0d) { green = chroma; blue = secondary; }
            else if (hue < 240.0d) { green = secondary; blue = chroma; }
            else if (hue < 300.0d) { red = secondary; blue = chroma; }
            else { red = chroma; blue = secondary; }
            Double offset = lightness - chroma / 2.0d;
            Int32 r = (Int32)Math.Round(255.0d * (red + offset));
            Int32 g = (Int32)Math.Round(255.0d * (green + offset));
            Int32 b = (Int32)Math.Round(255.0d * (blue + offset));
            return $"#{r:X2}{g:X2}{b:X2}";
        }

        private static void Text(StreamWriter writer, Int32 x, Int32 y, String text, Int32 size, Boolean bold = false)
        {
            writer.WriteLine($"<text x=\"{x}\" y=\"{y}\" font-family=\"Arial, Helvetica, sans-serif\" font-size=\"{size}\" fill=\"#172235\"{(bold ? " font-weight=\"bold\"" : "")}>{text}</text>");
        }
        #endregion
    }
}
