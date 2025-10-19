using FrostNova.Core.Common.Extensions;
using System;
using System.Collections.Generic;
using System.Drawing;
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

        //public static double TimeToPointMonth(DateTime time, DateTime startTime, DateTime endTime, double width, IList<IRuledLine> ruledLines)
        //{
        //    if (time < startTime)
        //    {
        //        //計算不可
        //        return 0;
        //    }
        //    if (endTime < time)
        //    {
        //        //計算不可
        //        return width;
        //    }

        //    if (ruledLines?.Count > 0)
        //    {
        //        var firstPoint = ruledLines.FirstOrDefault(x => x.DateValue <= time);
        //        var firstPointIndex = -1;
        //        if (firstPoint != null)
        //        {
        //            firstPointIndex = ruledLines.IndexOf(firstPoint);
        //        }
        //        var nextPoint = firstPointIndex + 1 < ruledLines.Count ? ruledLines[firstPointIndex + 1] : null;

        //        //point内では均等
        //        double innerPoint = double.NaN;

        //        if (firstPoint != null && nextPoint != null)
        //        {
        //            innerPoint = TimeToPoint(time, firstPoint.DateValue, nextPoint.DateValue, nextPoint.LeftPoint - firstPoint.LeftPoint);
        //            return innerPoint + firstPoint.LeftPoint;
        //        }
        //        else if (firstPoint != null)
        //        {
        //            innerPoint = TimeToPoint(time, firstPoint.DateValue, endTime, width - firstPoint.LeftPoint);
        //            return innerPoint + firstPoint.LeftPoint;
        //        }
        //    }

        //    return 0;
        //}

        public static double TimeToPointMonth(DateTime time, double widthPerRuledLine, List<DateTime> ruledLines)
        {
            //var c = DateTimeHelper.GetMonthDifference(endTime, startTime);
            ////渡された日付が何か月目か？を求める
            //var i = DateTimeHelper.GetMonthDifference(time, startTime);

            //var framePoint = i * (width / c/ monthUnit);
            //var frameInnerPoint = TimeToPoint(time, )



            if (ruledLines?.Count > 0)
            {
                //ショートカットできるか確認

                var nextPoint = ruledLines.FirstOrDefault<DateTime>(x => x >= time);
                var nextPointIndex = -1;
                if (nextPoint != default)
                {
                    nextPointIndex = ruledLines.IndexOf(nextPoint);
                }
                DateTime? firstPoint = 0 <= nextPointIndex - 1 ? ruledLines[nextPointIndex - 1] : null;

                //point内では均等
                double innerPoint = double.NaN;
                double frameLeftPoint = (nextPointIndex - 1) * widthPerRuledLine;

                if (firstPoint != null && nextPoint != default)
                {
                    innerPoint = TimeToPoint(time, firstPoint.Value, nextPoint, widthPerRuledLine);
                    return innerPoint + frameLeftPoint;
                }
            }

            return 0.0;

        }

        public static double TimeToPointMonth(DateTime time, double widthPerRuledLine, IList<IRuledLine> ruledLines)
        {
            //var c = DateTimeHelper.GetMonthDifference(endTime, startTime);
            ////渡された日付が何か月目か？を求める
            //var i = DateTimeHelper.GetMonthDifference(time, startTime);

            //var framePoint = i * (width / c/ monthUnit);
            //var frameInnerPoint = TimeToPoint(time, )

            if (ruledLines == null || ruledLines.Count == 0)
            {
                return 0.0;
            }

            if (time < ruledLines[0].DateValue) { return ruledLines[0].LeftPoint; }
            if (time > ruledLines[ruledLines.Count - 1].DateValue) { return ruledLines[ruledLines.Count - 1].LeftPoint; }

            if (ruledLines?.Count > 0)
            {
                //ショートカットできるか確認

                var nextPoint = ruledLines.FirstOrDefault(x => x.DateValue > time);
                var nextPointIndex = -1;
                if (nextPoint != default)
                {
                    nextPointIndex = ruledLines.IndexOf(nextPoint);
                }
                else
                {
                    //最後の値ピッタリのとき
                    return widthPerRuledLine * (ruledLines.Count-1);
                }
                    IRuledLine? firstPoint = 0 <= nextPointIndex - 1 ? ruledLines[nextPointIndex - 1] : null;

                //point内では均等
                double innerPoint = double.NaN;
                double frameLeftPoint = (nextPointIndex - 1) * widthPerRuledLine;

                if (firstPoint != null && nextPoint != default)
                {
                    innerPoint = TimeToPoint(time, firstPoint.DateValue, nextPoint.DateValue, widthPerRuledLine);
                    return innerPoint + frameLeftPoint;
                }
            }

            return 0.0;

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

        public static DateTime PointToTimeMonth(double point, double widthPerRuledLine, TimeSpan minSpan, IList<IRuledLine> ruledLines)
        {

            if (ruledLines == null || ruledLines.Count == 0)
            {
                return DateTime.MinValue;
            }

            if (point < ruledLines[0].LeftPoint) { return ruledLines[0].DateValue; }
            if (point > ruledLines[ruledLines.Count - 1].LeftPoint) { return ruledLines[ruledLines.Count - 1].DateValue; }

            //ショートカットできるか確認

            var nextPoint = ruledLines.FirstOrDefault(x => x.LeftPoint > point);
            var nextPointIndex = -1;
            if (nextPoint != null)
            {
                nextPointIndex = ruledLines.IndexOf(nextPoint);
            }
            else
            {
                //最後の値ピッタリのとき
                return ruledLines[ruledLines.Count - 1].DateValue;
            }
            IRuledLine? firstPoint = 0 <= nextPointIndex - 1 ? ruledLines[nextPointIndex - 1] : null;

            //point内では均等
            DateTime? innerDate = null;
            if (firstPoint != null && nextPoint != default)
            {
                innerDate = PointToTime(point, firstPoint.DateValue, nextPoint.DateValue, widthPerRuledLine, minSpan);
                //innerPoint = TimeToPoint(time, firstPoint.DateValue, nextPoint.DateValue, widthPerRuledLine);
                return innerDate.Value;
            }

            return DateTime.MinValue;
        }


    }
}
