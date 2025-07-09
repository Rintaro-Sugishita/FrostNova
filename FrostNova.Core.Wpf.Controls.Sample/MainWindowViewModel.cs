using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FrostNova.Core.Wpf;
using FrostNova.Core.Wpf.Controls.Dialogs;

namespace FrostNova.Core.Wpf.Controls.Sample
{
    public partial class MainWindowViewModel : ViewModelBases.ViewModelBase
    {
        [ObservableProperty]
        private DateTime? _date;

        [ObservableProperty]
        private TimeSpan? _span;

        [ObservableProperty]
        private Color? _selectedColor;

        [RelayCommand]
        private void ShowColorDialog()
        {

            SelectedColor = ColorSelectDialog.Show(SelectedColor);


        }

    }
}
