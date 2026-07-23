using FrostNova.Core.Wpf.ViewModelBases;
using FrostNova.Core.Wpf.ViewServices;
using FrostNova.Core.Wpf.Views;
using System;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace FrostNova.Core.Wpf.AsyncHelpers
{
    /// <summary>
    /// 実行時(ユーザー対話環境)用の <see cref="ICancelableProcessRunner"/>。
    /// 待機が一定時間を超えたときだけ <see cref="ProgressWindow"/> をモーダル表示し、
    /// キャンセル押下でトークンのキャンセルと強制終了フックを実行する。
    /// </summary>
    public class CancelableProcessRunner : ICancelableProcessRunner
    {
        /// <inheritdoc />
        public CancelableRunResult Run(CancelableRunRequest request)
        {
            ArgumentNullException.ThrowIfNull(request);

            var cts = request.CancellationTokenSource ?? new CancellationTokenSource();
            bool ownCts = request.CancellationTokenSource is null;

            Exception? failure = null;
            bool canceled = false;

            // UIスレッドを塞がないよう、実処理はUIスレッド外で走らせる。
            var workTask = Task.Run(async () =>
            {
                try
                {
                    await request.Work(cts.Token).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (cts.IsCancellationRequested)
                {
                    canceled = true;
                }
                catch (Exception ex)
                {
                    failure = ex;
                }
            });

            try
            {
                if (Environment.UserInteractive && Application.Current != null)
                {
                    // ダイアログは初期不可視で開き、ShowDelayMs を超えて実行中のときだけ姿を現す。
                    // ShowDialog はメッセージをポンプするので、待機中も UI は固まらない。
                    // 処理完了時に VM 側がウィンドウを自動で閉じるため、ここは同期的に戻る。
                    var vm = CreateViewModel(request, cts, workTask);
                    AbstractWindowService.ShowDialogWindow(
                        vm,
                        typeof(ProgressWindow),
                        windowSerVice: null,
                        ownerWindow: null,
                        location: WindowStartupLocation.CenterOwner);
                }

                // 非対話環境ルート / ダイアログを閉じた後の確定待ち。
                workTask.Wait();
            }
            finally
            {
                if (ownCts)
                {
                    cts.Dispose();
                }
            }

            if (failure != null)
            {
                // 元のスタックトレースを保ったまま再送出する。
                ExceptionDispatchInfo.Capture(failure).Throw();
            }

            return (canceled || cts.IsCancellationRequested)
                ? CancelableRunResult.Canceled
                : CancelableRunResult.Completed;
        }

        private static ProgressWindowViewModel CreateViewModel(
            CancelableRunRequest request, CancellationTokenSource cts, Task workTask)
        {
            var vm = new ProgressWindowViewModel();

            if (request.Progress != null)
            {
                vm.ProgressInfo = request.Progress;
            }
            else
            {
                vm.ProgressInfo.ProgressText = request.Message;
                vm.ProgressInfo.IsIndeterminate = true;
            }

            vm.ProgressInfo.CanCancel = request.CanCancel;
            vm.CancellationToken = cts;
            vm.ForceCancelAction = request.OnForceCancel;
            vm.CompletionTask = workTask;
            vm.ShowDelayMs = request.ShowDelayMs;
            vm.MinVisibleMs = request.MinVisibleMs;

            return vm;
        }
    }
}
