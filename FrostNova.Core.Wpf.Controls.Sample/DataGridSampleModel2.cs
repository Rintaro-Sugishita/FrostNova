using CommunityToolkit.Mvvm.ComponentModel;
using FrostNova.Core.Common;
using FrostNova.Core.Wpf.Models.DataGrdExtCols;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FrostNova.Core.Wpf.Controls.Sample
{
  
    public partial class DataGridSampleModel2 : ObservableObject, ICustomDataGridColumnItem
    {

        [ObservableProperty]
        private string _ColumnValue = "";


        public ObservableDictionary<string, object> CustomItems { get; set; } = [];
    }
}
