using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FrostNova.Core.Wpf.Controls.Sample
{
  
    public partial class DataGridSampleModel1 : ObservableObject
    {

        [ObservableProperty]
        private string _ColumnValue = "";

        [ObservableProperty]
        private int _NumValue = 0;


    }
}
