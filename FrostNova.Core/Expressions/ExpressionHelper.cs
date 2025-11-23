using FrostNova.Core.TypeToolkit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace FrostNova.Core.Expressions
{
    public class ExpressionHelper
    {
        public static string GetPropertyName<T, U>(Expression<Func<T, U>>? propertyExpression)
        {
            if (propertyExpression?.Body is MemberExpression memberExpression)
            {
                return memberExpression.Member.Name;
            }
            throw new ArgumentException("Invalid expression. Expected a member expression.", nameof(propertyExpression));
        }


        public static string GetPropertyPath<T, U>(Expression<Func<T, U>>? propertyExpression)
        {
            var names = new List<string>();
            Expression? expr = propertyExpression?.Body;
            while (expr is MemberExpression memberExpr)
            {
                names.Insert(0, memberExpr.Member.Name);
                expr = memberExpr.Expression;
            }
            return string.Join(".", names);
        }


        public static U GetValue<T, U>(T instance, Expression<Func<T, U>> propertyExpression)
        {
            return propertyExpression.Compile().Invoke(instance);
        }

        public static Type? GetReturnType(LambdaExpression expression)
        {
            return expression?.ReturnType;
        }

        public static object? GetValue(object? instance, LambdaExpression? expression)
        {
            if (expression == null) throw new ArgumentNullException(nameof(expression));
            if (instance == null) throw new ArgumentNullException(nameof(instance));

            var compileMethod = expression.GetType().GetMethod("Compile", Type.EmptyTypes);
            if (compileMethod == null)
                throw new InvalidOperationException("Compile method is not found.");

            var func = compileMethod.Invoke(expression, null);
            if (func == null)
                throw new InvalidOperationException("failed to invoke compile.");

            var invokeMethod = func.GetType().GetMethod("Invoke");
            if (invokeMethod == null)
                throw new InvalidOperationException("Invoke method is not found.");

            return invokeMethod.Invoke(func, new[] { instance });
        }

        public static object? GetValue(object? instance, string propertyPath)
        {
            if (instance == null)
                return null;
            var ex = CreatePropertyExpression(instance, propertyPath);
            return GetValue(instance, ex);
        }

        public static Type? GetReturnType(object? instance, string propertyPath)
        {
            if (instance == null)
                return null;
            if (string.IsNullOrWhiteSpace(propertyPath))
                throw new ArgumentNullException(nameof(propertyPath));

            var type = instance.GetType();
            Type? currentType = type;

            foreach (var propName in propertyPath.Split('.'))
            {
                var prop = currentType?.GetProperty(
                    propName,
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase);

                if (prop == null)
                    throw new ArgumentException($"Property '{propName}' not found in type '{currentType?.Name}'.");

                currentType = prop.PropertyType;
            }

            return currentType;
        }

        public static LambdaExpression CreatePropertyExpression(object instance, string propertyPath)
        {
            if (instance == null)
                throw new ArgumentNullException(nameof(instance));
            if (string.IsNullOrWhiteSpace(propertyPath))
                throw new ArgumentNullException(nameof(propertyPath));

            var type = instance.GetType();
            var parameter = Expression.Parameter(type, "x");
            Expression body = parameter;

            foreach (var propName in propertyPath.Split('.'))
            {
                var prop = body.Type.GetProperty(propName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase);
                if (prop == null)
                    throw new ArgumentException($"property '{propName}' is not found in  '{body.Type.Name}' type.");

                body = Expression.Property(body, prop);
            }

            // 値型は object に変換（ボックス化）
            if (body.Type.IsValueType)
            {
                body = Expression.Convert(body, typeof(object));
            }

            // 非ジェネリック版の LambdaExpression を返す
            return Expression.Lambda(body, parameter);
        }

        public static void SetValue<T, U>(T target, System.Linq.Expressions.Expression<Func<T, U>> propertyExpression, U value)
        {
            // MemberExpressionをたどってプロパティチェーンを取得
            var members = new System.Collections.Generic.List<MemberInfo>();
            Expression? current = propertyExpression.Body;
            while (current is MemberExpression memberExpr)
            {
                members.Insert(0, memberExpr.Member);
                current = memberExpr.Expression;
            }

            // 親オブジェクトをたどる
            object? obj = target;
            for (int i = 0; i < members.Count - 1; i++)
            {
                var prop = members[i] as PropertyInfo;
                if (prop == null) throw new InvalidOperationException();
                obj = prop.GetValue(obj);
                if (obj == null) throw new NullReferenceException();
            }

            // 最後のプロパティにSetValue
            var lastProp = members[^1] as PropertyInfo;
            if (lastProp == null) throw new InvalidOperationException();
            lastProp.SetValue(obj, value);
        }
        public static void SetValue(object instance, LambdaExpression expression, object? value)
        {
            if (expression == null) throw new ArgumentNullException(nameof(expression));
            if (instance == null) throw new ArgumentNullException(nameof(instance));

            // MemberExpressionをたどってプロパティチェーンを取得
            var members = new System.Collections.Generic.List<MemberInfo>();
            Expression? current = expression.Body;
            while (current is MemberExpression memberExpr)
            {
                members.Insert(0, memberExpr.Member);
                current = memberExpr.Expression;
            }

            // 親オブジェクトをたどる
            object? obj = instance;
            for (int i = 0; i < members.Count - 1; i++)
            {
                var prop = members[i] as PropertyInfo;
                if (prop == null) throw new InvalidOperationException();
                obj = prop.GetValue(obj);
                if (obj == null) throw new NullReferenceException();
            }

            // 最後のプロパティにSetValue
            var lastProp = members[^1] as PropertyInfo;
            if (lastProp == null) throw new InvalidOperationException();

            object? convertedValue = null;
            if (value != null && lastProp.PropertyType.IsAssignableFrom(value.GetType()) == false)
            {
                convertedValue = ConvertHelper.ConvToAnyNull(value, lastProp.PropertyType);
            }
            else
            {
                convertedValue = value;
            }

            lastProp.SetValue(obj, value);
        }
    }
}
