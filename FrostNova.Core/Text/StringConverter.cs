using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FrostNova.Core.Text
{
    public static class StringConverter
    {
        public static string ToSnakeCase(this string camelCaseText, bool isUpperCase = false, char delimiter = '_')
        {
            var length = camelCaseText.Length;
            var sb = new StringBuilder(length * 2);

            for (int i = 0; i < length; i++)
            {
                var chr = camelCaseText[i];
                if (char.IsUpper(chr))
                {
                    sb.Append(delimiter);
                }
                sb.Append(chr);
            }

            if (isUpperCase)
            {
                return sb.ToString().ToUpper();
            }
            else
            {
                return sb.ToString().ToLower();
            }

        }

        public static string ToCamelCase(this string snakeCaseText, bool isUpperCase = false, char delimiter = '_')
        {
            var length = snakeCaseText.Length;
            var sb = new StringBuilder(length);

            bool isNextUpper = isUpperCase;
            for (int i = 0; i < length; i++)
            {
                var chr = snakeCaseText[i];
                if (chr == delimiter)
                {
                    isNextUpper = true;
                }
                else
                {
                    if (isNextUpper)
                    {
                        sb.Append(chr.ToString().ToUpper());
                        isNextUpper = false;
                    }
                    else
                    {
                        sb.Append(chr);
                    }
                }
            }
            return sb.ToString();
        }

    }
}
