using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace FrostNova.Core.TableWork
{
    public class TableContainerBase : ITableObject
    {
        protected object tableInstance;

        public TableContainerBase(object table)
        {
            tableInstance = table ?? throw new ArgumentNullException();
            CanAddRow = true;
        }

        public virtual int RowCount
        {
            get
            {
                if (tableInstance is DataTable table)
                {
                    return table.Rows.Count;
                }
                else
                {
                    throw new InvalidOperationException();
                }
            }
        }
        public virtual int ColumnCount
        {
            get
            {
                if (tableInstance is DataTable table)
                {
                    return table.Columns.Count;
                }
                else
                {
                    throw new InvalidOperationException();
                }

            }
        }

        public virtual bool CanAddRow
        {
            get; set;
        }
        public List<TableColumn> TargetColumns { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public virtual IEnumerable<object>? AddRow(int count = 1)
        {
            if (!CanAddRow) return null;

            var list = new List<object>();
            if (tableInstance is DataTable table)
            {


                for (int i = 0; i < count; i++)
                {
                    var newRow = table.NewRow();

                    table.Rows.Add(newRow);
                    list.Add(newRow);
                }
            }
            return list;
        }

        public Type GetTableType()
        {
            return tableInstance.GetType();
        }

        public object GetRowInstance(int rowIndex)
        {
            if (tableInstance is DataTable table)
            {
                return table.Rows[rowIndex];
            }
            else { throw new InvalidOperationException(); }
        }

        public virtual IEnumerable<string> GetColumnCaptions()
        {
            if (tableInstance is DataTable table)
            {
                if (ColumnCount == 0) { return Array.Empty<string>(); }
                if (table.Columns[0].Caption != null && !string.IsNullOrEmpty(table.Columns[0].Caption))
                {
                    return table.Columns.Cast<DataColumn>().Select(x => x.Caption);
                }
                else
                {
                    return table.Columns.Cast<DataColumn>().Select(x => x.ColumnName);
                }
            }
            else { throw new InvalidOperationException(); }
        }

        public virtual object? GetValue(int rowIndex, int columnIndex)
        {
            if (tableInstance is DataTable table)
            {
                var value = table.Rows[rowIndex][columnIndex];

                return value == DBNull.Value ? null : value;
            }
            else
            {
                throw new InvalidOperationException();
            }
        }

        public virtual object?[] GetValues(int rowIndex)
        {
            if (tableInstance is DataTable table)
            {
                var items = table.Rows[rowIndex].ItemArray;
                if (items == null) return Array.Empty<object?>();

                // 配列内の DBNull をすべて null に置換
                for (int i = 0; i < items.Length; i++)
                {
                    if (items[i] == DBNull.Value) items[i] = null;
                }
                return items;
            }
            else
            {
                throw new InvalidOperationException();
            }

        }

        public virtual IEnumerable<Type> GetValueTypes()
        {
            if (tableInstance is DataTable table)
            {
                return table.Columns.Cast<DataColumn>().Select(x => x.DataType);
            }
            else
            {
                throw new InvalidOperationException();
            }

        }

        public virtual void SetValue(int rowIndex, int columnIndex, object? value)
        {
            if (tableInstance is DataTable table)
            {
                table.Rows[rowIndex][columnIndex] = value ?? DBNull.Value;
            }
            else
            {
                throw new InvalidOperationException();
            }
        }

        public void SetTargetColumn()
        {
            throw new NotImplementedException();
        }
    }
}
