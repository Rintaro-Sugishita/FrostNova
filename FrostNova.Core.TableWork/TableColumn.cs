using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using FrostNova.Core.Expressions;

namespace FrostNova.Core.TableWork
{
  
    public abstract class TableColumn
    {
        public string HeaderText { get; set; } = "";
        public string? Format { get; set; }
        public abstract LambdaExpression PropertySelector { get; }
        public abstract Type PropertyType { get; }

        public abstract object? GetRawValue(object instance);         // そのままの型
        public abstract string? GetTextValue(object instance);        // テキスト形式用 (Converter適用)
        public abstract object? GetExcelValue(object instance);       // Excel用 (ExcelConverter適用)

        // 2つの設定メソッド
        public abstract void SetValueFromText(object instance, string? value);
        public abstract void SetValueFromExcel(object instance, object? value);
    }
}
