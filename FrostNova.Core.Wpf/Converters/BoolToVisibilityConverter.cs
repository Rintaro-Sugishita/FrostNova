using FrostNova.Core.TypeToolkit;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace FrostNova.Core.Wpf.Converters
{
    [ValueConversion(typeof(bool), typeof(Visibility))]
    public class BoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            int invert = ConvertHelper.ConvToInt(parameter);

            if (value == null)
            {
                return invert <0 ? Visibility.Visible : Visibility.Collapsed;
            }

            if (value == DependencyProperty.UnsetValue)
            {
                return invert < 0 ? Visibility.Visible : Visibility.Collapsed;
            }
            if (value is bool boolValue)
            {
                if (boolValue == true)
                {
                    return invert < 0 ? Visibility.Collapsed : Visibility.Visible;
                }
                else
                {
                    return invert < 0 ? Visibility.Visible : Visibility.Collapsed;
                }

            }
            return invert < 0 ? Visibility.Visible : Visibility.Collapsed;

        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            int invert = ConvertHelper.ConvToInt(parameter);

            if (value == null || value == DependencyProperty.UnsetValue)
            {
                return invert>=0;
            }
            if (value is Visibility visibility)
            {
                if (visibility == Visibility.Collapsed)
                {
                    return invert < 0;
                }
                else
                {
                    return invert >= 0;
                }
            }
            return invert >= 0;

        }
    }
}
