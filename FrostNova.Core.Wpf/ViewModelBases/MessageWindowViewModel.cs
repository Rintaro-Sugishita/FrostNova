using FrostNova.Core.Wpf.Messages;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace FrostNova.Core.Wpf.ViewModelBases
{
    public partial class MessageWindowViewModel : DialogWindowViewModelBase
    {
        [ObservableProperty]
        private string _TitleText = string.Empty;

        [ObservableProperty]
        private string _Message = string.Empty;

        [ObservableProperty]

        private MessageBoxResult _result = MessageBoxResult.None;

        [ObservableProperty]

        private ObservableCollection<ButtonBindClass> _Buttons = new ObservableCollection<ButtonBindClass>();

        [ObservableProperty]

        private ICommand? _ButtonClickCommand;


        [ObservableProperty]

        private Brush? _iconBackgroundBrush;

        [ObservableProperty]
        private string _IconBackgroundText = string.Empty;

        [ObservableProperty]
        private string _IconForegroundText = string.Empty;


        [RelayCommand]
        private void OnKey()
        {
            if ((Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl)) && Keyboard.IsKeyDown(Key.C))
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("---------------------------");
                sb.AppendLine(TitleText ?? "");
                sb.AppendLine("---------------------------");
                sb.AppendLine(Message ?? "");
                Clipboard.SetText(sb.ToString());
            }
        }

        [RelayCommand]
        private void ClickButton(MessageBoxResult? result)
        {
            if (result == null)
            {
                Result = MessageBoxResult.None;
            }
            else
            {
                Result = result.Value;
            }
            RequestClose?.Invoke();

        }

    }
}
