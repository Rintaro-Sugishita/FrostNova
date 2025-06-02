using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows;

namespace FrostNova.Core.Wpf.Converters
{
    [ValueConversion(typeof(TimeSpan), typeof(DateTime))]
    public class TimespanToDateTimeConverter : IValueConverter
    {
        public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
            {
                return null;
            }

            if (value == DependencyProperty.UnsetValue)
            {
                return null;
            }

            if (value is TimeSpan)
            {
                var value2 = (TimeSpan)value;
                return DateTime.Now.Date.Add(value2);
            }
            return TimeSpan.Zero;

        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || value == DependencyProperty.UnsetValue)
            {
                return TimeSpan.Zero;
            }

            if (value is DateTime)
            {
                var value2 = (DateTime)value;
                return value2.TimeOfDay;
            }

            return TimeSpan.Zero;


        }
    }
}
