using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FrostNova.Core.Common;
using FrostNova.Core.Operators;
using FrostNova.Core.Wpf;
using FrostNova.Core.Wpf.AttachedProperties;
using FrostNova.Core.Wpf.Controls.Dialogs;
using FrostNova.Core.Wpf.Messages;
using FrostNova.Core.Wpf.Models.DataGrdExtCols;

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
        private NotifyCollection<DataGridSampleModel2> _samples2 = new NotifyCollection<DataGridSampleModel2>();

        [ObservableProperty]
        private NotifyCollection<ExtensionColumnInfo> _extCols = new NotifyCollection<ExtensionColumnInfo>();

        [ObservableProperty]
        private NotifyCollection<ColumnType> _colTypes = new NotifyCollection<ColumnType>(Enum.GetValues(typeof(ColumnType)).Cast<ColumnType>());

        [ObservableProperty]
        private ColumnType _selectedColType;

        [ObservableProperty]
        private List<object> _selectedSamples = new List<object>();

        [ObservableProperty]
        private NotifyCollection<int> _selectedIndices = new NotifyCollection<int>();

        [ObservableProperty]
        private AllowedCharTypes _testCharType = AllowedCharTypes.All;

        [ObservableProperty]
        private bool _canLineBreak;

        [ObservableProperty]
        private bool _isColumn2ForceCollapsed = true;

        partial void OnIsColumn2ForceCollapsedChanged(bool value)
        {
            OnPropertyChanged(nameof(Column2ForcedVisibility));
        }

        // trueなら強制非表示、falseならユーザー設定に従う（null = ForcedVisibility解除）
        public System.Windows.Visibility? Column2ForcedVisibility
            => _isColumn2ForceCollapsed ? System.Windows.Visibility.Collapsed : null;

        [RelayCommand]
        private void ShowColorDialog()
        {
            SelectedColor = ColorSelectDialog.Show(SelectedColor);
        }

        protected override void InitializeOnExecute()
        {
            base.InitializeOnExecute();
            Date = new DateTime(DateTime.Today.Year, DateTime.Today.Month, DateTime.Today.Day, 9, 30, 0);
            Span = TimeSpan.FromHours(9).Add(TimeSpan.FromMinutes(30));
            Samples.Clear();
            Samples.Add(new DataGridSampleModel1() { ColumnValue = "aaaa", NumValue = 20, MultiLineText = "aaa\nbbbb" });
            Samples.Add(new DataGridSampleModel1() { ColumnValue = "bbbb", NumValue = 40 });
            Samples.Add(new DataGridSampleModel1() { ColumnValue = "cccc", NumValue = 60 });
            Samples.Add(new DataGridSampleModel1() { ColumnValue = "cccc", NumValue = 80 });
            Samples.Add(new DataGridSampleModel1() { ColumnValue = "dddd", NumValue = 100 });


            Samples2.Clear();
            Samples2.Add(new DataGridSampleModel2() { ColumnValue = "aaaa" });
            Samples2.Add(new DataGridSampleModel2() { ColumnValue = "bbbb" });
            Samples2.Add(new DataGridSampleModel2() { ColumnValue = "cccc" });
            Samples2.Add(new DataGridSampleModel2() { ColumnValue = "dddd" });

            MessageBox = new FrostNovaMessageBox();



        }

        [RelayCommand]
        private void SelectedItemsChanged(List<object> selected)
        {

            using (var _ = new OperationScope(SelectedIndices))
            {
                SelectedIndices.Clear();
                foreach (var item in selected)
                {
                    var index = Samples.IndexOf((item as DataGridSampleModel1)!);
                    SelectedIndices.Add(index + 1);
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

        [RelayCommand]
        private void AddCustomColumn()
        {
            var newCol = new ExtensionColumnInfo() { HeaderText = $"CustomCol{ExtCols.Count}", UniqueKey = $"CustomCol{ExtCols.Count}", };

            newCol.Type = SelectedColType;
            newCol.Command = RunCustomColumnCommand;

            newCol.Options = new List<ComboBoxOption>()
            {
                new ComboBoxOption() { Value = "1", Label = "Vla1" },
                new ComboBoxOption() { Value = "2", Label = "Vla2" },
                new ComboBoxOption() { Value = "3", Label = "Vla3" },
            };

            foreach (var item in Samples2)
            {
               (string key, object value )pair =  newCol.Type switch
                {
                    ColumnType.text => (newCol.UniqueKey, string.Empty),
                    ColumnType.checkBox => (newCol.UniqueKey, false),
                    ColumnType.date => (newCol.UniqueKey, DateTime.Today),
                    ColumnType.comboBox => (newCol.UniqueKey,newCol.Options.First().Value),
                    ColumnType.numeric => (newCol.UniqueKey, 0m),
                    ColumnType.button => (newCol.UniqueKey, string.Empty),
                    _ => (newCol.UniqueKey, string.Empty)

                };
                item.CustomItems.Add(pair.key, pair.value);
            }

            ExtCols.Add(newCol);
        }

        [RelayCommand]
        private void ApplyTo()
        {
            foreach (var item in Samples2)
            {
                foreach (var item2 in ExtCols)
                {
                    if (item2.Type == ColumnType.text)
                    {
                        item.CustomItems[item2.UniqueKey] = "aaa";
                    }
                }



            }
        }

        [RelayCommand]
        private void RunCustomColumn(object parameter)
        {
            if (parameter is DataGridSampleModel2 colInfo)
            {
                MessageBox?.Show($"Command executed for {colInfo.ColumnValue}");
            }

        }
    }
}
