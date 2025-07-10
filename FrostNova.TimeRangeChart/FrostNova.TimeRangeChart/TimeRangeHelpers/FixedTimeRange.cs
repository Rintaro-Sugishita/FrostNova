using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FrostNova.TimeRangeChart.TimeRangeHelpers
{
    [DebuggerDisplay($"{nameof(FixedTimeRange)}: {nameof(StartTime)}:{{StartTime}}, {nameof(EndTime)}:{{EndTime}} ")]

    public partial class FixedTimeRange : ObservableObject, ITimeRange
    {
        [ObservableProperty]
        private DateTime _startTime;

        [ObservableProperty]
        private DateTime _endTime;

        public TimeSpan? WorkingSpan { get => ActualSpan; set => ActualSpan = value.HasValue ? value.Value : TimeSpan.Zero; }

        [ObservableProperty]
        TimeSpan _actualSpan;
        public bool IgnoreNonWorkingTimes { get; } = true;

        public FixedTimeRange(DateTime startTime, DateTime endTime)
        {
            StartTime = startTime;
            EndTime = endTime;
            ActualSpan = StartTime - EndTime;
        }
        public FixedTimeRange(DateTime startTime, TimeSpan span)
        {
            StartTime = startTime;
            WorkingSpan = span;
            EndTime = TimeRangeHelpers.TimeHelper.CalcEndTime<ITimeRange>(StartTime, WorkingSpan.Value);
            TimeRangeHelper.SetActualSpan(this);
        }

        public void ExpandEndTime(DateTime newEndTime, IEnumerable<ITimeRange>? nonWorkingTimes)
        {
            TimeRangeHelper.ExpandEndTime(this, newEndTime, nonWorkingTimes);
        }

        public void ExpandStartTime(DateTime newStartTime, IEnumerable<ITimeRange>? nonWorkingTimes)
        {
            TimeRangeHelper.ExpandStartTime(this, newStartTime, nonWorkingTimes);
        }

        public void Move(TimeSpan delta, IEnumerable<ITimeRange>? nonWorkingTimes)
        {

            TimeRangeHelper.Move(this, delta, nonWorkingTimes);

        }
        public void ExpandWorkingSpan(TimeSpan newSpan, IEnumerable<ITimeRange>? nonWorkingTimes)
        {
        }
    }
}
