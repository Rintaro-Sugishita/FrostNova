using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
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
    ///     <MyNamespace:TimePicker/>
    ///
    /// </summary>
    public class TimePicker : Control
    {
        static TimePicker()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(TimePicker), new FrameworkPropertyMetadata(typeof(TimePicker)));
        }

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
        public int HourValue
        {
            get { return (int)GetValue(HourValueProperty); }
            set { SetValue(HourValueProperty, value); }
        }
        public static DependencyProperty HourValueProperty = DependencyProperty.Register(nameof(HourValue), typeof(int), typeof(TimePicker));

        /// <summary>
        /// minute value
        /// </summary>
        public int MinuteValue
        {
            get { return (int)GetValue(MinuteValueProperty); }
            set { SetValue(MinuteValueProperty, value); }
        }
        public static DependencyProperty MinuteValueProperty = DependencyProperty.Register(nameof(MinuteValue), typeof(int), typeof(TimePicker));




        /// <summary>
        /// list of hours
        /// </summary>
        public IEnumerable<int> Hours
        {
            get { return (IEnumerable<int>)GetValue(HoursProperty); }
            set { SetValue(HoursProperty, value); }
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
            set { SetValue(MinutesProperty, value); }
        }
        public static DependencyProperty MinutesProperty = DependencyProperty.Register(nameof(Minutes), typeof(IEnumerable<int>), typeof(TimePicker));

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

        private static void SetHourList(TimePicker picker)
        {
            picker.Hours = Enumerable.Range(0, 23);
        }

        private static void SetValue(TimePicker picker)
        {
            var time = new TimeSpan(picker.HourValue, picker.MinuteValue, 0);

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
            }
            else if (picker.TimeValue is TimeSpan spanValue)
            {
                picker.HourValue = spanValue.Hours;
                picker.MinuteValue = spanValue.Minutes;
            }
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            SetHourList(this);
            SetMinuteList(this, this.MinuteSpan);


            var toggleButton = GetTemplateChild("PART_BUTTON") as ToggleButton;
            if (toggleButton != null)
            {
                toggleButton.Unchecked += (sender, e) =>
                {
                    SetValue(this);
                };
                toggleButton.Checked += (sender, e) =>
                {
                    SetHourAndMinute(this);
                };
            }
        }


    }
}
