using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace FrostNova.TimeRangeChart.Wpf.Converters;

public class MilestoneCenterConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        double symbolSize = values[0] is double s ? s : 0;
        double labelActualSize = values[1] is double a ? a : 0;
        LabelPosition pos = values[2] is LabelPosition p ? p : LabelPosition.Right;
        double spacing = values.Length > 3 && values[3] is double sp ? sp : 4.0; // 引数から取る
        string axis = parameter as string ?? "";

      
        double offset = 0.0;

        if (axis == "X")
        {
            // 左にあるラベル分を引き戻す
            if (pos == LabelPosition.Left) offset -= (labelActualSize + spacing);

            // 中央セルがラベルに合わせて広がった分、シンボルを中央に寄せるための補正
            double cellWidth = (pos == LabelPosition.Top || pos == LabelPosition.Bottom)
                ? Math.Max(symbolSize, labelActualSize)
                : symbolSize;

            offset -= (cellWidth / 2.0);
            return offset;
        }
        else // Y
        {
            if (pos == LabelPosition.Top) offset -= (labelActualSize + spacing);

            double cellHeight = (pos == LabelPosition.Left || pos == LabelPosition.Right)
                ? Math.Max(symbolSize, labelActualSize)
                : symbolSize;

            offset -= (cellHeight / 2.0);
            return offset;
        }
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
    
}
