using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FrostNova.TimeRangeChart.Wpf
{
   public partial class ColumnHeaderData : ObservableObject
    {
        [ObservableProperty]
        private DateTime _dateValue;


        [ObservableProperty]
        private GridTimeRangeChart _parentGridChart;

        internal ColumnHeaderData(GridTimeRangeChart gridChart)
        {
            ParentGridChart = gridChart;
        }

        


    }
}
