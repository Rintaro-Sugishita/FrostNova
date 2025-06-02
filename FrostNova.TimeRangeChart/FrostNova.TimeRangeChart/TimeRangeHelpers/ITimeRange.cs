using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FrostNova.TimeRangeChart.TimeRangeHelpers
{
    public interface ITimeRange
    {

        DateTime StartTime { get; set; }

        DateTime EndTime { get; set; }

        TimeSpan? WorkingSpan { get; set; }

        TimeSpan ActualSpan { get; set; }

        bool IgnoreNonWorkingTimes { get; }



        bool Contains(DateTime time) => StartTime <= time && time <= EndTime;

        bool IsOverlap(ITimeRange range)
        {
            return !(range.EndTime <= this.StartTime || this.EndTime <= range.StartTime);
        }

        void Move(TimeSpan delta, IEnumerable<ITimeRange>? nonWorkingTimes);

        void ExpandStartTime(DateTime newStartTime, IEnumerable<ITimeRange>? nonWorkingTimes);

        void ExpandEndTime(DateTime newEndTime, IEnumerable<ITimeRange>? nonWorkingTimes);


        void ExpandWorkingSpan(TimeSpan newSpan, IEnumerable<ITimeRange>? nonWorkingTimes);

    }
}
