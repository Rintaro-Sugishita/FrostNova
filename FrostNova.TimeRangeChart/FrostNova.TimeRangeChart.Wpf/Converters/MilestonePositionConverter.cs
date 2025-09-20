using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace FrostNova.TimeRangeChart.Wpf.Converters
{
    public class MilestonePositionConverter : IValueConverter
    {
        //与えられた widthの半分、左へずらす
        public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            //与えられた widthの半分、左へずらす
            if (value is double x)
            {
               // return new Thickness(-x/2.0,0,0,0);
               return -x / 2.0;
            }
            return null;
        }
   
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    
    
}
