namespace FrostNova.Core.ExcelWork
{
    public static class EnumExtensions
    {
        public static DeleteShiftType ToDeleteShiftType(this InsertShiftType shiftType)
        {
            return shiftType switch
            {
                InsertShiftType.Right => DeleteShiftType.Left,
                InsertShiftType.Down => DeleteShiftType.Top,
                InsertShiftType.EntireRow => DeleteShiftType.EntireRow,
                InsertShiftType.EntireColumn => DeleteShiftType.EntireColumn,
                _ => DeleteShiftType.EntireRow,
            };
        }

        public static InsertShiftType ToInsertShiftType(DeleteShiftType shiftType)
        {
            return shiftType switch
            {
                DeleteShiftType.Left => InsertShiftType.Right,
                DeleteShiftType.Top => InsertShiftType.Down,
                DeleteShiftType.EntireRow => InsertShiftType.EntireRow,
                DeleteShiftType.EntireColumn => InsertShiftType.EntireColumn,
                _ => InsertShiftType.EntireRow,
            };
        }
    }
}
