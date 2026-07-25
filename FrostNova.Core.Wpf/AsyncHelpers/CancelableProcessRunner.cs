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
    /// 実行中の進捗は <c>Work</c> から報告でき、UIスレッドへマーシャリングして反映する。
    /// </summary>
    public class CancelableProcessRunner : ICancelableProcessRunner
    {
        /// <inheritdoc />
        public CancelableRunResult Run(CancelableRunRequest request)
        {
            ArgumentNullException.ThrowIfNull(request);

            var cts = request.CancellationTokenSource ?? new CancellationTokenSource();
            bool ownCts = request.CancellationTokenSource is null;

            // ダイアログに束縛する進捗状態。Work からの報告はここへ反映する。
            var progressInfo = new ProgressInfo
            {
                ProgressText = request.Message,
                IsIndeterminate = request.IsIndeterminate,
                CanCancel = request.CanCancel,
            };

            // このスレッド(UIスレッド)で生成することで、Report がUIスレッドへ
            // マーシャリングされ、別スレッドの Work から安全に進捗更新できる。
            var reporter = new Progress<ProgressReport>(r => ApplyReport(progressInfo, r));

            Exception? failure = null;
            bool canceled = false;

            // UIスレッドを塞がないよう、実処理はUIスレッド外で走らせる。
            var workTask = Task.Run(async () =>
            {
                try
                {
                    await request.Work(reporter, cts.Token).ConfigureAwait(false);
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
                    var vm = CreateViewModel(request, cts, workTask, progressInfo);
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

        /// <summary>進捗報告を <see cref="ProgressInfo"/> に反映する（指定項目のみ）。</summary>
        private static void ApplyReport(ProgressInfo info, ProgressReport r)
        {
            if (r.Title != null) info.Title = r.Title;
            if (r.Message != null) info.ProgressText = r.Message;
            if (r.Minimum.HasValue) info.Minimum = r.Minimum.Value;
            if (r.Maximum.HasValue) info.Maximum = r.Maximum.Value;
            if (r.Value.HasValue) info.Value = r.Value.Value;
            if (r.IsIndeterminate.HasValue) info.IsIndeterminate = r.IsIndeterminate.Value;
        }

        private static ProgressWindowViewModel CreateViewModel(
            CancelableRunRequest request, CancellationTokenSource cts, Task workTask, ProgressInfo progressInfo)
        {
            var vm = new ProgressWindowViewModel
            {
                ProgressInfo = progressInfo,
                CancellationToken = cts,
                ForceCancelAction = request.OnForceCancel,
                CompletionTask = workTask,
                ShowDelayMs = request.ShowDelayMs,
                MinVisibleMs = request.MinVisibleMs,
            };

            return vm;
        }
    }
}
