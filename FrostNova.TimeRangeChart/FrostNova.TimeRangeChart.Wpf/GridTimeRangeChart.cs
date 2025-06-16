using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
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
using FrostNova.TimeRangeChart.TimeRangeHelpers;

namespace FrostNova.TimeRangeChart.Wpf
{
    public class GridTimeRangeChart : Control
    {
        static GridTimeRangeChart()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(GridTimeRangeChart), new FrameworkPropertyMetadata(typeof(GridTimeRangeChart)));
        }
        public GridTimeRangeChart()
        {
            if (TimeTicks == null)
            {
                TimeTicks = new ObservableCollection<IRuledLine>();
            }
        }

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


        public ObservableCollection<IRuledLine> TimeTicks
        {
            get { return (ObservableCollection<IRuledLine>)GetValue(TimeTicksProperty); }
            internal set { SetValue(TimeTicksProperty, value); }
        }
        public static DependencyProperty TimeTicksProperty = DependencyProperty.Register(nameof(TimeTicks), typeof(ObservableCollection<IRuledLine>), typeof(GridTimeRangeChart));


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


        private static void OnMyPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is GridTimeRangeChart gridChart)
            {
                gridChart.DrawBorder();
            }
        }
        //private static void OnNonWorkingTimesChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        //{
        //}


        private void DrawBorder()
        {
            //if (TimeTicks == null) { TimeTicks = new ObservableCollection<IRuledLine>(); }
            var ticks = TimeUnit == TimeUnitType.Normal ? TimeHelper.GenerateTicks(StartTime, EndTime, ScaleIncrement) : TimeHelper.GenerateTicksMonth(StartTime, EndTime, MonthSpan);

            TimeTicks.Clear();

            double newWidth = (ticks.Count-1) * WidthPerRuledLine;


            var localTimeTicks = new ObservableCollection<IRuledLine>();
            foreach (var item in ticks)
            {
                localTimeTicks.Add(new RuledLineData(this) { DateValue = item, LeftPoint= TimeScreenHelper.TimeToPoint(item, StartTime, EndTime, newWidth) });
            }
            TimeTicks = localTimeTicks;
        }


        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            //ganttCanvas = GetTemplateChild("ganttCanvas") as ItemsControl;
            this.Loaded += (s, e) =>
            {
                this.DrawBorder();
            };

        



        }


    }
}
