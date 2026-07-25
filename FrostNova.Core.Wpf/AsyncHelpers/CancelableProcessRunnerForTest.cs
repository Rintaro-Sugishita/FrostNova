using System;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;

namespace FrostNova.Core.Wpf.AsyncHelpers
{
    /// <summary>
    /// テスト用の <see cref="ICancelableProcessRunner"/>。ダイアログを表示せず処理だけを実行する。
    /// <see cref="SimulateCancel"/> を true にすると、処理開始前にキャンセルされた挙動を再現する。
    /// </summary>
    public class CancelableProcessRunnerForTest : ICancelableProcessRunner
    {
        public CancelableProcessRunnerForTest() { }

        public CancelableProcessRunnerForTest(bool simulateCancel)
        {
            SimulateCancel = simulateCancel;
        }

        /// <summary>true の場合、処理を実行せずキャンセル扱いにする（強制終了フックも呼ぶ）。</summary>
        public bool SimulateCancel { get; set; }

        /// <inheritdoc />
        public CancelableRunResult Run(CancelableRunRequest request)
        {
            ArgumentNullException.ThrowIfNull(request);

            var cts = request.CancellationTokenSource ?? new CancellationTokenSource();
            bool ownCts = request.CancellationTokenSource is null;

            try
            {
                if (SimulateCancel)
                {
                    cts.Cancel();
                    request.OnForceCancel?.Invoke();
                    return CancelableRunResult.Canceled;
                }

                try
                {
                    // テストなのでダイアログ無し・進捗レポートは無視して同期的に完了させる。
                    request.Work(NullProgress<ProgressReport>.Instance, cts.Token).GetAwaiter().GetResult();
                }
                catch (OperationCanceledException) when (cts.IsCancellationRequested)
                {
                    return CancelableRunResult.Canceled;
                }

                return cts.IsCancellationRequested
                    ? CancelableRunResult.Canceled
                    : CancelableRunResult.Completed;
            }
            finally
            {
                if (ownCts)
                {
                    cts.Dispose();
                }
            }
        }
    }
}
