using FrostNova.TimeRangeChart.TimeRangeHelpers;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FrostNova.TimeRangeChart.Wpf
{
    class DragTaskData
    {
        public object? TimeRange { get; set; }
        public DateTime? ChartStartTime { get; set; }
        public DateTime? ChartEndTime { get; set; }
        public IList? SourceRow { get; set; }
    }
}
