using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using FrostNova.Core.Wpf;

namespace FrostNova.Core.Wpf.Controls.Sample
{
    public partial class MainWindowViewModel : ViewModelBases.ViewModelBase
    {
        [ObservableProperty]
        private DateTime? _date;

        [ObservableProperty]
        private TimeSpan? _span;

    }
}
