using FrostNova.Core.Wpf.ViewModelBases;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace FrostNova.Core.Wpf.AsyncHelpers
{
    public class AsyncHelper
    {

        public static void RunAsyncProcess<T>(Func<ProgressInfo, Task> action, CancellationTokenSource token, Dispatcher? dispatcher = null)
        {
            //if (dispatcher == null)
            //{
            //    dispatcher = Application.Current.Dispatcher;
            //}

            //return dispatcher.InvokeAsync<T>(action);


            var vm = new ProgressWindowViewModel();
            vm.Action = action;
            vm.CancellationToken = token;


            var task = vm.RunTask();

            if (Environment.UserInteractive)
            {
                var result = vm.WindowService?.ShowDialogWindow(vm);
            }

            task.Wait();

            if (vm.Exception != null)
            {
                ExceptionDispatchInfo.Capture(vm.Exception).Throw();
            }



        }




    }
}
