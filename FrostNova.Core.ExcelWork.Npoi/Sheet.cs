using System;
using System.IO;
using NPOI.SS.UserModel;
using NPOI.SS.Util;
using NPOI.XSSF.UserModel;

namespace FrostNova.Core.ExcelWork
{
    /// <summary>
    /// A single worksheet. Row/column indexes in this API are 1-based (matching the EPPlus-based
    /// FrostNova.Core.ExcelWork); NPOI is 0-based internally.
    /// </summary>
    public class Sheet
    {
        private readonly ISheet _sheet;
        private readonly StyleCache _styles;

        internal Sheet(ISheet sheet, StyleCache styles)
        {
            _sheet = sheet;
            _styles = styles;
        }

        internal ISheet GetSheetObject() => _sheet;

        #region sheet

        public string Name
        {
            get => _sheet.SheetName;
            set
            {
                try
                {
                    var wb = _sheet.Workbook;
                    wb.SetSheetName(wb.GetSheetIndex(_sheet), value);
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"Sheet {_sheet.SheetName} was renamed to {value}, resulting in a duplicate name", ex);
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
            if (_sheet.PhysicalNumberOfRows == 0)
            {
                return new Range(_sheet, 1, 1, 1, 1, _styles);
            }

            int firstRow = _sheet.FirstRowNum + 1;
            int lastRow = _sheet.LastRowNum + 1;
            int firstCol = int.MaxValue;
            int lastCol = 1;

            for (int r = _sheet.FirstRowNum; r <= _sheet.LastRowNum; r++)
            {
                var row = _sheet.GetRow(r);
                if (row == null || row.PhysicalNumberOfCells == 0) continue;
                firstCol = Math.Min(firstCol, row.FirstCellNum + 1);
                lastCol = Math.Max(lastCol, row.LastCellNum); // LastCellNum is 1 past last (0-based) => 1-based last
            }

            if (firstCol == int.MaxValue) firstCol = 1;

            return needA1Cell
                ? new Range(_sheet, 1, 1, lastRow, lastCol, _styles)
                : new Range(_sheet, firstRow, firstCol, lastRow, lastCol, _styles);
        }

        public object? GetValue(int row, int column)
            => CellHelper.GetValue(CellHelper.GetCellOrNull(_sheet, row - 1, column - 1));

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
            => CellHelper.SetValue(CellHelper.GetOrCreateCell(_sheet, row - 1, column - 1), value);

        public void SetValue(string address, object? value)
        {
            var (r, c, _, _) = ExcelFunctions.ParseAddress(address);
            SetValue(r, c, value);
        }

        #endregion

        #region width and height

        // NPOI column width unit is 1/256 of a character; the public API uses character units.
        public double GetColumnWidth(int column) => _sheet.GetColumnWidth(column - 1) / 256.0;

        public void SetColumnWidth(int column, double width) => _sheet.SetColumnWidth(column - 1, (int)(width * 256));

        public void AutoFit()
        {
            int lastCol = LastUsedColumn0();
            for (int c = 0; c <= lastCol; c++) _sheet.AutoSizeColumn(c);
        }

        public void AutoFit(int column) => _sheet.AutoSizeColumn(column - 1);

        public void AutoFit(int fromColumn, int toColumn)
        {
            for (int c = fromColumn - 1; c <= toColumn - 1; c++) _sheet.AutoSizeColumn(c);
        }

        public double GetRowHeight(int row) => CellHelper.GetOrCreateRow(_sheet, row - 1).HeightInPoints;

        public void SetRowHeight(int row, double height) => CellHelper.GetOrCreateRow(_sheet, row - 1).HeightInPoints = (float)height;

        private int LastUsedColumn0()
        {
            int last = 0;
            for (int r = _sheet.FirstRowNum; r <= _sheet.LastRowNum; r++)
            {
                var row = _sheet.GetRow(r);
                if (row == null || row.PhysicalNumberOfCells == 0) continue;
                last = Math.Max(last, row.LastCellNum - 1);
            }
            return last;
        }

        #endregion

        #region row and column

        public void InsertRow(int row, int count = 1)
        {
            int start0 = row - 1;
            if (_sheet.LastRowNum >= start0)
            {
                _sheet.ShiftRows(start0, _sheet.LastRowNum, count);
            }
        }

        public void InsertColumn(int column, int count = 1)
        {
            throw new NotSupportedException("Column insertion is not supported by the NPOI backend (no column-shift primitive).");
        }

        public void InsertRange(int fromRow, int fromColumn, int toRow, int toColumn, InsertShiftType shiftType)
        {
            using var range = GetRange(fromRow, fromColumn, toRow, toColumn);
            range.Insert(shiftType);
        }

        public void CopyRow(int srcRow, int count, int destRow)
        {
            using var src = GetRange(srcRow, 1, srcRow + count - 1, Math.Max(1, LastUsedColumn0() + 1));
            using var dest = GetRange(destRow, 1, destRow + count - 1, Math.Max(1, LastUsedColumn0() + 1));
            src.CopyToRange(dest);
        }

        public void CopyColumn(int srcColumn, int count, int destColumn)
        {
            int lastRow = _sheet.LastRowNum + 1;
            using var src = GetRange(1, srcColumn, lastRow, srcColumn + count - 1);
            using var dest = GetRange(1, destColumn, lastRow, destColumn + count - 1);
            src.CopyToRange(dest);
        }

        public void DeleteRow(int row, int count = 1)
        {
            int start0 = row - 1;
            for (int i = 0; i < count; i++)
            {
                var r = _sheet.GetRow(start0 + i);
                if (r != null) _sheet.RemoveRow(r);
            }
            if (_sheet.LastRowNum >= start0 + count)
            {
                _sheet.ShiftRows(start0 + count, _sheet.LastRowNum, -count);
            }
        }

        public void DeleteColumn(int column, int count = 1)
        {
            throw new NotSupportedException("Column deletion is not supported by the NPOI backend (no column-shift primitive).");
        }

        public void DeleteRange(int fromRow, int fromColumn, int toRow, int toColumn, DeleteShiftType shiftType)
        {
            using var range = GetRange(fromRow, fromColumn, toRow, toColumn);
            range.Delete(shiftType);
        }

        #endregion

        #region select

        public void Select(string address)
        {
            var (r, c, _, _) = ExcelFunctions.ParseAddress(address);
            Select(r, c);
        }

        public void Select()
        {
            _sheet.IsSelected = true;
        }

        public void Select(int row, int column)
        {
            _sheet.ActiveCell = new CellAddress(row - 1, column - 1);
            _sheet.IsSelected = true;
        }

        public void Select(int row, string column)
            => Select(row, ExcelFunctions.GetColumnIndex(column));

        public void Select(int fromRow, int fromColumn, int toRow, int toColumn)
        {
            _sheet.ActiveCell = new CellAddress(fromRow - 1, fromColumn - 1);
            _sheet.IsSelected = true;
        }

        public void Select(int fromRow, string fromColumn, int toRow, string toColumn)
            => Select(fromRow, ExcelFunctions.GetColumnIndex(fromColumn), toRow, ExcelFunctions.GetColumnIndex(toColumn));

        public void SetSelect() => _sheet.IsSelected = true;

        public bool IsTabSelected => _sheet.IsSelected;

        #endregion

        #region shape and image

        public ExcelDrawingShape AddShape(ShapeStyle shapeStyle)
        {
            var drawing = (XSSFDrawing)_sheet.CreateDrawingPatriarch();
            var anchor = new XSSFClientAnchor(0, 0, 0, 0, 1, 1, 4, 6);
            var shape = drawing.CreateSimpleShape(anchor);
            shape.ShapeType = (int)shapeStyle.ToShapeType();
            return new ExcelDrawingShape(shape, anchor);
        }

        public ExcelDrawingImage AddImage(System.Drawing.Bitmap bitmap)
        {
            using var stream = new MemoryStream();
            bitmap.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
            return AddImageBytes(stream.ToArray(), PictureType.PNG);
        }

        public ExcelDrawingImage AddImage(Stream stream)
        {
            using var ms = new MemoryStream();
            stream.CopyTo(ms);
            return AddImageBytes(ms.ToArray(), PictureType.PNG);
        }

        public ExcelDrawingImage AddImage(string path)
        {
            var isJpeg = path.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase)
                      || path.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase);
            return AddImageBytes(File.ReadAllBytes(path), isJpeg ? PictureType.JPEG : PictureType.PNG);
        }

        private ExcelDrawingImage AddImageBytes(byte[] bytes, PictureType type)
        {
            int index = _sheet.Workbook.AddPicture(bytes, type);
            var drawing = (XSSFDrawing)_sheet.CreateDrawingPatriarch();
            var anchor = new XSSFClientAnchor(0, 0, 0, 0, 0, 0, 1, 1);
            var picture = drawing.CreatePicture(anchor, index);
            picture.Resize();
            return new ExcelDrawingImage(picture, anchor);
        }

        #endregion

        #region chart

        public ExcelDrawingChart AddChart(ChartType chartType)
        {
            var drawing = (XSSFDrawing)_sheet.CreateDrawingPatriarch();
            var anchor = new XSSFClientAnchor(0, 0, 0, 0, 1, 6, 10, 26);
            var chart = (XSSFChart)drawing.CreateChart(anchor);
            var wrapper = new ExcelDrawingChart(chart, anchor, _sheet);
            wrapper.Configure(chartType);
            return wrapper;
        }

        #endregion
    }
}
