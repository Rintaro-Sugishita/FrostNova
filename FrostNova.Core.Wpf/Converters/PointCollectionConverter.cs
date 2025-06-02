using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;
using System.Windows.Media;

namespace FrostNova.Core.Wpf.Converters
{
    [ValueConversion(typeof(ObservableCollection<Point>), typeof(PointCollection))]
    public class PointCollectionConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var collection = new PointCollection();
            if (value is ObservableCollection<Point>  points)
            {
                foreach (var item in points)
                {
                    collection.Add(item);
                }
            }
            return collection;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
