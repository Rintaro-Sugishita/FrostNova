using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.Text;

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
            var address1 = new ExcelAddress(fromAddress);
            var address2 = new ExcelAddress(toAddress);

            return new RangeTemplate(sheet, address1.Rows, address1.Columns, address2.Rows, address2.Columns, shiftType);
        }

        public static RangeTemplate GetTemplate(this Sheet sheet, InsertShiftType? shiftType = null)
        {
            using (var range = sheet.GetUsedRange())
            {
                return GetTemplate(sheet, range.Row, range.Column, range.Row + range.RowCount - 1, range.Column + range.ColumnCount - 1, shiftType);
            }
        }

    }
}
