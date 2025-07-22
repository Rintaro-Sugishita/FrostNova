using System;
using System.Collections.Generic;
using System.Text;

namespace FrostNova.Core.ExcelWork
{
    public static  class EnumExtensions
    {
        public static DeleteShiftType ToDeleteShiftType(this InsertShiftType shiftType)
        {
            switch (shiftType)
            {
                case InsertShiftType.Right:
                    return DeleteShiftType.Left;
                case InsertShiftType.Down:
                    return DeleteShiftType.Top;
                case InsertShiftType.EntireRow:
                    return DeleteShiftType.EntireRow;
                case InsertShiftType.EntireColumn:
                    return DeleteShiftType.EntireColumn;
                default:
                    return DeleteShiftType.EntireRow;
            }
        }

        public static InsertShiftType ToInsertShiftType(DeleteShiftType shiftType)
        {
            switch (shiftType)
            {
                case DeleteShiftType.Left:
                    return InsertShiftType.Right;
                case DeleteShiftType.Top:
                    return InsertShiftType.Down;
                case DeleteShiftType.EntireRow:
                    return InsertShiftType.EntireRow;
                case DeleteShiftType.EntireColumn:
                    return InsertShiftType.EntireColumn;
                default:
                    return InsertShiftType.EntireRow;
            }
        }

    }
}
