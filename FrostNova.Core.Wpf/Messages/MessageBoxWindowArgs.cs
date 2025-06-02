using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows;
using System.Windows.Media;
using FrostNova.Core.Wpf.ViewModelBases;

namespace FrostNova.Core.Wpf.Messages
{
    public class MessageBoxWindowArgs : DialogWindowViewModelBase
    {
        public MessageBoxWindowArgs() 
        {
            Result = MessageBoxResult.None;
            Buttons = new ObservableCollection<ButtonBindClass>();
            TitleText = string.Empty;
            Message = string.Empty;
        }



    }
}
