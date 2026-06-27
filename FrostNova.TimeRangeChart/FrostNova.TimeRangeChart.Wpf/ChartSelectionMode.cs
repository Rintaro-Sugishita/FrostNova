namespace FrostNova.TimeRangeChart.Wpf;

/// <summary>
/// Defines how the selection frame is displayed in the chart background.
/// </summary>
public enum ChartSelectionMode
{
    /// <summary>Background clicks are ignored; no selection frame is shown.</summary>
    None,

    /// <summary>The selection frame covers one ScaleIncrement width at the clicked time within the selected row.</summary>
    DateOnly,

    /// <summary>The selection frame spans the full width of the selected row.</summary>
    FullRow
}
