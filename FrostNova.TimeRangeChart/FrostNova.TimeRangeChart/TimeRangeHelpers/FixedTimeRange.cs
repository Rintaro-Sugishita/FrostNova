using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FrostNova.TimeRangeChart.TimeRangeHelpers
{

    /// <summary>
    /// fixed range task model class for 'TimeRangeChart'.
    /// </summary>
    [DebuggerDisplay($"{nameof(FixedTimeRange)}: {nameof(StartTime)}:{{StartTime}}, {nameof(EndTime)}:{{EndTime}} ")]
    public partial class FixedTimeRange : ObservableObject, ITimeRangeRowIndex
    {
        [ObservableProperty]
        private DateTime _startTime;

        [ObservableProperty]
        private DateTime _endTime;

        [ObservableProperty]
        private int _rowIndex;

        [ObservableProperty]
        private double _stackBaseTop = 0.0;

        public TimeSpan? WorkingSpan { get => ActualSpan; set => ActualSpan = value.HasValue ? value.Value : TimeSpan.Zero; }

        [ObservableProperty]
        TimeSpan _actualSpan;
        public bool IgnoreNonWorkingTimes { get; } = true;

        public bool IsSelected { get; set; }

        public bool CanDragTasksBetweenRows => false;

        public bool CanStack { get; set; }

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

        public void MoveTo(DateTime newStartTime, IEnumerable<ITimeRange>? nonWorkingTimes)
        {
            TimeRangeHelper.MoveTo(this, newStartTime, nonWorkingTimes);
        }

        public void ExpandWorkingSpan(TimeSpan newSpan, IEnumerable<ITimeRange>? nonWorkingTimes)
        {
        }

    }
}
