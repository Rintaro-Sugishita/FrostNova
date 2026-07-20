using System;
using NPOI.SS.Util;

namespace FrostNova.Core.ExcelWork
{
    /// <summary>
    /// Address / column-name helpers. Column indexes are 1-based to match the
    /// EPPlus-based FrostNova.Core.ExcelWork API.
    /// </summary>
    public static partial class ExcelFunctions
    {
        /// <summary>Convert an A1-style column name (e.g. "A", "AB") to a 1-based column index.</summary>
        public static int GetColumnIndex(string columnName)
        {
            if (string.IsNullOrEmpty(columnName)) return -1;

            int sum = 0;
            foreach (char c in columnName)
            {
                sum *= 26;
                sum += (c - 'A' + 1);
            }
            return sum;
        }

        /// <summary>Convert a 1-based column index to its A1-style column name.</summary>
        public static string GetColumnName(int columnIndex)
        {
            string columnName = string.Empty;
            while (columnIndex > 0)
            {
                int modulo = (columnIndex - 1) % 26;
                columnName = Convert.ToChar('A' + modulo) + columnName;
                columnIndex = (columnIndex - modulo) / 26;
            }
            return columnName;
        }

        /// <summary>True when <paramref name="address"/> denotes a single cell (e.g. "B3").</summary>
        public static bool IsValidSingleAddress(string address)
        {
            if (string.IsNullOrEmpty(address)) return false;
            try
            {
                if (address.Contains(':')) return false;
                var reference = new CellReference(address);
                return reference.Row >= 0 && reference.Col >= 0;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>Parse an A1 address (single cell or range) into 1-based row/column bounds.</summary>
        internal static (int fromRow, int fromColumn, int toRow, int toColumn) ParseAddress(string address)
        {
            if (address.Contains(':'))
            {
                var area = new AreaReference(address, NPOI.SS.SpreadsheetVersion.EXCEL2007);
                var first = area.FirstCell;
                var last = area.LastCell;
                return (first.Row + 1, first.Col + 1, last.Row + 1, last.Col + 1);
            }

            var cell = new CellReference(address);
            return (cell.Row + 1, cell.Col + 1, cell.Row + 1, cell.Col + 1);
        }
    }
}
