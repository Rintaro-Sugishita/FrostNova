using CommunityToolkit.Mvvm.Messaging;
using FrostNova.Core.Wpf.Messages;
using FrostNova.Core.Wpf.ViewModelBases;
using FrostNova.Core.Wpf.Views;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace FrostNova.Core.Wpf.ViewServices
{
    public abstract class AbstractWindowService : IWindowService
    {

        public AbstractWindowService()
        {
            AddDefaultViews();
        }

        public bool? ShowDialogWindow<T>(T vm) where T : DialogWindowViewModelBase
        {
            if (WindowList.TryGetValue(typeof(T), out Type? windowType))
            {
                return ShowDialogWindow(vm, windowType, this);
            }
            return null;
        }

        public static bool? ShowDialogWindow<T>(T vm, Type windowType, IWindowService? windowSerVice, Window? ownerWindow = null, WindowStartupLocation? location = null) where T : DialogWindowViewModelBase
        {
            var window = windowType.Assembly.CreateInstance(windowType.FullName!) as Window;
            if (window == null) { return null; }

            SetVm(vm, window, windowSerVice, ownerWindow, location);
            window.DataContext = vm;

            return window.ShowDialog();
        }



        public Window? ShowWindow<T>(T vm) where T : ViewModelBase
        {
            if (WindowList.TryGetValue(typeof(T), out Type? windowType))
            {
                return ShowWindow(vm, windowType, this);
            }
            return null;
        }

        public static Window? ShowWindow<T>(T vm, Type windowType, IWindowService? windowSerVice, Window? ownerWindow = null, WindowStartupLocation? location = null) where T : ViewModelBase
        {
            var window = windowType.Assembly.CreateInstance(windowType.FullName!) as Window;
            if (window == null) { return null; }

            SetVm(vm, window, windowSerVice, ownerWindow, location);

            window.DataContext = vm;
            window.Show();
            return window;
        }

        protected Dictionary<Type, Type> WindowList { get; set; } = [];


        protected virtual void AddDefaultViews()
        {
            WindowList.Add(typeof(ProgressWindowViewModel), typeof(ProgressWindow));
        }

        public static Func<IWpfMessageBox> GetMessageBox = () =>
        {
            return new FrostNovaMessageBox();
        };



        protected static void SetVm(ViewModelBase vm, Window subWindow, IWindowService? windowService, Window? ownerWindow = null, WindowStartupLocation? location = null)
        {
            subWindow.DataContext = vm;

            if (ownerWindow != null) subWindow.Owner = ownerWindow;
            if (location != null) subWindow.WindowStartupLocation = location.Value;

            vm.WindowService = windowService;
            vm.MessageBox = GetMessageBox();


            if (vm is DialogWindowViewModelBase dialogVm)
            {
                dialogVm.RequestClose += delegate
                {
                    if (dialogVm.OnOkClosing())
                    {
                        subWindow.DialogResult = true;
                        dialogVm.OnClosed();
                        subWindow.Close();
                    }
                };


                dialogVm.RequestCancelClose += delegate
                {
                    subWindow.DialogResult = false;
                    dialogVm.OnClosed();
                    subWindow.Close();
                };



            }
            if (vm is FloatingWindowViewModelBase floatingVm)
            {

                subWindow.Loaded += (object sender, RoutedEventArgs e) =>
                {
                    //floatingVm.OnWindowLoaded(sender as Window);
                    floatingVm.OnLoaded();
                };

                subWindow.Closing += (object? sender, CancelEventArgs e) =>
                {
                    floatingVm.OnClosing(e);
                };

                subWindow.Closed += (object? sender, EventArgs e) =>
                {
                    floatingVm.OnClosed();
                };

                if (vm is not DialogWindowViewModelBase)
                {
                    floatingVm.RequestClose += delegate
                    {
                        subWindow.Close();
                    };
                }



            }

        }



    }
}
