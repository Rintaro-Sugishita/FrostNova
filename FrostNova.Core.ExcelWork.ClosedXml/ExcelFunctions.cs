using System;
using System.Text.RegularExpressions;

namespace FrostNova.Core.ExcelWork
{
    /// <summary>
    /// Address / column-name helpers. Column indexes are 1-based to match the
    /// EPPlus-based FrostNova.Core.ExcelWork API.
    /// </summary>
    public static partial class ExcelFunctions
    {
        // A1 cell reference, ignoring any '$' absolute markers, e.g. $A$1, AB12.
        private static readonly Regex CellRefPattern =
            new(@"^\$?(?<col>[A-Za-z]+)\$?(?<row>\d+)$", RegexOptions.Compiled);

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
            if (address.Contains(':')) return false;
            return CellRefPattern.IsMatch(address.Trim());
        }

        /// <summary>Parse an A1 address (single cell or range) into 1-based row/column bounds.</summary>
        internal static (int fromRow, int fromColumn, int toRow, int toColumn) ParseAddress(string address)
        {
            if (address.Contains(':'))
            {
                var parts = address.Split(':');
                var (fr, fc) = ParseCell(parts[0]);
                var (tr, tc) = ParseCell(parts[1]);
                return (fr, fc, tr, tc);
            }

            var (r, c) = ParseCell(address);
            return (r, c, r, c);
        }

        private static (int row, int column) ParseCell(string cell)
        {
            var m = CellRefPattern.Match(cell.Trim());
            if (!m.Success)
            {
                throw new ArgumentException($"'{cell}' is not a valid A1 cell reference.", nameof(cell));
            }

            int column = GetColumnIndex(m.Groups["col"].Value.ToUpperInvariant());
            int row = int.Parse(m.Groups["row"].Value);
            return (row, column);
        }
    }
}
