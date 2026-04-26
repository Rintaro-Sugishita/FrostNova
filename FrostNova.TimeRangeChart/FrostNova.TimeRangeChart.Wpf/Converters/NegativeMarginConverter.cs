using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace FrostNova.TimeRangeChart.Wpf.Converters;

public class NegativeMarginConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        double width = values.Length > 0 && values[0] is double w ? w : 0;
        double height = values.Length > 1 && values[1] is double h ? h : 0;

        // 左と上の余白を殺すことで、Gridの(1,1)にあるSymbolを無理やり(0,0)の位置に重ねる
        return new Thickness(-width, -height, 0, 0);
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
