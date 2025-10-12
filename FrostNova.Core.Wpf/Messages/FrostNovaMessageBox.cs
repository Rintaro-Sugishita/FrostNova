
using FrostNova.Core.Wpf.ViewModelBases;
using FrostNova.Core.Wpf.Views;
using FrostNova.Core.Wpf.ViewServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;


namespace FrostNova.Core.Wpf.Messages
{
    public class FrostNovaMessageBox : IWpfMessageBox
    {

        public Window? OwnerWindow { get; set; }

        public FrostNovaMessageBox() { }

        public FrostNovaMessageBox(Window element)
        {
            OwnerWindow = element;
        }

        public ViewModelBase? VmBase { get; set; }

        public MessageBoxResult Show(string messageBoxText, string caption = "", MessageBoxButton button = MessageBoxButton.OK, MessageBoxImage icon = MessageBoxImage.None, MessageBoxResult defaultResult = MessageBoxResult.OK, MessageBoxOptions options = MessageBoxOptions.None, bool doCenterScreen = false)
        {

            if (doCenterScreen)
            {
                return Show(messageBoxText, caption, button, icon, defaultResult);

            }
            else
            {
                if (this.OwnerWindow != null)
                {
                    return Show(this.OwnerWindow, messageBoxText, caption, button, icon, defaultResult);
                }
                else
                {
                    return Show(Application.Current.MainWindow, messageBoxText, caption, button, icon, defaultResult);
                }
            }

        }

        public MessageBoxResult Show(Window owner, string messageBoxText, string caption = "", MessageBoxButton button = MessageBoxButton.OK, MessageBoxImage icon = MessageBoxImage.None, MessageBoxResult defaultResult = MessageBoxResult.OK, MessageBoxOptions options = MessageBoxOptions.None)
        {
            return Show(owner, messageBoxText, caption, button, icon, defaultResult);
        }


        public static MessageBoxResult Show(string messageBoxText, string caption = "", MessageBoxButton button = MessageBoxButton.OK, MessageBoxImage icon = MessageBoxImage.None, MessageBoxResult defaultResult = MessageBoxResult.OK)
        {
            if (Environment.UserInteractive)
            {
                var w = CreateMessageVm(messageBoxText, caption, button, icon, defaultResult);
                var r = AbstractWindowService.ShowDialogWindow(w, typeof(MessageWindow), w.WindowService!, null, WindowStartupLocation.CenterScreen);
                Application.Current.MainWindow.Activate();
                return w?.Result ?? MessageBoxResult.None;
            }
            else
            {
                return defaultResult;
            }
        }
        public static MessageBoxResult Show(Window owner, string messageBoxText, string caption = "", MessageBoxButton button = MessageBoxButton.OK, MessageBoxImage icon = MessageBoxImage.None, MessageBoxResult defaultResult = MessageBoxResult.OK)
        {
            if (Environment.UserInteractive)
            {
                var w = CreateMessageVm(messageBoxText, caption, button, icon, defaultResult);
                var r = AbstractWindowService.ShowDialogWindow(w, typeof(MessageWindow), w.WindowService!, owner, WindowStartupLocation.CenterOwner);
                owner.Activate();

                return w?.Result ?? MessageBoxResult.None;
            }
            else
            { return defaultResult; }
        }


        private static MessageWindowViewModel CreateMessageVm(string messageBoxText, string caption = "", MessageBoxButton button = MessageBoxButton.OK, MessageBoxImage icon = MessageBoxImage.None, MessageBoxResult defaultResult = MessageBoxResult.OK)
        {
            var w = new MessageWindowViewModel();
            w.TitleText = caption;
            w.Message = messageBoxText;

            switch (icon)
            {
                case MessageBoxImage.None:
                    break;
                case MessageBoxImage.Error:
                    w.IconBackgroundBrush = new SolidColorBrush(Color.FromRgb(0xDD, 0, 0));
                    w.IconBackgroundText = "\uF136";
                    w.IconForegroundText = "\uF13D";
                    break;
                case MessageBoxImage.Question:
                    w.IconBackgroundBrush = new SolidColorBrush(Color.FromRgb(0x0, 0x6C, 0xDF));
                    w.IconBackgroundText = "\uF136";
                    w.IconForegroundText = "\uF142";
                    break;
                case MessageBoxImage.Warning:
                    w.IconBackgroundBrush = new SolidColorBrush(Color.FromRgb(0xFE, 0xB0, 0));
                    w.IconBackgroundText = "\uF139";
                    w.IconForegroundText = "\uF13B";
                    break;
                case MessageBoxImage.Information:
                    w.IconBackgroundBrush = new SolidColorBrush(Color.FromRgb(0x0, 0x6C, 0xDF));
                    w.IconBackgroundText = "\uF136";
                    w.IconForegroundText = "\uF13C";
                    break;
                default:
                    break;
            }

            if (button == MessageBoxButton.OK)
            {
                w.Buttons.Add(new ButtonBindClass() { ButtonContent = "OK", MessageBoxResult = MessageBoxResult.OK, IsDefaultButton = true });
            }
            else if (button == MessageBoxButton.OKCancel)
            {
                w.Buttons.Add(new ButtonBindClass() { ButtonContent = "OK", MessageBoxResult = MessageBoxResult.OK, IsDefaultButton = true });
                w.Buttons.Add(new ButtonBindClass() { ButtonContent = "Cancel", MessageBoxResult = MessageBoxResult.Cancel, IsDefaultButton = defaultResult == MessageBoxResult.Cancel, IsCancelButton = true });
            }
            else if (button == MessageBoxButton.YesNo)
            {
                w.Buttons.Add(new ButtonBindClass() { ButtonContent = "Yes(_Y)", MessageBoxResult = MessageBoxResult.Yes, IsDefaultButton = defaultResult == MessageBoxResult.Yes });
                w.Buttons.Add(new ButtonBindClass() { ButtonContent = "No (_N)", MessageBoxResult = MessageBoxResult.No, IsDefaultButton = defaultResult == MessageBoxResult.No });
            }
            else if (button == MessageBoxButton.YesNoCancel)
            {
                w.Buttons.Add(new ButtonBindClass() { ButtonContent = "Yes (_Y)", MessageBoxResult = MessageBoxResult.Yes, IsDefaultButton = defaultResult == MessageBoxResult.Yes });
                w.Buttons.Add(new ButtonBindClass() { ButtonContent = "No (_N)", MessageBoxResult = MessageBoxResult.No, IsDefaultButton = defaultResult == MessageBoxResult.No });
                w.Buttons.Add(new ButtonBindClass() { ButtonContent = "Cancel (_C)", MessageBoxResult = MessageBoxResult.Cancel, IsDefaultButton = defaultResult == MessageBoxResult.Cancel, IsCancelButton = true });
            }
            else
            {
                w.Buttons.Add(new ButtonBindClass() { ButtonContent = "OK", MessageBoxResult = MessageBoxResult.OK, IsDefaultButton = true });
            }

            return w;
        }






    }



}
