using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media;

namespace FrostNova.Core.Wpf.Converters
{
    [ValueConversion(typeof(Color), typeof(string))]
    public class ColorTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (targetType != typeof(Color))
                throw new InvalidOperationException("The target must be a Color");

            if (value is Color c)
            {
                return Convert(c);
            }
            else
            {
                return "";
            }

        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is String s)
            {
                return ConvertBack(s);
            }
            else
            {
                return Color.FromRgb(0, 0, 0);
            }

        }

        public static string Convert(Color c)
        {
            return $"#{c.R.ToString("X2")}{c.G.ToString("X2")}{c.B.ToString("X2")}";
        }

        public static Color ConvertBack(string s)
        {
            var rgbValues = GetRgb(s);
            return Color.FromRgb(rgbValues[0], rgbValues[1], rgbValues[2]);
        }

        public static byte[] GetRgb(string colorHexText)
        {
            if (colorHexText.StartsWith("#"))
            {
                colorHexText = colorHexText.Substring(1);
            }

            if (colorHexText.Length < 6)
            {
                colorHexText = colorHexText.PadLeft(6, '0');
            }

            byte r = 0;
            if (byte.TryParse(colorHexText.Substring(0, 2), NumberStyles.HexNumber, null, out byte redValueTmp))
            {
                r = redValueTmp;
            }

            byte g = 0;
            if (byte.TryParse(colorHexText.Substring(2, 2), NumberStyles.HexNumber, null, out byte greenValueTmp))
            {
                g = greenValueTmp;
            }

            byte b = 0;
            if (byte.TryParse(colorHexText.Substring(4, 2), NumberStyles.HexNumber, null, out byte blueValueTmp))
            {
                b = blueValueTmp;
            }

            return  new byte[] { r, g, b };

        }


    }
}