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
    public class ForegroundConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is SolidColorBrush brush)
            {
                Color background = brush.Color;
return                Convert(background);
            }
            else if (value is Color color)
            {
                return Convert(color);
            }
                return Brushes.Black;
        }


        public static Color Convert(Color background)
        {
            double brightness = (background.R * 0.299) + (background.G * 0.587) + (background.B * 0.114);
            return brightness < 186 ? Colors.White : Colors.Black;

        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
