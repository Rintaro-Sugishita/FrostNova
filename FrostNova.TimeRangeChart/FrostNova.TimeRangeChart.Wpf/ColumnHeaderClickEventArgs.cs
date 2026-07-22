using System;
using System.Windows;

namespace FrostNova.TimeRangeChart.Wpf;

/// <summary>
/// 列ヘッダ（時間軸ヘッダ）がクリックされたときのイベント引数。
/// クリックされた列（または上段サマリのグループ）が表す時刻と、その範囲を渡す。
/// 「その日付の範囲を全選択する」といった用途を想定。
/// </summary>
public class ColumnHeaderClickEventArgs : RoutedEventArgs
{
    /// <summary>クリックされた列（サマリ段の場合はグループ先頭）が表す時刻。</summary>
    public DateTime ClickedDateTime { get; }

    /// <summary>クリック範囲の開始（この時刻以上）。<see cref="ClickedDateTime"/> と同じ。</summary>
    public DateTime RangeStart { get; }

    /// <summary>クリック範囲の終了（この時刻未満）。下段は1目盛り分、上段サマリはグループ全体の範囲。</summary>
    public DateTime RangeEnd { get; }

    /// <summary>上段（サマリ）段がクリックされたときは true、下段（各目盛り）は false。</summary>
    public bool IsSummaryTier { get; }

    public ColumnHeaderClickEventArgs(
        RoutedEvent routedEvent, object source,
        DateTime clickedDateTime, DateTime rangeStart, DateTime rangeEnd, bool isSummaryTier)
        : base(routedEvent, source)
    {
        ClickedDateTime = clickedDateTime;
        RangeStart = rangeStart;
        RangeEnd = rangeEnd;
        IsSummaryTier = isSummaryTier;
    }
}
