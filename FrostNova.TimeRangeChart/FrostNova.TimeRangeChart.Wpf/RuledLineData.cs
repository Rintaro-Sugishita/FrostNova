using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace FrostNova.TimeRangeChart.Wpf
{
    public partial class RuledLineData : ObservableObject
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
       
        [ObservableProperty]
        [property: NotifyPropertyChangedFor(nameof(RuledLineThickness))]
        [property: NotifyPropertyChangedFor(nameof(ActualHeight))]
        [property: NotifyPropertyChangedFor(nameof(RuledLineFill))]
        [property: NotifyPropertyChangedFor(nameof(WidthPerRuledLine))]
        [property: NotifyPropertyChangedFor(nameof(HeaderFormat))]
        private GridTimeRangeChart? _parentGridChart;

        internal RuledLineData(GridTimeRangeChart gridChart)
        {
            ParentGridChart = gridChart;
        }

        internal RuledLineData(TimeRangeChart chart)
        {
            ParentChart = chart;
        }

        public double RuledLineThickness
        {
            get
            {
                return ParentGridChart?.RuledLineThickness ?? ParentChart?.RuledLineThickness ?? 0.0;
            }
        }

        public double ActualHeight
        {
            get
            {
                return ParentGridChart?.ActualHeight ?? ParentChart?.ActualHeight ?? 0.0;
            }
        }

        public Brush RuledLineFill
        {
            get
            {
                return ParentGridChart?.RuledLineFill ?? ParentChart?.RuledLineFill ?? new SolidColorBrush();
            }
        }


        public double WidthPerRuledLine
        {
            get
            {
                return ParentGridChart?.WidthPerRuledLine ?? ParentChart?.WidthPerRuledLine ?? 0.0;
            }
        }

        public string HeaderFormat
        {
            get
            {
                return ParentGridChart?.HeaderFormat ?? "";
            }
        }




    }
}
