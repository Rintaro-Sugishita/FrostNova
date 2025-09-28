using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FrostNova.Core.Common;
using FrostNova.Core.Operators;
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

        [ObservableProperty]
        private NotifyCollection<DataGridSampleModel1> _samples = new NotifyCollection<DataGridSampleModel1>();

        [ObservableProperty]
        private List<object> _selectedSamples = new List<object>();

        [ObservableProperty]
        private NotifyCollection<int> _selectedIndices = new NotifyCollection<int>();

        [RelayCommand]
        private void ShowColorDialog()
        {
            SelectedColor = ColorSelectDialog.Show(SelectedColor);
        }

        protected override void InitializeOnExecute()
        {
            base.InitializeOnExecute();
            Samples.Clear();
            Samples.Add(new DataGridSampleModel1() {ColumnValue = "aaaa", NumValue = 20 });
            Samples.Add(new DataGridSampleModel1() {ColumnValue = "bbbb", NumValue = 40 });
            Samples.Add(new DataGridSampleModel1() {ColumnValue = "cccc", NumValue = 60 });
            Samples.Add(new DataGridSampleModel1() {ColumnValue = "cccc", NumValue = 80 });
            Samples.Add(new DataGridSampleModel1() {ColumnValue = "dddd", NumValue = 100 });
           
        }

        [RelayCommand]
        private void SelectedItemsChanged(List<object> selected)
        {

            using (var _= new OperationScope(SelectedIndices))
            {
                SelectedIndices.Clear();
                foreach (var item in selected)
                {
                    var index = Samples.IndexOf((item as DataGridSampleModel1)!);
                    SelectedIndices.Add(index+1);
                }
            }

        }


    }
}
