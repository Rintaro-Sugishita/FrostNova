using CommunityToolkit.Mvvm.ComponentModel;
using FrostNova.TimeRangeChart.TimeRangeHelpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace FrostNova.TimeRangeChart.Wpf.Sample
{
    public partial class SampleModel : TimeRange
    {

        public SampleModel(DateTime startTime, DateTime endTime) : base (startTime, endTime)
        {
        }

        public SampleModel(DateTime startTime, TimeSpan workingSpan, IEnumerable<ITimeRange>? nonWorkingTimes = null) : base(startTime, workingSpan, nonWorkingTimes)
        {
        }



        [ObservableProperty]
        private Color _color = Colors.Red;


       // TimeSpan? ITimeRange.WorkingSpan { get => WorkingSpan; set => throw new NotImplementedException(); }

    }
}
