using System;
using System.Collections.Generic;
using FrostNova.TimeRangeChart;
using FrostNova.TimeRangeChart.TimeRangeHelpers;

namespace FrostNova.TimeRangeChartTest
{
    public class TimeRangeListHelperTest
    {
        // Shorthand for times on 2023-10-01
        private static DateTime D(int hour, int minute) => new DateTime(2023, 10, 1, hour, minute, 0);

        private static List<ITimeRange> Nw(DateTime start, DateTime end)
            => new() { new FixedTimeRange(start, end) };

        // ─────────────────────────────────────────────────────────────
        // Basic behavior
        // ─────────────────────────────────────────────────────────────

        [Fact]
        public void InsertAndPushForward_EmptyPushList_OnlyAddsNewRange()
        {
            // TaskA starts before the new range → pushList is empty → TaskA is untouched
            var taskA = new TimeRange(D(8, 0), D(10, 0));
            var ranges = new List<ITimeRange> { taskA };

            var newTask = new TimeRange(D(11, 0), D(12, 0));
            TimeRangeListHelper.InsertAndPushForward(ranges, newTask, null);

            Assert.Equal(2, ranges.Count);
            Assert.Equal(D(8, 0), taskA.StartTime);
            Assert.Equal(D(10, 0), taskA.EndTime);
        }

        [Fact]
        public void InsertAndPushForward_NoNonWorkingTimes_AbsoluteGapPreserved()
        {
            // TaskA 10:00-12:00, TaskB 14:00-16:00 (2 h gap between them)
            // Insert newTask 10:00-11:00 → pushes A and B by 1 h
            // Expected: TaskA 11:00-13:00, TaskB 15:00-17:00 (gap of 2 h preserved)
            var taskA = new TimeRange(D(10, 0), D(12, 0));
            var taskB = new TimeRange(D(14, 0), D(16, 0));
            var ranges = new List<ITimeRange> { taskA, taskB };

            var newTask = new TimeRange(D(10, 0), D(11, 0));
            TimeRangeListHelper.InsertAndPushForward(ranges, newTask, null);

            Assert.Equal(D(11, 0), taskA.StartTime);
            Assert.Equal(D(13, 0), taskA.EndTime);
            Assert.Equal(D(15, 0), taskB.StartTime);
            Assert.Equal(D(17, 0), taskB.EndTime);
        }

        // ─────────────────────────────────────────────────────────────
        // Bug #1 — start time landing on non-working boundary
        // ─────────────────────────────────────────────────────────────

        [Fact]
        public void InsertAndPushForward_PrevEndExactlyAtNwStart_SkipsNonWorkingPeriod()
        {
            // prevEnd == nw.StartTime (gap = 0): the pushed task must start at nw.EndTime,
            // not inside the non-working period.
            //
            // NW: 12:00-14:00
            // TaskA: 10:00-12:00  (2 h, will land exactly at NW start when pushed)
            // Insert newTask: 10:00-12:00  → prevEnd = 12:00 == nw.StartTime
            //
            // Expected: TaskA starts at 14:00 (immediately after NW), ends 16:00
            var nw = Nw(D(12, 0), D(14, 0));
            var taskA = new TimeRange(D(10, 0), D(12, 0));
            var ranges = new List<ITimeRange> { taskA };

            var newTask = new TimeRange(D(10, 0), D(12, 0));
            TimeRangeListHelper.InsertAndPushForward(ranges, newTask, nw);

            Assert.Equal(D(14, 0), taskA.StartTime);
            Assert.Equal(D(16, 0), taskA.EndTime);
        }

        [Fact]
        public void InsertAndPushForward_PrevEndInsideNwPeriod_SkipsNonWorkingPeriod()
        {
            // prevEnd falls inside the NW period: the pushed task must start at nw.EndTime.
            //
            // NW: 12:00-14:00
            // TaskA: 10:00-15:00  (5 h actual span)
            // Insert newTask: 10:00-13:00  → newTask.EndTime = 13:00 (inside NW)
            //
            // Expected: TaskA starts at 14:00, ends at 19:00 (5 h from 14:00)
            var nw = Nw(D(12, 0), D(14, 0));
            var taskA = new TimeRange(D(10, 0), D(15, 0));
            var ranges = new List<ITimeRange> { taskA };

            var newTask = new TimeRange(D(10, 0), D(13, 0));
            TimeRangeListHelper.InsertAndPushForward(ranges, newTask, nw);

            Assert.Equal(D(14, 0), taskA.StartTime);
            Assert.Equal(D(19, 0), taskA.EndTime);
        }

        // ─────────────────────────────────────────────────────────────
        // Bug #2 — working-time gap must be maintained across NW periods
        // ─────────────────────────────────────────────────────────────

        [Fact]
        public void InsertAndPushForward_WorkingGapPreservedAcrossNonWorkingDay()
        {
            // The working-time gap between tasks must be preserved after cascading,
            // even when a non-working day separates them.
            //
            // NW: 2023-10-02 (all day)
            // TaskA: Oct 1 10:00-12:00  (2 h)
            // TaskB: Oct 3 08:00-10:00  (2 h)
            //   Working gap from A.End (Oct1 12:00) to B.Start (Oct3 08:00) =
            //   (Oct1 12:00→Oct2 00:00 = 12 h) + (skip Oct2) + (Oct3 00:00→Oct3 08:00 = 8 h) = 20 h
            //
            // Insert newTask: Oct 1 10:00-11:00
            //   → TaskA pushed by 1 h  → Oct1 11:00-13:00
            //   → 20 h working from Oct1 13:00 = (Oct1 13:00→Oct2 00:00 = 11 h) + (skip Oct2) +
            //                                     (Oct3 00:00 + 9 h = Oct3 09:00)
            //   → TaskB → Oct3 09:00-11:00
            var oct2 = new DateTime(2023, 10, 2);
            var nw = Nw(oct2, oct2.AddDays(1));

            var taskA = new TimeRange(new DateTime(2023, 10, 1, 10, 0, 0), new DateTime(2023, 10, 1, 12, 0, 0));
            var taskB = new TimeRange(new DateTime(2023, 10, 3, 8, 0, 0), new DateTime(2023, 10, 3, 10, 0, 0));
            var ranges = new List<ITimeRange> { taskA, taskB };

            var newTask = new TimeRange(new DateTime(2023, 10, 1, 10, 0, 0), new DateTime(2023, 10, 1, 11, 0, 0));
            TimeRangeListHelper.InsertAndPushForward(ranges, newTask, nw);

            Assert.Equal(new DateTime(2023, 10, 1, 11, 0, 0), taskA.StartTime);
            Assert.Equal(new DateTime(2023, 10, 1, 13, 0, 0), taskA.EndTime);
            Assert.Equal(new DateTime(2023, 10, 3, 9, 0, 0), taskB.StartTime);
            Assert.Equal(new DateTime(2023, 10, 3, 11, 0, 0), taskB.EndTime);
        }
    }
}
