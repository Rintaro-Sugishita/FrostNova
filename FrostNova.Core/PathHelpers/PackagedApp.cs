using System;
using System.Runtime.InteropServices;
using System.Text;

namespace FrostNova.Core.PathHelpers
{
    /// <summary>
    /// 実行中のプロセスが MSIX(ストアアプリ)としてパッケージ実行されているかの判定。
    /// </summary>
    /// <remarks>
    /// zip 版とストア版で同じバイナリを配る構成では、「パッケージ版だけ作法が変わる処理」
    /// (書き込み先・常駐登録・自動更新など)をここで振り分ける。
    /// パッケージ版はインストール先が <c>C:\Program Files\WindowsApps\...</c> になり
    /// 読み取り専用なので、exe 隣への書き込みは必ず失敗する
    /// (<see cref="System.UnauthorizedAccessException"/>)。
    /// 書き込み先の決定には <see cref="PathHelper.GetWritableBasePath(string?)"/> を使うこと。
    /// </remarks>
    public static class PackagedApp
    {
        /// <summary>APPMODEL_ERROR_NO_PACKAGE。パッケージ実行ではない場合に返る。</summary>
        private const int APPMODEL_ERROR_NO_PACKAGE = 15700;

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = false)]
        private static extern int GetCurrentPackageFullName(ref int packageFullNameLength, StringBuilder? packageFullName);

        /// <summary>MSIX パッケージとして実行中なら true。プロセス内で結果をキャッシュする。</summary>
        public static bool IsPackaged => _isPackaged ??= DetectIsPackaged();

        private static bool? _isPackaged;

        private static bool DetectIsPackaged()
        {
            if (!OperatingSystem.IsWindows()) { return false; }

            try
            {
                // バッファ長 0 / バッファ null で呼ぶと、パッケージ実行なら
                // ERROR_INSUFFICIENT_BUFFER、非パッケージなら APPMODEL_ERROR_NO_PACKAGE が返る。
                int length = 0;
                int result = GetCurrentPackageFullName(ref length, null);
                return result != APPMODEL_ERROR_NO_PACKAGE;
            }
            catch (Exception)
            {
                // エントリポイントが無い(Windows 8 未満)等の想定外は「非パッケージ」として扱う。
                return false;
            }
        }
    }
}
