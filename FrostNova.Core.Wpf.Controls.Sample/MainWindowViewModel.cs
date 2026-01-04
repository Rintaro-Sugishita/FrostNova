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
using FrostNova.Core.Wpf.AttachedProperties;
using FrostNova.Core.Wpf.Controls.Dialogs;
using FrostNova.Core.Wpf.Messages;

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

        [ObservableProperty]
        private AllowedCharTypes _testCharType = AllowedCharTypes.All;

        [RelayCommand]
        private void ShowColorDialog()
        {
            SelectedColor = ColorSelectDialog.Show(SelectedColor);
        }

        protected override void InitializeOnExecute()
        {
            base.InitializeOnExecute();
            Samples.Clear();
            Samples.Add(new DataGridSampleModel1() {ColumnValue = "aaaa", NumValue = 20,MultiLineText="aaa\nbbbb" });
            Samples.Add(new DataGridSampleModel1() {ColumnValue = "bbbb", NumValue = 40 });
            Samples.Add(new DataGridSampleModel1() {ColumnValue = "cccc", NumValue = 60 });
            Samples.Add(new DataGridSampleModel1() {ColumnValue = "cccc", NumValue = 80 });
            Samples.Add(new DataGridSampleModel1() {ColumnValue = "dddd", NumValue = 100 });

            MessageBox = new FrostNovaMessageBox();



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


        [RelayCommand]
        private void ShowErrorMessage()
        {
            MessageBox?.Show("an error occur.", "", button: System.Windows.MessageBoxButton.OK, icon: System.Windows.MessageBoxImage.Error);
        }


        [RelayCommand]
        private void ShowMultiLineMessage()
        {
            MessageBox?.Show("An error has occurred.\r\nPlease contact the developer.\r\nアプリケーション内部で予期しない例外が発生しました。処理を継続できないため、現在の操作は中断されました。入力データまたは設定ファイルに不整合がある可能性があります。詳細は以下のスタックトレースを確認してください。\r\n\r\nSystem.InvalidOperationException: 無効な状態遷移が検出されました。\r\n   at App.Core.ProcessManager.Execute(String command)\r\n   at App.MainWindow.OnStartClick(Object sender, EventArgs e)\r\n   at System.Windows.Forms.Control.OnClick(EventArgs e)\r\n   at System.Windows.Forms.Button.OnClick(EventArgs e)", "", button: System.Windows.MessageBoxButton.YesNoCancel, icon: System.Windows.MessageBoxImage.Information);
        }

        [RelayCommand]
        private void ShowWarningMessage()
        {
            MessageBox?.Show("warning!!!", button: System.Windows.MessageBoxButton.OKCancel, icon: System.Windows.MessageBoxImage.Warning);
        }
    }
}
