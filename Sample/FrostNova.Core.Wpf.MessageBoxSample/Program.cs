using System;
using System.Windows;
using FrostNova.Core.Wpf.Messages;

namespace FrostNova.Core.Wpf.MessageBoxSample
{
    /// <summary>
    /// FrostNovaMessageBox の「Application 無し / あり」両状況での動作確認サンプル。
    ///
    /// 確認したい退行:
    ///  1. Application.Current が null のコンソール状態でも表示でき、閉じても落ちないこと。
    ///  2. Owner が自分自身にならないこと(WPF は最初に生成した Window を MainWindow に
    ///     自動昇格させるため、対策しないと自己オーナー化で例外になる)。
    ///  3. Application を生成した後も同様に安全に表示できること。
    ///
    /// WPF ウィンドウを出すためエントリポイントは必ず STA スレッドである必要がある。
    /// </summary>
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            Console.WriteLine("=== FrostNovaMessageBox 動作確認サンプル ===");
            Console.WriteLine();

            // ---------------------------------------------------------------
            // Phase 1: Application を生成していない状態
            //   この時点で Application.Current は null。
            //   ここで落ちないこと・Owner が自分自身にならないことを確認する。
            // ---------------------------------------------------------------
            Console.WriteLine($"[Phase 1] Application 生成前: Application.Current = "
                              + (Application.Current == null ? "null" : "not null"));
            Console.WriteLine("[Phase 1] 1回目のメッセージボックスを表示します...");

            var r1a = FrostNovaMessageBox.Show(
                "Application を生成していない状態での表示です (1/2)。\n" +
                "この状態で閉じてもアプリが落ちないことを確認してください。",
                "Phase 1 / Application 無し (1/2)",
                MessageBoxButton.OKCancel,
                MessageBoxImage.Warning);
            Console.WriteLine($"[Phase 1] 1回目の結果: {r1a}");

            Console.WriteLine("[Phase 1] 2回目のメッセージボックスを表示します (連続表示)...");
            var r1b = FrostNovaMessageBox.Show(
                "Application 無しのまま連続で2回目の表示です (2/2)。\n" +
                "1回目を閉じた後でも問題なく再表示できることを確認してください。",
                "Phase 1 / Application 無し (2/2)",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);
            Console.WriteLine($"[Phase 1] 2回目の結果: {r1b}");
            Console.WriteLine();

            // ---------------------------------------------------------------
            // Phase 2: Application を生成した状態
            //   ライブラリ側では自動生成しない方針のため、利用側で明示的に用意する。
            //   リソース辞書をマージしておくと MessageWindow が正しい見た目で表示される。
            // ---------------------------------------------------------------
            Console.WriteLine("[Phase 2] Application を生成します...");
            var app = new Application
            {
                // メッセージボックスを閉じてもアプリが終了しないようにする
                ShutdownMode = ShutdownMode.OnExplicitShutdown,
            };

            // MessageWindow はボタン見た目を自己完結で持つため、
            // ボタンのスタイルを当てるためのリソースマージは不要。
            // (Converters は他機能で使う場合に備えて任意でマージしてよい)
            try
            {
                app.Resources.MergedDictionaries.Add(new ResourceDictionary
                {
                    Source = new Uri(
                        "pack://application:,,,/FrostNova.Core.Wpf;component/Resources/Converters.xaml",
                        UriKind.Absolute),
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Phase 2] リソースのマージに失敗(表示は継続): {ex.Message}");
            }

            Console.WriteLine($"[Phase 2] Application 生成後: Application.Current = "
                              + (Application.Current == null ? "null" : "not null"));
            Console.WriteLine("[Phase 2] 1回目のメッセージボックスを表示します...");

            var r2a = FrostNovaMessageBox.Show(
                "Application を生成した状態での表示です (1/2)。",
                "Phase 2 / Application あり (1/2)",
                MessageBoxButton.OKCancel,
                MessageBoxImage.Information);
            Console.WriteLine($"[Phase 2] 1回目の結果: {r2a}");

            Console.WriteLine("[Phase 2] 2回目のメッセージボックスを表示します (連続表示)...");
            var r2b = FrostNovaMessageBox.Show(
                "Application ありのまま連続で2回目の表示です (2/2)。",
                "Phase 2 / Application あり (2/2)",
                MessageBoxButton.YesNo,
                MessageBoxImage.Information);
            Console.WriteLine($"[Phase 2] 2回目の結果: {r2b}");
            Console.WriteLine();

            app.Shutdown();

            Console.WriteLine("=== 全フェーズ完了(クラッシュせず終了しました) ===");
        }
    }
}
