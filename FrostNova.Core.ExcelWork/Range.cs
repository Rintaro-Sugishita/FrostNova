using OfficeOpenXml;
using OfficeOpenXml.Drawing.Chart.ChartEx;
using OfficeOpenXml.Style;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Drawing.Text;
using System.Data;
using OfficeOpenXml.DataValidation;
using FrostNova.Core.TypeToolkit;

namespace FrostNova.Core.ExcelWork
{
    public class Range : IDisposable
    {
        internal ExcelRangeBase _range;

        #region 
        internal Range(ExcelRangeBase range)
        {
            _range = range;
        }
        public void Dispose()
        {
            _range.Dispose();
        }

        #endregion

        #region basic properties

        public object? Value
        {
            get => _range.Value;
            set => _range.Value = value;
        }

        public int Row
        {
            get => _range.Start.Row;
        }

        public int Column
        {
            get => _range.Start.Column;
        }

        public int ToRow
        {
            get => _range.End.Row;
        }

        public int ToColumn
        {
            get => _range.End.Column;
        }

        public string Address
        {
            get => _range.Address;
        }


        public int RowCount
        {
            get => _range.Rows;
        }

        public int ColumnCount
        {
            get => _range.Columns;
        }

        public bool IsSingleCell
        {
            get => RowCount == 1 && ColumnCount == 1;
        }

        public double Height
        {
            get => _range.EntireRow.Height;
            set { _range.EntireRow.Height = value; }
        }

        public double Width
        {
            get => _range.EntireColumn.Width;
            set { _range.EntireColumn.Width = value; }
        }

        #endregion

        public T? GetValue<T>()
        {
            return _range.GetValue<T>();
        }

        public void SetFormula(string formula)
        {
            _range.Formula = formula;
        }

        public void SetFormulaR1C1(string formula)
        {
            _range.FormulaR1C1 = formula;
        }


        public void Insert(InsertShiftType shiftType)
        {
            _range.Insert(ConvertHelper.ConvToEnum<eShiftTypeInsert>(shiftType.ToString()) ?? eShiftTypeInsert.EntireRow);
        }

        public void CopyToRange(Range destRange)
        {
            _range.Copy(destRange._range, ExcelRangeCopyOptionFlags.ExcludeDrawings);
            _range.CopyStyles(destRange._range);
        }

        public void Delete(DeleteShiftType shiftType)
        {
            _range.Delete(ConvertHelper.ConvToEnum<eShiftTypeDelete>(shiftType.ToString()) ?? eShiftTypeDelete.EntireRow);
        }

        public RangeValue GetRangeValue()
        {
            return new RangeValue(Value, Column, Row, this);
        }

 

        #region style

        public void SetBackground(Color color)
        {
            _range.Style.Fill.SetBackground(color, OfficeOpenXml.Style.ExcelFillStyle.Solid);
        }
        public void SetForeground(Color color)
        {
            _range.Style.Font.Color.SetColor(color);
        }
   
        public void SetFontFamily(string fontFamily)
        {
            _range.Style.Font.Name = fontFamily;
        }
        public void SetFontSize(float size)
        {
            _range.Style.Font.Size = size;
        }
        public void SetFont(Font font)
        {
            _range.Style.Font.SetFromFont(font.Name, font.Size, font.Italic, font.Underline, font.Strikeout);
        }
          public void SetFontStyle(FontStyle style)
        {
            _range.Style.Font.Bold = style.HasFlag(FontStyle.Bold);
            _range.Style.Font.Italic = style.HasFlag(FontStyle.Italic);
            _range.Style.Font.UnderLine = style.HasFlag(FontStyle.Underline);
            _range.Style.Font.Strike = style.HasFlag(FontStyle.Strikeout);
        }

        public void DrawBorder(System.Drawing.Color color, BorderPosition position = BorderPosition.All, BorderStyle style = BorderStyle.Thin)
        {
            if (position.HasFlag(BorderPosition.Left)) { DrawBorder(_range.Style.Border.Left, color, style); }
            ;
            if (position.HasFlag(BorderPosition.Top)) { DrawBorder(_range.Style.Border.Top, color, style); }
            ;
            if (position.HasFlag(BorderPosition.Right)) { DrawBorder(_range.Style.Border.Right, color, style); }
            ;
            if (position.HasFlag(BorderPosition.Bottom)) { DrawBorder(_range.Style.Border.Bottom, color, style); }
            ;
        }

        private void DrawBorder(ExcelBorderItem border, System.Drawing.Color color, BorderStyle style)
        {
            border.Style = ConvertHelper.ConvToEnum<ExcelBorderStyle>(style.ToString()) ?? ExcelBorderStyle.None;
            border.Color.SetColor(color);
        }

        public void ClearBorder(BorderPosition position = BorderPosition.All)
        {
            if (position.HasFlag(BorderPosition.Left)) { ClearBorder(_range.Style.Border.Left); }
            ;
            if (position.HasFlag(BorderPosition.Top)) { ClearBorder(_range.Style.Border.Top); }
            ;
            if (position.HasFlag(BorderPosition.Right)) { ClearBorder(_range.Style.Border.Right); }
            ;
            if (position.HasFlag(BorderPosition.Bottom)) { ClearBorder(_range.Style.Border.Bottom); }
            ;
        }

        private void ClearBorder(ExcelBorderItem border)
        {
            border.Color.SetAuto();
            border.Style = ExcelBorderStyle.None;
        }

        public void SetHorizontalContentAlignment(HorizontalAlignment horizontalAlignment)
        {
            _range.Style.HorizontalAlignment = ConvertHelper.ConvToEnum<ExcelHorizontalAlignment>(horizontalAlignment.ToString()) ?? ExcelHorizontalAlignment.General;
        }

        public void SetVerticalContentAlignment(VerticalAlignment verticalAlignment)
        {
            _range.Style.VerticalAlignment = ConvertHelper.ConvToEnum<ExcelVerticalAlignment>(verticalAlignment.ToString()) ?? ExcelVerticalAlignment.Top;
        }

        public void SetFormat(string format)
        {
            _range.Style.Numberformat.Format = format;
        }

        #endregion

        #region focus

        public void Select()
        {
            _range.Worksheet.Select(_range.Address, true);
        }

        #endregion

        public void SetTable(DataTable dt, bool isCreateColumn = true, int maxRow = ExcelOperator.MAX_ROW_COUNT)
        {
            var values = _range.Value as object[,];

            if (isCreateColumn)
            {
                dt.Clear();
                dt.Columns.Clear();
            }

            var columnLength = _range.EntireColumn.EndColumn;
            var rowLength = _range.EntireRow.EndRow;

            var startColumn = _range.EntireColumn.StartColumn;
            var startRow = _range.EntireRow.StartRow;

            if (values != null)
            {
                if (isCreateColumn)
                    for (int i = 0; i < columnLength; i++)
                    {
                        var col = new DataColumn();
                        var name = ExcelFunctions.GetColumnName(i + 1);
                        col.Caption = name;
                        col.ColumnName = name;
                        col.DataType = typeof(object);
                        dt.Columns.Add(col);
                    }

                var loopColumnCount = Math.Min(columnLength, dt.Columns.Count);
                var loopRowCount = Math.Min(rowLength, maxRow);

                for (var i = 0; i < loopRowCount; i++)
                {
                    var newRow = dt.NewRow();
                    var rowIndex = i + startRow;
                    for (var j = 0; j < loopColumnCount; j++)
                    {
                        var columnIndex = j + startColumn;
                        if (columnIndex >= startColumn)
                        {
                            newRow[j] = values[rowIndex - 1, columnIndex - 1];
                        }
                        else
                        {
                            newRow[j] = DBNull.Value;
                        }
                    }

                    dt.Rows.Add(newRow);

                }
            }



        }

        public void AddListValidation(IEnumerable<string> items, bool allowBlank = true, string errorMessage = "", string errorTitle = "")
        {
            _range.DataValidation.ClearDataValidation();
            var validation = _range.DataValidation.AddListDataValidation();

            validation.AllowBlank = allowBlank;

            validation.ErrorStyle = ExcelDataValidationWarningStyle.warning;
            if (errorMessage != "")
            {
                validation.ShowErrorMessage = true;
                validation.ErrorTitle = errorTitle;
                validation.Error = errorMessage;
            }

            foreach (var item in items)
            {
                validation.Formula.Values.Add(item);
            }
        }
        public void AddListValidation(Range referenceRange)
        {
            _range.DataValidation.ClearDataValidation();
            var validation = _range.DataValidation.AddListDataValidation();

            validation.Formula.ExcelFormula = referenceRange.Address;
        }



    }
}
