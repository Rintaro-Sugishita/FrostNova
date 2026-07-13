using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
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

namespace FrostNova.Core.Wpf.Controls
{
    /// <summary>
    /// このカスタム コントロールを XAML ファイルで使用するには、手順 1a または 1b の後、手順 2 に従います。
    ///
    /// 手順 1a) 現在のプロジェクトに存在する XAML ファイルでこのカスタム コントロールを使用する場合
    /// この XmlNamespace 属性を使用場所であるマークアップ ファイルのルート要素に
    /// 追加します:
    ///
    ///     xmlns:MyNamespace="clr-namespace:FrostNova.Core.Wpf.Controls"
    ///
    ///
    /// 手順 1b) 異なるプロジェクトに存在する XAML ファイルでこのカスタム コントロールを使用する場合
    /// この XmlNamespace 属性を使用場所であるマークアップ ファイルのルート要素に
    /// 追加します:
    ///
    ///     xmlns:MyNamespace="clr-namespace:FrostNova.Core.Wpf.Controls;assembly=FrostNova.Core.Wpf.Controls"
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
    ///     <MyNamespace:TimePickerEditBox/>
    ///
    /// </summary>
    public class TimePickerEditBox : Control
    {
        static TimePickerEditBox()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(TimePickerEditBox), new FrameworkPropertyMetadata(typeof(TimePickerEditBox)));
        }

        #region initialize

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            var upButton = GetTemplateChild("PART_upButton") as Button;
            var downButton = GetTemplateChild("PART_downButton") as Button;

            if (upButton != null) upButton.Click += (o, e) => { UpTime(); };
            if (downButton != null) downButton.Click += (o, e) => { DownTime(); };

        }

        #endregion

        #region properties

        /// <summary>
        /// value of datetime or timespan
        /// </summary>
        public object? TimeValue
        {
            get { return (object?)GetValue(TimeValueProperty); }
            set { SetValue(TimeValueProperty, value); }
        }
        public static DependencyProperty TimeValueProperty = DependencyProperty.Register(nameof(TimeValue), typeof(object), typeof(TimePickerEditBox),
            new PropertyMetadata(null, null, CoerceTimeValue));

        private static object CoerceTimeValue(DependencyObject d, object baseValue)
        {
            var control = (TimePickerEditBox)d;
            if (baseValue is TimeSpan ts && control.IsPartsDateTime)
            {
                var existingDate = (control.TimeValue as DateTime?)?.Date ?? DateTime.Today;
                return existingDate.Add(ts);
            }
            return baseValue;
        }


        /// <summary>
        /// type of TimeValue (true: DateTime false:TimeSpan)
        /// </summary>
        public bool IsPartsDateTime
        {
            get { return (bool)GetValue(IsPartsDateTimeProperty); }
            set { SetValue(IsPartsDateTimeProperty, value); }
        }
        public static DependencyProperty IsPartsDateTimeProperty = DependencyProperty.Register(nameof(IsPartsDateTime), typeof(bool), typeof(TimePickerEditBox));


        /// <summary>
        /// hour value
        /// </summary>
        public int? HourValue
        {
            get { return (int?)GetValue(HourValueProperty); }
            set { SetValue(HourValueProperty, value); }
        }
        public static DependencyProperty HourValueProperty = DependencyProperty.Register(nameof(HourValue), typeof(int?), typeof(TimePickerEditBox));

        /// <summary>
        /// minute value
        /// </summary>
        public int? MinuteValue
        {
            get { return (int?)GetValue(MinuteValueProperty); }
            set { SetValue(MinuteValueProperty, value); }
        }
        public static DependencyProperty MinuteValueProperty = DependencyProperty.Register(nameof(MinuteValue), typeof(int?), typeof(TimePickerEditBox));

        /// <summary>
        /// second value
        /// </summary>
        public int? SecondValue
        {
            get { return (int?)GetValue(SecondValueProperty); }
            set { SetValue(SecondValueProperty, value); }
        }
        public static DependencyProperty SecondValueProperty = DependencyProperty.Register(nameof(SecondValue), typeof(int?), typeof(TimePickerEditBox));


        /// <summary>
        /// span of minutes
        /// </summary>
        public int MinuteSpan
        {
            get { return (int)GetValue(MinuteSpanProperty); }
            set { SetValue(MinuteSpanProperty, value); }
        }
        public static DependencyProperty MinuteSpanProperty = DependencyProperty.Register(nameof(MinuteSpan), typeof(int), typeof(TimePickerEditBox), new PropertyMetadata(5));


        /// <summary>
        /// second span
        /// </summary>
        public int SecondSpan
        {
            get { return (int)GetValue(SecondSpanProperty); }
            set { SetValue(SecondSpanProperty, value); }
        }
        public static DependencyProperty SecondSpanProperty = DependencyProperty.Register(nameof(SecondSpan), typeof(int), typeof(TimePickerEditBox), new PropertyMetadata(10));


        /// <summary>
        /// show or hidden list of seconds
        /// </summary>
        public bool IsShowSecond
        {
            get { return (bool)GetValue(IsShowSecondProperty); }
            set { SetValue(IsShowSecondProperty, value); }
        }
        public static DependencyProperty IsShowSecondProperty = DependencyProperty.Register(nameof(IsShowSecond), typeof(bool), typeof(TimePickerEditBox), new PropertyMetadata(false));

        /// <summary>
        /// show or hidden day part
        /// </summary>
        public bool IsShowDay
        {
            get { return (bool)GetValue(IsShowDayProperty); }
            set { SetValue(IsShowDayProperty, value); }
        }
        public static DependencyProperty IsShowDayProperty = DependencyProperty.Register(nameof(IsShowDay), typeof(bool), typeof(TimePickerEditBox), new PropertyMetadata(false));


        /// <summary>
        /// max height of popup
        /// </summary>
        public double MaxPopupHeight
        {
            get { return (double)GetValue(MaxPopupHeightProperty); }
            set { SetValue(MaxPopupHeightProperty, value); }
        }
        public static DependencyProperty MaxPopupHeightProperty = DependencyProperty.Register(nameof(MaxPopupHeight), typeof(double), typeof(TimePickerEditBox), new PropertyMetadata(double.PositiveInfinity));

        /// <summary>
        /// min height of popup
        /// </summary>
        public double MinPopupHeight
        {
            get { return (double)GetValue(MinPopupHeightProperty); }
            set { SetValue(MinPopupHeightProperty, value); }
        }
        public static DependencyProperty MinPopupHeightProperty = DependencyProperty.Register(nameof(MinPopupHeight), typeof(double), typeof(TimePickerEditBox), new PropertyMetadata(0.0D));

        /// <summary>
        /// height of popup
        /// </summary>
        public double PopupHeight
        {
            get { return (double)GetValue(PopupHeightProperty); }
            set { SetValue(PopupHeightProperty, value); }
        }
        public static DependencyProperty PopupHeightProperty = DependencyProperty.Register(nameof(PopupHeight), typeof(double), typeof(TimePickerEditBox), new PropertyMetadata(double.NaN));

        /// <summary>
        /// max width of popup
        /// </summary>
        public double MaxPopupWidth
        {
            get { return (double)GetValue(MaxPopupWidthProperty); }
            set { SetValue(MaxPopupWidthProperty, value); }
        }
        public static DependencyProperty MaxPopupWidthProperty = DependencyProperty.Register(nameof(MaxPopupWidth), typeof(double), typeof(TimePickerEditBox), new PropertyMetadata(double.PositiveInfinity));

        /// <summary>
        /// min width of popup
        /// </summary>
        public double MinPopupWidth
        {
            get { return (double)GetValue(MinPopupWidthProperty); }
            set { SetValue(MinPopupWidthProperty, value); }
        }
        public static DependencyProperty MinPopupWidthProperty = DependencyProperty.Register(nameof(MinPopupWidth), typeof(double), typeof(TimePickerEditBox), new PropertyMetadata(0.0D));

        /// <summary>
        /// width of popup
        /// </summary>
        public double PopupWidth
        {
            get { return (double)GetValue(PopupWidthProperty); }
            set { SetValue(PopupWidthProperty, value); }
        }
        public static DependencyProperty PopupWidthProperty = DependencyProperty.Register(nameof(PopupWidth), typeof(double), typeof(TimePickerEditBox), new PropertyMetadata(double.NaN));

        /// <summary>
        /// PlacementTarget
        /// </summary>
        public UIElement PlacementTarget
        {
            get { return (UIElement)GetValue(PlacementTargetProperty); }
            set { SetValue(PlacementTargetProperty, value); }
        }
        public static DependencyProperty PlacementTargetProperty = DependencyProperty.Register(nameof(PlacementTarget), typeof(UIElement), typeof(TimePickerEditBox));

        /// <summary>
        /// Placement
        /// </summary>
        public PlacementMode Placement
        {
            get { return (PlacementMode)GetValue(PlacementProperty); }
            set { SetValue(PlacementProperty, value); }
        }
        public static DependencyProperty PlacementProperty = DependencyProperty.Register(nameof(Placement), typeof(PlacementMode), typeof(TimePickerEditBox), new PropertyMetadata(PlacementMode.Bottom));

        /// <summary>
        /// 
        /// </summary>
        public Visibility UpDownButtonVisibility
        {
            get { return (Visibility)GetValue(UpDownButtonVisibilityProperty); }
            set { SetValue(UpDownButtonVisibilityProperty, value); }
        }
        public static DependencyProperty UpDownButtonVisibilityProperty = DependencyProperty.Register(nameof(UpDownButtonVisibility), typeof(Visibility), typeof(TimePickerEditBox), new PropertyMetadata(Visibility.Collapsed));

        /// <summary>
        /// 
        /// </summary>
        public Visibility TimePickerButtonVisibility
        {
            get { return (Visibility)GetValue(TimePickerButtonVisibilityProperty); }
            set { SetValue(TimePickerButtonVisibilityProperty, value); }
        }
        public static DependencyProperty TimePickerButtonVisibilityProperty = DependencyProperty.Register(nameof(TimePickerButtonVisibility), typeof(Visibility), typeof(TimePickerEditBox), new PropertyMetadata(Visibility.Visible));

        /// <summary>
        /// max value
        /// </summary>
        public TimeSpan? MaxValue
        {
            get { return (TimeSpan?)GetValue(MaxValueProperty); }
            set { SetValue(MaxValueProperty, value); }
        }
        public static DependencyProperty MaxValueProperty = DependencyProperty.Register(nameof(MaxValue), typeof(TimeSpan?), typeof(TimePickerEditBox), new PropertyMetadata(null));

        /// <summary>
        /// min value
        /// </summary>
        public TimeSpan? MinValue
        {
            get { return (TimeSpan?)GetValue(MinValueProperty); }
            set { SetValue(MinValueProperty, value); }
        }
        public static DependencyProperty MinValueProperty = DependencyProperty.Register(nameof(MinValue), typeof(TimeSpan?), typeof(TimePickerEditBox), new PropertyMetadata(null));



        #endregion

        #region click events

        // Shift 押下時は MinuteSpan に依らず一律 1 分刻みにする（細かい値の調整用）。
        private int GetStepMinutes()
            => (System.Windows.Input.Keyboard.Modifiers & System.Windows.Input.ModifierKeys.Shift) == System.Windows.Input.ModifierKeys.Shift
                ? 1 : MinuteSpan;

        private void UpTime()
        {
            int span = GetStepMinutes();
            if (TimeValue is DateTime dtValue)
            {
                var newTime = dtValue.TimeOfDay + TimeSpan.FromMinutes(span);
                TimeValue = MaxValue.HasValue && MaxValue.Value < newTime
                    ? dtValue.Date.Add(MaxValue.Value)
                    : dtValue.Date.Add(newTime);
            }
            else
            {
                TimeSpan newValue = (TimeValue as TimeSpan?) ?? TimeSpan.Zero;
                newValue += TimeSpan.FromMinutes(span);
                TimeValue = MaxValue.HasValue && MaxValue.Value < newValue ? MaxValue.Value : newValue;
            }
        }

        private void DownTime()
        {
            int span = GetStepMinutes();
            if (TimeValue is DateTime dtValue)
            {
                var newTime = dtValue.TimeOfDay - TimeSpan.FromMinutes(span);
                TimeValue = MinValue.HasValue && newTime < MinValue.Value
                    ? dtValue.Date.Add(MinValue.Value)
                    : dtValue.Date.Add(newTime);
            }
            else
            {
                TimeSpan newValue = (TimeValue as TimeSpan?) ?? TimeSpan.Zero;
                newValue -= TimeSpan.FromMinutes(span);
                TimeValue = MinValue.HasValue && newValue < MinValue.Value ? MinValue.Value : newValue;
            }
        }


        #endregion
    }
}
