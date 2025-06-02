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
    [ValueConversion(typeof(Enum), typeof(bool))]
    public class EnumFlagsBoolConverter : IValueConverter
    {

        private Enum? target;

        private Type _PropertyType = typeof(Enum);
        public Type PropertyType
        {
            get { return _PropertyType; }
            set { _PropertyType = value; }
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            Enum mask = (Enum)parameter;
            this.target = (Enum)value;

            return target.HasFlag(mask);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (this.target == null) { return Binding.DoNothing; }

            int targetInt = ConvertHelper.ConvToInt(target);

            this.target = (Enum) Enum.ToObject(targetType, targetInt ^ (int)parameter);

            return this.target;
        }

    }
}
