using CommunityToolkit.Mvvm.Input;
using FrostNova.Core.Common;
using FrostNova.Core.Wpf.Behaviors;
using FrostNova.Core.Wpf.VisualHelpers;
using FrostNova.TimeRangeChart.TimeRangeHelpers;
using Microsoft.Xaml.Behaviors;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.DirectoryServices;
using System.Globalization;
using System.Linq;
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
using System.Xml.Linq;

namespace FrostNova.TimeRangeChart.Wpf
{

    public partial class GridTimeRangeChart : Control
    {
        static GridTimeRangeChart()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(GridTimeRangeChart), new FrameworkPropertyMetadata(typeof(GridTimeRangeChart)));
        }
        public GridTimeRangeChart()
        {
            if (TimeTicks == null)
            {
                TimeTicks = new List<IRuledLine>();
                SubTimeTicks = new List<IRuledLine>();
            }
            //if (TimeTicksForColumnHeader == null)
            //{
            //    TimeTicksForColumnHeader = new NotifyCollection<IRuledLine>();
            //}

            if (SelectedRanges == null)
            {
                SelectedRanges = new ObservableCollection<TimeRangeControl>();
            }
            if (SelectedValues == null)
            {
                SelectedValues = new ObservableCollection<object>();
            }
            TaskDragStartAction = OnTaskDragStart;
            TaskDragEndAction = OnTaskDragEnd;


        }
        public Action<TimeRangeControl, DragDropRangeEventArgs> TaskDragStartAction { get; private set; }
        public Action<TimeRangeControl, DragDropRangeEventArgs> TaskDragEndAction { get; private set; }

        private ColumnHeaderRenderLayer? columnHeaderLayer;

        [Bindable(true)]
        public DateTime StartTime
        {
            get { return (DateTime)GetValue(StartTimeProperty); }
            set { SetValue(StartTimeProperty, value); }
        }
        public static readonly DependencyProperty StartTimeProperty = DependencyProperty.Register(nameof(StartTime), typeof(DateTime), typeof(GridTimeRangeChart), new FrameworkPropertyMetadata(DateTime.Now.AddDays(-3), FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnMyPropertyChanged));

        public DateTime EndTime
        {
            get { return (DateTime)GetValue(EndTimeProperty); }
            set { SetValue(EndTimeProperty, value); }
        }
        public static readonly DependencyProperty EndTimeProperty = DependencyProperty.Register(nameof(EndTime), typeof(DateTime), typeof(GridTimeRangeChart), new FrameworkPropertyMetadata(DateTime.Now.AddDays(9), FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnMyPropertyChanged));

        public IList ItemsSource
        {
            get { return (IList)GetValue(ItemsSourceProperty); }
            set { SetValue(ItemsSourceProperty, value); }
        }
        public static readonly DependencyProperty ItemsSourceProperty = DependencyProperty.Register(nameof(ItemsSource), typeof(IList), typeof(GridTimeRangeChart));

        public TimeSpan ScaleIncrement
        {
            get { return (TimeSpan)GetValue(ScaleIncrementProperty); }
            set
            {
                SetValue(ScaleIncrementProperty, value);
            }
        }
        public static readonly DependencyProperty ScaleIncrementProperty = DependencyProperty.Register(nameof(ScaleIncrement), typeof(TimeSpan), typeof(GridTimeRangeChart), new PropertyMetadata(new TimeSpan(1, 0, 0), OnMyPropertyChanged));

        public TimeSpan SubScaleIncrement
        {
            get { return (TimeSpan)GetValue(SubScaleIncrementProperty); }
            set { SetValue(SubScaleIncrementProperty, value); }
        }
        public static DependencyProperty SubScaleIncrementProperty = DependencyProperty.Register(nameof(SubScaleIncrement), typeof(TimeSpan), typeof(GridTimeRangeChart), new PropertyMetadata(new TimeSpan(1, 0, 0), OnMyPropertyChanged));


        public TimeSpan MinSpan
        {
            get { return (TimeSpan)GetValue(MinSpanProperty); }
            set { SetValue(MinSpanProperty, value); }
        }
        public static readonly DependencyProperty MinSpanProperty = DependencyProperty.Register(nameof(MinSpan), typeof(TimeSpan), typeof(GridTimeRangeChart), new PropertyMetadata(TimeSpan.FromMinutes(15)));

        public Brush RuledLineFill
        {
            get { return (Brush)GetValue(RuledLineFillProperty); }
            set { SetValue(RuledLineFillProperty, value); }
        }
        public static DependencyProperty RuledLineFillProperty = DependencyProperty.Register(nameof(RuledLineFill), typeof(Brush), typeof(GridTimeRangeChart), new PropertyMetadata(new SolidColorBrush(Color.FromArgb(0, 255, 255, 255)), OnMyPropertyChanged));

        public double RuledLineThickness
        {
            get { return (double)GetValue(RuledLineThicknessProperty); }
            set { SetValue(RuledLineThicknessProperty, value); }
        }
        public static DependencyProperty RuledLineThicknessProperty = DependencyProperty.Register(nameof(RuledLineThickness), typeof(double), typeof(GridTimeRangeChart), new PropertyMetadata(1.0, OnMyPropertyChanged));

        /// <summary>
        /// background of non-working ranges
        /// </summary>
        public Brush NonWorkingRangeBackground
        {
            get { return (Brush)GetValue(NonWorkingRangeBackgroundProperty); }
            set { SetValue(NonWorkingRangeBackgroundProperty, value); }
        }
        public static DependencyProperty NonWorkingRangeBackgroundProperty = DependencyProperty.Register(nameof(NonWorkingRangeBackground), typeof(Brush), typeof(GridTimeRangeChart), new PropertyMetadata(new SolidColorBrush(Color.FromRgb(0xd8, 0xd8, 0xd8))));

        /// <summary>
        /// string
        /// </summary>
        //public IList<ITimeRange> NonWorkingTimes
        //{
        //    get { return (IList<ITimeRange>)GetValue(NonWorkingTimesProperty); }
        //    set { SetValue(NonWorkingTimesProperty, value); }
        //}
        //public static DependencyProperty NonWorkingTimesProperty = DependencyProperty.Register(nameof(NonWorkingTimes), typeof(IList<ITimeRange>), typeof(GridTimeRangeChart), new PropertyMetadata(OnNonWorkingTimesChanged));

        /// <summary>
        /// string
        /// </summary>
        [Bindable(true)]
        [Category("Content")]
        public Style TimeRangeStyle
        {
            get { return (Style)GetValue(TimeRangeStyleProperty); }
            set { SetValue(TimeRangeStyleProperty, value); }
        }
        public static DependencyProperty TimeRangeStyleProperty = DependencyProperty.Register(nameof(TimeRangeStyle), typeof(Style), typeof(GridTimeRangeChart), new PropertyMetadata(null));

        /// <summary>
        /// string
        /// </summary>
        public Style ChartStyle
        {
            get { return (Style)GetValue(ChartStyleProperty); }
            set { SetValue(ChartStyleProperty, value); }
        }
        public static DependencyProperty ChartStyleProperty = DependencyProperty.Register(nameof(ChartStyle), typeof(Style), typeof(GridTimeRangeChart));

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
        /// string
        /// </summary>
        public DataTemplate ColumnHeaderTemplate
        {
            get { return (DataTemplate)GetValue(ColumnHeaderTemplateProperty); }
            set { SetValue(ColumnHeaderTemplateProperty, value); }
        }
        public static DependencyProperty ColumnHeaderTemplateProperty = DependencyProperty.Register(nameof(ColumnHeaderTemplate), typeof(DataTemplate), typeof(GridTimeRangeChart));

        public DataTemplateSelector ColumnHeaderTemplateSelector
        {
            get { return (DataTemplateSelector)GetValue(ColumnHeaderTemplateSelectorProperty); }
            set { SetValue(ColumnHeaderTemplateSelectorProperty, value); }
        }
        public static readonly DependencyProperty ColumnHeaderTemplateSelectorProperty =
            DependencyProperty.Register(nameof(ColumnHeaderTemplateSelector), typeof(DataTemplateSelector), typeof(GridTimeRangeChart));

        /// <summary>
        /// string
        /// </summary>
        public double WidthPerRuledLine
        {
            get { return (double)GetValue(WidthPerIncrementProperty); }
            set { SetValue(WidthPerIncrementProperty, value); }
        }
        public static DependencyProperty WidthPerIncrementProperty = DependencyProperty.Register(nameof(WidthPerRuledLine), typeof(double), typeof(GridTimeRangeChart), new PropertyMetadata(double.NaN));

        /// <summary>
        /// string
        /// </summary>
        public string HeaderFormat
        {
            get { return (string)GetValue(HeaderFormatProperty); }
            set { SetValue(HeaderFormatProperty, value); }
        }
        public static DependencyProperty HeaderFormatProperty = DependencyProperty.Register(nameof(HeaderFormat), typeof(string), typeof(GridTimeRangeChart));

        public string SubScaleFormat
        {
            get { return (string)GetValue(SubScaleFormatProperty); }
            set { SetValue(SubScaleFormatProperty, value); }
        }
        public static DependencyProperty SubScaleFormatProperty = DependencyProperty.Register(nameof(SubScaleFormat), typeof(string), typeof(GridTimeRangeChart));


        public List<IRuledLine> TimeTicks
        {
            get { return (List<IRuledLine>)GetValue(TimeTicksProperty); }
            internal set { SetValue(TimeTicksProperty, value); }
        }
        public static DependencyProperty TimeTicksProperty = DependencyProperty.Register(nameof(TimeTicks), typeof(List<IRuledLine>), typeof(GridTimeRangeChart));


        public List<IRuledLine> SubTimeTicks
        {
            get { return (List<IRuledLine>)GetValue(SubTimeTicksProperty); }
            set { SetValue(SubTimeTicksProperty, value); }
        }
        public static DependencyProperty SubTimeTicksProperty = DependencyProperty.Register(nameof(SubTimeTicks), typeof(List<IRuledLine>), typeof(GridTimeRangeChart));


        /// <summary>
        /// string
        /// </summary>
        public double ChartWidth
        {
            get { return (double)GetValue(ChartWidthProperty); }
            set { SetValue(ChartWidthProperty, value); }
        }
        public static DependencyProperty ChartWidthProperty = DependencyProperty.Register(nameof(ChartWidth), typeof(double), typeof(GridTimeRangeChart));


        ///// <summary>
        ///// string
        ///// </summary>
        //public NotifyCollection<IRuledLine> TimeTicksForColumnHeader
        //{
        //    get { return (NotifyCollection<IRuledLine>)GetValue(TimeTicksForColumnHeaderProperty); }
        //    set { SetValue(TimeTicksForColumnHeaderProperty, value); }
        //}
        //public static DependencyProperty TimeTicksForColumnHeaderProperty = DependencyProperty.Register(nameof(TimeTicksForColumnHeader), typeof(NotifyCollection<IRuledLine>), typeof(GridTimeRangeChart));



        /// <summary>
        /// string
        /// </summary>
        public TimeUnitType TimeUnit
        {
            get { return (TimeUnitType)GetValue(TimeUnitProperty); }
            set { SetValue(TimeUnitProperty, value); }
        }
        public static DependencyProperty TimeUnitProperty = DependencyProperty.Register(nameof(TimeUnit), typeof(TimeUnitType), typeof(GridTimeRangeChart), new PropertyMetadata(TimeUnitType.Normal, OnMyPropertyChanged));


        /// <summary>
        /// string
        /// </summary>
        public int MonthSpan
        {
            get { return (int)GetValue(MonthSpanProperty); }
            set { SetValue(MonthSpanProperty, value); }
        }
        public static DependencyProperty MonthSpanProperty = DependencyProperty.Register(nameof(MonthSpan), typeof(int), typeof(GridTimeRangeChart), new PropertyMetadata(1, OnMyPropertyChanged));

        public MonthSubTickType SubMonthUnit
        {
            get { return (MonthSubTickType)GetValue(SubMonthUnitProperty); }
            set { SetValue(SubMonthUnitProperty, value); }
        }
        public static DependencyProperty SubMonthUnitProperty = DependencyProperty.Register(nameof(SubMonthUnit), typeof(MonthSubTickType), typeof(GridTimeRangeChart));


        public double MinRowWidth
        {
            get { return (double)GetValue(MinRowWidthProperty); }
            set { SetValue(MinRowWidthProperty, value); }
        }
        public static DependencyProperty MinRowWidthProperty = DependencyProperty.Register(nameof(MinRowWidth), typeof(double), typeof(GridTimeRangeChart), new PropertyMetadata(0.0));


        public double RowHeight
        {
            get { return (double)GetValue(RowHeightProperty); }
            set { SetValue(RowHeightProperty, value); }
        }
        public static DependencyProperty RowHeightProperty = DependencyProperty.Register(nameof(RowHeight), typeof(double), typeof(GridTimeRangeChart), new PropertyMetadata(30.0));

        public IList<TimeRangeControl> SelectedRanges
        {
            get { return (IList<TimeRangeControl>)GetValue(SelectedRangesProperty); }
            set { SetValue(SelectedRangesProperty, value); }
        }
        public static DependencyProperty SelectedRangesProperty = DependencyProperty.Register(nameof(SelectedRanges), typeof(IList<TimeRangeControl>), typeof(GridTimeRangeChart));

        public IList SelectedValues
        {
            get { return (IList)GetValue(SelectedValuesProperty); }
            set { SetValue(SelectedValuesProperty, value); }
        }
        public static DependencyProperty SelectedValuesProperty = DependencyProperty.Register(nameof(SelectedValues), typeof(IList), typeof(GridTimeRangeChart));

        public object ScrollTopItem
        {
            get { return (object)GetValue(ScrollTopItemProperty); }
            set { SetValue(ScrollTopItemProperty, value); }
        }
        public static DependencyProperty ScrollTopItemProperty = DependencyProperty.Register(nameof(ScrollTopItem), typeof(object), typeof(GridTimeRangeChart));

        public object ScrollBottomItem
        {
            get { return (object)GetValue(ScrollBottomItemProperty); }
            set { SetValue(ScrollBottomItemProperty, value); }
        }
        public static DependencyProperty ScrollBottomItemProperty = DependencyProperty.Register(nameof(ScrollBottomItem), typeof(object), typeof(GridTimeRangeChart));

        public object ScrollLeftDate
        {
            get { return (object)GetValue(ScrollLeftDateProperty); }
            set { SetValue(ScrollLeftDateProperty, value); }
        }
        public static DependencyProperty ScrollLeftDateProperty = DependencyProperty.Register(nameof(ScrollLeftDate), typeof(object), typeof(GridTimeRangeChart));


        public object ScrollRightDate
        {
            get { return (object)GetValue(ScrollRightDateProperty); }
            set { SetValue(ScrollRightDateProperty, value); }
        }
        public static DependencyProperty ScrollRightDateProperty = DependencyProperty.Register(nameof(ScrollRightDate), typeof(object), typeof(GridTimeRangeChart));


        public object ScrollCenterDate
        {
            get { return (object)GetValue(ScrollCenterDateProperty); }
            set { SetValue(ScrollCenterDateProperty, value); }
        }
        public static DependencyProperty ScrollCenterDateProperty = DependencyProperty.Register(nameof(ScrollCenterDate), typeof(object), typeof(GridTimeRangeChart));

        /// <summary>
        /// string
        /// </summary>
        public Func<DateTime, Brush> HeaderBrushSelector
        {
            get { return (Func<DateTime, Brush>)GetValue(HeaderBrushSelectorProperty); }
            set { SetValue(HeaderBrushSelectorProperty, value); }
        }
        public static DependencyProperty HeaderBrushSelectorProperty = DependencyProperty.Register(nameof(HeaderBrushSelector), typeof(Func<DateTime, Brush>), typeof(GridTimeRangeChart), new PropertyMetadata(new Func<DateTime, Brush>(d => DefaultHeaderSelector(d))));


        private static Brush DefaultHeaderSelector(DateTime date)
        {
            var b = date.DayOfWeek switch
            {
                DayOfWeek.Saturday => Brushes.Blue,
                DayOfWeek.Sunday => Brushes.Red,
                _ => Brushes.Black
            };
            b.Freeze();
            return b;
        }

        private static void OnMyPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is GridTimeRangeChart gridChart)
            {
                gridChart.DrawBorder();
                gridChart.InvalidateVisual();
            }
        }
        //private static void OnNonWorkingTimesChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        //{
        //}


        private void DrawBorder()
        {
            //if (TimeTicks == null) { TimeTicks = new ObservableCollection<IRuledLine>(); }
            var ticks = TimeUnit == TimeUnitType.Normal ?
                TimeHelper.GenerateTicks(StartTime, EndTime, ScaleIncrement, SubScaleIncrement) :
                TimeHelper.GenerateTicksMonth(StartTime, EndTime, MonthSpan, SubMonthUnit);

            TimeTicks.Clear();
            SubTimeTicks.Clear();

            double newWidth = (ticks.mainTicks.Count - 1) * WidthPerRuledLine;


            var localTimeTicks = new List<RuledLineData>();

            //var localTimeTickForColumn = new NotifyCollection<IRuledLine>();

            var tf = new Typeface(FontFamily, FontStyle, FontWeight, FontStretch);
            for (int i = 0; i < ticks.mainTicks.Count; i++) //1こ多いので減らす
            {
                var item = ticks.mainTicks[i];
                string text = item.Date.ToString(HeaderFormat);
                var brush = HeaderBrushSelector?.Invoke(item.Date) ?? Brushes.Black;
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
            ChartWidth = localTimeTicks.Count * WidthPerRuledLine;

            var localSubTimeTicks = TimeRangeChart.GenerateSubScaleTicks(this, ticks.subTicks, this.SubScaleFormat, tf, newWidth);
            SubTimeTicks.AddRange(localSubTimeTicks);

            //TimeTicksForColumnHeader = localTimeTickForColumn;

            columnHeaderLayer?.InvalidateVisual();

        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);
            //DrawBorder(drawingContext);
        }




        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            var scrollViewer = GetTemplateChild("chartScrollViewer") as ScrollViewer;
            var columnGrid = GetTemplateChild("PART_ColumnGrid") as ColumnHeaderRenderLayer;
            var columnHeaderScrollViewer = GetTemplateChild("columnScrollViewer") as ScrollViewer;
            var chartList = GetTemplateChild("chartItemsControl") as ListBox;
            var rowList = GetTemplateChild("rowHeaderListBox") as ListBox;



            columnHeaderLayer = columnGrid;

            if (columnHeaderLayer != null)
            {
                columnHeaderLayer.ParentChart = this;
            }



            this.Loaded += (s, e) =>
            {
                var rowSv = GeneralHelper.FindVisualChild<ScrollViewer>(rowList);
                var chartSv = GeneralHelper.FindVisualChild<ScrollViewer>(chartList);

                bool isSyncing = false;
                if (rowSv != null && chartSv != null)
                {
                    rowSv.ScrollChanged += (s, e) =>
                    {
                        if (isSyncing) return;
                        try { isSyncing = true; chartSv.ScrollToVerticalOffset(e.VerticalOffset); }
                        finally { isSyncing = false; }
                    };
                    chartSv.ScrollChanged += (s, e) =>
                    {
                        if (isSyncing) return;
                        try { isSyncing = true; rowSv.ScrollToVerticalOffset(e.VerticalOffset); }
                        finally { isSyncing = false; }
                    };
                }

                // もしカラムヘッダと横スクロール同期が必要なら同様に chartSv と columnScroll を接続
                if (chartSv != null && columnHeaderScrollViewer != null)
                {
                    chartSv.ScrollChanged += (s, e) =>
                    {
                        if (e.HorizontalChange != 0) columnHeaderScrollViewer.ScrollToHorizontalOffset(e.HorizontalOffset);

                        //日付位置を取得

                    };
                    columnHeaderScrollViewer.ScrollChanged += (s, e) =>
                    {
                        if (e.HorizontalChange != 0) chartSv.ScrollToHorizontalOffset(e.HorizontalOffset);
                    };
                }


                //
                GeneralHelper.SetBehavior<ScrollGestureBehavior>(chartSv, true);
                //GeneralHelper.SetBehavior<ScrollViewerTopLeftItemBehavior>(chartSv, true);

                var behaviors = Microsoft.Xaml.Behaviors.Interaction.GetBehaviors(rowSv);
                var tlBehavior = new ScrollViewerTopLeftItemBehavior();
                BindingOperations.SetBinding(tlBehavior, ScrollViewerTopLeftItemBehavior.TopVisibleItemProperty,
                    new Binding(nameof(ScrollTopItem)) { Source = this, Mode= BindingMode.TwoWay, UpdateSourceTrigger= UpdateSourceTrigger.PropertyChanged });
                BindingOperations.SetBinding(tlBehavior, ScrollViewerTopLeftItemBehavior.BottomVisibleItemProperty,
                    new Binding(nameof(ScrollBottomItem)) { Source = this, Mode = BindingMode.TwoWay, UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged });
                tlBehavior.TargetItemsControl = rowList;
                behaviors.Add(tlBehavior);

                



                this.DrawBorder();
            };
            AddHandler(TimeRangeChart.TaskDropEvent, new EventHandler<DropOnRowEventArgs>(OnTaskDrop));
            AddHandler(TimeRangeChart.PreviewTaskDropEvent, new EventHandler<DropOnRowEventArgs>(OnPreviewTaskDrop));

            if (scrollViewer != null)
            {
                scrollViewer.ScrollChanged += ScrollViewer_ScrollChanged;
            }

            if (columnHeaderScrollViewer != null)
            {
                columnHeaderScrollViewer.ScrollChanged += ColumnHeaderScrollViewer_ScrollChanged;
            }

            this.MouseLeftButtonDown += (s, e) =>
            {
                ClearSelectRange();
            };

        }


        protected void OnPreviewTaskDrop(object? sender, DropOnRowEventArgs e)
        {
            var newArgs = new DropOnRowEventArgs(PreviewTaskDropEvent, e.DropPoint, e.TaskObject!);
            RaiseEvent(newArgs);
        }

        protected void OnTaskDrop(object? sender, DropOnRowEventArgs e)
        {
            var newArgs = new DropOnRowEventArgs(TaskDropEvent, e.DropPoint, e.TaskObject!);
            RaiseEvent(newArgs);
        }



        public bool CanDragTasksBetweenRows
        {
            get { return (bool)GetValue(CanDragTasksBetweenRowsProperty); }
            set { SetValue(CanDragTasksBetweenRowsProperty, value); }
        }
        public static DependencyProperty CanDragTasksBetweenRowsProperty = DependencyProperty.Register(nameof(CanDragTasksBetweenRows), typeof(bool), typeof(GridTimeRangeChart), new PropertyMetadata(true));

        public static readonly RoutedEvent TaskDropEvent =
        EventManager.RegisterRoutedEvent(
            nameof(TaskDrop), RoutingStrategy.Bubble,
            typeof(EventHandler<DropOnRowEventArgs>), typeof(GridTimeRangeChart));

        public event EventHandler<DropOnRowEventArgs> TaskDrop
        {
            add { AddHandler(TaskDropEvent, value); }
            remove { RemoveHandler(TaskDropEvent, value); }
        }
        public static readonly RoutedEvent PreviewTaskDropEvent =
        EventManager.RegisterRoutedEvent(
            nameof(PreviewTaskDrop), RoutingStrategy.Bubble,
            typeof(EventHandler<DropOnRowEventArgs>), typeof(GridTimeRangeChart));

        public event EventHandler<DropOnRowEventArgs> PreviewTaskDrop
        {
            add { AddHandler(PreviewTaskDropEvent, value); }
            remove { RemoveHandler(PreviewTaskDropEvent, value); }
        }


        private void ClearSelectRange()
        {
            if (SelectedRanges != null)
                foreach (var item in SelectedRanges)
                {
                    item.IsSelected = false;
                }
            SelectedRanges?.Clear();
            SelectedValues?.Clear();


        }

        private void ScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            var vOffset = e.VerticalOffset;
            var hOffset = e.HorizontalOffset;


        }

        private void ColumnHeaderScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (sender is not ScrollViewer sc) return;


            var LeftItem = TimeTicks.LastOrDefault(x => x.LeftPoint < e.HorizontalOffset);
            var rightItem = TimeTicks.LastOrDefault(x => x.LeftPoint < e.HorizontalOffset + sc.ActualWidth);
            var centerItem = TimeTicks.LastOrDefault(x => x.LeftPoint < e.HorizontalOffset + sc.ActualWidth / 2);

            ScrollLeftDate = LeftItem;
            ScrollRightDate = rightItem;
            ScrollCenterDate = centerItem;
        }


        #region events

        public event EventHandler<DragDropRangeEventArgs>? OnDragStart;

        public event EventHandler<DragDropRangeEventArgs>? OnDragEnd;
        internal void OnTaskDragStart(TimeRangeControl ctrl, DragDropRangeEventArgs e)
        {
            OnDragStart?.Invoke(ctrl, e);
        }

        internal void OnTaskDragEnd(TimeRangeControl ctrl, DragDropRangeEventArgs e)
        {
            OnDragEnd?.Invoke(ctrl, e);
        }


        #endregion


    }
}
