using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FrostNova.TimeRangeChart.TimeRangeHelpers;

public interface ITimeRangeRowIndex : ITimeRange 
{
    int RowIndex { get; set; }
    double StackBaseTop { get; }

    bool IsSelected {  get; set; }

    bool CanDragTasksBetweenRows { get;}
}
