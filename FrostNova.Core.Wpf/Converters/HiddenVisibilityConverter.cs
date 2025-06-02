using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows;
using FrostNova.Core.TypeToolkit;

namespace FrostNova.Core.Wpf.Converters
{
    public class HiddenVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            int invert = ConvertHelper.ConvToInt(parameter);

            if (value == null)
            {
                return invert < 0 ? Visibility.Visible : Visibility.Hidden;
            }

            if (value == DependencyProperty.UnsetValue)
            {
                return invert < 0 ? Visibility.Visible : Visibility.Hidden;
            }

            if (((bool)value) == true)
            {
                return invert < 0 ? Visibility.Hidden : Visibility.Visible;
            }
            else
            {
                return invert < 0 ? Visibility.Visible : Visibility.Hidden;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            int invert = ConvertHelper.ConvToInt(parameter);

            if (value == null || value == DependencyProperty.UnsetValue)
            {
                return invert >= 0;
            }

            if ((Visibility)value == Visibility.Hidden)
            {
                return invert < 0;
            }
            else
            {
                return invert >= 0;
            }

        }
    }
}
