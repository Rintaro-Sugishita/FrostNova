using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using NPOI.SS.UserModel;
using NPOI.SS.Util;

namespace FrostNova.Core.ExcelWork
{
    /// <summary>
    /// A rectangular cell range (1-based, inclusive bounds). Mirrors the public surface of the
    /// EPPlus-based FrostNova.Core.ExcelWork.Range on top of NPOI.
    /// </summary>
    public class Range : IDisposable
    {
        private readonly ISheet _sheet;
        private readonly StyleCache _styles;
        private readonly int _fromRow, _fromColumn, _toRow, _toColumn;

        internal Range(ISheet sheet, int fromRow, int fromColumn, int toRow, int toColumn, StyleCache styles)
        {
            _sheet = sheet;
            _styles = styles;
            _fromRow = Math.Min(fromRow, toRow);
            _fromColumn = Math.Min(fromColumn, toColumn);
            _toRow = Math.Max(fromRow, toRow);
            _toColumn = Math.Max(fromColumn, toColumn);
        }

        public void Dispose() { /* NPOI ranges are lightweight; nothing to release. */ }

        #region basic properties

        public object? Value
        {
            get
            {
                if (IsSingleCell)
                {
                    return CellHelper.GetValue(CellHelper.GetCellOrNull(_sheet, _fromRow - 1, _fromColumn - 1));
                }

                var values = new object?[RowCount, ColumnCount];
                for (int i = 0; i < RowCount; i++)
                {
                    for (int j = 0; j < ColumnCount; j++)
                    {
                        values[i, j] = CellHelper.GetValue(CellHelper.GetCellOrNull(_sheet, _fromRow - 1 + i, _fromColumn - 1 + j));
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
                            CellHelper.SetValue(CellHelper.GetOrCreateCell(_sheet, _fromRow - 1 + i, _fromColumn - 1 + j), matrix[i, j]);
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
            get => CellHelper.GetOrCreateRow(_sheet, _fromRow - 1).HeightInPoints;
            set { for (int r = _fromRow; r <= _toRow; r++) CellHelper.GetOrCreateRow(_sheet, r - 1).HeightInPoints = (float)value; }
        }

        public double Width
        {
            get => _sheet.GetColumnWidth(_fromColumn - 1) / 256.0;
            set { for (int c = _fromColumn; c <= _toColumn; c++) _sheet.SetColumnWidth(c - 1, (int)(value * 256)); }
        }

        #endregion

        public T? GetValue<T>() => CellHelper.ConvertTo<T>(Value);

        public void SetFormula(string formula)
        {
            var f = formula.StartsWith("=") ? formula.Substring(1) : formula;
            foreach (var cell in Cells()) cell.SetCellFormula(f);
        }

        public void SetFormulaR1C1(string formula)
        {
            throw new NotSupportedException("R1C1 formulas are not supported by the NPOI backend; use SetFormula (A1).");
        }

        public void Insert(InsertShiftType shiftType)
        {
            switch (shiftType)
            {
                case InsertShiftType.Down:
                case InsertShiftType.EntireRow:
                    if (_sheet.LastRowNum >= _fromRow - 1)
                        _sheet.ShiftRows(_fromRow - 1, _sheet.LastRowNum, RowCount);
                    break;
                case InsertShiftType.Right:
                case InsertShiftType.EntireColumn:
                    throw new NotSupportedException("Column-direction insert is not supported by the NPOI backend (no column-shift primitive).");
            }
        }

        public void Delete(DeleteShiftType shiftType)
        {
            switch (shiftType)
            {
                case DeleteShiftType.Top:
                case DeleteShiftType.EntireRow:
                    for (int i = 0; i < RowCount; i++)
                    {
                        var r = _sheet.GetRow(_fromRow - 1 + i);
                        if (r != null) _sheet.RemoveRow(r);
                    }
                    if (_sheet.LastRowNum >= _fromRow - 1 + RowCount)
                        _sheet.ShiftRows(_fromRow - 1 + RowCount, _sheet.LastRowNum, -RowCount);
                    break;
                case DeleteShiftType.Left:
                case DeleteShiftType.EntireColumn:
                    throw new NotSupportedException("Column-direction delete is not supported by the NPOI backend (no column-shift primitive).");
            }
        }

        public void CopyToRange(Range destRange)
        {
            for (int i = 0; i < RowCount; i++)
            {
                for (int j = 0; j < ColumnCount; j++)
                {
                    var src = CellHelper.GetCellOrNull(_sheet, _fromRow - 1 + i, _fromColumn - 1 + j);
                    var dest = CellHelper.GetOrCreateCell(destRange._sheet, destRange._fromRow - 1 + i, destRange._fromColumn - 1 + j);
                    if (src == null) { dest.SetBlank(); continue; }

                    if (src.CellType == CellType.Formula) dest.SetCellFormula(src.CellFormula);
                    else CellHelper.SetValue(dest, CellHelper.GetValue(src));

                    dest.CellStyle = src.CellStyle; // styles are shared+immutable in our model
                }
            }
        }

        public RangeValue GetRangeValue() => new RangeValue(Value, Column, Row, this);

        #region style

        private void ApplyStyle(Action<CellStyleDescriptor> mutate)
        {
            foreach (var cell in Cells())
            {
                var descriptor = _styles.DescriptorOf(cell);
                mutate(descriptor);
                _styles.Apply(cell, descriptor);
            }
        }

        public void SetBackground(Color color) => ApplyStyle(d => d.FillRgb = ColorHelper.ToRgb(color));

        public void SetForeground(Color color) => ApplyStyle(d => d.FontColorRgb = ColorHelper.ToRgb(color));

        public void SetFontFamily(string fontFamily) => ApplyStyle(d => d.FontName = fontFamily);

        public void SetFontSize(float size) => ApplyStyle(d => d.FontSize = size);

        public void SetFont(Font font) => ApplyStyle(d =>
        {
            d.FontName = font.Name;
            d.FontSize = font.Size;
            d.Italic = font.Italic;
            d.Underline = font.Underline;
            d.Strike = font.Strikeout;
        });

        public void SetFontStyle(FontStyle style) => ApplyStyle(d =>
        {
            d.Bold = style.HasFlag(FontStyle.Bold);
            d.Italic = style.HasFlag(FontStyle.Italic);
            d.Underline = style.HasFlag(FontStyle.Underline);
            d.Strike = style.HasFlag(FontStyle.Strikeout);
        });

        public void DrawBorder(Color color, BorderPosition position = BorderPosition.All, BorderStyle style = BorderStyle.Thin)
        {
            ApplyStyle(d =>
            {
                foreach (var side in Sides(position))
                {
                    d.BorderStyles[side] = style.ToNpoi();
                    d.BorderColorRgb[side] = ColorHelper.ToRgb(color);
                }
            });
        }

        public void ClearBorder(BorderPosition position = BorderPosition.All)
        {
            ApplyStyle(d =>
            {
                foreach (var side in Sides(position))
                {
                    d.BorderStyles[side] = NPOI.SS.UserModel.BorderStyle.None;
                    d.BorderColorRgb[side] = null;
                }
            });
        }

        // Side indices: 0=Left, 1=Top, 2=Right, 3=Bottom
        private static IEnumerable<int> Sides(BorderPosition position)
        {
            if (position.HasFlag(BorderPosition.Left)) yield return 0;
            if (position.HasFlag(BorderPosition.Top)) yield return 1;
            if (position.HasFlag(BorderPosition.Right)) yield return 2;
            if (position.HasFlag(BorderPosition.Bottom)) yield return 3;
        }

        public void SetHorizontalContentAlignment(HorizontalAlignment horizontalAlignment)
            => ApplyStyle(d => d.HAlign = horizontalAlignment.ToNpoi());

        public void SetVerticalContentAlignment(VerticalAlignment verticalAlignment)
            => ApplyStyle(d => d.VAlign = verticalAlignment.ToNpoi());

        public void SetFormat(string format) => ApplyStyle(d => d.DataFormat = format);

        #endregion

        #region focus

        public void Select()
        {
            _sheet.ActiveCell = new CellAddress(_fromRow - 1, _fromColumn - 1);
            _sheet.IsSelected = true;
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
            var helper = _sheet.GetDataValidationHelper();
            var constraint = helper.CreateExplicitListConstraint(items.ToArray());
            var addressList = new CellRangeAddressList(_fromRow - 1, _toRow - 1, _fromColumn - 1, _toColumn - 1);
            var validation = helper.CreateValidation(constraint, addressList);

            validation.EmptyCellAllowed = allowBlank;
            validation.SuppressDropDownArrow = true;
            validation.ErrorStyle = ERRORSTYLE.WARNING;

            if (!string.IsNullOrEmpty(errorMessage))
            {
                validation.ShowErrorBox = true;
                validation.CreateErrorBox(errorTitle, errorMessage);
            }

            _sheet.AddValidationData(validation);
        }

        public void AddListValidation(Range referenceRange)
        {
            var helper = _sheet.GetDataValidationHelper();
            var constraint = helper.CreateFormulaListConstraint(referenceRange.Address);
            var addressList = new CellRangeAddressList(_fromRow - 1, _toRow - 1, _fromColumn - 1, _toColumn - 1);
            var validation = helper.CreateValidation(constraint, addressList);
            validation.SuppressDropDownArrow = true;
            _sheet.AddValidationData(validation);
        }

        private IEnumerable<ICell> Cells()
        {
            for (int r = _fromRow; r <= _toRow; r++)
            {
                for (int c = _fromColumn; c <= _toColumn; c++)
                {
                    yield return CellHelper.GetOrCreateCell(_sheet, r - 1, c - 1);
                }
            }
        }
    }
}
