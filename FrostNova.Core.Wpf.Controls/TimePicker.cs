using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
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

namespace FrostNova.Core.Wpf.Controls
{
    /// <summary>
    /// Time-of-day picker control. Bind <see cref="TimeValue"/> to a <see cref="DateTime"/> or
    /// <see cref="TimeSpan"/> and set <see cref="IsPartsDateTime"/> accordingly.
    /// Hour and minute parts are also individually accessible via <see cref="HourValue"/> and
    /// <see cref="MinuteValue"/>.
    /// </summary>
    public class TimePicker : Control
    {
        static TimePicker()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(TimePicker), new FrameworkPropertyMetadata(typeof(TimePicker)));
        }

        #region properties

        /// <summary>
        /// value of datetime or timespan
        /// </summary>
        public object? TimeValue
        {
            get { return (object?)GetValue(TimeValueProperty); }
            set { SetValue(TimeValueProperty, value); }
        }
        public static DependencyProperty TimeValueProperty = DependencyProperty.Register(nameof(TimeValue), typeof(object), typeof(TimePicker));


        /// <summary>
        /// type of TimeValue (true: DateTime false:TimeSpan)
        /// </summary>
        public bool IsPartsDateTime
        {
            get { return (bool)GetValue(IsPartsDateTimeProperty); }
            set { SetValue(IsPartsDateTimeProperty, value); }
        }
        public static DependencyProperty IsPartsDateTimeProperty = DependencyProperty.Register(nameof(IsPartsDateTime), typeof(bool), typeof(TimePicker));


        /// <summary>
        /// hour value
        /// </summary>
        public int? HourValue
        {
            get { return (int?)GetValue(HourValueProperty); }
            set { SetValue(HourValueProperty, value); }
        }
        public static DependencyProperty HourValueProperty = DependencyProperty.Register(nameof(HourValue), typeof(int?), typeof(TimePicker));

        /// <summary>
        /// minute value
        /// </summary>
        public int? MinuteValue
        {
            get { return (int?)GetValue(MinuteValueProperty); }
            set { SetValue(MinuteValueProperty, value); }
        }
        public static DependencyProperty MinuteValueProperty = DependencyProperty.Register(nameof(MinuteValue), typeof(int?), typeof(TimePicker));

        /// <summary>
        /// second value
        /// </summary>
        public int? SecondValue
        {
            get { return (int?)GetValue(SecondValueProperty); }
            set { SetValue(SecondValueProperty, value); }
        }
        public static DependencyProperty SecondValueProperty = DependencyProperty.Register(nameof(SecondValue), typeof(int?), typeof(TimePicker));




        /// <summary>
        /// list of hours
        /// </summary>
        public IEnumerable<int> Hours
        {
            get { return (IEnumerable<int>)GetValue(HoursProperty); }
            private set { SetValue(HoursProperty, value); }
        }
        public static DependencyProperty HoursProperty = DependencyProperty.Register(nameof(Hours), typeof(IEnumerable<int>), typeof(TimePicker));


        /// <summary>
        /// span of minutes
        /// </summary>
        public int MinuteSpan
        {
            get { return (int)GetValue(MinuteSpanProperty); }
            set { SetValue(MinuteSpanProperty, value); }
        }
        public static DependencyProperty MinuteSpanProperty = DependencyProperty.Register(nameof(MinuteSpan), typeof(int), typeof(TimePicker), new PropertyMetadata(5, OnMinuteSpanChanged));


        /// <summary>
        /// list of Minutes
        /// </summary>
        public IEnumerable<int> Minutes
        {
            get { return (IEnumerable<int>)GetValue(MinutesProperty); }
            private set { SetValue(MinutesProperty, value); }
        }
        public static DependencyProperty MinutesProperty = DependencyProperty.Register(nameof(Minutes), typeof(IEnumerable<int>), typeof(TimePicker));

        /// <summary>
        /// second span
        /// </summary>
        public int SecondSpan
        {
            get { return (int)GetValue(SecondSpanProperty); }
            set { SetValue(SecondSpanProperty, value); }
        }
        public static DependencyProperty SecondSpanProperty = DependencyProperty.Register(nameof(SecondSpan), typeof(int), typeof(TimePicker), new PropertyMetadata(10, OnSecondSpanChanged));


        /// <summary>
        /// list of seconds
        /// </summary>
        public IEnumerable<int> Seconds
        {
            get { return (IEnumerable<int>)GetValue(SecondsProperty); }
           private set { SetValue(SecondsProperty, value); }
        }
        public static DependencyProperty SecondsProperty = DependencyProperty.Register(nameof(Seconds), typeof(IEnumerable<int>), typeof(TimePicker));

        /// <summary>
        /// show or hidden list of seconds
        /// </summary>
        public bool IsShowSecond
        {
            get { return (bool)GetValue(IsShowSecondProperty); }
            set { SetValue(IsShowSecondProperty, value); }
        }
        public static DependencyProperty IsShowSecondProperty = DependencyProperty.Register(nameof(IsShowSecond), typeof(bool), typeof(TimePicker), new PropertyMetadata(false));


        /// <summary>
        /// max height of popup
        /// </summary>
        public double MaxPopupHeight
        {
            get { return (double)GetValue(MaxPopupHeightProperty); }
            set { SetValue(MaxPopupHeightProperty, value); }
        }
        public static DependencyProperty MaxPopupHeightProperty = DependencyProperty.Register(nameof(MaxPopupHeight), typeof(double), typeof(TimePicker), new PropertyMetadata(double.PositiveInfinity));

        /// <summary>
        /// min height of popup
        /// </summary>
        public double MinPopupHeight
        {
            get { return (double)GetValue(MinPopupHeightProperty); }
            set { SetValue(MinPopupHeightProperty, value); }
        }
        public static DependencyProperty MinPopupHeightProperty = DependencyProperty.Register(nameof(MinPopupHeight), typeof(double), typeof(TimePicker), new PropertyMetadata(0.0D));

        /// <summary>
        /// height of popup
        /// </summary>
        public double PopupHeight
        {
            get { return (double)GetValue(PopupHeightProperty); }
            set { SetValue(PopupHeightProperty, value); }
        }
        public static DependencyProperty PopupHeightProperty = DependencyProperty.Register(nameof(PopupHeight), typeof(double), typeof(TimePicker), new PropertyMetadata(double.NaN));

        /// <summary>
        /// max width of popup
        /// </summary>
        public double MaxPopupWidth
        {
            get { return (double)GetValue(MaxPopupWidthProperty); }
            set { SetValue(MaxPopupWidthProperty, value); }
        }
        public static DependencyProperty MaxPopupWidthProperty = DependencyProperty.Register(nameof(MaxPopupWidth), typeof(double), typeof(TimePicker), new PropertyMetadata(double.PositiveInfinity));

        /// <summary>
        /// min width of popup
        /// </summary>
        public double MinPopupWidth
        {
            get { return (double)GetValue(MinPopupWidthProperty); }
            set { SetValue(MinPopupWidthProperty, value); }
        }
        public static DependencyProperty MinPopupWidthProperty = DependencyProperty.Register(nameof(MinPopupWidth), typeof(double), typeof(TimePicker), new PropertyMetadata(0.0D));

        /// <summary>
        /// width of popup
        /// </summary>
        public double PopupWidth
        {
            get { return (double)GetValue(PopupWidthProperty); }
            set { SetValue(PopupWidthProperty, value); }
        }
        public static DependencyProperty PopupWidthProperty = DependencyProperty.Register(nameof(PopupWidth), typeof(double), typeof(TimePicker), new PropertyMetadata(double.NaN));

        /// <summary>
        /// PlacementTarget
        /// </summary>
        public UIElement PlacementTarget
        {
            get { return (UIElement)GetValue(PlacementTargetProperty); }
            set { SetValue(PlacementTargetProperty, value); }
        }
        public static DependencyProperty PlacementTargetProperty = DependencyProperty.Register(nameof(PlacementTarget), typeof(UIElement), typeof(TimePicker));

        /// <summary>
        /// string
        /// </summary>
        public PlacementMode Placement
        {
            get { return (PlacementMode)GetValue(PlacementProperty); }
            set { SetValue(PlacementProperty, value); }
        }
        public static DependencyProperty PlacementProperty = DependencyProperty.Register(nameof(Placement), typeof(PlacementMode), typeof(TimePicker), new PropertyMetadata(PlacementMode.Bottom));


        #endregion

        #region property changed actions


        private static void OnMinuteSpanChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is TimePicker timePicker)
            {
                var format = FormatMinuteSpan(
                timePicker.MinuteSpan);
                if (format != timePicker.MinuteSpan)
                {
                    timePicker.MinuteSpan = format;
                    return;
                }
                SetMinuteList(timePicker, timePicker.MinuteSpan);
            }

        }

        private static void OnSecondSpanChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is TimePicker timePicker)
            {
                var format = FormatMinuteSpan(
                timePicker.SecondSpan);
                if (format != timePicker.SecondSpan)
                {
                    timePicker.SecondSpan = format;
                    return;
                }
                SetSecondList(timePicker, timePicker.MinuteSpan);
            }
        }

        #endregion

        private static int FormatMinuteSpan(int span)
        {
            if (span < 0)
            {
                return 0;
            }

            if (span > 60)
            {
                return 0;
            }


            //60の倍数でない場合はそろえる
            /*
             2, 2 ,3, 5
             */
            int[] ALLOW_LIST = [0, 1, 2, 3, 4, 5, 6, 10, 12, 15, 20, 30];
            if (ALLOW_LIST.Contains(span))
            {
                return span;
            }

            for (int i = 0; i < ALLOW_LIST.Length - 1; i++)
            {
                int left = ALLOW_LIST[i];
                int right = ALLOW_LIST[i + 1];
                if (left == span)
                {
                    return span;
                }
                else if (left < span && span < right)
                {
                    return (left - span) > (span - right) ? right : left;
                }

            }

            return span;
        }

        private static void SetMinuteList(TimePicker picker, int span)
        {
            const int MAX = 60;
            int currentMinute = 0;
            List<int> list = new();
            while (currentMinute < MAX)
            {
                list.Add(currentMinute);
                currentMinute += span;
            }
            picker.Minutes = list;
        }
        
        private static void SetSecondList(TimePicker picker, int span)
        {
            const int MAX = 60;
            int currentMinute = 0;
            List<int> list = new();
            while (currentMinute < MAX)
            {
                list.Add(currentMinute);
                currentMinute += span;
            }
            picker.Seconds = list;
        }

        private static void SetHourList(TimePicker picker)
        {
            picker.Hours = Enumerable.Range(0, 24);
        }

        private static void SetValue(TimePicker picker)
        {
            if (picker.HourValue.HasValue == false || picker.MinuteValue.HasValue == false)
            {
                return;
            }
            if (picker.IsShowSecond && picker.SecondValue.HasValue == false)
            {
                return;
            }

            var time = new TimeSpan(picker.HourValue.Value, picker.MinuteValue.Value, picker.IsShowSecond ? picker.SecondValue!.Value : 0);

            if (picker.TimeValue is DateTime dateValue)
            {
                picker.TimeValue = dateValue.Date.Add(time);
            }
            else if (picker.TimeValue is TimeSpan)
            {
                picker.TimeValue = time;
            }
            else
            {
                if (picker.IsPartsDateTime)
                {
                    picker.TimeValue = new DateTime().Add(time);
                }
                else
                {
                    picker.TimeValue = time;
                }

            }
        }

        private static void SetHourAndMinute(TimePicker picker)
        {
            if (picker.TimeValue is DateTime dateValue)
            {
                picker.HourValue = dateValue.Hour;
                picker.MinuteValue = dateValue.Minute;

                if (picker.IsShowSecond)
                {
                    picker.SecondValue = dateValue.Second;
                }

            }
            else if (picker.TimeValue is TimeSpan spanValue)
            {
                picker.HourValue = spanValue.Hours;
                picker.MinuteValue = spanValue.Minutes;
                if (picker.IsShowSecond)
                {
                    picker.SecondValue = spanValue.Seconds;
                }
            }
            else
            {
                picker.HourValue = null;
                picker.MinuteValue = null;
                picker.SecondValue = null;
            }
        }

        private object? _timeValueBackup;
        private bool _confirmClicked;

        private readonly TextBlock?[] _hourRows   = new TextBlock?[3];
        private readonly TextBlock?[] _minuteRows = new TextBlock?[3];
        private readonly TextBlock?[] _secondRows = new TextBlock?[3];

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            SetHourList(this);
            SetMinuteList(this, MinuteSpan);
            SetSecondList(this, SecondSpan);

            var toggleButton = GetTemplateChild("PART_BUTTON") as ToggleButton;
            var okButton     = GetTemplateChild("PART_ok")     as ButtonBase;
            var cancelButton = GetTemplateChild("PART_cancel") as ButtonBase;

            for (int i = 0; i < 3; i++)
            {
                _hourRows[i]   = GetTemplateChild($"PART_hour_r{i}")   as TextBlock;
                _minuteRows[i] = GetTemplateChild($"PART_minute_r{i}") as TextBlock;
                _secondRows[i] = GetTemplateChild($"PART_second_r{i}") as TextBlock;
            }

            var hourContainer   = GetTemplateChild("PART_hour_container")   as UIElement;
            var minuteContainer = GetTemplateChild("PART_minute_container") as UIElement;
            var secondContainer = GetTemplateChild("PART_second_container") as UIElement;

            WireDrumColumn(HourValueProperty,   _hourRows,   hourContainer,   "PART_hour_up",   "PART_hour_down");
            WireDrumColumn(MinuteValueProperty, _minuteRows, minuteContainer, "PART_minute_up", "PART_minute_down");
            WireDrumColumn(SecondValueProperty, _secondRows, secondContainer, "PART_second_up", "PART_second_down");

            if (toggleButton != null)
            {
                toggleButton.Checked += (sender, e) =>
                {
                    _timeValueBackup = TimeValue;
                    _confirmClicked = false;
                    SetHourAndMinute(this);
                    RefreshAllDrums();
                };
                toggleButton.Unchecked += (sender, e) =>
                {
                    if (_confirmClicked)
                        SetValue(this);
                    else
                        TimeValue = _timeValueBackup;
                    _confirmClicked = false;
                };
            }

            if (okButton != null)
                okButton.Click += (s, e) => { _confirmClicked = true; if (toggleButton != null) toggleButton.IsChecked = false; };
            if (cancelButton != null)
                cancelButton.Click += (s, e) => { if (toggleButton != null) toggleButton.IsChecked = false; };
        }

        private void RefreshAllDrums()
        {
            UpdateDrumDisplay(HourValueProperty,   _hourRows);
            UpdateDrumDisplay(MinuteValueProperty, _minuteRows);
            UpdateDrumDisplay(SecondValueProperty, _secondRows);
        }

        private void WireDrumColumn(DependencyProperty valueProp, TextBlock?[] rows, UIElement? container, string upPartName, string downPartName)
        {
            var up   = GetTemplateChild(upPartName)   as ButtonBase;
            var down = GetTemplateChild(downPartName) as ButtonBase;

            if (up   != null) up.Click   += (s, e) => StepDrum(valueProp, rows, -1);
            if (down != null) down.Click += (s, e) => StepDrum(valueProp, rows,  1);

            if (container != null)
            {
                container.PreviewMouseWheel += (s, e) =>
                {
                    StepDrum(valueProp, rows, e.Delta > 0 ? -1 : 1);
                    e.Handled = true;
                };
            }

            // 上下の行をクリックするとその方向に1ステップ進む
            for (int i = 0; i < 3; i++)
            {
                if (i == 1) continue;   // 中央はスキップ
                int offset = i - 1;     // 0→-1, 2→+1
                var row = rows[i];
                if (row != null)
                    row.MouseLeftButtonUp += (s, e) => StepDrum(valueProp, rows, offset);
            }
        }

        private IList<int> GetItems(DependencyProperty valueProp)
        {
            IEnumerable<int>? src =
                valueProp == HourValueProperty   ? Hours   :
                valueProp == MinuteValueProperty ? Minutes :
                valueProp == SecondValueProperty ? Seconds : null;
            return src?.ToList() ?? new List<int>();
        }

        private void StepDrum(DependencyProperty valueProp, TextBlock?[] rows, int delta)
        {
            var items = GetItems(valueProp);
            if (items.Count == 0) return;
            int currentVal = (GetValue(valueProp) as int?) ?? 0;
            int idx = items.IndexOf(currentVal);
            if (idx < 0) idx = 0;
            int nextIdx = (idx + delta + items.Count) % items.Count;
            SetValue(valueProp, items[nextIdx]);
            RefreshDrumDisplay(items, nextIdx, rows);
        }

        private void UpdateDrumDisplay(DependencyProperty valueProp, TextBlock?[] rows)
        {
            var items = GetItems(valueProp);
            int currentVal = (GetValue(valueProp) as int?) ?? 0;
            int idx = items.IndexOf(currentVal);
            if (idx < 0) idx = 0;
            RefreshDrumDisplay(items, idx, rows);
        }

        private static void RefreshDrumDisplay(IList<int> items, int centerIdx, TextBlock?[] rows)
        {
            int count = items.Count;
            if (count == 0) return;
            for (int i = 0; i < 3; i++)
            {
                int offset = i - 1;  // -1, 0, +1
                int idx = ((centerIdx + offset) % count + count) % count;
                if (rows[i] != null) rows[i]!.Text = items[idx].ToString("D2");
            }
        }


    }
}
