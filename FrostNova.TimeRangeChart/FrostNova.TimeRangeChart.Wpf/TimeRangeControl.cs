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

namespace FrostNova.TimeRangeChart.Wpf;

/// <summary>
/// Represents a control that displays and manages a time range or milestone within a chart, supporting drag, resize,
/// and customizable appearance.
/// </summary>
/// <remarks>TimeRangeControl is typically used in scheduling or Gantt chart scenarios to visually represent a
/// time span or milestone. The control supports customization through data templates, symbol size, label positioning,
/// and milestone styles. It provides properties for binding to start and end times, as well as for configuring user
/// interactions such as dragging and resizing. The control is designed to be used within a charting or timeline context
/// and exposes dependency properties for integration with WPF data binding and styling systems.</remarks>
public class TimeRangeControl : Control
{

    internal enum HitTestMode
    {
        None,
        Move,
        ResizeLeft,
        ResizeRight
    }

    internal HitTestMode _currentMode;
    static TimeRangeControl()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(TimeRangeControl),
            new FrameworkPropertyMetadata(typeof(TimeRangeControl)));
    }

    public TimeRangeControl()
    {
    }
    public string ControlId => $"View_{this.GetHashCode():X8}";

    #region Dependency Properties

    /// <summary>Get or Set the content. (Model data)</summary>
    public object Content { get => GetValue(ContentProperty); set => SetValue(ContentProperty, value); }
    public static readonly DependencyProperty ContentProperty =
        DependencyProperty.Register(nameof(Content), typeof(object), typeof(TimeRangeControl), new PropertyMetadata(null));

    /// <summary>Get or set the template of control.</summary>
    public DataTemplate ContentTemplate { get => (DataTemplate)GetValue(ContentTemplateProperty); set => SetValue(ContentTemplateProperty, value); }

    public static readonly DependencyProperty ContentTemplateProperty =
        DependencyProperty.Register(nameof(ContentTemplate), typeof(DataTemplate), typeof(TimeRangeControl), new PropertyMetadata(null));

    /// <summary>
    /// Get or set then template selector of control.
    /// </summary>
    public DataTemplateSelector ContentTemplateSelector
    {
        get { return (DataTemplateSelector)GetValue(ContentTemplateSelectorProperty); }
        set { SetValue(ContentTemplateSelectorProperty, value); }
    }
    public static DependencyProperty ContentTemplateSelectorProperty = DependencyProperty.Register(nameof(ContentTemplateSelector), typeof(DataTemplateSelector), typeof(TimeRangeControl), new PropertyMetadata(null));




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
    /// Gets the actual Left position of the control within the chart.
    /// This value is internally calculated based on the StartTime.
    /// </summary>
    public double Left { get => (double)GetValue(LeftProperty); internal set => SetValue(LeftProperty, value); }
    public static readonly DependencyProperty LeftProperty = DependencyProperty.Register(nameof(Left), typeof(double), typeof(TimeRangeControl), new PropertyMetadata(0.0, OnPositionChanged));

    /// <summary>
    /// Gets the actual Top position of the control within the chart.
    /// This value is internally calculated based on the StackBaseTop.
    /// </summary>
    public double Top { get => (double)GetValue(TopProperty); internal set => SetValue(TopProperty, value); }
    public static readonly DependencyProperty TopProperty = DependencyProperty.Register(nameof(Top), typeof(double), typeof(TimeRangeControl), new PropertyMetadata(0.0, OnPositionChanged));


    /// <summary>
    /// Gets the actual Right position of the control.
    /// </summary>
    public double Right
    {
        get { return (double)GetValue(RightProperty); }
        internal set { SetValue(RightProperty, value); }
    }
    public static readonly DependencyProperty RightProperty = DependencyProperty.Register(nameof(Right), typeof(double), typeof(TimeRangeControl));

    /// <summary>
    /// Gets the current width of the task element, in device-independent units (DIPs).
    /// </summary>
    public double TaskWidth
    {
        get { return (double)GetValue(TaskWidthProperty); }
        internal set { SetValue(TaskWidthProperty, value); }
    }
    public static DependencyProperty TaskWidthProperty = DependencyProperty.Register(nameof(TaskWidth), typeof(double), typeof(TimeRangeControl));

    /// <summary>
    /// Gets or sets the size of the symbol displayed by the control.
    /// </summary>
    /// <remarks>The symbol size is typically specified in device-independent units (1/96th inch per
    /// unit). Adjust this property to change the visual appearance of the symbol. The default value may vary
    /// depending on the control's style or template.</remarks>
    public double SymbolSize
    {
        get => (double)GetValue(SymbolSizeProperty);
        set => SetValue(SymbolSizeProperty, value);
    }
    public static readonly DependencyProperty SymbolSizeProperty =
        DependencyProperty.Register(nameof(SymbolSize), typeof(double), typeof(TimeRangeControl), new PropertyMetadata(20.0));

    /// <summary>
    /// Gets or sets the position of milestone label contents
    /// </summary>
    public LabelPosition LabelPosition
    {
        get { return (LabelPosition)GetValue(LabelPositionProperty); }
        set { SetValue(LabelPositionProperty, value); }
    }
    public static DependencyProperty LabelPositionProperty = DependencyProperty.Register(nameof(LabelPosition), typeof(LabelPosition), typeof(TimeRangeControl));

    /// <summary>
    /// Gets or sets the spacing between the symbol and the label.
    /// </summary>
    public double LabelSpacing
    {
        get { return (double)GetValue(LabelSpacingProperty); }
        set { SetValue(LabelSpacingProperty, value); }
    }
    public static readonly DependencyProperty LabelSpacingProperty = DependencyProperty.Register(nameof(LabelSpacing), typeof(double), typeof(TimeRangeControl), new PropertyMetadata(4.0));

    /// <summary>
    /// IsMilestone
    /// </summary>
    public bool IsMilestone
    {
        get { return (bool)GetValue(IsMilestoneProperty); }
        set { SetValue(IsMilestoneProperty, value); }
    }
    public static DependencyProperty IsMilestoneProperty = DependencyProperty.Register(nameof(IsMilestone), typeof(bool), typeof(TimeRangeControl), new PropertyMetadata(false));


    public DateTime StartTime
    {
        get { return (DateTime)GetValue(StartTimeProperty); }
        set { SetValue(StartTimeProperty, value); }
    }
    public static DependencyProperty StartTimeProperty = DependencyProperty.Register(nameof(StartTime), typeof(DateTime), typeof(TimeRangeControl), new FrameworkPropertyMetadata(DateTime.MinValue, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnStartTimeChanged));




    /// <summary> 
    /// end time of task range
    /// </summary>
    public DateTime EndTime
    {
        get { return (DateTime)GetValue(EndTimeProperty); }
        set { SetValue(EndTimeProperty, value); }
    }
    public static DependencyProperty EndTimeProperty = DependencyProperty.Register(nameof(EndTime), typeof(DateTime), typeof(TimeRangeControl), new FrameworkPropertyMetadata(DateTime.MaxValue, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnEndTimeChanged));


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



    /// <summary>
    ///Get or set border thickness at selected. 
    /// </summary>
    public Thickness SelectedBorderThickness
    {
        get { return (Thickness)GetValue(SelectedBorderThicknessProperty); }
        set { SetValue(SelectedBorderThicknessProperty, value); }
    }
    public static DependencyProperty SelectedBorderThicknessProperty = DependencyProperty.Register(nameof(SelectedBorderThickness), typeof(Thickness), typeof(TimeRangeControl), new PropertyMetadata(new Thickness(1.0)));


    /// <summary>
    /// Get or set border brush at selected.
    /// </summary>
    public Brush SelectedBorderBrush
    {
        get { return (Brush)GetValue(SelectedBorderBrushProperty); }
        set { SetValue(SelectedBorderBrushProperty, value); }
    }
    public static DependencyProperty SelectedBorderBrushProperty = DependencyProperty.Register(nameof(SelectedBorderBrush), typeof(Brush), typeof(TimeRangeControl), new PropertyMetadata(Brushes.Red));



    private static void OnPositionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is TimeRangeControl ctrl)
        {
            Canvas.SetLeft(ctrl, ctrl.Left);
            Canvas.SetTop(ctrl, ctrl.Top);
        }
    }

    private static void OnStartTimeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is TimeRangeControl range)
        {
            if (range.IsMilestone && range.EndTime != range.StartTime)
            {
                range.EndTime = range.StartTime;
            }
            //SetLeft(range);
        }
    }
    private static void OnEndTimeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is TimeRangeControl range)
        {

            if (range.IsMilestone && range.EndTime != range.StartTime)
            {
                range.StartTime = range.EndTime;
                return;
            }
            //SetRight(range);
        }
    }


    private static void OnActualSpanChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is TimeRangeControl range)
        {
            var width = range.Right - range.Left;
            //if (width != range.Width && width >= 0 && !range.IsMilestone) range.TaskWidth = width;
        }
    }

    private static void OnWorkingSpanChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is TimeRangeControl range)
        {
            if ((range.IsMilestone == false) && e.OldValue != e.NewValue)
            {
                //SetTaskWidth(range);
            }
        }
    }




    #endregion


    #region Interaction (ドラッグ対応への布石)

    protected override void OnMouseLeftButtonDown(System.Windows.Input.MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonDown(e);
        // ここで ParentChart へのドラッグ開始通知などを行う
        this.CaptureMouse();
        Mouse.OverrideCursor = (_currentMode == HitTestMode.Move) ? Cursors.Hand : Cursors.SizeWE;
    }

    protected override void OnMouseLeftButtonUp(System.Windows.Input.MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonUp(e);
        if (this.IsMouseCaptured) this.ReleaseMouseCapture();
        Mouse.OverrideCursor = null;
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        if (e.LeftButton == MouseButtonState.Pressed) return; // ドラッグ中は判定不要

        var pos = e.GetPosition(this);

        var hitElement = InputHitTest(e.GetPosition(this)) as FrameworkElement;
        if (hitElement == null) return;
        if (hitElement.Name == "LeftHandle" && CanExpandSpan)
        {
            _currentMode = HitTestMode.ResizeLeft;
        }
        else if (hitElement.Name == "RightHandle" && CanExpandSpan)
        {
            _currentMode = HitTestMode.ResizeRight;
        }
        else if (CanDrag)
        {
            // PART_MainBorder や内部の ContentPresenter などの場合
            _currentMode = HitTestMode.Move;
        }
        else
        {
            _currentMode = HitTestMode.None;
        }
    }


    #endregion
}
