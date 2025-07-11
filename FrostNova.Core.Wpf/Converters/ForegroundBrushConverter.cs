using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media;

namespace FrostNova.Core.Wpf.Converters
{
    public class ForegroundBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is SolidColorBrush brush)
            {
                Color background = brush.Color;
                return new SolidColorBrush(ForegroundConverter.Convert(background));
            }
            else if (value is Color color)
            {
                return new SolidColorBrush(ForegroundConverter.Convert(color));
            }
            return Brushes.Black;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
