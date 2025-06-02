using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace FrostNova.Core.Wpf.ViewModelBases
{
    public partial class ProgressWindowViewModel : DialogWindowViewModelBase
    {

        [ObservableProperty]
        private Func<ProgressInfo, Task>? _action;

        internal Progress<ProgressInfo> _progress = new Progress<ProgressInfo>();

        [ObservableProperty]
        private ProgressInfo _progressInfo;

        [ObservableProperty]
        [property: NotifyPropertyChangedFor(nameof(CanCancel))]
        private CancellationTokenSource? _cancellationToken;

        [ObservableProperty]
        private Exception? _exception;


        [RelayCommand(CanExecute = nameof(CanCancel))]
        private void Cancel()
        {
            CancellationToken?.Cancel();
        }

        public bool CanCancel { get => CancellationToken != null; }


        public ProgressWindowViewModel()
        {
            _progressInfo = new ProgressInfo(_progress);
        }


        public DispatcherOperation RunTask()
        {
            //Exception? innerException = null;

            //await Task.Run(async () =>
            //{
            //    try
            //    {
            //        if (Action != null)
            //        {
            //            //重い処理は await Taskで実行するとUIを固めずに処理できる
            //            //UI更新はProgressのReportからする
            //            await Action.Invoke(ProgressInfo);
            //        }
            //    }
            //    catch (Exception ex)
            //    {
            //        Exception = ex;
            //    }
            //    finally
            //    {
            //        Application.Current.Dispatcher.Invoke(() =>
            //        {
            //            CloseCommandExecute();
            //        });
            //    }

            //});
            return Application.Current.Dispatcher.InvokeAsync(() =>
              {
                  try
                  {
                      if (Action != null)
                      {
                          //重い処理は await Taskで実行するとUIを固めずに処理できる
                          //UI更新はProgressのReportからする
                          Action.Invoke(ProgressInfo);
                      }
                  }
                  catch (Exception ex)
                  {
                      Exception = ex;
                  }
                  finally
                  {
                      Close();
                  }

              });
        }




    }
}
