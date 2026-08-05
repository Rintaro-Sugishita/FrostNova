using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;

namespace FrostNova.Core.DbAccess
{
    /// <summary>
    /// SQL 結果（DataTable）を列名で POCO へマッピングする。
    /// 列名は snake_case / camelCase を大小無視で照合し、数値型などは型補正する。
    /// 旧 DBUtil.ClassMapper の読み取りマッピング部分を移植（属性ベース ORM 機構は持たない）。
    /// </summary>
    public static class DbRowMapper
    {
        private static readonly HashSet<Type> _numericTypes = new HashSet<Type>
        {
            typeof(byte), typeof(sbyte), typeof(short), typeof(ushort),
            typeof(int), typeof(uint), typeof(long), typeof(ulong),
            typeof(float), typeof(double), typeof(decimal)
        };

        private static bool IsNumeric(Type t) => _numericTypes.Contains(t);

        public static List<T> Map<T>(DataTable table) where T : new()
        {
            var list = new List<T>();
            if (table == null || table.Rows.Count == 0) { return list; }

            var properties = typeof(T).GetProperties();
            var mappings = new List<ColumnMapping>();

            foreach (DataColumn column in table.Columns)
            {
                var columnName = column.ColumnName.ToLower();
                var camelName = columnName.Replace("_", "");
                var property = properties.FirstOrDefault(x =>
                    x.Name.ToLower() == columnName || x.Name.ToLower() == camelName);
                if (property == null) { continue; }

                var propertyType = property.PropertyType;
                var underlying = Nullable.GetUnderlyingType(propertyType);
                if (underlying != null) { propertyType = underlying; }

                if (column.DataType == propertyType)
                {
                    mappings.Add(new ColumnMapping(column.ColumnName, property, null));
                    continue;
                }

                var convert = ResolveConverter(column.DataType, propertyType);
                if (convert != null)
                {
                    mappings.Add(new ColumnMapping(column.ColumnName, property, convert));
                }
                // 型が異なり変換規則も無い列は（旧実装同様）マッピング対象外としてスキップする。
            }

            foreach (DataRow row in table.Rows)
            {
                var item = new T();
                foreach (var m in mappings)
                {
                    var value = row[m.ColumnName];
                    if (value == DBNull.Value)
                    {
                        m.Property.SetValue(item, null);
                    }
                    else if (m.Convert != null)
                    {
                        m.Property.SetValue(item, m.Convert(value));
                    }
                    else
                    {
                        m.Property.SetValue(item, value);
                    }
                }
                list.Add(item);
            }

            return list;
        }

        private static Func<object, object>? ResolveConverter(Type source, Type target)
        {
            if (target.IsEnum && (source == typeof(int) || source == typeof(decimal) || IsNumeric(source)))
            {
                return v => Enum.ToObject(target, Convert.ToInt32(v));
            }
            if ((source == typeof(decimal) || source == typeof(long) || source == typeof(ulong)
                    || source == typeof(short) || source == typeof(byte) || source == typeof(sbyte))
                && target == typeof(int))
            {
                return v => Convert.ToInt32(v);
            }
            if (source == typeof(double) && target == typeof(decimal))
            {
                return v => (decimal)(double)v;
            }
            if ((source == typeof(sbyte) || source == typeof(byte) || source == typeof(short)
                    || source == typeof(int) || source == typeof(long) || source == typeof(ulong)
                    || source == typeof(decimal))
                && target == typeof(bool))
            {
                return v => Convert.ToBoolean(v);
            }
            if (source == typeof(bool)
                && (target == typeof(sbyte) || target == typeof(byte) || target == typeof(short) || target == typeof(int)))
            {
                return v => Convert.ToInt32(v);
            }
            if (IsNumeric(source) && IsNumeric(target))
            {
                // 名前一致した数値型は汎用変換で必ずマッピングする（numeric_precision 等の CASE 列の取りこぼし防止）。
                return v => Convert.ChangeType(v, target);
            }
            return null;
        }

        private sealed class ColumnMapping
        {
            public string ColumnName { get; }
            public PropertyInfo Property { get; }
            public Func<object, object>? Convert { get; }

            public ColumnMapping(string columnName, PropertyInfo property, Func<object, object>? convert)
            {
                ColumnName = columnName;
                Property = property;
                Convert = convert;
            }
        }
    }
}
