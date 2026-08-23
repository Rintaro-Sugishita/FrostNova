namespace FrostNova.Core.ExcelWork.TemplateFramework
{
    public static class SheetExtensions
    {
        public static RangeTemplate GetTemplate(this Sheet sheet, int fromRow, int fromColumn, int toRow, int toColumn, InsertShiftType? shiftType = null)
        {
            return new RangeTemplate(sheet, fromRow, fromColumn, toRow, toColumn, shiftType);
        }

        public static RangeTemplate GetTemplate(this Sheet sheet, int fromRow, string fromColumn, int toRow, string toColumn, InsertShiftType? shiftType = null)
        {
            return new RangeTemplate(sheet, fromRow, ExcelFunctions.GetColumnIndex(fromColumn), toRow, ExcelFunctions.GetColumnIndex(toColumn), shiftType);
        }

        public static RangeTemplate GetTemplate(this Sheet sheet, string fromAddress, string toAddress, InsertShiftType? shiftType = null)
        {
            // Use the top-left cell of each address as the range bounds.
            var (fromRow, fromColumn, _, _) = ExcelFunctions.ParseAddress(fromAddress);
            var (toRow, toColumn, _, _) = ExcelFunctions.ParseAddress(toAddress);
            return new RangeTemplate(sheet, fromRow, fromColumn, toRow, toColumn, shiftType);
        }

        public static RangeTemplate GetTemplate(this Sheet sheet, InsertShiftType? shiftType = null)
        {
            using var range = sheet.GetUsedRange();
            return GetTemplate(sheet, range.Row, range.Column, range.Row + range.RowCount - 1, range.Column + range.ColumnCount - 1, shiftType);
        }
    }
}
