using CommunityToolkit.Mvvm.ComponentModel;
using FrostNova.TimeRangeChart.Wpf.TimeRangeHelpers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FrostNova.TimeRangeChart.Wpf.Sample
{
    public partial class GridSampleData : ObservableObject
    {

        [ObservableProperty]
        private string _name="";

        [ObservableProperty]
        private ObservableCollection<SampleModel> _tasks = new ObservableCollection<SampleModel>();

        [ObservableProperty]
        private ObservableCollection<IBackgroundTimeRange> _backgroundTimeRanges = [];

    }
}
