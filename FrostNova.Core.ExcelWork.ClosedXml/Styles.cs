using ClosedXML.Excel;
using XlBorder = ClosedXML.Excel.XLBorderStyleValues;
using XlHAlign = ClosedXML.Excel.XLAlignmentHorizontalValues;
using XlVAlign = ClosedXML.Excel.XLAlignmentVerticalValues;

namespace FrostNova.Core.ExcelWork
{
    internal static class ColorHelper
    {
        /// <summary>Convert a <see cref="System.Drawing.Color"/> to a ClosedXML color.</summary>
        public static XLColor ToXl(System.Drawing.Color color) => XLColor.FromColor(color);
    }

    /// <summary>
    /// Placeholder for the NPOI backend's style de-duplication cache. ClosedXML applies styles
    /// directly to each cell (styles are copy-on-write per cell), so no shared-style bookkeeping is
    /// needed here. Kept so the internal wiring (ExcelOperator → Sheet → Range) mirrors the NPOI backend.
    /// </summary>
    internal sealed class StyleCache
    {
        public StyleCache(IXLWorkbook workbook)
        {
            Workbook = workbook;
        }

        public IXLWorkbook Workbook { get; }
    }

    internal static class EnumMapping
    {
        public static XlBorder ToClosedXml(this BorderStyle style)
        {
            return style switch
            {
                BorderStyle.None => XlBorder.None,
                BorderStyle.Hair => XlBorder.Hair,
                BorderStyle.Dotted => XlBorder.Dotted,
                BorderStyle.DashDot => XlBorder.DashDot,
                BorderStyle.Thin => XlBorder.Thin,
                BorderStyle.DashDotDot => XlBorder.DashDotDot,
                BorderStyle.Dashed => XlBorder.Dashed,
                BorderStyle.MediumDashDotDot => XlBorder.MediumDashDotDot,
                BorderStyle.MediumDashed => XlBorder.MediumDashed,
                BorderStyle.MediumDashDot => XlBorder.MediumDashDot,
                BorderStyle.Thick => XlBorder.Thick,
                BorderStyle.Medium => XlBorder.Medium,
                BorderStyle.Double => XlBorder.Double,
                _ => XlBorder.None,
            };
        }

        public static XlHAlign ToClosedXml(this HorizontalAlignment align)
        {
            return align switch
            {
                HorizontalAlignment.General => XlHAlign.General,
                HorizontalAlignment.Left => XlHAlign.Left,
                HorizontalAlignment.Center => XlHAlign.Center,
                HorizontalAlignment.CenterContinuous => XlHAlign.CenterContinuous,
                HorizontalAlignment.Right => XlHAlign.Right,
                HorizontalAlignment.Fill => XlHAlign.Fill,
                HorizontalAlignment.Distributed => XlHAlign.Distributed,
                HorizontalAlignment.Justify => XlHAlign.Justify,
                _ => XlHAlign.General,
            };
        }

        public static XlVAlign ToClosedXml(this VerticalAlignment align)
        {
            return align switch
            {
                VerticalAlignment.Top => XlVAlign.Top,
                VerticalAlignment.Center => XlVAlign.Center,
                VerticalAlignment.Bottom => XlVAlign.Bottom,
                VerticalAlignment.Distributed => XlVAlign.Distributed,
                VerticalAlignment.Justify => XlVAlign.Justify,
                _ => XlVAlign.Top,
            };
        }
    }
}
