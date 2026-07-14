using System.Collections;
using System.Windows;
using System.Windows.Controls;

namespace FrostNova.TimeRangeChart.Wpf;

/// <summary>
/// Renders the footer row of a GridTimeRangeChart using a consumer-supplied DataTemplate,
/// one cell per column (matching the time axis divisions).
/// </summary>
public class FooterRenderLayer : ItemsControl
{
    public GridTimeRangeChart? ParentChart { get; internal set; }

    protected override void PrepareContainerForItemOverride(DependencyObject element, object item)
    {
        base.PrepareContainerForItemOverride(element, item);
        if (element is FrameworkElement fe && ParentChart != null)
            fe.Width = ParentChart.WidthPerRuledLine;
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        double contentWidth = ParentChart?._chart?.ContentWidth ?? 0;
        if (double.IsNaN(contentWidth) || double.IsInfinity(contentWidth) || contentWidth <= 0)
            contentWidth = availableSize.Width;

        // データがほぼ無いと ContentWidth<=0 で availableSize.Width にフォールバックするが、
        // これが Infinity(初期測定や無限幅コンテナ)だと DesiredSize に Infinity を返して
        // WPF が例外を投げるため 0 に丸める。
        if (double.IsNaN(contentWidth) || double.IsInfinity(contentWidth))
            contentWidth = 0;

        // Measure children so they lay out correctly, but always return ContentWidth
        // to ensure this control's ScrollableWidth matches the chart's ScrollableWidth exactly.
        Size baseSize = base.MeasureOverride(new Size(contentWidth, availableSize.Height));

        // 高さも Infinity のまま返すと例外になるので、その場合は子要素の希望高さを使う。
        double height = availableSize.Height;
        if (double.IsNaN(height) || double.IsInfinity(height))
            height = baseSize.Height;

        return new Size(contentWidth, height);
    }
}
