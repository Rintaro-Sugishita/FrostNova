using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace FrostNova.Core.TableWork;

public class DataColumnTableColumn : TableColumn
{
    private readonly int _columnIndex;
    public override Type PropertyType { get; }

    public override LambdaExpression PropertySelector => throw new NotImplementedException();

    public DataColumnTableColumn(DataColumn col)
    {
        _columnIndex = col.Ordinal;
        HeaderText = col.Caption ?? col.ColumnName;
        PropertyType = col.DataType;
    }

    // DataTable用のGet/Set実装
    public override object? GetRawValue(object instance)
    {
        var row = (DataRow)instance;
        var val = row[_columnIndex];
        return val == DBNull.Value ? null : val;
    }

    public override void SetValueFromText(object instance, string? value)
    {
        var row = (DataRow)instance;
        // 簡易的な実装例：必要に応じて ConvertHelper 等を使用
        row[_columnIndex] = string.IsNullOrEmpty(value) ? DBNull.Value : value;
    }

    public override string? GetTextValue(object instance)
    {
        throw new NotImplementedException();
    }

    public override object? GetExcelValue(object instance)
    {
        throw new NotImplementedException();
    }

    public override void SetValueFromExcel(object instance, object? value)
    {
        throw new NotImplementedException();
    }
}
