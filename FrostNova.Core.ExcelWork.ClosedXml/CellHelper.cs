using System;
using ClosedXML.Excel;

namespace FrostNova.Core.ExcelWork
{
    /// <summary>
    /// Conversion helpers between .NET values and ClosedXML cell contents.
    /// Rows/columns in the public API are 1-based, which also matches ClosedXML's own
    /// 1-based addressing, so no boundary offset is applied here.
    /// </summary>
    internal static class CellHelper
    {
        /// <summary>Read a cell's effective value as a .NET object (null when blank).</summary>
        public static object? GetValue(IXLCell? cell)
        {
            if (cell == null) return null;

            // Formula cells evaluate lazily; ClosedXML exposes the cached/evaluated value via CachedValue.
            var value = cell.HasFormula ? cell.CachedValue : cell.Value;

            return value.Type switch
            {
                XLDataType.Text => value.GetText(),
                XLDataType.Number => value.GetNumber(),
                XLDataType.Boolean => value.GetBoolean(),
                XLDataType.DateTime => value.GetDateTime(),
                XLDataType.TimeSpan => value.GetTimeSpan(),
                _ => null, // Blank / Error
            };
        }

        /// <summary>Write a .NET value into a cell, picking the closest native Excel type.</summary>
        public static void SetValue(IXLCell cell, object? value)
        {
            switch (value)
            {
                case null:
                    cell.Clear(XLClearOptions.Contents);
                    break;
                case string s:
                    // ClosedXML applies Excel's quote-prefix convention and strips a single leading
                    // apostrophe from a string cell value. Prepend one so the stored (and round-tripped)
                    // text matches the input verbatim — important for SQL defaults like 'active' or ''.
                    cell.Value = s.StartsWith("'", StringComparison.Ordinal) ? "'" + s : s;
                    break;
                case bool b:
                    cell.Value = b;
                    break;
                case DateTime dt:
                    cell.Value = dt;
                    break;
                case DateTimeOffset dto:
                    cell.Value = dto.DateTime;
                    break;
                case TimeSpan ts:
                    cell.Value = ts;
                    break;
                case sbyte or byte or short or ushort or int or uint or long or ulong or float or double or decimal:
                    cell.Value = Convert.ToDouble(value);
                    break;
                default:
                    cell.Value = value.ToString();
                    break;
            }
        }

        /// <summary>Get the row at a 1-based index (ClosedXML creates lazily as needed).</summary>
        public static IXLRow GetOrCreateRow(IXLWorksheet sheet, int row)
        {
            return sheet.Row(row);
        }

        /// <summary>Get the cell at 1-based row/column (ClosedXML creates lazily as needed).</summary>
        public static IXLCell GetOrCreateCell(IXLWorksheet sheet, int row, int column)
        {
            return sheet.Cell(row, column);
        }

        /// <summary>Read the cell at 1-based row/column. Never null in ClosedXML; blank cells read as null via GetValue.</summary>
        public static IXLCell? GetCellOrNull(IXLWorksheet sheet, int row, int column)
        {
            return sheet.Cell(row, column);
        }

        public static T? ConvertTo<T>(object? value)
        {
            if (value == null) return default;
            if (value is T typed) return typed;

            var target = typeof(T);
            var underlying = Nullable.GetUnderlyingType(target) ?? target;
            try
            {
                return (T)Convert.ChangeType(value, underlying);
            }
            catch
            {
                return default;
            }
        }
    }
}
