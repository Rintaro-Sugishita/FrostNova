using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FrostNova.TimeRangeChart.TimeRangeHelpers;

namespace FrostNova.TimeRangeChart
{

    /// <summary>
    /// Task model class for TimeRangeChart.
    /// </summary>
    public partial class TimeRange : ObservableObject, ITimeRangeRowIndex
    {
        [ObservableProperty]
        internal DateTime _startTime;
        [ObservableProperty]
        internal DateTime _endTime;
        [ObservableProperty]
        internal TimeSpan? _workingSpan;
        [ObservableProperty]
        internal TimeSpan _actualSpan;
        [ObservableProperty]
        protected bool _ignoreNonWorkingTimes = false;

        [ObservableProperty]
        internal int _rowIndex;

        [ObservableProperty]
        private double _stackBaseTop = 0;

        [ObservableProperty]
        private bool _IsSelected = false;

        public bool CanDragTasksBetweenRows { get; set; } = true;

        public bool CanStack { get; set; } = true;

        public TimeRange(DateTime startTime, DateTime endTime, IEnumerable<ITimeRange>? nonWorkingTimes = null)
        {
            StartTime = startTime;
            EndTime = endTime;
            TimeRangeHelper.SetActualSpan(this, nonWorkingTimes);
        }

        public TimeRange(DateTime startTime, TimeSpan workingSpan, IEnumerable<ITimeRange>? nonWorkingTimes = null)
        {
            StartTime = startTime;
            WorkingSpan = workingSpan;
            EndTime = TimeRangeHelpers.TimeHelper.CalcEndTime(StartTime, WorkingSpan.Value, nonWorkingTimes);
            TimeRangeHelper.SetActualSpan(this, nonWorkingTimes);
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
            TimeRangeHelper.ExpandSpan(this, newSpan, nonWorkingTimes);
        }


        //partial void OnWorkingSpanChanged(TimeSpan? oldValue, TimeSpan? newValue)
        //{ 
        //    var newEndTime = TimeRangeHelper.CalculateEndTime(this, NonWorkingTimes);
        //    TimeRangeHelper.ExpandEndTime(this, newEndTime);
        //    //if (IgnoreNonWorkingTimes)
        //    //{
        //    //    EndTime = StartTime + newValue.Value;


        //    //}
        //    //else
        //    //{
        //    //    var newEndTime = TimeRangeHelper.CalculateEndTime(this, NonWorkingTimes);
        //    //    TimeRangeHelper.ExpandEndTime(this, newEndTime);
        //    //}
        //}


    }
}
