using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.DirectoryServices.ActiveDirectory;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Markup;
using FrostNova.TimeRangeChart.TimeRangeHelpers;

namespace FrostNova.TimeRangeChart.Wpf
{
    [ContentProperty(nameof(Content))]
    public class TimeRangeControl : Control
    {

        /// <summary>
        /// parent chart
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Advanced)]
        public TimeRangeChart? ParentChart
        {
            get { return (TimeRangeChart)GetValue(ParentChartProperty); }
            set { SetValue(ParentChartProperty, value); }
        }
        public static DependencyProperty ParentChartProperty = DependencyProperty.Register(nameof(ParentChart), typeof(TimeRangeChart), typeof(TimeRangeControl), new PropertyMetadata(null, OnPrentChartChanged));

        private static void OnPrentChartChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is TimeRangeControl range)
            {
                if (e.NewValue != null)
                {
                    range.ParentChart!.TimeRangeNeedRefresh += (s, e) =>
                    {
                        if (range.CanCalculate)
                        {
                            SetLeft(range);
                            SetRight(range);
                        }
                    };
                }
            }
        }


        /// <summary>
        /// contet
        /// </summary>
        /// <remarks>donot set</remarks>
        [EditorBrowsable(EditorBrowsableState.Advanced)]
        public ContentControl Content
        {
            get { return (ContentControl)GetValue(ContentProperty); }
            set { SetValue(ContentProperty, value); }
        }
        public static DependencyProperty ContentProperty = DependencyProperty.Register(nameof(Content), typeof(ContentControl), typeof(TimeRangeControl));

        /// <summary>
        /// content template
        /// </summary>
        public DataTemplate ContentTemplate
        {
            get => (DataTemplate)GetValue(ContentTemplateProperty);
            set => SetValue(ContentTemplateProperty, value);
        }
        public static readonly DependencyProperty ContentTemplateProperty =
            DependencyProperty.Register(nameof(ContentTemplate), typeof(DataTemplate), typeof(TimeRangeControl), new PropertyMetadata(null));

        /// <summary>
        /// start time of gantt
        /// </summary>
        public DateTime StartTime
        {
            get { return (DateTime)GetValue(StartTimeProperty); }
            set { SetValue(StartTimeProperty, value); }
        }
        public static DependencyProperty StartTimeProperty = DependencyProperty.Register(nameof(StartTime), typeof(DateTime), typeof(TimeRangeControl), new FrameworkPropertyMetadata(DateTime.MinValue, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnStartTimeChanged));

        private static void OnStartTimeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is TimeRangeControl range)
            {
                if (range.IsMilestone && range.EndTime != range.StartTime)
                {
                    range.EndTime = range.StartTime;
                }
                SetLeft(range);
            }
        }

        /// <summary> 
        /// end time of gantt
        /// </summary>
        public DateTime EndTime
        {
            get { return (DateTime)GetValue(EndTimeProperty); }
            set { SetValue(EndTimeProperty, value); }
        }
        public static DependencyProperty EndTimeProperty = DependencyProperty.Register(nameof(EndTime), typeof(DateTime), typeof(TimeRangeControl), new FrameworkPropertyMetadata(DateTime.MaxValue, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnEndTimeChanged));

        private static void OnEndTimeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is TimeRangeControl range)
            {

                if (range.IsMilestone && range.EndTime != range.StartTime)
                {
                    range.StartTime = range.EndTime;
                    return;
                }
                SetRight(range);
            }
        }

        /// <summary>
        /// working time span
        /// </summary>
        public TimeSpan? WorkingSpan
        {
            get { return (TimeSpan?)GetValue(WorkingSpanProperty); }
            set { SetValue(WorkingSpanProperty, value); }
        }
        public static DependencyProperty WorkingSpanProperty = DependencyProperty.Register(nameof(WorkingSpan), typeof(TimeSpan?), typeof(TimeRangeControl), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnWorkingSpanChanged));


        /// <summary>
        /// actual time span
        /// </summary>
        public TimeSpan ActualSpan
        {
            get { return (TimeSpan)GetValue(ActualSpanProperty); }
            set { SetValue(ActualSpanProperty, value); }
        }
        public static DependencyProperty ActualSpanProperty = DependencyProperty.Register(nameof(ActualSpan), typeof(TimeSpan), typeof(TimeRangeControl), new FrameworkPropertyMetadata(TimeSpan.Zero, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnActualSpanChanged));

        private static void OnActualSpanChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is TimeRangeControl range)
            {
                var width = range.Right - range.Left;
                if (width != range.Width && width >= 0 && !range.IsMilestone) range.GanttWidth = width;
            }
        }

        private static void OnWorkingSpanChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is TimeRangeControl range)
            {
                if ((range.IsMilestone == false) && e.OldValue != e.NewValue)
                {
                    SetGanttWidth(range);
                }
            }
        }

        /// <summary>
        /// width when this is gantt
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Advanced)]
        public double GanttWidth
        {
            get { return (double)GetValue(GanttWidthProperty); }
            private set { SetValue(GanttWidthProperty, value); }
        }
        public static DependencyProperty GanttWidthProperty = DependencyProperty.Register(nameof(GanttWidth), typeof(double), typeof(TimeRangeControl));

        private static void SetLeft(TimeRangeControl range)
        {
            if (range.CanCalculate)
            {
                if (range.ParentChart != null)
                {
                    range.Left = range.ParentChart.TimeToPointFunc?.Invoke(range.StartTime) ?? 0;
                }
            }


        }

        private static void SetRight(TimeRangeControl range)
        {
            if (range.CanCalculate)
            {
                if (range.ParentChart != null)
                {
                    range.Right = range.ParentChart.TimeToPointFunc?.Invoke(range.EndTime) ?? 0;
                }
            }
        }
        private static void SetGanttWidth(TimeRangeControl range)
        {
            var width = range.Right - range.Left;
            if (width != range.Width && width >= 0 && !range.IsMilestone) range.GanttWidth = width;

        }


        /// <summary>
        /// IsMilestone
        /// </summary>
        public bool IsMilestone
        {
            get { return (bool)GetValue(IsMilestoneProperty); }
            set { SetValue(IsMilestoneProperty, value); }
        }
        public static DependencyProperty IsMilestoneProperty = DependencyProperty.Register(nameof(IsMilestone), typeof(bool), typeof(TimeRangeControl), new PropertyMetadata(false));

        /// <summary>
        /// MilestoneType
        /// </summary>
        public MilestoneStyle MilestoneType
        {
            get { return (MilestoneStyle)GetValue(MilestoneTypeProperty); }
            set { SetValue(MilestoneTypeProperty, value); }
        }
        public static DependencyProperty MilestoneTypeProperty = DependencyProperty.Register(nameof(MilestoneType), typeof(MilestoneStyle), typeof(TimeRangeControl), new PropertyMetadata(MilestoneStyle.Circle));



        /// <summary>
        /// CanDrag
        /// </summary>
        public bool CanDrag
        {
            get { return (bool)GetValue(CanDragProperty); }
            set { SetValue(CanDragProperty, value); }
        }
        public static DependencyProperty CanDragProperty = DependencyProperty.Register(nameof(CanDrag), typeof(bool), typeof(TimeRangeControl), new PropertyMetadata(true));

        /// <summary>
        /// CanExpandSpan
        /// </summary>
        public bool CanExpandSpan
        {
            get { return (bool)GetValue(CanExpandSpanProperty); }
            set { SetValue(CanExpandSpanProperty, value); }
        }
        public static DependencyProperty CanExpandSpanProperty = DependencyProperty.Register(nameof(CanExpandSpan), typeof(bool), typeof(TimeRangeControl), new PropertyMetadata(true));


        /// <summary>
        /// IgnoreNonWorkingTimes
        /// </summary>
        public bool IgnoreNonWorkingTimes
        {
            get { return (bool)GetValue(IgnoreNonWorkingTimesProperty); }
            set { SetValue(IgnoreNonWorkingTimesProperty, value); }
        }
        public static DependencyProperty IgnoreNonWorkingTimesProperty = DependencyProperty.Register(nameof(IgnoreNonWorkingTimes), typeof(bool), typeof(TimeRangeControl), new PropertyMetadata(false));






        /// <summary>
        /// Left
        /// </summary>
        public double Left
        {
            get { return (double)GetValue(LeftProperty); }
            private set { SetValue(LeftProperty, value); }
        }
        public static DependencyProperty LeftProperty = DependencyProperty.Register(nameof(Left), typeof(double), typeof(TimeRangeControl));

        /// <summary>
        /// Top
        /// </summary>
        public double Top
        {
            get { return (double)GetValue(TopProperty); }
            set { SetValue(TopProperty, value); }
        }
        public static DependencyProperty TopProperty = DependencyProperty.Register(nameof(Top), typeof(double), typeof(TimeRangeControl));


        /// <summary>
        /// Right
        /// </summary>
        public double Right
        {
            get { return (double)GetValue(RightProperty); }
            private set { SetValue(RightProperty, value); }
        }
        public static DependencyProperty RightProperty = DependencyProperty.Register(nameof(Right), typeof(double), typeof(TimeRangeControl));


        public bool CanCalculate => ParentChart != null && ParentChart.TimeToPointFunc != null && ParentChart.PointToTimeFunc != null && StartTime != DateTime.MinValue && EndTime != DateTime.MaxValue;

        ///// <summary>
        ///// VerticalAlignment
        ///// </summary>
        //public new VerticalAlignment VerticalAlignment
        //{
        //    get { return (VerticalAlignment)GetValue(VerticalAlignmentProperty); }
        //    set { SetValue(VerticalAlignmentProperty, value); }
        //}
        //public static DependencyProperty VerticalAlignmentProperty = DependencyProperty.Register(nameof(VerticalAlignment), typeof(VerticalAlignment), typeof(TimeRangeControl));

        private void Aaa()
        {
           
        }





        public ICommand MoveCommand
        {
            get { return (ICommand)GetValue(MoveCommandProperty); }
            set { SetValue(MoveCommandProperty, value); }
        }
        public static DependencyProperty MoveCommandProperty = DependencyProperty.Register(nameof(MoveCommand), typeof(ICommand), typeof(TimeRangeControl));

        public ICommand ExpandStartTimeCommand
        {
            get { return (ICommand)GetValue(ExpandStartTimeCommandProperty); }
            set { SetValue(ExpandStartTimeCommandProperty, value); }
        }
        public static DependencyProperty ExpandStartTimeCommandProperty = DependencyProperty.Register(nameof(ExpandStartTimeCommand), typeof(ICommand), typeof(TimeRangeControl));

        public ICommand ExpandEndTimeCommand
        {
            get { return (ICommand)GetValue(ExpandEndTimeCommandProperty); }
            set { SetValue(ExpandEndTimeCommandProperty, value); }
        }
        public static DependencyProperty ExpandEndTimeCommandProperty = DependencyProperty.Register(nameof(ExpandEndTimeCommand), typeof(ICommand), typeof(TimeRangeControl));


        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            var moveThumb = GetTemplateChild("PART_MoveThumb") as Thumb;
            var leftThumb = GetTemplateChild("PART_LeftThumb") as Thumb;
            var rightThumb = GetTemplateChild("PART_RightThumb") as Thumb;

            if (moveThumb != null)
            {
                moveThumb.DragDelta += (s, e) =>
                {
                    if (!CanDrag) return;
                    if (!CanCalculate) return;
                    if (ParentChart == null) return;
                    var beforeSpan = WorkingSpan;

                    var deltaX = e.HorizontalChange;

                    var startPoint = ParentChart.TimeToPointFunc(StartTime);
                    var movedStartTime = ParentChart.PointToTimeFunc(deltaX + startPoint);

                    var deltaTime = movedStartTime - StartTime;

                    if (deltaTime == TimeSpan.Zero) return;
                    var args = new MoveEventParameter(DataContext, deltaTime);

                    if (MoveCommand != null)
                    {
                        if (MoveCommand?.CanExecute(args) == true)
                            MoveCommand.Execute(args);
                    }
                    else if (this.DataContext is ITimeRange timeRange)
                    {
                        timeRange.Move(deltaTime, ParentChart.NonWorkingTimes);
                    }

                };
            }

            if (leftThumb != null)
            {
                leftThumb.DragDelta += (s, e) =>
                {
                    if (!CanExpandSpan) { return; }
                    if (!CanCalculate) return;
                    // 左側を動かす（StartTimeを変更、Spanを再計算）
                    if (ParentChart == null) return;

                    var oldStartTime = StartTime;

                    var deltaX = e.HorizontalChange;
                    var newStart = ParentChart.PointToTimeFunc(ParentChart.TimeToPointFunc(StartTime) + deltaX);

                    if (oldStartTime == newStart) return; // 変更なし

                    if (newStart < EndTime)
                    {
                        var args = new ExpandEventParameter(DataContext, newStart);

                        if (ExpandStartTimeCommand != null)
                        {
                            if (ExpandStartTimeCommand?.CanExecute(args) == true)
                                ExpandStartTimeCommand.Execute(args);
                        }
                        else if (this.DataContext is ITimeRange timeRange)
                        {
                            timeRange.ExpandStartTime(newStart, ParentChart.NonWorkingTimes);
                        }

                    }




                };
            }

            if (rightThumb != null)
            {
                rightThumb.DragDelta += (s, e) =>
                {
                    if (!CanExpandSpan) { return; }
                    if (ParentChart == null) return;
                    if (!CanCalculate) return;

                    // 右側を動かす（EndTimeまたはSpanを変更）

                    var oldEndTime = EndTime;
                    var deltaX = e.HorizontalChange;
                    var newEnd = ParentChart.PointToTimeFunc(ParentChart.TimeToPointFunc(EndTime) + deltaX);

                    if (oldEndTime == newEnd) return; // 変更なし

                    if (newEnd > StartTime)
                    {
                        var args = new ExpandEventParameter(DataContext, newEnd);

                        if (ExpandEndTimeCommand != null)
                        {
                            if (ExpandEndTimeCommand?.CanExecute(args) == true)
                                ExpandEndTimeCommand.Execute(args);
                        }
                        else if (this.DataContext is ITimeRange timeRange)
                        {
                            timeRange.ExpandEndTime(newEnd, ParentChart.NonWorkingTimes);
                        }

                    }


                };
            }
            TryBindDefaultProperties();

            if (CanCalculate)
            {

                SetLeft(this);
                SetRight(this);
                SetGanttWidth(this);
            }

            this.Loaded += (s, e) =>
            {

                if (this.DataContext is not ITimeRange)
                {
                    Debug.Assert(false, $"DataContext of TimeRangeControl should be a {typeof(ITimeRange).FullName} type.");
                }
                SetLeft(this);
                SetRight(this);
                SetGanttWidth(this);

            };


        }
        //private bool _isloaded = false;

        private void TryBindDefaultProperties()
        {
            if (this.DataContext is ITimeRange timeRange)
            {

                if (BindingOperations.GetBindingExpression(this, StartTimeProperty) == null)
                {
                    var startBinding = new Binding(nameof(ITimeRange.StartTime))
                    {
                        Source = timeRange,
                        Mode = BindingMode.TwoWay
                    };
                    BindingOperations.SetBinding(this, StartTimeProperty, startBinding);
                }

                if (BindingOperations.GetBindingExpression(this, EndTimeProperty) == null)
                {
                    var endBinding = new Binding(nameof(ITimeRange.EndTime))
                    {
                        Source = timeRange,
                        Mode = BindingMode.TwoWay
                    };
                    BindingOperations.SetBinding(this, EndTimeProperty, endBinding);
                }

                if (BindingOperations.GetBindingExpression(this, WorkingSpanProperty) == null)
                {
                    var workingTimeBinding = new Binding(nameof(ITimeRange.WorkingSpan))
                    {
                        Source = timeRange,
                        Mode = BindingMode.TwoWay
                    };
                    BindingOperations.SetBinding(this, WorkingSpanProperty, workingTimeBinding);
                }

                if (BindingOperations.GetBindingExpression(this, ActualSpanProperty) == null)
                {
                    var actualTimeBinding = new Binding(nameof(ITimeRange.ActualSpan))
                    {
                        Source = timeRange,
                        Mode = BindingMode.OneWayToSource
                    };
                    BindingOperations.SetBinding(this, ActualSpanProperty, actualTimeBinding);
                }

            }


        }

    }


}
