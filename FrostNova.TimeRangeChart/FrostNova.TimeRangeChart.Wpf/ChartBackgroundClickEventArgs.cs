using System.Windows;

namespace FrostNova.TimeRangeChart.Wpf;

internal class ChartBackgroundClickEventArgs : RoutedEventArgs
{
    public int RowIndex { get; }
    public DateTime ClickedDateTime { get; }

    public ChartBackgroundClickEventArgs(RoutedEvent routedEvent, int rowIndex, DateTime clickedDateTime)
        : base(routedEvent)
    {
        RowIndex = rowIndex;
        ClickedDateTime = clickedDateTime;
    }
}
