using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FrostNova.Core.Wpf.Common
{
    public class BindingResolveHelper
    {

        public static Type? GetBindingSourceType(System.Windows.Data.BindingExpression bindingExpr)
        {
            var source = bindingExpr.DataItem;
            var path = bindingExpr.ParentBinding.Path?.Path;
            if (source == null || string.IsNullOrEmpty(path)) return null;

            var type = source.GetType();
            Type? result = null;

            result = Expressions.ExpressionHelper.GetReturnType(source, path);

            //foreach (var part in path.Split('.'))
            //{
            //    var prop = type.GetProperty(part);
            //    if (prop == null) return null;

            //    result = prop.PropertyType;
            //    type = result;
            //}
            return result;
        }

        public static object? GetDefaultValueForType(Type type)
        {
            var underlying = Nullable.GetUnderlyingType(type);
            if (underlying != null)
                return null; // Nullable<T> は null を返す

            if (type == typeof(string))
                return string.Empty;

            if (type == typeof(int)) return 0;
            if (type == typeof(double)) return 0d;
            if (type == typeof(decimal)) return 0m;
            if (type == typeof(long)) return 0L;
            if (type == typeof(bool)) return false;

            if (type == typeof(DateTime))
                return DateTime.Today;

            if (type.IsEnum)
            {
                var names = Enum.GetValues(type);
                return names.Length > 0 ? names.GetValue(0) : null;
            }

            // その他の構造体やクラスは default(T) を使う
            try
            {
                return Activator.CreateInstance(type);
            }
            catch
            {
                return null;
            }

        }

    }
}
