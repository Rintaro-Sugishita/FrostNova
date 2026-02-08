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
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using FrostNova.Core.Wpf.DragHelpers;
using FrostNova.TimeRangeChart.TimeRangeHelpers;

namespace FrostNova.TimeRangeChart.Wpf
{
    [ContentProperty(nameof(Content))]
    public class TimeRangeControl : Control
    {

        #region properties

        /// <summary>
        /// parent chart
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Advanced)]
        public TimeRangeChart? ParentChart
        {
            get { return (TimeRangeChart)GetValue(ParentChartProperty); }
            set { SetValue(ParentChartProperty, value); }
        }
        public static DependencyProperty ParentChartProperty = DependencyProperty.Register(nameof(ParentChart), typeof(TimeRangeChart), typeof(TimeRangeControl), new PropertyMetadata(null, OnParentChartChanged));

        private static void OnParentChartChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
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
                            SetTaskWidth(range);
                        }
                    };
                    if (range.CanCalculate)
                    {
                        SetLeft(range);
                        SetRight(range);
                        SetTaskWidth(range);
                    }
                }
            }
        }


        /// <summary>
        /// content
        /// </summary>
        /// <remarks>do not set</remarks>
        [EditorBrowsable(EditorBrowsableState.Advanced)]
        public ContentControl Content
        {
            get { return (ContentControl)GetValue(ContentProperty); }
            internal set { SetValue(ContentProperty, value); }
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
        /// start time of task range
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
        /// end time of task range
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
                if (width != range.Width && width >= 0 && !range.IsMilestone) range.TaskWidth = width;
            }
        }

        private static void OnWorkingSpanChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is TimeRangeControl range)
            {
                if ((range.IsMilestone == false) && e.OldValue != e.NewValue)
                {
                    SetTaskWidth(range);
                }
            }
        }

        /// <summary>
        /// width when this is task range
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Advanced)]
        public double TaskWidth
        {
            get { return (double)GetValue(TaskWidthProperty); }
            private set { SetValue(TaskWidthProperty, value); }
        }
        public static DependencyProperty TaskWidthProperty = DependencyProperty.Register(nameof(TaskWidth), typeof(double), typeof(TimeRangeControl));

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
        private static void SetTaskWidth(TimeRangeControl range)
        {
            var width = range.Right - range.Left;
            if (width != range.Width && width >= 0 && !range.IsMilestone) range.TaskWidth = width;

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
        /// min span of range
        /// </summary>
        public TimeSpan? MinSpan
        {
            get { return (TimeSpan?)GetValue(MinSpanProperty); }
            set { SetValue(MinSpanProperty, value); }
        }
        public static readonly DependencyProperty MinSpanProperty = DependencyProperty.Register(nameof(MinSpan), typeof(TimeSpan?), typeof(TimeRangeControl), new PropertyMetadata(null));





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

        /// <summary>
        /// string
        /// </summary>
        public bool IsExpandThumbAnyTimeVisible
        {
            get { return (bool)GetValue(IsExpandThumbAnyTimeVisibleProperty); }
            set { SetValue(IsExpandThumbAnyTimeVisibleProperty, value); }
        }
        public static DependencyProperty IsExpandThumbAnyTimeVisibleProperty = DependencyProperty.Register(nameof(IsExpandThumbAnyTimeVisible), typeof(bool), typeof(TimeRangeControl), new PropertyMetadata(false));


        /// <summary>
        /// 
        /// </summary>
        public Brush SelectedBorderBrush
        {
            get { return (Brush)GetValue(SelectedBorderBrushProperty); }
            set { SetValue(SelectedBorderBrushProperty, value); }
        }
        public static DependencyProperty SelectedBorderBrushProperty = DependencyProperty.Register(nameof(SelectedBorderBrush), typeof(Brush), typeof(TimeRangeControl), new PropertyMetadata(Brushes.Red));

        /// <summary>
        /// 
        /// </summary>
        public Thickness SelectedBorderThickness
        {
            get { return (Thickness)GetValue(SelectedBorderThicknessProperty); }
            set { SetValue(SelectedBorderThicknessProperty, value); }
        }
        public static DependencyProperty SelectedBorderThicknessProperty = DependencyProperty.Register(nameof(SelectedBorderThickness), typeof(Thickness), typeof(TimeRangeControl), new PropertyMetadata(new Thickness(2)));

        /// <summary>
        /// is select range
        /// </summary>
        public bool IsSelected
        {
            get { return (bool)GetValue(IsSelectedProperty); }
            set { SetValue(IsSelectedProperty, value); }
        }
        public static DependencyProperty IsSelectedProperty = DependencyProperty.Register(nameof(IsSelected), typeof(bool), typeof(TimeRangeControl));


        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);

            //ParentChart?.OnTaskClicked(this);           
        }

        private void PART_DragThumb_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is Thumb thumb && thumb.TemplatedParent is TimeRangeControl parent)
            {
                //parent.Focus(); // 親（本体）にフォーカスを与える
                e.Handled = false; // Thumb のドラッグ処理も有効に残すなら false
            }
            ParentChart?.OnTaskClicked(this);
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

        #endregion

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            var moveThumb = GetTemplateChild("PART_MoveThumb") as Thumb;
            var leftThumb = GetTemplateChild("PART_LeftThumb") as Thumb;
            var rightThumb = GetTemplateChild("PART_RightThumb") as Thumb;

            if (moveThumb != null)
            {
                bool isMoving = false;
                moveThumb.DragDelta += (s, e) =>
                {

                    if (!CanDrag) return;
                    if (!CanCalculate) return;
                    if (ParentChart == null) return;
                    var beforeSpan = WorkingSpan;

                    var deltaX = e.HorizontalChange;

                    var startPoint = ParentChart.TimeToPointFunc(StartTime);
                    var movedStartTime = ParentChart.PointToTimeFunc(deltaX + startPoint, MinSpan);

                    var deltaTime = movedStartTime - StartTime;

                    if (deltaTime == TimeSpan.Zero) return;

                    if (isMoving) return;
                    try
                    {
                        isMoving = true;
                        var args = new MoveEventParameter(DataContext, deltaTime);

                        if (MoveCommand != null)
                        {
                            if (MoveCommand?.CanExecute(args) == true)
                                MoveCommand.Execute(args);
                        }
                        else if (this.DataContext is ITimeRange timeRange)
                        {
                            timeRange.Move(deltaTime, ParentChart.SortedNonWorkingTimes);
                        }

                    }
                    finally
                    {
                        isMoving = false;
                    }
                };

                if (this.IsSelected == false)
                    moveThumb.PreviewMouseLeftButtonDown += PART_DragThumb_PreviewMouseDown;
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
                    var newStart = ParentChart.PointToTimeFunc(ParentChart.TimeToPointFunc(StartTime) + deltaX, MinSpan);

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
                            timeRange.ExpandStartTime(newStart, ParentChart.SortedNonWorkingTimes);
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
                    var newEnd = ParentChart.PointToTimeFunc(ParentChart.TimeToPointFunc(EndTime) + deltaX, MinSpan);

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
                            timeRange.ExpandEndTime(newEnd, ParentChart.SortedNonWorkingTimes);
                        }

                    }


                };
            }
            TryBindDefaultProperties();

            if (CanCalculate)
            {

                SetLeft(this);
                SetRight(this);
                SetTaskWidth(this);
            }

            this.Loaded += (s, e) =>
            {

                if (this.DataContext is not ITimeRange)
                {
                    Debug.Assert(false, $"DataContext of TimeRangeControl should be a {typeof(ITimeRange).FullName} type.");
                }
                SetLeft(this);
                SetRight(this);
                SetTaskWidth(this);

            };


        }

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





        private DragAdorner? _dragAdorner;
        private AdornerLayer? _adornerLayer;
        private Point _dragStartPoint;

        //start time when start drag
        private DateTime? _dragStartTime = null;
        private TimeSpan? _dragActualSpan = null;
        private TimeSpan? _dragWorkingSpan = null;
        private DateTime? _dragEndTime = null;

        protected override void OnPreviewMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            base.OnPreviewMouseLeftButtonDown(e);
            _dragStartPoint = e.GetPosition(this);
            _dragStartTime = StartTime;
            _dragActualSpan = ActualSpan;
            _dragWorkingSpan = WorkingSpan;
            _dragEndTime = EndTime;
            //var startTime = ParentChart?.PointToTimeFunc(_dragStartPoint.X, MinSpan);
            var e2 = new DragDropRangeEventArgs();
            e2.DragStartTime = _dragStartTime;
            e2.DragActualSpan = _dragActualSpan.Value;
            if (_dragWorkingSpan != null) e2.DragWorkingSpan = _dragWorkingSpan.Value;

            OnDragStart?.Invoke(this, e2);
            ParentChart?.TaskDragStartAction.Invoke(this, e2);

        }

        protected override void OnPreviewMouseMove(MouseEventArgs e)
        {
            base.OnPreviewMouseMove(e);

            if (ParentChart?.CanDragTasksBetweenRows != true) { return; }

            //ParentChart .ParentGridChart.MouseMove+=



            if (e.LeftButton == MouseButtonState.Pressed && _dragAdorner == null)
            {
                var currentPos = e.GetPosition(this);
                if (Math.Abs(currentPos.Y - _dragStartPoint.Y) > this?.ActualHeight)
                {
                    // AdornerLayer取得
                    _adornerLayer = AdornerLayer.GetAdornerLayer(this);
                    if (_adornerLayer != null)
                    {
                        _dragAdorner = new DragAdorner(this, this, 0.5);
                        _adornerLayer.Add(_dragAdorner);
                    }

                    var newE = new DragTaskData();
                    newE.ChartStartTime = ParentChart?.PointToTimeFunc(_dragStartPoint.X, MinSpan);
                    newE.SourceRow = ParentChart?.ItemsSource;
                    newE.TimeRange = this.DataContext;

                    // DragDrop開始
                    DragDrop.DoDragDrop(this, newE, DragDropEffects.Move);

                    // 終了時にAdornerを削除
                    if (_adornerLayer != null && _dragAdorner != null)
                    {
                        _adornerLayer.Remove(_dragAdorner);
                        _dragAdorner = null;
                    }
                }
            }




            // Adornerの位置を更新
            if (_dragAdorner != null)
            {
                var pos = e.GetPosition(_adornerLayer);
                _dragAdorner.SetOffset(new Point(pos.X - _dragStartPoint.X, pos.Y - _dragStartPoint.Y));
            }

        }

        protected override void OnPreviewMouseLeftButtonUp(MouseButtonEventArgs e)
        {
            base.OnPreviewMouseLeftButtonUp(e);

            if (_dragStartTime == null) return;

            var dragEndPosition = e.GetPosition(this);
            var e2 = new DragDropRangeEventArgs();
            e2.DragData = this.DataContext;
            e2.DragStartTime = _dragStartTime;
            e2.DragEndTime = _dragEndTime;
            if (_dragActualSpan != null) e2.DragActualSpan = _dragActualSpan.Value;
            if (_dragWorkingSpan != null) e2.DragWorkingSpan = _dragWorkingSpan.Value;

            e2.DropStartTime = StartTime;
            e2.DropWorkingSpan = WorkingSpan;
            e2.DropActualSpan = ActualSpan;

            var minSpan = MinSpan ?? ParentChart?.MinSpan;

            if (minSpan != null && (ActualSpan < minSpan.Value || (IgnoreNonWorkingTimes ==false && WorkingSpan != null && WorkingSpan.Value < minSpan.Value))) e2.Cancel = true;

            OnDragEnd?.Invoke(this, e2);
            ParentChart?.TaskDragEndAction.Invoke(this, e2);

            if (e2.Cancel)
            {
                if (_dragStartTime == null || _dragActualSpan == null) return;

                //StartTime = _dragStartTime.Value;
                //ActualSpan = _dragActualSpan.Value;
                //if (_dragWorkingSpan != null) WorkingSpan = _dragWorkingSpan.Value;

                if (this.DataContext is ITimeRange timeRange)
                {
                    //開始と終了が変わっていたらMoveで戻す
                    //どっちかだけ変わってたらExpandで戻す
                    if (WorkingSpan == _dragWorkingSpan)
                    {
                        //移動しているので元の場所に戻す
                        timeRange.MoveTo(_dragStartTime.Value, ParentChart?.SortedNonWorkingTimes);
                    }
                    else
                    {
                        if (_dragStartTime.Value != StartTime)
                        {
                            timeRange.ExpandStartTime(_dragStartTime.Value, ParentChart?.SortedNonWorkingTimes);
                        }
                        else if(_dragEndTime.Value != EndTime)
                        {
                            timeRange.ExpandEndTime(_dragEndTime.Value, ParentChart?.SortedNonWorkingTimes);
                        }
                    }
                }
            }

            _dragStartTime = null;
            _dragActualSpan = null;
            _dragWorkingSpan = null;
            _dragEndTime = null;


        }

        #region events

        public event EventHandler<DragDropRangeEventArgs>? OnDragStart;

        public event EventHandler<DragDropRangeEventArgs>? OnDragEnd;




        #endregion


    }
}
