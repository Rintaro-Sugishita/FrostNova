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

        public static double TimeToPointMonth(DateTime time, DateTime startTime, DateTime endTime, double width, IList<IRuledLine> ruledLines)
        {
            if (time < startTime)
            {
                //計算不可
                return 0;
            }
            if (endTime < time)
            {
                //計算不可
                return width;
            }


            var firstPoint = ruledLines.FirstOrDefault(x => x.DateValue <= time);
            var firstPointIndex = -1;
            if (firstPoint != null)
            {
                firstPointIndex = ruledLines.IndexOf(firstPoint);
            }
            var nextPoint = firstPointIndex + 1 < ruledLines.Count ? ruledLines[firstPointIndex + 1] : null;

            //point内では均等
            double innerPoint = double.NaN;

            if (firstPoint != null && nextPoint != null)
            {
                innerPoint = TimeToPoint(time, firstPoint.DateValue, nextPoint.DateValue, nextPoint.LeftPoint - firstPoint.LeftPoint);
                return innerPoint + firstPoint.LeftPoint;
            }
            else if (firstPoint != null)
            {
                innerPoint = TimeToPoint(time, firstPoint.DateValue, endTime, width - firstPoint.LeftPoint);
                return innerPoint + firstPoint.LeftPoint;
            }
            return 0;
        }



        public static DateTime PointToTime(double point, DateTime startTime, DateTime endTime, double width, TimeSpan minSpan)
        {
            var time = startTime.AddTicks((long)((endTime - startTime).Ticks * point / width));
            var roundTime = TimeHelper.RoundToNearestIncrement(time, minSpan);

            return roundTime;
        }

        public static DateTime PointToTimeMonth(double point, DateTime startTime, DateTime endTime, double width, TimeSpan minSpan, IList<IRuledLine> ruledLines)
        {
            if (point < 0)
            {
                return startTime;
            }
            if (width < point)
            {
                return endTime;
            }


            var firstPoint = ruledLines.FirstOrDefault(x => x.LeftPoint <= point);
            var firstPointIndex = -1;
            if (firstPoint != null)
            {
                firstPointIndex = ruledLines.IndexOf(firstPoint);
            }
            var nextPoint = firstPointIndex + 1 < ruledLines.Count ? ruledLines[firstPointIndex + 1] : null;

            //point内では均等
            DateTime? innerDate = null;
            if (firstPoint != null && nextPoint != null)
            {
                innerDate = PointToTime(point, firstPoint.DateValue, nextPoint.DateValue, nextPoint.LeftPoint - firstPoint.LeftPoint, minSpan);
                return innerDate.Value;
            }
            else if (firstPoint != null)
            {
                innerDate = PointToTime(point, firstPoint.DateValue, endTime, width - firstPoint.LeftPoint, minSpan);
                return innerDate.Value;
            }
            return startTime;
        }
    }
}
