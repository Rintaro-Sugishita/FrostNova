using System;
using ClosedXML.Excel;

namespace FrostNova.Core.ExcelWork
{
    /// <summary>
    /// A single worksheet. Row/column indexes in this API are 1-based, matching both the EPPlus-based
    /// FrostNova.Core.ExcelWork and ClosedXML's own 1-based addressing.
    /// The ClosedXML backend is cross-platform and does not support images, shapes or charts.
    /// </summary>
    public class Sheet
    {
        private readonly IXLWorksheet _sheet;
        private readonly StyleCache _styles;

        internal Sheet(IXLWorksheet sheet, StyleCache styles)
        {
            _sheet = sheet;
            _styles = styles;
        }

        internal IXLWorksheet GetSheetObject() => _sheet;

        #region sheet

        public string Name
        {
            get => _sheet.Name;
            set
            {
                try
                {
                    _sheet.Name = value;
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"Sheet {_sheet.Name} was renamed to {value}, resulting in a duplicate name", ex);
                }
            }
        }

        #endregion

        #region range

        public Range GetRange(int row, int column)
            => new Range(_sheet, row, column, row, column, _styles);

        public Range GetRange(int fromRow, int fromColumn, int toRow, int toColumn)
            => new Range(_sheet, fromRow, fromColumn, toRow, toColumn, _styles);

        public Range GetRangeFromCount(int fromRow, int fromColumn, int rowCount, int columnCount)
            => new Range(_sheet, fromRow, fromColumn, fromRow + rowCount - 1, fromColumn + columnCount - 1, _styles);

        public Range GetRange(string address)
        {
            var (fromRow, fromColumn, toRow, toColumn) = ExcelFunctions.ParseAddress(address);
            return new Range(_sheet, fromRow, fromColumn, toRow, toColumn, _styles);
        }

        public Range GetUsedRange(bool needA1Cell = true)
        {
            var used = _sheet.RangeUsed();
            if (used == null)
            {
                return new Range(_sheet, 1, 1, 1, 1, _styles);
            }

            int firstRow = used.RangeAddress.FirstAddress.RowNumber;
            int firstCol = used.RangeAddress.FirstAddress.ColumnNumber;
            int lastRow = used.RangeAddress.LastAddress.RowNumber;
            int lastCol = used.RangeAddress.LastAddress.ColumnNumber;

            return needA1Cell
                ? new Range(_sheet, 1, 1, lastRow, lastCol, _styles)
                : new Range(_sheet, firstRow, firstCol, lastRow, lastCol, _styles);
        }

        public object? GetValue(int row, int column)
            => CellHelper.GetValue(CellHelper.GetCellOrNull(_sheet, row, column));

        public object? GetValue(int row, string columnText)
            => GetValue(row, ExcelFunctions.GetColumnIndex(columnText));

        public object? GetValue(string singleCellsAddress)
        {
            var (r, c, _, _) = ExcelFunctions.ParseAddress(singleCellsAddress);
            return GetValue(r, c);
        }

        public T? GetValue<T>(int row, int column)
            => CellHelper.ConvertTo<T>(GetValue(row, column));

        public T? GetValue<T>(int row, string columnText)
            => GetValue<T>(row, ExcelFunctions.GetColumnIndex(columnText));

        public T? GetValue<T>(string singleCellsAddress)
        {
            var (r, c, _, _) = ExcelFunctions.ParseAddress(singleCellsAddress);
            return GetValue<T>(r, c);
        }

        public void SetValue(int row, int column, object? value)
            => CellHelper.SetValue(CellHelper.GetOrCreateCell(_sheet, row, column), value);

        public void SetValue(string address, object? value)
        {
            var (r, c, _, _) = ExcelFunctions.ParseAddress(address);
            SetValue(r, c, value);
        }

        #endregion

        #region width and height

        // ClosedXML column width is already in character units, matching the public API.
        public double GetColumnWidth(int column) => _sheet.Column(column).Width;

        public void SetColumnWidth(int column, double width) => _sheet.Column(column).Width = width;

        public void AutoFit() => _sheet.Columns().AdjustToContents();

        public void AutoFit(int column) => _sheet.Column(column).AdjustToContents();

        public void AutoFit(int fromColumn, int toColumn) => _sheet.Columns(fromColumn, toColumn).AdjustToContents();

        public double GetRowHeight(int row) => _sheet.Row(row).Height;

        public void SetRowHeight(int row, double height) => _sheet.Row(row).Height = height;

        #endregion

        #region row and column

        public void InsertRow(int row, int count = 1)
        {
            _sheet.Row(row).InsertRowsAbove(count);
        }

        public void InsertColumn(int column, int count = 1)
        {
            _sheet.Column(column).InsertColumnsBefore(count);
        }

        public void InsertRange(int fromRow, int fromColumn, int toRow, int toColumn, InsertShiftType shiftType)
        {
            using var range = GetRange(fromRow, fromColumn, toRow, toColumn);
            range.Insert(shiftType);
        }

        public void CopyRow(int srcRow, int count, int destRow)
        {
            int lastCol = LastUsedColumn();
            using var src = GetRange(srcRow, 1, srcRow + count - 1, lastCol);
            using var dest = GetRange(destRow, 1, destRow + count - 1, lastCol);
            src.CopyToRange(dest);
        }

        public void CopyColumn(int srcColumn, int count, int destColumn)
        {
            int lastRow = LastUsedRow();
            using var src = GetRange(1, srcColumn, lastRow, srcColumn + count - 1);
            using var dest = GetRange(1, destColumn, lastRow, destColumn + count - 1);
            src.CopyToRange(dest);
        }

        public void DeleteRow(int row, int count = 1)
        {
            _sheet.Rows(row, row + count - 1).Delete();
        }

        public void DeleteColumn(int column, int count = 1)
        {
            _sheet.Columns(column, column + count - 1).Delete();
        }

        public void DeleteRange(int fromRow, int fromColumn, int toRow, int toColumn, DeleteShiftType shiftType)
        {
            using var range = GetRange(fromRow, fromColumn, toRow, toColumn);
            range.Delete(shiftType);
        }

        private int LastUsedColumn() => _sheet.LastColumnUsed()?.ColumnNumber() ?? 1;

        private int LastUsedRow() => _sheet.LastRowUsed()?.RowNumber() ?? 1;

        #endregion

        #region select

        public void Select(string address)
        {
            var (r, c, _, _) = ExcelFunctions.ParseAddress(address);
            Select(r, c);
        }

        public void Select()
        {
            _sheet.SetTabSelected(true);
        }

        public void Select(int row, int column)
        {
            _sheet.Cell(row, column).SetActive();
            _sheet.SetTabSelected(true);
        }

        public void Select(int row, string column)
            => Select(row, ExcelFunctions.GetColumnIndex(column));

        public void Select(int fromRow, int fromColumn, int toRow, int toColumn)
        {
            _sheet.Cell(fromRow, fromColumn).SetActive();
            _sheet.SetTabSelected(true);
        }

        public void Select(int fromRow, string fromColumn, int toRow, string toColumn)
            => Select(fromRow, ExcelFunctions.GetColumnIndex(fromColumn), toRow, ExcelFunctions.GetColumnIndex(toColumn));

        public void SetSelect() => _sheet.SetTabSelected(true);

        public bool IsTabSelected => _sheet.TabSelected;

        #endregion
    }
}
