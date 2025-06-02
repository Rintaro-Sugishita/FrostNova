using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace FrostNova.Core.Wpf.Converters
{
    [ValueConversion(typeof(DateTime),typeof(string))]
    public class DateTimeToTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is DateTime d)
            {
                if (parameter is string s)
                {
                    return d.ToString(s);
                }
                else
                {
                    return d.ToString();
                }

            }
            else
            {
                return "";
            }

        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
