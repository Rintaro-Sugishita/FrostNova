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

/// <summary>
/// 比率（0..1）とコンテナ幅から、積み上げバー 1 セグメントのピクセル幅を求める。
/// WPF の Grid は ColumnDefinition をコレクションにバインドできないため、
/// 横並び StackPanel の各要素の Width をこの変換で決めて比例配分を実現している。
/// </summary>
public class FractionToWidthConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length < 2 || values[0] is not double fraction || values[1] is not double totalWidth)
            return 0d;
        if (double.IsNaN(fraction) || double.IsNaN(totalWidth) || totalWidth <= 0) return 0d;

        // 0 ではないのに幅 0 で消えてしまうと「一瞬で終わったフェーズ」が見えなくなるので、
        // 比率が正なら最低 2px は確保する。
        var w = fraction * totalWidth;
        return fraction > 0 ? Math.Max(2d, w) : 0d;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
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
