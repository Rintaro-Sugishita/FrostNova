namespace FrostNova.TimeRangeChart.Wpf;

/// <summary>
/// Passed as CommandParameter when a chart-area context menu item is invoked.
/// Contains the row item and the date/time at the point where the user clicked.
/// </summary>
public class ChartContextMenuArgs
{
    public object? RowItem { get; init; }
    public DateTime ClickedDateTime { get; init; }
    public int RowIndex { get; init; }
}
