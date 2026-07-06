using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
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
using FrostNova.Core.Wpf.Behaviors;
using FrostNova.Core.Wpf.VisualHelpers;
using FrostNova.TimeRangeChart.TimeRangeHelpers;
using FrostNova.TimeRangeChart.Wpf.TimeRangeHelpers;
using Microsoft.VisualBasic;
using static FrostNova.TimeRangeChart.Wpf.TimeRangeControl;

namespace FrostNova.TimeRangeChart.Wpf;


[TemplatePart(Name = "PART_ChartScrollViewer", Type = typeof(ScrollViewer))]
[TemplatePart(Name = "PART_ItemsHost", Type = typeof(Canvas))]
public class TimeRangeChart : Control
{

    static TimeRangeChart()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(TimeRangeChart), new FrameworkPropertyMetadata(typeof(TimeRangeChart)));
    }


    #region Fields

    /// <summary>
    /// Represents the canvas used as the host for item elements.
    /// </summary>
    private Canvas? _itemsHost;
    private ScrollViewer? _scrollViewer;

    private Pen? _ruledLinePen;

    private double _contentWidth;

    /// <summary>
    /// Get the width of chart.
    /// </summary>
    public double ContentWidth => _contentWidth;


    // 描画時に高速アクセスするためのキャッシュ
    private Dictionary<int, List<ITimeRangeRowIndex>> _indexedNonWorkingTimes = new();
    private Dictionary<int, List<IBackgroundTimeRange>> _indexedBackgroundTimes = new();
    private Dictionary<int, List<ITimeRangeRowIndex>> _indexedTasks = new();


    #region viewport info

    int _currentFirstRow = -1;
    int _currentLastRow = -1;
    private int _lastFirstRow = -1;
    private int _lastLastRow = -1;
    DateTime _lastFirstTime;
    DateTime _lastLastTime;
    private bool _forceSyncVisuals = false;

    public int CurrentFirstRow => _currentFirstRow;
    public int CurrentLastRow => _currentLastRow;

    DateTime _currentFirstTime;
    DateTime _currentCenterTime;
    DateTime _currentLastTime;

    public DateTime CurrentFirstTime => _currentFirstTime;
    public DateTime CurrentCenterTime => _currentCenterTime;
    public DateTime CurrentLastTime => _currentLastTime;


    #endregion

    #region background popup

    private Popup? _backgroundPopup;
    private ContentPresenter? _backgroundPopupPresenter;
    private IBackgroundTimeRange? _hoveredBackgroundRange;

    #endregion

    #region drag info

    private TimeRangeControl? _draggingControl;
    private object? _recentlyDroppedTask;  // ドラッグ終了直後の SyncVisuals でタスクが除去されるのを防ぐ
    private TimeRangeControl.HitTestMode _dragMode;
    private Point _lastMousePosition;
    private DateTime _originalStartTime;
    private DateTime _originalEndTime;
    private TimeSpan _originalActualSpan;
    private TimeSpan? _originalWorkingSpan;
    private DateTime _dragStartMouseTime;
    private TimeSpan _dragStartOffsetFromStart;
    private bool _dragOccured = false;
    private bool _dropBetweenRowsOccured = false;
    private int _originalRowIndex;

    public bool IsDragging => _draggingControl != null;

    #endregion

    internal List<IRuledLine> TimeTicks { get; set; } = [];

    internal List<IRuledLine> SubTimeTicks { get; set; } = [];

    #endregion


    #region Properties

    #region time

    public DateTime StartTime
    {
        get { return (DateTime)GetValue(StartTimeProperty); }
        set { SetValue(StartTimeProperty, value); }
    }
    public static readonly DependencyProperty StartTimeProperty = DependencyProperty.Register(nameof(StartTime), typeof(DateTime), typeof(TimeRangeChart), new FrameworkPropertyMetadata(DateTime.Now.AddDays(-3), FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnTimeUnitChanged));

    public DateTime EndTime
    {
        get { return (DateTime)GetValue(EndTimeProperty); }
        set { SetValue(EndTimeProperty, value); }
    }
    public static readonly DependencyProperty EndTimeProperty = DependencyProperty.Register(nameof(EndTime), typeof(DateTime), typeof(TimeRangeChart), new FrameworkPropertyMetadata(DateTime.Now.AddDays(9), FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnTimeUnitChanged));


    public TimeSpan MinSpan
    {
        get { return (TimeSpan)GetValue(MinSpanProperty); }
        set { SetValue(MinSpanProperty, value); }
    }
    public static readonly DependencyProperty MinSpanProperty = DependencyProperty.Register(nameof(MinSpan), typeof(TimeSpan), typeof(TimeRangeChart), new PropertyMetadata(TimeSpan.FromMinutes(15)));
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
    /// width per ruled line
    /// </summary>
    public double WidthPerRuledLine
    {
        get { return (double)GetValue(WidthPerRuledLineProperty); }
        set { SetValue(WidthPerRuledLineProperty, value); }
    }
    public static DependencyProperty WidthPerRuledLineProperty = DependencyProperty.Register(nameof(WidthPerRuledLine), typeof(double), typeof(TimeRangeChart), new PropertyMetadata(100.0d, OnTimeUnitChanged));
    public TimeUnitType TimeUnit
    {
        get { return (TimeUnitType)GetValue(TimeUnitProperty); }
        set { SetValue(TimeUnitProperty, value); }
    }
    public static DependencyProperty TimeUnitProperty = DependencyProperty.Register(nameof(TimeUnit), typeof(TimeUnitType), typeof(TimeRangeChart), new PropertyMetadata(TimeUnitType.Normal, OnTimeUnitChanged));

    public int MonthSpan
    {
        get { return (int)GetValue(MonthSpanProperty); }
        set { SetValue(MonthSpanProperty, value); }
    }
    public static DependencyProperty MonthSpanProperty = DependencyProperty.Register(nameof(MonthSpan), typeof(int), typeof(TimeRangeChart), new PropertyMetadata(1, OnTimeUnitChanged));
    public string HeaderFormat
    {
        get { return (string)GetValue(HeaderFormatProperty); }
        set { SetValue(HeaderFormatProperty, value); }
    }
    public static DependencyProperty HeaderFormatProperty = DependencyProperty.Register(nameof(HeaderFormat), typeof(string), typeof(TimeRangeChart), new PropertyMetadata("", OnTimeUnitChanged));
    public Func<TimeRangeChart, DateTime, Brush> HeaderBrushSelector
    {
        get { return (Func<TimeRangeChart, DateTime, Brush>)GetValue(HeaderBrushSelectorProperty); }
        set { SetValue(HeaderBrushSelectorProperty, value); }
    }
    public static DependencyProperty HeaderBrushSelectorProperty = DependencyProperty.Register(nameof(HeaderBrushSelector), typeof(Func<TimeRangeChart, DateTime, Brush>), typeof(TimeRangeChart), new PropertyMetadata(new Func<TimeRangeChart, DateTime, Brush>((gridChart, d) => GridTimeRangeChart.DefaultHeaderSelector(gridChart, d))));


    #endregion


    #region Drag Controls

    public bool CanStackRange
    {
        get { return (bool)GetValue(CanStackRangeProperty); }
        set { SetValue(CanStackRangeProperty, value); }
    }
    public static DependencyProperty CanStackRangeProperty = DependencyProperty.Register(nameof(CanStackRange), typeof(bool), typeof(TimeRangeChart), new PropertyMetadata(false));

    public bool CanDragTasksBetweenRows
    {
        get { return (bool)GetValue(CanDragTasksBetweenRowsProperty); }
        set { SetValue(CanDragTasksBetweenRowsProperty, value); }
    }
    public static DependencyProperty CanDragTasksBetweenRowsProperty = DependencyProperty.Register(nameof(CanDragTasksBetweenRows), typeof(bool), typeof(TimeRangeChart), new PropertyMetadata(true));


    /// <summary>
    /// Get or Set the height of task when stack.
    /// </summary>
    public double StackHeight
    {
        get { return (double)GetValue(StackHeightProperty); }
        set { SetValue(StackHeightProperty, value); }
    }
    public static DependencyProperty StackHeightProperty = DependencyProperty.Register(nameof(StackHeight), typeof(double), typeof(TimeRangeChart), new PropertyMetadata(24.0));


    /// <summary>
    /// Gets or sets a value indicating whether multiple items can be selected simultaneously.
    /// </summary>
    /// <remarks>Set this property to <see langword="true"/> to allow users to select more than one item at a
    /// time. When set to <see langword="false"/>, only single selection is permitted.</remarks>
    public bool AllowMultiSelection
    {
        get => (bool)GetValue(AllowMultiSelectionProperty);
        set => SetValue(AllowMultiSelectionProperty, value);
    }
    public static readonly DependencyProperty AllowMultiSelectionProperty =
    DependencyProperty.Register(nameof(AllowMultiSelection), typeof(bool), typeof(TimeRangeChart), new PropertyMetadata(true));

    /// <summary>
    /// Gets or sets a value indicating whether multiple items can be dragged simultaneously.
    /// </summary>
    /// <remarks>Set this property to <see langword="true"/> to enable users to select and drag more than one
    /// item at a time. When set to <see langword="false"/>, only single-item drag operations are permitted.</remarks>
    public bool AllowMultiDrag
    {
        get => (bool)GetValue(AllowMultiDragProperty);
        set => SetValue(AllowMultiDragProperty, value);
    }
    public static readonly DependencyProperty AllowMultiDragProperty =
        DependencyProperty.Register(nameof(AllowMultiDrag), typeof(bool), typeof(TimeRangeChart), new PropertyMetadata(true));


    #endregion



    /// <summary>
    /// Height
    /// </summary>
    public double ChartHeight
    {
        get { return (double)GetValue(ChartHeightProperty); }
        set { SetValue(ChartHeightProperty, value); }
    }
    public static DependencyProperty ChartHeightProperty = DependencyProperty.Register(nameof(ChartHeight), typeof(double), typeof(TimeRangeChart), new PropertyMetadata(80.0d));

    public Brush RuledLineFill
    {
        get { return (Brush)GetValue(RuledLineFillProperty); }
        set { SetValue(RuledLineFillProperty, value); RefreshCall(); }
    }
    public static DependencyProperty RuledLineFillProperty = DependencyProperty.Register(nameof(RuledLineFill), typeof(Brush), typeof(TimeRangeChart), new PropertyMetadata(new SolidColorBrush(Color.FromArgb(255, 125, 125, 125)), OnRuledLinePropertyChanged));

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




    private Pen GetRuledLinePen()
    {
        var brush = RuledLineFill;
        brush.Freeze();
        var thickness = RuledLineThickness;

        var pen = new Pen(brush, thickness);
        pen.Freeze();

        return pen;

    }



    /// <summary>
    /// string
    /// </summary>
    public DataTemplate DataTemplate
    {
        get { return (DataTemplate)GetValue(DataTemplateProperty); }
        set { SetValue(DataTemplateProperty, value); }
    }
    public static DependencyProperty DataTemplateProperty = DependencyProperty.Register(nameof(DataTemplate), typeof(DataTemplate), typeof(TimeRangeChart));

    /// <summary>
    /// string
    /// </summary>
    public DataTemplateSelector DataTemplateSelector
    {
        get { return (DataTemplateSelector)GetValue(DataTemplateSelectorProperty); }
        set { SetValue(DataTemplateSelectorProperty, value); }
    }
    public static DependencyProperty DataTemplateSelectorProperty = DependencyProperty.Register(nameof(DataTemplateSelector), typeof(DataTemplateSelector), typeof(TimeRangeChart));

    public Func<object, int> IndexFunc
    {
        get { return (Func<object, int>)GetValue(IndexFuncProperty); }
        set { SetValue(IndexFuncProperty, value); }
    }
    public static DependencyProperty IndexFuncProperty = DependencyProperty.Register(nameof(IndexFunc), typeof(Func<object, int>), typeof(TimeRangeChart));


    public IList<ITimeRangeRowIndex> NonWorkingTimes
    {
        get { return (IList<ITimeRangeRowIndex>)GetValue(NonWorkingTimesProperty); }
        set { SetValue(NonWorkingTimesProperty, value); }
    }
    public static DependencyProperty NonWorkingTimesProperty = DependencyProperty.Register(nameof(NonWorkingTimes), typeof(IList<ITimeRangeRowIndex>), typeof(TimeRangeChart), new PropertyMetadata(OnNonWorkingTimesChanged));
    public IList<IBackgroundTimeRange> BackgroundTimes
    {
        get { return (IList<IBackgroundTimeRange>)GetValue(BackgroundTimesProperty); }
        set { SetValue(BackgroundTimesProperty, value); }
    }
    public static DependencyProperty BackgroundTimesProperty = DependencyProperty.Register(nameof(BackgroundTimes), typeof(IList<IBackgroundTimeRange>), typeof(TimeRangeChart), new PropertyMetadata(OnBackgroundTimesChanged));

    public DataTemplate? BackgroundTimeRangePopupTemplate
    {
        get => (DataTemplate?)GetValue(BackgroundTimeRangePopupTemplateProperty);
        set => SetValue(BackgroundTimeRangePopupTemplateProperty, value);
    }
    public static readonly DependencyProperty BackgroundTimeRangePopupTemplateProperty =
        DependencyProperty.Register(nameof(BackgroundTimeRangePopupTemplate), typeof(DataTemplate), typeof(TimeRangeChart));

    public string SubScaleFormat
    {
        get { return (string)GetValue(SubScaleFormatProperty); }
        set { SetValue(SubScaleFormatProperty, value); }
    }
    public static DependencyProperty SubScaleFormatProperty = DependencyProperty.Register(nameof(SubScaleFormat), typeof(string), typeof(TimeRangeChart));



    public IEnumerable<ITimeRangeRowIndex> ItemsSource
    {
        get { return (IEnumerable<ITimeRangeRowIndex>)GetValue(ItemsSourceProperty); }
        set { SetValue(ItemsSourceProperty, value); }
    }
    public static readonly DependencyProperty ItemsSourceProperty = DependencyProperty.Register(nameof(ItemsSource), typeof(IEnumerable<ITimeRangeRowIndex>), typeof(TimeRangeChart), new PropertyMetadata(null, OnItemSourceChanged));

    public int RowCount { get; internal set; } = 0;

    #region Stack properties

    /// <summary>
    /// string
    /// </summary>
    public double StackMargin
    {
        get { return (double)GetValue(StackMarginProperty); }
        set { SetValue(StackMarginProperty, value); }
    }
    public static DependencyProperty StackMarginProperty = DependencyProperty.Register(nameof(StackMargin), typeof(double), typeof(TimeRangeChart), new PropertyMetadata(0.0));



    #endregion

    #endregion


    #region Property changed Events



    private static void OnTimeUnitChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is TimeRangeChart chart)
        {
            // 自身の再描画をトリガーする
            chart.CalclateTimeAxis();
            chart.RefreshCall();
            chart.InvokeOnTimeAxisChanged();
        }
    }

    private static void OnRuledLinePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is TimeRangeChart chart)
        {
            chart._ruledLinePen = chart.GetRuledLinePen();
            OnMyPropertyChanged(d, e);
        }
    }
    private static void OnMyPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        TimeRangeChart customControl = (TimeRangeChart)d;
        // 自身の再描画をトリガーする
        customControl.RefreshCall();
    }


    private static void OnItemSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not TimeRangeChart chart) return;
        if (e.OldValue != null)
        {
            if (e.OldValue is INotifyCollectionChanged observable)
            {
                observable.CollectionChanged -= chart.OnItemsSourceCollectionChanged;
            }
        }
        if (e.NewValue is IEnumerable<ITimeRangeRowIndex> newSource)
        {
            chart._forceSyncVisuals = true;
            chart.UpdateLayoutMetrics();
            // 2. 現在のスクロール位置に合わせて表示更新
            chart.UpdateVisibleRange();

            // 3. 新しいイベント購読
            if (newSource is INotifyCollectionChanged newObservable)
            {
                newObservable.CollectionChanged += chart.OnItemsSourceCollectionChanged;
            }
        }
    }

    private void OnItemsSourceCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        // データが変わったらレイアウト（高さ・スタック）を再計算
        UpdateLayoutMetrics();

        // 配置と描画を更新
        UpdateVisibleRange();
    }

    private static void OnBackgroundTimesChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not TimeRangeChart chart) return;
        if (e.OldValue is ObservableCollection<ITimeRange> oldObsTimes)
        {
            if (chart._backgroundTimesCollectionChangedHandler != null)
            {
                oldObsTimes.CollectionChanged -= chart._backgroundTimesCollectionChangedHandler;
            }
        }

        if (e.NewValue is ObservableCollection<ITimeRange> obsTimes)
        {
            chart._backgroundTimesCollectionChangedHandler = (s, ev) =>
            {
                chart.CalculateIndexedBackgroundTimes();
                chart.RefreshCall();
            };
            obsTimes.CollectionChanged += chart._backgroundTimesCollectionChangedHandler;
        }

        chart.CalculateIndexedBackgroundTimes();
    }



    private static void OnNonWorkingTimesChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is TimeRangeChart chart)
        {
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
    public Style TimeRangeStyle
    {
        get { return (Style)GetValue(TimeRangeStyleProperty); }
        set { SetValue(TimeRangeStyleProperty, value); }
    }
    public static DependencyProperty TimeRangeStyleProperty = DependencyProperty.Register(nameof(TimeRangeStyle), typeof(Style), typeof(TimeRangeChart), new PropertyMetadata(null));

    private NotifyCollectionChangedEventHandler? _nonworkingTimesCollectionChangedHandler;
    private NotifyCollectionChangedEventHandler? _backgroundTimesCollectionChangedHandler;

    private void OnScrollViewerSizeChanged(object sender, SizeChangedEventArgs e)
    {
        // スクロール位置が変わったら表示を更新
        UpdateVisibleRange();
        InvalidateVisual();
    }

    private void OnScrollViewerScrollChanged(object sender, ScrollChangedEventArgs e)
    {
        // スクロール位置が変わったら表示を更新
        UpdateVisibleRange();
        InvalidateVisual();
    }


    #endregion

    #region Event Definitions



    public event EventHandler<EventArgs>? OnTimeAxisChanged;




    public event EventHandler<RoutedEventArgs>? ViewPortChanged
    {
        add { AddHandler(ViewPortChangedEvent, value); }
        remove { RemoveHandler(ViewPortChangedEvent, value); }
    }
    public static readonly RoutedEvent ViewPortChangedEvent =
    EventManager.RegisterRoutedEvent(
        nameof(ViewPortChanged), RoutingStrategy.Bubble,
        typeof(EventHandler<RoutedEventArgs>), typeof(TimeRangeChart));




    public event EventHandler<RoutedEventArgs>? RowHeightsChanged
    {
        add { AddHandler(RowHeightsChangedEvent, value); }
        remove { RemoveHandler(RowHeightsChangedEvent, value); }
    }
    public static readonly RoutedEvent RowHeightsChangedEvent =
    EventManager.RegisterRoutedEvent(
        nameof(RowHeightsChanged), RoutingStrategy.Bubble,
        typeof(EventHandler<RoutedEventArgs>), typeof(TimeRangeChart));



    public event EventHandler<DragDropRangeEventArgs>? TaskDragStart
    {
        add { AddHandler(TaskDragStartEvent, value); }
        remove { RemoveHandler(TaskDragStartEvent, value); }
    }
    public static readonly RoutedEvent TaskDragStartEvent =
        EventManager.RegisterRoutedEvent(
            nameof(TaskDragStart), RoutingStrategy.Bubble,
        typeof(EventHandler<DragDropRangeEventArgs>), typeof(TimeRangeChart));


    public event EventHandler<DragDropRangeEventArgs>? TaskDragEnd
    {
        add { AddHandler(TaskDragEndEvent, value); }
        remove { RemoveHandler(TaskDragEndEvent, value); }

    }
    public static readonly RoutedEvent TaskDragEndEvent =
        EventManager.RegisterRoutedEvent(
            nameof(TaskDragEnd), RoutingStrategy.Bubble,
        typeof(EventHandler<DragDropRangeEventArgs>), typeof(TimeRangeChart));


    public event EventHandler<DropOnRowEventArgs>? TaskDrop
    {
        add { AddHandler(TaskDropEvent, value); }
        remove { RemoveHandler(TaskDropEvent, value); }
    }
    public static readonly RoutedEvent TaskDropEvent =
    EventManager.RegisterRoutedEvent(
        nameof(TaskDrop), RoutingStrategy.Bubble,
        typeof(EventHandler<DropOnRowEventArgs>), typeof(TimeRangeChart));


    public event EventHandler<DropOnRowEventArgs>? PreviewTaskDrop
    {
        add { AddHandler(PreviewTaskDropEvent, value); }
        remove { RemoveHandler(PreviewTaskDropEvent, value); }
    }
    public static readonly RoutedEvent PreviewTaskDropEvent =
    EventManager.RegisterRoutedEvent(
        nameof(PreviewTaskDrop), RoutingStrategy.Bubble,
        typeof(EventHandler<DropOnRowEventArgs>), typeof(TimeRangeChart));







    internal void InvokeOnTimeAxisChanged()
    {
        OnTimeAxisChanged?.Invoke(this, EventArgs.Empty);
    }

    // ── Chart background selection events (internal; consumed by GridTimeRangeChart) ─────

    internal static readonly RoutedEvent ChartBackgroundLeftClickEvent =
        EventManager.RegisterRoutedEvent(
            nameof(ChartBackgroundLeftClickEvent), RoutingStrategy.Bubble,
            typeof(EventHandler<ChartBackgroundClickEventArgs>), typeof(TimeRangeChart));

    internal static readonly RoutedEvent ChartBackgroundRightClickEvent =
        EventManager.RegisterRoutedEvent(
            nameof(ChartBackgroundRightClickEvent), RoutingStrategy.Bubble,
            typeof(EventHandler<ChartBackgroundClickEventArgs>), typeof(TimeRangeChart));

    #endregion

    // ── ChartSelectionMode DP ────────────────────────────────────────────────────────────

    public static readonly DependencyProperty ChartSelectionModeProperty =
        DependencyProperty.Register(
            nameof(ChartSelectionMode), typeof(ChartSelectionMode), typeof(TimeRangeChart),
            new PropertyMetadata(ChartSelectionMode.FullRow, OnChartSelectionModeChanged));

    public ChartSelectionMode ChartSelectionMode
    {
        get => (ChartSelectionMode)GetValue(ChartSelectionModeProperty);
        set => SetValue(ChartSelectionModeProperty, value);
    }

    private static void OnChartSelectionModeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is TimeRangeChart chart && chart._selectedRowIndex >= 0)
            chart.UpdateSelectionRect();
    }

    // ── Selection state ─────────────────────────────────────────────────────────────────

    private int _selectedRowIndex = -1;
    private DateTime _selectedDateTime;

    // ── Selection appearance DPs ─────────────────────────────────────────────────

    public static readonly DependencyProperty SelectionFillProperty =
        DependencyProperty.Register(nameof(SelectionFill), typeof(Brush), typeof(TimeRangeChart),
            new PropertyMetadata(new SolidColorBrush(Color.FromArgb(60, 30, 120, 220))));

    public Brush SelectionFill
    {
        get => (Brush)GetValue(SelectionFillProperty);
        set => SetValue(SelectionFillProperty, value);
    }

    public static readonly DependencyProperty SelectionStrokeProperty =
        DependencyProperty.Register(nameof(SelectionStroke), typeof(Brush), typeof(TimeRangeChart),
            new PropertyMetadata(new SolidColorBrush(Color.FromArgb(180, 30, 120, 220))));

    public Brush SelectionStroke
    {
        get => (Brush)GetValue(SelectionStrokeProperty);
        set => SetValue(SelectionStrokeProperty, value);
    }

    public static readonly DependencyProperty SelectionStrokeThicknessProperty =
        DependencyProperty.Register(nameof(SelectionStrokeThickness), typeof(double), typeof(TimeRangeChart),
            new PropertyMetadata(1.5));

    public double SelectionStrokeThickness
    {
        get => (double)GetValue(SelectionStrokeThicknessProperty);
        set => SetValue(SelectionStrokeThicknessProperty, value);
    }

    private readonly Rectangle _selectionRect = new Rectangle
    {
        IsHitTestVisible = false,
        Visibility = Visibility.Collapsed,
    };

    private void UpdateSelectionRect()
    {
        if (_selectedRowIndex < 0 || RowOffsets == null || RowOffsets.Length == 0
            || _selectedRowIndex >= RowOffsets.Length) return;

        double rowTop    = RowOffsets[_selectedRowIndex];
        double rowHeight = RowHeights[_selectedRowIndex];
        double left, width;

        if (ChartSelectionMode == ChartSelectionMode.DateOnly)
        {
            left  = TimeToPoint(_selectedDateTime);
            width = TimeToPoint(_selectedDateTime + ScaleIncrement) - left;
        }
        else
        {
            left  = 0;
            width = _contentWidth;
        }

        Canvas.SetLeft(_selectionRect, left);
        Canvas.SetTop(_selectionRect, rowTop);
        _selectionRect.Width     = Math.Max(0, width);
        _selectionRect.Height    = rowHeight;
        _selectionRect.Visibility = Visibility.Visible;
    }

    private void ClearSelection()
    {
        _selectedRowIndex = -1;
        _selectionRect.Visibility = Visibility.Collapsed;
    }

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        _itemsHost = GetTemplateChild("PART_ItemsHost") as Canvas;
        _scrollViewer = GetTemplateChild("PART_ChartScrollViewer") as ScrollViewer;

        _ruledLinePen = GetRuledLinePen();

        TimeTicks = new List<IRuledLine>();
        SubTimeTicks = new List<IRuledLine>();

        CalclateTimeAxis();

        if (_scrollViewer != null)
        {
            _scrollViewer.ScrollChanged += OnScrollViewerScrollChanged;
            _scrollViewer.SizeChanged += OnScrollViewerSizeChanged;

            GeneralHelper.SetBehavior<ScrollGestureBehavior>(_scrollViewer, true);
        }

        _backgroundPopupPresenter = new ContentPresenter();
        _backgroundPopup = new Popup
        {
            Child = _backgroundPopupPresenter,
            Placement = PlacementMode.Mouse,
            StaysOpen = true,
            AllowsTransparency = true,
        };

        if (_itemsHost != null)
        {
            _itemsHost.MouseMove += OnItemsHostMouseMove;
            _itemsHost.MouseLeave += OnItemsHostMouseLeave;

            // Register directly on the canvas so ScrollViewer.OnMouseLeftButtonDown
            // (which sets e.Handled=true via Focus()) cannot suppress the event.
            _itemsHost.MouseLeftButtonDown += OnItemsHostBackgroundClick;

            // Bind selection rect appearance to DPs so consumers can style it
            _selectionRect.SetBinding(Rectangle.FillProperty,
                new System.Windows.Data.Binding(nameof(SelectionFill)) { Source = this });
            _selectionRect.SetBinding(Rectangle.StrokeProperty,
                new System.Windows.Data.Binding(nameof(SelectionStroke)) { Source = this });
            _selectionRect.SetBinding(Rectangle.StrokeThicknessProperty,
                new System.Windows.Data.Binding(nameof(SelectionStrokeThickness)) { Source = this });

            // Selection rect sits below all task controls
            Panel.SetZIndex(_selectionRect, -1);
            _itemsHost.Children.Add(_selectionRect);
        }
    }

    private void RebuildTasksByRow()
    {
        _indexedTasks.Clear();

        for (int i = 0; i < RowCount; i++)
        {
            _indexedTasks[i] = new List<ITimeRangeRowIndex>();
        }

        // 2. 既存のタスクをそれぞれの行に振り分ける
        if (ItemsSource is IEnumerable<ITimeRangeRowIndex> allTasks)
        {
            foreach (var t in allTasks)
            {
                // 上で全行初期化していれば、ここでの ContainsKey チェックは不要になります
                if (_indexedTasks.TryGetValue(t.RowIndex, out var list))
                {
                    list.Add(t);
                }
                else
                {
                    // 万が一、RowOffsetsの範囲外のRowIndexを持つタスクがいた場合の安全策
                    var newList = new List<ITimeRangeRowIndex> { t };
                    _indexedTasks[t.RowIndex] = newList;
                }
            }
        }
    }


    private void UpdateVisibleRange()
    {
        if (_scrollViewer == null || RowOffsets == null || RowOffsets.Length == 0) return;

        double top = _scrollViewer.VerticalOffset;
        double bottom = top + _scrollViewer.ViewportHeight;

        double left = _scrollViewer.HorizontalOffset;
        double right = left + _scrollViewer.ViewportWidth;

        // 二分探索で可視範囲の行インデックスを特定
        int firstRow = FindRowIndexAtOffset(top);
        int lastRow = FindRowIndexAtOffset(bottom);


        DateTime firstDate = FindColumnAtOffset(left);
        DateTime centerDate = FindColumnAtOffset(left + (right - left) / 2.0);
        DateTime lastDate = FindColumnAtOffset(right);
        bool isScopeChanged = firstRow != _lastFirstRow ||
                          lastRow != _lastLastRow ||
                          firstDate != _lastFirstTime ||
                          lastDate != _lastLastTime;
        // 1. タスクの配置更新 (仮想化)
        if (isScopeChanged || _forceSyncVisuals)
        {
            _forceSyncVisuals = false;
            _lastFirstRow = firstRow;
            _lastLastRow = lastRow;
            _lastFirstTime = firstDate;
            _lastLastTime = lastDate;

            // 見えている行のタスクだけを集める（全件スキャンしない！）
            var visibleTasks = new List<ITimeRangeRowIndex>();
            for (int i = firstRow; i <= lastRow; i++)
            {
                if (_indexedTasks.TryGetValue(i, out var tasks))
                {
                    foreach (var task in tasks)
                    {
                        // 横方向の可視判定：
                        // タスクがドラッグ中、または画面の左右の範囲に重なっているか
                        bool isDragging = (_draggingControl?.DataContext == task);
                        bool isVisibleX = task.StartTime <= lastDate && task.EndTime >= firstDate;

                        if (isDragging || isVisibleX)
                        {
                            visibleTasks.Add(task);
                        }
                    }
                }
            }

            SyncVisuals(visibleTasks);
        }
        // 2. 背景・罫線の再描画を予約 (OnRender -> DrawBorder が呼ばれる)
        // DrawBorder内で使用するために現在の可視範囲を保持しておく
        this._currentFirstRow = firstRow;
        this._currentLastRow = lastRow;

        this._currentFirstTime = firstDate;
        this._currentCenterTime = centerDate;
        this._currentLastTime = lastDate;



        var viewportChangedEventArgs = new RoutedEventArgs(ViewPortChangedEvent);
        RaiseEvent(viewportChangedEventArgs);

        InvalidateVisual();
    }

    private int FindRowIndexAtOffset(double y)
    {
        if (RowOffsets == null) return 0;
        int idx = Array.BinarySearch(RowOffsets, y);
        // 直接一致しない場合は「次の要素の補数」が返るためビット反転して調整
        if (idx < 0) idx = (~idx) - 1;
        return Math.Max(0, Math.Min(idx, RowOffsets.Length - 1));
    }

    private IBackgroundTimeRange? FindBackgroundTimeRangeAt(Point pos)
    {
        if (RowOffsets == null || RowHeights == null || _indexedBackgroundTimes.Count == 0)
            return null;

        foreach (var (rowIndex, ranges) in _indexedBackgroundTimes)
        {
            if (rowIndex >= RowOffsets.Length || rowIndex >= RowHeights.Length) continue;
            double y = RowOffsets[rowIndex];
            double height = RowHeights[rowIndex];
            if (pos.Y < y || pos.Y > y + height) continue;

            foreach (var item in ranges)
            {
                double x1 = TimeToPoint(item.StartTime);
                double x2 = TimeToPoint(item.EndTime);
                if (pos.X >= x1 && pos.X <= x2)
                    return item;
            }
        }

        return null;
    }

    private void OnItemsHostMouseMove(object sender, MouseEventArgs e)
    {
        if (BackgroundTimeRangePopupTemplate == null)
        {
            if (_backgroundPopup!.IsOpen)
            {
                _backgroundPopup.IsOpen = false;
                _hoveredBackgroundRange = null;
            }
            return;
        }

        var pos = e.GetPosition(_itemsHost);

        // タスクコントロールの上にマウスがある場合はBackgroundポップアップを表示しない
        bool overTask = false;
        VisualTreeHelper.HitTest(
            _itemsHost,
            null,
            r =>
            {
                if (r.VisualHit is FrameworkElement fe && fe.DataContext is ITimeRange)
                {
                    overTask = true;
                    return HitTestResultBehavior.Stop;
                }
                return HitTestResultBehavior.Continue;
            },
            new PointHitTestParameters(pos));

        var hit = overTask ? null : FindBackgroundTimeRangeAt(pos);

        if (hit != null)
        {
            _backgroundPopupPresenter!.ContentTemplate = BackgroundTimeRangePopupTemplate;
            if (!ReferenceEquals(hit, _hoveredBackgroundRange))
            {
                _hoveredBackgroundRange = hit;
                _backgroundPopupPresenter.Content = hit;
                _backgroundPopup!.IsOpen = false;
                _backgroundPopup.IsOpen = true;
            }
        }
        else
        {
            _backgroundPopup!.IsOpen = false;
            _hoveredBackgroundRange = null;
        }
    }

    private void OnItemsHostMouseLeave(object sender, MouseEventArgs e)
    {
        _backgroundPopup!.IsOpen = false;
        _hoveredBackgroundRange = null;
    }

    private DateTime FindColumnAtOffset(double x)
    {
        //var d = TimeTicks.FirstOrDefault(xx => xx.LeftPoint < x);

        //if (d != null)
        //{
        //    return d.DateValue;
        //}

        //return StartTime;
        if (TimeTicks == null || TimeTicks.Count == 0) return StartTime;

        // TimeTicks の LeftPoint のみを取り出した配列（またはリスト）があると高速
        // ※ 頻繁に呼ぶ場合は、TimeTicks.Select(t => t.LeftPoint).ToArray() をキャッシュしておくのが理想
        double[] offsets = TimeTicks.Select(t => t.LeftPoint).ToArray();

        int idx = Array.BinarySearch(offsets, x);

        if (idx < 0)
        {
            // 補数を使って「x 未満で最大の要素」を特定
            idx = (~idx) - 1;
        }

        // 範囲外チェック
        if (idx < 0) return StartTime;
        if (idx >= TimeTicks.Count) return TimeTicks.Last().DateValue;

        return TimeTicks[idx].DateValue;
    }


    protected override void OnRender(DrawingContext dc)
    {
        // UpdateVisibleRange で計算済みのインデックスを使用して描画
        DrawBorder(dc, _currentFirstRow, _currentLastRow);
        base.OnRender(dc);
    }

    #region Time Axis and Background



    public void DrawBorder(DrawingContext dc, int firstRow, int lastRow)
    {
        if (RowOffsets == null || RowHeights == null || _scrollViewer == null) return;

        // コンテンツ全体のサイズを取得
        // _itemsHost.Width がセットされていない場合は ActualWidth で代用
        double totalWidth = (_itemsHost != null && _itemsHost.Width > 0) ? _itemsHost.Width : ActualWidth;
        double totalHeight = RowOffsets.Length > 0 ? (RowOffsets.Last() + RowHeights.Last()) : ActualHeight;

        // 座標系をスクロールに合わせてオフセット
        var clipRect = new Rect(0, 0, ActualWidth, ActualHeight);
        var clipGeometry = new RectangleGeometry(clipRect);

        // 描画コンテキストにクリップ領域をプッシュ
        dc.PushClip(clipGeometry); dc.PushTransform(new TranslateTransform(-_scrollViewer.HorizontalOffset, -_scrollViewer.VerticalOffset));

        // 1. 全体背景（コントロールの Background）
        // Rect の幅を totalWidth にすることで、横スクロールしても背景が途切れない
        dc.DrawRectangle(Background, new Pen(BorderBrush, BorderThickness.Left), new Rect(0, 0, totalWidth, totalHeight));

        if (firstRow != -1 && lastRow != -1)
        {
            for (int i = firstRow; i <= lastRow; i++)
            {
                if (i >= RowOffsets.Length) break;

                double y = RowOffsets[i];
                double height = RowHeights[i];

                // 2. 行ごとの背景色 (BackgroundTimes)
                if (_indexedBackgroundTimes.TryGetValue(i, out var bgRanges))
                {
                    foreach (var item in bgRanges)
                    {
                        double x1 = TimeToPoint(item.StartTime);
                        double x2 = TimeToPoint(item.EndTime);
                        // 右端が 0 未満、または左端が全幅を超えている場合はスキップ
                        if (x2 < 0 || x1 > totalWidth) continue;
                        dc.DrawRectangle(item.Background, null, new Rect(x1, y, Math.Max(0, x2 - x1), height));
                    }
                }

                // 3. 非稼働領域 (NonWorkingTimes)
                if (_indexedNonWorkingTimes.TryGetValue(i, out var nonWorking))
                {
                    foreach (var range in nonWorking)
                    {
                        double x1 = TimeToPoint(range.StartTime);
                        double x2 = TimeToPoint(range.EndTime);
                        if (x2 < 0 || x1 > totalWidth) continue;
                        dc.DrawRectangle(NonWorkingRangeBackground, null, new Rect(x1, y, Math.Max(0, x2 - x1), height));
                    }
                }

                // 4. 横方向の罫線
                // 終点を ActualWidth ではなく totalWidth にする
                dc.DrawLine(_ruledLinePen, new Point(0, y), new Point(totalWidth, y));

                if (i == lastRow || i == RowOffsets.Length - 1)
                {
                    dc.DrawLine(_ruledLinePen, new Point(0, y + height), new Point(totalWidth, y + height));
                }
            }
        }

        // 5. 縦方向の罫線 (時間軸)
        if (TimeTicks != null)
        {
            foreach (var item in TimeTicks)
            {
                // 縦線の長さも totalHeight (全行分) にする
                dc.DrawLine(_ruledLinePen, new Point(item.LeftPoint, 0), new Point(item.LeftPoint, totalHeight));
            }
        }

        dc.Pop(); // Transform を解除
    }

    public void CalclateTimeAxis()
    {
        if (TimeTicks == null) return;
        var ticks = TimeTicks.ToList();

        var newTicks = TimeUnit == TimeUnitType.Normal ?
            TimeHelper.GenerateTicks(StartTime, EndTime, ScaleIncrement, SubScaleIncrement) :
            TimeHelper.GenerateTicksMonth(StartTime, EndTime, 1, MonthSubTickType.None);

        var newWidth = CalculateNewWidth(newTicks.mainTicks.Count);
        _contentWidth = newWidth; // ここで確定
        SetWidth(_contentWidth);

        TimeTicks.Clear();

        //foreach (var item in newTicks.mainTicks)
        //{
        //    TimeTicks.Add(new RuledLineData(this) { DateValue = item, LeftPoint = TimeToPoint(item) });
        //}

        var localTimeTicks = new List<RuledLineData>();
        var tf = new Typeface(FontFamily, FontStyle, FontWeight, FontStretch);
        for (int i = 0; i < newTicks.mainTicks.Count; i++) //1こ多いので減らす
        {
            var item = newTicks.mainTicks[i];
            string text = item.ToString(HeaderFormat);
            var brush = HeaderBrushSelector?.Invoke(this, item.Date) ?? Brushes.Black;
            var ft = new FormattedText(
                text,
                CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                tf,
                FontSize,
                brush,
                VisualTreeHelper.GetDpi(this).PixelsPerDip);
            var tickObject = new RuledLineData(this)
            {
                DateValue = item,
                LeftPoint = TimeUnit == TimeUnitType.Normal ? TimeScreenHelper.TimeToPoint(item, StartTime, EndTime, newWidth) : i * WidthPerRuledLine
                    ,
                FormattedText = ft

            };
            tickObject.Rect = new Rect(tickObject.LeftPoint, 0, WidthPerRuledLine, FontSize);
            localTimeTicks.Add(tickObject);
            //    if (i != ticks.Count - 1)
            //    {
            //        localTimeTickForColumn.Add(tickObject);
            //    }
        }
        TimeTicks.AddRange(localTimeTicks);

        SubTimeTicks.Clear();
        if (newTicks.subTicks.Count > 0)
        {
            var subTicks = TimeRangeChart.GenerateSubScaleTicks(this, newTicks.subTicks, this.SubScaleFormat, tf, newWidth);
            SubTimeTicks.AddRange(subTicks);

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
        if (_itemsHost == null) return;

        if (newWidth == double.NaN || newWidth <= 0)
        {
            _itemsHost.ClearValue(WidthProperty);

        }
        else
        {
            _itemsHost.Width = newWidth;
        }
    }

    internal static List<IRuledLine> GenerateSubScaleTicks(Control dp, List<DateTime> ticks, string subScaleFormat, Typeface tf, double newWidth)
    {
        var localSubTimeTicks = new List<IRuledLine>();

        if (dp is not TimeRangeChart timeChart) { return []; }

        DateTime startTime = timeChart.StartTime;
        DateTime endTime = timeChart.EndTime;


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
            var tickObject = new RuledLineData(timeChart!);
            tickObject.DateValue = item;

            var x = timeChart.TimeToPoint(item);
            tickObject.LeftPoint = x;


            tickObject.FormattedText = ft;


            localSubTimeTicks.Add(tickObject);

        }

        for (int i = 0; i < localSubTimeTicks.Count; i++)
        {
            var item = (RuledLineData)localSubTimeTicks[i];
            var rectLeft = i > 0 ? localSubTimeTicks[i - 1].LeftPoint : 0;
            var rectRight = i < localSubTimeTicks.Count - 1 ? localSubTimeTicks[i + 1].LeftPoint : newWidth;

            item.Rect = new(new Point(item.LeftPoint - (item.LeftPoint - rectLeft) / 2, 0), new Point(item.LeftPoint + (rectRight - item.LeftPoint) / 2, timeChart!.FontSize));
        }

        return localSubTimeTicks;

    }



    private void CalculateSortedNonWorkingTimes()
    {
        // ① 非稼働時間をソート＆マージ
        if (NonWorkingTimes == null || NonWorkingTimes.Count == 0)
        {
            _indexedNonWorkingTimes = [];
            return;
        }

        var newDict = new Dictionary<int, List<ITimeRangeRowIndex>>();

        // 1. 行ごとにグループ化
        foreach (var rowGroup in NonWorkingTimes.GroupBy(r => r.RowIndex))
        {
            int rowIndex = rowGroup.Key;

            // 2. その行の中で時間をソート
            var sorted = rowGroup.OrderBy(r => r.StartTime).ToList();
            var merged = new List<ITimeRangeRowIndex>();

            // 3. 重なっている非稼働時間をマージ (隙間を埋めて描画負荷を減らす)
            foreach (var r in sorted)
            {
                if (merged.Count == 0 || merged.Last().EndTime < r.StartTime)
                {
                    // 固定の型（FixedTimeRange等）でマージ結果を保持
                    merged.Add(new FixedTimeRange(r.StartTime, r.EndTime) { RowIndex = rowIndex });
                }
                else
                {
                    var last = merged[merged.Count - 1];
                    if (r.EndTime > last.EndTime)
                    {
                        // 重なっている場合は終了時間を更新
                        merged[merged.Count - 1] = new FixedTimeRange(last.StartTime, r.EndTime) { RowIndex = rowIndex };
                    }
                }
            }
            newDict[rowIndex] = merged;
        }

        _indexedNonWorkingTimes = newDict;
    }
    private void CalculateIndexedBackgroundTimes()
    {
        if (BackgroundTimes == null)
        {
            _indexedBackgroundTimes = [];
            return;
        }

        _indexedBackgroundTimes = BackgroundTimes
            .GroupBy(x => x.RowIndex)
            .ToDictionary(g => g.Key, g => g.ToList());
    }


    #endregion

    #region Height Control

    internal double[] RowHeights = Array.Empty<double>();
    private double[] RowOffsets = Array.Empty<double>();
    private Dictionary<int, object> ItemsSourceWithRowIndex = new Dictionary<int, object>();
    internal Dictionary<object, int> _dataStackLevelMap = new();

    /// <summary>
    /// 全行を走査し、スタックを考慮した各行の高さとオフセットを再計算します。
    /// </summary>
    public void UpdateLayoutMetrics()
    {
        if (ItemsSource is not IEnumerable<ITimeRangeRowIndex> allTasks) return;

        RebuildTasksByRow();

        // 1. 全タスクのスタックレベルを計算
        ComputeStackLevelsFromData(allTasks, clearMap: true);

        // 2. 行ごとの最大レベルを把握
        // 辞書から RowIndex ごとに最大の Value(Level) を探す
        var maxLevelByRow = new Dictionary<int, int>();
        foreach (var kvp in _dataStackLevelMap)
        {
            var task = (ITimeRangeRowIndex)kvp.Key;
            int level = kvp.Value;

            if (!maxLevelByRow.ContainsKey(task.RowIndex) || maxLevelByRow[task.RowIndex] < level)
            {
                maxLevelByRow[task.RowIndex] = level;
            }
        }


        var rowInfoMap = new Dictionary<int, (int MaxLevel, double MaxBaseTop)>();
        foreach (var kvp in _dataStackLevelMap)
        {
            var task = (ITimeRangeRowIndex)kvp.Key;
            int level = kvp.Value;

            if (!rowInfoMap.TryGetValue(task.RowIndex, out var info))
            {
                rowInfoMap[task.RowIndex] = (level, task.StackBaseTop);
            }
            else
            {
                rowInfoMap[task.RowIndex] = (
                    Math.Max(info.MaxLevel, level),
                    Math.Max(info.MaxBaseTop, task.StackBaseTop)
                );
            }
        }


        // --- 非稼働時間のインデックス化 ---
        _indexedNonWorkingTimes = NonWorkingTimes?
            .GroupBy(x => x.RowIndex)
            .ToDictionary(g => g.Key, g => g.ToList()) ?? new();

        // --- 背景色のインデックス化 ---
        _indexedBackgroundTimes = BackgroundTimes?
            .GroupBy(x => x.RowIndex)
            .ToDictionary(g => g.Key, g => g.ToList()) ?? new();

        int maxRowFromTasks = ItemsSource?.Any() == true ? ItemsSource.Max(t => t.RowIndex) : -1;
        int maxRowFromNonWorking = NonWorkingTimes?.Any() == true ? NonWorkingTimes.Max(t => t.RowIndex) : -1;
        int maxRowFromBackground = BackgroundTimes?.Any() == true ? BackgroundTimes.Max(t => t.RowIndex) : -1;
        int rowCount = Math.Max(Math.Max(maxRowFromTasks, maxRowFromNonWorking), maxRowFromBackground) + 1;
        // 画面上に表示すべき最低限の行数がある場合は、大きい方を取る。
        // タスク/背景/非稼働のいずれも持たない行（末尾の空行など）は最大RowIndexに現れず
        // 行数から漏れてしまうため、明示指定された RowCount を下限として採用する。
        // これを行わないと空行の高さが確保されず、RowHeader 側とスクロールがずれる。
        rowCount = Math.Max(rowCount, RowCount);

        var tempRowHeights = new double[rowCount];
        var tempRowOffsets = new double[rowCount];
        double currentOffset = 0;

        for (int i = 0; i < rowCount; i++)
        {
            tempRowOffsets[i] = currentOffset;


            double requiredHeight = ChartHeight; // デフォルトの最小高さ

            if (rowInfoMap.TryGetValue(i, out var info))
            {
                // 1. その行のスタック最大段数を取得

                double contentHeight = info.MaxBaseTop + (info.MaxLevel + 1) * (StackHeight + StackMargin);
                requiredHeight = Math.Max(ChartHeight, contentHeight);
            }

            tempRowHeights[i] = requiredHeight;
            currentOffset += requiredHeight;
        }

        // 4. TimeRangeChart へ通知
        if (_itemsHost != null)
        {
            RowOffsets = tempRowOffsets;
            RowHeights = tempRowHeights;
            _itemsHost.Height = currentOffset;


            var eventArgs = new RoutedEventArgs(RowHeightsChangedEvent, this);
            RaiseEvent(eventArgs);
        }
    }
    /// <summary>
    /// 特定の行のスタック状況から、チャート全体のメトリクスを部分更新します。
    /// </summary>
    public void UpdateMetricsForRows(params int[] targetRowIndices)
    {
        if (ItemsSource is not IEnumerable<ITimeRangeRowIndex> allTasks || RowOffsets == null) return;

        var draggingTask = _draggingControl?.DataContext as ITimeRangeRowIndex;
        var dragTargets = new HashSet<ITimeRangeRowIndex>();

        bool isActuallyDragging = _dragOccured && draggingTask != null;
        if (isActuallyDragging)
        {
            if (AllowMultiDrag && draggingTask!.IsSelected)
            {
                foreach (var t in ItemsSource.Cast<ITimeRangeRowIndex>().Where(x => x.IsSelected))
                    dragTargets.Add(t);
            }
            else
            {
                dragTargets.Add(draggingTask!);
            }
        }
        // 1. 指定された行に属するタスクのみスタックレベルを再計算
        bool isHeightChanged = false;
        foreach (var rowIndex in targetRowIndices)
        {
            var rowTasks = allTasks.Where(t => t.RowIndex == rowIndex);

            var otherTasks = rowTasks.Where(t => !dragTargets.Contains(t)).ToList();
            ComputeStackLevelsFromData(otherTasks, clearMap: false); // 部分的に更新

            //// 他のタスクの中での最大スタックレベルを取得

            // ドラッグ中のタスクを最下部に配置
            int finalMaxLevelInRow = -1;
            if (otherTasks.Any())
            {
                finalMaxLevelInRow = otherTasks.Max(t => _dataStackLevelMap.TryGetValue(t, out int l) ? l : 0);
            }
            // 3. この行に属するドラッグ対象タスクを、それぞれの位置での最下部に配置
            var dragTargetsInRow = dragTargets.Where(t => t.RowIndex == rowIndex);
            foreach (var t in dragTargetsInRow)
            {
                // スタック禁止タスクなら、計算せずに Level 0 固定
                if (!CanStackRange || !t.CanStack)
                {
                    _dataStackLevelMap[t] = 0;
                    continue;
                }

                // 各タスクの「今の位置」で、計算済みの otherTasks と重なりを判定
                // ★修正点：otherTasks の中でも「CanStack == true」のものだけを比較対象にする
                var overlappedTasks = otherTasks.Where(ot =>
                    ot.CanStack && // スタックしないタスクは判定から除外
                    ot.StartTime < t.EndTime &&
                    ot.EndTime > t.StartTime);

                int maxLevelAtPos = overlappedTasks.Any()
                    ? overlappedTasks.Max(ot => _dataStackLevelMap.TryGetValue(ot, out int l) ? l : 0)
                    : -1;

                // その場所での最下部 + 1
                int myLevel = maxLevelAtPos + 1;
                _dataStackLevelMap[t] = myLevel;

                // 行全体の高さに影響させる
                finalMaxLevelInRow = Math.Max(finalMaxLevelInRow, myLevel);
            }

            //その行の新しい高さを算出
            double maxBaseTop = 0;
            int maxLevel = 0;

            var tasksInRow = allTasks.Where(t => t.RowIndex == rowIndex).ToList();
            if (tasksInRow.Any())
            {
                maxLevel = tasksInRow.Max(t => _dataStackLevelMap.TryGetValue(t, out int l) ? l : 0);
                maxBaseTop = tasksInRow.Max(t => t.StackBaseTop);
            }

            double contentHeight = maxBaseTop + (maxLevel + 1) * (StackHeight + StackMargin);
            double newHeight = Math.Max(ChartHeight, contentHeight);

            // 3. 高さが変わった場合、それ以降の行の Offset を全てずらす
            double diff = newHeight - RowHeights[rowIndex];
            if (Math.Abs(diff) > 0.001)
            {
                isHeightChanged = true;
                RowHeights[rowIndex] = newHeight;
                for (int i = rowIndex + 1; i < RowOffsets.Length; i++)
                {
                    RowOffsets[i] += diff;
                }
                if (_itemsHost != null) _itemsHost.Height += diff;
            }
        }

        if (isHeightChanged)
        {

            // 4. 可視範囲のコントロールのみ位置を同期

            // 5. 罫線(Border)の再描画
            InvalidateVisual();
        }
        foreach (var kvp in _activeControls)
        {
            UpdateControlPosition(kvp.Value, (ITimeRangeRowIndex)kvp.Key);
        }
    }

    private void ComputeStackLevelsFromData(IEnumerable<ITimeRangeRowIndex>? allTasks, bool clearMap)
    {
        if (clearMap) _dataStackLevelMap.Clear();
        if (allTasks == null) return;

        var groupedByRow = allTasks.GroupBy(x => x.RowIndex);

        foreach (var rowGroup in groupedByRow)
        {
            // 1. スタックするタスクだけを抽出して計算
            var stackableTasks = rowGroup
                .Where(t => CanStackRange && t.CanStack)
                .OrderBy(x => x.StartTime)
                .ThenByDescending(x => x.EndTime)
                .ToList();

            var levelEnds = new List<DateTime>();

            foreach (var task in stackableTasks)
            {
                int assignedLevel = -1;
                for (int i = 0; i < levelEnds.Count; i++)
                {
                    if (task.StartTime >= levelEnds[i])
                    {
                        assignedLevel = i;
                        levelEnds[i] = task.EndTime;
                        break;
                    }
                }
                if (assignedLevel == -1)
                {
                    assignedLevel = levelEnds.Count;
                    levelEnds.Add(task.EndTime);
                }
                _dataStackLevelMap[task] = assignedLevel;
            }

            // 2. スタックしないタスクは後から問答無用で 0 に設定
            // これにより、通常のタスクの計算結果（levelEnds）に一切影響を与えない
            var nonStackableTasks = rowGroup.Where(t => !CanStackRange || !t.CanStack);
            foreach (var task in nonStackableTasks)
            {
                _dataStackLevelMap[task] = 0;
            }
        }
    }


    #endregion

    #region Items Management

    // 画面外に去ったコントロールを保管しておくスタック
    private readonly Stack<TimeRangeControl> _controlPool = new();

    // 現在画面に表示されているコントロールとデータの紐付け管理
    private readonly Dictionary<object, TimeRangeControl> _activeControls = new();

    /// <summary>
    /// 表示すべきTimeRangeControlを生成/再利用し、キャンバスに配置します。
    /// </summary>
    /// <param name="visibleTasks">可視範囲にあるタスクデータのリスト</param>
    public void SyncVisuals(IEnumerable<ITimeRangeRowIndex> visibleTasks)
    {
        if (_itemsHost == null || RowOffsets == null) return;
        var draggingTask = _draggingControl?.DataContext ?? _recentlyDroppedTask;
        _recentlyDroppedTask = null; // 次のSyncVisuals呼び出しでは保護しない
        // 1. 今回表示すべきデータの集合
        var visibleTaskSet = new HashSet<object>(visibleTasks);
        if (draggingTask != null)
        {
            visibleTaskSet.Add(draggingTask);
        }
        // 2. 画面外に去った（＝今回不要な）コントロールを回収
        var activeKeys = _activeControls.Keys.ToList(); // 走査用にコピー
        foreach (var key in activeKeys)
        {
            //ドラッグ中のものは回収しない
            if (key == draggingTask) continue;

            if (!visibleTaskSet.Contains(key))
            {
                var ctrl = _activeControls[key];

                //マウスキャプチャのあるものは回収しない
                if (ctrl.IsMouseCaptured) continue;

                if (key is INotifyPropertyChanged oldNotifiable)
                    oldNotifiable.PropertyChanged -= OnActiveItemPropertyChanged;

                _itemsHost.Children.Remove(ctrl);
                ctrl._currentMode = TimeRangeControl.HitTestMode.None; // プール返却時に古い状態をリセット
                _controlPool.Push(ctrl);
                _activeControls.Remove(key);
            }
        }

        // 3. 新しく画面に入るデータを描画
        foreach (var task in visibleTasks)
        {
            bool isNew = false;
            // ドラッグ中のタスクは、既に _activeControls にあるはずなので
            // TryGetValue で見つかり、下の新規作成ロジックはスルーされる
            if (!_activeControls.TryGetValue(task, out var ctrl))
            {
                // プールに在庫があれば使い、なければ新しく作る
                ctrl = _controlPool.Count > 0 ? _controlPool.Pop() : CreateTimeRangeControl();
                isNew = true;

                // データの紐付け（DataContextの更新）
                ctrl.Content = task;
                ctrl.DataContext = task;
                ctrl.ContentTemplate = this.DataTemplate;
                ctrl.ContentTemplateSelector = this.DataTemplateSelector;

                TryBindDefaultProperties(ctrl);

                if (task is INotifyPropertyChanged notifiable)
                    notifiable.PropertyChanged += OnActiveItemPropertyChanged;

                _itemsHost.Children.Add(ctrl);
                _activeControls[task] = ctrl;
            }

            // 4. 配置の更新（ここは共通処理）
            UpdateControlPosition(ctrl, task);
            if (isNew)
            {
                ctrl.InvalidateArrange();
            }
        }
    }

    private void OnActiveItemPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender == null) return;
        if (e.PropertyName != nameof(ITimeRange.StartTime) && e.PropertyName != nameof(ITimeRange.EndTime)) return;
        if (_activeControls.TryGetValue(sender, out var ctrl) && sender is ITimeRangeRowIndex task)
            UpdateControlPosition(ctrl, task);
    }

    private TimeRangeControl CreateTimeRangeControl()
    {
        var ctrl = new TimeRangeControl();
        ctrl.MouseLeftButtonDown += OnControlMouseDown;


        if (this.TimeRangeStyle != null)
        {
            ctrl.Style = this.TimeRangeStyle;
        }
        return ctrl;
    }


    private double TimeToPoint(DateTime time)
    {
        switch (TimeUnit)
        {
            case TimeUnitType.Normal:
                return TimeScreenHelper.TimeToPoint(time, StartTime, EndTime, _contentWidth);
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
                return TimeScreenHelper.PointToTime(point, StartTime, EndTime, _contentWidth, minSpan ?? TimeSpan.Zero);
            case TimeUnitType.Month:
                //return TimeScreenHelper.PointToTimeMonth(point, StartTime, EndTime, ActualWidth == 0 && Width != double.NaN ? Width : ActualWidth, minSpan ?? MinSpan, TimeTicks);
                return TimeScreenHelper.PointToTimeMonth(point, WidthPerRuledLine, minSpan ?? TimeSpan.Zero, TimeTicks);
            default:
                break;
        }
        return StartTime;
    }
    private static void TryBindDefaultProperties(TimeRangeControl range)
    {
        if (range.DataContext is ITimeRange timeRange)
        {

            var startBinding = new Binding(nameof(ITimeRange.StartTime))
            {
                Source = timeRange,
                Mode = BindingMode.TwoWay
            };
            BindingOperations.SetBinding(range, TimeRangeControl.StartTimeProperty, startBinding);

            var endBinding = new Binding(nameof(ITimeRange.EndTime))
            {
                Source = timeRange,
                Mode = BindingMode.TwoWay
            };
            BindingOperations.SetBinding(range, EndTimeProperty, endBinding);

            var workingTimeBinding = new Binding(nameof(ITimeRange.WorkingSpan))
            {
                Source = timeRange,
                Mode = BindingMode.TwoWay
            };
            BindingOperations.SetBinding(range, TimeRangeControl.WorkingSpanProperty, workingTimeBinding);

            var actualTimeBinding = new Binding(nameof(ITimeRange.ActualSpan))
            {
                Source = timeRange,
                Mode = BindingMode.OneWay
            };
            BindingOperations.SetBinding(range, TimeRangeControl.ActualSpanProperty, actualTimeBinding);

        }


    }

    public void RefreshCall()
    {
        Refresh();
    }

    public void Refresh()
    {
        this.InvalidateVisual();
    }

    private void UpdateControlPosition(TimeRangeControl ctrl, ITimeRangeRowIndex task)
    {
        SetTimeAxisPosition(this, ctrl, task);

        if (task.RowIndex >= 0 && task.RowIndex < RowOffsets.Length)
        {
            double rowTop = RowOffsets[task.RowIndex];
            int stackLevel = _dataStackLevelMap.TryGetValue(task, out int level) ? level : 0;

            double stackOffset = stackLevel * (StackHeight + StackMargin);
            ctrl.Top = rowTop + task.StackBaseTop + stackOffset;
            if (!ctrl.IsMilestone)
            {
                ctrl.Height = StackHeight;
            }
            if (ctrl.IsMilestone)
            {
                // マイルストーンの場合、Left位置を「中心」にするための補正が必要な場合があります
                // ここでは XAML の RenderTransform や Margin で補正する前提で、計算上の Left をそのままセット
                Canvas.SetLeft(ctrl, ctrl.Left);

                // 重要：マイルストーンは Width/Height を明示的に double.NaN (Auto) に戻す
                // これにより、Style で設定された Width/Height が有効になります
                ctrl.Width = double.NaN;
            }
            else
            {
                Canvas.SetLeft(ctrl, ctrl.Left);
            }
            Canvas.SetTop(ctrl, ctrl.Top);
        }
    }

    private static void SetTimeAxisPosition(TimeRangeChart chart, TimeRangeControl range, ITimeRangeRowIndex task)
    {
        if (task == null || range == null) return;

        SetLeft(chart, range, task);

        if ((range.EndTime == DateTime.MinValue || range.EndTime == DateTime.MaxValue))
        {
            if (range.WorkingSpan != null)
            {
                var nw = chart._indexedNonWorkingTimes.TryGetValue(task.RowIndex, out var n) ? n : null;

                range.EndTime = TimeHelper.CalcEndTime(range.StartTime, range.WorkingSpan.Value, nw);
            }
            else
            {
                range.EndTime = TimeHelper.CalcEndTime<ITimeRange>(range.StartTime, range.ActualSpan);
            }

        }

        SetRight(chart, range, task);

        SetTaskWidth(range);
    }

    private static void SetLeft(TimeRangeChart chart, TimeRangeControl range, ITimeRange task)
    {
        if (chart != null)
        {
            range.Left = chart.TimeToPoint(task.StartTime);
        }

    }

    private static void SetRight(TimeRangeChart chart, TimeRangeControl range, ITimeRange task)
    {
        if (chart != null)
        {

            if (range.IsMilestone)
            {
                //range.Right = chart.TimeToPoint(task.EndTime);
            }
            else
            {
                range.Right = chart.TimeToPoint(task.EndTime);
            }
        }
    }
    private static void SetTaskWidth(TimeRangeControl range)
    {

        //if (!range.IsMilestone)
        //{
        //    var width = range.Right - range.Left;
        //    if (width != range.Width && width >= 0 && !range.IsMilestone) range.TaskWidth = width;
        //    range.Width = width;

        //}
        if (range.IsMilestone) return;

        var width = range.Right - range.Left;
        if (width >= 0)
        {
            // 以前の Width と異なる場合のみ更新
            if (width != range.TaskWidth) range.TaskWidth = width;
            range.Width = width;
        }

    }


    #endregion


    #region Chart background click / selection

    // Registered directly on _itemsHost (Canvas) so that ScrollViewer.OnMouseLeftButtonDown
    // cannot suppress it.  TimeRangeControl sets e.Handled=true, so this handler is
    // skipped for task-bar clicks — only background clicks reach here.
    private void OnItemsHostBackgroundClick(object sender, MouseButtonEventArgs e)
    {
        if (ChartSelectionMode == ChartSelectionMode.None) return;

        // _itemsHost coordinates are already in canvas space (no scroll offset needed)
        var pos = e.GetPosition(_itemsHost);

        int rowIndex = GetRowIndexFromMouse(pos.Y);
        if (rowIndex < 0) return;

        _selectedRowIndex = rowIndex;
        _selectedDateTime = PointToTime(pos.X, null);

        UpdateSelectionRect();

        RaiseEvent(new ChartBackgroundClickEventArgs(
            ChartBackgroundLeftClickEvent, _selectedRowIndex, _selectedDateTime));
    }

    protected override void OnMouseRightButtonDown(MouseButtonEventArgs e)
    {
        base.OnMouseRightButtonDown(e);

        // 右クリックがタスク(TimeRangeControl)上のときは、行(空欄)用メニューを開かず
        // タスク自身のコンテキストメニューに委ねる。
        // （以前は左クリックで設定された古い _selectedRowIndex を使っていたため、
        //   一度行を選択するとタスク右クリックでも行メニューが開いてしまっていた）
        if (IsWithinTask(e.OriginalSource as DependencyObject)) return;
        if (_itemsHost == null) return;

        // 現在のマウス位置から行を判定する（古い _selectedRowIndex は使わない）
        var pos = e.GetPosition(_itemsHost);
        int rowIndex = GetRowIndexFromMouse(pos.Y);
        if (rowIndex < 0) return;

        var clickedTime = PointToTime(pos.X, null);

        RaiseEvent(new ChartBackgroundClickEventArgs(
            ChartBackgroundRightClickEvent, rowIndex, clickedTime));

        e.Handled = true;
    }

    private static bool IsWithinTask(DependencyObject? source)
    {
        while (source != null)
        {
            if (source is TimeRangeControl) return true;

            source = (source is Visual || source is System.Windows.Media.Media3D.Visual3D)
                ? VisualTreeHelper.GetParent(source)
                : LogicalTreeHelper.GetParent(source);
        }
        return false;
    }

    #endregion

    #region Dragging

    private void OnControlMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is not TimeRangeControl ctrl || ctrl.DataContext is not ITimeRangeRowIndex task) return;

        bool isShiftDown = Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift);
        bool isCtrlDown = Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl);

        if (!task.IsSelected)
        {
            // 選択されていないタスクをクリックした場合
            if (!isShiftDown && !isCtrlDown)
            {
                // Ctrl/Shiftなしなら、一旦すべての選択を解除してから自分を選択
                ClearAllSelection();
            }
            task.IsSelected = true;
        }


        _draggingControl = ctrl;
        _dragMode = GetHitMode(ctrl, e.GetPosition(ctrl)); // 前述の判定ロジック

        var mousePos = e.GetPosition(this);
        double absoluteX = mousePos.X + (_scrollViewer?.HorizontalOffset ?? 0);
        _lastMousePosition = e.GetPosition(this); // Chart基準の座標


        _dragStartMouseTime = PointToTime(absoluteX, null);
        _originalRowIndex = task.RowIndex;
        _dragStartOffsetFromStart = _dragStartMouseTime - task.StartTime;

        _originalStartTime = task.StartTime;
        _originalEndTime = task.EndTime;
        _originalActualSpan = task.ActualSpan;
        _originalWorkingSpan = task.WorkingSpan;
        _originalRowIndex = task.RowIndex;


        ctrl.CaptureMouse();

        var dragEventArgs = new DragDropRangeEventArgs(TaskDragStartEvent);
        dragEventArgs.DragData = task;
        dragEventArgs.DragStartTime = task.StartTime;
        dragEventArgs.DragEndTime = task.EndTime;
        dragEventArgs.DragWorkingSpan = task.WorkingSpan ?? TimeSpan.Zero;
        dragEventArgs.DragActualSpan = task.ActualSpan;
        dragEventArgs.Cancel = false;

        RaiseEvent(dragEventArgs);
        if (dragEventArgs.Cancel)
        {
            //Cancel drag
            CancelDrag();
        }


        e.Handled = true;


    }

    protected void MoveAnotherRow(ITimeRangeRowIndex task, int newRowIndex)
    {
        var oldIndex = task.RowIndex;
        _indexedTasks[oldIndex].Remove(task);

        // 1. モデルの行インデックスを更新
        task.RowIndex = newRowIndex;

        _indexedTasks[task.RowIndex].Add(task);

        // 2. スタック（重なり）の再計算を要求
        // ここでチャート全体のスタック計算ロジック（以前作ったもの）を呼び出す
        RefreshStackLayout(oldIndex);

    }

    // ドラッグと判定する最小移動距離（ピクセル）。これ未満の移動はクリックとして扱い、タスクを動かさない。
    private const double DragThresholdPixels = 4.0;

    protected override void OnMouseMove(MouseEventArgs e)
    {
        if (_draggingControl == null || _draggingControl.DataContext is not ITimeRangeRowIndex task)
        {
            base.OnMouseMove(e);
            return;
        }

        // 1. 現在のマウス位置を「時間」に変換
        var currentMousePos = e.GetPosition(this);

        // マウスダウン位置からの移動距離が閾値未満はクリックとみなしてタスクを動かさない
        var dragDelta = currentMousePos - _lastMousePosition;
        if (!_dragOccured && dragDelta.Length < DragThresholdPixels) return;
        double absoluteX = currentMousePos.X + (_scrollViewer?.HorizontalOffset ?? 0);
        var currentMousePointTime = PointToTime(absoluteX, null);
        var currentMouseTime = PointToTime(absoluteX, _draggingControl.MinSpan ?? MinSpan);

        var nonWorkingTimes = _indexedNonWorkingTimes.TryGetValue(task.RowIndex, out var nw) ? nw : null;
        HandleAutoScroll(currentMousePos);



        var dragTargets = new List<ITimeRangeRowIndex>();
        if (AllowMultiDrag && task.IsSelected)
        {
            // 選択されている全タスクを対象にする
            dragTargets.AddRange(ItemsSource.Cast<ITimeRangeRowIndex>().Where(t => t.IsSelected));
        }
        else
        {
            // 単体、あるいは選択されていないものを掴んだ場合はそれのみ
            dragTargets.Add(task);
        }

        switch (_dragMode)
        {
            case TimeRangeControl.HitTestMode.Move:
                // 開始時のStartTimeに、マウスの移動時間分を足して移動

                DateTime moveNewStart = TimeHelper.RoundToNearestIncrement(currentMousePointTime - _dragStartOffsetFromStart, _draggingControl.MinSpan ?? MinSpan);

                // チャートの時間範囲外への移動を防ぐ（オートスクロール等で範囲外に出るのを防止）
                var taskDuration = task.EndTime - task.StartTime;
                if (moveNewStart < StartTime) moveNewStart = StartTime;
                if (moveNewStart + taskDuration > EndTime) moveNewStart = EndTime - taskDuration;

                TimeSpan moveDiff = moveNewStart - task.StartTime;

                if (moveDiff != TimeSpan.Zero)
                {
                    foreach (var t in dragTargets)
                    {
                        // 全員の時間をズラす
                        var nwMove = _indexedNonWorkingTimes.TryGetValue(t.RowIndex, out var n) ? n : null;
                        t.MoveTo(t.StartTime + moveDiff, _draggingControl.IgnoreNonWorkingTimes ? null : nwMove);
                    }
                    _dragOccured = true;
                }

                // --- 縦方向の移動---
                if (CanDragTasksBetweenRows && task.CanDragTasksBetweenRows)
                {
                    double absoluteY = currentMousePos.Y + (_scrollViewer?.VerticalOffset ?? 0);
                    int newRowIndex = GetRowIndexFromMouse(absoluteY);

                    // 行が変わった場合のみ処理
                    if (newRowIndex != -1 && newRowIndex != task.RowIndex)
                    {
                        var previewTaskDropArgs = new DropOnRowEventArgs(PreviewTaskDropEvent, currentMouseTime, task);
                        previewTaskDropArgs.OldRowIndex = _originalRowIndex;
                        previewTaskDropArgs.NewRowIndex = newRowIndex;

                        RaiseEvent(previewTaskDropArgs);

                        if (!previewTaskDropArgs.Cancel)
                        {

                            MoveAnotherRow(task, newRowIndex);
                            if (!_dropBetweenRowsOccured) _dropBetweenRowsOccured = true;
                        }

                    }
                }

                break;

            case TimeRangeControl.HitTestMode.ResizeLeft:
                var newStart = currentMouseTime;
                TimeSpan resizeDiffLeft = newStart - task.StartTime;
                if (resizeDiffLeft != TimeSpan.Zero)
                {
                    foreach (var t in dragTargets)
                    {
                        var nwExpandLeft = _indexedNonWorkingTimes.TryGetValue(t.RowIndex, out var n) ? n : null;
                        // 全員の開始時間をリーダーと同じ変化量だけ増減させる
                        t.ExpandStartTime(t.StartTime + resizeDiffLeft, _draggingControl.IgnoreNonWorkingTimes ? null : nwExpandLeft);
                    }
                    _dragOccured = true;
                }


                break;

            case TimeRangeControl.HitTestMode.ResizeRight:

                var newEnd = currentMouseTime;
                TimeSpan resizeDiff = newEnd - task.EndTime;

                if (resizeDiff != TimeSpan.Zero)
                {
                    foreach (var t in dragTargets)
                    {
                        var nwExpandRight = _indexedNonWorkingTimes.TryGetValue(t.RowIndex, out var n) ? n : null;
                        // 全員の終了時間をリーダーと同じ変化量だけ増減させる
                        t.ExpandEndTime(t.EndTime + resizeDiff, _draggingControl.IgnoreNonWorkingTimes ? null : nwExpandRight);
                    }
                    _dragOccured = true;
                }


                break;
        }

        _draggingControl.StartTime = task.StartTime;
        _draggingControl.EndTime = task.EndTime;
        _draggingControl.ActualSpan = task.ActualSpan;
        _draggingControl.WorkingSpan = task.WorkingSpan;

        // 3. コントロールの見た目を更新
        // task.StartTime/EndTime は既存関数によって整合性が保たれた状態になっている
        if (_dragOccured || _dropBetweenRowsOccured)
        {
            if (!task.IsSelected) task.IsSelected = true;

            var affectedRowIndices = dragTargets.Select(t => t.RowIndex).Distinct();
            foreach (var rowIndex in affectedRowIndices)
            {
                UpdateMetricsForRows(rowIndex);
            }

            // 画面に表示されているコントロールすべての座標を再同期
            // (選択中のタスクが別の行に影響を与えている可能性があるため)
            foreach (var kvp in _activeControls)
            {
                UpdateControlPosition(kvp.Value, (ITimeRangeRowIndex)kvp.Key);
            }

            var eventArgs = new RoutedEventArgs(RowHeightsChangedEvent, this);
            RaiseEvent(eventArgs);
        }


    }
    private void RefreshStackLayout(int newRowIndex)
    {
        //// 1. 全タスクの重なり順（StackLevel）を計算し直す既存のメソッドを呼ぶ
        UpdateMetricsForRows(newRowIndex);
    }

    private void HandleAutoScroll(Point mousePos)
    {
        if (_scrollViewer == null) return;
        double scrollMargin = 20; // 画面端から何pxでスクロール開始するか
        double scrollSpeed = 10;  // 1フレームあたりのスクロール量

        // 横方向
        if (mousePos.X > _scrollViewer.ViewportWidth - scrollMargin)
            _scrollViewer.ScrollToHorizontalOffset(_scrollViewer.HorizontalOffset + scrollSpeed);
        else if (mousePos.X < scrollMargin)
            _scrollViewer.ScrollToHorizontalOffset(_scrollViewer.HorizontalOffset - scrollSpeed);

        // 縦方向
        if (mousePos.Y > _scrollViewer.ViewportHeight - scrollMargin)
            _scrollViewer.ScrollToVerticalOffset(_scrollViewer.VerticalOffset + scrollSpeed);
        else if (mousePos.Y < scrollMargin)
            _scrollViewer.ScrollToVerticalOffset(_scrollViewer.VerticalOffset - scrollSpeed);
    }

    protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
    {
        if (_draggingControl == null || _draggingControl.DataContext is not ITimeRangeRowIndex task)
        {
            ClearAllSelection();
            return;
        }
        var currentMousePos = e.GetPosition(this);
        double absoluteX = currentMousePos.X + (_scrollViewer?.HorizontalOffset ?? 0);
        var currentMouseTime = PointToTime(absoluteX, _draggingControl.MinSpan ?? MinSpan);

        if (_draggingControl != null)
        {
            if (_dragOccured || _dropBetweenRowsOccured)
            {
                if (_dropBetweenRowsOccured)
                {
                    var dragEventArgs = new DropOnRowEventArgs(TaskDropEvent, currentMouseTime, task);
                    dragEventArgs.DragStartPoint = _dragStartMouseTime;
                    dragEventArgs.OldRowIndex = _originalRowIndex;
                    dragEventArgs.NewRowIndex = task.RowIndex;

                    RaiseEvent(dragEventArgs);
                    if (dragEventArgs.Cancel)
                    {
                        MoveAnotherRow(task, _originalRowIndex);
                    }
                }
                else if (_dragOccured)
                {
                    var dragEventArgs = new DragDropRangeEventArgs(TaskDragEndEvent);
                    dragEventArgs.DragStartTime = _originalStartTime;
                    dragEventArgs.DragEndTime = _originalEndTime;
                    dragEventArgs.DragWorkingSpan = _originalWorkingSpan ?? TimeSpan.Zero;
                    dragEventArgs.DragActualSpan = _originalActualSpan;

                    dragEventArgs.DragData = task;
                    dragEventArgs.DropStartTime = task.StartTime;
                    dragEventArgs.DropWorkingSpan = task.WorkingSpan;
                    dragEventArgs.DropActualSpan = task.ActualSpan;
                    dragEventArgs.Cancel = false;

                    RaiseEvent(dragEventArgs);

                    if (dragEventArgs.Cancel)
                    {
                        task.StartTime = _originalStartTime;
                        task.EndTime = _originalEndTime;
                        if (_originalWorkingSpan.HasValue) task.WorkingSpan = _originalWorkingSpan.Value;
                        task.ActualSpan = _originalActualSpan;
                    }
                }

            }
            else
            {
                // Shiftキーが押されているか確認
                bool isShiftDown = Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift);

                if (!isShiftDown || !AllowMultiSelection)
                {
                    // 他のすべての項目の選択を解除する
                    // ItemsSource が ITimeRangeRowIndex のコレクションであることを想定
                    foreach (var item in ItemsSource)
                    {
                        if (item != task)
                        {
                            item.IsSelected = false;
                        }
                    }
                }
                task.IsSelected = true;

            }

            _recentlyDroppedTask = _draggingControl.DataContext; // ドラッグ終了直後の保護用
            _draggingControl.ReleaseMouseCapture();
            _draggingControl = null;
            _dragOccured = false;
            _dropBetweenRowsOccured = false;
        }
    }

    private void ClearAllSelection()
    {
        if (ItemsSource is not IEnumerable<ITimeRangeRowIndex> allTasks) return;

        foreach (var task in allTasks)
        {
            if (task.IsSelected)
            {
                task.IsSelected = false;
            }
        }
    }
    private HitTestMode GetHitMode(TimeRangeControl ctrl, Point relativePos)
    {
        return ctrl._currentMode;
    }
    private int GetRowIndexFromMouse(double y)
    {
        if (RowOffsets == null || RowOffsets.Length == 0) return -1;

        // Y座標がスクロールの影響を受けている場合は、HorizontalOffset/VerticalOffsetを考慮
        // OnMouseMoveで e.GetPosition(this) を使っているなら、そのままでOK
        int idx = Array.BinarySearch(RowOffsets, y);

        if (idx < 0)
        {
            // 直接一致しない場合、(~idx) - 1 がその座標が含まれる行のインデックスになる
            idx = (~idx) - 1;
        }

        // 最後に、見つけた行の高さの範囲内（Bottomを超えていないか）をチェック
        if (idx >= 0 && idx < RowOffsets.Length)
        {
            double top = RowOffsets[idx];
            double bottom = top + RowHeights[idx];
            if (y >= top && y <= bottom)
            {
                return idx;
            }
        }

        return -1;
    }

    protected void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        // ドラッグ中に背後のデータが入れ替わったら、即座にキャプチャを離す
        if (IsDragging)
        {
            CancelDrag();
        }
    }

    private void CancelDrag()
    {
        if (_draggingControl == null) return;

        _draggingControl.ReleaseMouseCapture();
        _draggingControl = null;
        _dragOccured = false;
        // 必要に応じてViewModelの値を元の位置にロールバック
    }

    #endregion
}
