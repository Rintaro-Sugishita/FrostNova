using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FrostNova.TimeRangeChart.TimeRangeHelpers
{
    /// <summary>
    /// Helper functions of TimeRange.
    /// </summary>
    public static class TimeRangeHelper
    {

        public static DateTime CalculateEndTime(ITimeRange range, IEnumerable<ITimeRange>? nonWorkingTimes = null)
        {

            var span = range.WorkingSpan.HasValue ? range.WorkingSpan.Value : range.ActualSpan;
            if (!range.IgnoreNonWorkingTimes)
            {
                return TimeHelper.CalcEndTime(range.StartTime, span, nonWorkingTimes);
            }
            else
            {
                return range.StartTime + span;
            }
        }
        public static TimeSpan CalculateWorkingSpan(ITimeRange range, IEnumerable<ITimeRange>? nonWorkingTimes = null)
        {
            if (!range.IgnoreNonWorkingTimes)
            {
                return TimeHelper.CalcWorkingSpan(range.StartTime, range.EndTime, nonWorkingTimes);
            }
            else
            {
                return range.EndTime - range.StartTime;
            }
        }

        public static void Move(ITimeRange range, TimeSpan span, IEnumerable<ITimeRange>? nonWorkingTimes = null)
        {
            range.StartTime += span;
            range.EndTime = CalculateEndTime(range, nonWorkingTimes);

            SetActualSpan(range, nonWorkingTimes);

        }

        public static void MoveTo(ITimeRange range, DateTime newStartTime, IEnumerable<ITimeRange>? nonWorkingTimes = null)
        {
            var delta = newStartTime - range.StartTime;
            Move(range, delta, nonWorkingTimes);
        }

        public static void SetActualSpan(this ITimeRange range, IEnumerable<ITimeRange>? nonWorkingTimes = null)
        {
          var actualSpan =  range.EndTime - range.StartTime;
            //if (actualSpan != range.ActualSpan)
            range.ActualSpan = actualSpan;

        }

        public static void ExpandStartTime(this ITimeRange range, DateTime newStartTime, IEnumerable<ITimeRange>? nonWorkingTimes = null)
        {
            var oldStartTime = range.StartTime;
            var oldEndTime = range.EndTime;

            range.StartTime = newStartTime;

            //実働時間を変更する
            range.WorkingSpan = TimeRangeHelper.CalculateWorkingSpan(range,nonWorkingTimes);
            //差分時間も変更する
            TimeRangeHelper.SetActualSpan(range, nonWorkingTimes);
        }

        public static void ExpandEndTime(this ITimeRange range, DateTime newEndTime, IEnumerable<ITimeRange>? nonWorkingTimes = null)
        {
            var oldStartTime = range.StartTime;
            var oldEndTime = range.EndTime;

            if (range.EndTime != newEndTime) range.EndTime = newEndTime;

            //実働時間を変更する
            var workingSpan = TimeRangeHelper.CalculateWorkingSpan(range, nonWorkingTimes);
            if (workingSpan != range.WorkingSpan) range.WorkingSpan = workingSpan;
            //差分時間も変更する
            TimeRangeHelper.SetActualSpan(range, nonWorkingTimes);
        }
        public static void ExpandSpan(this ITimeRange range, TimeSpan newSpan, IEnumerable<ITimeRange>? nonWorkingTimes = null)
        {
            var oldStartTime = range.StartTime;
            var oldEndTime = range.EndTime;


            range.WorkingSpan = newSpan;
            var newEndTime = TimeRangeHelper.CalculateEndTime(range, nonWorkingTimes);
            range.EndTime = newEndTime;
        }


        public static bool Contains(this ITimeRange range, DateTime time) => range.StartTime <= time && time <= range.EndTime;

        public static bool IsOverlap(this ITimeRange range, ITimeRange target)
        {
            return !(target.EndTime <= range.StartTime || range.EndTime <= target.StartTime);
        }



    }
}
