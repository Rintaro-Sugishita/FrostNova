using FrostNova.Core.TypeToolkit;
using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace FrostNova.Core.ExcelWork
{
    public class RangeValue
    {

        object[,]? _values;



        int rowIndex;
        int columnIndex;

        int rowCount;
        int columnCount;

        Range _range;

        internal RangeValue(object? value, int leftCellIndex, int topCellIndex, Range source)
        {
            if (value is object[,] values)
            {
                _values = values;
                rowCount = values.GetLength(0);
                columnCount = values.GetLength(1);
            }

                rowIndex = topCellIndex;
            columnIndex = leftCellIndex;
            _range = source;
        }



        public object? this[int row, int column]
        {
            get
            {
                return GetValue(row, column);
            }
        }

        public object? this[string address]
        {
            get
            {
                return GetValue(address);
            }
        }

        public object? this[int row, string columnAddress]
        {
            get
            {
                return GetValue(row, columnAddress);
            }
        }

        public object? GetValue(int row, int column)
        {
            if (_values == null) return _values;
            if (row <= rowCount && column <= columnCount)
            {
                return _values[row - rowIndex, column - columnIndex];
            }
            else
            {
                return null;
            }

        }

        public object? GetValue(int row, string columnAddress)
        {
            return GetValue(row, ExcelFunctions.GetColumnIndex(columnAddress));
        }

        public object? GetValue(string address)
        {
            var rangeAddress = new ExcelAddress(address);
            return GetValue(rangeAddress.Start.Row, rangeAddress.Start.Column);
        }

        public T? GetValue<T>(int row, int column)
        {
            return ConvertHelper.ConvToAnyNull<T>(GetValue(row, column));
        }

        public T? GetValue<T>(int row, string columnAddress)
        {
            return GetValue<T>(row, ExcelFunctions.GetColumnIndex(columnAddress));
        }

        public T? GetValue<T>(string address)
        {
            var rangeAddress = new ExcelAddress(address);

            return GetValue<T>(rangeAddress.Start.Row, rangeAddress.Start.Column);
        }

        public void Apply()
        {
            if (_range != null)
            {
                Apply(_range);
            }
        }

        public void Apply(Range range)
        {
            range.Value = _values;
        }

    }
}
