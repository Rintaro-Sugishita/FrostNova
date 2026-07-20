using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Net.NetworkInformation;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics.CodeAnalysis;

namespace FrostNova.Core.TypeToolkit
{
    public static class ConvertHelper
    {        /// <summary>
             /// convert string type to any one
             /// </summary>
             /// <typeparam name="T"></typeparam>
             /// <param name="text"></param>
             /// <param name="def"></param>
             /// <returns></returns>
        public static T StrToAny<T>(string? text, T def = default) where T : struct, IParsable<T>
        {
            T? val = StrToAnyNull<T>(text, def);
            return val ?? def;
        }



        public static T? StrToAnyNull<T>(string? text, T? def = null) where T : struct, IParsable<T>
        {
            if (text == null) return def;

            // リフレクションもExpressionも不要。静的に型が確定する。
            if (T.TryParse(text, null, out T result))
            {
                return result;
            }
            return def;
        }

        /// <summary>
        /// →decimal変換
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="def"></param>
        /// <returns></returns>
        public static decimal ConvToDec(object? obj, decimal def = 0)
        {
            var retValue = ConvToDecNull(obj, def);
            return retValue ?? def;
        }

        public static decimal? ConvToDecNull(object? obj, decimal? def = null)
        {
            if (obj == null) { return def; }
            if (obj is decimal decimalValue)
            {
                return decimalValue;
            }
            else if (obj is string text)
            {
                return StrToAnyNull<decimal>(text, def);
            }

            try
            {
                return Convert.ToDecimal(obj);
            }
            catch (Exception)
            {

                return def;
            }
        }

        /// <summary>
        /// →double変換
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="def"></param>
        /// <returns></returns>
        public static double ConvToDbl(object? obj, double def = 0D)
        {
            var retValue = ConvToDblNull(obj, def);
            return retValue ?? def;

        }

        public static double? ConvToDblNull(object? obj, double? def = null)
        {
            if (obj == null) { return def; }
            if (obj is double doubleValue)
            {
                return doubleValue;
            }
            else if (obj is string text)
            {
                return StrToAnyNull<double>(text, def);
            }

            try
            {
                return Convert.ToDouble(obj);
            }
            catch (Exception)
            {
                return def;
            }

        }

        public static int ConvToInt(object? obj, int def = 0)
        {
            var retValue = ConvToIntNull(obj, def);
            return retValue ?? def;
        }

        public static int? ConvToIntNull(object? obj, int? def = null)
        {
            if (obj == null)
            {
                return def;
            }
            else if (obj is int intValue)
            {
                return intValue;
            }
            else if (obj is string objText)
            {
                return StrToAnyNull<int>(objText, def);
            }
            else
            {

                try
                {
                    return Convert.ToInt32(obj);
                }
                catch (Exception)
                {
                    return def;
                }
            }


        }

        public static long ConvToLong(object? obj, long def = 0)
        {
            var retValue = ConvToLongNull(obj, def);
            return retValue ?? def;
        }

        public static long? ConvToLongNull(object? obj, long? def = null)
        {
            if (obj == null)
            {
                return def;
            }
            else if (obj is long longValue)
            {
                return longValue;
            }
            else if (obj is string objText)
            {
                return StrToAnyNull<long>(objText, def);
            }

            try
            {
                return Convert.ToInt64(obj);
            }
            catch
            {
                return def;
            }
        }


        public static string ConvToStr(object? obj, string def = "")
        {
            var retValue = ConvToStrNull(obj, def);
            return retValue ?? def;
        }
        public static string? ConvToStrNull(object? obj, string? def = null)
        {
            if (obj == null) { return def; }
            try
            {
                return Convert.ToString(obj);
            }
            catch (Exception)
            {
                return def;
            }
        }

        public static bool ConvToBool(object? obj, bool def = false)
        {
            var retValue = ConvToBoolNull(obj, def);
            return retValue ?? def;
        }

        public static bool? ConvToBoolNull(object? obj, bool? def = null)
        {
            if (obj == null) { return def; }
            if (obj is bool boolValue)
            {
                return boolValue;
            }
            else if (obj is int intValue)
            {
                return intValue != 0;
            }
            else if (obj is string text)
            {
                if (string.IsNullOrEmpty(text))
                {
                    return def;
                }
                return StrToAnyNull<bool>(text, def);
            }

            try
            {
                return Convert.ToBoolean(obj);
            }
            catch (Exception)
            {
                return def;
            }


        }

        public static DateTime ConvToDateTime(object? obj, DateTime def = default, string? parseFormat = null)
        {
            var retValue = ConvToDateTimeNull(obj, def, parseFormat);
            return retValue ?? def;
        }
        public static DateTime? ConvToDateTimeNull(object? obj, DateTime? def = null, string? parseFormat = null)
        {
            if (obj == null) { return def; }
            if (obj is DateTime dateValue)
            {
                return dateValue;
            }
            else if (obj is string text)
            {
                if (parseFormat == null)
                {
                    return StrToAnyNull<DateTime>(text, def);
                }
                else
                {
                    if (DateTime.TryParseExact(text, parseFormat, null, System.Globalization.DateTimeStyles.None, out var retValue))
                    {
                        return retValue;
                    }
                }
            }
            else if (obj is double dblValue)
            {
                return DateTime.FromOADate(dblValue);
            }

            try
            {
                return Convert.ToDateTime(obj);
            }
            catch (Exception)
            {
                return def;
            }
        }

        public static TimeSpan ConvToTimeSpan(object? obj, TimeSpan def = default)
        {
            var retValue = ConvToTimeSpanNull(obj, def);
            return retValue ?? def;
        }

        public static TimeSpan? ConvToTimeSpanNull(object? obj, TimeSpan? def = default)
        {
            if (obj == null) { return def; }
            if (obj is TimeSpan timeValue)
            {
                return timeValue;
            }
            if (obj is long longValue)
            {
                return new TimeSpan(longValue);
            }
            else if (obj is int doubleValue)
            {
                return new TimeSpan(doubleValue);
            }
            else
            {
                var text = ConvToStr(obj);
                return StrToAnyNull(text, def);
            }
        }



        public static bool IsNullable(Type type) => Nullable.GetUnderlyingType(type) != null;

        public static IPAddress? ConvToIpAddress(object? obj, IPAddress? def = null)
        {
            if (obj == null)
            {
                return def;
            }
            if (obj is IPAddress addressValue)
            {
                return addressValue;
            }

            if (IPAddress.TryParse(ConvToStr(obj), out var ipAddress))
            {
                return ipAddress;
            }
            else
            {
                return def;
            }

        }

        public static PhysicalAddress? ConvToPhysicalAddress(object? obj, PhysicalAddress? def = null)
        {
            if (obj == null)
            {
                return def;
            }

            try
            {
                var address = PhysicalAddress.Parse(ConvToStr(obj));
                return address;

            }
            catch (Exception)
            {
                return def;

            }

        }


        public static Enum ConvToEnum(Type enumType, int value)
        {
            if (!enumType.IsEnum)
            {
                throw new ArgumentException($"argument {nameof(enumType)} must be enum. but type is {enumType.FullName}.");
            }

            if (Enum.IsDefined(enumType, value))
            {
                var enumValue = (Enum)Enum.ToObject(enumType, value);
                return enumValue;
            }
            else
            {
                throw new ArgumentException($"The value {value} is not defined by {enumType.FullName}.");
            }

        }

        public static T ConvToEnum<T>(int value) where T : Enum
        {
            return (T)ConvToEnum(typeof(T), value);
        }

        public static Enum ConvToEnum(Type enumType, string enumName)
        {
            if (!enumType.IsEnum)
            {
                throw new ArgumentException($"argument {nameof(enumType)} must be enum. but type is {enumType.FullName}.");
            }

            var enumValue = (Enum)Enum.Parse(enumType, enumName); ;
            return enumValue;
        }

        public static T? ConvToEnum<T>(string enumName) where T : struct
        {
            if (!typeof(T).IsEnum)
            {
                throw new ArgumentException($"argument {nameof(T)} must be enum. but type is {typeof(T).FullName}.");
            }

            if (Enum.TryParse<T>(enumName, out T convertedValue))
            {
                return convertedValue;
            }
            else
            {
                return null;
            }
        }

        public static T ConvToAny<T>(object? value, [AllowNull] T defaultValue = default)
        {
            var aa = ConvToAnyNull<T>(value, defaultValue);
            if (aa == null)
            {
                return defaultValue!;
            }
            else
            {
                return aa;
            }
        }


        public static T? ConvToAnyNull<T>(object? value, T? defaultValue = default)
        {
            return (T?)ConvToTypeNull(value, typeof(T), defaultValue);
        }

        /// <summary>
        /// 値を実行時に指定した <paramref name="propertyType"/> へ変換します。
        /// ジェネリック版 <see cref="ConvToAnyNull{T}(object?, T?)"/> の型を実行時に決めたい場合に使用します。
        /// </summary>
        public static object? ConvToTypeNull(object? value, Type propertyType, object? defaultValue = null)
        {
            var nullableType = Nullable.GetUnderlyingType(propertyType);
            if (nullableType != null)
            {
                propertyType = nullableType;
            }

            if (propertyType == typeof(string))
            {
                return ConvToStrNull(value, (string?)defaultValue);
            }
            else if (propertyType == typeof(int))
            {
                return ConvToIntNull(value, (int?)defaultValue);
            }
            else if (propertyType == typeof(decimal))
            {
                return ConvToDecNull(value, (decimal?)defaultValue);
            }
            else if (propertyType == typeof(double))
            {
                return ConvToDblNull(value, (double?)defaultValue);
            }
            else if (propertyType == typeof(DateTime))
            {
                return ConvToDateTimeNull(value, (DateTime?)defaultValue);
            }
            else if (propertyType == typeof(TimeSpan))
            {
                return ConvToTimeSpanNull(value, (TimeSpan?)defaultValue);
            }
            else if (propertyType == typeof(bool))
            {
                return ConvToBoolNull(value, (bool?)defaultValue);
            }
            else if (propertyType == typeof(IPAddress))
            {
                return ConvToIpAddress(value, (IPAddress?)defaultValue);
            }
            else if (propertyType == typeof(PhysicalAddress))
            {
                return ConvToPhysicalAddress(value, (PhysicalAddress?)defaultValue);
            }
            else if (propertyType == typeof(long))
            {
                return ConvToLongNull(value, (long?)defaultValue);
            }
            else if (propertyType.IsEnum)
            {
                return ConvToEnum(propertyType, ConvToInt(value));
            }

            throw new NotImplementedException($"the type {propertyType.Name} is not implemented");
        }



    }
}
