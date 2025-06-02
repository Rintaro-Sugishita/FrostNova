using FrostNova.Core.TypeToolkit;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace FrostNova.Core.Wpf.Converters
{
    [ValueConversion(typeof(double), typeof(string))]
    public class DoubleRateTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {

            if (value is double dblValue)
            {
                var digits = ConvertHelper.StrToAny<int>(parameter?.ToString(), 0);

                return (dblValue * 100).ToString($"F{digits}") + "%";
            }
            else
            {
                return "";
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string strValue)
            {
                return ConvertHelper.ConvToDbl(value, 0) / 100;


            }
            else
            {
                return 0.0;
            }
        }
    }
}
