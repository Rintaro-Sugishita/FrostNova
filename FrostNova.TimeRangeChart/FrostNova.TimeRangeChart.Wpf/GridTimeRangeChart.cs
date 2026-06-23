using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.DirectoryServices;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Permissions;
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
using System.Xml.Linq;
using CommunityToolkit.Mvvm.Input;
using FrostNova.Core.Common;
using FrostNova.Core.Wpf.Behaviors;
using FrostNova.Core.Wpf.VisualHelpers;
using FrostNova.TimeRangeChart.TimeRangeHelpers;
using FrostNova.TimeRangeChart.Wpf.TimeRangeHelpers;
using Microsoft.Xaml.Behaviors;

namespace FrostNova.TimeRangeChart.Wpf
{


    [TemplatePart(Name = "PART_RowHeaderListBox", Type = typeof(ListBox))]
    [TemplatePart(Name = "PART_RowHeaderDataGrid", Type = typeof(DataGrid))]
    [TemplatePart(Name = "PART_Chart", Type = typeof(TimeRangeChart))]
    [TemplatePart(Name = "PART_ColumnGrid", Type = typeof(ColumnHeaderRenderLayer))]
    [TemplatePart(Name = "PART_ChartScrollViewer", Type = typeof(ScrollViewer))]
    [TemplatePart(Name = "PART_RowScrollViewer", Type = typeof(ScrollViewer))]
    [TemplatePart(Name = "PART_ColumnScrollViewer", Type = typeof(ScrollViewer))]
    [TemplatePart(Name = "PART_ColumnRightSpacer", Type = typeof(Border))]
    [TemplatePart(Name = "PART_RowBottomSpacer", Type = typeof(Border))]
    [TemplatePart(Name = "PART_Footer", Type = typeof(FooterRenderLayer))]
    [TemplatePart(Name = "PART_FooterScrollViewer", Type = typeof(ScrollViewer))]

    public class GridTimeRangeChart : Control
    {
        //TODO: DragEvent


        static GridTimeRangeChart()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(GridTimeRangeChart), new FrameworkPropertyMetadata(typeof(GridTimeRangeChart)));
        }

        public GridTimeRangeChart()
        {

        }

        #region cashe of the children

        internal ListBox _rowHeaderListBox = null!;
        internal DataGrid? _rowHeaderDataGrid = null;
        internal TimeRangeChart _chart = null!;
        internal ColumnHeaderRenderLayer _columnHeaderLayer = null!;
        internal ScrollViewer _chartScrollViewer = null!;
        internal Border _columnRightSpacer = null!;
        internal Border _rowBottomSpacer = null!;
        internal FooterRenderLayer? _footerLayer;
        internal ScrollViewer? _footerScrollViewer;

        #endregion

        #region DependencyProperties

        #region Time properties

        public DateTime StartTime
        {
            get { return (DateTime)GetValue(StartTimeProperty); }
            set { SetValue(StartTimeProperty, value); }
        }
        public static readonly DependencyProperty StartTimeProperty = DependencyProperty.Register(nameof(StartTime), typeof(DateTime), typeof(GridTimeRangeChart), new FrameworkPropertyMetadata(DateTime.Now.AddDays(-3), FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public DateTime EndTime
        {
            get { return (DateTime)GetValue(EndTimeProperty); }
            set { SetValue(EndTimeProperty, value); }
        }
        public static readonly DependencyProperty EndTimeProperty = DependencyProperty.Register(nameof(EndTime), typeof(DateTime), typeof(GridTimeRangeChart), new FrameworkPropertyMetadata(DateTime.Now.AddDays(9), FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));


        public TimeSpan ScaleIncrement
        {
            get { return (TimeSpan)GetValue(ScaleIncrementProperty); }
            set
            {
                SetValue(ScaleIncrementProperty, value);
            }
        }
        public static readonly DependencyProperty ScaleIncrementProperty = DependencyProperty.Register(nameof(ScaleIncrement), typeof(TimeSpan), typeof(GridTimeRangeChart), new PropertyMetadata(new TimeSpan(1, 0, 0)));

        public TimeSpan SubScaleIncrement
        {
            get { return (TimeSpan)GetValue(SubScaleIncrementProperty); }
            set { SetValue(SubScaleIncrementProperty, value); }
        }
        public static DependencyProperty SubScaleIncrementProperty = DependencyProperty.Register(nameof(SubScaleIncrement), typeof(TimeSpan), typeof(GridTimeRangeChart), new PropertyMetadata(new TimeSpan(1, 0, 0)));
        public string SubScaleFormat
        {
            get { return (string)GetValue(SubScaleFormatProperty); }
            set { SetValue(SubScaleFormatProperty, value); }
        }
        public static DependencyProperty SubScaleFormatProperty = DependencyProperty.Register(nameof(SubScaleFormat), typeof(string), typeof(GridTimeRangeChart));



        public TimeSpan MinSpan
        {
            get { return (TimeSpan)GetValue(MinSpanProperty); }
            set { SetValue(MinSpanProperty, value); }
        }
        public static readonly DependencyProperty MinSpanProperty = DependencyProperty.Register(nameof(MinSpan), typeof(TimeSpan), typeof(GridTimeRangeChart), new PropertyMetadata(TimeSpan.FromMinutes(15)));


        public TimeUnitType TimeUnit
        {
            get { return (TimeUnitType)GetValue(TimeUnitProperty); }
            set { SetValue(TimeUnitProperty, value); }
        }
        public static DependencyProperty TimeUnitProperty = DependencyProperty.Register(nameof(TimeUnit), typeof(TimeUnitType), typeof(GridTimeRangeChart), new PropertyMetadata(TimeUnitType.Normal));

        public int MonthSpan
        {
            get { return (int)GetValue(MonthSpanProperty); }
            set { SetValue(MonthSpanProperty, value); }
        }
        public static DependencyProperty MonthSpanProperty = DependencyProperty.Register(nameof(MonthSpan), typeof(int), typeof(GridTimeRangeChart), new PropertyMetadata(1));

        public string HeaderFormat
        {
            get { return (string)GetValue(HeaderFormatProperty); }
            set { SetValue(HeaderFormatProperty, value); }
        }
        public static DependencyProperty HeaderFormatProperty = DependencyProperty.Register(nameof(HeaderFormat), typeof(string), typeof(GridTimeRangeChart));
        public Func<TimeRangeChart, DateTime, Brush> HeaderBrushSelector
        {
            get { return (Func<TimeRangeChart, DateTime, Brush>)GetValue(HeaderBrushSelectorProperty); }
            set { SetValue(HeaderBrushSelectorProperty, value); }
        }
        public static DependencyProperty HeaderBrushSelectorProperty = DependencyProperty.Register(nameof(HeaderBrushSelector), typeof(Func<TimeRangeChart, DateTime, Brush>), typeof(GridTimeRangeChart), new PropertyMetadata(new Func<TimeRangeChart, DateTime, Brush>((gridChart, d) => DefaultHeaderSelector(gridChart, d))));


        internal static Brush DefaultHeaderSelector(TimeRangeChart gridChart, DateTime date)
        {
            if (gridChart.TimeUnit == TimeUnitType.Month) return Brushes.Black;

            // 時刻レベルの表示（スケールが1日未満）では曜日カラーを適用しない
            if (gridChart.ScaleIncrement < TimeSpan.FromDays(1)) return Brushes.Black;

            var b = date.DayOfWeek switch
            {
                DayOfWeek.Saturday => Brushes.Blue,
                DayOfWeek.Sunday => Brushes.Red,
                _ => Brushes.Black
            };
            b.Freeze();
            return b;
        }

        #endregion

        #region Drag Controls

        public bool CanStackRange
        {
            get { return (bool)GetValue(CanStackRangeProperty); }
            set { SetValue(CanStackRangeProperty, value); }
        }
        public static DependencyProperty CanStackRangeProperty = DependencyProperty.Register(nameof(CanStackRange), typeof(bool), typeof(GridTimeRangeChart), new PropertyMetadata(false));

        public bool CanDragTasksBetweenRows
        {
            get { return (bool)GetValue(CanDragTasksBetweenRowsProperty); }
            set { SetValue(CanDragTasksBetweenRowsProperty, value); }
        }
        public static DependencyProperty CanDragTasksBetweenRowsProperty = DependencyProperty.Register(nameof(CanDragTasksBetweenRows), typeof(bool), typeof(GridTimeRangeChart), new PropertyMetadata(true));

        /// <summary>
        /// Get or Set the height of task when stack.
        /// </summary>
        public double StackHeight
        {
            get { return (double)GetValue(StackHeightProperty); }
            set { SetValue(StackHeightProperty, value); }
        }
        public static DependencyProperty StackHeightProperty = DependencyProperty.Register(nameof(StackHeight), typeof(double), typeof(GridTimeRangeChart), new PropertyMetadata(24.0));

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
        DependencyProperty.Register(nameof(AllowMultiSelection), typeof(bool), typeof(GridTimeRangeChart), new PropertyMetadata(true));

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
            DependencyProperty.Register(nameof(AllowMultiDrag), typeof(bool), typeof(GridTimeRangeChart), new PropertyMetadata(true));

        #endregion

        #region Corner header properties

        /// <summary>
        /// string
        /// </summary>
        public DataTemplate CornerHeaderTemplate
        {
            get { return (DataTemplate)GetValue(CornerHeaderTemplateProperty); }
            set { SetValue(CornerHeaderTemplateProperty, value); }
        }
        public static DependencyProperty CornerHeaderTemplateProperty = DependencyProperty.Register(nameof(CornerHeaderTemplate), typeof(DataTemplate), typeof(GridTimeRangeChart));

        /// <summary>
        /// string
        /// </summary>
        public DataTemplateSelector CornerHeaderTemplateSelector
        {
            get { return (DataTemplateSelector)GetValue(CornerHeaderTemplateSelectorProperty); }
            set { SetValue(CornerHeaderTemplateSelectorProperty, value); }
        }
        public static DependencyProperty CornerHeaderTemplateSelectorProperty = DependencyProperty.Register(nameof(CornerHeaderTemplateSelector), typeof(DataTemplateSelector), typeof(GridTimeRangeChart));

        /// <summary>
        /// string
        /// </summary>
        public object? CornerHeaderSource
        {
            get { return (object?)GetValue(CornerHeaderSourceProperty); }
            set { SetValue(CornerHeaderSourceProperty, value); }
        }
        public static DependencyProperty CornerHeaderSourceProperty = DependencyProperty.Register(nameof(CornerHeaderSource), typeof(object), typeof(GridTimeRangeChart));

        #endregion

        #region Row header properties

        public double MinRowWidth
        {
            get { return (double)GetValue(MinRowWidthProperty); }
            set { SetValue(MinRowWidthProperty, value); }
        }
        public static DependencyProperty MinRowWidthProperty = DependencyProperty.Register(nameof(MinRowWidth), typeof(double), typeof(GridTimeRangeChart), new PropertyMetadata(0.0));

        /// <summary>
        /// string
        /// </summary>
        public DataTemplate RowHeaderTemplate
        {
            get { return (DataTemplate)GetValue(RowHeaderTemplateProperty); }
            set { SetValue(RowHeaderTemplateProperty, value); }
        }
        public static DependencyProperty RowHeaderTemplateProperty = DependencyProperty.Register(nameof(RowHeaderTemplate), typeof(DataTemplate), typeof(GridTimeRangeChart));

        /// <summary>
        /// string
        /// </summary>
        public DataTemplateSelector RowHeaderTemplateSelector
        {
            get { return (DataTemplateSelector)GetValue(RowHeaderTemplateSelectorProperty); }
            set { SetValue(RowHeaderTemplateSelectorProperty, value); }
        }
        public static DependencyProperty RowHeaderTemplateSelectorProperty = DependencyProperty.Register(nameof(RowHeaderTemplateSelector), typeof(DataTemplateSelector), typeof(GridTimeRangeChart));

        /// <summary>
        /// DataGrid モードで使用する列定義コレクション。
        /// 各行の DataContext は RowInternalContainer のため、バインディングパスには "RawData.PropertyName" を使用してください。
        /// DataGridRowHeaderStyle テンプレートと組み合わせて使用します。
        /// </summary>
        public ObservableCollection<DataGridColumn> RowHeaderDataGridColumns
        {
            get { return (ObservableCollection<DataGridColumn>)GetValue(RowHeaderDataGridColumnsProperty); }
            set { SetValue(RowHeaderDataGridColumnsProperty, value); }
        }
        public static DependencyProperty RowHeaderDataGridColumnsProperty = DependencyProperty.Register(
            nameof(RowHeaderDataGridColumns),
            typeof(ObservableCollection<DataGridColumn>),
            typeof(GridTimeRangeChart),
            new PropertyMetadata(null));

        /// <summary>
        /// DataGrid モードで使用する列ヘッダーの高さ。時間軸ヘッダーと揃える値に設定してください。
        /// </summary>
        public double RowHeaderDataGridColumnHeaderHeight
        {
            get { return (double)GetValue(RowHeaderDataGridColumnHeaderHeightProperty); }
            set { SetValue(RowHeaderDataGridColumnHeaderHeightProperty, value); }
        }
        public static DependencyProperty RowHeaderDataGridColumnHeaderHeightProperty = DependencyProperty.Register(
            nameof(RowHeaderDataGridColumnHeaderHeight),
            typeof(double),
            typeof(GridTimeRangeChart),
            new PropertyMetadata(20.0));

        #endregion

        #region TimeRangeControl Properties

        public DataTemplate ItemTemplate
        {
            get { return (DataTemplate)GetValue(ItemTemplateProperty); }
            set { SetValue(ItemTemplateProperty, value); }
        }
        public static DependencyProperty ItemTemplateProperty = DependencyProperty.Register(nameof(ItemTemplate), typeof(DataTemplate), typeof(GridTimeRangeChart));

        public DataTemplateSelector ItemTemplateSelector
        {
            get { return (DataTemplateSelector)GetValue(ItemTemplateSelectorProperty); }
            set { SetValue(ItemTemplateSelectorProperty, value); }
        }
        public static DependencyProperty ItemTemplateSelectorProperty = DependencyProperty.Register(nameof(ItemTemplateSelector), typeof(DataTemplateSelector), typeof(GridTimeRangeChart));
        public Style TimeRangeStyle
        {
            get { return (Style)GetValue(TimeRangeStyleProperty); }
            set { SetValue(TimeRangeStyleProperty, value); }
        }
        public static DependencyProperty TimeRangeStyleProperty = DependencyProperty.Register(nameof(TimeRangeStyle), typeof(Style), typeof(GridTimeRangeChart), new PropertyMetadata(null));

        #endregion

        #region ViewPort properties

        /// <summary>
        /// Get the first row object in ScrollViewer
        /// </summary>
        public object? CurrentFirstRow
        {
            get { return (object)GetValue(CurrentFirstRowProperty); }
            set { SetValue(CurrentFirstRowProperty, value); }
        }
        public static DependencyProperty CurrentFirstRowProperty = DependencyProperty.Register(nameof(CurrentFirstRow), typeof(object), typeof(GridTimeRangeChart));

        /// <summary>
        /// Get the last row object in ScrollViewer
        /// </summary>
        public object? CurrentLastRow
        {
            get { return (object)GetValue(CurrentLastRowProperty); }
            set { SetValue(CurrentLastRowProperty, value); }
        }
        public static DependencyProperty CurrentLastRowProperty = DependencyProperty.Register(nameof(CurrentLastRow), typeof(object), typeof(GridTimeRangeChart));


        /// <summary>
        /// Get the center time in ScrollViewer
        /// </summary>
        public DateTime CurrentCenterTime
        {
            get { return (DateTime)GetValue(CurrentCenterTimeProperty); }
            set { SetValue(CurrentCenterTimeProperty, value); }
        }
        public static DependencyProperty CurrentCenterTimeProperty = DependencyProperty.Register(nameof(CurrentCenterTime), typeof(DateTime), typeof(GridTimeRangeChart));

        /// <summary>
        /// Get the left time in ScrollViewer
        /// </summary>
        public DateTime CurrentLeftTime
        {
            get { return (DateTime)GetValue(CurrentLeftTimeProperty); }
            set { SetValue(CurrentLeftTimeProperty, value); }
        }
        public static DependencyProperty CurrentLeftTimeProperty = DependencyProperty.Register(nameof(CurrentLeftTime), typeof(DateTime), typeof(GridTimeRangeChart));

        /// <summary>
        /// Get the right time in ScrollViewer
        /// </summary>
        public DateTime CurrentRightTime
        {
            get { return (DateTime)GetValue(CurrentRightTimeProperty); }
            set { SetValue(CurrentRightTimeProperty, value); }
        }
        public static DependencyProperty CurrentRightTimeProperty = DependencyProperty.Register(nameof(CurrentRightTime), typeof(DateTime), typeof(GridTimeRangeChart));


        #endregion

        #region Data

        public IList ItemsSource
        {
            get { return (IList)GetValue(ItemsSourceProperty); }
            set { SetValue(ItemsSourceProperty, value); }
        }
        public static readonly DependencyProperty ItemsSourceProperty = DependencyProperty.Register(nameof(ItemsSource), typeof(IList), typeof(GridTimeRangeChart), new PropertyMetadata(propertyChangedCallback: OnItemsSourceChanged));
        // タスクのコレクションが格納されているプロパティ名 (例: "Tasks")
        public string TasksSourcePath
        {
            get => (string)GetValue(TasksSourcePathProperty);
            set => SetValue(TasksSourcePathProperty, value);
        }
        public static readonly DependencyProperty TasksSourcePathProperty =
            DependencyProperty.Register(nameof(TasksSourcePath), typeof(string), typeof(GridTimeRangeChart), new PropertyMetadata(string.Empty, propertyChangedCallback: OnTasksSourcePathChanged));
        public IList<ITimeRangeRowIndex> NonWorkingTimes
        {
            get { return (IList<ITimeRangeRowIndex>)GetValue(NonWorkingTimesProperty); }
            set { SetValue(NonWorkingTimesProperty, value); }
        }
        public static DependencyProperty NonWorkingTimesProperty = DependencyProperty.Register(nameof(NonWorkingTimes), typeof(IList<ITimeRangeRowIndex>), typeof(GridTimeRangeChart));



        public IList<IBackgroundTimeRange> BackgroundTimes
        {
            get { return (IList<IBackgroundTimeRange>)GetValue(BackgroundTimesProperty); }
            set { SetValue(BackgroundTimesProperty, value); }
        }
        public static DependencyProperty BackgroundTimesProperty = DependencyProperty.Register(nameof(BackgroundTimes), typeof(IList<IBackgroundTimeRange>), typeof(GridTimeRangeChart));

        public DataTemplate? BackgroundTimeRangePopupTemplate
        {
            get => (DataTemplate?)GetValue(BackgroundTimeRangePopupTemplateProperty);
            set => SetValue(BackgroundTimeRangePopupTemplateProperty, value);
        }
        public static readonly DependencyProperty BackgroundTimeRangePopupTemplateProperty =
            DependencyProperty.Register(nameof(BackgroundTimeRangePopupTemplate), typeof(DataTemplate), typeof(GridTimeRangeChart));

        #endregion

        #region Chart Styles

        public double ChartHeight
        {
            get { return (double)GetValue(ChartHeightProperty); }
            set { SetValue(ChartHeightProperty, value); }
        }
        public static DependencyProperty ChartHeightProperty = DependencyProperty.Register(nameof(ChartHeight), typeof(double), typeof(GridTimeRangeChart), new PropertyMetadata(80.0d));


        public double WidthPerRuledLine
        {
            get { return (double)GetValue(WidthPerRuledLineProperty); }
            set { SetValue(WidthPerRuledLineProperty, value); }
        }
        public static DependencyProperty WidthPerRuledLineProperty = DependencyProperty.Register(nameof(WidthPerRuledLine), typeof(double), typeof(GridTimeRangeChart), new PropertyMetadata(100.0));

        public Brush RuledLineFill
        {
            get { return (Brush)GetValue(RuledLineFillProperty); }
            set { SetValue(RuledLineFillProperty, value); }
        }
        public static DependencyProperty RuledLineFillProperty = DependencyProperty.Register(nameof(RuledLineFill), typeof(Brush), typeof(GridTimeRangeChart), new PropertyMetadata(new SolidColorBrush(Color.FromArgb(255, 125, 125, 125))));

        public double RuledLineThickness
        {
            get { return (double)GetValue(RuledLineThicknessProperty); }
            set { SetValue(RuledLineThicknessProperty, value); }
        }
        public static DependencyProperty RuledLineThicknessProperty = DependencyProperty.Register(nameof(RuledLineThickness), typeof(double), typeof(GridTimeRangeChart), new PropertyMetadata(1.0));

        /// <summary>
        /// background of non-working ranges
        /// </summary>
        public Brush NonWorkingRangeBackground
        {
            get { return (Brush)GetValue(NonWorkingRangeBackgroundProperty); }
            set { SetValue(NonWorkingRangeBackgroundProperty, value); }
        }
        public static DependencyProperty NonWorkingRangeBackgroundProperty = DependencyProperty.Register(nameof(NonWorkingRangeBackground), typeof(Brush), typeof(GridTimeRangeChart), new PropertyMetadata(new SolidColorBrush(Color.FromRgb(0xd8, 0xd8, 0xd8))));


        #endregion

        #region Footer

        /// <summary>
        /// Content shown in the footer corner cell (bottom-left, same column as the row header).
        /// </summary>
        public object? FooterCornerSource
        {
            get => (object?)GetValue(FooterCornerSourceProperty);
            set => SetValue(FooterCornerSourceProperty, value);
        }
        public static readonly DependencyProperty FooterCornerSourceProperty =
            DependencyProperty.Register(nameof(FooterCornerSource), typeof(object), typeof(GridTimeRangeChart));

        /// <summary>
        /// DataTemplate for the footer corner cell.
        /// </summary>
        public DataTemplate? FooterCornerTemplate
        {
            get => (DataTemplate?)GetValue(FooterCornerTemplateProperty);
            set => SetValue(FooterCornerTemplateProperty, value);
        }
        public static readonly DependencyProperty FooterCornerTemplateProperty =
            DependencyProperty.Register(nameof(FooterCornerTemplate), typeof(DataTemplate), typeof(GridTimeRangeChart));

        /// <summary>
        /// DataTemplate applied to each footer cell (one cell per time column).
        /// The DataContext of each cell is the object from <see cref="FooterItemsSource"/>.
        /// </summary>
        public DataTemplate? FooterTemplate
        {
            get => (DataTemplate?)GetValue(FooterTemplateProperty);
            set => SetValue(FooterTemplateProperty, value);
        }
        public static readonly DependencyProperty FooterTemplateProperty =
            DependencyProperty.Register(nameof(FooterTemplate), typeof(DataTemplate), typeof(GridTimeRangeChart));

        /// <summary>
        /// Items displayed in the footer — one item per time column.
        /// The consumer is responsible for keeping this list in sync with the time axis tick count.
        /// </summary>
        public IList? FooterItemsSource
        {
            get => (IList?)GetValue(FooterItemsSourceProperty);
            set => SetValue(FooterItemsSourceProperty, value);
        }
        public static readonly DependencyProperty FooterItemsSourceProperty =
            DependencyProperty.Register(nameof(FooterItemsSource), typeof(IList), typeof(GridTimeRangeChart));

        /// <summary>
        /// Height of the footer row. Set to 0 (default) to hide the footer.
        /// </summary>
        public double FooterHeight
        {
            get => (double)GetValue(FooterHeightProperty);
            set => SetValue(FooterHeightProperty, value);
        }
        public static readonly DependencyProperty FooterHeightProperty =
            DependencyProperty.Register(nameof(FooterHeight), typeof(double), typeof(GridTimeRangeChart), new PropertyMetadata(0.0));

        #endregion

        #endregion


        #region Event Definitions


        public event EventHandler<RoutedEventArgs>? ViewPortChanged
        {
            add { AddHandler(ViewPortChangedEvent, value); }
            remove { RemoveHandler(ViewPortChangedEvent, value); }
        }
        public static readonly RoutedEvent ViewPortChangedEvent =
        EventManager.RegisterRoutedEvent(
            nameof(ViewPortChanged), RoutingStrategy.Bubble,
            typeof(EventHandler<RoutedEventArgs>), typeof(GridTimeRangeChart));



        /// <summary>
        /// Occurs when a task drag operation begins, specifically for horizontal movement or resizing within a row.
        /// </summary>
        public event EventHandler<DragDropRangeEventArgs>? TaskDragStart
        {
            add { AddHandler(TaskDragStartEvent, value); }
            remove { RemoveHandler(TaskDragStartEvent, value); }
        }
        public static readonly RoutedEvent TaskDragStartEvent =
            EventManager.RegisterRoutedEvent(
                nameof(TaskDragStart), RoutingStrategy.Bubble,
            typeof(EventHandler<DragDropRangeEventArgs>), typeof(GridTimeRangeChart));

        /// <summary>
        /// Occurs when a task drag operation ends, finalizing changes to the task's time range within the row.
        /// </summary>
        public event EventHandler<DragDropRangeEventArgs>? TaskDragEnd
        {
            add { AddHandler(TaskDragEndEvent, value); }
            remove { RemoveHandler(TaskDragEndEvent, value); }

        }
        public static readonly RoutedEvent TaskDragEndEvent =
            EventManager.RegisterRoutedEvent(
                nameof(TaskDragEnd), RoutingStrategy.Bubble,
            typeof(EventHandler<DragDropRangeEventArgs>), typeof(GridTimeRangeChart));

        /// <summary>
        /// Occurs when a task is dropped onto a row, finalizing vertical movement between different rows.
        /// </summary>
        public event EventHandler<DropOnRowEventArgs> TaskDrop
        {
            add { AddHandler(TaskDropEvent, value); }
            remove { RemoveHandler(TaskDropEvent, value); }
        }
        public static readonly RoutedEvent TaskDropEvent =
        EventManager.RegisterRoutedEvent(
            nameof(TaskDrop), RoutingStrategy.Bubble,
            typeof(EventHandler<DropOnRowEventArgs>), typeof(GridTimeRangeChart));

        /// <summary>
        /// Occurs when a task is about to be dropped onto a row, allowing for validation of vertical movement across rows.
        /// </summary>
        public event EventHandler<DropOnRowEventArgs> PreviewTaskDrop
        {
            add { AddHandler(PreviewTaskDropEvent, value); }
            remove { RemoveHandler(PreviewTaskDropEvent, value); }
        }
        public static readonly RoutedEvent PreviewTaskDropEvent =
        EventManager.RegisterRoutedEvent(
            nameof(PreviewTaskDrop), RoutingStrategy.Bubble,
            typeof(EventHandler<DropOnRowEventArgs>), typeof(GridTimeRangeChart));



        #endregion



        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            _rowHeaderListBox = (ListBox)GetTemplateChild("PART_RowHeaderListBox");
            _rowHeaderDataGrid = GetTemplateChild("PART_RowHeaderDataGrid") as DataGrid;
            _chart = (TimeRangeChart)GetTemplateChild("PART_Chart");
            _columnHeaderLayer = (ColumnHeaderRenderLayer)GetTemplateChild("PART_ColumnGrid");

            _chartScrollViewer = (ScrollViewer)GetTemplateChild("PART_ChartScrollViewer");
            var columnHeaderScrollViewer = (ScrollViewer)GetTemplateChild("PART_ColumnScrollViewer");
            var rowHeaderScrollViewer = (ScrollViewer)GetTemplateChild("PART_RowScrollViewer");

            _columnRightSpacer = (Border)GetTemplateChild("PART_ColumnRightSpacer");
            _rowBottomSpacer = (Border)GetTemplateChild("PART_RowBottomSpacer");


            _footerLayer = GetTemplateChild("PART_Footer") as FooterRenderLayer;
            _footerScrollViewer = GetTemplateChild("PART_FooterScrollViewer") as ScrollViewer;

            // ListBox か DataGrid の少なくとも一方と、Chart と ColumnHeader が必須
            if ((_rowHeaderListBox == null && _rowHeaderDataGrid == null) || _chart == null || _columnHeaderLayer == null)
            {
                throw new InvalidOperationException();
            }

            _columnHeaderLayer.ParentChart = this;
            if (_footerLayer != null) _footerLayer.ParentChart = this;

            if (_rowHeaderListBox != null)
            {
                _rowHeaderListBox.ItemContainerGenerator.StatusChanged += (s, e) =>
                {
                    if (_rowHeaderListBox.ItemContainerGenerator.Status == GeneratorStatus.ContainersGenerated)
                        SyncHeights();
                };

                _rowHeaderListBox.SizeChanged += (s, e) =>
                {
                    if (e.WidthChanged) SyncHeights();
                };
            }

            if (_rowHeaderDataGrid != null)
            {
                _rowHeaderDataGrid.SizeChanged += (s, e) =>
                {
                    if (e.WidthChanged) SyncHeights();
                };
            }

            _chart.OnTimeAxisChanged += Chart_OnTimeAxisChanged;

            this.Loaded += (s, e) =>
            {
                _columnHeaderLayer.InvalidateVisual();
                if (_chart == null) return;

                RebuildFlatTasks();
                SyncInternalRows();


                var horizontalSyncBehavior = new ScrollSyncBehavior();
                horizontalSyncBehavior.IsVerticalSync = false;
                horizontalSyncBehavior.IsHorizontalSync = true;

                var verticalSyncBehavior = new ScrollSyncBehavior();
                verticalSyncBehavior.IsVerticalSync = true;
                verticalSyncBehavior.IsHorizontalSync = false;
                GeneralHelper.SetBehavior(_chart, verticalSyncBehavior, true);
                GeneralHelper.SetBehavior(_chart, horizontalSyncBehavior, true);

                horizontalSyncBehavior.SyncTarget = columnHeaderScrollViewer;

                // DataGrid モードの場合は DataGrid を同期ターゲットに設定
                FrameworkElement rowSyncTarget = _rowHeaderDataGrid ?? (FrameworkElement)_rowHeaderListBox;
                verticalSyncBehavior.SyncTarget = rowSyncTarget;
                if (_footerScrollViewer != null)
                {
                    var footerHSyncBehavior = new ScrollSyncBehavior { IsHorizontalSync = true, IsVerticalSync = false };
                    GeneralHelper.SetBehavior(_chart, footerHSyncBehavior, true);
                    footerHSyncBehavior.SyncTarget = _footerScrollViewer;
                }

                verticalSyncBehavior.SyncTarget = _rowHeaderListBox;

            };
            if (_chart != null)
            {
                // Chart 自体がテンプレート適用済みかチェック
                _chart.Loaded += OnChartLoaded;
            }

            AddHandler(TimeRangeChart.ViewPortChangedEvent, new EventHandler<RoutedEventArgs>(OnViewPortChanged));
            AddHandler(TimeRangeChart.RowHeightsChangedEvent, new EventHandler<RoutedEventArgs>(OnRowHeightsChanged));

            AddHandler(TimeRangeChart.PreviewTaskDropEvent, new EventHandler<DropOnRowEventArgs>(OnPreviewTaskDrop));
            AddHandler(TimeRangeChart.TaskDropEvent, new EventHandler<DropOnRowEventArgs>(OnTaskDrop));
            AddHandler(TimeRangeChart.TaskDragStartEvent, new EventHandler<DragDropRangeEventArgs>(OnTaskDragStart));
            AddHandler(TimeRangeChart.TaskDragEndEvent, new EventHandler<DragDropRangeEventArgs>(OnTaskDragEnd));

        }

        private void OnChartLoaded(object sender, RoutedEventArgs e)
        {
            if (_chart == null) return;
            _chart.Loaded -= OnChartLoaded; // 二重登録防止

            // TimeRangeChart の中にある ScrollViewer を探す
            // (PART_ScrollViewer という名前で公開されている、あるいは VisualTreeHelper で探す)
            var scrollViewer = (ScrollViewer)_chart.Template.FindName("PART_ChartScrollViewer", _chart);
            _chartScrollViewer = scrollViewer;
            if (scrollViewer != null)
            {
                SetupScrollSync();
            }

            _columnHeaderLayer.InvalidateMeasure();
        }

        private void Chart_OnTimeAxisChanged(object? sender, EventArgs e)
        {
            _columnHeaderLayer.InvalidateMeasure();
            _columnHeaderLayer.InvalidateVisual();
            _footerLayer?.InvalidateMeasure();
        }

        private readonly List<INotifyCollectionChanged> _trackedTaskCollections = new();

        private static void OnItemsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var self = (GridTimeRangeChart)d;


            // 前の監視を解除
            foreach (var coll in self._trackedTaskCollections)
                coll.CollectionChanged -= self.OnTaskCollectionChanged;

            self._trackedTaskCollections.Clear();

            if (e.OldValue is INotifyCollectionChanged oldRows)
                oldRows.CollectionChanged -= self.OnRowsCollectionChanged;



            if (e.NewValue is INotifyCollectionChanged newRows)
                newRows.CollectionChanged += self.OnRowsCollectionChanged;

            if (self._chart == null) return;

            self.RebuildFlatTasks();
            self.SyncInternalRows();
        }

        private static void OnTasksSourcePathChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var self = (GridTimeRangeChart)d;
            if (self._chart == null) return;
            self.RebuildFlatTasks();
            self.SyncInternalRows();

        }


        private void SyncInternalRows()
        {
            if (_chart == null) return;
            _internalRows.Clear();
            if (ItemsSource != null)
            {
                foreach (var item in ItemsSource)
                    _internalRows.Add(new RowInternalContainer(item));
            }
            if (_rowHeaderListBox != null)
                _rowHeaderListBox.ItemsSource = _internalRows;
            if (_rowHeaderDataGrid != null)
                _rowHeaderDataGrid.ItemsSource = _internalRows;
        }
        // 行リストが変わった（行の追加・削除・入れ替え）
        private void OnRowsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (_chart == null) return;
            // 行の順序が変わると全タスクの RowIndex がズレるため、全体を再構築
            RebuildFlatTasks();
            SyncInternalRows();
        }

        // 行の中のタスクリストが変わった
        private void OnTaskCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (_chart == null) return;
            // 特定の行だけ更新する最適化も可能ですが、
            // 整合性を保つため一旦 RebuildFlatTasks を呼ぶのが安全です
            RebuildFlatTasks();
        }

        private void RebuildFlatTasks()
        {
            if (_chart == null) return;
            var flatList = new List<ITimeRangeRowIndex>();
            var rows = ItemsSource as IList;
            if (rows == null) return;

            for (int i = 0; i < rows.Count; i++)
            {
                var row = rows[i];
                var tasks = GetTasksFromRow(row);

                if (tasks is INotifyCollectionChanged observableTasks)
                {
                    // まだ監視していなければ登録
                    if (!_trackedTaskCollections.Contains(observableTasks))
                    {
                        observableTasks.CollectionChanged += OnTaskCollectionChanged;
                        _trackedTaskCollections.Add(observableTasks);
                    }
                }

                if (tasks != null)
                {
                    foreach (var t in tasks)
                    {
                        if (t is ITimeRangeRowIndex task)
                        {
                            task.RowIndex = i; // ここでインデックスを注入
                            flatList.Add(task);
                        }
                    }
                }
            }

            // TimeRangeChart の ItemsSource を更新
            _chart.RowCount = rows.Count;
            _chart.ItemsSource = flatList;

            // 前述した「スタック計算とRowHeaderへの高さ反映」もここで呼ぶ
            SyncHeights();

        }

        private IEnumerable? GetTasksFromRow(object? row)
        {
            if (string.IsNullOrEmpty(TasksSourcePath)) return null;
            return row?.GetType().GetProperty(TasksSourcePath)?.GetValue(row) as IEnumerable;
        }

        protected void OnPreviewTaskDrop(object? sender, DropOnRowEventArgs e)
        {
            var newArgs = new DropOnRowEventArgs(PreviewTaskDropEvent, e);
            RaiseEvent(newArgs);
            if (newArgs.Cancel) e.Cancel = true;
        }

        protected void OnTaskDrop(object? sender, DropOnRowEventArgs e)
        {
            var newArgs = new DropOnRowEventArgs(TaskDropEvent, e);
            RaiseEvent(newArgs);
            if (newArgs.Cancel) e.Cancel = true;


            if (!e.Cancel)
            {
                var oldItem = GetTasksFromRow(ItemsSource[e.OldRowIndex]);
                var newItem = GetTasksFromRow(ItemsSource[e.NewRowIndex]);

                if (oldItem is IList oldRowList && newItem is IList newRowList)
                {
                    oldRowList.Remove(e.TaskObject);
                    newRowList.Add(e.TaskObject);
                }
            }

        }

        protected void OnTaskDragStart(object? sender, DragDropRangeEventArgs e)
        {
            var newArgs = new DragDropRangeEventArgs(TaskDragStartEvent, e);

            RaiseEvent(newArgs);
            if (newArgs.Cancel) e.Cancel = true;

        }

        protected void OnTaskDragEnd(object? sender, DragDropRangeEventArgs e)
        {
            var newArgs = new DragDropRangeEventArgs(TaskDragEndEvent, e);

            RaiseEvent(newArgs);
            if (newArgs.Cancel) e.Cancel = true;

        }


        protected void OnViewPortChanged(object? sender, RoutedEventArgs e)
        {
            if (ItemsSource != null)
            {
                if (_chart.CurrentFirstRow > -1 && _chart.CurrentFirstRow < ItemsSource.Count)
                { CurrentFirstRow = ItemsSource[_chart.CurrentFirstRow]; }
                else { CurrentFirstRow = null; }
                if (_chart.CurrentLastRow > -1 && _chart.CurrentLastRow < ItemsSource.Count)
                { CurrentLastRow = ItemsSource[_chart.CurrentLastRow]; }
                else
                {
                    CurrentLastRow = null;
                }

            }

            CurrentCenterTime = _chart.CurrentCenterTime;
            CurrentLeftTime = _chart.CurrentFirstTime;
            CurrentRightTime = _chart.CurrentLastTime;

            var newArgs = new RoutedEventArgs(ViewPortChangedEvent);
            RaiseEvent(newArgs);
        }

        protected void OnRowHeightsChanged(object? sender, RoutedEventArgs e)
        {
            SyncHeights();
        }


        private void SyncHeights()
        {
            var heights = _chart.RowHeights;
            for (int i = 0; i < _internalRows.Count; i++)
            {
                if (i < heights.Length)
                {
                    // 内部管理オブジェクトのHeightを変えるだけなので、
                    // 外のデータモデルは一切汚れない！
                    _internalRows[i].Height = heights[i];
                }
            }
        }
        private ObservableCollection<RowInternalContainer> _internalRows = new();


        private void SetupScrollSync()
        {
            // PART_Chart の中にある ScrollViewer を取得 (GetTemplateChild 等で取得済みとする)

            if (_chartScrollViewer == null) return;

            // スクロールバーの可視性が変わったとき、またはサイズが変わったときに同期
            _chartScrollViewer.LayoutUpdated += (s, e) =>
            {
                // 縦スクロールバーの幅を ColumnHeader の Spacer に適用
                if (_columnRightSpacer != null)
                {
                    // ScrollViewer.ComputedVerticalScrollBarVisibility が Visible の時だけ幅を持つ
                    _columnRightSpacer.Width = (_chartScrollViewer.ComputedVerticalScrollBarVisibility == Visibility.Visible)
                        ? SystemParameters.VerticalScrollBarWidth : 0;
                }

                // 横スクロールバーの高さを RowHeader の Spacer に適用
                if (_rowBottomSpacer != null)
                {
                    _rowBottomSpacer.Height = (_chartScrollViewer.ComputedHorizontalScrollBarVisibility == Visibility.Visible)
                        ? SystemParameters.HorizontalScrollBarHeight : 0;
                }
            };
        }

    }


}
