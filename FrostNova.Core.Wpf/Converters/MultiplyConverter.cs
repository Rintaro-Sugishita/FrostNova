using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace FrostNova.Core.Wpf.Converters
{
    public class MultiplyConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {

            double a = 1.0;
            if (values.Length > 0) 
            {
                foreach (var item in values)
                {
                    if (item is double dbl)
                    {
                        a *= dbl;
                    }

                }
            }

            return a;
            throw new NotImplementedException();
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
