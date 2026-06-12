using System.Windows.Interop;

namespace FrostNova.Core.Wpf.Common
{
    /// <summary>
    /// WPF 標準イベント（PreviewKeyDown 等）でテンキー Enter と通常 Enter を区別するためのヘルパー。
    /// Win32 WM_KEYDOWN メッセージの lParam ビット24（extended key フラグ）を監視する。
    /// テンキー Enter は extended キーとして報告される。
    /// </summary>
    public static class NumpadEnterDetector
    {
        private const int WM_KEYDOWN = 0x0100;
        private const int WM_SYSKEYDOWN = 0x0104;
        private const int VK_RETURN = 0x0D;
        private const int ExtendedKeyFlag = 1 << 24;

        private static bool _initialized;
        private static bool _isNumpadEnter;

        /// <summary>
        /// アプリケーション起動時等に一度だけ呼び出す。
        /// WPF ディスパッチャースレッドで呼ぶ必要がある。
        /// </summary>
        public static void EnsureInitialized()
        {
            if (_initialized) return;
            ComponentDispatcher.ThreadPreprocessMessage += OnThreadPreprocessMessage;
            _initialized = true;
        }

        private static void OnThreadPreprocessMessage(ref MSG msg, ref bool handled)
        {
            if (msg.message != WM_KEYDOWN && msg.message != WM_SYSKEYDOWN) return;
            if ((int)msg.wParam == VK_RETURN)
                _isNumpadEnter = ((int)msg.lParam & ExtendedKeyFlag) != 0;
        }

        /// <summary>
        /// 直前に押された Enter キーがテンキー Enter であれば true を返す。
        /// PreviewKeyDown/KeyDown ハンドラー内で e.Key == Key.Enter を確認した後に参照する。
        /// </summary>
        public static bool IsNumpadEnter => _isNumpadEnter;
    }
}
