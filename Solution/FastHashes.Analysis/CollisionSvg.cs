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

        public static void WriteHistogram(String path, String corpus, Int32[] buckets, Int64 inputs)
        {
            if (buckets.Length != CELLS)
                throw new ArgumentException("A collision histogram requires 65,536 bucket loads.", nameof(buckets));

            Int32 maximum = 0;
            foreach (Int32 load in buckets)
                maximum = Math.Max(maximum, load);

            Int32 lastExact = Math.Min(20, Math.Max(4, maximum));
            Boolean hasTail = maximum > lastExact;
            Int32 groups = lastExact + 1 + (hasTail ? 1 : 0);
            Int32[] observed = new Int32[groups];
            Double[] expected = new Double[groups];

            foreach (Int32 load in buckets)
                ++observed[Math.Min(load, groups - 1)];

            Double lambda = (Double)inputs / CELLS;
            Double probability = Math.Exp(-lambda);
            Double cumulative = 0.0d;
            for (Int32 load = 0; load <= lastExact; ++load)
            {
                expected[load] = CELLS * probability;
                cumulative += probability;
                probability *= lambda / (load + 1.0d);
            }
            if (hasTail)
                expected[^1] = CELLS * Math.Max(0.0d, 1.0d - cumulative);

            Double ceiling = 1.0d;
            for (Int32 i = 0; i < groups; ++i)
                ceiling = Math.Max(ceiling, Math.Max(observed[i], expected[i]));
            ceiling *= 1.1d;

            const Int32 left = 105;
            const Int32 top = 150;
            const Int32 width = 980;
            const Int32 height = 430;
            Double step = (Double)width / groups;
            StringBuilder expectedPath = new();

            using StreamWriter writer = new(path, false, new UTF8Encoding(false));
            writer.WriteLine("<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 1200 740\" role=\"img\" aria-label=\"Collision bucket-load frequency histogram\">");
            writer.WriteLine("<rect width=\"100%\" height=\"100%\" fill=\"#ffffff\"/>");
            Text(writer, 72, 52, $"Collision analysis | {corpus} | bucket-load histogram", 25, true);
            Text(writer, 72, 84, $"{CELLS:N0} projected buckets | {inputs:N0} distinct inputs | Poisson mean = {lambda.ToString("0.###", CultureInfo.InvariantCulture)}", 15);
            Text(writer, 72, 110, "Bars: observed bucket count | Orange line: approximate Poisson expectation | Fixed input count", 14);

            for (Int32 tick = 0; tick <= 4; ++tick)
            {
                Int32 y = top + height - (tick * height / 4);
                writer.WriteLine($"<line x1=\"{left}\" y1=\"{y}\" x2=\"{left + width}\" y2=\"{y}\" stroke=\"#e5e7eb\" stroke-width=\"1\"/>");
                Text(writer, 18, y + 5, (ceiling * tick / 4.0d).ToString("N0", CultureInfo.InvariantCulture), 13);
            }

            for (Int32 load = 0; load < groups; ++load)
            {
                Int32 center = left + (Int32)Math.Round((load + 0.5d) * step);
                Int32 observedHeight = (Int32)Math.Round(observed[load] / ceiling * height);
                Int32 observedY = top + height - observedHeight;
                Int32 barWidth = Math.Max(2, (Int32)Math.Floor(step) - 8);
                writer.WriteLine($"<rect x=\"{center - barWidth / 2}\" y=\"{observedY}\" width=\"{barWidth}\" height=\"{observedHeight}\" fill=\"#508fca\"/>");

                Int32 expectedY = top + height - (Int32)Math.Round(expected[load] / ceiling * height);
                expectedPath.Append(load == 0 ? "M " : " L ").Append(center).Append(' ').Append(expectedY);
                if ((load % 2 == 0) || (load == groups - 1))
                    Text(writer, center - 7, top + height + 23, hasTail && load == groups - 1 ? $"{lastExact + 1}+" : load.ToString(CultureInfo.InvariantCulture), 12);
            }

            writer.WriteLine($"<path d=\"{expectedPath}\" fill=\"none\" stroke=\"#e28b3d\" stroke-width=\"3\" stroke-linejoin=\"round\"/>");
            writer.WriteLine($"<line x1=\"{left}\" y1=\"{top}\" x2=\"{left}\" y2=\"{top + height}\" stroke=\"#6b7280\" stroke-width=\"1.5\"/>");
            writer.WriteLine($"<line x1=\"{left}\" y1=\"{top + height}\" x2=\"{left + width}\" y2=\"{top + height}\" stroke=\"#6b7280\" stroke-width=\"1.5\"/>");
            Text(writer, 105, 625, "X: inputs per projected bucket (load)", 15);
            Text(writer, 105, 651, "Y: number of projected buckets", 15);
            Text(writer, 105, 695, hasTail ? $"The final bin groups all loads >= {lastExact + 1}. Poisson is an approximation to fixed-N occupancy." : "Poisson is an approximation to fixed-N occupancy; comparisons are descriptive.", 14);
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
