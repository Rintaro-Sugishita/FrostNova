using FrostNova.TimeRangeChart.TimeRangeHelpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace FrostNova.TimeRangeChart.Wpf.TimeRangeHelpers
{
    public interface IBackgroundTimeRange : ITimeRange
    {
        Brush Background { get; }
    }
}
