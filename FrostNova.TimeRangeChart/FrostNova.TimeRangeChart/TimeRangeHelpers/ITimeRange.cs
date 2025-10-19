using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.VisualBasic;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FrostNova.TimeRangeChart.TimeRangeHelpers
{

    /// <summary>
    /// The task interface for 'TimeRangeChart'.
    /// </summary>
    public interface ITimeRange
    {
        /// <summary>
        /// Start time of task.
        /// </summary>
        DateTime StartTime { get; set; }

        /// <summary>
        /// End time of task.
        /// </summary>
        DateTime EndTime { get; set; }

        /// <summary>
        /// Working span of task. This property has "'ActualSpan' - non-working span" value.
        /// </summary>
        TimeSpan? WorkingSpan { get; set; }

        /// <summary>
        /// Time span of task. Working span and non-working span.
        /// </summary>
        TimeSpan ActualSpan { get; set; }

        bool IgnoreNonWorkingTimes { get; }




        void Move(TimeSpan delta, IEnumerable<ITimeRange>? nonWorkingTimes);

        void MoveTo(DateTime newStartTime, IEnumerable<ITimeRange>? nonWorkingTimes);

        void ExpandStartTime(DateTime newStartTime, IEnumerable<ITimeRange>? nonWorkingTimes);

        void ExpandEndTime(DateTime newEndTime, IEnumerable<ITimeRange>? nonWorkingTimes);


        void ExpandWorkingSpan(TimeSpan newSpan, IEnumerable<ITimeRange>? nonWorkingTimes);

    }
}
