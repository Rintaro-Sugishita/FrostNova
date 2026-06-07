using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FrostNova.TimeRangeChart.TimeRangeHelpers
{
    public static class TimeHelper
    {



        public static List<DateTime> GenerateTicks(DateTime startTime, DateTime endTime, TimeSpan increment)
        {
            List<DateTime> ticks = [];

            // 最も近い増分の倍数に調整
            DateTime adjustedStartTime = TimeHelper.RoundToNearestIncrement(startTime, increment);

            DateTime currentTime = adjustedStartTime;
            while (currentTime <= endTime)
            {
                ticks.Add(currentTime);
                currentTime = currentTime.Add(increment);
            }
            return ticks;
        }

        public static (List<DateTime> mainTicks, List<DateTime> subTicks) GenerateTicks(DateTime startTime, DateTime endTime, TimeSpan increment, TimeSpan subIncrement)
        {
            List<DateTime> ticks = [];
            List<DateTime> subTicks = [];

            // 最も近い増分の倍数に調整
            DateTime adjustedStartTime = TimeHelper.RoundToNearestIncrement(startTime, increment);

            DateTime currentTime = adjustedStartTime;
            while (currentTime <= endTime)
            {
                ticks.Add(currentTime);
                var next = currentTime.Add(increment);

                if (increment != subIncrement)
                {
                    var subCurrent = currentTime;
                    while (subCurrent < next)
                    {
                        subCurrent = subCurrent.Add(subIncrement);
                       if (subCurrent < next) subTicks.Add(subCurrent);
                    }
                }

                currentTime = next;
            }
            return (ticks, subTicks);
        }

        public static List<DateTime> GenerateTicksMonth(DateTime startTime, DateTime endTime, int spanMonth)
        {
            List<DateTime> ticks = [];

            // 最も近い増分の倍数に調整

            DateTime adjustedStartTime = new DateTime(startTime.Year, startTime.Month, 1);

            DateTime currentTime = adjustedStartTime;
            while (currentTime <= endTime)
            {
                ticks.Add(currentTime);
                currentTime = currentTime.AddMonths(spanMonth);
            }
            return ticks;
        }

        public static (List<DateTime> mainTicks, List<DateTime> subTicks) GenerateTicksMonth(
                DateTime startTime,
                DateTime endTime,
                int spanMonth,
                MonthSubTickType subTickType)
        {
            List<DateTime> mainTicks = [];
            List<DateTime> subTicks = [];

            // 月Tickは必ず月初
            DateTime currentTime = new DateTime(startTime.Year, startTime.Month, 1);

            while (currentTime <= endTime)
            {
                mainTicks.Add(currentTime);

                DateTime next = currentTime.AddMonths(spanMonth);
                DateTime rangeEnd = next < endTime ? next : endTime;

                if (subTickType != MonthSubTickType.None)
                {
                    subTicks.AddRange(
                        GenerateSubTicksForRange(
                            currentTime,
                            rangeEnd,
                            subTickType));
                }

                currentTime = next;
            }

            return (mainTicks, subTicks);
        }
        private static List<DateTime> GenerateSubTicksForRange(
            DateTime rangeStart,
            DateTime rangeEnd,
            MonthSubTickType type)
        {
            List<DateTime> ticks = [];

            switch (type)
            {
                case MonthSubTickType.Weekly:
                    {
                        DateTime d = rangeStart.AddDays(7);
                        while (d < rangeEnd)
                        {
                            ticks.Add(d);
                            d = d.AddDays(7);
                        }
                        break;
                    }

                case MonthSubTickType.HalfMonth:
                    {
                        DateTime d = rangeStart;
                        while (d < rangeEnd)
                        {
                            DateTime first = new DateTime(d.Year, d.Month, 1);
                            DateTime second = new DateTime(d.Year, d.Month, 16);

                            if (first > rangeStart && first < rangeEnd)
                                ticks.Add(first);

                            if (second > rangeStart && second < rangeEnd)
                                ticks.Add(second);

                            d = d.AddMonths(1);
                        }
                        break;
                    }

                case MonthSubTickType.MonthDivided2:
                    {
                        double totalDays = (rangeEnd - rangeStart).TotalDays;

                        ticks.Add(rangeStart.AddDays(totalDays / 2));
                        break;
                    }

                case MonthSubTickType.MonthDivided4:
                    {
                        double totalDays = (rangeEnd - rangeStart).TotalDays;

                        for (int i = 1; i < 4; i++)
                        {
                            ticks.Add(rangeStart.AddDays(totalDays * i / 4));
                        }
                        break;
                    }
            }

            return ticks;
        }

        public static List<DateTime> GenerateSubTicksForMonth(
    DateTime monthStart,
    MonthSubTickType type)
        {
            List<DateTime> ticks = [];

            int year = monthStart.Year;
            int month = monthStart.Month;
            DateTime monthEnd = monthStart.AddMonths(1);

            switch (type)
            {
                case MonthSubTickType.Weekly:
                    {
                        DateTime d = monthStart;
                        while (d < monthEnd)
                        {
                            d = d.AddDays(7);
                            ticks.Add(d);
                        }
                        break;
                    }

                case MonthSubTickType.HalfMonth:
                    {
                        ticks.Add(new DateTime(year, month, 1));
                        ticks.Add(new DateTime(year, month, 16));
                        break;
                    }

                case MonthSubTickType.MonthDivided2:
                    {
                        int days = DateTime.DaysInMonth(year, month);
                        ticks.Add(monthStart);
                        ticks.Add(monthStart.AddDays((int)(days / 2.0)));
                        break;
                    }

                case MonthSubTickType.MonthDivided4:
                    {
                        int days = DateTime.DaysInMonth(year, month);
                        for (int i = 0; i < 4; i++)
                        {
                            ticks.Add(monthStart.AddDays((int)(days * i / 4.0)));
                        }
                        break;
                    }
            }

            return ticks;
        }





        public static DateTime CalcEndTime<T>(DateTime startTime, TimeSpan workingSpan, IEnumerable<T>? nonWorkingTimes = null) where T : ITimeRange
        {

            if (workingSpan <= TimeSpan.Zero)
                return startTime;

            return CalcShiftTime(startTime, workingSpan, nonWorkingTimes);
        }


        public static DateTime CalcShiftTime<T>(DateTime value, TimeSpan span, IEnumerable<T>? nonWorkingTimes = null) where T : ITimeRange
        {
            if (span == TimeSpan.Zero)
                return value;

            // 正方向（未来へ進める）
            if (span > TimeSpan.Zero)
            {
                DateTime current = value;
                TimeSpan remaining = span;
                if (nonWorkingTimes?.Any() == true)
                {
                    foreach (var range in nonWorkingTimes.OrderBy(x => x.StartTime))
                    {
                        if (current >= range.EndTime)
                            continue;

                        if (current < range.StartTime)
                        {
                            var chunk = range.StartTime - current;
                            if (chunk >= remaining)
                                return current + remaining;

                            remaining -= chunk;
                            current = range.EndTime;
                        }
                        else
                        {
                            // current はこの non-working 時間帯の中にいる
                            current = range.EndTime;
                        }
                    }
                }
                return current + remaining;
            }
            // 負方向（過去へ戻す）
            else
            {
                DateTime current = value;
                TimeSpan remaining = -span; // 正の値で扱う
                if (nonWorkingTimes?.Any() == true)
                {
                    foreach (var range in nonWorkingTimes.OrderByDescending(x => x.EndTime))
                    {
                        if (current <= range.StartTime)
                            continue;

                        if (current > range.EndTime)
                        {
                            var chunk = current - range.EndTime;
                            if (chunk >= remaining)
                                return current - remaining;

                            remaining -= chunk;
                            current = range.StartTime;
                        }
                        else
                        {
                            // current はこの non-working 時間帯の中にいる
                            current = range.StartTime;
                        }
                    }
                }
                return current - remaining;
            }
        }

        public static TimeSpan CalcWorkingSpan<T>(DateTime startTime, DateTime endTime, IEnumerable<T>? nonWorkingTimes = null) where T : ITimeRange
        {
            //重複は省く
            if (endTime <= startTime)
                return TimeSpan.Zero;

            TimeSpan total = endTime - startTime;
            if (nonWorkingTimes?.Any() == true)
            {
                foreach (var nonWorking in nonWorkingTimes)
                {
                    // 非稼働時間が範囲外なら無視
                    if (nonWorking.EndTime <= startTime || nonWorking.StartTime >= endTime)
                        continue;

                    // 重複部分の計算
                    var overlapStart = nonWorking.StartTime < startTime ? startTime : nonWorking.StartTime;
                    var overlapEnd = nonWorking.EndTime > endTime ? endTime : nonWorking.EndTime;

                    var overlap = overlapEnd - overlapStart;
                    total -= overlap;
                }
            }

            return total < TimeSpan.Zero ? TimeSpan.Zero : total;
        }
        public static DateTime RoundToNearestIncrement(DateTime time, TimeSpan increment)
        {
            if (increment <= TimeSpan.Zero) { return time; }

            if (increment > TimeSpan.FromDays(1))
            {
                //月初を基準とする
                DateTime monthStart = new(time.Year, time.Month, 1);
                int daysFromStart = (time - monthStart).Days;

                int incrementDays = (int)increment.TotalDays;
                int roundedStep = (int)Math.Round((double)daysFromStart / (double)incrementDays);

                DateTime candidate = monthStart.AddDays(roundedStep * incrementDays);

                // 月末を超えたら、月末で丸める
                int lastDay = DateTime.DaysInMonth(time.Year, time.Month);
                if (candidate.Day > lastDay || (time.Year != candidate.Year || time.Month != candidate.Month))
                {
                    candidate = new DateTime(time.Year, time.Month, lastDay);
                }

                return candidate;
            }
            else
            {
                long ticks = ((time.Ticks + (increment.Ticks / 2) - 1) / increment.Ticks) * increment.Ticks;
                return new DateTime(ticks, time.Kind);

            }

        }



    }
}
