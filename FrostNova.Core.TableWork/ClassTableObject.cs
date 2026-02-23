using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FrostNova.Core.Expressions;

namespace FrostNova.Core.TableWork
{
    public class ClassTableObject<T> : ITableObject where T : new()
    {
        protected IList<T> tableInstance;

        public ClassTableObject(IList<T> i)
        {
            tableInstance = i;
        }

        public int RowCount => tableInstance.Count();

        public int ColumnCount => TargetColumns.Count;

        public bool CanAddRow { get; set; }
        public List<TableColumn> TargetColumns { get; set; } = new List<TableColumn>();


        public IEnumerable<object>? AddRow(int count = 1)
        {

            if (!CanAddRow) return null;

            var list = new List<object>();
            
            for (int i = 0; i < count; i++)
            {
                var newRow = new T();
                tableInstance.Add(newRow);
                list.Add(newRow);
            }

            return list;
        }

        public object GetRowInstance(int rowIndex)
        {
            return tableInstance[rowIndex]!;
        }

        public IEnumerable<string> GetColumnCaptions()
        {
            return TargetColumns.Select(x => x.HeaderText).ToList();
        }

        public Type GetTableType()
        {
            return tableInstance.GetType();
        }

        public object? GetValue(int rowIndex, int columnIndex)
        {
            var rowValue = tableInstance[rowIndex];
            var column = TargetColumns[columnIndex];
            if (rowValue == null || column == null) return null;

            return column.GetRawValue(rowValue);
        }

        public object?[] GetValues(int rowIndex)
        {
            var rowValue = tableInstance[rowIndex];

            if (rowValue == null) return Array.Empty<object?>();

            var list = new List<object?>();
            foreach (var item in TargetColumns)
            {
                var val = item.GetRawValue(item);
                list.Add(val);
            }

            return list.ToArray();
        }

        public IEnumerable<Type> GetValueTypes()
        {
            var list = new List<Type>();

            return TargetColumns.Select(x => x.PropertyType);
        }

        public void SetValue(int rowIndex, int columnIndex, object? value)
        {
            var rowValue = tableInstance[rowIndex];

            var column = TargetColumns[columnIndex];
            if (rowValue == null || column == null)
            {
                throw new IndexOutOfRangeException();
            }

            column.SetValueFromExcel(rowValue,value);
        }

    }
}
