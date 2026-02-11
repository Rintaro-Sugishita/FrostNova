using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using FrostNova.Core.Wpf.VisualHelpers;
using FrostNova.TimeRangeChart.TimeRangeHelpers;
using FrostNova.TimeRangeChart.Wpf.TimeRangeHelpers;

namespace FrostNova.TimeRangeChart.Wpf;

/// <summary>
/// このカスタム コントロールを XAML ファイルで使用するには、手順 1a または 1b の後、手順 2 に従います。
///
/// 手順 1a) 現在のプロジェクトに存在する XAML ファイルでこのカスタム コントロールを使用する場合
/// この XmlNamespace 属性を使用場所であるマークアップ ファイルのルート要素に
/// 追加します:
///
///     xmlns:MyNamespace="clr-namespace:TimeRangeChart"
///
///
/// 手順 1b) 異なるプロジェクトに存在する XAML ファイルでこのカスタム コントロールを使用する場合
/// この XmlNamespace 属性を使用場所であるマークアップ ファイルのルート要素に
/// 追加します:
///
///     xmlns:MyNamespace="clr-namespace:TimeRangeChart;assembly=TimeRangeChart"
///
/// また、XAML ファイルのあるプロジェクトからこのプロジェクトへのプロジェクト参照を追加し、
/// リビルドして、コンパイル エラーを防ぐ必要があります:
///
///     ソリューション エクスプローラーで対象のプロジェクトを右クリックし、
///     [参照の追加] の [プロジェクト] を選択してから、このプロジェクトを参照し、選択します。
///
///
/// 手順 2)
/// コントロールを XAML ファイルで使用します。
///
///     <MyNamespace:TimeRangeChart/>
///
/// </summary>
public class TimeRangeChart : Control
{
    static TimeRangeChart()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(TimeRangeChart), new FrameworkPropertyMetadata(typeof(TimeRangeChart)));
    }

    public TimeRangeChart()
    {
        TimeToPointFunc = TimeToPoint;
        PointToTimeFunc = PointToTime;
        GetEndTimeFunc = GetEndTime;
        GetWorkingSpanFunc = GetWorkingSpan;
        TaskClickAction = OnTaskClicked;
        TaskDragStartAction = OnTaskDragStart;
        TaskDragEndAction = OnTaskDragEnd;
        if (TimeTicks == null)
        {
            //TimeTicks = new ObservableCollection<IRuledLine>();
            SortedNonWorkingTimes = [];

        }
        _ruledLinePen = GetRuledLinePen();
    }

    public Func<DateTime, double> TimeToPointFunc { get; private set; }
    public Func<double, TimeSpan?, DateTime> PointToTimeFunc { get; private set; }

    public Func<DateTime, TimeSpan, DateTime> GetEndTimeFunc { get; private set; }
    public Func<DateTime, DateTime, TimeSpan> GetWorkingSpanFunc { get; private set; }

    public Action<TimeRangeControl> TaskClickAction { get; private set; }

    public Action<TimeRangeControl, DragDropRangeEventArgs> TaskDragStartAction { get; private set; }
    public Action<TimeRangeControl, DragDropRangeEventArgs> TaskDragEndAction { get; private set; }

    public event EventHandler<EventArgs>? TimeRangeNeedRefresh;



    private Pen _ruledLinePen;


    /// <summary>
    /// string
    /// </summary>
    public GridTimeRangeChart ParentGridChart
    {
        get { return (GridTimeRangeChart)GetValue(ParentGridChartProperty); }
        set { SetValue(ParentGridChartProperty, value); }
    }
    public static DependencyProperty ParentGridChartProperty = DependencyProperty.Register(nameof(ParentGridChart), typeof(GridTimeRangeChart), typeof(TimeRangeChart));


    public DateTime StartTime
    {
        get { return (DateTime)GetValue(StartTimeProperty); }
        set { SetValue(StartTimeProperty, value); }
    }
    public static readonly DependencyProperty StartTimeProperty = DependencyProperty.Register(nameof(StartTime), typeof(DateTime), typeof(TimeRangeChart), new FrameworkPropertyMetadata(DateTime.Now.AddDays(-3), FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnMyPropertyChanged));

    public DateTime EndTime
    {
        get { return (DateTime)GetValue(EndTimeProperty); }
        set { SetValue(EndTimeProperty, value); }
    }
    public static readonly DependencyProperty EndTimeProperty = DependencyProperty.Register(nameof(EndTime), typeof(DateTime), typeof(TimeRangeChart), new FrameworkPropertyMetadata(DateTime.Now.AddDays(9), FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnMyPropertyChanged));


    public IList ItemsSource
    {
        get { return (IList)GetValue(ItemsSourceProperty); }
        set { SetValue(ItemsSourceProperty, value); }
    }
    public static readonly DependencyProperty ItemsSourceProperty = DependencyProperty.Register(nameof(ItemsSource), typeof(IList), typeof(TimeRangeChart));

    /// <summary>
    /// Height
    /// </summary>
    public double ChartHeight
    {
        get { return (double)GetValue(ChartHeightProperty); }
        set { SetValue(ChartHeightProperty, value); }
    }
    public static DependencyProperty ChartHeightProperty = DependencyProperty.Register(nameof(ChartHeight), typeof(double), typeof(TimeRangeChart), new PropertyMetadata(double.NaN));


    public TimeSpan ScaleIncrement
    {
        get { return (TimeSpan)GetValue(ScaleIncrementProperty); }
        set
        {
            SetValue(ScaleIncrementProperty, value);
            RefreshCall();
        }
    }
    public static readonly DependencyProperty ScaleIncrementProperty = DependencyProperty.Register(nameof(ScaleIncrement), typeof(TimeSpan), typeof(TimeRangeChart), new PropertyMetadata(new TimeSpan(1, 0, 0), OnMyPropertyChanged));

    /// <summary>
    /// string
    /// </summary>
    public TimeSpan SubScaleIncrement
    {
        get { return (TimeSpan)GetValue(SubScaleIncrementProperty); }
        set { SetValue(SubScaleIncrementProperty, value); }
    }
    public static DependencyProperty SubScaleIncrementProperty = DependencyProperty.Register(nameof(SubScaleIncrement), typeof(TimeSpan), typeof(TimeRangeChart), new PropertyMetadata(new TimeSpan(1, 0, 0), OnMyPropertyChanged));


    /// <summary>
    /// min span of range
    /// </summary>
    public TimeSpan MinSpan
    {
        get { return (TimeSpan)GetValue(MinSpanProperty); }
        set { SetValue(MinSpanProperty, value); }
    }
    public static readonly DependencyProperty MinSpanProperty = DependencyProperty.Register(nameof(MinSpan), typeof(TimeSpan), typeof(TimeRangeChart), new PropertyMetadata(TimeSpan.FromMinutes(15)));


    public Brush RuledLineFill
    {
        get { return (Brush)GetValue(RuledLineFillProperty); }
        set { SetValue(RuledLineFillProperty, value); RefreshCall(); }
    }
    public static DependencyProperty RuledLineFillProperty = DependencyProperty.Register(nameof(RuledLineFill), typeof(Brush), typeof(TimeRangeChart), new PropertyMetadata(new SolidColorBrush(Color.FromArgb(255, 255, 255, 255)), OnRuledLinePropertyChanged));

    public double RuledLineThickness
    {
        get { return (double)GetValue(RuledLineThicknessProperty); }
        set { SetValue(RuledLineThicknessProperty, value); RefreshCall(); }
    }
    public static DependencyProperty RuledLineThicknessProperty = DependencyProperty.Register(nameof(RuledLineThickness), typeof(double), typeof(TimeRangeChart), new PropertyMetadata(1.0, OnRuledLinePropertyChanged));

    /// <summary>
    /// background of non-working ranges
    /// </summary>
    public Brush NonWorkingRangeBackground
    {
        get { return (Brush)GetValue(NonWorkingRangeBackgroundProperty); }
        set { SetValue(NonWorkingRangeBackgroundProperty, value); }
    }
    public static DependencyProperty NonWorkingRangeBackgroundProperty = DependencyProperty.Register(nameof(NonWorkingRangeBackground), typeof(Brush), typeof(TimeRangeChart), new PropertyMetadata(new SolidColorBrush(Color.FromRgb(0xd8, 0xd8, 0xd8))));


    private static void OnRuledLinePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is TimeRangeChart chart)
        {
            chart._ruledLinePen = chart.GetRuledLinePen();
            OnMyPropertyChanged(d, e);
        }
    }

    private Pen GetRuledLinePen()
    {
        var brush = RuledLineFill;
        brush.Freeze();
        var thickness = RuledLineThickness;

        var pen = new Pen(brush, thickness);
        pen.Freeze();

        return pen;

    }


    private Rectangle _borderRect = new Rectangle();

    public IList<ITimeRange> NonWorkingTimes
    {
        get { return (IList<ITimeRange>)GetValue(NonWorkingTimesProperty); }
        set { SetValue(NonWorkingTimesProperty, value); }
    }
    public static DependencyProperty NonWorkingTimesProperty = DependencyProperty.Register(nameof(NonWorkingTimes), typeof(IList<ITimeRange>), typeof(TimeRangeChart), new PropertyMetadata(OnNonWorkingTimesChanged));

    private static void OnNonWorkingTimesChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is TimeRangeChart chart)
        {
            // ① 非稼働時間をソート＆マージ
            //var sorted = chart.NonWorkingTimes
            //    .OrderBy(r => r.StartTime)
            //    .ToList();

            //var merged = new List<ITimeRange>();
            //foreach (var r in sorted)
            //{
            //    if (merged.Count == 0 || merged.Last().EndTime < r.StartTime)
            //    {
            //        merged.Add(new FixedTimeRange(r.StartTime, r.EndTime));
            //    }
            //    else
            //    {
            //        var last = merged[merged.Count - 1];
            //        merged[merged.Count - 1] = new FixedTimeRange(last.StartTime, Max(last.EndTime, r.EndTime));
            //    }
            //}

            //chart.SortedNonWorkingTimes = merged;

            chart.CalculateSortedNonWorkingTimes();

            if (e.OldValue is ObservableCollection<ITimeRange> oldObsTimes)
            {
                if (chart._nonworkingTimesCollectionChangedHandler != null)
                {
                    oldObsTimes.CollectionChanged -= chart._nonworkingTimesCollectionChangedHandler;
                }
            }

            if (e.NewValue is ObservableCollection<ITimeRange> obsTimes)
            {
                chart._nonworkingTimesCollectionChangedHandler = (s, ev) =>
                {
                    chart.CalculateSortedNonWorkingTimes();
                    chart.RefreshCall();
                };
                obsTimes.CollectionChanged += chart._nonworkingTimesCollectionChangedHandler;
            }
        }
    }

    private NotifyCollectionChangedEventHandler? _nonworkingTimesCollectionChangedHandler;

    private void CalculateSortedNonWorkingTimes()
    {
        // ① 非稼働時間をソート＆マージ
        var sorted = NonWorkingTimes
            .OrderBy(r => r.StartTime)
            .ToList();
        var merged = new List<ITimeRange>();
        foreach (var r in sorted)
        {
            if (merged.Count == 0 || merged.Last().EndTime < r.StartTime)
            {
                merged.Add(new FixedTimeRange(r.StartTime, r.EndTime));
            }
            else
            {
                var last = merged[merged.Count - 1];
                merged[merged.Count - 1] = new FixedTimeRange(last.StartTime, Max(last.EndTime, r.EndTime));
            }
        }
        static DateTime Max(DateTime a, DateTime b) => a > b ? a : b;
        SortedNonWorkingTimes = merged;
    }

    /// <summary>
    /// time
    /// </summary>
    public IList<ITimeRange> SortedNonWorkingTimes
    {
        get { return (IList<ITimeRange>)GetValue(SortedNonWorkingTimesProperty); }
        internal set { SetValue(SortedNonWorkingTimesProperty, value); }
    }
    public static DependencyProperty SortedNonWorkingTimesProperty = DependencyProperty.Register(nameof(SortedNonWorkingTimes), typeof(IList<ITimeRange>), typeof(TimeRangeChart));


    /// <summary>
    /// string
    /// </summary>
    public IList<IBackgroundTimeRange> BackgroundTimes
    {
        get { return (IList<IBackgroundTimeRange>)GetValue(BackgroundTimesProperty); }
        set { SetValue(BackgroundTimesProperty, value); }
    }
    public static DependencyProperty BackgroundTimesProperty = DependencyProperty.Register(nameof(BackgroundTimes), typeof(IList<IBackgroundTimeRange>), typeof(TimeRangeChart));


    /// <summary>
    /// string
    /// </summary>
    public DataTemplate TimeRangeTemplate
    {
        get { return (DataTemplate)GetValue(TimeRangeTemplateProperty); }
        set { SetValue(TimeRangeTemplateProperty, value); }
    }
    public static DependencyProperty TimeRangeTemplateProperty = DependencyProperty.Register(nameof(TimeRangeTemplate), typeof(DataTemplate), typeof(TimeRangeChart));


    public Style TimeRangeStyle
    {
        get { return (Style)GetValue(TimeRangeStyleProperty); }
        set { SetValue(TimeRangeStyleProperty, value); }
    }
    public static DependencyProperty TimeRangeStyleProperty = DependencyProperty.Register(nameof(TimeRangeStyle), typeof(Style), typeof(TimeRangeChart), new PropertyMetadata(null));

    /// <summary>
    /// width per ruled line
    /// </summary>
    public double WidthPerRuledLine
    {
        get { return (double)GetValue(WidthPerRuledLineProperty); }
        set { SetValue(WidthPerRuledLineProperty, value); }
    }
    public static DependencyProperty WidthPerRuledLineProperty = DependencyProperty.Register(nameof(WidthPerRuledLine), typeof(double), typeof(TimeRangeChart), new PropertyMetadata(double.NaN, OnWidthPerIncrementChanged));

    private static void OnWidthPerIncrementChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is TimeRangeChart chart)
        {
            chart.RefreshCall();
        }
    }


    /// <summary>
    /// Whether to control tick self
    /// </summary>
    public bool IsControlTick
    {
        get { return (bool)GetValue(IsTickControlProperty); }
        internal set { SetValue(IsTickControlProperty, value); }
    }
    public static DependencyProperty IsTickControlProperty = DependencyProperty.Register(nameof(IsControlTick), typeof(bool), typeof(TimeRangeChart), new PropertyMetadata(true));
    public string SubScaleFormat
    {
        get { return (string)GetValue(SubScaleFormatProperty); }
        set { SetValue(SubScaleFormatProperty, value); }
    }
    public static DependencyProperty SubScaleFormatProperty = DependencyProperty.Register(nameof(SubScaleFormat), typeof(string), typeof(TimeRangeChart));


    public List<IRuledLine> TimeTicks
    {
        get { return (List<IRuledLine>)GetValue(TimeTicksProperty); }
        internal set { SetValue(TimeTicksProperty, value); }
    }
    public static DependencyProperty TimeTicksProperty = DependencyProperty.Register(nameof(TimeTicks), typeof(List<IRuledLine>), typeof(TimeRangeChart));

    public List<IRuledLine> SubTimeTicks
    {
        get { return (List<IRuledLine>)GetValue(SubTimeTicksProperty); }
        set { SetValue(SubTimeTicksProperty, value); }
    }
    public static DependencyProperty SubTimeTicksProperty = DependencyProperty.Register(nameof(SubTimeTicks), typeof(List<IRuledLine>), typeof(TimeRangeChart));

    /// <summary>
    /// Gets or sets a value indicating whether range bars can be stacked in the chart.
    /// </summary>
    public bool CanStackRange
    {
        get { return (bool)GetValue(CanStackRangeProperty); }
        set { SetValue(CanStackRangeProperty, value); }
    }
    public static DependencyProperty CanStackRangeProperty = DependencyProperty.Register(nameof(CanStackRange), typeof(bool), typeof(TimeRangeChart), new PropertyMetadata(false));



    private static void OnMyPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        TimeRangeChart customControl = (TimeRangeChart)d;
        // 自身の再描画をトリガーする
        customControl.RefreshCall();
    }


    /// <summary>
    /// string
    /// </summary>
    public TimeUnitType TimeUnit
    {
        get { return (TimeUnitType)GetValue(TimeUnitProperty); }
        set { SetValue(TimeUnitProperty, value); }
    }
    public static DependencyProperty TimeUnitProperty = DependencyProperty.Register(nameof(TimeUnit), typeof(TimeUnitType), typeof(TimeRangeChart), new PropertyMetadata(TimeUnitType.Normal, OnTimeUnitChanged));

    private static void OnTimeUnitChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        TimeRangeChart customControl = (TimeRangeChart)d;
        // 自身の再描画をトリガーする
        customControl.RefreshCall();
    }

    /// <summary>
    /// string
    /// </summary>
    public int MonthSpan
    {
        get { return (int)GetValue(MonthSpanProperty); }
        set { SetValue(MonthSpanProperty, value); }
    }
    public static DependencyProperty MonthSpanProperty = DependencyProperty.Register(nameof(MonthSpan), typeof(int), typeof(TimeRangeChart), new PropertyMetadata(1, OnMonthSpanChanged));

    private static void OnMonthSpanChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        TimeRangeChart customControl = (TimeRangeChart)d;
        // 自身の再描画をトリガーする
        customControl.RefreshCall();
    }

    private void CalculateBorder()
    {
        if (TimeTicks == null) return;
        var ticks = TimeTicks.ToList();

        if (IsControlTick)
        {
            //自身で罫線を管理する場合は再計算する
            //そうでない場合は親から指定された値を使用する
            var newTicks = TimeUnit == TimeUnitType.Normal ?
                TimeHelper.GenerateTicks(StartTime, EndTime, ScaleIncrement, SubScaleIncrement) :
                TimeHelper.GenerateTicksMonth(StartTime, EndTime, 1, MonthSubTickType.None);

            var newWidth = CalculateNewWidth(newTicks.mainTicks.Count);

            TimeTicks.Clear();

            foreach (var item in newTicks.mainTicks)
            {
                TimeTicks.Add(new RuledLineData(this) { DateValue = item, LeftPoint = TimeToPoint(item) });
            }


            SubTimeTicks.Clear();
            if (newTicks.subTicks.Count > 0)
            {
                var tf = new Typeface(FontFamily, FontStyle, FontWeight, FontStretch);
                var subTicks = TimeRangeChart.GenerateSubScaleTicks(this, newTicks.subTicks, this.SubScaleFormat, tf, newWidth);
                SubTimeTicks.AddRange(subTicks);

            }

        }
        else
        {
            var newWidth = CalculateNewWidth(TimeTicks.Count);
            SetWidth(newWidth);

        }


    }

    private double CalculateNewWidth(int tickCount)
    {
        var newWidth = (tickCount - 1) * WidthPerRuledLine;
        if (newWidth > 0)
        {
            return newWidth;
        }
        else
        {
            return double.NaN;
        }

    }
    private void SetWidth(double newWidth)
    {
        if (newWidth == double.NaN || newWidth <= 0)
        {
            this.ClearValue(WidthProperty);

        }
        else
        {
            this.Width = newWidth;
        }
    }

    internal static List<IRuledLine> GenerateSubScaleTicks(Control dp, List<DateTime> ticks, string subScaleFormat, Typeface tf, double newWidth)
    {
        var localSubTimeTicks = new List<IRuledLine>();

        DateTime startTime;
        DateTime endTime;
        GridTimeRangeChart? gridChart = null;
        TimeRangeChart? timeChart = null;
        if (dp is GridTimeRangeChart gridChart2)
        {
            gridChart = gridChart2;
            startTime = gridChart.StartTime;
            endTime = gridChart.EndTime;
        }
        else if (dp is TimeRangeChart chart)
        {
            timeChart = chart;
            startTime = timeChart.StartTime;
            endTime = timeChart.EndTime;
        }
        else return localSubTimeTicks;

        for (int i = 0; i < ticks.Count; i++)
        {
            var item = ticks[i];
            string text = item.Date.ToString(subScaleFormat);
            var brush = Brushes.Black;
            var ft = new FormattedText(
                text,
                CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                tf,
                dp.FontSize,
                brush,
                VisualTreeHelper.GetDpi(dp).PixelsPerDip);
            var tickObject = gridChart != null ? new RuledLineData(gridChart) : new RuledLineData(timeChart!);
            tickObject.DateValue = item;

            var x = 0.0;
            if (timeChart != null)
            {
                x = timeChart.TimeToPoint(item);
            }
            else
            {
                var timeUnit = gridChart != null ? gridChart.TimeUnit : timeChart!.TimeUnit;
                x = timeUnit == TimeUnitType.Normal ?
                         TimeScreenHelper.TimeToPoint(item, startTime, endTime, newWidth) :
                         TimeScreenHelper.TimeToPointMonth(item, gridChart?.WidthPerRuledLine ?? timeChart!.WidthPerRuledLine, gridChart?.TimeTicks ?? timeChart!.TimeTicks);
            }
            tickObject.LeftPoint = x;


            tickObject.FormattedText = ft;


            localSubTimeTicks.Add(tickObject);

        }

        for (int i = 0; i < localSubTimeTicks.Count; i++)
        {
            var item = (RuledLineData)localSubTimeTicks[i];
            var rectLeft = i > 0 ? localSubTimeTicks[i - 1].LeftPoint : 0;
            var rectRight = i < localSubTimeTicks.Count - 1 ? localSubTimeTicks[i + 1].LeftPoint : newWidth;

            item.Rect = new(new Point(item.LeftPoint - (item.LeftPoint - rectLeft) / 2, 0), new Point(item.LeftPoint + (rectRight - item.LeftPoint) / 2, gridChart?.FontSize ?? timeChart!.FontSize));
        }

        return localSubTimeTicks;

    }

    private void DrawBorder(DrawingContext dc)
    {
        //if (TimeTicks == null) { TimeTicks = new ObservableCollection<IRuledLine>(); } //これをすると親のDependencyPropertyから外れる
        //実際に描画する

        //背景の描画
        dc.DrawRectangle(Background, new Pen(BorderBrush, BorderThickness.Left), new Rect(0, 0, ActualWidth, ActualHeight));

        //非稼働領域の描画
        if (SortedNonWorkingTimes?.Count > 0)
        {
            var brush = this.NonWorkingRangeBackground;
            brush.Freeze();
            //var pen = new Pen();
            //pen.Freeze();
            foreach (var item in SortedNonWorkingTimes)
            {
                dc.DrawRectangle(brush, null, new Rect(new Point(TimeToPoint(item.StartTime), 0), new Point(TimeToPoint(item.EndTime), this.ActualHeight)));
            }
        }

        //背景領域の描画
        if (BackgroundTimes?.Count > 0)
        {
            foreach (var item in BackgroundTimes)
            {
                dc.DrawRectangle(item.Background, null, new Rect(new Point(TimeToPoint(item.StartTime), 0), new Point(TimeToPoint(item.EndTime), this.ActualHeight)));
            }
        }


        //罫線の描画
        if (TimeTicks.Count > 0)
        {
            foreach (var item in TimeTicks)
            {
                dc.DrawLine(_ruledLinePen, new Point(item.LeftPoint, 0), new Point(item.LeftPoint, this.ActualHeight));
            }


            if (SubTimeTicks.Count > 0)
            {
                foreach (var item in SubTimeTicks)
                {
                    dc.DrawLine(_ruledLinePen, new Point(item.LeftPoint, 0), new Point(item.LeftPoint, this.ActualHeight * 0.08));
                    dc.DrawLine(_ruledLinePen, new Point(item.LeftPoint, this.ActualHeight * 0.92), new Point(item.LeftPoint, this.ActualHeight));
                }

            }

        }


    }


    private DateTime GetEndTime(DateTime startTime, TimeSpan workingSpan)
    {
        return TimeHelper.CalcEndTime(startTime, workingSpan, SortedNonWorkingTimes);
    }


    private TimeSpan GetWorkingSpan(DateTime startTime, DateTime endTime)
    {
        return TimeHelper.CalcWorkingSpan(startTime, endTime, SortedNonWorkingTimes);
    }




    public IList<TimeRangeControl> SelectedRanges
    {
        get { return (IList<TimeRangeControl>)GetValue(SelectedRangesProperty); }
        set { SetValue(SelectedRangesProperty, value); }
    }
    public static DependencyProperty SelectedRangesProperty = DependencyProperty.Register(nameof(SelectedRanges), typeof(IList<TimeRangeControl>), typeof(TimeRangeChart));

    public IList SelectedValues
    {
        get { return (IList)GetValue(SelectedValuesProperty); }
        set { SetValue(SelectedValuesProperty, value); }
    }
    public static DependencyProperty SelectedValuesProperty = DependencyProperty.Register(nameof(SelectedValues), typeof(IList), typeof(TimeRangeChart));

    #region Stack Control

    private const double _defaultStackVerticalMargin = 2.0;
    private const double _heightEpsilon = 0.5; // 小さな差は無視する

    private bool _isApplyingHeight = false;
    private void ApplyStackingIfNeeded()
    {
        if (!CanStackRange) return;

        // ビジュアルツリーから TimeRangeControl を収集
        var controls = GeneralHelper.FindVisualChildren<TimeRangeControl>(this).Where(x => x.CanStack).ToList();
        if (controls.Count == 0) return;

        // itemHeight: 各コントロールの Height を優先して使用（未設定時は ActualHeight、さらに未設定なら ParentGridChart.RowHeight）
        double itemHeight = 0;
        foreach (var c in controls)
        {
            double h = c.Height;
            if (double.IsNaN(h) || h <= 0) h = c.ActualHeight;
            if (double.IsNaN(h) || h <= 0) h = ParentGridChart?.RowHeight ?? 30.0;
            itemHeight = Math.Max(itemHeight, h);
        }
        if (itemHeight <= 0) itemHeight = ParentGridChart?.RowHeight ?? 30.0;

        double baseTop = 0;
        RecalculateStackLevels(controls, itemHeight, _defaultStackVerticalMargin, baseTop);

        // 必要高さを確保（見切れ防止）
        //int maxLevel = controls.Select(c => c.StackLevel).DefaultIfEmpty(0).Max();
        //double requiredHeight = baseTop + (maxLevel + 1) * (itemHeight + _defaultStackVerticalMargin);

        //if (double.IsNaN(this.Height) && requiredHeight > this.ActualHeight)
        //{
        //    this.Height = requiredHeight;
        //}
        //else 
        //{
        //    this.Height = double.NaN;
        //}
        int maxLevel = controls.Select(c => c.StackLevel).DefaultIfEmpty(0).Max();
        double requiredHeight = baseTop + (maxLevel + 1) * (itemHeight + _defaultStackVerticalMargin);

        // ループ抑止用: 自分で Height を設定中は反応しない
        if (_isApplyingHeight) return;

        double baseHeight = ChartHeight > 0 ? ChartHeight : (ParentGridChart?.RowHeight ?? 30.0);

        // 判定基準を「コンテナのActualHeight」から「本来のベース高さ」に変える
        if (requiredHeight > baseHeight + _heightEpsilon)
        {
            if (double.IsNaN(this.Height) || Math.Abs(this.Height - requiredHeight) > _heightEpsilon)
            {
                _isApplyingHeight = true;
                this.Height = requiredHeight; // 拡張が必要
                Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(() => _isApplyingHeight = false));
            }
        }
        else
        {
            // 算出した必要高がベース以下なら、Height指定を解除してベース（＝初期値）に戻れるようにする
            if (!double.IsNaN(this.Height))
            {
                _isApplyingHeight = true;
                this.ClearValue(HeightProperty); // 解除
                Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(() => _isApplyingHeight = false));
            }
        }
    }

    //private static IEnumerable<T> FindVisualChildren<T>(DependencyObject depObj) where T : DependencyObject
    //{
    //    if (depObj == null) yield break;
    //    for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
    //    {
    //        var child = VisualTreeHelper.GetChild(depObj, i);
    //        if (child is T t) yield return t;
    //        foreach (var childOfChild in FindVisualChildren<T>(child))
    //            yield return childOfChild;
    //    }
    //}
    public static void RecalculateStackLevels(IEnumerable<TimeRangeControl> ranges, double itemHeight, double verticalMargin, double baseTop = 0)
    {
        // 開始時刻でソート（StartTime を使用）
        var list = ranges
            .Where(r => r != null)
            .OrderBy(r => r.StartTime)
            .ToList();

        // 各レベルにおける直近終了時刻を保持
        var levelEnds = new List<DateTime>();

        foreach (var ctrl in list)
        {
            // find lowest level whose end <= ctrl.StartTime
            int assignedLevel = -1;
            for (int i = 0; i < levelEnds.Count; i++)
            {
                if (ctrl.StartTime >= levelEnds[i])
                {
                    assignedLevel = i;
                    levelEnds[i] = ctrl.EndTime; // 更新
                    break;
                }
            }
            if (assignedLevel == -1)
            {
                assignedLevel = levelEnds.Count;
                levelEnds.Add(ctrl.EndTime);
            }

            // 割り当て
            ctrl.StackLevel = assignedLevel;

            // Canvas 上の Top を設定（行基準の baseTop を足す）
            var extraOffset = ctrl.StackLevel == 0 ? ctrl.StackBaseTop : 0.0;
            var newTop = baseTop + assignedLevel * (itemHeight + verticalMargin) + extraOffset;
            ctrl.Top = newTop; // TimeRangeControl.Top に反映
        }
    }


    #endregion

    internal static void OnTaskClicked(TimeRangeChart chart, TimeRangeControl ctrl)
    {

        if (chart.IsControlTick)
        {
            if (chart.SelectedRanges == null)
            {
                chart.SelectedRanges = new ObservableCollection<TimeRangeControl>();
                chart.SelectedValues = new ObservableCollection<object>();
            }
        }


        if (!Keyboard.Modifiers.HasFlag(ModifierKeys.Shift) && !Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
        {
            ClearSelectRange(chart);
        }

        if (ctrl.IsSelected)
        {
            if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift) || Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
            {
                ctrl.IsSelected = false;
                chart.SelectedRanges?.Remove(ctrl);
                chart.SelectedValues?.Remove(ctrl.DataContext);
            }
        }
        else
        {
            ctrl.IsSelected = true;
            chart.SelectedRanges?.Add(ctrl);
            chart.SelectedValues?.Add(ctrl.DataContext);
        }

    }

    internal void OnTaskClicked(TimeRangeControl ctrl)
    {
        OnTaskClicked(this, ctrl);
    }


    private static void ClearSelectRange(TimeRangeChart chart)
    {

        if (chart.SelectedRanges != null)
            foreach (var item in chart.SelectedRanges)
            {
                item.IsSelected = false;
            }
        chart.SelectedRanges?.Clear();
        chart.SelectedValues?.Clear();
    }

    private void ClearSelectRange()
    {
        ClearSelectRange(this);
    }

    private void Refresh()
    {
        CalculateBorder();
        //DrawBorder();
        InvalidateVisual();

        // スタック再計算（表示用 Top / Chart 高さの調整）
        ApplyStackingIfNeeded();

        //AddSeries();
        //AddPoints();

        TimeRangeNeedRefresh?.Invoke(this, new EventArgs());
    }

    private void RefreshCall()
    {
        Dispatcher.BeginInvoke(Refresh);

    }
    protected override void OnRender(DrawingContext dc)
    {
        base.OnRender(dc);
        dc.PushClip(new RectangleGeometry(new Rect(0, 0, ActualWidth, ActualHeight)));
        DrawBorder(dc);
        dc.Pop();
    }

    private double TimeToPoint(DateTime time)
    {
        switch (TimeUnit)
        {
            case TimeUnitType.Normal:
                return TimeScreenHelper.TimeToPoint(time, StartTime, EndTime, ActualWidth == 0 && Width != double.NaN ? Width : ActualWidth);
            case TimeUnitType.Month:
                //return TimeScreenHelper.TimeToPointMonth(time, StartTime, EndTime, ActualWidth == 0 && Width != double.NaN ? Width : ActualWidth, TimeTicks);
                return TimeScreenHelper.TimeToPointMonth(time, WidthPerRuledLine, TimeTicks);
            default:
                break;
        }
        return 0;
    }

    private DateTime PointToTime(double point, TimeSpan? minSpan = null)
    {
        switch (TimeUnit)
        {
            case TimeUnitType.Normal:
                return TimeScreenHelper.PointToTime(point, StartTime, EndTime, ActualWidth == 0 && Width != double.NaN ? Width : ActualWidth, minSpan ?? MinSpan);
            case TimeUnitType.Month:
                //return TimeScreenHelper.PointToTimeMonth(point, StartTime, EndTime, ActualWidth == 0 && Width != double.NaN ? Width : ActualWidth, minSpan ?? MinSpan, TimeTicks);
                return TimeScreenHelper.PointToTimeMonth(point, WidthPerRuledLine, minSpan ?? MinSpan, TimeTicks);
            default:
                break;
        }
        return StartTime;
    }


    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        if (TimeTicks == null && IsControlTick)
        {
            TimeTicks = new List<IRuledLine>();
            SubTimeTicks = new List<IRuledLine>();
        }

        //Refresh();
        this.SizeChanged += (s, e) =>
        {
            // サイズ変更時に再描画
            Refresh();
        };

        this.Loaded += (s, e) =>
        {
            if (_popup.Parent == null)
            {
                AddLogicalChild(_popup);
            }
        };

        this.MouseLeftButtonDown += (s, e) =>
        {
            ClearSelectRange();
        };
    }

    #region Popup

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);

        if (BackgroundTimes?.Count > 0)
        {
            var p = e.GetPosition(this);
            var dropPoint = this?.PointToTimeFunc(e.GetPosition(this).X, null);

            var hit = BackgroundTimes.FirstOrDefault(x => x.StartTime <= dropPoint && dropPoint <= x.EndTime);
            if (!ReferenceEquals(hit, _currentHover))
            {
                _currentHover = hit;

                if (hit != null)
                    ShowPopup(hit, p);
                else
                    HidePopup();
            }
            else if (hit != null)
            {
                MovePopup(p);
            }
        }
    }
    protected override void OnMouseLeave(MouseEventArgs e)
    {
        base.OnMouseLeave(e);
        HidePopup();
    }

    private readonly Popup _popup = new()
    {
        Placement = PlacementMode.Relative,
        StaysOpen = true,
        AllowsTransparency = true,
        IsHitTestVisible = false
    };

    private IBackgroundTimeRange? _currentHover;

    public DataTemplate BackgroundRangeToolTipTemplate
    {
        get { return (DataTemplate)GetValue(BackgroundRangeToolTipTemplateProperty); }
        set { SetValue(BackgroundRangeToolTipTemplateProperty, value); }
    }
    public static DependencyProperty BackgroundRangeToolTipTemplateProperty = DependencyProperty.Register(nameof(BackgroundRangeToolTipTemplate), typeof(DataTemplate), typeof(TimeRangeChart));


    UIElement? CreateToolTipContent(IBackgroundTimeRange r)
    {
        var template = BackgroundRangeToolTipTemplate;
        if (template == null)
            return null;

        var content = template.LoadContent();

        if (content is FrameworkElement fe)
        {
            fe.DataContext = r;
            return fe;
        }

        // 想定外だが UI を壊さない
        return null;
    }

    void ShowPopup(IBackgroundTimeRange range, Point p)
    {
        _popup.PlacementTarget = this;
        var content = CreateToolTipContent(range);
        if (content == null)
            return;

        if (content is FrameworkElement fe)
        {
            fe.Loaded += (s, e) =>
            {
                content.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                content.Arrange(new Rect(0, 0,
                    content.DesiredSize.Width,
                    content.DesiredSize.Height));
            };
        }


        content.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        content.Arrange(new Rect(0, 0, content.DesiredSize.Width, content.DesiredSize.Height));

        _popup.Child = content;
        _popup.Placement = PlacementMode.Relative;

        _popup.PlacementRectangle =
            new Rect(0, 0, content.DesiredSize.Width, content.DesiredSize.Height);

        MovePopup(p);

        _popup.IsOpen = true;
        Dispatcher.BeginInvoke(
            DispatcherPriority.Loaded,
            new Action(() =>
            {
                if (content is FrameworkElement fe)
                {
                    fe.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                    fe.Arrange(new Rect(0, 0, fe.DesiredSize.Width, fe.DesiredSize.Height));
                }
            }));

    }

    void MovePopup(Point p)
    {

        _popup.HorizontalOffset = p.X + 14;
        _popup.VerticalOffset = p.Y + 14;

    }
    void HidePopup()
    {
        _popup.IsOpen = false;
        _popup.Child = null;
        _currentHover = null;
    }

    #endregion

    public bool CanDragTasksBetweenRows
    {
        get { return (bool)GetValue(CanDragTasksBetweenRowsProperty); }
        set { SetValue(CanDragTasksBetweenRowsProperty, value); }
    }
    public static DependencyProperty CanDragTasksBetweenRowsProperty = DependencyProperty.Register(nameof(CanDragTasksBetweenRows), typeof(bool), typeof(TimeRangeChart), new PropertyMetadata(false));




    protected override void OnPreviewDrop(DragEventArgs e)
    {
        base.OnPreviewDrop(e);

        if (CanDragTasksBetweenRows == false) { return; }

        var formats = e.Data.GetFormats();
        if (formats != null && formats.Length > 0)
        {
            var data = e.Data.GetData(formats[0]);
            if (data is DragTaskData data2)
            {
                var param = new DragTaskData();


                var dropPoint = this.PointToTimeFunc(e.GetPosition(this).X, null);

                var args = new DropOnRowEventArgs(PreviewTaskDropEvent, dropPoint, data2.TimeRange!);
                args.SourceList = data2.SourceRow;
                args.DestList = this.ItemsSource;
                RaiseEvent(args);
                if (args.Cancel) { return; }

                args.SourceList?.Remove(args.TaskObject);


                //開始時間を書き換える
                if (args.TaskObject is ITimeRange range)
                {
                    range.Move(dropPoint - range.StartTime, NonWorkingTimes);
                }

                args.DestList.Add(args.TaskObject);

                var args2 = new DropOnRowEventArgs(TaskDropEvent, dropPoint, data2.TimeRange!);
                RaiseEvent(args2);

                if (CanStackRange) RefreshCall();
            }
        }
    }


    public static readonly RoutedEvent TaskDropEvent =
    EventManager.RegisterRoutedEvent(
        nameof(TaskDrop), RoutingStrategy.Bubble,
        typeof(EventHandler<DropOnRowEventArgs>), typeof(TimeRangeChart));

    public event EventHandler<DropOnRowEventArgs> TaskDrop
    {
        add { AddHandler(TaskDropEvent, value); }
        remove { RemoveHandler(TaskDropEvent, value); }
    }
    public static readonly RoutedEvent PreviewTaskDropEvent =
    EventManager.RegisterRoutedEvent(
        nameof(PreviewTaskDrop), RoutingStrategy.Bubble,
        typeof(EventHandler<DropOnRowEventArgs>), typeof(TimeRangeChart));

    public event EventHandler<DropOnRowEventArgs> PreviewTaskDrop
    {
        add { AddHandler(PreviewTaskDropEvent, value); }
        remove { RemoveHandler(PreviewTaskDropEvent, value); }
    }

    #region events

    public event EventHandler<DragDropRangeEventArgs>? OnDragStart;

    public event EventHandler<DragDropRangeEventArgs>? OnDragEnd;

    internal void OnTaskDragStart(TimeRangeControl ctrl, DragDropRangeEventArgs e)
    {
        OnDragStart?.Invoke(ctrl, e);
        ParentGridChart?.OnTaskDragStart(ctrl, e);
    }

    internal void OnTaskDragEnd(TimeRangeControl ctrl, DragDropRangeEventArgs e)
    {
        OnDragEnd?.Invoke(ctrl, e);
        ParentGridChart?.OnTaskDragEnd(ctrl, e);

        if (CanStackRange) RefreshCall();

    }

    #endregion


}
