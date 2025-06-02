using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace FrostNova.Core.Wpf.Converters
{
    public class BorderThicknessConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (values.Length == 2 && values[0] is double left && values[1] is double right)
            {
                return new System.Windows.Thickness(left, 0, right, 0);
            }
            else if (values.Length > 3 && values[0] is double left2 && values[1] is double top && values[2] is double right2 && values[3] is double bottom)
            {
                return new System.Windows.Thickness(left2, top, right2, bottom);
            }



            return new System.Windows.Thickness(0);
        }
        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
