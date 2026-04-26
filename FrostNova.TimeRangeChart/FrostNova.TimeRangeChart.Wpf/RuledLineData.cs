using CommunityToolkit.Mvvm.ComponentModel;
using FrostNova.TimeRangeChart.TimeRangeHelpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace FrostNova.TimeRangeChart.Wpf
{
    public partial class RuledLineData : ObservableObject, IRuledLine
    {
        [ObservableProperty]
        private double _leftPoint = 0;

        [ObservableProperty]
        [property: NotifyPropertyChangedFor(nameof(LeftPoint))]
        private DateTime _dateValue;

        [ObservableProperty]
        [property: NotifyPropertyChangedFor(nameof(RuledLineThickness))]
        [property: NotifyPropertyChangedFor(nameof(ActualHeight))]
        [property: NotifyPropertyChangedFor(nameof(RuledLineFill))]
        [property: NotifyPropertyChangedFor(nameof(WidthPerRuledLine))]
        [property: NotifyPropertyChangedFor(nameof(HeaderFormat))]
        private TimeRangeChart? _parentChart;
       

        internal RuledLineData(TimeRangeChart chart)
        {
            ParentChart = chart;
        }


        public FormattedText? FormattedText { get; set; }

        public Rect Rect { get; set; }



    }
}
