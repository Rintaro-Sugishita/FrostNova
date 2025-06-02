using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FrostNova.TimeRangeChart;
using FrostNova.TimeRangeChart.TimeRangeHelpers;

namespace FrostNova.TimeRangeChartTest
{
    public class TimeRangeTest
    {

        public void Move(DateTime startTime, TimeSpan span, (DateTime startTime, DateTime endTime) answer)
        {
            var range =new TimeRange(startTime, span);
            
        }

    }
}
