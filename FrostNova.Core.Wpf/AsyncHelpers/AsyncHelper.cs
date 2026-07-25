using FrostNova.Core.Wpf.ViewServices;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace FrostNova.Core.Wpf.AsyncHelpers
{
    public class AsyncHelper
    {
        /// <summary>
        /// 進捗表示付きでキャンセル可能な非同期処理を実行する。
        /// 実体は <see cref="ICancelableProcessRunner"/>（<see cref="AbstractWindowService.GetCancelableProcessRunner"/>）へ委譲する。
        /// <para>
        /// <paramref name="action"/> は第1引数の <see cref="IProgress{T}"/> に
        /// <see cref="ProgressReport"/> を報告することで、実行中にメッセージやプログレスバーを更新できる。
        /// </para>
        /// </summary>
        public static CancelableRunResult RunAsyncProcess(
            Func<IProgress<ProgressReport>, CancellationToken, Task> action,
            string message = "",
            CancellationTokenSource? token = null,
            bool isIndeterminate = true)
        {
            var runner = AbstractWindowService.GetCancelableProcessRunner();
            return runner.Run(new CancelableRunRequest
            {
                Work = action,
                Message = message,
                CancellationTokenSource = token,
                IsIndeterminate = isIndeterminate,
            });
        }
    }
}
