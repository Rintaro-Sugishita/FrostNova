using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace FrostNova.Core.Wpf.Converters
{
    public class DoubleAddConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double srcValue) 
            {
                double addValue = 0.0;
                if (parameter is string strParam)
                {
                    if (double.TryParse(strParam, out double converted))
                    {
                        addValue = converted;
                    }
                }
                else if (parameter is double dblParam)
                {
                    addValue = dblParam;
                }

                var calc= srcValue + addValue;
                if (calc < 0.0) 
                {
                    return 0.0;
                }
                else
                {
                    return calc;
                }
            }
            else
            {
                return value;
            }

        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
