using System;
using System.Threading;
using System.Threading.Tasks;

namespace FrostNova.Core.Wpf.AsyncHelpers
{
    /// <summary>
    /// <see cref="ICancelableProcessRunner.Run"/> の実行結果。
    /// </summary>
    public enum CancelableRunResult
    {
        /// <summary>処理が最後まで完了した。</summary>
        Completed,

        /// <summary>ユーザーによってキャンセル（強制終了）された。</summary>
        Canceled,
    }

    /// <summary>
    /// キャンセル可能な非同期処理を、待機中だけ「実行中...」ダイアログ付きで実行するための共通パーツ。
    /// <para>
    /// <see cref="CancelableRunRequest.OnForceCancel"/> を指定すると、協調キャンセル
    /// （<see cref="CancellationToken"/>）に応じない外部プロセスなどを強制終了できる。
    /// </para>
    /// <para>
    /// <c>IWpfMessageBox</c> と同様に「実物(<see cref="CancelableProcessRunner"/>)」と
    /// 「テスト用(<see cref="CancelableProcessRunnerForTest"/>)」を差し替えて使う。
    /// </para>
    /// </summary>
    public interface ICancelableProcessRunner
    {
        /// <summary>
        /// <paramref name="request"/> の処理を実行し、完了またはキャンセルまで同期的に待機する。
        /// </summary>
        CancelableRunResult Run(CancelableRunRequest request);
    }

    /// <summary>
    /// <see cref="ICancelableProcessRunner.Run"/> への入力。
    /// </summary>
    public sealed class CancelableRunRequest
    {
        /// <summary>
        /// 実際に実行する処理。UIスレッドを塞がないようUIスレッド外で実行される。
        /// 渡される <see cref="CancellationToken"/> がキャンセルされたら、
        /// できるだけ速やかに <see cref="OperationCanceledException"/> で抜けること。
        /// </summary>
        public required Func<CancellationToken, Task> Work { get; init; }

        /// <summary>「実行中...」ダイアログに表示する本文。</summary>
        public string Message { get; init; } = "";

        /// <summary>
        /// キャンセル押下時に、協調キャンセルに続けて呼ばれる強制終了処理。
        /// 例: <c>() =&gt; process.Kill(entireProcessTree: true)</c>。
        /// </summary>
        public Action? OnForceCancel { get; init; }

        /// <summary>
        /// 外部から与えるキャンセルトークンソース。null の場合は内部で生成・破棄する。
        /// </summary>
        public CancellationTokenSource? CancellationTokenSource { get; init; }

        /// <summary>
        /// 進捗表示に使う <see cref="ViewModelBases.ProgressInfo"/>。null の場合は
        /// <see cref="Message"/> を用いた不確定(indeterminate)表示になる。
        /// </summary>
        public ViewModelBases.ProgressInfo? Progress { get; init; }

        /// <summary>キャンセルボタンを表示するか。</summary>
        public bool CanCancel { get; init; } = true;

        /// <summary>
        /// この時間(ms)以内に処理が完了した場合はダイアログを表示しない（チラつき防止）。
        /// .NET 製 exe の起動コスト(数百ms)を吸収できるよう既定は長めにしてある。
        /// </summary>
        public int ShowDelayMs { get; init; } = 1000;

        /// <summary>
        /// 一度ダイアログを表示したら、最低この時間(ms)は表示し続けてから閉じる（フラッシュ防止）。
        /// </summary>
        public int MinVisibleMs { get; init; } = 500;
    }
}
