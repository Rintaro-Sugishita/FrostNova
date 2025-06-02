using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Threading.Tasks;

namespace FrostNova.Core.TypeToolkit
{
    public static class DeepCloneHelper
    {
        public static T? DeepClone<T>(this T src) where T : class
        {
            ReadOnlySpan<byte> b = System.Text.Json.JsonSerializer.SerializeToUtf8Bytes<T>(src);
            var result = JsonSerializer.Deserialize<T>(b);
            if (result != null)
            {
                return result;
            }
            else
            {
                return null;
            }
        }

        public static T? DeepClone<T>(this T src, Type type) where T : class
        {
            ReadOnlySpan<byte> b = System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(src, type);
            var result = JsonSerializer.Deserialize(b, type);
            if (result == null)
            {
                return null;
            }
            else
            {
                return (T)result;
            }

        }

        public static void CopyProperty<T>(T src, T dest)
        {
            CopyPropertySameName(src, dest);
        }

        public static void CopyPropertySameName<T, U>(T src, U dest)
        {
            if (src == null) return;

            Type srcType = src.GetType();
            Type destType = src.GetType();

            var properties = srcType.GetProperties();
            var destProperties = destType.GetProperties();
            if (properties.Length == 0 && srcType.Name.StartsWith("ValueTuple"))
            {

                var fields = srcType.GetFields();
                var destFields = destType.GetFields();

                foreach (var field in fields)
                {

                    var v = field.GetValue(src);
                    object? dv = null;
                    if (v != null)
                    {
                        Type fieldType = v.GetType();
                        if (fieldType.GetInterfaces().Contains(typeof(ICloneable)))
                        {
                            dv = ((ICloneable)v).Clone();
                        }
                        else if (v is IList srcList)
                        {
                            var destList = field.FieldType.Assembly.CreateInstance(field.FieldType?.FullName ?? "");
                            foreach (var i in srcList)
                            {
                                var destItem = i.GetType().Assembly.CreateInstance(i.GetType().FullName ?? "");
                                CopyProperty((object)i, destItem);
                                ((IList)destList).Add(destItem);

                            }
                            dv = destList;
                        }
                        else if (fieldType.IsClass)
                        {
                            dv = DeepClone(v, fieldType);
                        }
                        else
                        {
                            dv = v;
                        }

                    }
                    field.SetValue(dest, dv);
                }

            }
            else if (destProperties.Length > 0)
            {
                foreach (var destProperty in destProperties)
                {
                    PropertyInfo? srcProperty = destProperty;
                    if (srcType != destType)
                    {
                        srcProperty = properties.FirstOrDefault(x => x.Name == destProperty.Name && x.PropertyType == destProperty.PropertyType);

                        if (srcProperty != null) { continue; }
                    }


                    Attribute? att = Attribute.GetCustomAttribute(destProperty, typeof(IgnoreCopyAttribute));
                    if (att != null && att is IgnoreCopyAttribute)
                    {
                        continue;
                    }

                    var v = srcProperty?.GetValue(src);
                    if (destProperty.CanWrite)
                    {
                        if (v != null)
                        {
                            Type propertyType = v.GetType();
                            object dv;
                            if (propertyType.GetInterfaces().Contains(typeof(ICloneable)))
                            {
                                dv = ((ICloneable)v).Clone();
                            }
                            else if (v is IList srcList)
                            {
                                var destList = destProperty.PropertyType.Assembly.CreateInstance(destProperty.PropertyType?.FullName ?? "");
                                foreach (var i in srcList)
                                {
                                    var destItem = i.GetType().Assembly.CreateInstance(i.GetType()?.FullName ?? "");
                                    CopyProperty((object)i, destItem);
                                    ((IList)destList).Add(destItem);

                                }
                                dv = destList;
                            }
                            else if (propertyType.IsClass)
                            {
                                dv = DeepClone(v, propertyType);
                            }
                            else
                            {
                                dv = v;
                            }
                            destProperty.SetValue(dest, dv);

                        }
                        else
                        {
                            destProperty.SetValue(dest, null);
                        }

                    }
                }

            }





        }

        public static T CopyProperty<T>(T src) where T : class, new()
        {
            T dest = new T();

            CopyProperty(src, dest);

            return dest;

        }

        public static void CopyField<T>(T src, T dest)
        {
            if (src == null) return;
            Type type = src.GetType();

            var fields = type.GetFields();
            foreach (var field in fields)
            {
                Attribute att = Attribute.GetCustomAttribute(field, typeof(IgnoreCopyAttribute));
                if (att != null && (att is IgnoreCopyAttribute || att is JsonIgnoreAttribute))
                {
                    continue;
                }

                var v = field.GetValue(src);
                Type fieldType = v.GetType();
                object dv;
                if (fieldType.GetInterfaces().Contains(typeof(ICloneable)))
                {
                    dv = ((ICloneable)v).Clone();
                }
                else if (fieldType.IsClass)
                {
                    dv = DeepClone(v, fieldType);
                }
                else
                {
                    dv = v;
                }
                field.SetValue(dest, dv);
            }

        }

        public static T CopyField<T>(T src) where T : class, new()
        {
            T dest = new T();

            CopyField(src, dest);

            return dest;
        }

        public static void CopyPropertyAndField<T>(T src, T dest)
        {
            CopyField(src, dest);
            CopyProperty(src, dest);

        }

        public static T CopyPropertyAndField<T>(T src) where T : class, new()
        {
            var dest = CopyProperty(src);
            CopyField(src, dest);
            return dest;
        }

        public static KeyValuePair<TKey, TValue> DeepCopy<TKey, TValue>(KeyValuePair<TKey, TValue> pair)
        {

            var keyType = pair.Key?.GetType();
            var valueType = pair.Value?.GetType();

            TKey destKey = default(TKey);
            if (keyType != null)
            {
                if (keyType.IsClass && keyType != typeof(string))
                {
                    destKey = (TKey)keyType.Assembly.CreateInstance(keyType.FullName);
                }
                if (destKey != null)
                {
                    CopyProperty(pair.Key, destKey);
                }
                else if (keyType == typeof(string))
                {
                    destKey = pair.Key;
                }
            }

            TValue destValue = default(TValue);
            if (valueType != null)
            {
                if (valueType.IsClass && valueType != typeof(string))
                {
                    destValue = (TValue)valueType.Assembly.CreateInstance(valueType.FullName);
                }
                if (destValue != null)
                {
                    CopyProperty(pair.Value, destValue);
                }
                else if (valueType == typeof(string))
                {
                    destValue = pair.Value;
                }
            }

            if (destKey != null && destValue != null)
            {
                return new KeyValuePair<TKey, TValue>(destKey, destValue);
            }
            else
            {
                return new KeyValuePair<TKey, TValue>();
            }
        }

    }
}
