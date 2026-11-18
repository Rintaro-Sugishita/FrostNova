using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FrostNova.Core.PathHelpers
{
    public static class PathHelper
    {
        public static string GetAbsolutePath(string relativePath, string? basePath = null)
        {
            if (IsAbsolutePath(relativePath))
            {
                return relativePath;
            }

            Uri baseUrl;
            if (basePath != null)
            {
                if (basePath.Last() != '\\')
                {
                    basePath += '\\';
                }
                baseUrl = new Uri(basePath.Replace("%", "%25"));
            }
            else
            {
                baseUrl = new Uri(AppDomain.CurrentDomain.BaseDirectory.Replace("%", "%25"));
            }

            string relativePathLocal = relativePath.Replace("%", "%25");

            Uri uri = new Uri(baseUrl, relativePathLocal);

            return uri.LocalPath;
        }


        public static bool IsAbsolutePath(string relativePath)
        {

            if (string.IsNullOrEmpty(relativePath)) { return false; }

            return (System.IO.Path.GetFullPath(relativePath) == relativePath);
        }

        public static void CopyFile(string filePath, string directoryName, bool isOverwrite)
        {
            var a = System.IO.Path.GetFileName(filePath);
            var b = System.IO.Path.Combine(directoryName, a);

            System.IO.File.Copy(filePath, b, isOverwrite);
        }

        /// <summary>
        /// Get default application path of the argument file.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static string GetDefaultApplicationPath(string path)
        {
            System.Text.StringBuilder exePath = new System.Text.StringBuilder(255);
            //fileNameに関連付けられた実行ファイルのパスを取得する
            if (FindExecutable(path, null, exePath) <= 32)
            {
                //失敗した時
                return string.Empty;
            }
            var b = exePath.ToString();
            return b;

        }

        [System.Runtime.InteropServices.DllImport("shell32.dll")]
        private static extern int FindExecutable(string lpFile, string? lpDirectory, System.Text.StringBuilder lpResult);




        public static string GetExecutingPath()
        {
            return AppDomain.CurrentDomain.BaseDirectory;
        }

        public static string GetUserDataPath(string? appName = null)
        {
            string localAppName;
            if (string.IsNullOrEmpty(appName))
            {
                localAppName = GetCurrentProcessName();
            }
            else
            {
                localAppName = appName;
            }

            string appPath = System.Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            return System.IO.Path.Combine(appPath, localAppName);
        }

        /// <summary>
        /// 現在のプロセス名(拡張子なしの exe 名)を返す。プロセス内でキャッシュする。
        /// </summary>
        /// <remarks>
        /// <c>Process.GetCurrentProcess()</c> は IDisposable なプロセスハンドルを確保するため、
        /// 呼ぶたびに破棄せず使うとハンドルが溜まる。プロセス名は実行中に変わらないので、
        /// ハンドルを持たない <see cref="Environment.ProcessPath"/> から求めて使い回す
        /// (単一ファイル発行でも実行中 exe のパスが得られる)。
        /// </remarks>
        public static string GetCurrentProcessName()
        {
            return _processName ??= ResolveCurrentProcessName();
        }

        private static string? _processName;

        private static string ResolveCurrentProcessName()
        {
            string? exePath = Environment.ProcessPath;
            if (!string.IsNullOrEmpty(exePath))
            {
                string name = System.IO.Path.GetFileNameWithoutExtension(exePath);
                if (!string.IsNullOrEmpty(name)) { return name; }
            }

            // ProcessPath が取れない環境向けのフォールバック。ハンドルは必ず破棄する。
            using var p = System.Diagnostics.Process.GetCurrentProcess();
            return p.ProcessName;
        }

        /// <summary>
        /// 設定やログなどを書き込むための「アプリ基準フォルダ」を返す。
        /// </summary>
        /// <param name="appName">
        /// ユーザーフォルダへ退避する場合に使うアプリ名。省略時はプロセス名。
        /// </param>
        /// <returns>
        /// exe 隣(<see cref="GetExecutingPath"/>)が書き込み可能ならそのパス。
        /// MSIX(ストアアプリ)実行時や、インストール先が読み取り専用(Program Files 配下など)の
        /// 場合は <see cref="GetUserDataPath(string?)"/>(= %APPDATA%\&lt;appName&gt;)。
        /// </returns>
        /// <remarks>
        /// ストアアプリのインストール先は <c>C:\Program Files\WindowsApps\...</c> で読み取り専用のため、
        /// exe 隣に書こうとすると <see cref="UnauthorizedAccessException"/> になる。
        /// 「exe 隣に置く」既定を持つ処理は <see cref="GetExecutingPath"/> ではなくこちらを使う。
        /// 非パッケージかつ書き込み可能な場所にインストールされている場合は
        /// 従来どおり exe 隣を返すので、zip 版の保存先は変わらない。
        /// </remarks>
        public static string GetWritableBasePath(string? appName = null)
        {
            if (PackagedApp.IsPackaged)
            {
                // パッケージ版はインストール先が常に読み取り専用。判定を省いて AppData へ。
                return GetUserDataPath(appName);
            }

            string baseDir = GetExecutingPath();
            if (string.IsNullOrEmpty(baseDir)) { return GetUserDataPath(appName); }

            return IsDirectoryWritable(baseDir) ? baseDir : GetUserDataPath(appName);
        }

        /// <summary>
        /// 指定フォルダに実際にファイルを作成できるかを調べる。結果はプロセス内でキャッシュする。
        /// </summary>
        /// <remarks>
        /// ACL の解釈は仮想化やストアアプリの制限で当てにならないため、
        /// 一時ファイルを作って消すことで実地に確認する。
        /// </remarks>
        public static bool IsDirectoryWritable(string directoryPath)
        {
            if (string.IsNullOrEmpty(directoryPath)) { return false; }

            return _writableCache.GetOrAdd(directoryPath, static path =>
            {
                try
                {
                    if (!System.IO.Directory.Exists(path))
                    {
                        System.IO.Directory.CreateDirectory(path);
                    }

                    string probe = System.IO.Path.Combine(path, $".fnwritetest_{Guid.NewGuid():N}.tmp");
                    using (new System.IO.FileStream(
                        probe,
                        System.IO.FileMode.CreateNew,
                        System.IO.FileAccess.Write,
                        System.IO.FileShare.None,
                        bufferSize: 1,
                        System.IO.FileOptions.DeleteOnClose))
                    {
                    }
                    return true;
                }
                catch (Exception)
                {
                    return false;
                }
            });
        }

        private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, bool> _writableCache
            = new System.Collections.Concurrent.ConcurrentDictionary<string, bool>(StringComparer.OrdinalIgnoreCase);

    }
}
