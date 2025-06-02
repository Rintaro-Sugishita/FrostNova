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
    [ValueConversion(typeof(double), typeof(double))]
    public class DoubleMultiplyConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            double dblValue = (double)value;
            double dblRate = 1.0;
            if (parameter is string strParam)
            {
                if (double.TryParse(strParam, out double converted))
                {
                    dblRate = converted;
                }
            }
            else if (parameter is double dblParam)
            {
                dblRate = dblParam;
            }
            if (dblRate < 0)
            {
                dblRate = 1;
            }

            return dblValue * dblRate;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
           throw new Exception();
        }
    }
}
