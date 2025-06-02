using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows;

namespace FrostNova.Core.Wpf.Converters
{
    [ValueConversion(typeof(bool), typeof(Visibility))]
    public class CollapseVisibilityInvertConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {

            if (value == null || value == DependencyProperty.UnsetValue)
            {
                return Visibility.Visible;
            }

            if (((bool)value) == true)
            {
                return Visibility.Collapsed;
            }
            else
            {
                return Visibility.Visible;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {

            if (value == null || value == DependencyProperty.UnsetValue)
            {
                return false;
            }

            if ((Visibility)value == Visibility.Collapsed)
            {
                return true;
            }
            else
            {
                return false;
            }

        }
    }
}
