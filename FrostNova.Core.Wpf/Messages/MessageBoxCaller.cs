using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace FrostNova.Core.Wpf.Messages
{
    public class MessageBoxCaller : IWpfMessageBox
    {


        public MessageBoxResult Show(string messageBoxText, string caption = "", MessageBoxButton button = MessageBoxButton.OK, MessageBoxImage icon = MessageBoxImage.None, MessageBoxResult defaultResult = MessageBoxResult.OK, MessageBoxOptions options = MessageBoxOptions.None, bool doCenterScreen = false)
        {
            if (Environment.UserInteractive)
            {
                return MessageBox.Show(messageBoxText, caption, button, icon, defaultResult, options);
            }
            else
            {
                return defaultResult;
            }
        }

        public MessageBoxResult Show(Window owner, string messageBoxText, string caption = "", MessageBoxButton button = MessageBoxButton.OK, MessageBoxImage icon = MessageBoxImage.None, MessageBoxResult defaultResult = MessageBoxResult.OK, MessageBoxOptions options = MessageBoxOptions.None)
        {

            if (Environment.UserInteractive)
            {
                return MessageBox.Show(owner, messageBoxText, caption, button, icon, defaultResult, options);
            }
            else
            {
                return defaultResult;
            }
        }

    }
}
