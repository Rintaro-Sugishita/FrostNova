using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;

namespace FrostNova.TimeRangeChart.Wpf.Converters
{

    
    public class RangeContentPaddingConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null || values.Length == 0) return new Thickness(0);
            if (values.Length > 2 && values[0] is Thickness normalThickness && values[1] is Thickness selectedThickness && values[2] is bool isSelected) 
            {
                if (isSelected)
                {
                    return new Thickness(
                                   Math.Max(0, normalThickness.Left - selectedThickness.Left),
                                   Math.Max(0, normalThickness.Top - selectedThickness.Top),
                                   Math.Max(0, normalThickness.Right - selectedThickness.Right),
                                   Math.Max(0, normalThickness.Bottom - selectedThickness.Bottom));
                }
                else
                {
                    return new Thickness(
                                   Math.Max(0, selectedThickness.Left - normalThickness.Left),
                                   Math.Max(0, selectedThickness.Top - normalThickness.Top),
                                   Math.Max(0, selectedThickness.Right - normalThickness.Right),
                                   Math.Max(0, selectedThickness.Bottom - normalThickness.Bottom));


                }
            }

            return new Thickness(0);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
