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
            if (TimeTicks == null)
            {
                //TimeTicks = new ObservableCollection<IRuledLine>();
            }
        }

        public Func<DateTime, double> TimeToPointFunc { get; private set; }
        public Func<double, TimeSpan?, DateTime> PointToTimeFunc { get; private set; }

        public Func<DateTime, TimeSpan, DateTime> GetEndTimeFunc { get; private set; }
        public Func<DateTime, DateTime, TimeSpan> GetWorkingSpanFunc { get; private set; }


        public event EventHandler<EventArgs>? TimeRangeNeedRefresh;

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
        public static DependencyProperty RuledLineFillProperty = DependencyProperty.Register(nameof(RuledLineFill), typeof(Brush), typeof(TimeRangeChart), new PropertyMetadata(new SolidColorBrush(Color.FromArgb(255, 255, 255, 255)), OnMyPropertyChanged));

        public double RuledLineThickness
        {
            get { return (double)GetValue(RuledLineThicknessProperty); }
            set { SetValue(RuledLineThicknessProperty, value); RefreshCall(); }
        }
        public static DependencyProperty RuledLineThicknessProperty = DependencyProperty.Register(nameof(RuledLineThickness), typeof(double), typeof(TimeRangeChart), new PropertyMetadata(1.0, OnMyPropertyChanged));

        /// <summary>
        /// background of non-working ranges
        /// </summary>
        public Brush NonWorkingRangeBackground
        {
            get { return (Brush)GetValue(NonWorkingRangeBackgroundProperty); }
            set { SetValue(NonWorkingRangeBackgroundProperty, value); }
        }
        public static DependencyProperty NonWorkingRangeBackgroundProperty = DependencyProperty.Register(nameof(NonWorkingRangeBackground), typeof(Brush), typeof(TimeRangeChart), new PropertyMetadata(new SolidColorBrush(Color.FromRgb(0xd8, 0xd8, 0xd8))));




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
                var sorted = chart.NonWorkingTimes
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

                chart.SortedNonWorkingTimes = merged;


            }
            static DateTime Max(DateTime a, DateTime b) => a > b ? a : b;

        }

        /// <summary>
        /// time
        /// </summary>
        public IList<ITimeRange> SortedNonWorkingTimes
        {
            get { return (IList<ITimeRange>)GetValue(SortedNonWorkingTimesProperty); }
            set { SetValue(SortedNonWorkingTimesProperty, value); }
        }
        public static DependencyProperty SortedNonWorkingTimesProperty = DependencyProperty.Register(nameof(SortedNonWorkingTimes), typeof(IList<ITimeRange>), typeof(TimeRangeChart));

        
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


        public ObservableCollection<IRuledLine> TimeTicks
        {
            get { return (ObservableCollection<IRuledLine>)GetValue(TimeTicksProperty); }
            set { SetValue(TimeTicksProperty, value); }
        }
        public static DependencyProperty TimeTicksProperty = DependencyProperty.Register(nameof(TimeTicks), typeof(ObservableCollection<IRuledLine>), typeof(TimeRangeChart));



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

        private void DrawBorder()
        {
            //if (TimeTicks == null) { TimeTicks = new ObservableCollection<IRuledLine>(); } //これをすると親のDependencyPropertyから外れる
            if (TimeTicks == null) return;
            var ticks = TimeTicks.ToList();
            if (IsControlTick)
            {
                //自身で罫線を管理する場合は再計算する
                //そうでない場合は親から指定された値を使用する
               var newTicks = TimeUnit == TimeUnitType.Normal ? TimeHelper.GenerateTicks(StartTime, EndTime, ScaleIncrement) : TimeHelper.GenerateTicksMonth(StartTime, EndTime, 1);

                TimeTicks.Clear();


                foreach (var item in newTicks)
                {
                    TimeTicks.Add(new RuledLineData(this) { DateValue = item, LeftPoint = TimeToPoint(item) });
                }
            }


            if (!double.IsNaN(WidthPerRuledLine) && WidthPerRuledLine > 0)
            {

                var newWidth = (ticks.Count-1) * WidthPerRuledLine;
                if (newWidth > 0)
                {
                    this.Width = newWidth;
                }
            }
            else
            {
                //this.Width = double.NaN;
                this.ClearValue(WidthProperty);
            }



            //ScaleIncrement


        }


    


        private DateTime GetEndTime(DateTime startTime, TimeSpan workingSpan)
        {
            return TimeHelper.CalcEndTime(startTime, workingSpan, SortedNonWorkingTimes);
        }


        private TimeSpan GetWorkingSpan(DateTime startTime, DateTime endTime)
        {
            return TimeHelper.CalcWorkingSpan(startTime, endTime, SortedNonWorkingTimes);
        }



        private void Refresh()
        {
            DrawBorder();
            //AddSeries();
            //AddPoints();

            TimeRangeNeedRefresh?.Invoke(this, new EventArgs());
        }

        private void RefreshCall()
        {
            Refresh();

        }


        private double TimeToPoint(DateTime time)
        {
            switch (TimeUnit)
            {
                case TimeUnitType.Normal:
                    return TimeScreenHelper.TimeToPoint(time, StartTime, EndTime, ActualWidth == 0 && Width != double.NaN ? Width : ActualWidth);
                case TimeUnitType.Month:
                    return TimeScreenHelper.TimeToPointMonth(time, StartTime, EndTime, ActualWidth == 0 && Width != double.NaN ? Width: ActualWidth, TimeTicks);
                default:
                    break;
            }
            return 0;
        }

        private DateTime PointToTime(double point, TimeSpan? minSpan=null)
        {
            switch (TimeUnit)
            {
                case TimeUnitType.Normal:
                    return TimeScreenHelper.PointToTime(point, StartTime, EndTime, ActualWidth == 0 && Width != double.NaN ? Width : ActualWidth,minSpan?? MinSpan);
                case TimeUnitType.Month:
                    return TimeScreenHelper.PointToTimeMonth(point, StartTime, EndTime, ActualWidth == 0 && Width != double.NaN ? Width : ActualWidth,minSpan?? MinSpan, TimeTicks);
                default:
                    break;
            }
            return StartTime;
        }


        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            if (TimeTicks == null)
            {
                TimeTicks = new ObservableCollection<IRuledLine>();
            }

            //Refresh();
            this.SizeChanged += (s, e) =>
            {
                // サイズ変更時に再描画
                Refresh();
            };
        }


        public bool CanDragTasksBetweenRows
        {
            get { return (bool)GetValue(CanDragTasksBetweenRowsProperty); }
            set { SetValue(CanDragTasksBetweenRowsProperty, value); }
        }
        public static DependencyProperty CanDragTasksBetweenRowsProperty = DependencyProperty.Register(nameof(CanDragTasksBetweenRows), typeof(bool), typeof(TimeRangeChart), new PropertyMetadata(false));




        protected override void OnPreviewDrop(DragEventArgs e)
        {
            base.OnPreviewDrop(e);

           if (CanDragTasksBetweenRows  == false) { return; }

            var formats = e.Data.GetFormats();
            if (formats != null && formats.Length > 0)
            {
                var data = e.Data.GetData(formats[0]);
                if (data is DragTaskData data2)
                {
                    var param = new DragTaskData();


                    var dropPoint = this?.PointToTimeFunc(e.GetPosition(this).X, null);

                    var args = new DropOnRowEventArgs(PreviewTaskDropEvent, dropPoint.Value, data2.TimeRange);
                    args.SourceList = data2.SourceRow;
                    args.DestList = this.ItemsSource;
                    RaiseEvent(args);
                    if (args.Cancel) { return; }

                    args.SourceList?.Remove(args.TaskObject);


                    //開始時間を書き換える
                    if (args.TaskObject is ITimeRange range)
                    {
                        range.Move(dropPoint.Value - range.StartTime, NonWorkingTimes);
                    }

                    args.DestList.Add(args.TaskObject);

                    var args2 = new DropOnRowEventArgs(TaskDropEvent, dropPoint.Value, data2.TimeRange);
                    RaiseEvent(args2);
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

    }
}
