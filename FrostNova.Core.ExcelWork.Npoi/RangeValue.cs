using System;

namespace FrostNova.Core.ExcelWork
{
    /// <summary>
    /// An in-memory snapshot of a range's values (0-based matrix relative to the range's top-left),
    /// addressable by 1-based row/column or A1 address, that can be written back with <see cref="Apply()"/>.
    /// </summary>
    public class RangeValue
    {
        private readonly object?[,]? _values;
        private readonly int _rowIndex;
        private readonly int _columnIndex;
        private readonly int _rowCount;
        private readonly int _columnCount;
        private readonly Range _range;

        internal RangeValue(object? value, int leftCellIndex, int topCellIndex, Range source)
        {
            if (value is object?[,] values)
            {
                _values = values;
                _rowCount = values.GetLength(0);
                _columnCount = values.GetLength(1);
            }

            _rowIndex = topCellIndex;
            _columnIndex = leftCellIndex;
            _range = source;
        }

        public object? this[int row, int column] => GetValue(row, column);
        public object? this[string address] => GetValue(address);
        public object? this[int row, string columnAddress] => GetValue(row, columnAddress);

        public object? GetValue(int row, int column)
        {
            if (_values == null) return null;
            if (row - _rowIndex >= 0 && row - _rowIndex < _rowCount &&
                column - _columnIndex >= 0 && column - _columnIndex < _columnCount)
            {
                return _values[row - _rowIndex, column - _columnIndex];
            }
            return null;
        }

        public object? GetValue(int row, string columnAddress)
            => GetValue(row, ExcelFunctions.GetColumnIndex(columnAddress));

        public object? GetValue(string address)
        {
            var (r, c, _, _) = ExcelFunctions.ParseAddress(address);
            return GetValue(r, c);
        }

        public T? GetValue<T>(int row, int column) => CellHelper.ConvertTo<T>(GetValue(row, column));

        public T? GetValue<T>(int row, string columnAddress)
            => GetValue<T>(row, ExcelFunctions.GetColumnIndex(columnAddress));

        public T? GetValue<T>(string address)
        {
            var (r, c, _, _) = ExcelFunctions.ParseAddress(address);
            return GetValue<T>(r, c);
        }

        public void Apply()
        {
            if (_range != null) Apply(_range);
        }

        public void Apply(Range range)
        {
            range.Value = _values;
        }
    }
}
