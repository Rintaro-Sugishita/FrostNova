using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using NpoiBorderStyle = NPOI.SS.UserModel.BorderStyle;
using NpoiHAlign = NPOI.SS.UserModel.HorizontalAlignment;
using NpoiVAlign = NPOI.SS.UserModel.VerticalAlignment;
using IIndexedColorMap = NPOI.OOXML.XSSF.UserModel.IIndexedColorMap;

namespace FrostNova.Core.ExcelWork
{
    internal static class ColorHelper
    {
        public static XSSFColor ToXssf(System.Drawing.Color color)
        {
            // NPOI 2.8.0 replaced XSSFColor(byte[]) with XSSFColor(byte[], IIndexedColorMap);
            // a null color map is fine for plain RGB colors.
            return new XSSFColor(new byte[] { color.R, color.G, color.B }, (IIndexedColorMap?)null);
        }

        public static int ToRgb(System.Drawing.Color color)
        {
            return (color.R << 16) | (color.G << 8) | color.B;
        }

        public static XSSFColor RgbToXssf(int rgb)
        {
            return new XSSFColor(new byte[] { (byte)((rgb >> 16) & 0xFF), (byte)((rgb >> 8) & 0xFF), (byte)(rgb & 0xFF) }, (IIndexedColorMap?)null);
        }
    }

    /// <summary>
    /// A value description of a cell style. NPOI shares <see cref="ICellStyle"/> objects across cells,
    /// so mutating a style in place would affect every cell that references it. This descriptor lets the
    /// <see cref="StyleCache"/> resolve each requested combination to a single shared, immutable style.
    /// Null fields mean "leave at NPOI default".
    /// </summary>
    internal sealed class CellStyleDescriptor
    {
        public string? FontName;
        public double? FontSize;
        public bool? Bold;
        public bool? Italic;
        public bool? Underline;
        public bool? Strike;
        public int? FontColorRgb;

        public int? FillRgb;

        public NpoiHAlign? HAlign;
        public NpoiVAlign? VAlign;

        public string? DataFormat;

        // Per side: index 0=Left, 1=Top, 2=Right, 3=Bottom
        public readonly NpoiBorderStyle?[] BorderStyles = new NpoiBorderStyle?[4];
        public readonly int?[] BorderColorRgb = new int?[4];

        public CellStyleDescriptor Clone()
        {
            var c = new CellStyleDescriptor
            {
                FontName = FontName,
                FontSize = FontSize,
                Bold = Bold,
                Italic = Italic,
                Underline = Underline,
                Strike = Strike,
                FontColorRgb = FontColorRgb,
                FillRgb = FillRgb,
                HAlign = HAlign,
                VAlign = VAlign,
                DataFormat = DataFormat,
            };
            Array.Copy(BorderStyles, c.BorderStyles, 4);
            Array.Copy(BorderColorRgb, c.BorderColorRgb, 4);
            return c;
        }

        public bool HasFontSettings =>
            FontName != null || FontSize != null || Bold != null || Italic != null ||
            Underline != null || Strike != null || FontColorRgb != null;

        public string Signature()
        {
            var sb = new StringBuilder();
            sb.Append(FontName).Append('|')
              .Append(FontSize?.ToString(CultureInfo.InvariantCulture)).Append('|')
              .Append(Bold).Append('|').Append(Italic).Append('|')
              .Append(Underline).Append('|').Append(Strike).Append('|')
              .Append(FontColorRgb).Append('|')
              .Append(FillRgb).Append('|')
              .Append((int?)HAlign).Append('|').Append((int?)VAlign).Append('|')
              .Append(DataFormat).Append('|');
            for (int i = 0; i < 4; i++)
            {
                sb.Append((int?)BorderStyles[i]).Append(',').Append(BorderColorRgb[i]).Append(';');
            }
            return sb.ToString();
        }

        public string FontSignature()
        {
            return new StringBuilder()
                .Append(FontName).Append('|')
                .Append(FontSize?.ToString(CultureInfo.InvariantCulture)).Append('|')
                .Append(Bold).Append('|').Append(Italic).Append('|')
                .Append(Underline).Append('|').Append(Strike).Append('|')
                .Append(FontColorRgb)
                .ToString();
        }
    }

    /// <summary>
    /// Caches resolved <see cref="ICellStyle"/> / <see cref="IFont"/> objects per workbook, keyed by their
    /// value signature, so repeated styling never exceeds Excel's style/font limits and never mutates a
    /// style shared by other cells. Also remembers the descriptor behind every style it created, so that
    /// successive setter calls on the same cell accumulate rather than reset.
    /// </summary>
    internal sealed class StyleCache
    {
        private readonly IWorkbook _workbook;
        private readonly Dictionary<string, ICellStyle> _styles = new();
        private readonly Dictionary<string, IFont> _fonts = new();
        private readonly Dictionary<ICellStyle, CellStyleDescriptor> _descriptors = new();

        public StyleCache(IWorkbook workbook)
        {
            _workbook = workbook;
        }

        /// <summary>Return the descriptor that produced the cell's current style, or an empty one.</summary>
        public CellStyleDescriptor DescriptorOf(ICell cell)
        {
            var style = cell.CellStyle;
            if (style != null && _descriptors.TryGetValue(style, out var known))
            {
                return known.Clone();
            }
            return ReadBack(style);
        }

        /// <summary>Resolve a descriptor to a shared style and assign it to the cell.</summary>
        public void Apply(ICell cell, CellStyleDescriptor descriptor)
        {
            cell.CellStyle = Resolve(descriptor);
        }

        public ICellStyle Resolve(CellStyleDescriptor descriptor)
        {
            var sig = descriptor.Signature();
            if (_styles.TryGetValue(sig, out var cached)) return cached;

            var style = _workbook.CreateCellStyle();

            if (descriptor.HasFontSettings)
            {
                style.SetFont(ResolveFont(descriptor));
            }

            if (descriptor.FillRgb is int fill)
            {
                style.FillPattern = FillPattern.SolidForeground;
                ((XSSFCellStyle)style).SetFillForegroundColor(ColorHelper.RgbToXssf(fill));
            }

            if (descriptor.HAlign is NpoiHAlign h) style.Alignment = h;
            if (descriptor.VAlign is NpoiVAlign v) style.VerticalAlignment = v;

            if (descriptor.DataFormat is string fmt)
            {
                style.DataFormat = _workbook.CreateDataFormat().GetFormat(fmt);
            }

            for (int i = 0; i < 4; i++)
            {
                if (descriptor.BorderStyles[i] is NpoiBorderStyle bs)
                {
                    SetBorderStyle(style, i, bs);
                }
                if (descriptor.BorderColorRgb[i] is int rgb)
                {
                    SetBorderColor((XSSFCellStyle)style, i, ColorHelper.RgbToXssf(rgb));
                }
            }

            _styles[sig] = style;
            _descriptors[style] = descriptor.Clone();
            return style;
        }

        private IFont ResolveFont(CellStyleDescriptor descriptor)
        {
            var sig = descriptor.FontSignature();
            if (_fonts.TryGetValue(sig, out var cached)) return cached;

            var font = _workbook.CreateFont();
            if (descriptor.FontName is string name) font.FontName = name;
            if (descriptor.FontSize is double size) font.FontHeightInPoints = size;
            if (descriptor.Bold is bool bold) font.IsBold = bold;
            if (descriptor.Italic is bool italic) font.IsItalic = italic;
            if (descriptor.Underline is bool underline)
                font.Underline = underline ? FontUnderlineType.Single : FontUnderlineType.None;
            if (descriptor.Strike is bool strike) font.IsStrikeout = strike;
            if (descriptor.FontColorRgb is int color) ((XSSFFont)font).SetColor(ColorHelper.RgbToXssf(color));

            _fonts[sig] = font;
            return font;
        }

        private static void SetBorderStyle(ICellStyle style, int side, NpoiBorderStyle bs)
        {
            switch (side)
            {
                case 0: style.BorderLeft = bs; break;
                case 1: style.BorderTop = bs; break;
                case 2: style.BorderRight = bs; break;
                case 3: style.BorderBottom = bs; break;
            }
        }

        private static void SetBorderColor(XSSFCellStyle style, int side, XSSFColor color)
        {
            switch (side)
            {
                case 0: style.SetLeftBorderColor(color); break;
                case 1: style.SetTopBorderColor(color); break;
                case 2: style.SetRightBorderColor(color); break;
                case 3: style.SetBottomBorderColor(color); break;
            }
        }

        /// <summary>Best-effort reconstruction of a descriptor from an existing (e.g. template-loaded) style.</summary>
        private CellStyleDescriptor ReadBack(ICellStyle? style)
        {
            var d = new CellStyleDescriptor();
            if (style is not XSSFCellStyle xs) return d;

            d.HAlign = xs.Alignment;
            d.VAlign = xs.VerticalAlignment;

            var fmt = xs.GetDataFormatString();
            if (!string.IsNullOrEmpty(fmt) && fmt != "General") d.DataFormat = fmt;

            var font = xs.GetFont();
            if (font != null)
            {
                d.FontName = font.FontName;
                d.FontSize = font.FontHeightInPoints;
                d.Bold = font.IsBold;
                d.Italic = font.IsItalic;
                d.Underline = font.Underline != FontUnderlineType.None;
                d.Strike = font.IsStrikeout;
            }

            d.BorderStyles[0] = xs.BorderLeft;
            d.BorderStyles[1] = xs.BorderTop;
            d.BorderStyles[2] = xs.BorderRight;
            d.BorderStyles[3] = xs.BorderBottom;
            return d;
        }
    }

    internal static class EnumMapping
    {
        public static NpoiBorderStyle ToNpoi(this BorderStyle style)
        {
            return style switch
            {
                BorderStyle.None => NpoiBorderStyle.None,
                BorderStyle.Hair => NpoiBorderStyle.Hair,
                BorderStyle.Dotted => NpoiBorderStyle.Dotted,
                BorderStyle.DashDot => NpoiBorderStyle.DashDot,
                BorderStyle.Thin => NpoiBorderStyle.Thin,
                BorderStyle.DashDotDot => NpoiBorderStyle.DashDotDot,
                BorderStyle.Dashed => NpoiBorderStyle.Dashed,
                BorderStyle.MediumDashDotDot => NpoiBorderStyle.MediumDashDotDot,
                BorderStyle.MediumDashed => NpoiBorderStyle.MediumDashed,
                BorderStyle.MediumDashDot => NpoiBorderStyle.MediumDashDot,
                BorderStyle.Thick => NpoiBorderStyle.Thick,
                BorderStyle.Medium => NpoiBorderStyle.Medium,
                BorderStyle.Double => NpoiBorderStyle.Double,
                _ => NpoiBorderStyle.None,
            };
        }

        public static NpoiHAlign ToNpoi(this HorizontalAlignment align)
        {
            return align switch
            {
                HorizontalAlignment.General => NpoiHAlign.General,
                HorizontalAlignment.Left => NpoiHAlign.Left,
                HorizontalAlignment.Center => NpoiHAlign.Center,
                HorizontalAlignment.CenterContinuous => NpoiHAlign.CenterSelection,
                HorizontalAlignment.Right => NpoiHAlign.Right,
                HorizontalAlignment.Fill => NpoiHAlign.Fill,
                HorizontalAlignment.Distributed => NpoiHAlign.Distributed,
                HorizontalAlignment.Justify => NpoiHAlign.Justify,
                _ => NpoiHAlign.General,
            };
        }

        public static NpoiVAlign ToNpoi(this VerticalAlignment align)
        {
            return align switch
            {
                VerticalAlignment.Top => NpoiVAlign.Top,
                VerticalAlignment.Center => NpoiVAlign.Center,
                VerticalAlignment.Bottom => NpoiVAlign.Bottom,
                VerticalAlignment.Distributed => NpoiVAlign.Distributed,
                VerticalAlignment.Justify => NpoiVAlign.Justify,
                _ => NpoiVAlign.Top,
            };
        }
    }
}
