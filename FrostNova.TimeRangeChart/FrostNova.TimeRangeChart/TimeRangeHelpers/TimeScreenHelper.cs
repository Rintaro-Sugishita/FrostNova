using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FrostNova.TimeRangeChart.TimeRangeHelpers
{
    public class TimeScreenHelper
    {
        public static double TimeToPoint(DateTime time, DateTime startTime, DateTime endTime, double width)
        {
            var a = (time - startTime).Ticks * width / (endTime - startTime).Ticks;

            return a;
        }

        public static DateTime PointToTime(double point, DateTime startTime, DateTime endTime, double width, TimeSpan minSpan)
        {
            var time = startTime.AddTicks((long)((endTime - startTime).Ticks * point / width));
            var roundTime = TimeHelper.RoundToNearestIncrement(time, minSpan);

            return roundTime;
        }

    }
}
