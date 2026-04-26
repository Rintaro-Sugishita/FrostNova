using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace FrostNova.TimeRangeChart.Wpf.Converters;

public class MilestoneCellSizeConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        double symbolSize = values[0] is double s ? s : 0;
        double labelActualSize = values[1] is double a ? a : 0;
        LabelPosition pos = (LabelPosition)values[2];
        string type = parameter as string ?? "";

        if (type == "Width")
        {
            // 上下にラベルがある時は、大きい方の幅をセルの幅にする
            return (pos == LabelPosition.Top || pos == LabelPosition.Bottom)
                ? new GridLength(Math.Max(symbolSize, labelActualSize))
                : new GridLength(symbolSize);

        }
        else // Height
        {
            // 左右にラベルがある時は、大きい方の高さをセルの高さにする
            return (pos == LabelPosition.Left || pos == LabelPosition.Right)
                ? new GridLength(Math.Max(symbolSize, labelActualSize))
                : new GridLength(symbolSize);
        }
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
