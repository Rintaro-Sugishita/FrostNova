using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace FrostNova.Core.Wpf.Converters
{
    public class MultiValueToArrayConverter : IMultiValueConverter
    {
        public object? Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {

            if (values == null || values.Length == 0)
            {
                return null;
            }
            else if (values.Length == 1) 
            { 
            return values[0];
            }
            else if (values.Length == 2)
            {
                return (values[0], values[1]);
            }

            return null;


        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
