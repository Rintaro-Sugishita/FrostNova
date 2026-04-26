using CommunityToolkit.Mvvm.ComponentModel;
using FrostNova.TimeRangeChart.TimeRangeHelpers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace FrostNova.TimeRangeChart.Wpf.Sample
{

    [DebuggerDisplay("StartTime: {StartTime}, Span: {WorkingSpan}")]
    public partial class SampleModel : TimeRange
    {

        public SampleModel(DateTime startTime, DateTime endTime) : base (startTime, endTime)
        {
        }

        public SampleModel(DateTime startTime, TimeSpan workingSpan, IEnumerable<ITimeRange>? nonWorkingTimes = null) : base(startTime, workingSpan, nonWorkingTimes)
        {
        }

        public string InstanceId => $"VM_{this.GetHashCode():X8}";

        [ObservableProperty]
        private Color _color = Colors.Red;


        [ObservableProperty]
        private TimeSpan? _minSpan = null;

        [ObservableProperty]
        private MilestoneStyle _milestoneStyle = MilestoneStyle.None;



       // TimeSpan? ITimeRange.WorkingSpan { get => WorkingSpan; set => throw new NotImplementedException(); }

    }
}
