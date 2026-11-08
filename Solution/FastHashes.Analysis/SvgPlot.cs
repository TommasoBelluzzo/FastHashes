#region Using Directives
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
#endregion

namespace FastHashes.Analysis
{
    internal sealed class SvgPlot : IDisposable
    {
        #region Setup
        private const String CLIP_ID = "fasthashes-plot-clip";
        private readonly StreamWriter writer;
        private readonly Dictionary<String, String[]> palettes = new(StringComparer.Ordinal);
        private String lastPaletteLow;
        private String lastPaletteHigh;
        private Int32 lastPaletteResolution;
        private String[] lastPalette;
        private Boolean completed;
        private Boolean prepared;
        private Boolean plotOpen;
        private Double xMinimum;
        private Double xMaximum;
        private Double yMinimum;
        private Double yMaximum;
        private Double requestedLeft;
        private Double requestedTop;
        private Double requestedWidth;
        private Double requestedHeight;
        private String[] metadataLines = Array.Empty<String>();
        private List<AxisTick> xTicks;
        private List<AxisTick> yTicks;
        private List<String> customYAxisLabels;
        private Double customYAxisSpacing = 12.0d;
        private Boolean clipDefined;
        private String xAxisTitle;
        private String yAxisTitle;
        private Boolean xGrid;
        private Boolean yGrid;
        private ColorbarSettings rightColorbar;
        private IReadOnlyList<LegendItem> bottomLegend;
        private Double bottomLegendSlotWidth = 180.0d;

        private readonly record struct AxisTick(Double Position, String Label);
        private sealed record ColorbarSettings(String Title, Double Minimum, Double Maximum, String LowColor, String HighColor,
            Int32 Intervals, Boolean Saturated, String Format, String Suffix, String MiddleColor, Double? Center);
        public readonly record struct LegendItem(String Label, String Color, Boolean IsLine, Boolean Dashed = false);

        public Int32 Width { get; }
        public Int32 Height { get; }
        public Double PlotLeft { get; private set; }
        public Double PlotTop { get; private set; }
        public Double PlotWidth { get; private set; }
        public Double PlotHeight { get; private set; }

        public String FontFamily { get; set; } = "Arial, Helvetica, sans-serif";
        public String BackgroundColor { get; set; } = "#ffffff";
        public String TitleColor { get; set; } = "#172235";
        public String TextColor { get; set; } = "#303c50";
        public String SecondaryColor { get; set; } = "#556174";
        public String MutedColor { get; set; } = "#64748b";
        public String GridColor { get; set; } = "#e3e9f1";
        public String BorderColor { get; set; } = "#8b99ab";
        public String ObservedColor { get; set; } = "#4683ad";
        public String ReferenceColor { get; set; } = "#ed8a37";
        public String HighlightColor { get; set; } = "#b43f53";
        public String SequentialColor { get; set; } = "#1F77B4";

        public Int32 TitleSize { get; set; } = 26;
        public Int32 SubtitleSize { get; set; } = 14;
        public Int32 MetadataSize { get; set; } = 14;
        public Int32 AxisTitleSize { get; set; } = 14;
        public Int32 TickSize { get; set; } = 12;
        public Int32 LegendSize { get; set; } = 13;
        public Double TitleLeft { get; set; } = 66.0d;
        public Double TitleTop { get; set; } = 43.0d;
        public Double SubtitleTop { get; set; } = 70.0d;
        public Double MetadataTop { get; set; } = 103.0d;
        public Double MetadataSpacing { get; set; } = 23.0d;
        public Double AxisLabelSpacing { get; set; } = 24.0d;
        public Double AxisTitleSpacing { get; set; } = 48.0d;
        public Double BottomLegendMargin { get; set; } = 32.0d;
        public Double RightColorbarGap { get; set; } = 60.0d;
        public Double RightColorbarWidth { get; set; } = 24.0d;
        public Double RightColorbarHeight { get; set; } = 390.0d;
        public Double MinimumHeaderGap { get; set; } = 50.0d;
        public Int32 PaletteResolution { get; set; } = 256;
        #endregion

        #region Constructors
        public SvgPlot(String path, Int32 width = 1200, Int32 height = 900, String description = null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(path, nameof(path));
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width, nameof(width));
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(height, nameof(height));

            Width = width;
            Height = height;
            
            PlotArea(125.0d, 180.0d, Math.Max(1.0d, width - 250.0d), Math.Max(1.0d, height - 290.0d));
            DataRange(0.0d, 1.0d, 0.0d, 1.0d);

            writer = new StreamWriter(path, false, new UTF8Encoding(false));
            writer.WriteLine($"<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"{width}\" height=\"{height}\" viewBox=\"0 0 {width} {height}\" role=\"img\"{(description == null ? String.Empty : $" aria-label=\"{Escape(description)}\"")}>");
            writer.WriteLine($"<rect width=\"100%\" height=\"100%\" fill=\"{BackgroundColor}\"/>");
        }
        #endregion

        #region Layout
        public void PlotArea(Double left, Double top, Double width, Double height)
        {
            RequireUnprepared();
            RequireFinite(left, nameof(left));
            RequireFinite(top, nameof(top));
            RequirePositive(width, nameof(width));
            RequirePositive(height, nameof(height));
            requestedLeft = PlotLeft = left;
            requestedTop = PlotTop = top;
            requestedWidth = PlotWidth = width;
            requestedHeight = PlotHeight = height;
        }

        public void DataRange(Double minimumX, Double maximumX, Double minimumY, Double maximumY)
        {
            RequireFinite(minimumX, nameof(minimumX));
            RequireFinite(maximumX, nameof(maximumX));
            RequireFinite(minimumY, nameof(minimumY));
            RequireFinite(maximumY, nameof(maximumY));
            if (maximumX <= minimumX || maximumY <= minimumY)
                throw new ArgumentException("Data ranges must be strictly increasing.");
            xMinimum = minimumX;
            xMaximum = maximumX;
            yMinimum = minimumY;
            yMaximum = maximumY;
        }

        public Double X(Double value) => PlotLeft + (value - xMinimum) / (xMaximum - xMinimum) * PlotWidth;
        public Double Y(Double value) => PlotTop + PlotHeight - (value - yMinimum) / (yMaximum - yMinimum) * PlotHeight;
        public Double CellX(Int32 index, Int32 count) => PlotLeft + (index + 0.5d) * PlotWidth / count;
        public Double CellY(Int32 index, Int32 count) => PlotTop + (index + 0.5d) * PlotHeight / count;

        public void ConfigureDiscreteXAxis(String title, Double minimum, Double maximum, Int32 intervals = 5, String format = "0.##", Boolean grid = true)
        {
            RequireUnprepared();
            RequireAxis(minimum, maximum, intervals);
            xAxisTitle = title;
            xGrid = grid;
            xTicks = new List<AxisTick>(intervals + 1);
            for (Int32 i = 0; i <= intervals; ++i)
            {
                Double value = minimum + (maximum - minimum) * i / intervals;
                xTicks.Add(new AxisTick(value, Format(value, format)));
            }
        }

        public void ConfigureDiscreteYAxis(String title, Double minimum, Double maximum, Int32 intervals = 5, String format = "0.##", Boolean grid = true)
        {
            RequireUnprepared();
            RequireAxis(minimum, maximum, intervals);
            yAxisTitle = title;
            yGrid = grid;
            yTicks = new List<AxisTick>(intervals + 1);
            for (Int32 i = 0; i <= intervals; ++i)
            {
                Double value = minimum + (maximum - minimum) * i / intervals;
                yTicks.Add(new AxisTick(value, Format(value, format)));
            }
        }

        public void ConfigureXTicks(String title, IReadOnlyList<(Double Value, String Label)> ticks, Boolean grid = false)
        {
            RequireUnprepared();
            if (ticks == null)
                throw new ArgumentNullException(nameof(ticks));
            xAxisTitle = title;
            xGrid = grid;
            xTicks = new List<AxisTick>(ticks.Count);
            foreach ((Double value, String label) in ticks)
            {
                RequireFinite(value, nameof(ticks));
                xTicks.Add(new AxisTick(value, label));
            }
        }

        public void ConfigureYTicks(String title, IReadOnlyList<(Double Value, String Label)> ticks, Boolean grid = false)
        {
            RequireUnprepared();
            if (ticks == null)
                throw new ArgumentNullException(nameof(ticks));
            yAxisTitle = title;
            yGrid = grid;
            yTicks = new List<AxisTick>(ticks.Count);
            foreach ((Double value, String label) in ticks)
            {
                RequireFinite(value, nameof(ticks));
                yTicks.Add(new AxisTick(value, label));
            }
        }

        public void ConfigureIndexedXAxis(String title, Int32 count, Int32 step, Boolean includeLast = true)
        {
            RequireIndexed(count, step);
            List<(Double Value, String Label)> ticks = new();
            for (Int32 i = 0; i < count; i += step)
                ticks.Add((i + 0.5d, i.ToString(CultureInfo.InvariantCulture)));
            if (includeLast && (count - 1) % step != 0)
                ticks.Add((count - 0.5d, (count - 1).ToString(CultureInfo.InvariantCulture)));
            ConfigureXTicks(title, ticks);
        }

        public void ConfigureIndexedYAxis(String title, Int32 count, Int32 step, Boolean topDown = true, Boolean includeLast = true)
        {
            RequireIndexed(count, step);
            List<(Double Value, String Label)> ticks = new();
            for (Int32 i = 0; i < count; i += step)
                ticks.Add((topDown ? count - i - 0.5d : i + 0.5d, i.ToString(CultureInfo.InvariantCulture)));
            if (includeLast && (count - 1) % step != 0)
                ticks.Add((topDown ? 0.5d : count - 0.5d, (count - 1).ToString(CultureInfo.InvariantCulture)));
            ConfigureYTicks(title, ticks);
        }

        public void ConfigureRightColorbar(String title, Double minimum, Double maximum, String lowColor, String highColor,
            Int32 intervals = 5, Boolean saturated = false, String format = "0.##", String suffix = "", String middleColor = null, Double? center = null)
        {
            RequireUnprepared();
            RequireAxis(minimum, maximum, intervals);
            if ((middleColor == null) != (center == null))
                throw new ArgumentException("Diverging colorbars require both a middle color and a center value.");
            if (center.HasValue && (center.Value <= minimum || center.Value >= maximum))
                throw new ArgumentOutOfRangeException(nameof(center));
            rightColorbar = new ColorbarSettings(title, minimum, maximum, lowColor, highColor, intervals, saturated, format, suffix, middleColor, center);
        }

        public void ConfigureBottomLegend(IReadOnlyList<LegendItem> items, Double slotWidth = 180.0d)
        {
            RequireUnprepared();
            if (items == null)
                throw new ArgumentNullException(nameof(items));
            RequirePositive(slotWidth, nameof(slotWidth));
            bottomLegend = new List<LegendItem>(items);
            bottomLegendSlotWidth = slotWidth;
        }

        public void FitYAxisLabels(IEnumerable<String> labels, Double spacing = 12.0d)
        {
            RequireUnprepared();
            if (labels == null)
                throw new ArgumentNullException(nameof(labels));
            RequireNonnegative(spacing, nameof(spacing));
            customYAxisLabels = new List<String>(labels);
            customYAxisSpacing = spacing;
            ApplyYAxisFit(customYAxisLabels, spacing);
        }

        private void ApplyYAxisFit(IEnumerable<String> labels, Double spacing)
        {
            Double longest = 0.0d;
            foreach (String label in labels)
                longest = Math.Max(longest, EstimateTextWidth(label, TickSize));
            Double requiredLeft = TitleLeft + AxisTitleSize / 2.0d + spacing + longest + 15.0d;
            Double shift = Math.Max(0.0d, requiredLeft - PlotLeft);
            if (shift > 0.0d)
            {
                if (shift >= PlotWidth)
                    throw new InvalidOperationException("Y-axis labels do not fit within the available plot width.");
                PlotLeft += shift;
                PlotWidth -= shift;
            }
        }

        /// <summary>Finalize chart margins before rendering any plot geometry.</summary>
        public void PrepareLayout()
        {
            if (prepared)
                return;
            PlotLeft = requestedLeft;
            PlotTop = requestedTop;
            PlotWidth = requestedWidth;
            PlotHeight = requestedHeight;

            if (metadataLines.Length != 0)
            {
                Double headerEnd = MetadataTop + (metadataLines.Length - 1) * MetadataSpacing + MinimumHeaderGap;
                Double shift = Math.Max(0.0d, headerEnd - PlotTop);
                PlotTop += shift;
                PlotHeight -= shift;
            }
            if (yTicks != null)
                ApplyYAxisFit(GetLabels(yTicks), 12.0d);
            if (customYAxisLabels != null)
                ApplyYAxisFit(customYAxisLabels, customYAxisSpacing);
            if (rightColorbar != null)
            {
                Double reserved = RightColorbarGap + RightColorbarWidth + 55.0d;
                Double freeRight = Width - (PlotLeft + PlotWidth);
                Double reduction = Math.Max(0.0d, reserved - freeRight);
                PlotWidth -= reduction;
            }
            if (bottomLegend != null && bottomLegend.Count != 0)
            {
                Double bottomLimit = Height - BottomLegendMargin - LegendSize - AxisTitleSpacing - AxisTitleSize - 16.0d;
                PlotHeight = Math.Min(PlotHeight, bottomLimit - PlotTop);
            }
            if (PlotWidth <= 0.0d || PlotHeight <= 0.0d)
                throw new InvalidOperationException("Plot area is too small for the configured layout.");
            prepared = true;
        }

        public void Title(String value) => Text(TitleLeft, TitleTop, value, TitleSize, TitleColor, true);
        public void Subtitle(String value) => Text(TitleLeft, SubtitleTop, value, SubtitleSize, SecondaryColor);

        public void Metadata(params String[] lines)
        {
            if (lines == null)
                throw new ArgumentNullException(nameof(lines));
            if (!prepared)
                metadataLines = (String[])lines.Clone();
            for (Int32 i = 0; i < lines.Length; ++i)
                Text(TitleLeft, MetadataTop + i * MetadataSpacing, lines[i], MetadataSize, TextColor);
        }
        #endregion

        #region Axes
        public void DrawGrid()
        {
            EnsurePrepared();
            if (xGrid && xTicks != null)
                foreach (AxisTick tick in xTicks)
                    DataLine(tick.Position, yMinimum, tick.Position, yMaximum, GridColor, 1.0d);
            if (yGrid && yTicks != null)
                foreach (AxisTick tick in yTicks)
                    DataLine(xMinimum, tick.Position, xMaximum, tick.Position, GridColor, 1.0d);
        }

        public void DrawAxes()
        {
            EnsurePrepared();
            if (xTicks != null)
            {
                foreach (AxisTick tick in xTicks)
                    Label(X(tick.Position), PlotTop + PlotHeight + AxisLabelSpacing, tick.Label, TickSize, "middle");
                XAxisTitle(xAxisTitle);
            }
            if (yTicks != null)
            {
                foreach (AxisTick tick in yTicks)
                    Label(PlotLeft - 15.0d, Y(tick.Position) + TickSize * 0.35d, tick.Label, TickSize, "end");
                YAxisTitle(yAxisTitle);
            }
        }

        // Immediate-mode axes are kept for older plot writers. Configured axes are preferable.
        public void XAxis(String title, Double minimum, Double maximum, Int32 intervals = 5, String numberFormat = "0.##", Boolean grid = true)
        {
            RequireAxis(minimum, maximum, intervals);
            for (Int32 i = 0; i <= intervals; ++i)
            {
                Double value = minimum + (maximum - minimum) * i / intervals;
                Double x = X(value);
                if (grid)
                    Line(x, PlotTop, x, PlotTop + PlotHeight, GridColor);
                Label(x, PlotTop + PlotHeight + AxisLabelSpacing, Format(value, numberFormat), TickSize, "middle");
            }
            XAxisTitle(title);
        }

        public void YAxis(String title, Double minimum, Double maximum, Int32 intervals = 5, String numberFormat = "0.##", Boolean grid = true)
        {
            RequireAxis(minimum, maximum, intervals);
            for (Int32 i = 0; i <= intervals; ++i)
            {
                Double value = minimum + (maximum - minimum) * i / intervals;
                Double y = Y(value);
                if (grid)
                    Line(PlotLeft, y, PlotLeft + PlotWidth, y, GridColor);
                Label(PlotLeft - 15.0d, y + TickSize * 0.35d, Format(value, numberFormat), TickSize, "end");
            }
            YAxisTitle(title);
        }

        public void XTicks(Int32 count, Func<Int32, String> label, Double start = 0.0d, Double step = 1.0d)
        {
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count));
            if (label == null)
                throw new ArgumentNullException(nameof(label));
            for (Int32 i = 0; i < count; ++i)
                Label(X(start + i * step), PlotTop + PlotHeight + AxisLabelSpacing, label(i), TickSize, "middle");
        }

        public void YTicks(Int32 count, Func<Int32, String> label, Double start = 0.0d, Double step = 1.0d)
        {
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count));
            if (label == null)
                throw new ArgumentNullException(nameof(label));
            for (Int32 i = 0; i < count; ++i)
                Label(PlotLeft - 15.0d, Y(start + i * step) + TickSize * 0.35d, label(i), TickSize, "end");
        }

        public void XAxisTitle(String title)
        {
            if (!String.IsNullOrEmpty(title))
                Text(PlotLeft + PlotWidth / 2.0d, PlotTop + PlotHeight + AxisTitleSpacing, title, AxisTitleSize, TextColor, true, "middle");
        }

        public void YAxisTitle(String title)
        {
            if (String.IsNullOrEmpty(title))
                return;
            Double cx = TitleLeft;
            Double cy = PlotTop + PlotHeight / 2.0d;
            writer.WriteLine($"<text x=\"{F(cx)}\" y=\"{F(cy)}\" transform=\"rotate(-90 {F(cx)} {F(cy)})\" text-anchor=\"middle\" font-family=\"{Escape(FontFamily)}\" font-size=\"{AxisTitleSize}\" font-weight=\"bold\" fill=\"{Escape(TextColor)}\">{Escape(title)}</text>");
        }

        public void DrawBorder() => Border(PlotLeft, PlotTop, PlotWidth, PlotHeight);
        #endregion

        #region Drawing
        public void BeginPlot()
        {
            EnsurePrepared();
            if (plotOpen)
                throw new InvalidOperationException("A clipped plot group is already open.");
            if (!clipDefined)
            {
                writer.WriteLine($"<defs><clipPath id=\"{CLIP_ID}\"><rect x=\"{F(PlotLeft)}\" y=\"{F(PlotTop)}\" width=\"{F(PlotWidth)}\" height=\"{F(PlotHeight)}\"/></clipPath></defs>");
                clipDefined = true;
            }
            writer.WriteLine($"<g clip-path=\"url(#{CLIP_ID})\">");
            plotOpen = true;
        }

        public void EndPlot()
        {
            if (!plotOpen)
                throw new InvalidOperationException("No clipped plot group is open.");
            writer.WriteLine("</g>");
            plotOpen = false;
        }

        public void Rectangle(Double x, Double y, Double width, Double height, String fill, String stroke = null, Double strokeWidth = 1.0d)
        {
            RequireFinite(x, nameof(x));
            RequireFinite(y, nameof(y));
            RequireNonnegative(width, nameof(width));
            RequireNonnegative(height, nameof(height));
            writer.WriteLine($"<rect x=\"{F(x)}\" y=\"{F(y)}\" width=\"{F(width)}\" height=\"{F(height)}\" fill=\"{Escape(fill)}\"{(stroke == null ? String.Empty : $" stroke=\"{Escape(stroke)}\" stroke-width=\"{F(strokeWidth)}\"")}/>");
        }

        public void Border(Double x, Double y, Double width, Double height) => Rectangle(x, y, width, height, "none", BorderColor);

        public void Line(Double x1, Double y1, Double x2, Double y2, String color, Double thickness = 1.0d, String dash = null, Double opacity = 1.0d)
        {
            RequireFinite(x1, nameof(x1));
            RequireFinite(y1, nameof(y1));
            RequireFinite(x2, nameof(x2));
            RequireFinite(y2, nameof(y2));
            RequireNonnegative(thickness, nameof(thickness));
            if (!Double.IsFinite(opacity) || opacity < 0.0d || opacity > 1.0d)
                throw new ArgumentOutOfRangeException(nameof(opacity));
            writer.WriteLine($"<line x1=\"{F(x1)}\" y1=\"{F(y1)}\" x2=\"{F(x2)}\" y2=\"{F(y2)}\" stroke=\"{Escape(color)}\" stroke-width=\"{F(thickness)}\"{(dash == null ? String.Empty : $" stroke-dasharray=\"{Escape(dash)}\"")}{(opacity == 1.0d ? String.Empty : $" stroke-opacity=\"{F(opacity)}\"")}/>");
        }

        public void DataLine(Double x1, Double y1, Double x2, Double y2, String color, Double thickness = 2.0d, String dash = null, Double opacity = 1.0d)
            => Line(X(x1), Y(y1), X(x2), Y(y2), color, thickness, dash, opacity);

        public void VerticalReference(Double value, String color, Double thickness = 2.0d, Boolean dashed = false)
            => Line(X(value), PlotTop, X(value), PlotTop + PlotHeight, color, thickness, dashed ? "7 6" : null);

        public void HorizontalReference(Double value, String color, Double thickness = 2.0d, Boolean dashed = false)
            => Line(PlotLeft, Y(value), PlotLeft + PlotWidth, Y(value), color, thickness, dashed ? "7 6" : null);

        public void Bar(Int32 index, Int32 count, Double value, String color, Double gap = 1.0d)
        {
            if (count <= 0 || index < 0 || index >= count)
                throw new ArgumentOutOfRangeException(nameof(index));
            RequireNonnegative(gap, nameof(gap));
            RequireFinite(value, nameof(value));
            Double width = PlotWidth / count;
            Double zero = Y(0.0d);
            Double end = Y(value);
            Rectangle(PlotLeft + index * width + gap / 2.0d, Math.Min(zero, end), Math.Max(0.0d, width - gap), Math.Abs(end - zero), color);
        }

        public void HeatmapCell(Int32 column, Int32 row, Int32 columns, Int32 rows, String color, Double overlap = 0.008d)
        {
            if (columns <= 0 || rows <= 0 || column < 0 || column >= columns || row < 0 || row >= rows)
                throw new ArgumentOutOfRangeException(nameof(column));
            RequireNonnegative(overlap, nameof(overlap));
            Double width = PlotWidth / columns;
            Double height = PlotHeight / rows;
            Rectangle(PlotLeft + column * width, PlotTop + row * height, width + overlap, height + overlap, color);
        }

        /// <summary>
        /// Render one top-down heatmap row. Identically colored adjacent cells are merged
        /// to reduce SVG size. A cached palette avoids per-cell hexadecimal parsing.
        /// </summary>
        public void HeatmapRow(ReadOnlySpan<Double> values, Int32 row, Int32 rows, Double minimum, Double maximum,
            String lowColor, String highColor, Boolean mergeCells = true)
        {
            if (rows <= 0 || row < 0 || row >= rows || values.IsEmpty)
                throw new ArgumentOutOfRangeException(nameof(row));
            String[] palette = GetPalette(lowColor, highColor);
            Double cellWidth = PlotWidth / values.Length;
            Double cellHeight = PlotHeight / rows;
            Double y = PlotTop + row * cellHeight;
            Int32 start = 0;
            while (start < values.Length)
            {
                Int32 colorIndex = PaletteIndex(values[start], minimum, maximum, palette.Length);
                Int32 end = start + 1;
                if (mergeCells)
                    while (end < values.Length && PaletteIndex(values[end], minimum, maximum, palette.Length) == colorIndex)
                        ++end;
                Rectangle(PlotLeft + start * cellWidth, y, (end - start) * cellWidth + 0.008d, cellHeight + 0.008d, palette[colorIndex]);
                start = end;
            }
        }

        public void Polyline(IReadOnlyList<(Double X, Double Y)> points, String color, Double thickness = 2.0d)
        {
            if (points == null)
                throw new ArgumentNullException(nameof(points));
            if (points.Count == 0)
                return;
            StringBuilder data = new();
            for (Int32 i = 0; i < points.Count; ++i)
                data.Append(i == 0 ? "M " : " L ").Append(F(X(points[i].X))).Append(' ').Append(F(Y(points[i].Y)));
            Path(data.ToString(), color, thickness);
        }

        public void Path(String data, String stroke, Double thickness = 2.0d, String fill = "none")
        {
            RequireNonnegative(thickness, nameof(thickness));
            writer.WriteLine($"<path d=\"{Escape(data)}\" fill=\"{Escape(fill)}\" stroke=\"{Escape(stroke)}\" stroke-width=\"{F(thickness)}\" stroke-linejoin=\"round\"/>");
        }

        public void Text(Double x, Double y, String value, Int32 size, String color, Boolean bold = false, String anchor = "start")
        {
            RequireFinite(x, nameof(x));
            RequireFinite(y, nameof(y));
            writer.WriteLine($"<text x=\"{F(x)}\" y=\"{F(y)}\" text-anchor=\"{Escape(anchor)}\" font-family=\"{Escape(FontFamily)}\" font-size=\"{size}\" fill=\"{Escape(color)}\"{(bold ? " font-weight=\"bold\"" : String.Empty)}>{Escape(value)}</text>");
        }

        public void Label(Double x, Double y, String value, Int32 size = 12, String anchor = "start", String color = null)
            => Text(x, y, value, size, color ?? SecondaryColor, false, anchor);
        #endregion

        #region Colors and Legends
        public String Color(Double value, Double minimum, Double maximum, String lowColor, String highColor, Boolean saturate = true)
        {
            String[] palette = GetPalette(lowColor, highColor);
            if (!saturate && (value < minimum || value > maximum))
                throw new ArgumentOutOfRangeException(nameof(value));
            return palette[PaletteIndex(value, minimum, maximum, palette.Length)];
        }

        public String DivergingColor(Double value, Double minimum, Double center, Double maximum, String lowColor, String middleColor, String highColor)
        {
            if (!(minimum < center && center < maximum))
                throw new ArgumentException("Diverging color bounds must be strictly ordered.");
            return value <= center ? Color(value, minimum, center, lowColor, middleColor) : Color(value, center, maximum, middleColor, highColor);
        }

        public void DrawRightColorbar()
        {
            EnsurePrepared();
            if (rightColorbar == null)
                return;
            ColorbarSettings c = rightColorbar;
            Double left = PlotLeft + PlotWidth + RightColorbarGap;
            Double top = PlotTop + 30.0d;
            Double height = Math.Min(RightColorbarHeight, Math.Max(1.0d, PlotHeight - 30.0d));
            Colorbar(c.Title, left, top, RightColorbarWidth, height, c.Minimum, c.Maximum, c.LowColor, c.HighColor,
                c.Intervals, c.Saturated, c.Format, c.Suffix, c.MiddleColor, c.Center);
        }

        public void Colorbar(String title, Double left, Double top, Double width, Double height, Double minimum, Double maximum,
            String lowColor, String highColor, Int32 intervals = 5, Boolean saturated = false, String format = "0.##", String suffix = "",
            String middleColor = null, Double? center = null)
        {
            RequireAxis(minimum, maximum, intervals);
            RequirePositive(width, nameof(width));
            RequirePositive(height, nameof(height));
            if ((middleColor == null) != (center == null))
                throw new ArgumentException("Diverging colorbars require both a middle color and a center value.");
            if (center.HasValue && (center.Value <= minimum || center.Value >= maximum))
                throw new ArgumentOutOfRangeException(nameof(center));
            Text(left - 5.0d, top - 13.0d, title, 14, TextColor, true);
            const Int32 bands = 100;
            for (Int32 i = 0; i < bands; ++i)
            {
                Double segmentHeight = height / bands;
                Double value = minimum + (i + 0.5d) / bands * (maximum - minimum);
                String color = center.HasValue
                    ? DivergingColor(value, minimum, center.Value, maximum, lowColor, middleColor, highColor)
                    : Color(value, minimum, maximum, lowColor, highColor);
                Rectangle(left, top + (bands - i - 1) * segmentHeight, width, segmentHeight + 0.02d, color);
            }
            Border(left, top, width, height);
            for (Int32 i = 0; i <= intervals; ++i)
            {
                Double value = minimum + (maximum - minimum) * i / intervals;
                String label = Format(value, format) + suffix;
                if (saturated && i == 0)
                    label = "≤ " + label;
                else if (saturated && i == intervals)
                    label = "≥ " + label;
                Label(left + width + 10.0d, top + height - i * height / intervals + 4.0d, label, TickSize);
            }
        }

        public void LegendBox(Double x, Double y, String label, String color)
        {
            Rectangle(x, y - 11.0d, 17.0d, 12.0d, color);
            Label(x + 26.0d, y, label, LegendSize, "start", TextColor);
        }

        public void LegendLine(Double x, Double y, String label, String color, Boolean dashed = false)
        {
            Line(x, y - 5.0d, x + 30.0d, y - 5.0d, color, 3.0d, dashed ? "7 5" : null);
            Label(x + 39.0d, y, label, LegendSize, "start", TextColor);
        }

        public void DrawBottomLegend()
        {
            if (bottomLegend != null)
                BottomLegend(bottomLegend, bottomLegendSlotWidth);
        }

        public void BottomLegend(IReadOnlyList<LegendItem> items, Double slotWidth = 180.0d)
        {
            if (items == null)
                throw new ArgumentNullException(nameof(items));
            if (items.Count == 0)
                return;
            RequirePositive(slotWidth, nameof(slotWidth));

            const Double symbolWidth = 30.0d;
            const Double boxWidth = 17.0d;
            const Double labelGap = 9.0d;
            Double required = slotWidth;
            foreach (LegendItem item in items)
                required = Math.Max(required, (item.IsLine ? symbolWidth : boxWidth) + labelGap + EstimateTextWidth(item.Label, LegendSize) + 12.0d);
            Int32 columns = Math.Max(1, Math.Min(items.Count, (Int32)Math.Floor(PlotWidth / required)));
            Double actualSlotWidth = Math.Min(required, PlotWidth / columns);
            Int32 rows = (items.Count + columns - 1) / columns;
            Double lastBaseline = Height - BottomLegendMargin;
            for (Int32 row = 0; row < rows; ++row)
            {
                Int32 first = row * columns;
                Int32 length = Math.Min(columns, items.Count - first);
                Double startX = PlotLeft + (PlotWidth - length * actualSlotWidth) / 2.0d;
                Double y = lastBaseline - (rows - row - 1) * (LegendSize + 12.0d);
                for (Int32 i = 0; i < length; ++i)
                {
                    LegendItem item = items[first + i];
                    Double symbol = item.IsLine ? symbolWidth : boxWidth;
                    Double groupWidth = symbol + labelGap + EstimateTextWidth(item.Label, LegendSize);
                    Double x = startX + (i + 0.5d) * actualSlotWidth - groupWidth / 2.0d;
                    if (item.IsLine)
                        LegendLine(x, y, item.Label, item.Color, item.Dashed);
                    else
                        LegendBox(x, y, item.Label, item.Color);
                }
            }
        }
        #endregion

        #region Document
        public void End()
        {
            if (completed)
                return;
            if (plotOpen)
                EndPlot();
            writer.WriteLine("</svg>");
            completed = true;
            writer.Flush();
        }

        public void Dispose()
        {
            try
            {
                End();
            }
            finally
            {
                writer.Dispose();
            }
        }
        #endregion

        #region Helpers
        public static String Format(Double value, String numberFormat = "0.######") => value.ToString(numberFormat, CultureInfo.InvariantCulture);
        public static Double NiceCeiling(Double maximum, Int32 targetIntervals = 5)
        {
            RequirePositive(maximum, nameof(maximum));
            if (targetIntervals <= 0)
                throw new ArgumentOutOfRangeException(nameof(targetIntervals));
            Double roughStep = maximum / targetIntervals;
            Double magnitude = Math.Pow(10.0d, Math.Floor(Math.Log10(roughStep)));
            Double normalized = roughStep / magnitude;
            Double step = (normalized <= 1.0d ? 1.0d : normalized <= 2.0d ? 2.0d : normalized <= 5.0d ? 5.0d : 10.0d) * magnitude;
            return Math.Ceiling(maximum / step) * step;
        }

        private static String F(Double value) => Format(value);

        private static String Escape(String value)
        {
            if (value == null)
                return String.Empty;
            return value.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;").Replace("'", "&apos;");
        }

        private String[] GetPalette(String lowColor, String highColor)
        {
            if (PaletteResolution < 2 || PaletteResolution > 4096)
                throw new ArgumentOutOfRangeException(nameof(PaletteResolution));
            if (lastPalette != null && lastPaletteResolution == PaletteResolution &&
                String.Equals(lastPaletteLow, lowColor, StringComparison.Ordinal) &&
                String.Equals(lastPaletteHigh, highColor, StringComparison.Ordinal))
                return lastPalette;
            String key = lowColor + "|" + highColor + "|" + PaletteResolution.ToString(CultureInfo.InvariantCulture);
            if (palettes.TryGetValue(key, out String[] cached))
            {
                lastPaletteLow = lowColor;
                lastPaletteHigh = highColor;
                lastPaletteResolution = PaletteResolution;
                lastPalette = cached;
                return cached;
            }
            (Int32 r0, Int32 g0, Int32 b0) = ParseColor(lowColor);
            (Int32 r1, Int32 g1, Int32 b1) = ParseColor(highColor);
            String[] palette = new String[PaletteResolution];
            for (Int32 i = 0; i < palette.Length; ++i)
            {
                Double t = (Double)i / (palette.Length - 1);
                Int32 r = (Int32)Math.Round(r0 + (r1 - r0) * t);
                Int32 g = (Int32)Math.Round(g0 + (g1 - g0) * t);
                Int32 b = (Int32)Math.Round(b0 + (b1 - b0) * t);
                palette[i] = $"#{r:X2}{g:X2}{b:X2}";
            }
            palettes.Add(key, palette);
            lastPaletteLow = lowColor;
            lastPaletteHigh = highColor;
            lastPaletteResolution = PaletteResolution;
            lastPalette = palette;
            return palette;
        }

        private static (Int32 R, Int32 G, Int32 B) ParseColor(String value)
        {
            if (value == null || value.Length != 7 || value[0] != '#')
                throw new ArgumentException("Colors must use #RRGGBB notation.", nameof(value));
            return (
                Int32.Parse(value.Substring(1, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture),
                Int32.Parse(value.Substring(3, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture),
                Int32.Parse(value.Substring(5, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture)
            );
        }

        private static Int32 PaletteIndex(Double value, Double minimum, Double maximum, Int32 count)
        {
            if (!Double.IsFinite(value) || !Double.IsFinite(minimum) || !Double.IsFinite(maximum) || maximum <= minimum)
                throw new ArgumentOutOfRangeException(nameof(value), "Palette values and bounds must be finite, with an increasing range.");
            Double t = Math.Max(0.0d, Math.Min(1.0d, (value - minimum) / (maximum - minimum)));
            return (Int32)Math.Round(t * (count - 1));
        }

        private static IEnumerable<String> GetLabels(IEnumerable<AxisTick> ticks)
        {
            foreach (AxisTick tick in ticks)
                yield return tick.Label;
        }

        private static Double EstimateTextWidth(String value, Int32 size)
        {
            if (String.IsNullOrEmpty(value))
                return 0.0d;
            Double units = 0.0d;
            foreach (Char c in value)
                units += c == ' ' ? 0.3d : c == ',' || c == '.' || c == ':' ? 0.28d :
                    c == 'I' || c == 'l' || c == 'i' ? 0.3d : c == 'W' || c == 'M' ? 0.9d : 0.6d;
            return units * size * 1.07d;
        }

        private static void RequireIndexed(Int32 count, Int32 step)
        {
            if (count <= 0)
                throw new ArgumentOutOfRangeException(nameof(count));
            if (step <= 0)
                throw new ArgumentOutOfRangeException(nameof(step));
        }

        private static void RequireAxis(Double minimum, Double maximum, Int32 intervals)
        {
            RequireFinite(minimum, nameof(minimum));
            RequireFinite(maximum, nameof(maximum));
            if (maximum <= minimum)
                throw new ArgumentException("Axis bounds must be strictly increasing.");
            if (intervals <= 0)
                throw new ArgumentOutOfRangeException(nameof(intervals));
        }

        private static void RequireFinite(Double value, String name)
        {
            if (!Double.IsFinite(value))
                throw new ArgumentOutOfRangeException(name);
        }

        private static void RequirePositive(Double value, String name)
        {
            RequireFinite(value, name);
            if (value <= 0.0d)
                throw new ArgumentOutOfRangeException(name);
        }

        private static void RequireNonnegative(Double value, String name)
        {
            RequireFinite(value, name);
            if (value < 0.0d)
                throw new ArgumentOutOfRangeException(name);
        }

        private void RequireUnprepared()
        {
            if (prepared)
                throw new InvalidOperationException("Layout cannot be reconfigured after PrepareLayout().");
        }

        private void EnsurePrepared()
        {
            if (!prepared)
                PrepareLayout();
        }
        #endregion
    }
}