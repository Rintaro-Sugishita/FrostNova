using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace FrostNova.Core.Wpf.Converters
{

    /// <summary>
    /// Converts a <see cref="TimeSpan"/> value and display options into a formatted text representation for use in data
    /// binding scenarios.
    /// </summary>
    /// <remarks><para> <b>TimeSpanToTextConverter</b> is an <see cref="IMultiValueConverter"/> implementation
    /// designed for WPF or similar XAML-based frameworks. It formats a <see cref="TimeSpan"/> value as a string, with
    /// options to include or omit seconds and days in the output. This converter is typically used in data binding to
    /// present time intervals in a user-friendly textual format. </para> <para> The <c>Convert</c> method expects an
    /// array of values where the first element is a <see cref="TimeSpan"/>, the second is a <see cref="bool"/>
    /// indicating whether to show seconds, and the third is a <see cref="bool"/> indicating whether to show days. The
    /// output string format adapts based on these options. </para> <para> The <c>ConvertBack</c> method attempts to
    /// parse a string back into a <see cref="TimeSpan"/>. If parsing fails, it returns <see cref="Binding.DoNothing"/>
    /// for all target values. </para></remarks>
    public class TimeSpanToTextConverter : IMultiValueConverter
    {

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            //秒を表示するかどうかと、日付を表示するかどうか
            TimeSpan time = TimeSpan.Zero;
            bool isShowSecond = false;
            bool isShowDay = false;


            if (values.Length > 0 && values[0] is TimeSpan ts)
            {
                time = ts;
            }
            else if (values.Length > 0 && values[0] is DateTime dt)
            {
                time = dt.TimeOfDay;
            }

            if (values.Length > 1 && values[1] is bool)
            {
                isShowSecond = (bool)values[1];
            }

            if (values.Length > 2 && values[2] is bool)
            {
                isShowDay = (bool)values[2];
            }

            string text = Convert(time, isShowSecond, isShowDay);
            return text;

        }

        public static string Convert(TimeSpan time, bool isShowSecond, bool isShowDay)
        {

            string text = "";

            int absDays =Math.Abs( time.Days);
            int absHours = Math.Abs(time.Hours);
            int absMinutes = Math.Abs(time.Minutes);
            int absSeconds = Math.Abs(time.Seconds);


            if (isShowDay == false && isShowSecond)
            {
                text = $"{(absDays * 24 + absHours).ToString().PadLeft(2, '0')}:{absMinutes.ToString().PadLeft(2, '0')}:{absSeconds.ToString().PadLeft(2, '0')}";
            }
            else if (isShowDay == false && isShowSecond == false)
            {
                text = $"{(absDays * 24 + absHours).ToString().PadLeft(2, '0')}:{absMinutes.ToString().PadLeft(2, '0')}";
            }
            else if (isShowDay && isShowSecond)
            {
                if (absDays > 0)
                {
                    text = $"{(absDays)}.{(absHours).ToString().PadLeft(2, '0')}:{absMinutes.ToString().PadLeft(2, '0')}:{absSeconds.ToString().PadLeft(2, '0')}";
                }
                else
                {
                    text = $"{(absHours).ToString().PadLeft(2, '0')}:{absMinutes.ToString().PadLeft(2, '0')}:{absSeconds.ToString().PadLeft(2, '0')}";
                }
            }
            else if (isShowDay && isShowSecond == false)
            {
                if (absDays > 0)
                {
                    text = $"{(absDays)}.{(absHours).ToString().PadLeft(2, '0')}:{absMinutes.ToString().PadLeft(2, '0')}";
                }
                else
                {
                    text = $"{(absHours).ToString().PadLeft(2, '0')}:{absMinutes.ToString().PadLeft(2, '0')}";
                }
            }


            if (time < TimeSpan.Zero)
            {
                text = $"-{text}";
            }

            return text;

        }



        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            if (value is not string text) { return [Binding.DoNothing, Binding.DoNothing, Binding.DoNothing]; }

            if (TimeSpan.TryParse(text, out var time)) 
            {
                return [time, Binding.DoNothing, Binding.DoNothing];
            }
            return [Binding.DoNothing, Binding.DoNothing, Binding.DoNothing];
        }
    }
}
