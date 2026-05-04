using FrostNova.TimeRangeChart.TimeRangeHelpers;
using FrostNova.TimeRangeChart.Wpf.TimeRangeHelpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace FrostNova.TimeRangeChart.Wpf
{
    public class BackgroundTimeRange : IBackgroundTimeRange
    {
        public Brush Background { get; set; } = Brushes.Transparent;

        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public TimeSpan? WorkingSpan { get => ActualSpan; set => ActualSpan = value ?? TimeSpan.Zero; }
        public TimeSpan ActualSpan { get; set; }

        public int RowIndex { get; set; }

        public double StackBaseTop => 0.0;

        public bool IsSelected { get; set; }

        public bool CanDragTasksBetweenRows => false;

        public bool CanStack => false;

        public bool IgnoreNonWorkingTimes => true;

        public void ExpandEndTime(DateTime newEndTime, IEnumerable<ITimeRange>? nonWorkingTimes)
        {
            throw new NotImplementedException();
        }

        public void ExpandStartTime(DateTime newStartTime, IEnumerable<ITimeRange>? nonWorkingTimes)
        {
            throw new NotImplementedException();
        }

        public void ExpandWorkingSpan(TimeSpan newSpan, IEnumerable<ITimeRange>? nonWorkingTimes)
        {
            throw new NotImplementedException();
        }

        public void Move(TimeSpan delta, IEnumerable<ITimeRange>? nonWorkingTimes)
        {
            throw new NotImplementedException();
        }

        public void MoveTo(DateTime newStartTime, IEnumerable<ITimeRange>? nonWorkingTimes)
        {
            throw new NotImplementedException();
        }
    }
}
