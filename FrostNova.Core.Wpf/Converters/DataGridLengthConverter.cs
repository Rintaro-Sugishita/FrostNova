using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Data;

namespace FrostNova.Core.Wpf.Converters
{
    [ValueConversion(typeof(double), typeof(DataGridLength))]
    public class DataGridLengthConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            DataGridLength length = 0;
            length = (double)value;
            return length;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            DataGridLength length = (DataGridLength)value;
            return (double)length.Value;
        }
    }
}
