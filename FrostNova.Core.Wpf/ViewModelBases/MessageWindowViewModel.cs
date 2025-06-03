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

namespace FrostNova.Core.Wpf.ViewModelBases
{
    public partial class MessageWindowViewModel : DialogWindowViewModelBase
    {
        [ObservableProperty]
        private string _TitleText = string.Empty;

        [ObservableProperty]
        private string _Message = string.Empty;

        [ObservableProperty]

        public MessageBoxResult _result = MessageBoxResult.None;

        [ObservableProperty]

        private ObservableCollection<ButtonBindClass> _Buttons = new ObservableCollection<ButtonBindClass>();

        [ObservableProperty]

        public ICommand? _ButtonClickCommand;

        [ObservableProperty]

        private Color _IconBackGroundColor;

        [ObservableProperty]

        public Brush? _iconBackgroundBrush;

        [ObservableProperty]
        private string _IconBackgroundText = string.Empty;

        [ObservableProperty]
        private string _IconForegroundText = string.Empty;

    }
}
