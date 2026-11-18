using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using FrostNova.Core.PathHelpers;
using FrostNova.Core.Wpf.Messages;

namespace FrostNova.Core.Wpf.Diagnostics;

/// <summary>
/// Provides global exception handling for the application, including logging unhandled exceptions and notifying users
/// of unexpected errors.
/// </summary>
/// <remarks>This static class centralizes exception handling for UI, task, and AppDomain unhandled exceptions. It
/// logs detailed exception information to uniquely named files in a configurable log directory and displays
/// user-friendly error messages. Use this class to ensure consistent error reporting and logging throughout the
/// application.</remarks>
public static class GlobalExceptionHandler
{
    private static string LogFolderName = "log";

    /// <summary>ログ出力先の絶対パス。指定されている場合はこれを最優先で使う。</summary>
    private static string? LogDirectoryOverride = null;

    /// <summary>ユーザーフォルダへ退避する際に使うアプリ名。null ならプロセス名。</summary>
    private static string? AppName = null;

    /// <summary>true の場合、exe 隣の書き込み可否を見ずに常に %APPDATA% 配下へ出力する。</summary>
    private static bool ForceUserDataPath = false;

    /// <summary>解決済みのログ出力先候補。設定が変わるまで使い回す。</summary>
    private static IReadOnlyList<string>? _logDirectoryCandidates;

    public static void SetLogFolder(string logFolderName)
    {
        LogFolderName = logFolderName;
        _logDirectoryCandidates = null;
    }

    /// <summary>
    /// ログ出力先を絶対パスで直接指定する。<see cref="SetLogFolder(string)"/> より優先される。
    /// </summary>
    /// <param name="absoluteDirectory">書き込み可能なフォルダの絶対パス。</param>
    public static void SetLogDirectory(string absoluteDirectory)
    {
        LogDirectoryOverride = absoluteDirectory;
        _logDirectoryCandidates = null;
    }


    /// <summary>
    /// Registers exception handling events and prepares the log directory.
    /// </summary>
    /// <param name="isDevelopment">
    /// A value indicating whether the application is running in a development environment.
    /// If <see langword="true"/>, detailed exception information is displayed.
    /// </param>
    /// <remarks>
    /// ログは exe 隣の <c>log</c> フォルダに出力するが、そこが書き込み不可の場合
    /// (MSIX ストアアプリ・Program Files 配下インストールなど)は自動的に
    /// <c>%APPDATA%\&lt;アプリ名&gt;\log</c> へ退避する。
    /// ストアアプリ専用の明示的な初期化は <see cref="InitializeForPackagedApp(string?, bool)"/>。
    /// </remarks>
    public static void Initialize(bool isDevelopment = false)
    {
        Application.Current.DispatcherUnhandledException += (s, e) =>
        {
            HandleException(e.Exception, "UI Dispatcher", isDevelopment);
            e.Handled = true;
        };

        TaskScheduler.UnobservedTaskException += (s, e) =>
        {
            HandleException(e.Exception, "Task Scheduler", isDevelopment);
            e.SetObserved();
        };

        AppDomain.CurrentDomain.UnhandledException += (s, e) =>
        {
            if (e.ExceptionObject is Exception ee)
                HandleException(ee, "AppDomain", isDevelopment);
        };
    }

    /// <summary>
    /// MSIX(ストアアプリ)向けの初期化。ログを <c>%APPDATA%\&lt;appName&gt;\&lt;ログフォルダ名&gt;</c> へ出力する。
    /// </summary>
    /// <param name="appName">
    /// %APPDATA% 配下に作るフォルダ名。省略時はプロセス名。
    /// zip 版と設定を共有したい場合は zip 版と同じ名前を渡す。
    /// </param>
    /// <param name="isDevelopment">
    /// 開発環境として扱うか。<see langword="true"/> なら例外の詳細をそのまま表示する。
    /// </param>
    /// <remarks>
    /// ストアアプリのインストール先(<c>C:\Program Files\WindowsApps\...</c>)は読み取り専用で、
    /// 既定の「exe 隣の log フォルダ」へ書こうとすると
    /// <see cref="UnauthorizedAccessException"/> になり、例外処理自体が失敗する。
    /// パッケージ版と zip 版で同じバイナリを配る場合は、
    /// <see cref="PackagedApp.IsPackaged"/> で分岐せずこのメソッドを常に呼んでも問題ない
    /// (非パッケージ版でも %APPDATA% 配下に出力されるだけ)。
    /// 既定の <see cref="Initialize(bool)"/> も書き込み不可なら自動退避するため、
    /// 置き場所を明示したいとき以外は <see cref="Initialize(bool)"/> でも動作する。
    /// </remarks>
    public static void InitializeForPackagedApp(string? appName = null, bool isDevelopment = false)
    {
        AppName = appName;
        ForceUserDataPath = true;
        _logDirectoryCandidates = null;
        Initialize(isDevelopment);
    }

    /// <summary>
    /// Handles the exception by logging to a unique file and notifying the user.
    /// </summary>
    private static void HandleException(Exception ex, string source, bool isDevelop)
    {
        if (ex == null) return;

        try
        {
            // ログ内容の構築
            var logContent = BuildLogMessage(ex, source);

            // ファイルへの書き出し（1例外 1ファイル）
            string logFilePath = SaveLogToFile(logContent);

            // ユーザーへの通知
            ShowErrorMessage(ex, logFilePath, isDevelop);
        }
        catch (Exception fatalEx)
        {
            // ログ出力自体が失敗した場合の最終手段
            MessageBox.Show($"Failed to write the log file: {fatalEx.Message}", "Fatal Error");
        }
    }

    /// <summary>
    /// Builds a formatted log message string.
    /// </summary>
    private static string BuildLogMessage(Exception ex, string source)
    {
        string appPath = Environment.ProcessPath ?? string.Empty;

        string appVersion = "Unknown";
        if (!string.IsNullOrEmpty(appPath))
        {
            try
            {
                // ファイルのプロパティからバージョン情報を取得
                var versionInfo = FileVersionInfo.GetVersionInfo(appPath);
                appVersion = versionInfo.FileVersion ?? "Unknown";
            }
            catch
            {
                // フォールバック: アセンブリの情報から取得
                var assembly = Assembly.GetEntryAssembly();
                appVersion = assembly?.GetName().Version?.ToString() ?? "Unknown";
            }
        }        // バージョン情報の取得（AssemblyVersionではなくFileVersionが実用的です）

        var sb = new StringBuilder();
        sb.AppendLine("==================================================");
        sb.AppendLine($"Timestamp: {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");
        sb.AppendLine($"App Path:  {appPath}");
        sb.AppendLine($"Version:   {appVersion}");
        sb.AppendLine($"Source:    {source}");
        sb.AppendLine($"Exception: {ex.GetType().FullName}");
        sb.AppendLine($"Message:   {ex.Message}");
        sb.AppendLine("==================================================");
        sb.AppendLine("StackTrace:");
        sb.AppendLine(ex.StackTrace);

        var inner = ex.InnerException;
        while (inner != null)
        {
            sb.AppendLine("\n[Inner Exception]");
            sb.AppendLine($"Type:    {inner.GetType().FullName}");
            sb.AppendLine($"Message: {inner.Message}");
            sb.AppendLine(inner.StackTrace);
            inner = inner.InnerException;
        }
        return sb.ToString();
    }

    /// <summary>
    /// Saves the log content to a new file in the log directory.
    /// </summary>
    private static string SaveLogToFile(string content)
    {
        // ファイル名の生成 (yyyyMMdd_HHmmss_Guid.log)
        string fileName = $"{DateTime.Now:yyyyMMdd_HHmmss}_{Guid.NewGuid():N}.log";

        Exception? lastError = null;
        foreach (var logDir in GetLogDirectoryCandidates())
        {
            try
            {
                // logフォルダがなければ作成
                if (!Directory.Exists(logDir))
                {
                    Directory.CreateDirectory(logDir);
                }

                string filePath = Path.Combine(logDir, fileName);
                File.WriteAllText(filePath, content, Encoding.UTF8);
                return filePath;
            }
            catch (Exception ex)
            {
                // 読み取り専用・アクセス拒否などは次の候補へ。
                lastError = ex;
            }
        }

        throw lastError ?? new IOException("No log directory candidate was available.");
    }

    /// <summary>
    /// ログ出力先の候補を優先順に返す。
    /// </summary>
    /// <remarks>
    /// 先頭が既定の出力先。ストアアプリ等で書き込みに失敗しても例外処理そのものが
    /// 落ちないよう、%APPDATA% 配下と一時フォルダをフォールバックとして続ける。
    /// 解決結果はキャッシュし、出力先の設定が変わったときだけ作り直す
    /// (書き込み可否の実地判定も PathHelper 側でキャッシュされるため、プロセス内で1回だけ)。
    /// </remarks>
    private static IReadOnlyList<string> GetLogDirectoryCandidates()
    {
        return _logDirectoryCandidates ??= BuildLogDirectoryCandidates();
    }

    private static IReadOnlyList<string> BuildLogDirectoryCandidates()
    {
        var candidates = new List<string>();

        void Add(string dir)
        {
            if (string.IsNullOrEmpty(dir)) { return; }
            if (candidates.Any(x => string.Equals(x, dir, StringComparison.OrdinalIgnoreCase))) { return; }
            candidates.Add(dir);
        }

        if (!string.IsNullOrEmpty(LogDirectoryOverride))
        {
            Add(LogDirectoryOverride!);
        }
        else if (ForceUserDataPath)
        {
            Add(Path.Combine(PathHelper.GetUserDataPath(AppName), LogFolderName));
        }
        else
        {
            // exe 隣が書き込み可能ならそこ。読み取り専用なら %APPDATA% 配下。
            Add(Path.Combine(PathHelper.GetWritableBasePath(AppName), LogFolderName));
        }

        // 最終フォールバック
        Add(Path.Combine(PathHelper.GetUserDataPath(AppName), LogFolderName));
        Add(Path.Combine(Path.GetTempPath(), AppName ?? "FrostNova", LogFolderName));

        return candidates;
    }

    /// <summary>
    /// Displays an error dialog to the user.
    /// </summary>
    private static void ShowErrorMessage(Exception ex, string logPath, bool isDevelop)
    {
        string message;
        if (isDevelop || Debugger.IsAttached)
        {
            message = "An unexpected error has occurred.\n\n" +
                                 $"Error: {ex}";
        }
        else
        {
            message = "An unexpected error has occurred.\n\n" +
                                 $"Error: {ex.Message}\n\n" +
                                 $"For more details, please check the log file at:\n{logPath}";
        }

        FrostNovaMessageBox.Show(message, "Unexpected Error", MessageBoxButton.OK, MessageBoxImage.Error);
    }
}
