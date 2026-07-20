using System;
using NPOI.SS.UserModel;

namespace FrostNova.Core.ExcelWork
{
    /// <summary>
    /// Conversion helpers between .NET values and NPOI <see cref="ICell"/> contents.
    /// Rows/columns in the public API are 1-based (matching the EPPlus-based ExcelWork);
    /// NPOI is 0-based, so callers convert at the boundary.
    /// </summary>
    internal static class CellHelper
    {
        /// <summary>Read a cell's effective value as a .NET object (null when blank).</summary>
        public static object? GetValue(ICell? cell)
        {
            if (cell == null) return null;

            switch (cell.CellType)
            {
                case CellType.String:
                    return cell.StringCellValue;
                case CellType.Numeric:
                    return DateUtil.IsCellDateFormatted(cell) ? cell.DateCellValue : cell.NumericCellValue;
                case CellType.Boolean:
                    return cell.BooleanCellValue;
                case CellType.Formula:
                    return GetFormulaValue(cell);
                case CellType.Blank:
                case CellType.Unknown:
                case CellType.Error:
                default:
                    return null;
            }
        }

        private static object? GetFormulaValue(ICell cell)
        {
            switch (cell.CachedFormulaResultType)
            {
                case CellType.String:
                    return cell.StringCellValue;
                case CellType.Numeric:
                    return DateUtil.IsCellDateFormatted(cell) ? cell.DateCellValue : cell.NumericCellValue;
                case CellType.Boolean:
                    return cell.BooleanCellValue;
                default:
                    return null;
            }
        }

        /// <summary>Write a .NET value into a cell, picking the closest native Excel type.</summary>
        public static void SetValue(ICell cell, object? value)
        {
            switch (value)
            {
                case null:
                    cell.SetBlank();
                    break;
                case string s:
                    cell.SetCellValue(s);
                    break;
                case bool b:
                    cell.SetCellValue(b);
                    break;
                case DateTime dt:
                    cell.SetCellValue(dt);
                    break;
                case DateTimeOffset dto:
                    cell.SetCellValue(dto.DateTime);
                    break;
                case sbyte or byte or short or ushort or int or uint or long or ulong or float or double or decimal:
                    cell.SetCellValue(Convert.ToDouble(value));
                    break;
                default:
                    cell.SetCellValue(value.ToString());
                    break;
            }
        }

        /// <summary>Get-or-create the row at a 0-based index.</summary>
        public static IRow GetOrCreateRow(ISheet sheet, int rowIndex0)
        {
            return sheet.GetRow(rowIndex0) ?? sheet.CreateRow(rowIndex0);
        }

        /// <summary>Get-or-create the cell at 0-based row/column.</summary>
        public static ICell GetOrCreateCell(ISheet sheet, int rowIndex0, int colIndex0)
        {
            var row = GetOrCreateRow(sheet, rowIndex0);
            return row.GetCell(colIndex0) ?? row.CreateCell(colIndex0);
        }

        /// <summary>Read the cell at 0-based row/column without creating it.</summary>
        public static ICell? GetCellOrNull(ISheet sheet, int rowIndex0, int colIndex0)
        {
            return sheet.GetRow(rowIndex0)?.GetCell(colIndex0);
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
