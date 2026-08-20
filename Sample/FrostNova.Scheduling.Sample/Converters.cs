using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace FrostNova.Scheduling.Sample;

[ValueConversion(typeof(int), typeof(Brush))]
public class CountToForegroundConverter : IValueConverter
{
    private static readonly Brush Zero = new SolidColorBrush(Color.FromRgb(180, 180, 180));
    private static readonly Brush Positive = Brushes.Black;

    static CountToForegroundConverter()
    {
        ((SolidColorBrush)Zero).Freeze();
    }

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is int n && n > 0 ? Positive : Zero;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>実行中フラグを「操作可能か」に反転する（実行中はボタンを無効化する用途）。</summary>
[ValueConversion(typeof(bool), typeof(bool))]
public class InverseBoolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is not bool b || !b;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => value is not bool b || !b;
}
