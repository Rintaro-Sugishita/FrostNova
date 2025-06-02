using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace FrostNova.Core.Wpf.Messages
{
    internal class MessageBoxForTest : IWpfMessageBox
    {
        public MessageBoxResult Show(string messageBoxText, string caption = "", MessageBoxButton button = MessageBoxButton.OK, MessageBoxImage icon = MessageBoxImage.None, MessageBoxResult defaultResult = MessageBoxResult.OK, MessageBoxOptions options = MessageBoxOptions.None, bool doCenterScreen = false)
        {
            
            return TestResult ?? defaultResult;
        }

        public MessageBoxResult Show(Window owner, string messageBoxText, string caption = "", MessageBoxButton button = MessageBoxButton.OK, MessageBoxImage icon = MessageBoxImage.None, MessageBoxResult defaultResult = MessageBoxResult.OK, MessageBoxOptions options = MessageBoxOptions.None)
        {
            return TestResult ?? defaultResult;
        }

        public MessageBoxForTest()
        {

        }

        public MessageBoxForTest(MessageBoxResult defaultResult)
        {
            TestResult = defaultResult;            
        }

        public MessageBoxResult? TestResult { get; set; } = null;

    }
}
