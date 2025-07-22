using FrostNova.Core.ExcelWork.TemplateFramework;
using FrostNova.Core.TypeToolkit;
using OfficeOpenXml;
using OfficeOpenXml.Drawing;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq.Expressions;
using System.Text;

namespace FrostNova.Core.ExcelWork
{
    public class Sheet
    {

        ExcelWorksheet _sheet;

        #region constructor and destructor

        internal Sheet(ExcelWorksheet sheet)
        {
            _sheet = sheet;
        }


        #endregion

        #region sheet 

        public string Name
        {
            get { return _sheet.Name; }
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

        internal ExcelWorksheet GetSheetObject() { return _sheet; }

        #endregion

        #region range

        public Range GetRange(int row, int column)
        {
            return new Range(_sheet.Cells[row, column]);
        }

        public Range GetRange(int fromRow, int fromColumn, int toRow, int toColumn)
        {
            return new Range(_sheet.Cells[fromRow, fromColumn, toRow, toColumn]);
        }

        public Range GetRangeFromCount(int fromRow, int fromColumn, int rowCount, int columnCount)
        {
            return new Range(_sheet.Cells[fromRow, fromColumn, fromRow + rowCount - 1, fromColumn + columnCount - 1]);
        }

        public Range GetRange(string address)
        {
            return new Range(_sheet.Cells[address]);
        }

        public Range GetUsedRange(bool needA1Cell = true)
        {
            if (_sheet.Dimension == null)
            {
                return new Range(_sheet.Cells[1, 1]);
            }
            try
            {
                var rangeAddress = new ExcelAddress(_sheet.Dimension.Address);
                if (needA1Cell)
                {
                    return new Range(_sheet.Cells[1, 1, rangeAddress.End.Row, rangeAddress.End.Column]);
                }
                else
                {
                    return new Range(_sheet.Cells[rangeAddress.Start.Row, rangeAddress.Start.Column, rangeAddress.End.Row, rangeAddress.End.Column]);
                }
            }
            catch
            {
                return new Range(_sheet.Cells[1, 1]);
            }
        }


        /// <summary>
        /// Get the single cells value.
        /// </summary>
        /// <returns></returns>
        public object GetValue(int row, int column)
        {
            return _sheet.GetValue(row, column);
        }

        /// <summary>
        /// Get the single cells value.
        /// </summary>
        public object GetValue(int row, string columnText)
        {
            return _sheet.GetValue(row, ExcelFunctions.GetColumnIndex(columnText));
        }
        /// <summary>
        /// Get the single cells value.
        /// </summary>
        /// <returns></returns>
        public object GetValue(string singleCellsAddress)
        {
            ExcelAddress addressObject = new ExcelAddress(singleCellsAddress);
            return GetValue(addressObject.Start.Row, addressObject.Start.Column);
        }

        /// <summary>
        /// Get the single cells value.
        /// </summary>
        /// <returns></returns>
        public T GetValue<T>(int row, int column)
        {
            return _sheet.GetValue<T>(row, column);
        }

        public T GetValue<T>(int row, string columnText)
        {
            return _sheet.GetValue<T>(row, ExcelFunctions.GetColumnIndex(columnText));
        }

        /// <summary>
        /// Get the single cells value.
        /// </summary>
        /// <returns></returns>
        public T GetValue<T>(string singleCellsAddress)
        {
            ExcelAddress addressObject = new ExcelAddress(singleCellsAddress);
            return GetValue<T>(addressObject.Start.Row, addressObject.Start.Column);
        }


        public void SetValue(int row, int column, object? value)
        {
            _sheet.SetValue(row, column, value);
        }

        public void SetValue(string address, object? value)
        {
            _sheet.SetValue(address, value);
        }


       

        #endregion

        #region width and height
        public double GetColumnWidth(int column)
        {
            return _sheet.Columns[column].Width;
        }

        public void SetColumnWidth(int column, double width)
        {
            _sheet.Columns[column].Width = width;

        }

        public void AutoFit()
        {
            _sheet.Columns.AutoFit();
        }

        public void AutoFit(int column)
        {
            _sheet.Columns[column].AutoFit();
        }

        public void AutoFit(int fromColumn, int toColumn)
        {
            _sheet.Columns[fromColumn, toColumn].AutoFit();
        }

        public double GetRowHeight(int row)
        {
            return _sheet.Rows[row].Height;
        }

        public void SetRowHeight(int row, double height)
        {
            _sheet.Rows[row].Height = height;
        }

        #endregion

        #region row and column

        public void InsertRow(int row, int count = 1)
        {
            _sheet.InsertRow(row, count);
        }

        public void InsertColumn(int column, int count = 1)
        {
            _sheet.InsertColumn(column, count);
        }

        public void InsertRange(int fromRow, int fromColumn, int toRow, int toColumn, InsertShiftType shiftType)
        {
            using (var range = GetRange(fromRow, fromColumn, toRow, toColumn))
            {
                range.Insert(shiftType);
            }
        }

        public void CopyRow(int srcRow, int count, int destRow)
        {
            using (var srcRange = new Range(_sheet.Rows[srcRow, srcRow + count - 1].Range))
            using (var destRange = new Range(_sheet.Rows[destRow, destRow + count - 1].Range))
            {
                srcRange.CopyToRange(destRange);
            }
        }

        public void CopyColumn(int srcColumn, int count, int destColumn)
        {
            using (var srcRange = new Range(_sheet.Columns[srcColumn, srcColumn + count - 1].Range))
            using (var destRange = new Range(_sheet.Columns[destColumn, destColumn + count - 1].Range))
            {
                srcRange.CopyToRange(destRange);
            }
        }


        public void DeleteRow(int row, int count = 1)
        {
            _sheet.DeleteRow(row, count);
        }

        public void DeleteColumn(int column, int count = 1)
        {
            _sheet.DeleteColumn(column, count);
        }

        public void DeleteRange(int fromRow, int fromColumn, int toRow, int toColumn, DeleteShiftType shiftType)
        {
            using (var range = GetRange(fromRow, fromColumn, toRow, toColumn))
            {
                range.Delete(shiftType);
            }

        }

        #endregion

        #region select

        public void Select(string address)
        {
            _sheet.Select(address);
        }

        public void Select()
        {
            _sheet.Select();
        }

        public void Select(int row, int column)
        {
            var add = new ExcelAddress(row, column, row, column);
            _sheet.Select(add, true);
        }

        public void Select(int row, string column)
        {
            var columnIndex = ExcelFunctions.GetColumnIndex(column);
            var add = new ExcelAddress(row, columnIndex, row, columnIndex);
            _sheet.Select(add, true);

        }

        public void Select(int fromRow, int fromColumn, int toRow, int toColumn)
        {
            var add = new ExcelAddress(fromRow, fromColumn, toRow, toColumn);
            _sheet.Select(add, true);
        }
        public void Select(int fromRow, string fromColumn, int toRow, string toColumn)
        {
            var add = new ExcelAddress(fromRow, ExcelFunctions.GetColumnIndex(fromColumn), toRow, ExcelFunctions.GetColumnIndex(toColumn));
            _sheet.Select(add, true);
        }

        public void SetSelect()
        {
            _sheet.View.TabSelected = true;
        }

        public bool IsTabSelected => _sheet.View.TabSelectedMulti;

        #endregion

        #region graph

        public ExcelDrawingChart AddChart(ChartType chartType)
        {

            var type = ConvertHelper.ConvToEnum<OfficeOpenXml.Drawing.Chart.eChartType>(chartType.ToString());
            if (type == null)
            {
                throw new ArgumentException($"Invalid value : {chartType.ToString()}", nameof(chartType));
            }

            OfficeOpenXml.Drawing.ExcelDrawing chart = _sheet.Drawings.AddChart(GenerateNewDrawingName("chart"), type.Value);

            ExcelDrawingChart chart1 = new ExcelDrawingChart(chart);
            return chart1;

        }


        #endregion

        #region shape and image

        public ExcelDrawingShape AddShape(ShapeStyle shapeStyle)
        {
            var type = ConvertHelper.ConvToEnum<OfficeOpenXml.Drawing.eShapeStyle>(shapeStyle.ToString())!;

            OfficeOpenXml.Drawing.ExcelDrawing a = _sheet.Drawings.AddShape(GenerateNewDrawingName("shape"), type.Value);
        
            ExcelDrawingShape shape = new ExcelDrawingShape(a);
            return shape;
        }

        public ExcelDrawingImage AddImage(System.Drawing.Bitmap bitmap)
        {
            // 保存
            using (var stream = new MemoryStream())
            {
                bitmap.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
                return AddImage(stream);
            }
        }

        public ExcelDrawingImage AddImage(Stream stream)
        {
            OfficeOpenXml.Drawing.ExcelDrawing a = _sheet.Drawings.AddPicture(GenerateNewDrawingName("image"), stream);
            ExcelDrawingImage image = new ExcelDrawingImage(a);
            return image;
        }

        public ExcelDrawingImage AddImage(string path)
        {
            OfficeOpenXml.Drawing.ExcelDrawing a = _sheet.Drawings.AddPicture(GenerateNewDrawingName("image"), path);
            ExcelDrawingImage image = new ExcelDrawingImage(a);
            return image;
        }

        #endregion

        #region naming

        private int sheetNameIndex = 1;
        private string GenerateNewDrawingName(string baseSheetName = "")
        {
            while (true)
            {
                string newObjectName = string.IsNullOrEmpty(baseSheetName) ? $"sheet{sheetNameIndex}" : baseSheetName + sheetNameIndex;
                if (!IsDuplicatedObjectName(newObjectName))
                {
                    return newObjectName;
                }
                sheetNameIndex++;
            }
        }

        private bool IsDuplicatedObjectName(string sheetName)
        {
            foreach (var item in _sheet.Drawings)
            {
                if (item.Name == sheetName) return true;
            }
            return false;
        }

        #endregion


    }
}
