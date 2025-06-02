using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace FrostNova.Core.Wpf.Messages
{
    public interface IWpfMessageBox
    {
        public MessageBoxResult Show(string messageBoxText, string cpation = "", MessageBoxButton button = MessageBoxButton.OK, MessageBoxImage icon = MessageBoxImage.None, MessageBoxResult defaultResult = MessageBoxResult.OK, MessageBoxOptions options = MessageBoxOptions.None, bool showCenterScreen = false);
        public MessageBoxResult Show(Window owner, string messageBoxText, string cpation = "", MessageBoxButton button = MessageBoxButton.OK, MessageBoxImage icon = MessageBoxImage.None, MessageBoxResult defaultResult = MessageBoxResult.OK, MessageBoxOptions options = MessageBoxOptions.None);


    }
}
