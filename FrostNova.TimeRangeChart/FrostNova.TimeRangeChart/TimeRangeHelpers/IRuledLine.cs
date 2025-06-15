using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FrostNova.TimeRangeChart.TimeRangeHelpers
{
    public interface IRuledLine
    {
        double LeftPoint { get; }

        DateTime DateValue { get; }

    }
}
