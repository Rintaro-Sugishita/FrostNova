using FrostNova.Core.Wpf.ViewModelBases;
using FrostNova.Core.Wpf.ViewServices;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace FrostNova.Core.Wpf.AsyncHelpers
{
    public class AsyncHelper
    {
        /// <summary>
        /// 進捗表示付きでキャンセル可能な非同期処理を実行する。
        /// 実体は <see cref="ICancelableProcessRunner"/>（<see cref="AbstractWindowService.GetCancelableProcessRunner"/>）へ委譲する。
        /// </summary>
        public static void RunAsyncProcess<T>(Func<ProgressInfo, Task> action, CancellationTokenSource token, Dispatcher? dispatcher = null)
        {
            var progress = new ProgressInfo();

            var runner = AbstractWindowService.GetCancelableProcessRunner();
            runner.Run(new CancelableRunRequest
            {
                Work = _ => action(progress),
                Progress = progress,
                CancellationTokenSource = token,
            });
        }
    }
}
