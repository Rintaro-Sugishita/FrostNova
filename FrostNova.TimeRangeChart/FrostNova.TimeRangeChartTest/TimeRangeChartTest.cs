using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FrostNova.TimeRangeChart;
using FrostNova.TimeRangeChart.TimeRangeHelpers;

namespace FrostNova.TimeRangeChartTest
{
    public class TimeRangeChartTest
    {

        [Theory]
        [MemberData(nameof(RoundToNearestIncrementTestData))]
        public void RoundToNearestIncrement(DateTime time, TimeSpan minSpan, DateTime answer)
        {
            Assert.Equal(answer, TimeHelper.RoundToNearestIncrement(time, minSpan));
        }

        public static object[][] RoundToNearestIncrementTestData =>
            new object[][]
            {
                new object[] { new DateTime(2023, 10, 1, 10, 0, 0), TimeSpan.FromMinutes(5), new DateTime(2023, 10, 1, 10, 0, 0) },
                new object[] { new DateTime(2023, 10, 1, 10, 0, 0), TimeSpan.FromMinutes(10), new DateTime(2023, 10, 1, 10, 0, 0) },
                new object[] { new DateTime(2023, 10, 1, 10, 0, 0), TimeSpan.FromMinutes(60), new DateTime(2023, 10, 1, 10, 0, 0) },


                new object[] { new DateTime(2023, 10, 1, 10, 0, 0), TimeSpan.FromMinutes(120), new DateTime(2023, 10, 1, 10, 0, 0) },
                new object[] { new DateTime(2023, 10, 1, 11, 0, 0), TimeSpan.FromMinutes(120), new DateTime(2023, 10, 1, 10, 0, 0) },
                new object[] { new DateTime(2023, 10, 1, 11, 0, 0)+TimeSpan.FromTicks(1), TimeSpan.FromMinutes(120), new DateTime(2023, 10, 1, 12, 0, 0) },
                new object[] { new DateTime(2023, 10, 1, 11, 0, 1), TimeSpan.FromMinutes(120), new DateTime(2023, 10, 1, 12, 0, 0) },
                new object[] { new DateTime(2023, 10, 1, 11, 30, 0), TimeSpan.FromMinutes(120), new DateTime(2023, 10, 1, 12, 0, 0) },


                new object[] { new DateTime(2023, 10, 1, 10, 10, 0), TimeSpan.FromMinutes(5), new DateTime(2023, 10, 1, 10, 10, 0) },
                new object[] { new DateTime(2023, 10, 1, 10, 10, 0), TimeSpan.FromMinutes(10), new DateTime(2023, 10, 1, 10, 10, 0) },
                new object[] { new DateTime(2023, 10, 1, 10, 10, 0), TimeSpan.FromMinutes(15), new DateTime(2023, 10, 1, 10, 15, 0) },
                new object[] { new DateTime(2023, 10, 1, 10, 10, 0), TimeSpan.FromMinutes(30), new DateTime(2023, 10, 1, 10, 0, 0) },


                new object[] { new DateTime(2023, 10, 1, 10, 10, 0), TimeSpan.FromDays(1), new DateTime(2023, 10, 1, 0, 0, 0) },
                new object[] { new DateTime(2023, 10, 2, 10, 10, 0), TimeSpan.FromDays(1), new DateTime(2023, 10, 2, 0, 0, 0) },
                new object[] { new DateTime(2023, 10, 2, 23, 10, 0), TimeSpan.FromDays(1), new DateTime(2023, 10, 3, 0, 0, 0) },
                new object[] { new DateTime(2023, 10, 2, 11, 59, 59), TimeSpan.FromDays(1), new DateTime(2023, 10, 2, 0, 0, 0) },
                new object[] { new DateTime(2023, 10, 2, 12, 0, 0), TimeSpan.FromDays(1), new DateTime(2023, 10, 2, 0, 0, 0) },
                new object[] { new DateTime(2023, 10, 2, 12, 0, 1), TimeSpan.FromDays(1), new DateTime(2023, 10, 3, 0, 0, 0) },

                new object[] { new DateTime(2023, 10, 1, 0, 0, 0), TimeSpan.FromDays(10), new DateTime(2023, 10, 1, 0, 0, 0) },
                new object[] { new DateTime(2023, 10, 2, 0, 0, 0), TimeSpan.FromDays(10), new DateTime(2023, 10, 1, 0, 0, 0) },
                new object[] { new DateTime(2023, 10, 4, 0, 0, 0), TimeSpan.FromDays(10), new DateTime(2023, 10, 1, 0, 0, 0) },
                new object[] { new DateTime(2023, 10, 5, 0, 0, 0), TimeSpan.FromDays(10), new DateTime(2023, 10, 1, 0, 0, 0) },
                new object[] { new DateTime(2023, 10, 7, 0, 0, 0)-TimeSpan.FromTicks(1), TimeSpan.FromDays(10), new DateTime(2023, 10, 1, 0, 0, 0) },
                new object[] { new DateTime(2023, 10, 7, 0, 0, 0), TimeSpan.FromDays(10), new DateTime(2023, 10, 11, 0, 0, 0) },
                new object[] { new DateTime(2023, 10, 9, 0, 0, 0), TimeSpan.FromDays(10), new DateTime(2023, 10, 11, 0, 0, 0) },
                new object[] { new DateTime(2023, 10, 10, 0, 0, 0), TimeSpan.FromDays(10), new DateTime(2023, 10, 11, 0, 0, 0) },
                new object[] { new DateTime(2023, 10, 11, 0, 0, 0), TimeSpan.FromDays(10), new DateTime(2023, 10, 11, 0, 0, 0) },
                new object[] { new DateTime(2023, 10, 18, 0, 0, 0), TimeSpan.FromDays(10), new DateTime(2023, 10, 21, 0, 0, 0) },
                new object[] { new DateTime(2023, 10, 21, 0, 0, 0), TimeSpan.FromDays(10), new DateTime(2023, 10, 21, 0, 0, 0) },
                new object[] { new DateTime(2023, 10, 24, 0, 0, 0), TimeSpan.FromDays(10), new DateTime(2023, 10, 21, 0, 0, 0) },
                new object[] { new DateTime(2023, 10, 25, 0, 0, 0), TimeSpan.FromDays(10), new DateTime(2023, 10, 21, 0, 0, 0) },
                new object[] { new DateTime(2023, 10, 26, 0, 0, 0), TimeSpan.FromDays(10), new DateTime(2023, 10, 21, 0, 0, 0) },
                new object[] { new DateTime(2023, 10, 27, 0, 0, 0)-TimeSpan.FromTicks(1), TimeSpan.FromDays(10), new DateTime(2023, 10, 21, 0, 0, 0) },
                new object[] { new DateTime(2023, 10, 27, 0, 0, 0), TimeSpan.FromDays(10), new DateTime(2023, 10, 31, 0, 0, 0) },
                new object[] { new DateTime(2023, 10, 30, 0, 0, 0), TimeSpan.FromDays(10), new DateTime(2023, 10, 31, 0, 0, 0) },
                new object[] { new DateTime(2023, 10, 31, 0, 0, 0), TimeSpan.FromDays(10), new DateTime(2023, 10, 31, 0, 0, 0) },

                new object[] { new DateTime(2023, 2, 28, 0, 0, 0), TimeSpan.FromDays(10), new DateTime(2023, 2, 28, 0, 0, 0) },
                new object[] { new DateTime(2024, 2, 28, 0, 0, 0), TimeSpan.FromDays(10), new DateTime(2024, 2, 29, 0, 0, 0) },
                new object[] { new DateTime(2024, 3, 1, 0, 0, 0)-TimeSpan.FromTicks(1), TimeSpan.FromDays(10), new DateTime(2024, 2, 29, 0, 0, 0) },

            };



        [Theory]
        [MemberData(nameof(PointToTimeTestData))]
        public void PointToTime(double point, DateTime startTime, DateTime endTime, TimeSpan minSpan, double width, DateTime answer)
        {
            Assert.Equal(answer, TimeScreenHelper.PointToTime(point, startTime, endTime, width, minSpan));
        }

        public static object[][] PointToTimeTestData => new object[][] {
            new object[] {0d, new DateTime(2023, 10, 1, 0, 0, 0), new DateTime(2023, 10, 2, 0, 0, 0), TimeSpan.FromMinutes(0), 100.0, new DateTime(2023, 10, 1, 0, 0, 0) },
            new object[] {100d, new DateTime(2023, 10, 1, 0, 0, 0), new DateTime(2023, 10, 2, 0, 0, 0), TimeSpan.FromMinutes(0), 100.0, new DateTime(2023, 10, 2, 0, 0, 0) },
            new object[] {50d, new DateTime(2023, 10, 1, 0, 0, 0), new DateTime(2023, 10, 2, 0, 0, 0), TimeSpan.FromMinutes(0), 100.0, new DateTime(2023, 10, 1, 12, 0, 0) },


        };

        [Theory]
        [MemberData(nameof(TimeToPointMonthTestData))]
        public void TimeToPointMonth(DateTime dateTime, double widthPerRuledLine, List<DateTime> ruledLines, double answer)
        {
            Assert.Equal(answer, TimeScreenHelper.TimeToPointMonth(dateTime, widthPerRuledLine, ruledLines));
            Assert.Equal(answer, TimeScreenHelper.TimeToPointMonth(dateTime, widthPerRuledLine, ruledLines.Select(x=> new RuledLine() { DateValue = x} ).ToList<IRuledLine>()));
        }

        public static object[][] TimeToPointMonthTestData => new object[][]
        {
            new object[] {new DateTime (2025, 10, 1), 100,new List<DateTime>() { new DateTime(2025, 10, 1), new DateTime(2025, 11, 1), new DateTime(2025, 12, 1) }, 0.0d },
            new object[] {new DateTime (2025, 12, 1), 100,new List<DateTime>() { new DateTime(2025, 10, 1), new DateTime(2025, 11, 1), new DateTime(2025, 12, 1) }, 200.0d },
            new object[] {new DateTime (2025, 11, 1), 100,new List<DateTime>() { new DateTime(2025, 10, 1), new DateTime(2025, 11, 1), new DateTime(2025, 12, 1) }, 100.0d },
            new object[] {new DateTime (2025, 11, 16), 100,new List<DateTime>() { new DateTime(2025, 10, 1), new DateTime(2025, 11, 1), new DateTime(2025, 12, 1) }, 150.0d },
            new object[] {new DateTime (2025, 11, 15), 300,new List<DateTime>() { new DateTime(2025, 10, 1), new DateTime(2025, 11, 1), new DateTime(2025, 12, 1) }, 440.0d },
            new object[] {new DateTime (2025, 11, 14), 300,new List<DateTime>() { new DateTime(2025, 10, 1), new DateTime(2025, 11, 1), new DateTime(2025, 12, 1) }, 430.0d },
            new object[] {new DateTime (2025, 11, 17), 300,new List<DateTime>() { new DateTime(2025, 10, 1), new DateTime(2025, 11, 1), new DateTime(2025, 12, 1) }, 460.0d },
            new object[] {new DateTime (2025, 10, 2), 310,new List<DateTime>() { new DateTime(2025, 10, 1), new DateTime(2025, 11, 1), new DateTime(2025, 12, 1) }, 10.0d },
        };

        [DebuggerDisplay("Date: {DateValue}, Left: {LeftPoint}")]
        internal class RuledLine : IRuledLine
        {
            public double LeftPoint { get; set; }

            public DateTime DateValue { get; set; }
        }


        [Theory]
        [MemberData(nameof(CalcEndTimeTestData))]
        public void CalcEndTime(DateTime startTime, TimeSpan workingSpan, IEnumerable<ITimeRange> nonWorkingTimes, DateTime answer)
        {

            TimeRange range = new TimeRange(startTime, workingSpan);
            var result = TimeRangeHelper.CalculateEndTime(range, nonWorkingTimes);
            Assert.Equal(answer, result);

        }

        public static object[][] CalcEndTimeTestData =>
            new object[][]
            {
                new object[] { new DateTime(2023, 10, 1, 00, 0, 0), TimeSpan.FromDays(2), 
                    new List<ITimeRange>() { new FixedTimeRange(new DateTime(2023,10,2), new DateTime(2023, 10, 3)) } , 
                    new DateTime(2023, 10, 4, 0, 0, 0) },
                new object[] { new DateTime(2023, 10, 1, 00, 0, 0), TimeSpan.FromDays(2), 
                    new List<ITimeRange>() { new FixedTimeRange(new DateTime(2023,10,2), new DateTime(2023, 10, 4)) } , 
                    new DateTime(2023, 10, 5, 0, 0, 0) },
                new object[] { new DateTime(2023, 10, 1, 00, 0, 0), TimeSpan.FromDays(2), 
                    new List<ITimeRange>() { new FixedTimeRange(new DateTime(2023,10,2), new DateTime(2023, 10, 3)),
                                             new FixedTimeRange(new DateTime(2023,10,3), new DateTime(2023, 10, 4)) } , 
                    new DateTime(2023, 10, 5, 0, 0, 0) },


            };

    }
}
