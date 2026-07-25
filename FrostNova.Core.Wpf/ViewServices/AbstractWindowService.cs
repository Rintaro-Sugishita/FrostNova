using CommunityToolkit.Mvvm.Messaging;
using FrostNova.Core.Wpf.AsyncHelpers;
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
            // オーナーはウィンドウ生成より「前」に解決する。
            // WPF は最初に生成された Window を自動的に Application.MainWindow に設定するため、
            // 生成後に解決すると「これから表示するダイアログ自身」をオーナーに選んでしまい、
            // 自己オーナー化(例外)や自身の MainWindow 化を招く。
            ownerWindow ??= GetDefaultOwnerWindow();

            var app = Application.Current;
            var previousMain = app?.MainWindow;

            var window = windowType.Assembly.CreateInstance(windowType.FullName!) as Window;
            if (window == null) { return null; }

            // 生成時に MainWindow が自分自身へ置き換わっていたら元に戻す。
            // これを怠ると ShutdownMode.OnMainWindowClose のアプリでは、
            // ダイアログを閉じた瞬間にアプリ全体が終了してしまう。
            if (app != null && ReferenceEquals(app.MainWindow, window))
            {
                app.MainWindow = previousMain!;
            }

            // オーナー未指定なら、CenterOwner では位置が不定になるため画面中央に出す。
            if (location == null)
            {
                location = ownerWindow != null
                    ? WindowStartupLocation.CenterOwner
                    : WindowStartupLocation.CenterScreen;
            }

            SetVm(vm, window, windowSerVice, ownerWindow, location);
            window.DataContext = vm;

            return window.ShowDialog();
        }

        /// <summary>
        /// ダイアログの既定オーナー(アクティブウィンドウ、無ければ MainWindow)を返す。
        /// </summary>
        private static Window? GetDefaultOwnerWindow()
        {
            var app = Application.Current;
            if (app == null) return null;

            // オーナーにできるのは「既に表示済み(IsLoaded かつ IsVisible)」のウィンドウのみ。
            // 未表示ウィンドウをオーナーに設定すると ShowDialog が例外を投げる。
            var shown = app.Windows.OfType<Window>()
                                   .Where(w => w.IsLoaded && w.IsVisible)
                                   .ToList();

            var main = app.MainWindow;
            if (main != null && (!main.IsLoaded || !main.IsVisible)) main = null;

            return shown.FirstOrDefault(w => w.IsActive) ?? main ?? shown.FirstOrDefault();
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

        /// <summary>
        /// キャンセル可能な非同期処理を実行するランナーの生成関数。
        /// テスト時は <see cref="CancelableProcessRunnerForTest"/> を返すよう差し替える。
        /// </summary>
        public static Func<ICancelableProcessRunner> GetCancelableProcessRunner = () =>
        {
            return new CancelableProcessRunner();
        };



        protected static void SetVm(ViewModelBase vm, Window subWindow, IWindowService? windowService, Window? ownerWindow = null, WindowStartupLocation? location = null)
        {
            subWindow.DataContext = vm;

            // 自分自身をオーナーにすると例外。未表示ウィンドウもオーナーにできない。
            if (ownerWindow != null
                && !ReferenceEquals(ownerWindow, subWindow)
                && ownerWindow.IsLoaded)
            {
                subWindow.Owner = ownerWindow;
            }
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
