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
    /// 実行中の進捗更新。指定した項目だけが反映される（null の項目は据え置き）。
    /// <para>
    /// <c>Work</c> に渡される <see cref="IProgress{T}"/> 経由で報告すると、
    /// 更新はUIスレッドへマーシャリングされて「実行中...」ダイアログに反映される。
    /// </para>
    /// </summary>
    public sealed class ProgressReport
    {
        /// <summary>本文メッセージ。</summary>
        public string? Message { get; init; }

        /// <summary>タイトル（上部の帯）。</summary>
        public string? Title { get; init; }

        /// <summary>プログレスバーの現在値。</summary>
        public double? Value { get; init; }

        /// <summary>プログレスバーの最小値。</summary>
        public double? Minimum { get; init; }

        /// <summary>プログレスバーの最大値。</summary>
        public double? Maximum { get; init; }

        /// <summary>不確定(indeterminate)表示にするか。確定バーを動かすときは false。</summary>
        public bool? IsIndeterminate { get; init; }
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
        /// <para>
        /// 第1引数の <see cref="IProgress{T}"/> に <see cref="ProgressReport"/> を報告すると、
        /// 実行中にメッセージやプログレスバーを更新できる（UIスレッドへ自動マーシャリング）。
        /// </para>
        /// <para>
        /// 第2引数の <see cref="CancellationToken"/> がキャンセルされたら、
        /// できるだけ速やかに <see cref="OperationCanceledException"/> で抜けること。
        /// </para>
        /// </summary>
        public required Func<IProgress<ProgressReport>, CancellationToken, Task> Work { get; init; }

        /// <summary>「実行中...」ダイアログの初期本文。実行中は <c>Work</c> から更新できる。</summary>
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

        /// <summary>キャンセルボタンを表示するか。</summary>
        public bool CanCancel { get; init; } = true;

        /// <summary>
        /// 初期状態を不確定(indeterminate)表示にするか。確定プログレスを使う場合は false にし、
        /// <c>Work</c> から <see cref="ProgressReport.Value"/> を報告する。
        /// </summary>
        public bool IsIndeterminate { get; init; } = true;

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

    /// <summary>何もしない <see cref="IProgress{T}"/>（ダイアログを出さない実行時に使う）。</summary>
    internal sealed class NullProgress<T> : IProgress<T>
    {
        public static readonly NullProgress<T> Instance = new();
        public void Report(T value) { }
    }
}
