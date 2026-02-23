using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FrostNova.Core.TableWork
{
    public interface ITableObject
    {
        public int RowCount { get; }

        public int ColumnCount { get; }

        public bool CanAddRow { get; set; }
        public Type GetTableType();

        object GetRowInstance(int rowIndex);

        public IEnumerable<string> GetColumnCaptions();

        public object? GetValue(int rowIndex, int columnIndex);

        public object?[] GetValues(int rowIndex);

        public IEnumerable<Type> GetValueTypes();

        public void SetValue(int rowIndex, int columnIndex, object? value);


        public IEnumerable<object>? AddRow(int count = 1);

        public List<TableColumn> TargetColumns { get; set; }

    }
}
