using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace FrostNova.Core.Common.Extensions
{
    public static class EnumExtensions
    {
        public static IEnumerable<Attribute> GetAttributes(this Enum enumField)
        {
            var info = enumField.GetType().GetField(enumField.ToString());
            return info?.GetCustomAttributes() ?? Array.Empty<Attribute>();

        }

        public static T? GetAttribute<T>(this Enum enumField) where T : Attribute
        {
            var info = enumField.GetType().GetField(enumField.ToString());
            if (info?.GetCustomAttribute<T>() is T ret)
            {
                return ret;
            }
            else
            {
                return null;
            }

        }



    }
}
