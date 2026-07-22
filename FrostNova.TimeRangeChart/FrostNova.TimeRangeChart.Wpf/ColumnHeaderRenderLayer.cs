using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using FrostNova.TimeRangeChart.TimeRangeHelpers;

namespace FrostNova.TimeRangeChart.Wpf;

/// <summary>
/// Represents the visual layer responsible for rendering column headers in a time range chart control.
/// </summary>
/// <remarks>This element renders the time-axis header above the chart. When
/// <see cref="GridTimeRangeChart.ShowSummaryHeader"/> is enabled it draws two tiers:
/// a lower tier with one label per tick (the existing behavior), and an upper "summary" tier that groups
/// adjacent ticks sharing the same coarse label (e.g. year/month) and pins that label to the left edge of
/// the viewport while the group is visible (sticky), so the current position stays visible even after the
/// axis wraps a full unit (a year for a day-scale, a day for an hour-scale).</remarks>
public class ColumnHeaderRenderLayer : FrameworkElement
{
    public GridTimeRangeChart? ParentChart
    {
        get; internal set;
    }

    private double SummaryHeight =>
        (ParentChart != null && ParentChart.ShowSummaryHeader) ? Math.Max(0, ParentChart.SummaryHeaderHeight) : 0;

    private double BaseHeight =>
        ParentChart != null ? Math.Max(0, ParentChart.ColumnHeaderHeight) : 20;

    protected override void OnRender(DrawingContext drawingContext)
    {
        base.OnRender(drawingContext);

        if (ParentChart == null || ParentChart._chart == null) return;

        var borderPen = new Pen(ParentChart.RuledLineFill, ParentChart.RuledLineThickness);
        borderPen.Freeze();

        var ticks = ParentChart._chart.TimeTicks;
        if (ticks == null || ticks.Count == 0) return;

        double summaryH = SummaryHeight;
        double baseH = BaseHeight;

        // ヘッダ全域をヒットテスト可能にする（列ヘッダクリックイベント用）。
        drawingContext.DrawRectangle(Brushes.Transparent, null, new Rect(RenderSize));

        // 下段：各目盛りのラベル（従来動作）。列幅は隣接目盛りの LeftPoint 差で求める。
        for (int i = 0; i < ticks.Count - 1; i++)
        {
            if (ticks[i] is not RuledLineData t) continue;
            double colWidth = ticks[i + 1].LeftPoint - t.LeftPoint;
            var band = new Rect(t.LeftPoint, summaryH, Math.Max(0, colWidth), baseH);
            DrawCellText(drawingContext, t.FormattedText, band);
            drawingContext.DrawRectangle(Brushes.Transparent, borderPen, band);
        }

        // 上段：粗単位でグループ化し、ラベルを左端スティッキーで描画。
        if (summaryH > 0)
        {
            DrawSummaryTier(drawingContext, ticks, summaryH, borderPen);
        }
    }

    private void DrawSummaryTier(DrawingContext dc, IList<IRuledLine> ticks, double summaryH, Pen borderPen)
    {
        int n = ticks.Count;
        if (n < 2) return;

        string fmt = GetEffectiveSummaryFormat();
        double offset = GetHorizontalOffset();
        var tf = new Typeface(ParentChart!.FontFamily, ParentChart.FontStyle, ParentChart.FontWeight, ParentChart.FontStretch);
        double dpi = VisualTreeHelper.GetDpi(this).PixelsPerDip;
        Brush fg = ParentChart.SummaryHeaderForeground ?? Brushes.Black;

        // 描画対象の列は 0..n-2。最後の目盛り(n-1)は軸右端の境界として使う。
        int groupStart = 0;
        string groupKey = Key(ticks[0].DateValue, fmt);
        for (int col = 1; col <= n - 1; col++)
        {
            bool isEnd = (col == n - 1);
            string? key = isEnd ? null : Key(ticks[col].DateValue, fmt);
            if (isEnd || key != groupKey)
            {
                double groupLeft = ticks[groupStart].LeftPoint;
                double groupRight = ticks[col].LeftPoint;
                DrawSummaryGroup(dc, groupKey, groupLeft, groupRight, summaryH, offset, tf, dpi, fg, borderPen);
                groupStart = col;
                groupKey = key!;
            }
        }
    }

    private void DrawSummaryGroup(DrawingContext dc, string label, double groupLeft, double groupRight,
        double summaryH, double offset, Typeface tf, double dpi, Brush fg, Pen borderPen)
    {
        double groupWidth = Math.Max(0, groupRight - groupLeft);

        // グループの枠（上段の帯）
        dc.DrawRectangle(Brushes.Transparent, borderPen, new Rect(groupLeft, 0, groupWidth, summaryH));

        if (string.IsNullOrEmpty(label) || groupWidth <= 0) return;

        var ft = new FormattedText(
            label, CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
            tf, ParentChart!.FontSize, fg, dpi)
        {
            MaxTextWidth = groupWidth,
            MaxLineCount = 1,
            Trimming = TextTrimming.CharacterEllipsis,
        };

        const double pad = 4;
        double y = (summaryH - ft.Height) / 2;

        // スティッキー：表示中は左端に寄せる。ただしグループ範囲からははみ出さない。
        double x = Math.Max(groupLeft + pad, offset + pad);
        double maxX = groupRight - ft.Width - pad;
        if (x > maxX) x = maxX;
        if (x < groupLeft + pad) x = groupLeft + pad;

        dc.DrawText(ft, new Point(x, y));
    }

    private void DrawCellText(DrawingContext dc, FormattedText? text, Rect band)
    {
        if (text == null) return;
        double x = band.X + band.Width / 2 - text.Width / 2;
        double y = band.Y + band.Height / 2 - text.Height / 2;
        dc.DrawText(text, new Point(x, y));
    }

    private string GetEffectiveSummaryFormat()
    {
        var f = ParentChart!.SummaryHeaderFormat;
        if (!string.IsNullOrEmpty(f)) return f;

        if (ParentChart.TimeUnit == TimeUnitType.Month) return "yyyy";
        if (ParentChart.ScaleIncrement >= TimeSpan.FromDays(1)) return "yyyy/MM";
        return "yyyy/MM/dd";
    }

    private static string Key(DateTime d, string fmt) => d.ToString(fmt, CultureInfo.CurrentCulture);

    // 列ヘッダ ScrollViewer の横スクロール量。レイヤーはこの ScrollViewer によって -offset だけ
    // 平行移動されているため、content 座標 x=offset に描くと画面左端に貼り付く。
    private double GetHorizontalOffset()
    {
        DependencyObject? p = VisualTreeHelper.GetParent(this);
        while (p != null)
        {
            if (p is ScrollViewer sv) return sv.HorizontalOffset;
            p = VisualTreeHelper.GetParent(p);
        }
        return 0;
    }

    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonDown(e);
        if (ParentChart?._chart?.TimeTicks is not { Count: >= 2 } ticks) return;

        var pos = e.GetPosition(this);
        bool isSummary = ParentChart.ShowSummaryHeader && pos.Y < SummaryHeight;

        if (TryGetClickRange(ticks, pos.X, isSummary, out var clicked, out var rangeStart, out var rangeEnd))
        {
            ParentChart.RaiseColumnHeaderClick(clicked, rangeStart, rangeEnd, isSummary);
            e.Handled = true;
        }
    }

    // クリック位置 x（content座標）を、下段=1目盛り／上段=同一サマリキーの連続グループ の時刻範囲に変換する。
    private bool TryGetClickRange(IList<IRuledLine> ticks, double x, bool summary,
        out DateTime clicked, out DateTime rangeStart, out DateTime rangeEnd)
    {
        clicked = rangeStart = rangeEnd = default;
        int n = ticks.Count;
        int col = FindColumnIndex(ticks, x);   // 0..n-2
        if (col < 0) return false;

        if (!summary)
        {
            clicked = rangeStart = ticks[col].DateValue;
            rangeEnd = ticks[col + 1].DateValue;
            return true;
        }

        // 上段：クリックした列と同じサマリキーの連続範囲へ拡張。
        string fmt = GetEffectiveSummaryFormat();
        string key = Key(ticks[col].DateValue, fmt);
        int first = col, last = col;
        while (first - 1 >= 0 && Key(ticks[first - 1].DateValue, fmt) == key) first--;
        while (last + 1 <= n - 2 && Key(ticks[last + 1].DateValue, fmt) == key) last++;

        clicked = rangeStart = ticks[first].DateValue;
        rangeEnd = ticks[last + 1].DateValue;
        return true;
    }

    // クリック位置 x を含む列インデックス（ticks[i].LeftPoint <= x < ticks[i+1].LeftPoint）を返す。0..n-2 にクランプ。
    private static int FindColumnIndex(IList<IRuledLine> ticks, double x)
    {
        int n = ticks.Count;
        if (n < 2) return -1;
        if (x <= ticks[0].LeftPoint) return 0;
        if (x >= ticks[n - 1].LeftPoint) return n - 2;

        int lo = 0, hi = n - 1;
        while (lo <= hi)
        {
            int mid = (lo + hi) / 2;
            if (ticks[mid].LeftPoint <= x) lo = mid + 1;
            else hi = mid - 1;
        }
        // hi = x 以下で最大の LeftPoint を持つインデックス
        return Math.Max(0, Math.Min(hi, n - 2));
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        if (ParentChart == null || ParentChart._chart == null)
        {
            return base.MeasureOverride(availableSize);
        }

        double width = ParentChart._chart.ContentWidth;
        if (double.IsNaN(width) || double.IsInfinity(width) || width <= 0)
        {
            width = availableSize.Width;
        }
        if (double.IsNaN(width) || double.IsInfinity(width) || width <= 0)
        {
            width = 0;
        }

        double height = SummaryHeight + BaseHeight;
        if (double.IsNaN(height) || double.IsInfinity(height) || height <= 0)
            height = 0;

        return new Size(width, height);
    }

}
