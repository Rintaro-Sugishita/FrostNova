using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace FrostNova.Core.Wpf.Converters
{

    [ValueConversion(typeof(Enum),(typeof(bool)))]
    public class EnumBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Convert(value, parameter);
        }

        public static bool Convert(object value, object parameter)
        {
            if (value == null || parameter == null) return false;

            return value.ToString() == parameter.ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || parameter == null) return Binding.DoNothing;
            if (value is bool boolValue && boolValue)
            {
                return Enum.Parse(targetType, parameter.ToString() ?? "");
            }

            return Binding.DoNothing;
        }
    }
}
