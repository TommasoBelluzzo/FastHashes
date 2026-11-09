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
        #region Constants
        private const String CLIP_ID = "fasthashes-plot-clip";
        #endregion

        #region Members
        private Boolean m_ClipDefined;
        private Boolean m_Completed;
        private Boolean m_GridX;
        private Boolean m_GridY;
        private Boolean m_PlotOpen;
        private Boolean m_Prepared;
        private ColorbarSettings m_ColorbarSettings;
        private readonly Dictionary<String, String[]> m_Palettes = new(StringComparer.Ordinal);
        private Double m_CustomAxisSpacingY = 12.0d;
        private Double m_MaximumX;
        private Double m_MaximumY;
        private Double m_MinimumX;
        private Double m_MinimumY;
        private Double m_RequestedHeight;
        private Double m_RequestedLeft;
        private Double m_RequestedTop;
        private Double m_RequestedWidth;
        private Int32 m_LastPaletteResolution;
        private IReadOnlyList<AxisTick> m_TicksX;
        private IReadOnlyList<AxisTick> m_TicksY;
        private IReadOnlyList<LegendItem> m_Legend;
        private IReadOnlyList<String> m_CustomAxisLabelsY;
        private readonly StreamWriter m_Writer;
        private String[] m_LastPalette;
        private String[] m_MetadataLines = [];
        private String m_AxisTitleX;
        private String m_AxisTitleY;
        private String m_LastPaletteHigh;
        private String m_LastPaletteLow;
        #endregion

        #region Properties: Appearance
        public String BackgroundColor { get; set; } = "#ffffff";
        public String BorderColor { get; set; } = "#8b99ab";
        public String GridColor { get; set; } = "#e3e9f1";
        public String HighlightColor { get; set; } = "#b43f53";
        public String MutedColor { get; set; } = "#64748b";
        public String ObservedColor { get; set; } = "#4683ad";
        public Int32 PaletteResolution { get; set; } = 256;
        public String ReferenceColor { get; set; } = "#ed8a37";
        public String SecondaryColor { get; set; } = "#556174";
        public String TextColor { get; set; } = "#303c50";
        public String TitleColor { get; set; } = "#172235";
        #endregion

        #region Properties: Geometry
        public Int32 Height { get; }
        public Int32 Width { get; }
        public Double PlotLeft { get; private set; }
        public Double PlotTop { get; private set; }
        public Double PlotHeight { get; private set; }
        public Double PlotWidth { get; private set; }
        #endregion

        #region Properties: Layout
        public Double AxisLabelSpacing { get; set; } = 24.0d;
        public Double AxisTitleSpacing { get; set; } = 48.0d;
        public Double BottomLegendMargin { get; set; } = 32.0d;
        public Double BottomLegendSlotWidth { get; set; } = 180.0d;
        public Double MetadataSpacing { get; set; } = 23.0d;
        public Double MetadataTop { get; set; } = 103.0d;
        public Double MinimumHeaderGap { get; set; } = 50.0d;
        public Double RightColorbarGap { get; set; } = 60.0d;
        public Double RightColorbarHeight { get; set; } = 390.0d;
        public Double RightColorbarWidth { get; set; } = 24.0d;
        public Double SubtitleTop { get; set; } = 70.0d;
        public Double TitleLeft { get; set; } = 66.0d;
        public Double TitleTop { get; set; } = 43.0d;
        #endregion

        #region Properties: Typography
        public String FontFamily { get; set; } = "Arial, Helvetica, sans-serif";
        public Int32 AxisTitleSize { get; set; } = 14;
        public Int32 LegendSize { get; set; } = 13;
        public Int32 MetadataSize { get; set; } = 14;
        public Int32 SubtitleSize { get; set; } = 14;
        public Int32 TickSize { get; set; } = 12;
        public Int32 TitleSize { get; set; } = 26;
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

            String ariaLabel = (description == null) ? String.Empty : $" aria-label=\"{Escape(description)}\"";

            m_Writer = new StreamWriter(path, false, new UTF8Encoding(false));
            m_Writer.WriteLine($"<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"{width}\" height=\"{height}\" viewBox=\"0 0 {width} {height}\" role=\"img\"{ariaLabel}>");
            m_Writer.WriteLine($"<rect width=\"100%\" height=\"100%\" fill=\"{BackgroundColor}\"/>");
        }
        #endregion

        #region Methods: Header
        public void Metadata(params String[] lines)
        {
            ArgumentNullException.ThrowIfNull(lines);

            if (!m_Prepared)
                m_MetadataLines = (String[])lines.Clone();

            for (Int32 i = 0; i < lines.Length; ++i)
                Text(TitleLeft, MetadataTop + (i * MetadataSpacing), lines[i], MetadataSize, TextColor);
        }

        public void Subtitle(String value)
        {
            Text(TitleLeft, SubtitleTop, value, SubtitleSize, SecondaryColor);
        }

        public void Title(String value)
        {
            Text(TitleLeft, TitleTop, value, TitleSize, TitleColor, true);
        }
        #endregion

        #region Methods: Layout
        public Double CellX(Int32 index, Int32 count)
        {
            return PlotLeft + (((index + 0.5d) * PlotWidth) / count);
        }

        public Double CellY(Int32 index, Int32 count)
        {
            return PlotTop + (((index + 0.5d) * PlotHeight) / count);
        }

        public Double X(Double value)
        {
            return PlotLeft + (((value - m_MinimumX) / (m_MaximumX - m_MinimumX)) * PlotWidth);
        }

        public Double Y(Double value)
        {
            return PlotTop + PlotHeight - (((value - m_MinimumY) / (m_MaximumY - m_MinimumY)) * PlotHeight);
        }

        public void ConfigureDiscreteAxisX(String title, Double minimum, Double maximum, Boolean grid = true, Int32 intervals = 5, String format = "0.##")
        {
            RequireUnprepared();
            RequireAxis(minimum, maximum, intervals);

            List<AxisTick> ticks = new(intervals + 1);
            
            for (Int32 i = 0; i <= intervals; ++i)
            {
                Double value = minimum + (((maximum - minimum) * i) / intervals);
                ticks.Add(new AxisTick(value, Format(value, format)));
            }

            m_GridX = grid;
            m_AxisTitleX = title;
            m_TicksX = ticks;
        }

        public void ConfigureDiscreteAxisY(String title, Double minimum, Double maximum, Boolean grid = true, Int32 intervals = 5, String format = "0.##")
        {
            RequireUnprepared();
            RequireAxis(minimum, maximum, intervals);

            List<AxisTick> ticks = new(intervals + 1);

            for (Int32 i = 0; i <= intervals; ++i)
            {
                Double value = minimum + (((maximum - minimum) * i) / intervals);
                ticks.Add(new AxisTick(value, Format(value, format)));
            }

            m_GridY = grid;
            m_AxisTitleY = title;
            m_TicksY = ticks;
        }

        public void ConfigureIndexedAxisX(String title, Int32 count, Int32 step, Boolean includeLast = true)
        {
            RequireIndexed(count, step);

            List<(Double, String)> ticks = [];
            
            for (Int32 i = 0; i < count; i += step)
                ticks.Add((i + 0.5d, i.ToString(CultureInfo.InvariantCulture)));
            
            if (includeLast && (((count - 1) % step) != 0))
                ticks.Add((count - 0.5d, (count - 1).ToString(CultureInfo.InvariantCulture)));
            
            ConfigureTicksX(title, ticks);
        }

        public void ConfigureIndexedAxisY(String title, Int32 count, Int32 step, Boolean topDown = true, Boolean includeLast = true)
        {
            RequireIndexed(count, step);

            List<(Double Value, String Label)> ticks = [];
            
            for (Int32 i = 0; i < count; i += step)
                ticks.Add((topDown ? (count - i - 0.5d) : (i + 0.5d), i.ToString(CultureInfo.InvariantCulture)));
            
            if (includeLast && (((count - 1) % step) != 0))
                ticks.Add((topDown ? 0.5d : (count - 0.5d), (count - 1).ToString(CultureInfo.InvariantCulture)));
            
            ConfigureTicksY(title, ticks);
        }

        public void ConfigureTicksX(String title, IReadOnlyList<(Double, String)> sourceTicks, Boolean grid = false)
        {
            RequireUnprepared();
            ArgumentNullException.ThrowIfNull(sourceTicks);

            List<AxisTick> ticks = new(sourceTicks.Count);

            foreach ((Double value, String label) in sourceTicks)
            {
                RequireFinite(value, nameof(sourceTicks));
                ticks.Add(new AxisTick(value, label));
            }

            m_GridX = grid;
            m_AxisTitleX = title;
            m_TicksX = ticks;
        }

        public void ConfigureTicksY(String title, IReadOnlyList<(Double, String)> sourceTicks, Boolean grid = false)
        {
            RequireUnprepared();
            ArgumentNullException.ThrowIfNull(sourceTicks);

            List<AxisTick> ticks = new(sourceTicks.Count);

            foreach ((Double value, String label) in sourceTicks)
            {
                RequireFinite(value, nameof(sourceTicks));
                ticks.Add(new AxisTick(value, label));
            }

            m_GridY = grid;
            m_AxisTitleY = title;
            m_TicksY = ticks;
        }

        public void DataRange(Double minimumX, Double maximumX, Double minimumY, Double maximumY)
        {
            RequireFinite(minimumX, nameof(minimumX));
            RequireFinite(maximumX, nameof(maximumX));
            RequireFinite(minimumY, nameof(minimumY));
            RequireFinite(maximumY, nameof(maximumY));
            
            if ((maximumX <= minimumX) || (maximumY <= minimumY))
                throw new ArgumentException("Data ranges must be strictly increasing.");
            
            m_MinimumX = minimumX;
            m_MaximumX = maximumX;
            m_MinimumY = minimumY;
            m_MaximumY = maximumY;
        }

        public void PlotArea(Double left, Double top, Double width, Double height)
        {
            RequireUnprepared();
            RequireFinite(left, nameof(left));
            RequireFinite(top, nameof(top));
            RequirePositive(width, nameof(width));
            RequirePositive(height, nameof(height));
            
            m_RequestedLeft = PlotLeft = left;
            m_RequestedTop = PlotTop = top;
            m_RequestedWidth = PlotWidth = width;
            m_RequestedHeight = PlotHeight = height;
        }








        public void ConfigureRightColorbar(String title, Double minimum, Double maximum, String lowColor, String highColor,
            Int32 intervals = 5, Boolean saturatedLower = false, Boolean saturatedUpper = false, String format = "0.##", String suffix = "", String middleColor = null, Double? center = null)
        {
            RequireUnprepared();
            RequireAxis(minimum, maximum, intervals);
            if ((middleColor == null) != (center == null))
                throw new ArgumentException("Diverging colorbars require both a middle color and a center value.");
            if (center.HasValue && (center.Value <= minimum || center.Value >= maximum))
                throw new ArgumentOutOfRangeException(nameof(center));
            m_ColorbarSettings = new ColorbarSettings(title, minimum, maximum, lowColor, middleColor, highColor, intervals, center, saturatedLower, saturatedUpper, format, suffix);
        }

        public void ConfigureBottomLegend(IReadOnlyList<LegendItem> items, Double slotWidth = 180.0d)
        {
            RequireUnprepared();
            if (items == null)
                throw new ArgumentNullException(nameof(items));
            RequirePositive(slotWidth, nameof(slotWidth));
            m_Legend = [.. items];
            BottomLegendSlotWidth = slotWidth;
        }

        public void FitYAxisLabels(IEnumerable<String> labels, Double spacing = 12.0d)
        {
            RequireUnprepared();
            if (labels == null)
                throw new ArgumentNullException(nameof(labels));
            RequireNonnegative(spacing, nameof(spacing));
            m_CustomAxisLabelsY = [.. labels];
            m_CustomAxisSpacingY = spacing;
            ApplyYAxisFit(m_CustomAxisLabelsY, spacing);
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

        public void PrepareLayout()
        {
            if (m_Prepared)
                return;
            PlotLeft = m_RequestedLeft;
            PlotTop = m_RequestedTop;
            PlotWidth = m_RequestedWidth;
            PlotHeight = m_RequestedHeight;

            if (m_MetadataLines.Length != 0)
            {
                Double headerEnd = MetadataTop + (m_MetadataLines.Length - 1) * MetadataSpacing + MinimumHeaderGap;
                Double shift = Math.Max(0.0d, headerEnd - PlotTop);
                PlotTop += shift;
                PlotHeight -= shift;
            }
            if (m_TicksY != null)
                ApplyYAxisFit(GetLabels(m_TicksY), 12.0d);
            if (m_CustomAxisLabelsY != null)
                ApplyYAxisFit(m_CustomAxisLabelsY, m_CustomAxisSpacingY);
            if (m_ColorbarSettings != null)
            {
                Double reserved = RightColorbarGap + RightColorbarWidth + 55.0d;
                Double freeRight = Width - (PlotLeft + PlotWidth);
                Double reduction = Math.Max(0.0d, reserved - freeRight);
                PlotWidth -= reduction;
            }
            if (m_Legend != null && m_Legend.Count != 0)
            {
                Double bottomLimit = Height - BottomLegendMargin - LegendSize - AxisTitleSpacing - AxisTitleSize - 16.0d;
                PlotHeight = Math.Min(PlotHeight, bottomLimit - PlotTop);
            }
            if (PlotWidth <= 0.0d || PlotHeight <= 0.0d)
                throw new InvalidOperationException("Plot area is too small for the configured layout.");
            m_Prepared = true;
        }
        #endregion

        #region Axes
        public void DrawGrid()
        {
            EnsurePrepared();
            if (m_GridX && m_TicksX != null)
                foreach (AxisTick tick in m_TicksX)
                    DataLine(tick.Position, m_MinimumY, tick.Position, m_MaximumY, GridColor, 1.0d);
            if (m_GridY && m_TicksY != null)
                foreach (AxisTick tick in m_TicksY)
                    DataLine(m_MinimumX, tick.Position, m_MaximumX, tick.Position, GridColor, 1.0d);
        }

        public void DrawAxes()
        {
            EnsurePrepared();

            if (m_TicksX != null)
            {
                foreach (AxisTick tick in m_TicksX)
                    Label(X(tick.Position), PlotTop + PlotHeight + AxisLabelSpacing, tick.Label, TickSize, "middle");
                XAxisTitle(m_AxisTitleX);
            }

            if (m_TicksY != null)
            {
                foreach (AxisTick tick in m_TicksY)
                    Label(PlotLeft - 15.0d, Y(tick.Position) + TickSize * 0.35d, tick.Label, TickSize, "end");
                YAxisTitle(m_AxisTitleY);
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
            Double centerX = TitleLeft;
            Double centerY = PlotTop + PlotHeight / 2.0d;
            m_Writer.WriteLine($"<text x=\"{F(centerX)}\" y=\"{F(centerY)}\" transform=\"rotate(-90 {F(centerX)} {F(centerY)})\" text-anchor=\"middle\" font-family=\"{Escape(FontFamily)}\" font-size=\"{AxisTitleSize}\" font-weight=\"bold\" fill=\"{Escape(TextColor)}\">{Escape(title)}</text>");
        }

        public void DrawBorder() => Border(PlotLeft, PlotTop, PlotWidth, PlotHeight);
        #endregion

        #region Drawing
        public void BeginPlot()
        {
            EnsurePrepared();
            if (m_PlotOpen)
                throw new InvalidOperationException("A clipped plot group is already open.");
            if (!m_ClipDefined)
            {
                m_Writer.WriteLine($"<defs><clipPath id=\"{CLIP_ID}\"><rect x=\"{F(PlotLeft)}\" y=\"{F(PlotTop)}\" width=\"{F(PlotWidth)}\" height=\"{F(PlotHeight)}\"/></clipPath></defs>");
                m_ClipDefined = true;
            }
            m_Writer.WriteLine($"<g clip-path=\"url(#{CLIP_ID})\">");
            m_PlotOpen = true;
        }

        public void EndPlot()
        {
            if (!m_PlotOpen)
                throw new InvalidOperationException("No clipped plot group is open.");
            m_Writer.WriteLine("</g>");
            m_PlotOpen = false;
        }

        public void Rectangle(Double x, Double y, Double width, Double height, String fill, String stroke = null, Double strokeWidth = 1.0d)
        {
            RequireFinite(x, nameof(x));
            RequireFinite(y, nameof(y));
            RequireNonnegative(width, nameof(width));
            RequireNonnegative(height, nameof(height));
            m_Writer.WriteLine($"<rect x=\"{F(x)}\" y=\"{F(y)}\" width=\"{F(width)}\" height=\"{F(height)}\" fill=\"{Escape(fill)}\"{(stroke == null ? String.Empty : $" stroke=\"{Escape(stroke)}\" stroke-width=\"{F(strokeWidth)}\"")}/>");
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
            m_Writer.WriteLine($"<line x1=\"{F(x1)}\" y1=\"{F(y1)}\" x2=\"{F(x2)}\" y2=\"{F(y2)}\" stroke=\"{Escape(color)}\" stroke-width=\"{F(thickness)}\"{(dash == null ? String.Empty : $" stroke-dasharray=\"{Escape(dash)}\"")}{(opacity == 1.0d ? String.Empty : $" stroke-opacity=\"{F(opacity)}\"")}/>");
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
            ArgumentNullException.ThrowIfNull(points);
            
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
            m_Writer.WriteLine($"<path d=\"{Escape(data)}\" fill=\"{Escape(fill)}\" stroke=\"{Escape(stroke)}\" stroke-width=\"{F(thickness)}\" stroke-linejoin=\"round\"/>");
        }

        public void Text(Double x, Double y, String value, Int32 size, String color, Boolean bold = false, String anchor = "start")
        {
            RequireFinite(x, nameof(x));
            RequireFinite(y, nameof(y));
            m_Writer.WriteLine($"<text x=\"{F(x)}\" y=\"{F(y)}\" text-anchor=\"{Escape(anchor)}\" font-family=\"{Escape(FontFamily)}\" font-size=\"{size}\" fill=\"{Escape(color)}\"{(bold ? " font-weight=\"bold\"" : String.Empty)}>{Escape(value)}</text>");
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
            
            if (m_ColorbarSettings == null)
                return;

            ColorbarSettings settings = m_ColorbarSettings;

            Double left = PlotLeft + PlotWidth + RightColorbarGap;
            Double top = PlotTop + 30.0d;
            Double height = Math.Min(RightColorbarHeight, Math.Max(1.0d, PlotHeight - 30.0d));
            Colorbar(settings.Title, left, top, RightColorbarWidth, height, settings.Minimum, settings.Maximum, settings.LowColor, settings.HighColor,
                settings.Intervals, settings.SaturatedLower, settings.SaturatedUpper, settings.Format, settings.Suffix, settings.MiddleColor, settings.Center);
        }

        public void Colorbar(String title, Double left, Double top, Double width, Double height, Double minimum, Double maximum,
            String lowColor, String highColor, Int32 intervals = 5, Boolean saturateLower = false, Boolean saturatedUpper = false, String format = "0.##", String suffix = "",
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
                
                if (saturateLower && (i == 0))
                    label = "≤ " + label;

                if (saturatedUpper && (i == intervals))
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
            if (m_Legend != null)
                BottomLegend(m_Legend, BottomLegendSlotWidth);
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
            if (m_Completed)
                return;
            if (m_PlotOpen)
                EndPlot();
            m_Writer.WriteLine("</svg>");
            m_Completed = true;
            m_Writer.Flush();
        }

        public void Dispose()
        {
            try
            {
                End();
            }
            finally
            {
                m_Writer.Dispose();
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
            if (m_LastPalette != null && m_LastPaletteResolution == PaletteResolution &&
                String.Equals(m_LastPaletteLow, lowColor, StringComparison.Ordinal) &&
                String.Equals(m_LastPaletteHigh, highColor, StringComparison.Ordinal))
                return m_LastPalette;
            String key = lowColor + "|" + highColor + "|" + PaletteResolution.ToString(CultureInfo.InvariantCulture);
            if (m_Palettes.TryGetValue(key, out String[] cached))
            {
                m_LastPaletteLow = lowColor;
                m_LastPaletteHigh = highColor;
                m_LastPaletteResolution = PaletteResolution;
                m_LastPalette = cached;
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
            m_Palettes.Add(key, palette);
            m_LastPaletteLow = lowColor;
            m_LastPaletteHigh = highColor;
            m_LastPaletteResolution = PaletteResolution;
            m_LastPalette = palette;
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
            if (m_Prepared)
                throw new InvalidOperationException("Layout cannot be reconfigured after PrepareLayout().");
        }

        private void EnsurePrepared()
        {
            if (!m_Prepared)
                PrepareLayout();
        }
        #endregion

        #region Records
        private sealed record ColorbarSettings(
            String Title,
            Double Minimum,
            Double Maximum,
            String LowColor,
            String MiddleColor,
            String HighColor,
            Int32 Intervals,
            Double? Center,
            Boolean SaturatedLower,
            Boolean SaturatedUpper,
            String Format,
            String Suffix
        );

        private readonly record struct AxisTick(
            Double Position,
            String Label
        );


        public readonly record struct LegendItem(
            String Label,
            String Color,
            Boolean IsLine,
            Boolean Dashed = false
        );
        #endregion
    }
}