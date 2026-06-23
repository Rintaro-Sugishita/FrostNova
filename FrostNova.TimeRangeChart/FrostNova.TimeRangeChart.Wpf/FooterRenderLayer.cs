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

        // Measure children so they lay out correctly, but always return ContentWidth
        // to ensure this control's ScrollableWidth matches the chart's ScrollableWidth exactly.
        base.MeasureOverride(new Size(contentWidth, availableSize.Height));
        return new Size(contentWidth, availableSize.Height);
    }
}
