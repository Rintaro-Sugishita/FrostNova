using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using ClosedXML.Excel;

namespace FrostNova.Core.ExcelWork
{
    /// <summary>
    /// A rectangular cell range (1-based, inclusive bounds). Mirrors the public surface of the
    /// EPPlus/NPOI-based FrostNova.Core.ExcelWork.Range on top of ClosedXML.
    /// </summary>
    public class Range : IDisposable
    {
        private readonly IXLWorksheet _sheet;
        private readonly StyleCache _styles;
        private readonly int _fromRow, _fromColumn, _toRow, _toColumn;

        internal Range(IXLWorksheet sheet, int fromRow, int fromColumn, int toRow, int toColumn, StyleCache styles)
        {
            _sheet = sheet;
            _styles = styles;
            _fromRow = Math.Min(fromRow, toRow);
            _fromColumn = Math.Min(fromColumn, toColumn);
            _toRow = Math.Max(fromRow, toRow);
            _toColumn = Math.Max(fromColumn, toColumn);
        }

        public void Dispose() { /* ClosedXML ranges are lightweight; nothing to release. */ }

        private IXLRange XlRange() => _sheet.Range(_fromRow, _fromColumn, _toRow, _toColumn);

        #region basic properties

        public object? Value
        {
            get
            {
                if (IsSingleCell)
                {
                    return CellHelper.GetValue(CellHelper.GetCellOrNull(_sheet, _fromRow, _fromColumn));
                }

                var values = new object?[RowCount, ColumnCount];
                for (int i = 0; i < RowCount; i++)
                {
                    for (int j = 0; j < ColumnCount; j++)
                    {
                        values[i, j] = CellHelper.GetValue(CellHelper.GetCellOrNull(_sheet, _fromRow + i, _fromColumn + j));
                    }
                }
                return values;
            }
            set
            {
                if (value is object?[,] matrix)
                {
                    int rows = Math.Min(RowCount, matrix.GetLength(0));
                    int cols = Math.Min(ColumnCount, matrix.GetLength(1));
                    for (int i = 0; i < rows; i++)
                    {
                        for (int j = 0; j < cols; j++)
                        {
                            CellHelper.SetValue(CellHelper.GetOrCreateCell(_sheet, _fromRow + i, _fromColumn + j), matrix[i, j]);
                        }
                    }
                }
                else
                {
                    foreach (var cell in Cells()) CellHelper.SetValue(cell, value);
                }
            }
        }

        public int Row => _fromRow;
        public int Column => _fromColumn;
        public int ToRow => _toRow;
        public int ToColumn => _toColumn;

        public string Address
        {
            get
            {
                string start = ExcelFunctions.GetColumnName(_fromColumn) + _fromRow;
                if (IsSingleCell) return start;
                return start + ":" + ExcelFunctions.GetColumnName(_toColumn) + _toRow;
            }
        }

        public int RowCount => _toRow - _fromRow + 1;
        public int ColumnCount => _toColumn - _fromColumn + 1;
        public bool IsSingleCell => RowCount == 1 && ColumnCount == 1;

        public double Height
        {
            get => _sheet.Row(_fromRow).Height;
            set { for (int r = _fromRow; r <= _toRow; r++) _sheet.Row(r).Height = value; }
        }

        public double Width
        {
            get => _sheet.Column(_fromColumn).Width;
            set { for (int c = _fromColumn; c <= _toColumn; c++) _sheet.Column(c).Width = value; }
        }

        #endregion

        public T? GetValue<T>() => CellHelper.ConvertTo<T>(Value);

        public void SetFormula(string formula)
        {
            var f = formula.StartsWith("=") ? formula.Substring(1) : formula;
            foreach (var cell in Cells()) cell.FormulaA1 = f;
        }

        public void SetFormulaR1C1(string formula)
        {
            var f = formula.StartsWith("=") ? formula.Substring(1) : formula;
            foreach (var cell in Cells()) cell.FormulaR1C1 = f;
        }

        public void Insert(InsertShiftType shiftType)
        {
            switch (shiftType)
            {
                case InsertShiftType.Down:
                    XlRange().InsertRowsAbove(RowCount);
                    break;
                case InsertShiftType.EntireRow:
                    _sheet.Row(_fromRow).InsertRowsAbove(RowCount);
                    break;
                case InsertShiftType.Right:
                    XlRange().InsertColumnsBefore(ColumnCount);
                    break;
                case InsertShiftType.EntireColumn:
                    _sheet.Column(_fromColumn).InsertColumnsBefore(ColumnCount);
                    break;
            }
        }

        public void Delete(DeleteShiftType shiftType)
        {
            switch (shiftType)
            {
                case DeleteShiftType.Top:
                    XlRange().Delete(XLShiftDeletedCells.ShiftCellsUp);
                    break;
                case DeleteShiftType.EntireRow:
                    _sheet.Rows(_fromRow, _toRow).Delete();
                    break;
                case DeleteShiftType.Left:
                    XlRange().Delete(XLShiftDeletedCells.ShiftCellsLeft);
                    break;
                case DeleteShiftType.EntireColumn:
                    _sheet.Columns(_fromColumn, _toColumn).Delete();
                    break;
            }
        }

        public void CopyToRange(Range destRange)
        {
            var source = XlRange();
            var target = destRange._sheet.Cell(destRange._fromRow, destRange._fromColumn);
            source.CopyTo(target);
        }

        public RangeValue GetRangeValue() => new RangeValue(Value, Column, Row, this);

        #region style

        public void SetBackground(Color color) => XlRange().Style.Fill.BackgroundColor = ColorHelper.ToXl(color);

        public void SetForeground(Color color) => XlRange().Style.Font.FontColor = ColorHelper.ToXl(color);

        public void SetFontFamily(string fontFamily) => XlRange().Style.Font.FontName = fontFamily;

        public void SetFontSize(float size) => XlRange().Style.Font.FontSize = size;

        public void DrawBorder(Color color, BorderPosition position = BorderPosition.All, BorderStyle style = BorderStyle.Thin)
        {
            var xlStyle = XlRange().Style;
            var xlBorder = style.ToClosedXml();
            var xlColor = ColorHelper.ToXl(color);

            if (position.HasFlag(BorderPosition.Left)) { xlStyle.Border.LeftBorder = xlBorder; xlStyle.Border.LeftBorderColor = xlColor; }
            if (position.HasFlag(BorderPosition.Top)) { xlStyle.Border.TopBorder = xlBorder; xlStyle.Border.TopBorderColor = xlColor; }
            if (position.HasFlag(BorderPosition.Right)) { xlStyle.Border.RightBorder = xlBorder; xlStyle.Border.RightBorderColor = xlColor; }
            if (position.HasFlag(BorderPosition.Bottom)) { xlStyle.Border.BottomBorder = xlBorder; xlStyle.Border.BottomBorderColor = xlColor; }
        }

        public void ClearBorder(BorderPosition position = BorderPosition.All)
        {
            var xlStyle = XlRange().Style;
            if (position.HasFlag(BorderPosition.Left)) xlStyle.Border.LeftBorder = XLBorderStyleValues.None;
            if (position.HasFlag(BorderPosition.Top)) xlStyle.Border.TopBorder = XLBorderStyleValues.None;
            if (position.HasFlag(BorderPosition.Right)) xlStyle.Border.RightBorder = XLBorderStyleValues.None;
            if (position.HasFlag(BorderPosition.Bottom)) xlStyle.Border.BottomBorder = XLBorderStyleValues.None;
        }

        public void SetHorizontalContentAlignment(HorizontalAlignment horizontalAlignment)
            => XlRange().Style.Alignment.Horizontal = horizontalAlignment.ToClosedXml();

        public void SetVerticalContentAlignment(VerticalAlignment verticalAlignment)
            => XlRange().Style.Alignment.Vertical = verticalAlignment.ToClosedXml();

        public void SetFormat(string format) => XlRange().Style.NumberFormat.Format = format;

        #endregion

        #region focus

        public void Select()
        {
            _sheet.Cell(_fromRow, _fromColumn).SetActive();
            _sheet.SetTabSelected(true);
        }

        #endregion

        public void SetTable(DataTable dt, bool isCreateColumn = true, int maxRow = ExcelOperator.MAX_ROW_COUNT)
        {
            var values = Value as object?[,];

            if (isCreateColumn)
            {
                dt.Clear();
                dt.Columns.Clear();
            }

            if (values == null) return;

            int columnLength = ColumnCount;
            int rowLength = RowCount;

            if (isCreateColumn)
            {
                for (int i = 0; i < columnLength; i++)
                {
                    var name = ExcelFunctions.GetColumnName(i + 1);
                    dt.Columns.Add(new DataColumn { Caption = name, ColumnName = name, DataType = typeof(object) });
                }
            }

            int loopColumnCount = Math.Min(columnLength, dt.Columns.Count);
            int loopRowCount = Math.Min(rowLength, maxRow);

            for (int i = 0; i < loopRowCount; i++)
            {
                var newRow = dt.NewRow();
                for (int j = 0; j < loopColumnCount; j++)
                {
                    newRow[j] = values[i, j] ?? (object)DBNull.Value;
                }
                dt.Rows.Add(newRow);
            }
        }

        public void AddListValidation(IEnumerable<string> items, bool allowBlank = true, string errorMessage = "", string errorTitle = "")
        {
            var validation = XlRange().CreateDataValidation();
            validation.List("\"" + string.Join(",", items) + "\"", false);
            validation.IgnoreBlanks = allowBlank;
            validation.ErrorStyle = XLErrorStyle.Warning;

            if (!string.IsNullOrEmpty(errorMessage))
            {
                validation.ShowErrorMessage = true;
                validation.ErrorTitle = errorTitle;
                validation.ErrorMessage = errorMessage;
            }
        }

        public void AddListValidation(Range referenceRange)
        {
            var validation = XlRange().CreateDataValidation();
            validation.List("=" + referenceRange.Address, false);
        }

        private IEnumerable<IXLCell> Cells()
        {
            for (int r = _fromRow; r <= _toRow; r++)
            {
                for (int c = _fromColumn; c <= _toColumn; c++)
                {
                    yield return CellHelper.GetOrCreateCell(_sheet, r, c);
                }
            }
        }
    }
}
